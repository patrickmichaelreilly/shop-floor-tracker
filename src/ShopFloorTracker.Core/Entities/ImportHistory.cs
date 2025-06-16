namespace ShopFloorTracker.Core.Entities;

public class ImportHistory
{
    public Guid ImportId { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public DateTime ImportDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = string.Empty;
    public int RecordsImported { get; set; } = 0;
    public string? ErrorMessage { get; set; }
    public int WorkOrdersCreated { get; set; } = 0;
    public int ProductsCreated { get; set; } = 0;
    public int PartsCreated { get; set; } = 0;
    public int HardwareCreated { get; set; } = 0;
    public int PlacedSheetsCreated { get; set; } = 0;
    public int PartPlacementsCreated { get; set; } = 0;
    public string? FilePath { get; set; }
    public long? FileSize { get; set; }
    public string? ImportedBy { get; set; }
    public int ErrorCount { get; set; } = 0;
    public string? WorkOrderId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation
    public WorkOrder? WorkOrder { get; set; }
}