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
/// پوشش گالری رسانهٔ محصول: primary، ترتیب، unassign، readiness (TB-P07-T014).
/// </summary>
[Collection("PostgresSerial")]
public sealed class ProductMediaGalleryTests : IAsyncLifetime
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
                .WithDatabase("tooba_product_media_gallery")
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
    public async Task Gallery_attach_primary_reorder_detach_and_readiness()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-media-gallery", "tenant-media-gallery"));
        await using var db = CreateCatalogDb(cs, commerce);
        await db.Database.EnsureCreatedAsync();
        var dir = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var product = await dir.CreateProductAsync(
            CatalogProductKind.PhysicalGood,
            "media-phone",
            null,
            new Dictionary<string, string> { ["fa-IR"] = "گوشی رسانه" },
            CancellationToken.None);

        Assert.DoesNotContain(typeof(CatalogProduct).GetProperties(), p => p.Name is "Price" or "Stock");

        var emptyReady = await dir.GetProductMediaReadinessAsync(product.ProductId, CancellationToken.None);
        Assert.False(emptyReady.HasPrimaryImage);
        Assert.Equal(0, emptyReady.MediaCount);
        Assert.False(emptyReady.IsReady);
        Assert.Equal("تصویر اصلی تعیین نشده", emptyReady.MessageFa);

        var firstId = Guid.Parse("11111111-1111-7111-8111-111111111111");
        await dir.AttachMediaReferenceAsync(product.ProductId, firstId, "اولین", CancellationToken.None);
        var afterFirst = await dir.GetProductMediaEditorStateAsync(product.ProductId, CancellationToken.None);
        Assert.Single(afterFirst.Items);
        Assert.True(afterFirst.Items[0].IsPrimary);
        Assert.Equal(0, afterFirst.Items[0].DisplayOrder);
        Assert.True(afterFirst.Readiness.IsReady);
        Assert.Equal("رسانه کامل است", afterFirst.Readiness.MessageFa);

        var secondId = Guid.Parse("22222222-2222-7222-8222-222222222222");
        await dir.AttachMediaReferenceAsync(product.ProductId, secondId, "دومین", CancellationToken.None);
        var thirdId = await dir.AttachGeneratedPlaceholderMediaAsync(product.ProductId, "نمایشی", CancellationToken.None);
        Assert.NotEqual(Guid.Empty, thirdId);

        var three = await dir.GetProductMediaEditorStateAsync(product.ProductId, CancellationToken.None);
        Assert.Equal(3, three.Items.Count);
        Assert.Equal(1, three.Items.Count(x => x.IsPrimary));
        Assert.True(three.Items.Single(x => x.MediaAssetId == firstId).IsPrimary);
        Assert.Equal(3, three.Readiness.MediaCount);
        Assert.True(three.Readiness.HasPrimaryImage);

        await dir.SetProductPrimaryMediaAsync(product.ProductId, secondId, CancellationToken.None);
        var afterPrimary = await dir.GetProductMediaEditorStateAsync(product.ProductId, CancellationToken.None);
        Assert.Equal(1, afterPrimary.Items.Count(x => x.IsPrimary));
        Assert.True(afterPrimary.Items.Single(x => x.MediaAssetId == secondId).IsPrimary);

        await dir.ReorderProductMediaAsync(
            product.ProductId,
            [thirdId, firstId, secondId],
            CancellationToken.None);
        var reordered = await dir.GetProductMediaEditorStateAsync(product.ProductId, CancellationToken.None);
        Assert.Equal(
            new[] { thirdId, firstId, secondId },
            reordered.Items.OrderBy(x => x.DisplayOrder).Select(x => x.MediaAssetId).ToArray());
        Assert.True(reordered.Items.Single(x => x.MediaAssetId == secondId).IsPrimary);

        await dir.PatchProductMediaAltAsync(product.ProductId, firstId, "alt به‌روز", CancellationToken.None);
        var patched = await dir.GetProductMediaEditorStateAsync(product.ProductId, CancellationToken.None);
        Assert.Equal("alt به‌روز", patched.Items.Single(x => x.MediaAssetId == firstId).AltText);

        await dir.DetachProductMediaAsync(product.ProductId, secondId, CancellationToken.None);
        var afterDetach = await dir.GetProductMediaEditorStateAsync(product.ProductId, CancellationToken.None);
        Assert.Equal(2, afterDetach.Items.Count);
        Assert.DoesNotContain(afterDetach.Items, x => x.MediaAssetId == secondId);
        Assert.Equal(1, afterDetach.Items.Count(x => x.IsPrimary));
        var fallbackPrimary = afterDetach.Items.Single(x => x.IsPrimary);
        Assert.Equal(thirdId, fallbackPrimary.MediaAssetId);
        Assert.Equal(0, fallbackPrimary.DisplayOrder);

        var assetRowsStillAbsent = await db.MediaReferences.CountAsync(
            x => x.ProductId == product.ProductId && x.MediaAssetId == secondId);
        Assert.Equal(0, assetRowsStillAbsent);

        var readiness = await dir.GetProductMediaReadinessAsync(product.ProductId, CancellationToken.None);
        Assert.True(readiness.HasPrimaryImage);
        Assert.Equal(2, readiness.MediaCount);
        Assert.True(readiness.IsReady);
        Assert.Equal("رسانه کامل است", readiness.MessageFa);
        Assert.Contains("رسانه", $"{readiness.MediaCount} رسانه");
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
