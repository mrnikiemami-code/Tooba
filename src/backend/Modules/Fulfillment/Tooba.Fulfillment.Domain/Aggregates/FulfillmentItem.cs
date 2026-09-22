using Tooba.Fulfillment.Domain.ValueObjects;

using Tooba.BuildingBlocks;

namespace Tooba.Fulfillment.Domain.Aggregates;


/// <summary>
/// خط fulfillment با snapshot تعداد سفارش.
/// </summary>
public sealed class FulfillmentItem
{
    private FulfillmentItem()
    {
    }

    /// <summary>شناسه خط fulfillment.</summary>
    public Guid FulfillmentItemId { get; init; }

    /// <summary>fulfillment مالک.</summary>
    public Guid FulfillmentId { get; init; }

    /// <summary>شناسه خط سفارش مرجع.</summary>
    public Guid OrderLineId { get; init; }

    /// <summary>تعداد سفارش‌داده‌شده.</summary>
    public decimal QuantityOrdered { get; init; }

    /// <summary>تعداد واردشده به پردازش.</summary>
    public decimal QuantityProcessing { get; private set; }

    /// <summary>تعداد بسته‌بندی‌شده تجمعی.</summary>
    public decimal QuantityPacked { get; private set; }

    /// <summary>تعداد dispatch‌شده تجمعی.</summary>
    public decimal QuantityShipped { get; private set; }

    /// <summary>رزرو موجودی مرجع فعال؛ FK Inventory نیست.</summary>
    public Guid? ReservationId { get; private set; }

    /// <summary>آیا رزرو مصرف شده است.</summary>
    public bool ReservationConsumed { get; private set; }

    internal static FulfillmentItem Create(Guid fulfillmentItemId, Guid fulfillmentId, Guid orderLineId, decimal quantityOrdered, Guid? reservationId) =>
        new()
        {
            FulfillmentItemId = fulfillmentItemId,
            FulfillmentId = fulfillmentId,
            OrderLineId = orderLineId,
            QuantityOrdered = quantityOrdered,
            ReservationId = reservationId,
        };

    /// <summary>
    /// مرجع رزرو فعال را به شناسهٔ فعلی Order/Inventory هم‌تراز می‌کند.
    /// رزرو Released/Consumed تاریخی را زنده نمی‌کند.
    /// </summary>
    internal void RebindActiveReservation(Guid? reservationId)
    {
        if (ReservationConsumed || QuantityShipped >= QuantityOrdered)
        {
            return;
        }

        ReservationId = reservationId;
    }

    internal void ApplyProcessingQuantity(decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new ContractOperationException("fulfillment.processing.qty_positive");
        }

        if (QuantityProcessing + quantity > QuantityOrdered)
        {
            throw new ContractOperationException("fulfillment.processing.qty_exceeds");
        }

        QuantityProcessing += quantity;
    }

    internal void ReleaseProcessingQuantity(decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new ContractOperationException("fulfillment.processing.release_qty_positive");
        }

        if (QuantityProcessing - quantity < QuantityPacked)
        {
            throw new ContractOperationException("fulfillment.processing.release_invalid");
        }

        QuantityProcessing -= quantity;
    }

    internal void ApplyPackedQuantity(decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new ContractOperationException("fulfillment.pack.qty_positive");
        }

        if (QuantityPacked + quantity > QuantityOrdered)
        {
            throw new ContractOperationException("fulfillment.pack.qty_exceeds");
        }

        QuantityPacked += quantity;
    }

    internal void ReleasePackedQuantity(decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new ContractOperationException("fulfillment.pack.release_qty_positive");
        }

        if (quantity > QuantityPacked)
        {
            throw new ContractOperationException("fulfillment.pack.release_exceeds");
        }

        QuantityPacked -= quantity;
    }

    internal void ResetWarehouseProgress()
    {
        QuantityProcessing = 0;
        QuantityPacked = 0;
    }

    internal void ApplyShippedQuantity(decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new ContractOperationException("fulfillment.ship.qty_positive");
        }

        if (QuantityShipped + quantity > QuantityOrdered)
        {
            throw new ContractOperationException("fulfillment.ship.qty_exceeds");
        }

        QuantityShipped += quantity;
    }

    /// <summary>رزرو را مصرف‌شده علامت می‌زند.</summary>
    public void MarkReservationConsumed() => ReservationConsumed = true;
}
