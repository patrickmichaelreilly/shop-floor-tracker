using Microsoft.EntityFrameworkCore;
using ShopFloorTracker.Infrastructure.Data;
using ShopFloorTracker.Web.Hubs;
using ShopFloorTracker.Web.Services;
using ShopFloorTracker.Web.Endpoints;
using ShopFloorTracker.Core.Enums;
using ShopFloorTracker.Core.Entities;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add Entity Framework services with EF Core 9.0.0 (compatibility issue resolved)
builder.Services.AddDbContext<ShopFloorDbContext>(options =>
    options.UseSqlite("Data Source=shopfloor.db"));

// Add SignalR services
builder.Services.AddSignalR();

// Add anti-forgery services
builder.Services.AddAntiforgery();

// Add custom services
builder.Services.AddScoped<IStatusBroadcaster, StatusBroadcaster>();
builder.Services.AddScoped<ShopFloorTracker.Application.Interfaces.IMicrovellumImportService, ShopFloorTracker.Application.Services.MicrovellumImportService>();
builder.Services.AddHostedService<HeartbeatService>();

var app = builder.Build();

// Add global exception handling middleware
app.UseExceptionHandler(appError =>
{
    appError.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "text/html";
        
        var exceptionHandlerFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (exceptionHandlerFeature != null)
        {
            var html = @"
<!DOCTYPE html>
<html>
<head>
    <title>Error - Shop Floor Tracker</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; background-color: #f5f5f5; }
        .error-container { background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .error-header { color: #e74c3c; font-size: 1.5em; margin-bottom: 20px; }
        .error-message { color: #666; margin-bottom: 20px; }
        .back-link { color: #3498db; text-decoration: none; }
    </style>
</head>
<body>
    <div class='error-container'>
        <div class='error-header'>Something went wrong</div>
        <div class='error-message'>We're sorry, but an error occurred while processing your request. Please try again or contact support if the problem persists.</div>
        <a href='/' class='back-link'>&larr; Back to Dashboard</a>
    </div>
</body>
</html>";
            
            await context.Response.WriteAsync(html);
        }
    });
});

// Add anti-forgery middleware
app.UseAntiforgery();

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
            <a href='/cnc' class='nav-link'>CNC Station</a>
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

// CSV Import endpoint
app.MapPost("/admin/import-csv", async (HttpContext context, ShopFloorDbContext dbContext) =>
{
    try
    {
        var form = await context.Request.ReadFormAsync();
        var file = form.Files["csvFile"];
        
        if (file == null || file.Length == 0)
        {
            return Results.Redirect("/admin?error=upload");
        }

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Redirect("/admin?error=format");
        }

        var importedWorkOrders = 0;
        var importedParts = 0;
        
        using var reader = new StreamReader(file.OpenReadStream());
        var csvData = await reader.ReadToEndAsync();
        var lines = csvData.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length < 2)
        {
            return Results.Redirect("/admin?error=format");
        }

        // Parse header
        var headers = lines[0].Split(',').Select(h => h.Trim().Trim('"')).ToArray();
        var expectedHeaders = new[] { "WorkOrderNumber", "ProductName", "PartNumber", "PartDescription", "Quantity", "DueDate" };
        
        if (!expectedHeaders.All(eh => headers.Contains(eh, StringComparer.OrdinalIgnoreCase)))
        {
            return Results.Redirect("/admin?error=format");
        }

        var workOrdersToImport = new Dictionary<string, (WorkOrder workOrder, List<(Product product, List<ShopFloorTracker.Core.Entities.Part> parts)> products)>();

        // Parse data lines
        for (int i = 1; i < lines.Length; i++)
        {
            var values = ParseCsvLine(lines[i]);
            if (values.Length < 6) continue;

            var workOrderNumber = values[0].Trim().Trim('"');
            var productName = values[1].Trim().Trim('"');
            var partNumber = values[2].Trim().Trim('"');
            var partDescription = values[3].Trim().Trim('"');
            var quantity = int.TryParse(values[4].Trim().Trim('"'), out var qty) ? qty : 1;
            var dueDate = DateTime.TryParse(values[5].Trim().Trim('"'), out var due) ? due : (DateTime?)null;

            if (string.IsNullOrEmpty(workOrderNumber) || string.IsNullOrEmpty(partNumber)) continue;

            // Create or get work order
            if (!workOrdersToImport.ContainsKey(workOrderNumber))
            {
                var workOrder = new ShopFloorTracker.Core.Entities.WorkOrder
                {
                    WorkOrderId = Guid.NewGuid().ToString(),
                    WorkOrderNumber = workOrderNumber,
                    CustomerName = "Imported Customer",
                    DueDate = dueDate,
                    Status = WorkOrderStatus.Active,
                    ImportedDate = DateTime.UtcNow,
                    ImportedBy = "Admin"
                };
                workOrdersToImport[workOrderNumber] = (workOrder, new List<(Product, List<ShopFloorTracker.Core.Entities.Part>)>());
            }

            var workOrderData = workOrdersToImport[workOrderNumber];
            
            // Find or create product
            var product = workOrderData.products.FirstOrDefault(p => p.product.ProductName == productName).product;
            if (product == null)
            {
                product = new ShopFloorTracker.Core.Entities.Product
                {
                    ProductId = Guid.NewGuid().ToString(),
                    WorkOrderId = workOrderData.workOrder.WorkOrderId,
                    ProductNumber = $"{workOrderNumber}-{productName.Split(' ')[0]}",
                    ProductName = productName,
                    Status = ProductStatus.InProgress
                };
                workOrderData.products.Add((product, new List<ShopFloorTracker.Core.Entities.Part>()));
            }

            var productData = workOrderData.products.First(p => p.product.ProductId == product.ProductId);

            // Create parts based on quantity
            for (int q = 0; q < quantity; q++)
            {
                var part = new ShopFloorTracker.Core.Entities.Part
                {
                    PartId = Guid.NewGuid().ToString(),
                    ProductId = product.ProductId,
                    PartNumber = quantity > 1 ? $"{partNumber}-{q + 1}" : partNumber,
                    PartName = partDescription,
                    Status = PartStatus.Pending,
                    Material = "Imported Material"
                };
                productData.parts.Add(part);
                importedParts++;
            }
        }

        // Save to database
        foreach (var (workOrderNumber, (workOrder, products)) in workOrdersToImport)
        {
            // Check for duplicate work order numbers
            if (await dbContext.WorkOrders.AnyAsync(w => w.WorkOrderNumber == workOrder.WorkOrderNumber))
            {
                continue; // Skip duplicates
            }

            workOrder.TotalProducts = products.Count;
            workOrder.TotalParts = products.Sum(p => p.parts.Count);
            
            dbContext.WorkOrders.Add(workOrder);
            
            foreach (var (product, parts) in products)
            {
                dbContext.Products.Add(product);
                foreach (var part in parts)
                {
                    dbContext.Parts.Add(part);
                }
            }
            
            importedWorkOrders++;
        }

        await dbContext.SaveChangesAsync();
        
        return Results.Redirect($"/admin?imported={importedWorkOrders}&parts={importedParts}");
    }
    catch (Exception)
    {
        return Results.Redirect("/admin?error=upload");
    }
});

