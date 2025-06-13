namespace ShopFloorTracker.Core.Entities;

public class DetachedProduct
{
    public string DetachedProductId { get; set; } = string.Empty;
    public string WorkOrderId { get; set; } = string.Empty;
    public string ProductNumber { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? IncludedDate { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    public virtual WorkOrder WorkOrder { get; set; } = null!;
}