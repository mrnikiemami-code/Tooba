namespace Tooba.Inventory.Contracts.Orders;

/// <summary>
/// سرنوشت تمدید اتمی مهلت رزروهای Held در مرز ورود پول. هیچ حالت مبهمی ندارد.
/// </summary>
public enum InventoryPaymentHoldExtensionOutcome
{
    /// <summary>
    /// همهٔ رزروهای خواسته‌شده، تحت قفل ردیف و با ساعت دیتابیس، Held و منقضی‌نشده بودند و مهلتشان
    /// در همان یک تراکنش Inventory به حداقل مهلت درخواستی رسید. رزروهای بدون انقضا (NULL) دست‌نخورده ماندند.
    /// </summary>
    Extended = 0,

    /// <summary>
    /// حداقل یک رزرو گم، Released، Consumed یا منقضی بود؛ یا شمار ردیف‌های به‌روزشده با درخواست
    /// نخواند. هیچ تغییری commit نشد.
    /// </summary>
    NotHeld = 1,

    /// <summary>مهلت درخواستی غایب/نامعتبر بود یا نسبت به ساعت دیتابیس گذشته بود؛ هیچ تغییری commit نشد.</summary>
    DeadlineInvalid = 2,

    /// <summary>
    /// فراخوان داخل یک تراکنش محیطی (<c>System.Transactions.TransactionScope</c>) اجرا شده است.
    /// این درز مالک تراکنش خودش است و تراکنش محیطی چند‌ماژولی را نمی‌پذیرد؛ در امن بسته است.
    /// </summary>
    AmbientTransactionUnsupported = 3,
}

/// <summary>
/// گواه تمدید مهلت. فقط <see cref="InventoryPaymentHoldExtensionOutcome.Extended"/> یعنی مهلت
/// همهٔ رزروهای خواسته‌شده رسماً تا <see cref="GuaranteedUntil"/> (یا بی‌نهایت برای رزرو بدون انقضا) تمدید شد.
/// </summary>
/// <param name="Outcome">سرنوشت تمدید.</param>
/// <param name="RequestedCount">تعداد رزروهای یکتا و متمایز خواسته‌شده.</param>
/// <param name="GuaranteedUntil">
/// دیرترین مهلتی که این فراخوانی برای کل مجموعه تضمین می‌کند: کمینهٔ مهلت‌های متناهیِ نهایی رزروها
/// (کمینه چون ضعیف‌ترین حلقهٔ مجموعه است). <c>null</c> یعنی همهٔ رزروها بدون انقضای زمانی (durable) هستند.
/// </param>
/// <param name="UnavailableReservationIds">زیرمجموعه‌ای از شناسه‌های خواسته‌شده که اثبات نشدند.</param>
public sealed record InventoryPaymentHoldExtensionResult(
    InventoryPaymentHoldExtensionOutcome Outcome,
    int RequestedCount,
    DateTimeOffset? GuaranteedUntil,
    IReadOnlyList<Guid> UnavailableReservationIds)
{
    /// <summary>فقط و فقط وقتی همهٔ رزروها اثبات و تمدید شده باشند درست است.</summary>
    public bool AllHeldAndExtended => Outcome == InventoryPaymentHoldExtensionOutcome.Extended;

    /// <summary>گواه موفق.</summary>
    public static InventoryPaymentHoldExtensionResult Extended(int requestedCount, DateTimeOffset? guaranteedUntil) =>
        new(InventoryPaymentHoldExtensionOutcome.Extended, requestedCount, guaranteedUntil, []);

    /// <summary>گواه «حداقل یک رزرو زنده نبود»؛ هیچ رزروی تغییر نکرده است.</summary>
    public static InventoryPaymentHoldExtensionResult NotHeld(
        int requestedCount,
        IReadOnlyList<Guid> unavailableReservationIds)
    {
        ArgumentNullException.ThrowIfNull(unavailableReservationIds);
        return new(InventoryPaymentHoldExtensionOutcome.NotHeld, requestedCount, null, unavailableReservationIds);
    }

    /// <summary>گواه «مهلت نامعتبر»؛ هیچ رزروی تغییر نکرده است.</summary>
    public static InventoryPaymentHoldExtensionResult DeadlineInvalid(int requestedCount) =>
        new(InventoryPaymentHoldExtensionOutcome.DeadlineInvalid, requestedCount, null, []);

    /// <summary>گواه «تراکنش محیطی پشتیبانی نمی‌شود»؛ هیچ رزروی تغییر نکرده است.</summary>
    public static InventoryPaymentHoldExtensionResult AmbientTransactionUnsupported(int requestedCount) =>
        new(InventoryPaymentHoldExtensionOutcome.AmbientTransactionUnsupported, requestedCount, null, []);
}

