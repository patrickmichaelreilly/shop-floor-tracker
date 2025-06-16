using System.Data.SqlServerCe;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShopFloorTracker.Application.Interfaces;
using ShopFloorTracker.Core.Entities;
using ShopFloorTracker.Core.Enums;
using ShopFloorTracker.Infrastructure.Data;

namespace ShopFloorTracker.Application.Services;

public class MicrovellumImportService : IMicrovellumImportService
{
    private readonly ShopFloorDbContext _context;
    private readonly ILogger<MicrovellumImportService> _logger;

    public MicrovellumImportService(ShopFloorDbContext context, ILogger<MicrovellumImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ImportResult> ImportWorkOrderAsync(string sdfFilePath)
    {
        var result = new ImportResult();
        
        try
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            // Connect to SQL CE file
            var connectionString = $"Data Source={sdfFilePath}";
            using var sdfConnection = new SqlCeConnection(connectionString);
            await sdfConnection.OpenAsync();
            
            // Import in correct order (dependencies first)
            var workOrderData = await ExtractWorkOrdersAsync(sdfConnection);
            var workOrders = await ImportWorkOrdersAsync(workOrderData);
            result.WorkOrdersCreated = workOrders.Count;
            
            var productData = await ExtractProductsAsync(sdfConnection);
            var products = await ImportProductsAsync(productData, workOrders);
            result.ProductsCreated = products.Count;
            
            var placedSheetData = await ExtractPlacedSheetsAsync(sdfConnection);
            var placedSheets = await ImportPlacedSheetsAsync(placedSheetData, workOrders);
            result.PlacedSheetsCreated = placedSheets.Count;
            
            var partData = await ExtractPartsAsync(sdfConnection);
            var parts = await ImportPartsAsync(partData, products, workOrders);
            result.PartsCreated = parts.Count;
            
            var optimizationData = await ExtractOptimizationResultsAsync(sdfConnection);
            var placements = await ImportPartPlacementsAsync(optimizationData, parts, placedSheets);
            result.PartPlacementsCreated = placements.Count;
            
            var hardwareData = await ExtractHardwareAsync(sdfConnection);
            var hardware = await ImportHardwareAsync(hardwareData, products, workOrders);
            result.HardwareCreated = hardware.Count;
            
            // Log import history
            var importHistory = new ImportHistory
            {
                FileName = Path.GetFileName(sdfFilePath),
                Status = "Success",
                RecordsImported = result.WorkOrdersCreated + result.ProductsCreated + result.PartsCreated + result.HardwareCreated,
                WorkOrdersCreated = result.WorkOrdersCreated,
                ProductsCreated = result.ProductsCreated,
                PartsCreated = result.PartsCreated,
                HardwareCreated = result.HardwareCreated,
                PlacedSheetsCreated = result.PlacedSheetsCreated,
                PartPlacementsCreated = result.PartPlacementsCreated,
                FilePath = sdfFilePath,
                FileSize = new FileInfo(sdfFilePath).Length
            };
            
            _context.ImportHistory.Add(importHistory);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            result.Success = true;
            result.Message = $"Successfully imported {result.WorkOrdersCreated + result.ProductsCreated + result.PartsCreated + result.HardwareCreated} records";
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing work order from {FilePath}", sdfFilePath);
            result.Success = false;
            result.Message = ex.Message;
            result.Errors.Add(ex.Message);
        }
        
        return result;
    }

    public async Task<List<string>> GetAvailableFilesAsync()
    {
        var importFolder = Path.Combine(Environment.CurrentDirectory, "Import");
        if (!Directory.Exists(importFolder))
        {
            Directory.CreateDirectory(importFolder);
            return new List<string>();
        }

        var sdfFiles = Directory.GetFiles(importFolder, "*.sdf")
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .Cast<string>()
            .ToList();

        return await Task.FromResult(sdfFiles);
    }

    #region Data Extraction Methods

