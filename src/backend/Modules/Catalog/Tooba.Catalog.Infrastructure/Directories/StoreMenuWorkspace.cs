using Microsoft.EntityFrameworkCore;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.StoreMenus.Models;
using Tooba.Catalog.Application.StoreMenus.Ports;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Directories;

/// <summary>منوی فروشگاه: درخت کران‌دار، ارجاع هدر، و تصویر عمومی (مالک Catalog).</summary>
public sealed class StoreMenuWorkspace : IStoreMenuWorkspace
{
    internal const string MenuCachePrefix = "store-menu:";
    internal const string HeaderCachePrefix = "store-header-menu:";

    private readonly CatalogDbContext _catalog;
    private readonly ICurrentCommerceContext _commerce;
    private readonly IMemoryCache _cache;
    private readonly ISender _sender;

    /// <summary>خواندن از Catalog؛ نوشتن از طریق ISender → Command.</summary>
    public StoreMenuWorkspace(
        CatalogDbContext catalog,
        ICurrentCommerceContext commerce,
        IMemoryCache cache,
        ISender sender)
    {
        _catalog = catalog;
        _commerce = commerce;
        _cache = cache;
        _sender = sender;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<StoreMenuDetailView> GetAsync(Guid menuId, CancellationToken cancellationToken)
    {
        var menu = await RequireMenuAsync(menuId, cancellationToken);
        var items = await LoadItemsAsync(menuId, cancellationToken);
        return ToDetail(menu, items);
    }

    /// <inheritdoc />
    public async Task<StoreMenuDetailView> CreateAsync(StoreMenuWriteRequest request, CancellationToken cancellationToken)
    {
        var menu = await _sender.Send(new CreateStoreMenuCommand(ToWriteModel(request)), cancellationToken);
        Invalidate(menu.MenuId);
        return ToDetail(menu, []);
    }

    /// <inheritdoc />
    public async Task<StoreMenuDetailView> UpdateAsync(
        Guid menuId,
        StoreMenuWriteRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new UpdateStoreMenuCommand(menuId, ToWriteModel(request)), cancellationToken);
        Invalidate(menuId);
        return await GetAsync(menuId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<StoreMenuDetailView> SetEnabledAsync(Guid menuId, bool enabled, CancellationToken cancellationToken)
    {
        await _sender.Send(new SetStoreMenuEnabledCommand(menuId, enabled), cancellationToken);
        Invalidate(menuId);
        return await GetAsync(menuId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid menuId, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteStoreMenuCommand(menuId), cancellationToken);
        Invalidate(menuId);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<StoreMenuUsageView>> GetUsageAsync(Guid menuId, CancellationToken cancellationToken) =>
        FindUsageAsync(menuId, cancellationToken);

    /// <inheritdoc />
    public async Task<StoreMenuItemAdminView> AddItemAsync(
        Guid menuId,
        StoreMenuItemWriteRequest request,
        CancellationToken cancellationToken)
    {
        var item = await _sender.Send(new AddStoreMenuItemCommand(menuId, ToItemModel(request)), cancellationToken);
        Invalidate(menuId);
        return ToItem(item);
    }

    /// <inheritdoc />
    public async Task<StoreMenuItemAdminView> UpdateItemAsync(
        Guid menuId,
        Guid menuItemId,
        StoreMenuItemWriteRequest request,
        CancellationToken cancellationToken)
    {
        var item = await _sender.Send(
            new UpdateStoreMenuItemCommand(menuId, menuItemId, ToItemModel(request)),
            cancellationToken);
        Invalidate(menuId);
        return ToItem(item);
    }

    /// <inheritdoc />
    public async Task<StoreMenuItemAdminView> SetItemEnabledAsync(
        Guid menuId,
        Guid menuItemId,
        bool enabled,
        CancellationToken cancellationToken)
    {
        var item = await _sender.Send(
            new SetStoreMenuItemEnabledCommand(menuId, menuItemId, enabled),
            cancellationToken);
        Invalidate(menuId);
        return ToItem(item);
    }

    /// <inheritdoc />
    public async Task DeleteItemAsync(Guid menuId, Guid menuItemId, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteStoreMenuItemCommand(menuId, menuItemId), cancellationToken);
        Invalidate(menuId);
    }

    /// <inheritdoc />
    public async Task<StoreMenuDetailView> ReorderItemsAsync(
        Guid menuId,
        IReadOnlyList<Guid>? orderedIds,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new ReorderStoreMenuItemsCommand(menuId, orderedIds ?? []), cancellationToken);
        Invalidate(menuId);
        return await GetAsync(menuId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<StoreHeaderMenuSelectionView> GetHeaderSelectionAsync(CancellationToken cancellationToken)
    {
        var settings = await _catalog.StoreAppearanceSettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SettingsId == StoreAppearanceSettings.SingletonId, cancellationToken);
        return await ResolveHeaderAsync(settings?.HeaderMenuId, includeTree: false, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<StoreHeaderMenuSelectionView> SetHeaderAsync(Guid? headerMenuId, CancellationToken cancellationToken)
    {
        await _sender.Send(new SetStoreHeaderMenuCommand(headerMenuId), cancellationToken);
        InvalidateHeader();
        return await ResolveHeaderAsync(headerMenuId, includeTree: false, cancellationToken);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoreMenuPublicItemView>> ProjectPublicAsync(
        Guid menuId,
        CancellationToken cancellationToken)
    {
        var key = MenuCacheKey(Scope(), menuId);
        return (await _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
            return await ProjectUncachedAsync(menuId, cancellationToken);
        }))!;
    }

    private async Task<IReadOnlyList<StoreMenuPublicItemView>> ProjectUncachedAsync(
        Guid menuId,
        CancellationToken cancellationToken)
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

            projected.Add(new StoreMenuPublicItemView(
                item.MenuItemId,
                item.ParentMenuItemId,
                item.Label,
                href,
                DepthOf(items, item.MenuItemId)));
        }

        return projected;
    }

    private async Task<StoreHeaderMenuSelectionView> ResolveHeaderAsync(
        Guid? headerMenuId,
        bool includeTree,
        CancellationToken cancellationToken)
    {
        if (headerMenuId is { } menuId)
        {
            var menu = await _catalog.StoreMenus.AsNoTracking().SingleOrDefaultAsync(x => x.MenuId == menuId, cancellationToken);
            if (menu is { IsEnabled: true })
            {
                var items = includeTree
                    ? await ProjectPublicAsync(menuId, cancellationToken)
                    : Array.Empty<StoreMenuPublicItemView>();
                return new StoreHeaderMenuSelectionView(Scope(), menuId, menu.Title, UsesFallback: false, items);
            }
        }

        return new StoreHeaderMenuSelectionView(Scope(), null, null, UsesFallback: true, []);
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
                    .SingleOrDefaultAsync(
                        x => x.PageId == item.TargetId && x.Status == StoreLandingPageStatus.Published,
                        cancellationToken);
                return page is null ? null : $"/{page.Slug}";
            }
            case StoreMenuLinkType.Product:
            {
                var product = await _catalog.Products.AsNoTracking()
                    .SingleOrDefaultAsync(
                        x => x.ProductId == item.TargetId && x.Status == CatalogPublicationStatus.Published,
                        cancellationToken);
                return string.IsNullOrWhiteSpace(product?.SlugSeam) ? null : $"/products/{product.SlugSeam}";
            }
            case StoreMenuLinkType.Category:
            {
                var ui = locale == "en" ? "en" : "fa";
                var slug = await _catalog.CategoryTranslations.AsNoTracking()
                    .Where(x => x.CategoryId == item.TargetId
                        && (x.Locale == locale || x.Locale == "fa-IR" || x.Locale == "en-US"))
                    .Select(x => x.Slug)
                    .FirstOrDefaultAsync(cancellationToken);
                return string.IsNullOrWhiteSpace(slug)
                    ? $"/products?categoryId={item.TargetId}"
                    : $"/{ui}/category/{slug}";
            }
            case StoreMenuLinkType.Brand:
            {
                var brand = await _catalog.Brands.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.BrandId == item.TargetId, cancellationToken);
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

    private void Invalidate(Guid menuId)
    {
        _cache.Remove(MenuCacheKey(Scope(), menuId));
        InvalidateHeader();
    }

    private void InvalidateHeader() => _cache.Remove(HeaderCacheKey(Scope()));

    private string Scope() => CatalogStoreScope.ScopeKey(_commerce.Current);

    private static string MenuCacheKey(string scope, Guid menuId) => $"{MenuCachePrefix}{scope}:{menuId:N}";

    private static string HeaderCacheKey(string scope) => $"{HeaderCachePrefix}{scope}";

    private static StoreMenuWriteModel ToWriteModel(StoreMenuWriteRequest request) =>
        new(request.Title, request.Locale, request.MenuKey, request.IsEnabled);

    private static StoreMenuItemWriteModel ToItemModel(StoreMenuItemWriteRequest request) => new(
        request.Label,
        request.LinkType,
        request.ParentMenuItemId,
        request.TargetId,
        request.ExternalUrl,
        request.SortOrder,
        request.IsEnabled);

    private static StoreMenuListView ToList(StoreMenu menu, int itemCount) =>
        new(menu.MenuId, menu.Title, menu.Locale, menu.IsEnabled, itemCount, menu.UpdatedAt);

    private static StoreMenuDetailView ToDetail(StoreMenu menu, IReadOnlyList<StoreMenuItem> items) =>
        new(menu.MenuId, menu.Title, menu.Locale, menu.IsEnabled, menu.UpdatedAt, items.Select(ToItem).ToList());

    private static StoreMenuItemAdminView ToItem(StoreMenuItem item) =>
        new(
            item.MenuItemId,
            item.MenuId,
            item.ParentMenuItemId,
            item.Label,
            item.LinkType.ToString(),
            item.TargetId,
            item.ExternalUrl,
            item.SortOrder,
            item.IsEnabled,
            item.UpdatedAt);
}
