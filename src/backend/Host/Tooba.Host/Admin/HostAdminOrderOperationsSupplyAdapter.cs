using Tooba.Inventory.Application.Orders;
using Tooba.Order.Application.Admin.Operations.Ports;

namespace Tooba.Host.Admin;

/// <summary>
/// Thin Host adapter over OrderSupplyComposer — no Order business logic.
/// </summary>
internal sealed class HostAdminOrderOperationsSupplyAdapter(OrderSupplyComposer supply) : IAdminOrderOperationsSupplyPort
{
    public async Task<AdminOrderOpsSupplyStatus> GetStatusAsync(Guid checkoutId, CancellationToken cancellationToken)
    {
        var status = await supply.GetStatusAsync(checkoutId, cancellationToken);
        return Map(status);
    }

    public async Task<AdminOrderOpsSupplyEnsureResult> EnsurePaidDurableAsync(
        Guid checkoutId,
        string reason,
        CancellationToken cancellationToken)
    {
        var result = await supply.EnsureAsync(
            checkoutId,
            OrderSupplyMode.EnsurePaidDurable,
            allowReacquire: true,
            reason,
            cancellationToken);
        var unavailable = result.Outcome is OrderSupplyOutcome.Unavailable or OrderSupplyOutcome.PartiallyUnavailable
            || result.Status is OrderSupplyStatusKind.Unavailable or OrderSupplyStatusKind.PartiallyUnavailable;
        return new AdminOrderOpsSupplyEnsureResult(
            result.Outcome.ToString(),
            result.Status.ToString(),
            unavailable);
    }

    private static AdminOrderOpsSupplyStatus Map(OrderSupplyStatus status) =>
        new(
            status.CheckoutId,
            status.Status.ToString(),
            OrderSupplyComposer.MessageFa(status.Status),
            status.Lines.Select(x => new AdminOrderOpsSupplyLine(
                x.ItemTitle,
                x.UnitCode,
                x.Required,
                x.Available,
                x.Shortage,
                x.LineStatus.ToString())).ToList());
}
