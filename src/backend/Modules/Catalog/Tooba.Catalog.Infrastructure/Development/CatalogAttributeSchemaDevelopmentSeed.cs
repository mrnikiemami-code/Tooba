#pragma warning disable CS1591
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Development;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Development;

/// <summary>
/// دانهٔ Development برای Category Attribute Schema + محورهای Variant موبایل.
/// Idempotent است؛ ماتریس کامل ترکیبی تولید نمی‌کند و Brand را به‌عنوان attribute تکرار نمی‌کند.
/// Offer/Pricing/Inventory/Tax/Party via <see cref="ICatalogAttributeSchemaSellableEnricher"/>.
/// </summary>
public static class CatalogAttributeSchemaDevelopmentSeed
{
    public const string MobileCategoryMarker = "schema-mobile-category";
    public const string DemoProductSlug = "schema-mobile-demo-phone";

    /// <summary>
    /// schema موبایل و یک محصول نمونه را در صورت نبودن درج می‌کند؛ سپس enricher فروش‌پذیری را صدا می‌زند.
    /// </summary>
    public static async Task ApplyAsync(IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        var catalog = provider.GetRequiredService<ICatalogDirectory>();
        var db = provider.GetRequiredService<CatalogDbContext>();

        if (await db.Products.AnyAsync(p => p.SlugSeam == DemoProductSlug, cancellationToken))
        {
            await InvokeEnricherAsync(provider, cancellationToken);
            return;
        }

        var mobile = await EnsureMobileCategoryAsync(catalog, db);
        var colorId = await EnsureDefinitionAsync(
            catalog,
            db,
            "color",
            CatalogAttributeValueKind.Enumeration,
            isVariantAxis: true,
            names: new Dictionary<string, string> { ["fa-IR"] = "رنگ", ["en-US"] = "Color" },
            meta: (null, false, true, false, false, 10, null, null, null, true));
        var storageId = await EnsureDefinitionAsync(
            catalog,
            db,
            "storage",
            CatalogAttributeValueKind.Enumeration,
            isVariantAxis: true,
            names: new Dictionary<string, string> { ["fa-IR"] = "حافظه", ["en-US"] = "Storage" },
            meta: (null, false, true, false, false, 20, null, null, null, true));
        var ramId = await EnsureDefinitionAsync(
            catalog,
            db,
            "ram",
            CatalogAttributeValueKind.Enumeration,
            isVariantAxis: false,
            names: new Dictionary<string, string> { ["fa-IR"] = "رم", ["en-US"] = "RAM" },
            meta: (null, false, true, true, false, 30, null, null, null, true));
        var screenId = await EnsureDefinitionAsync(
            catalog,
            db,
            "screen_size",
            CatalogAttributeValueKind.Number,
            isVariantAxis: false,
            names: new Dictionary<string, string> { ["fa-IR"] = "اندازه صفحه", ["en-US"] = "Screen Size" },
            meta: ("inch", false, true, true, false, 40, 4m, 10m, null, true));

        await EnsureBoundAsync(catalog, db, mobile, colorId, 10, new CategoryAttributeAssignmentFlags(false, true, true, false));
        await EnsureBoundAsync(catalog, db, mobile, storageId, 20, new CategoryAttributeAssignmentFlags(false, true, true, false));
        await EnsureBoundAsync(catalog, db, mobile, ramId, 30, new CategoryAttributeAssignmentFlags(false, true, false, true));
        await EnsureBoundAsync(catalog, db, mobile, screenId, 40, new CategoryAttributeAssignmentFlags(true, true, false, true));

        var black = await EnsureOptionAsync(catalog, db, colorId, "black", new Dictionary<string, string> { ["fa-IR"] = "مشکی", ["en-US"] = "Black" });
        var blue = await EnsureOptionAsync(catalog, db, colorId, "blue", new Dictionary<string, string> { ["fa-IR"] = "آبی", ["en-US"] = "Blue" });
        var storage128 = await EnsureOptionAsync(catalog, db, storageId, "128gb", new Dictionary<string, string> { ["fa-IR"] = "۱۲۸ گیگ", ["en-US"] = "128GB" });
        var storage256 = await EnsureOptionAsync(catalog, db, storageId, "256gb", new Dictionary<string, string> { ["fa-IR"] = "۲۵۶ گیگ", ["en-US"] = "256GB" });
        var ram8 = await EnsureOptionAsync(catalog, db, ramId, "8gb", new Dictionary<string, string> { ["fa-IR"] = "۸ گیگ", ["en-US"] = "8GB" });

        var product = await catalog.CreateProductAsync(
            CatalogProductKind.PhysicalGood,
            DemoProductSlug,
            null,
            new Dictionary<string, string> { ["fa-IR"] = "گوشی نمونه schema", ["en-US"] = "Schema demo phone" },
            cancellationToken);
        await catalog.AssignCategoryAsync(product.ProductId, mobile, cancellationToken);
        await catalog.SetProductAttributeAsync(product.ProductId, screenId, "6.1", null, cancellationToken);
        await catalog.SetProductAttributeAsync(product.ProductId, ramId, "ignored", ram8, cancellationToken);
        await catalog.SetProductVariantAxesAsync(product.ProductId, [colorId, storageId], cancellationToken);

        // چند ترکیب نمونه برای اثبات محورها؛ FULL_VARIANT_MATRIX تولید نمی‌شود.
        await catalog.CreateVariantAsync(product.ProductId, "PHONE-BLK-128", [(colorId, "ignored", black), (storageId, "ignored", storage128)], cancellationToken);
        await catalog.CreateVariantAsync(product.ProductId, "PHONE-BLU-256", [(colorId, "ignored", blue), (storageId, "ignored", storage256)], cancellationToken);
        await catalog.AttachMediaReferenceAsync(
            product.ProductId, Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"), cancellationToken);
        await ProductPublishPrep.EnsureMinimalSeoForPublishAsync(
            catalog, product.ProductId, "توضیح سئو گوشی نمونه schema", cancellationToken);
        await catalog.PublishCategoryAsync(mobile, cancellationToken);
        await catalog.PublishProductAsync(product.ProductId, cancellationToken);
        await InvokeEnricherAsync(provider, cancellationToken);
    }

    private static async Task InvokeEnricherAsync(IServiceProvider provider, CancellationToken cancellationToken)
    {
        var enricher = provider.GetService<ICatalogAttributeSchemaSellableEnricher>();
        if (enricher is not null)
        {
            await enricher.EnsurePublishedAndSellableAsync(cancellationToken);
        }
    }

    private static async Task<Guid> EnsureMobileCategoryAsync(ICatalogDirectory catalog, CatalogDbContext db)
    {
        var existing = await db.LocalizedTexts.AsNoTracking()
            .Where(t => t.OwnerKind == CatalogLocalizedOwnerKind.Category
                && t.FieldKey == "name"
                && t.Locale == "en-US"
                && t.Value == "Mobile phones")
            .Select(t => t.OwnerId)
            .FirstOrDefaultAsync();
        if (existing != Guid.Empty)
        {
            return existing;
        }

        // Prefer existing L3 leaf if earlier seeds already created one under موبایل.
        var legacyMobile = await db.LocalizedTexts.AsNoTracking()
            .Where(t => t.OwnerKind == CatalogLocalizedOwnerKind.Category
                && t.FieldKey == "name"
                && t.Locale == "en-US"
                && t.Value == "Mobile")
            .Select(t => t.OwnerId)
            .FirstOrDefaultAsync();
        if (legacyMobile != Guid.Empty)
        {
            var parentById = await db.Categories.AsNoTracking()
                .ToDictionaryAsync(x => x.CategoryId, x => x.ParentCategoryId);
            if (CatalogCategoryTreeRules.IsAssignableProductCategory(legacyMobile, parentById))
            {
                return legacyMobile;
            }

            var mid = await catalog.CreateCategoryAsync(
                legacyMobile,
                new Dictionary<string, string> { ["fa-IR"] = "موبایل و تبلت", ["en-US"] = "Mobile & tablet" },
                CancellationToken.None);
            await catalog.PublishCategoryAsync(mid.CategoryId, CancellationToken.None);
            var leaf = await catalog.CreateCategoryAsync(
                mid.CategoryId,
                new Dictionary<string, string> { ["fa-IR"] = "گوشی موبایل", ["en-US"] = "Mobile phones" },
                CancellationToken.None);
            await catalog.PublishCategoryAsync(leaf.CategoryId, CancellationToken.None);
            _ = MobileCategoryMarker;
            return leaf.CategoryId;
        }

        var root = await catalog.CreateCategoryAsync(
            null,
            new Dictionary<string, string> { ["fa-IR"] = "کالای دیجیتال", ["en-US"] = "Digital goods" },
            CancellationToken.None);
        await catalog.PublishCategoryAsync(root.CategoryId, CancellationToken.None);
        var midFresh = await catalog.CreateCategoryAsync(
            root.CategoryId,
            new Dictionary<string, string> { ["fa-IR"] = "موبایل و تبلت", ["en-US"] = "Mobile & tablet" },
            CancellationToken.None);
        await catalog.PublishCategoryAsync(midFresh.CategoryId, CancellationToken.None);
        var category = await catalog.CreateCategoryAsync(
            midFresh.CategoryId,
            new Dictionary<string, string> { ["fa-IR"] = "گوشی موبایل", ["en-US"] = "Mobile phones" },
            CancellationToken.None);
        await catalog.PublishCategoryAsync(category.CategoryId, CancellationToken.None);
        _ = MobileCategoryMarker;
        return category.CategoryId;
    }

    private static async Task<Guid> EnsureDefinitionAsync(
        ICatalogDirectory catalog,
        CatalogDbContext db,
        string code,
        CatalogAttributeValueKind kind,
        bool isVariantAxis,
        Dictionary<string, string> names,
        (string? Unit, bool IsRequired, bool IsFilterable, bool IsComparable, bool IsMultivalue, int DisplayOrder, decimal? Min, decimal? Max, int? MaxLength, bool IsActive) meta)
    {
        var existing = await db.AttributeDefinitions.AsNoTracking()
            .SingleOrDefaultAsync(d => d.Code == code);
        if (existing is not null)
        {
            await catalog.UpdateAttributeDefinitionAsync(
                existing.DefinitionId,
                meta.Unit,
                meta.IsRequired,
                meta.IsFilterable,
                meta.IsComparable,
                meta.IsMultivalue,
                meta.DisplayOrder,
                meta.Min,
                meta.Max,
                meta.MaxLength,
                meta.IsActive,
                CancellationToken.None);
            return existing.DefinitionId;
        }

        var id = await catalog.CreateAttributeDefinitionAsync(code, kind, isVariantAxis, names, CancellationToken.None);
        await catalog.UpdateAttributeDefinitionAsync(
            id,
            meta.Unit,
            meta.IsRequired,
            meta.IsFilterable,
            meta.IsComparable,
            meta.IsMultivalue,
            meta.DisplayOrder,
            meta.Min,
            meta.Max,
            meta.MaxLength,
            meta.IsActive,
            CancellationToken.None);
        return id;
    }

    private static async Task EnsureBoundAsync(
        ICatalogDirectory catalog,
        CatalogDbContext db,
        Guid categoryId,
        Guid definitionId,
        int displayOrder,
        CategoryAttributeAssignmentFlags flags)
    {
        if (await db.CategoryAttributeBindings.AnyAsync(b => b.CategoryId == categoryId && b.DefinitionId == definitionId))
        {
            return;
        }

        await catalog.BindCategoryAttributeAsync(categoryId, definitionId, displayOrder, flags, CancellationToken.None);
    }

    private static async Task<Guid> EnsureOptionAsync(
        ICatalogDirectory catalog,
        CatalogDbContext db,
        Guid definitionId,
        string code,
        Dictionary<string, string> names)
    {
        var existing = await db.AttributeOptions.AsNoTracking()
            .SingleOrDefaultAsync(o => o.DefinitionId == definitionId && o.Code == code);
        if (existing is not null)
        {
            return existing.OptionId;
        }

        return await catalog.AddAttributeOptionAsync(definitionId, code, names, CancellationToken.None);
    }
}