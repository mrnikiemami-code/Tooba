

namespace Tooba.Inventory.Domain.ValueObjects;

/// <summary>
/// وضعیت محل نگهداری موجودی. توپولوژی لجستیک کامل اینجا مدل نمی‌شود.
/// </summary>
public enum InventoryLocationStatus
{
    /// <summary>
    /// محل برای دریافت و رزرو قابل‌استفاده است.
    /// </summary>
    Active = 0,

    /// <summary>
    /// محل از گردش خارج شده؛ حذف Catalog نیست.
    /// </summary>
    Retired = 1,
}
