using Tooba.Order.Application.Admin.Dashboard.Ports;
using Tooba.Order.Application.Admin.Sellers.Ports;
using Tooba.Order.Contracts.Admin;

namespace Tooba.Order.Infrastructure.Admin;

/// <summary>
/// Order-owned adapter exposing Admin dashboard counters through Order.Contracts.
/// Delegates to the existing Application-layer store; no persistence leaks to Host.
/// </summary>
internal sealed class AdminOrderDashboardMetricsPort(IAdminOrderDashboardMetricsStore store)
    : IAdminOrderDashboardMetricsPort
{
    public async Task<AdminOrderDashboardMetricsDto> GetMetricsAsync(CancellationToken cancellationToken)
    {
        var metrics = await store.GetAsync(cancellationToken);
        return new AdminOrderDashboardMetricsDto(
            metrics.OpenOrders,
            metrics.PaidOrders,
            metrics.PendingOrders,
            metrics.Customers);
    }
}

/// <summary>
/// Order-owned adapter exposing per-seller order counts through Order.Contracts.
/// Delegates to the existing Application-layer reader; no persistence leaks to Host.
/// </summary>
internal sealed class AdminSellerOrderCountPort(ISellerOrderCountReader reader) : IAdminSellerOrderCountPort
{
    public async Task<IReadOnlyDictionary<Guid, int>> GetCountsBySellerAsync(
        IReadOnlyList<Guid> sellerPartyIds,
        CancellationToken cancellationToken)
    {
        var counts = await reader.GetCountsBySellerAsync(sellerPartyIds, cancellationToken);
        return counts is Dictionary<Guid, int> dict
            ? dict
            : counts.ToDictionary(x => x.Key, x => x.Value);
    }
}
