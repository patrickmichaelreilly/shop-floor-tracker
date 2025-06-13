using Microsoft.EntityFrameworkCore;
using ShopFloorTracker.Core.Entities;
using ShopFloorTracker.Core.Enums;

namespace ShopFloorTracker.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ShopFloorDbContext context)
    {
        // Only seed if database is empty
        if (await context.WorkOrders.AnyAsync())
        {
            return;
        }

        // Create sample work order
        var workOrder = new WorkOrder
        {
            WorkOrderId = "WO-2025-001",
            WorkOrderNumber = "240613-Smith Kitchen",
            CustomerName = "John Smith",
            OrderDate = DateTime.Now.AddDays(-5),
            DueDate = DateTime.Now.AddDays(10),
            Status = WorkOrderStatus.Active,
            ImportedBy = "System",
            TotalProducts = 2,
            TotalParts = 6
        };

        context.WorkOrders.Add(workOrder);

        // Create sample products
        var product1 = new Product
        {
            ProductId = "PRD-001",
            WorkOrderId = workOrder.WorkOrderId,
            ProductNumber = "Base Cabinet B24",
            ProductName = "24 inch Base Cabinet",
            ProductType = "Base Cabinet",
            Status = ProductStatus.Pending,
            TotalParts = 4
        };

        var product2 = new Product
        {
            ProductId = "PRD-002",
            WorkOrderId = workOrder.WorkOrderId,
            ProductNumber = "Wall Cabinet W30",
            ProductName = "30 inch Wall Cabinet",
            ProductType = "Wall Cabinet", 
            Status = ProductStatus.Pending,
            TotalParts = 2
        };

        context.Products.Add(product1);
        context.Products.Add(product2);

        // Create sample parts for Product 1
        var parts1 = new[]
        {
            new Part
            {
                PartId = "PRT-001",
                ProductId = product1.ProductId,
                PartNumber = "B24-SIDE-L",
                PartName = "Left Side Panel",
                Material = "3/4 Maple Plywood",
                Thickness = 0.75m,
                Length = 23.25m,
                Width = 34.5m,
                EdgeBanding = "Front Edge: Maple 2mm",
                NestingSheet = "NEST-001",
                Status = PartStatus.Pending
            },
            new Part
            {
                PartId = "PRT-002", 
                ProductId = product1.ProductId,
                PartNumber = "B24-SIDE-R",
                PartName = "Right Side Panel",
                Material = "3/4 Maple Plywood",
                Thickness = 0.75m,
                Length = 23.25m,
                Width = 34.5m,
                EdgeBanding = "Front Edge: Maple 2mm",
                NestingSheet = "NEST-001",
                Status = PartStatus.Pending
            },
            new Part
            {
                PartId = "PRT-003",
                ProductId = product1.ProductId,
                PartNumber = "B24-TOP",
                PartName = "Top Panel",
                Material = "3/4 Maple Plywood",
                Thickness = 0.75m,
                Length = 22.5m,
                Width = 23.25m,
                EdgeBanding = "All Edges: Maple 2mm",
                NestingSheet = "NEST-002",
                Status = PartStatus.Pending
            },
            new Part
            {
                PartId = "PRT-004",
                ProductId = product1.ProductId,
                PartNumber = "B24-BOTTOM",
                PartName = "Bottom Panel",
                Material = "3/4 Maple Plywood",
                Thickness = 0.75m,
                Length = 22.5m,
                Width = 23.25m,
                EdgeBanding = "Front Edge: Maple 2mm",
                NestingSheet = "NEST-002",
                Status = PartStatus.Pending
            }
        };

        // Create sample parts for Product 2
        var parts2 = new[]
        {
            new Part
            {
                PartId = "PRT-005",
                ProductId = product2.ProductId,
                PartNumber = "W30-SIDE-L",
                PartName = "Left Side Panel",
                Material = "3/4 Maple Plywood",
                Thickness = 0.75m,
                Length = 29.25m,
                Width = 30m,
                EdgeBanding = "Front Edge: Maple 2mm",
                NestingSheet = "NEST-003",
                Status = PartStatus.Pending
            },
            new Part
            {
                PartId = "PRT-006",
                ProductId = product2.ProductId,
                PartNumber = "W30-SIDE-R",
                PartName = "Right Side Panel",
                Material = "3/4 Maple Plywood",
                Thickness = 0.75m,
                Length = 29.25m,
                Width = 30m,
                EdgeBanding = "Front Edge: Maple 2mm",
                NestingSheet = "NEST-003",
                Status = PartStatus.Pending
            }
        };

        context.Parts.AddRange(parts1);
        context.Parts.AddRange(parts2);

        await context.SaveChangesAsync();
    }
}