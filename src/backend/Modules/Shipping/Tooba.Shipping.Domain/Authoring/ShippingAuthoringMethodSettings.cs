using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Packaging;

namespace Tooba.Shipping.Domain.Authoring;

/// <summary>
/// تنظیمات روش یک پیش‌نویس مالک: زمان پرداخت کرایه، حالت محاسبهٔ آن و سیاست زمان‌بندی فیزیکی
/// (آماده‌سازی و انتقال به حامل)، همه انتخاب‌های <b>مستقل</b>.
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
/// <b>TB-264: زمان فیزیکی، نه روز تقویمی.</b> <see cref="PreparationMinutes"/> زمان آماده‌سازی
/// فیزیکی و <see cref="HandoffMinutes"/> زمان انتقال/تحویل به حامل است. هر دو <b>دقیقه</b>اند و
/// صفرشان فقط وقتی معنا دارد که اپراتور صریحاً صفر نوشته باشد؛ مقدار تهی یعنی «سیاست زمان‌بندی تنظیم
/// نشده» و هرگز به «آمادهٔ همین لحظه» تفسیر نمی‌شود. این دو مقدار هیچ رابطه‌ای با روزهای حمل
/// (Lead/Transit) ندارند و یک‌بار شمرده می‌شوند.
/// </para>
/// <para>
/// <b>TB-264-R1: معنای صریح زمان پردازش.</b> <see cref="PreparationMode"/> می‌گوید آماده‌سازی
/// <b>کجا</b> شمرده می‌شود: داخل هفتهٔ کاری پردازش، یا کارکرد پیوستهٔ ۲۴/۷ که اپراتور صریحاً انتخاب
/// کرده است. مقدار <c>Unspecified</c> یعنی سیاست ناقص؛ تعهد جدید با آن ساخته نمی‌شود و هرگز به
/// کارکرد شبانه‌روزی پیش‌فرض تبدیل نمی‌گردد. انتقال به حامل همیشه سپری‌شده و یک‌بار است.
/// </para>
/// </remarks>
public sealed class ShippingAuthoringMethodSettings
{
    /// <summary>بیشینهٔ زمان آماده‌سازی (هفت روز کامل) هم‌ارز کران Contracts.</summary>
    public const int MaxPreparationMinutes = 7 * 24 * 60;

    /// <summary>بیشینهٔ زمان انتقال/تحویل به حامل (یک روز کامل) هم‌ارز کران Contracts.</summary>
    public const int MaxHandoffMinutes = 24 * 60;

    private ShippingAuthoringMethodSettings()
    {
    }

    /// <summary>شناسهٔ پیش‌نویس مالک؛ همان کلید یکتای تنظیمات.</summary>
    public Guid DraftId { get; private set; }

    /// <summary>زمان پرداخت کرایه (پیش‌پرداخت یا پس‌کرایه)؛ یک انتخاب مستقل از حالت محاسبه.</summary>
    public ShippingFreightPayerMode FreightPaymentMode { get; private set; }

    /// <summary>حالت محاسبهٔ کرایه (دستی، API حامل یا رایگان)؛ مستقل از زمان پرداخت.</summary>
    public ShippingAuthoringCalculationMode CalculationMode { get; private set; }

    /// <summary>TB-264: زمان سپری‌شدهٔ آماده‌سازی فیزیکی به دقیقه؛ تهی یعنی سیاست تنظیم نشده.</summary>
    public int? PreparationMinutes { get; private set; }

    /// <summary>TB-264: زمان انتقال/تحویل به حامل به دقیقه؛ تهی یعنی سیاست تنظیم نشده.</summary>
    public int? HandoffMinutes { get; private set; }

    /// <summary>
    /// TB-264-R1: معنای صریح زمان آماده‌سازی (هفتهٔ کاری پردازش یا کارکرد پیوستهٔ ۲۴/۷)؛
    /// <c>Unspecified</c> یعنی سیاست ناقص.
    /// </summary>
    public ShippingAuthoringPreparationProcessingMode PreparationMode { get; private set; }

    /// <summary>زمان آخرین جایگزینی UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    internal static ShippingAuthoringMethodSettings Create(
        Guid draftId,
        ShippingFreightPayerMode freightPaymentMode,
        ShippingAuthoringCalculationMode calculationMode,
        DateTimeOffset now,
        int? preparationMinutes = null,
        int? handoffMinutes = null,
        ShippingAuthoringPreparationProcessingMode preparationMode =
            ShippingAuthoringPreparationProcessingMode.Unspecified)
        => new()
        {
            DraftId = draftId,
            FreightPaymentMode = freightPaymentMode,
            CalculationMode = calculationMode,
            PreparationMinutes = preparationMinutes,
            HandoffMinutes = handoffMinutes,
            PreparationMode = preparationMode,
            UpdatedAt = now,
        };

    internal void Apply(
        ShippingFreightPayerMode freightPaymentMode,
        ShippingAuthoringCalculationMode calculationMode,
        DateTimeOffset now,
        int? preparationMinutes = null,
        int? handoffMinutes = null,
        ShippingAuthoringPreparationProcessingMode preparationMode =
            ShippingAuthoringPreparationProcessingMode.Unspecified)
    {
        FreightPaymentMode = freightPaymentMode;
        CalculationMode = calculationMode;
        PreparationMinutes = preparationMinutes;
        HandoffMinutes = handoffMinutes;
        PreparationMode = preparationMode;
        UpdatedAt = now;
    }

    internal bool Matches(
        ShippingFreightPayerMode freightPaymentMode,
        ShippingAuthoringCalculationMode calculationMode,
        int? preparationMinutes = null,
        int? handoffMinutes = null,
        ShippingAuthoringPreparationProcessingMode preparationMode =
            ShippingAuthoringPreparationProcessingMode.Unspecified)
        => FreightPaymentMode == freightPaymentMode
           && CalculationMode == calculationMode
           && PreparationMinutes == preparationMinutes
           && HandoffMinutes == handoffMinutes
           && PreparationMode == preparationMode;

    /// <summary>
    /// TB-264: آیا سیاست زمان‌بندی فیزیکی به‌طور کامل تنظیم شده است (هر دو مقدار با هم).
    /// </summary>
    public bool HasTimingPolicy => PreparationMinutes is not null && HandoffMinutes is not null;

    /// <summary>
    /// TB-264-R1: آیا سیاست فیزیکی <b>کامل و قابل‌تعهد</b> است — جفت زمان‌ها به‌همراه معنای صریح زمان
    /// پردازش. سیاست ناقص تعهد جدید نمی‌سازد و هیچ پیش‌فرض شبانه‌روزی ندارد.
    /// </summary>
    public bool HasCommittableTimingPolicy =>
        HasTimingPolicy && PreparationMode != ShippingAuthoringPreparationProcessingMode.Unspecified;
}
