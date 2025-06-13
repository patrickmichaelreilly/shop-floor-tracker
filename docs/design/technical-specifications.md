# Technical Specifications
## Shop Floor Part Tracking System

**Document Version:** 1.0  
**Date:** 2025-01-13  
**Author:** Claude Code  
**Status:** Draft

---

## Overview

This document provides detailed technical specifications for implementing the Shop Floor Part Tracking System. It covers database implementation, API design, UI specifications, and integration requirements.

---

## Database Implementation Specifications

### Entity Framework Core Configuration

#### DbContext Implementation
```csharp
public class ShopFloorDbContext : DbContext
{
    public ShopFloorDbContext(DbContextOptions<ShopFloorDbContext> options) : base(options) { }

    // Entity DbSets
    public DbSet<WorkOrder> WorkOrders { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Subassembly> Subassemblies { get; set; }
    public DbSet<Part> Parts { get; set; }
    public DbSet<Hardware> Hardware { get; set; }
    public DbSet<DetachedProduct> DetachedProducts { get; set; }
    public DbSet<StorageRack> StorageRacks { get; set; }
    public DbSet<StorageSlot> StorageSlots { get; set; }
    public DbSet<PartStorageAssignment> PartStorageAssignments { get; set; }
    public DbSet<ProcessStation> ProcessStations { get; set; }
    public DbSet<ProcessStep> ProcessSteps { get; set; }
    public DbSet<PartProcessHistory> PartProcessHistory { get; set; }
    public DbSet<SystemUser> SystemUsers { get; set; }
    public DbSet<SystemSetting> SystemSettings { get; set; }
    public DbSet<ImportHistory> ImportHistory { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure entity relationships and constraints
        ConfigureWorkOrderHierarchy(modelBuilder);
        ConfigureStorageManagement(modelBuilder);
        ConfigureProcessTracking(modelBuilder);
        ConfigureSystemConfiguration(modelBuilder);
    }
}
```

#### Entity Configurations

##### WorkOrder Entity
```csharp
public class WorkOrder
{
    public string WorkOrderId { get; set; } // Primary Key
    public string WorkOrderNumber { get; set; }
    public string CustomerName { get; set; }
    public DateTime? OrderDate { get; set; }
    public DateTime? DueDate { get; set; }
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Active;
    public DateTime ImportedDate { get; set; } = DateTime.UtcNow;
    public string ImportedBy { get; set; }
    public string ImportFilePath { get; set; }
    public int TotalProducts { get; set; }
    public int TotalParts { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    public virtual ICollection<Hardware> Hardware { get; set; } = new List<Hardware>();
    public virtual ICollection<DetachedProduct> DetachedProducts { get; set; } = new List<DetachedProduct>();
}
```

##### Part Entity with Status Tracking
```csharp
public class Part
{
    public string PartId { get; set; } // Primary Key
    public string ProductId { get; set; } // Foreign Key
    public string SubassemblyId { get; set; } // Foreign Key (nullable)
    public string PartNumber { get; set; }
    public string PartName { get; set; }
    public string Material { get; set; }
    public decimal? Thickness { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public string EdgeBanding { get; set; }
    public string NestingSheet { get; set; }
    public PartStatus Status { get; set; } = PartStatus.Pending;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual Product Product { get; set; }
    public virtual Subassembly Subassembly { get; set; }
    public virtual ICollection<PartStorageAssignment> StorageAssignments { get; set; } = new List<PartStorageAssignment>();
    public virtual ICollection<PartProcessHistory> ProcessHistory { get; set; } = new List<PartProcessHistory>();
}
```

#### Database Connection Configuration

##### Development (SQLite)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=shopfloor.db;Cache=Shared"
  },
  "DatabaseProvider": "SQLite"
}
```

##### Production (SQL Server Express)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=ShopFloorTracker;Integrated Security=true;TrustServerCertificate=true;"
  },
  "DatabaseProvider": "SqlServer"
}
```

#### Migration Strategy

##### Initial Migration Commands
```bash
# Add initial migration
dotnet ef migrations add InitialCreate --project ShopFloorTracker.Infrastructure --startup-project ShopFloorTracker.Web

# Update database
dotnet ef database update --project ShopFloorTracker.Infrastructure --startup-project ShopFloorTracker.Web
```

