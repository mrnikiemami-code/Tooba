using Tooba.Order.Application.Admin.OrdersGrid.Ports;
using Tooba.Order.Application.Admin.Supply.Services;

namespace Tooba.Order.Infrastructure.Admin.OrdersGrid;

/// <summary>Order-owned supply status reader for OrdersGrid.</summary>
internal sealed class AdminOrderSupplyStatusReader(OrderSupplyService supply) : IAdminOrderSupplyStatusReader
{
    public async Task<IReadOnlyDictionary<Guid, string>> GetStatusesAsync(
        IReadOnlyList<Guid> checkoutIds,
        CancellationToken cancellationToken)
    {
        var statuses = await supply.GetStatusesAsync(checkoutIds, cancellationToken);
        return statuses.ToDictionary(x => x.Key, x => x.Value.Status.ToString());
    }
}
