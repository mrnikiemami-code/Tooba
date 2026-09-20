using Microsoft.EntityFrameworkCore;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Content.Domain;
using Tooba.Host.Storefront;
using Tooba.Promotion.Application;
using Tooba.Promotion.Domain;

namespace Tooba.Host.Admin;

/// <summary>صفحات Landing همین Store و ارجاع Home.</summary>
public sealed class StoreLandingPageComposer
{
    internal const string PageCachePrefix = "store-landing-page:";
    internal const string HomeCachePrefix = "store-landing-home:";

    private readonly CatalogDbContext _catalog;
    private readonly ICurrentCommerceContext _commerce;
    private readonly IMemoryCache _cache;
    private readonly StorefrontComposer? _storefront;
    private readonly IMerchandisingCampaignQuery _campaignQuery;
    private readonly ISender _sender;

    /// <summary>خواندن از Catalog؛ نوشتن از طریق ISender → Command.</summary>
    public StoreLandingPageComposer(
        CatalogDbContext catalog,
        ICurrentCommerceContext commerce,
        IMemoryCache cache,
        IMerchandisingCampaignQuery campaignQuery,
        ISender sender,
        StorefrontComposer? storefront = null)
    {
        _catalog = catalog;
        _commerce = commerce;
        _cache = cache;
        _campaignQuery = campaignQuery;
        _sender = sender;
        _storefront = storefront;
    }

