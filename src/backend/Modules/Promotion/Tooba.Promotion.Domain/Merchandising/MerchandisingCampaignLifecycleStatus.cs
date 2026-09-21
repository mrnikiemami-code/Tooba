

namespace Tooba.Promotion.Domain.Merchandising;

/// <summary>
/// وضعیت چرخهٔ عمر کمپین مرچندایزینگ فروشگاهی. با <see cref="PromotionStatus"/> تسویه یکی نیست.
/// </summary>
public enum MerchandisingCampaignLifecycleStatus
{
    /// <summary>
    /// پیش‌نویس؛ هرگز در زمان اجرا فعال نیست.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// منتشرشده؛ فقط داخل پنجرهٔ StartAt/EndAt فعال محسوب می‌شود.
    /// </summary>
    Published = 1,

    /// <summary>
    /// بایگانی؛ هرگز فعال نیست.
    /// </summary>
    Archived = 2,
}
