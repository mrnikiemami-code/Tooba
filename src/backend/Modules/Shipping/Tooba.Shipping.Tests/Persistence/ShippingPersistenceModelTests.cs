using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Persistence;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Domain.Aggregates;
using Tooba.Shipping.Domain.Scheduling;
using Tooba.Shipping.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Shipping.Tests.Persistence;

/// <summary>
/// آزمون‌های ماندگاری Shipping: مدل EF، مهاجرت اولیه و قیدهای واقعی در برابر هم پین می‌شوند.
/// </summary>
/// <remarks>
/// <para>
/// هیچ اتصالی به دیتابیس باز نمی‌شود: ساخت مدل، ساخت اسکریپت مهاجرت و بررسی «هم‌خوانی مدل با
/// snapshot» همه offline هستند، بنابراین این فایل در CI بدون PostgreSQL هم اجرا می‌شود.
/// </para>
/// <para>
/// مدل از <see cref="IDesignTimeModel"/> خوانده می‌شود نه از <c>DbContext.Model</c>: مدل runtime
/// برای کارایی «بهینه‌شده برای خواندن» است و قیدهای CHECK را نگه نمی‌دارد. بررسی متن DDL هم عمدی
/// است: یک <c>NULLS NOT DISTINCT</c> که در مدل تعریف شده باشد ولی در اسکریپت نیفتد، یکتایی را
/// فقط در حافظه تضمین می‌کند.
/// </para>
/// </remarks>
public sealed class ShippingPersistenceModelTests
{
    private const string OfflineConnection =
        "Host=127.0.0.1;Port=1;Username=model_only;Password=model_only;Database=model_only";

