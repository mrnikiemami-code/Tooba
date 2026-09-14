using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Host.Storefront;

namespace Tooba.Host.Admin;

/// <summary>منوی فروشگاه: درخت کران‌دار، ارجاع هدر، و تصویر عمومی.</summary>
public sealed class StoreMenuComposer
{
    internal const string MenuCachePrefix = "store-menu:";
    internal const string HeaderCachePrefix = "store-header-menu:";

    private readonly CatalogDbContext _catalog;
    private readonly ICurrentCommerceContext _commerce;
    private readonly IMemoryCache _cache;

    /// <summary>Composer را با Catalog و کش تصویر می‌سازد.</summary>
    public StoreMenuComposer(CatalogDbContext catalog, ICurrentCommerceContext commerce, IMemoryCache cache)
    {
        _catalog = catalog;
        _commerce = commerce;
        _cache = cache;
    }

    /// <summary>فهرست منوها با شمار آیتم.</summary>
    public async Task<IReadOnlyList<StoreMenuListView>> ListAsync(CancellationToken cancellationToken)
    {
        var menus = await _catalog.StoreMenus.AsNoTracking()
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(cancellationToken);
        var counts = await _catalog.StoreMenuItems.AsNoTracking()
            .GroupBy(x => x.MenuId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
        return menus.Select(x => ToList(x, counts.GetValueOrDefault(x.MenuId))).ToList();
    }

    /// <summary>جزئیات منو و آیتم‌ها.</summary>
    public async Task<StoreMenuDetailView> GetAsync(Guid menuId, CancellationToken cancellationToken)
    {
        var menu = await RequireMenuAsync(menuId, cancellationToken);
        var items = await LoadItemsAsync(menuId, cancellationToken);
        return ToDetail(menu, items);
    }

    /// <summary>منوی جدید.</summary>
    public async Task<StoreMenuDetailView> CreateAsync(StoreMenuWriteRequest request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var menu = StoreMenu.Create(request.Title, request.Locale, request.MenuKey, now);
        if (await _catalog.StoreMenus.AnyAsync(x => x.Locale == menu.Locale && x.MenuKey == menu.MenuKey, cancellationToken))
        {
            throw new PlatformHttpException(409, "کلید منو تکراری است.", "menu.key.duplicate");
        }

        _catalog.StoreMenus.Add(menu);
        await _catalog.SaveChangesAsync(cancellationToken);
        Invalidate(menu.MenuId);
        return ToDetail(menu, []);
    }

    /// <summary>عنوان و زبان.</summary>
    public async Task<StoreMenuDetailView> UpdateAsync(Guid menuId, StoreMenuWriteRequest request, CancellationToken cancellationToken)
    {
        var menu = await RequireMenuAsync(menuId, cancellationToken);
        menu.Update(request.Title, request.Locale, DateTimeOffset.UtcNow);
        await _catalog.SaveChangesAsync(cancellationToken);
        Invalidate(menu.MenuId);
        return await GetAsync(menuId, cancellationToken);
    }

    /// <summary>فعال/غیرفعال منو.</summary>
    public async Task<StoreMenuDetailView> SetEnabledAsync(Guid menuId, bool enabled, CancellationToken cancellationToken)
    {
        var menu = await RequireMenuAsync(menuId, cancellationToken);
        menu.SetEnabled(enabled, DateTimeOffset.UtcNow);
        if (!enabled)
        {
            await ClearHeaderIfMatchAsync(menuId, cancellationToken);
        }

        await _catalog.SaveChangesAsync(cancellationToken);
        Invalidate(menu.MenuId);
        return await GetAsync(menuId, cancellationToken);
    }

    /// <summary>حذف وقتی ارجاعی نیست.</summary>
    public async Task DeleteAsync(Guid menuId, CancellationToken cancellationToken)
    {
        var usage = await FindUsageAsync(menuId, cancellationToken);
        if (usage.Count > 0)
        {
            throw new PlatformHttpException(409, "این منو در حال استفاده است.", "menu.delete.referenced");
        }

        var items = await _catalog.StoreMenuItems.Where(x => x.MenuId == menuId).ToListAsync(cancellationToken);
        _catalog.StoreMenuItems.RemoveRange(items);
        var menu = await RequireMenuAsync(menuId, cancellationToken);
        _catalog.StoreMenus.Remove(menu);
        await _catalog.SaveChangesAsync(cancellationToken);
        Invalidate(menuId);
    }

    /// <summary>ارجاع‌های حذف‌مسدود.</summary>
    public Task<IReadOnlyList<StoreMenuUsageView>> GetUsageAsync(Guid menuId, CancellationToken cancellationToken) =>
        FindUsageAsync(menuId, cancellationToken);

    /// <summary>افزودن آیتم با عمق و چرخهٔ امن.</summary>
    public async Task<StoreMenuItemAdminView> AddItemAsync(Guid menuId, StoreMenuItemWriteRequest request, CancellationToken cancellationToken)
    {
        var menu = await RequireMenuAsync(menuId, cancellationToken);
        var items = await LoadItemsAsync(menuId, cancellationToken);
        var parentId = request.ParentMenuItemId;
        EnsureParent(items, menuId, parentId);
        EnsureDepth(items, parentId, addedLevels: 1);
        var linkType = StoreMenuItem.ParseLinkType(request.LinkType);
        await EnsureTargetAsync(menu, linkType, request.TargetId, cancellationToken);
        var sort = request.SortOrder ?? NextSort(items, parentId);
        var item = StoreMenuItem.Create(
            menuId,
            parentId,
            request.Label,
            linkType,
            request.TargetId,
            request.ExternalUrl,
            sort,
            DateTimeOffset.UtcNow);
        _catalog.StoreMenuItems.Add(item);
        menu.Touch(item.UpdatedAt);
        await _catalog.SaveChangesAsync(cancellationToken);
        Invalidate(menuId);
        return ToItem(item);
    }

    /// <summary>ویرایش آیتم.</summary>
    public async Task<StoreMenuItemAdminView> UpdateItemAsync(Guid menuId, Guid menuItemId, StoreMenuItemWriteRequest request, CancellationToken cancellationToken)
    {
        var menu = await RequireMenuAsync(menuId, cancellationToken);
        var items = await LoadItemsAsync(menuId, cancellationToken);
        var item = items.SingleOrDefault(x => x.MenuItemId == menuItemId)
            ?? throw new PlatformHttpException(404, "آیتم منو یافت نشد.", "menu.item.missing");
        var parentId = request.ParentMenuItemId;
        if (parentId is { } nextParent)
        {
            EnsureParent(items, menuId, nextParent);
            EnsureNoCycle(items, menuItemId, nextParent);
            EnsureDepth(items, nextParent, addedLevels: SubtreeHeight(items, menuItemId));
        }

        var linkType = StoreMenuItem.ParseLinkType(request.LinkType);
        await EnsureTargetAsync(menu, linkType, request.TargetId, cancellationToken);
        item.Apply(parentId, request.Label, linkType, request.TargetId, request.ExternalUrl, request.SortOrder ?? item.SortOrder, request.IsEnabled ?? item.IsEnabled, DateTimeOffset.UtcNow);
        menu.Touch(item.UpdatedAt);
        await _catalog.SaveChangesAsync(cancellationToken);
        Invalidate(menuId);
        return ToItem(item);
    }

    /// <summary>فعال/غیرفعال آیتم.</summary>
    public async Task<StoreMenuItemAdminView> SetItemEnabledAsync(Guid menuId, Guid menuItemId, bool enabled, CancellationToken cancellationToken)
    {
        var menu = await RequireMenuAsync(menuId, cancellationToken);
        var item = await _catalog.StoreMenuItems.SingleOrDefaultAsync(x => x.MenuId == menuId && x.MenuItemId == menuItemId, cancellationToken)
            ?? throw new PlatformHttpException(404, "آیتم منو یافت نشد.", "menu.item.missing");
        item.SetEnabled(enabled, DateTimeOffset.UtcNow);
        menu.Touch(item.UpdatedAt);
        await _catalog.SaveChangesAsync(cancellationToken);
        Invalidate(menuId);
        return ToItem(item);
    }

    /// <summary>حذف آیتم و فرزندان.</summary>
    public async Task DeleteItemAsync(Guid menuId, Guid menuItemId, CancellationToken cancellationToken)
    {
        var menu = await RequireMenuAsync(menuId, cancellationToken);
        var items = await LoadItemsAsync(menuId, cancellationToken);
        if (items.All(x => x.MenuItemId != menuItemId))
        {
            throw new PlatformHttpException(404, "آیتم منو یافت نشد.", "menu.item.missing");
        }

        var remove = CollectSubtree(items, menuItemId);
        _catalog.StoreMenuItems.RemoveRange(remove);
        menu.Touch(DateTimeOffset.UtcNow);
        await _catalog.SaveChangesAsync(cancellationToken);
        Invalidate(menuId);
    }

    /// <summary>ترتیب پایدار با حفظ شناسه.</summary>
    public async Task<StoreMenuDetailView> ReorderItemsAsync(Guid menuId, IReadOnlyList<Guid>? orderedIds, CancellationToken cancellationToken)
    {
        var menu = await RequireMenuAsync(menuId, cancellationToken);
        var items = await LoadItemsAsync(menuId, cancellationToken);
        if (orderedIds is null || orderedIds.Count == 0 || orderedIds.Count != items.Count || orderedIds.Distinct().Count() != items.Count)
        {
            throw new PlatformHttpException(400, "ترتیب آیتم‌ها کامل نیست.", "menu.item.reorder.invalid");
        }

        var byId = items.ToDictionary(x => x.MenuItemId);
        foreach (var id in orderedIds)
        {
            if (!byId.ContainsKey(id))
            {
                throw new PlatformHttpException(400, "ترتیب آیتم‌ها کامل نیست.", "menu.item.reorder.invalid");
            }
        }

        var groups = orderedIds
            .Select(id => byId[id])
            .GroupBy(x => x.ParentMenuItemId);
        var now = DateTimeOffset.UtcNow;
        foreach (var group in groups)
        {
            var sort = 0;
            foreach (var item in group)
            {
                item.SetSortOrder(sort++, now);
            }
        }

        menu.Touch(now);
        await _catalog.SaveChangesAsync(cancellationToken);
        Invalidate(menuId);
        return await GetAsync(menuId, cancellationToken);
    }

    /// <summary>انتخاب هدر Admin.</summary>
    public async Task<StoreHeaderMenuSelectionView> GetHeaderSelectionAsync(CancellationToken cancellationToken)
    {
        var settings = await _catalog.StoreAppearanceSettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SettingsId == StoreAppearanceSettings.SingletonId, cancellationToken);
        return await ResolveHeaderAsync(settings?.HeaderMenuId, includeTree: false, cancellationToken);
    }