    /// <summary>فهرست صفحات همین Store.</summary>
    public async Task<IReadOnlyList<StoreLandingPageAdminView>> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await _catalog.StoreLandingPages.AsNoTracking()
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(cancellationToken);
        return rows.Select(ToAdmin).ToList();
    }

    /// <summary>یک صفحه برای ویرایش Admin.</summary>
    public async Task<StoreLandingPageAdminView> GetAsync(Guid pageId, CancellationToken cancellationToken) =>
        ToAdmin(await RequirePageAsync(pageId, cancellationToken));

    /// <summary>پیش‌نمایش مجاز Admin؛ پیش‌نویس عمومی نمی‌شود.</summary>
    public async Task<StoreLandingPagePublicView> ResolvePreviewAsync(Guid pageId, CancellationToken cancellationToken)
    {
        var page = await RequirePageAsync(pageId, cancellationToken);
        return await ToPublicAsync(page, publicOnly: true, cancellationToken);
    }

    /// <summary>صفحهٔ پیش‌نویس می‌سازد.</summary>
    public async Task<StoreLandingPageAdminView> CreateAsync(StoreLandingPageWriteRequest body, CancellationToken cancellationToken)
    {
        var page = await _sender.Send(
            new CreateStoreLandingPageCommand(ToWriteModel(body)),
            cancellationToken);
        Invalidate(page.Locale, page.Slug);
        return ToAdmin(page);
    }

    /// <summary>صفحه را به‌روز می‌کند.</summary>
    public async Task<StoreLandingPageAdminView> UpdateAsync(Guid pageId, StoreLandingPageWriteRequest body, CancellationToken cancellationToken)
    {
        var existing = await RequirePageAsync(pageId, cancellationToken);
        var previousLocale = existing.Locale;
        var previousSlug = existing.Slug;
        var page = await _sender.Send(
            new UpdateStoreLandingPageCommand(pageId, ToWriteModel(body)),
            cancellationToken);
        Invalidate(previousLocale, previousSlug);
        Invalidate(page.Locale, page.Slug);
        return ToAdmin(page);
    }

    /// <summary>انتشار یا برگشت به پیش‌نویس.</summary>
    public async Task<StoreLandingPageAdminView> SetStatusAsync(Guid pageId, string? status, CancellationToken cancellationToken)
    {
        var page = await _sender.Send(
            new SetStoreLandingPageStatusCommand(pageId, status),
            cancellationToken);
        Invalidate(page.Locale, page.Slug);
        return ToAdmin(page);
    }

    /// <summary>ارجاع خانه را اتمیک روی همین Store می‌نویسد؛ نوع صفحه را هم‌زمان عوض می‌کند.</summary>
    public async Task<StoreHomeSelectionView> SetHomeAsync(Guid? pageId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetStoreHomePageCommand(pageId), cancellationToken);
        if (result.InvalidateLocale is not null && result.InvalidateSlug is not null)
        {
            Invalidate(result.InvalidateLocale, result.InvalidateSlug);
        }

        if (result.InvalidateHome)
        {
            InvalidateHome();
        }

        return await GetHomeSelectionAsync(cancellationToken);
    }

    /// <summary>صفحهٔ Landing منتشرشدهٔ عمومی را با Store+Locale+Slug حل می‌کند.</summary>
    public async Task<StoreLandingPagePublicView?> ResolvePublicAsync(string? locale, string? slug, CancellationToken cancellationToken)
    {
        var normalizedLocale = StoreLandingPageSlug.NormalizeLocale(locale);
        var normalizedSlug = StoreLandingPageSlug.Normalize(slug);
        if (StoreLandingPageSlug.IsReserved(normalizedSlug) || !StoreLandingPageSlug.IsValid(normalizedSlug))
        {
            return null;
        }

        var cacheKey = PageCacheKey(Scope(), normalizedLocale, normalizedSlug);
        if (_cache.TryGetValue(cacheKey, out StoreLandingPagePublicView? cached))
        {
            return cached;
        }

        // Indexed Locale+Slug unique; Status/PageType predicates avoid loading non-public rows.
        var page = await _catalog.StoreLandingPages.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Locale == normalizedLocale
                    && x.Slug == normalizedSlug
                    && x.PageType == StorePageType.Landing
                    && x.Status == StoreLandingPageStatus.Published,
                cancellationToken);
        if (page is null)
        {
            _cache.Set(cacheKey, (StoreLandingPagePublicView?)null, TimeSpan.FromSeconds(15));
            return null;
        }

        var view = await ToPublicAsync(page, publicOnly: true, cancellationToken);
        _cache.Set(cacheKey, view, TimeSpan.FromMinutes(2));
        return view;
    }

    /// <summary>Landingهای ایندکس‌پذیر برای sitemap (بدون تکرار ریشهٔ Home).</summary>
    public async Task<IReadOnlyList<StoreLandingSitemapEntry>> ListIndexableLandingsAsync(CancellationToken cancellationToken)
    {
        var rows = await _catalog.StoreLandingPages.AsNoTracking()
            .Where(x =>
                x.PageType == StorePageType.Landing
                && x.Status == StoreLandingPageStatus.Published
                && x.RobotsIndex)
            .OrderBy(x => x.Locale)
            .ThenBy(x => x.Slug)
            .Select(x => new StoreLandingSitemapEntry(x.Locale, x.Slug, x.UpdatedAt))
            .ToListAsync(cancellationToken);
        return rows;
    }

    /// <summary>فهرست بخش‌های یک صفحه برای Admin.</summary>
    public async Task<IReadOnlyList<StoreLandingPageSectionAdminView>> ListSectionsAsync(Guid pageId, CancellationToken cancellationToken)
    {
        await RequirePageAsync(pageId, cancellationToken);
        var rows = await LoadSectionsAsync(pageId, cancellationToken);
        return rows.Select(ToAdminSection).ToList();
    }

    /// <summary>بخش تأییدشده به صفحه اضافه می‌کند؛ InsertAt ایندکس اختیاری برای درج بین بخش‌ها است.</summary>
    public async Task<StoreLandingPageSectionAdminView> AddSectionAsync(Guid pageId, StoreLandingPageSectionWriteRequest body, CancellationToken cancellationToken)
    {
        var page = await RequirePageAsync(pageId, cancellationToken);
        var section = await _sender.Send(
            new AddStoreLandingPageSectionCommand(pageId, ToSectionModel(body)),
            cancellationToken);
        Invalidate(page.Locale, page.Slug);
        return ToAdminSection(section);
    }

    /// <summary>ترکیب بخش‌های صفحه را یکجا جایگزین می‌کند (اعمال قالب)؛ دادهٔ Catalog کسب‌وکار را لمس نمی‌کند.</summary>
    public async Task<IReadOnlyList<StoreLandingPageSectionAdminView>> ReplaceCompositionAsync(
        Guid pageId,
        IReadOnlyList<StoreLandingPageSectionWriteRequest>? sections,
        CancellationToken cancellationToken)
    {
        var page = await RequirePageAsync(pageId, cancellationToken);
        var payloads = (sections ?? []).Select(ToSectionModel).ToList();
        var created = await _sender.Send(
            new ReplaceStoreLandingPageCompositionCommand(pageId, payloads),
            cancellationToken);
        Invalidate(page.Locale, page.Slug);
        return created.OrderBy(x => x.SortOrder).Select(ToAdminSection).ToList();
    }

    /// <summary>پیکربندی بخش را به‌روز می‌کند.</summary>
    public async Task<StoreLandingPageSectionAdminView> UpdateSectionAsync(Guid pageId, Guid sectionId, StoreLandingPageSectionWriteRequest body, CancellationToken cancellationToken)
    {
        var page = await RequirePageAsync(pageId, cancellationToken);
        var section = await _sender.Send(
            new UpdateStoreLandingPageSectionCommand(pageId, sectionId, ToSectionModel(body)),
            cancellationToken);
        Invalidate(page.Locale, page.Slug);
        return ToAdminSection(section);
    }

    /// <summary>فعال یا غیرفعال کردن بخش.</summary>
    public async Task<StoreLandingPageSectionAdminView> SetSectionEnabledAsync(Guid pageId, Guid sectionId, bool enabled, CancellationToken cancellationToken)
    {
        var page = await RequirePageAsync(pageId, cancellationToken);
        var section = await _sender.Send(
            new SetStoreLandingPageSectionEnabledCommand(pageId, sectionId, enabled),
            cancellationToken);
        Invalidate(page.Locale, page.Slug);
        return ToAdminSection(section);
    }

    /// <summary>ترتیب پایدار بخش‌ها را بدون حذف/ایجاد دوباره می‌نویسد.</summary>
    public async Task<IReadOnlyList<StoreLandingPageSectionAdminView>> ReorderSectionsAsync(Guid pageId, IReadOnlyList<Guid>? sectionIds, CancellationToken cancellationToken)
    {
        var page = await RequirePageAsync(pageId, cancellationToken);
        var rows = await _sender.Send(
            new ReorderStoreLandingPageSectionsCommand(pageId, sectionIds ?? []),
            cancellationToken);
        Invalidate(page.Locale, page.Slug);
        return rows.OrderBy(x => x.SortOrder).Select(ToAdminSection).ToList();
    }

    /// <summary>صفحه و بخش‌هایش را حذف می‌کند؛ اگر خانه بود ارجاع را پاک می‌کند.</summary>
    public async Task DeletePageAsync(Guid pageId, CancellationToken cancellationToken)
    {
        var page = await _sender.Send(new DeleteStoreLandingPageCommand(pageId), cancellationToken);
        Invalidate(page.Locale, page.Slug);
        if (page.PageType == StorePageType.Home)
        {
            Invalidate(page.Locale, "home");
        }
    }

    /// <summary>بخش را حذف می‌کند و ترتیب را نرمال می‌کند.</summary>
    public async Task DeleteSectionAsync(Guid pageId, Guid sectionId, CancellationToken cancellationToken)
    {
        var page = await RequirePageAsync(pageId, cancellationToken);
        await _sender.Send(new DeleteStoreLandingPageSectionCommand(pageId, sectionId), cancellationToken);
        Invalidate(page.Locale, page.Slug);
    }

    /// <summary>ارجاع خانه بدون جایگزینی ترکیب خانهٔ فعلی.</summary>
    public async Task<StoreHomeSelectionView> GetHomeSelectionAsync(CancellationToken cancellationToken)
    {
        var cacheKey = HomeCacheKey(Scope());
        if (_cache.TryGetValue(cacheKey, out StoreHomeSelectionView? cached) && cached is not null)
        {
            return cached;
        }

        var settings = await _catalog.StoreAppearanceSettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SettingsId == StoreAppearanceSettings.SingletonId, cancellationToken);
        StoreLandingPagePublicView? selected = null;
        if (settings?.HomePageId is { } homeId)
        {
            var page = await _catalog.StoreLandingPages.AsNoTracking()
                .SingleOrDefaultAsync(x => x.PageId == homeId, cancellationToken);
            if (page is { Status: StoreLandingPageStatus.Published })
            {
                selected = await ToPublicAsync(page, publicOnly: true, cancellationToken);
            }
        }

        var view = new StoreHomeSelectionView(Scope(), settings?.HomePageId, selected, UsesCanonicalHome: selected is null);
        _cache.Set(cacheKey, view, TimeSpan.FromMinutes(2));
        return view;
    }

    private async Task<List<StoreLandingPageSection>> LoadSectionsAsync(Guid pageId, CancellationToken cancellationToken) =>
        await _catalog.StoreLandingPageSections
            .Where(x => x.PageId == pageId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.PageSectionId)
            .ToListAsync(cancellationToken);

    private async Task<StoreLandingPage> RequirePageAsync(Guid pageId, CancellationToken cancellationToken)
    {
        var page = await _catalog.StoreLandingPages.SingleOrDefaultAsync(x => x.PageId == pageId, cancellationToken);
        if (page is null)
        {
            throw new PlatformHttpException(404, "صفحه یافت نشد.", "landing.page.missing");
        }

        return page;
    }

    private void Invalidate(string locale, string slug)
    {
        _cache.Remove(PageCacheKey(Scope(), locale, slug));
        InvalidateHome();
    }

    private void InvalidateHome() => _cache.Remove(HomeCacheKey(Scope()));

    private string Scope() => StoreAppearanceProjector.ScopeKey(_commerce.Current);

    private static string PageCacheKey(string scope, string locale, string slug) =>
        $"{PageCachePrefix}{scope}:{locale}:{slug}";

    private static string HomeCacheKey(string scope) => $"{HomeCachePrefix}{scope}";

    private static StoreLandingPageWriteModel ToWriteModel(StoreLandingPageWriteRequest body) => new(
        body.Title,
        body.Slug,
        body.Locale,
        body.SeoTitle,
        body.SeoDescription,
        body.PageType,
        body.RobotsIndex,
        body.RobotsFollow,
        body.CanonicalUrl,
        body.OgTitle,
        body.OgDescription,
        body.OgImageUrl,
        body.PrimaryH1);

    private static StoreLandingPageSectionWriteModel ToSectionModel(StoreLandingPageSectionWriteRequest body) => new(
        body.SectionType,
        body.ConfigJson,
        body.Config,
        body.IsEnabled,
        body.InsertAt);

    private static StoreLandingPageAdminView ToAdmin(StoreLandingPage page) => new(
        page.PageId,
        page.PageType.ToString(),
        page.Locale,
        page.Slug,
        page.Title,
        page.SeoTitle,
        page.SeoDescription,
        page.RobotsIndex,
        page.RobotsFollow,
        page.CanonicalUrl,
        page.OgTitle,
        page.OgDescription,
        page.OgImageUrl,
        page.PrimaryH1,
        page.TemplateKey,
        page.Status.ToString(),
        page.UpdatedAt);

    private static StoreLandingPageSectionAdminView ToAdminSection(StoreLandingPageSection section) => new(
        section.PageSectionId,
        section.PageId,
        section.SectionType,
        section.SortOrder,
        section.IsEnabled,
        section.ConfigurationJson,
        section.UpdatedAt);

    private async Task<StoreLandingPagePublicView> ToPublicAsync(StoreLandingPage page, bool publicOnly, CancellationToken cancellationToken)
    {
        var rows = await _catalog.StoreLandingPageSections.AsNoTracking()
            .Where(x => x.PageId == page.PageId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.PageSectionId)
            .ToListAsync(cancellationToken);
        if (publicOnly)
        {
            rows = rows.Where(x => x.IsEnabled).ToList();
        }

        // Sequential section resolve — CatalogDbContext is not thread-safe for concurrent queries.
        var sections = new List<StoreLandingPagePublicSectionView>(rows.Count);
        foreach (var row in rows)
        {
            var items = row.SectionType == StoreLandingPageSectionRegistry.ProductCollection
                ? await ResolveProductItemsAsync(row, page.Locale, cancellationToken)
                : Array.Empty<StoreLandingPageResolvedItem>();
            sections.Add(new StoreLandingPagePublicSectionView(
                row.PageSectionId,
                row.SectionType,
                row.SortOrder,
                row.ConfigurationJson,
                items));
        }

        // Embed only section-scoped shell data so FE SSR avoids heavy /home + full Catalog listing.
        var needsProducts = sections.Any(x => x.SectionType == StoreLandingPageSectionRegistry.ProductCollection);
        var needsBrands = sections.Any(x => x.SectionType == StoreLandingPageSectionRegistry.BrandStrip);
        var needsArticles = sections.Any(x => x.SectionType == StoreLandingPageSectionRegistry.ArticleList);
        var needsReviews = sections.Any(x => x.SectionType == StoreLandingPageSectionRegistry.Reviews);

        IReadOnlyList<StorefrontProductCard> products = Array.Empty<StorefrontProductCard>();
        IReadOnlyList<StorefrontCategoryItem> categories = Array.Empty<StorefrontCategoryItem>();
        IReadOnlyList<StorefrontBrandItem> brands = Array.Empty<StorefrontBrandItem>();
        IReadOnlyList<StorefrontArticleItem> articles = Array.Empty<StorefrontArticleItem>();
        IReadOnlyList<StorefrontFeaturedReviewItem> reviews = Array.Empty<StorefrontFeaturedReviewItem>();

        if (_storefront is not null)
        {
            if (needsProducts)
            {
                var productIds = sections
                    .SelectMany(section => section.Items.Select(item => item.Id))
                    .Distinct()
                    .ToArray();
                var composed = await _storefront.ComposeProductCardsAsync(productIds, cancellationToken);
                var overlays = sections
                    .SelectMany(section => section.Items)
                    .Where(item => item.PromotionalAmountExclusiveOfTax is not null
                                   || item.OfferAmountExclusiveOfTax is not null
                                   || item.MerchandisingCampaignId is not null)
                    .GroupBy(item => item.Id)
                    .ToDictionary(group => group.Key, group => group.First());
                products = composed.Values.Select(card =>
                {
                    if (!overlays.TryGetValue(card.ProductId, out var overlay))
                    {
                        return card;
                    }

                    var offerAmount = overlay.OfferAmountExclusiveOfTax ?? card.OfferAmountExclusiveOfTax;
                    var promo = overlay.PromotionalAmountExclusiveOfTax;
                    if (promo is decimal promoAmount && promoAmount >= offerAmount)
                    {
                        promo = null;
                    }

                    return card with
                    {
                        OfferAmountExclusiveOfTax = offerAmount,
                        PromotionalAmountExclusiveOfTax = promo,
                        Currency = overlay.Currency ?? card.Currency,
                        PromotionLabel = overlay.PromotionLabel ?? card.PromotionLabel,
                        MerchandisingCampaignId = overlay.MerchandisingCampaignId ?? card.MerchandisingCampaignId,
                    };
                }).ToList();
            }

            // Sequential shell fetches — shared scoped DbContexts must not run concurrently.
            categories = await _storefront.ListCategoriesAsync(cancellationToken);
            brands = needsBrands
                ? await _storefront.ListBrandsAsync(cancellationToken)
                : Array.Empty<StorefrontBrandItem>();
            articles = needsArticles
                ? await _storefront.BuildLatestArticlesAsync(
                    ContentTaxonomySeoRules.ResolveContentLocale(page.Locale),
                    cancellationToken)
                : Array.Empty<StorefrontArticleItem>();
            reviews = needsReviews
                ? await _storefront.BuildFeaturedReviewsAsync(cancellationToken)
                : Array.Empty<StorefrontFeaturedReviewItem>();
        }

        return new StoreLandingPagePublicView(
            page.PageId,
            page.PageType.ToString(),
            page.Locale,
            page.Slug,
            page.Title,
            page.SeoTitle ?? page.Title,
            page.SeoDescription,
            page.RobotsIndex,
            page.RobotsFollow,
            page.CanonicalUrl,
            page.OgTitle ?? page.SeoTitle ?? page.Title,
            page.OgDescription ?? page.SeoDescription,
            page.OgImageUrl,
            page.ResolvePrimaryH1(),
            page.TemplateKey,
            sections,
            products,
            categories,
            brands,
            articles,
            reviews);
    }

    private async Task<IReadOnlyList<StoreLandingPageResolvedItem>> ResolveProductItemsAsync(
        StoreLandingPageSection section,
        string pageLocale,
        CancellationToken cancellationToken)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(section.ConfigurationJson);
        var root = doc.RootElement;
        var source = root.TryGetProperty("source", out var sourceEl) ? sourceEl.GetString() : "Newest";
        var take = root.TryGetProperty("take", out var takeEl) && takeEl.TryGetInt32(out var parsedTake)
            ? Math.Clamp(parsedTake, 1, StoreLandingPageSectionRegistry.MaxTake)
            : StoreLandingPageSectionRegistry.DefaultTake;

        if (string.Equals(source, "PromotionCampaign", StringComparison.OrdinalIgnoreCase))
        {
            return await ResolvePromotionCampaignItemsAsync(root, pageLocale, take, cancellationToken);
        }

        IQueryable<CatalogProduct> query = _catalog.Products.AsNoTracking()
            .Where(x => x.Status == CatalogPublicationStatus.Published);
        if (string.Equals(source, "Category", StringComparison.Ordinal) && root.TryGetProperty("categoryId", out var categoryEl))
        {
            var categoryId = categoryEl.GetGuid();
            var productIds = _catalog.ProductCategories
                .Where(x => x.CategoryId == categoryId)
                .Select(x => x.ProductId);
            query = query.Where(x => productIds.Contains(x.ProductId));
        }
        else if (string.Equals(source, "Brand", StringComparison.Ordinal) && root.TryGetProperty("brandId", out var brandEl))
        {
            var brandId = brandEl.GetGuid();
            query = query.Where(x => x.BrandId == brandId);
        }
        else if (string.Equals(source, "Manual", StringComparison.Ordinal) && root.TryGetProperty("productIds", out var idsEl))
        {
            var ids = idsEl.EnumerateArray().Select(x => x.GetGuid()).ToList();
            query = query.Where(x => ids.Contains(x.ProductId));
        }

        return await query
            .OrderByDescending(x => x.UpdatedAt)
            .Take(take)
            .Select(x => new StoreLandingPageResolvedItem(
                x.ProductId,
                x.SlugSeam,
                null,
                null,
                null,
                null,
                null))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<StoreLandingPageResolvedItem>> ResolvePromotionCampaignItemsAsync(
        System.Text.Json.JsonElement root,
        string pageLocale,
        int take,
        CancellationToken cancellationToken)
    {
        var storeId = ResolveMerchandisingStoreId();
        if (storeId is null)
        {
            return Array.Empty<StoreLandingPageResolvedItem>();
        }

        var typeCode = root.TryGetProperty("promotionTypeCode", out var typeEl)
            ? typeEl.GetString()?.Trim()
            : null;
        if (string.IsNullOrWhiteSpace(typeCode))
        {
            typeCode = MerchandisingPromotionType.AmazingCode;
        }

        Guid? campaignId = null;
        if (root.TryGetProperty("campaignId", out var campaignEl)
            && campaignEl.ValueKind is not System.Text.Json.JsonValueKind.Null
            && campaignEl.ValueKind is not System.Text.Json.JsonValueKind.Undefined
            && Guid.TryParse(campaignEl.ToString(), out var parsedCampaign)
            && parsedCampaign != Guid.Empty)
        {
            campaignId = parsedCampaign;
        }

        var now = DateTimeOffset.UtcNow;
        // Oversample so commercial-eligibility filter can still fill `take`.
        var fetchTake = Math.Min(
            MerchandisingCampaignRuntimeLimits.MaxMemberTake,
            Math.Max(take * 3, take));
        IReadOnlyList<MerchandisingCampaignMemberRuntimeModel> members;
        string? badge = "Amazing";
        Guid? resolvedCampaignId = campaignId;
        if (campaignId is { } explicitId)
        {
            members = await _campaignQuery.ResolveCampaignMembersAsync(
                explicitId,
                storeId.Value,
                pageLocale,
                now,
                fetchTake,
                null,
                cancellationToken);
        }
        else
        {
            var active = await _campaignQuery.ResolveActiveByTypeAsync(
                storeId.Value,
                typeCode,
                pageLocale,
                now,
                fetchTake,
                null,
                cancellationToken);
            members = active?.Members ?? Array.Empty<MerchandisingCampaignMemberRuntimeModel>();
            resolvedCampaignId = active?.CampaignId;
            if (!string.IsNullOrWhiteSpace(active?.BadgeText))
            {
                badge = active.BadgeText;
            }
        }

        // Membership may omit promo price; Amazing rail only shows commercially eligible discounts.
        members = members
            .Where(MerchandisingCampaignStorefrontEligibility.IsAmazingRailEligible)
            .Take(take)
            .ToList();

        if (members.Count == 0)
        {
            return Array.Empty<StoreLandingPageResolvedItem>();
        }

        var variantIds = members.Select(x => x.CatalogVariantId).Distinct().ToArray();
        var variantRows = await _catalog.Variants.AsNoTracking()
            .Where(x => variantIds.Contains(x.VariantId))
            .Select(x => new { x.VariantId, x.ProductId })
            .ToListAsync(cancellationToken);
        var variantToProduct = variantRows.ToDictionary(x => x.VariantId, x => x.ProductId);
        var productIdsOrdered = new List<Guid>(members.Count);
        var memberByProduct = new Dictionary<Guid, MerchandisingCampaignMemberRuntimeModel>();
        foreach (var member in members)
        {
            if (!variantToProduct.TryGetValue(member.CatalogVariantId, out var productId))
            {
                continue;
            }

            if (!memberByProduct.TryAdd(productId, member))
            {
                continue;
            }

            productIdsOrdered.Add(productId);
        }

        if (productIdsOrdered.Count == 0)
        {
            return Array.Empty<StoreLandingPageResolvedItem>();
        }

        var products = await _catalog.Products.AsNoTracking()
            .Where(x => productIdsOrdered.Contains(x.ProductId) && x.Status == CatalogPublicationStatus.Published)
            .Select(x => new { x.ProductId, x.SlugSeam })
            .ToListAsync(cancellationToken);
        var byId = products.ToDictionary(x => x.ProductId);
        var result = new List<StoreLandingPageResolvedItem>(productIdsOrdered.Count);
        foreach (var productId in productIdsOrdered)
        {
            if (!byId.TryGetValue(productId, out var row))
            {
                continue;
            }

            memberByProduct.TryGetValue(productId, out var member);
            decimal? offerAmount = null;
            decimal? promoAmount = null;
            string? currency = null;
            string? label = null;
            if (member is not null)
            {
                currency = member.PriceCurrency;
                if (member.CompareAtAmount is decimal compareAt && member.PriceAmount is decimal selling)
                {
                    offerAmount = compareAt;
                    promoAmount = selling;
                    label = badge;
                }
                else if (member.PriceAmount is decimal only)
                {
                    offerAmount = only;
                }
            }

            result.Add(new StoreLandingPageResolvedItem(
                row.ProductId,
                row.SlugSeam,
                offerAmount,
                promoAmount,
                currency,
                label,
                resolvedCampaignId));
        }

        return result;
    }

    /// <summary>
    /// StoreId کمپین مرچندایزینگ برای tenant جاری (SingleStore store-alpha → GUID پایدار دانهٔ Dev).
    /// </summary>
    private Guid? ResolveMerchandisingStoreId()
    {
        var tenantId = _commerce.Current?.Tenant?.TenantId.Value;
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return null;
        }

        if (string.Equals(tenantId, "store-alpha", StringComparison.OrdinalIgnoreCase))
        {
            return MerchandisingCampaignDevelopmentSeed.StoreAlphaId;
        }

        return Guid.TryParse(tenantId, out var parsed) ? parsed : null;
    }
}

