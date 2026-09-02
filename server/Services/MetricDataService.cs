using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models.Dtos;
using server.Models.Entities;
using server.Models.Enums;

namespace server.Services;

public class MetricDataService(AppDbContext db)
{
    public async Task<List<FieldFilterOptionDto>?> GetFieldsAsync(int metricId)
    {
        var activeDefinition = await db.MetricFileDefinitions
            .AsNoTracking()
            .Include(fd => fd.FieldMappings)
            .Where(fd => fd.MetricId == metricId && fd.IsActive)
            .FirstOrDefaultAsync();

        if (activeDefinition is null) return null;

        return activeDefinition.FieldMappings
            .OrderBy(fm => fm.DisplayOrder)
            .Select(fm => new FieldFilterOptionDto
            {
                SystemFieldName = fm.SystemFieldName,
                ExcelColumnName = fm.ExcelColumnName,
                DataType = fm.DataType,
                IsRequired = fm.IsRequired,
                DisplayOrder = fm.DisplayOrder,
                AvailableOperators = fm.DataType switch
                {
                    FieldDataType.String => new List<string> { "contains" },
                    FieldDataType.Number => new List<string> { "eq", "range" },
                    FieldDataType.Date => new List<string> { "range" },
                    FieldDataType.Boolean => new List<string> { "eq" },
                    _ => new List<string>()
                }
            })
            .ToList();
    }

    public async Task<(bool Success, string? Error, PagedResultDto<MetricDataRowDto>? Result)> GetDataAsync(
        int metricId, int? importBatchId, int? year, string? period, bool? validOnly,
        List<DataFieldFilter> filters, string? sortField, string? sortDirection, int page, int pageSize)
    {
        var metric = await db.Metrics.FirstOrDefaultAsync(m => m.Id == metricId);
        if (metric is null) return (false, "המדד לא נמצא.", null);

        var fieldMappings = await GetFieldsAsync(metricId);
        if (fieldMappings is null)
        {
            return (false, "לא הוגדר מבנה קובץ פעיל עבור מדד זה.", null);
        }

        var fieldTypeMap = fieldMappings.ToDictionary(f => f.SystemFieldName, f => f.DataType);

        var query = db.ImportRows
            .AsNoTracking()
            .Include(r => r.ImportBatch)
            .Where(r => r.ImportBatch!.MetricId == metricId)
            .AsQueryable();

        if (importBatchId.HasValue) query = query.Where(r => r.ImportBatchId == importBatchId.Value);
        if (year.HasValue) query = query.Where(r => r.ImportBatch!.Year == year.Value);
        if (!string.IsNullOrWhiteSpace(period)) query = query.Where(r => r.ImportBatch!.Period == period);
        if (validOnly.HasValue) query = query.Where(r => r.IsValid == validOnly.Value);

        var rows = await query.ToListAsync();

        var parsedRows = rows.Select(r => new
        {
            Row = r,
            Data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(r.DataJson) ?? new()
        }).ToList();

        foreach (var filter in filters)
        {
            if (!fieldTypeMap.TryGetValue(filter.Field, out var dataType)) continue;

            parsedRows = parsedRows.Where(pr => MatchesFilter(pr.Data, filter, dataType)).ToList();
        }

        var totalCount = parsedRows.Count;

        if (!string.IsNullOrWhiteSpace(sortField) && fieldTypeMap.TryGetValue(sortField, out var sortType))
        {
            var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            parsedRows = (descending
                    ? parsedRows.OrderByDescending(pr => GetSortKey(pr.Data, sortField, sortType))
                    : parsedRows.OrderBy(pr => GetSortKey(pr.Data, sortField, sortType)))
                .ToList();
        }
        else
        {
            parsedRows = parsedRows.OrderBy(pr => pr.Row.ImportBatchId).ThenBy(pr => pr.Row.RowNumber).ToList();
        }

        var pageItems = parsedRows
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(pr => new MetricDataRowDto
            {
                RowId = pr.Row.Id,
                ImportBatchId = pr.Row.ImportBatchId,
                RowNumber = pr.Row.RowNumber,
                IsValid = pr.Row.IsValid,
                ValidationErrors = pr.Row.ValidationErrors is null
                    ? new()
                    : JsonSerializer.Deserialize<List<string>>(pr.Row.ValidationErrors) ?? new(),
                Data = pr.Data.ToDictionary(kv => kv.Key, kv => (object?)ConvertJsonElement(kv.Value))
            })
            .ToList();

        return (true, null, new PagedResultDto<MetricDataRowDto>
        {
            Items = pageItems,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    private static object? ConvertJsonElement(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => element.ToString()
    };

    private static IComparable GetSortKey(Dictionary<string, JsonElement> data, string field, FieldDataType dataType)
    {
        if (!data.TryGetValue(field, out var element) || element.ValueKind == JsonValueKind.Null)
        {
            return dataType == FieldDataType.Number ? double.MinValue : string.Empty;
        }

        return dataType switch
        {
            FieldDataType.Number => element.ValueKind == JsonValueKind.Number ? element.GetDouble() : 0d,
            FieldDataType.Date => DateTime.TryParse(element.GetString(), out var d) ? d : DateTime.MinValue,
            FieldDataType.Boolean => element.ValueKind == JsonValueKind.True,
            _ => element.ToString()
        };
    }

    private static bool MatchesFilter(Dictionary<string, JsonElement> data, DataFieldFilter filter, FieldDataType dataType)
    {
        if (!data.TryGetValue(filter.Field, out var element) || element.ValueKind == JsonValueKind.Null)
        {
            return false;
        }

        switch (dataType)
        {
            case FieldDataType.String:
            {
                var text = element.GetString() ?? string.Empty;
                return string.IsNullOrEmpty(filter.Value) ||
                       text.Contains(filter.Value, StringComparison.OrdinalIgnoreCase);
            }
            case FieldDataType.Number:
            {
                var number = element.ValueKind == JsonValueKind.Number ? element.GetDouble() : (double?)null;
                if (number is null) return false;

                if (string.Equals(filter.Operator, "eq", StringComparison.OrdinalIgnoreCase))
                {
                    return double.TryParse(filter.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var eqVal)
                           && Math.Abs(number.Value - eqVal) < 0.0000001;
                }

                var min = double.TryParse(filter.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var minVal) ? minVal : (double?)null;
                var max = double.TryParse(filter.ValueTo, NumberStyles.Any, CultureInfo.InvariantCulture, out var maxVal) ? maxVal : (double?)null;

                if (min.HasValue && number < min.Value) return false;
                if (max.HasValue && number > max.Value) return false;
                return true;
            }
            case FieldDataType.Date:
            {
                if (!DateTime.TryParse(element.GetString(), out var date)) return false;

                var from = DateTime.TryParse(filter.Value, out var fromVal) ? fromVal : (DateTime?)null;
                var to = DateTime.TryParse(filter.ValueTo, out var toVal) ? toVal : (DateTime?)null;

                if (from.HasValue && date < from.Value) return false;
                if (to.HasValue && date > to.Value) return false;
                return true;
            }
            case FieldDataType.Boolean:
            {
                var boolValue = element.ValueKind == JsonValueKind.True;
                return bool.TryParse(filter.Value, out var target) && boolValue == target;
            }
            default:
                return true;
        }
    }
}
