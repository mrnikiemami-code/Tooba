using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure;

/// <summary>
/// orchestration موقت نوشتن منو روی Catalog DbContext تا جابه‌جایی BC.
/// </summary>
public sealed class StoreMenuDirectory : IStoreMenuDirectory
{
    private readonly CatalogDbContext _catalog;
    private readonly IClock _clock;

    /// <summary>دایرکتوری را به schema catalog وصل می‌کند.</summary>
    public StoreMenuDirectory(CatalogDbContext catalog, IClock clock)
    {
        _catalog = catalog;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<StoreMenu> CreateAsync(StoreMenuWriteModel request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var menu = StoreMenu.Create(request.Title, request.Locale, request.MenuKey, now);
        if (await _catalog.StoreMenus.AnyAsync(x => x.Locale == menu.Locale && x.MenuKey == menu.MenuKey, cancellationToken))
        {
            throw new PlatformHttpException(409, "کلید منو تکراری است.", "menu.key.duplicate");
        }

        _catalog.StoreMenus.Add(menu);
        await _catalog.SaveChangesAsync(cancellationToken);
        return menu;
    }

    /// <inheritdoc />
    public async Task<StoreMenu> UpdateAsync(Guid menuId, StoreMenuWriteModel request, CancellationToken cancellationToken)
    {
        var menu = await RequireMenuAsync(menuId, cancellationToken);
        menu.Update(request.Title, request.Locale, _clock.UtcNow);
        await _catalog.SaveChangesAsync(cancellationToken);
        return menu;
    }

    /// <inheritdoc />
    public async Task<StoreMenu> SetEnabledAsync(Guid menuId, bool enabled, CancellationToken cancellationToken)
    {
        var menu = await RequireMenuAsync(menuId, cancellationToken);
        menu.SetEnabled(enabled, _clock.UtcNow);
        if (!enabled)
        {
            await ClearHeaderIfMatchAsync(menuId, cancellationToken);
        }

        await _catalog.SaveChangesAsync(cancellationToken);
        return menu;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid menuId, CancellationToken cancellationToken)
    {
        var usage = await FindUsageCountAsync(menuId, cancellationToken);
        if (usage > 0)
        {
            throw new PlatformHttpException(409, "این منو در حال استفاده است.", "menu.delete.referenced");
        }

        var items = await _catalog.StoreMenuItems.Where(x => x.MenuId == menuId).ToListAsync(cancellationToken);
        _catalog.StoreMenuItems.RemoveRange(items);
        var menu = await RequireMenuAsync(menuId, cancellationToken);
        _catalog.StoreMenus.Remove(menu);
        await _catalog.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<StoreMenuItem> AddItemAsync(Guid menuId, StoreMenuItemWriteModel request, CancellationToken cancellationToken)
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
            _clock.UtcNow);
        _catalog.StoreMenuItems.Add(item);
        menu.Touch(item.UpdatedAt);
        await _catalog.SaveChangesAsync(cancellationToken);
        return item;
    }

    /// <inheritdoc />
    public async Task<StoreMenuItem> UpdateItemAsync(Guid menuId, Guid menuItemId, StoreMenuItemWriteModel request, CancellationToken cancellationToken)
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
        item.Apply(parentId, request.Label, linkType, request.TargetId, request.ExternalUrl, request.SortOrder ?? item.SortOrder, request.IsEnabled ?? item.IsEnabled, _clock.UtcNow);
        menu.Touch(item.UpdatedAt);
        await _catalog.SaveChangesAsync(cancellationToken);
        return item;
    }

    /// <inheritdoc />
    public async Task<StoreMenuItem> SetItemEnabledAsync(Guid menuId, Guid menuItemId, bool enabled, CancellationToken cancellationToken)
    {
        var menu = await RequireMenuAsync(menuId, cancellationToken);
        var item = await _catalog.StoreMenuItems.SingleOrDefaultAsync(x => x.MenuId == menuId && x.MenuItemId == menuItemId, cancellationToken)
            ?? throw new PlatformHttpException(404, "آیتم منو یافت نشد.", "menu.item.missing");
        item.SetEnabled(enabled, _clock.UtcNow);
        menu.Touch(item.UpdatedAt);
        await _catalog.SaveChangesAsync(cancellationToken);
        return item;
    }

    /// <inheritdoc />
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
        menu.Touch(_clock.UtcNow);
        await _catalog.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<StoreMenu> ReorderItemsAsync(Guid menuId, IReadOnlyList<Guid> orderedIds, CancellationToken cancellationToken)
    {
        var menu = await RequireMenuAsync(menuId, cancellationToken);
        var items = await LoadItemsAsync(menuId, cancellationToken);
        if (orderedIds.Count == 0 || orderedIds.Count != items.Count || orderedIds.Distinct().Count() != items.Count)
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
        var now = _clock.UtcNow;
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
        return menu;
    }

    /// <inheritdoc />
    public async Task SetHeaderAsync(Guid? headerMenuId, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
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

    private async Task<int> FindUsageCountAsync(Guid menuId, CancellationToken cancellationToken)
    {
        var count = 0;
        var settings = await _catalog.StoreAppearanceSettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SettingsId == StoreAppearanceSettings.SingletonId, cancellationToken);
        if (settings?.HeaderMenuId == menuId)
        {
            count++;
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
                count++;
            }
        }

        return count;
    }

    private async Task ClearHeaderIfMatchAsync(Guid menuId, CancellationToken cancellationToken)
    {
        var settings = await _catalog.StoreAppearanceSettings
            .SingleOrDefaultAsync(x => x.SettingsId == StoreAppearanceSettings.SingletonId, cancellationToken);
        if (settings?.HeaderMenuId == menuId)
        {
            settings.SetHeaderMenu(null, _clock.UtcNow);
        }
    }

    private async Task<StoreMenu> RequireMenuAsync(Guid menuId, CancellationToken cancellationToken) =>
        await _catalog.StoreMenus.SingleOrDefaultAsync(x => x.MenuId == menuId, cancellationToken)
            ?? throw new PlatformHttpException(404, "منو یافت نشد.", "menu.missing");

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
}
