using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Offer.Domain;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Pricing.Infrastructure.Persistence;
using Tooba.Party.Application;
using Tooba.Tax.Infrastructure.Persistence;

namespace Tooba.Host.Admin;

/// <summary>
/// ترکیب خواندنی Workspace در Host. هر DbContext جدا پرس‌وجو می‌شود؛ SQL بین schemaها JOIN نمی‌شود.
/// </summary>
public sealed class ProductWorkspaceComposer
{
    private readonly CatalogDbContext _catalog;
    private readonly OfferDbContext _offers;
    private readonly PricingDbContext _prices;
    private readonly InventoryDbContext _inventory;
    private readonly TaxDbContext _tax;
    private readonly IPartyLookupGateway _parties;

    /// <summary>
    /// سازندهٔ ترکیب Host. جستجوی نام فروشنده جدا از Offer است و JOIN بین‌schema نیست.
    /// </summary>
    public ProductWorkspaceComposer(
        CatalogDbContext catalog,
        OfferDbContext offers,
        PricingDbContext prices,
        InventoryDbContext inventory,
        TaxDbContext tax,
        IPartyLookupGateway parties)
    {
        _catalog = catalog;
        _offers = offers;
        _prices = prices;
        _inventory = inventory;
        _tax = tax;
        _parties = parties;
    }

    /// <summary>
    /// فهرست محصولات Catalog برای ورود به Workspace.
    /// </summary>
    public async Task<IReadOnlyList<AdminProductListItem>> ListAsync(CancellationToken cancellationToken)
    {
        var products = await _catalog.Products.AsNoTracking().OrderByDescending(x => x.UpdatedAt).Take(100).ToListAsync(cancellationToken);
        var names = await LoadNamesAsync(CatalogLocalizedOwnerKind.Product, products.Select(x => x.ProductId).ToList(), cancellationToken);
        var variantRows = await _catalog.Variants.AsNoTracking().Select(x => new { x.ProductId, x.VariantId }).ToListAsync(cancellationToken);
        var offers = await _offers.Offers.AsNoTracking().Select(x => x.CatalogVariantId).ToListAsync(cancellationToken);
        return products.Select(product =>
        {
            var variantIds = variantRows.Where(v => v.ProductId == product.ProductId).Select(v => v.VariantId).ToList();
            return new AdminProductListItem(
                product.ProductId,
                names.GetValueOrDefault(product.ProductId) ?? product.SlugSeam ?? product.ProductId.ToString("N")[..8],
                product.Status.ToString(),
                variantIds.Count,
                offers.Count(id => variantIds.Contains(id)),
                product.UpdatedAt);
        }).ToList();
    }

    /// <summary>
    /// Workspace یک محصول را از برش‌های ماژولی می‌سازد.
    /// </summary>
    public async Task<ProductWorkspaceView?> GetAsync(Guid productId, ProductWorkspacePermissions permissions, CancellationToken cancellationToken)
    {
        var product = await _catalog.Products.AsNoTracking().SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken);
        if (product is null)
        {
            return null;
        }

        var productNames = await LoadNamesAsync(CatalogLocalizedOwnerKind.Product, [productId], cancellationToken);
        var variants = await _catalog.Variants.AsNoTracking().Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        var variantIds = variants.Select(x => x.VariantId).ToList();
        var axes = await _catalog.VariantAttributeValues.AsNoTracking()
            .Where(x => variantIds.Contains(x.VariantId))
            .ToListAsync(cancellationToken);
        var defs = await _catalog.AttributeDefinitions.AsNoTracking().ToDictionaryAsync(x => x.DefinitionId, cancellationToken);
        var productAttrs = await _catalog.ProductAttributeValues.AsNoTracking().Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        var media = await _catalog.MediaReferences.AsNoTracking().Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        var categoryLinks = await _catalog.ProductCategories.AsNoTracking().Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        var categoryNames = await LoadNamesAsync(CatalogLocalizedOwnerKind.Category, categoryLinks.Select(x => x.CategoryId).ToList(), cancellationToken);
        var brandName = product.BrandId is Guid brandId
            ? (await LoadNamesAsync(CatalogLocalizedOwnerKind.Brand, [brandId], cancellationToken)).GetValueOrDefault(brandId)
            : null;

