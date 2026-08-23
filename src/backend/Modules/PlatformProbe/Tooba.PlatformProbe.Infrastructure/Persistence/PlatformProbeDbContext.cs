using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NodaTime;
using Tooba.BuildingBlocks;
using Tooba.Persistence;

namespace Tooba.PlatformProbe.Infrastructure.Persistence;

public sealed class PlatformProbeRecord
{
    public Guid Id { get; set; }
    public Instant CreatedAt { get; set; }
    public Guid? ExternalReference { get; set; }
}

public sealed class PlatformProbeDbContext : DbContext
{
    public const string Schema = "platform_probe";

    public PlatformProbeDbContext(DbContextOptions<PlatformProbeDbContext> options)
        : base(options)
    {
    }

    public DbSet<PlatformProbeRecord> Records => Set<PlatformProbeRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<PlatformProbeRecord>(entity =>
        {
            entity.ToTable("probe_records");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.ExternalReference);
        });
    }
}

public sealed class PlatformProbeDbContextFactory : IDesignTimeDbContextFactory<PlatformProbeDbContext>
{
    public PlatformProbeDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PlatformProbeDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            ToobaNpgsql.DesignTimeConnectionString(),
            PlatformProbeDbContext.Schema,
            typeof(PlatformProbeDbContext));
        return new PlatformProbeDbContext(options.Options);
    }
}

public static class PlatformProbePersistence
{
    public static PlatformProbeRecord NewRecord(Guid? externalReference = null) => new()
    {
        Id = UuidV7.New(),
        CreatedAt = SystemClock.Instance.GetCurrentInstant(),
        ExternalReference = externalReference,
    };
}
