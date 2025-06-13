using Microsoft.EntityFrameworkCore;
using ShopFloorTracker.Infrastructure.Data;
using ShopFloorTracker.Core.Enums;

namespace ShopFloorTracker.Web.Endpoints;

public static class SummaryEndpoints
{
    public static void MapSummaryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/summary/sorting", GetSortingSummary);
        endpoints.MapGet("/api/summary/assembly", GetAssemblySummary);
    }

    private static async Task<IResult> GetSortingSummary(ShopFloorDbContext context)
    {
        // Get rack occupancy data
        var storageRacks = await context.StorageRacks
            .Include(r => r.Parts.Where(p => p.Status == PartStatus.Sorted))
            .Where(r => r.IsActive)
            .Select(r => new
            {
                id = r.Id,
                name = r.Name,
                occupied = r.Parts.Count(p => p.Status == PartStatus.Sorted),
                total = r.Rows * r.Columns
            })
            .ToListAsync();

        // Get parts ready for sorting with their current locations
        var parts = await context.Parts
            .Include(p => p.Product)
            .ThenInclude(p => p.WorkOrder)
            .Include(p => p.StorageRack)
            .Where(p => p.Status == PartStatus.Cut || p.Status == PartStatus.Pending || p.Status == PartStatus.Sorted)
            .Select(p => new
            {
                partId = p.PartId,
                partNumber = p.PartNumber,
                status = p.Status.ToString(),
                row = p.StorageRow,
                col = p.StorageColumn,
                rackName = p.StorageRack != null ? p.StorageRack.Name : null,
                rackId = p.StorageRackId,
                workOrder = p.Product.WorkOrder.WorkOrderNumber,
                productNumber = p.Product.ProductNumber
            })
            .ToListAsync();

        return Results.Json(new
        {
            rackOccupancy = storageRacks,
            parts = parts
        });
    }

    private static async Task<IResult> GetAssemblySummary(ShopFloorDbContext context)
    {
        // Get products and their parts readiness status
        var products = await context.Products
            .Include(p => p.WorkOrder)
            .Include(p => p.Parts)
            .Where(p => p.Status != ProductStatus.Complete)
            .Select(p => new
            {
                productId = p.ProductId,
                productNumber = p.ProductNumber,
                productName = p.ProductName,
                workOrderNumber = p.WorkOrder.WorkOrderNumber,
                customerName = p.WorkOrder.CustomerName,
                status = p.Status.ToString(),
                totalParts = p.Parts.Count,
                sortedParts = p.Parts.Count(part => part.Status == PartStatus.Sorted),
                assembledParts = p.Parts.Count(part => part.Status == PartStatus.Assembled),
                isReady = p.Parts.All(part => part.Status == PartStatus.Sorted),
                parts = p.Parts.Select(part => new
                {
                    partId = part.PartId,
                    partNumber = part.PartNumber,
                    partName = part.PartName,
                    status = part.Status.ToString(),
                    rackName = part.StorageRack != null ? part.StorageRack.Name : null,
                    row = part.StorageRow,
                    col = part.StorageColumn
                }).ToList()
            })
            .ToListAsync();

        return Results.Json(new
        {
            products = products
        });
    }
}