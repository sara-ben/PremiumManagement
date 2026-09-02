namespace server.Models.Entities;

public class MetricFileDefinition
{
    public int Id { get; set; }
    public int MetricId { get; set; }
    public int VersionNumber { get; set; }
    public string SheetName { get; set; } = string.Empty;
    public int HeaderRowNumber { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Metric? Metric { get; set; }
    public ICollection<MetricFieldMapping> FieldMappings { get; set; } = new List<MetricFieldMapping>();
}