    /// <summary>نوشتن ارجاع هدر؛ null یعنی fallback.</summary>
    public async Task<StoreHeaderMenuSelectionView> SetHeaderAsync(Guid? headerMenuId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        if (headerMenuId is { } menuId)
        {
            var menu = await RequireMenuAsync(menuId, cancellationToken);
            if (!menu.IsEnabled)
            {
                throw new PlatformHttpException(400, "فقط منوی فعال قابل انتخاب است.", "menu.header.ineligible");
            }
        }

        var settings = await RequireSettingsAsync(now, cancellationToken);
        settings.SetHeaderMenu(headerMenuId, now);
        await _catalog.SaveChangesAsync(cancellationToken);
        InvalidateHeader();
        return await ResolveHeaderAsync(headerMenuId, includeTree: false, cancellationToken);
    }

    /// <summary>تصویر عمومی هدر یا fallback.</summary>
    public Task<StoreHeaderMenuSelectionView> GetHeaderPublicAsync(CancellationToken cancellationToken)
    {
        var key = HeaderCacheKey(Scope());
        return _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
            var settings = await _catalog.StoreAppearanceSettings.AsNoTracking()
                .SingleOrDefaultAsync(x => x.SettingsId == StoreAppearanceSettings.SingletonId, cancellationToken);
            return await ResolveHeaderAsync(settings?.HeaderMenuId, includeTree: true, cancellationToken);
        })!;
    }

    /// <summary>تصویر عمومی یک منوی فعال.</summary>
    public async Task<IReadOnlyList<StoreMenuPublicItemView>> ProjectPublicAsync(Guid menuId, CancellationToken cancellationToken)
    {
        var key = MenuCacheKey(Scope(), menuId);
        return (await _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
            return await ProjectUncachedAsync(menuId, cancellationToken);
        }))!;
    }

    private async Task<IReadOnlyList<StoreMenuPublicItemView>> ProjectUncachedAsync(Guid menuId, CancellationToken cancellationToken)
    {
        var menu = await _catalog.StoreMenus.AsNoTracking().SingleOrDefaultAsync(x => x.MenuId == menuId, cancellationToken);
        if (menu is null || !menu.IsEnabled)
        {
            return [];
        }

        var items = await _catalog.StoreMenuItems.AsNoTracking()
            .Where(x => x.MenuId == menuId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.MenuItemId)
            .ToListAsync(cancellationToken);
        var enabled = VisibleItems(items);
        var projected = new List<StoreMenuPublicItemView>(enabled.Count);
        foreach (var item in enabled)
        {
            var href = await ResolveHrefAsync(menu.Locale, item, cancellationToken);
            if (item.LinkType != StoreMenuLinkType.Group && href is null)
            {
                continue;
            }

            projected.Add(new StoreMenuPublicItemView(item.MenuItemId, item.ParentMenuItemId, item.Label, href, DepthOf(items, item.MenuItemId)));
        }

        return projected;
    }

    private async Task<StoreHeaderMenuSelectionView> ResolveHeaderAsync(Guid? headerMenuId, bool includeTree, CancellationToken cancellationToken)
    {
        if (headerMenuId is { } menuId)
        {
            var menu = await _catalog.StoreMenus.AsNoTracking().SingleOrDefaultAsync(x => x.MenuId == menuId, cancellationToken);
            if (menu is { IsEnabled: true })
            {
                var items = includeTree ? await ProjectPublicAsync(menuId, cancellationToken) : Array.Empty<StoreMenuPublicItemView>();
                return new StoreHeaderMenuSelectionView(Scope(), menuId, menu.Title, UsesFallback: false, items);
            }
        }

        return new StoreHeaderMenuSelectionView(Scope(), null, null, UsesFallback: true, []);
    }

    private async Task EnsureTargetAsync(StoreMenu menu, StoreMenuLinkType linkType, Guid? targetId, CancellationToken cancellationToken)
    {
        if (linkType is StoreMenuLinkType.Home or StoreMenuLinkType.Group or StoreMenuLinkType.External)
        {
            return;
        }

        if (targetId is not { } id)
        {
            throw new PlatformHttpException(400, "مقصد داخلی را از فهرست انتخاب کنید.", "menu.target.required");
        }

        var ok = linkType switch
        {
            StoreMenuLinkType.LandingPage => await _catalog.StoreLandingPages.AnyAsync(
                x => x.PageId == id && x.Status == StoreLandingPageStatus.Published && x.Locale == menu.Locale,
                cancellationToken),
            StoreMenuLinkType.Product => await _catalog.Products.AnyAsync(
                x => x.ProductId == id && x.Status == CatalogPublicationStatus.Published,
                cancellationToken),
            StoreMenuLinkType.Category => await _catalog.Categories.AnyAsync(x => x.CategoryId == id, cancellationToken),
            StoreMenuLinkType.Brand => await _catalog.Brands.AnyAsync(x => x.BrandId == id, cancellationToken),
            StoreMenuLinkType.Article => true,
            _ => false,
        };
        if (!ok)
        {
            throw new PlatformHttpException(400, "مقصد انتخاب‌شده در این فروشگاه نیست.", "menu.target.missing");
        }
    }

    private async Task<string?> ResolveHrefAsync(string locale, StoreMenuItem item, CancellationToken cancellationToken)
    {
        switch (item.LinkType)
        {
            case StoreMenuLinkType.Home:
                return "/";
            case StoreMenuLinkType.Group:
                return null;
            case StoreMenuLinkType.External:
                return item.ExternalUrl;
            case StoreMenuLinkType.LandingPage:
            {
                var page = await _catalog.StoreLandingPages.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.PageId == item.TargetId && x.Status == StoreLandingPageStatus.Published, cancellationToken);
                return page is null ? null : $"/{page.Slug}";
            }
            case StoreMenuLinkType.Product:
            {
                var product = await _catalog.Products.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.ProductId == item.TargetId && x.Status == CatalogPublicationStatus.Published, cancellationToken);
                return string.IsNullOrWhiteSpace(product?.SlugSeam) ? null : $"/products/{product.SlugSeam}";
            }
            case StoreMenuLinkType.Category:
            {
                var ui = locale == "en" ? "en" : "fa";
                var slug = await _catalog.CategoryTranslations.AsNoTracking()
                    .Where(x => x.CategoryId == item.TargetId && (x.Locale == locale || x.Locale == "fa-IR" || x.Locale == "en-US"))
                    .Select(x => x.Slug)
                    .FirstOrDefaultAsync(cancellationToken);
                return string.IsNullOrWhiteSpace(slug)
                    ? $"/products?categoryId={item.TargetId}"
                    : $"/{ui}/category/{slug}";
            }
            case StoreMenuLinkType.Brand:
            {
                var brand = await _catalog.Brands.AsNoTracking().SingleOrDefaultAsync(x => x.BrandId == item.TargetId, cancellationToken);
                return string.IsNullOrWhiteSpace(brand?.SlugSeam) ? "/brands" : $"/brand/{brand.SlugSeam}";
            }
            case StoreMenuLinkType.Article:
                return item.TargetId is { } articleId ? $"/blogs/{articleId:N}" : "/blogs";
            default:
                return null;
        }
    }

    private static IReadOnlyList<StoreMenuItem> VisibleItems(IReadOnlyList<StoreMenuItem> items)
    {
        var byId = items.ToDictionary(x => x.MenuItemId);
        bool Visible(StoreMenuItem item)
        {
            if (!item.IsEnabled)
            {
                return false;
            }

            var current = item;
            while (current.ParentMenuItemId is { } parentId)
            {
                if (!byId.TryGetValue(parentId, out current!) || !current.IsEnabled)
                {
                    return false;
                }
            }

            return true;
        }

        return items.Where(Visible).ToList();
    }

    private static void EnsureParent(IReadOnlyList<StoreMenuItem> items, Guid menuId, Guid? parentId)
    {
        if (parentId is null)
        {
            return;
        }

        var parent = items.SingleOrDefault(x => x.MenuItemId == parentId);
        if (parent is null || parent.MenuId != menuId)
        {
            throw new PlatformHttpException(400, "والد باید در همین منو باشد.", "menu.item.parent.invalid");
        }
    }

    private static void EnsureNoCycle(IReadOnlyList<StoreMenuItem> items, Guid itemId, Guid parentId)
    {
        var byId = items.ToDictionary(x => x.MenuItemId);
        var current = parentId;
        var guard = 0;
        while (true)
        {
            if (current == itemId)
            {
                throw new PlatformHttpException(400, "چرخه در درخت منو مجاز نیست.", "menu.item.cycle");
            }

            if (!byId.TryGetValue(current, out var row) || row.ParentMenuItemId is not { } next)
            {
                return;
            }

            current = next;
            if (++guard > StoreMenuItem.MaxDepth + 2)
            {
                throw new PlatformHttpException(400, "چرخه در درخت منو مجاز نیست.", "menu.item.cycle");
            }
        }
    }

    private static void EnsureDepth(IReadOnlyList<StoreMenuItem> items, Guid? parentId, int addedLevels)
    {
        var parentDepth = parentId is null ? 0 : DepthOf(items, parentId.Value);
        if (parentDepth + addedLevels > StoreMenuItem.MaxDepth)
        {
            throw new PlatformHttpException(400, "حداکثر سه سطح تو در تو مجاز است.", "menu.item.depth");
        }
    }

    private static int DepthOf(IReadOnlyList<StoreMenuItem> items, Guid itemId)
    {
        var byId = items.ToDictionary(x => x.MenuItemId);
        var depth = 0;
        var current = itemId;
        while (byId.TryGetValue(current, out var row))
        {
            depth++;
            if (row.ParentMenuItemId is not { } parent)
            {
                break;
            }

            current = parent;
            if (depth > StoreMenuItem.MaxDepth + 2)
            {
                break;
            }
        }

        return depth;
    }

    private static int SubtreeHeight(IReadOnlyList<StoreMenuItem> items, Guid rootId)
    {
        int Height(Guid id)
        {
            var children = items.Where(x => x.ParentMenuItemId == id).Select(x => Height(x.MenuItemId)).DefaultIfEmpty(0).Max();
            return 1 + children;
        }

        return Height(rootId);
    }

    private static List<StoreMenuItem> CollectSubtree(IReadOnlyList<StoreMenuItem> items, Guid rootId)
    {
        var result = new List<StoreMenuItem>();
        void Walk(Guid id)
        {
            foreach (var child in items.Where(x => x.ParentMenuItemId == id))
            {
                Walk(child.MenuItemId);
            }

            var row = items.Single(x => x.MenuItemId == id);
            result.Add(row);
        }

        Walk(rootId);
        return result;
    }

    private static int NextSort(IReadOnlyList<StoreMenuItem> items, Guid? parentId) =>
        items.Where(x => x.ParentMenuItemId == parentId).Select(x => x.SortOrder).DefaultIfEmpty(-1).Max() + 1;

    private async Task<IReadOnlyList<StoreMenuUsageView>> FindUsageAsync(Guid menuId, CancellationToken cancellationToken)
    {
        var usage = new List<StoreMenuUsageView>();
        var settings = await _catalog.StoreAppearanceSettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SettingsId == StoreAppearanceSettings.SingletonId, cancellationToken);
        if (settings?.HeaderMenuId == menuId)
        {
            usage.Add(new StoreMenuUsageView("header", "منوی نوار بالای فروشگاه"));
        }

        var sections = await _catalog.StoreLandingPageSections.AsNoTracking()
            .Where(x => x.SectionType == StoreLandingPageSectionRegistry.NavigationMenu)
            .ToListAsync(cancellationToken);
        foreach (var section in sections)
        {
            using var doc = System.Text.Json.JsonDocument.Parse(section.ConfigurationJson);
            if (doc.RootElement.TryGetProperty("menuId", out var el)
                && el.ValueKind == System.Text.Json.JsonValueKind.String
                && Guid.TryParse(el.GetString(), out var referenced)
                && referenced == menuId)
            {
                usage.Add(new StoreMenuUsageView("landing", "بخش فهرست پیوند در صفحهٔ فرود"));
            }
        }

        return usage;
    }

    private async Task ClearHeaderIfMatchAsync(Guid menuId, CancellationToken cancellationToken)
    {
        var settings = await _catalog.StoreAppearanceSettings
            .SingleOrDefaultAsync(x => x.SettingsId == StoreAppearanceSettings.SingletonId, cancellationToken);
        if (settings?.HeaderMenuId == menuId)
        {
            settings.SetHeaderMenu(null, DateTimeOffset.UtcNow);
        }
    }

    private async Task<StoreMenu> RequireMenuAsync(Guid menuId, CancellationToken cancellationToken)
    {
        return await _catalog.StoreMenus.SingleOrDefaultAsync(x => x.MenuId == menuId, cancellationToken)
            ?? throw new PlatformHttpException(404, "منو یافت نشد.", "menu.missing");
    }

    private Task<List<StoreMenuItem>> LoadItemsAsync(Guid menuId, CancellationToken cancellationToken) =>
        _catalog.StoreMenuItems
            .Where(x => x.MenuId == menuId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.MenuItemId)
            .ToListAsync(cancellationToken);

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

    private void Invalidate(Guid menuId)
    {
        _cache.Remove(MenuCacheKey(Scope(), menuId));
        InvalidateHeader();
    }

    private void InvalidateHeader() => _cache.Remove(HeaderCacheKey(Scope()));

    private string Scope() => StoreAppearanceProjector.ScopeKey(_commerce.Current);

    private static string MenuCacheKey(string scope, Guid menuId) => $"{MenuCachePrefix}{scope}:{menuId:N}";

    private static string HeaderCacheKey(string scope) => $"{HeaderCachePrefix}{scope}";

    private static StoreMenuListView ToList(StoreMenu menu, int itemCount) =>
        new(menu.MenuId, menu.Title, menu.Locale, menu.IsEnabled, itemCount, menu.UpdatedAt);

    private static StoreMenuDetailView ToDetail(StoreMenu menu, IReadOnlyList<StoreMenuItem> items) =>
        new(menu.MenuId, menu.Title, menu.Locale, menu.IsEnabled, menu.UpdatedAt, items.Select(ToItem).ToList());

    private static StoreMenuItemAdminView ToItem(StoreMenuItem item) =>
        new(item.MenuItemId, item.MenuId, item.ParentMenuItemId, item.Label, item.LinkType.ToString(), item.TargetId, item.ExternalUrl, item.SortOrder, item.IsEnabled, item.UpdatedAt);
}

