using Tooba.Payment.Application.Ports;
using Tooba.Payment.Contracts.Returns;

namespace Tooba.Payment.Infrastructure.Adapters;

/// <summary>
/// پل خواندن پرداخت برای مسیر مرجوعی بدون افشای Directory کامل.
/// </summary>
public sealed class PaymentReturnBridge : IPaymentReturnReader
{
    private readonly IPaymentDirectory _payments;

    /// <summary>پل را به دایرکتوری پرداخت وصل می‌کند.</summary>
    public PaymentReturnBridge(IPaymentDirectory payments) => _payments = payments;

    /// <inheritdoc />
    public async Task<PaymentReturnSnapshot?> GetAsync(
        Guid paymentId,
        Guid actorUserId,
        Guid? buyerPartyId,
        CancellationToken cancellationToken)
    {
        var snapshot = await _payments.GetAsync(paymentId, actorUserId, buyerPartyId, cancellationToken);
        return snapshot is null ? null : Map(snapshot);
    }

    /// <inheritdoc />
    public async Task<PaymentReturnSnapshot?> GetLatestForCheckoutAsync(
        Guid checkoutId,
        Guid actorUserId,
        Guid? buyerPartyId,
        CancellationToken cancellationToken)
    {
        var snapshot = await _payments.GetLatestForCheckoutAsync(checkoutId, actorUserId, buyerPartyId, cancellationToken);
        return snapshot is null ? null : Map(snapshot);
    }

    private static PaymentReturnSnapshot Map(PaymentSnapshot snapshot) =>
        new(
            snapshot.PaymentId,
            snapshot.CheckoutId,
            snapshot.Amount,
            snapshot.Currency,
            snapshot.Status.ToString());
}
