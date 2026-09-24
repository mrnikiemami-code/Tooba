using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.Payment.Application.Models;
using Tooba.Payment.Application.Ports;
using Tooba.Wallet.Contracts.Dtos;
using Tooba.Wallet.Contracts.Payments;

namespace Tooba.Payment.Infrastructure.Providers;

/// <summary>
/// درگاه کیف پول: ATOMIC_DEBIT_AT_PAID در Verify؛ بدون redirect به sandbox/PSP.
/// </summary>
public sealed class WalletPaymentGateway : IPaymentGateway
{
    /// <summary>کد پایدار درگاه.</summary>
    public const string ProviderCodeValue = "wallet";

    private readonly IWalletOrderPaymentPort _wallets;
    private readonly PaymentGatewayActorContext _actorContext;
    private readonly IClock _clock;
    private readonly IModuleCallTracer _tracer;

    /// <summary>درگاه کیف پول را به درز پرداخت Wallet وصل می‌کند.</summary>
    public WalletPaymentGateway(
        IWalletOrderPaymentPort wallets,
        PaymentGatewayActorContext actorContext,
        IClock clock,
        IModuleCallTracer tracer)
    {
        _wallets = wallets;
        _actorContext = actorContext;
        _clock = clock;
        _tracer = tracer;
    }

    /// <inheritdoc />
    public string ProviderCode => ProviderCodeValue;

    /// <inheritdoc />
    public async Task<GatewayInitiation> InitiateAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken)
    {
        var actorUserId = _actorContext.ActorUserId;
        if (actorUserId == Guid.Empty)
            throw new InvalidOperationException("payment.wallet.customer_required");
        if (amount <= 0)
            throw new InvalidOperationException("payment.wallet.amount_positive");

        using var quoteTrace = _tracer.Begin("payment", "wallet", "QuoteForPayable");
        var quote = await _wallets.QuoteForPayableAsync(actorUserId, amount, currency, cancellationToken).ConfigureAwait(false);
        if (!quote.CanPayFullyWithWallet)
            throw new InvalidOperationException("payment.wallet.insufficient_balance");

        var reference = ComposeReference(paymentId, actorUserId, amount, WalletCurrency.Normalize(currency));
        return new GatewayInitiation(reference, null, _clock.UtcNow.AddMinutes(15));
    }

    /// <inheritdoc />
    public async Task<GatewayVerification> VerifyAsync(
        string providerRequestReference,
        bool callbackClaimsSuccess,
        CancellationToken cancellationToken)
    {
        _ = callbackClaimsSuccess;
        if (!TryParseReference(providerRequestReference, out var paymentId, out var actorUserId, out var amount, out var currency))
        {
            return new GatewayVerification(false, null, "WALLET_REFERENCE_INVALID");
        }

        try
        {
            using var spendTrace = _tracer.Begin("payment", "wallet", "SpendForOrderPayment");
            await _wallets.SpendForOrderPaymentAsync(
                actorUserId,
                amount,
                currency,
                paymentId,
                $"wallet-order-debit:{paymentId:D}",
                cancellationToken).ConfigureAwait(false);
            return new GatewayVerification(true, $"wallet:{paymentId:D}", null);
        }
        catch (ContractOperationException)
        {
            // Expected Wallet order-payment rejection crosses the contract boundary typed; no Message classification.
            return new GatewayVerification(false, null, "WALLET_SPEND_REJECTED");
        }
    }

    /// <summary>مرجع درخواست درگاه را می‌سازد.</summary>
    public static string ComposeReference(Guid paymentId, Guid actorUserId, decimal amount, string currency) =>
        $"w|{paymentId:N}|{actorUserId:N}|{decimal.Round(amount, 0, MidpointRounding.AwayFromZero)}|{currency.Trim().ToUpperInvariant()}";

    /// <summary>مرجع را پارس می‌کند.</summary>
    public static bool TryParseReference(
        string reference,
        out Guid paymentId,
        out Guid actorUserId,
        out decimal amount,
        out string currency)
    {
        paymentId = Guid.Empty;
        actorUserId = Guid.Empty;
        amount = 0;
        currency = string.Empty;
        if (string.IsNullOrWhiteSpace(reference))
            return false;
        var parts = reference.Trim().Split('|');
        if (parts.Length != 5 || parts[0] != "w")
            return false;
        if (!Guid.TryParseExact(parts[1], "N", out paymentId))
            return false;
        if (!Guid.TryParseExact(parts[2], "N", out actorUserId))
            return false;
        if (!decimal.TryParse(parts[3], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out amount)
            || amount <= 0)
            return false;
        if (string.IsNullOrWhiteSpace(parts[4]) || parts[4].Length is < 3 or > 8)
            return false;
        currency = parts[4].Trim().ToUpperInvariant();
        return true;
    }
}
