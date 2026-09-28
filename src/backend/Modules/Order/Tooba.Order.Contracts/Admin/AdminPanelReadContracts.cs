namespace Tooba.Order.Contracts.Admin;

/// <summary>Order-owned Admin dashboard counters (Host composes with Catalog/Offer).</summary>
public sealed record AdminOrderDashboardMetricsDto(
    int OpenOrders,
    int PaidOrders,
    int PendingOrders,
    int Customers);

/// <summary>
/// Order-owned Contracts read port for cross-module Admin dashboard composition.
/// Exposes counters only — never ORM types, request types, or entities.
/// </summary>
public interface IAdminOrderDashboardMetricsPort
{
    /// <summary>Returns Order dashboard counters for the Admin dashboard.</summary>
    Task<AdminOrderDashboardMetricsDto> GetMetricsAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Order-owned Contracts read port for per-seller order counts used by the Admin sellers surface.
/// </summary>
public interface IAdminSellerOrderCountPort
{
    /// <summary>Counts SellerOrders per requested SellerPartyId.</summary>
    Task<IReadOnlyDictionary<Guid, int>> GetCountsBySellerAsync(
        IReadOnlyList<Guid> sellerPartyIds,
        CancellationToken cancellationToken);
}
