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
/// پوشش آمادگی تجمیعی انتشار و چرخهٔ عمر Draft/Published/Archived بدون Offer/Price/Stock.
/// </summary>
[Collection("PostgresSerial")]
public sealed class ProductPublishTests : IAsyncLifetime
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
                .WithDatabase("tooba_product_publish")
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
    public void Publish_rules_summarize_missing_in_persian()
    {
        Assert.Equal(ProductPublishRules.MessageReadyFa, ProductPublishRules.SummarizeMissingFa(0));
        Assert.Contains("2", ProductPublishRules.SummarizeMissingFa(2), StringComparison.Ordinal);
        Assert.Equal("پیش‌نویس", ProductPublishRules.LifecycleLabelFa(CatalogPublicationStatus.Draft));
        Assert.Equal("منتشرشده", ProductPublishRules.LifecycleLabelFa(CatalogPublicationStatus.Published));
        Assert.Equal("بایگانی‌شده", ProductPublishRules.LifecycleLabelFa(CatalogPublicationStatus.Archived));
    }

    [SkippableFact]
    public async Task Aggregate_readiness_and_lifecycle_gates_without_offer_dependency()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-publish", "tenant-publish"));
        await using var db = CreateCatalogDb(_container.GetConnectionString(), commerce);
        await db.Database.EnsureCreatedAsync();
        var dir = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var l1 = await dir.CreateCategoryAsync(null, new Dictionary<string, string> { ["fa-IR"] = "ریشه" }, CancellationToken.None);
        var l2 = await dir.CreateCategoryAsync(l1.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "میانی" }, CancellationToken.None);
        var l3 = await dir.CreateCategoryAsync(l2.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "برگ" }, CancellationToken.None);

        var product = await dir.CreateProductAsync(
            CatalogProductKind.PhysicalGood,
            "publish-ready",
            null,
            new Dictionary<string, string> { ["fa-IR"] = "کالای انتشار" },
            CancellationToken.None);
        Assert.Equal(CatalogPublicationStatus.Draft, product.Status);

        var notReady = await dir.GetProductPublishReadinessAsync(product.ProductId, "fa-IR", CancellationToken.None);
        Assert.False(notReady.IsReady);
        Assert.False(notReady.CategoryReady);
        Assert.Contains(notReady.MissingRequirements, m => m.Code == "category");
        Assert.DoesNotContain(notReady.MissingRequirements, m => m.Code.Contains("offer", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(notReady.MissingRequirements, m => m.Code.Contains("price", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(notReady.MissingRequirements, m => m.Code.Contains("stock", StringComparison.OrdinalIgnoreCase));

        await Assert.ThrowsAnyAsync<Exception>(() =>
            dir.PublishProductAsync(product.ProductId, CancellationToken.None));

        await dir.AssignCategoryAsync(product.ProductId, l3.CategoryId, CancellationToken.None);
        await dir.AttachMediaReferenceAsync(
            product.ProductId, Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"), CancellationToken.None);
        await ProductPublishPrep.EnsureMinimalSeoForPublishAsync(
            dir, product.ProductId, "توضیح سئو کالای انتشار", CancellationToken.None);

        var ready = await dir.GetProductPublishReadinessAsync(product.ProductId, "fa-IR", CancellationToken.None);
        Assert.True(ready.IsReady);
        Assert.True(ready.CategoryReady);
        Assert.True(ready.TranslationReady);
        Assert.True(ready.AttributeReady);
        Assert.True(ready.VariantReady);
        Assert.True(ready.MediaReady);
        Assert.True(ready.SeoReady);
        Assert.Equal(ProductPublishRules.MessageReadyFa, ready.MessageFa);

        await dir.PublishProductAsync(product.ProductId, CancellationToken.None);
        await dir.PublishProductAsync(product.ProductId, CancellationToken.None);
        var published = await db.Products.AsNoTracking().SingleAsync(x => x.ProductId == product.ProductId);
        Assert.Equal(CatalogPublicationStatus.Published, published.Status);
        var publishedEvents = await db.OutboxMessages.AsNoTracking()
            .CountAsync(x => x.EventType == CatalogProductPublishedIntegrationEvent.EventTypeName);
        Assert.Equal(1, publishedEvents);

        await dir.UnpublishProductAsync(product.ProductId, CancellationToken.None);
        var draft = await db.Products.AsNoTracking().SingleAsync(x => x.ProductId == product.ProductId);
        Assert.Equal(CatalogPublicationStatus.Draft, draft.Status);

        await dir.PublishProductAsync(product.ProductId, CancellationToken.None);
        await dir.ArchiveProductAsync(product.ProductId, CancellationToken.None);
        var archived = await db.Products.AsNoTracking().SingleAsync(x => x.ProductId == product.ProductId);
        Assert.Equal(CatalogPublicationStatus.Archived, archived.Status);
        Assert.Equal("publish-ready", archived.SlugSeam);

        var archivedPublish = await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            dir.PublishProductAsync(product.ProductId, CancellationToken.None));
        Assert.Equal(ProductPublishRules.MessageRestoreBeforePublishFa, archivedPublish.Message);
        Assert.Equal(
            CatalogPublicationStatus.Archived,
            (await db.Products.AsNoTracking().SingleAsync(x => x.ProductId == product.ProductId)).Status);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            dir.UnpublishProductAsync(product.ProductId, CancellationToken.None));

        await dir.RestoreProductAsync(product.ProductId, CancellationToken.None);
        var restored = await db.Products.AsNoTracking().SingleAsync(x => x.ProductId == product.ProductId);
        Assert.Equal(CatalogPublicationStatus.Draft, restored.Status);
        Assert.Equal("publish-ready", restored.SlugSeam);

        await dir.PublishProductAsync(product.ProductId, CancellationToken.None);
        Assert.Equal(
            CatalogPublicationStatus.Published,
            (await db.Products.AsNoTracking().SingleAsync(x => x.ProductId == product.ProductId)).Status);
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
