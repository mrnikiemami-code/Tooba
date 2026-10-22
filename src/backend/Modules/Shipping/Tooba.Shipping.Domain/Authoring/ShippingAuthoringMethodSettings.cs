using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Packaging;

namespace Tooba.Shipping.Domain.Authoring;

/// <summary>
/// تنظیمات روش یک پیش‌نویس مالک: زمان پرداخت کرایه و حالت محاسبهٔ آن، دو انتخاب <b>مستقل</b>.
/// </summary>
/// <remarks>
/// <para>
/// <b>روی ریشهٔ روش، نه روی گروه.</b> این رکورد یک‌بار به خود پیش‌نویس/روش وصل است و همهٔ گروه‌های
/// منطقه‌ای همان پیش‌نویس آن را به اشتراک می‌گذارند؛ هیچ جغرافیا، ارز، زبان یا actor دوباره اینجا
/// تکرار نمی‌شود و مالک/روش دیگر آن را به ارث نمی‌برد.
/// </para>
/// <para>
/// <b>نبود ≠ Manual/Prepaid.</b> نبود این رکورد یعنی «تنظیم‌نشده»؛ هیچ پیش‌فرضی ساخته نمی‌شود. هر دو
/// ستون اجباری‌اند، پس یک مقدار تهی هرگز به مقدار صفر enum تبدیل نمی‌شود.
/// </para>
/// <para>
/// <b>انتخاب، نه اجرا.</b> این رکورد هیچ کرایه‌ای محاسبه نمی‌کند، هیچ اتصال حاملی را اثبات نمی‌کند و
/// هیچ هزینهٔ واقعی/تأمین‌کننده‌ای را ثبت نمی‌کند. ردیف‌های دستی، ارز و متن‌های ذخیره‌شده با تغییر
/// حالت پاک نمی‌شوند.
/// </para>
/// </remarks>
public sealed class ShippingAuthoringMethodSettings
{
    private ShippingAuthoringMethodSettings()
    {
    }

    /// <summary>شناسهٔ پیش‌نویس مالک؛ همان کلید یکتای تنظیمات.</summary>
    public Guid DraftId { get; private set; }

    /// <summary>زمان پرداخت کرایه (پیش‌پرداخت یا پس‌کرایه)؛ یک انتخاب مستقل از حالت محاسبه.</summary>
    public ShippingFreightPayerMode FreightPaymentMode { get; private set; }

    /// <summary>حالت محاسبهٔ کرایه (دستی، API حامل یا رایگان)؛ مستقل از زمان پرداخت.</summary>
    public ShippingAuthoringCalculationMode CalculationMode { get; private set; }

    /// <summary>زمان آخرین جایگزینی UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    internal static ShippingAuthoringMethodSettings Create(
        Guid draftId,
        ShippingFreightPayerMode freightPaymentMode,
        ShippingAuthoringCalculationMode calculationMode,
        DateTimeOffset now)
        => new()
        {
            DraftId = draftId,
            FreightPaymentMode = freightPaymentMode,
            CalculationMode = calculationMode,
            UpdatedAt = now,
        };

    internal void Apply(
        ShippingFreightPayerMode freightPaymentMode,
        ShippingAuthoringCalculationMode calculationMode,
        DateTimeOffset now)
    {
        FreightPaymentMode = freightPaymentMode;
        CalculationMode = calculationMode;
        UpdatedAt = now;
    }

    internal bool Matches(
        ShippingFreightPayerMode freightPaymentMode,
        ShippingAuthoringCalculationMode calculationMode)
        => FreightPaymentMode == freightPaymentMode && CalculationMode == calculationMode;
}
