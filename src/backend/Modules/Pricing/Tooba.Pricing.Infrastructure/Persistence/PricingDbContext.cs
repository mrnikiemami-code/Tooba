using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Pricing.Domain;
using Tooba.Persistence;

namespace Tooba.Pricing.Infrastructure.Persistence;

/// <summary>
/// DbContext مالک schema <c>pricing</c>. قیمت را روی Product یا Offer نگه نمی‌دارد.
/// </summary>
public sealed class PricingDbContext : DbContext
{
    /// <summary>
    /// schema اختصاصی Pricing.
    /// </summary>
    public const string Schema = "pricing";

    /// <summary>
    /// DbContext را با گزینه‌های Host می‌سازد.
    /// </summary>
    public PricingDbContext(DbContextOptions<PricingDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// قیمت‌های نوشته‌شده.
    /// </summary>
    public DbSet<AuthoredPrice> Prices => Set<AuthoredPrice>();

    /// <summary>
    /// Outbox همین ماژول.
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<AuthoredPrice>(entity =>
        {
            entity.ToTable("prices");
            entity.HasKey(x => x.PriceId);
            entity.Property(x => x.PriceId).ValueGeneratedNever();
            entity.Property(x => x.Market).HasMaxLength(16);
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.Property(x => x.Amount).HasPrecision(19, 4);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Channel).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.QualifierKind).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.QualifierKey).HasMaxLength(64);
            entity.Ignore(x => x.DomainEvents);
            entity.HasIndex(x => new { x.OfferId, x.Market, x.Channel, x.Currency, x.QualifierKind, x.ValidFrom });
        });
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>
/// کارخانهٔ design-time مهاجرت Pricing.
/// </summary>
public sealed class PricingDbContextFactory : IDesignTimeDbContextFactory<PricingDbContext>
{
    /// <inheritdoc />
    public PricingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PricingDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            ToobaNpgsql.DesignTimeConnectionString(),
            PricingDbContext.Schema,
            typeof(PricingDbContext));
        return new PricingDbContext(options.Options);
    }
}
