namespace ShopFloorTracker.Core.Entities;

public class Hardware
{
    public string HardwareId { get; set; } = string.Empty;
    public string HardwareName { get; set; } = string.Empty;
    public string? HardwareDescription { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string WorkOrderId { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public string Status { get; set; } = "Pending";
    public string? MicrovellumLinkID { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public Product Product { get; set; } = null!;
    public WorkOrder WorkOrder { get; set; } = null!;
}