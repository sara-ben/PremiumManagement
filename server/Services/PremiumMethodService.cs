using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models.Dtos;
using server.Models.Entities;
using server.Models.Enums;

namespace server.Services;

public class PremiumMethodService(AppDbContext db)
{
    public async Task<PagedResultDto<PremiumMethodListItemDto>> GetListAsync(
        string? search, CalculationPeriod? calculationPeriod, int page, int pageSize)
    {
        var query = db.PremiumMethods.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(m =>
                m.MethodNumber.Contains(search) || m.Description.Contains(search));
        }

        if (calculationPeriod.HasValue)
        {
            query = query.Where(m => m.CalculationPeriod == calculationPeriod.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(m => m.MethodNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new PremiumMethodListItemDto
            {
                Id = m.Id,
                MethodNumber = m.MethodNumber,
                Description = m.Description,
                PremiumPercentage = m.PremiumPercentage,
                CalculationPeriod = m.CalculationPeriod,
                IsActive = m.IsActive,
                MetricsCount = m.Metrics.Count
            })
            .ToListAsync();

        return new PagedResultDto<PremiumMethodListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PremiumMethodDetailDto?> GetByIdAsync(int id)
    {
        var entity = await db.PremiumMethods
            .AsNoTracking()
            .Include(m => m.Metrics)
            .ThenInclude(metric => metric.FileDefinitions)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (entity is null) return null;

        return new PremiumMethodDetailDto
        {
            Id = entity.Id,
            MethodNumber = entity.MethodNumber,
            Description = entity.Description,
            PremiumPercentage = entity.PremiumPercentage,
            CalculationPeriod = entity.CalculationPeriod,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            MetricsCount = entity.Metrics.Count,
            Metrics = entity.Metrics.Select(metric => new MetricListItemDto
            {
                Id = metric.Id,
                PremiumMethodId = metric.PremiumMethodId,
                Name = metric.Name,
                Description = metric.Description,
                SourceType = metric.SourceType,
                ImportFrequency = metric.ImportFrequency,
                IsActive = metric.IsActive,
                ActiveFileDefinitionVersion = metric.FileDefinitions
                    .Where(fd => fd.IsActive)
                    .Select(fd => (int?)fd.VersionNumber)
                    .FirstOrDefault()
            }).ToList()
        };
    }

    public async Task<(bool Success, string? Error, PremiumMethodDetailDto? Result)> CreateAsync(PremiumMethodCreateDto dto)
    {
        var exists = await db.PremiumMethods.AnyAsync(m => m.MethodNumber == dto.MethodNumber);
        if (exists)
        {
            return (false, $"שיטת פרמיה עם מספר '{dto.MethodNumber}' כבר קיימת.", null);
        }

        var entity = new PremiumMethod
        {
            MethodNumber = dto.MethodNumber,
            Description = dto.Description,
            PremiumPercentage = dto.PremiumPercentage,
            CalculationPeriod = dto.CalculationPeriod
        };

        db.PremiumMethods.Add(entity);
        await db.SaveChangesAsync();

        return (true, null, await GetByIdAsync(entity.Id));
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, PremiumMethodUpdateDto dto)
    {
        var entity = await db.PremiumMethods.FindAsync(id);
        if (entity is null) return (false, "שיטת הפרמיה לא נמצאה.");

        entity.Description = dto.Description;
        entity.PremiumPercentage = dto.PremiumPercentage;
        entity.CalculationPeriod = dto.CalculationPeriod;
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> SetActiveAsync(int id, bool isActive)
    {
        var entity = await db.PremiumMethods.FindAsync(id);
        if (entity is null) return (false, "שיטת הפרמיה לא נמצאה.");

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        var entity = await db.PremiumMethods
            .Include(m => m.Metrics)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (entity is null) return (false, "שיטת הפרמיה לא נמצאה.");

        if (entity.Metrics.Count > 0)
        {
            return (false, "לא ניתן למחוק שיטת פרמיה עם מדדים מקושרים. ניתן להקפיא אותה במקום.");
        }

        db.PremiumMethods.Remove(entity);
        await db.SaveChangesAsync();
        return (true, null);
    }
}
