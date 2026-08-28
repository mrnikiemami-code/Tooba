using System.Reflection;
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
/// TB-P07-T004: foundation درخت/ترجمه/مسیر رده Catalog.
/// </summary>
[Collection("PostgresSerial")]
public sealed class CatalogCategoryFoundationTests : IAsyncLifetime
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
                .WithDatabase("tooba_category_foundation")
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

    [Fact]
    public void Category_has_no_NameFa_or_NameEn_properties()
    {
        var names = typeof(CatalogCategory).GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain("NameFa", names);
        Assert.DoesNotContain("NameEn", names);
        Assert.DoesNotContain("Name", names);
        Assert.Contains("SortOrder", names);
        Assert.Contains("IsVisible", names);
        Assert.Contains("ImageMediaAssetId", names);
        Assert.Contains("IconMediaAssetId", names);
    }

    [Fact]
    public void Slug_normalizer_produces_kebab_lowercase()
    {
        Assert.Equal("summer-shirts", CatalogCategorySlugNormalizer.NormalizeSlug("  Summer Shirts  "));
        Assert.Equal("fa-IR", CatalogCategorySlugNormalizer.NormalizeLocale(" fa-IR "));
    }

    [SkippableFact]
    public async Task Category_foundation_tree_translation_route_and_move_rules()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-cat", "tenant-cat"));
        await using var db = CreateCatalogDb(cs, commerce);
        await db.Database.EnsureCreatedAsync();
        var dir = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var root = await dir.CreateCategoryAsync(
            null,
            new Dictionary<string, string> { ["fa-IR"] = "الکترونیک", ["en-US"] = "Electronics" },
            CancellationToken.None);
        Assert.Equal(CatalogPublicationStatus.Draft, root.Status);

        var child = await dir.CreateCategoryAsync(
            root.CategoryId,
            new Dictionary<string, string> { ["fa-IR"] = "موبایل" },
            CancellationToken.None);
        Assert.Equal(root.CategoryId, child.ParentCategoryId);

        var workspace = await dir.GetCategoryWorkspaceAsync(root.CategoryId, null, CancellationToken.None);
        Assert.NotNull(workspace);
        Assert.Equal(2, workspace!.Translations.Count);
        Assert.Contains(workspace.Translations, t => t.Locale == "fa-IR" && t.Name == "الکترونیک");
        Assert.Contains(workspace.Translations, t => t.Locale == "en-US" && t.Slug == "electronics");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dir.UpsertCategoryTranslationAsync(
                child.CategoryId,
                new CategoryTranslationUpsertRequest("fa-IR", "تکراری", "الکترونیک"),
                CancellationToken.None));

        await dir.UpsertCategoryTranslationAsync(
            child.CategoryId,
            new CategoryTranslationUpsertRequest("en-US", "Mobile", "الکترونیک"),
            CancellationToken.None);
        var childEn = await dir.GetCategoryWorkspaceAsync(child.CategoryId, "en-US", CancellationToken.None);
        Assert.Single(childEn!.Translations);
        Assert.Equal("الکترونیک", childEn.Translations[0].Slug);

        var beforeSlug = workspace.Translations.Single(t => t.Locale == "fa-IR").Slug;
        await dir.UpsertCategoryTranslationAsync(
            root.CategoryId,
            new CategoryTranslationUpsertRequest("fa-IR", "الکترونیک", "electronics-fa", "کوتاه", null, "seo", "desc", "kw"),
            CancellationToken.None);
        var history = await db.CategorySlugHistories.AsNoTracking()
            .Where(h => h.CategoryId == root.CategoryId && h.Locale == "fa-IR" && h.OldSlug == beforeSlug)
            .ToListAsync();
        Assert.Single(history);

        var redirect = await dir.ResolveCategoryRouteAsync("fa-IR", beforeSlug, forStorefront: false, CancellationToken.None);
        Assert.NotNull(redirect);
        Assert.True(redirect!.IsRedirect);
        Assert.Equal("electronics-fa", redirect.CurrentSlug);
        Assert.Equal("/fa-IR/category/electronics-fa", redirect.CanonicalPath);

        var current = await dir.ResolveCategoryRouteAsync("fa-IR", "electronics-fa", forStorefront: false, CancellationToken.None);
        Assert.NotNull(current);
        Assert.False(current!.IsRedirect);
        Assert.Equal(root.CategoryId, current.CategoryId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dir.MoveCategoryAsync(root.CategoryId, root.CategoryId, null, CancellationToken.None));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dir.MoveCategoryAsync(root.CategoryId, child.CategoryId, null, CancellationToken.None));

        var otherRoot = await dir.CreateCategoryAsync(
            null,
            new Dictionary<string, string> { ["fa-IR"] = "کتاب" },
            CancellationToken.None);
        await dir.MoveCategoryAsync(child.CategoryId, otherRoot.CategoryId, null, CancellationToken.None);
        var moved = await dir.GetCategoryWorkspaceAsync(child.CategoryId, null, CancellationToken.None);
        Assert.Equal(otherRoot.CategoryId, moved!.ParentCategoryId);

        var siblingA = await dir.CreateCategoryAsync(
            otherRoot.CategoryId,
            new Dictionary<string, string> { ["fa-IR"] = "الف" },
            CancellationToken.None);
        var siblingB = await dir.CreateCategoryAsync(
            otherRoot.CategoryId,
            new Dictionary<string, string> { ["fa-IR"] = "ب" },
            CancellationToken.None);
        var underOther = await db.Categories.AsNoTracking()
            .Where(c => c.ParentCategoryId == otherRoot.CategoryId)
            .OrderBy(c => c.SortOrder)
            .Select(c => c.CategoryId)
            .ToListAsync();
        Assert.Equal(3, underOther.Count);
        await dir.ReorderCategorySiblingsAsync(
            otherRoot.CategoryId,
            [siblingB.CategoryId, child.CategoryId, siblingA.CategoryId],
            CancellationToken.None);
        var reordered = await db.Categories.AsNoTracking()
            .Where(c => c.ParentCategoryId == otherRoot.CategoryId)
            .OrderBy(c => c.SortOrder)
            .Select(c => c.CategoryId)
            .ToListAsync();
        Assert.Equal([siblingB.CategoryId, child.CategoryId, siblingA.CategoryId], reordered);

        await dir.PublishCategoryAsync(root.CategoryId, CancellationToken.None);
        await dir.UpdateCategoryCoreAsync(
            root.CategoryId,
            new CategoryCoreUpdateRequest(null, null, true, null, null),
            CancellationToken.None);
        var storefrontOk = await dir.ResolveCategoryRouteAsync("fa-IR", "electronics-fa", forStorefront: true, CancellationToken.None);
        Assert.NotNull(storefrontOk);

        await dir.ArchiveCategoryAsync(root.CategoryId, CancellationToken.None);
        var storefrontBlocked = await dir.ResolveCategoryRouteAsync("fa-IR", "electronics-fa", forStorefront: true, CancellationToken.None);
        Assert.Null(storefrontBlocked);
        var adminStillResolves = await dir.ResolveCategoryRouteAsync("fa-IR", "electronics-fa", forStorefront: false, CancellationToken.None);
        Assert.NotNull(adminStillResolves);

        var tree = await dir.GetCategoryTreeAsync("fa-IR", null, CancellationToken.None);
        Assert.True(tree.Count >= 3);
        Assert.Contains(tree, n => n.Id == otherRoot.CategoryId && n.Name == "کتاب");

        var names = await dir.GetCategoryNamesAsync([otherRoot.CategoryId], CancellationToken.None);
        Assert.Equal("کتاب", names[otherRoot.CategoryId]);

        var searched = await dir.GetCategoryTreeAsync("fa-IR", "کتاب", CancellationToken.None);
        Assert.Contains(searched, n => n.Id == otherRoot.CategoryId);
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
