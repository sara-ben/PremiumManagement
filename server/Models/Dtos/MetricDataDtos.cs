using server.Models.Enums;

namespace server.Models.Dtos;

public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class FieldFilterOptionDto
{
    public string SystemFieldName { get; set; } = string.Empty;
    public string ExcelColumnName { get; set; } = string.Empty;
    public FieldDataType DataType { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
    public List<string> AvailableOperators { get; set; } = new();
}

public class MetricDataRowDto
{
    public int RowId { get; set; }
    public int ImportBatchId { get; set; }
    public int RowNumber { get; set; }
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public Dictionary<string, object?> Data { get; set; } = new();
}

public class DataFieldFilter
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? ValueTo { get; set; }
}

public class MetricDataQueryDto
{
    public int? ImportBatchId { get; set; }
    public int? Year { get; set; }
    public string? Period { get; set; }
    public bool? ValidOnly { get; set; }
    public List<DataFieldFilter> Filters { get; set; } = new();
    public string? SortField { get; set; }
    public string? SortDirection { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
