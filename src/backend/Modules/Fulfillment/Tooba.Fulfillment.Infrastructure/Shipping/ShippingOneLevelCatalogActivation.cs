using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Shipping.Ports;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Infrastructure.Persistence;

namespace Tooba.Fulfillment.Infrastructure.Shipping;

/// <summary>
/// مرجع واحد و تراکنشی فعال‌سازی روش یک‌سطحی گزینه‌دار روی schema fulfillment — باریک و fail-closed.
/// </summary>
/// <remarks>
/// <para>
/// مرجع عملیاتی فعال‌سازی همان ستون قدیمی <c>shipping_service_options.is_active</c> است، چون فروشگاه
/// (<c>StorefrontShippingService</c>) فقط سرویس/گزینهٔ قدیمی را می‌خواند. ردیف یک‌سطحی
/// <c>shipping_methods.is_active</c> فرافکنی ادمین است؛ این درز هر دو را در یک
/// <c>SaveChangesAsync</c> هم‌راستا می‌کند تا هیچ‌گاه یک «فعال» نمایشی و بی‌اثر ساخته نشود.
/// </para>
/// <para>
/// TB-SHIPPING-NEW-METHOD-ACTIVATION-170: روش تازهٔ canonical (سرویس والد موجود، اما هیچ ردیف
/// گزینهٔ قدیمی متناظر) دیگر رد نمی‌شود. در این حالت فقط ردیف روش فعال/غیرفعال می‌شود و هیچ
/// <c>shipping_service_options</c> جعلی ساخته نمی‌شود. این حالت یعنی «فعال در فرافکنی ادمین»، نه
/// «قابل انتخاب برای مشتری»؛ دیده‌شدن در فروشگاه فقط با نرخ/پوشش واقعی مالک Shipping ممکن است.
/// سرویس والد ناموجود یا غیرفعال همچنان fail-closed است، چون ثبت ارائه‌دهنده یک پیش‌نیاز واقعی است.
/// </para>
/// <para>
/// پیوند قدیمی فقط از ردیف روش خوانده می‌شود (<c>LegacyServiceCode</c>/<c>LegacyOptionCode</c> که
/// تغییرناپذیرند)؛ هیچ کد سرویس/گزینه یا UUID گزینه‌ای از کلاینت گرفته نمی‌شود. تنها نوشتن روی ردیف
/// گزینهٔ هدف، متد دامنهٔ موجود <see cref="ShippingServiceOption.SetActive"/> است: کد، ترتیب، ترجمه‌ها
/// و گزینه‌های خواهر دست‌نخورده می‌مانند و هیچ حذف/درج دسته‌جمعی رخ نمی‌دهد.
/// </para>
/// <para>
/// دروازهٔ خاموش ⇒ هیچ پرس‌وجو/نوشتنی روی دیتابیس. والد غیرفعال هرگز به‌صورت خودکار فعال نمی‌شود
/// (روش به سرویس بدون گزینه هم حدس زده نمی‌شود). جدول مهاجرت‌نشده یک نتیجهٔ کنترل‌شده است، نه ۵۰۰.
/// این درز <c>Tooba:ShippingRateAuthority</c> و <c>Tooba:ShippingOperationalShadow</c> را نمی‌خواند،
/// القا نمی‌کند و تغییر نمی‌دهد و هیچ قیمت/سفارش/اسنپ‌شاتی را لمس نمی‌کند.
/// </para>
/// </remarks>
public sealed class ShippingOneLevelCatalogActivation : IShippingOneLevelCatalogAdminActivation
{
    private readonly IOptions<ShippingOneLevelCatalogOptions> _options;
    private readonly FulfillmentDbContext _db;
    private readonly IClock _clock;

