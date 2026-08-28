using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
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
