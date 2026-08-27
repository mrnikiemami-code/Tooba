using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tooba.Payment.Application;
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
    private readonly IWalletDirectory _wallets;
    private readonly PaymentGatewayOptions _gatewayOptions;
    private readonly ILogger<StorefrontPaymentComposer> _logger;

    /// <summary>
    /// سازندهٔ ترکیب پرداخت ویترین.
    /// </summary>
    public StorefrontPaymentComposer(
        StorefrontCheckoutComposer checkouts,
        IPaymentDirectory payments,
        IWalletDirectory wallets,
        IOptions<PaymentGatewayOptions> gatewayOptions,
        ILogger<StorefrontPaymentComposer> logger)
    {
        _checkouts = checkouts;
        _payments = payments;
        _wallets = wallets;
        _gatewayOptions = gatewayOptions.Value;
        _logger = logger;
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

        var quote = await _wallets.QuoteForPayableAsync(
            StorefrontCheckoutComposer.StorefrontGuestActorId,
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
            MixedTenderDeferred: true);
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
        if (useWallet)
        {
            var quote = await _wallets.QuoteForPayableAsync(
                StorefrontCheckoutComposer.StorefrontGuestActorId,
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

        var initiated = await _payments.InitiateAsync(
            new InitiatePaymentCommand(
                checkout.CheckoutId.Value,
                StorefrontCheckoutComposer.StorefrontGuestActorId,
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
                StorefrontCheckoutComposer.StorefrontGuestActorId,
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
    /// تصویر پرداخت را پس از اثبات مالکیت سبد/سفارش برمی‌گرداند.
    /// </summary>
    public async Task<StorefrontPaymentPage?> GetAsync(
        Guid paymentId,
        Guid cartId,
        string? guestSecret,
        CancellationToken cancellationToken)
    {
        var payment = await _payments.GetAsync(
            paymentId,
            StorefrontCheckoutComposer.StorefrontGuestActorId,
            null,
            cancellationToken);
        if (payment is null)
        {
            return null;
        }

        var checkout = await _checkouts.GetAsync(payment.CheckoutId, cartId, guestSecret, cancellationToken);
        if (checkout is null)
        {
            throw new InvalidOperationException("دسترسی به پرداخت بدون هویت سفارش رد شد.");
        }

        return new StorefrontPaymentPage(
            payment.PaymentId,
            payment.CheckoutId,
            payment.Amount,
            payment.Currency,
            payment.Status.ToString(),
            payment.ProviderCode,
            payment.Allocations
                .Select(x => new StorefrontPaymentAllocationView(x.SellerOrderId, x.AllocatedAmount, x.Currency))
                .ToArray());
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
}
