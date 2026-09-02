using server.Models.Enums;

namespace server.Models.Entities;

public class PremiumMethod
{
    public int Id { get; set; }
    public string MethodNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PremiumPercentage { get; set; }
    public CalculationPeriod CalculationPeriod { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Metric> Metrics { get; set; } = new List<Metric>();
}