/// <summary>نوشتن منو.</summary>
public sealed record StoreMenuWriteRequest(string? Title, string? Locale, string? MenuKey, bool? IsEnabled);

/// <summary>نوشتن آیتم.</summary>
public sealed record StoreMenuItemWriteRequest(
    string? Label,
    string? LinkType,
    Guid? ParentMenuItemId,
    Guid? TargetId,
    string? ExternalUrl,
    int? SortOrder,
    bool? IsEnabled);

/// <summary>ترتیب آیتم‌ها.</summary>
public sealed record StoreMenuItemReorderRequest(IReadOnlyList<Guid>? ItemIds);

/// <summary>فعال‌سازی.</summary>
public sealed record StoreMenuEnabledRequest(bool IsEnabled);

/// <summary>انتخاب هدر.</summary>
public sealed record StoreHeaderMenuWriteRequest(Guid? HeaderMenuId);

/// <summary>فهرست Admin.</summary>
public sealed record StoreMenuListView(Guid MenuId, string Title, string Locale, bool IsEnabled, int ItemCount, DateTimeOffset UpdatedAt);

/// <summary>جزئیات Admin.</summary>
public sealed record StoreMenuDetailView(Guid MenuId, string Title, string Locale, bool IsEnabled, DateTimeOffset UpdatedAt, IReadOnlyList<StoreMenuItemAdminView> Items);

/// <summary>آیتم Admin.</summary>
public sealed record StoreMenuItemAdminView(
    Guid MenuItemId,
    Guid MenuId,
    Guid? ParentMenuItemId,
    string Label,
    string LinkType,
    Guid? TargetId,
    string? ExternalUrl,
    int SortOrder,
    bool IsEnabled,
    DateTimeOffset UpdatedAt);

/// <summary>آیتم عمومی حل‌شده.</summary>
public sealed record StoreMenuPublicItemView(Guid MenuItemId, Guid? ParentMenuItemId, string Label, string? Href, int Depth);

/// <summary>انتخاب هدر.</summary>
public sealed record StoreHeaderMenuSelectionView(
    string StoreScope,
    Guid? HeaderMenuId,
    string? Title,
    bool UsesFallback,
    IReadOnlyList<StoreMenuPublicItemView> Items);

/// <summary>محل استفاده برای حذف امن.</summary>
public sealed record StoreMenuUsageView(string Kind, string Label);