    private async Task<List<dynamic>> ExtractWorkOrdersAsync(SqlCeConnection connection)
    {
        // Extract unique work orders from Products table
        var sql = @"
            SELECT DISTINCT 
                LinkIDWorkOrder,
                WorkOrderName,
                RoomName
            FROM Products 
            WHERE LinkIDWorkOrder IS NOT NULL";
            
        var command = new SqlCeCommand(sql, connection);
        var reader = await command.ExecuteReaderAsync();
        
        var results = new List<dynamic>();
        while (await reader.ReadAsync())
        {
            results.Add(new
            {
                LinkIDWorkOrder = reader["LinkIDWorkOrder"]?.ToString(),
                WorkOrderName = reader["WorkOrderName"]?.ToString(),
                RoomName = reader["RoomName"]?.ToString()
            });
        }
        reader.Close();
        
        return results;
    }

    private async Task<List<dynamic>> ExtractProductsAsync(SqlCeConnection connection)
    {
        var sql = @"
            SELECT 
                LinkIDProduct,
                LinkIDWorkOrder,
                ProductCode,
                ProductName,
                ProductClass,
                RoomName,
                CabinetType
            FROM Products 
            WHERE LinkIDProduct IS NOT NULL";
            
        var command = new SqlCeCommand(sql, connection);
        var reader = await command.ExecuteReaderAsync();
        
        var results = new List<dynamic>();
        while (await reader.ReadAsync())
        {
            results.Add(new
            {
                LinkIDProduct = reader["LinkIDProduct"]?.ToString(),
                LinkIDWorkOrder = reader["LinkIDWorkOrder"]?.ToString(),
                ProductCode = reader["ProductCode"]?.ToString(),
                ProductName = reader["ProductName"]?.ToString(),
                ProductClass = reader["ProductClass"]?.ToString(),
                RoomName = reader["RoomName"]?.ToString(),
                CabinetType = reader["CabinetType"]?.ToString()
            });
        }
        reader.Close();
        
        return results;
    }

    private async Task<List<dynamic>> ExtractPlacedSheetsAsync(SqlCeConnection connection)
    {
        var sql = @"
            SELECT DISTINCT
                SheetName,
                BarCode,
                FileName,
                MaterialType,
                Length,
                Width,
                Thickness
            FROM PlacedSheets 
            WHERE SheetName IS NOT NULL";
            
        var command = new SqlCeCommand(sql, connection);
        var reader = await command.ExecuteReaderAsync();
        
        var results = new List<dynamic>();
        while (await reader.ReadAsync())
        {
            results.Add(new
            {
                SheetName = reader["SheetName"]?.ToString(),
                BarCode = reader["BarCode"]?.ToString(),
                FileName = reader["FileName"]?.ToString(),
                MaterialType = reader["MaterialType"]?.ToString(),
                Length = reader["Length"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["Length"]),
                Width = reader["Width"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["Width"]),
                Thickness = reader["Thickness"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["Thickness"])
            });
        }
        reader.Close();
        
        return results;
    }

    private async Task<List<dynamic>> ExtractPartsAsync(SqlCeConnection connection)
    {
        var sql = @"
            SELECT 
                p.LinkIDPart,
                p.LinkIDProduct,
                p.PartName,
                p.MaterialName,
                p.MaterialCode,
                p.Length,
                p.Width,
                p.Thickness,
                p.EdgeBanding,
                p.Quantity,
                prod.LinkIDWorkOrder
            FROM Parts p
            INNER JOIN Products prod ON p.LinkIDProduct = prod.LinkIDProduct
            WHERE p.LinkIDPart IS NOT NULL";
            
        var command = new SqlCeCommand(sql, connection);
        var reader = await command.ExecuteReaderAsync();
        
        var results = new List<dynamic>();
        while (await reader.ReadAsync())
        {
            results.Add(new
            {
                LinkIDPart = reader["LinkIDPart"]?.ToString(),
                LinkIDProduct = reader["LinkIDProduct"]?.ToString(),
                LinkIDWorkOrder = reader["LinkIDWorkOrder"]?.ToString(),
                PartName = reader["PartName"]?.ToString(),
                MaterialName = reader["MaterialName"]?.ToString(),
                MaterialCode = reader["MaterialCode"]?.ToString(),
                Length = reader["Length"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["Length"]),
                Width = reader["Width"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["Width"]),
                Thickness = reader["Thickness"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["Thickness"]),
                EdgeBanding = reader["EdgeBanding"]?.ToString(),
                Quantity = reader["Quantity"] is DBNull ? 1 : Convert.ToInt32(reader["Quantity"])
            });
        }
        reader.Close();
        
        return results;
    }

