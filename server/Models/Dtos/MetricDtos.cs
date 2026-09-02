using System.ComponentModel.DataAnnotations;
using server.Models.Enums;

namespace server.Models.Dtos;

public class MetricListItemDto
{
    public int Id { get; set; }
    public int PremiumMethodId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MetricSourceType SourceType { get; set; }
    public CalculationPeriod ImportFrequency { get; set; }
    public bool IsActive { get; set; }
    public int? ActiveFileDefinitionVersion { get; set; }
}

public class MetricDetailDto : MetricListItemDto
{
    public List<FileDefinitionDto> FileDefinitions { get; set; } = new();
}

public class MetricCreateDto
{
    [Required]
    public int PremiumMethodId { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public MetricSourceType SourceType { get; set; }

    public CalculationPeriod ImportFrequency { get; set; }
}

public class MetricUpdateDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public MetricSourceType SourceType { get; set; }

    public CalculationPeriod ImportFrequency { get; set; }
}

public class FieldMappingDto
{
    public int Id { get; set; }
    public string ExcelColumnName { get; set; } = string.Empty;
    public string SystemFieldName { get; set; } = string.Empty;
    public FieldDataType DataType { get; set; }
    public bool IsRequired { get; set; }
    public string? ValidationRule { get; set; }
    public int DisplayOrder { get; set; }
}

public class FieldMappingCreateDto
{
    [Required, MaxLength(200)]
    public string ExcelColumnName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string SystemFieldName { get; set; } = string.Empty;

    public FieldDataType DataType { get; set; }
    public bool IsRequired { get; set; }
    public string? ValidationRule { get; set; }
    public int DisplayOrder { get; set; }
}

public class FileDefinitionDto
{
    public int Id { get; set; }
    public int MetricId { get; set; }
    public int VersionNumber { get; set; }
    public string SheetName { get; set; } = string.Empty;
    public int HeaderRowNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<FieldMappingDto> FieldMappings { get; set; } = new();
}

public class FileDefinitionCreateDto
{
    [Required, MaxLength(200)]
    public string SheetName { get; set; } = string.Empty;

    [Range(1, 1000)]
    public int HeaderRowNumber { get; set; } = 1;

    public bool SetAsActive { get; set; } = true;

    [MinLength(1)]
    public List<FieldMappingCreateDto> FieldMappings { get; set; } = new();
}
