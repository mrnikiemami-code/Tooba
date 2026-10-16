using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Domain.Products;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Development;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-SHIPPING-ONE-LEVEL-TABS-108 — narrow sparse-safe default-variant seam
/// (<see cref="ICatalogDirectory.SetProductVariantDefaultAsync"/>) and its use by the
/// Catalog attribute-schema Development seed.
/// <para>
/// The seed creates a SPARSE (diagonal) two-variant set on purpose; the canonical
/// <see cref="ICatalogDirectory.ApplyProductVariantMatrixAsync"/> is Cartesian and would create or archive
/// variants, so the invariant must be reachable without it. These tests prove the seam keeps the variant set,
/// axes and fingerprints intact, keeps exactly one default, is idempotent, refuses invalid targets and lets the
/// existing variant-readiness rule pass; then that the real seed reaches publish through it.
/// </para>
/// </summary>
[Collection("PostgresSerial")]
public sealed class CatalogSparseDefaultVariantTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("tooba_sparse_default_variant")
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

    [SkippableFact]
    public async Task Sparse_set_keeps_identity_and_gains_exactly_one_default()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateCatalogDb(_container.GetConnectionString());
        await db.Database.EnsureCreatedAsync();
        var catalog = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var fixture = await CreateSparseTwoVariantProductAsync(catalog, db, "sparse-default", "PHONE-A", "PHONE-B");
        var beforeCount = await db.Variants.CountAsync(x => x.ProductId == fixture.ProductId);
        var beforeFingerprints = await db.Variants.AsNoTracking()
            .Where(x => x.ProductId == fixture.ProductId)
            .Select(x => x.CombinationFingerprint)
            .OrderBy(x => x)
            .ToListAsync();
        var beforeAxes = await db.ProductVariantAxes.AsNoTracking()
            .Where(x => x.ProductId == fixture.ProductId)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => x.DefinitionId)
            .ToListAsync();

        // Before: no default exists, so the EXISTING readiness rule already refuses the product (TB-106's observation).
        var readinessBefore = await catalog.GetProductVariantReadinessAsync(fixture.ProductId, CancellationToken.None);
        Assert.False(readinessBefore.IsValid);
        Assert.True(readinessBefore.NoDefaultVariant);
        Assert.Equal(2, beforeCount);

        await catalog.SetProductVariantDefaultAsync(fixture.ProductId, fixture.FirstVariantId, CancellationToken.None);

        var afterVariants = await db.Variants.AsNoTracking()
            .Where(x => x.ProductId == fixture.ProductId)
            .ToListAsync();
        Assert.Equal(2, afterVariants.Count);
        Assert.Equal(beforeFingerprints, afterVariants.Select(x => x.CombinationFingerprint).OrderBy(x => x).ToList());
        Assert.Equal(
            beforeAxes,
            await db.ProductVariantAxes.AsNoTracking()
                .Where(x => x.ProductId == fixture.ProductId)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => x.DefinitionId)
                .ToListAsync());
        Assert.All(afterVariants, v => Assert.Equal(CatalogPublicationStatus.Draft, v.Status));
        Assert.Equal("PHONE-A", afterVariants.Single(x => x.VariantId == fixture.FirstVariantId).CatalogCodeSeam);
        Assert.Single(afterVariants, x => x.IsDefault);
        Assert.True(afterVariants.Single(x => x.IsDefault).VariantId == fixture.FirstVariantId);

        // The existing rule now passes, and the seam itself changed nothing about the rule's inputs.
        var readinessAfter = await catalog.GetProductVariantReadinessAsync(fixture.ProductId, CancellationToken.None);
        Assert.True(readinessAfter.IsValid);
        Assert.False(readinessAfter.NoDefaultVariant);
        Assert.Empty(readinessAfter.MissingAxes);
        Assert.Empty(readinessAfter.InvalidVariants);
        Assert.Empty(readinessAfter.DuplicateCombinations);

        // Idempotent: re-assigning the same variant is a true no-op (no extra history, no oscillation).
        await catalog.SetProductVariantDefaultAsync(fixture.ProductId, fixture.FirstVariantId, CancellationToken.None);
        var afterRepeat = await db.Variants.AsNoTracking()
            .Where(x => x.ProductId == fixture.ProductId)
            .ToListAsync();
        Assert.Equal(2, afterRepeat.Count);
        Assert.Single(afterRepeat, x => x.IsDefault);
        Assert.Equal(fixture.FirstVariantId, afterRepeat.Single(x => x.IsDefault).VariantId);
        Assert.Equal(
            1,
            await db.ProductHistoryEntries.CountAsync(
                x => x.ProductId == fixture.ProductId
                    && x.EventType == ProductHistoryRules.EventVariantsChanged));

        // Switching the default clears the previous one and still leaves exactly one (this is the only other write).
        await catalog.SetProductVariantDefaultAsync(fixture.ProductId, fixture.SecondVariantId, CancellationToken.None);
        var afterSwitch = await db.Variants.AsNoTracking()
            .Where(x => x.ProductId == fixture.ProductId)
            .ToListAsync();
        Assert.Equal(2, afterSwitch.Count);
        Assert.Single(afterSwitch, x => x.IsDefault);
        Assert.Equal(fixture.SecondVariantId, afterSwitch.Single(x => x.IsDefault).VariantId);
        Assert.False(afterSwitch.Single(x => x.VariantId == fixture.FirstVariantId).IsDefault);
    }

    [SkippableFact]
    public async Task Invalid_targets_are_refused_and_leave_state_untouched()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateCatalogDb(_container.GetConnectionString());
        await db.Database.EnsureCreatedAsync();
        var catalog = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var fixture = await CreateSparseTwoVariantProductAsync(catalog, db, "sparse-refusal", "REF-A", "REF-B");
        var other = await CreateSparseTwoVariantProductAsync(catalog, db, "sparse-other", "OTH-A", "OTH-B");

        // Missing product.
        var missingProduct = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            catalog.SetProductVariantDefaultAsync(Guid.NewGuid(), fixture.FirstVariantId, CancellationToken.None));
        Assert.Equal(CatalogErrorCodes.ProductMissing, missingProduct.Message);

        // Missing variant on the requested product.
        var missingVariant = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            catalog.SetProductVariantDefaultAsync(fixture.ProductId, Guid.NewGuid(), CancellationToken.None));
        Assert.Equal(CatalogErrorCodes.VariantDefaultMissing, missingVariant.Message);

        // Mismatched variant: it belongs to another product.
        var mismatched = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            catalog.SetProductVariantDefaultAsync(fixture.ProductId, other.FirstVariantId, CancellationToken.None));
        Assert.Equal(CatalogErrorCodes.VariantDefaultMissing, mismatched.Message);

        // Archived target.
        var archived = await db.Variants.SingleAsync(x => x.VariantId == fixture.SecondVariantId);
        archived.SetStatus(CatalogPublicationStatus.Archived, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();
        var forbidden = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            catalog.SetProductVariantDefaultAsync(fixture.ProductId, fixture.SecondVariantId, CancellationToken.None));
        Assert.Equal(CatalogErrorCodes.VariantArchivedCannotBeDefault, forbidden.Message);

        // Every refusal left the sparse set exactly as it was: two variants, none default, axes intact.
        var variants = await db.Variants.AsNoTracking()
            .Where(x => x.ProductId == fixture.ProductId)
            .ToListAsync();
        Assert.Equal(2, variants.Count);
        Assert.DoesNotContain(variants, x => x.IsDefault);
        Assert.Equal(
            2,
            await db.ProductVariantAxes.AsNoTracking().CountAsync(x => x.ProductId == fixture.ProductId));
        Assert.Equal(
            0,
            await db.ProductHistoryEntries.CountAsync(
                x => x.ProductId == fixture.ProductId
                    && x.EventType == ProductHistoryRules.EventVariantsChanged));
    }

    [SkippableFact]
    public async Task Attribute_schema_seed_publishes_its_sparse_product_with_one_default_and_is_repeat_safe()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateCatalogDb(_container.GetConnectionString());
        await db.Database.EnsureCreatedAsync();
        var catalog = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        // The seed resolves only ICatalogDirectory + CatalogDbContext; the sellable enricher is optional
        // (GetService), so it is deliberately absent here: this test covers the seed's own variant defaulting
        // and publish, not Offer/Pricing/Inventory/Tax provisioning.
        await using var provider = new ServiceCollection()
            .AddSingleton<ICatalogDirectory>(catalog)
            .AddSingleton(db)
            .BuildServiceProvider();

        // Before the seam existed this call threw InvalidOperationException("محصول برای انتشار آماده نیست.")
        // because ready variant readiness requires a default variant (TB-106 observed missing=[variants]).
        await CatalogAttributeSchemaDevelopmentSeed.ApplyAsync(provider, CancellationToken.None);

        var product = await db.Products.SingleAsync(
            x => x.SlugSeam == CatalogAttributeSchemaDevelopmentSeed.DemoProductSlug);
        Assert.Equal(CatalogPublicationStatus.Published, product.Status);

        var variants = await db.Variants.AsNoTracking()
            .Where(x => x.ProductId == product.ProductId)
            .ToListAsync();
        Assert.Equal(2, variants.Count);
        Assert.Single(variants, x => x.IsDefault);
        var defaultVariant = variants.Single(x => x.IsDefault);
        Assert.Equal("PHONE-BLK-128", defaultVariant.CatalogCodeSeam);
        Assert.Equal(
            ["PHONE-BLK-128", "PHONE-BLU-256"],
            variants.Select(x => x.CatalogCodeSeam).OrderBy(x => x, StringComparer.Ordinal).ToList());

        var axes = await db.ProductVariantAxes.AsNoTracking()
            .Where(x => x.ProductId == product.ProductId)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync();
        Assert.Equal(2, axes.Count);
        var axisIds = axes.Select(a => a.DefinitionId).ToList();
        var axisCodes = (await db.AttributeDefinitions.AsNoTracking()
                .Where(x => axisIds.Contains(x.DefinitionId))
                .Select(x => x.Code)
                .ToListAsync())
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
        Assert.Equal(["color", "storage"], axisCodes);

        var readiness = await catalog.GetProductVariantReadinessAsync(product.ProductId, CancellationToken.None);
        Assert.True(readiness.IsValid);
        Assert.False(readiness.NoDefaultVariant);

        // Repeat safety: a second seed pass must not add variants, re-create anything or oscillate the default.
        await CatalogAttributeSchemaDevelopmentSeed.ApplyAsync(provider, CancellationToken.None);
        var afterRepeat = await db.Variants.AsNoTracking()
            .Where(x => x.ProductId == product.ProductId)
            .ToListAsync();
        Assert.Equal(2, afterRepeat.Count);
        Assert.Single(afterRepeat, x => x.IsDefault);
        Assert.Equal(defaultVariant.VariantId, afterRepeat.Single(x => x.IsDefault).VariantId);
        Assert.Equal(
            2,
            await db.ProductVariantAxes.AsNoTracking().CountAsync(x => x.ProductId == product.ProductId));
    }

    private static async Task<SparseProductFixture> CreateSparseTwoVariantProductAsync(
        CatalogDirectory catalog,
        CatalogDbContext db,
        string slug,
        string firstCode,
        string secondCode)
    {
        var root = await catalog.CreateCategoryAsync(
            null, new Dictionary<string, string> { ["fa-IR"] = $"کالای دیجیتال {slug}" }, CancellationToken.None);
        await catalog.PublishCategoryAsync(root.CategoryId, CancellationToken.None);
        var mid = await catalog.CreateCategoryAsync(
            root.CategoryId, new Dictionary<string, string> { ["fa-IR"] = $"موبایل {slug}" }, CancellationToken.None);
        await catalog.PublishCategoryAsync(mid.CategoryId, CancellationToken.None);
        var category = await catalog.CreateCategoryAsync(
            mid.CategoryId, new Dictionary<string, string> { ["fa-IR"] = $"گوشی {slug}" }, CancellationToken.None);
        await catalog.PublishCategoryAsync(category.CategoryId, CancellationToken.None);

        var colorId = await catalog.CreateAttributeDefinitionAsync(
            $"color-{slug}", CatalogAttributeValueKind.Enumeration, true,
            new Dictionary<string, string> { ["fa-IR"] = $"رنگ {slug}" }, CancellationToken.None);
        var storageId = await catalog.CreateAttributeDefinitionAsync(
            $"storage-{slug}", CatalogAttributeValueKind.Enumeration, true,
            new Dictionary<string, string> { ["fa-IR"] = $"حافظه {slug}" }, CancellationToken.None);
        await catalog.BindCategoryAttributeAsync(
            category.CategoryId, colorId, 10,
            new CategoryAttributeAssignmentFlags(false, true, true, false), CancellationToken.None);
        await catalog.BindCategoryAttributeAsync(
            category.CategoryId, storageId, 20,
            new CategoryAttributeAssignmentFlags(false, true, true, false), CancellationToken.None);

        var black = await catalog.AddAttributeOptionAsync(
            colorId, "black", new Dictionary<string, string> { ["fa-IR"] = "مشکی" }, CancellationToken.None);
        var blue = await catalog.AddAttributeOptionAsync(
            colorId, "blue", new Dictionary<string, string> { ["fa-IR"] = "آبی" }, CancellationToken.None);
        var gb128 = await catalog.AddAttributeOptionAsync(
            storageId, "128gb", new Dictionary<string, string> { ["fa-IR"] = "۱۲۸" }, CancellationToken.None);
        var gb256 = await catalog.AddAttributeOptionAsync(
            storageId, "256gb", new Dictionary<string, string> { ["fa-IR"] = "۲۵۶" }, CancellationToken.None);

        var product = await catalog.CreateProductAsync(
            CatalogProductKind.PhysicalGood, slug, null,
            new Dictionary<string, string> { ["fa-IR"] = "گوشی پراکنده" }, CancellationToken.None);
        await catalog.AssignCategoryAsync(product.ProductId, category.CategoryId, CancellationToken.None);
        await catalog.SetProductVariantAxesAsync(product.ProductId, [colorId, storageId], CancellationToken.None);

        // Deliberately a sparse diagonal: 2 axes x 2 options would allow 4 combinations, but only 2 exist.
        var first = await catalog.CreateVariantAsync(
            product.ProductId, firstCode,
            [(colorId, "ignored", black), (storageId, "ignored", gb128)], CancellationToken.None);
        var second = await catalog.CreateVariantAsync(
            product.ProductId, secondCode,
            [(colorId, "ignored", blue), (storageId, "ignored", gb256)], CancellationToken.None);

        await db.SaveChangesAsync();
        return new SparseProductFixture(product.ProductId, first.VariantId, second.VariantId);
    }

    private static CatalogDbContext CreateCatalogDb(string connectionString)
    {
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-sparse-default", "tenant-sparse-default"));
        var modules = new IOutboxModuleRegistration[] { new CatalogOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<CatalogDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, CatalogDbContext.Schema, typeof(CatalogDbContext));
        options.AddInterceptors(interceptor);
        return new CatalogDbContext(options.Options);
    }

    private sealed record SparseProductFixture(Guid ProductId, Guid FirstVariantId, Guid SecondVariantId);
}
