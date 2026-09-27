using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application.Tags.Models;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// بنیاد برچسب تاکسونومی Catalog (TB-P07-T032 L/M) — نه meta keywords.
/// W4: expected failures assert stable CatalogErrorCodes (not Persian IOE messages).
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
        var guard = new OpenCatalogUseCaseGuard();
        var tags = new TagDirectory(db, guard);
        var dir = new CatalogDirectory(db, guard);

        var tagResult = await tags.CreateAsync(
            new CreateTagWriteModel(
                null,
                null,
                "fa-IR",
                new Dictionary<string, string> { ["fa-IR"] = "پرفروش", ["en"] = "Bestseller" }),
            CancellationToken.None);
        Assert.True(tagResult.IsSuccess);
        var tag = tagResult.Value;
        Assert.False(string.IsNullOrWhiteSpace(tag.Code));
        Assert.Equal("پرفروش", tag.Name);
        Assert.Equal(CatalogPublicationStatus.Draft, tag.Status);

        var listed = await tags.ListAsync("fa-IR", "فروش", CancellationToken.None);
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

        var assignProduct = await tags.AssignProductTagAsync(product.ProductId, tag.TagId, CancellationToken.None);
        Assert.True(assignProduct.IsSuccess);
        Assert.Single(assignProduct.Value);

        var dupProduct = await tags.AssignProductTagAsync(product.ProductId, tag.TagId, CancellationToken.None);
        Assert.True(dupProduct.IsFailure);
        Assert.Equal(CatalogErrorCodes.TagAssignDuplicate, dupProduct.FirstError.Code);

        var removeProduct = await tags.RemoveProductTagAsync(product.ProductId, tag.TagId, CancellationToken.None);
        Assert.True(removeProduct.IsSuccess);
        Assert.Empty(removeProduct.Value);

        var assignCategory = await tags.AssignCategoryTagAsync(l3.CategoryId, tag.TagId, CancellationToken.None);
        Assert.True(assignCategory.IsSuccess);
        Assert.Equal("پرفروش", assignCategory.Value[0].Name);

        var dupCategory = await tags.AssignCategoryTagAsync(l3.CategoryId, tag.TagId, CancellationToken.None);
        Assert.True(dupCategory.IsFailure);
        Assert.Equal(CatalogErrorCodes.TagAssignDuplicate, dupCategory.FirstError.Code);

        var removeCategory = await tags.RemoveCategoryTagAsync(l3.CategoryId, tag.TagId, CancellationToken.None);
        Assert.True(removeCategory.IsSuccess);
        Assert.Empty(removeCategory.Value);
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