/// <summary>درخواست نوشتن صفحه.</summary>
public sealed record StoreLandingPageWriteRequest(
    string? Title,
    string? Slug,
    string? Locale,
    string? SeoTitle,
    string? SeoDescription,
    string? Status,
    string? PageType = null,
    bool? RobotsIndex = null,
    bool? RobotsFollow = null,
    string? CanonicalUrl = null,
    string? OgTitle = null,
    string? OgDescription = null,
    string? OgImageUrl = null,
    string? PrimaryH1 = null);

/// <summary>درخواست انتخاب خانه.</summary>
public sealed record StoreHomeSelectionWriteRequest(Guid? HomePageId);

/// <summary>نمای Admin.</summary>
public sealed record StoreLandingPageAdminView(
    Guid PageId,
    string PageType,
    string Locale,
    string Slug,
    string Title,
    string? SeoTitle,
    string? SeoDescription,
    bool RobotsIndex,
    bool RobotsFollow,
    string? CanonicalUrl,
    string? OgTitle,
    string? OgDescription,
    string? OgImageUrl,
    string? PrimaryH1,
    string TemplateKey,
    string Status,
    DateTimeOffset UpdatedAt);

/// <summary>درخواست نوشتن بخش.</summary>
public sealed record StoreLandingPageSectionWriteRequest(
    string? SectionType,
    string? Config,
    string? ConfigJson,
    bool? IsEnabled,
    int? InsertAt = null);

