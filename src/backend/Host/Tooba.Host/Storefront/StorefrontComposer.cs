using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Offer.Domain;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Party.Application;
using Tooba.Pricing.Domain;
using Tooba.Pricing.Infrastructure.Persistence;
using Tooba.Promotion.Application;
using Tooba.Tax.Infrastructure.Persistence;
using Tooba.Reviews.Application;
using Tooba.Content.Application;

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
    private readonly IPromotionEvaluator _promotions;
    private readonly IReviewDirectory _reviews;
    private readonly IContentDirectory _content;
    private readonly ICatalogDirectory _catalogDirectory;

    /// <summary>
    /// سازندهٔ ترکیب فروشگاه. نام فروشنده از Party جدا از Offer خوانده می‌شود.
    /// </summary>
    public StorefrontComposer(
        CatalogDbContext catalog,
        OfferDbContext offers,
        PricingDbContext prices,
        InventoryDbContext inventory,
        TaxDbContext tax,
        IPartyLookupGateway parties,
        IPromotionEvaluator promotions,
        IReviewDirectory reviews,
        IContentDirectory content,
        ICatalogDirectory catalogDirectory)
    {
        _catalog = catalog;
        _offers = offers;
        _prices = prices;
        _inventory = inventory;
        _tax = tax;
        _parties = parties;
        _promotions = promotions;
        _reviews = reviews;
        _content = content;
        _catalogDirectory = catalogDirectory;
    }

    /// <summary>
    /// خانهٔ فروشگاه را با رده‌ها و محصولات منتشرشده می‌سازد.
    /// </summary>
    public async Task<StorefrontHomePage> GetHomeAsync(CancellationToken cancellationToken)
    {
        var categories = await ListCategoriesAsync(cancellationToken);
        var listing = await GetListingAsync(null, null, null, null, "newest", 1, 48, cancellationToken);
        var brands = await ListBrandsAsync(cancellationToken);
        var promotedProducts = listing.Products.Where(card => card.PromotionLabel is not null).Take(10).ToList();
        var homeCategories = SelectHomeCategories(categories, 20);
        var bestSellerColumns = BuildBestSellerColumns(categories, listing.Products, 4, 3);
        var mostViewed = listing.Products
            .OrderByDescending(card => card.ReviewCount)
            .ThenBy(card => card.Title, StringComparer.Ordinal)
            .Take(12)
            .ToList();
        var featuredReviews = await BuildFeaturedReviewsAsync(cancellationToken);
        var latestArticles = await BuildLatestArticlesAsync(cancellationToken);
        return new StorefrontHomePage(
            categories,
            listing.Products.Take(24).ToList(),
            promotedProducts.Take(8).ToList(),
            promotedProducts.Skip(Math.Min(5, promotedProducts.Count)).Take(8).ToList(),
            listing.Products.Take(8).ToList(),
            listing.Products.Skip(8).Take(8).ToList(),
            brands.Take(20).ToList(),
            "فروشگاه توبا",
            "کالای واقعی از Catalog با قیمت Offer و موجودی انبار",
            homeCategories,
            bestSellerColumns,
            mostViewed,
            featuredReviews,
            latestArticles);
    }

    /// <summary>
    /// نظرهای Published اخیر را برای ریل خانه به DTO عمومی نگاشت می‌کند.
    /// </summary>
    private async Task<IReadOnlyList<StorefrontFeaturedReviewItem>> BuildFeaturedReviewsAsync(CancellationToken cancellationToken)
    {
        var reviews = await _reviews.GetRecentPublishedForHomeAsync(8, cancellationToken);
        return reviews.Select(review => new StorefrontFeaturedReviewItem(
            review.ReviewId.ToString("N"),
            review.AuthorDisplayName,
            review.Rating,
            review.Title,
            review.Body,
            review.IsVerifiedPurchase,
            review.CreatedAt,
            review.ProductTitle,
            review.ProductSlug)).ToList();
    }

    /// <summary>
    /// مقالات Published اخیر را برای ریل خانه می‌خواند.
    /// </summary>
    private async Task<IReadOnlyList<StorefrontArticleItem>> BuildLatestArticlesAsync(CancellationToken cancellationToken)
    {
        var articles = await _content.ListPublishedForHomeAsync(6, cancellationToken);
        return articles.Select(article => new StorefrontArticleItem(
            article.ArticleId.ToString("N"),
            article.Slug,
            article.Title,
            article.Excerpt,
            article.CoverMediaAssetId,
            article.PublishDate,
            article.AuthorDisplayName,
            article.Tags,
            article.IsFeatured)).ToList();
    }

    /// <summary>
    /// رده‌های ریل خانهٔ Shopeiva: ریشهٔ منتشرشده تا سقف مشخص؛ dump کامل Catalog نیست.
    /// </summary>
    internal static IReadOnlyList<StorefrontCategoryItem> SelectHomeCategories(
        IReadOnlyList<StorefrontCategoryItem> categories,
        int limit)
    {
        var roots = categories.Where(category => category.ParentCategoryId is null).ToList();
        var source = roots.Count > 0 ? roots : categories;
        return source.Take(Math.Clamp(limit, 1, 20)).ToList();
    }

    /// <summary>
    /// ستون‌های پرفروش مطابق الگوی Shopeiva: تا چهار رده با سه کارت زنده در هر ستون.
    /// </summary>
    internal static IReadOnlyList<StorefrontBestSellerColumn> BuildBestSellerColumns(
        IReadOnlyList<StorefrontCategoryItem> categories,
        IReadOnlyList<StorefrontProductCard> products,
        int columnCount,
        int productsPerColumn)
    {
        var used = new HashSet<Guid>();
        var columns = new List<StorefrontBestSellerColumn>();
        var roots = categories.Where(category => category.ParentCategoryId is null).ToList();
        var categoryOrder = roots.Count > 0 ? roots.Concat(categories).DistinctBy(category => category.CategoryId).ToList() : categories.ToList();
        foreach (var category in categoryOrder)
        {
            if (columns.Count >= columnCount)
            {
                break;
            }

            var included = DescendantCategoryIds(categories, category.CategoryId);
            var columnProducts = products
                .Where(card => card.CategoryId is Guid categoryId && included.Contains(categoryId) && used.Add(card.ProductId))
                .Take(productsPerColumn)
                .ToList();
            if (columnProducts.Count == 0)
            {
                continue;
            }

            columns.Add(new StorefrontBestSellerColumn(category.CategoryId, category.Name, columnProducts));
        }

        if (columns.Count < columnCount)
        {
            var remaining = products.Where(card => used.Add(card.ProductId)).ToList();
            for (var index = columns.Count; index < columnCount; index++)
            {
                var slice = remaining.Skip((index - columns.Count) * productsPerColumn).Take(productsPerColumn).ToList();
                if (slice.Count == 0)
                {
                    break;
                }

                columns.Add(new StorefrontBestSellerColumn(Guid.Empty, "پرفروش‌ها", slice));
            }
        }

        return columns;
    }

    /// <summary>
    /// برندهای Catalog را برای نوار برند خانه برمی‌گرداند.
    /// </summary>
    public async Task<IReadOnlyList<StorefrontBrandItem>> ListBrandsAsync(CancellationToken cancellationToken)
    {
        var brands = await _catalog.Brands.AsNoTracking()
            .Where(brand => brand.Status == CatalogPublicationStatus.Published)
            .ToListAsync(cancellationToken);
        var names = await LoadNamesAsync(CatalogLocalizedOwnerKind.Brand, brands.Select(x => x.BrandId).ToList(), cancellationToken);
        var productCounts = await _catalog.Products.AsNoTracking()
            .Where(product => product.Status == CatalogPublicationStatus.Published && product.BrandId != null)
            .GroupBy(product => product.BrandId!.Value)
            .Select(group => new { BrandId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.BrandId, row => row.Count, cancellationToken);
        return brands
            .Select(brand => new StorefrontBrandItem(
                brand.BrandId,
                brand.SlugSeam ?? brand.BrandId.ToString("N"),
                names.GetValueOrDefault(brand.BrandId) ?? brand.SlugSeam ?? "برند",
                productCounts.GetValueOrDefault(brand.BrandId),
                brand.LogoMediaAssetId))
            .OrderBy(item => item.Name, StringComparer.Ordinal)
            .Take(24)
            .ToList();
    }

    /// <summary>
    /// landing عمومی برند را فقط برای برند منتشرشده می‌سازد؛ متن بازاریابی ساختگی تولید نمی‌شود.
    /// </summary>
    public async Task<StorefrontBrandPage?> GetBrandAsync(string slug, CancellationToken cancellationToken)
    {
        var brands = await ListBrandsAsync(cancellationToken);
        var brand = brands.FirstOrDefault(item => string.Equals(item.Slug, slug, StringComparison.OrdinalIgnoreCase));
        if (brand is null)
        {
            return null;
        }

        var productIds = await _catalog.Products.AsNoTracking()
            .Where(product => product.Status == CatalogPublicationStatus.Published && product.BrandId == brand.BrandId)
            .Select(product => product.ProductId)
            .ToListAsync(cancellationToken);
        var cards = await BuildProductCardsAsync(cancellationToken);
        return new StorefrontBrandPage(brand, cards.Where(card => productIds.Contains(card.ProductId)).ToList());
    }

    /// <summary>
    /// فروشندگان عمومی را صرفاً از Offerهای فعال و قابل‌ترکیب استخراج می‌کند؛ شناسهٔ Party با کلید عمومی هش‌شده جایگزین می‌شود.
    /// </summary>
    public async Task<IReadOnlyList<StorefrontPublicSellerItem>> ListPublicSellersAsync(CancellationToken cancellationToken)
    {
        var activeOffers = await _offers.Offers.AsNoTracking()
            .Where(offer => offer.Status == OfferStatus.Active)
            .ToListAsync(cancellationToken);
        var variants = await _catalog.Variants.AsNoTracking()
            .Where(variant => activeOffers.Select(offer => offer.CatalogVariantId).Contains(variant.VariantId))
            .ToDictionaryAsync(variant => variant.VariantId, variant => variant.ProductId, cancellationToken);
        var sellers = new List<StorefrontPublicSellerItem>();
        foreach (var group in activeOffers.GroupBy(offer => offer.SellerPartyId))
        {
            var cards = await BuildProductCardsAsync(cancellationToken, group.Key);
            if (cards.Count == 0)
            {
                continue;
            }

            var party = await _parties.FindByIdAsync(group.Key, cancellationToken);
            sellers.Add(new StorefrontPublicSellerItem(
                CreatePublicSellerId(group.Key),
                party?.DisplayName ?? "فروشنده",
                group.Count(offer => variants.ContainsKey(offer.CatalogVariantId)),
                cards.Select(card => card.ProductId).Distinct().Count()));
        }

        return sellers
            .OrderBy(item => item.DisplayName, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// پروفایل عمومی فروشنده را با کلید عمومی resolve می‌کند و فقط کارت‌های Offer فعال او را برمی‌گرداند.
    /// </summary>
    public async Task<StorefrontPublicSellerPage?> GetPublicSellerAsync(string publicId, CancellationToken cancellationToken)
    {
        var activeOffers = await _offers.Offers.AsNoTracking()
            .Where(offer => offer.Status == OfferStatus.Active)
            .ToListAsync(cancellationToken);
        var sellerPartyId = activeOffers.Select(offer => offer.SellerPartyId).Distinct().FirstOrDefault(partyId =>
            string.Equals(CreatePublicSellerId(partyId), publicId, StringComparison.OrdinalIgnoreCase));
        if (sellerPartyId == Guid.Empty)
        {
            return null;
        }

        var sellerCards = await BuildProductCardsAsync(cancellationToken, sellerPartyId);
        if (sellerCards.Count == 0)
        {
            return null;
        }

        var seller = new StorefrontPublicSellerItem(
            publicId,
            sellerCards[0].SellerDisplayName,
            activeOffers.Count(offer => offer.SellerPartyId == sellerPartyId),
            sellerCards.Select(card => card.ProductId).Distinct().Count());
        return new StorefrontPublicSellerPage(seller, sellerCards);
    }

    /// <summary>
    /// مسیرهای merchandising را با سیگنال موجود می‌سازد؛ مسیرهای فاقد تحلیل فروش/بازدید صریحاً unsupported می‌مانند.
    /// </summary>
    public async Task<StorefrontMerchandisingPage> GetMerchandisingAsync(string kind, CancellationToken cancellationToken)
    {
        var normalized = kind.Trim().ToLowerInvariant();
        var cards = await BuildProductCardsAsync(cancellationToken);
        return normalized switch
        {
            "new-products" => new(normalized, "محصولات جدید", true, null, cards.Take(24).ToList()),
            "offers" or "sale" => new(
                normalized,
                normalized == "sale" ? "فروش ویژه" : "پیشنهادها",
                true,
                null,
                cards.Where(card => card.PromotionalAmountExclusiveOfTax is not null).Take(24).ToList()),
            "best-seller" => UnsupportedMerchandising(normalized, "پرفروش‌ترین‌ها", "سیگنال معتبر فروش تجمیعی هنوز در دسترس نیست."),
            "most-viewed" => UnsupportedMerchandising(normalized, "پربازدیدترین‌ها", "سیگنال معتبر بازدید هنوز در دسترس نیست."),
            "trending" => UnsupportedMerchandising(normalized, "محبوب‌های روز", "سیگنال معتبر روند هنوز در دسترس نیست."),
            _ => UnsupportedMerchandising(normalized, "کالاها", "این مسیر پشتیبانی نمی‌شود."),
        };
    }

    /// <summary>
    /// حالت unsupported را بدون محصول یا رتبه‌بندی ساختگی ایجاد می‌کند.
    /// </summary>
    internal static StorefrontMerchandisingPage UnsupportedMerchandising(string kind, string title, string reason)
        => new(kind, title, false, reason, []);

    /// <summary>
    /// از PartyId یک شناسهٔ عمومی یک‌طرفه می‌سازد تا شناسهٔ داخلی در URL یا JSON افشا نشود.
    /// </summary>
    internal static string CreatePublicSellerId(Guid partyId)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes($"tooba-public-seller:{partyId:N}"));
        return Convert.ToHexString(digest[..12]).ToLowerInvariant();
    }

    /// <summary>
    /// رده‌های منتشرشده را برای ناوبری برمی‌گرداند.
    /// </summary>
    public async Task<IReadOnlyList<StorefrontCategoryItem>> ListCategoriesAsync(CancellationToken cancellationToken)
    {
        var categories = await _catalog.Categories.AsNoTracking()
            .Where(category => category.Status == CatalogPublicationStatus.Published)
            .ToListAsync(cancellationToken);
        var names = await LoadNamesAsync(CatalogLocalizedOwnerKind.Category, categories.Select(x => x.CategoryId).ToList(), cancellationToken);
        return categories
            .Select(category => new StorefrontCategoryItem(
                category.CategoryId,
                category.ParentCategoryId,
                names.GetValueOrDefault(category.CategoryId) ?? "رده"))
            .OrderBy(item => item.Name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// فهرست فروشگاهی را از محصولات Published و Offer فعال می‌سازد.
    /// </summary>
    public async Task<StorefrontListingPage> GetListingAsync(
        string? query,
        Guid? categoryId,
        Guid? sellerPartyId,
        bool? inStock,
        string? sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var categories = await ListCategoriesAsync(cancellationToken);
        var cards = await BuildProductCardsAsync(cancellationToken);
        var sellers = cards
            .GroupBy(card => new { card.SellerPartyId, card.SellerDisplayName })
            .Select(group => new StorefrontSellerFilterItem(group.Key.SellerPartyId, group.Key.SellerDisplayName))
            .OrderBy(item => item.DisplayName, StringComparer.Ordinal)
            .ToList();
        var filtered = cards.AsEnumerable();
        if (categoryId is Guid selected)
        {
            var includedCategoryIds = DescendantCategoryIds(categories, selected);
            filtered = filtered.Where(card => card.CategoryId is Guid cardCategoryId && includedCategoryIds.Contains(cardCategoryId));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var needle = query.Trim();
            filtered = filtered.Where(card =>
                card.Title.Contains(needle, StringComparison.OrdinalIgnoreCase)
                || card.CategoryName.Contains(needle, StringComparison.OrdinalIgnoreCase)
                || card.SellerDisplayName.Contains(needle, StringComparison.OrdinalIgnoreCase));
        }

        if (sellerPartyId is Guid selectedSeller)
        {
            filtered = filtered.Where(card => card.SellerPartyId == selectedSeller);
        }

        if (inStock is bool availability)
        {
            filtered = filtered.Where(card => card.InStock == availability);
        }

        var normalizedSort = sort?.Trim().ToLowerInvariant() switch
        {
            "price-asc" => "price-asc",
            "price-desc" => "price-desc",
            "newest" => "newest",
            _ => "default",
        };
        filtered = normalizedSort switch
        {
            "price-asc" => filtered.OrderBy(card => card.PromotionalAmountExclusiveOfTax ?? card.OfferAmountExclusiveOfTax),
            "price-desc" => filtered.OrderByDescending(card => card.PromotionalAmountExclusiveOfTax ?? card.OfferAmountExclusiveOfTax),
            _ => filtered,
        };

        var safePageSize = Math.Clamp(pageSize, 1, 48);
        var safePage = Math.Max(page, 1);
        var materialized = filtered.ToList();
        return new StorefrontListingPage(
            categories,
            sellers,
            materialized.Skip((safePage - 1) * safePageSize).Take(safePageSize).ToList(),
            string.IsNullOrWhiteSpace(query) ? null : query.Trim(),
            categoryId,
            sellerPartyId,
            inStock,
            normalizedSort,
            safePage,
            safePageSize,
            materialized.Count);
    }

    /// <summary>
    /// PLP رده: resolve مسیر، subtree، facet پویا، فیلتر تایپ‌شده، صفحه‌بندی.
    /// زیرشاخه: محصولات انتساب‌شده به رده یا هر فرزند در taxonomy (نه Parent مگامنو).
    /// فیلتر: بین attributeها AND؛ داخل multi-select یک attribute OR.
    /// </summary>
    public async Task<StorefrontCategoryPlpPage?> GetCategoryPlpAsync(
        string locale,
        string slug,
        IReadOnlyList<StorefrontPlpFilterInput> filters,
        string? sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var catalogLocale = NormalizeCatalogLocale(locale);
        var uiSegment = MapUiLocaleSegment(catalogLocale);
        var resolved = await _catalogDirectory.ResolveCategoryRouteAsync(
            catalogLocale,
            slug,
            forStorefront: true,
            cancellationToken);
        if (resolved is null)
        {
            return null;
        }

        var category = await _catalog.Categories.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CategoryId == resolved.CategoryId, cancellationToken);
        if (category is null)
        {
            return null;
        }

        var translation = await _catalog.CategoryTranslations.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.CategoryId == resolved.CategoryId && x.Locale == catalogLocale,
                cancellationToken);

        var name = translation?.Name
            ?? (await LoadNamesAsync(CatalogLocalizedOwnerKind.Category, [resolved.CategoryId], cancellationToken))
                .GetValueOrDefault(resolved.CategoryId)
            ?? "رده";
        var currentSlug = resolved.CurrentSlug;
        var canonicalPath = $"/{uiSegment}/category/{currentSlug}";
        var redirectTo = resolved.IsRedirect ? canonicalPath : null;

        var allCategories = await _catalog.Categories.AsNoTracking()
            .Where(x => x.Status == CatalogPublicationStatus.Published && x.IsVisible)
            .ToListAsync(cancellationToken);
        var categoryItems = await ListCategoriesAsync(cancellationToken);
        var subtreeIds = DescendantCategoryIds(categoryItems, resolved.CategoryId);

        var breadcrumb = await BuildBreadcrumbAsync(resolved.CategoryId, allCategories, catalogLocale, uiSegment, cancellationToken);
        var children = await BuildSubcategoriesAsync(resolved.CategoryId, allCategories, catalogLocale, uiSegment, cancellationToken);

        var facetDefs = (await _catalogDirectory.GetEffectiveCategoryFacetsAsync(
                resolved.CategoryId,
                catalogLocale,
                cancellationToken))
            .Where(f => f.IsVisible)
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.LocalizedName, StringComparer.Ordinal)
            .ToList();

        var cards = await BuildProductCardsAsync(cancellationToken);
        var inSubtree = cards
            .Where(card => card.CategoryId is Guid cid && subtreeIds.Contains(cid))
            .ToList();

        var productIds = inSubtree
            .Select(c => c.ProductId)
            .Distinct()
            .ToList();
        var attributeValues = productIds.Count == 0
            ? []
            : await _catalog.ProductAttributeValues.AsNoTracking()
                .Where(v => productIds.Contains(v.ProductId))
                .ToListAsync(cancellationToken);

        var filteredProducts = ApplyTypedFilters(inSubtree, attributeValues, filters, facetDefs);
        var plpFacets = await BuildPlpFacetsAsync(
            facetDefs,
            filteredProducts,
            attributeValues,
            catalogLocale,
            cancellationToken);
        var applied = BuildAppliedChips(filters, facetDefs, plpFacets);

        var normalizedSort = sort?.Trim().ToLowerInvariant() switch
        {
            "price-asc" => "price-asc",
            "price-desc" => "price-desc",
            "newest" => "newest",
            _ => "default",
        };
        var ordered = normalizedSort switch
        {
            "price-asc" => filteredProducts.OrderBy(c => c.PromotionalAmountExclusiveOfTax ?? c.OfferAmountExclusiveOfTax).ToList(),
            "price-desc" => filteredProducts.OrderByDescending(c => c.PromotionalAmountExclusiveOfTax ?? c.OfferAmountExclusiveOfTax).ToList(),
            "newest" => filteredProducts.OrderByDescending(c => c.Title, StringComparer.Ordinal).ToList(),
            _ => filteredProducts,
        };

        var safePageSize = Math.Clamp(pageSize, 1, 48);
        var safePage = Math.Max(page, 1);
        var pageItems = ordered.Skip((safePage - 1) * safePageSize).Take(safePageSize).ToList();

        return new StorefrontCategoryPlpPage(
            resolved.CategoryId,
            catalogLocale,
            currentSlug,
            name,
            translation?.ShortDescription,
            translation?.Description,
            canonicalPath,
            resolved.IsRedirect,
            redirectTo,
            ordered.Count,
            safePage,
            safePageSize,
            normalizedSort,
            breadcrumb,
            children,
            plpFacets,
            applied,
            pageItems,
            ["default", "newest", "price-asc", "price-desc"]);
    }

    /// <summary>
    /// شناسهٔ ردهٔ انتخابی و همهٔ فرزندان آن را برای landing رده برمی‌گرداند؛ محاسبه در حافظه و فقط روی دادهٔ Catalog است.
    /// </summary>
    internal static IReadOnlySet<Guid> DescendantCategoryIds(
        IReadOnlyList<StorefrontCategoryItem> categories,
        Guid selectedCategoryId)
    {
        var result = new HashSet<Guid> { selectedCategoryId };
        var pending = new Queue<Guid>();
        pending.Enqueue(selectedCategoryId);
        while (pending.TryDequeue(out var parentId))
        {
            foreach (var child in categories.Where(category => category.ParentCategoryId == parentId))
            {
                if (result.Add(child.CategoryId))
                {
                    pending.Enqueue(child.CategoryId);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// PDP را از slug می‌خواند و گونهٔ درخواستی را فقط پس از عضویت در همان محصول در backend انتخاب می‌کند.
    /// </summary>
    public async Task<StorefrontProductDetailPage?> GetDetailAsync(
        string slug,
        Guid? variantId,
        CancellationToken cancellationToken)
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

        var bundle = await ComposeProductAsync(product, cancellationToken, selectedVariantId: variantId);
        if (bundle is null)
        {
            return null;
        }

        var (card, primary, others, shortDescription, fullDescription, brand, media, selectedVariantId, specifications, variants) = bundle.Value;
        var relatedProducts = SelectRelatedProducts(
            await BuildProductCardsAsync(cancellationToken),
            product.ProductId,
            card.CategoryId);
        var reviewSummaries = await _reviews.GetPublishedSummariesAsync([product.ProductId], cancellationToken);
        var reviewSummary = reviewSummaries.GetValueOrDefault(product.ProductId);
        var seoTitle = string.IsNullOrWhiteSpace(product.SeoTitleSeam) ? card.Title : product.SeoTitleSeam;
        var localizedSeoDescriptions = await LoadFieldAsync(
            CatalogLocalizedOwnerKind.Product,
            [product.ProductId],
            "seo_description",
            cancellationToken);
        var localizedSeoDescription = localizedSeoDescriptions.GetValueOrDefault(product.ProductId);
        var seoDescription = !string.IsNullOrWhiteSpace(localizedSeoDescription)
            ? localizedSeoDescription
            : string.IsNullOrWhiteSpace(shortDescription)
                ? $"{card.Title} از {card.SellerDisplayName}"
                : shortDescription;
        return new StorefrontProductDetailPage(
            product.ProductId,
            card.Slug,
            card.Title,
            shortDescription,
            shortDescription,
            fullDescription,
            card.CategoryName,
            brand,
            media,
            specifications,
            variants,
            selectedVariantId,
            primary,
            card.PromotionalAmountExclusiveOfTax,
            card.PromotionLabel,
            others,
            relatedProducts,
            seoTitle!,
            seoDescription,
            CartMutationEnabled: true,
            AverageRating: reviewSummary?.AverageRating,
            ReviewCount: reviewSummary?.ReviewCount ?? 0);
    }

    /// <summary>
    /// کارت‌های زندهٔ دیگر را با اولویت ردهٔ همان Product انتخاب می‌کند؛ شناسهٔ Product جاری هرگز در rail تکرار نمی‌شود.
    /// </summary>
    internal static IReadOnlyList<StorefrontProductCard> SelectRelatedProducts(
        IReadOnlyList<StorefrontProductCard> cards,
        Guid currentProductId,
        Guid? categoryId)
        => cards
            .Where(item => item.ProductId != currentProductId)
            .OrderByDescending(item => categoryId is Guid selected && item.CategoryId == selected)
            .Take(10)
            .ToList();

    /// <summary>
    /// فقط شناسه‌های محصول درخواستی را از Catalog می‌خواند و به کارت زنده ترکیب می‌کند؛
    /// برخلاف listing همهٔ محصولات را پیمایش نمی‌کند و خلاصهٔ Reviews را گروهی می‌گیرد.
    /// </summary>
    public async Task<IReadOnlyDictionary<Guid, StorefrontProductCard>> ComposeProductCardsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0) return new Dictionary<Guid, StorefrontProductCard>();
        var requested = productIds.Distinct().ToArray();
        var products = await _catalog.Products.AsNoTracking()
            .Where(x => requested.Contains(x.ProductId) && x.Status == CatalogPublicationStatus.Published)
            .ToListAsync(cancellationToken);
        var cards = new List<StorefrontProductCard>(products.Count);
        foreach (var product in products)
        {
            var composed = await ComposeProductAsync(product, cancellationToken);
            if (composed is not null) cards.Add(composed.Value.Card);
        }
        var summaries = await _reviews.GetPublishedSummariesAsync(cards.Select(x => x.ProductId).ToArray(), cancellationToken);
        return cards.ToDictionary(
            card => card.ProductId,
            card =>
            {
                var summary = summaries.GetValueOrDefault(card.ProductId);
                return card with { AverageRating = summary?.AverageRating, ReviewCount = summary?.ReviewCount ?? 0 };
            });
    }

    private async Task<IReadOnlyList<StorefrontProductCard>> BuildProductCardsAsync(
        CancellationToken cancellationToken,
        Guid? preferredSellerPartyId = null)
    {
        var products = await _catalog.Products.AsNoTracking()
            .Where(x => x.Status == CatalogPublicationStatus.Published)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var cards = new List<StorefrontProductCard>();
        foreach (var product in products)
        {
            var composed = await ComposeProductAsync(product, cancellationToken, preferredSellerPartyId);
            if (composed is null)
            {
                continue;
            }

            cards.Add(composed.Value.Card);
        }

        var summaries = await _reviews.GetPublishedSummariesAsync(cards.Select(x => x.ProductId).ToArray(), cancellationToken);
        return cards.Select(card =>
        {
            var summary = summaries.GetValueOrDefault(card.ProductId);
            return card with { AverageRating = summary?.AverageRating, ReviewCount = summary?.ReviewCount ?? 0 };
        }).ToList();
    }

    private async Task<(StorefrontProductCard Card, StorefrontOfferCandidate Primary, IReadOnlyList<StorefrontAlternateOffer> Others, string? ShortDescription, string? FullDescription, string? Brand, IReadOnlyList<Guid> Media, Guid VariantId, IReadOnlyList<StorefrontProductSpecification> Specifications, IReadOnlyList<StorefrontProductVariant> Variants)?> ComposeProductAsync(
        CatalogProduct product,
        CancellationToken cancellationToken,
        Guid? preferredSellerPartyId = null,
        Guid? selectedVariantId = null)
    {
        var now = DateTimeOffset.UtcNow;
        var title = (await LoadNamesAsync(CatalogLocalizedOwnerKind.Product, [product.ProductId], cancellationToken))
            .GetValueOrDefault(product.ProductId)
            ?? product.SlugSeam
            ?? "کالا";
        var shortDescriptions = await LoadFieldAsync(CatalogLocalizedOwnerKind.Product, [product.ProductId], "short_description", cancellationToken);
        var fullDescriptions = await LoadFieldAsync(CatalogLocalizedOwnerKind.Product, [product.ProductId], "full_description", cancellationToken);
        var legacyDescriptions = await LoadFieldAsync(CatalogLocalizedOwnerKind.Product, [product.ProductId], "description", cancellationToken);
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

        if (selectedVariantId is Guid requestedVariant && !variantIds.Contains(requestedVariant))
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

        var chosenVariantId = selectedVariantId
            ?? StorefrontPrimaryOfferResolver.Resolve(candidates)?.CatalogVariantId;
        if (chosenVariantId is null)
        {
            return null;
        }

        var selectedCandidates = candidates.Where(candidate => candidate.CatalogVariantId == chosenVariantId.Value).ToList();
        var resolvableCandidates = preferredSellerPartyId is Guid sellerPartyId
            ? selectedCandidates.Where(candidate => candidate.SellerPartyId == sellerPartyId).ToList()
            : selectedCandidates;
        var primary = StorefrontPrimaryOfferResolver.Resolve(resolvableCandidates);
        if (primary is null)
        {
            return null;
        }

        var others = selectedCandidates
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
        var promotion = await _promotions.EvaluateAsync(
            new PromotionEvaluationRequest(
                primary.OfferId,
                primary.CatalogVariantId,
                categoryId,
                primary.SellerPartyId,
                primary.Market,
                "storefront",
                primary.Currency,
                1,
                primary.AmountExclusiveOfTax,
                CustomerPartyId: null,
                OrganizationPartyId: null,
                CouponCode: null,
                At: now),
            cancellationToken);
        var promotionLabel = promotion.Applied.Count == 0
            ? null
            : string.Join("، ", promotion.Applied.Select(applied => applied.Name));
        var card = new StorefrontProductCard(
            product.ProductId,
            slug,
            title,
            categoryName,
            categoryId,
            media.FirstOrDefault() == Guid.Empty ? null : media.FirstOrDefault(),
            primary.OfferId,
            primary.SellerPartyId,
            primary.SellerDisplayName,
            primary.AmountExclusiveOfTax,
            promotion.DiscountAmount > 0 ? promotion.PostDiscountTaxExclusiveAmount : null,
            primary.Currency,
            primary.AvailableUnits,
            primary.AvailableUnits > 0,
            promotionLabel);
        var specifications = await BuildSpecificationsAsync(product.ProductId, chosenVariantId.Value, cancellationToken);
        var variantViews = await BuildVariantsAsync(variants, candidates, cancellationToken);
        var shortDescription = shortDescriptions.GetValueOrDefault(product.ProductId)
            ?? legacyDescriptions.GetValueOrDefault(product.ProductId);
        var fullDescription = fullDescriptions.GetValueOrDefault(product.ProductId)
            ?? legacyDescriptions.GetValueOrDefault(product.ProductId);
        return (card, primary, others, shortDescription, fullDescription, brandName, media, chosenVariantId.Value, specifications, variantViews);
    }

    /// <summary>
    /// مشخصات محصول و محورهای گونهٔ انتخاب‌شده را فقط از جداول Catalog می‌خواند و
    /// شناسه‌های شمارشی را به برچسب فارسی تبدیل می‌کند. محور انتخاب‌شده یک ویژگی
    /// واقعی Catalog است و برای دانه‌های قدیمی که مشخصهٔ غیرمحور ندارند نیز PDP را
    /// بدون جعل داده قابل ارزیابی نگه می‌دارد.
    /// </summary>
    private async Task<IReadOnlyList<StorefrontProductSpecification>> BuildSpecificationsAsync(
        Guid productId,
        Guid selectedVariantId,
        CancellationToken cancellationToken)
    {
        var productValues = await _catalog.ProductAttributeValues.AsNoTracking()
            .Where(value => value.ProductId == productId)
            .ToListAsync(cancellationToken);
        var variantValues = await _catalog.VariantAttributeValues.AsNoTracking()
            .Where(value => value.VariantId == selectedVariantId)
            .ToListAsync(cancellationToken);
        return await ProjectAttributesAsync(
            productValues.Select(value => (value.DefinitionId, value.CanonicalValue))
                .Concat(variantValues.Select(value => (value.DefinitionId, value.CanonicalValue)))
                .Distinct()
                .ToList(),
            cancellationToken,
            static (label, value) => new StorefrontProductSpecification(label, value));
    }

    /// <summary>
    /// همهٔ گونه‌ها را با محور خوانا و Offer اصلی همان گونه نمایش می‌دهد؛ انتخاب کلاینت در این مرحله معتبر فرض نمی‌شود.
    /// </summary>
    private async Task<IReadOnlyList<StorefrontProductVariant>> BuildVariantsAsync(
        IReadOnlyList<CatalogVariant> variants,
        IReadOnlyList<StorefrontOfferCandidate> candidates,
        CancellationToken cancellationToken)
    {
        var variantIds = variants.Select(variant => variant.VariantId).ToList();
        var axisRows = await _catalog.VariantAttributeValues.AsNoTracking()
            .Where(value => variantIds.Contains(value.VariantId))
            .ToListAsync(cancellationToken);
        var result = new List<StorefrontProductVariant>();
        foreach (var variant in variants.OrderBy(item => item.CreatedAt))
        {
            var axes = await ProjectAttributesAsync(
                axisRows.Where(value => value.VariantId == variant.VariantId)
                    .Select(value => (value.DefinitionId, value.CanonicalValue))
                    .ToList(),
                cancellationToken,
                static (label, value) => new StorefrontVariantAxis(label, value));
            var primary = StorefrontPrimaryOfferResolver.Resolve(
                candidates.Where(candidate => candidate.CatalogVariantId == variant.VariantId).ToList());
            result.Add(new StorefrontProductVariant(
                variant.VariantId,
                axes,
                primary is not null && primary.AvailableUnits > 0,
                primary,
                PromotionalAmountExclusiveOfTax: null,
                PromotionLabel: null));
        }

        return result;
    }

    /// <summary>
    /// مقادیر تایپ‌شدهٔ Catalog را در حافظه به جفت برچسب/مقدار فارسی تبدیل می‌کند.
    /// </summary>
    private async Task<IReadOnlyList<T>> ProjectAttributesAsync<T>(
        IReadOnlyList<(Guid DefinitionId, string CanonicalValue)> values,
        CancellationToken cancellationToken,
        Func<string, string, T> projector)
    {
        if (values.Count == 0)
        {
            return [];
        }

        var definitionIds = values.Select(value => value.DefinitionId).Distinct().ToList();
        var definitions = await _catalog.AttributeDefinitions.AsNoTracking()
            .Where(definition => definitionIds.Contains(definition.DefinitionId))
            .ToDictionaryAsync(definition => definition.DefinitionId, cancellationToken);
        var labels = await LoadNamesAsync(CatalogLocalizedOwnerKind.AttributeDefinition, definitionIds, cancellationToken);
        var optionIds = values
            .Where(value => definitions.GetValueOrDefault(value.DefinitionId)?.ValueKind == CatalogAttributeValueKind.Enumeration)
            .Select(value => Guid.TryParseExact(value.CanonicalValue, "N", out var optionId) ? optionId : Guid.Empty)
            .Where(optionId => optionId != Guid.Empty)
            .Distinct()
            .ToList();
        var optionLabels = await LoadNamesAsync(CatalogLocalizedOwnerKind.AttributeOption, optionIds, cancellationToken);

        return values
            .Where(value => definitions.ContainsKey(value.DefinitionId))
            .Select(value =>
            {
                var definition = definitions[value.DefinitionId];
                var displayValue = definition.ValueKind switch
                {
                    CatalogAttributeValueKind.Enumeration when Guid.TryParseExact(value.CanonicalValue, "N", out var optionId)
                        => optionLabels.GetValueOrDefault(optionId) ?? "گزینهٔ تعریف‌شده",
                    CatalogAttributeValueKind.Boolean => bool.TryParse(value.CanonicalValue, out var flag) && flag ? "بله" : "خیر",
                    _ => value.CanonicalValue,
                };
                return projector(labels.GetValueOrDefault(value.DefinitionId) ?? definition.Code, displayValue);
            })
            .ToList();
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

    private static string NormalizeCatalogLocale(string locale)
    {
        var trimmed = locale.Trim();
        if (trimmed.Equals("fa", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("fa-", StringComparison.OrdinalIgnoreCase))
        {
            return "fa-IR";
        }

        if (trimmed.Equals("en", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("en-", StringComparison.OrdinalIgnoreCase))
        {
            return "en-US";
        }

        if (trimmed.Equals("ar", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("ar-", StringComparison.OrdinalIgnoreCase))
        {
            return "ar-SA";
        }

        return trimmed;
    }

    private static string MapUiLocaleSegment(string catalogLocale)
    {
        if (catalogLocale.StartsWith("en", StringComparison.OrdinalIgnoreCase))
        {
            return "en";
        }

        if (catalogLocale.StartsWith("ar", StringComparison.OrdinalIgnoreCase))
        {
            return "ar";
        }

        return "fa";
    }

    private async Task<IReadOnlyList<StorefrontCategoryBreadcrumbItem>> BuildBreadcrumbAsync(
        Guid categoryId,
        IReadOnlyList<CatalogCategory> allCategories,
        string catalogLocale,
        string uiSegment,
        CancellationToken cancellationToken)
    {
        var byId = allCategories.ToDictionary(x => x.CategoryId);
        var chain = new List<Guid>();
        var current = categoryId;
        var seen = new HashSet<Guid>();
        while (byId.TryGetValue(current, out var node) && seen.Add(current))
        {
            chain.Add(current);
            if (node.ParentCategoryId is not Guid parent)
            {
                break;
            }

            current = parent;
        }

        chain.Reverse();
        var translations = await _catalog.CategoryTranslations.AsNoTracking()
            .Where(t => chain.Contains(t.CategoryId) && t.Locale == catalogLocale)
            .ToDictionaryAsync(t => t.CategoryId, cancellationToken);
        var names = await LoadNamesAsync(CatalogLocalizedOwnerKind.Category, chain, cancellationToken);
        return chain.Select(id =>
        {
            translations.TryGetValue(id, out var tr);
            var slug = tr?.Slug ?? id.ToString("N")[..8];
            var label = tr?.Name ?? names.GetValueOrDefault(id) ?? "رده";
            return new StorefrontCategoryBreadcrumbItem(id, label, slug, $"/{uiSegment}/category/{slug}");
        }).ToList();
    }

    private async Task<IReadOnlyList<StorefrontCategoryChildItem>> BuildSubcategoriesAsync(
        Guid categoryId,
        IReadOnlyList<CatalogCategory> allCategories,
        string catalogLocale,
        string uiSegment,
        CancellationToken cancellationToken)
    {
        var children = allCategories
            .Where(c => c.ParentCategoryId == categoryId)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.CategoryId)
            .ToList();
        if (children.Count == 0)
        {
            return [];
        }

        var ids = children.Select(c => c.CategoryId).ToList();
        var translations = await _catalog.CategoryTranslations.AsNoTracking()
            .Where(t => ids.Contains(t.CategoryId) && t.Locale == catalogLocale)
            .ToDictionaryAsync(t => t.CategoryId, cancellationToken);
        var names = await LoadNamesAsync(CatalogLocalizedOwnerKind.Category, ids, cancellationToken);
        return children.Select(c =>
        {
            translations.TryGetValue(c.CategoryId, out var tr);
            var slug = tr?.Slug ?? c.CategoryId.ToString("N")[..8];
            var label = tr?.Name ?? names.GetValueOrDefault(c.CategoryId) ?? "رده";
            return new StorefrontCategoryChildItem(c.CategoryId, label, slug, $"/{uiSegment}/category/{slug}");
        }).ToList();
    }

    private static IReadOnlyList<StorefrontProductCard> ApplyTypedFilters(
        IReadOnlyList<StorefrontProductCard> products,
        IReadOnlyList<CatalogProductAttributeValue> attributeValues,
        IReadOnlyList<StorefrontPlpFilterInput> filters,
        IReadOnlyList<EffectiveCategoryFacet> facetDefs)
    {
        if (filters.Count == 0)
        {
            return products;
        }

        var byCode = facetDefs.ToDictionary(f => f.Code, StringComparer.OrdinalIgnoreCase);
        var valuesByProduct = attributeValues
            .GroupBy(v => v.ProductId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return products.Where(product =>
        {
            if (!valuesByProduct.TryGetValue(product.ProductId, out var rows))
            {
                rows = [];
            }

            // Cross-attribute AND
            foreach (var filter in filters)
            {
                if (!byCode.TryGetValue(filter.Code, out var facet))
                {
                    continue;
                }

                var matching = rows.Where(r => r.DefinitionId == facet.DefinitionId).ToList();
                if (matching.Count == 0)
                {
                    return false;
                }

                var kind = filter.Kind.Trim().ToLowerInvariant();
                if (kind is "enum" or "color" or "enumeration")
                {
                    // Same-attribute OR
                    var wanted = filter.Values.Select(v => v.Trim().ToLowerInvariant()).Where(v => v.Length > 0).ToHashSet();
                    if (wanted.Count == 0)
                    {
                        continue;
                    }

                    var hit = matching.Any(r =>
                    {
                        var canonical = r.CanonicalValue.Trim().ToLowerInvariant();
                        if (wanted.Contains(canonical))
                        {
                            return true;
                        }

                        if (Guid.TryParseExact(r.CanonicalValue, "N", out var optionId))
                        {
                            if (wanted.Contains(optionId.ToString("N"))
                                || wanted.Contains(optionId.ToString("D").ToLowerInvariant()))
                            {
                                return true;
                            }
                        }

                        return false;
                    });
                    if (!hit)
                    {
                        return false;
                    }
                }
                else if (kind is "boolean" or "bool")
                {
                    var expected = filter.Values.FirstOrDefault()?.Trim().ToLowerInvariant();
                    if (expected is null)
                    {
                        continue;
                    }

                    var wantTrue = expected is "true" or "1" or "بله";
                    if (!matching.Any(r => bool.TryParse(r.CanonicalValue, out var flag) && flag == wantTrue))
                    {
                        return false;
                    }
                }
                else if (kind is "range" or "number")
                {
                    var numbers = matching
                        .Select(r => decimal.TryParse(r.CanonicalValue, out var n) ? n : (decimal?)null)
                        .Where(n => n.HasValue)
                        .Select(n => n!.Value)
                        .ToList();
                    if (numbers.Count == 0)
                    {
                        return false;
                    }

                    var value = numbers[0];
                    if (filter.Min is decimal min && value < min)
                    {
                        return false;
                    }

                    if (filter.Max is decimal max && value > max)
                    {
                        return false;
                    }
                }
            }

            return true;
        }).ToList();
    }

    private async Task<IReadOnlyList<StorefrontPlpFacet>> BuildPlpFacetsAsync(
        IReadOnlyList<EffectiveCategoryFacet> facetDefs,
        IReadOnlyList<StorefrontProductCard> filteredProducts,
        IReadOnlyList<CatalogProductAttributeValue> allAttributeValues,
        string catalogLocale,
        CancellationToken cancellationToken)
    {
        var filteredIds = filteredProducts.Select(p => p.ProductId).ToHashSet();
        var relevantValues = allAttributeValues.Where(v => filteredIds.Contains(v.ProductId)).ToList();
        var result = new List<StorefrontPlpFacet>();

        foreach (var facet in facetDefs)
        {
            var forDef = relevantValues.Where(v => v.DefinitionId == facet.DefinitionId).ToList();
            if (facet.ValueKind == CatalogAttributeValueKind.Number || facet.DisplayType == CatalogFacetDisplayType.Range)
            {
                var nums = forDef
                    .Select(v => decimal.TryParse(v.CanonicalValue, out var n) ? n : (decimal?)null)
                    .Where(n => n.HasValue)
                    .Select(n => n!.Value)
                    .ToList();
                result.Add(new StorefrontPlpFacet(
                    facet.DefinitionId,
                    facet.Code,
                    facet.LocalizedName,
                    facet.ValueKind.ToString(),
                    facet.DisplayType.ToString(),
                    facet.IsSearchable,
                    facet.IsCollapsedByDefault,
                    facet.ShowCounts,
                    nums.Count == 0 ? null : nums.Min(),
                    nums.Count == 0 ? null : nums.Max(),
                    []));
                continue;
            }

            if (facet.ValueKind == CatalogAttributeValueKind.Boolean
                || facet.DisplayType == CatalogFacetDisplayType.BooleanToggle)
            {
                var trueCount = forDef.Count(v => bool.TryParse(v.CanonicalValue, out var f) && f);
                var falseCount = forDef.Count(v => bool.TryParse(v.CanonicalValue, out var f) && !f);
                var options = new List<StorefrontPlpFacetOption>();
                if (trueCount > 0 || forDef.Count == 0)
                {
                    options.Add(new StorefrontPlpFacetOption("true", "بله", facet.ShowCounts ? trueCount : null));
                }

                if (falseCount > 0 || forDef.Count == 0)
                {
                    options.Add(new StorefrontPlpFacetOption("false", "خیر", facet.ShowCounts ? falseCount : null));
                }

                result.Add(new StorefrontPlpFacet(
                    facet.DefinitionId,
                    facet.Code,
                    facet.LocalizedName,
                    facet.ValueKind.ToString(),
                    facet.DisplayType.ToString(),
                    facet.IsSearchable,
                    facet.IsCollapsedByDefault,
                    facet.ShowCounts,
                    null,
                    null,
                    options));
                continue;
            }

            // Enumeration / Text — group by canonical; resolve option labels
            var groups = forDef.GroupBy(v => v.CanonicalValue.Trim(), StringComparer.OrdinalIgnoreCase).ToList();
            var optionIds = groups
                .Select(g => Guid.TryParseExact(g.Key, "N", out var id) ? id : Guid.Empty)
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();
            var optionLabels = await LoadNamesAsync(CatalogLocalizedOwnerKind.AttributeOption, optionIds, cancellationToken);
            var optionsEnum = groups
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g =>
                {
                    var label = Guid.TryParseExact(g.Key, "N", out var oid)
                        ? optionLabels.GetValueOrDefault(oid) ?? g.Key
                        : g.Key;
                    return new StorefrontPlpFacetOption(
                        g.Key,
                        label,
                        facet.ShowCounts ? g.Select(x => x.ProductId).Distinct().Count() : null);
                })
                .ToList();

            result.Add(new StorefrontPlpFacet(
                facet.DefinitionId,
                facet.Code,
                facet.LocalizedName,
                facet.ValueKind.ToString(),
                facet.DisplayType.ToString(),
                facet.IsSearchable,
                facet.IsCollapsedByDefault,
                facet.ShowCounts,
                null,
                null,
                optionsEnum));
        }

        return result;
    }

    private static IReadOnlyList<StorefrontAppliedFilterChip> BuildAppliedChips(
        IReadOnlyList<StorefrontPlpFilterInput> filters,
        IReadOnlyList<EffectiveCategoryFacet> facetDefs,
        IReadOnlyList<StorefrontPlpFacet> plpFacets)
    {
        var byCode = facetDefs.ToDictionary(f => f.Code, StringComparer.OrdinalIgnoreCase);
        var optionsByCode = plpFacets.ToDictionary(f => f.Code, f => f.Options, StringComparer.OrdinalIgnoreCase);
        var chips = new List<StorefrontAppliedFilterChip>();
        foreach (var filter in filters)
        {
            if (!byCode.TryGetValue(filter.Code, out var facet))
            {
                continue;
            }

            var kind = filter.Kind.Trim().ToLowerInvariant();
            if (kind is "range" or "number")
            {
                var display = $"{filter.Min?.ToString() ?? "…"} – {filter.Max?.ToString() ?? "…"}";
                chips.Add(new StorefrontAppliedFilterChip(filter.Code, facet.LocalizedName, display, display));
                continue;
            }

            optionsByCode.TryGetValue(filter.Code, out var options);
            foreach (var value in filter.Values)
            {
                var label = options?.FirstOrDefault(o => o.Value.Equals(value, StringComparison.OrdinalIgnoreCase))?.Label
                    ?? value;
                chips.Add(new StorefrontAppliedFilterChip(filter.Code, facet.LocalizedName, value, label));
            }
        }

        return chips;
    }
}
