namespace Tooba.Fulfillment.Contracts.Operations;

/// <summary>
/// TB-257: the ONE owning calculation of remaining packed quantity available for a NEW parcel allocation.
/// </summary>
/// <remarks>
/// <para>
/// Rule: <c>remaining = packed − shipped − open Created allocations</c>.
/// </para>
/// <para>
/// The two subtractions never overlap: <c>QuantityShipped</c> is increased by dispatch at the same moment the
/// shipment leaves <see cref="ShipmentOperationStatus.Created"/>, so a dispatched parcel is counted once in
/// <c>QuantityShipped</c> and no longer as an open allocation. A cancelled pre-dispatch shipment is neither
/// shipped nor an open allocation, so it releases the remainder, and a delivered parcel keeps its shipped
/// quantity subtracted forever.
/// </para>
/// <para>
/// Before TB-257 the Order committed-facts reader subtracted only open allocations and ignored
/// <c>QuantityShipped</c>, so a wholly shipped group still advertised an executable line and an enabled create
/// entry while the create modal (which did subtract shipped) was empty and the owning command refused with
/// <c>fulfillment.shipment.qty_exceeds_packed</c>.
/// </para>
/// </remarks>
public static class FulfillmentPackedQuantityAuthority
{
    /// <summary>No manufactured stock: a negative remainder is reported as zero.</summary>
    public static decimal RemainingPackedQuantity(decimal packed, decimal shipped, decimal openAllocated)
    {
        var remaining = packed - shipped - openAllocated;
        return remaining > 0m ? remaining : 0m;
    }

    /// <summary>
    /// Open (not yet dispatched, not cancelled) quantity of one order line in Created shipments.
    /// </summary>
    /// <param name="snapshot">Authoritative Fulfillment projection.</param>
    /// <param name="orderLineId">Order line to measure.</param>
    /// <param name="shippingGroupId">
    /// When supplied, shipments that already carry another shipping group are ignored, so a sibling group's
    /// allocation cannot be mistaken for this group's. A shipment without a group identity stays visible to
    /// every group (the historical shape).
    /// </param>
    public static decimal OpenAllocatedQuantity(
        FulfillmentSnapshot snapshot,
        Guid orderLineId,
        Guid? shippingGroupId = null)
    {
        var total = 0m;
        foreach (var shipment in snapshot.Shipments)
        {
            if (shipment.Status != ShipmentOperationStatus.Created)
            {
                continue;
            }

            // TB-258: a resend parcel is funded by physically received returned capacity, not by the packed
            // remainder. Counting it here would shrink an unrelated fresh remainder and hide real packed stock.
            if (shipment.ResendOfShipmentId is not null)
            {
                continue;
            }

            if (shippingGroupId is Guid groupId
                && shipment.ShippingGroupId is Guid shipmentGroupId
                && shipmentGroupId != groupId)
            {
                continue;
            }

            foreach (var line in shipment.Items)
            {
                if (line.OrderLineId == orderLineId)
                {
                    total += line.Quantity;
                }
            }
        }

        return total;
    }

    /// <summary>Remaining packed quantity of one line of the snapshot.</summary>
    public static decimal RemainingPackedQuantity(
        FulfillmentSnapshot snapshot,
        FulfillmentItemSnapshot item,
        Guid? shippingGroupId = null)
        => RemainingPackedQuantity(
            item.QuantityPacked,
            item.QuantityShipped,
            OpenAllocatedQuantity(snapshot, item.OrderLineId, shippingGroupId));

    /// <summary>True when at least one line can still be allocated to a new parcel.</summary>
    public static bool HasRemainingPackedQuantity(
        FulfillmentSnapshot snapshot,
        Guid? shippingGroupId = null)
    {
        foreach (var item in snapshot.Items)
        {
            if (RemainingPackedQuantity(snapshot, item, shippingGroupId) > 0m)
            {
                return true;
            }
        }

        return false;
    }
}
