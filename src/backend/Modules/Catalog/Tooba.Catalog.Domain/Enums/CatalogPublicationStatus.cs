using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Enums;

/// <summary>
/// وضعیت انتشار توصیفی Catalog. قابل‌خرید بودن Offer نیست و موجودی انبار را نشان نمی‌دهد.
/// </summary>
public enum CatalogPublicationStatus
{
    /// <summary>
    /// پیش‌نویس تحریری؛ وجود محصول با قابل‌فروش بودن Offer یکی نیست.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// منتشرشده در Catalog. هنوز یعنی Offer/قیمت/موجودی آمادهٔ خرید نیست.
    /// </summary>
    Published = 1,

    /// <summary>
    /// بایگانی تحریری؛ حذف Offer یا موجودی نیست.
    /// </summary>
    Archived = 2,
}
