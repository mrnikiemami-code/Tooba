#pragma warning disable CS1591
using Tooba.Payment.Application.Ports;
using Tooba.Payment.Contracts.Storefront;

namespace Tooba.Payment.Infrastructure.Adapters;

public sealed class PendingPaymentBridge(
    IPaymentQueryDirectory queries,
    IPaymentAdminDirectory admin) : IPendingPaymentReader
{
    public async Task<IReadOnlyList<PendingPaymentLatestSnapshot>> GetLatestByCheckoutIdsAsync(
        IReadOnlyCollection<Guid> checkoutIds, CancellationToken cancellationToken) =>
        (await queries.GetLatestByCheckoutIdsAsync(checkoutIds, cancellationToken))
        .Select(x => new PendingPaymentLatestSnapshot(
            x.PaymentId, x.CheckoutId, x.Amount, x.Currency, x.Status, x.ProviderCode,
            x.CreatedAt, x.CompletedAt, x.EvidenceSubmittedAt)).ToArray();

    public async Task<PendingPaymentOperationalSnapshot?> GetLatestOperationalForCheckoutAsync(
        Guid checkoutId, CancellationToken cancellationToken)
    {
        var x = await admin.GetLatestOperationalForCheckoutAsync(checkoutId, cancellationToken);
        return x is null ? null : new(
            x.PaymentId, x.CheckoutId, x.Status.ToString(), x.Amount, x.Currency, x.ProviderCode,
            x.ProviderRequestReference, x.ProviderTransactionReference, x.CreatedAt, x.UpdatedAt,
            x.CompletedAt, x.LastFailureCode, x.ReconcileEligible, x.ConfirmDepositEligible,
            x.RejectDepositEligible, x.RestoreDepositEligible, x.HasManualDepositRejection,
            x.UnconfirmDepositEligible, x.CustomerTransferReference, x.ProofMediaAssetId,
            x.EvidenceSubmittedAt);
    }

    public Task CloseOrStartRefundForOrderCancelAsync(Guid checkoutId, CancellationToken cancellationToken) =>
        admin.CloseOrStartRefundForOrderCancelAsync(checkoutId, cancellationToken);
}