##### Seed Data Implementation
```csharp
public static class DatabaseSeeder
{
    public static void SeedDatabase(ShopFloorDbContext context)
    {
        SeedProcessStations(context);
        SeedProcessSteps(context);
        SeedSystemSettings(context);
        SeedDefaultUsers(context);
    }

    private static void SeedProcessStations(ShopFloorDbContext context)
    {
        if (!context.ProcessStations.Any())
        {
            context.ProcessStations.AddRange(
                new ProcessStation { StationId = "CNC-001", StationName = "CNC Machine #1", StationType = "CNC", Location = "Production Floor - North" },
                new ProcessStation { StationId = "SORT-001", StationName = "Sorting Station #1", StationType = "Sorting", Location = "Production Floor - Center" },
                new ProcessStation { StationId = "ASSY-001", StationName = "Assembly Line #1", StationType = "Assembly", Location = "Production Floor - South" },
                new ProcessStation { StationId = "SHIP-001", StationName = "Shipping Dock #1", StationType = "Shipping", Location = "Warehouse - Loading Dock" }
            );
            context.SaveChanges();
        }
    }
}
```

---

## API Design Specifications

### RESTful API Endpoints

#### Part Management API
```csharp
[ApiController]
[Route("api/[controller]")]
public class PartsController : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<PartDto>> GetPart(string id)
    
    [HttpPost("{id}/scan")]
    public async Task<ActionResult<ScanResultDto>> ScanPart(string id, ScanPartCommand command)
    
    [HttpGet("{id}/status")]
    public async Task<ActionResult<PartStatusDto>> GetPartStatus(string id)
    
    [HttpGet("by-product/{productId}")]
    public async Task<ActionResult<List<PartDto>>> GetPartsByProduct(string productId)
}
```

#### Storage Management API
```csharp
[ApiController]
[Route("api/[controller]")]
public class StorageController : ControllerBase
{
    [HttpGet("racks")]
    public async Task<ActionResult<List<StorageRackDto>>> GetStorageRacks()
    
    [HttpGet("racks/{rackId}/availability")]
    public async Task<ActionResult<RackAvailabilityDto>> GetRackAvailability(string rackId)
    
    [HttpPost("assignments")]
    public async Task<ActionResult<StorageAssignmentDto>> CreateStorageAssignment(CreateStorageAssignmentCommand command)
    
    [HttpDelete("assignments/{assignmentId}")]
    public async Task<ActionResult> RemoveStorageAssignment(Guid assignmentId)
}
```

#### Work Order Management API
```csharp
[ApiController]
[Route("api/[controller]")]
public class WorkOrdersController : ControllerBase
{
    [HttpPost("import")]
    public async Task<ActionResult<ImportResultDto>> ImportWorkOrder(IFormFile sqlCeFile)
    
    [HttpGet("{id}/status")]
    public async Task<ActionResult<WorkOrderStatusDto>> GetWorkOrderStatus(string id)
    
    [HttpGet("{id}/products")]
    public async Task<ActionResult<List<ProductDto>>> GetWorkOrderProducts(string id)
    
    [HttpGet("active")]
    public async Task<ActionResult<List<WorkOrderSummaryDto>>> GetActiveWorkOrders()
}
```

### Data Transfer Objects (DTOs)

#### Part-related DTOs
```csharp
public class PartDto
{
    public string PartId { get; set; }
    public string PartNumber { get; set; }
    public string PartName { get; set; }
    public string ProductId { get; set; }
    public string ProductName { get; set; }
    public PartStatus Status { get; set; }
    public string CurrentLocation { get; set; }
    public DateTime? LastScanned { get; set; }
}

public class ScanResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public PartStatus NewStatus { get; set; }
    public string AssignedLocation { get; set; }
    public List<string> ValidationErrors { get; set; } = new List<string>();
}

public class StorageAssignmentDto
{
    public string PartId { get; set; }
    public string SlotId { get; set; }
    public string RackName { get; set; }
    public int Row { get; set; }
    public int Column { get; set; }
    public DateTime AssignedDate { get; set; }
    public string AssignedBy { get; set; }
}
```

---

## User Interface Specifications

### Responsive Design Framework

