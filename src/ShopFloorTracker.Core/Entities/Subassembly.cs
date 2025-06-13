namespace ShopFloorTracker.Core.Entities;

public class Subassembly
{
    public string SubassemblyId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string SubassemblyNumber { get; set; } = string.Empty;
    public string? SubassemblyName { get; set; }
    public string? SubassemblyType { get; set; }
    public string Status { get; set; } = "Pending";
    public int TotalParts { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    public virtual Product Product { get; set; } = null!;
    public virtual ICollection<Part> Parts { get; set; } = new List<Part>();
}