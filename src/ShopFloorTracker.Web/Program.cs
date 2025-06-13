using Microsoft.EntityFrameworkCore;
using ShopFloorTracker.Infrastructure.Data;
using ShopFloorTracker.Web.Hubs;
using ShopFloorTracker.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Entity Framework services with EF Core 9.0.0 (compatibility issue resolved)
builder.Services.AddDbContext<ShopFloorDbContext>(options =>
    options.UseSqlite("Data Source=shopfloor.db"));

// Add SignalR services
builder.Services.AddSignalR();

// Add custom services
builder.Services.AddScoped<IStatusBroadcaster, StatusBroadcaster>();
builder.Services.AddHostedService<HeartbeatService>();

var app = builder.Build();

// Shared SignalR client scripts for all station pages
const string SignalRClientScript = @"
    <!-- SignalR Client -->
    <script src=""https://unpkg.com/@microsoft/signalr@latest/dist/browser/signalr.js""></script>
    <script>
        // Initialize SignalR connection
        const connection = new signalR.HubConnectionBuilder()
            .withUrl(""/hubs/status"")
            .build();

        // Start the connection
        connection.start().then(function () {
            console.log(""SignalR connected to StatusHub"");
        }).catch(function (err) {
            console.error(""SignalR connection error: "" + err.toString());
        });

        // Handle heartbeat messages
        connection.on(""Heartbeat"", function (serverUtc) {
            console.log(""Heartbeat received:"", serverUtc);
        });

        // Handle part status change messages
        connection.on(""PartStatusChanged"", function (partId, newStatus) {
            console.log(""Part status changed:"", partId, ""->"", newStatus);
        });

        // Handle connection errors
        connection.onclose(function () {
            console.log(""SignalR connection closed"");
        });
    </script>";

// Initialize database with seeded data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ShopFloorDbContext>();
    await context.Database.EnsureCreatedAsync();
    await DatabaseSeeder.SeedAsync(context);
}

