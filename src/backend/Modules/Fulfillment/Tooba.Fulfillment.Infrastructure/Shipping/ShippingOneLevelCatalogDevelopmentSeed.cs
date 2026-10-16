using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Application.Shipping.Ports;
using Tooba.Fulfillment.Contracts.Shipping;

namespace Tooba.Fulfillment.Infrastructure.Shipping;

/// <summary>
/// درز راه‌اندازی Development در مالکیت Fulfillment که کاتالوگ یک‌سطحی ارسال را از کاتالوگ
/// سرویس/گزینهٔ قدیمیِ از قبل دانه‌شده می‌سازد.
/// </summary>
/// <remarks>
/// <para>
/// این درز هیچ قاعدهٔ نگاشتی، هیچ SQL و هیچ الگوریتم تکراری ندارد: از برنامه‌ریز خالص موجود
/// (<see cref="ShippingMethodBackfillPlanner"/>) و درز ماندگار موجود
/// (<see cref="IShippingOneLevelBackfillStore"/>) استفاده می‌کند. Host فقط آن را داخل scope فروشگاه
/// Development صدا می‌زند؛ خود درز باید Development-only و gate-controlled بماند:
/// </para>
/// <list type="bullet">
/// <item>بیرون از Development بدون هیچ دسترسی به دیتابیس برمی‌گردد؛</item>
/// <item>وقتی دروازهٔ <see cref="ShippingOneLevelCatalogOptions"/> خاموش است، هیچ پرس‌وجو/نوشتنی رخ نمی‌دهد؛</item>
/// <item>ابتدا کاتالوگ قدیمی (منبع معتبر) از طریق درز مالکِ ماژول تضمین می‌شود و هیچ ردیفی جعل نمی‌شود؛</item>
/// <item>برنامهٔ دارای تخلف هرگز اعمال نمی‌شود (fail-closed) و برنامهٔ تمام-<c>Skip</c> هیچ تراکنش/نوشتنی
/// ندارد، پس اجرای مکرر ردیف تکراری نمی‌سازد.</item>
/// </list>
/// </remarks>
public static class ShippingOneLevelCatalogDevelopmentSeed
{
    /// <summary>یک بار backfill کاتالوگ یک‌سطحی را داخل scope معتبر Development اجرا می‌کند.</summary>
    public static async Task ApplyAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        var environment = services.GetRequiredService<IHostEnvironment>();
        if (!environment.IsDevelopment())
        {
            return;
        }

        var gate = ShippingOneLevelCatalogPolicy.Evaluate(
            services.GetRequiredService<IOptions<ShippingOneLevelCatalogOptions>>().Value);
        if (gate != ShippingOneLevelCatalogGate.Enabled)
        {
            // دروازهٔ خاموش ⇒ صفر دسترسی به دیتابیس.
            return;
        }

        // منبع معتبر: کاتالوگ سرویس/گزینهٔ قدیمی باید ابتدا وجود داشته باشد. همان درز دانهٔ مالکِ ماژول
        // که فرافکنی فروشگاه هم به آن تکیه دارد بازاستفاده می‌شود؛ هیچ ردیفی ساخته/جعل نمی‌شود.
        await services.GetRequiredService<IShippingCatalogSeedPort>().EnsureSeedAsync(cancellationToken);

        var catalog = await services.GetRequiredService<IShippingCatalogReader>().ListAsync(cancellationToken);
        var store = services.GetRequiredService<IShippingOneLevelBackfillStore>();
        var existing = await store.ListExistingAsync(cancellationToken);

        var plan = ShippingMethodBackfillPlanner.Plan(catalog, existing);
        if (!plan.IsExecutable)
        {
            // fail-closed: برنامهٔ دارای تخلف هرگز اعمال نمی‌شود و برای دور زدن سیاست چیزی ساخته نمی‌شود.
            return;
        }

        if (plan.Items.All(item => item.Action == ShippingMethodPlanAction.Skip))
        {
            // idempotent: چیزی برای ساخت/هم‌راستاسازی نیست ⇒ بدون تراکنش و بدون نوشتن.
            return;
        }

        await store.ApplyAsync(plan, cancellationToken);
    }
}