    private async Task<List<dynamic>> ExtractOptimizationResultsAsync(SqlCeConnection connection)
    {
        var sql = @"
            SELECT 
                opt.LinkIDPart,
                opt.SheetName,
                opt.XCoord,
                opt.YCoord,
                opt.Rotation,
                opt.IsFlipped
            FROM OptimizationResults opt
            WHERE opt.LinkIDPart IS NOT NULL AND opt.SheetName IS NOT NULL";
            
        var command = new SqlCeCommand(sql, connection);
        var reader = await command.ExecuteReaderAsync();
        
        var results = new List<dynamic>();
        while (await reader.ReadAsync())
        {
            results.Add(new
            {
                LinkIDPart = reader["LinkIDPart"]?.ToString(),
                SheetName = reader["SheetName"]?.ToString(),
                XCoord = reader["XCoord"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["XCoord"]),
                YCoord = reader["YCoord"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["YCoord"]),
                Rotation = reader["Rotation"] is DBNull ? 0 : Convert.ToInt32(reader["Rotation"]),
                IsFlipped = reader["IsFlipped"] is DBNull ? false : Convert.ToBoolean(reader["IsFlipped"])
            });
        }
        reader.Close();
        
        return results;
    }

    private async Task<List<dynamic>> ExtractHardwareAsync(SqlCeConnection connection)
    {
        var sql = @"
            SELECT 
                h.LinkIDHardware,
                h.LinkIDProduct,
                h.HardwareName,
                h.HardwareDescription,
                h.Quantity,
                prod.LinkIDWorkOrder
            FROM Hardware h
            INNER JOIN Products prod ON h.LinkIDProduct = prod.LinkIDProduct
            WHERE h.LinkIDHardware IS NOT NULL";
            
        var command = new SqlCeCommand(sql, connection);
        var reader = await command.ExecuteReaderAsync();
        
        var results = new List<dynamic>();
        while (await reader.ReadAsync())
        {
            results.Add(new
            {
                LinkIDHardware = reader["LinkIDHardware"]?.ToString(),
                LinkIDProduct = reader["LinkIDProduct"]?.ToString(),
                LinkIDWorkOrder = reader["LinkIDWorkOrder"]?.ToString(),
                HardwareName = reader["HardwareName"]?.ToString(),
                HardwareDescription = reader["HardwareDescription"]?.ToString(),
                Quantity = reader["Quantity"] is DBNull ? 1 : Convert.ToInt32(reader["Quantity"])
            });
        }
        reader.Close();
        
        return results;
    }

    #endregion

    #region Data Import Methods

    private async Task<List<WorkOrder>> ImportWorkOrdersAsync(List<dynamic> workOrderData)
    {
        var workOrders = new List<WorkOrder>();
        
        // Get all existing work orders to check against
        var existingWorkOrders = await _context.WorkOrders.ToListAsync();
        
        foreach (var data in workOrderData)
        {
            var linkId = data.LinkIDWorkOrder?.ToString();
            if (string.IsNullOrEmpty(linkId)) continue;

            // Check if work order already exists
            var existingWorkOrder = existingWorkOrders
                .FirstOrDefault(w => w.MicrovellumLinkID == linkId);
            
            if (existingWorkOrder == null)
            {
                var workOrder = new WorkOrder
                {
                    WorkOrderId = Guid.NewGuid().ToString(),
                    WorkOrderNumber = data.WorkOrderName ?? $"WO-{linkId}",
                    CustomerName = data.RoomName ?? "Imported Customer",
                    Status = WorkOrderStatus.Active,
                    MicrovellumLinkID = linkId,
                    ImportedBy = "MicrovellumImport",
                    ImportFilePath = "SDF Import"
                };

                _context.WorkOrders.Add(workOrder);
                workOrders.Add(workOrder);
            }
            else
            {
                workOrders.Add(existingWorkOrder);
            }
        }
        
        await _context.SaveChangesAsync();
        return workOrders;
    }

