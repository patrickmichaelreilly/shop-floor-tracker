using ShopFloorTracker.Core.Enums;

namespace ShopFloorTracker.Core.Entities;

public class ScanActivity
{
    public int Id { get; set; }
    public string PartId { get; set; } = string.Empty;
    public string StationName { get; set; } = string.Empty;
    public string Activity { get; set; } = string.Empty;
    public PartStatus? OldStatus { get; set; }
    public PartStatus? NewStatus { get; set; }
    public string? StorageLocation { get; set; }
    public string? OperatorId { get; set; }
    public DateTime ScanDateTime { get; set; } = DateTime.UtcNow;

    public virtual Part Part { get; set; } = null!;
}