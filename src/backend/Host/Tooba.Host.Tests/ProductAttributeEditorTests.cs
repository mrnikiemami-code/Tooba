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
/// پوشش ویرایشگر ویژگی محصول بر اساس schema مؤثر رده (TB-P07-T012).
/// </summary>
[Collection("PostgresSerial")]
public sealed class ProductAttributeEditorTests : IAsyncLifetime
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
                .WithDatabase("tooba_product_attr_editor")
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
    public async Task Editor_load_set_readiness_and_category_change_report()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-attr-editor", "tenant-attr-editor"));
        await using var db = CreateCatalogDb(cs, commerce);
        await db.Database.EnsureCreatedAsync();
        var dir = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var parent = await dir.CreateCategoryAsync(
            null, new Dictionary<string, string> { ["fa-IR"] = "الکترونیک" }, CancellationToken.None);
        var mid = await dir.CreateCategoryAsync(
            parent.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "موبایل و تبلت" }, CancellationToken.None);
        var child = await dir.CreateCategoryAsync(
            mid.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "گوشی موبایل" }, CancellationToken.None);
        var otherRoot = await dir.CreateCategoryAsync(
            null, new Dictionary<string, string> { ["fa-IR"] = "کتاب" }, CancellationToken.None);
        var otherMid = await dir.CreateCategoryAsync(
            otherRoot.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "رمان" }, CancellationToken.None);
        var other = await dir.CreateCategoryAsync(
            otherMid.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "داستان" }, CancellationToken.None);

        var screenId = await dir.CreateAttributeDefinitionAsync(
            "screen_size", CatalogAttributeValueKind.Number, false,
            new Dictionary<string, string> { ["fa-IR"] = "اندازه صفحه" }, CancellationToken.None);
        var waterproofId = await dir.CreateAttributeDefinitionAsync(
            "waterproof", CatalogAttributeValueKind.Boolean, false,
            new Dictionary<string, string> { ["fa-IR"] = "ضدآب" }, CancellationToken.None);
        var noteId = await dir.CreateAttributeDefinitionAsync(
            "note", CatalogAttributeValueKind.Text, false,
            new Dictionary<string, string> { ["fa-IR"] = "یادداشت" }, CancellationToken.None);
        var materialId = await dir.CreateAttributeDefinitionAsync(
            "material", CatalogAttributeValueKind.Enumeration, false,
            new Dictionary<string, string> { ["fa-IR"] = "جنس" }, CancellationToken.None);
        var colorId = await dir.CreateAttributeDefinitionAsync(
            "color", CatalogAttributeValueKind.Enumeration, true,
            new Dictionary<string, string> { ["fa-IR"] = "رنگ" }, CancellationToken.None);
        var weightId = await dir.CreateAttributeDefinitionAsync(
            "weight_g", CatalogAttributeValueKind.Number, false,
            new Dictionary<string, string> { ["fa-IR"] = "وزن" }, CancellationToken.None);
        var authorId = await dir.CreateAttributeDefinitionAsync(
            "author", CatalogAttributeValueKind.Text, false,
            new Dictionary<string, string> { ["fa-IR"] = "نویسنده" }, CancellationToken.None);

        await dir.UpdateAttributeDefinitionAsync(
            screenId, "inch", isRequired: true, isFilterable: true, isComparable: true, isMultivalue: false,
            displayOrder: 1, validationMin: 4m, validationMax: 10m, validationMaxLength: null, isActive: true,
            CancellationToken.None);
        await dir.UpdateAttributeDefinitionAsync(
            waterproofId, null, isRequired: false, isFilterable: true, isComparable: false, isMultivalue: false,
            displayOrder: 2, null, null, null, true, CancellationToken.None);
        await dir.UpdateAttributeDefinitionAsync(
            noteId, null, isRequired: false, isFilterable: false, isComparable: false, isMultivalue: false,
            displayOrder: 3, null, null, 200, true, CancellationToken.None);
        await dir.UpdateAttributeDefinitionAsync(
            materialId, null, isRequired: false, isFilterable: true, isComparable: false, isMultivalue: false,
            displayOrder: 4, null, null, null, true, CancellationToken.None);

        var aluminum = await dir.AddAttributeOptionAsync(
            materialId, "aluminum", new Dictionary<string, string> { ["fa-IR"] = "آلومینیوم" }, CancellationToken.None);
        var plastic = await dir.AddAttributeOptionAsync(
            materialId, "plastic", new Dictionary<string, string> { ["fa-IR"] = "پلاستیک" }, CancellationToken.None);
        var black = await dir.AddAttributeOptionAsync(
            colorId, "black", new Dictionary<string, string> { ["fa-IR"] = "مشکی" }, CancellationToken.None);

        await dir.BindCategoryAttributeAsync(
            child.CategoryId, screenId, 10,
            new CategoryAttributeAssignmentFlags(true, true, false, true), CancellationToken.None);
        await dir.BindCategoryAttributeAsync(
            child.CategoryId, waterproofId, 20,
            new CategoryAttributeAssignmentFlags(false, true, false, false), CancellationToken.None);
        await dir.BindCategoryAttributeAsync(
            child.CategoryId, noteId, 30,
            new CategoryAttributeAssignmentFlags(false, false, false, false), CancellationToken.None);
        await dir.BindCategoryAttributeAsync(
            child.CategoryId, materialId, 40,
            new CategoryAttributeAssignmentFlags(false, true, false, false), CancellationToken.None);
        await dir.BindCategoryAttributeAsync(
            child.CategoryId, colorId, 50,
            new CategoryAttributeAssignmentFlags(false, true, true, false), CancellationToken.None);

        await dir.BindCategoryAttributeAsync(
            other.CategoryId, authorId, 1,
            new CategoryAttributeAssignmentFlags(true, false, false, false), CancellationToken.None);
        await dir.BindCategoryAttributeAsync(
            other.CategoryId, noteId, 2,
            new CategoryAttributeAssignmentFlags(false, false, false, false), CancellationToken.None);

        var product = await dir.CreateProductAsync(
            CatalogProductKind.PhysicalGood, "attr-phone", null,
            new Dictionary<string, string> { ["fa-IR"] = "گوشی تست" }, CancellationToken.None);
        await dir.AssignCategoryAsync(product.ProductId, child.CategoryId, CancellationToken.None);

        var editor = await dir.GetProductAttributeEditorStateAsync(product.ProductId, "fa-IR", CancellationToken.None);
        Assert.Equal(child.CategoryId, editor.CategoryId);
        Assert.Contains("الکترونیک", editor.CategoryPath);
        Assert.Contains("موبایل و تبلت", editor.CategoryPath);
        Assert.Contains("گوشی موبایل", editor.CategoryPath);
        Assert.Equal(5, editor.Fields.Count);
        Assert.Equal(screenId, editor.Fields[0].DefinitionId);
        Assert.Equal("اندازه صفحه", editor.Fields[0].LocalizedName);
        Assert.True(editor.Fields[0].IsRequired);
        Assert.True(editor.Fields[0].IsMissingRequired);
        var colorField = Assert.Single(editor.Fields, f => f.DefinitionId == colorId);
        Assert.True(colorField.IsVariantAxis);
        Assert.False(editor.Readiness.IsComplete);
        Assert.Contains("screen_size", editor.Readiness.MissingRequiredCodes);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            dir.SetProductAttributesAsync(
                product.ProductId,
                [new ProductAttributeValueInput(screenId, "not-a-number", null, false)],
                CancellationToken.None));

        await Assert.ThrowsAnyAsync<Exception>(() =>
            dir.SetProductAttributesAsync(
                product.ProductId,
                [new ProductAttributeValueInput(weightId, "100", null, false)],
                CancellationToken.None));

        await Assert.ThrowsAnyAsync<Exception>(() =>
            dir.SetProductAttributesAsync(
                product.ProductId,
                [new ProductAttributeValueInput(materialId, "ignored", Guid.NewGuid(), false)],
                CancellationToken.None));

        await Assert.ThrowsAnyAsync<Exception>(() =>
            dir.SetProductAttributesAsync(
                product.ProductId,
                [new ProductAttributeValueInput(colorId, "ignored", black, false)],
                CancellationToken.None));

        await dir.SetProductAttributesAsync(
            product.ProductId,
            [
                new ProductAttributeValueInput(screenId, "6.1", null, false),
                new ProductAttributeValueInput(waterproofId, "true", null, false),
                new ProductAttributeValueInput(noteId, "سری A", null, false),
                new ProductAttributeValueInput(materialId, "ignored", aluminum, false),
            ],
            CancellationToken.None);

        editor = await dir.GetProductAttributeEditorStateAsync(product.ProductId, "fa-IR", CancellationToken.None);
        Assert.False(editor.Fields.First(f => f.DefinitionId == screenId).IsMissingRequired);
        Assert.Equal("6.1 inch", editor.Fields.First(f => f.DefinitionId == screenId).DisplayValue);
        Assert.Equal("بله", editor.Fields.First(f => f.DefinitionId == waterproofId).DisplayValue);
        Assert.Equal("آلومینیوم", editor.Fields.First(f => f.DefinitionId == materialId).DisplayValue);
        Assert.Equal(aluminum, editor.Fields.First(f => f.DefinitionId == materialId).CurrentEnumOptionId);
        Assert.True(editor.Readiness.IsComplete);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            dir.SetProductAttributesAsync(
                product.ProductId,
                [new ProductAttributeValueInput(screenId, null, null, true)],
                CancellationToken.None));

        await dir.SetProductAttributesAsync(
            product.ProductId,
            [new ProductAttributeValueInput(noteId, null, null, true)],
            CancellationToken.None);
        Assert.False(await db.ProductAttributeValues.AnyAsync(
            v => v.ProductId == product.ProductId && v.DefinitionId == noteId));

        await dir.SetProductAttributesAsync(
            product.ProductId,
            [
                new ProductAttributeValueInput(noteId, "سری B", null, false),
                new ProductAttributeValueInput(materialId, "ignored", plastic, false),
            ],
            CancellationToken.None);

        var report = await dir.PreviewCategoryChangeReportAsync(
            product.ProductId, other.CategoryId, "fa-IR", CancellationToken.None);
        Assert.Equal(1, report.CompatiblePreservedCount); // note
        Assert.True(report.OrphanCount >= 3); // screen, waterproof, material
        Assert.Equal(1, report.NewlyRequiredMissingCount);
        Assert.Contains(report.NewlyRequiredLabels, l => l.Contains("نویسنده", StringComparison.Ordinal));
        Assert.Contains("حفظ می‌شود", report.MessageFa);
        Assert.Contains("وجود ندارد", report.MessageFa);
        Assert.True(await db.ProductAttributeValues.CountAsync(v => v.ProductId == product.ProductId) >= 3);

        var readiness = await dir.GetProductAttributeReadinessAsync(product.ProductId, CancellationToken.None);
        Assert.True(readiness.IsComplete);
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
