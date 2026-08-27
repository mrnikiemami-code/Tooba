using Tooba.Payment.Application;
using Tooba.Wallet.Application;
using Tooba.Wallet.Domain;

namespace Tooba.Payment.Infrastructure;

/// <summary>
/// درگاه کیف پول: ATOMIC_DEBIT_AT_PAID در Verify؛ بدون redirect به sandbox/PSP.
/// </summary>
public sealed class WalletPaymentGateway : IPaymentGateway
{
    /// <summary>کد پایدار درگاه.</summary>
    public const string ProviderCodeValue = "wallet";

    private readonly IWalletDirectory _wallets;
    private readonly PaymentGatewayActorContext _actorContext;

    /// <summary>درگاه کیف پول را به دایرکتوری ledger وصل می‌کند.</summary>
    public WalletPaymentGateway(IWalletDirectory wallets, PaymentGatewayActorContext actorContext)
    {
        _wallets = wallets;
        _actorContext = actorContext;
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
            throw new InvalidOperationException("هویت مشتری برای پرداخت کیف پول الزامی است.");
        if (amount <= 0)
            throw new InvalidOperationException("مبلغ پرداخت کیف پول باید مثبت باشد.");

        var quote = await _wallets.QuoteForPayableAsync(actorUserId, amount, currency, cancellationToken);
        if (!quote.CanPayFullyWithWallet)
            throw new InvalidOperationException("موجودی کیف پول کافی نیست.");

        // مرجع پایدار شامل actor/amount/currency برای Verify پس از restart.
        var reference = ComposeReference(paymentId, actorUserId, amount, WalletAccount.NormalizeCurrency(currency));
        // Redirect خالی → PaymentDirectory مسیر /payment/result را می‌سازد (بدون sandbox).
        return new GatewayInitiation(reference, null, DateTimeOffset.UtcNow.AddMinutes(15));
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
            await _wallets.SpendForOrderPaymentAsync(
                actorUserId,
                amount,
                currency,
                paymentId,
                $"wallet-order-debit:{paymentId:D}",
                cancellationToken);
            return new GatewayVerification(true, $"wallet:{paymentId:D}", null);
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("موجودی", StringComparison.Ordinal)
            || ex.Message.Contains("ارز", StringComparison.Ordinal)
            || ex.Message.Contains("مسدود", StringComparison.Ordinal))
        {
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