#### CSS Grid Layout System
```css
/* Shop floor responsive grid */
.station-container {
    display: grid;
    grid-template-columns: 1fr;
    gap: 1rem;
    padding: 1rem;
}

@media (min-width: 768px) {
    .station-container {
        grid-template-columns: 2fr 1fr;
    }
}

@media (min-width: 1200px) {
    .station-container {
        grid-template-columns: 3fr 1fr 1fr;
    }
}
```

#### Touch-Friendly Button Specifications
```css
.btn-scan {
    min-height: 60px;
    min-width: 200px;
    font-size: 1.5rem;
    font-weight: bold;
    border-radius: 8px;
    margin: 8px;
    touch-action: manipulation;
}

.btn-scan:active {
    transform: scale(0.98);
    transition: transform 0.1s;
}
```

### Station Interface Components

#### Barcode Scanner Input Component
```html
<!-- Shared/_BarcodeScanner.cshtml -->
<div class="barcode-scanner-container">
    <label for="barcodeInput" class="scanner-label">Scan Part Barcode</label>
    <div class="input-group input-group-lg">
        <input type="text" 
               id="barcodeInput" 
               class="form-control barcode-input" 
               placeholder="Scan or enter barcode"
               autocomplete="off"
               autofocus />
        <button class="btn btn-outline-primary" type="button" id="manualEntryBtn">
            <i class="fas fa-keyboard"></i> Manual Entry
        </button>
    </div>
    <div class="scanner-feedback" id="scannerFeedback"></div>
</div>

<script>
document.getElementById('barcodeInput').addEventListener('input', function(e) {
    if (e.target.value.length >= 8) { // Minimum barcode length
        processBarcodeInput(e.target.value);
    }
});
</script>
```

#### Storage Location Display Component
```html
<!-- Shared/_StorageLocation.cshtml -->
<div class="storage-location-card">
    <div class="card">
        <div class="card-header bg-primary text-white">
            <h5 class="mb-0">Storage Assignment</h5>
        </div>
        <div class="card-body">
            <div class="row">
                <div class="col-md-6">
                    <h3 class="rack-name">@Model.RackName</h3>
                    <p class="rack-location">@Model.RackLocation</p>
                </div>
                <div class="col-md-6">
                    <div class="slot-coordinates">
                        <span class="row-indicator">Row @Model.Row</span>
                        <span class="column-indicator">Column @Model.Column</span>
                    </div>
                </div>
            </div>
            <div class="walking-directions mt-3">
                <i class="fas fa-route"></i>
                <span>@Model.WalkingDirections</span>
            </div>
        </div>
    </div>
</div>
```

### Station-Specific Page Specifications

#### Sorting Station - Main Scanning Page
```html
<!-- Pages/Sorting/Scan.cshtml -->
@page "/sorting/scan"
@model SortingModel

<div class="station-header">
    <h1>Sorting Station</h1>
    <div class="station-status">
        <span class="badge bg-success">Online</span>
        <span class="scan-count">Scanned Today: @Model.TodaysScanCount</span>
    </div>
</div>

<div class="scanning-area">
    <partial name="_BarcodeScanner" />
    
    <div class="recent-scans">
        <h3>Recent Scans</h3>
        <div class="scan-history" id="scanHistory">
            @foreach(var scan in Model.RecentScans)
            {
                <div class="scan-entry">
                    <span class="part-number">@scan.PartNumber</span>
                    <span class="timestamp">@scan.ScannedAt.ToString("HH:mm:ss")</span>
                    <span class="assignment">@scan.AssignedLocation</span>
                </div>
            }
        </div>
    </div>
</div>

<div class="storage-assignment" id="storageAssignment" style="display: none;">
    <!-- Storage assignment will be populated via AJAX -->
</div>
```

#### Assembly Station - Product Assembly Page
```html
<!-- Pages/Assembly/Scan.cshtml -->
@page "/assembly/scan"
@model AssemblyModel

<div class="station-header">
    <h1>Assembly Station</h1>
    <div class="assembly-queue">
        <span class="queue-count">Ready for Assembly: @Model.ReadyCount</span>
    </div>
</div>

<div class="assembly-area">
    <partial name="_BarcodeScanner" />
    
    <div class="assembly-info" id="assemblyInfo" style="display: none;">
        <div class="product-details">
            <h3 id="productName"></h3>
            <p id="productDescription"></p>
        </div>
        
        <div class="subassembly-locations">
            <h4>Component Locations</h4>
            <div id="componentList">
                <!-- Populated via AJAX -->
            </div>
        </div>
        
        <div class="assembly-actions">
            <button class="btn btn-success btn-lg" id="confirmAssemblyBtn">
                <i class="fas fa-check"></i> Confirm Assembly Complete
            </button>
        </div>
    </div>
</div>
```