/// <summary>
/// درز مالکیت Inventory برای «اثبات و تمدید یک‌ضربِ» مهلت رزروهای Held پیش از ورود پول.
///
/// این یک بلوک سازنده است، نه مجوز پرداخت: هیچ PSP، هیچ برداشت کیف پول، هیچ initiation و هیچ
/// تغییر Order/Cart در این درز نیست. تنها یک تراکنش و یک اتصال محلی Inventory، و هیچ
/// <c>TransactionScope</c> چند‌ماژولی.
///
/// قرارداد اجرا:
/// <list type="bullet">
/// <item>ورودی خالی، رزرو گم/Released/Consumed/منقضی و <see cref="Guid.Empty"/> در امن بسته است:
/// همه‌یا‌هیچ؛ اگر یک رزرو اثبات نشود، هیچ ردیفی commit نمی‌شود.</item>
/// <item>اعتبار زمانی فقط با ساعت دیتابیس سنجیده می‌شود؛ مهلت باید مطلق و در آیندهٔ دیتابیس باشد.
/// مهلت گذشته یا غایب رد می‌شود.</item>
/// <item><c>expires_at IS NULL</c> یعنی رزرو پایدار (durable)؛ دست‌نخورده می‌ماند. مهلت‌های متناهی هرگز
/// کوتاه نمی‌شوند: نتیجهٔ هر ردیف <c>max(مهلت موجود، مهلت درخواستی)</c> است، پس replay یکنوا و بی‌خطر است.</item>
/// <item>رزرو Released/Expired هرگز زنده نمی‌شود.</item>
/// <item>این درز <b>مالکیت</b> شناسه‌ها را اثبات نمی‌کند: نمی‌گوید این رزروها به کدام checkout تعلق دارند.
/// Order باید شناسه‌ها را از خطوط پایدار خودش استخراج کند و مهلت را از سیاست پیکربندی‌شدهٔ مؤثر بدهد.</item>
/// <item>قفل ردیف‌ها در پایان همان فراخوانی و همان تراکنش آزاد می‌شود؛ پس از بازگشت متد هیچ قفلی
/// نگه داشته نمی‌شود.</item>
/// <item>این درز به‌تنهایی انقضای رزرو در طول عملیات PSP را حذف نمی‌کند و initiation را race-free
/// نمی‌کند؛ Order/Payment موظف‌اند پنجرهٔ مهلت را معنادار کنند و callback دیرهنگام/جبران را مدیریت کنند.</item>
/// </list>
/// </summary>
public interface IInventoryPaymentHoldExtensionPort
{
    /// <summary>
    /// مهلت همهٔ رزروهای Held خواسته‌شده را در یک تراکنش Inventory و به‌صورت همه‌یا‌هیچ تا مهلت
    /// درخواستی (یا مهلت موجود اگر دیرتر باشد) بالا می‌برد.
    /// </summary>
    /// <param name="reservationIds">شناسه‌های رزرو؛ تکراری‌ها و ترتیب بی‌اثرند.</param>
    /// <param name="payWindowDeadlineUtc">مهلت مطلقِ برگرفته از سیاست مؤثر؛ باید در آیندهٔ ساعت دیتابیس باشد.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<InventoryPaymentHoldExtensionResult> ExtendHeldForPaymentAsync(
        IReadOnlyCollection<Guid> reservationIds,
        DateTimeOffset payWindowDeadlineUtc,
        CancellationToken cancellationToken);
}
