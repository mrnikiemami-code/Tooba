using Tooba.Promotion.Domain.Aggregates;

namespace Tooba.Promotion.Application.Promotions.Ports;

/// <summary>
/// منبع خواندن تعریف‌های پروموشن برای ارزیابی. مالکیت خواندن در Promotion می‌ماند و
/// تعریف‌ها هرگز از مرز Contracts بیرون نمی‌روند.
/// یک نمونهٔ scoped فقط یک‌بار جدول promotion را می‌خواند و هر جهش همان scope آن را باطل می‌کند،
/// تا ارزیابی چند آیتمی (Home/Checkout) به یک خواندن تبدیل شود، نه یک خواندن به‌ازای هر آیتم.
/// </summary>
public interface IPromotionCandidateSource
{
    /// <summary>
    /// کاندیدهای فعلی را برمی‌گرداند؛ در یک scope حداکثر یک خواندن واقعی انجام می‌شود.
    /// </summary>
    Task<IReadOnlyList<PromotionDefinition>> GetAsync(CancellationToken cancellationToken);

    /// <summary>
    /// تصویر فعلی را باطل می‌کند تا خواندن بعدی دادهٔ تازه بگیرد (پس از هر جهش در همان scope).
    /// </summary>
    void Invalidate();
}
