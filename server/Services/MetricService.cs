using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models.Dtos;
using server.Models.Entities;

namespace server.Services;

public class MetricService(AppDbContext db)
{
    public async Task<List<MetricListItemDto>> GetListAsync(int? premiumMethodId)
    {
        var query = db.Metrics.AsNoTracking().Include(m => m.FileDefinitions).AsQueryable();

        if (premiumMethodId.HasValue)
        {
            query = query.Where(m => m.PremiumMethodId == premiumMethodId.Value);
        }

        return await query
            .OrderBy(m => m.Name)
            .Select(m => new MetricListItemDto
            {
                Id = m.Id,
                PremiumMethodId = m.PremiumMethodId,
                Name = m.Name,
                Description = m.Description,
                SourceType = m.SourceType,
                ImportFrequency = m.ImportFrequency,
                IsActive = m.IsActive,
                ActiveFileDefinitionVersion = m.FileDefinitions
                    .Where(fd => fd.IsActive)
                    .Select(fd => (int?)fd.VersionNumber)
                    .FirstOrDefault()
            })
            .ToListAsync();
    }

    public async Task<MetricDetailDto?> GetByIdAsync(int id)
    {
        var entity = await db.Metrics
            .AsNoTracking()
            .Include(m => m.FileDefinitions)
            .ThenInclude(fd => fd.FieldMappings)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (entity is null) return null;

        return MapDetail(entity);
    }

    private static MetricDetailDto MapDetail(Metric entity) => new()
    {
        Id = entity.Id,
        PremiumMethodId = entity.PremiumMethodId,
        Name = entity.Name,
        Description = entity.Description,
        SourceType = entity.SourceType,
        ImportFrequency = entity.ImportFrequency,
        IsActive = entity.IsActive,
        ActiveFileDefinitionVersion = entity.FileDefinitions
            .Where(fd => fd.IsActive)
            .Select(fd => (int?)fd.VersionNumber)
            .FirstOrDefault(),
        FileDefinitions = entity.FileDefinitions
            .OrderByDescending(fd => fd.VersionNumber)
            .Select(fd => new FileDefinitionDto
            {
                Id = fd.Id,
                MetricId = fd.MetricId,
                VersionNumber = fd.VersionNumber,
                SheetName = fd.SheetName,
                HeaderRowNumber = fd.HeaderRowNumber,
                IsActive = fd.IsActive,
                CreatedAt = fd.CreatedAt,
                FieldMappings = fd.FieldMappings
                    .OrderBy(fm => fm.DisplayOrder)
                    .Select(fm => new FieldMappingDto
                    {
                        Id = fm.Id,
                        ExcelColumnName = fm.ExcelColumnName,
                        SystemFieldName = fm.SystemFieldName,
                        DataType = fm.DataType,
                        IsRequired = fm.IsRequired,
                        ValidationRule = fm.ValidationRule,
                        DisplayOrder = fm.DisplayOrder
                    }).ToList()
            }).ToList()
    };

