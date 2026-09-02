using Microsoft.EntityFrameworkCore;
using server.Models.Entities;

namespace server.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PremiumMethod> PremiumMethods => Set<PremiumMethod>();
    public DbSet<Metric> Metrics => Set<Metric>();
    public DbSet<MetricFileDefinition> MetricFileDefinitions => Set<MetricFileDefinition>();
    public DbSet<MetricFieldMapping> MetricFieldMappings => Set<MetricFieldMapping>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<ImportRow> ImportRows => Set<ImportRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PremiumMethod>(entity =>
        {
            entity.Property(x => x.PremiumPercentage).HasColumnType("decimal(9,4)");
            entity.HasIndex(x => x.MethodNumber).IsUnique();
        });

        modelBuilder.Entity<Metric>(entity =>
        {
            entity.HasOne(x => x.PremiumMethod)
                .WithMany(x => x.Metrics)
                .HasForeignKey(x => x.PremiumMethodId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MetricFileDefinition>(entity =>
        {
            entity.HasOne(x => x.Metric)
                .WithMany(x => x.FileDefinitions)
                .HasForeignKey(x => x.MetricId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new { x.MetricId, x.VersionNumber }).IsUnique();
        });

        modelBuilder.Entity<MetricFieldMapping>(entity =>
        {
            entity.HasOne(x => x.MetricFileDefinition)
                .WithMany(x => x.FieldMappings)
                .HasForeignKey(x => x.MetricFileDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ImportBatch>(entity =>
        {
            // Metric -> ImportBatch and Metric -> MetricFileDefinition -> ImportBatch would otherwise be
            // two cascade paths into the same table, which the provider rejects; both FKs are Restrict.
            entity.HasOne(x => x.Metric)
                .WithMany(x => x.ImportBatches)
                .HasForeignKey(x => x.MetricId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.MetricFileDefinition)
                .WithMany()
                .HasForeignKey(x => x.MetricFileDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ImportRow>(entity =>
        {
            entity.HasOne(x => x.ImportBatch)
                .WithMany(x => x.Rows)
                .HasForeignKey(x => x.ImportBatchId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
