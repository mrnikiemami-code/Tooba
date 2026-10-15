using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Persistence;
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Domain.Aggregates;
using Tooba.Shipping.Domain.Scheduling;

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
/// <b>NOT WIRED.</b> این DbContext به Host وصل نمی‌شود (هیچ ProjectReference از Host، هیچ ثبت DI،
/// هیچ endpoint) و مهاجرت آن در هیچ دیتابیس واقعی اجرا نمی‌شود؛ هیچ رفتار runtime تغییر نمی‌کند.
/// </para>
/// <para>
/// <b>Invariants at the storage boundary.</b> قواعدی که aggregate در حافظه تضمین می‌کند و از دست
/// رفتن‌شان به دادهٔ نامعتبر در دیتابیس منجر می‌شود، به‌صورت CHECK/UNIQUE در همین schema تکرار
/// شده‌اند (نام‌گذاری <c>ck_</c>/<c>ux_</c>). مرز مشترک (service_code، دامنهٔ مقصد) با
/// <c>NULLS NOT DISTINCT</c> یکتا می‌شود تا ردیف‌های هم‌دامنه با ستون تهی نتوانند تکراری ثبت شوند؛
/// این نیازمند PostgreSQL 15+ است (محیط پروژه PostgreSQL 17 است) و در نبود آن مهاجرت با خطای صریح
/// متوقف می‌شود، نه با یکتایی ناقص.
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

    /// <summary>برنامه‌های عملیاتی هفتگی ارسال (پنجره‌های محلی تحویل به حامل).</summary>
    public DbSet<ShippingOperatingSchedule> OperatingSchedules => Set<ShippingOperatingSchedule>();

    /// <summary>سیاست‌های هزینهٔ بسته‌بندی، جدا از سیاست نرخ حمل.</summary>
    public DbSet<ShippingPackagingPolicy> PackagingPolicies => Set<ShippingPackagingPolicy>();

    /// <summary>Outbox همین ماژول؛ پیروی از اسکلت متعارف DbContext ماژول‌ها، بدون هیچ مصرف فعلی.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<ShippingRatePolicy>(entity =>
        {
            entity.ToTable("rate_policies", table =>
            {
                // The amount/currency/day rules below are the storage mirror of the aggregate's
                // Normalize* guards: a row that the domain would refuse can never be persisted by any
                // other writer (raw SQL, an import, or a future sibling handler).
                table.HasCheckConstraint(
                    "ck_rate_policies_charge_amount_non_negative",
                    "charge_amount >= 0");
                table.HasCheckConstraint(
                    "ck_rate_policies_charge_amount_within_max",
                    $"charge_amount <= {ShippingRatePolicy.ChargeAmountMaxValue.ToString(CultureInfo.InvariantCulture)}");
                table.HasCheckConstraint(
                    "ck_rate_policies_free_above_threshold_non_negative",
                    "free_above_threshold IS NULL OR free_above_threshold >= 0");
                table.HasCheckConstraint(
                    "ck_rate_policies_preparation_days_range",
                    $"preparation_days >= 0 AND preparation_days <= {ShippingRatePolicy.DaysMaxValue.ToString(CultureInfo.InvariantCulture)}");
                table.HasCheckConstraint(
                    "ck_rate_policies_lead_days_range",
                    $"lead_days >= 0 AND lead_days <= {ShippingRatePolicy.DaysMaxValue.ToString(CultureInfo.InvariantCulture)}");
                // The engine matches codes with ordinal-ignore-case against a trimmed request code, so a
                // padded or non-3-letter code would silently never match: both are refused here.
                table.HasCheckConstraint(
                    "ck_rate_policies_service_code_canonical",
                    "char_length(service_code) > 0 AND service_code = btrim(service_code)");
                // NULL option_code is the service-level policy and stays legal; a present option code must be
                // canonical for the same reason as the service code, or it would never match a request.
                table.HasCheckConstraint(
                    "ck_rate_policies_option_code_canonical",
                    "option_code IS NULL OR (char_length(option_code) > 0 AND option_code = btrim(option_code))");
                table.HasCheckConstraint(
                    "ck_rate_policies_currency_code_canonical",
                    "char_length(currency_code) = 3 AND currency_code = upper(currency_code)");
                // A service area whose owning city is absent is a half-scope that could match the wrong
                // destination; the aggregate rejects it and so does the table.
                table.HasCheckConstraint(
                    "ck_rate_policies_scope_coherent",
                    "service_area_id IS NULL OR city_id IS NOT NULL");
                // Measurement tiers are a pair: a present lower bound is what makes the axis conditioned,
                // and the upper bound must be strictly greater (half-open [min, max)). A reversed or
                // zero-width tier can never be stored, and a NULL lower bound with a non-NULL upper bound
                // (a half-written axis the aggregate would never produce) is refused too.
                table.HasCheckConstraint(
                    "ck_rate_policies_weight_tier_coherent",
                    $"min_weight_grams IS NULL OR (min_weight_grams >= 0 AND min_weight_grams <= {ShippingRatePolicy.WeightGramsMaxValue.ToString(CultureInfo.InvariantCulture)} AND (max_weight_grams IS NULL OR (max_weight_grams > min_weight_grams AND max_weight_grams <= {ShippingRatePolicy.WeightGramsMaxValue.ToString(CultureInfo.InvariantCulture)})))");
                table.HasCheckConstraint(
                    "ck_rate_policies_count_tier_coherent",
                    $"min_item_count IS NULL OR (min_item_count >= 0 AND min_item_count <= {ShippingRatePolicy.ItemCountMaxValue.ToString(CultureInfo.InvariantCulture)} AND (max_item_count IS NULL OR (max_item_count > min_item_count AND max_item_count <= {ShippingRatePolicy.ItemCountMaxValue.ToString(CultureInfo.InvariantCulture)})))");
                table.HasCheckConstraint(
                    "ck_rate_policies_tier_bounds_paired",
                    "(max_weight_grams IS NULL OR min_weight_grams IS NOT NULL) AND (max_item_count IS NULL OR min_item_count IS NOT NULL)");
            });
            entity.HasKey(x => x.RatePolicyId);
            entity.Property(x => x.RatePolicyId).ValueGeneratedNever();
            entity.Property(x => x.ServiceCode)
                .HasMaxLength(ShippingRatePolicy.ServiceCodeMaxLength)
                .IsRequired();
            entity.Property(x => x.OptionCode)
                .HasMaxLength(ShippingRatePolicy.OptionCodeMaxLength);
            entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            entity.Property(x => x.ChargeAmount).HasPrecision(18, 2);
            entity.Property(x => x.FreeAboveThreshold).HasPrecision(18, 2);
            entity.Property(x => x.MinWeightGrams);
            entity.Property(x => x.MaxWeightGrams);
            entity.Property(x => x.MinItemCount);
            entity.Property(x => x.MaxItemCount);

            // One commercial policy per (service, option, scope, tier). Scope is the four-level destination
            // tuple and the tier is the half-open measurement range, so a country-level unconditioned row is
            // distinguished from a city-level weight tier for the same service. The column order
            // intentionally mirrors the specificity order used by the engine (option identity first, then
            // destination specificity, then the measurement tier).
            //
            // NULLS NOT DISTINCT is required for this to be a real invariant: with PostgreSQL's default
            // NULLS DISTINCT, two rows that differ only by a NULL level (e.g. two country-scope policies
            // for the same service, or two option-less tipax rows) would both be accepted, and the engine
            // would then answer ScopeConflict for every such request. Requires PostgreSQL 15+.
            entity.HasIndex(x => new
                {
                    x.ServiceCode,
                    x.OptionCode,
                    x.CountryId,
                    x.AdministrativeAreaId,
                    x.CityId,
                    x.ServiceAreaId,
                    x.MinWeightGrams,
                    x.MaxWeightGrams,
                    x.MinItemCount,
                    x.MaxItemCount,
                })
                .IsUnique()
                .AreNullsDistinct(false)
                .HasDatabaseName("ux_rate_policies_service_option_scope");

            // Every quote filters by service code + active flag before evaluating scope.
            entity.HasIndex(x => new { x.ServiceCode, x.IsActive })
                .HasDatabaseName("ix_rate_policies_service_active");
        });

        modelBuilder.Entity<ShippingCoverageGrant>(entity =>
        {
            entity.ToTable("coverage_grants", table =>
            {
                table.HasCheckConstraint(
                    "ck_coverage_grants_service_code_canonical",
                    "char_length(service_code) > 0 AND service_code = btrim(service_code)");
                table.HasCheckConstraint(
                    "ck_coverage_grants_scope_coherent",
                    "service_area_id IS NULL OR city_id IS NOT NULL");
            });
            entity.HasKey(x => x.CoverageGrantId);
            entity.Property(x => x.CoverageGrantId).ValueGeneratedNever();
            entity.Property(x => x.ServiceCode).HasMaxLength(ShippingRatePolicy.ServiceCodeMaxLength).IsRequired();

            // Same rule as the rate policy: coverage is granted at most once per service and exact scope,
            // including scopes whose deeper levels are NULL. Requires PostgreSQL 15+.
            entity.HasIndex(x => new { x.ServiceCode, x.CountryId, x.AdministrativeAreaId, x.CityId, x.ServiceAreaId })
                .IsUnique()
                .AreNullsDistinct(false)
                .HasDatabaseName("ux_coverage_grants_service_scope");

            // Coverage rows are sought by service + country on every quote; this index keeps that lookup
            // narrow without introducing any cross-schema reference.
            entity.HasIndex(x => new { x.ServiceCode, x.CountryId, x.IsActive })
                .HasDatabaseName("ix_coverage_grants_service_country_active");
        });

        modelBuilder.Entity<ShippingPackagingPolicy>(entity =>
        {
            entity.ToTable("packaging_policies", table =>
            {
                // The storage mirror of the aggregate guards: a row the domain refuses can never be
                // persisted by raw SQL, an import, or a future sibling handler.
                table.HasCheckConstraint(
                    "ck_packaging_policies_fee_amount_non_negative",
                    "fee_amount >= 0");
                table.HasCheckConstraint(
                    "ck_packaging_policies_fee_amount_within_max",
                    $"fee_amount <= {ShippingPackagingPolicy.FeeAmountMaxValue.ToString(CultureInfo.InvariantCulture)}");
                table.HasCheckConstraint(
                    "ck_packaging_policies_service_code_canonical",
                    "char_length(service_code) > 0 AND service_code = btrim(service_code)");
                table.HasCheckConstraint(
                    "ck_packaging_policies_option_code_canonical",
                    "option_code IS NULL OR (char_length(option_code) > 0 AND option_code = btrim(option_code))");
                table.HasCheckConstraint(
                    "ck_packaging_policies_currency_code_canonical",
                    "char_length(currency_code) = 3 AND currency_code = upper(currency_code)");
                table.HasCheckConstraint(
                    "ck_packaging_policies_scope_coherent",
                    "service_area_id IS NULL OR city_id IS NOT NULL");
                // The payer mode is a stable code, never free-form prose: the two documented values are
                // the only ones a writer may persist, so no row can exist with no responsible party.
                table.HasCheckConstraint(
                    "ck_packaging_policies_freight_payer_mode_known",
                    $"freight_payer_mode IN ({(int)ShippingFreightPayerMode.Prepaid}, {(int)ShippingFreightPayerMode.CollectOnDelivery})");
            });
            entity.HasKey(x => x.PackagingPolicyId);
            entity.Property(x => x.PackagingPolicyId).ValueGeneratedNever();
            entity.Property(x => x.ServiceCode)
                .HasMaxLength(ShippingRatePolicy.ServiceCodeMaxLength)
                .IsRequired();
            entity.Property(x => x.OptionCode)
                .HasMaxLength(ShippingRatePolicy.OptionCodeMaxLength);
            entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            entity.Property(x => x.FeeAmount).HasPrecision(18, 2);
            entity.Property(x => x.FreightPayerMode);

            // One packaging policy per (service, option, exact scope). Scope is the four-level destination
            // tuple, so a country-level policy is distinguished from a city-level one for the same service.
            // NULLS NOT DISTINCT is required for this to be a real invariant: with PostgreSQL's default
            // NULLS DISTINCT, two option-less rows for the same service would both be accepted and the
            // resolver would then answer Ambiguous for every request. Requires PostgreSQL 15+.
            entity.HasIndex(x => new
                {
                    x.ServiceCode,
                    x.OptionCode,
                    x.CountryId,
                    x.AdministrativeAreaId,
                    x.CityId,
                    x.ServiceAreaId,
                })
                .IsUnique()
                .AreNullsDistinct(false)
                .HasDatabaseName("ux_packaging_policies_service_option_scope");

            // Every packaging resolve filters by service code + active flag before evaluating scope.
            entity.HasIndex(x => new { x.ServiceCode, x.IsActive })
                .HasDatabaseName("ix_packaging_policies_service_active");
        });

        modelBuilder.Entity<ShippingOperatingSchedule>(entity =>
        {
            entity.ToTable("operating_schedules", table =>
            {
                // The storage mirror of the aggregate guards: a row the domain refuses can never be
                // persisted by raw SQL, an import, or a future sibling handler.
                table.HasCheckConstraint(
                    "ck_operating_schedules_service_code_canonical",
                    "char_length(service_code) > 0 AND service_code = btrim(service_code)");
                table.HasCheckConstraint(
                    "ck_operating_schedules_option_code_canonical",
                    "option_code IS NULL OR (char_length(option_code) > 0 AND option_code = btrim(option_code))");
                table.HasCheckConstraint(
                    "ck_operating_schedules_timezone_canonical",
                    "char_length(time_zone_id) > 0 AND time_zone_id = btrim(time_zone_id)");
            });
            entity.HasKey(x => x.ScheduleId);
            entity.Property(x => x.ScheduleId).ValueGeneratedNever();
            entity.Property(x => x.ServiceCode)
                .HasMaxLength(ShippingRatePolicy.ServiceCodeMaxLength)
                .IsRequired();
            entity.Property(x => x.OptionCode)
                .HasMaxLength(ShippingRatePolicy.OptionCodeMaxLength);
            entity.Property(x => x.TimeZoneId)
                .HasMaxLength(ShippingRatePolicyLimits.TimeZoneIdMaxLength)
                .IsRequired();

            // One operating policy per (service, option). Option is a real identity, so a service-level
            // schedule and an option schedule are distinct rows; NULLS NOT DISTINCT stops a second
            // option-less row for the same service, which would make "which window applies" ambiguous.
            // Requires PostgreSQL 15+.
            entity.HasIndex(x => new { x.ServiceCode, x.OptionCode })
                .IsUnique()
                .AreNullsDistinct(false)
                .HasDatabaseName("ux_operating_schedules_service_option");

            entity.HasMany(x => x.Windows)
                .WithOne()
                .HasForeignKey("schedule_id")
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Windows).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ShippingOperatingWindow>(entity =>
        {
            entity.ToTable("operating_windows", table =>
            {
                table.HasCheckConstraint(
                    "ck_operating_windows_weekday_iso_range",
                    $"weekday >= {ShippingOperatingWindow.MinWeekday.ToString(CultureInfo.InvariantCulture)} AND weekday <= {ShippingOperatingWindow.MaxWeekday.ToString(CultureInfo.InvariantCulture)}");
                // [start, end) semantics: end must be strictly after start. This single predicate refuses
                // reversed, zero-length AND overnight windows, exactly like the aggregate.
                table.HasCheckConstraint(
                    "ck_operating_windows_range_half_open",
                    "end_time > start_time");
                table.HasCheckConstraint(
                    "ck_operating_windows_capacity_positive",
                    $"capacity IS NULL OR (capacity > 0 AND capacity <= {ShippingOperatingWindow.CapacityMaxValue.ToString(CultureInfo.InvariantCulture)})");
            });
            entity.HasKey(x => x.WindowId);
            entity.Property(x => x.WindowId).ValueGeneratedNever();
            entity.Property<Guid>("schedule_id").IsRequired();

            // Windows are read per schedule and per weekday; this index keeps that lookup narrow.
            entity.HasIndex("schedule_id", nameof(ShippingOperatingWindow.Weekday))
                .HasDatabaseName("ix_operating_windows_schedule_weekday");
        });

        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>
/// کارخانهٔ design-time مهاجرت Shipping. اتصال فقط از متغیر محیطی ابزار EF خوانده می‌شود؛ هیچ
/// رشتهٔ اتصال runtime یا credential در این پروژه وجود ندارد.
/// </summary>
/// <remarks>
/// وجود این کارخانه صرفاً امکان تولید مهاجرت را فراهم می‌کند؛ ثبت migrator در Host همچنان انجام
/// نشده و هیچ DDL در startup اجرا نمی‌شود.
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
