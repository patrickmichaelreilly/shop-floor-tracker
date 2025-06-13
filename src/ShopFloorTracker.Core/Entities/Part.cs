using ShopFloorTracker.Core.Enums;

namespace ShopFloorTracker.Core.Entities;

public class Part
{
    public string PartId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string? SubassemblyId { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string? PartName { get; set; }
    public string? Material { get; set; }
    public decimal? Thickness { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public string? EdgeBanding { get; set; }
    public string? NestingSheet { get; set; }
    public PartStatus Status { get; set; } = PartStatus.Pending;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    
    // Storage location tracking
    public int? StorageRackId { get; set; }
    public int? StorageRow { get; set; }
    public int? StorageColumn { get; set; }
    public DateTime? SortedDateTime { get; set; }
    public DateTime? AssembledDateTime { get; set; }

    public virtual Product Product { get; set; } = null!;
    public virtual Subassembly? Subassembly { get; set; }
    public virtual StorageRack? StorageRack { get; set; }
    public virtual ICollection<ScanActivity> ScanActivities { get; set; } = new List<ScanActivity>();
}