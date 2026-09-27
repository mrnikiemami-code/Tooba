using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// پوشش bind مگامنو به رده و resolver مسیر canonical.
/// </summary>
[Collection("PostgresSerial")]
public sealed class CatalogCategoryMegaMenuTests : IAsyncLifetime
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
                .WithDatabase("tooba_catalog_mega_a")
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
    public async Task Bind_category_uses_canonical_slug_and_menu_parent_independent_from_taxonomy()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-mega-a", "tenant-mega-a"));
        await using var db = CreateCatalogDb(cs, commerce);
        await db.Database.EnsureCreatedAsync();
        var dir = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var root = await dir.CreateCategoryAsync(null, new Dictionary<string, string> { ["fa-IR"] = "دیجیتال" }, CancellationToken.None);
        var childTaxonomy = await dir.CreateCategoryAsync(
            root.CategoryId,
            new Dictionary<string, string> { ["fa-IR"] = "موبایل" },
            CancellationToken.None);
        await dir.UpsertCategoryTranslationAsync(
            childTaxonomy.CategoryId,
            new CategoryTranslationUpsertRequest("fa-IR", "موبایل", "mobile-phones", null, null, null, null, null),
            CancellationToken.None);
        await dir.PublishCategoryAsync(root.CategoryId, CancellationToken.None);
        await dir.PublishCategoryAsync(childTaxonomy.CategoryId, CancellationToken.None);

        await dir.UpsertCategoryMegaMenuBindingAsync(
            root.CategoryId,
            "fa-IR",
            new CategoryMegaMenuBindingInput(null, 0, true, false, null, null, null, null, null),
            CancellationToken.None);

        // Child taxonomy parent is root, but menu parent is null (root of presentation) — independence check.
        await dir.UpsertCategoryMegaMenuBindingAsync(
            childTaxonomy.CategoryId,
            "fa-IR",
            new CategoryMegaMenuBindingInput(null, 1, true, false, null, null, "گوشی", null, null),
            CancellationToken.None);

        var config = await dir.GetCategoryMegaMenuConfigurationAsync(childTaxonomy.CategoryId, "fa-IR", CancellationToken.None);
        Assert.True(config.IsBound);
        Assert.Equal("گوشی", config.DisplayTitle);
        Assert.Equal("/fa/category/mobile-phones", config.DestinationPreview);
        Assert.Null(config.ParentMegaMenuItemId);

        var menu = await dir.GetStorefrontMegaMenuAsync("fa-IR", CancellationToken.None);
        Assert.Equal(2, menu.Count);
        Assert.Contains(menu, x => x.Destination == "/fa/category/mobile-phones");

        await dir.UpsertCategoryTranslationAsync(
            childTaxonomy.CategoryId,
            new CategoryTranslationUpsertRequest("fa-IR", "موبایل", "mobile-new-slug", null, null, null, null, null),
            CancellationToken.None);

        var menuAfterSlug = await dir.GetStorefrontMegaMenuAsync("fa-IR", CancellationToken.None);
        var childItem = menuAfterSlug.Single(x => x.CategoryId == childTaxonomy.CategoryId);
        Assert.Equal("/fa/category/mobile-new-slug", childItem.Destination);
        Assert.Equal("گوشی", childItem.Title);
    }

    [SkippableFact]
    public async Task Unbind_does_not_delete_category_and_hides_from_storefront_menu()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-mega-b", "tenant-mega-b"));
        await using var db = CreateCatalogDb(cs, commerce);
        await db.Database.EnsureCreatedAsync();
        var dir = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var category = await dir.CreateCategoryAsync(null, new Dictionary<string, string> { ["fa-IR"] = "الکترونیک" }, CancellationToken.None);
        await dir.PublishCategoryAsync(category.CategoryId, CancellationToken.None);

        await dir.UpsertCategoryMegaMenuBindingAsync(
            category.CategoryId,
            "fa-IR",
            new CategoryMegaMenuBindingInput(null, 0, true, false, null, null, null, null, null),
            CancellationToken.None);

        Assert.Single(await dir.GetStorefrontMegaMenuAsync("fa-IR", CancellationToken.None));

        await dir.RemoveCategoryMegaMenuBindingAsync(category.CategoryId, CancellationToken.None);
        Assert.True(await db.Categories.AnyAsync(x => x.CategoryId == category.CategoryId));
        Assert.False(await db.MegaMenuItems.AnyAsync(x => x.CategoryId == category.CategoryId));
        Assert.Empty(await dir.GetStorefrontMegaMenuAsync("fa-IR", CancellationToken.None));
    }

    [SkippableFact]
    public async Task MegaMenuDirectory_result_codes_for_missing_placement_children_and_unbound_preview()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-mega-c", "tenant-mega-c"));
        await using var db = CreateCatalogDb(cs, commerce);
        await db.Database.EnsureCreatedAsync();
        var guard = new OpenCatalogUseCaseGuard();
        var catalog = new CatalogDirectory(db, guard);
        var mega = new MegaMenuDirectory(db, guard);

        var missing = await mega.GetCategoryConfigurationAsync(Guid.NewGuid(), "fa-IR", CancellationToken.None);
        Assert.True(missing.IsFailure);
        Assert.Equal(CatalogErrorCodes.MegaMenuCategoryMissing, missing.FirstError.Code);

        var root = await catalog.CreateCategoryAsync(null, new Dictionary<string, string> { ["fa-IR"] = "ریشه" }, CancellationToken.None);
        var child = await catalog.CreateCategoryAsync(null, new Dictionary<string, string> { ["fa-IR"] = "فرزند" }, CancellationToken.None);
        await catalog.UpsertCategoryTranslationAsync(
            root.CategoryId,
            new CategoryTranslationUpsertRequest("fa-IR", "ریشه", "root-mega", null, null, null, null, null),
            CancellationToken.None);
        await catalog.PublishCategoryAsync(root.CategoryId, CancellationToken.None);

        var unbound = await mega.GetCategoryConfigurationAsync(root.CategoryId, "fa-IR", CancellationToken.None);
        Assert.True(unbound.IsSuccess);
        Assert.False(unbound.Value.IsBound);

        var upsertRoot = await mega.UpsertBindingAsync(
            root.CategoryId,
            "fa-IR",
            new CategoryMegaMenuBindingInput(null, 0, true, false, null, null, null, null, null),
            CancellationToken.None);
        Assert.True(upsertRoot.IsSuccess);

        var options = await mega.ListPlacementOptionsAsync(child.CategoryId, "fa-IR", CancellationToken.None);
        Assert.Contains(options, o => o.CategoryId == root.CategoryId);

        var badParent = Guid.NewGuid();
        var invalidPlacement = await mega.UpsertBindingAsync(
            child.CategoryId,
            "fa-IR",
            new CategoryMegaMenuBindingInput(badParent, 1, true, false, null, null, null, null, null),
            CancellationToken.None);
        Assert.True(invalidPlacement.IsFailure);
        Assert.Equal(CatalogErrorCodes.MegaMenuPlacementInvalid, invalidPlacement.FirstError.Code);

        var upsertChild = await mega.UpsertBindingAsync(
            child.CategoryId,
            "fa-IR",
            new CategoryMegaMenuBindingInput(
                (await db.MegaMenuItems.SingleAsync(x => x.CategoryId == root.CategoryId)).MegaMenuItemId,
                1,
                true,
                false,
                null,
                null,
                null,
                null,
                null),
            CancellationToken.None);
        Assert.True(upsertChild.IsSuccess);

        var removeParent = await mega.RemoveBindingAsync(root.CategoryId, CancellationToken.None);
        Assert.True(removeParent.IsFailure);
        Assert.Equal(CatalogErrorCodes.MegaMenuRemoveHasChildren, removeParent.FirstError.Code);

        var removeAbsent = await mega.RemoveBindingAsync(Guid.NewGuid(), CancellationToken.None);
        Assert.True(removeAbsent.IsSuccess);

        var storefront = await mega.GetStorefrontMenuAsync("fa-IR", CancellationToken.None);
        Assert.Contains(storefront, x => x.CategoryId == root.CategoryId);
        Assert.DoesNotContain(storefront, x => x.CategoryId == child.CategoryId); // child not published
    }

    private static CatalogDbContext CreateCatalogDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new CatalogOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<CatalogDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, CatalogDbContext.Schema, typeof(CatalogDbContext));
        options.AddInterceptors(interceptor);
        return new CatalogDbContext(options.Options);
    }
}
