namespace Tooba.Order.Application;

/// <summary>Port for checkout line data needed by reservation-cycle orchestration (no DbContext in Application).</summary>
public interface IReservationCycleCheckoutLineSource
{
    /// <summary>Loads offer/category lines for policy resolution.</summary>
    Task<IReadOnlyList<ReservationCyclePolicyLine>> LoadPolicyLinesAsync(
        Guid checkoutId,
        CancellationToken cancellationToken);

    /// <summary>Loads distinct reservation ids bound to checkout lines.</summary>
    Task<IReadOnlyList<Guid>> LoadReservationIdsAsync(
        Guid checkoutId,
        CancellationToken cancellationToken);
}

/// <summary>Order-owned reservation cycle retry orchestration.</summary>
public interface IReservationCycleCoordinator
{
    /// <summary>Resolves effective policy for a checkout.</summary>
    Task<ReservationCyclePolicySnapshot> ResolveForCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken);

    /// <summary>Ensures supply for unpaid retry after cycle expiry (max-cycle / reacquire / start).</summary>
    Task<Admin.Supply.Models.EnsureOrderSupplyResult> EnsureRetryAfterExpiryAsync(
        Guid checkoutId,
        CancellationToken cancellationToken);
}
