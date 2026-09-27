namespace Tooba.Catalog.Application.StoreMenus.Models;

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
public sealed record StoreMenuDetailView(
    Guid MenuId,
    string Title,
    string Locale,
    bool IsEnabled,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<StoreMenuItemAdminView> Items);

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

/// <summary>پاسخ حذف با شکل Host سابق.</summary>
public sealed record StoreMenuDeletedView(bool Deleted = true);
