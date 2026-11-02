using Microsoft.EntityFrameworkCore;
using Tooba.Order.Contracts.Shipping;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure.Integrations.Shipping;

/// <summary>
/// TB-248-R1: committed Order shipping-group facts for carrier shipment create. Order storage only.
/// </summary>
public sealed class OrderCommittedCarrierShipmentFactsReader(OrderDbContext db) : IOrderCommittedCarrierShipmentFactsReader
{
    /// <inheritdoc />
    public async Task<OrderCommittedCarrierShipmentFacts?> GetAsync(
        Guid checkoutId,
        Guid shippingGroupId,
        CancellationToken cancellationToken)
    {
        if (checkoutId == Guid.Empty || shippingGroupId == Guid.Empty)
        {
            return null;
        }

        var group = await db.OrderShippingGroups
            .AsNoTracking()
            .Include(x => x.AcceptedEvidence)
            .SingleOrDefaultAsync(
                x => x.CheckoutId == checkoutId && x.ShippingGroupId == shippingGroupId,
                cancellationToken);
        if (group is null)
        {
            return null;
        }

        var checkout = await db.Checkouts
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken);
        if (checkout is null)
        {
            return null;
        }

        var evidence = group.AcceptedEvidence;
        return new OrderCommittedCarrierShipmentFacts(
            checkoutId,
            shippingGroupId,
            checkout.RecipientName,
            checkout.ContactMobile,
            checkout.PostalAddress,
            checkout.PostalCode,
            group.Currency,
            evidence?.PayableAtCheckoutAmount ?? group.ShippingChargeSnapshot,
            evidence?.CarrierCollectAmount ?? 0m,
            evidence?.CarrierQuote?.AccountId,
            evidence?.CarrierQuote?.ServiceCode);
    }
}
