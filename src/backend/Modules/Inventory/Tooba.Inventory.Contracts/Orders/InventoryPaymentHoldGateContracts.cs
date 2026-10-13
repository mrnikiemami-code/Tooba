namespace Tooba.Inventory.Contracts.Orders;

/// <summary>
/// سرنوشت پایش «قفل زندهٔ رزرو» در مرز ورود پول. سه حالت بسته و بدون حالت مبهم.
/// </summary>
public enum InventoryPaymentHoldGateOutcome
{
    /// <summary>همهٔ رزروهای خواسته‌شده، تحت قفل ردیف و با ساعت دیتابیس، Held و منقضی‌نشده اثبات شدند.</summary>
    Verified = 0,

    /// <summary>حداقل یک رزرو گم، Released، Consumed یا منقضی است؛ یا ورودی قابل اثبات نبود. در امن بسته است.</summary>
    NotHeld = 1,

    /// <summary>تراکنش محیطیِ مالک‌دار وجود ندارد یا enlistment تضمین‌شدنی نیست؛ در امن بسته است.</summary>
    AmbientTransactionRequired = 2,
}

/// <summary>
/// گواه نتیجهٔ درز قفل رزرو. <see cref="Verified"/> تنها حالت مجاز برای ادامهٔ ورود پول است؛
/// هر حالت دیگر یعنی «اثبات نشد» و باید مثل رد تلقی شود.
/// </summary>
/// <param name="Outcome">سرنوشت پایش.</param>
/// <param name="RequestedCount">تعداد رزروهای یکتا و متمایز درخواست‌شده.</param>
/// <param name="HeldCount">تعداد رزروهایی که در همان تراکنش زنده اثبات شدند.</param>
/// <param name="UnavailableReservationIds">زیرمجموعه‌ای از شناسه‌های درخواستی که زنده اثبات نشدند.</param>
public sealed record InventoryPaymentHoldGateResult(
    InventoryPaymentHoldGateOutcome Outcome,
    int RequestedCount,
    int HeldCount,
    IReadOnlyList<Guid> UnavailableReservationIds)
{
    /// <summary>فقط و فقط وقتی همهٔ رزروهای درخواستی زنده اثبات شده باشند درست است.</summary>
    public bool AllHeld => Outcome == InventoryPaymentHoldGateOutcome.Verified;

    /// <summary>گواه موفق برای مجموعهٔ درخواستی.</summary>
    public static InventoryPaymentHoldGateResult Verified(int requestedCount) =>
        new(InventoryPaymentHoldGateOutcome.Verified, requestedCount, requestedCount, []);

    /// <summary>گواه رد؛ هیچ رزروی زنده اثبات نشده است.</summary>
    public static InventoryPaymentHoldGateResult NotHeld(int requestedCount, IReadOnlyList<Guid> unavailableReservationIds)
    {
        ArgumentNullException.ThrowIfNull(unavailableReservationIds);
        return new(InventoryPaymentHoldGateOutcome.NotHeld, requestedCount, 0, unavailableReservationIds);
    }

    /// <summary>گواه «قفل محیطی قابل تضمین نیست»؛ مصرف‌کننده باید مانند رد رفتار کند.</summary>
    public static InventoryPaymentHoldGateResult AmbientTransactionRequired(int requestedCount) =>
        new(InventoryPaymentHoldGateOutcome.AmbientTransactionRequired, requestedCount, 0, []);
}

/// <summary>
/// درز مالکیت Inventory برای اثبات «هنوز می‌توان روی این رزروها پول گرفت».
///
/// قرارداد اجرا:
/// <list type="bullet">
/// <item>فراخوان باید خودش یک تراکنش محیطی (<c>System.Transactions.TransactionScope</c>) باز کرده باشد؛
/// Inventory در این درز هیچ تراکنش مستقلی باز نمی‌کند و اگر تراکنش محیطی نباشد یا enlistment قابل
/// تضمین نباشد، <see cref="InventoryPaymentHoldGateOutcome.AmbientTransactionRequired"/> برمی‌گردد (بستن امن).</item>
/// <item>رزروها به ترتیب قطعی و با قفل ردیف PostgreSQL (<c>FOR UPDATE</c>) خوانده می‌شوند؛ قفل‌ها در پایان
/// تراکنش فراخوان آزاد می‌شوند، نه در پایان این متد.</item>
/// <item>اعتبار زمانی با ساعت دیتابیس سنجیده می‌شود؛ هیچ زمان فراخوان‌داده‌شده پذیرفته نمی‌شود.
/// <c>expires_at</c> تهی یعنی رزرو بدون انقضای زمانی است.</item>
/// <item>این درز فقط حقیقت «همین حالا زنده است» را اثبات می‌کند. تنها نیست و به‌تنهایی از انقضای بعد از
/// commit جلوگیری نمی‌کند؛ هیچ فراخوانی PSP نباید داخل همان تراکنش انجام شود، چون فراخوانی بیرونی
/// تراکنش را طولانی و قفل ردیف را پرریسک می‌کند.</item>
/// <item>ورودی خالی یا شامل <see cref="Guid.Empty"/> هرگز Verified نمی‌شود.</item>
/// </list>
/// </summary>
public interface IInventoryPaymentHoldGate
{
    /// <summary>
    /// اثبات می‌کند هر یک از شناسه‌های متمایز رزرو، در همان تراکنش محیطی فراخوان و تحت قفل ردیف،
    /// همچنان Held و منقضی‌نشده است.
    /// </summary>
    /// <param name="reservationIds">شناسه‌های رزرو؛ تکراری‌ها نادیده گرفته می‌شوند و ترتیب اثر ندارد.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<InventoryPaymentHoldGateResult> VerifyHeldForPaymentAsync(
        IReadOnlyCollection<Guid> reservationIds,
        CancellationToken cancellationToken);
}
