using Microsoft.EntityFrameworkCore;
using ShopFloorTracker.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// TODO: Entity Framework Core 8.0.11 SQLite compatibility issue - using mock data for Phase 2C
// Add Entity Framework services (commented out due to runtime compatibility issue)
// builder.Services.AddDbContext<ShopFloorDbContext>(options =>
//     options.UseSqlite("Data Source=shopfloor.db"));

var app = builder.Build();

// TODO: Database initialization will be enabled once EF Core compatibility is resolved
// using (var scope = app.Services.CreateScope())
// {
//     var context = scope.ServiceProvider.GetRequiredService<ShopFloorDbContext>();
//     await context.Database.EnsureCreatedAsync();
//     await DatabaseSeeder.SeedAsync(context);
// }

// Dashboard endpoint with mock data (simulating database queries)
app.MapGet("/", () =>
{
    // Mock data representing what would come from database
    var totalWorkOrders = 1;
    var activeWorkOrders = 1;
    var totalParts = 6;
    var recentWorkOrders = new[]
    {
        new { WorkOrderNumber = "240613-Smith Kitchen", CustomerName = "John Smith", Status = "Active", TotalProducts = 2, TotalParts = 6 }
    };

    var html = $@"
<!DOCTYPE html>
<html>
<head>
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
<head><title>Admin Station</title></head>
<body style='font-family: Arial, sans-serif; margin: 40px;'>
    <h1>Admin Station</h1>
    <p>Work order import and system administration</p>
    <a href='/' style='color: #3498db;'>← Back to Dashboard</a>
    <p style='color: #7f8c8d; margin-top: 30px;'>Coming in Phase 2D...</p>
</body>
</html>", "text/html"));

app.MapGet("/sorting", () => Results.Content(@"
<!DOCTYPE html>
<html>
<head><title>Sorting Station</title></head>
<body style='font-family: Arial, sans-serif; margin: 40px;'>
    <h1>Sorting Station</h1>
    <p>Scan and organize parts by storage racks</p>
    <a href='/' style='color: #3498db;'>← Back to Dashboard</a>
    <p style='color: #7f8c8d; margin-top: 30px;'>Coming in Phase 2D...</p>
</body>
</html>", "text/html"));

app.MapGet("/assembly", () => Results.Content(@"
<!DOCTYPE html>
<html>
<head><title>Assembly Station</title></head>
<body style='font-family: Arial, sans-serif; margin: 40px;'>
    <h1>Assembly Station</h1>
    <p>Track assembly progress and completion</p>
    <a href='/' style='color: #3498db;'>← Back to Dashboard</a>
    <p style='color: #7f8c8d; margin-top: 30px;'>Coming in Phase 2D...</p>
</body>
</html>", "text/html"));

app.MapGet("/shipping", () => Results.Content(@"
<!DOCTYPE html>
<html>
<head><title>Shipping Station</title></head>
<body style='font-family: Arial, sans-serif; margin: 40px;'>
    <h1>Shipping Station</h1>
    <p>Final quality check and shipping preparation</p>
    <a href='/' style='color: #3498db;'>← Back to Dashboard</a>
    <p style='color: #7f8c8d; margin-top: 30px;'>Coming in Phase 2D...</p>
</body>
</html>", "text/html"));

app.Run();
