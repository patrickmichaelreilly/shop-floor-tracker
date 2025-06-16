namespace ShopFloorTracker.Application.Interfaces;

public interface IMicrovellumImportService
{
    Task<ImportResult> ImportWorkOrderAsync(string sdfFilePath);
    Task<List<string>> GetAvailableFilesAsync();
}

public class ImportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int WorkOrdersCreated { get; set; }
    public int ProductsCreated { get; set; }
    public int PartsCreated { get; set; }
    public int HardwareCreated { get; set; }
    public int PlacedSheetsCreated { get; set; }
    public int PartPlacementsCreated { get; set; }
    public List<string> Errors { get; set; } = new();
}