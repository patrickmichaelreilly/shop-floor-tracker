namespace ShopFloorTracker.Core.Entities;

public class PartPlacement
{
    public Guid PlacementId { get; set; } = Guid.NewGuid();
    public string PartId { get; set; } = string.Empty;
    public string PlacedSheetId { get; set; } = string.Empty;
    public decimal? XCoord { get; set; }
    public decimal? YCoord { get; set; }
    public int Rotation { get; set; } = 0;
    public bool IsFlipped { get; set; } = false;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation
    public Part Part { get; set; } = null!;
    public PlacedSheet PlacedSheet { get; set; } = null!;
}