using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Persistence;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Domain.Aggregates;
using Tooba.Shipping.Domain.Authoring;
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
/// <b>CLI-only migration.</b> مهاجرت‌های این DbContext فقط توسط <c>Tooba.MigrationRunner</c> اعمال
/// می‌شوند؛ هیچ <c>IModuleSchemaMigrator</c> در این ماژول ثبت نشده و هیچ DDL در startup اجرا نمی‌شود.
/// DDL همیشه یک تغییر صریح و بازبینی‌شده است، نه اثر جانبی بالا آمدن Host.
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

    /// <summary>اعلام‌های حالت تحدید جغرافیایی هر روش ارسال.</summary>
    public DbSet<ShippingMethodCoverage> MethodCoverages => Set<ShippingMethodCoverage>();

    /// <summary>برنامه‌های عملیاتی هفتگی ارسال (پنجره‌های محلی تحویل به حامل).</summary>
    public DbSet<ShippingOperatingSchedule> OperatingSchedules => Set<ShippingOperatingSchedule>();

    /// <summary>سیاست‌های هزینهٔ بسته‌بندی، جدا از سیاست نرخ حمل.</summary>
    public DbSet<ShippingPackagingPolicy> PackagingPolicies => Set<ShippingPackagingPolicy>();

    /// <summary>پیش‌نویس‌های نویسندگی روش ارسال (Draft، بدون فعال‌سازی یا مصرف استعلام).</summary>
    public DbSet<ShippingAuthoringDraft> AuthoringDrafts => Set<ShippingAuthoringDraft>();

    /// <summary>capture‌های تغییرناپذیر بازبینی پیش‌نویس نویسندگی (staging، نه انتشار).</summary>
    public DbSet<ShippingAuthoringRevisionSnapshot> AuthoringRevisionSnapshots =>
        Set<ShippingAuthoringRevisionSnapshot>();

    /// <summary>اشاره‌گر نسخه‌دار انتخاب capture برای مالک + Binding (قصد انتشار، نه آمادگی).</summary>
    public DbSet<ShippingSelectedCapturedRevision> SelectedCapturedRevisions =>
        Set<ShippingSelectedCapturedRevision>();

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
                // A present option code must be canonical for the same reason as the service code: the engine
                // compares it ordinal-ignore-case against a trimmed request option, so a padded value would
                // silently never match. NULL keeps the documented legacy service-level meaning.
                table.HasCheckConstraint(
                    "ck_coverage_grants_option_code_canonical",
                    "option_code IS NULL OR (char_length(option_code) > 0 AND option_code = btrim(option_code))");
                table.HasCheckConstraint(
                    "ck_coverage_grants_scope_coherent",
                    "service_area_id IS NULL OR city_id IS NOT NULL");
            });
            entity.HasKey(x => x.CoverageGrantId);
            entity.Property(x => x.CoverageGrantId).ValueGeneratedNever();
            entity.Property(x => x.ServiceCode).HasMaxLength(ShippingRatePolicy.ServiceCodeMaxLength).IsRequired();
            entity.Property(x => x.OptionCode).HasMaxLength(ShippingRatePolicy.OptionCodeMaxLength);

            // Same rule as the rate policy: coverage is granted at most once per method (service + option)
            // and exact scope, including scopes whose deeper levels are NULL. An option-scoped grant and a
            // service-level grant for the same scope are distinct decisions, so option_code is part of the
            // key. Requires PostgreSQL 15+.
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
                .HasDatabaseName("ux_coverage_grants_service_option_scope");

            // Coverage rows are sought by service + country on every quote; this index keeps that lookup
            // narrow without introducing any cross-schema reference.
            entity.HasIndex(x => new { x.ServiceCode, x.CountryId, x.IsActive })
                .HasDatabaseName("ix_coverage_grants_service_country_active");
        });

        modelBuilder.Entity<ShippingMethodCoverage>(entity =>
        {
            entity.ToTable("method_coverages", table =>
            {
                table.HasCheckConstraint(
                    "ck_method_coverages_service_code_canonical",
                    "char_length(service_code) > 0 AND service_code = btrim(service_code)");
                table.HasCheckConstraint(
                    "ck_method_coverages_option_code_canonical",
                    "option_code IS NULL OR (char_length(option_code) > 0 AND option_code = btrim(option_code))");
                // The mode is a stable code, never free-form prose: only the two documented meanings may be
                // persisted, so no row can exist whose restriction intent is unreadable. A third value would
                // make "restricted or not" depend on the reader.
                table.HasCheckConstraint(
                    "ck_method_coverages_mode_known",
                    $"mode IN ({(int)ShippingCoverageMode.Unrestricted}, {(int)ShippingCoverageMode.Restricted})");
            });
            entity.HasKey(x => x.MethodCoverageId);
            entity.Property(x => x.MethodCoverageId).ValueGeneratedNever();
            entity.Property(x => x.ServiceCode)
                .HasMaxLength(ShippingRatePolicy.ServiceCodeMaxLength)
                .IsRequired();
            entity.Property(x => x.OptionCode).HasMaxLength(ShippingRatePolicy.OptionCodeMaxLength);
            entity.Property(x => x.Mode);

            // One declaration per method identity (service + option). NULLS NOT DISTINCT is required: two
            // option-less declarations for the same service would make "is this service restricted" depend on
            // row order. Requires PostgreSQL 15+.
            entity.HasIndex(x => new { x.ServiceCode, x.OptionCode })
                .IsUnique()
                .AreNullsDistinct(false)
                .HasDatabaseName("ux_method_coverages_service_option");

            entity.HasIndex(x => x.ServiceCode)
                .HasDatabaseName("ix_method_coverages_service");
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

        // D3 authoring draft: Shipping-owned, owner-scoped, explicit relational structure for the region
        // tree. Geography is stored ONCE per group (selected node identities only); future rate,
        // packaging and schedule rows will reference the stable group id instead of repeating it.
        modelBuilder.Entity<ShippingAuthoringDraft>(entity =>
        {
            entity.ToTable("authoring_drafts", table =>
            {
                // The persisted owner shape mirrors the TB-189 scope exactly: Store carries the real
                // tenant and no seller, Marketplace carries neither, Seller carries the real party id and
                // no tenant. An impossible combination can never be stored by any writer.
                table.HasCheckConstraint(
                    "ck_authoring_drafts_owner_coherent",
                    $"((owner_kind = {(int)ShippingDraftOwnerKind.Store} AND owner_tenant_id IS NOT NULL AND owner_seller_party_id IS NULL)"
                    + $" OR (owner_kind = {(int)ShippingDraftOwnerKind.Marketplace} AND owner_tenant_id IS NULL AND owner_seller_party_id IS NULL)"
                    + $" OR (owner_kind = {(int)ShippingDraftOwnerKind.Seller} AND owner_tenant_id IS NULL AND owner_seller_party_id IS NOT NULL))");
                table.HasCheckConstraint("ck_authoring_drafts_revision_positive", "revision > 0");
                table.HasCheckConstraint(
                    "ck_authoring_drafts_service_code_canonical",
                    "char_length(service_code) > 0 AND service_code = btrim(service_code)");
                table.HasCheckConstraint(
                    "ck_authoring_drafts_option_code_canonical",
                    "option_code IS NULL OR (char_length(option_code) > 0 AND option_code = btrim(option_code))");
                table.HasCheckConstraint(
                    "ck_authoring_drafts_tenant_not_blank",
                    "owner_tenant_id IS NULL OR char_length(owner_tenant_id) > 0");
                // TB-197 optional root-level type reference. NULL is the explicit UNCONFIGURED state; a
                // zero guid is never a legal reference (it is not "no selection"), so no writer can store
                // one. It is a plain value: no cross-schema FK, navigation or join exists.
                table.HasCheckConstraint(
                    "ck_authoring_drafts_shipping_type_nonempty",
                    "shipping_type_id IS NULL OR shipping_type_id <> '00000000-0000-0000-0000-000000000000'::uuid");
            });
            entity.HasKey(x => x.DraftId);
            entity.Property(x => x.DraftId).ValueGeneratedNever();
            entity.Property(x => x.OwnerKind).HasConversion<int>();
            entity.Property(x => x.OwnerTenantId).HasMaxLength(ShippingDraftOwner.TenantIdMaxLength);
            entity.Property(x => x.ServiceCode).HasMaxLength(ShippingAuthoringDraft.ServiceCodeMaxLength).IsRequired();
            entity.Property(x => x.OptionCode).HasMaxLength(ShippingAuthoringDraft.OptionCodeMaxLength);
            // Optional owner-local reference to the shared Fulfillment type definition. A value, never a
            // relationship: no foreign key, navigation or cross-schema read is created by this mapping.
            entity.Property(x => x.ShippingTypeId);
            // A real relational concurrency predicate: the UPDATE that advances the revision is only
            // applied while the stored revision still equals the value read for this request.
            entity.Property(x => x.Revision).IsConcurrencyToken();

            // Every list/read/update filters by the full owner scope; this index keeps that narrow.
            entity.HasIndex(x => new { x.OwnerKind, x.OwnerTenantId, x.OwnerSellerPartyId })
                .HasDatabaseName("ix_authoring_drafts_owner");

            // One draft per owner and canonical binding. NULLS NOT DISTINCT is required because a
            // service-level binding has no option code, and with PostgreSQL's default NULLS DISTINCT two
            // option-less drafts for the same owner would both be accepted. Requires PostgreSQL 15+.
            entity.HasIndex(x => new
                {
                    x.OwnerKind,
                    x.OwnerTenantId,
                    x.OwnerSellerPartyId,
                    x.ServiceCode,
                    x.OptionCode,
                })
                .IsUnique()
                .AreNullsDistinct(false)
                .HasDatabaseName("ux_authoring_drafts_owner_binding");

            entity.HasMany(x => x.RegionGroups)
                .WithOne()
                .HasForeignKey(group => group.DraftId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.RegionGroups).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ShippingAuthoringRegionGroup>(entity =>
        {
            entity.ToTable("authoring_region_groups", table =>
            {
                table.HasCheckConstraint("ck_authoring_region_groups_order_non_negative", "group_order >= 0");
                // The currency binding of the group's manual rate rows: either "never bound" (no rows
                // saved yet) or an explicit canonical three-letter code. A blank/lowercase value would
                // make stored amounts ambiguous, so it can never be persisted.
                table.HasCheckConstraint(
                    "ck_authoring_region_groups_currency_canonical",
                    "currency_code IS NULL OR (octet_length(currency_code) = 3 AND currency_code COLLATE \"C\" ~ '^[A-Z]{3}$')");
            });
            entity.HasKey(x => x.GroupId);
            entity.Property(x => x.GroupId).ValueGeneratedNever();
            entity.Property(x => x.Order).HasColumnName("group_order");
            entity.Property(x => x.DraftId).IsRequired();
            entity.Property(x => x.CurrencyCode).HasMaxLength(3);

            // The explicit order is part of the aggregate identity of the tree: two groups of the same
            // draft can never claim the same position.
            entity.HasIndex(x => new { x.DraftId, x.Order })
                .IsUnique()
                .HasDatabaseName("ux_authoring_region_groups_draft_order");

            entity.HasMany(x => x.Areas)
                .WithOne()
                .HasForeignKey(area => area.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Areas).UsePropertyAccessMode(PropertyAccessMode.Field);

            // Manual rate rows own the SAME stable group identity as their only geographic reference.
            // Deleting a group removes exactly its own dependent draft rates, never another owner's.
            entity.HasMany(x => x.RateRows)
                .WithOne()
                .HasForeignKey(row => row.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.RateRows).UsePropertyAccessMode(PropertyAccessMode.Field);

            // TB-192 optional owner-authored group titles. Removing the group deletes exactly its own
            // titles; a title never carries geography, rates or currency.
            entity.HasMany(x => x.Titles)
                .WithOne()
                .HasForeignKey(title => title.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Titles).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // TB-192 owner-local multilingual presentation for ONE draft. The language key is the real
        // authoritative Localization id, but it is NOT a cross-schema FK: no foreign navigation, no join
        // and no copied registry row. Text is plain Unicode and is never fabricated or transliterated.
        modelBuilder.Entity<ShippingAuthoringDraftTranslation>(entity =>
        {
            entity.ToTable("authoring_draft_translations", table =>
            {
                table.HasCheckConstraint(
                    "ck_authoring_draft_translations_name_length",
                    $"char_length(btrim(name)) > 0 AND char_length(name) <= {ShippingAuthoringPresentationRules.NameMaxLength}");
                table.HasCheckConstraint(
                    "ck_authoring_draft_translations_description_length",
                    $"description IS NULL OR char_length(description) <= {ShippingAuthoringPresentationRules.DescriptionMaxLength}");
            });
            entity.HasKey(x => new { x.DraftId, x.LanguageId });
            entity.Property(x => x.DraftId).IsRequired();
            entity.Property(x => x.Name).IsRequired().HasMaxLength(ShippingAuthoringPresentationRules.NameMaxLength);
            entity.Property(x => x.Description).HasMaxLength(ShippingAuthoringPresentationRules.DescriptionMaxLength);

            // ONE text per draft+language: a second row for the same language would make "the" name
            // depend on row order, so the primary key above is the real invariant.
            entity.HasOne<ShippingAuthoringDraft>()
                .WithMany(draft => draft.Translations)
                .HasForeignKey(translation => translation.DraftId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ShippingAuthoringRegionGroupTitle>(entity =>
        {
            entity.ToTable("authoring_group_titles", table =>
            {
                table.HasCheckConstraint(
                    "ck_authoring_group_titles_title_length",
                    $"char_length(btrim(title)) > 0 AND char_length(title) <= {ShippingAuthoringPresentationRules.TitleMaxLength}");
            });
            entity.HasKey(x => new { x.GroupId, x.LanguageId });
            entity.Property(x => x.GroupId).IsRequired();
            entity.Property(x => x.DraftId).IsRequired();
            entity.Property(x => x.Title).IsRequired().HasMaxLength(ShippingAuthoringPresentationRules.TitleMaxLength);

            // Denormalized draft id mirrors the selected-area rule: it makes the ownership scope
            // inspectable without a second FK, while the only cascade is the group's own.
            entity.HasOne<ShippingAuthoringRegionGroup>()
                .WithMany(group => group.Titles)
                .HasForeignKey(title => title.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TB-191 manual condition/cost/delivery rows. Every CHECK below is the storage mirror of a domain
        // guard, so a row the aggregate refuses can never be written by raw SQL, an import or a sibling
        // handler. Amount precision is the canonical numeric(18,2): PostgreSQL may never silently round a
        // threshold or fee into a different rule.
        modelBuilder.Entity<ShippingAuthoringRateRow>(entity =>
        {
            entity.ToTable("authoring_rate_rows", table =>
            {
                // Each axis is EITHER unconditioned (both bounds NULL) OR explicitly conditioned with a
                // present, bounded minimum. The second branch repeats "min IS NOT NULL" because without it a
                // min=NULL / max=positive row makes the whole predicate UNKNOWN, which PostgreSQL CHECK
                // accepts; the aggregate and transport reject that max-only shape, so storage must too.
                table.HasCheckConstraint(
                    "ck_authoring_rate_rows_weight_interval",
                    $"(min_weight_grams IS NULL AND max_weight_grams IS NULL) OR (min_weight_grams IS NOT NULL AND min_weight_grams >= 0 AND min_weight_grams <= {ShippingAuthoringRateRowRules.WeightGramsMaxValue.ToString(CultureInfo.InvariantCulture)} AND (max_weight_grams IS NULL OR (max_weight_grams > min_weight_grams AND max_weight_grams <= {ShippingAuthoringRateRowRules.WeightGramsMaxValue.ToString(CultureInfo.InvariantCulture)})))");
                table.HasCheckConstraint(
                    "ck_authoring_rate_rows_count_interval",
                    $"(min_item_count IS NULL AND max_item_count IS NULL) OR (min_item_count IS NOT NULL AND min_item_count >= 1 AND min_item_count <= {ShippingAuthoringRateRowRules.ItemCountMaxValue.ToString(CultureInfo.InvariantCulture)} AND (max_item_count IS NULL OR (max_item_count > min_item_count AND max_item_count <= {ShippingAuthoringRateRowRules.ItemCountMaxValue.ToString(CultureInfo.InvariantCulture)})))");
                table.HasCheckConstraint(
                    "ck_authoring_rate_rows_merchandise_interval",
                    $"(min_merchandise_amount IS NULL AND max_merchandise_amount IS NULL) OR (min_merchandise_amount IS NOT NULL AND min_merchandise_amount >= 0 AND min_merchandise_amount <= {ShippingAuthoringRateRowRules.AmountMaxValue.ToString(CultureInfo.InvariantCulture)} AND (max_merchandise_amount IS NULL OR (max_merchandise_amount > min_merchandise_amount AND max_merchandise_amount <= {ShippingAuthoringRateRowRules.AmountMaxValue.ToString(CultureInfo.InvariantCulture)})))");
                table.HasCheckConstraint("ck_authoring_rate_rows_freight_non_negative", "freight_amount >= 0");
                table.HasCheckConstraint("ck_authoring_rate_rows_packaging_non_negative", "packaging_amount >= 0");
                table.HasCheckConstraint(
                    "ck_authoring_rate_rows_amount_within_max",
                    $"freight_amount <= {ShippingAuthoringRateRowRules.AmountMaxValue.ToString(CultureInfo.InvariantCulture)} AND packaging_amount <= {ShippingAuthoringRateRowRules.AmountMaxValue.ToString(CultureInfo.InvariantCulture)} AND (min_merchandise_amount IS NULL OR min_merchandise_amount <= {ShippingAuthoringRateRowRules.AmountMaxValue.ToString(CultureInfo.InvariantCulture)}) AND (max_merchandise_amount IS NULL OR max_merchandise_amount <= {ShippingAuthoringRateRowRules.AmountMaxValue.ToString(CultureInfo.InvariantCulture)})");
                table.HasCheckConstraint(
                    "ck_authoring_rate_rows_duration_range",
                    $"min_delivery_days >= 0 AND max_delivery_days >= min_delivery_days AND max_delivery_days <= {ShippingAuthoringRateRowRules.DaysMaxValue.ToString(CultureInfo.InvariantCulture)}");
            });
            entity.HasKey(x => x.RateRowId);
            entity.Property(x => x.RateRowId).ValueGeneratedNever();
            entity.Property(x => x.GroupId).IsRequired();
            entity.Property(x => x.FreightAmount).HasPrecision(18, 2);
            entity.Property(x => x.PackagingAmount).HasPrecision(18, 2);
            entity.Property(x => x.MinMerchandiseAmount).HasPrecision(18, 2);
            entity.Property(x => x.MaxMerchandiseAmount).HasPrecision(18, 2);

            // Rows are always read per group; no uniqueness/ordering index is added because row order has
            // no pricing priority and rows are alternatives, validated as a whole tuple.
            entity.HasIndex(x => x.GroupId)
                .HasDatabaseName("ix_authoring_rate_rows_group");
        });

        // TB-193 owner-scoped per-weekday operating schedule for ONE draft. It is attached to the draft
        // (method), never to a region group, rate row, language, seller actor or geography, so every
        // region group of the same draft shares exactly this one header. Absence of the header is the
        // explicit UNCONFIGURED state; an all-closed week is seven real rows and is NOT the same thing.
        modelBuilder.Entity<ShippingAuthoringWeeklySchedule>(entity =>
        {
            entity.ToTable("authoring_weekly_schedules", table =>
            {
                // A blank or over-long timezone would make every local interval uninterpretable; the
                // runtime resolvability itself stays a canonical preflight rule, not a fake SQL catalog
                // lookup.
                table.HasCheckConstraint(
                    "ck_authoring_weekly_schedules_timezone_length",
                    $"char_length(btrim(time_zone_id)) > 0 AND char_length(time_zone_id) <= {ShippingRatePolicyLimits.TimeZoneIdMaxLength.ToString(CultureInfo.InvariantCulture)}");
                // TB-241: no slot duration means ONE full-day window; a present duration must be a real
                // positive whole number of minutes inside one day, never zero/negative/overflowing.
                table.HasCheckConstraint(
                    "ck_authoring_weekly_schedules_slot_duration",
                    "slot_duration_minutes IS NULL OR (slot_duration_minutes >= 1 AND slot_duration_minutes <= 1440)");
            });
            entity.HasKey(x => x.DraftId);
            entity.Property(x => x.DraftId).ValueGeneratedNever();
            entity.Property(x => x.TimeZoneId).IsRequired().HasMaxLength(ShippingRatePolicyLimits.TimeZoneIdMaxLength);
            entity.Property(x => x.SlotDurationMinutes);
            entity.Property(x => x.UpdatedAt).IsRequired();

            // ONE schedule per draft/method: the draft key is the real invariant, so a second header for
            // the same method is impossible even for a raw writer.
            entity.HasOne<ShippingAuthoringDraft>()
                .WithOne(draft => draft.WeeklySchedule)
                .HasForeignKey<ShippingAuthoringWeeklySchedule>(schedule => schedule.DraftId)
                .HasConstraintName("fk_authoring_weekly_schedules_draft")
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(x => x.Days).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.HasMany(x => x.Days)
                .WithOne()
                .HasForeignKey(day => day.DraftId)
                .HasConstraintName("fk_authoring_weekly_schedule_days_schedule")
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TB-194 method-root draft settings: freight payment timing and calculation mode are TWO
        // independent selections owned once by the draft/method, never per region group, rate row,
        // translation or schedule. Absence of the row is the explicit UNCONFIGURED state and is never
        // read as Manual/Prepaid, so both columns are required and constrained to the defined values.
        modelBuilder.Entity<ShippingAuthoringMethodSettings>(entity =>
        {
            entity.ToTable("authoring_method_settings", table =>
            {
                // An unknown numeric enum value is a rejected payload, never coerced to a defined one.
                // NULL is impossible because both columns are NOT NULL, so a missing choice cannot
                // bypass the constraints by being "unknown" in SQL three-valued logic.
                table.HasCheckConstraint(
                    "ck_authoring_method_settings_freight_mode",
                    $"freight_payment_mode IN ({(int)ShippingFreightPayerMode.Prepaid}, {(int)ShippingFreightPayerMode.CollectOnDelivery})");
                table.HasCheckConstraint(
                    "ck_authoring_method_settings_calculation_mode",
                    $"calculation_mode IN ({(int)ShippingAuthoringCalculationMode.Manual}, {(int)ShippingAuthoringCalculationMode.CarrierApi}, {(int)ShippingAuthoringCalculationMode.Free})");
            });
            entity.HasKey(x => x.DraftId);
            entity.Property(x => x.DraftId).ValueGeneratedNever();
            entity.Property(x => x.FreightPaymentMode).HasConversion<int>().IsRequired();
            entity.Property(x => x.CalculationMode).HasConversion<int>().IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();

            // ONE settings row per draft/method: the draft key is the real invariant, so a second row for
            // the same method is impossible even for a raw writer.
            entity.HasOne<ShippingAuthoringDraft>()
                .WithOne(draft => draft.MethodSettings)
                .HasForeignKey<ShippingAuthoringMethodSettings>(settings => settings.DraftId)
                .HasConstraintName("fk_authoring_method_settings_draft")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ShippingAuthoringScheduleDay>(entity =>
        {
            entity.ToTable("authoring_weekly_schedule_days", table =>
            {
                // ISO weekday identity is 1=Monday .. 7=Sunday. Storage rejects anything else so no
                // locale label, .NET DayOfWeek cast or array position can become the persisted identity.
                table.HasCheckConstraint(
                    "ck_authoring_weekly_schedule_days_weekday_range",
                    $"weekday BETWEEN {ShippingOperatingWindow.MinWeekday} AND {ShippingOperatingWindow.MaxWeekday}");
                // Both hours are present or both absent: a half-stored pair is not a remembered pair.
                table.HasCheckConstraint(
                    "ck_authoring_weekly_schedule_days_hours_paired",
                    "(start_time IS NULL) = (end_time IS NULL)");
                // The local interval is half-open [start,end): equal, inverted and overnight ranges are
                // rejected instead of being reversed, extended or normalized into tomorrow.
                table.HasCheckConstraint(
                    "ck_authoring_weekly_schedule_days_range",
                    "start_time IS NULL OR end_time > start_time");
                // A working day is not a working day without an explicit interval.
                table.HasCheckConstraint(
                    "ck_authoring_weekly_schedule_days_working_hours",
                    "is_working = FALSE OR (start_time IS NOT NULL AND end_time IS NOT NULL)");
            });
            // Exactly one row per (draft, weekday) is a stable day identity across whole-week
            // replacements; exactly-seven completeness stays a canonical preflight rule.
            entity.HasKey(x => new { x.DraftId, x.Weekday });
            entity.Property(x => x.DraftId).IsRequired();
            entity.Property(x => x.IsWorking).IsRequired();
        });

        modelBuilder.Entity<ShippingAuthoringSelectedArea>(entity =>
        {
            entity.ToTable("authoring_selected_areas", table =>
            {
                // The selected node kind is a stable code, never free-form prose: a row whose kind is
                // unreadable would make two selections indistinguishable (the same GUID can exist in two
                // reference node kinds).
                table.HasCheckConstraint(
                    "ck_authoring_selected_areas_node_kind_known",
                    $"node_kind IN ({(int)ShippingDestinationNodeKind.Country}, {(int)ShippingDestinationNodeKind.AdministrativeArea}, {(int)ShippingDestinationNodeKind.City}, {(int)ShippingDestinationNodeKind.ServiceArea})");
            });
            entity.HasKey(x => x.SelectedAreaId);
            entity.Property(x => x.SelectedAreaId).ValueGeneratedNever();
            entity.Property(x => x.NodeKind).HasConversion<int>();
            entity.Property(x => x.DraftId).IsRequired();

            // A selection may exist at most once per draft, not merely per group: the ancestor/descendant
            // rule is enforced in the aggregate, and this index is the relational mirror for the flat
            // duplicate case that another writer could otherwise introduce.
            entity.HasIndex(x => new { x.DraftId, x.NodeKind, x.NodeId })
                .IsUnique()
                .HasDatabaseName("ux_authoring_selected_areas_draft_node");
        });

        // TB-214 immutable revision capture (STAGING ONLY). One row per trusted owner + draft + source
        // revision. The payload is versioned typed JSONB owned by Shipping's own codec: it duplicates no
        // rule table, holds no FK/navigation into any other schema, and is never updated after insert.
        modelBuilder.Entity<ShippingAuthoringRevisionSnapshot>(entity =>
        {
            entity.ToTable("authoring_revision_snapshots", table =>
            {
                table.HasCheckConstraint(
                    "ck_authoring_revision_snapshots_owner_coherent",
                    $"((owner_kind = {(int)ShippingDraftOwnerKind.Store} AND owner_tenant_id IS NOT NULL AND owner_seller_party_id IS NULL)"
                    + $" OR (owner_kind = {(int)ShippingDraftOwnerKind.Marketplace} AND owner_tenant_id IS NULL AND owner_seller_party_id IS NULL)"
                    + $" OR (owner_kind = {(int)ShippingDraftOwnerKind.Seller} AND owner_tenant_id IS NULL AND owner_seller_party_id IS NOT NULL))");
                table.HasCheckConstraint(
                    "ck_authoring_revision_snapshots_source_revision_positive",
                    "source_revision > 0");
                table.HasCheckConstraint(
                    "ck_authoring_revision_snapshots_format_version_known",
                    $"format_version IN ({ShippingAuthoringRevisionSnapshot.LegacyFormatVersion}, "
                    + $"{ShippingAuthoringRevisionSnapshot.SlotDurationFormatVersion})");
                table.HasCheckConstraint(
                    "ck_authoring_revision_snapshots_tenant_not_blank",
                    "owner_tenant_id IS NULL OR char_length(owner_tenant_id) > 0");
            });
            entity.HasKey(x => x.SnapshotId);
            entity.Property(x => x.SnapshotId).ValueGeneratedNever();
            entity.Property(x => x.OwnerKind).HasConversion<int>();
            entity.Property(x => x.OwnerTenantId).HasMaxLength(ShippingDraftOwner.TenantIdMaxLength);
            entity.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();

            // One capture per trusted owner + draft + source revision: a second capture of the SAME
            // revision is idempotent, never a second row pretending to be a new publication.
            entity.HasIndex(x => new
                {
                    x.OwnerKind,
                    x.OwnerTenantId,
                    x.OwnerSellerPartyId,
                    x.DraftId,
                    x.SourceRevision,
                })
                .IsUnique()
                .AreNullsDistinct(false)
                .HasDatabaseName("ux_authoring_revision_snapshots_owner_revision");
        });

        // TB-219 selected captured revision pointer. ONE row per FULL owner + canonical service/option;
        // destination, method label and shipping type are NOT part of the key, and the persisted service/
        // option columns are the canonical (trim + lowercase) keys so casing cannot create a second pointer.
        // The selected row is an already-existing TB-214 immutable snapshot: this table copies no payload,
        // has no FK/navigation into another schema and never mutates a capture.
        modelBuilder.Entity<ShippingSelectedCapturedRevision>(entity =>
        {
            entity.ToTable("selected_captured_revisions", table =>
            {
                table.HasCheckConstraint(
                    "ck_selected_captured_revisions_owner_coherent",
                    $"((owner_kind = {(int)ShippingDraftOwnerKind.Store} AND owner_tenant_id IS NOT NULL AND owner_seller_party_id IS NULL)"
                    + $" OR (owner_kind = {(int)ShippingDraftOwnerKind.Marketplace} AND owner_tenant_id IS NULL AND owner_seller_party_id IS NULL)"
                    + $" OR (owner_kind = {(int)ShippingDraftOwnerKind.Seller} AND owner_tenant_id IS NULL AND owner_seller_party_id IS NOT NULL))");
                table.HasCheckConstraint(
                    "ck_selected_captured_revisions_version_positive",
                    "selection_version > 0");
                table.HasCheckConstraint(
                    "ck_selected_captured_revisions_service_key_canonical",
                    "char_length(binding_service_key) > 0 AND binding_service_key = lower(btrim(binding_service_key))");
                table.HasCheckConstraint(
                    "ck_selected_captured_revisions_option_key_canonical",
                    "binding_option_key IS NULL OR (char_length(binding_option_key) > 0 AND binding_option_key = lower(btrim(binding_option_key)))");
                table.HasCheckConstraint(
                    "ck_selected_captured_revisions_selected_snapshot_not_empty",
                    "selected_snapshot_id IS NULL OR selected_snapshot_id <> '00000000-0000-0000-0000-000000000000'::uuid");
            });
            entity.HasKey(x => x.SelectionId);
            entity.Property(x => x.SelectionId).ValueGeneratedNever();
            entity.Property(x => x.OwnerKind).HasConversion<int>();
            entity.Property(x => x.OwnerTenantId).HasMaxLength(ShippingDraftOwner.TenantIdMaxLength);
            entity.Property(x => x.BindingServiceKey).HasMaxLength(ShippingAuthoringDraft.ServiceCodeMaxLength).IsRequired();
            entity.Property(x => x.BindingOptionKey).HasMaxLength(ShippingAuthoringDraft.OptionCodeMaxLength);

            // The version column is the real concurrency token: a stale writer is a conflict, never a merge.
            entity.Property(x => x.SelectionVersion).IsConcurrencyToken();

            // Missing nullable owner/option columns must not bypass uniqueness (NULLS NOT DISTINCT), which is
            // the established repository pattern for this module.
            entity.HasIndex(x => new
                {
                    x.OwnerKind,
                    x.OwnerTenantId,
                    x.OwnerSellerPartyId,
                    x.BindingServiceKey,
                    x.BindingOptionKey,
                })
                .IsUnique()
                .AreNullsDistinct(false)
                .HasDatabaseName("ux_selected_captured_revisions_owner_binding");
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

