using ShopFloorTracker.Core.Enums;

namespace ShopFloorTracker.Core.Entities;

public class WorkOrder
{
    public string WorkOrderId { get; set; } = string.Empty;
    public string WorkOrderNumber { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public DateTime? OrderDate { get; set; }
    public DateTime? DueDate { get; set; }
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Active;
    public DateTime ImportedDate { get; set; } = DateTime.UtcNow;
    public string? ImportedBy { get; set; }
    public string? ImportFilePath { get; set; }
    public int TotalProducts { get; set; }
    public int TotalParts { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    public virtual ICollection<Hardware> Hardware { get; set; } = new List<Hardware>();
    public virtual ICollection<DetachedProduct> DetachedProducts { get; set; } = new List<DetachedProduct>();
}