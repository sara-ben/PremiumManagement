using server.Models.Enums;

namespace server.Models.Entities;

public class MetricFieldMapping
{
    public int Id { get; set; }
    public int MetricFileDefinitionId { get; set; }
    public string ExcelColumnName { get; set; } = string.Empty;
    public string SystemFieldName { get; set; } = string.Empty;
    public FieldDataType DataType { get; set; }
    public bool IsRequired { get; set; }
    public string? ValidationRule { get; set; }
    public int DisplayOrder { get; set; }

    public MetricFileDefinition? MetricFileDefinition { get; set; }
}
