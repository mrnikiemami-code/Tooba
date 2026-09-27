using Tooba.Catalog.Application.StoreMenus.Models;

namespace Tooba.Catalog.Application.StoreMenus.Ports;

/// <summary>Orchestration خواندن/نوشتن منوی فروشگاه + cache (مالک Catalog).</summary>
public interface IStoreMenuWorkspace
{
    /// <summary>فهرست منوها با شمار آیتم.</summary>
    Task<IReadOnlyList<StoreMenuListView>> ListAsync(CancellationToken cancellationToken);

    /// <summary>جزئیات منو و آیتم‌ها.</summary>
    Task<StoreMenuDetailView> GetAsync(Guid menuId, CancellationToken cancellationToken);

    /// <summary>منوی جدید.</summary>
    Task<StoreMenuDetailView> CreateAsync(StoreMenuWriteRequest request, CancellationToken cancellationToken);

    /// <summary>عنوان و زبان.</summary>
    Task<StoreMenuDetailView> UpdateAsync(Guid menuId, StoreMenuWriteRequest request, CancellationToken cancellationToken);

    /// <summary>فعال/غیرفعال منو.</summary>
    Task<StoreMenuDetailView> SetEnabledAsync(Guid menuId, bool enabled, CancellationToken cancellationToken);

    /// <summary>حذف وقتی ارجاعی نیست.</summary>
    Task DeleteAsync(Guid menuId, CancellationToken cancellationToken);

    /// <summary>ارجاع‌های حذف‌مسدود.</summary>
    Task<IReadOnlyList<StoreMenuUsageView>> GetUsageAsync(Guid menuId, CancellationToken cancellationToken);

    /// <summary>افزودن آیتم با عمق و چرخهٔ امن.</summary>
    Task<StoreMenuItemAdminView> AddItemAsync(Guid menuId, StoreMenuItemWriteRequest request, CancellationToken cancellationToken);

    /// <summary>ویرایش آیتم.</summary>
    Task<StoreMenuItemAdminView> UpdateItemAsync(
        Guid menuId,
        Guid menuItemId,
        StoreMenuItemWriteRequest request,
        CancellationToken cancellationToken);

    /// <summary>فعال/غیرفعال آیتم.</summary>
    Task<StoreMenuItemAdminView> SetItemEnabledAsync(
        Guid menuId,
        Guid menuItemId,
        bool enabled,
        CancellationToken cancellationToken);

    /// <summary>حذف آیتم و فرزندان.</summary>
    Task DeleteItemAsync(Guid menuId, Guid menuItemId, CancellationToken cancellationToken);

    /// <summary>ترتیب پایدار با حفظ شناسه.</summary>
    Task<StoreMenuDetailView> ReorderItemsAsync(
        Guid menuId,
        IReadOnlyList<Guid>? orderedIds,
        CancellationToken cancellationToken);

    /// <summary>انتخاب هدر Admin.</summary>
    Task<StoreHeaderMenuSelectionView> GetHeaderSelectionAsync(CancellationToken cancellationToken);

    /// <summary>نوشتن ارجاع هدر؛ null یعنی fallback.</summary>
    Task<StoreHeaderMenuSelectionView> SetHeaderAsync(Guid? headerMenuId, CancellationToken cancellationToken);

    /// <summary>تصویر عمومی هدر یا fallback.</summary>
    Task<StoreHeaderMenuSelectionView> GetHeaderPublicAsync(CancellationToken cancellationToken);

    /// <summary>تصویر عمومی یک منوی فعال.</summary>
    Task<IReadOnlyList<StoreMenuPublicItemView>> ProjectPublicAsync(Guid menuId, CancellationToken cancellationToken);
}
