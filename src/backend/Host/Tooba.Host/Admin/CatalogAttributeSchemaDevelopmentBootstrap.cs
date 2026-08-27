using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Inventory.Application;
using Tooba.Inventory.Domain;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Offer.Application;
using Tooba.Offer.Domain;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Party.Application;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Pricing.Application;
using Tooba.Tax.Application;
using Tooba.Tax.Domain;

namespace Tooba.Host.Admin;

/// <summary>
/// دانهٔ Development برای Category Attribute Schema + محورهای Variant موبایل.
/// Idempotent است؛ ماتریس کامل ترکیبی تولید نمی‌کند و Brand را به‌عنوان attribute تکرار نمی‌کند.
/// </summary>
internal static class CatalogAttributeSchemaDevelopmentBootstrap
{
    internal const string MobileCategoryMarker = "schema-mobile-category";
    internal const string DemoProductSlug = "schema-mobile-demo-phone";
    private const string DemoSellerSkuPrefix = "SCHEMA-PHONE";

    /// <summary>
    /// schema موبایل و یک محصول نمونه را در صورت نبودن درج می‌کند.
    /// </summary>
    public static async Task ApplyAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var registry = provider.GetRequiredService<ControlPlaneRegistry>();
        if (!registry.Tenants.TryGetValue("store-alpha", out var tenant) || tenant.Status != TenantStatus.Active)
        {
            return;
        }

        var assigner = provider.GetRequiredService<ICommerceContextAssigner>();
        assigner.Assign(new CommerceContext(
            new EditionContext(registry.Edition, registry.DeploymentId),
            new TenantContext(
                tenant.TenantId,
                tenant.Status,
                tenant.ConnectionReference,
                tenant.DisplayName,
                tenant.ThemeReference,
                tenant.DefaultMarketReference,
                tenant.Hosts[0],
                tenant.PrimaryDomain),
            tenant.ConnectionReference,
            "catalog-attribute-schema-seed"));

        var catalog = provider.GetRequiredService<ICatalogDirectory>();
        var db = provider.GetRequiredService<CatalogDbContext>();

        if (await db.Products.AnyAsync(p => p.SlugSeam == DemoProductSlug))
        {
            await EnsurePublishedAndSellableAsync(provider, db, CancellationToken.None);
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

        await EnsureBoundAsync(catalog, db, mobile, colorId, 10, null);
        await EnsureBoundAsync(catalog, db, mobile, storageId, 20, null);
        await EnsureBoundAsync(catalog, db, mobile, ramId, 30, null);
        await EnsureBoundAsync(catalog, db, mobile, screenId, 40, true);

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
            CancellationToken.None);
        await catalog.AssignCategoryAsync(product.ProductId, mobile, CancellationToken.None);
        await catalog.SetProductAttributeAsync(product.ProductId, screenId, "6.1", null, CancellationToken.None);
        await catalog.SetProductAttributeAsync(product.ProductId, ramId, "ignored", ram8, CancellationToken.None);
        await catalog.SetProductVariantAxesAsync(product.ProductId, [colorId, storageId], CancellationToken.None);

