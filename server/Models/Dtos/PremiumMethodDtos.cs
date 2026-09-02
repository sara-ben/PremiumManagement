using System.ComponentModel.DataAnnotations;
using server.Models.Enums;

namespace server.Models.Dtos;

public class PremiumMethodListItemDto
{
    public int Id { get; set; }
    public string MethodNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PremiumPercentage { get; set; }
    public CalculationPeriod CalculationPeriod { get; set; }
    public bool IsActive { get; set; }
    public int MetricsCount { get; set; }
}

public class PremiumMethodDetailDto : PremiumMethodListItemDto
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<MetricListItemDto> Metrics { get; set; } = new();
}

public class PremiumMethodCreateDto
{
    [Required, MaxLength(50)]
    public string MethodNumber { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Range(0, 1000)]
    public decimal PremiumPercentage { get; set; }

    public CalculationPeriod CalculationPeriod { get; set; }
}

public class PremiumMethodUpdateDto
{
    [Required, MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Range(0, 1000)]
    public decimal PremiumPercentage { get; set; }

    public CalculationPeriod CalculationPeriod { get; set; }
}