// Helper function to parse CSV line properly handling quoted values
static string[] ParseCsvLine(string line)
{
    var result = new List<string>();
    var current = new StringBuilder();
    var inQuotes = false;
    
    for (int i = 0; i < line.Length; i++)
    {
        var c = line[i];
        if (c == '"')
        {
            inQuotes = !inQuotes;
        }
        else if (c == ',' && !inQuotes)
        {
            result.Add(current.ToString());
            current.Clear();
        }
        else
        {
            current.Append(c);
        }
    }
    result.Add(current.ToString());
    
    return result.ToArray();
}

// Add new work order endpoint
app.MapPost("/admin/add-work-order", async (HttpContext context, ShopFloorDbContext dbContext) =>
{
    var form = await context.Request.ReadFormAsync();
    var workOrderNumber = form["workOrderNumber"].ToString().Trim();
    var customerName = form["customerName"].ToString().Trim();
    var dueDateStr = form["dueDate"].ToString().Trim();
    
    if (string.IsNullOrEmpty(workOrderNumber) || string.IsNullOrEmpty(customerName))
    {
        return Results.Redirect("/admin?error=required");
    }

    // Check for duplicate work order number
    if (await dbContext.WorkOrders.AnyAsync(w => w.WorkOrderNumber == workOrderNumber))
    {
        return Results.Redirect("/admin?error=duplicate");
    }

    var dueDate = DateTime.TryParse(dueDateStr, out var due) ? due : (DateTime?)null;

    var workOrder = new ShopFloorTracker.Core.Entities.WorkOrder
    {
        WorkOrderId = Guid.NewGuid().ToString(),
        WorkOrderNumber = workOrderNumber,
        CustomerName = customerName,
        DueDate = dueDate,
        Status = WorkOrderStatus.Active,
        TotalProducts = 0,
        TotalParts = 0
    };

    dbContext.WorkOrders.Add(workOrder);
    await dbContext.SaveChangesAsync();

    return Results.Redirect($"/admin?created={workOrderNumber}");
});

// Delete work order endpoint
app.MapDelete("/admin/work-order/{workOrderId}/delete", async (string workOrderId, ShopFloorDbContext dbContext) =>
{
    var workOrder = await dbContext.WorkOrders
        .Include(w => w.Products)
        .ThenInclude(p => p.Parts)
        .FirstOrDefaultAsync(w => w.WorkOrderId == workOrderId);
    
    if (workOrder == null)
    {
        return Results.NotFound();
    }

    // Remove all associated parts and products
    foreach (var product in workOrder.Products)
    {
        dbContext.Parts.RemoveRange(product.Parts);
    }
    dbContext.Products.RemoveRange(workOrder.Products);
    dbContext.WorkOrders.Remove(workOrder);
    
    await dbContext.SaveChangesAsync();
    
    return Results.Ok();
});

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
        
        /* Flash animation for live updates */
        .flash { animation: flash 1s ease-in-out; }
        @keyframes flash {
            0% { background-color: inherit; }
            50% { background-color: #fff3cd; border-color: #ffeaa7; }
            100% { background-color: inherit; }
        }
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
                var slotContent = isOccupied ? "X" : "O";
                
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
    <script src='/js/sorting-live.js'></script>
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
app.MapPost("/sorting/smart-scan", async (HttpContext context, ShopFloorDbContext dbContext, IStatusBroadcaster statusBroadcaster) =>
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
    
    // Broadcast the part status change
    await statusBroadcaster.BroadcastPartStatusAsync(part);
    
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
        
        /* Flash animation for live updates */
        .flash { animation: flash 1s ease-in-out; }
        @keyframes flash {
            0% { background-color: inherit; }
            50% { background-color: #fff3cd; border-color: #ffeaa7; }
            100% { background-color: inherit; }
        }
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
                ? $"[{part.StorageRack.Name} R{part.StorageRow}C{part.StorageColumn}]" 
                : "[Not Located]";

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
                <button type='submit' class='assemble-button'>Mark as Assembled</button>
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
    <script src='/js/assembly-live.js'></script>
</body>
</html>";

    return Results.Content(html, "text/html");
});

// Handle product assembly completion
app.MapPost("/assembly/complete", async (HttpContext context, ShopFloorDbContext dbContext, IStatusBroadcaster statusBroadcaster) =>
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
    
    // Broadcast part status changes for all assembled parts
    foreach (var part in product.Parts)
    {
        await statusBroadcaster.BroadcastPartStatusAsync(part);
    }
    
    return Results.Redirect($"/assembly?success={productNumber}&parts={product.Parts.Count}");
});

