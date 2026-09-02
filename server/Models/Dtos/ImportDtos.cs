using server.Models.Enums;

namespace server.Models.Dtos;

public class ImportBatchListItemDto
{
    public int Id { get; set; }
    public int MetricId { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Period { get; set; } = string.Empty;
    public DateTime ImportDate { get; set; }
    public ImportStatus Status { get; set; }
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }
}

public class ImportBatchDetailDto : ImportBatchListItemDto
{
    public int MetricFileDefinitionId { get; set; }
    public int FileDefinitionVersion { get; set; }
    public string? ErrorSummary { get; set; }
}

public class RowValidationErrorDto
{
    public int RowNumber { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class ImportResultDto
{
    public ImportBatchDetailDto Batch { get; set; } = new();
    public List<RowValidationErrorDto> RowErrors { get; set; } = new();
}
