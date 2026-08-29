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
/// پوشش ماتریس تنوع محصول (TB-P07-T013).
/// </summary>
[Collection("PostgresSerial")]
public sealed class ProductVariantMatrixTests : IAsyncLifetime
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
                .WithDatabase("tooba_product_variant_matrix")
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
    public async Task Matrix_generation_reconcile_readiness_and_category_impact()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-variant-matrix", "tenant-variant-matrix"));
        await using var db = CreateCatalogDb(cs, commerce);
        await db.Database.EnsureCreatedAsync();
        var dir = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var category = await dir.CreateCategoryAsync(
            null, new Dictionary<string, string> { ["fa-IR"] = "موبایل" }, CancellationToken.None);
        var other = await dir.CreateCategoryAsync(
            null, new Dictionary<string, string> { ["fa-IR"] = "کتاب" }, CancellationToken.None);

        var colorId = await dir.CreateAttributeDefinitionAsync(
            "color", CatalogAttributeValueKind.Enumeration, true,
            new Dictionary<string, string> { ["fa-IR"] = "رنگ" }, CancellationToken.None);
        var storageId = await dir.CreateAttributeDefinitionAsync(
            "storage", CatalogAttributeValueKind.Enumeration, true,
            new Dictionary<string, string> { ["fa-IR"] = "حافظه" }, CancellationToken.None);
        var noteId = await dir.CreateAttributeDefinitionAsync(
            "weight_g", CatalogAttributeValueKind.Number, true,
            new Dictionary<string, string> { ["fa-IR"] = "وزن" }, CancellationToken.None);
        var authorId = await dir.CreateAttributeDefinitionAsync(
            "author", CatalogAttributeValueKind.Text, false,
            new Dictionary<string, string> { ["fa-IR"] = "نویسنده" }, CancellationToken.None);

        await dir.UpdateAttributeDefinitionAsync(
            colorId, null, false, true, false, false, 10, null, null, null, true, CancellationToken.None);
        await dir.UpdateAttributeDefinitionAsync(
            storageId, null, false, true, false, false, 20, null, null, null, true, CancellationToken.None);

        var black = await dir.AddAttributeOptionAsync(
            colorId, "black", new Dictionary<string, string> { ["fa-IR"] = "مشکی" }, CancellationToken.None);
        var white = await dir.AddAttributeOptionAsync(
            colorId, "white", new Dictionary<string, string> { ["fa-IR"] = "سفید" }, CancellationToken.None);
        var s128 = await dir.AddAttributeOptionAsync(
            storageId, "128", new Dictionary<string, string> { ["fa-IR"] = "128GB" }, CancellationToken.None);
        var s256 = await dir.AddAttributeOptionAsync(
            storageId, "256", new Dictionary<string, string> { ["fa-IR"] = "256GB" }, CancellationToken.None);
        var s512 = await dir.AddAttributeOptionAsync(
            storageId, "512", new Dictionary<string, string> { ["fa-IR"] = "512GB" }, CancellationToken.None);

        await dir.BindCategoryAttributeAsync(
            category.CategoryId, colorId, 10,
            new CategoryAttributeAssignmentFlags(false, true, true, false), CancellationToken.None);
        await dir.BindCategoryAttributeAsync(
            category.CategoryId, storageId, 20,
            new CategoryAttributeAssignmentFlags(false, true, true, false), CancellationToken.None);
        // Architectural: non-option axis allowed on schema but rejected by matrix generator.
        await dir.BindCategoryAttributeAsync(
            category.CategoryId, noteId, 30,
            new CategoryAttributeAssignmentFlags(false, false, true, false), CancellationToken.None);
        await dir.BindCategoryAttributeAsync(
            other.CategoryId, authorId, 1,
            new CategoryAttributeAssignmentFlags(true, false, false, false), CancellationToken.None);

        var product = await dir.CreateProductAsync(
            CatalogProductKind.PhysicalGood, "variant-phone", null,
            new Dictionary<string, string> { ["fa-IR"] = "گوشی تنوع" }, CancellationToken.None);
        await dir.AssignCategoryAsync(product.ProductId, category.CategoryId, CancellationToken.None);

        var editor = await dir.GetProductVariantEditorStateAsync(product.ProductId, "fa-IR", CancellationToken.None);
        Assert.Equal(3, editor.Axes.Count);
        Assert.Null(editor.MessageFa);

        // one-axis generation
        var oneAxisPreview = await dir.PreviewProductVariantCombinationsAsync(
            product.ProductId,
            [new ProductVariantSelectedAxisInput(colorId, [black, white])],
            "fa-IR",
            CancellationToken.None);
        Assert.Equal(2, oneAxisPreview.TotalDesired);
        Assert.Equal(2, oneAxisPreview.NewCount);
        Assert.False(oneAxisPreview.Capped);

        var oneAxisApply = await dir.ApplyProductVariantMatrixAsync(
            product.ProductId,
            new ProductVariantApplyInput(
                "fa-IR",
                [new ProductVariantSelectedAxisInput(colorId, [black, white])],
                null,
                null),
            CancellationToken.None);
        Assert.Equal(2, oneAxisApply.Created);
        Assert.Equal(2, oneAxisApply.Variants.Count(v => v.Status != CatalogPublicationStatus.Archived));

        // multi-axis generation + deterministic order + uniqueness
        var multiPreview = await dir.PreviewProductVariantCombinationsAsync(
            product.ProductId,
            [
                new ProductVariantSelectedAxisInput(colorId, [black, white]),
                new ProductVariantSelectedAxisInput(storageId, [s128, s256, s512]),
            ],
            "fa-IR",
            CancellationToken.None);
        Assert.Equal(6, multiPreview.TotalDesired);
        Assert.Equal(6, multiPreview.NewCount);
        Assert.Equal(2, multiPreview.DeactivateCount);
        Assert.Contains("جدید", multiPreview.MessageFa ?? "");

        var desired = multiPreview.Combinations
            .Where(c => c.Action == ProductVariantCombinationAction.New)
            .Select(c => string.Join("/", c.AxisLabels.Select(l => l.ValueLabel)))
            .ToList();
        Assert.Equal(6, desired.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("مشکی/128GB", desired[0]);
        Assert.Equal(desired.Count, desired.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(
            multiPreview.Combinations.Count(c => c.Action != ProductVariantCombinationAction.Deactivate),
            multiPreview.Combinations
                .Where(c => c.Action != ProductVariantCombinationAction.Deactivate)
                .Select(c => c.DesiredFingerprint)
                .Distinct(StringComparer.Ordinal)
                .Count());

        var multiApply = await dir.ApplyProductVariantMatrixAsync(
            product.ProductId,
            new ProductVariantApplyInput(
                "fa-IR",
                [
                    new ProductVariantSelectedAxisInput(colorId, [black, white]),
                    new ProductVariantSelectedAxisInput(storageId, [s128, s256]),
                ],
                null,
                null),
            CancellationToken.None);
        Assert.Equal(4, multiApply.Created);
        Assert.Equal(2, multiApply.Deactivated);
        Assert.Equal(4, multiApply.Variants.Count(v => v.Status != CatalogPublicationStatus.Archived));

        // unchanged preserved on re-apply same matrix
        var preservedIds = multiApply.Variants
            .Where(v => v.Status != CatalogPublicationStatus.Archived)
            .Select(v => v.VariantId)
            .OrderBy(x => x)
            .ToList();
        var reapply = await dir.ApplyProductVariantMatrixAsync(
            product.ProductId,
            new ProductVariantApplyInput(
                "fa-IR",
                [
                    new ProductVariantSelectedAxisInput(colorId, [black, white]),
                    new ProductVariantSelectedAxisInput(storageId, [s128, s256]),
                ],
                null,
                null),
            CancellationToken.None);
        Assert.Equal(4, reapply.Unchanged);
        Assert.Equal(0, reapply.Created);
        Assert.Equal(
            preservedIds,
            reapply.Variants.Where(v => v.Status != CatalogPublicationStatus.Archived).Select(v => v.VariantId).OrderBy(x => x).ToList());

        // remove combinations → archive, never hard-delete
        var beforeCount = await db.Variants.CountAsync(x => x.ProductId == product.ProductId);
        var shrink = await dir.ApplyProductVariantMatrixAsync(
            product.ProductId,
            new ProductVariantApplyInput(
                "fa-IR",
                [
                    new ProductVariantSelectedAxisInput(colorId, [black]),
                    new ProductVariantSelectedAxisInput(storageId, [s128]),
                ],
                null,
                null),
            CancellationToken.None);
        Assert.True(shrink.Deactivated >= 1);
        Assert.Equal(1, shrink.Variants.Count(v => v.Status != CatalogPublicationStatus.Archived));
        var afterCount = await db.Variants.CountAsync(x => x.ProductId == product.ProductId);
        Assert.Equal(beforeCount, afterCount);
        Assert.DoesNotContain(db.Model.FindEntityType(typeof(CatalogVariant))!.GetProperties(), p => p.Name is "Price" or "Stock");

        var surviving = shrink.Variants.Single(v => v.Status != CatalogPublicationStatus.Archived);
        await dir.ApplyProductVariantMatrixAsync(
            product.ProductId,
            new ProductVariantApplyInput(
                "fa-IR",
                [
                    new ProductVariantSelectedAxisInput(colorId, [white]),
                    new ProductVariantSelectedAxisInput(storageId, [s256]),
                ],
                null,
                null),
            CancellationToken.None);
        Assert.True(await db.Variants.AnyAsync(x => x.VariantId == surviving.VariantId));
        Assert.Equal(
            CatalogPublicationStatus.Archived,
            (await db.Variants.SingleAsync(x => x.VariantId == surviving.VariantId)).Status);

        // invalid free-text axis rejected
        var ex = await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            dir.PreviewProductVariantCombinationsAsync(
                product.ProductId,
                [new ProductVariantSelectedAxisInput(noteId, [Guid.NewGuid()])],
                "fa-IR",
                CancellationToken.None));
        Assert.Contains("گزینه‌دار", ex.Message);

        // default uniqueness
        var defaultApply = await dir.ApplyProductVariantMatrixAsync(
            product.ProductId,
            new ProductVariantApplyInput(
                "fa-IR",
                [
                    new ProductVariantSelectedAxisInput(colorId, [black, white]),
                    new ProductVariantSelectedAxisInput(storageId, [s128]),
                ],
                null,
                null),
            CancellationToken.None);
        var defaultId = defaultApply.Variants.First(v => v.Status != CatalogPublicationStatus.Archived).VariantId;
        var withDefault = await dir.ApplyProductVariantMatrixAsync(
            product.ProductId,
            new ProductVariantApplyInput(
                "fa-IR",
                [
                    new ProductVariantSelectedAxisInput(colorId, [black, white]),
                    new ProductVariantSelectedAxisInput(storageId, [s128]),
                ],
                defaultId,
                null),
            CancellationToken.None);
        Assert.Equal(1, withDefault.Variants.Count(v => v.IsDefault && v.Status != CatalogPublicationStatus.Archived));
        Assert.Equal(defaultId, withDefault.Variants.Single(v => v.IsDefault).VariantId);

        var readiness = await dir.GetProductVariantReadinessAsync(product.ProductId, CancellationToken.None);
        Assert.True(readiness.IsValid);
        Assert.False(readiness.NoDefaultVariant);

        var impact = await dir.PreviewCategoryChangeReportAsync(
            product.ProductId, other.CategoryId, "fa-IR", CancellationToken.None);
        Assert.True(impact.ImpactedVariantCount > 0);
        Assert.Contains("تنوع", impact.MessageFa);
        Assert.Contains("تنوع", impact.VariantImpactMessageFa ?? "");
    }

    [SkippableFact]
    public async Task Empty_axes_message_and_no_fake_singleton()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-variant-empty", "tenant-variant-empty"));
        await using var db = CreateCatalogDb(cs, commerce);
        await db.Database.EnsureCreatedAsync();
        var dir = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var category = await dir.CreateCategoryAsync(
            null, new Dictionary<string, string> { ["fa-IR"] = "ساده" }, CancellationToken.None);
        var product = await dir.CreateProductAsync(
            CatalogProductKind.PhysicalGood, "no-axes", null,
            new Dictionary<string, string> { ["fa-IR"] = "بدون محور" }, CancellationToken.None);
        await dir.AssignCategoryAsync(product.ProductId, category.CategoryId, CancellationToken.None);

        var editor = await dir.GetProductVariantEditorStateAsync(product.ProductId, "fa-IR", CancellationToken.None);
        Assert.Empty(editor.Axes);
        Assert.Equal("برای این دسته‌بندی ویژگی تنوع تعریف نشده است.", editor.MessageFa);
        Assert.Empty(editor.Variants);
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
