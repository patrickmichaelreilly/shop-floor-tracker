namespace ShopFloorTracker.Core.Entities;

public class Hardware
{
    public string HardwareId { get; set; } = string.Empty;
    public string WorkOrderId { get; set; } = string.Empty;
    public string HardwareNumber { get; set; } = string.Empty;
    public string? HardwareName { get; set; }
    public int Quantity { get; set; } = 1;
    public string Status { get; set; } = "Pending";
    public DateTime? IncludedDate { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    public virtual WorkOrder WorkOrder { get; set; } = null!;
}