app.MapGet("/admin", async (HttpContext context, ShopFloorDbContext dbContext) =>
{
    // Live database queries for system statistics
    var totalWorkOrders = await dbContext.WorkOrders.CountAsync();
    var activeWorkOrders = await dbContext.WorkOrders.CountAsync(w => w.Status == WorkOrderStatus.Active);
    var completedWorkOrders = await dbContext.WorkOrders.CountAsync(w => w.Status == WorkOrderStatus.Complete);
    var shippedWorkOrders = await dbContext.WorkOrders.CountAsync(w => w.Status == WorkOrderStatus.Shipped);
    
    var totalParts = await dbContext.Parts.CountAsync();
    var pendingParts = await dbContext.Parts.CountAsync(p => p.Status == ShopFloorTracker.Core.Enums.PartStatus.Pending);
    var sortedParts = await dbContext.Parts.CountAsync(p => p.Status == ShopFloorTracker.Core.Enums.PartStatus.Sorted);
    var assembledParts = await dbContext.Parts.CountAsync(p => p.Status == ShopFloorTracker.Core.Enums.PartStatus.Assembled);
    var shippedParts = await dbContext.Parts.CountAsync(p => p.Status == ShopFloorTracker.Core.Enums.PartStatus.Shipped);
    
    var totalProducts = await dbContext.Products.CountAsync();
    var inProgressProducts = await dbContext.Products.CountAsync(p => p.Status == ShopFloorTracker.Core.Enums.ProductStatus.InProgress);
    var completeProducts = await dbContext.Products.CountAsync(p => p.Status == ShopFloorTracker.Core.Enums.ProductStatus.Complete);
    
    var totalStorageRacks = await dbContext.StorageRacks.CountAsync(r => r.IsActive);
    var totalSlots = await dbContext.StorageRacks.Where(r => r.IsActive).SumAsync(r => r.Rows * r.Columns);
    var occupiedSlots = await dbContext.Parts.CountAsync(p => p.Status == ShopFloorTracker.Core.Enums.PartStatus.Sorted && p.StorageRackId != null);
    var storageUtilization = totalSlots > 0 ? (occupiedSlots * 100 / totalSlots) : 0;
    
    // Get recent work orders for management
    var recentWorkOrders = await dbContext.WorkOrders
        .OrderByDescending(w => w.CreatedDate)
        .Take(10)
        .Select(w => new { w.WorkOrderId, w.WorkOrderNumber, w.CustomerName, w.Status, w.TotalProducts, w.TotalParts, w.CreatedDate, w.DueDate })
        .ToListAsync();
    
    // Get query parameters for feedback messages
    var query = context.Request.Query;
    var successMessage = "";
    var errorMessage = "";
    
    if (query.ContainsKey("imported"))
    {
        successMessage = $"Successfully imported {query["imported"]} work orders with {(query.ContainsKey("parts") ? query["parts"].ToString() : "0")} parts.";
    }
    else if (query.ContainsKey("created"))
    {
        successMessage = $"Work order {query["created"]} created successfully.";
    }
    else if (query.ContainsKey("deleted"))
    {
        successMessage = "Work order deleted successfully.";
    }
    
    if (query.ContainsKey("error"))
    {
        var error = query["error"].ToString();
        errorMessage = error switch
        {
            "upload" => "Error uploading file. Please try again.",
            "format" => "Invalid file format. Please use a valid CSV file with required headers.",
            "required" => "Work order number and customer name are required.",
            "duplicate" => "Work order number already exists.",
            _ => "An error occurred. Please try again."
        };
    }

    var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Admin Station - Shop Floor Tracker</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; background-color: #f5f5f5; }}
        .header {{ background-color: #e74c3c; color: white; padding: 20px; border-radius: 8px; margin-bottom: 30px; }}
        .nav {{ margin-bottom: 20px; }}
        .nav a {{ color: #3498db; text-decoration: none; font-size: 16px; }}
        
        .stats-grid {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 20px; margin-bottom: 30px; }}
        .stat-card {{ background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); text-align: center; }}
        .stat-number {{ font-size: 2em; font-weight: bold; margin-bottom: 5px; }}
        .stat-label {{ color: #7f8c8d; font-size: 0.9em; }}
        .stat-work-orders {{ color: #3498db; }}
        .stat-parts {{ color: #27ae60; }}
        .stat-products {{ color: #f39c12; }}
        .stat-storage {{ color: #9b59b6; }}
        
        .content-grid {{ display: grid; grid-template-columns: 2fr 1fr; gap: 20px; margin-bottom: 30px; }}
        .work-order-management {{ background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .system-config {{ background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        
        .work-order-list {{ max-height: 400px; overflow-y: auto; }}
        .work-order-item {{ border: 1px solid #ecf0f1; padding: 15px; margin-bottom: 10px; border-radius: 4px; }}
        .work-order-header {{ font-weight: bold; color: #2c3e50; margin-bottom: 5px; }}
        .work-order-details {{ color: #7f8c8d; font-size: 0.9em; margin-bottom: 10px; }}
        .work-order-actions {{ display: flex; gap: 10px; }}
        
        .status-active {{ border-left: 4px solid #27ae60; }}
        .status-complete {{ border-left: 4px solid #3498db; }}
        .status-shipped {{ border-left: 4px solid #95a5a6; }}
        
        .form-group {{ margin-bottom: 15px; }}
        .form-label {{ display: block; margin-bottom: 5px; font-weight: bold; color: #2c3e50; }}
        .form-input {{ width: 100%; padding: 10px; border: 1px solid #ddd; border-radius: 4px; font-size: 14px; }}
        .form-button {{ background-color: #3498db; color: white; padding: 10px 20px; border: none; border-radius: 4px; cursor: pointer; font-size: 14px; }}
        .form-button:hover {{ background-color: #2980b9; }}
        .danger-button {{ background-color: #e74c3c; }}
        .danger-button:hover {{ background-color: #c0392b; }}
        
        .message {{ padding: 15px; border-radius: 4px; margin-bottom: 20px; }}
        .success-message {{ background-color: #d5f4e6; border: 1px solid #27ae60; color: #1e7e34; }}
        .error-message {{ background-color: #f8d7da; border: 1px solid #e74c3c; color: #721c24; }}
        
        .config-item {{ margin-bottom: 15px; padding: 15px; border: 1px solid #ecf0f1; border-radius: 4px; }}
        .config-label {{ font-weight: bold; color: #2c3e50; margin-bottom: 5px; }}
        .config-value {{ color: #7f8c8d; }}
        
        .search-box {{ width: 100%; padding: 10px; margin-bottom: 15px; border: 1px solid #ddd; border-radius: 4px; }}
        
        .tab-container {{ margin-bottom: 20px; }}
        .tab-buttons {{ display: flex; gap: 0; margin-bottom: 20px; }}
        .tab-button {{ padding: 10px 20px; border: 1px solid #ddd; background: #f8f9fa; cursor: pointer; }}
        .tab-button.active {{ background: #3498db; color: white; border-color: #3498db; }}
        .tab-content {{ display: none; }}
        .tab-content.active {{ display: block; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Admin Station</h1>
        <p>System management and work order administration</p>
    </div>
    
    <div class='nav'>
        <a href='/'>&larr; Back to Dashboard</a>
    </div>";

    if (!string.IsNullOrEmpty(successMessage))
    {
        html += $@"<div class='message success-message'>{successMessage}</div>";
    }
    
    if (!string.IsNullOrEmpty(errorMessage))
    {
        html += $@"<div class='message error-message'>{errorMessage}</div>";
    }

    html += $@"
    <div class='stats-grid'>
        <div class='stat-card'>
            <div class='stat-number stat-work-orders'>{totalWorkOrders}</div>
            <div class='stat-label'>Total Work Orders</div>
            <div style='font-size: 0.8em; margin-top: 5px; color: #95a5a6;'>
                Active: {activeWorkOrders} | Complete: {completedWorkOrders} | Shipped: {shippedWorkOrders}
            </div>
        </div>
        
        <div class='stat-card'>
            <div class='stat-number stat-parts'>{totalParts}</div>
            <div class='stat-label'>Total Parts</div>
            <div style='font-size: 0.8em; margin-top: 5px; color: #95a5a6;'>
                Pending: {pendingParts} | Sorted: {sortedParts} | Assembled: {assembledParts}
            </div>
        </div>
        
        <div class='stat-card'>
            <div class='stat-number stat-products'>{totalProducts}</div>
            <div class='stat-label'>Total Products</div>
            <div style='font-size: 0.8em; margin-top: 5px; color: #95a5a6;'>
                In Progress: {inProgressProducts} | Complete: {completeProducts}
            </div>
        </div>
        
        <div class='stat-card'>
            <div class='stat-number stat-storage'>{storageUtilization}%</div>
            <div class='stat-label'>Storage Utilization</div>
            <div style='font-size: 0.8em; margin-top: 5px; color: #95a5a6;'>
                {occupiedSlots}/{totalSlots} slots ({totalStorageRacks} racks)
            </div>
        </div>
    </div>
    
    <div class='content-grid'>
        <div class='work-order-management'>
            <div class='tab-container'>
                <div class='tab-buttons'>
                    <div class='tab-button active' onclick='showTab(""manage"")'>Manage Work Orders</div>
                    <div class='tab-button' onclick='showTab(""create"")'>Create New</div>
                    <div class='tab-button' onclick='showTab(""import"")'>Import CSV</div>
                    <div class='tab-button' onclick='showTab(""microvellum"")'>Microvellum Import</div>
                </div>
                
                <div id='tab-manage' class='tab-content active'>
                    <h3>Work Order Management</h3>
                    <input type='text' class='search-box' placeholder='Search by work order number or customer name...' onkeyup='filterWorkOrders(this.value)'>
                    
                    <div class='work-order-list' id='workOrderList'>";

    foreach (var order in recentWorkOrders)
    {
        var statusClass = order.Status switch
        {
            WorkOrderStatus.Active => "status-active",
            WorkOrderStatus.Complete => "status-complete",
            WorkOrderStatus.Shipped => "status-shipped",
            _ => ""
        };
        
        var dueDateDisplay = order.DueDate?.ToString("yyyy-MM-dd") ?? "No due date";
        var createdDisplay = order.CreatedDate.ToString("yyyy-MM-dd HH:mm");

        html += $@"
                        <div class='work-order-item {statusClass}' data-search='{order.WorkOrderNumber.ToLower()} {order.CustomerName?.ToLower()}'>
                            <div class='work-order-header'>{order.WorkOrderNumber} - {order.CustomerName}</div>
                            <div class='work-order-details'>
                                Status: {order.Status} | Products: {order.TotalProducts} | Parts: {order.TotalParts}<br>
                                Created: {createdDisplay} | Due: {dueDateDisplay}
                            </div>
                            <div class='work-order-actions'>
                                <button class='form-button' onclick='editWorkOrder(""{order.WorkOrderId}"", ""{order.WorkOrderNumber}"", ""{order.CustomerName}"")'>Edit</button>
                                <button class='form-button danger-button' onclick='deleteWorkOrder(""{order.WorkOrderId}"", ""{order.WorkOrderNumber}"")'>Delete</button>
                            </div>
                        </div>";
    }

    html += $@"
                    </div>
                </div>
                
                <div id='tab-create' class='tab-content'>
                    <h3>Create New Work Order</h3>
                    <form method='post' action='/admin/add-work-order'>
                        <div class='form-group'>
                            <label class='form-label'>Work Order Number</label>
                            <input type='text' name='workOrderNumber' class='form-input' required placeholder='WO-001'>
                        </div>
                        <div class='form-group'>
                            <label class='form-label'>Customer Name</label>
                            <input type='text' name='customerName' class='form-input' required placeholder='Customer Name'>
                        </div>
                        <div class='form-group'>
                            <label class='form-label'>Due Date (Optional)</label>
                            <input type='date' name='dueDate' class='form-input'>
                        </div>
                        <button type='submit' class='form-button'>Create Work Order</button>
                    </form>
                </div>
                
                <div id='tab-import' class='tab-content'>
                    <h3>Import Work Orders from CSV</h3>
                    <p style='color: #7f8c8d; margin-bottom: 15px;'>
                        Upload a CSV file with columns: WorkOrderNumber, ProductName, PartNumber, PartDescription, Quantity, DueDate
                    </p>
                    <form method='post' action='/admin/import-csv' enctype='multipart/form-data'>
                        <div class='form-group'>
                            <label class='form-label'>CSV File</label>
                            <input type='file' name='csvFile' class='form-input' accept='.csv' required>
                        </div>
                        <button type='submit' class='form-button'>Import CSV</button>
                    </form>
                </div>
                
                <div id='tab-microvellum' class='tab-content'>
                    <h3>Microvellum Import</h3>
                    <p style='color: #7f8c8d; margin-bottom: 15px;'>
                        Upload Microvellum .sdf files to import work orders, parts, and nesting data.
                    </p>
                    
                    <div class='form-group'>
                        <label class='form-label'>Microvellum SDF File</label>
                        <input type='file' id='sdfFile' class='form-input' accept='.sdf' />
                    </div>
                    
                    <button id='uploadImportBtn' class='form-button' onclick='uploadAndImport()'>Upload and Import</button>
                    
                    <div id='uploadProgress' style='display: none; margin: 15px 0;'>
                        <div style='background-color: #ecf0f1; border-radius: 4px; overflow: hidden;'>
                            <div id='progressBar' style='width: 0%; height: 20px; background-color: #3498db; transition: width 0.3s ease;'></div>
                        </div>
                        <div id='progressText' style='margin-top: 5px; color: #7f8c8d; font-size: 0.9em;'>Uploading...</div>
                    </div>
                    
                    <div id='importStatus'></div>
                    
                    <div style='margin-top: 25px;'>
                        <h4>Import History</h4>
                        <div id='importHistory'>Loading...</div>
                    </div>
                </div>
            </div>
        </div>
        
        <div class='system-config'>
            <h3>System Configuration</h3>
            
            <div class='config-item'>
                <div class='config-label'>Database Status</div>
                <div class='config-value'>✅ Connected and operational</div>
            </div>
            
            <div class='config-item'>
                <div class='config-label'>Last Update</div>
                <div class='config-value'>{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</div>
            </div>
            
            <div class='config-item'>
                <div class='config-label'>Storage Racks</div>
                <div class='config-value'>{totalStorageRacks} active racks configured</div>
            </div>
            
            <div class='config-item'>
                <div class='config-label'>System Health</div>
                <div class='config-value'>
                    📊 Database: OK<br>
                    🔄 SignalR: Active<br>
                    💾 Storage: {storageUtilization}% utilized
                </div>
            </div>
            
            <h4 style='margin-top: 25px; margin-bottom: 15px;'>System Actions</h4>
            
            <div style='margin-bottom: 10px;'>
                <button class='form-button' onclick='refreshStats()'>Refresh Statistics</button>
            </div>
            
            <div style='margin-bottom: 10px;'>
                <button class='form-button danger-button' onclick='clearCompletedOrders()'>Clear Completed Orders</button>
            </div>
        </div>
    </div>" + SignalRClientScript + @"
    
    <script>
        function showTab(tabName) {{
            // Hide all tab contents
            document.querySelectorAll('.tab-content').forEach(tab => tab.classList.remove('active'));
            document.querySelectorAll('.tab-button').forEach(btn => btn.classList.remove('active'));
            
            // Show selected tab
            document.getElementById('tab-' + tabName).classList.add('active');
            
            // Activate the clicked button
            document.querySelectorAll('.tab-button').forEach(btn => {{
                if (btn.onclick && btn.onclick.toString().includes(tabName)) {{
                    btn.classList.add('active');
                }}
            }});
        }}
        
        function filterWorkOrders(searchTerm) {{
            const items = document.querySelectorAll('.work-order-item');
            items.forEach(item => {{
                const searchData = item.getAttribute('data-search');
                if (searchData.includes(searchTerm.toLowerCase())) {{
                    item.style.display = 'block';
                }} else {{
                    item.style.display = 'none';
                }}
            }});
        }}
        
        function editWorkOrder(workOrderId, workOrderNumber, customerName) {{
            const newCustomerName = prompt('Edit customer name for ' + workOrderNumber + ':', customerName);
            if (newCustomerName && newCustomerName !== customerName) {{
                // For now, just show an alert. In a full implementation, this would make an API call
                alert('Edit functionality will be implemented in the next phase.');
            }}
        }}
        
        function deleteWorkOrder(workOrderId, workOrderNumber) {{
            if (confirm('Are you sure you want to delete work order ' + workOrderNumber + '? This will also delete all associated products and parts.')) {{
                fetch('/admin/work-order/' + workOrderId + '/delete', {{
                    method: 'DELETE'
                }})
                .then(response => {{
                    if (response.ok) {{
                        location.href = '/admin?deleted=true';
                    }} else {{
                        alert('Error deleting work order. Please try again.');
                    }}
                }})
                .catch(error => {{
                    alert('Error deleting work order. Please try again.');
                }});
            }}
        }}
        
        function refreshStats() {{
            location.reload();
        }}
        
        function clearCompletedOrders() {{
            if (confirm('Are you sure you want to clear all completed and shipped work orders? This action cannot be undone.')) {{
                alert('Clear completed orders functionality will be implemented in the next phase.');
            }}
        }}
        
        // Microvellum Import Functions
        async function loadImportHistory() {{
            try {{
                const response = await fetch('/api/import/history');
                const history = await response.json();
                
                const historyDiv = document.getElementById('importHistory');
                if (history.length === 0) {{
                    historyDiv.innerHTML = '<div style=""color: #7f8c8d; text-align: center; padding: 20px;"">No import history found</div>';
                    return;
                }}
                
                historyDiv.innerHTML = history.map(item => `
                    <div style=""border-bottom: 1px solid #ecf0f1; padding: 10px 0; margin-bottom: 10px;"">
                        <div style=""font-weight: bold; color: #2c3e50;"">${{item.fileName}}</div>
                        <div style=""font-size: 0.9em; color: #7f8c8d; margin-top: 2px;"">
                            ${{new Date(item.importDate).toLocaleString()}}
                        </div>
                        <div style=""font-size: 0.85em; margin-top: 5px;"">
                            <span class=""${{item.status === 'Success' ? 'text-success' : 'text-danger'}}"" style=""color: ${{item.status === 'Success' ? '#27ae60' : '#e74c3c'}};"">
                                ${{item.status}}
                            </span>
                            ${{item.status === 'Success' ? 
                                ` | WO: ${{item.workOrdersCreated}} | Products: ${{item.productsCreated}} | Parts: ${{item.partsCreated}}` :
                                ` | Error: ${{item.errorMessage || 'Unknown error'}}`
                            }}
                        </div>
                    </div>
                `).join('');
            }} catch (error) {{
                document.getElementById('importHistory').innerHTML = 
                    '<div style=""color: #e74c3c; text-align: center; padding: 20px;"">Error loading import history</div>';
                console.error('Error loading import history:', error);
            }}
        }}
        
        async function uploadAndImport() {{
            const fileInput = document.getElementById('sdfFile');
            const file = fileInput.files[0];
            
            if (!file) {{
                showImportStatus('Please select an SDF file to upload.', 'error');
                return;
            }}
            
            if (!file.name.toLowerCase().endsWith('.sdf')) {{
                showImportStatus('Please select a valid SDF file.', 'error');
                return;
            }}
            
            const formData = new FormData();
            formData.append('file', file);
            
            // Show progress
            document.getElementById('uploadProgress').style.display = 'block';
            document.getElementById('uploadImportBtn').disabled = true;
            document.getElementById('progressText').textContent = 'Uploading file...';
            
            try {{
                // Simulate progress for upload
                let progress = 0;
                const progressInterval = setInterval(() => {{
                    progress += 10;
                    document.getElementById('progressBar').style.width = progress + '%';
                    if (progress >= 50) {{
                        document.getElementById('progressText').textContent = 'Processing import...';
                    }}
                    if (progress >= 90) {{
                        clearInterval(progressInterval);
                    }}
                }}, 200);
                
                const response = await fetch('/api/import/sdf', {{
                    method: 'POST',
                    body: formData
                }});
                
                clearInterval(progressInterval);
                document.getElementById('progressBar').style.width = '100%';
                document.getElementById('progressText').textContent = 'Complete!';
                
                const result = await response.json();
                
                if (response.ok && result.success) {{
                    showImportStatus(`
                        <strong>Import Successful!</strong><br>
                        Work Orders: ${{result.workOrdersCreated}}<br>
                        Products: ${{result.productsCreated}}<br>
                        Parts: ${{result.partsCreated}}<br>
                        Hardware: ${{result.hardwareCreated}}<br>
                        Placed Sheets: ${{result.placedSheetsCreated}}<br>
                        Part Placements: ${{result.partPlacementsCreated}}
                    `, 'success');
                    
                    // Clear file input
                    fileInput.value = '';
                    
                    // Refresh import history
                    await loadImportHistory();
                    
                    // Refresh page statistics
                    setTimeout(() => {{
                        location.reload();
                    }}, 2000);
                }} else {{
                    showImportStatus(`Import failed: ${{result.message || 'Unknown error'}}`, 'error');
                    if (result.errors && result.errors.length > 0) {{
                        showImportStatus(`Errors: ${{result.errors.join(', ')}}`, 'error');
                    }}
                }}
            }} catch (error) {{
                showImportStatus(`Import failed: ${{error.message}}`, 'error');
                console.error('Import error:', error);
            }} finally {{
                document.getElementById('uploadProgress').style.display = 'none';
                document.getElementById('uploadImportBtn').disabled = false;
            }}
        }}
        
        function showImportStatus(message, type) {{
            const statusDiv = document.getElementById('importStatus');
            const bgColor = type === 'success' ? '#d5f4e6' : '#f8d7da';
            const borderColor = type === 'success' ? '#27ae60' : '#e74c3c';
            const textColor = type === 'success' ? '#1e7e34' : '#721c24';
            
            statusDiv.innerHTML = `
                <div style=""background-color: ${{bgColor}}; border: 1px solid ${{borderColor}}; color: ${{textColor}}; padding: 15px; border-radius: 4px; margin-bottom: 15px;"">
                    ${{message}}
                </div>
            `;
            
            // Auto-hide after 10 seconds for success messages
            if (type === 'success') {{
                setTimeout(() => {{
                    statusDiv.innerHTML = '';
                }}, 10000);
            }}
        }}
        
        // Load import history when page loads
        document.addEventListener('DOMContentLoaded', function() {{
            loadImportHistory();
        }});
    </script>
</body>
</html>";

    return Results.Content(html, "text/html");
});

app.MapGet("/cnc", async (ShopFloorDbContext context) =>
{
    // Get available placed sheets for scanning
    var availableSheets = await context.PlacedSheets
        .Where(ps => ps.Status == "Pending")
        .OrderBy(ps => ps.CreatedDate)
        .ToListAsync();
    
    // Get recently cut sheets
    var recentlyCutSheets = await context.PlacedSheets
        .Where(ps => ps.Status == "Cut")
        .OrderByDescending(ps => ps.CreatedDate)
        .Take(10)
        .ToListAsync();

    var html = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>CNC Station - Nest Sheet Scanning</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; background-color: #f5f5f5; }
        .header { background-color: #34495e; color: white; padding: 20px; border-radius: 8px; margin-bottom: 30px; }
        .nav { margin-bottom: 20px; }
        .nav a { color: #3498db; text-decoration: none; font-size: 16px; }
        .content-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; margin-bottom: 30px; }
        .scan-section { background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .sheets-list { background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .recent-cuts { background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); grid-column: 1 / -1; }
        .scan-input { width: 250px; padding: 12px; font-size: 16px; border: 2px solid #34495e; border-radius: 4px; }
        .scan-button { background-color: #34495e; color: white; padding: 12px 24px; border: none; border-radius: 4px; font-size: 16px; cursor: pointer; margin-left: 10px; }
        .scan-button:hover { background-color: #2c3e50; }
        .sheet-item { border: 1px solid #ecf0f1; padding: 15px; margin-bottom: 10px; border-radius: 4px; }
        .sheet-item:hover { background-color: #f8f9fa; }
        .sheet-header { font-weight: bold; color: #2c3e50; margin-bottom: 5px; }
        .sheet-details { color: #7f8c8d; font-size: 0.9em; }
        .status-pending { border-left: 4px solid #f39c12; }
        .status-cut { border-left: 4px solid #27ae60; }
        .result-message { margin-top: 15px; padding: 10px; border-radius: 4px; display: none; }
        .result-success { background-color: #d5f4e6; border: 1px solid #27ae60; color: #1e7e34; }
        .result-error { background-color: #f8d7da; border: 1px solid #e74c3c; color: #721c24; }
        
        /* Flash animation for live updates */
        .flash { animation: flash 1s ease-in-out; }
        @keyframes flash {
            0% { background-color: inherit; }
            50% { background-color: #fff3cd; border-color: #ffeaa7; }
            100% { background-color: inherit; }
        }
    </style>
</head>
<body>
    <div class='header'>
        <h1>CNC Station</h1>
        <p>Nest Sheet Scanning for Batch Processing</p>
    </div>
    
    <div class='nav'>
        <a href='/'>&larr; Back to Dashboard</a>
    </div>
    
    <div class='content-grid'>
        <div class='scan-section'>
            <h3>Nest Sheet Scanner</h3>
            <p>Scan nest sheet barcode to mark all parts as Cut:</p>
            <form id='scanForm' onsubmit='scanSheet(event)'>
                <input type='text' id='barcodeInput' class='scan-input' placeholder='Scan nest sheet barcode' autofocus>
                <button type='submit' class='scan-button'>Process Sheet</button>
            </form>
            <div id='scanResult' class='result-message'></div>
            
            <h4 style='margin-top: 25px;'>Quick Stats</h4>
            <div style='display: grid; grid-template-columns: 1fr 1fr; gap: 10px; margin-top: 10px;'>
                <div style='text-align: center; padding: 10px; background-color: #ecf0f1; border-radius: 4px;'>
                    <div style='font-size: 1.5em; font-weight: bold; color: #f39c12;'>" + availableSheets.Count + @"</div>
                    <div style='font-size: 0.9em; color: #7f8c8d;'>Sheets Ready</div>
                </div>
                <div style='text-align: center; padding: 10px; background-color: #ecf0f1; border-radius: 4px;'>
                    <div style='font-size: 1.5em; font-weight: bold; color: #27ae60;'>" + recentlyCutSheets.Count + @"</div>
                    <div style='font-size: 0.9em; color: #7f8c8d;'>Recently Cut</div>
                </div>
            </div>
        </div>
        
        <div class='sheets-list'>
            <h3>Available Nest Sheets</h3>
            <div style='max-height: 300px; overflow-y: auto;'>";

    foreach (var sheet in availableSheets.Take(10))
    {
        html += $@"
                <div class='sheet-item status-pending'>
                    <div class='sheet-header'>{sheet.SheetName}</div>
                    <div class='sheet-details'>
                        Barcode: {sheet.BarCode}<br>
                        Material: {sheet.MaterialType ?? "Unknown"}<br>
                        Size: {sheet.Length}x{sheet.Width}x{sheet.Thickness}
                    </div>
                </div>";
    }

    if (!availableSheets.Any())
    {
        html += @"<div style='text-align: center; color: #7f8c8d; padding: 20px;'>No nest sheets available for cutting</div>";
    }

    html += @"
            </div>
        </div>
    </div>
    
    <div class='recent-cuts'>
        <h3>Recently Cut Sheets (" + recentlyCutSheets.Count + @")</h3>";

    foreach (var sheet in recentlyCutSheets)
    {
        html += $@"
        <div class='sheet-item status-cut'>
            <div class='sheet-header'>{sheet.SheetName} - COMPLETED</div>
            <div class='sheet-details'>
                Barcode: {sheet.BarCode} | Material: {sheet.MaterialType ?? "Unknown"} | Cut: {sheet.CreatedDate:yyyy-MM-dd HH:mm}
            </div>
        </div>";
    }

    if (!recentlyCutSheets.Any())
    {
        html += @"<div style='text-align: center; color: #7f8c8d; padding: 20px;'>No sheets have been cut yet</div>";
    }

    html += @"
    </div>" + SignalRClientScript + @"
    
    <script>
        async function scanSheet(event) {
            event.preventDefault();
            
            const barcode = document.getElementById('barcodeInput').value.trim();
            if (!barcode) {
                showResult('Please enter a barcode', 'error');
                return;
            }
            
            try {
                const response = await fetch(`/api/cnc/scan-sheet/${encodeURIComponent(barcode)}`, {
                    method: 'POST'
                });
                
                const result = await response.json();
                
                if (response.ok) {
                    showResult(`
                        <strong>Success!</strong><br>
                        Sheet: ${result.sheetName}<br>
                        Parts processed: ${result.partsProcessed}<br>
                        ${result.message}
                    `, 'success');
                    
                    // Clear input
                    document.getElementById('barcodeInput').value = '';
                    
                    // Refresh page after 2 seconds
                    setTimeout(() => {
                        location.reload();
                    }, 2000);
                } else {
                    showResult(`Error: ${result.message || result}`, 'error');
                }
            } catch (error) {
                showResult(`Error: ${error.message}`, 'error');
                console.error('Scan error:', error);
            }
        }
        
        function showResult(message, type) {
            const resultDiv = document.getElementById('scanResult');
            resultDiv.className = `result-message result-${type}`;
            resultDiv.innerHTML = message;
            resultDiv.style.display = 'block';
            
            // Auto-hide success messages after 5 seconds
            if (type === 'success') {
                setTimeout(() => {
                    resultDiv.style.display = 'none';
                }, 5000);
            }
        }
        
        // Auto-focus barcode input
        document.getElementById('barcodeInput').focus();
        
        // Handle barcode scanner input (typically ends with Enter)
        document.getElementById('barcodeInput').addEventListener('keypress', function(e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                scanSheet(e);
            }
        });
        
        // Handle SignalR part status updates
        connection.on('PartStatusChanged', function (partId, newStatus) {
            console.log('Part status changed:', partId, '->', newStatus);
            // Flash the page to indicate updates
            document.body.classList.add('flash');
            setTimeout(() => {
                document.body.classList.remove('flash');
            }, 1000);
        });
    </script>
</body>
</html>";

    return Results.Content(html, "text/html");
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

// Microvellum Import endpoints
app.MapPost("/api/import/sdf", async (
    IFormFile file,
    ShopFloorTracker.Application.Interfaces.IMicrovellumImportService importService) =>
{
    if (file == null || file.Length == 0)
        return Results.BadRequest("No file uploaded");

    if (!file.FileName.EndsWith(".sdf", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest("Only .sdf files are supported");

    var tempPath = Path.GetTempFileName();
    try
    {
        using var stream = File.Create(tempPath);
        await file.CopyToAsync(stream);
        
        var result = await importService.ImportWorkOrderAsync(tempPath);
        return Results.Ok(result);
    }
    finally
    {
        if (File.Exists(tempPath))
            File.Delete(tempPath);
    }
}).DisableAntiforgery();

app.MapGet("/api/import/history", async (ShopFloorDbContext db) =>
{
    var history = await db.ImportHistory
        .OrderByDescending(h => h.ImportDate)
        .Take(10)
        .ToListAsync();
    return Results.Ok(history);
});

// CNC Nest Sheet Scanning endpoint
app.MapPost("/api/cnc/scan-sheet/{barcode}", async (
    string barcode,
    ShopFloorDbContext db,
    IStatusBroadcaster broadcaster) =>
{
    // Find the placed sheet by barcode
    var placedSheet = await db.PlacedSheets
        .FirstOrDefaultAsync(ps => ps.BarCode == barcode);
        
    if (placedSheet == null)
        return Results.NotFound($"Nest sheet with barcode {barcode} not found");
    
    // Get all parts on this sheet
    var partsOnSheet = await db.PartPlacements
        .Include(pp => pp.Part)
        .Where(pp => pp.PlacedSheetId == placedSheet.PlacedSheetId)
        .ToListAsync();
    
    if (!partsOnSheet.Any())
        return Results.BadRequest("No parts found on this nest sheet");
    
    // Mark all parts as Cut
    foreach (var placement in partsOnSheet)
    {
        placement.Part.Status = ShopFloorTracker.Core.Enums.PartStatus.Cut;
        placement.Part.ModifiedDate = DateTime.UtcNow;
    }
    
    // Mark sheet as Cut
    placedSheet.Status = "Cut";
    
    await db.SaveChangesAsync();
    
    // Broadcast status changes
    foreach (var placement in partsOnSheet)
    {
        await broadcaster.BroadcastPartStatusAsync(placement.Part);
    }
    
    return Results.Ok(new
    {
        Message = $"Marked {partsOnSheet.Count} parts as Cut from sheet {placedSheet.SheetName}",
        SheetName = placedSheet.SheetName,
        PartsProcessed = partsOnSheet.Count,
        PartIds = partsOnSheet.Select(p => p.Part.PartId).ToList()
    });
});

// Map API endpoints
app.MapSummaryEndpoints();

app.Run();
