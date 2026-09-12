using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tooba.Media.Application;
using Tooba.Payment.Application;
using Tooba.Payment.Domain;
using Tooba.Payment.Infrastructure;
using Tooba.Wallet.Application;

namespace Tooba.Host.Storefront;

/// <summary>
/// ترکیب HTTP پرداخت فروشگاه روی IPaymentDirectory موجود. مبلغ از کلاینت پذیرفته نمی‌شود.
/// WALLET_MIXED_TENDER = DEFERRED — فقط پوشش کامل کیف پول مجاز است.
/// </summary>
public sealed class StorefrontPaymentComposer
{
    private readonly StorefrontCheckoutComposer _checkouts;
    private readonly IPaymentDirectory _payments;
    private readonly IOrderPaymentProjection _orderPayments;
    private readonly IWalletDirectory _wallets;
    private readonly PaymentGatewayOptions _gatewayOptions;
    private readonly CurrentAuthenticatedSession _session;
    private readonly IHostEnvironment _environment;
    private readonly IMediaDirectory _media;
    private readonly ILogger<StorefrontPaymentComposer> _logger;

    /// <summary>
    /// سازندهٔ ترکیب پرداخت ویترین.
    /// داخلی است چون <see cref="CurrentAuthenticatedSession"/> عمومی نیست؛ ثبت DI با کارخانه در Program انجام می‌شود.
    /// </summary>
    internal StorefrontPaymentComposer(
        StorefrontCheckoutComposer checkouts,
        IPaymentDirectory payments,
        IOrderPaymentProjection orderPayments,
        IWalletDirectory wallets,
        IOptions<PaymentGatewayOptions> gatewayOptions,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        IMediaDirectory media,
        ILogger<StorefrontPaymentComposer> logger)
    {
        _checkouts = checkouts;
        _payments = payments;
        _orderPayments = orderPayments;
        _wallets = wallets;
        _gatewayOptions = gatewayOptions.Value;
        _session = session;
        _environment = environment;
        _media = media;
        _logger = logger;
    }

    /// <summary>
    /// Actor پرداخت را با نشست احرازشده هم‌تراز می‌کند؛ مهمان همان GuestActor ثابت ویترین است.
    /// </summary>
    private Guid ResolvePaymentActor()
    {
        if (_session.IsAuthenticated && _session.UserId is Guid userId && userId != Guid.Empty)
        {
            return userId;
        }

        return StorefrontCheckoutComposer.StorefrontGuestActorId;
    }

    /// <summary>
    /// نقل قول موجودی کیف پول در برابر مبلغ قابل پرداخت checkout.
    /// </summary>
    public async Task<StorefrontWalletQuotePage> GetWalletQuoteAsync(
        Guid checkoutId,
        Guid cartId,
        string? guestSecret,
        CancellationToken cancellationToken)
    {
        var checkout = await _checkouts.GetAsync(checkoutId, cartId, guestSecret, cancellationToken)
            ?? throw new InvalidOperationException("سفارش پیدا نشد.");
        if (checkout.CheckoutId is null)
            throw new InvalidOperationException("سفارش پیدا نشد.");

        var actor = ResolvePaymentActor();
        var quote = await _wallets.QuoteForPayableAsync(
            actor,
            checkout.PayableAmount,
            checkout.Currency,
            cancellationToken);
        return new StorefrontWalletQuotePage(
            checkout.CheckoutId.Value,
            quote.WalletBalance,
            quote.MaxUsable,
            quote.RemainingPayable,
            quote.CanPayFullyWithWallet,
            quote.Currency,
            MixedTenderDeferred: true,
            ManualCardToCardEnabled: _gatewayOptions.ManualCardToCardEnabled);
    }

    /// <summary>
    /// روش‌های پرداخت فعال برای ویترین (پیکربندی فروشگاه/محیط).
    /// درگاه فقط وقتی Mode عملیاتی باشد (Sandbox یا Webhook پیکربندی‌شده)؛ Disabled/ناقص حذف می‌شود.
    /// </summary>
    public StorefrontPaymentMethodsPage ListPaymentMethods()
    {
        var methods = new List<StorefrontPaymentMethodOption>();
        if (IsOnlineGatewayOffered())
        {
            methods.Add(new("gateway", "درگاه بانکی", "پرداخت آنلاین از طریق درگاه"));
        }

        if (_gatewayOptions.ManualCardToCardEnabled)
        {
            methods.Add(new(
                ManualPaymentGateway.ProviderCodeValue,
                "کارت به کارت",
                "پرداخت دستی؛ سفارش پس از تأیید واریز توسط فروشگاه تکمیل می‌شود"));
        }

        return new StorefrontPaymentMethodsPage(
            methods,
            _gatewayOptions.ManualCardToCardEnabled,
            _gatewayOptions.NormalizedManualProofRequirement(),
            _gatewayOptions.ManualPaymentInstructions ?? string.Empty);
    }

