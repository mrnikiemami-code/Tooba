namespace Tooba.Order.Contracts.Payments;

/// <summary>
/// سرنوشت آماده‌سازی مهلت رزرو برای شروع پرداخت. هیچ حالت مبهمی ندارد و همهٔ حالت‌های غیر از
/// <see cref="Prepared"/> در امن بسته‌اند (پرداخت نباید آغاز شود).
/// </summary>
public enum OrderPaymentHoldPreparationOutcome
{
    /// <summary>
    /// مالکیت اثبات شد، چرخهٔ رزرو Order در همین لحظه قابل پرداخت بود، و تمدید اتمی همه‌یا‌هیچ Inventory
    /// برای همهٔ رزروهای خطوط سفارش commit شد و چرخه پس از تمدید هم هنوز قابل پرداخت بود.
    /// </summary>
    Prepared = 0,

    /// <summary>
    /// checkout وجود ندارد، یا فراخوان مالک آن نیست، یا cartId با checkout نمی‌خواند.
    /// عمداً بدون تفکیک گزارش می‌شود تا اطلاعاتی افشا نشود؛ هیچ رزروی لمس نشد.
    /// </summary>
    AccessDenied = 1,

    /// <summary>
    /// مجموعهٔ رزرو قابل استخراج نبود: سفارش خطی ندارد، خطی بدون رزرو است
    /// (<c>null</c>/<c>Guid.Empty</c>)، یا یک شناسهٔ رزرو به بیش از یک خط چسبیده است (مبهم). هیچ
    /// رزروی تمدید نشد.
    /// </summary>
    ReservationSetIncomplete = 2,

    /// <summary>
    /// چرخهٔ رزرو Order در همین لحظه قابل پرداخت نیست (غایب، بسته، یا مهلت به‌وقت دیوار تمام شده).
    /// هیچ رزروی تمدید نشد.
    /// </summary>
    CycleNotPayable = 3,

    /// <summary>
    /// مهلت امن از سیاست مؤثر قابل تعیین نبود (روش پرداخت مشخص نیست، یا منبع سیاست در دسترس نیست).
    /// ایـن درز TTL نمی‌سازد و حدس نمی‌زند. هیچ رزروی تمدید نشد.
    /// </summary>
    PolicyUnavailable = 4,

    /// <summary>
    /// Inventory حداقل یک رزرو را اثبات نکرد (گم/Released/Consumed/منقضی). همه‌یا‌هیچ: هیچ رزروی تمدید نشد.
    /// </summary>
    HoldUnavailable = 5,

    /// <summary>Inventory مهلت را نامعتبر دانست (غایب یا گذشته نسبت به ساعت دیتابیس). هیچ رزروی تمدید نشد.</summary>
    DeadlineRejected = 6,

    /// <summary>
    /// فراخوان داخل یک <c>TransactionScope</c> محیطی اجرا شده است. این درز تراکنش چند‌ماژولی نمی‌سازد.
    /// هیچ رزروی تمدید نشد.
    /// </summary>
    AmbientTransactionUnsupported = 7,

    /// <summary>
    /// تمدید اتمی Inventory commit شد، اما بازخوانی Order نشان داد چرخهٔ رزرو دیگر قابل پرداخت نیست
    /// (بین دو گام بسته شد). عمداً <see cref="Prepared"/> گزارش نمی‌شود؛ تمدید انجام‌شده یکنوا و بی‌ضرر
    /// است و با مسیر بستن چرخه آزاد می‌شود. هیچ تضمین ACID بین دو ماژول ادعا نمی‌شود.
    /// </summary>
    CycleClosedAfterExtension = 8,
}

/// <summary>
/// درخواست آماده‌سازی. <b>هیچ شناسهٔ رزرو، مبلغ یا مهلتی از فراخوان پذیرفته نمی‌شود</b>؛ مجموعهٔ رزرو
/// فقط از خطوط پایدار سفارش استخراج می‌شود. <see cref="CheckoutId"/> و <see cref="CartId"/> و
/// <see cref="GuestSecret"/> فقط برای اثبات مالکیت به کار می‌روند و به‌تنهایی مرجع نیستند.
/// </summary>
/// <param name="CheckoutId">شناسهٔ checkout؛ مالکیت آن اثبات می‌شود.</param>
/// <param name="CartId">شناسهٔ سبد برای تطبیق مالکیت (اختیاری ولی هنگام ارسال باید با checkout بخواند).</param>
/// <param name="GuestSecret">گواه مالکیت سبد مهمان (فقط در حالت مهمان).</param>
/// <param name="AuthenticatedUserId">کاربر احراز‌هویت‌شده در صورت وجود.</param>
/// <param name="ProviderCode">
/// روش پرداخت انتخاب‌شده؛ تنها به‌عنوان <b>انتخاب‌گر سیاست</b> (override همان روش در تنظیمات) استفاده
/// می‌شود و هیچ اقتدار مالی ندارد. موجود بودن واقعی روش همچنان توسط ماژول Payment تعیین می‌شود.
/// </param>
public sealed record OrderPaymentHoldPreparationRequest(
    Guid CheckoutId,
    Guid? CartId,
    string? GuestSecret,
    Guid? AuthenticatedUserId,
    string ProviderCode);

