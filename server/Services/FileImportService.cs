using System.Globalization;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models.Dtos;
using server.Models.Entities;
using server.Models.Enums;

namespace server.Services;

public class FileImportService(AppDbContext db)
{
    public async Task<(bool Success, string? Error, ImportResultDto? Result)> ImportAsync(
        int metricId, int year, string period, Stream fileStream, string fileName)
    {
        var metric = await db.Metrics
            .Include(m => m.FileDefinitions)
            .ThenInclude(fd => fd.FieldMappings)
            .FirstOrDefaultAsync(m => m.Id == metricId);

        if (metric is null) return (false, "המדד לא נמצא.", null);
        if (!metric.IsActive) return (false, "לא ניתן לקלוט נתונים למדד מוקפא.", null);
        if (metric.FileDefinitions.Count == 0) return (false, "לא הוגדר מבנה קובץ עבור מדד זה.", null);

        using var workbook = new XLWorkbook(fileStream);

        var candidates = metric.FileDefinitions
            .OrderByDescending(fd => fd.IsActive)
            .ThenByDescending(fd => fd.VersionNumber)
            .ToList();

        MetricFileDefinition? matchedDefinition = null;
        IXLWorksheet? matchedWorksheet = null;
        Dictionary<string, int>? matchedColumnIndex = null;

        foreach (var candidate in candidates)
        {
            var worksheet = ResolveWorksheet(workbook, candidate.SheetName);
            if (worksheet is null) continue;

            var columnIndex = ReadHeaderColumns(worksheet, candidate.HeaderRowNumber);
            var requiredColumns = candidate.FieldMappings.Select(fm => fm.ExcelColumnName);
            var allPresent = requiredColumns.All(columnIndex.ContainsKey);

            if (allPresent)
            {
                matchedDefinition = candidate;
                matchedWorksheet = worksheet;
                matchedColumnIndex = columnIndex;
                break;
            }
        }

        if (matchedDefinition is null || matchedWorksheet is null || matchedColumnIndex is null)
        {
            return (false, "מבנה הקובץ שהועלה אינו תואם לאף אחת מהגדרות המבנה של המדד.", null);
        }

        var batch = new ImportBatch
        {
            MetricId = metricId,
            MetricFileDefinitionId = matchedDefinition.Id,
            FileName = fileName,
            Year = year,
            Period = period,
            ImportDate = DateTime.UtcNow,
            Status = ImportStatus.Processing
        };

        var rowErrors = new List<RowValidationErrorDto>();
        var lastRowUsed = matchedWorksheet.LastRowUsed()?.RowNumber() ?? matchedDefinition.HeaderRowNumber;

        for (var rowNum = matchedDefinition.HeaderRowNumber + 1; rowNum <= lastRowUsed; rowNum++)
        {
            var row = matchedWorksheet.Row(rowNum);
            if (row.IsEmpty()) continue;

            var data = new Dictionary<string, object?>();
            var errors = new List<string>();

            foreach (var mapping in matchedDefinition.FieldMappings.OrderBy(fm => fm.DisplayOrder))
            {
                var colIndex = matchedColumnIndex[mapping.ExcelColumnName];
                var cell = row.Cell(colIndex);
                var (value, error) = ConvertCell(cell, mapping);

                if (error is not null)
                {
                    errors.Add($"{mapping.SystemFieldName}: {error}");
                }

                data[mapping.SystemFieldName] = value;
            }

            var isValid = errors.Count == 0;
            var importRow = new ImportRow
            {
                RowNumber = rowNum,
                DataJson = JsonSerializer.Serialize(data),
                IsValid = isValid,
                ValidationErrors = isValid ? null : JsonSerializer.Serialize(errors)
            };

            batch.Rows.Add(importRow);

            if (!isValid)
            {
                rowErrors.Add(new RowValidationErrorDto { RowNumber = rowNum, Errors = errors });
            }
        }

        batch.TotalRows = batch.Rows.Count;
        batch.ValidRows = batch.Rows.Count(r => r.IsValid);
        batch.InvalidRows = batch.Rows.Count(r => !r.IsValid);

        batch.Status = batch.TotalRows == 0
            ? ImportStatus.Failed
            : batch.InvalidRows == 0
                ? ImportStatus.Success
                : batch.ValidRows == 0
                    ? ImportStatus.Failed
                    : ImportStatus.PartialSuccess;

        if (batch.TotalRows == 0)
        {
            batch.ErrorSummary = "לא נמצאו שורות נתונים לעיבוד בקובץ.";
        }
        else if (batch.InvalidRows > 0)
        {
            batch.ErrorSummary = $"{batch.InvalidRows} מתוך {batch.TotalRows} שורות נכשלו בוולידציה.";
        }

        db.ImportBatches.Add(batch);
        await db.SaveChangesAsync();

        return (true, null, new ImportResultDto
        {
            Batch = new ImportBatchDetailDto
            {
                Id = batch.Id,
                MetricId = batch.MetricId,
                MetricName = metric.Name,
                FileName = batch.FileName,
                Year = batch.Year,
                Period = batch.Period,
                ImportDate = batch.ImportDate,
                Status = batch.Status,
                TotalRows = batch.TotalRows,
                ValidRows = batch.ValidRows,
                InvalidRows = batch.InvalidRows,
                MetricFileDefinitionId = matchedDefinition.Id,
                FileDefinitionVersion = matchedDefinition.VersionNumber,
                ErrorSummary = batch.ErrorSummary
            },
            RowErrors = rowErrors
        });
    }