    public async Task<(bool Success, string? Error, MetricDetailDto? Result)> CreateAsync(MetricCreateDto dto)
    {
        var methodExists = await db.PremiumMethods.AnyAsync(m => m.Id == dto.PremiumMethodId);
        if (!methodExists)
        {
            return (false, "שיטת הפרמיה שנבחרה לא נמצאה.", null);
        }

        var entity = new Metric
        {
            PremiumMethodId = dto.PremiumMethodId,
            Name = dto.Name,
            Description = dto.Description,
            SourceType = dto.SourceType,
            ImportFrequency = dto.ImportFrequency
        };

        db.Metrics.Add(entity);
        await db.SaveChangesAsync();

        return (true, null, await GetByIdAsync(entity.Id));
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, MetricUpdateDto dto)
    {
        var entity = await db.Metrics.FindAsync(id);
        if (entity is null) return (false, "המדד לא נמצא.");

        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.SourceType = dto.SourceType;
        entity.ImportFrequency = dto.ImportFrequency;
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> SetActiveAsync(int id, bool isActive)
    {
        var entity = await db.Metrics.FindAsync(id);
        if (entity is null) return (false, "המדד לא נמצא.");

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        var entity = await db.Metrics.FirstOrDefaultAsync(m => m.Id == id);
        if (entity is null) return (false, "המדד לא נמצא.");

        var hasImports = await db.ImportBatches.AnyAsync(b => b.MetricId == id);
        if (hasImports)
        {
            return (false, "לא ניתן למחוק מדד עם היסטוריית קליטות. ניתן להקפיא אותו במקום.");
        }

        db.Metrics.Remove(entity);
        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error, FileDefinitionDto? Result)> CreateFileDefinitionAsync(
        int metricId, FileDefinitionCreateDto dto)
    {
        var metric = await db.Metrics.FirstOrDefaultAsync(m => m.Id == metricId);
        if (metric is null) return (false, "המדד לא נמצא.", null);

        var duplicateColumns = dto.FieldMappings
            .GroupBy(fm => fm.SystemFieldName)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateColumns.Count > 0)
        {
            return (false, $"שמות שדה כפולים במיפוי: {string.Join(", ", duplicateColumns)}", null);
        }

        var nextVersion = await db.MetricFileDefinitions
            .Where(fd => fd.MetricId == metricId)
            .Select(fd => (int?)fd.VersionNumber)
            .MaxAsync() ?? 0;
        nextVersion += 1;

        var entity = new MetricFileDefinition
        {
            MetricId = metricId,
            VersionNumber = nextVersion,
            SheetName = dto.SheetName,
            HeaderRowNumber = dto.HeaderRowNumber,
            IsActive = dto.SetAsActive,
            FieldMappings = dto.FieldMappings.Select(fm => new MetricFieldMapping
            {
                ExcelColumnName = fm.ExcelColumnName,
                SystemFieldName = fm.SystemFieldName,
                DataType = fm.DataType,
                IsRequired = fm.IsRequired,
                ValidationRule = fm.ValidationRule,
                DisplayOrder = fm.DisplayOrder
            }).ToList()
        };

        if (dto.SetAsActive)
        {
            var existingActive = await db.MetricFileDefinitions
                .Where(fd => fd.MetricId == metricId && fd.IsActive)
                .ToListAsync();
            foreach (var fd in existingActive) fd.IsActive = false;
        }

        db.MetricFileDefinitions.Add(entity);
        await db.SaveChangesAsync();

        var reloaded = await db.MetricFileDefinitions
            .AsNoTracking()
            .Include(fd => fd.FieldMappings)
            .FirstAsync(fd => fd.Id == entity.Id);

        return (true, null, new FileDefinitionDto
        {
            Id = reloaded.Id,
            MetricId = reloaded.MetricId,
            VersionNumber = reloaded.VersionNumber,
            SheetName = reloaded.SheetName,
            HeaderRowNumber = reloaded.HeaderRowNumber,
            IsActive = reloaded.IsActive,
            CreatedAt = reloaded.CreatedAt,
            FieldMappings = reloaded.FieldMappings.OrderBy(fm => fm.DisplayOrder).Select(fm => new FieldMappingDto
            {
                Id = fm.Id,
                ExcelColumnName = fm.ExcelColumnName,
                SystemFieldName = fm.SystemFieldName,
                DataType = fm.DataType,
                IsRequired = fm.IsRequired,
                ValidationRule = fm.ValidationRule,
                DisplayOrder = fm.DisplayOrder
            }).ToList()
        });
    }

    public async Task<(bool Success, string? Error)> SetActiveFileDefinitionAsync(int metricId, int fileDefinitionId)
    {
        var definitions = await db.MetricFileDefinitions
            .Where(fd => fd.MetricId == metricId)
            .ToListAsync();

        var target = definitions.FirstOrDefault(fd => fd.Id == fileDefinitionId);
        if (target is null) return (false, "גרסת מבנה הקובץ לא נמצאה.");

        foreach (var fd in definitions) fd.IsActive = fd.Id == fileDefinitionId;

        await db.SaveChangesAsync();
        return (true, null);
    }
}
