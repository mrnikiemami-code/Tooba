using Tooba.BuildingBlocks;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Contracts.Payments;
using Tooba.Order.Contracts.Storefront;
using Tooba.Payment.Contracts.Storefront;

namespace Tooba.Order.Application.Storefront.Services;

/// <summary>درخواست «کلیک نهایی پرداخت» آدرس‌داده‌شده با سبد.</summary>
/// <param name="CartId">شناسهٔ سبد فعال؛ تنها مرجع آدرس‌دهی فراخوان است.</param>
/// <param name="GuestSecret">گواه مالکیت سبد مهمان (فقط حالت مهمان).</param>
/// <param name="ExpectedCartVersion">نسخهٔ سبد که فراخوان دیده است (قبل از commit اعتبارسنجی می‌شود).</param>
/// <param name="IdempotencyKey">
/// کلید پایدارِ فراخوان. کلید commit و کلید Payment هر دو با namespace صریح از همین مقدار مشتق می‌شوند،
/// پس کلیک مجدد/پاسخ گم‌شده همان ord/payment را replay می‌کند و دوباره پول جابه‌جا نمی‌شود.
/// </param>
/// <param name="UseWallet">پرداخت کامل با کیف پول (روش انتخاب‌شده).</param>
/// <param name="ProviderCode">
/// انتخاب‌گر روش پرداخت (کد درگاه یا <c>manual</c>). خالی یعنی روش مشخصی انتخاب نشده؛ این درز روش
/// جایگزین نمی‌سازد و «آماده‌سازی» با <c>PolicyUnavailable</c> در امن بسته می‌شود.
/// </param>
/// <param name="AuthenticatedUserId">کاربر احراز‌هویت‌شده در صورت وجود.</param>
/// <param name="CouponCode">کد تخفیف اختیاری (بدون اعتبارسنجی تازه در این لایه).</param>
public sealed record StorefrontStartPaymentRequest(
    Guid CartId,
    string? GuestSecret,
    int ExpectedCartVersion,
    string IdempotencyKey,
    bool UseWallet,
    string? ProviderCode,
    Guid? AuthenticatedUserId,
    string? CouponCode = null);

/// <summary>دلیل رد درخواست پیش از هر عملیات پولی (بدون کد خطای ساختگی).</summary>
public enum StorefrontStartPaymentFault
{
    /// <summary>رد نشد.</summary>
    None = 0,

    /// <summary>CartId غایب/خالی است.</summary>
    CartRequired = 1,

    /// <summary>کلید idempotency غایب است.</summary>
    IdempotencyKeyRequired = 2,

    /// <summary>کلید + namespace از سقف ستون‌های idempotency (۱۲۸) بلندتر می‌شود.</summary>
    IdempotencyKeyTooLong = 3,
}

/// <summary>سرنوشت کلیک نهایی؛ همهٔ حالت‌ها امن‌بسته‌اند جز <see cref="Initiated"/>.</summary>
public enum StorefrontStartPaymentOutcome
{
    /// <summary>checkout commit/replay شد، مهلت رزرو آماده شد و پرداخت انتخاب‌شده یک‌بار شروع شد.</summary>
    Initiated = 0,

    /// <summary>درخواست ناقص/مبهم است؛ هیچ سفارش، مهلت یا پرداختی لمس نشد.</summary>
    InvalidRequest = 1,

    /// <summary>
    /// مرحلهٔ commit/replay شکست خورد. سبد Active باقی می‌ماند، هیچ مسیر پولی اجرا نشد و کد شکست
    /// (<see cref="StorefrontStartPaymentResult.FailureCode"/>) همان کد تایپ‌شدهٔ مالکِ خطاست و ساخته نمی‌شود.
    /// </summary>
    CheckoutCommitFailed = 2,

    /// <summary>
    /// checkout پایدار است اما آماده‌سازی ۰۱۸ «Prepared» نشد؛ مسیر پولی اجرا نشد و سفارش موجود
    /// قابل‌بازیابی می‌ماند (بدون commit دوباره و بدون لغو خودکار).
    /// </summary>
    HoldPreparationFailed = 3,
}

