#pragma warning disable CS1591
using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Development;

/// <summary>
/// Development seed data for the live storefront product workspace demo product
/// (<c>workspace-live-shirt</c>), its operator-facing copy, and the Admin R3 preview gallery.
/// Catalog-owned; its only persistence access is its own <see cref="CatalogDbContext"/>.
/// </summary>
public sealed class WorkspaceDemoProductSeed(
    CatalogDbContext catalogDb,
    ICatalogDirectory catalog)
{
    /// <summary>Slug seam of the live workspace demo product.</summary>
    public const string SeedSlug = "workspace-live-shirt";

    /// <summary>Slug seam of the Admin R3 draft product.</summary>
    public const string DraftSlug = "admin-r3-draft-scarf";

    /// <summary>Slug seam of the Admin R3 archived product.</summary>
    public const string ArchivedSlug = "admin-r3-archived-hat";

    /// <summary>Stable media asset id of the front view.</summary>
    public const string FrontMediaId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";

    /// <summary>Stable media asset id of the back view.</summary>
    public const string BackMediaId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";

    /// <summary>Stable media asset id of the collar detail.</summary>
    public const string CollarMediaId = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";

    /// <summary>Stable media asset id of the sleeve detail.</summary>
    public const string SleeveMediaId = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";

    /// <summary>Stable media asset id of the mannequin view.</summary>
    public const string MannequinMediaId = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";

    /// <summary>Brand slug seam of the demo product.</summary>
    public const string BrandSlugSeam = "tooba-live";

    /// <summary>Attribute code of the colour axis.</summary>
    public const string ColorAttributeCode = "color";

    /// <summary>Attribute option code of the black colour.</summary>
    public const string BlackOptionCode = "black";

    /// <summary>Variant catalog code seam of the demo product.</summary>
    public const string VariantCodeSeam = "LIVE-SHIRT-BLK";

    /// <summary>Localized product name (fa-IR).</summary>
    public const string ProductNameFa = "پیراهن مردانه لینن";

    /// <summary>Localized product name (en-US).</summary>
    public const string ProductNameEn = "Men's Linen Shirt";

    /// <summary>Legacy operator-facing product name (fa-IR) rewritten by the refresh step.</summary>
    public const string LegacyProductNameFa = "پیراهن Workspace زنده";

    /// <summary>Legacy operator-facing product name (en-US) rewritten by the refresh step.</summary>
    public const string LegacyProductNameEn = "Live Workspace Shirt";

    /// <summary>
    /// Seeds the workspace demo product, returning the demo variant id. The Host caller reaches
    /// this branch only when the demo product is absent, mirroring the former unconditional
    /// create path exactly. Not idempotent on its own by design; seed only when absent.
    /// </summary>
    public async Task<Guid> SeedProductAsync(CancellationToken cancellationToken = default)
    {
        var categoryNames = new Dictionary<string, string>
        {
            ["fa-IR"] = "پوشاک",
            ["en-US"] = "Apparel",
        };
        var midNames = new Dictionary<string, string>
        {
            ["fa-IR"] = "پوشاک مردانه",
            ["en-US"] = "Men's apparel",
        };
        var leafNames = new Dictionary<string, string>
        {
            ["fa-IR"] = "پیراهن مردانه",
            ["en-US"] = "Men's shirts",
        };
        var brandNames = new Dictionary<string, string>
        {
            ["fa-IR"] = "آرمان",
            ["en-US"] = "Arman",
        };
        var productNames = new Dictionary<string, string>
        {
            ["fa-IR"] = ProductNameFa,
            ["en-US"] = ProductNameEn,
        };

        var root = await catalog.CreateCategoryAsync(null, categoryNames, cancellationToken);
        var mid = await catalog.CreateCategoryAsync(root.CategoryId, midNames, cancellationToken);
        var category = await catalog.CreateCategoryAsync(mid.CategoryId, leafNames, cancellationToken);
        var brand = await catalog.CreateBrandAsync(BrandSlugSeam, brandNames, cancellationToken);
        var colorId = await catalog.CreateAttributeDefinitionAsync(
            ColorAttributeCode,
            CatalogAttributeValueKind.Enumeration,
            isVariantAxis: true,
            new Dictionary<string, string> { ["fa-IR"] = "رنگ", ["en-US"] = "Color" },
            cancellationToken);
        var black = await catalog.AddAttributeOptionAsync(
            colorId,
            BlackOptionCode,
            new Dictionary<string, string> { ["fa-IR"] = "سیاه", ["en-US"] = "Black" },
            cancellationToken);

        var product = await catalog.CreateProductAsync(
            CatalogProductKind.PhysicalGood,
            SeedSlug,
            brand.BrandId,
            productNames,
            cancellationToken);
        await catalog.AssignCategoryAsync(product.ProductId, category.CategoryId, cancellationToken);
        await catalog.AttachMediaReferenceAsync(product.ProductId, Guid.Parse(FrontMediaId), "نمای جلو", cancellationToken);
        await catalog.AttachMediaReferenceAsync(product.ProductId, Guid.Parse(BackMediaId), "نمای پشت", cancellationToken);
        await catalog.AttachMediaReferenceAsync(product.ProductId, Guid.Parse(CollarMediaId), "جزئیات یقه", cancellationToken);
        await catalog.AttachMediaReferenceAsync(product.ProductId, Guid.Parse(SleeveMediaId), "جزئیات آستین", cancellationToken);
        await ProductPublishPrep.EnsureMinimalSeoForPublishAsync(
            catalog,
            product.ProductId,
            "توضیح سئو پیراهن زنده Workspace",
            cancellationToken);
        await catalog.PublishProductAsync(product.ProductId, cancellationToken);
        var variant = await catalog.CreateVariantAsync(
            product.ProductId,
            VariantCodeSeam,
            [(colorId, "ignored", black)],
            cancellationToken);
        return variant.VariantId;
    }

    /// <summary>
    /// Rewrites legacy operator-facing Catalog copy on the live demo product, its categories,
    /// and its brand. Idempotent; rewrites only the exact legacy values.
    /// </summary>
    public async Task RefreshOperatorFacingCopyAsync(CancellationToken cancellationToken = default)
    {
        await catalogDb.Products.AsNoTracking()
            .SingleAsync(item => item.SlugSeam == SeedSlug, cancellationToken);

        var changed = false;
        foreach (var text in catalogDb.LocalizedTexts.Where(item => item.FieldKey == "name"))
        {
            var persian = text.Locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase);
            if (text.OwnerKind == CatalogLocalizedOwnerKind.Product
                && text.Value is LegacyProductNameFa or LegacyProductNameEn)
            {
                text.Value = persian ? ProductNameFa : ProductNameEn;
                changed = true;
            }
            else if (text.OwnerKind == CatalogLocalizedOwnerKind.Category
                && text.Value is LegacyProductNameFa or LegacyProductNameEn or ProductNameFa or ProductNameEn)
            {
                text.Value = persian ? "پوشاک مردانه" : "Men's apparel";
                changed = true;
            }
            else if (text.OwnerKind == CatalogLocalizedOwnerKind.Brand
                && text.Value is LegacyProductNameFa or LegacyProductNameEn or ProductNameFa or ProductNameEn)
            {
                text.Value = persian ? "آرمان" : "Arman";
                changed = true;
            }
        }

        if (changed)
        {
            await catalogDb.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Idempotent Admin R3 preview enrichment: five-image gallery plus archived and draft
    /// products cloned from the live demo brand/category.
    /// </summary>
    public async Task EnsureAdminR3PreviewAsync(CancellationToken cancellationToken = default)
    {
        var live = await catalogDb.Products.AsNoTracking()
            .SingleOrDefaultAsync(p => p.SlugSeam == SeedSlug, cancellationToken);
        if (live is null)
        {
            return;
        }

        var gallery = new (string Id, string Alt)[]
        {
            (FrontMediaId, "نمای جلو"),
            (BackMediaId, "نمای پشت"),
            (CollarMediaId, "جزئیات یقه"),
            (SleeveMediaId, "جزئیات آستین"),
            (MannequinMediaId, "روی مانکن"),
        };
        foreach (var (id, alt) in gallery)
        {
            var mediaId = Guid.Parse(id);
            var exists = await catalogDb.MediaReferences.AsNoTracking()
                .AnyAsync(m => m.ProductId == live.ProductId && m.MediaAssetId == mediaId, cancellationToken);
            if (exists)
            {
                continue;
            }

            try
            {
                await catalog.AttachMediaReferenceAsync(live.ProductId, mediaId, alt, cancellationToken);
            }
            catch (InvalidOperationException)
            {
                // هم‌زمانی یا اتصال تکراری — نادیده
            }
        }

        if (!await catalogDb.Products.AnyAsync(p => p.SlugSeam == DraftSlug, cancellationToken))
        {
            var draft = await catalog.CreateProductAsync(
                CatalogProductKind.PhysicalGood,
                DraftSlug,
                live.BrandId,
                new Dictionary<string, string> { ["fa-IR"] = "شال پیش‌نویس R3", ["en-US"] = "R3 Draft Scarf" },
                cancellationToken);
            await catalog.AttachMediaReferenceAsync(
                draft.ProductId,
                Guid.Parse("11111111-aaaa-4aaa-8aaa-aaaaaaaaaaa1"),
                "پیش‌نمایش شال",
                cancellationToken);
        }

        if (!await catalogDb.Products.AnyAsync(p => p.SlugSeam == ArchivedSlug, cancellationToken))
        {
            var archived = await catalog.CreateProductAsync(
                CatalogProductKind.PhysicalGood,
                ArchivedSlug,
                live.BrandId,
                new Dictionary<string, string> { ["fa-IR"] = "کلاه بایگانی R3", ["en-US"] = "R3 Archived Hat" },
                cancellationToken);
            var liveCategoryId = await catalogDb.ProductCategories.AsNoTracking()
                .Where(x => x.ProductId == live.ProductId)
                .Select(x => x.CategoryId)
                .FirstAsync(cancellationToken);
            await catalog.AssignCategoryAsync(archived.ProductId, liveCategoryId, cancellationToken);
            await catalog.AttachMediaReferenceAsync(
                archived.ProductId,
                Guid.Parse("22222222-bbbb-4bbb-8bbb-bbbbbbbbbbb2"),
                "پیش‌نمایش کلاه",
                cancellationToken);
            await ProductPublishPrep.EnsureMinimalSeoForPublishAsync(
                catalog,
                archived.ProductId,
                "توضیح سئو کلاه بایگانی",
                cancellationToken);
            await catalog.PublishProductAsync(archived.ProductId, cancellationToken);
            await catalog.ArchiveProductAsync(archived.ProductId, cancellationToken);
        }
    }
}