    private static ShippingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ShippingDbContext>();
        // The real runtime configuration (snake_case naming, module schema, module migrations assembly)
        // rather than a bare UseNpgsql, so the assertions describe the actual deployment shape.
        ToobaNpgsql.ConfigureModuleContext(
            options,
            OfflineConnection,
            ShippingDbContext.Schema,
            typeof(ShippingDbContext));
        return new ShippingDbContext(options.Options);
    }

    private static IModel DesignModel(ShippingDbContext context)
        => context.GetService<IDesignTimeModel>().Model;

    private static IEntityType Entity(IModel model, string tableName)
        => model.GetEntityTypes().Single(x => x.GetTableName() == tableName);

    [Fact]
    public void Model_keeps_the_module_schema_and_the_owned_tables()
    {
        using var context = CreateContext();
        var model = DesignModel(context);

        Assert.Equal("shipping", model.GetDefaultSchema());
        Assert.Equal(
            new[]
            {
                "authoring_draft_translations",
                "authoring_drafts",
                "authoring_group_titles",
                "authoring_method_settings",
                "authoring_rate_rows",
                "authoring_region_groups",
                "authoring_selected_areas",
                "authoring_weekly_schedule_days",
                "authoring_weekly_schedules",
                "coverage_grants",
                "method_coverages",
                "operating_schedules",
                "operating_windows",
                "outbox_messages",
                "packaging_policies",
                "rate_policies",
            },
            model.GetEntityTypes().Select(x => x.GetTableName()).OrderBy(x => x, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void Primary_keys_are_application_assigned_so_replay_stays_deterministic()
    {
        using var context = CreateContext();
        var model = DesignModel(context);

        var rateKey = Assert.Single(Entity(model, "rate_policies").FindPrimaryKey()!.Properties);
        Assert.Equal(nameof(ShippingRatePolicy.RatePolicyId), rateKey.Name);
        Assert.Equal(ValueGenerated.Never, rateKey.ValueGenerated);

        var grantKey = Assert.Single(Entity(model, "coverage_grants").FindPrimaryKey()!.Properties);
        Assert.Equal(nameof(ShippingCoverageGrant.CoverageGrantId), grantKey.Name);
        Assert.Equal(ValueGenerated.Never, grantKey.ValueGenerated);

        var coverageKey = Assert.Single(Entity(model, "method_coverages").FindPrimaryKey()!.Properties);
        Assert.Equal(nameof(ShippingMethodCoverage.MethodCoverageId), coverageKey.Name);
        Assert.Equal(ValueGenerated.Never, coverageKey.ValueGenerated);
    }

    [Fact]
    public void Required_columns_cannot_be_null_at_the_storage_boundary()
    {
        using var context = CreateContext();
        var model = DesignModel(context);

        var rate = Entity(model, "rate_policies");
        foreach (var required in new[]
                 {
                     nameof(ShippingRatePolicy.ServiceCode),
                     nameof(ShippingRatePolicy.CountryId),
                     nameof(ShippingRatePolicy.ChargeAmount),
                     nameof(ShippingRatePolicy.CurrencyCode),
                     nameof(ShippingRatePolicy.PreparationDays),
                     nameof(ShippingRatePolicy.LeadDays),
                     nameof(ShippingRatePolicy.IsActive),
                 })
        {
            Assert.False(rate.FindProperty(required)!.IsNullable, $"rate_policies.{required} must be NOT NULL");
        }

        var coverage = Entity(model, "coverage_grants");
        foreach (var required in new[]
                 {
                     nameof(ShippingCoverageGrant.ServiceCode),
                     nameof(ShippingCoverageGrant.CountryId),
                     nameof(ShippingCoverageGrant.IsActive),
                 })
        {
            Assert.False(coverage.FindProperty(required)!.IsNullable, $"coverage_grants.{required} must be NOT NULL");
        }

        Assert.True(coverage.FindProperty(nameof(ShippingCoverageGrant.AdministrativeAreaId))!.IsNullable);
        Assert.True(coverage.FindProperty(nameof(ShippingCoverageGrant.CityId))!.IsNullable);
        Assert.True(coverage.FindProperty(nameof(ShippingCoverageGrant.ServiceAreaId))!.IsNullable);

        var packaging = Entity(model, "packaging_policies");
        foreach (var required in new[]
                 {
                     nameof(ShippingPackagingPolicy.ServiceCode),
                     nameof(ShippingPackagingPolicy.CountryId),
                     nameof(ShippingPackagingPolicy.FeeAmount),
                     nameof(ShippingPackagingPolicy.CurrencyCode),
                     nameof(ShippingPackagingPolicy.FreightPayerMode),
                     nameof(ShippingPackagingPolicy.IsActive),
                 })
        {
            Assert.False(packaging.FindProperty(required)!.IsNullable, $"packaging_policies.{required} must be NOT NULL");
        }

        Assert.Equal(ShippingRatePolicy.ServiceCodeMaxLength, rate.FindProperty(nameof(ShippingRatePolicy.ServiceCode))!.GetMaxLength());
        Assert.Equal(3, rate.FindProperty(nameof(ShippingRatePolicy.CurrencyCode))!.GetMaxLength());
        Assert.Equal(18, rate.FindProperty(nameof(ShippingRatePolicy.ChargeAmount))!.GetPrecision());
        Assert.Equal(2, rate.FindProperty(nameof(ShippingRatePolicy.ChargeAmount))!.GetScale());
        Assert.Equal(18, rate.FindProperty(nameof(ShippingRatePolicy.FreeAboveThreshold))!.GetPrecision());
    }

    [Theory]
    [InlineData("rate_policies", "ck_rate_policies_charge_amount_non_negative", "charge_amount >= 0")]
    [InlineData("rate_policies", "ck_rate_policies_charge_amount_within_max", "1000000000")]
    [InlineData("rate_policies", "ck_rate_policies_free_above_threshold_non_negative", "free_above_threshold IS NULL")]
    [InlineData("rate_policies", "ck_rate_policies_preparation_days_range", "preparation_days >= 0")]
    [InlineData("rate_policies", "ck_rate_policies_lead_days_range", "lead_days <= 365")]
    [InlineData("rate_policies", "ck_rate_policies_service_code_canonical", "btrim(service_code)")]
    [InlineData("rate_policies", "ck_rate_policies_currency_code_canonical", "char_length(currency_code) = 3")]
    [InlineData("rate_policies", "ck_rate_policies_scope_coherent", "service_area_id IS NULL OR city_id IS NOT NULL")]
    [InlineData("rate_policies", "ck_rate_policies_weight_tier_coherent", "max_weight_grams > min_weight_grams")]
    [InlineData("rate_policies", "ck_rate_policies_count_tier_coherent", "max_item_count > min_item_count")]
    [InlineData("rate_policies", "ck_rate_policies_tier_bounds_paired", "max_weight_grams IS NULL OR min_weight_grams IS NOT NULL")]
    [InlineData("coverage_grants", "ck_coverage_grants_service_code_canonical", "btrim(service_code)")]
    [InlineData("coverage_grants", "ck_coverage_grants_scope_coherent", "service_area_id IS NULL OR city_id IS NOT NULL")]
    [InlineData("operating_schedules", "ck_operating_schedules_service_code_canonical", "btrim(service_code)")]
    [InlineData("operating_schedules", "ck_operating_schedules_option_code_canonical", "option_code IS NULL")]
    [InlineData("operating_schedules", "ck_operating_schedules_timezone_canonical", "btrim(time_zone_id)")]
    [InlineData("operating_windows", "ck_operating_windows_weekday_iso_range", "weekday >= 1 AND weekday <= 7")]
    [InlineData("operating_windows", "ck_operating_windows_range_half_open", "end_time > start_time")]
    [InlineData("operating_windows", "ck_operating_windows_capacity_positive", "capacity IS NULL OR (capacity > 0")]
    [InlineData("packaging_policies", "ck_packaging_policies_fee_amount_non_negative", "fee_amount >= 0")]
    [InlineData("packaging_policies", "ck_packaging_policies_fee_amount_within_max", "1000000000")]
    [InlineData("packaging_policies", "ck_packaging_policies_service_code_canonical", "btrim(service_code)")]
    [InlineData("packaging_policies", "ck_packaging_policies_option_code_canonical", "option_code IS NULL")]
    [InlineData("packaging_policies", "ck_packaging_policies_currency_code_canonical", "char_length(currency_code) = 3")]
    [InlineData("packaging_policies", "ck_packaging_policies_scope_coherent", "service_area_id IS NULL OR city_id IS NOT NULL")]
    [InlineData("packaging_policies", "ck_packaging_policies_freight_payer_mode_known", "freight_payer_mode IN (0, 1)")]
    [InlineData("authoring_drafts", "ck_authoring_drafts_owner_coherent", "owner_kind = 0 AND owner_tenant_id IS NOT NULL")]
    [InlineData("authoring_drafts", "ck_authoring_drafts_revision_positive", "revision > 0")]
    [InlineData("authoring_drafts", "ck_authoring_drafts_service_code_canonical", "btrim(service_code)")]
    [InlineData("authoring_drafts", "ck_authoring_drafts_option_code_canonical", "option_code IS NULL")]
    [InlineData("authoring_region_groups", "ck_authoring_region_groups_order_non_negative", "group_order >= 0")]
    [InlineData("authoring_selected_areas", "ck_authoring_selected_areas_node_kind_known", "node_kind IN (0, 1, 2, 3)")]
    public void Check_constraints_mirror_the_aggregate_guards(string table, string constraintName, string predicateFragment)
    {
        using var context = CreateContext();
        var entity = Entity(DesignModel(context), table);

        var constraint = Assert.Single(entity.GetCheckConstraints(), x => x.Name == constraintName);
        Assert.Contains(predicateFragment, constraint.Sql);
    }

    [Theory]
    [InlineData("rate_policies", "ux_rate_policies_service_option_scope", new[] { "ServiceCode", "OptionCode", "CountryId", "AdministrativeAreaId", "CityId", "ServiceAreaId", "MinWeightGrams", "MaxWeightGrams", "MinItemCount", "MaxItemCount" })]
    [InlineData("coverage_grants", "ux_coverage_grants_service_option_scope", new[] { "ServiceCode", "OptionCode", "CountryId", "AdministrativeAreaId", "CityId", "ServiceAreaId" })]
    [InlineData("method_coverages", "ux_method_coverages_service_option", new[] { "ServiceCode", "OptionCode" })]
    [InlineData("operating_schedules", "ux_operating_schedules_service_option", new[] { "ServiceCode", "OptionCode" })]
    [InlineData("packaging_policies", "ux_packaging_policies_service_option_scope", new[] { "ServiceCode", "OptionCode", "CountryId", "AdministrativeAreaId", "CityId", "ServiceAreaId" })]
    [InlineData("authoring_drafts", "ux_authoring_drafts_owner_binding", new[] { "OwnerKind", "OwnerTenantId", "OwnerSellerPartyId", "ServiceCode", "OptionCode" })]
    public void Identity_index_is_unique_over_nulls_not_distinct_columns(
        string table,
        string indexName,
        string[] expectedColumns)
    {
        using var context = CreateContext();
        var entity = Entity(DesignModel(context), table);
        var index = Assert.Single(entity.GetIndexes(), x => x.GetDatabaseName() == indexName);

        Assert.True(index.IsUnique);
        Assert.Equal(expectedColumns, index.Properties.Select(x => x.Name).ToArray());

        // Without this annotation PostgreSQL treats every NULL as distinct and would silently admit two
        // option-less rows for the same service - exactly the duplicate that makes "which window applies"
        // ambiguous at request time.
        var annotation = index.FindAnnotation("Npgsql:NullsDistinct");
        Assert.NotNull(annotation);
        Assert.False((bool?)annotation!.Value is not false, "the identity unique index must be NULLS NOT DISTINCT");
    }

    [Fact]
    public void Operating_windows_are_reached_through_an_intra_module_cascade_only()
    {
        using var context = CreateContext();
        var model = DesignModel(context);
        var window = Entity(model, "operating_windows");

        var foreignKey = Assert.Single(window.GetForeignKeys());
        Assert.Equal("shipping", foreignKey.PrincipalEntityType.GetSchema());
        Assert.Equal("operating_schedules", foreignKey.PrincipalEntityType.GetTableName());
        Assert.True(foreignKey.IsRequired);
        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);

        var index = Assert.Single(
            window.GetIndexes(),
            x => x.GetDatabaseName() == "ix_operating_windows_schedule_weekday");
        Assert.False(index.IsUnique);
        Assert.Equal(new[] { "schedule_id", nameof(ShippingOperatingWindow.Weekday) }, index.Properties.Select(x => x.Name).ToArray());
    }

    [Theory]
    [InlineData("rate_policies", "ix_rate_policies_service_active", new[] { "ServiceCode", "IsActive" })]
    [InlineData("coverage_grants", "ix_coverage_grants_service_country_active", new[] { "ServiceCode", "CountryId", "IsActive" })]
    [InlineData("packaging_policies", "ix_packaging_policies_service_active", new[] { "ServiceCode", "IsActive" })]
    [InlineData("outbox_messages", "ix_outbox_messages_pending", new[] { "OccurredAt" })]
    public void Quote_lookup_indexes_exist_and_are_not_unique(string table, string indexName, string[] columns)
    {
        using var context = CreateContext();
        var index = Assert.Single(Entity(DesignModel(context), table).GetIndexes(), x => x.GetDatabaseName() == indexName);

        Assert.False(index.IsUnique);
        Assert.Equal(columns, index.Properties.Select(x => x.Name).ToArray());
    }

    [Fact]
    public void Shipping_owns_no_cross_module_foreign_key()
    {
        using var context = CreateContext();

        // The only foreign key in the whole model is the intra-module schedule -> windows relation:
        // Shipping never references another module's schema.
        foreach (var entity in DesignModel(context).GetEntityTypes())
        {
            foreach (var foreignKey in entity.GetForeignKeys())
            {
                Assert.Equal("shipping", foreignKey.PrincipalEntityType.GetSchema());
            }
        }
    }

    [Fact]
    public void Model_and_the_initial_migration_are_in_sync()
    {
        using var context = CreateContext();
        Assert.False(
            context.Database.HasPendingModelChanges(),
            "a model change exists that the InitialShipping migration does not describe");
    }

    [Fact]
    public void Full_migration_script_emits_the_expected_schema_tables_and_constraints()
    {
        using var context = CreateContext();
        var script = context.GetService<IMigrator>().GenerateScript();

        Assert.Contains("CREATE SCHEMA shipping;", script);
        Assert.Contains("CREATE TABLE shipping.rate_policies", script);
        Assert.Contains("CREATE TABLE shipping.coverage_grants", script);
        Assert.Contains("CREATE TABLE shipping.method_coverages", script);
        Assert.Contains("CREATE TABLE shipping.operating_schedules", script);
        Assert.Contains("CREATE TABLE shipping.operating_windows", script);
        Assert.Contains("CREATE TABLE shipping.packaging_policies", script);
        Assert.Contains("CREATE TABLE shipping.authoring_drafts", script);
        Assert.Contains("CREATE TABLE shipping.authoring_region_groups", script);
        Assert.Contains("CREATE TABLE shipping.authoring_selected_areas", script);
        Assert.Contains("CREATE TABLE shipping.authoring_weekly_schedules", script);
        Assert.Contains("CREATE TABLE shipping.authoring_weekly_schedule_days", script);
        Assert.Contains("CREATE TABLE shipping.outbox_messages", script);
        Assert.Contains("__ef_migrations_history", script);

        foreach (var constraint in new[]
                 {
                     "ck_rate_policies_charge_amount_non_negative",
                     "ck_rate_policies_charge_amount_within_max",
                     "ck_rate_policies_free_above_threshold_non_negative",
                     "ck_rate_policies_preparation_days_range",
                     "ck_rate_policies_lead_days_range",
                     "ck_rate_policies_service_code_canonical",
                     "ck_rate_policies_currency_code_canonical",
                     "ck_rate_policies_scope_coherent",
                     "ck_rate_policies_option_code_canonical",
                     "ck_rate_policies_weight_tier_coherent",
                     "ck_rate_policies_count_tier_coherent",
                     "ck_rate_policies_tier_bounds_paired",
                     "ck_coverage_grants_service_code_canonical",
                     "ck_coverage_grants_option_code_canonical",
                     "ck_coverage_grants_scope_coherent",
                     "ck_method_coverages_service_code_canonical",
                     "ck_method_coverages_option_code_canonical",
                     "ck_method_coverages_mode_known",
                     "ck_operating_schedules_service_code_canonical",
                     "ck_operating_schedules_option_code_canonical",
                     "ck_operating_schedules_timezone_canonical",
                     "ck_operating_windows_weekday_iso_range",
                     "ck_operating_windows_range_half_open",
                     "ck_operating_windows_capacity_positive",
                     "ux_rate_policies_service_option_scope",
                     "ux_coverage_grants_service_option_scope",
                     "ux_method_coverages_service_option",
                     "ux_operating_schedules_service_option",
                     "ux_packaging_policies_service_option_scope",
                     "ck_packaging_policies_fee_amount_non_negative",
                     "ck_packaging_policies_fee_amount_within_max",
                     "ck_packaging_policies_service_code_canonical",
                     "ck_packaging_policies_option_code_canonical",
                     "ck_packaging_policies_currency_code_canonical",
                     "ck_packaging_policies_scope_coherent",
                     "ck_packaging_policies_freight_payer_mode_known",
                     "ck_authoring_drafts_owner_coherent",
                     "ck_authoring_drafts_revision_positive",
                     "ck_authoring_drafts_service_code_canonical",
                     "ck_authoring_drafts_option_code_canonical",
                     "ck_authoring_drafts_tenant_not_blank",
                     "ck_authoring_region_groups_order_non_negative",
                     "ck_authoring_region_groups_currency_canonical",
                     "ck_authoring_selected_areas_node_kind_known",
                     "ck_authoring_rate_rows_weight_interval",
                     "ck_authoring_rate_rows_count_interval",
                     "ck_authoring_rate_rows_merchandise_interval",
                     "ck_authoring_rate_rows_freight_non_negative",
                     "ck_authoring_rate_rows_packaging_non_negative",
                     "ck_authoring_rate_rows_amount_within_max",
                     "ck_authoring_rate_rows_duration_range",
                     "ux_authoring_drafts_owner_binding",
                     "ux_authoring_region_groups_draft_order",
                     "ux_authoring_selected_areas_draft_node",
                     "ck_authoring_draft_translations_name_length",
                     "ck_authoring_draft_translations_description_length",
                     "ck_authoring_group_titles_title_length",
                     "ck_authoring_weekly_schedules_timezone_length",
                     "ck_authoring_method_settings_freight_mode",
                     "ck_authoring_method_settings_calculation_mode",
                     "ck_authoring_weekly_schedule_days_weekday_range",
                     "ck_authoring_weekly_schedule_days_hours_paired",
                     "ck_authoring_weekly_schedule_days_range",
                     "ck_authoring_weekly_schedule_days_working_hours",
                     "fk_authoring_weekly_schedules_draft",
                     "fk_authoring_method_settings_draft",
                     "fk_authoring_weekly_schedule_days_schedule",
                     "ix_authoring_rate_rows_group",
                 })
        {
            Assert.Contains(constraint, script);
        }

        // Every scope/identity index must carry the modifier: the initial slice (rate + coverage = 2),
        // then one re-created rate index in each of the three additive migrations (option identity,
        // tier) plus the schedule identity index, plus the packaging policy identity index, plus the
        // option-scoped coverage index rebuild and the method coverage identity index, plus the
        // owner-scoped authoring draft binding index. The drops do not emit the modifier, so this
        // counts CREATE statements only.
        Assert.Equal(9, script.Split("NULLS NOT DISTINCT", StringSplitOptions.None).Length - 1);

        // The foreign keys in the whole schema are intra-module only: the schedule -> windows cascade,
        // the two authoring draft -> group -> selected-area cascades, the group -> rate-row cascade,
        // ... draft -> method translations, draft -> group titles, the TB-193 draft -> weekly schedule
        // Nothing points at another module's schema.
        Assert.Contains("REFERENCES shipping.operating_schedules", script);
        Assert.Contains("REFERENCES shipping.authoring_drafts", script);
        Assert.Contains("REFERENCES shipping.authoring_region_groups", script);
        // TB-192 adds exactly two more intra-module cascades: draft -> method translations and
        // group -> optional titles. TB-193 adds two more: draft -> weekly schedule header and
        // header -> the seven day rows. TB-194 adds draft -> method settings. No language/registry FK
        // crosses a module schema.
        Assert.Equal(
            9,
            script.Split("FOREIGN KEY", StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public void Migrations_are_the_initial_slice_plus_eight_additive_updates()
    {
        using var context = CreateContext();
        var migrations = context.Database.GetMigrations().ToArray();

        // Ten migrations only: the initial slice, five additive changes (option identity, operating
        // schedule, weight/count tiers, packaging policy, option-scoped coverage restriction), the D3
        // authoring draft/group/area addition, the TB-191 group rate-row addition, the TB-191-R1
        // constraint-parity correction and the TB-193 weekly schedule addition. Asserting the exact
        // count keeps the schema reviewable as a unit.
        Assert.Equal(12, migrations.Length);
        Assert.EndsWith("_InitialShipping", migrations[0], StringComparison.Ordinal);
        Assert.EndsWith("_OptionLevelRatePolicy", migrations[1], StringComparison.Ordinal);
        Assert.EndsWith("_OperatingSchedule", migrations[2], StringComparison.Ordinal);
        Assert.EndsWith("_WeightAndCountRateTiers", migrations[3], StringComparison.Ordinal);
        Assert.EndsWith("_ShippingPackagingPolicy", migrations[4], StringComparison.Ordinal);
        Assert.EndsWith("_OptionScopedCoverageRestriction", migrations[5], StringComparison.Ordinal);
        Assert.EndsWith("_AuthoringDraftPersistence", migrations[6], StringComparison.Ordinal);
        Assert.EndsWith("_AuthoringGroupRateRows", migrations[7], StringComparison.Ordinal);
        Assert.EndsWith("_AuthoringRateRowConstraintParity", migrations[8], StringComparison.Ordinal);
        Assert.EndsWith("_AuthoringPresentationTranslations", migrations[9], StringComparison.Ordinal);
        Assert.EndsWith("_AuthoringWeeklySchedule", migrations[10], StringComparison.Ordinal);
        Assert.EndsWith("_AuthoringMethodSettings", migrations[11], StringComparison.Ordinal);
    }

    [Fact]
    public void Domain_scope_coherence_rule_is_the_same_rule_as_the_stored_check_constraint()
    {
        var country = Guid.NewGuid();
        var serviceArea = Guid.NewGuid();

        // The aggregate refuses a half-scope (a service area with no owning city) ...
        var thrown = Record.Exception(() => ShippingRatePolicy.Create(
            "post",
            new ShippingDestinationScope(country, null, null, serviceArea),
            1_000m,
            "IRR",
            0,
            1,
            null,
            DateTimeOffset.UnixEpoch));
        Assert.NotNull(thrown);
        Assert.Equal(ShippingErrorCodes.CoverageScopeInvalid, thrown!.Message);

        // ... and the same rule is literally the CHECK predicate on both tables, so raw SQL cannot store
        // the half-scope the aggregate refuses.
        using var context = CreateContext();
        foreach (var table in new[] { "rate_policies", "coverage_grants" })
        {
            var constraint = Assert.Single(
                Entity(DesignModel(context), table).GetCheckConstraints(),
                x => x.Name is { } name && name.EndsWith("_scope_coherent", StringComparison.Ordinal));
            Assert.NotNull(constraint);
            Assert.Equal("service_area_id IS NULL OR city_id IS NOT NULL", constraint!.Sql);
        }
    }

    [Fact]
    public void Shipping_module_still_registers_no_schema_migrator()
    {
        // The migration exists for review only: nothing in this task wires it into Host or
        // MigrationRunner, so no running process may apply the shipping schema.
        var assembly = typeof(Tooba.Shipping.Infrastructure.DependencyInjection.ShippingModule).Assembly;

        Assert.DoesNotContain(
            assembly.GetTypes(),
            type => typeof(IModuleSchemaMigrator).IsAssignableFrom(type)
                && type is { IsClass: true, IsAbstract: false });
    }
}
