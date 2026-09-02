using server.Models.Enums;

namespace server.Models.Entities;

public class ImportBatch
{
    public int Id { get; set; }
    public int MetricId { get; set; }
    public int MetricFileDefinitionId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Period { get; set; } = string.Empty;
    public DateTime ImportDate { get; set; } = DateTime.UtcNow;
    public ImportStatus Status { get; set; } = ImportStatus.Pending;
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }
    public string? ErrorSummary { get; set; }

    public Metric? Metric { get; set; }
    public MetricFileDefinition? MetricFileDefinition { get; set; }
    public ICollection<ImportRow> Rows { get; set; } = new List<ImportRow>();
}
