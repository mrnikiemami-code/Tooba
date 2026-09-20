using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure;

/// <summary>
/// orchestration موقت نوشتن Landing روی Catalog DbContext تا جابه‌جایی BC.
/// تراکنش SetHome اینجا می‌ماند (همان معنای قبلی Host).
/// </summary>
public sealed class StoreLandingPageDirectory : IStoreLandingPageDirectory
{
    private readonly CatalogDbContext _catalog;
    private readonly IClock _clock;
    private readonly ICurrentCommerceContext _commerce;
    private readonly IStoreLandingExternalReferenceGate? _campaignGate;

    /// <summary>دایرکتوری را به schema catalog وصل می‌کند.</summary>
    public StoreLandingPageDirectory(
        CatalogDbContext catalog,
        IClock clock,
        ICurrentCommerceContext commerce,
        IStoreLandingExternalReferenceGate? campaignGate = null)
    {
        _catalog = catalog;
        _clock = clock;
        _commerce = commerce;
        _campaignGate = campaignGate;
    }

    /// <inheritdoc />
    public async Task<StoreLandingPage> CreateAsync(StoreLandingPageWriteModel body, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
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
        return page;
    }

    /// <inheritdoc />
    public async Task<StoreLandingPage> UpdateAsync(Guid pageId, StoreLandingPageWriteModel body, CancellationToken cancellationToken)
    {
        var page = await RequirePageAsync(pageId, cancellationToken);
        page.Update(
            body.Slug,
            body.Title,
            body.SeoTitle,
            body.SeoDescription,
            _clock.UtcNow,
            body.RobotsIndex,
            body.RobotsFollow,
            body.CanonicalUrl,
            body.OgTitle,
            body.OgDescription,
            body.OgImageUrl,
            body.PrimaryH1);
        await EnsureUniqueSlugAsync(page.Locale, page.Slug, page.PageId, cancellationToken);
        await _catalog.SaveChangesAsync(cancellationToken);
        return page;
    }

