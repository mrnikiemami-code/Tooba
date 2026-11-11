using Microsoft.EntityFrameworkCore;
using Tooba.Order.Contracts.Shipping;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure.Integrations.DispatchPlan;

/// <summary>
/// TB-260: read-only bridge that exposes durable accepted dispatch plans to the operational queue without
/// handing Order storage to another module.
/// </summary>
/// <remarks>
/// Only committed groups whose accepted evidence actually carries a plan are returned. A historical group
/// with <c>NULL</c> dispatch columns is absent by design: the queue must show it as «مشخص نشده» and must
/// never manufacture a deadline from the customer receiving date or the current carrier settings.
/// </remarks>
public sealed class OrderDispatchPlanFactsBridge : IOrderDispatchPlanFactsReader
{
    private readonly OrderDbContext _db;

    /// <summary>Bridges the reader onto Order persistence.</summary>
    public OrderDispatchPlanFactsBridge(OrderDbContext db) => _db = db;

    /// <inheritdoc />
    public Task<IReadOnlyList<OrderAcceptedGroupDispatchPlanFact>> ListPlannedAsync(
        CancellationToken cancellationToken) =>
        ProjectAsync(_db.OrderShippingGroups.AsNoTracking(), cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<OrderAcceptedGroupDispatchPlanFact>> ListPlannedForSellerOrdersAsync(
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken)
    {
        if (sellerOrderIds.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<OrderAcceptedGroupDispatchPlanFact>>([]);
        }

        return ProjectAsync(
            _db.OrderShippingGroups.AsNoTracking().Where(x => sellerOrderIds.Contains(x.SellerOrderId)),
            cancellationToken);
    }

    private static async Task<IReadOnlyList<OrderAcceptedGroupDispatchPlanFact>> ProjectAsync(
        IQueryable<Domain.Checkout.OrderShippingGroup> source,
        CancellationToken cancellationToken)
    {
        var rows = await source
            .Where(x => x.AcceptedEvidence != null && x.AcceptedEvidence.PlannedDispatch != null)
            .OrderBy(x => x.SellerOrderId)
            .ThenBy(x => x.ShippingGroupId)
            .Select(x => new
            {
                x.SellerOrderId,
                x.ShippingGroupId,
                x.CheckoutId,
                x.SellerPartyId,
                x.MethodCode,
                x.MethodLabel,
                x.RequestedDeliveryDate,
                x.RequestedDeliveryTimeWindow,
                x.AcceptedEvidence!.PlannedDispatch!.HandoffDate,
                x.AcceptedEvidence.PlannedDispatch.HandoffOpenTicks,
                x.AcceptedEvidence.PlannedDispatch.HandoffCloseTicks,
                x.AcceptedEvidence.PlannedDispatch.HandoffTimeZone,
                x.AcceptedEvidence.PlannedDispatch.TransportWorkingDays,
                x.AcceptedEvidence.PlannedDispatch.PolicyVersion,
                x.AcceptedEvidence.PlannedDispatch.PreparationStartUtc,
                x.AcceptedEvidence.PlannedDispatch.TransferDepartureUtc,
                x.AcceptedEvidence.PlannedDispatch.HandoffDeadlineUtc,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => new OrderAcceptedGroupDispatchPlanFact(
                x.SellerOrderId,
                x.ShippingGroupId,
                x.CheckoutId,
                x.SellerPartyId,
                x.MethodCode,
                x.MethodLabel,
                x.HandoffDate,
                x.HandoffOpenTicks,
                x.HandoffCloseTicks,
                x.HandoffTimeZone,
                x.TransportWorkingDays,
                x.PolicyVersion,
                x.RequestedDeliveryDate,
                x.RequestedDeliveryTimeWindow,
                x.PreparationStartUtc,
                x.TransferDepartureUtc,
                x.HandoffDeadlineUtc))
            .ToArray();
    }
}
