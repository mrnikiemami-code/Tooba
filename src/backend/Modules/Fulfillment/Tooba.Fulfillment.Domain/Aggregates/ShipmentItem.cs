

namespace Tooba.Fulfillment.Domain.Aggregates;


/// <summary>
/// خط محموله.
/// </summary>
public sealed class ShipmentItem
{
    private ShipmentItem()
    {
    }

    /// <summary>شناسه خط محموله.</summary>
    public Guid ShipmentItemId { get; init; }

    /// <summary>شناسه shipment.</summary>
    public Guid ShipmentId { get; init; }

    /// <summary>خط سفارش.</summary>
    public Guid OrderLineId { get; init; }

    /// <summary>تعداد.</summary>
    public decimal Quantity { get; init; }

    internal static ShipmentItem Create(Guid shipmentItemId, Guid shipmentId, Guid orderLineId, decimal quantity) =>
        new()
        {
            ShipmentItemId = shipmentItemId,
            ShipmentId = shipmentId,
            OrderLineId = orderLineId,
            Quantity = quantity,
        };
}
