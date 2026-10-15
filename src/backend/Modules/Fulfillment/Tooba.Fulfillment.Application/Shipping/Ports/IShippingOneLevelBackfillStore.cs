using Tooba.Fulfillment.Application.Shipping;

namespace Tooba.Fulfillment.Application.Shipping.Ports;

/// <summary>
/// درز ماندگار backfill کاتالوگ یک‌سطحی. مسئولیت خواندن وضعیت موجود و اعمال اتمی برنامه است؛
/// تصمیم‌گیری در <see cref="ShippingMethodBackfillPlanner"/> می‌ماند و این درز هیچ قاعده‌ای را
/// دوباره تعریف نمی‌کند.
/// </summary>
/// <remarks>
/// دو قاعدهٔ سخت این درز:
/// <list type="bullet">
/// <item>اعمال باید همه‌یا‌هیچ باشد: اگر هر ردیفی شکست بخورد، هیچ ردیفی نباید باقی بماند.</item>
/// <item>هیچ ردیف موجودی حذف نمی‌شود و هیچ کد/پیوند تاریخی بازنویسی نمی‌شود؛ فقط ردیف تازه ساخته
/// می‌شود و فعال/غیرفعال بودن صریحاً هم‌راستا می‌شود.</item>
/// </list>
/// </remarks>
public interface IShippingOneLevelBackfillStore
{
    /// <summary>روش‌های موجود کاتالوگ یک‌سطحی را می‌خواند؛ جدول مهاجرت‌نشده باید خطا بدهد، نه لیست خالی.</summary>
    Task<IReadOnlyList<ShippingMethodExistingRow>> ListExistingAsync(CancellationToken cancellationToken);

    /// <summary>
    /// برنامهٔ بدون‌تخلف را اتمی اعمال می‌کند. اجرای دوباره روی کاتالوگ نگاشته‌شده باید همهٔ
    /// اقدام‌ها را <see cref="ShippingMethodPlanAction.Skip"/> ببیند و هیچ نوشتنی نداشته باشد.
    /// </summary>
    Task ApplyAsync(ShippingMethodBackfillPlan plan, CancellationToken cancellationToken);
}
