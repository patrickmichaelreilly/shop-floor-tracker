using System.Diagnostics;
using System.Text.Json;
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
            if (!File.Exists(sdfFilePath))
            {
                result.Success = false;
                result.Message = "SDF file not found.";
                result.Errors.Add($"File not found: {sdfFilePath}");
                return result;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            
            var sdfData = await ExtractSdfDataAsync(sdfFilePath);
            if (sdfData == null)
            {
                result.Success = false;
                result.Message = "SDF Reader failed to process file.";
                result.Errors.Add("The SDF reader executable (SdfReader.exe) failed to process the file.");
                result.Errors.Add("SDF files require a .NET Framework 4.8 application to read SQL Server Compact Edition files.");
                result.Errors.Add("Running on Windows runtime for native SDF file support.");
                result.Errors.Add("SOLUTION: Ensure SdfReader.exe is built for Windows .NET Framework and properly deployed.");
                result.Errors.Add("ALTERNATIVE: Export your Microvellum data to CSV format and use the CSV import feature instead.");
                result.Errors.Add($"Expected location: {GetSdfReaderPath()}");
                return result;
            }

            var workOrders = await ImportWorkOrdersFromJsonAsync(sdfData);
            result.WorkOrdersCreated = workOrders.Count;
            
            var products = await ImportProductsFromJsonAsync(sdfData, workOrders);
            result.ProductsCreated = products.Count;
            
            var placedSheets = await ImportPlacedSheetsFromJsonAsync(sdfData, workOrders);
            result.PlacedSheetsCreated = placedSheets.Count;
            
            var parts = await ImportPartsFromJsonAsync(sdfData, products, workOrders);
            result.PartsCreated = parts.Count;
            
            var placements = await ImportPartPlacementsFromJsonAsync(sdfData, parts, placedSheets);
            result.PartPlacementsCreated = placements.Count;
            
            var hardware = await ImportHardwareFromJsonAsync(sdfData, products, workOrders);
            result.HardwareCreated = hardware.Count;
            
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

    #region SDF Data Extraction

    private string GetSdfReaderPath()
    {
        var appDirectory = AppContext.BaseDirectory;
        return Path.Combine(appDirectory, "SdfReader.exe");
    }

    private async Task<Dictionary<string, JsonElement>?> ExtractSdfDataAsync(string sdfFilePath)
    {
        try
        {
            var sdfReaderPath = GetSdfReaderPath();
            
            if (!File.Exists(sdfReaderPath))
            {
                _logger.LogError("SDF Reader executable not found at: {Path}", sdfReaderPath);
                _logger.LogError("The SDF Reader must be built on Windows with .NET Framework 4.8");
                _logger.LogError("Running natively on Windows for SDF file support");
                _logger.LogError("Please build the SdfReader project on a Windows machine and copy SdfReader.exe to: {Path}", Path.GetDirectoryName(sdfReaderPath));
                return null;
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = sdfReaderPath,
                Arguments = $"\"{sdfFilePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(sdfReaderPath)
            };

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                _logger.LogError("Failed to start SDF reader process");
                return null;
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError("SDF reader process failed with exit code {ExitCode}. Error: {Error}", process.ExitCode, error);
                return null;
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                _logger.LogError("SDF reader process returned empty output");
                return null;
            }

            var jsonData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(output);
            _logger.LogInformation("Successfully extracted data from SDF file using external reader");
            
            return jsonData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting data from SDF file using external reader");
            return null;
        }
    }

    #endregion

    #region JSON Data Import Methods

    private async Task<List<WorkOrder>> ImportWorkOrdersFromJsonAsync(Dictionary<string, JsonElement> sdfData)
    {
        var workOrders = new List<WorkOrder>();
        
        if (!sdfData.ContainsKey("Products")) return workOrders;
        
        var existingWorkOrders = await _context.WorkOrders.ToListAsync();
        var processedWorkOrderIds = new HashSet<string>();
        
        foreach (var productElement in sdfData["Products"].EnumerateArray())
        {
            if (!productElement.TryGetProperty("LinkIDWorkOrder", out var linkIdProp)) continue;
            
            var linkId = linkIdProp.GetString();
            if (string.IsNullOrEmpty(linkId) || processedWorkOrderIds.Contains(linkId)) continue;
            
            processedWorkOrderIds.Add(linkId);

            var existingWorkOrder = existingWorkOrders.FirstOrDefault(w => w.MicrovellumLinkID == linkId);
            
            if (existingWorkOrder == null)
            {
                var workOrderName = productElement.TryGetProperty("WorkOrderName", out var nameProp) ? nameProp.GetString() : null;
                var roomName = productElement.TryGetProperty("RoomName", out var roomProp) ? roomProp.GetString() : null;
                
                var workOrder = new WorkOrder
                {
                    WorkOrderId = Guid.NewGuid().ToString(),
                    WorkOrderNumber = workOrderName ?? $"WO-{linkId}",
                    CustomerName = roomName ?? "Imported Customer",
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

    private async Task<List<Product>> ImportProductsFromJsonAsync(Dictionary<string, JsonElement> sdfData, List<WorkOrder> workOrders)
    {
        var products = new List<Product>();
        
        if (!sdfData.ContainsKey("Products")) return products;
        
        var existingProducts = await _context.Products.ToListAsync();
        
        foreach (var productElement in sdfData["Products"].EnumerateArray())
        {
            if (!productElement.TryGetProperty("LinkIDProduct", out var linkIdProp) ||
                !productElement.TryGetProperty("LinkIDWorkOrder", out var workOrderLinkIdProp)) continue;
            
            var linkId = linkIdProp.GetString();
            var workOrderLinkId = workOrderLinkIdProp.GetString();
            
            if (string.IsNullOrEmpty(linkId) || string.IsNullOrEmpty(workOrderLinkId)) continue;

            var workOrder = workOrders.FirstOrDefault(w => w.MicrovellumLinkID == workOrderLinkId);
            if (workOrder == null) continue;

            var existingProduct = existingProducts.FirstOrDefault(p => p.MicrovellumLinkID == linkId);
            
            if (existingProduct == null)
            {
                var productCode = productElement.TryGetProperty("ProductCode", out var codeProp) ? codeProp.GetString() : null;
                var productName = productElement.TryGetProperty("ProductName", out var nameProp) ? nameProp.GetString() : null;
                var cabinetType = productElement.TryGetProperty("CabinetType", out var typeProp) ? typeProp.GetString() : null;
                var productClass = productElement.TryGetProperty("ProductClass", out var classProp) ? classProp.GetString() : null;
                
                var product = new Product
                {
                    ProductId = Guid.NewGuid().ToString(),
                    WorkOrderId = workOrder.WorkOrderId,
                    ProductNumber = productCode ?? $"PRD-{linkId}",
                    ProductName = productName ?? "Imported Product",
                    ProductType = cabinetType ?? productClass ?? "Cabinet",
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

    private async Task<List<PlacedSheet>> ImportPlacedSheetsFromJsonAsync(Dictionary<string, JsonElement> sdfData, List<WorkOrder> workOrders)
    {
        var placedSheets = new List<PlacedSheet>();
        
        if (!sdfData.ContainsKey("PlacedSheets") || !workOrders.Any()) return placedSheets;

        var defaultWorkOrder = workOrders.First();
        var existingSheets = await _context.PlacedSheets.ToListAsync();
        
        foreach (var sheetElement in sdfData["PlacedSheets"].EnumerateArray())
        {
            if (!sheetElement.TryGetProperty("SheetName", out var sheetNameProp)) continue;
            
            var sheetName = sheetNameProp.GetString();
            if (string.IsNullOrEmpty(sheetName)) continue;

            var existingSheet = existingSheets.FirstOrDefault(ps => ps.SheetName == sheetName);
            
            if (existingSheet == null)
            {
                var barCode = sheetElement.TryGetProperty("BarCode", out var barCodeProp) ? barCodeProp.GetString() : null;
                var fileName = sheetElement.TryGetProperty("FileName", out var fileNameProp) ? fileNameProp.GetString() : null;
                var materialType = sheetElement.TryGetProperty("MaterialType", out var materialProp) ? materialProp.GetString() : null;
                
                decimal? length = null, width = null, thickness = null;
                if (sheetElement.TryGetProperty("Length", out var lengthProp) && lengthProp.ValueKind == JsonValueKind.Number)
                    length = lengthProp.GetDecimal();
                if (sheetElement.TryGetProperty("Width", out var widthProp) && widthProp.ValueKind == JsonValueKind.Number)
                    width = widthProp.GetDecimal();
                if (sheetElement.TryGetProperty("Thickness", out var thicknessProp) && thicknessProp.ValueKind == JsonValueKind.Number)
                    thickness = thicknessProp.GetDecimal();
                
                var placedSheet = new PlacedSheet
                {
                    PlacedSheetId = Guid.NewGuid().ToString(),
                    SheetName = sheetName,
                    BarCode = barCode ?? sheetName,
                    FileName = fileName ?? $"{sheetName}.nc",
                    WorkOrderId = defaultWorkOrder.WorkOrderId,
                    MaterialType = materialType,
                    Length = length,
                    Width = width,
                    Thickness = thickness,
                    Status = "Pending",
                    MicrovellumLinkID = sheetName
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

    private async Task<List<Part>> ImportPartsFromJsonAsync(Dictionary<string, JsonElement> sdfData, List<Product> products, List<WorkOrder> workOrders)
    {
        var parts = new List<Part>();
        
        if (!sdfData.ContainsKey("Parts")) return parts;
        
        var existingParts = await _context.Parts.ToListAsync();
        
        foreach (var partElement in sdfData["Parts"].EnumerateArray())
        {
            if (!partElement.TryGetProperty("LinkIDPart", out var linkIdProp) ||
                !partElement.TryGetProperty("LinkIDProduct", out var productLinkIdProp)) continue;
            
            var linkId = linkIdProp.GetString();
            var productLinkId = productLinkIdProp.GetString();
            
            if (string.IsNullOrEmpty(linkId) || string.IsNullOrEmpty(productLinkId)) continue;

            var product = products.FirstOrDefault(p => p.MicrovellumLinkID == productLinkId);
            if (product == null) continue;

            var existingPart = existingParts.FirstOrDefault(p => p.MicrovellumLinkID == linkId);
            
            if (existingPart == null)
            {
                var partName = partElement.TryGetProperty("PartName", out var nameProp) ? nameProp.GetString() : null;
                var materialName = partElement.TryGetProperty("MaterialName", out var matNameProp) ? matNameProp.GetString() : null;
                var materialCode = partElement.TryGetProperty("MaterialCode", out var matCodeProp) ? matCodeProp.GetString() : null;
                var edgeBanding = partElement.TryGetProperty("EdgeBanding", out var edgeProp) ? edgeProp.GetString() : null;
                
                decimal? length = null, width = null, thickness = null;
                if (partElement.TryGetProperty("Length", out var lengthProp) && lengthProp.ValueKind == JsonValueKind.Number)
                    length = lengthProp.GetDecimal();
                if (partElement.TryGetProperty("Width", out var widthProp) && widthProp.ValueKind == JsonValueKind.Number)
                    width = widthProp.GetDecimal();
                if (partElement.TryGetProperty("Thickness", out var thicknessProp) && thicknessProp.ValueKind == JsonValueKind.Number)
                    thickness = thicknessProp.GetDecimal();
                
                int quantity = 1;
                if (partElement.TryGetProperty("Quantity", out var qtyProp) && qtyProp.ValueKind == JsonValueKind.Number)
                    quantity = qtyProp.GetInt32();

                for (int i = 0; i < quantity; i++)
                {
                    var part = new Part
                    {
                        PartId = Guid.NewGuid().ToString(),
                        ProductId = product.ProductId,
                        PartNumber = quantity > 1 ? $"{linkId}-{i + 1}" : linkId,
                        PartName = partName ?? "Imported Part",
                        MaterialName = materialName,
                        MaterialCode = materialCode,
                        Length = length,
                        Width = width,
                        Thickness = thickness,
                        EdgeBanding = edgeBanding,
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

    private async Task<List<PartPlacement>> ImportPartPlacementsFromJsonAsync(Dictionary<string, JsonElement> sdfData, List<Part> parts, List<PlacedSheet> placedSheets)
    {
        var placements = new List<PartPlacement>();
        
        if (!sdfData.ContainsKey("OptimizationResults")) return placements;
        
        foreach (var optElement in sdfData["OptimizationResults"].EnumerateArray())
        {
            if (!optElement.TryGetProperty("LinkIDPart", out var partLinkIdProp) ||
                !optElement.TryGetProperty("SheetName", out var sheetNameProp)) continue;
            
            var partLinkId = partLinkIdProp.GetString();
            var sheetName = sheetNameProp.GetString();
            
            if (string.IsNullOrEmpty(partLinkId) || string.IsNullOrEmpty(sheetName)) continue;

            var part = parts.FirstOrDefault(p => p.MicrovellumLinkID == partLinkId);
            var sheet = placedSheets.FirstOrDefault(s => s.SheetName == sheetName);
            
            if (part == null || sheet == null) continue;

            decimal? xCoord = null, yCoord = null;
            if (optElement.TryGetProperty("XCoord", out var xProp) && xProp.ValueKind == JsonValueKind.Number)
                xCoord = xProp.GetDecimal();
            if (optElement.TryGetProperty("YCoord", out var yProp) && yProp.ValueKind == JsonValueKind.Number)
                yCoord = yProp.GetDecimal();
            
            int rotation = 0;
            if (optElement.TryGetProperty("Rotation", out var rotProp) && rotProp.ValueKind == JsonValueKind.Number)
                rotation = rotProp.GetInt32();
            
            bool isFlipped = false;
            if (optElement.TryGetProperty("IsFlipped", out var flipProp) && flipProp.ValueKind == JsonValueKind.True)
                isFlipped = true;

            var placement = new PartPlacement
            {
                PartId = part.PartId,
                PlacedSheetId = sheet.PlacedSheetId,
                XCoord = xCoord,
                YCoord = yCoord,
                Rotation = rotation,
                IsFlipped = isFlipped
            };

            _context.PartPlacements.Add(placement);
            placements.Add(placement);
        }
        
        await _context.SaveChangesAsync();
        return placements;
    }

    private async Task<List<Hardware>> ImportHardwareFromJsonAsync(Dictionary<string, JsonElement> sdfData, List<Product> products, List<WorkOrder> workOrders)
    {
        var hardwareList = new List<Hardware>();
        
        if (!sdfData.ContainsKey("Hardware")) return hardwareList;
        
        var existingHardwareList = await _context.Hardware.ToListAsync();
        
        foreach (var hardwareElement in sdfData["Hardware"].EnumerateArray())
        {
            if (!hardwareElement.TryGetProperty("LinkIDHardware", out var linkIdProp) ||
                !hardwareElement.TryGetProperty("LinkIDProduct", out var productLinkIdProp) ||
                !hardwareElement.TryGetProperty("LinkIDWorkOrder", out var workOrderLinkIdProp)) continue;
            
            var linkId = linkIdProp.GetString();
            var productLinkId = productLinkIdProp.GetString();
            var workOrderLinkId = workOrderLinkIdProp.GetString();
            
            if (string.IsNullOrEmpty(linkId) || string.IsNullOrEmpty(productLinkId)) continue;

            var product = products.FirstOrDefault(p => p.MicrovellumLinkID == productLinkId);
            var workOrder = workOrders.FirstOrDefault(w => w.MicrovellumLinkID == workOrderLinkId);
            
            if (product == null || workOrder == null) continue;

            var existingHardware = existingHardwareList.FirstOrDefault(h => h.MicrovellumLinkID == linkId);
            
            if (existingHardware == null)
            {
                var hardwareName = hardwareElement.TryGetProperty("HardwareName", out var nameProp) ? nameProp.GetString() : null;
                var hardwareDescription = hardwareElement.TryGetProperty("HardwareDescription", out var descProp) ? descProp.GetString() : null;
                
                int quantity = 1;
                if (hardwareElement.TryGetProperty("Quantity", out var qtyProp) && qtyProp.ValueKind == JsonValueKind.Number)
                    quantity = qtyProp.GetInt32();
                
                var hardware = new Hardware
                {
                    HardwareId = Guid.NewGuid().ToString(),
                    HardwareName = hardwareName ?? "Imported Hardware",
                    HardwareDescription = hardwareDescription,
                    ProductId = product.ProductId,
                    WorkOrderId = workOrder.WorkOrderId,
                    Quantity = quantity,
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