/// <summary>نتیجهٔ کلیک نهایی. هر حالت با دلیل تایپ‌شده و مالک‌دار گزارش می‌شود؛ خطای عمومی ساخته نمی‌شود.</summary>
/// <param name="Outcome">سرنوشت کلیک.</param>
/// <param name="Fault">دلیل رد درخواست وقتی <see cref="StorefrontStartPaymentOutcome.InvalidRequest"/>.</param>
/// <param name="CheckoutId">شناسهٔ checkout پایدار (در صورت پایدار‌شدن).</param>
/// <param name="CheckoutReplayed">آیا checkout موجود بازیابی شد (بدون commit دوباره).</param>
/// <param name="CommitIdempotencyKey">کلید مشتق‌شدهٔ namespace «commit» (پایدار و تکرارپذیر).</param>
/// <param name="PaymentIdempotencyKey">کلید مشتق‌شدهٔ namespace «payment» (پایدار و تکرارپذیر).</param>
/// <param name="FailureCode">کد تایپ‌شدهٔ تولیدکنندهٔ خطا در مرحلهٔ commit/replay؛ هیچ‌وقت بازنویسی نمی‌شود.</param>
/// <param name="PreparationOutcome">سرنوشت ۰۱۸ وقتی <see cref="StorefrontStartPaymentOutcome.HoldPreparationFailed"/>.</param>
/// <param name="ReservationCount">تعداد رزروهای اثبات/تمدید‌شدهٔ خطوط سفارش.</param>
/// <param name="RequestedDeadlineUtc">مهلت درخواست‌شده از سیاست مؤثر روش انتخاب‌شده.</param>
/// <param name="GuaranteedUntilUtc">ضعیف‌ترین مهلت نهایی متناهی رزروها (null = durable).</param>
/// <param name="UnavailableReservationIds">رزروهایی که Inventory اثبات نکرد (همه‌یا‌هیچ).</param>
/// <param name="PaymentId">شناسهٔ پرداخت پایدار (فقط <see cref="StorefrontStartPaymentOutcome.Initiated"/>).</param>
/// <param name="AttemptId">شناسهٔ تلاش درگاه.</param>
/// <param name="PaymentStatus">وضعیت پایدار پرداخت.</param>
/// <param name="ProviderCode">روش پایدار‌شدهٔ پرداخت (تصمیم Payment، نه این درز).</param>
/// <param name="RedirectUrl">مقصد هدایت درگاه.</param>
/// <param name="Amount">مبلغ پایدار‌شده (از پرداخت/سفارش، نه از فراخوان).</param>
/// <param name="Currency">ارز پایدار‌شده.</param>
/// <param name="RequiresPspRedirect">آیا هدایت به درگاه لازم است.</param>
public sealed record StorefrontStartPaymentResult(
    StorefrontStartPaymentOutcome Outcome,
    StorefrontStartPaymentFault Fault,
    Guid? CheckoutId,
    bool CheckoutReplayed,
    string CommitIdempotencyKey,
    string PaymentIdempotencyKey,
    string? FailureCode,
    OrderPaymentHoldPreparationOutcome? PreparationOutcome,
    int ReservationCount,
    DateTimeOffset? RequestedDeadlineUtc,
    DateTimeOffset? GuaranteedUntilUtc,
    IReadOnlyList<Guid> UnavailableReservationIds,
    Guid? PaymentId,
    Guid? AttemptId,
    string? PaymentStatus,
    string? ProviderCode,
    string? RedirectUrl,
    decimal Amount,
    string Currency,
    bool RequiresPspRedirect)
{
    /// <summary>پرداخت واقعاً یک‌بار شروع شد.</summary>
    public bool Started => Outcome == StorefrontStartPaymentOutcome.Initiated;

    /// <summary>checkout پایدار شده و همان سفارش قابل‌بازیابی است.</summary>
    public bool CheckoutPersisted => CheckoutId is not null;
}

