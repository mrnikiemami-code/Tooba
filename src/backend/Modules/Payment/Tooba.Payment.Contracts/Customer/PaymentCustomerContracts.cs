#pragma warning disable CS1591
namespace Tooba.Payment.Contracts.Customer;

public sealed record PaymentCustomerAllocationSnapshot(Guid SellerOrderId, decimal AllocatedAmount, string Currency);

public sealed record PaymentCustomerSnapshot(
    Guid PaymentId, Guid CheckoutId, decimal Amount, string Currency, string Status,
    string ProviderCode, IReadOnlyList<PaymentCustomerAllocationSnapshot> Allocations,
    string? CustomerTransferReference, Guid? ProofMediaAssetId, DateTimeOffset? EvidenceSubmittedAt);

public interface IPaymentCustomerGateway
{
    Task<PaymentCustomerSnapshot?> GetLatestForCheckoutAsync(
        Guid checkoutId, Guid actorUserId, Guid? buyerPartyId, CancellationToken cancellationToken);
    Task<bool> HasSucceededPaymentForCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> ExpireDueUnpaidAsync(
        DateTimeOffset utcNow, int batchSize, CancellationToken cancellationToken);
    Task ReopenExpiredForRetryAsync(
        Guid paymentId, Guid actorUserId, Guid? buyerPartyId, CancellationToken cancellationToken);
}
