using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Events;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// تاریخچهٔ append-only محصول Catalog — بدون Offer/Price/Stock و بدون mutation API.
/// </summary>
[Collection("PostgresSerial")]
public sealed class ProductHistoryTests : IAsyncLifetime
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
                .WithDatabase("tooba_product_history")
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
    public void History_rules_expose_persian_section_labels()
    {
        Assert.Equal("عمومی", ProductHistoryRules.SectionLabelFa(ProductHistoryRules.SectionGeneral));
        Assert.Equal("انتشار", ProductHistoryRules.SectionLabelFa(ProductHistoryRules.SectionLifecycle));
        Assert.Equal("محصول منتشر شد", ProductHistoryRules.SummaryPublishedFa);
        Assert.Equal("محصول از بایگانی خارج شد", ProductHistoryRules.SummaryRestoredFa);
        Assert.NotEqual(ProductHistoryRules.EventPublished, ProductHistoryRules.EventRestored);
    }

    [Fact]
    public void Product_workspace_history_endpoint_is_authorized_and_read_only()
    {
        var root = FindRepoRoot();
        var endpoints = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Host", "Tooba.Host", "Admin", "ProductWorkspaceEndpoints.cs"));
        Assert.Contains("MapGet(\"/{productId:guid}/history\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("GetHistoryAsync", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapDelete(\"/{productId:guid}/history\"", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPatch(\"/{productId:guid}/history\"", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPut(\"/{productId:guid}/history\"", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(\"/{productId:guid}/history\"", endpoints, StringComparison.Ordinal);

        var directory = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Catalog", "Tooba.Catalog.Infrastructure", "CatalogDirectory.cs"));
        Assert.DoesNotContain("PricingDbContext", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("InventoryDbContext", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("OfferDbContext", directory, StringComparison.Ordinal);
    }

    [SkippableFact]
    public async Task Records_lifecycle_and_edit_events_with_deterministic_order_and_paging()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-history", "tenant-history"));
        await using var db = CreateCatalogDb(_container.GetConnectionString(), commerce);
        await db.Database.EnsureCreatedAsync();

        var actor = new CatalogActorContext
        {
            ActorUserId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb"),
            ActorDisplayName = "اپراتور تست",
        };
        var dir = new CatalogDirectory(db, new OpenCatalogUseCaseGuard(), actor);

        var l1 = await dir.CreateCategoryAsync(null, new Dictionary<string, string> { ["fa-IR"] = "ریشه" }, CancellationToken.None);
        var l2 = await dir.CreateCategoryAsync(l1.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "میانی" }, CancellationToken.None);
        var l3 = await dir.CreateCategoryAsync(l2.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "برگ" }, CancellationToken.None);
        var l3b = await dir.CreateCategoryAsync(l2.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "برگ۲" }, CancellationToken.None);

        var product = await dir.CreateProductAsync(
            CatalogProductKind.PhysicalGood,
            "history-ready",
            null,
            new Dictionary<string, string> { ["fa-IR"] = "کالای تاریخچه" },
            CancellationToken.None);

        await dir.AssignCategoryAsync(product.ProductId, l3.CategoryId, CancellationToken.None);
        await dir.ReplaceProductPrimaryCategoryAsync(product.ProductId, l3b.CategoryId, CancellationToken.None);
        await dir.AttachMediaReferenceAsync(
            product.ProductId, Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"), CancellationToken.None);
        await ProductPublishPrep.EnsureMinimalSeoForPublishAsync(
            dir, product.ProductId, "توضیح سئو تاریخچه", CancellationToken.None);
        await dir.AppendProductHistoryAsync(
            product.ProductId,
            ProductHistoryRules.EventGeneralChanged,
            ProductHistoryRules.SectionGeneral,
            ProductHistoryRules.SummaryGeneralFa,
            "عنوان: قدیم",
            "عنوان: جدید",
            CancellationToken.None);
        await dir.AppendProductHistoryAsync(
            product.ProductId,
            ProductHistoryRules.EventAttributesChanged,
            ProductHistoryRules.SectionAttributes,
            ProductHistoryRules.SummaryAttributesFa,
            null,
            null,
            CancellationToken.None);
        await dir.AppendProductHistoryAsync(
            product.ProductId,
            ProductHistoryRules.EventVariantsChanged,
            ProductHistoryRules.SectionVariants,
            ProductHistoryRules.SummaryVariantsFa,
            null,
            null,
            CancellationToken.None);

        await dir.PublishProductAsync(product.ProductId, CancellationToken.None);
        await dir.UnpublishProductAsync(product.ProductId, CancellationToken.None);
        await dir.PublishProductAsync(product.ProductId, CancellationToken.None);
        await dir.ArchiveProductAsync(product.ProductId, CancellationToken.None);
        await dir.RestoreProductAsync(product.ProductId, CancellationToken.None);

        var page = await dir.ListProductHistoryAsync(product.ProductId, null, 0, 100, CancellationToken.None);
        Assert.True(page.TotalCount >= 10);
        Assert.Equal(page.Items.Count, Math.Min(page.TotalCount, 100));

        for (var i = 1; i < page.Items.Count; i++)
        {
            Assert.True(
                page.Items[i - 1].OccurredAt >= page.Items[i].OccurredAt,
                "تاریخچه باید جدیدترین‌ها اول باشد.");
        }

        Assert.Contains(page.Items, x => x.EventType == ProductHistoryRules.EventCreated);
        Assert.Contains(page.Items, x => x.EventType == ProductHistoryRules.EventCategoryChanged);
        Assert.Contains(page.Items, x => x.EventType == ProductHistoryRules.EventMediaChanged);
        Assert.Contains(page.Items, x => x.EventType == ProductHistoryRules.EventSeoChanged);
        Assert.Contains(page.Items, x => x.EventType == ProductHistoryRules.EventGeneralChanged);
        Assert.Contains(page.Items, x => x.EventType == ProductHistoryRules.EventAttributesChanged);
        Assert.Contains(page.Items, x => x.EventType == ProductHistoryRules.EventVariantsChanged);
        Assert.Contains(page.Items, x => x.EventType == ProductHistoryRules.EventPublished);
        Assert.Contains(page.Items, x => x.EventType == ProductHistoryRules.EventUnpublished);
        Assert.Contains(page.Items, x => x.EventType == ProductHistoryRules.EventArchived);
        Assert.Contains(page.Items, x => x.EventType == ProductHistoryRules.EventRestored);

        var publish = page.Items.First(x => x.EventType == ProductHistoryRules.EventPublished);
        var restore = page.Items.First(x => x.EventType == ProductHistoryRules.EventRestored);
        Assert.NotEqual(publish.SummaryFa, restore.SummaryFa);
        Assert.Equal(ProductHistoryRules.SummaryPublishedFa, publish.SummaryFa);
        Assert.Equal(ProductHistoryRules.SummaryRestoredFa, restore.SummaryFa);
        Assert.Equal("اپراتور تست", publish.ActorDisplayName);

        var lifecycle = await dir.ListProductHistoryAsync(
            product.ProductId, ProductHistoryRules.SectionLifecycle, 0, 10, CancellationToken.None);
        Assert.All(lifecycle.Items, x => Assert.Equal(ProductHistoryRules.SectionLifecycle, x.Section));
        Assert.True(lifecycle.TotalCount >= 4);

        var paged = await dir.ListProductHistoryAsync(product.ProductId, null, 0, 3, CancellationToken.None);
        Assert.Equal(3, paged.Items.Count);
        Assert.True(paged.TotalCount > 3);
        var next = await dir.ListProductHistoryAsync(product.ProductId, null, 3, 3, CancellationToken.None);
        Assert.DoesNotContain(next.Items, x => paged.Items.Any(p => p.HistoryId == x.HistoryId));

        var immutableCount = await db.ProductHistoryEntries.CountAsync(x => x.ProductId == product.ProductId);
        Assert.Equal(page.TotalCount, immutableCount);
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

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