    private async Task<List<Product>> ImportProductsAsync(List<dynamic> productData, List<WorkOrder> workOrders)
    {
        var products = new List<Product>();
        
        // Get all existing products to check against
        var existingProducts = await _context.Products.ToListAsync();
        
        foreach (var data in productData)
        {
            var linkId = data.LinkIDProduct?.ToString();
            var workOrderLinkId = data.LinkIDWorkOrder?.ToString();
            if (string.IsNullOrEmpty(linkId) || string.IsNullOrEmpty(workOrderLinkId)) continue;

            var workOrder = workOrders.FirstOrDefault(w => w.MicrovellumLinkID == workOrderLinkId);
            if (workOrder == null) continue;

            // Check if product already exists
            var existingProduct = existingProducts
                .FirstOrDefault(p => p.MicrovellumLinkID == linkId);
            
            if (existingProduct == null)
            {
                var product = new Product
                {
                    ProductId = Guid.NewGuid().ToString(),
                    WorkOrderId = workOrder.WorkOrderId,
                    ProductNumber = data.ProductCode ?? $"PRD-{linkId}",
                    ProductName = data.ProductName ?? "Imported Product",
                    ProductType = data.CabinetType ?? data.ProductClass ?? "Cabinet",
                    Status = ProductStatus.Pending,
                    MicrovellumLinkID = linkId
                };

                _context.Products.Add(product);
                products.Add(product);
            }
            else
            {
                products.Add(existingProduct);
            }
        }
        
        await _context.SaveChangesAsync();
        return products;
    }

    private async Task<List<PlacedSheet>> ImportPlacedSheetsAsync(List<dynamic> placedSheetData, List<WorkOrder> workOrders)
    {
        var placedSheets = new List<PlacedSheet>();
        
        // For simplicity, assign placed sheets to the first work order
        // In a real scenario, you'd need to determine the correct work order based on the data
        var defaultWorkOrder = workOrders.FirstOrDefault();
        if (defaultWorkOrder == null) return placedSheets;

        // Get all existing placed sheets to check against
        var existingSheets = await _context.PlacedSheets.ToListAsync();
        
        foreach (var data in placedSheetData)
        {
            var sheetName = data.SheetName?.ToString();
            if (string.IsNullOrEmpty(sheetName)) continue;

            // Check if placed sheet already exists
            var existingSheet = existingSheets
                .FirstOrDefault(ps => ps.SheetName == sheetName);
            
            if (existingSheet == null)
            {
                var placedSheet = new PlacedSheet
                {
                    PlacedSheetId = Guid.NewGuid().ToString(),
                    SheetName = sheetName,
                    BarCode = data.BarCode ?? sheetName,
                    FileName = data.FileName ?? $"{sheetName}.nc",
                    WorkOrderId = defaultWorkOrder.WorkOrderId,
                    MaterialType = data.MaterialType,
                    Length = data.Length,
                    Width = data.Width,
                    Thickness = data.Thickness,
                    Status = "Pending",
                    MicrovellumLinkID = sheetName // Using sheet name as link ID
                };

                _context.PlacedSheets.Add(placedSheet);
                placedSheets.Add(placedSheet);
            }
            else
            {
                placedSheets.Add(existingSheet);
            }
        }
        
        await _context.SaveChangesAsync();
        return placedSheets;
    }

