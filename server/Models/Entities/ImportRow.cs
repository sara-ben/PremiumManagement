namespace server.Models.Entities;

public class ImportRow
{
    public int Id { get; set; }
    public int ImportBatchId { get; set; }
    public int RowNumber { get; set; }
    public string DataJson { get; set; } = "{}";
    public bool IsValid { get; set; }
    public string? ValidationErrors { get; set; }

    public ImportBatch? ImportBatch { get; set; }
}