---

## SignalR Real-Time Specifications

### Hub Implementation
```csharp
[Authorize]
public class ShopFloorHub : Hub
{
    private readonly ILogger<ShopFloorHub> _logger;
    private readonly IUserService _userService;

    public ShopFloorHub(ILogger<ShopFloorHub> logger, IUserService userService)
    {
        _logger = logger;
        _userService = userService;
    }

    public async Task JoinStationGroup(string stationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Station_{stationId}");
        _logger.LogInformation($"User {Context.UserIdentifier} joined station group {stationId}");
    }

    public async Task LeaveStationGroup(string stationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Station_{stationId}");
        _logger.LogInformation($"User {Context.UserIdentifier} left station group {stationId}");
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        _logger.LogInformation($"User {Context.UserIdentifier} disconnected");
        await base.OnDisconnectedAsync(exception);
    }
}
```

### Client-Side SignalR Integration
```javascript
// wwwroot/js/signalr-client.js
class ShopFloorSignalR {
    constructor() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/shopfloorhub")
            .withAutomaticReconnect()
            .build();
            
        this.setupEventHandlers();
    }

    async start() {
        try {
            await this.connection.start();
            console.log("SignalR Connected");
            await this.joinStationGroup(window.stationId);
        } catch (err) {
            console.error("SignalR Connection Error: ", err);
        }
    }

    setupEventHandlers() {
        this.connection.on("PartScanned", (partData) => {
            this.handlePartScanned(partData);
        });

        this.connection.on("StorageAssigned", (assignmentData) => {
            this.handleStorageAssigned(assignmentData);
        });

        this.connection.on("ProductReady", (productData) => {
            this.handleProductReady(productData);
        });

        this.connection.on("SystemAlert", (alertData) => {
            this.handleSystemAlert(alertData);
        });
    }

    async joinStationGroup(stationId) {
        await this.connection.invoke("JoinStationGroup", stationId);
    }

    handlePartScanned(partData) {
        // Update UI with new part status
        updateScanHistory(partData);
        showScanFeedback(partData.success, partData.message);
    }

    handleStorageAssigned(assignmentData) {
        // Display storage assignment information
        showStorageAssignment(assignmentData);
    }
}

// Initialize SignalR connection
const signalR = new ShopFloorSignalR();
signalR.start();
```

---

## Background Services Specifications

### File Import Service Implementation
```csharp
public class FileImportService : BackgroundService
{
    private readonly ILogger<FileImportService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private FileSystemWatcher _fileWatcher;
    private readonly SemaphoreSlim _importSemaphore = new(1, 1);

    public FileImportService(
        ILogger<FileImportService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var importPath = _configuration["FileImport:ImportPath"];
        
        _fileWatcher = new FileSystemWatcher(importPath)
        {
            Filter = "*.sdf", // SQL CE file extension
            EnableRaisingEvents = true,
            IncludeSubdirectories = false
        };

        _fileWatcher.Created += OnFileCreated;
        _fileWatcher.Renamed += OnFileRenamed;

        _logger.LogInformation($"File import service monitoring: {importPath}");

        // Process any existing files on startup
        await ProcessExistingFiles(importPath, stoppingToken);

        // Keep service running
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        await ProcessImportFile(e.FullPath);
    }

    private async Task ProcessImportFile(string filePath)
    {
        await _importSemaphore.WaitAsync();
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var importService = scope.ServiceProvider.GetRequiredService<IWorkOrderImportService>();
            
            var result = await importService.ImportFromSqlCeFile(filePath);
            
            if (result.Success)
            {
                await ArchiveFile(filePath);
                _logger.LogInformation($"Successfully imported work order from {filePath}");
            }
            else
            {
                _logger.LogError($"Failed to import file {filePath}: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing import file {filePath}");
        }
        finally
        {
            _importSemaphore.Release();
        }
    }
}
```

