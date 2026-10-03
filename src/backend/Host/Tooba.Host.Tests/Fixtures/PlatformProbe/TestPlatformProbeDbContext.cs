using Microsoft.EntityFrameworkCore;
using NodaTime;
using Tooba.BuildingBlocks;
using Tooba.Persistence;

namespace Tooba.Host.Tests.Fixtures.PlatformProbe;

/// <summary>ردیف نمونهٔ تست در schema <c>platform_probe</c>.</summary>
public sealed class TestPlatformProbeRecord : IHasDomainEvents
{
    private readonly DomainEventCollector _domainEvents = new();

    public Guid Id { get; set; }

    public Instant CreatedAt { get; set; }

    public Guid? ExternalReference { get; set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.Events;

    public void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}

/// <summary>DbContext تست‌محور با schema دقیق <c>platform_probe</c>؛ بدون وابستگی به production PlatformProbe.</summary>
public sealed class TestPlatformProbeDbContext : DbContext
{
    public const string Schema = "platform_probe";

    public TestPlatformProbeDbContext(DbContextOptions<TestPlatformProbeDbContext> options)
        : base(options)
    {
    }

    public DbSet<TestPlatformProbeRecord> Records => Set<TestPlatformProbeRecord>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<TestPlatformProbeRecord>(entity =>
        {
            entity.ToTable("probe_records");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.CreatedAt).MapAsTimestamp();
            entity.Property(x => x.ExternalReference);
            entity.Ignore(x => x.DomainEvents);
        });
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>سازندهٔ ردیف تست با UUID v7 و رویداد دامنه.</summary>
public static class TestPlatformProbePersistence
{
    public static TestPlatformProbeRecord NewRecord(Guid? externalReference = null)
    {
        var record = new TestPlatformProbeRecord
        {
            Id = UuidV7.New(),
            CreatedAt = SystemClock.Instance.GetCurrentInstant(),
            ExternalReference = externalReference,
        };
        record.Raise(new ProbeRecordCreatedDomainEvent(record.Id));
        return record;
    }
}
