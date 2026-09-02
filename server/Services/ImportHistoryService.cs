using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models.Dtos;
using server.Models.Enums;

namespace server.Services;

public class ImportHistoryService(AppDbContext db)
{
    public async Task<PagedResultDto<ImportBatchListItemDto>> GetListAsync(
        int? metricId, int? year, string? period, ImportStatus? status, int page, int pageSize)
    {
        var query = db.ImportBatches.AsNoTracking().Include(b => b.Metric).AsQueryable();

        if (metricId.HasValue) query = query.Where(b => b.MetricId == metricId.Value);
        if (year.HasValue) query = query.Where(b => b.Year == year.Value);
        if (!string.IsNullOrWhiteSpace(period)) query = query.Where(b => b.Period == period);
        if (status.HasValue) query = query.Where(b => b.Status == status.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(b => b.ImportDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new ImportBatchListItemDto
            {
                Id = b.Id,
                MetricId = b.MetricId,
                MetricName = b.Metric!.Name,
                FileName = b.FileName,
                Year = b.Year,
                Period = b.Period,
                ImportDate = b.ImportDate,
                Status = b.Status,
                TotalRows = b.TotalRows,
                ValidRows = b.ValidRows,
                InvalidRows = b.InvalidRows
            })
            .ToListAsync();

        return new PagedResultDto<ImportBatchListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ImportResultDto?> GetByIdAsync(int id)
    {
        var batch = await db.ImportBatches
            .AsNoTracking()
            .Include(b => b.Metric)
            .Include(b => b.MetricFileDefinition)
            .Include(b => b.Rows)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (batch is null) return null;

        var rowErrors = batch.Rows
            .Where(r => !r.IsValid && r.ValidationErrors is not null)
            .OrderBy(r => r.RowNumber)
            .Select(r => new RowValidationErrorDto
            {
                RowNumber = r.RowNumber,
                Errors = System.Text.Json.JsonSerializer.Deserialize<List<string>>(r.ValidationErrors!) ?? new()
            })
            .ToList();

        return new ImportResultDto
        {
            Batch = new ImportBatchDetailDto
            {
                Id = batch.Id,
                MetricId = batch.MetricId,
                MetricName = batch.Metric!.Name,
                FileName = batch.FileName,
                Year = batch.Year,
                Period = batch.Period,
                ImportDate = batch.ImportDate,
                Status = batch.Status,
                TotalRows = batch.TotalRows,
                ValidRows = batch.ValidRows,
                InvalidRows = batch.InvalidRows,
                MetricFileDefinitionId = batch.MetricFileDefinitionId,
                FileDefinitionVersion = batch.MetricFileDefinition!.VersionNumber,
                ErrorSummary = batch.ErrorSummary
            },
            RowErrors = rowErrors
        };
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        var batch = await db.ImportBatches.FirstOrDefaultAsync(b => b.Id == id);
        if (batch is null) return (false, "רשומת הקליטה לא נמצאה.");

        db.ImportBatches.Remove(batch);
        await db.SaveChangesAsync();
        return (true, null);
    }
}
