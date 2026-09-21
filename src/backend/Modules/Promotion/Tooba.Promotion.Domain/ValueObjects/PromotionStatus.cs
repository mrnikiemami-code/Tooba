

namespace Tooba.Promotion.Domain.ValueObjects;

/// <summary>
/// وضعیت انتشار پروموشن. پیش‌نویس در ارزیابی شرکت نمی‌کند.
/// </summary>
public enum PromotionStatus
{
    /// <summary>
    /// هنوز برای تسویه قابل‌اعمال نیست.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// در پنجرهٔ اعتبار می‌تواند ارزیابی شود.
    /// </summary>
    Active = 1,

    /// <summary>
    /// منقضی یا بازنشسته؛ سفارش تاریخی را عوض نمی‌کند.
    /// </summary>
    Expired = 2,
}
