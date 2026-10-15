using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Persistence;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Domain.Aggregates;
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
    public void Model_keeps_the_module_schema_and_the_three_owned_tables()
    {
        using var context = CreateContext();
        var model = DesignModel(context);

        Assert.Equal("shipping", model.GetDefaultSchema());
        Assert.Equal(
            new[] { "coverage_grants", "outbox_messages", "rate_policies" },
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
    [InlineData("coverage_grants", "ck_coverage_grants_service_code_canonical", "btrim(service_code)")]
    [InlineData("coverage_grants", "ck_coverage_grants_scope_coherent", "service_area_id IS NULL OR city_id IS NOT NULL")]
    public void Check_constraints_mirror_the_aggregate_guards(string table, string constraintName, string predicateFragment)
    {
        using var context = CreateContext();
        var entity = Entity(DesignModel(context), table);

        var constraint = Assert.Single(entity.GetCheckConstraints(), x => x.Name == constraintName);
        Assert.Contains(predicateFragment, constraint.Sql);
    }

    [Theory]
    [InlineData("rate_policies", "ux_rate_policies_service_option_scope", true)]
    [InlineData("coverage_grants", "ux_coverage_grants_service_scope", false)]
    public void Scope_identity_index_is_unique_over_nulls_not_distinct_columns(
        string table,
        string indexName,
        bool optionAware)
    {
        using var context = CreateContext();
        var entity = Entity(DesignModel(context), table);
        var index = Assert.Single(entity.GetIndexes(), x => x.GetDatabaseName() == indexName);

        Assert.True(index.IsUnique);
        Assert.Equal(
            optionAware
                ? new[]
                {
                    nameof(ShippingRatePolicy.ServiceCode),
                    nameof(ShippingRatePolicy.OptionCode),
                    nameof(ShippingRatePolicy.CountryId),
                    nameof(ShippingRatePolicy.AdministrativeAreaId),
                    nameof(ShippingRatePolicy.CityId),
                    nameof(ShippingRatePolicy.ServiceAreaId),
                }
                : new[]
                {
                    nameof(ShippingRatePolicy.ServiceCode),
                    nameof(ShippingRatePolicy.CountryId),
                    nameof(ShippingRatePolicy.AdministrativeAreaId),
                    nameof(ShippingRatePolicy.CityId),
                    nameof(ShippingRatePolicy.ServiceAreaId),
                },
            index.Properties.Select(x => x.Name).ToArray());

        // Without this annotation PostgreSQL treats every NULL level as distinct and would silently admit
        // two "country scope" rows for the same service - exactly the duplicate the engine answers with
        // ScopeConflict at request time.
        var annotation = index.FindAnnotation("Npgsql:NullsDistinct");
        Assert.NotNull(annotation);
        Assert.False((bool?)annotation!.Value is not false, "the scope unique index must be NULLS NOT DISTINCT");
    }

    [Theory]
    [InlineData("rate_policies", "ix_rate_policies_service_active", new[] { "ServiceCode", "IsActive" })]
    [InlineData("coverage_grants", "ix_coverage_grants_service_country_active", new[] { "ServiceCode", "CountryId", "IsActive" })]
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

        foreach (var entity in DesignModel(context).GetEntityTypes())
        {
            Assert.Empty(entity.GetForeignKeys());
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
    public void Initial_migration_emits_the_expected_schema_tables_and_constraints()
    {
        using var context = CreateContext();
        var script = context.GetService<IMigrator>().GenerateScript();

        Assert.Contains("CREATE SCHEMA shipping;", script);
        Assert.Contains("CREATE TABLE shipping.rate_policies", script);
        Assert.Contains("CREATE TABLE shipping.coverage_grants", script);
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
                     "ck_coverage_grants_service_code_canonical",
                     "ck_coverage_grants_scope_coherent",
                     "ux_rate_policies_service_option_scope",
                     "ux_coverage_grants_service_scope",
                 })
        {
            Assert.Contains(constraint, script);
        }

        // Every scope index must carry the modifier: coverage (1) + the initial rate index (1) + the
        // option-aware rate index added by the second migration (1).
        Assert.Equal(3, script.Split("NULLS NOT DISTINCT", StringSplitOptions.None).Length - 1);
        Assert.DoesNotContain("FOREIGN KEY", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Migrations_are_the_initial_slice_plus_one_additive_option_column_update()
    {
        using var context = CreateContext();
        var migrations = context.Database.GetMigrations().ToArray();

        // Two migrations only: the initial slice and the additive option-identity change. Asserting the
        // exact count is what keeps the schema reviewable as a unit instead of an open-ended chain.
        Assert.Equal(2, migrations.Length);
        Assert.EndsWith("_InitialShipping", migrations[0], StringComparison.Ordinal);
        Assert.EndsWith("_OptionLevelRatePolicy", migrations[1], StringComparison.Ordinal);
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