// Dashboard endpoint with live database queries
app.MapGet("/", async (ShopFloorDbContext context) =>
{
    // Live database queries
    var totalWorkOrders = await context.WorkOrders.CountAsync();
    var activeWorkOrders = await context.WorkOrders.CountAsync(w => w.Status == ShopFloorTracker.Core.Enums.WorkOrderStatus.Active);
    var totalParts = await context.Parts.CountAsync();
    var recentWorkOrders = await context.WorkOrders
        .OrderByDescending(w => w.CreatedDate)
        .Take(5)
        .Select(w => new { w.WorkOrderNumber, w.CustomerName, w.Status, w.TotalProducts, w.TotalParts })
        .ToListAsync();

    var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Shop Floor Tracker - Dashboard</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; background-color: #f5f5f5; }}
        .header {{ background-color: #2c3e50; color: white; padding: 20px; border-radius: 8px; margin-bottom: 30px; }}
        .stats {{ display: flex; gap: 20px; margin-bottom: 30px; }}
        .stat-card {{ background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); flex: 1; text-align: center; }}
        .stat-number {{ font-size: 2em; font-weight: bold; color: #3498db; }}
        .stat-label {{ color: #7f8c8d; margin-top: 5px; }}
        .navigation {{ background: white; padding: 20px; border-radius: 8px; margin-bottom: 30px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .nav-links {{ display: flex; gap: 15px; }}
        .nav-link {{ background-color: #3498db; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px; }}
        .nav-link:hover {{ background-color: #2980b9; }}
        .recent-orders {{ background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .order-item {{ border-bottom: 1px solid #ecf0f1; padding: 15px 0; }}
        .order-item:last-child {{ border-bottom: none; }}
        .order-header {{ font-weight: bold; color: #2c3e50; }}
        .order-details {{ color: #7f8c8d; font-size: 0.9em; margin-top: 5px; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Shop Floor Tracker</h1>
        <p>Real-time Manufacturing Workflow Management</p>
    </div>
    
    <div class='stats'>
        <div class='stat-card'>
            <div class='stat-number'>{totalWorkOrders}</div>
            <div class='stat-label'>Total Work Orders</div>
        </div>
        <div class='stat-card'>
            <div class='stat-number'>{activeWorkOrders}</div>
            <div class='stat-label'>Active Work Orders</div>
        </div>
        <div class='stat-card'>
            <div class='stat-number'>{totalParts}</div>
            <div class='stat-label'>Total Parts</div>
        </div>
    </div>
    
    <div class='navigation'>
        <h3>Station Access</h3>
        <div class='nav-links'>
            <a href='/admin' class='nav-link'>Admin Station</a>
            <a href='/sorting' class='nav-link'>Sorting Station</a>
            <a href='/assembly' class='nav-link'>Assembly Station</a>
            <a href='/shipping' class='nav-link'>Shipping Station</a>
        </div>
    </div>
    
    <div class='recent-orders'>
        <h3>Recent Work Orders</h3>";

    foreach (var order in recentWorkOrders)
    {
        html += $@"
        <div class='order-item'>
            <div class='order-header'>{order.WorkOrderNumber} - {order.CustomerName}</div>
            <div class='order-details'>Status: {order.Status} | Products: {order.TotalProducts} | Parts: {order.TotalParts}</div>
        </div>";
    }

    html += @"
    </div>
</body>
</html>";

    return Results.Content(html, "text/html");
});

// Station placeholder endpoints
app.MapGet("/admin", () => Results.Content(@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Admin Station</title>
</head>
<body style='font-family: Arial, sans-serif; margin: 40px;'>
    <h1>Admin Station</h1>
    <p>Work order import and system administration</p>
    <a href='/' style='color: #3498db;'>&larr; Back to Dashboard</a>
    <p style='color: #7f8c8d; margin-top: 30px;'>Coming in Phase 2D...</p>
</body>
</html>", "text/html"));

app.MapGet("/sorting", async (ShopFloorDbContext context) =>
{
    // Get all parts that are ready for sorting
    var partsToSort = await context.Parts
        .Include(p => p.Product)
        .ThenInclude(p => p.WorkOrder)
        .Include(p => p.StorageRack)
        .Where(p => p.Status == ShopFloorTracker.Core.Enums.PartStatus.Cut || p.Status == ShopFloorTracker.Core.Enums.PartStatus.Pending)
        .OrderBy(p => p.Product.WorkOrder.WorkOrderNumber)
        .ThenBy(p => p.PartNumber)
        .ToListAsync();

    // Get storage racks with current occupancy
    var storageRacks = await context.StorageRacks
        .Include(r => r.Parts.Where(p => p.Status == ShopFloorTracker.Core.Enums.PartStatus.Sorted))
        .Where(r => r.IsActive)
        .ToListAsync();

    var html = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Advanced Sorting Station</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; background-color: #f5f5f5; }
        .header { background-color: #27ae60; color: white; padding: 20px; border-radius: 8px; margin-bottom: 30px; }
        .nav { margin-bottom: 20px; }
        .nav a { color: #3498db; text-decoration: none; font-size: 16px; }
        .content-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; margin-bottom: 30px; }
        .scan-section { background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .storage-visualization { background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .parts-list { background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); grid-column: 1 / -1; }
        .part-item { border: 1px solid #ecf0f1; padding: 15px; margin-bottom: 10px; border-radius: 4px; }
        .part-item:hover { background-color: #f8f9fa; }
        .part-header { font-weight: bold; color: #2c3e50; margin-bottom: 5px; }
        .part-details { color: #7f8c8d; font-size: 0.9em; }
        .scan-input { width: 250px; padding: 12px; font-size: 16px; border: 2px solid #3498db; border-radius: 4px; }
        .scan-button { background-color: #27ae60; color: white; padding: 12px 24px; border: none; border-radius: 4px; font-size: 16px; cursor: pointer; margin-left: 10px; }
        .scan-button:hover { background-color: #229954; }
        .status-pending { border-left: 4px solid #f39c12; }
        .status-cut { border-left: 4px solid #e74c3c; }
        .status-sorted { border-left: 4px solid #27ae60; }
        
        /* Storage Rack Visualization */
        .rack-container { margin-bottom: 20px; }
        .rack-header { font-weight: bold; margin-bottom: 10px; padding: 10px; background-color: #34495e; color: white; border-radius: 4px; }
        .rack-grid { display: grid; gap: 2px; margin-bottom: 15px; }
        .rack-slot { width: 30px; height: 30px; border: 1px solid #bdc3c7; border-radius: 2px; display: flex; align-items: center; justify-content: center; font-size: 10px; cursor: pointer; }
        .slot-available { background-color: #2ecc71; color: white; }
        .slot-occupied { background-color: #e74c3c; color: white; }
        .slot-selected { background-color: #f39c12; color: white; }
        .rack-stats { font-size: 0.9em; color: #7f8c8d; }
        
        /* Smart Assignment */
        .assignment-result { margin-top: 15px; padding: 10px; border-radius: 4px; display: none; }
        .assignment-success { background-color: #d5f4e6; border: 1px solid #27ae60; color: #1e7e34; }
        .assignment-error { background-color: #f8d7da; border: 1px solid #e74c3c; color: #721c24; }
    </style>
</head>
<body>
    <div class='header'>
        <h1>Advanced Sorting Station</h1>
        <p>Smart part scanning with visual storage management</p>
    </div>
    
    <div class='nav'>
        <a href='/'>&larr; Back to Dashboard</a>
    </div>
    
    <div class='content-grid'>
        <div class='scan-section'>
            <h3>Smart Part Scanner</h3>
            <p>Scan part number for automatic optimal slot assignment:</p>
            <form method='post' action='/sorting/smart-scan' id='smartScanForm'>
                <input type='text' name='partNumber' class='scan-input' placeholder='Scan or enter part number' autofocus>
                <button type='submit' class='scan-button'>Auto-Assign Slot</button>
            </form>
            <div id='assignmentResult' class='assignment-result'></div>
            
            <h4 style='margin-top: 25px;'>Quick Stats</h4>
            <div style='display: grid; grid-template-columns: 1fr 1fr; gap: 10px; margin-top: 10px;'>
                <div style='text-align: center; padding: 10px; background-color: #ecf0f1; border-radius: 4px;'>
                    <div style='font-size: 1.5em; font-weight: bold; color: #e74c3c;'>" + partsToSort.Count + @"</div>
                    <div style='font-size: 0.9em; color: #7f8c8d;'>Ready to Sort</div>
                </div>
                <div style='text-align: center; padding: 10px; background-color: #ecf0f1; border-radius: 4px;'>
                    <div style='font-size: 1.5em; font-weight: bold; color: #27ae60;'>" + storageRacks.Sum(r => r.Parts.Count) + @"</div>
                    <div style='font-size: 0.9em; color: #7f8c8d;'>Parts Stored</div>
                </div>
            </div>
        </div>
        
        <div class='storage-visualization'>
            <h3>Storage Rack Occupancy</h3>";

    // Generate storage rack visualizations
    foreach (var rack in storageRacks)
    {
        var totalSlots = rack.Rows * rack.Columns;
        var occupiedSlots = rack.Parts.Count;
        var availableSlots = totalSlots - occupiedSlots;
        var occupancyPercentage = totalSlots > 0 ? (occupiedSlots * 100 / totalSlots) : 0;

        html += $@"
            <div class='rack-container'>
                <div class='rack-header'>{rack.Name} - {occupancyPercentage}% Full</div>
                <div class='rack-grid' style='grid-template-columns: repeat({rack.Columns}, 1fr);'>";

        // Create visual grid for rack slots
        for (int row = 1; row <= rack.Rows; row++)
        {
            for (int col = 1; col <= rack.Columns; col++)
            {
                var isOccupied = rack.Parts.Any(p => p.StorageRow == row && p.StorageColumn == col);
                var slotClass = isOccupied ? "slot-occupied" : "slot-available";
                var slotContent = isOccupied ? "●" : "○";
                
                html += $@"<div class='rack-slot {slotClass}' title='Row {row}, Col {col}'>{slotContent}</div>";
            }
        }

        html += $@"
                </div>
                <div class='rack-stats'>Available: {availableSlots} | Occupied: {occupiedSlots} | Total: {totalSlots}</div>
            </div>";
    }

    html += @"
        </div>
    </div>
    
    <div class='parts-list'>
        <h3>Parts Queue - Ready for Sorting (" + partsToSort.Count + @")</h3>";

    foreach (var part in partsToSort)
    {
        var statusClass = part.Status == ShopFloorTracker.Core.Enums.PartStatus.Pending ? "status-pending" : "status-cut";
        var storageInfo = part.StorageRack != null ? $"Storage: {part.StorageRack.Name} R{part.StorageRow}C{part.StorageColumn}" : "Not Assigned";
        
        html += $@"
        <div class='part-item {statusClass}'>
            <div class='part-header'>{part.PartNumber} - {part.PartName}</div>
            <div class='part-details'>
                Work Order: {part.Product.WorkOrder.WorkOrderNumber} | 
                Product: {part.Product.ProductNumber} | 
                Material: {part.Material} | 
                Status: {part.Status} | 
                {storageInfo}
            </div>
        </div>";
    }

    html += @"
    </div>" + SignalRClientScript + @"
</body>
</html>";

    return Results.Content(html, "text/html");
});

// Smart slot assignment algorithm
static (int rackId, int row, int column)? FindOptimalSlot(List<ShopFloorTracker.Core.Entities.StorageRack> racks, string productId)
{
    // Strategy: Group parts by product for easy assembly retrieval
    foreach (var rack in racks.OrderBy(r => r.Id))
    {
        // Check if this rack already has parts from the same product
        var existingProductParts = rack.Parts.Where(p => p.ProductId == productId).ToList();
        if (existingProductParts.Any())
        {
            // Try to find a slot near existing parts from same product
            for (int row = 1; row <= rack.Rows; row++)
            {
                for (int col = 1; col <= rack.Columns; col++)
                {
                    if (!rack.Parts.Any(p => p.StorageRow == row && p.StorageColumn == col))
                    {
                        return (rack.Id, row, col);
                    }
                }
            }
        }
    }

    // If no optimal grouping available, find first available slot
    foreach (var rack in racks.OrderBy(r => r.Id))
    {
        for (int row = 1; row <= rack.Rows; row++)
        {
            for (int col = 1; col <= rack.Columns; col++)
            {
                if (!rack.Parts.Any(p => p.StorageRow == row && p.StorageColumn == col))
                {
                    return (rack.Id, row, col);
                }
            }
        }
    }

    return null; // No available slots
}

// Handle smart part scanning with automatic slot assignment
app.MapPost("/sorting/smart-scan", async (HttpContext context, ShopFloorDbContext dbContext) =>
{
    var form = await context.Request.ReadFormAsync();
    var partNumber = form["partNumber"].ToString().Trim();
    
    if (string.IsNullOrEmpty(partNumber))
    {
        return Results.Redirect("/sorting?error=empty");
    }
    
    // Find the part
    var part = await dbContext.Parts
        .Include(p => p.Product)
        .ThenInclude(p => p.WorkOrder)
        .FirstOrDefaultAsync(p => p.PartNumber.ToLower() == partNumber.ToLower());
    
    if (part == null)
    {
        return Results.Redirect("/sorting?error=notfound");
    }

    if (part.Status == ShopFloorTracker.Core.Enums.PartStatus.Sorted)
    {
        return Results.Redirect("/sorting?error=alreadysorted");
    }
    
    // Get available storage racks with current occupancy
    var storageRacks = await dbContext.StorageRacks
        .Include(r => r.Parts.Where(p => p.Status == ShopFloorTracker.Core.Enums.PartStatus.Sorted))
        .Where(r => r.IsActive)
        .ToListAsync();

    // Find optimal slot using smart assignment algorithm
    var optimalSlot = FindOptimalSlot(storageRacks, part.ProductId);
    
    if (optimalSlot == null)
    {
        return Results.Redirect("/sorting?error=noslots");
    }

    // Update part with storage assignment
    part.Status = ShopFloorTracker.Core.Enums.PartStatus.Sorted;
    part.StorageRackId = optimalSlot.Value.rackId;
    part.StorageRow = optimalSlot.Value.row;
    part.StorageColumn = optimalSlot.Value.column;
    part.SortedDateTime = DateTime.UtcNow;
    part.ModifiedDate = DateTime.UtcNow;

    // Create scan activity record
    var scanActivity = new ShopFloorTracker.Core.Entities.ScanActivity
    {
        PartId = part.PartId,
        StationName = "Sorting",
        Activity = "Smart Scan and Sort",
        OldStatus = ShopFloorTracker.Core.Enums.PartStatus.Pending,
        NewStatus = ShopFloorTracker.Core.Enums.PartStatus.Sorted,
        StorageLocation = $"Rack {optimalSlot.Value.rackId} R{optimalSlot.Value.row}C{optimalSlot.Value.column}",
        ScanDateTime = DateTime.UtcNow
    };

    dbContext.ScanActivities.Add(scanActivity);
    await dbContext.SaveChangesAsync();
    
    var rackName = storageRacks.First(r => r.Id == optimalSlot.Value.rackId).Name;
    return Results.Redirect($"/sorting?success={partNumber}&rack={rackName}&row={optimalSlot.Value.row}&col={optimalSlot.Value.column}");
});

app.MapGet("/assembly", async (ShopFloorDbContext context) =>
{
    // Get products ready for assembly (all parts sorted)
    var productsForAssembly = await context.Products
        .Include(p => p.WorkOrder)
        .Include(p => p.Parts)
        .ThenInclude(p => p.StorageRack)
        .Where(p => p.Status != ShopFloorTracker.Core.Enums.ProductStatus.Complete 
                    && p.Parts.All(part => part.Status == ShopFloorTracker.Core.Enums.PartStatus.Sorted))
        .OrderBy(p => p.WorkOrder.WorkOrderNumber)
        .ThenBy(p => p.ProductNumber)
        .ToListAsync();

    // Get products currently being assembled (some parts assembled, some sorted)
    var productsInProgress = await context.Products
        .Include(p => p.WorkOrder)
        .Include(p => p.Parts)
        .ThenInclude(p => p.StorageRack)
        .Where(p => p.Status != ShopFloorTracker.Core.Enums.ProductStatus.Complete 
                    && p.Parts.Any(part => part.Status == ShopFloorTracker.Core.Enums.PartStatus.Sorted)
                    && p.Parts.Any(part => part.Status != ShopFloorTracker.Core.Enums.PartStatus.Sorted))
        .OrderBy(p => p.WorkOrder.WorkOrderNumber)
        .ThenBy(p => p.ProductNumber)
        .ToListAsync();

    var html = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Assembly Station</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; background-color: #f5f5f5; }
        .header { background-color: #8e44ad; color: white; padding: 20px; border-radius: 8px; margin-bottom: 30px; }
        .nav { margin-bottom: 20px; }
        .nav a { color: #3498db; text-decoration: none; font-size: 16px; }
        .content-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; margin-bottom: 30px; }
        .scan-section { background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .assembly-queue { background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .product-list { background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); grid-column: 1 / -1; }
        .product-item { border: 1px solid #ecf0f1; padding: 20px; margin-bottom: 15px; border-radius: 4px; }
        .product-header { font-weight: bold; color: #2c3e50; margin-bottom: 10px; font-size: 1.1em; }
        .product-details { color: #7f8c8d; font-size: 0.9em; margin-bottom: 15px; }
        .parts-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 10px; }
        .part-card { padding: 10px; border: 1px solid #ddd; border-radius: 4px; font-size: 0.85em; }
        .part-sorted { background-color: #d5f4e6; border-color: #27ae60; }
        .part-pending { background-color: #fcf3cf; border-color: #f39c12; }
        .scan-input { width: 250px; padding: 12px; font-size: 16px; border: 2px solid #8e44ad; border-radius: 4px; }
        .scan-button { background-color: #8e44ad; color: white; padding: 12px 24px; border: none; border-radius: 4px; font-size: 16px; cursor: pointer; margin-left: 10px; }
        .scan-button:hover { background-color: #7d3c98; }
        .assemble-button { background-color: #27ae60; color: white; padding: 8px 16px; border: none; border-radius: 4px; cursor: pointer; margin-top: 10px; }
        .assemble-button:hover { background-color: #229954; }
        .ready-indicator { background-color: #d5f4e6; color: #1e7e34; padding: 5px 10px; border-radius: 15px; font-size: 0.8em; font-weight: bold; }
        .in-progress-indicator { background-color: #fff3cd; color: #856404; padding: 5px 10px; border-radius: 15px; font-size: 0.8em; font-weight: bold; }
    </style>
</head>
<body>
    <div class='header'>
        <h1>Assembly Station</h1>
        <p>Assemble products from sorted components</p>
    </div>
    
    <div class='nav'>
        <a href='/'>&larr; Back to Dashboard</a>
    </div>
    
    <div class='content-grid'>
        <div class='scan-section'>
            <h3>Product Assembly</h3>
            <p>Scan product number to mark as assembled:</p>
            <form method='post' action='/assembly/complete'>
                <input type='text' name='productNumber' class='scan-input' placeholder='Scan product number' autofocus>
                <button type='submit' class='scan-button'>Mark Assembled</button>
            </form>
            
            <h4 style='margin-top: 25px;'>Assembly Stats</h4>
            <div style='display: grid; grid-template-columns: 1fr 1fr; gap: 10px; margin-top: 10px;'>
                <div style='text-align: center; padding: 10px; background-color: #ecf0f1; border-radius: 4px;'>
                    <div style='font-size: 1.5em; font-weight: bold; color: #27ae60;'>" + productsForAssembly.Count + @"</div>
                    <div style='font-size: 0.9em; color: #7f8c8d;'>Ready to Assemble</div>
                </div>
                <div style='text-align: center; padding: 10px; background-color: #ecf0f1; border-radius: 4px;'>
                    <div style='font-size: 1.5em; font-weight: bold; color: #f39c12;'>" + productsInProgress.Count + @"</div>
                    <div style='font-size: 0.9em; color: #7f8c8d;'>In Progress</div>
                </div>
            </div>
        </div>
        
        <div class='assembly-queue'>
            <h3>Component Locations</h3>
            <p>Select a product to see component storage locations:</p>
            <div style='max-height: 300px; overflow-y: auto;'>";

    foreach (var product in productsForAssembly.Take(5))
    {
        html += $@"
                <div style='margin-bottom: 15px; padding: 10px; border: 1px solid #ddd; border-radius: 4px;'>
                    <div style='font-weight: bold; margin-bottom: 5px;'>{product.ProductNumber}</div>
                    <div style='font-size: 0.85em; color: #666; margin-bottom: 8px;'>{product.ProductName}</div>";

        foreach (var part in product.Parts.Take(3))
        {
            var locationInfo = part.StorageRack != null 
                ? $"{part.StorageRack.Name} R{part.StorageRow}C{part.StorageColumn}" 
                : "Not Located";
            html += $@"
                    <div style='font-size: 0.8em; color: #7f8c8d; margin-left: 10px;'>
                        &bull; {part.PartNumber}: {locationInfo}
                    </div>";
        }

        if (product.Parts.Count > 3)
        {
            html += $@"<div style='font-size: 0.8em; color: #7f8c8d; margin-left: 10px;'>... and {product.Parts.Count - 3} more parts</div>";
        }

        html += "</div>";
    }

    html += @"
            </div>
        </div>
    </div>
    
    <div class='product-list'>
        <h3>Products Ready for Assembly (" + productsForAssembly.Count + @")</h3>";

    foreach (var product in productsForAssembly)
    {
        var sortedCount = product.Parts.Count(p => p.Status == ShopFloorTracker.Core.Enums.PartStatus.Sorted);
        var totalCount = product.Parts.Count;
        var readyPercent = totalCount > 0 ? (sortedCount * 100 / totalCount) : 0;

        html += $@"
        <div class='product-item'>
            <div class='product-header'>
                {product.ProductNumber} - {product.ProductName}
                <span class='ready-indicator'>READY FOR ASSEMBLY</span>
            </div>
            <div class='product-details'>
                Work Order: {product.WorkOrder.WorkOrderNumber} | 
                Customer: {product.WorkOrder.CustomerName} | 
                Parts Ready: {sortedCount}/{totalCount} ({readyPercent}%)
            </div>
            <div class='parts-grid'>";

        foreach (var part in product.Parts)
        {
            var partClass = part.Status == ShopFloorTracker.Core.Enums.PartStatus.Sorted ? "part-sorted" : "part-pending";
            var locationInfo = part.StorageRack != null 
                ? $"📍 {part.StorageRack.Name} R{part.StorageRow}C{part.StorageColumn}" 
                : "❓ Not Located";

            html += $@"
                <div class='{partClass} part-card'>
                    <div style='font-weight: bold;'>{part.PartNumber}</div>
                    <div style='font-size: 0.9em; color: #666;'>{part.PartName}</div>
                    <div style='font-size: 0.8em; margin-top: 5px;'>{locationInfo}</div>
                </div>";
        }

        html += $@"
            </div>
            <form method='post' action='/assembly/complete' style='margin-top: 10px;'>
                <input type='hidden' name='productNumber' value='{product.ProductNumber}'>
                <button type='submit' class='assemble-button'>✓ Mark as Assembled</button>
            </form>
        </div>";
    }

    if (productsInProgress.Any())
    {
        html += @"
        <h3 style='margin-top: 30px;'>Products In Progress (" + productsInProgress.Count + @")</h3>";

        foreach (var product in productsInProgress)
        {
            var sortedCount = product.Parts.Count(p => p.Status == ShopFloorTracker.Core.Enums.PartStatus.Sorted);
            var totalCount = product.Parts.Count;
            var progressPercent = totalCount > 0 ? (sortedCount * 100 / totalCount) : 0;

            html += $@"
            <div class='product-item' style='border-left: 4px solid #f39c12;'>
                <div class='product-header'>
                    {product.ProductNumber} - {product.ProductName}
                    <span class='in-progress-indicator'>IN PROGRESS</span>
                </div>
                <div class='product-details'>
                    Work Order: {product.WorkOrder.WorkOrderNumber} | 
                    Customer: {product.WorkOrder.CustomerName} | 
                    Progress: {sortedCount}/{totalCount} parts ready ({progressPercent}%)
                </div>
            </div>";
        }
    }

    html += @"
    </div>" + SignalRClientScript + @"
</body>
</html>";

    return Results.Content(html, "text/html");
});

// Handle product assembly completion
app.MapPost("/assembly/complete", async (HttpContext context, ShopFloorDbContext dbContext) =>
{
    var form = await context.Request.ReadFormAsync();
    var productNumber = form["productNumber"].ToString().Trim();
    
    if (string.IsNullOrEmpty(productNumber))
    {
        return Results.Redirect("/assembly?error=empty");
    }
    
    // Find the product
    var product = await dbContext.Products
        .Include(p => p.WorkOrder)
        .Include(p => p.Parts)
        .FirstOrDefaultAsync(p => p.ProductNumber.ToLower() == productNumber.ToLower());
    
    if (product == null)
    {
        return Results.Redirect("/assembly?error=notfound");
    }

    if (product.Status == ShopFloorTracker.Core.Enums.ProductStatus.Complete)
    {
        return Results.Redirect("/assembly?error=alreadyassembled");
    }

    // Check if all parts are sorted
    var unsortedParts = product.Parts.Where(p => p.Status != ShopFloorTracker.Core.Enums.PartStatus.Sorted).ToList();
    if (unsortedParts.Any())
    {
        return Results.Redirect($"/assembly?error=partsnotready&missing={unsortedParts.Count}");
    }
    
    // Mark all parts as assembled
    foreach (var part in product.Parts)
    {
        part.Status = ShopFloorTracker.Core.Enums.PartStatus.Assembled;
        part.AssembledDateTime = DateTime.UtcNow;
        part.ModifiedDate = DateTime.UtcNow;

        // Create scan activity for each part
        var scanActivity = new ShopFloorTracker.Core.Entities.ScanActivity
        {
            PartId = part.PartId,
            StationName = "Assembly",
            Activity = "Product Assembly Complete",
            OldStatus = ShopFloorTracker.Core.Enums.PartStatus.Sorted,
            NewStatus = ShopFloorTracker.Core.Enums.PartStatus.Assembled,
            ScanDateTime = DateTime.UtcNow
        };
        dbContext.ScanActivities.Add(scanActivity);
    }

    // Mark product as assembled
    product.Status = ShopFloorTracker.Core.Enums.ProductStatus.Complete;
    product.ModifiedDate = DateTime.UtcNow;

    await dbContext.SaveChangesAsync();
    
    return Results.Redirect($"/assembly?success={productNumber}&parts={product.Parts.Count}");
});

app.MapGet("/shipping", () => Results.Content(@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Shipping Station</title>
</head>
<body style='font-family: Arial, sans-serif; margin: 40px;'>
    <h1>Shipping Station</h1>
    <p>Final quality check and shipping preparation</p>
    <a href='/' style='color: #3498db;'>&larr; Back to Dashboard</a>
    <p style='color: #7f8c8d; margin-top: 30px;'>Coming in Phase 2D...</p>
</body>
</html>", "text/html"));

// Map SignalR hub
app.MapHub<StatusHub>("/hubs/status");

app.Run();