    /// <summary>
    /// آیا شبیه‌ساز سندباکس در این محیط مجاز است؟ Production هرگز بله نیست.
    /// </summary>
    public bool IsSandboxSimulatorEnabled()
        => !_environment.IsProduction()
            && (_gatewayOptions.Mode ?? string.Empty).Trim().Equals("Sandbox", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// آیا درگاه آنلاین برای انتخاب مشتری در ویترین پیشنهاد می‌شود؟
    /// </summary>
    internal bool IsOnlineGatewayOffered()
    {
        var mode = (_gatewayOptions.Mode ?? string.Empty).Trim();
        if (mode.Equals("Disabled", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (mode.Equals("Sandbox", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (mode.Equals("Webhook", StringComparison.OrdinalIgnoreCase))
        {
            return !string.IsNullOrWhiteSpace(_gatewayOptions.InitiateBaseUrl)
                && !string.IsNullOrWhiteSpace(_gatewayOptions.WebhookSigningSecret);
        }

        return false;
    }

    /// <summary>
    /// پرداخت را برای سفارش PendingPayment شروع می‌کند. سفارش Paid دوباره شارژ نمی‌شود.
    /// </summary>
    public async Task<StorefrontPaymentInitiationPage> InitiateAsync(
        Guid checkoutId,
        Guid cartId,
        string? guestSecret,
        string idempotencyKey,
        bool useWallet,
        string? providerCodeOverride,
        CancellationToken cancellationToken)
    {
        var checkout = await _checkouts.GetAsync(checkoutId, cartId, guestSecret, cancellationToken)
            ?? throw new InvalidOperationException("سفارش پیدا نشد.");
        if (checkout.CheckoutId is null)
        {
            throw new InvalidOperationException("سفارش پیدا نشد.");
        }

        if (string.Equals(checkout.PaymentState, "Paid", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("این سفارش قبلاً پرداخت شده است.");
        }

        var providerCode = _gatewayOptions.DefaultProvider;
        var actor = ResolvePaymentActor();
        if (useWallet)
        {
            var quote = await _wallets.QuoteForPayableAsync(
                actor,
                checkout.PayableAmount,
                checkout.Currency,
                cancellationToken);
            if (!quote.CanPayFullyWithWallet || quote.RemainingPayable > 0)
            {
                throw new InvalidOperationException(
                    "پرداخت ترکیبی کیف پول و درگاه هنوز فعال نیست؛ موجودی باید کل مبلغ را پوشش دهد.");
            }

            providerCode = WalletPaymentGateway.ProviderCodeValue;
        }
        else if (!string.IsNullOrWhiteSpace(providerCodeOverride)
            && ManualPaymentGateway.IsManual(providerCodeOverride))
        {
            if (!_gatewayOptions.ManualCardToCardEnabled)
            {
                throw new InvalidOperationException("پرداخت کارت به کارت در این فروشگاه فعال نیست.");
            }

            providerCode = ManualPaymentGateway.ProviderCodeValue;
        }
        else if (!string.IsNullOrWhiteSpace(providerCodeOverride)
            && providerCodeOverride.Trim().Equals("gateway", StringComparison.OrdinalIgnoreCase))
        {
            if (!IsOnlineGatewayOffered())
            {
                throw new InvalidOperationException("payment.method.unavailable");
            }

            providerCode = _gatewayOptions.DefaultProvider;
        }
        else if (!useWallet && string.IsNullOrWhiteSpace(providerCodeOverride))
        {
            // پیش‌فرض درگاه — فقط وقتی در کاتالوگ فروشگاه پیشنهاد شده باشد.
            if (!IsOnlineGatewayOffered())
            {
                throw new InvalidOperationException("payment.method.unavailable");
            }
        }

        var initiated = await _payments.InitiateAsync(
            new InitiatePaymentCommand(
                checkout.CheckoutId.Value,
                actor,
                null,
                idempotencyKey,
                providerCode),
            cancellationToken);

        // مسیر full-wallet: Verify بلافاصله؛ بدون redirect به sandbox/PSP.
        if (string.Equals(initiated.ProviderCode, WalletPaymentGateway.ProviderCodeValue, StringComparison.OrdinalIgnoreCase)
            && initiated.Status != Tooba.Payment.Domain.PaymentStatus.Succeeded)
        {
            var verified = await _payments.VerifyAsync(
                new VerifyPaymentCommand(
                    initiated.PaymentId,
                    initiated.AttemptId,
                    initiated.ProviderRequestReference,
                    true),
                cancellationToken);
            _logger.LogInformation(
                "Storefront wallet payment verified immediately. CheckoutId={CheckoutId} PaymentId={PaymentId} Status={Status} NewlySucceeded={NewlySucceeded}",
                checkout.CheckoutId.Value,
                verified.PaymentId,
                verified.Status,
                verified.NewlySucceeded);

            var after = await _payments.GetAsync(
                initiated.PaymentId,
                actor,
                null,
                cancellationToken) ?? throw new InvalidOperationException("پرداخت پیدا نشد.");

            return new StorefrontPaymentInitiationPage(
                after.PaymentId,
                initiated.AttemptId,
                checkout.CheckoutId.Value,
                after.Status.ToString(),
                after.ProviderCode,
                initiated.ProviderRequestReference,
                RedirectUrl: $"/payment/result?checkoutId={checkout.CheckoutId.Value:D}&paymentId={after.PaymentId:D}",
                after.Amount,
                after.Currency,
                RequiresPspRedirect: false);
        }

        if (ManualPaymentGateway.IsManual(initiated.ProviderCode))
        {
            return new StorefrontPaymentInitiationPage(
                initiated.PaymentId,
                initiated.AttemptId,
                checkout.CheckoutId.Value,
                initiated.Status.ToString(),
                initiated.ProviderCode,
                initiated.ProviderRequestReference,
                RedirectUrl: $"/payment/result?checkoutId={checkout.CheckoutId.Value:D}&paymentId={initiated.PaymentId:D}&awaitingManual=1",
                initiated.Amount,
                initiated.Currency,
                RequiresPspRedirect: false);
        }

        var redirect = string.IsNullOrWhiteSpace(initiated.RedirectUrl)
            ? "/payment/sandbox"
            : initiated.RedirectUrl;
        if (!redirect.Contains("checkoutId=", StringComparison.Ordinal))
        {
            redirect += (redirect.Contains('?', StringComparison.Ordinal) ? "&" : "?")
                + "checkoutId=" + checkout.CheckoutId.Value.ToString("D");
        }

        _logger.LogInformation(
            "Storefront payment initiated. CheckoutId={CheckoutId} PaymentId={PaymentId} Amount={Amount} Currency={Currency}",
            checkout.CheckoutId.Value,
            initiated.PaymentId,
            initiated.Amount,
            initiated.Currency);

        return new StorefrontPaymentInitiationPage(
            initiated.PaymentId,
            initiated.AttemptId,
            checkout.CheckoutId.Value,
            initiated.Status.ToString(),
            initiated.ProviderCode,
            initiated.ProviderRequestReference,
            redirect,
            initiated.Amount,
            initiated.Currency,
            RequiresPspRedirect: true);
    }

    /// <summary>
    /// تصویر پرداخت را پس از مالکیت سفارش/پرداخت برمی‌گرداند.
    /// پس از نهایی‌شدن سبد، به سبد فعال جدید وابسته نیست (R4).
    /// cartId اختیاری/سازگاری است؛ مالکیت مهمان با راز روی Cart متعهد سفارش است.
    /// </summary>
    public async Task<StorefrontPaymentPage?> GetAsync(
        Guid paymentId,
        Guid cartId,
        string? guestSecret,
        CancellationToken cancellationToken)
    {
        _ = cartId;
        var actor = ResolvePaymentActor();
        var payment = await _payments.GetAsync(
            paymentId,
            actor,
            null,
            cancellationToken);
        if (payment is null)
        {
            return null;
        }

        var checkout = await _checkouts.GetOwnedForPaymentResultAsync(
            payment.CheckoutId,
            guestSecret,
            cancellationToken);
        if (checkout is null)
        {
            throw new InvalidOperationException("دسترسی به پرداخت بدون هویت سفارش رد شد.");
        }

        return MapPaymentPage(payment, checkout);
    }

    /// <summary>
    /// زمینهٔ شبیه‌ساز سندباکس. Production هرگز صفحه/اقدام جعلی PSP ندارد.
    /// </summary>
    public async Task<StorefrontSandboxContextPage> GetSandboxContextAsync(
        Guid paymentId,
        Guid cartId,
        string? guestSecret,
        CancellationToken cancellationToken)
    {
        if (!IsSandboxSimulatorEnabled())
        {
            throw new InvalidOperationException("payment.sandbox.unavailable");
        }

        var payment = await GetAsync(paymentId, cartId, guestSecret, cancellationToken)
            ?? throw new InvalidOperationException("پرداخت پیدا نشد.");
        var checkout = await _checkouts.GetAsync(payment.CheckoutId, cartId, guestSecret, cancellationToken)
            ?? throw new InvalidOperationException("سفارش پیدا نشد.");
        var orderNumber = checkout.SellerOrders.Select(x => x.OrderNumber).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
            ?? checkout.CheckoutId?.ToString("N")[..12]
            ?? "—";
        return new StorefrontSandboxContextPage(
            payment.PaymentId,
            payment.CheckoutId,
            string.IsNullOrWhiteSpace(_gatewayOptions.StoreDisplayName) ? "Tooba" : _gatewayOptions.StoreDisplayName,
            orderNumber,
            payment.Amount,
            payment.Currency,
            "درگاه بانکی (آزمایشی)",
            Sandbox: true);
    }

    /// <summary>
    /// نتیجهٔ sandbox/dev را به Verify سمت سرور می‌سپارد. متن Outcome حقیقت Paid نیست.
    /// </summary>
    public async Task<StorefrontPaymentPage> CompleteSandboxAsync(
        Guid paymentId,
        Guid cartId,
        string? guestSecret,
        Guid attemptId,
        string providerRequestReference,
        string outcome,
        CancellationToken cancellationToken)
    {
        if (!IsSandboxSimulatorEnabled())
        {
            throw new InvalidOperationException("payment.sandbox.unavailable");
        }

        var before = await GetAsync(paymentId, cartId, guestSecret, cancellationToken)
            ?? throw new InvalidOperationException("پرداخت پیدا نشد.");

        var successRequested = string.Equals(outcome, "success", StringComparison.OrdinalIgnoreCase);
        if (!successRequested)
        {
            FakePaymentGateway.SandboxDeclinedReferences[providerRequestReference] = 1;
        }

        var verified = await _payments.VerifyAsync(
            new VerifyPaymentCommand(paymentId, attemptId, providerRequestReference, successRequested),
            cancellationToken);

        _logger.LogInformation(
            "Storefront sandbox payment verified. PaymentId={PaymentId} Status={Status} NewlySucceeded={NewlySucceeded} Duplicate={Duplicate}",
            verified.PaymentId,
            verified.Status,
            verified.NewlySucceeded,
            !verified.NewlySucceeded && string.Equals(verified.Status.ToString(), "Succeeded", StringComparison.Ordinal));

        var after = await GetAsync(paymentId, cartId, guestSecret, cancellationToken)
            ?? throw new InvalidOperationException("پرداخت پیدا نشد.");
        _ = before;
        return after;
    }

    /// <summary>
    /// آپلود مدرک واریز با سیاست Media موجود و اتصال به همان پرداخت.
    /// </summary>
    public async Task<Guid> UploadManualProofAsync(
        Guid paymentId,
        Guid cartId,
        string? guestSecret,
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        _ = await GetAsync(paymentId, cartId, guestSecret, cancellationToken)
            ?? throw new InvalidOperationException("پرداخت پیدا نشد.");
        var actor = ResolvePaymentActor();
        var asset = await _media.UploadAsync(stream, fileName, contentType, actor, cancellationToken);
        await _payments.RegisterProofAssetAsync(paymentId, actor, null, asset.MediaAssetId, cancellationToken);
        return asset.MediaAssetId;
    }

    /// <summary>
    /// ثبت شماره پیگیری کارت‌به‌کارت. Succeeded نمی‌سازد.
    /// </summary>
    public async Task<StorefrontPaymentPage> SubmitManualEvidenceAsync(
        Guid paymentId,
        Guid cartId,
        string? guestSecret,
        string transferReference,
        Guid? proofMediaAssetId,
        CancellationToken cancellationToken)
    {
        _ = await GetAsync(paymentId, cartId, guestSecret, cancellationToken)
            ?? throw new InvalidOperationException("پرداخت پیدا نشد.");
        var requirement = _gatewayOptions.NormalizedManualProofRequirement();
        if (requirement.Equals("Required", StringComparison.OrdinalIgnoreCase) && proofMediaAssetId is null)
        {
            throw new InvalidOperationException("payment.proof.required");
        }

        if (requirement.Equals("Disabled", StringComparison.OrdinalIgnoreCase))
        {
            proofMediaAssetId = null;
        }

        await _payments.SubmitManualEvidenceAsync(
            paymentId,
            ResolvePaymentActor(),
            null,
            transferReference,
            proofMediaAssetId,
            cancellationToken);

        var hours = Math.Clamp(_gatewayOptions.ManualPaymentReviewHoldHours, 1, 24 * 30);
        var reviewExpiresAt = DateTimeOffset.UtcNow.AddHours(hours);
        var payment = await _payments.GetAsync(paymentId, ResolvePaymentActor(), null, cancellationToken)
            ?? throw new InvalidOperationException("پرداخت پیدا نشد.");
        await _orderPayments.PromoteReservationsForManualPaymentReviewAsync(
            payment.CheckoutId,
            reviewExpiresAt,
            cancellationToken);

        return await GetAsync(paymentId, cartId, guestSecret, cancellationToken)
            ?? throw new InvalidOperationException("پرداخت پیدا نشد.");
    }

    /// <summary>
    /// تلاش مجدد پس از رد ادمین روی همان سفارش/پرداخت.
    /// </summary>
    public async Task<StorefrontPaymentPage> RetryManualAsync(
        Guid paymentId,
        Guid cartId,
        string? guestSecret,
        CancellationToken cancellationToken)
    {
        _ = await GetAsync(paymentId, cartId, guestSecret, cancellationToken)
            ?? throw new InvalidOperationException("پرداخت پیدا نشد.");
        await _payments.RetryManualAfterRejectionAsync(paymentId, ResolvePaymentActor(), null, cancellationToken);
        return await GetAsync(paymentId, cartId, guestSecret, cancellationToken)
            ?? throw new InvalidOperationException("پرداخت پیدا نشد.");
    }

    private StorefrontPaymentPage MapPaymentPage(PaymentSnapshot payment, StorefrontCheckoutPage checkout)
    {
        var orderNumber = checkout.SellerOrders.Select(x => x.OrderNumber).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
        var history = (payment.EvidenceHistory ?? [])
            .Select(x => new StorefrontManualEvidenceHistoryItem(
                x.AttemptId,
                x.AttemptStatus.ToString(),
                x.CustomerTransferReference,
                x.ProofMediaAssetId,
                x.EvidenceSubmittedAt,
                x.FailureCode))
            .ToArray();
        var manual = ManualPaymentGateway.IsManual(payment.ProviderCode);
        var canSubmit = manual
            && payment.Status == PaymentStatus.Pending
            && payment.EvidenceSubmittedAt is null;
        var canRetry = manual && payment.Status == PaymentStatus.Failed;
        return new StorefrontPaymentPage(
            payment.PaymentId,
            payment.CheckoutId,
            payment.Amount,
            payment.Currency,
            payment.Status.ToString(),
            payment.ProviderCode,
            payment.Allocations
                .Select(x => new StorefrontPaymentAllocationView(
                    x.SellerOrderId,
                    x.AllocatedAmount,
                    x.Currency,
                    x.TargetKind.ToString()))
                .ToArray(),
            payment.CustomerTransferReference,
            payment.ProofMediaAssetId,
            payment.EvidenceSubmittedAt,
            orderNumber,
            _gatewayOptions.NormalizedManualProofRequirement(),
            _gatewayOptions.ManualPaymentInstructions ?? string.Empty,
            canSubmit,
            canRetry,
            history);
    }
}
