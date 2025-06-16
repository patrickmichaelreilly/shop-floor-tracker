namespace ShopFloorTracker.Core.Entities;

public class PlacedSheet
{
    public string PlacedSheetId { get; set; } = string.Empty;
    public string SheetName { get; set; } = string.Empty;
    public string BarCode { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string WorkOrderId { get; set; } = string.Empty;
    public string? MaterialType { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Thickness { get; set; }
    public string Status { get; set; } = "Pending";
    public string? MicrovellumLinkID { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation
    public WorkOrder WorkOrder { get; set; } = null!;
    public List<PartPlacement> PartPlacements { get; set; } = new();
}