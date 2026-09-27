using Tooba.Order.Application.Customer;
using Tooba.Order.Application.Customer.Ports;
using Tooba.Order.Contracts.Customer;

namespace Tooba.Order.Infrastructure.Adapters;

/// <summary>
/// Order-owned Contracts adapter for customer-account dashboard composition.
/// Reuses the same store/composer path as GetCustomerOrderDashboardSummaryHandler.
/// </summary>
public sealed class CustomerOrderDashboardSummaryAdapter(
    ICustomerOrderCheckoutStore store,
    Application.Customer.CustomerOrderComposer composer) : ICustomerOrderDashboardSummaryPort
{
    /// <inheritdoc />
    public async Task<CustomerOrderDashboardSummaryDto?> GetAsync(
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (actorUserId == Guid.Empty)
        {
            return null;
        }

        var groups = await store.ListByActorAsync(actorUserId, take: 200, cancellationToken);
        var orders = new List<Application.Customer.Models.CustomerOrderListItem>(groups.Count);
        foreach (var group in groups)
        {
            orders.Add(await composer.MapListItemAsync(group, actorUserId, cancellationToken));
        }

        var latest = groups.Count == 0 ? null : groups[0];
        var summary = Application.Customer.CustomerOrderComposer.BuildDashboardSummary(orders, latest);
        return Map(summary);
    }

    private static CustomerOrderDashboardSummaryDto Map(Application.Customer.Models.CustomerOrderDashboardSummary summary) =>
        new(
            summary.TotalOrders,
            summary.PendingOrders,
            summary.PaidOrders,
            summary.RecentOrders.Select(x => new CustomerOrderListItemDto(
                x.CheckoutId,
                x.Reference,
                x.SubmittedAt,
                x.SellerCount,
                x.ItemCount,
                x.PayableAmount,
                x.Currency,
                x.PaymentState,
                x.Status)).ToArray(),
            summary.LatestOrderRecipientDisplayName,
            summary.LatestShippingAddress,
            summary.LatestContactMobile);
}
