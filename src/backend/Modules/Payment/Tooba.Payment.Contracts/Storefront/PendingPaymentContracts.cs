#pragma warning disable CS1591
namespace Tooba.Payment.Contracts.Storefront;

public sealed record PendingPaymentLatestSnapshot(
    Guid PaymentId, Guid CheckoutId, decimal Amount, string Currency, string Status,
    string ProviderCode, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt,
    DateTimeOffset? EvidenceSubmittedAt);

public sealed record PendingPaymentOperationalSnapshot(
    Guid PaymentId, Guid CheckoutId, string Status, decimal Amount, string Currency,
    string ProviderCode, string? ProviderRequestReference, string? ProviderTransactionReference,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? CompletedAt,
    string? LastFailureCode, bool ReconcileEligible, bool ConfirmDepositEligible,
    bool RejectDepositEligible, bool RestoreDepositEligible, bool HasManualDepositRejection,
    bool UnconfirmDepositEligible, string? CustomerTransferReference, Guid? ProofMediaAssetId,
    DateTimeOffset? EvidenceSubmittedAt);

public interface IPendingPaymentReader
{
    Task<IReadOnlyList<PendingPaymentLatestSnapshot>> GetLatestByCheckoutIdsAsync(
        IReadOnlyCollection<Guid> checkoutIds, CancellationToken cancellationToken);
    Task<PendingPaymentOperationalSnapshot?> GetLatestOperationalForCheckoutAsync(
        Guid checkoutId, CancellationToken cancellationToken);
    Task CloseOrStartRefundForOrderCancelAsync(Guid checkoutId, CancellationToken cancellationToken);
}
