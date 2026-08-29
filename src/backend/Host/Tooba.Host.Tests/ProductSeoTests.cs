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
/// پوشش SEO محصول: slug یونیکد، آمادگی، ایزولهٔ locale، بدون Price/Stock (TB-P07-T017).
/// </summary>
[Collection("PostgresSerial")]
public sealed class ProductSeoTests : IAsyncLifetime
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
                .WithDatabase("tooba_product_seo")
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
    public void ProductSeoRules_persian_slug_public_path_and_readiness_messages()
    {
        Assert.Equal("گوشی-سامسونگ-galaxy-s24", CatalogCategorySlugNormalizer.NormalizeSlug("گوشی سامسونگ Galaxy S24"));
        Assert.Equal("/fa/products/گوشی-سامسونگ", ProductSeoRules.BuildPublicPath("fa-IR", "گوشی-سامسونگ"));
        Assert.Equal("/en/products/linen-shirt", ProductSeoRules.BuildPublicPath("en", "linen-shirt"));
        Assert.DoesNotContain("/product/", ProductSeoRules.BuildPublicPath("fa-IR", "x"), StringComparison.Ordinal);

        var incomplete = ProductSeoRules.Evaluate(null, null, null, null);
        Assert.False(incomplete.IsReady);
        Assert.Equal(ProductSeoRules.MessageSlugIncompleteFa, incomplete.MessageFa);

        var missingDesc = ProductSeoRules.Evaluate("slug-ok", null, null, "نام محصول");
        Assert.True(missingDesc.HasValidSlug);
        Assert.True(missingDesc.HasSeoTitleOrFallback);
        Assert.False(missingDesc.HasSeoDescription);
        Assert.Equal(ProductSeoRules.MessageDescriptionIncompleteFa, missingDesc.MessageFa);

        var ready = ProductSeoRules.Evaluate("slug-ok", "عنوان", "توضیح", "نام");
        Assert.True(ready.IsReady);
        Assert.Equal(ProductSeoRules.MessageReadyFa, ready.MessageFa);

        Assert.ThrowsAny<Exception>(() => CatalogCategorySlugNormalizer.NormalizeSlug("   ---   "));
        Assert.DoesNotContain(typeof(CatalogProduct).GetProperties(), p => p.Name is "Price" or "Stock");
    }

    [SkippableFact]
    public async Task Seo_update_locale_isolation_duplicate_and_readiness()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-product-seo", "tenant-product-seo"));
        await using var db = CreateCatalogDb(cs, commerce);
        await db.Database.EnsureCreatedAsync();
        var dir = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var product = await dir.CreateProductAsync(
            CatalogProductKind.PhysicalGood,
            "seo-base-phone",
            null,
            new Dictionary<string, string> { ["fa-IR"] = "گوشی سئو", ["en"] = "SEO Phone" },
            CancellationToken.None);

        var other = await dir.CreateProductAsync(
            CatalogProductKind.PhysicalGood,
            "taken-slug",
            null,
            new Dictionary<string, string> { ["fa-IR"] = "محصول دیگر" },
            CancellationToken.None);
        Assert.NotEqual(Guid.Empty, other.ProductId);

        var beforeFa = await dir.GetProductSeoAsync(product.ProductId, "fa-IR", CancellationToken.None);
        var emptyReady = beforeFa.Readiness;
        Assert.True(emptyReady.HasValidSlug);
        Assert.True(emptyReady.HasLocalizedIdentity);
        Assert.True(emptyReady.HasSeoTitleOrFallback);
        Assert.False(emptyReady.HasSeoDescription);
        Assert.False(emptyReady.IsReady);
        Assert.Equal(ProductSeoRules.MessageDescriptionIncompleteFa, emptyReady.MessageFa);

        var faUpdated = await dir.UpdateProductSeoAsync(
            product.ProductId,
            new ProductSeoUpdateInput(
                "fa-IR",
                "گوشی-سامسونگ-galaxy-s24",
                "عنوان فارسی سئو",
                "توضیح فارسی نتیجه جستجو",
                beforeFa.UpdatedAt),
            CancellationToken.None);

        Assert.Equal("گوشی-سامسونگ-galaxy-s24", faUpdated.Slug);
        Assert.Equal("عنوان فارسی سئو", faUpdated.SeoTitle);
        Assert.Equal("توضیح فارسی نتیجه جستجو", faUpdated.SeoDescription);
        Assert.Equal("/fa/products/گوشی-سامسونگ-galaxy-s24", faUpdated.PublicPath);
        Assert.True(faUpdated.Readiness.IsReady);
        Assert.Equal(ProductSeoRules.MessageReadyFa, faUpdated.Readiness.MessageFa);
        Assert.DoesNotContain("Guid", faUpdated.PublicPath, StringComparison.OrdinalIgnoreCase);

        var enBefore = await dir.GetProductSeoAsync(product.ProductId, "en", CancellationToken.None);
        Assert.Null(enBefore.SeoTitle);
        Assert.Null(enBefore.SeoDescription);
        Assert.Equal("گوشی-سامسونگ-galaxy-s24", enBefore.Slug);

        var enUpdated = await dir.UpdateProductSeoAsync(
            product.ProductId,
            new ProductSeoUpdateInput(
                "en",
                "گوشی-سامسونگ-galaxy-s24",
                "English SEO title",
                "English SEO description",
                faUpdated.UpdatedAt),
            CancellationToken.None);
        Assert.Equal("English SEO title", enUpdated.SeoTitle);
        Assert.Equal("/en/products/گوشی-سامسونگ-galaxy-s24", enUpdated.PublicPath);

        var faAfterEn = await dir.GetProductSeoAsync(product.ProductId, "fa-IR", CancellationToken.None);
        Assert.Equal("عنوان فارسی سئو", faAfterEn.SeoTitle);
        Assert.Equal("توضیح فارسی نتیجه جستجو", faAfterEn.SeoDescription);

        var stale = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dir.UpdateProductSeoAsync(
                product.ProductId,
                new ProductSeoUpdateInput("fa-IR", "x", "t", "d", faUpdated.UpdatedAt),
                CancellationToken.None));
        Assert.Equal("workspace.catalog.stale", stale.Message);

        var dup = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dir.UpdateProductSeoAsync(
                product.ProductId,
                new ProductSeoUpdateInput("fa-IR", "taken-slug", "t", "d", enUpdated.UpdatedAt),
                CancellationToken.None));
        Assert.Contains("قبلاً استفاده", dup.Message, StringComparison.Ordinal);

        var invalid = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dir.UpdateProductSeoAsync(
                product.ProductId,
                new ProductSeoUpdateInput("fa-IR", "---", "t", "d", enUpdated.UpdatedAt),
                CancellationToken.None));
        Assert.Contains("نامعتبر", invalid.Message, StringComparison.Ordinal);

        var resolved = await db.Products.AsNoTracking()
            .SingleAsync(x => x.SlugSeam == "گوشی-سامسونگ-galaxy-s24");
        Assert.Equal(product.ProductId, resolved.ProductId);
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