    /// <summary>درز را به پیکربندی، DbContext مالک ماژول و ساعت وصل می‌کند.</summary>
    public ShippingOneLevelCatalogActivation(
        IOptions<ShippingOneLevelCatalogOptions> options,
        FulfillmentDbContext db,
        IClock clock)
    {
        _options = options;
        _db = db;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<ShippingOneLevelMethodActivationResult> SetActivationAsync(
        string methodCode,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var gate = ShippingOneLevelCatalogPolicy.Evaluate(_options.Value);
        if (gate != ShippingOneLevelCatalogGate.Enabled)
        {
            // دروازهٔ خاموش ⇒ هیچ پرس‌وجو/نوشتنی روی دیتابیس.
            return Outcome(
                ShippingOneLevelMethodActivationStatus.GateDisabled, string.Empty, isActive, isActive);
        }

        var normalized = ShippingMethod.TryNormalizeCode(methodCode);
        if (normalized is null)
        {
            // کد خام/نامعتبر هرگز نرمال یا به هویت تازه تبدیل نمی‌شود.
            return Outcome(
                ShippingOneLevelMethodActivationStatus.InvalidMethodCode, string.Empty, isActive, isActive);
        }

        try
        {
            var method = await _db.ShippingMethods
                .FirstOrDefaultAsync(x => x.MethodCode == normalized, cancellationToken);
            if (method is null)
            {
                return Outcome(ShippingOneLevelMethodActivationStatus.MethodNotFound, normalized, isActive, isActive);
            }

            // پیوند قدیمی فقط از ردیف روش؛ هرگز از ورودی کلاینت.
            var service = await _db.ShippingServices
                .FirstOrDefaultAsync(x => x.Code == method.LegacyServiceCode, cancellationToken);
            if (service is null)
            {
                return Outcome(ShippingOneLevelMethodActivationStatus.LegacyBindingMissing, normalized, isActive, method.IsActive);
            }

            if (!service.IsActive)
            {
                // والد غیرفعال هرگز خودکار فعال نمی‌شود؛ این عملیات سطح گزینه است، نه سرویس.
                return Outcome(ShippingOneLevelMethodActivationStatus.LegacyParentInactive, normalized, isActive, method.IsActive);
            }

            if (string.IsNullOrWhiteSpace(method.LegacyOptionCode))
            {
                // روش بدون گزینه = وضعیت سطح سرویس؛ در این عملیات حدس زده نمی‌شود.
                return Outcome(
                    ShippingOneLevelMethodActivationStatus.OptionlessRequiresServiceDecision, normalized, isActive, method.IsActive);
            }

            var option = await _db.ShippingServiceOptions
                .FirstOrDefaultAsync(
                    x => x.ShippingServiceId == service.ShippingServiceId && x.Code == method.LegacyOptionCode,
                    cancellationToken);
            if (option is null)
            {
                // TB-SHIPPING-NEW-METHOD-ACTIVATION-170: روش تازهٔ canonical هیچ ردیف گزینهٔ قدیمی
                // ندارد و ساختن ردیف جعلی برای عبور از نگهبان ممنوع است. فعال‌سازی فقط فرافکنی
                // ادمین (ردیف روش) را به‌روز می‌کند؛ هیچ ادعای «آماده برای مشتری» این‌جا نیست.
                if (method.IsActive == isActive)
                {
                    return Outcome(ShippingOneLevelMethodActivationStatus.NoChange, normalized, isActive, isActive);
                }

                method.Update(method.SortOrder, isActive, _clock.UtcNow);
                await _db.SaveChangesAsync(cancellationToken);
                return Outcome(
                    ShippingOneLevelMethodActivationStatus.ActivatedCanonicalOnly, normalized, isActive, isActive);
            }

            if (option.IsActive == isActive && method.IsActive == isActive)
            {
                // وضعیت از قبل برقرار است ⇒ نوشتنی انجام نمی‌شود (idempotent).
                return Outcome(ShippingOneLevelMethodActivationStatus.NoChange, normalized, isActive, isActive);
            }

            var now = _clock.UtcNow;
            // فقط همان ردیف گزینهٔ موجود؛ بدون بازنویسی کد/ترتیب/ترجمه و بدون لمس گزینه‌های خواهر.
            option.SetActive(isActive, now);
            // ردیف یک‌سطحی فرافکنی ادمین است و در همان تراکنش هم‌راستا می‌شود.
            method.Update(method.SortOrder, isActive, now);

            await _db.SaveChangesAsync(cancellationToken);
            return Outcome(ShippingOneLevelMethodActivationStatus.Applied, normalized, isActive, isActive);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            // کاتالوگ یک‌سطحی (یا جدول قدیمی) مهاجرت نشده است؛ نتیجهٔ کنترل‌شده و بدون نوشتن.
            return Outcome(ShippingOneLevelMethodActivationStatus.CatalogNotReady, normalized, isActive, isActive);
        }
    }

    private static ShippingOneLevelMethodActivationResult Outcome(
        ShippingOneLevelMethodActivationStatus status,
        string methodCode,
        bool requestedIsActive,
        bool effectiveIsActive) =>
        new(status, methodCode, requestedIsActive, effectiveIsActive);
}
