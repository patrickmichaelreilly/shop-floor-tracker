using ShopFloorTracker.Core.Enums;

namespace ShopFloorTracker.Core.Entities;

public class StorageRack
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Rows { get; set; }
    public int Columns { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Part> Parts { get; set; } = new List<Part>();
}