        var offers = variantIds.Count == 0
            ? []
            : await _offers.Offers.AsNoTracking().Where(x => variantIds.Contains(x.CatalogVariantId)).ToListAsync(cancellationToken);
        var offerIds = offers.Select(x => x.OfferId).ToList();
        var prices = offerIds.Count == 0
            ? []
            : await _prices.Prices.AsNoTracking().Where(x => offerIds.Contains(x.OfferId)).ToListAsync(cancellationToken);
        var positions = offerIds.Count == 0
            ? []
            : await _inventory.Positions.AsNoTracking().Where(x => offerIds.Contains(x.OfferId)).ToListAsync(cancellationToken);
        var locationIds = positions.Select(x => x.LocationId).Distinct().ToList();
        var locations = locationIds.Count == 0
            ? []
            : await _inventory.Locations.AsNoTracking().Where(x => locationIds.Contains(x.LocationId)).ToListAsync(cancellationToken);
        var taxRows = offerIds.Count == 0
            ? []
            : await _tax.OfferClassifications.AsNoTracking().Where(x => offerIds.Contains(x.OfferId)).ToListAsync(cancellationToken);
        var taxCats = taxRows.Count == 0
            ? []
            : await _tax.Categories.AsNoTracking().Where(x => taxRows.Select(r => r.CategoryId).Contains(x.CategoryId)).ToListAsync(cancellationToken);

        var offerViews = new List<ProductOfferView>(offers.Count);
        foreach (var offer in offers)
        {
            var seller = await _parties.FindByIdAsync(offer.SellerPartyId, cancellationToken);
            offerViews.Add(new ProductOfferView(
                offer.OfferId,
                offer.CatalogVariantId,
                offer.SellerPartyId,
                seller?.DisplayName ?? "فروشنده",
                offer.Status.ToString(),
                offer.Channel.ToString(),
                offer.SellerSku));
        }
        var priceViews = prices.Select(p => new ProductPriceView(
            p.PriceId,
            p.OfferId,
            p.Market,
            p.Currency,
            p.Amount,
            p.Status.ToString(),
            p.ValidFrom,
            p.ValidTo)).ToList();
        var taxViews = taxRows.Select(row =>
        {
            var cat = taxCats.Single(c => c.CategoryId == row.CategoryId);
            return new ProductTaxView(row.OfferId, cat.CategoryId, cat.Code, cat.DisplayName);
        }).ToList();
        var stockViews = positions.Select(pos =>
        {
            var loc = locations.Single(l => l.LocationId == pos.LocationId);
            return new ProductStockView(pos.OfferId, loc.LocationId, loc.Code, loc.Name, pos.OnHand, pos.Reserved, pos.Available);
        }).ToList();

        var variantViews = variants.Select(v => new ProductVariantView(
            v.VariantId,
            v.CombinationFingerprint,
            v.Status.ToString(),
            offers.Count(o => o.CatalogVariantId == v.VariantId),
            stockViews.Where(s => offers.Any(o => o.OfferId == s.OfferId && o.CatalogVariantId == v.VariantId))
                .Select(s => s.LocationId)
                .Distinct()
                .Count())).ToList();

        var attrViews = productAttrs.Select(a => new ProductAttributeView(
            defs.TryGetValue(a.DefinitionId, out var def) ? def.Code : a.DefinitionId.ToString("N")[..8],
            a.CanonicalValue,
            false)).Concat(axes.Select(a => new ProductAttributeView(
            defs.TryGetValue(a.DefinitionId, out var def) ? def.Code : "axis",
            a.CanonicalValue,
            true))).ToList();

        var title = productNames.GetValueOrDefault(productId) ?? product.SlugSeam ?? "untitled";
        var warnings = new List<string>();
        if (string.IsNullOrWhiteSpace(title) || title == "untitled")
        {
            warnings.Add("missing-title");
        }

