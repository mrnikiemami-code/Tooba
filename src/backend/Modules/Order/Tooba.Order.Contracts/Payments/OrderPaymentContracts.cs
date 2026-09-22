#pragma warning disable CS1591
namespace Tooba.Order.Contracts.Payments;

public sealed record CheckoutPaymentAccessSnapshot(Guid CheckoutId, decimal PayableAmount, string Currency, string? OrderNumber);

public interface ICheckoutPaymentAccessReader
{
    Task<CheckoutPaymentAccessSnapshot?> GetForMutationAsync(Guid checkoutId, Guid? cartId, string? guestSecret, Guid? authenticatedUserId, CancellationToken cancellationToken);
    Task<CheckoutPaymentAccessSnapshot?> GetOwnedForPaymentResultAsync(Guid checkoutId, string? guestSecret, Guid? authenticatedUserId, CancellationToken cancellationToken);
    Task<CheckoutPaymentAccessSnapshot?> GetOwnedAsync(Guid checkoutId, Guid? cartId, string? guestSecret, Guid? authenticatedUserId, CancellationToken cancellationToken);
}

public sealed record AdminPaymentOrderEnrichmentSnapshot(
    Guid CheckoutId, string OrderReference, string CustomerDisplayName, string SupplyStatus,
    string ReservationLabel, string ReservationLabelEn, string ReservationState,
    int? ReservationCycleNumber, bool ReservationRetryPossible, bool ReservationNeedsReacquire,
    bool ReservationRetryLimitReached);

public interface IPaymentAdminOrderEnrichmentReader
{
    Task<IReadOnlyList<Guid>> ResolveSearchCheckoutIdsAsync(string search, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> ResolveSupplyFilterCheckoutIdsAsync(IReadOnlyList<string> wantedValues, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> ResolveReservationFilterCheckoutIdsAsync(IReadOnlyList<string> wantedValues, CancellationToken cancellationToken);
    Task<IReadOnlyList<AdminPaymentOrderEnrichmentSnapshot>> EnrichAsync(IReadOnlyList<Guid> checkoutIds, CancellationToken cancellationToken);
}

public interface IOrderUnpaidRetrySupplyPort
{
    Task EnsureRetrySupplyAsync(Guid checkoutId, CancellationToken cancellationToken);
}

public interface IOrderPaymentProjectionPort
{
    Task ApplyVerifiedSuccessAsync(
        Guid checkoutId, Guid paymentId, IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken);
    Task RevertVerifiedSuccessAsync(
        Guid checkoutId, IReadOnlyList<Guid> sellerOrderIds, CancellationToken cancellationToken);
    Task PromoteReservationsForManualPaymentReviewAsync(
        Guid checkoutId, DateTimeOffset reviewExpiresAt, CancellationToken cancellationToken);
    Task ReleaseReservationsAfterManualRejectAsync(
        Guid checkoutId, CancellationToken cancellationToken);
}
