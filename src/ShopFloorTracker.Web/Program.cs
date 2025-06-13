using Microsoft.EntityFrameworkCore;
using ShopFloorTracker.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add Entity Framework services with EF Core 9.0.0 (compatibility issue resolved)
builder.Services.AddDbContext<ShopFloorDbContext>(options =>
    options.UseSqlite("Data Source=shopfloor.db"));

var app = builder.Build();

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
    // Get all parts that are in "Cut" status and ready for sorting
    var partsToSort = await context.Parts
        .Include(p => p.Product)
        .ThenInclude(p => p.WorkOrder)
        .Where(p => p.Status == ShopFloorTracker.Core.Enums.PartStatus.Cut || p.Status == ShopFloorTracker.Core.Enums.PartStatus.Pending)
        .OrderBy(p => p.Product.WorkOrder.WorkOrderNumber)
        .ThenBy(p => p.PartNumber)
        .ToListAsync();

    var html = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Sorting Station</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; background-color: #f5f5f5; }
        .header { background-color: #27ae60; color: white; padding: 20px; border-radius: 8px; margin-bottom: 30px; }
        .nav { margin-bottom: 20px; }
        .nav a { color: #3498db; text-decoration: none; font-size: 16px; }
        .scan-section { background: white; padding: 20px; border-radius: 8px; margin-bottom: 30px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .parts-list { background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .part-item { border: 1px solid #ecf0f1; padding: 15px; margin-bottom: 10px; border-radius: 4px; }
        .part-item:hover { background-color: #f8f9fa; }
        .part-header { font-weight: bold; color: #2c3e50; margin-bottom: 5px; }
        .part-details { color: #7f8c8d; font-size: 0.9em; }
        .scan-input { width: 300px; padding: 10px; font-size: 16px; border: 2px solid #3498db; border-radius: 4px; }
        .scan-button { background-color: #27ae60; color: white; padding: 10px 20px; border: none; border-radius: 4px; font-size: 16px; cursor: pointer; margin-left: 10px; }
        .scan-button:hover { background-color: #229954; }
        .rack-select { padding: 8px; margin-left: 10px; border: 1px solid #bdc3c7; border-radius: 4px; }
        .status-pending { border-left: 4px solid #f39c12; }
        .status-cut { border-left: 4px solid #e74c3c; }
    </style>
</head>
<body>
    <div class='header'>
        <h1>Sorting Station</h1>
        <p>Scan parts and assign to storage racks</p>
    </div>
    
    <div class='nav'>
        <a href='/'>&larr; Back to Dashboard</a>
    </div>
    
    <div class='scan-section'>
        <h3>Scan Part</h3>
        <p>Enter part number or scan barcode:</p>
        <form method='post' action='/sorting/scan'>
            <input type='text' name='partNumber' class='scan-input' placeholder='Enter part number (e.g., B24-SIDE-L)' autofocus>
            <select name='rackNumber' class='rack-select'>
                <option value='1'>Rack 1</option>
                <option value='2'>Rack 2</option>
                <option value='3'>Rack 3</option>
                <option value='4'>Rack 4</option>
                <option value='5'>Rack 5</option>
                <option value='6'>Rack 6</option>
                <option value='7'>Rack 7</option>
                <option value='8'>Rack 8</option>
            </select>
            <button type='submit' class='scan-button'>Sort to Rack</button>
        </form>
    </div>
    
    <div class='parts-list'>
        <h3>Parts Ready for Sorting (" + partsToSort.Count + @")</h3>";

    foreach (var part in partsToSort)
    {
        var statusClass = part.Status == ShopFloorTracker.Core.Enums.PartStatus.Pending ? "status-pending" : "status-cut";
        html += $@"
        <div class='part-item {statusClass}'>
            <div class='part-header'>{part.PartNumber} - {part.PartName}</div>
            <div class='part-details'>
                Work Order: {part.Product.WorkOrder.WorkOrderNumber} | 
                Product: {part.Product.ProductNumber} | 
                Material: {part.Material} | 
                Status: {part.Status}
            </div>
        </div>";
    }

    html += @"
    </div>
</body>
</html>";

    return Results.Content(html, "text/html");
});

// Handle part scanning and rack assignment
app.MapPost("/sorting/scan", async (HttpContext context, ShopFloorDbContext dbContext) =>
{
    var form = await context.Request.ReadFormAsync();
    var partNumber = form["partNumber"].ToString().Trim();
    var rackNumber = form["rackNumber"].ToString();
    
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
    
    // Update part status to Sorted
    part.Status = ShopFloorTracker.Core.Enums.PartStatus.Sorted;
    part.ModifiedDate = DateTime.UtcNow;
    
    await dbContext.SaveChangesAsync();
    
    return Results.Redirect($"/sorting?success={partNumber}&rack={rackNumber}");
});

app.MapGet("/assembly", () => Results.Content(@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Assembly Station</title>
</head>
<body style='font-family: Arial, sans-serif; margin: 40px;'>
    <h1>Assembly Station</h1>
    <p>Track assembly progress and completion</p>
    <a href='/' style='color: #3498db;'>&larr; Back to Dashboard</a>
    <p style='color: #7f8c8d; margin-top: 30px;'>Coming in Phase 2D...</p>
</body>
</html>", "text/html"));

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

app.Run();
