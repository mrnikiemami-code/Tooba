#pragma warning disable CS1591
namespace Tooba.Payment.Contracts.Admin;

public sealed record PaymentAdminOperationalSnapshot(
    Guid PaymentId, Guid CheckoutId, string Status, decimal Amount, string Currency,
    string ProviderCode, string? ProviderRequestReference, string? ProviderTransactionReference,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? CompletedAt,
    string? LastFailureCode, bool ReconcileEligible, bool ConfirmDepositEligible,
    bool RejectDepositEligible, bool RestoreDepositEligible = false, bool HasManualDepositRejection = false,
    bool UnconfirmDepositEligible = false, string? CustomerTransferReference = null, Guid? ProofMediaAssetId = null,
    DateTimeOffset? EvidenceSubmittedAt = null);

public sealed record PaymentAdminMutationResult(Guid PaymentId, string Status, bool NewlySucceeded);

public interface IPaymentAdminGateway
{
    Task<PaymentAdminOperationalSnapshot?> GetLatestOperationalForCheckoutAsync(
        Guid checkoutId, CancellationToken cancellationToken);
    Task CloseOrStartRefundForOrderCancelAsync(Guid checkoutId, CancellationToken cancellationToken);
    Task RestoreAfterOrderCancelRestoreAsync(Guid checkoutId, CancellationToken cancellationToken);
    Task<PaymentAdminMutationResult> ConfirmDepositAsync(Guid paymentId, CancellationToken cancellationToken);
    Task<PaymentAdminMutationResult> RejectDepositAsync(Guid paymentId, CancellationToken cancellationToken);
    Task<PaymentAdminMutationResult> RestoreDepositAsync(Guid paymentId, CancellationToken cancellationToken);
    Task<PaymentAdminMutationResult> UnconfirmDepositAsync(Guid paymentId, CancellationToken cancellationToken);
}