        if (media.Count == 0)
        {
            warnings.Add("missing-image");
        }

        if (offers.All(o => o.Status != OfferStatus.Active))
        {
            warnings.Add("no-active-offer");
        }

        if (priceViews.Count == 0)
        {
            warnings.Add("no-price");
        }

        if (stockViews.All(s => s.Available <= 0))
        {
            warnings.Add("no-inventory");
        }

        if (string.IsNullOrWhiteSpace(product.SeoTitleSeam) || string.IsNullOrWhiteSpace(product.SlugSeam))
        {
            warnings.Add("seo-incomplete");
        }

        var purchasable = offers.Any(o => o.Status == OfferStatus.Active)
            && priceViews.Count > 0
            && stockViews.Any(s => s.Available > 0);
        var publication = new ProductPublicationView(product.Status.ToString(), purchasable, warnings);

        return new ProductWorkspaceView(
            product.ProductId,
            title,
            product.Status.ToString(),
            product.Kind.ToString(),
            brandName,
            categoryLinks.Select(l => categoryNames.GetValueOrDefault(l.CategoryId) ?? l.CategoryId.ToString("N")[..8]).ToList(),
            attrViews,
            variantViews,
            media.Select((m, i) => new ProductMediaView(m.MediaAssetId, i == 0)).ToList(),
            offerViews,
            priceViews,
            taxViews,
            stockViews,
            new ProductSeoView(product.SlugSeam, product.SeoTitleSeam, "Semantic Content != Page Composition"),
            publication,
            [new ProductHistoryItem("activity", "Workspace opened from Catalog identity.", product.UpdatedAt)],
            [new ProductHistoryItem("audit", "Catalog row loaded; Offer/Price/Stock queried separately.", product.UpdatedAt)],
            permissions,
            product.UpdatedAt,
            warnings,
            ["media-binary-upload", "promotion-write", "full-content-studio"]);
    }

    /// <summary>
    /// عنوان محلی محصول را با قفل خوش‌بینانه به‌روز می‌کند.
    /// </summary>
    public async Task<ProductWorkspaceView> UpdateCatalogTitleAsync(
        Guid productId,
        string locale,
        string title,
        DateTimeOffset expectedUpdatedAt,
        ProductWorkspacePermissions permissions,
        CancellationToken cancellationToken)
    {
        if (!permissions.CanEditCatalog)
        {
            throw new PlatformHttpException(403, "Forbidden", "workspace.permission.denied");
        }

        var product = await _catalog.Products.SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken)
            ?? throw new PlatformHttpException(404, "Not Found", "workspace.product.missing");
        if (product.UpdatedAt != expectedUpdatedAt)
        {
            throw new PlatformHttpException(409, "Conflict", "workspace.catalog.stale");
        }

        var row = await _catalog.LocalizedTexts.SingleOrDefaultAsync(
            x => x.OwnerKind == CatalogLocalizedOwnerKind.Product && x.OwnerId == productId && x.FieldKey == "name" && x.Locale == locale,
            cancellationToken);
        if (row is null)
        {
            _catalog.LocalizedTexts.Add(CatalogLocalizedText.Create(CatalogLocalizedOwnerKind.Product, productId, "name", locale, title));
        }
        else
        {
            row.Value = title.Trim();
        }

        product.UpdatedAt = DateTimeOffset.UtcNow;
        await _catalog.SaveChangesAsync(cancellationToken);
        return (await GetAsync(productId, permissions, cancellationToken))!;
    }

    private async Task<Dictionary<Guid, string>> LoadNamesAsync(
        CatalogLocalizedOwnerKind kind,
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var rows = await _catalog.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == kind && ids.Contains(x.OwnerId) && x.FieldKey == "name")
            .ToListAsync(cancellationToken);
        return rows
            .GroupBy(x => x.OwnerId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Locale == "fa" ? 0 : 1).First().Value);
    }
}