### SQL CE File Processing Service
```csharp
public class SqlCeImportService : IWorkOrderImportService
{
    private readonly ILogger<SqlCeImportService> _logger;
    private readonly ShopFloorDbContext _context;

    public SqlCeImportService(ILogger<SqlCeImportService> logger, ShopFloorDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<ImportResult> ImportFromSqlCeFile(string filePath)
    {
        try
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            var connectionString = $"Data Source={filePath};";
            
            // Connect to SQL CE database
            using var sqlCeConnection = new SqlCeConnection(connectionString);
            await sqlCeConnection.OpenAsync();

            // Extract hierarchical data
            var workOrderData = await ExtractWorkOrderData(sqlCeConnection);
            var productData = await ExtractProductData(sqlCeConnection, workOrderData.WorkOrderId);
            var partData = await ExtractPartData(sqlCeConnection, workOrderData.WorkOrderId);
            var hardwareData = await ExtractHardwareData(sqlCeConnection, workOrderData.WorkOrderId);

            // Import to main database
            await ImportWorkOrderHierarchy(workOrderData, productData, partData, hardwareData);

            await transaction.CommitAsync();
            
            return new ImportResult { Success = true, WorkOrderId = workOrderData.WorkOrderId };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error importing SQL CE file: {filePath}");
            return new ImportResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private async Task<WorkOrderData> ExtractWorkOrderData(SqlCeConnection connection)
    {
        var query = @"
            SELECT WorkOrderId, WorkOrderNumber, CustomerName, OrderDate, DueDate
            FROM WorkOrders";
            
        using var command = new SqlCeCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();
        
        if (await reader.ReadAsync())
        {
            return new WorkOrderData
            {
                WorkOrderId = reader.GetString("WorkOrderId"),
                WorkOrderNumber = reader.GetString("WorkOrderNumber"),
                CustomerName = reader.GetString("CustomerName"),
                OrderDate = reader.GetDateTime("OrderDate"),
                DueDate = reader.GetDateTime("DueDate")
            };
        }
        
        throw new InvalidOperationException("No work order data found in SQL CE file");
    }
}
```

---

## Authentication and Authorization Specifications

### Authentication Configuration
```csharp
// Program.cs authentication setup
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8); // Work shift duration
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Administrator"));
    options.AddPolicy("OperatorAccess", policy => policy.RequireRole("Administrator", "Operator"));
    options.AddPolicy("StationAccess", policy => policy.RequireClaim("StationId"));
});
```

### User Management Service
```csharp
public class UserService : IUserService
{
    private readonly ShopFloorDbContext _context;
    private readonly IPasswordHasher<SystemUser> _passwordHasher;

    public async Task<AuthResult> AuthenticateAsync(string username, string password)
    {
        var user = await _context.SystemUsers
            .FirstOrDefaultAsync(u => u.UserName == username && u.IsActive);

        if (user == null)
            return new AuthResult { Success = false, Message = "Invalid username or password" };

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        
        if (verificationResult == PasswordVerificationResult.Success)
        {
            return new AuthResult 
            { 
                Success = true, 
                User = user,
                Claims = BuildUserClaims(user)
            };
        }

        return new AuthResult { Success = false, Message = "Invalid username or password" };
    }

    private List<Claim> BuildUserClaims(SystemUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Role, user.Role),
            new("DisplayName", user.DisplayName)
        };

        if (!string.IsNullOrEmpty(user.StationId))
        {
            claims.Add(new Claim("StationId", user.StationId));
        }

        return claims;
    }
}
```

---

## Error Handling and Logging Specifications

### Global Exception Handler
```csharp
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.StatusCode = exception switch
        {
            NotFoundException => 404,
            ValidationException => 400,
            UnauthorizedAccessException => 401,
            _ => 500
        };

        var response = new
        {
            error = new
            {
                message = exception.Message,
                statusCode = context.Response.StatusCode
            }
        };

        var jsonResponse = JsonSerializer.Serialize(response);
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(jsonResponse);
    }
}
```

### Logging Configuration
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information",
      "ShopFloorTracker": "Debug"
    },
    "Console": {
      "IncludeScopes": true
    },
    "File": {
      "Path": "logs/shopfloor-{Date}.log",
      "MinLevel": "Information",
      "RetainedFileCountLimit": 30
    }
  }
}
```

---

## Performance Optimization Specifications

### Database Query Optimization
```csharp
// Optimized queries for common operations
public class PartRepository : IPartRepository
{
    private readonly ShopFloorDbContext _context;

