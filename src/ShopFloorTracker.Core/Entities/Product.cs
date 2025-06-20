using ShopFloorTracker.Core.Enums;

namespace ShopFloorTracker.Core.Entities;

public class Product
{
    public string ProductId { get; set; } = string.Empty;
    public string WorkOrderId { get; set; } = string.Empty;
    public string ProductNumber { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public string? ProductType { get; set; }
    public ProductStatus Status { get; set; } = ProductStatus.Pending;
    public string? MicrovellumLinkID { get; set; }
    public DateTime? AssemblyDate { get; set; }
    public DateTime? ShippedDate { get; set; }
    public int TotalParts { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    public virtual WorkOrder WorkOrder { get; set; } = null!;
    public virtual ICollection<Part> Parts { get; set; } = new List<Part>();
    public virtual ICollection<Subassembly> Subassemblies { get; set; } = new List<Subassembly>();
    public virtual ICollection<Hardware> Hardware { get; set; } = new List<Hardware>();
}