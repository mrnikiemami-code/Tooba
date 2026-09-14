using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
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

    /// <summary>نویسنده صفحه را به Catalog همین Store وصل می‌کند.</summary>
    public StoreLandingPageComposer(
        CatalogDbContext catalog,
        ICurrentCommerceContext commerce,
        IMemoryCache cache)
    {
        _catalog = catalog;
        _commerce = commerce;
        _cache = cache;
    }

    /// <summary>فهرست صفحات همین Store.</summary>
    public async Task<IReadOnlyList<StoreLandingPageAdminView>> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await _catalog.StoreLandingPages.AsNoTracking()
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(cancellationToken);
        return rows.Select(ToAdmin).ToList();
    }

    /// <summary>صفحهٔ پیش‌نویس می‌سازد.</summary>
    public async Task<StoreLandingPageAdminView> CreateAsync(StoreLandingPageWriteRequest body, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var page = StoreLandingPage.Create(body.Locale, body.Slug, body.Title, body.SeoTitle, body.SeoDescription, now);
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
        page.Update(body.Slug, body.Title, body.SeoTitle, body.SeoDescription, DateTimeOffset.UtcNow);
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

    /// <summary>ارجاع خانه را روی همین Store می‌نویسد.</summary>
    public async Task<StoreHomeSelectionView> SetHomeAsync(Guid? pageId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var settings = await RequireSettingsAsync(now, cancellationToken);
        if (pageId is null)
        {
            settings.SetHomePage(null, now);
        }
        else
        {
            var page = await RequirePageAsync(pageId.Value, cancellationToken);
            if (!page.IsEligibleHome)
            {
                throw new PlatformHttpException(400, "فقط صفحهٔ منتشرشده را می‌توان خانه کرد.", "landing.home.ineligible");
            }

            settings.SetHomePage(page.PageId, now);
        }

        await _catalog.SaveChangesAsync(cancellationToken);
        InvalidateHome();
        return await GetHomeSelectionAsync(cancellationToken);
    }

    /// <summary>صفحهٔ منتشرشدهٔ عمومی را با Store+Locale+Slug حل می‌کند.</summary>
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

        var page = await _catalog.StoreLandingPages.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Locale == normalizedLocale && x.Slug == normalizedSlug, cancellationToken);
        if (page is null || page.Status != StoreLandingPageStatus.Published)
        {
            _cache.Set(cacheKey, (StoreLandingPagePublicView?)null, TimeSpan.FromSeconds(15));
            return null;
        }

        var view = ToPublic(page);
        _cache.Set(cacheKey, view, TimeSpan.FromMinutes(2));
        return view;
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
                selected = ToPublic(page);
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
        page.Locale,
        page.Slug,
        page.Title,
        page.SeoTitle,
        page.SeoDescription,
        page.TemplateKey,
        page.Status.ToString(),
        page.UpdatedAt);

    private static StoreLandingPagePublicView ToPublic(StoreLandingPage page) => new(
        page.PageId,
        page.Locale,
        page.Slug,
        page.Title,
        page.SeoTitle ?? page.Title,
        page.SeoDescription,
        page.TemplateKey);
}

/// <summary>درخواست نوشتن صفحه.</summary>
public sealed record StoreLandingPageWriteRequest(
    string? Title,
    string? Slug,
    string? Locale,
    string? SeoTitle,
    string? SeoDescription,
    string? Status);

/// <summary>درخواست انتخاب خانه.</summary>
public sealed record StoreHomeSelectionWriteRequest(Guid? HomePageId);

/// <summary>نمای Admin.</summary>
public sealed record StoreLandingPageAdminView(
    Guid PageId,
    string Locale,
    string Slug,
    string Title,
    string? SeoTitle,
    string? SeoDescription,
    string TemplateKey,
    string Status,
    DateTimeOffset UpdatedAt);

/// <summary>نمای عمومی Published.</summary>
public sealed record StoreLandingPagePublicView(
    Guid PageId,
    string Locale,
    string Slug,
    string Title,
    string SeoTitle,
    string? SeoDescription,
    string TemplateKey);

/// <summary>ارجاع خانه بدون جایگزینی UI خانه.</summary>
public sealed record StoreHomeSelectionView(
    string StoreScope,
    Guid? HomePageId,
    StoreLandingPagePublicView? SelectedPage,
    bool UsesCanonicalHome);