/// <summary>درخواست جایگزینی کامل ترکیب بخش‌ها (اعمال قالب).</summary>
public sealed record StoreLandingPageCompositionReplaceRequest(
    IReadOnlyList<StoreLandingPageSectionWriteRequest>? Sections);

/// <summary>درخواست ترتیب بخش‌ها.</summary>
public sealed record StoreLandingPageSectionReorderRequest(IReadOnlyList<Guid>? SectionIds);

/// <summary>درخواست فعال‌سازی بخش.</summary>
public sealed record StoreLandingPageSectionEnabledRequest(bool IsEnabled);

/// <summary>نمای Admin بخش.</summary>
public sealed record StoreLandingPageSectionAdminView(
    Guid PageSectionId,
    Guid PageId,
    string SectionType,
    int SortOrder,
    bool IsEnabled,
    string Config,
    DateTimeOffset UpdatedAt);

/// <summary>آیتم حل‌شدهٔ منبع کنترل‌شده.</summary>
public sealed record StoreLandingPageResolvedItem(
    Guid Id,
    string? Slug,
    decimal? OfferAmountExclusiveOfTax = null,
    decimal? PromotionalAmountExclusiveOfTax = null,
    string? Currency = null,
    string? PromotionLabel = null,
    Guid? MerchandisingCampaignId = null);