    public async Task<List<PartDto>> GetPartsByProductAsync(string productId)
    {
        return await _context.Parts
            .Where(p => p.ProductId == productId)
            .Select(p => new PartDto
            {
                PartId = p.PartId,
                PartNumber = p.PartNumber,
                PartName = p.PartName,
                Status = p.Status,
                CurrentLocation = p.StorageAssignments
                    .Where(sa => sa.IsActive)
                    .Select(sa => $"{sa.StorageSlot.StorageRack.RackName}-{sa.StorageSlot.Row}-{sa.StorageSlot.Column}")
                    .FirstOrDefault()
            })
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> UpdatePartStatusAsync(string partId, PartStatus newStatus, string userId)
    {
        var part = await _context.Parts.FindAsync(partId);
        if (part == null) return false;

        var oldStatus = part.Status;
        part.Status = newStatus;
        part.ModifiedDate = DateTime.UtcNow;

        // Add process history
        _context.PartProcessHistory.Add(new PartProcessHistory
        {
            PartId = partId,
            PreviousStatus = oldStatus.ToString(),
            NewStatus = newStatus.ToString(),
            ProcessedBy = userId,
            ProcessedDate = DateTime.UtcNow
        });

        var rowsAffected = await _context.SaveChangesAsync();
        return rowsAffected > 0;
    }
}
```

### Caching Strategy
```csharp
public class CachedConfigurationService : IConfigurationService
{
    private readonly IMemoryCache _cache;
    private readonly ShopFloorDbContext _context;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(30);

    public async Task<string> GetSettingAsync(string key)
    {
        var cacheKey = $"setting_{key}";
        
        if (_cache.TryGetValue(cacheKey, out string cachedValue))
        {
            return cachedValue;
        }

        var setting = await _context.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SettingKey == key);

        if (setting != null)
        {
            _cache.Set(cacheKey, setting.SettingValue, _cacheExpiry);
            return setting.SettingValue;
        }

        return null;
    }
}
```

---

## Testing Specifications

### Unit Test Framework Setup
```csharp
public class PartServiceTests
{
    private readonly Mock<IPartRepository> _mockPartRepository;
    private readonly Mock<IStorageService> _mockStorageService;
    private readonly Mock<ISignalRService> _mockSignalRService;
    private readonly PartService _partService;

    public PartServiceTests()
    {
        _mockPartRepository = new Mock<IPartRepository>();
        _mockStorageService = new Mock<IStorageService>();
        _mockSignalRService = new Mock<ISignalRService>();
        
        _partService = new PartService(
            _mockPartRepository.Object,
            _mockStorageService.Object,
            _mockSignalRService.Object);
    }

    [Fact]
    public async Task ScanPart_ValidPart_UpdatesStatusAndAssignsStorage()
    {
        // Arrange
        var partId = "PART001";
        var part = new Part { PartId = partId, Status = PartStatus.Cut };
        var storageAssignment = new StorageAssignment { SlotId = "RACK01-05-10" };

        _mockPartRepository.Setup(r => r.GetByIdAsync(partId))
            .ReturnsAsync(part);
        _mockStorageService.Setup(s => s.AssignStorageAsync(partId))
            .ReturnsAsync(storageAssignment);

        // Act
        var result = await _partService.ScanPartAsync(partId, "SORT-001", "user1");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(PartStatus.Sorted, part.Status);
        _mockSignalRService.Verify(s => s.NotifyPartScannedAsync(It.IsAny<PartScannedEvent>()), Times.Once);
    }
}
```

### Integration Test Configuration
```csharp
public class WebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the app's ApplicationDbContext registration
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ShopFloorDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add ApplicationDbContext using an in-memory database for testing
            services.AddDbContext<ShopFloorDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });

            // Build the service provider
            var sp = services.BuildServiceProvider();

            // Create a scope to obtain a reference to the database context
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<ShopFloorDbContext>();

            // Ensure the database is created
            db.Database.EnsureCreated();

            // Seed the database with test data
            DatabaseSeeder.SeedDatabase(db);
        });
    }
}
```

---

*This technical specification document provides the detailed implementation guidelines for all system components. All development should follow these specifications to ensure consistency and maintainability.*