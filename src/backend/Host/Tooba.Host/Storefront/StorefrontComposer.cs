using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Offer.Domain;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Party.Application;
using Tooba.Pricing.Domain;
using Tooba.Pricing.Infrastructure.Persistence;
using Tooba.Tax.Infrastructure.Persistence;

namespace Tooba.Host.Storefront;

/// <summary>
/// ترکیب خواندنی فروشگاه در Host. هر DbContext جدا خوانده می‌شود و SQL بین schemaها JOIN نمی‌شود.
/// مبلغ از Pricing و موجودی از Inventory روی Offer است؛ هویت Product قیمت یا موجودی ندارد.
/// </summary>
public sealed class StorefrontComposer
{
    private readonly CatalogDbContext _catalog;
    private readonly OfferDbContext _offers;
    private readonly PricingDbContext _prices;
    private readonly InventoryDbContext _inventory;
    private readonly TaxDbContext _tax;
    private readonly IPartyLookupGateway _parties;

    /// <summary>
    /// سازندهٔ ترکیب فروشگاه. نام فروشنده از Party جدا از Offer خوانده می‌شود.
    /// </summary>
    public StorefrontComposer(
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
    /// خانهٔ فروشگاه را با رده‌ها و محصولات منتشرشده می‌سازد.
    /// </summary>
    public async Task<StorefrontHomePage> GetHomeAsync(CancellationToken cancellationToken)
    {
        var categories = await ListCategoriesAsync(cancellationToken);
        var listing = await GetListingAsync(null, null, cancellationToken);
        return new StorefrontHomePage(
            categories,
            listing.Products,
            "فروشگاه توبا",
            "کالای واقعی از Catalog با قیمت Offer و موجودی انبار");
    }

    /// <summary>
    /// رده‌های منتشرشده را برای ناوبری برمی‌گرداند.
    /// </summary>
    public async Task<IReadOnlyList<StorefrontCategoryItem>> ListCategoriesAsync(CancellationToken cancellationToken)
    {
        var categories = await _catalog.Categories.AsNoTracking().ToListAsync(cancellationToken);
        var names = await LoadNamesAsync(CatalogLocalizedOwnerKind.Category, categories.Select(x => x.CategoryId).ToList(), cancellationToken);
        return categories
            .Select(category => new StorefrontCategoryItem(
                category.CategoryId,
                names.GetValueOrDefault(category.CategoryId) ?? "رده"))
            .OrderBy(item => item.Name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// فهرست فروشگاهی را از محصولات Published و Offer فعال می‌سازد.
    /// </summary>
    public async Task<StorefrontListingPage> GetListingAsync(string? query, Guid? categoryId, CancellationToken cancellationToken)
    {
        var categories = await ListCategoriesAsync(cancellationToken);
        var cards = await BuildProductCardsAsync(cancellationToken);
        var filtered = cards.AsEnumerable();
        if (categoryId is Guid selected)
        {
            filtered = filtered.Where(card => card.CategoryId == selected);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var needle = query.Trim();
            filtered = filtered.Where(card =>
                card.Title.Contains(needle, StringComparison.OrdinalIgnoreCase)
                || card.SellerDisplayName.Contains(needle, StringComparison.OrdinalIgnoreCase));
        }

        return new StorefrontListingPage(categories, filtered.ToList(), query, categoryId);
    }

    /// <summary>
    /// PDP را از slug درز SEO می‌خواند. چند فروشنده به‌صورت Offer جدا نمایش داده می‌شوند.
    /// </summary>
    public async Task<StorefrontProductDetailPage?> GetDetailAsync(string slug, CancellationToken cancellationToken)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        var products = await _catalog.Products.AsNoTracking()
            .Where(x => x.Status == CatalogPublicationStatus.Published)
            .ToListAsync(cancellationToken);
        var product = products.FirstOrDefault(item => string.Equals(item.SlugSeam, normalized, StringComparison.OrdinalIgnoreCase))
            ?? products.FirstOrDefault(item => item.ProductId.ToString("N") == normalized.Replace("-", string.Empty));
        if (product is null)
        {
            return null;
        }

        var bundle = await ComposeProductAsync(product, cancellationToken);
        if (bundle is null)
        {
            return null;
        }

        var (card, primary, others, description, brand, media, variantId) = bundle.Value;
        var seoTitle = string.IsNullOrWhiteSpace(product.SeoTitleSeam) ? card.Title : product.SeoTitleSeam;
        var seoDescription = string.IsNullOrWhiteSpace(description)
            ? $"{card.Title} از {card.SellerDisplayName}"
            : description;
        return new StorefrontProductDetailPage(
            product.ProductId,
            card.Slug,
            card.Title,
            description,
            card.CategoryName,
            brand,
            media,
            variantId,
            primary,
            others,
            seoTitle!,
            seoDescription,
            CartMutationEnabled: false);
    }

    private async Task<IReadOnlyList<StorefrontProductCard>> BuildProductCardsAsync(CancellationToken cancellationToken)
    {
        var products = await _catalog.Products.AsNoTracking()
            .Where(x => x.Status == CatalogPublicationStatus.Published)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(cancellationToken);
        var cards = new List<StorefrontProductCard>();
        foreach (var product in products)
        {
            var composed = await ComposeProductAsync(product, cancellationToken);
            if (composed is null)
            {
                continue;
            }

            cards.Add(composed.Value.Card);
        }

        return cards;
    }

    private async Task<(StorefrontProductCard Card, StorefrontOfferCandidate Primary, IReadOnlyList<StorefrontAlternateOffer> Others, string? Description, string? Brand, IReadOnlyList<Guid> Media, Guid VariantId)?> ComposeProductAsync(
        CatalogProduct product,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var title = (await LoadNamesAsync(CatalogLocalizedOwnerKind.Product, [product.ProductId], cancellationToken))
            .GetValueOrDefault(product.ProductId)
            ?? product.SlugSeam
            ?? "کالا";
        var descriptions = await LoadFieldAsync(CatalogLocalizedOwnerKind.Product, [product.ProductId], "description", cancellationToken);
        var categoryLinks = await _catalog.ProductCategories.AsNoTracking()
            .Where(x => x.ProductId == product.ProductId)
            .ToListAsync(cancellationToken);
        var categoryNames = await LoadNamesAsync(
            CatalogLocalizedOwnerKind.Category,
            categoryLinks.Select(x => x.CategoryId).ToList(),
            cancellationToken);
        var categoryId = categoryLinks.FirstOrDefault()?.CategoryId;
        var categoryName = categoryId is Guid cid
            ? categoryNames.GetValueOrDefault(cid) ?? "رده"
            : "بدون رده";
        var brandName = product.BrandId is Guid brandId
            ? (await LoadNamesAsync(CatalogLocalizedOwnerKind.Brand, [brandId], cancellationToken)).GetValueOrDefault(brandId)
            : null;
        var media = await _catalog.MediaReferences.AsNoTracking()
            .Where(x => x.ProductId == product.ProductId)
            .Select(x => x.MediaAssetId)
            .ToListAsync(cancellationToken);
        var variants = await _catalog.Variants.AsNoTracking()
            .Where(x => x.ProductId == product.ProductId)
            .ToListAsync(cancellationToken);
        var variantIds = variants.Select(x => x.VariantId).ToList();
        if (variantIds.Count == 0)
        {
            return null;
        }

        var offers = await _offers.Offers.AsNoTracking()
            .Where(x => variantIds.Contains(x.CatalogVariantId) && x.Status == OfferStatus.Active)
            .ToListAsync(cancellationToken);
        var offerIds = offers.Select(x => x.OfferId).ToList();
        if (offerIds.Count == 0)
        {
            return null;
        }

        var prices = await _prices.Prices.AsNoTracking()
            .Where(x => offerIds.Contains(x.OfferId) && x.Status == PriceStatus.Active)
            .ToListAsync(cancellationToken);
        var positions = await _inventory.Positions.AsNoTracking()
            .Where(x => offerIds.Contains(x.OfferId))
            .ToListAsync(cancellationToken);
        var taxRows = await _tax.OfferClassifications.AsNoTracking()
            .Where(x => offerIds.Contains(x.OfferId))
            .ToListAsync(cancellationToken);
        var taxCats = taxRows.Count == 0
            ? []
            : await _tax.Categories.AsNoTracking()
                .Where(x => taxRows.Select(row => row.CategoryId).Contains(x.CategoryId))
                .ToListAsync(cancellationToken);

        var candidates = new List<StorefrontOfferCandidate>();
        foreach (var offer in offers)
        {
            var price = prices
                .Where(item => item.OfferId == offer.OfferId
                    && item.ValidFrom <= now
                    && (item.ValidTo is null || item.ValidTo >= now))
                .OrderBy(item => item.Amount)
                .FirstOrDefault();
            if (price is null)
            {
                continue;
            }

            var available = positions.Where(item => item.OfferId == offer.OfferId).Sum(item => item.OnHand - item.Reserved);
            var seller = await _parties.FindByIdAsync(offer.SellerPartyId, cancellationToken);
            var tax = taxRows.FirstOrDefault(row => row.OfferId == offer.OfferId);
            var taxLabel = tax is null
                ? "طبقه مالیات ثبت نشده"
                : taxCats.FirstOrDefault(cat => cat.CategoryId == tax.CategoryId)?.DisplayName ?? tax.CategoryId.ToString("N")[..8];
            candidates.Add(new StorefrontOfferCandidate(
                offer.OfferId,
                offer.CatalogVariantId,
                offer.SellerPartyId,
                seller?.DisplayName ?? "فروشنده",
                offer.SellerSku,
                price.Amount,
                price.Currency,
                price.Market,
                available,
                taxLabel));
        }

        var primary = StorefrontPrimaryOfferResolver.Resolve(candidates);
        if (primary is null)
        {
            return null;
        }

        var others = candidates
            .Where(item => item.OfferId != primary.OfferId)
            .Select(item => new StorefrontAlternateOffer(
                item.OfferId,
                item.SellerDisplayName,
                item.AmountExclusiveOfTax,
                item.Currency,
                item.AvailableUnits,
                item.AvailableUnits > 0))
            .ToList();
        var slug = string.IsNullOrWhiteSpace(product.SlugSeam)
            ? product.ProductId.ToString("N")
            : product.SlugSeam;
        var card = new StorefrontProductCard(
            product.ProductId,
            slug,
            title,
            categoryName,
            categoryId,
            media.FirstOrDefault() == Guid.Empty ? null : media.FirstOrDefault(),
            primary.OfferId,
            primary.SellerDisplayName,
            primary.AmountExclusiveOfTax,
            primary.Currency,
            primary.AvailableUnits,
            primary.AvailableUnits > 0,
            PromotionLabel: null);
        return (card, primary, others, descriptions.GetValueOrDefault(product.ProductId), brandName, media, primary.CatalogVariantId);
    }

    private async Task<Dictionary<Guid, string>> LoadNamesAsync(
        CatalogLocalizedOwnerKind kind,
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
        => await LoadFieldAsync(kind, ids, "name", cancellationToken);

    private async Task<Dictionary<Guid, string>> LoadFieldAsync(
        CatalogLocalizedOwnerKind kind,
        IReadOnlyCollection<Guid> ids,
        string fieldKey,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var rows = await _catalog.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == kind && ids.Contains(x.OwnerId) && x.FieldKey == fieldKey)
            .ToListAsync(cancellationToken);
        return rows
            .GroupBy(x => x.OwnerId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(item => item.Locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase) ? 0 : 1).First().Value);
    }
}