/// <summary>نمای عمومی بخش Published.</summary>
public sealed record StoreLandingPagePublicSectionView(
    Guid PageSectionId,
    string SectionType,
    int SortOrder,
    string Config,
    IReadOnlyList<StoreLandingPageResolvedItem> Items);

/// <summary>نمای عمومی Published.</summary>
public sealed record StoreLandingPagePublicView(
    Guid PageId,
    string PageType,
    string Locale,
    string Slug,
    string Title,
    string SeoTitle,
    string? SeoDescription,
    bool RobotsIndex,
    bool RobotsFollow,
    string? CanonicalUrl,
    string? OgTitle,
    string? OgDescription,
    string? OgImageUrl,
    string PrimaryH1,
    string TemplateKey,
    IReadOnlyList<StoreLandingPagePublicSectionView> Sections,
    IReadOnlyList<StorefrontProductCard> Products,
    IReadOnlyList<StorefrontCategoryItem> Categories,
    IReadOnlyList<StorefrontBrandItem> Brands,
    IReadOnlyList<StorefrontArticleItem> Articles,
    IReadOnlyList<StorefrontFeaturedReviewItem> Reviews);

/// <summary>ورودی sitemap برای Landing ایندکس‌پذیر.</summary>
public sealed record StoreLandingSitemapEntry(string Locale, string Slug, DateTimeOffset UpdatedAt);

/// <summary>ارجاع خانه بدون جایگزینی UI خانه.</summary>
public sealed record StoreHomeSelectionView(
    string StoreScope,
    Guid? HomePageId,
    StoreLandingPagePublicView? SelectedPage,
    bool UsesCanonicalHome);