    private async Task<List<Part>> ImportPartsAsync(List<dynamic> partData, List<Product> products, List<WorkOrder> workOrders)
    {
        var parts = new List<Part>();
        
        // Get all existing parts to check against
        var existingParts = await _context.Parts.ToListAsync();
        
        foreach (var data in partData)
        {
            var linkId = data.LinkIDPart?.ToString();
            var productLinkId = data.LinkIDProduct?.ToString();
            if (string.IsNullOrEmpty(linkId) || string.IsNullOrEmpty(productLinkId)) continue;

            var product = products.FirstOrDefault(p => p.MicrovellumLinkID == productLinkId);
            if (product == null) continue;

            // Check if part already exists
            var existingPart = existingParts
                .FirstOrDefault(p => p.MicrovellumLinkID == linkId);
            
            if (existingPart == null)
            {
                // Create multiple parts if quantity > 1
                var quantity = data.Quantity;
                for (int i = 0; i < quantity; i++)
                {
                    var part = new Part
                    {
                        PartId = Guid.NewGuid().ToString(),
                        ProductId = product.ProductId,
                        PartNumber = quantity > 1 ? $"{linkId}-{i + 1}" : linkId,
                        PartName = data.PartName ?? "Imported Part",
                        MaterialName = data.MaterialName,
                        MaterialCode = data.MaterialCode,
                        Length = data.Length,
                        Width = data.Width,
                        Thickness = data.Thickness,
                        EdgeBanding = data.EdgeBanding,
                        Status = PartStatus.Pending,
                        MicrovellumLinkID = linkId
                    };

                    _context.Parts.Add(part);
                    parts.Add(part);
                }
            }
            else
            {
                parts.Add(existingPart);
            }
        }
        
        await _context.SaveChangesAsync();
        return parts;
    }

    private async Task<List<PartPlacement>> ImportPartPlacementsAsync(List<dynamic> optimizationData, List<Part> parts, List<PlacedSheet> placedSheets)
    {
        var placements = new List<PartPlacement>();
        
        foreach (var data in optimizationData)
        {
            var partLinkId = data.LinkIDPart;
            var sheetName = data.SheetName;
            if (string.IsNullOrEmpty(partLinkId) || string.IsNullOrEmpty(sheetName)) continue;

            var part = parts.FirstOrDefault(p => p.MicrovellumLinkID == partLinkId);
            var sheet = placedSheets.FirstOrDefault(s => s.SheetName == sheetName);
            
            if (part == null || sheet == null) continue;

            var placement = new PartPlacement
            {
                PartId = part.PartId,
                PlacedSheetId = sheet.PlacedSheetId,
                XCoord = data.XCoord,
                YCoord = data.YCoord,
                Rotation = data.Rotation,
                IsFlipped = data.IsFlipped
            };

            _context.PartPlacements.Add(placement);
            placements.Add(placement);
        }
        
        await _context.SaveChangesAsync();
        return placements;
    }

    private async Task<List<Hardware>> ImportHardwareAsync(List<dynamic> hardwareData, List<Product> products, List<WorkOrder> workOrders)
    {
        var hardwareList = new List<Hardware>();
        
        // Get all existing hardware to check against
        var existingHardwareList = await _context.Hardware.ToListAsync();
        
        foreach (var data in hardwareData)
        {
            var linkId = data.LinkIDHardware?.ToString();
            var productLinkId = data.LinkIDProduct?.ToString();
            var workOrderLinkId = data.LinkIDWorkOrder?.ToString();
            
            if (string.IsNullOrEmpty(linkId) || string.IsNullOrEmpty(productLinkId)) continue;

            var product = products.FirstOrDefault(p => p.MicrovellumLinkID == productLinkId);
            var workOrder = workOrders.FirstOrDefault(w => w.MicrovellumLinkID == workOrderLinkId);
            
            if (product == null || workOrder == null) continue;

            // Check if hardware already exists
            var existingHardware = existingHardwareList
                .FirstOrDefault(h => h.MicrovellumLinkID == linkId);
            
            if (existingHardware == null)
            {
                var hardware = new Hardware
                {
                    HardwareId = Guid.NewGuid().ToString(),
                    HardwareName = data.HardwareName ?? "Imported Hardware",
                    HardwareDescription = data.HardwareDescription,
                    ProductId = product.ProductId,
                    WorkOrderId = workOrder.WorkOrderId,
                    Quantity = data.Quantity,
                    Status = "Pending",
                    MicrovellumLinkID = linkId
                };

                _context.Hardware.Add(hardware);
                hardwareList.Add(hardware);
            }
            else
            {
                hardwareList.Add(existingHardware);
            }
        }
        
        await _context.SaveChangesAsync();
        return hardwareList;
    }

    #endregion
}