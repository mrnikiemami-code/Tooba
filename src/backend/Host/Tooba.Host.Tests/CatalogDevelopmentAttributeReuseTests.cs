using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Models;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Development;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-SHIPPING-ONE-LEVEL-TABS-112 — narrow Development-only attribute reuse seam
/// (<see cref="CatalogDevelopmentAttributeReuse"/>) used by the legacy workspace demo seed.
/// <para>
/// This is the focused regression coverage for the TB-111 startup collision
/// <c>catalog.attribute.code.duplicate</c>: an empty catalog must still create the required attribute
/// normally, an already existing semantically compatible attribute must be reused with the SAME persistent
/// identifier, an incompatible one must be refused before any downstream write, repeated preparation must be
/// idempotent, and the reused axis/option identifiers must stay valid for variant creation.
/// </para>
/// </summary>
[Collection("PostgresSerial")]
public sealed class CatalogDevelopmentAttributeReuseTests : IAsyncLifetime
{
    private const string EmptyCatalogCode = "color-reuse-empty";
    private const string CompatibleCode = "color-reuse-compatible";
    private const string IncompatibleCode = "color-reuse-incompatible";
    private const string RepeatCode = "color-reuse-repeat";
    private const string VariantCode = "color-reuse-variant";

    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("tooba_attribute_reuse")
                .WithUsername("tooba")
                .WithPassword("dev-placeholder")
                .Build();
            await _container.StartAsync();
            _dockerAvailable = true;
        }
        catch (Exception)
        {
            _dockerAvailable = false;
        }
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    // -----------------------------------------------------------------------------------------------
    // Pure decision rules: the diagnostic may name ONLY the code and the mismatched rule(s).
    // -----------------------------------------------------------------------------------------------

    [Fact]
    public void Incompatibility_diagnostic_names_only_the_code_and_the_mismatched_rules()
    {
        var incompatible = Definition("color", CatalogAttributeValueKind.Text, isVariantAxis: false, isActive: false);
        var message = CatalogDevelopmentAttributeReuse.DescribeIncompatibility(
            "Color",
            incompatible,
            CatalogAttributeValueKind.Enumeration,
            isVariantAxis: true);

        Assert.Equal(
            $"{CatalogDevelopmentAttributeReuse.IncompatibleReuseCode}: code=color; rules=value-kind,variant-axis,active",
            message);
    }

    [Fact]
    public void Compatible_definition_reports_no_incompatibility()
    {
        var compatible = Definition("color", CatalogAttributeValueKind.Enumeration, isVariantAxis: true, isActive: true);

        Assert.Null(CatalogDevelopmentAttributeReuse.DescribeIncompatibility(
            "color",
            compatible,
            CatalogAttributeValueKind.Enumeration,
            isVariantAxis: true));

        // Only the mismatching rule is named, not all of them.
        Assert.Equal(
            $"{CatalogDevelopmentAttributeReuse.IncompatibleReuseCode}: code=color; rules=variant-axis",
            CatalogDevelopmentAttributeReuse.DescribeIncompatibility(
                "color",
                compatible,
                CatalogAttributeValueKind.Enumeration,
                isVariantAxis: false));
    }

    // -----------------------------------------------------------------------------------------------
    // Operational behavior against a real PostgreSQL catalog schema.
    // -----------------------------------------------------------------------------------------------

    [SkippableFact]
    public async Task Empty_catalog_creates_the_required_attribute_through_the_catalog_api()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateCatalogDb(_container.GetConnectionString());
        await db.Database.EnsureCreatedAsync();
        var catalog = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var definitionId = await CatalogDevelopmentAttributeReuse.EnsureCompatibleDefinitionAsync(
            catalog,
            EmptyCatalogCode,
            CatalogAttributeValueKind.Enumeration,
            isVariantAxis: true,
            ColorNames,
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, definitionId);
        var created = await db.AttributeDefinitions.AsNoTracking().SingleAsync(x => x.Code == EmptyCatalogCode);
        Assert.Equal(definitionId, created.DefinitionId);
        Assert.Equal(CatalogAttributeValueKind.Enumeration, created.ValueKind);
        Assert.True(created.IsVariantAxis);
    }

    [SkippableFact]
    public async Task Existing_compatible_attribute_is_reused_with_the_same_persistent_identifier()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateCatalogDb(_container.GetConnectionString());
        await db.Database.EnsureCreatedAsync();
        var catalog = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        // Stand-in for the Catalog attribute-schema Development seed: it owns the code first.
        var ownedBySchemaSeed = await catalog.CreateAttributeDefinitionAsync(
            CompatibleCode,
            CatalogAttributeValueKind.Enumeration,
            isVariantAxis: true,
            ColorNames,
            CancellationToken.None);

        var reused = await CatalogDevelopmentAttributeReuse.EnsureCompatibleDefinitionAsync(
            catalog,
            CompatibleCode,
            CatalogAttributeValueKind.Enumeration,
            isVariantAxis: true,
            ColorNames,
            CancellationToken.None);

        Assert.Equal(ownedBySchemaSeed, reused);
        Assert.Equal(1, await db.AttributeDefinitions.AsNoTracking().CountAsync(x => x.Code == CompatibleCode));
    }

    [SkippableFact]
    public async Task Existing_incompatible_attribute_is_refused_before_any_downstream_write()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateCatalogDb(_container.GetConnectionString());
        await db.Database.EnsureCreatedAsync();
        var catalog = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var foreign = await catalog.CreateAttributeDefinitionAsync(
            IncompatibleCode,
            CatalogAttributeValueKind.Text,
            isVariantAxis: false,
            ColorNames,
            CancellationToken.None);
        var definitionsBefore = await db.AttributeDefinitions.AsNoTracking().CountAsync();

        var refused = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CatalogDevelopmentAttributeReuse.EnsureCompatibleDefinitionAsync(
                catalog,
                IncompatibleCode,
                CatalogAttributeValueKind.Enumeration,
                isVariantAxis: true,
                ColorNames,
                CancellationToken.None));

        Assert.Equal(
            $"{CatalogDevelopmentAttributeReuse.IncompatibleReuseCode}: code={IncompatibleCode}; rules=value-kind,variant-axis",
            refused.Message);

        // Nothing was rewritten, replaced or appended: same definition, same kind, still no options.
        Assert.Equal(definitionsBefore, await db.AttributeDefinitions.AsNoTracking().CountAsync());
        var untouched = await db.AttributeDefinitions.AsNoTracking().SingleAsync(x => x.Code == IncompatibleCode);
        Assert.Equal(foreign, untouched.DefinitionId);
        Assert.Equal(CatalogAttributeValueKind.Text, untouched.ValueKind);
        Assert.False(untouched.IsVariantAxis);
        Assert.Equal(0, await db.AttributeOptions.AsNoTracking().CountAsync(x => x.DefinitionId == foreign));

        // The refusal carries no display text, localized name or other unrelated data.
        Assert.DoesNotContain("رنگ", refused.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Color", refused.Message, StringComparison.Ordinal);
    }

    [SkippableFact]
    public async Task Repeated_preparation_is_idempotent_and_keeps_definition_and_option_identity()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateCatalogDb(_container.GetConnectionString());
        await db.Database.EnsureCreatedAsync();
        var catalog = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var firstDefinition = await CatalogDevelopmentAttributeReuse.EnsureCompatibleDefinitionAsync(
            catalog, RepeatCode, CatalogAttributeValueKind.Enumeration, true, ColorNames, CancellationToken.None);
        var firstOption = await CatalogDevelopmentAttributeReuse.EnsureCompatibleOptionAsync(
            catalog, db, firstDefinition, "black", BlackNames, CancellationToken.None);

        // A second preparation pass (the real legacy seed runs once per startup, but the helper must be safe).
        var secondDefinition = await CatalogDevelopmentAttributeReuse.EnsureCompatibleDefinitionAsync(
            catalog, RepeatCode, CatalogAttributeValueKind.Enumeration, true, ColorNames, CancellationToken.None);
        var secondOption = await CatalogDevelopmentAttributeReuse.EnsureCompatibleOptionAsync(
            catalog, db, secondDefinition, "black", BlackNames, CancellationToken.None);

        Assert.Equal(firstDefinition, secondDefinition);
        Assert.Equal(firstOption, secondOption);
        Assert.Equal(1, await db.AttributeDefinitions.AsNoTracking().CountAsync(x => x.Code == RepeatCode));
        Assert.Equal(
            1,
            await db.AttributeOptions.AsNoTracking().CountAsync(
                x => x.DefinitionId == firstDefinition && x.Code == "black"));
    }

    [SkippableFact]
    public async Task Reused_axis_and_option_identifiers_stay_valid_for_variant_creation()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateCatalogDb(_container.GetConnectionString());
        await db.Database.EnsureCreatedAsync();
        var catalog = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        // The Catalog attribute-schema seed owns the definition AND the option first.
        var owned = await catalog.CreateAttributeDefinitionAsync(
            VariantCode, CatalogAttributeValueKind.Enumeration, isVariantAxis: true, ColorNames, CancellationToken.None);
        var ownedOption = await catalog.AddAttributeOptionAsync(
            owned, "black", BlackNames, CancellationToken.None);

        var colorId = await CatalogDevelopmentAttributeReuse.EnsureCompatibleDefinitionAsync(
            catalog, VariantCode, CatalogAttributeValueKind.Enumeration, true, ColorNames, CancellationToken.None);
        var black = await CatalogDevelopmentAttributeReuse.EnsureCompatibleOptionAsync(
            catalog, db, colorId, "black", BlackNames, CancellationToken.None);
        Assert.Equal(owned, colorId);
        Assert.Equal(ownedOption, black);

        var product = await catalog.CreateProductAsync(
            CatalogProductKind.PhysicalGood,
            "attribute-reuse-variant-product",
            null,
            new Dictionary<string, string> { ["fa-IR"] = "پیراهن بازاستفاده" },
            CancellationToken.None);
        await catalog.SetProductVariantAxesAsync(product.ProductId, [colorId], CancellationToken.None);
        var variant = await catalog.CreateVariantAsync(
            product.ProductId,
            "REUSE-BLK",
            [(colorId, "ignored", black)],
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, variant.VariantId);
        var axes = await db.ProductVariantAxes.AsNoTracking()
            .Where(x => x.ProductId == product.ProductId)
            .ToListAsync();
        var axis = Assert.Single(axes);
        Assert.Equal(colorId, axis.DefinitionId);
        Assert.Single(
            await db.VariantAttributeValues.AsNoTracking()
                .Where(x => x.VariantId == variant.VariantId && x.DefinitionId == colorId)
                .ToListAsync());
    }

    private static readonly Dictionary<string, string> ColorNames = new()
    {
        ["fa-IR"] = "رنگ",
        ["en-US"] = "Color",
    };

    private static readonly Dictionary<string, string> BlackNames = new()
    {
        ["fa-IR"] = "سیاه",
        ["en-US"] = "Black",
    };

    private static AttributeDefinitionView Definition(
        string code,
        CatalogAttributeValueKind valueKind,
        bool isVariantAxis,
        bool isActive) =>
        new(
            Guid.NewGuid(),
            code,
            valueKind,
            isVariantAxis,
            Unit: null,
            IsRequired: false,
            IsFilterable: false,
            IsComparable: false,
            IsMultivalue: false,
            DisplayOrder: 0,
            ValidationMin: null,
            ValidationMax: null,
            ValidationMaxLength: null,
            IsActive: isActive,
            DateTimeOffset.UtcNow);

    private static CatalogDbContext CreateCatalogDb(string connectionString)
    {
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-attribute-reuse", "tenant-attribute-reuse"));
        var modules = new IOutboxModuleRegistration[] { new CatalogOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<CatalogDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, CatalogDbContext.Schema, typeof(CatalogDbContext));
        options.AddInterceptors(interceptor);
        return new CatalogDbContext(options.Options);
    }
}