/// <summary>
/// گواه آماده‌سازی. <see cref="RequestedDeadlineUtc"/> = دیرترین مهلتی که این فراخوانی از سیاست مؤثر
/// درخواست کرده، و <see cref="GuaranteedUntilUtc"/> = کمینهٔ مهلت‌های متناهیِ نهایی در Inventory
/// (<c>null</c> یعنی همهٔ رزروها بی‌مهلت/durable). این دو مهلت و مهلت چرخهٔ Order
/// (<see cref="CycleExpiresAtUtc"/>) مستقل‌اند و هیچ تضمین ACID بین دو ماژول وجود ندارد.
/// </summary>
/// <param name="Outcome">سرنوشت.</param>
/// <param name="CheckoutId">شناسهٔ checkout.</param>
/// <param name="ProviderCode">روش انتخاب‌شدهٔ به‌کار‌رفته برای حل سیاست.</param>
/// <param name="ReservationCount">تعداد رزروهای یکتای خطوط سفارش که اثبات/تمدید شدند.</param>
/// <param name="RequestedDeadlineUtc">مهلت درخواست‌شده از سیاست (در صورت تعیین‌شدن).</param>
/// <param name="GuaranteedUntilUtc">ضعیف‌ترین مهلت نهایی تضمین‌شده در Inventory.</param>
/// <param name="CycleNumber">شمارهٔ چرخهٔ رزرو Order در آخرین بازخوانی.</param>
/// <param name="CycleExpiresAtUtc">مهلت چرخهٔ رزرو Order در آخرین بازخوانی (ساعت Order).</param>
/// <param name="UnavailableReservationIds">زیرمجموعه‌ای از رزروها که Inventory اثبات نکرد.</param>
public sealed record OrderPaymentHoldPreparationResult(
    OrderPaymentHoldPreparationOutcome Outcome,
    Guid CheckoutId,
    string ProviderCode,
    int ReservationCount,
    DateTimeOffset? RequestedDeadlineUtc,
    DateTimeOffset? GuaranteedUntilUtc,
    int? CycleNumber,
    DateTimeOffset? CycleExpiresAtUtc,
    IReadOnlyList<Guid> UnavailableReservationIds)
{
    /// <summary>فقط و فقط وقتی همهٔ گام‌ها سبز بودند درست است.</summary>
    public bool Prepared => Outcome == OrderPaymentHoldPreparationOutcome.Prepared;

    /// <summary>
    /// تمدید اتمی Inventory واقعاً commit شده است (یا <see cref="Prepared"/>، یا چرخه بعد از تمدید بسته شد).
    /// این پرچم دربارهٔ مجاز‌بودن پرداخت چیزی نمی‌گوید.
    /// </summary>
    public bool HoldExtensionCommitted =>
        Outcome is OrderPaymentHoldPreparationOutcome.Prepared
            or OrderPaymentHoldPreparationOutcome.CycleClosedAfterExtension;
}

/// <summary>
/// درز آماده‌سازی پرداخت به‌مالکیت Order: پیش از هر عملیات پولی، مجموعهٔ رزرو خطوط پایدار سفارش را
/// مستقل از فراخوان استخراج می‌کند، مالکیت و قابل‌پرداخت‌بودن چرخهٔ Order را می‌سنجد، مهلت را از
/// سیاست مؤثر همان روش پرداخت می‌گیرد و سپس تمدید اتمی همه‌یا‌هیچ Inventory را اعمال می‌کند.
///
/// این یک بلوک سازنده است و <b>هیچ endpoint، PSP، برداشت کیف پول، ساخت payment attempt، تبدیل سبد یا
/// نوشتن Order</b> در آن نیست. هیچ تأییدیهٔ ACID بین Order و Inventory ادعا نمی‌شود.
/// </summary>
public interface IOrderPaymentHoldPreparationPort
{
    /// <summary>
    /// مهلت رزروهای چسبیده به خطوط سفارش را برای شروع پرداخت آماده می‌کند.
    /// تکرارپذیر است: تمدید Inventory یکنوا است و هرگز مهلت را کوتاه نمی‌کند، و این درز هیچ حالتی در
    /// Order نمی‌نویسد.
    /// </summary>
    /// <param name="request">درخواست؛ بدون شناسهٔ رزرو یا مهلت از سمت فراخوان.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<OrderPaymentHoldPreparationResult> PrepareAsync(
        OrderPaymentHoldPreparationRequest request,
        CancellationToken cancellationToken);
}
