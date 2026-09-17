using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Content.Domain;
using Tooba.Host.Storefront;

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

    /// <summary>نویسنده صفحه را به Catalog همین Store وصل می‌کند.</summary>
    public StoreLandingPageComposer(
        CatalogDbContext catalog,
        ICurrentCommerceContext commerce,
        IMemoryCache cache,
        StorefrontComposer? storefront = null)
    {
        _catalog = catalog;
        _commerce = commerce;
        _cache = cache;
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
        var now = DateTimeOffset.UtcNow;
        var page = StoreLandingPage.Create(
            body.Locale,
            body.Slug,
            body.Title,
            body.SeoTitle,
            body.SeoDescription,
            now,
            body.PageType,
            body.RobotsIndex,
            body.RobotsFollow,
            body.CanonicalUrl,
            body.OgTitle,
            body.OgDescription,
            body.OgImageUrl,
            body.PrimaryH1);
        await EnsureUniqueSlugAsync(page.Locale, page.Slug, exceptPageId: null, cancellationToken);
        _catalog.StoreLandingPages.Add(page);
        await _catalog.SaveChangesAsync(cancellationToken);
        Invalidate(page.Locale, page.Slug);
        return ToAdmin(page);
    }

    /// <summary>صفحه را به‌روز می‌کند.</summary>
    public async Task<StoreLandingPageAdminView> UpdateAsync(Guid pageId, StoreLandingPageWriteRequest body, CancellationToken cancellationToken)
    {
        var page = await RequirePageAsync(pageId, cancellationToken);
        var previousLocale = page.Locale;
        var previousSlug = page.Slug;
        page.Update(
            body.Slug,
            body.Title,
            body.SeoTitle,
            body.SeoDescription,
            DateTimeOffset.UtcNow,
            body.RobotsIndex,
            body.RobotsFollow,
            body.CanonicalUrl,
            body.OgTitle,
            body.OgDescription,
            body.OgImageUrl,
            body.PrimaryH1);
        await EnsureUniqueSlugAsync(page.Locale, page.Slug, page.PageId, cancellationToken);
        await _catalog.SaveChangesAsync(cancellationToken);
        Invalidate(previousLocale, previousSlug);
        Invalidate(page.Locale, page.Slug);
        return ToAdmin(page);
    }

    /// <summary>انتشار یا برگشت به پیش‌نویس.</summary>
    public async Task<StoreLandingPageAdminView> SetStatusAsync(Guid pageId, string? status, CancellationToken cancellationToken)
    {
        var page = await RequirePageAsync(pageId, cancellationToken);
        if (string.Equals(status, "Published", StringComparison.OrdinalIgnoreCase))
        {
            page.Publish(DateTimeOffset.UtcNow);
        }
        else if (string.Equals(status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            page.Unpublish(DateTimeOffset.UtcNow);
            await ClearHomeIfMatchesAsync(page.PageId, cancellationToken);
        }
        else
        {
            throw new PlatformHttpException(400, "وضعیت انتشار معتبر نیست.", "landing.status.invalid");
        }

        await _catalog.SaveChangesAsync(cancellationToken);
        Invalidate(page.Locale, page.Slug);
        return ToAdmin(page);
    }

    /// <summary>ارجاع خانه را اتمیک روی همین Store می‌نویسد؛ نوع صفحه را هم‌زمان عوض می‌کند.</summary>
    public async Task<StoreHomeSelectionView> SetHomeAsync(Guid? pageId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var useTx = _catalog.Database.IsRelational();
        await using var tx = useTx
            ? await _catalog.Database.BeginTransactionAsync(cancellationToken)
            : null;
        var settings = await RequireSettingsAsync(now, cancellationToken);

        if (settings.HomePageId is { } previousHomeId)
        {
            var previous = await _catalog.StoreLandingPages
                .SingleOrDefaultAsync(x => x.PageId == previousHomeId, cancellationToken);
            if (previous is not null && previous.PageType == StorePageType.Home)
            {
                previous.SetPageType(StorePageType.Landing, now);
                Invalidate(previous.Locale, previous.Slug);
            }
        }

            if (pageId is null)
            {
                // بازگردانی خانهٔ پیش‌فرض: home_page_id=null → StorefrontShopeivaHome canonical در FE.
                // Catalog/Template و Store Pages دیگر لمس نمی‌شوند.
                settings.SetHomePage(null, now);
            }
        else
        {
            var page = await RequirePageAsync(pageId.Value, cancellationToken);
            if (!page.IsEligibleHome)
            {
                throw new PlatformHttpException(400, "فقط صفحهٔ فرود منتشرشده را می‌توان خانه کرد.", "landing.home.ineligible");
            }

            page.SetPageType(StorePageType.Home, now);
            settings.SetHomePage(page.PageId, now);
        }

        await _catalog.SaveChangesAsync(cancellationToken);
        if (tx is not null)
        {
            await tx.CommitAsync(cancellationToken);
        }

        // پس از Persist، کش عمومی صفحه/خانه را خالی کن تا PageType جدید دیده شود.
        if (settings.HomePageId is { } activeHomeId)
        {
            var active = await _catalog.StoreLandingPages.AsNoTracking()
                .SingleOrDefaultAsync(x => x.PageId == activeHomeId, cancellationToken);
            if (active is not null)
            {
                Invalidate(active.Locale, active.Slug);
            }
        }
        else if (pageId is null)
        {
            InvalidateHome();
        }

        // demoted previous already invalidated above when type flipped
        InvalidateHome();
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
        var existing = await LoadSectionsAsync(pageId, cancellationToken);
        if (existing.Count >= StoreLandingPageSectionRegistry.MaxSectionsPerPage)
        {
            throw new PlatformHttpException(400, "تعداد بخش‌های صفحه به سقف رسیده است.", "landing.section.limit");
        }

        var now = DateTimeOffset.UtcNow;
        var insertAt = body.InsertAt;
        int targetOrder;
        if (insertAt is null)
        {
            targetOrder = existing.Count == 0 ? 0 : existing.Max(x => x.SortOrder) + 1;
        }
        else
        {
            if (insertAt < 0 || insertAt > existing.Count)
            {
                throw new PlatformHttpException(400, "موقعیت درج بخش نامعتبر است.", "landing.section.insert.invalid");
            }

            targetOrder = insertAt.Value;
            for (var i = existing.Count - 1; i >= targetOrder; i--)
            {
                existing[i].SetSortOrder(i + 1, now);
            }
        }

        var section = StoreLandingPageSection.Create(pageId, body.SectionType, body.ConfigJson ?? body.Config, targetOrder, now);
        await EnsureReferencedEntitiesAsync(section, cancellationToken);
        _catalog.StoreLandingPageSections.Add(section);
        await _catalog.SaveChangesAsync(cancellationToken);
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
        var payloads = sections ?? [];
        if (payloads.Count > StoreLandingPageSectionRegistry.MaxSectionsPerPage)
        {
            throw new PlatformHttpException(400, "تعداد بخش‌های صفحه به سقف رسیده است.", "landing.section.limit");
        }

        var existing = await LoadSectionsAsync(pageId, cancellationToken);
        if (existing.Count > 0)
        {
            _catalog.StoreLandingPageSections.RemoveRange(existing);
            await _catalog.SaveChangesAsync(cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        var created = new List<StoreLandingPageSection>(payloads.Count);
        for (var i = 0; i < payloads.Count; i++)
        {
            var body = payloads[i];
            var section = StoreLandingPageSection.Create(pageId, body.SectionType, body.ConfigJson ?? body.Config, i, now);
            if (body.IsEnabled is { } enabled)
            {
                section.SetEnabled(enabled, now);
            }

            await EnsureReferencedEntitiesAsync(section, cancellationToken);
            _catalog.StoreLandingPageSections.Add(section);
            created.Add(section);
        }

        await _catalog.SaveChangesAsync(cancellationToken);
        Invalidate(page.Locale, page.Slug);
        return created.OrderBy(x => x.SortOrder).Select(ToAdminSection).ToList();
    }

    /// <summary>پیکربندی بخش را به‌روز می‌کند.</summary>
    public async Task<StoreLandingPageSectionAdminView> UpdateSectionAsync(Guid pageId, Guid sectionId, StoreLandingPageSectionWriteRequest body, CancellationToken cancellationToken)
    {
        var page = await RequirePageAsync(pageId, cancellationToken);
        var section = await RequireSectionAsync(pageId, sectionId, cancellationToken);
        section.UpdateConfig(body.ConfigJson ?? body.Config, DateTimeOffset.UtcNow);
        if (body.IsEnabled is { } enabled)
        {
            section.SetEnabled(enabled, DateTimeOffset.UtcNow);
        }

        await EnsureReferencedEntitiesAsync(section, cancellationToken);
        await _catalog.SaveChangesAsync(cancellationToken);
        Invalidate(page.Locale, page.Slug);
        return ToAdminSection(section);
    }

    /// <summary>فعال یا غیرفعال کردن بخش.</summary>
    public async Task<StoreLandingPageSectionAdminView> SetSectionEnabledAsync(Guid pageId, Guid sectionId, bool enabled, CancellationToken cancellationToken)
    {
        var page = await RequirePageAsync(pageId, cancellationToken);
        var section = await RequireSectionAsync(pageId, sectionId, cancellationToken);
        section.SetEnabled(enabled, DateTimeOffset.UtcNow);
        await _catalog.SaveChangesAsync(cancellationToken);
        Invalidate(page.Locale, page.Slug);
        return ToAdminSection(section);
    }

    /// <summary>ترتیب پایدار بخش‌ها را بدون حذف/ایجاد دوباره می‌نویسد.</summary>
    public async Task<IReadOnlyList<StoreLandingPageSectionAdminView>> ReorderSectionsAsync(Guid pageId, IReadOnlyList<Guid>? sectionIds, CancellationToken cancellationToken)
    {
        var page = await RequirePageAsync(pageId, cancellationToken);
        var rows = await LoadSectionsAsync(pageId, cancellationToken);
        var ids = sectionIds ?? [];
        if (ids.Count != rows.Count || ids.Distinct().Count() != ids.Count || ids.Any(id => rows.All(x => x.PageSectionId != id)))
        {
            throw new PlatformHttpException(400, "ترتیب بخش‌ها کامل نیست.", "landing.section.reorder.invalid");
        }

        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < ids.Count; i++)
        {
            rows.Single(x => x.PageSectionId == ids[i]).SetSortOrder(i, now);
        }

        await _catalog.SaveChangesAsync(cancellationToken);
        Invalidate(page.Locale, page.Slug);
        return rows.OrderBy(x => x.SortOrder).Select(ToAdminSection).ToList();
    }

    /// <summary>صفحه و بخش‌هایش را حذف می‌کند؛ اگر خانه بود ارجاع را پاک می‌کند.</summary>
    public async Task DeletePageAsync(Guid pageId, CancellationToken cancellationToken)
    {
        var page = await RequirePageAsync(pageId, cancellationToken);
        await ClearHomeIfMatchesAsync(page.PageId, cancellationToken);
        var sections = await _catalog.StoreLandingPageSections
            .Where(x => x.PageId == pageId)
            .ToListAsync(cancellationToken);
        if (sections.Count > 0)
        {
            _catalog.StoreLandingPageSections.RemoveRange(sections);
        }

        _catalog.StoreLandingPages.Remove(page);
        await _catalog.SaveChangesAsync(cancellationToken);
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
        var section = await RequireSectionAsync(pageId, sectionId, cancellationToken);
        _catalog.StoreLandingPageSections.Remove(section);
        await _catalog.SaveChangesAsync(cancellationToken);
        var remaining = await LoadSectionsAsync(pageId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < remaining.Count; i++)
        {
            remaining[i].SetSortOrder(i, now);
        }

        await _catalog.SaveChangesAsync(cancellationToken);
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

    private async Task EnsureUniqueSlugAsync(string locale, string slug, Guid? exceptPageId, CancellationToken cancellationToken)
    {
        var exists = await _catalog.StoreLandingPages
            .AnyAsync(x => x.Locale == locale && x.Slug == slug && x.PageId != exceptPageId, cancellationToken);
        if (exists)
        {
            throw new PlatformHttpException(409, "این آدرس در همین زبان قبلاً ثبت شده است.", "landing.slug.duplicate");
        }
    }

    private async Task<StoreLandingPageSection> RequireSectionAsync(Guid pageId, Guid sectionId, CancellationToken cancellationToken)
    {
        var section = await _catalog.StoreLandingPageSections
            .SingleOrDefaultAsync(x => x.PageId == pageId && x.PageSectionId == sectionId, cancellationToken);
        if (section is null)
        {
            throw new PlatformHttpException(404, "بخش یافت نشد.", "landing.section.missing");
        }

        return section;
    }

    private async Task<List<StoreLandingPageSection>> LoadSectionsAsync(Guid pageId, CancellationToken cancellationToken) =>
        await _catalog.StoreLandingPageSections
            .Where(x => x.PageId == pageId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.PageSectionId)
            .ToListAsync(cancellationToken);

    private async Task EnsureReferencedEntitiesAsync(StoreLandingPageSection section, CancellationToken cancellationToken)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(section.ConfigurationJson);
        var root = doc.RootElement;
        if (section.SectionType == StoreLandingPageSectionRegistry.ProductCollection)
        {
            var source = root.TryGetProperty("source", out var sourceEl) ? sourceEl.GetString() : null;
            if (string.Equals(source, "Category", StringComparison.Ordinal) && root.TryGetProperty("categoryId", out var categoryEl))
            {
                await EnsureCategoryAsync(categoryEl.GetGuid(), cancellationToken);
            }
            else if (string.Equals(source, "Brand", StringComparison.Ordinal) && root.TryGetProperty("brandId", out var brandEl))
            {
                await EnsureBrandAsync(brandEl.GetGuid(), cancellationToken);
            }
            else if (string.Equals(source, "Manual", StringComparison.Ordinal) && root.TryGetProperty("productIds", out var idsEl))
            {
                var ids = idsEl.EnumerateArray().Select(x => x.GetGuid()).ToList();
                var found = await _catalog.Products.CountAsync(x => ids.Contains(x.ProductId), cancellationToken);
                if (found != ids.Count)
                {
                    throw new PlatformHttpException(400, "محصول انتخاب‌شده در این فروشگاه نیست.", "landing.section.ref.missing");
                }
            }

            return;
        }

        if ((section.SectionType == StoreLandingPageSectionRegistry.CategoryGrid
                || section.SectionType == StoreLandingPageSectionRegistry.BrandStrip)
            && root.TryGetProperty("ids", out var listEl)
            && listEl.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            var ids = listEl.EnumerateArray().Select(x => x.GetGuid()).ToList();
            if (section.SectionType == StoreLandingPageSectionRegistry.CategoryGrid)
            {
                foreach (var id in ids)
                {
                    await EnsureCategoryAsync(id, cancellationToken);
                }
            }
            else
            {
                foreach (var id in ids)
                {
                    await EnsureBrandAsync(id, cancellationToken);
                }
            }
        }

        if (section.SectionType == StoreLandingPageSectionRegistry.NavigationMenu
            && root.TryGetProperty("menuId", out var menuEl)
            && menuEl.ValueKind == System.Text.Json.JsonValueKind.String
            && Guid.TryParse(menuEl.GetString(), out var menuId))
        {
            if (!await _catalog.StoreMenus.AnyAsync(x => x.MenuId == menuId && x.IsEnabled, cancellationToken))
            {
                throw new PlatformHttpException(400, "منوی انتخاب‌شده در این فروشگاه فعال نیست.", "landing.section.ref.missing");
            }
        }
    }

    private async Task EnsureCategoryAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        if (!await _catalog.Categories.AnyAsync(x => x.CategoryId == categoryId, cancellationToken))
        {
            throw new PlatformHttpException(400, "رده در این فروشگاه نیست.", "landing.section.ref.missing");
        }
    }

    private async Task EnsureBrandAsync(Guid brandId, CancellationToken cancellationToken)
    {
        if (!await _catalog.Brands.AnyAsync(x => x.BrandId == brandId, cancellationToken))
        {
            throw new PlatformHttpException(400, "برند در این فروشگاه نیست.", "landing.section.ref.missing");
        }
    }

    private async Task<StoreLandingPage> RequirePageAsync(Guid pageId, CancellationToken cancellationToken)
    {
        var page = await _catalog.StoreLandingPages.SingleOrDefaultAsync(x => x.PageId == pageId, cancellationToken);
        if (page is null)
        {
            throw new PlatformHttpException(404, "صفحه یافت نشد.", "landing.page.missing");
        }

        return page;
    }

    private async Task<StoreAppearanceSettings> RequireSettingsAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var row = await _catalog.StoreAppearanceSettings
            .SingleOrDefaultAsync(x => x.SettingsId == StoreAppearanceSettings.SingletonId, cancellationToken);
        if (row is not null)
        {
            return row;
        }

        row = StoreAppearanceSettings.CreateDefault(now);
        _catalog.StoreAppearanceSettings.Add(row);
        return row;
    }

    private async Task ClearHomeIfMatchesAsync(Guid pageId, CancellationToken cancellationToken)
    {
        var settings = await _catalog.StoreAppearanceSettings
            .SingleOrDefaultAsync(x => x.SettingsId == StoreAppearanceSettings.SingletonId, cancellationToken);
        if (settings?.HomePageId == pageId)
        {
            var page = await _catalog.StoreLandingPages.SingleOrDefaultAsync(x => x.PageId == pageId, cancellationToken);
            if (page is not null && page.PageType == StorePageType.Home)
            {
                page.SetPageType(StorePageType.Landing, DateTimeOffset.UtcNow);
            }

            settings.SetHomePage(null, DateTimeOffset.UtcNow);
            InvalidateHome();
        }
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
                ? await ResolveProductItemsAsync(row, cancellationToken)
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
                products = (await _storefront.ComposeProductCardsAsync(productIds, cancellationToken)).Values.ToList();
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
        CancellationToken cancellationToken)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(section.ConfigurationJson);
        var root = doc.RootElement;
        var source = root.TryGetProperty("source", out var sourceEl) ? sourceEl.GetString() : "Newest";
        var take = root.TryGetProperty("take", out var takeEl) && takeEl.TryGetInt32(out var parsedTake)
            ? Math.Clamp(parsedTake, 1, StoreLandingPageSectionRegistry.MaxTake)
            : StoreLandingPageSectionRegistry.DefaultTake;

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
            .Select(x => new StoreLandingPageResolvedItem(x.ProductId, x.SlugSeam))
            .ToListAsync(cancellationToken);
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
public sealed record StoreLandingPageResolvedItem(Guid Id, string? Slug);

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
