using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// بنیاد برچسب تاکسونومی Catalog (TB-P07-T032 L/M) — نه meta keywords.
/// </summary>
[Collection("PostgresSerial")]
public sealed class CatalogTagFoundationTests : IAsyncLifetime
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
                .WithDatabase("tooba_catalog_tag")
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
    public async Task Create_tag_assign_remove_product_and_category_reject_duplicate()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-catalog-tag", "tenant-catalog-tag"));
        await using var db = CreateCatalogDb(cs, commerce);
        await db.Database.EnsureCreatedAsync();
        var dir = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var tag = await dir.CreateTagAsync(
            null,
            null,
            new Dictionary<string, string> { ["fa-IR"] = "پرفروش", ["en"] = "Bestseller" },
            "fa-IR",
            CancellationToken.None);
        Assert.False(string.IsNullOrWhiteSpace(tag.Code));
        Assert.Equal("پرفروش", tag.Name);
        Assert.Equal(CatalogPublicationStatus.Draft, tag.Status);

        var listed = await dir.ListTagsAsync("fa-IR", "فروش", CancellationToken.None);
        Assert.Contains(listed, t => t.TagId == tag.TagId);

        var l1 = await dir.CreateCategoryAsync(
            null, new Dictionary<string, string> { ["fa-IR"] = "کالای دیجیتال" }, CancellationToken.None);
        var l2 = await dir.CreateCategoryAsync(
            l1.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "موبایل" }, CancellationToken.None);
        var l3 = await dir.CreateCategoryAsync(
            l2.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "گوشی" }, CancellationToken.None);
        var product = await dir.CreateProductAsync(
            CatalogProductKind.PhysicalGood,
            "phone-tag",
            null,
            new Dictionary<string, string> { ["fa-IR"] = "گوشی تست برچسب" },
            CancellationToken.None);
        await dir.AssignCategoryAsync(product.ProductId, l3.CategoryId, CancellationToken.None);

        await dir.AssignProductTagAsync(product.ProductId, tag.TagId, CancellationToken.None);
        var productTags = await dir.ListProductTagsAsync(product.ProductId, "fa-IR", CancellationToken.None);
        Assert.Single(productTags);
        Assert.Equal(tag.TagId, productTags[0].TagId);

        var dupProduct = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dir.AssignProductTagAsync(product.ProductId, tag.TagId, CancellationToken.None));
        Assert.Contains("برچسب", dupProduct.Message, StringComparison.Ordinal);

        await dir.RemoveProductTagAsync(product.ProductId, tag.TagId, CancellationToken.None);
        Assert.Empty(await dir.ListProductTagsAsync(product.ProductId, "fa-IR", CancellationToken.None));

        await dir.AssignCategoryTagAsync(l3.CategoryId, tag.TagId, CancellationToken.None);
        var categoryTags = await dir.ListCategoryTagsAsync(l3.CategoryId, "fa-IR", CancellationToken.None);
        Assert.Single(categoryTags);
        Assert.Equal("پرفروش", categoryTags[0].Name);

        var dupCategory = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dir.AssignCategoryTagAsync(l3.CategoryId, tag.TagId, CancellationToken.None));
        Assert.Contains("برچسب", dupCategory.Message, StringComparison.Ordinal);

        await dir.RemoveCategoryTagAsync(l3.CategoryId, tag.TagId, CancellationToken.None);
        Assert.Empty(await dir.ListCategoryTagsAsync(l3.CategoryId, "fa-IR", CancellationToken.None));
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
