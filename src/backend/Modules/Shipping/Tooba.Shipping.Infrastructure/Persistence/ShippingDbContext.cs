using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Persistence;
using Tooba.Shipping.Domain.Aggregates;

namespace Tooba.Shipping.Infrastructure.Persistence;

/// <summary>
/// DbContext مالک schema مستقل <c>shipping</c>: فقط سیاست تجاری نرخ و مجوز پوشش.
/// </summary>
/// <remarks>
/// <para>
/// <b>Dependency direction.</b> این پروژه هیچ DbContext، جدول یا aggregate ماژول دیگری را
/// نمی‌شناسد. هویت مقصد و کد خدمت از طریق Contracts می‌آید: شناسه‌های پایدار Geography و کد خدمت
/// Fulfillment، هر دو فقط به‌عنوان مقدار کلید. هیچ join یا FK میان‌ماژولی در این مدل وجود ندارد.
/// </para>
/// <para>
/// <b>NOT WIRED.</b> این DbContext در این Task به Host وصل نمی‌شود (هیچ ProjectReference از Host،
/// هیچ ثبت DI، هیچ endpoint) و <em>هیچ مهاجرتی تولید یا اجرا نشده است</em>؛ بنابراین هیچ DDL و
/// هیچ رفتار runtime تغییر نمی‌کند. نگاشت زیر «آمادهٔ بازبینی» است، نه فعال.
/// </para>
/// </remarks>
public sealed class ShippingDbContext : DbContext
{
    /// <summary>schema اختصاصی Shipping.</summary>
    public const string Schema = "shipping";

    /// <summary>DbContext را با گزینه‌های میزبان می‌سازد.</summary>
    /// <param name="options">گزینه‌های EF.</param>
    public ShippingDbContext(DbContextOptions<ShippingDbContext> options) : base(options)
    {
    }

    /// <summary>سیاست‌های نرخ ارسال.</summary>
    public DbSet<ShippingRatePolicy> RatePolicies => Set<ShippingRatePolicy>();

    /// <summary>مجوزهای پوشش ارسال.</summary>
    public DbSet<ShippingCoverageGrant> CoverageGrants => Set<ShippingCoverageGrant>();

    /// <summary>Outbox همین ماژول؛ پیروی از اسکلت متعارف DbContext ماژول‌ها، بدون هیچ مصرف فعلی.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<ShippingRatePolicy>(entity =>
        {
            entity.ToTable("rate_policies");
            entity.HasKey(x => x.RatePolicyId);
            entity.Property(x => x.RatePolicyId).ValueGeneratedNever();
            entity.Property(x => x.ServiceCode)
                .HasMaxLength(ShippingRatePolicy.ServiceCodeMaxLength)
                .IsRequired();
            entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            entity.Property(x => x.ChargeAmount).HasPrecision(18, 2);
            entity.Property(x => x.FreeAboveThreshold).HasPrecision(18, 2);

            // One commercial policy per (service, scope). Scope is the four-level destination tuple, so a
            // country-level row is distinguished from a city-level row for the same service. The column
            // order intentionally mirrors the specificity order used by the engine.
            entity.HasIndex(x => new { x.ServiceCode, x.CountryId, x.AdministrativeAreaId, x.CityId, x.ServiceAreaId })
                .IsUnique()
                .HasDatabaseName("ux_rate_policies_service_scope");
        });

        modelBuilder.Entity<ShippingCoverageGrant>(entity =>
        {
            entity.ToTable("coverage_grants");
            entity.HasKey(x => x.CoverageGrantId);
            entity.Property(x => x.CoverageGrantId).ValueGeneratedNever();
            entity.Property(x => x.ServiceCode).HasMaxLength(ShippingRatePolicy.ServiceCodeMaxLength).IsRequired();

            entity.HasIndex(x => new { x.ServiceCode, x.CountryId, x.AdministrativeAreaId, x.CityId, x.ServiceAreaId })
                .IsUnique()
                .HasDatabaseName("ux_coverage_grants_service_scope");

            // Coverage rows are sought by service + country on every quote; this index keeps that lookup
            // narrow without introducing any cross-schema reference.
            entity.HasIndex(x => new { x.ServiceCode, x.CountryId, x.IsActive })
                .HasDatabaseName("ix_coverage_grants_service_country_active");
        });

        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>
/// کارخانهٔ design-time مهاجرت Shipping. اتصال فقط از متغیر محیطی ابزار EF خوانده می‌شود؛ هیچ
/// رشتهٔ اتصال runtime یا credential در این پروژه وجود ندارد.
/// </summary>
/// <remarks>
/// وجود این کارخانه صرفاً امکان تولید مهاجرت در یک Task مجاز آینده را فراهم می‌کند؛ در این Task
/// هیچ مهاجرتی ساخته یا اجرا نشده است.
/// </remarks>
public sealed class ShippingDbContextFactory : IDesignTimeDbContextFactory<ShippingDbContext>
{
    /// <inheritdoc />
    public ShippingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ShippingDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            ToobaNpgsql.DesignTimeConnectionString(),
            ShippingDbContext.Schema,
            typeof(ShippingDbContext));
        return new ShippingDbContext(options.Options);
    }
}