        // چند ترکیب نمونه برای اثبات محورها؛ FULL_VARIANT_MATRIX تولید نمی‌شود.
        await catalog.CreateVariantAsync(product.ProductId, "PHONE-BLK-128", [(colorId, "ignored", black), (storageId, "ignored", storage128)], CancellationToken.None);
        await catalog.CreateVariantAsync(product.ProductId, "PHONE-BLU-256", [(colorId, "ignored", blue), (storageId, "ignored", storage256)], CancellationToken.None);
        await catalog.PublishCategoryAsync(mobile, CancellationToken.None);
        await catalog.PublishProductAsync(product.ProductId, CancellationToken.None);
        await EnsurePublishedAndSellableAsync(provider, db, CancellationToken.None);
    }

    /// <summary>
    /// انتشار و Offer/قیمت/موجودی حداقلی برای PDP سازگار با فروش فعلی؛ ماتریس کامل نیست.
    /// </summary>
    private static async Task EnsurePublishedAndSellableAsync(
        IServiceProvider provider,
        CatalogDbContext catalogDb,
        CancellationToken cancellationToken)
    {
        var catalog = provider.GetRequiredService<ICatalogDirectory>();
        var product = await catalogDb.Products.SingleAsync(p => p.SlugSeam == DemoProductSlug, cancellationToken);
        var categoryIds = await catalogDb.ProductCategories.AsNoTracking()
            .Where(x => x.ProductId == product.ProductId)
            .Select(x => x.CategoryId)
            .ToListAsync(cancellationToken);
        foreach (var categoryId in categoryIds)
        {
            await catalog.PublishCategoryAsync(categoryId, cancellationToken);
        }

        if (product.Status != CatalogPublicationStatus.Published)
        {
            await catalog.PublishProductAsync(product.ProductId, CancellationToken.None);
        }

        var variants = await catalogDb.Variants.AsNoTracking()
            .Where(v => v.ProductId == product.ProductId)
            .OrderBy(v => v.CatalogCodeSeam)
            .ToListAsync(cancellationToken);
        if (variants.Count == 0)
        {
            return;
        }

        var offers = provider.GetRequiredService<IOfferDirectory>();
        var offerDb = provider.GetRequiredService<OfferDbContext>();
        var parties = provider.GetRequiredService<IPartyDirectory>();
        var partyDb = provider.GetRequiredService<PartyDbContext>();
        var prices = provider.GetRequiredService<IPriceDirectory>();
        var tax = provider.GetRequiredService<ITaxDirectory>();
        var inventory = provider.GetRequiredService<IInventoryDirectory>();
        var inventoryDb = provider.GetRequiredService<InventoryDbContext>();

        var seller = await partyDb.Parties.AsNoTracking()
            .OrderBy(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        Guid sellerPartyId;
        if (seller is null)
        {
            var created = await parties.CreateOrganizationAsync(
                "فروشنده schema موبایل",
                "Schema Mobile Seller Legal",
                cancellationToken);
            sellerPartyId = created.PartyId;
        }
        else
        {
            sellerPartyId = seller.PartyId;
        }

        var start = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var amount = 12_500_000m;
        var locationCode = "WH-SCHEMA-MOBILE";
        var location = await inventoryDb.Locations.AsNoTracking()
            .SingleOrDefaultAsync(l => l.Code == locationCode, cancellationToken);
        var locationId = location?.LocationId
            ?? await inventory.CreateLocationAsync(locationCode, "انبار schema موبایل", cancellationToken);

        foreach (var variant in variants)
        {
            var sku = $"{DemoSellerSkuPrefix}-{variant.CatalogCodeSeam ?? variant.VariantId.ToString("N")[..8]}";
            if (await offerDb.Offers.AnyAsync(
                    o => o.SellerPartyId == sellerPartyId && o.SellerSku == sku,
                    cancellationToken))
            {
                continue;
            }

            var offer = await offers.CreateOfferAsync(
                variant.VariantId,
                sellerPartyId,
                SalesChannel.Marketplace,
                sku,
                cancellationToken);
            await offers.ActivateAsync(offer.OfferId, cancellationToken);
            var price = await prices.CreatePriceAsync(
                offer.OfferId,
                "IR",
                SalesChannel.Marketplace,
                amount,
                "IRR",
                start,
                null,
                cancellationToken);
            await prices.ActivateAsync(price.PriceId, cancellationToken);

            var taxCode = $"sch-{offer.OfferId:N}"[..20];
            var taxCategory = await tax.CreateCategoryAsync(taxCode, "schema phone", cancellationToken);
            await tax.AssignOfferCategoryAsync(offer.OfferId, taxCategory.CategoryId, cancellationToken);

            var stock = await inventory.OpenPositionAsync(offer.OfferId, locationId, cancellationToken);
            await inventory.AdjustAsync(stock, StockAdjustmentKind.Increase, 5, "schema-seed", null, cancellationToken);
            amount += 500_000m;
        }
    }

    private static async Task<Guid> EnsureMobileCategoryAsync(ICatalogDirectory catalog, CatalogDbContext db)
    {
        var existing = await db.LocalizedTexts.AsNoTracking()
            .Where(t => t.OwnerKind == CatalogLocalizedOwnerKind.Category
                && t.FieldKey == "name"
                && t.Locale == "en-US"
                && t.Value == "Mobile")
            .Select(t => t.OwnerId)
            .FirstOrDefaultAsync();
        if (existing != Guid.Empty)
        {
            return existing;
        }

        var category = await catalog.CreateCategoryAsync(
            null,
            new Dictionary<string, string> { ["fa-IR"] = "موبایل", ["en-US"] = "Mobile" },
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
        bool? requiredOverride)
    {
        if (await db.CategoryAttributeBindings.AnyAsync(b => b.CategoryId == categoryId && b.DefinitionId == definitionId))
        {
            return;
        }

        await catalog.BindCategoryAttributeAsync(categoryId, definitionId, displayOrder, requiredOverride, CancellationToken.None);
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