/// <summary>
/// ترکیب‌کنندهٔ «کلیک نهایی پرداخت» به‌مالکیت Order — درون‌فرآیندی و بدون endpoint.
///
/// ترتیب ثابت:
/// <list type="number">
/// <item>اعتبارسنجی نحوی درخواست (سبد + کلید idempotency + سقف طول با namespace)؛</item>
/// <item>تشخیص بازگشت پذیری: ردیاب پایدار فرآیند checkout با همان کلید commit؛ اگر submit قبلی موفق
/// بوده باشد فقط مالکیت همان checkout بازبررسی می‌شود و <b>هیچ commit دوباره‌ای انجام نمی‌شود</b>؛</item>
/// <item>در غیر این صورت همان مسیر commit متعارف Order
/// (<see cref="StorefrontShippingService.CommitAsync"/> → <c>ICheckoutDirectory.SubmitAsync</c>) دقیقاً یک‌بار؛
/// سبد، رزرو، سهمیه، تبدیل سبد و وضعیت چرخه هیچ‌کدام اینجا بازپیاده‌سازی نمی‌شوند؛</item>
/// <item>آماده‌سازی مهلت رزرو ۰۱۸ برای روش انتخاب‌شده؛ فقط <c>Prepared</c> اجازهٔ ادامه دارد؛</item>
/// <item>یک‌بار فراخوانی درز متعارف Payment (<see cref="IPaymentInitiationPort"/>) — بدون مسیر جانبی برای
/// کیف پول/درگاه/دستی.</item>
/// </list>
///
/// تضمین‌ها: «دقیقاً یک checkout برای یک سبد» با کلید commit پایدار و replay متعارف Order؛ همان کلید در
/// هر دو مرحله پایدار می‌ماند؛ شکست commit مسیر پولی را اجرا نمی‌کند؛ شکست آماده‌سازی/شروع، سفارش موجود را
/// دست‌نخورده و قابل‌بازیابی می‌گذارد.
///
/// تضمین‌نشده‌ها: هیچ ACID بین Order و Inventory ادعا نمی‌شود؛ بازهٔ بین تمدید مهلت و فراخوانی درگاه
/// (و کال‌بک دیرهنگام) بسته نمی‌شود؛ مهلت‌های ستون‌های idempotency یکسان فرض نمی‌شوند بلکه صریحاً بررسی می‌شوند؛
/// اگر انتخاب‌گر روش «درگاه پیش‌فرض» باشد، Provider نهایی داخل Payment انتخاب می‌شود، پس سیاست مهلت
/// استفاده‌شده در این درز لایهٔ store/platform است و override همان Provider نهایی نیست.
///
/// خطاها: شکست commit/replay با کد تایپ‌شدهٔ خودِ تولیدکننده و بدون بازنویسی گزارش می‌شود؛ شکست
/// آماده‌سازی با سرنوشت تایپ‌شدهٔ ۰۱۸ گزارش می‌شود؛ خطاهای Payment دست‌نخورده propagate می‌شوند تا هیچ کد
/// Payment ساخته یا جعل نشود و پیام عمومی ۵۰۰ تولید نشود.
/// </summary>
public sealed class StorefrontStartPaymentComposer
{
    /// <summary>namespace کلید commit (ستون order.checkouts.idempotency_key).</summary>
    public const string CommitKeyPrefix = "start-payment:commit:";

    /// <summary>namespace کلید Payment (ستون payment.payments.idempotency_key).</summary>
    public const string PaymentKeyPrefix = "start-payment:payment:";

    /// <summary>
    /// سقف مشترک ستون‌های idempotency در سه تولیدکننده: order.checkouts.idempotency_key (128)،
    /// order.checkout_processes.submission_idempotency_key (128)، payment.payments.idempotency_key (128).
    /// </summary>
    private const int SharedIdempotencyKeyMaxLength = 128;

    private readonly StorefrontShippingService _shipping;
    private readonly StorefrontCheckoutService _checkouts;
    private readonly IOrderPaymentHoldPreparationPort _preparation;
    private readonly IPaymentInitiationPort _payments;
    private readonly ICheckoutProcessTracker? _processes;

