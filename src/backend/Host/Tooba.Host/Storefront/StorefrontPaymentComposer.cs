using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tooba.Payment.Application;
using Tooba.Payment.Infrastructure;

namespace Tooba.Host.Storefront;

/// <summary>
/// ترکیب HTTP پرداخت فروشگاه روی IPaymentDirectory موجود. مبلغ از کلاینت پذیرفته نمی‌شود.
/// </summary>
public sealed class StorefrontPaymentComposer
{
    private readonly StorefrontCheckoutComposer _checkouts;
    private readonly IPaymentDirectory _payments;
    private readonly PaymentGatewayOptions _gatewayOptions;
    private readonly ILogger<StorefrontPaymentComposer> _logger;

    /// <summary>
    /// سازندهٔ ترکیب پرداخت ویترین.
    /// </summary>
    public StorefrontPaymentComposer(
        StorefrontCheckoutComposer checkouts,
        IPaymentDirectory payments,
        IOptions<PaymentGatewayOptions> gatewayOptions,
        ILogger<StorefrontPaymentComposer> logger)
    {
        _checkouts = checkouts;
        _payments = payments;
        _gatewayOptions = gatewayOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// پرداخت را برای سفارش PendingPayment شروع می‌کند. سفارش Paid دوباره شارژ نمی‌شود.
    /// </summary>
    public async Task<StorefrontPaymentInitiationPage> InitiateAsync(
        Guid checkoutId,
        Guid cartId,
        string? guestSecret,
        string idempotencyKey,
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

        var initiated = await _payments.InitiateAsync(
            new InitiatePaymentCommand(
                checkout.CheckoutId.Value,
                StorefrontCheckoutComposer.StorefrontGuestActorId,
                null,
                idempotencyKey,
                _gatewayOptions.DefaultProvider),
            cancellationToken);

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
            initiated.Currency);
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
