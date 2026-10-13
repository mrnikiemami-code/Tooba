namespace Tooba.Order.Contracts.Payments;

/// <summary>
/// حکم «پول تازه» برای شروع پرداخت از دید مالک سفارش. این حکم فقط‌خواندنی است و هیچ رزروی را تمدید،
/// مصرف یا آزاد نمی‌کند و هیچ مهلتی نمی‌سازد. تنها <see cref="Payable"/> اجازهٔ شروع تلاش پولی تازه
/// می‌دهد؛ همهٔ حالت‌های دیگر در امن بسته‌اند. غیبت اقتدار/چرخه هرگز مجوز نیست.
/// </summary>
public enum OrderFreshPaymentPayability
{
    /// <summary>
    /// همهٔ اثبات‌ها سبز: خطوط پایدار رزرو‌شدهٔ یکتا، چرخهٔ فعال با مهلت باقی‌مانده به‌وقت دیوار، و همهٔ
    /// رزروها Held و منقضی‌نشده. فقط این حالت اجازهٔ تلاش پولی تازه می‌دهد.
    /// </summary>
    Payable = 0,

    /// <summary>checkout خوانده نشد (ناموجود/غیرقابل‌خواندن). هیچ چیزی اثبات نشد.</summary>
    CheckoutUnavailable = 1,

    /// <summary>
    /// مجموعهٔ رزرو قابل اثبات نبود: سفارش خطی ندارد، خطی بدون رزرو است، یا یک شناسهٔ رزرو به بیش از
    /// یک خط چسبیده است (تکرار/بیگانه). امن بسته.
    /// </summary>
    ReservationSetIncomplete = 2,

    /// <summary>
    /// چرخهٔ رزرو قابل پرداخت نیست: غایب، بسته (لغو/آزادسازی/پرداخت‌شده) یا مهلتش به‌وقت دیوار تمام شده.
    /// امن بسته.
    /// </summary>
    CycleNotPayable = 3,

    /// <summary>
    /// سقف تمدید/چرخهٔ پیکربندی‌شده مصرف شده و چرخهٔ فعالی نمانده است؛ پرداخت تازه مجاز نیست تا مسیر
    /// بازتملک متعارف تصمیم بگیرد. امن بسته.
    /// </summary>
    RetryExhausted = 4,

    /// <summary>
    /// حداقل یک رزرو در Inventory اثبات نشد (گم، Released، Consumed یا منقضی). همه‌یا‌هیچ: امن بسته.
    /// </summary>
    HoldUnavailable = 5,

    /// <summary>
    /// اقتدار چرخهٔ رزرو در این ترکیب وجود ندارد، پس قابل‌پرداخت‌بودن قابل اثبات نیست. امن بسته و
    /// هرگز <see cref="Payable"/> نیست.
    /// </summary>
    AuthorityUnavailable = 6,
}

/// <summary>
/// نتیجهٔ فقط‌خواندنی ارزیابی «پول تازه». مهلت‌ها و شمارنده‌ها از تصویر متعارف چرخه گزارش می‌شوند و
/// هیچ‌کدام تضمین ACID بین Order و Inventory نیستند.
/// </summary>
/// <param name="Outcome">حکم.</param>
/// <param name="CheckoutId">شناسهٔ checkout ارزیابی‌شده.</param>
/// <param name="ReservationCount">تعداد رزروهای یکتای اثبات‌شده (صفر وقتی مجموعه استخراج نشد).</param>
/// <param name="UnavailableReservationIds">زیرمجموعه‌ای از رزروها که Inventory اثبات نکرد.</param>
/// <param name="CycleNumber">شمارهٔ چرخهٔ جاری در تصویر خوانده‌شده.</param>
/// <param name="CycleExpiresAtUtc">مهلت چرخه (ساعت Order) در تصویر خوانده‌شده.</param>
/// <param name="RetryCountRemaining">تمدیدهای باقی‌مانده طبق سیاست مؤثر (بدون سقف ابداعی).</param>
/// <param name="EffectiveMaxCycles">سقف مؤثر چرخه طبق سیاست مؤثر (بدون سقف ابداعی).</param>
public sealed record OrderFreshPaymentPayabilityResult(
    OrderFreshPaymentPayability Outcome,
    Guid CheckoutId,
    int ReservationCount,
    IReadOnlyList<Guid> UnavailableReservationIds,
    int? CycleNumber,
    DateTimeOffset? CycleExpiresAtUtc,
    int? RetryCountRemaining,
    int? EffectiveMaxCycles)
{
    /// <summary>فقط و فقط وقتی همهٔ اثبات‌ها سبز بودند درست است.</summary>
    public bool Payable => Outcome == OrderFreshPaymentPayability.Payable;
}

/// <summary>
/// درز فقط‌خواندنی قابل‌پرداخت‌بودن «پول تازه» برای شروع پرداخت. هیچ mutation، تمدید، TTL، PSP یا
/// فراخوان پولی در آن نیست و مالکیت اینجا ادعا نمی‌شود؛ اعمال مالکیت نزد هماهنگ‌کنندهٔ استورفرانت
/// Payment می‌ماند. مصرف‌کنندهٔ متعارف: مرحلهٔ شروع پرداخت تازه در Payment.
/// </summary>
public interface IOrderPaymentFreshPayabilityPort
{
    /// <summary>
    /// ارزیابی فقط‌خواندنی: خطوط پایدار سفارش، چرخهٔ رزرو با سیاست متعارف قابل‌پرداخت‌بودن، و
    /// وضعیت واقعی رزروهای Inventory. هیچ استثنایی صرفاً به‌خاطر منقضی‌بودن چرخه/رزرو پرتاب نمی‌شود.
    /// </summary>
    Task<OrderFreshPaymentPayabilityResult> GetFreshPaymentPayabilityAsync(
        Guid checkoutId,
        CancellationToken cancellationToken);
}

/// <summary>
/// مجموعهٔ رزرو خطوط یک checkout از دید Order، فقط‌خواندنی و بدون سنجش مالکیت. به‌صورت دنبالهٔ خام
/// شناسه‌های رزرو (به‌ترتیب خطوط) برگردانده می‌شود تا ارزیاب بتواند غیبت/تکرار را خودش تشخیص دهد.
/// </summary>
/// <param name="CheckoutId">شناسهٔ checkout.</param>
/// <param name="ReservationIdsByLine">
/// شناسهٔ رزرو هر خط پایدار سفارش، به‌ترتیب قطعی؛ <c>null</c> یعنی خط رزرو ندارد.
/// </param>
public sealed record OrderCheckoutReservationSet(
    Guid CheckoutId,
    IReadOnlyList<Guid?> ReservationIdsByLine);

/// <summary>خواندن فقط‌خواندنی مجموعهٔ رزرو خطوط؛ پیاده‌سازی آن در Order است و DbContext سفارش را باز می‌کند.</summary>
public interface IOrderCheckoutReservationSetReader
{
    /// <summary>مجموعهٔ رزرو خطوط را بدون هیچ mutation یا سنجش مالکیت برمی‌گرداند؛ <c>null</c> یعنی ناموجود.</summary>
    Task<OrderCheckoutReservationSet?> GetReservationSetAsync(Guid checkoutId, CancellationToken cancellationToken);
}