    private static IXLWorksheet? ResolveWorksheet(XLWorkbook workbook, string sheetName)
    {
        if (workbook.TryGetWorksheet(sheetName, out var worksheet)) return worksheet;
        return workbook.Worksheets.Count > 0 ? workbook.Worksheet(1) : null;
    }

    private static Dictionary<string, int> ReadHeaderColumns(IXLWorksheet worksheet, int headerRowNumber)
    {
        var result = new Dictionary<string, int>();
        var headerRow = worksheet.Row(headerRowNumber);
        var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        for (var col = 1; col <= lastColumn; col++)
        {
            var text = headerRow.Cell(col).GetString().Trim();
            if (!string.IsNullOrEmpty(text) && !result.ContainsKey(text))
            {
                result[text] = col;
            }
        }

        return result;
    }

    private static (object? Value, string? Error) ConvertCell(IXLCell cell, MetricFieldMapping mapping)
    {
        var isBlank = cell.IsEmpty();

        if (isBlank)
        {
            return mapping.IsRequired ? (null, "שדה חובה חסר") : (null, null);
        }

        switch (mapping.DataType)
        {
            case FieldDataType.String:
            {
                var text = cell.GetString().Trim();
                if (mapping.IsRequired && string.IsNullOrEmpty(text))
                    return (null, "שדה חובה חסר");

                if (!string.IsNullOrEmpty(mapping.ValidationRule) && !string.IsNullOrEmpty(text))
                {
                    try
                    {
                        if (!System.Text.RegularExpressions.Regex.IsMatch(text, mapping.ValidationRule))
                            return (text, "הערך אינו תואם לכלל הוולידציה");
                    }
                    catch (ArgumentException)
                    {
                        // Invalid regex in configuration - skip pattern validation.
                    }
                }

                return (text, null);
            }
            case FieldDataType.Number:
            {
                double number;
                if (cell.DataType == XLDataType.Number)
                {
                    number = cell.GetDouble();
                }
                else if (!double.TryParse(cell.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out number))
                {
                    return (null, "ערך אינו מספר תקין");
                }

                if (!string.IsNullOrEmpty(mapping.ValidationRule))
                {
                    var parts = mapping.ValidationRule.Split('|');
                    if (parts.Length == 2)
                    {
                        if (double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var min) && number < min)
                            return (number, $"הערך קטן מהמינימום המותר ({min})");
                        if (double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var max) && number > max)
                            return (number, $"הערך גדול מהמקסימום המותר ({max})");
                    }
                }

                return (number, null);
            }
            case FieldDataType.Date:
            {
                DateTime date;
                if (cell.DataType == XLDataType.DateTime)
                {
                    date = cell.GetDateTime();
                }
                else if (!DateTime.TryParse(cell.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    return (null, "ערך אינו תאריך תקין");
                }

                return (date.ToString("yyyy-MM-dd"), null);
            }
            case FieldDataType.Boolean:
            {
                var text = cell.GetString().Trim();
                if (bool.TryParse(text, out var boolValue)) return (boolValue, null);
                if (text is "1" or "כן") return (true, null);
                if (text is "0" or "לא") return (false, null);
                return (null, "ערך אינו בוליאני תקין");
            }
            default:
                return (cell.GetString(), null);
        }
    }
}