    /// <summary>ترکیب‌کننده را به روزنه‌های متعارف commit، مالکیت، آماده‌سازی ۰۱۸ و شروع Payment وصل می‌کند.</summary>
    /// <param name="shipping">مسیر commit متعارف Order (انتخاب ارسال + <c>ICheckoutDirectory.SubmitAsync</c>).</param>
    /// <param name="checkouts">قاعدهٔ موجود مالکیت checkout برای مسیر بازگشت پذیری.</param>
    /// <param name="preparation">درز آماده‌سازی مهلت ۰۱۸.</param>
    /// <param name="payments">درز Contracts-only شروع پرداخت ۰۱۲R.</param>
    /// <param name="processes">ردیاب پایدار فرآیند checkout؛ اگر غایب باشد فقط replay متعارف commit باقی می‌ماند.</param>
    public StorefrontStartPaymentComposer(
        StorefrontShippingService shipping,
        StorefrontCheckoutService checkouts,
        IOrderPaymentHoldPreparationPort preparation,
        IPaymentInitiationPort payments,
        ICheckoutProcessTracker? processes = null)
    {
        _shipping = shipping;
        _checkouts = checkouts;
        _preparation = preparation;
        _payments = payments;
        _processes = processes;
    }

    /// <summary>
    /// یک کلیک نهایی را ترکیب می‌کند. این متد هیچ endpoint یا عمل پولی خارج از درز متعارف Payment ندارد و
    /// هیچ حالتی در Order نمی‌نویسد (commit خودش متعارف و idempotent است).
    /// </summary>
    /// <param name="request">درخواست آدرس‌داده‌شده با سبد؛ بدون شناسهٔ رزرو/مبلغ/checkout ساختگی.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    public async Task<StorefrontStartPaymentResult> StartPaymentAsync(
        StorefrontStartPaymentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.CartId == Guid.Empty)
        {
            return Reject(StorefrontStartPaymentFault.CartRequired);
        }

        var rawKey = request.IdempotencyKey?.Trim() ?? string.Empty;
        if (rawKey.Length == 0)
        {
            return Reject(StorefrontStartPaymentFault.IdempotencyKeyRequired);
        }

        if (CommitKeyPrefix.Length + rawKey.Length > SharedIdempotencyKeyMaxLength
            || PaymentKeyPrefix.Length + rawKey.Length > SharedIdempotencyKeyMaxLength)
        {
            return Reject(StorefrontStartPaymentFault.IdempotencyKeyTooLong);
        }

        var commitKey = CommitKeyPrefix + rawKey;
        var paymentKey = PaymentKeyPrefix + rawKey;
        var methodSelector = request.UseWallet
            ? PaymentProviderCodes.Wallet
            : request.ProviderCode?.Trim() ?? string.Empty;

        // بازگشت پذیری پیش از هر commit: ردیاب پایدار با همان کلید commit.
        var replayed = false;
        Guid checkoutId;
        var prior = _processes is null
            ? null
            : await _processes.FindSucceededBySubmissionKeyAsync(commitKey, cancellationToken).ConfigureAwait(false);
        if (prior?.CheckoutId is Guid priorCheckoutId)
        {
            // مالکیت همان checkout با قاعدهٔ موجود Order بازبررسی می‌شود؛ هیچ commit دوباره‌ای رخ نمی‌دهد.
            var owned = await _checkouts
                .GetOwnedForPaymentResultAsync(priorCheckoutId, request.GuestSecret, cancellationToken)
                .ConfigureAwait(false);
            if (owned is null)
            {
                return CommitFailed(StorefrontOrderErrors.CheckoutAccessDenied, null, replayed: false, commitKey, paymentKey);
            }

            checkoutId = priorCheckoutId;
            replayed = true;
        }
        else
        {
            StorefrontCheckoutPage page;
            try
            {
                page = await _shipping.CommitAsync(
                    request.CartId,
                    request.GuestSecret,
                    request.ExpectedCartVersion,
                    commitKey,
                    request.CouponCode,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (StorefrontOrderException exception)
            {
                return CommitFailed(exception.Code, null, replayed: false, commitKey, paymentKey);
            }
            catch (ContractOperationException exception)
            {
                // کد تایپ‌شدهٔ تولیدکنندهٔ خطا (Order/Inventory) بدون بازنویسی گزارش می‌شود.
                return CommitFailed(exception.Code, null, replayed: false, commitKey, paymentKey);
            }
            catch (SemanticException exception)
            {
                return CommitFailed(exception.Error.Code, null, replayed: false, commitKey, paymentKey);
            }

            if (page.CheckoutId is not Guid committedId || committedId == Guid.Empty)
            {
                return CommitFailed(StorefrontOrderErrors.CheckoutRejected, null, replayed: false, commitKey, paymentKey);
            }

            checkoutId = committedId;
        }

        // آماده‌سازی ۰۱۸: مالکیت/خطوط پایدار/چرخه/سیاست/تمدید اتمی. فقط Prepared اجازهٔ ادامه دارد.
        var preparation = await _preparation.PrepareAsync(
            new OrderPaymentHoldPreparationRequest(
                checkoutId,
                request.CartId,
                request.GuestSecret,
                request.AuthenticatedUserId,
                methodSelector),
            cancellationToken).ConfigureAwait(false);
        if (!preparation.Prepared)
        {
            return new StorefrontStartPaymentResult(
                StorefrontStartPaymentOutcome.HoldPreparationFailed,
                StorefrontStartPaymentFault.None,
                checkoutId,
                replayed,
                commitKey,
                paymentKey,
                FailureCode: null,
                preparation.Outcome,
                preparation.ReservationCount,
                preparation.RequestedDeadlineUtc,
                preparation.GuaranteedUntilUtc,
                preparation.UnavailableReservationIds,
                PaymentId: null,
                AttemptId: null,
                PaymentStatus: null,
                ProviderCode: null,
                RedirectUrl: null,
                Amount: 0m,
                Currency: string.Empty,
                RequiresPspRedirect: false);
        }

        // مسیر متعارف Payment. خطاهای Payment دست‌نخورده propagate می‌شوند (بدون کد ساختگی).
        var initiated = await _payments.InitiateForCheckoutAsync(
            new StorefrontPaymentInitiationRequest(
                checkoutId,
                request.CartId,
                request.GuestSecret,
                paymentKey,
                request.UseWallet,
                request.ProviderCode,
                request.AuthenticatedUserId),
            cancellationToken).ConfigureAwait(false);

        return new StorefrontStartPaymentResult(
            StorefrontStartPaymentOutcome.Initiated,
            StorefrontStartPaymentFault.None,
            initiated.CheckoutId == Guid.Empty ? checkoutId : initiated.CheckoutId,
            replayed,
            commitKey,
            paymentKey,
            FailureCode: null,
            preparation.Outcome,
            preparation.ReservationCount,
            preparation.RequestedDeadlineUtc,
            preparation.GuaranteedUntilUtc,
            preparation.UnavailableReservationIds,
            initiated.PaymentId,
            initiated.AttemptId,
            initiated.Status,
            initiated.ProviderCode,
            initiated.RedirectUrl,
            initiated.Amount,
            initiated.Currency,
            initiated.RequiresPspRedirect);
    }

    private static StorefrontStartPaymentResult Reject(StorefrontStartPaymentFault fault) =>
        new(
            StorefrontStartPaymentOutcome.InvalidRequest,
            fault,
            CheckoutId: null,
            CheckoutReplayed: false,
            CommitIdempotencyKey: string.Empty,
            PaymentIdempotencyKey: string.Empty,
            FailureCode: null,
            PreparationOutcome: null,
            ReservationCount: 0,
            RequestedDeadlineUtc: null,
            GuaranteedUntilUtc: null,
            UnavailableReservationIds: [],
            PaymentId: null,
            AttemptId: null,
            PaymentStatus: null,
            ProviderCode: null,
            RedirectUrl: null,
            Amount: 0m,
            Currency: string.Empty,
            RequiresPspRedirect: false);

    private static StorefrontStartPaymentResult CommitFailed(
        string failureCode,
        Guid? checkoutId,
        bool replayed,
        string commitKey,
        string paymentKey) =>
        new(
            StorefrontStartPaymentOutcome.CheckoutCommitFailed,
            StorefrontStartPaymentFault.None,
            checkoutId,
            replayed,
            commitKey,
            paymentKey,
            failureCode,
            PreparationOutcome: null,
            ReservationCount: 0,
            RequestedDeadlineUtc: null,
            GuaranteedUntilUtc: null,
            UnavailableReservationIds: [],
            PaymentId: null,
            AttemptId: null,
            PaymentStatus: null,
            ProviderCode: null,
            RedirectUrl: null,
            Amount: 0m,
            Currency: string.Empty,
            RequiresPspRedirect: false);
}