    /// <inheritdoc />
    public async Task<StoreLandingPage> SetStatusAsync(Guid pageId, string? status, CancellationToken cancellationToken)
    {
        var page = await RequirePageAsync(pageId, cancellationToken);
        var now = _clock.UtcNow;
        if (string.Equals(status, "Published", StringComparison.OrdinalIgnoreCase))
        {
            page.Publish(now);
        }
        else if (string.Equals(status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            page.Unpublish(now);
            await ClearHomeIfMatchesAsync(page.PageId, cancellationToken);
        }
        else
        {
            throw new PlatformHttpException(400, "وضعیت انتشار معتبر نیست.", "landing.status.invalid");
        }

        await _catalog.SaveChangesAsync(cancellationToken);
        return page;
    }

    /// <inheritdoc />
    public async Task<StoreHomeWriteResult> SetHomeAsync(Guid? pageId, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var useTx = _catalog.Database.IsRelational();
        await using var tx = useTx
            ? await _catalog.Database.BeginTransactionAsync(cancellationToken)
            : null;
        var settings = await RequireSettingsAsync(now, cancellationToken);
        string? invalidateLocale = null;
        string? invalidateSlug = null;

        if (settings.HomePageId is { } previousHomeId)
        {
            var previous = await _catalog.StoreLandingPages
                .SingleOrDefaultAsync(x => x.PageId == previousHomeId, cancellationToken);
            if (previous is not null && previous.PageType == StorePageType.Home)
            {
                previous.SetPageType(StorePageType.Landing, now);
                invalidateLocale = previous.Locale;
                invalidateSlug = previous.Slug;
            }
        }

        if (pageId is null)
        {
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
            invalidateLocale = page.Locale;
            invalidateSlug = page.Slug;
        }

        await _catalog.SaveChangesAsync(cancellationToken);
        if (tx is not null)
        {
            await tx.CommitAsync(cancellationToken);
        }

        return new StoreHomeWriteResult(settings.HomePageId, invalidateLocale, invalidateSlug, InvalidateHome: true);
    }

    /// <inheritdoc />
    public async Task<StoreLandingPageSection> AddSectionAsync(Guid pageId, StoreLandingPageSectionWriteModel body, CancellationToken cancellationToken)
    {
        await RequirePageAsync(pageId, cancellationToken);
        var existing = await LoadSectionsAsync(pageId, cancellationToken);
        if (existing.Count >= StoreLandingPageSectionRegistry.MaxSectionsPerPage)
        {
            throw new PlatformHttpException(400, "تعداد بخش‌های صفحه به سقف رسیده است.", "landing.section.limit");
        }

        var now = _clock.UtcNow;
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
        return section;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoreLandingPageSection>> ReplaceCompositionAsync(
        Guid pageId,
        IReadOnlyList<StoreLandingPageSectionWriteModel> payloads,
        CancellationToken cancellationToken)
    {
        await RequirePageAsync(pageId, cancellationToken);
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

        var now = _clock.UtcNow;
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
        return created.OrderBy(x => x.SortOrder).ToList();
    }

    /// <inheritdoc />
    public async Task<StoreLandingPageSection> UpdateSectionAsync(Guid pageId, Guid sectionId, StoreLandingPageSectionWriteModel body, CancellationToken cancellationToken)
    {
        await RequirePageAsync(pageId, cancellationToken);
        var section = await RequireSectionAsync(pageId, sectionId, cancellationToken);
        var now = _clock.UtcNow;
        section.UpdateConfig(body.ConfigJson ?? body.Config, now);
        if (body.IsEnabled is { } enabled)
        {
            section.SetEnabled(enabled, now);
        }

        await EnsureReferencedEntitiesAsync(section, cancellationToken);
        await _catalog.SaveChangesAsync(cancellationToken);
        return section;
    }

    /// <inheritdoc />
    public async Task<StoreLandingPageSection> SetSectionEnabledAsync(Guid pageId, Guid sectionId, bool enabled, CancellationToken cancellationToken)
    {
        await RequirePageAsync(pageId, cancellationToken);
        var section = await RequireSectionAsync(pageId, sectionId, cancellationToken);
        section.SetEnabled(enabled, _clock.UtcNow);
        await _catalog.SaveChangesAsync(cancellationToken);
        return section;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoreLandingPageSection>> ReorderSectionsAsync(Guid pageId, IReadOnlyList<Guid> ids, CancellationToken cancellationToken)
    {
        await RequirePageAsync(pageId, cancellationToken);
        var rows = await LoadSectionsAsync(pageId, cancellationToken);
        if (ids.Count != rows.Count || ids.Distinct().Count() != ids.Count || ids.Any(id => rows.All(x => x.PageSectionId != id)))
        {
            throw new PlatformHttpException(400, "ترتیب بخش‌ها کامل نیست.", "landing.section.reorder.invalid");
        }

        var now = _clock.UtcNow;
        for (var i = 0; i < ids.Count; i++)
        {
            rows.Single(x => x.PageSectionId == ids[i]).SetSortOrder(i, now);
        }

        await _catalog.SaveChangesAsync(cancellationToken);
        return rows.OrderBy(x => x.SortOrder).ToList();
    }

    /// <inheritdoc />
    public async Task<StoreLandingPage> DeletePageAsync(Guid pageId, CancellationToken cancellationToken)
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
        return page;
    }

    /// <inheritdoc />
    public async Task DeleteSectionAsync(Guid pageId, Guid sectionId, CancellationToken cancellationToken)
    {
        await RequirePageAsync(pageId, cancellationToken);
        var section = await RequireSectionAsync(pageId, sectionId, cancellationToken);
        _catalog.StoreLandingPageSections.Remove(section);
        await _catalog.SaveChangesAsync(cancellationToken);
        var remaining = await LoadSectionsAsync(pageId, cancellationToken);
        var now = _clock.UtcNow;
        for (var i = 0; i < remaining.Count; i++)
        {
            remaining[i].SetSortOrder(i, now);
        }

        await _catalog.SaveChangesAsync(cancellationToken);
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
            else if (string.Equals(source, "PromotionCampaign", StringComparison.OrdinalIgnoreCase)
                     && root.TryGetProperty("campaignId", out var campaignEl)
                     && campaignEl.ValueKind is not System.Text.Json.JsonValueKind.Null
                     && campaignEl.ValueKind is not System.Text.Json.JsonValueKind.Undefined
                     && Guid.TryParse(campaignEl.ToString(), out var campaignId)
                     && campaignId != Guid.Empty)
            {
                var storeId = ResolveMerchandisingStoreId();
                if (storeId is null || _campaignGate is null)
                {
                    throw new PlatformHttpException(400, "کمپین انتخاب‌شده در این فروشگاه نیست.", "landing.section.ref.missing");
                }

                var belongs = await _campaignGate.CampaignBelongsToStoreAsync(campaignId, storeId.Value, cancellationToken);
                if (!belongs)
                {
                    throw new PlatformHttpException(400, "کمپین انتخاب‌شده در این فروشگاه نیست.", "landing.section.ref.missing");
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
                page.SetPageType(StorePageType.Landing, _clock.UtcNow);
            }

            settings.SetHomePage(null, _clock.UtcNow);
        }
    }

    private Guid? ResolveMerchandisingStoreId()
    {
        var tenantId = _commerce.Current?.Tenant?.TenantId.Value;
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return null;
        }

        // هم‌تراز Host: store-alpha → GUID پایدار دانهٔ Dev (بدون ارجاع به Host seed).
        if (string.Equals(tenantId, "store-alpha", StringComparison.OrdinalIgnoreCase))
        {
            return Guid.Parse("aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaa1");
        }

        return Guid.TryParse(tenantId, out var parsed) ? parsed : null;
    }
}
