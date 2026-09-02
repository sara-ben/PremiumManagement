using server.Models.Enums;

namespace server.Models.Entities;

public class Metric
{
    public int Id { get; set; }
    public int PremiumMethodId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MetricSourceType SourceType { get; set; }
    public CalculationPeriod ImportFrequency { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public PremiumMethod? PremiumMethod { get; set; }
    public ICollection<MetricFileDefinition> FileDefinitions { get; set; } = new List<MetricFileDefinition>();
    public ICollection<ImportBatch> ImportBatches { get; set; } = new List<ImportBatch>();
}
