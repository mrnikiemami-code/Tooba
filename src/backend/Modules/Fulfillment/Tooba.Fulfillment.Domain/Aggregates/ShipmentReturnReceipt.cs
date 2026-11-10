using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Domain.ValueObjects;

namespace Tooba.Fulfillment.Domain.Aggregates;


/// <summary>
/// TB-258: the durable origin receipt of ONE returned parcel. It is the only fact that turns physically received
/// usable goods into controlled resend capacity, so it is created exactly once per shipment and never recomputed.
/// </summary>
public sealed class ShipmentReturnReceipt
{
    private readonly List<ShipmentReturnReceiptItem> _items = [];

    private ShipmentReturnReceipt()
    {
    }

    /// <summary>شناسه رسید دریافت.</summary>
    public Guid ShipmentReturnReceiptId { get; init; }

    /// <summary>محمولهٔ برگشتی که در مبدأ دریافت شد.</summary>
    public Guid ShipmentId { get; init; }

    /// <summary>fulfillment مالک.</summary>
    public Guid FulfillmentId { get; init; }

    /// <summary>زمان دریافت فیزیکی.</summary>
    public DateTimeOffset ReceivedAt { get; init; }

    /// <summary>اپراتور ثبت‌کننده (server-side).</summary>
    public Guid ReceivedByUserId { get; init; }

    /// <summary>یادداشت اختیاری.</summary>
    public string? Note { get; init; }

    /// <summary>زمان ایجاد رکورد.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>خطوط دریافت‌شده.</summary>
    public IReadOnlyList<ShipmentReturnReceiptItem> Items => _items;

    internal static ShipmentReturnReceipt Create(
        Guid receiptId,
        Func<Guid> newId,
        Guid shipmentId,
        Guid fulfillmentId,
        Guid receivedByUserId,
        DateTimeOffset receivedAt,
        DateTimeOffset now,
        string? note,
        IReadOnlyList<(Guid OrderLineId, decimal Quantity, ReturnReceiptCondition Condition)> items,
        IReadOnlyList<ShipmentItem> shipmentItems)
    {
        if (shipmentId == Guid.Empty)
        {
            throw new ContractOperationException("fulfillment.return.shipment_required");
        }

        if (receivedByUserId == Guid.Empty)
        {
            throw new ContractOperationException("fulfillment.return.actor_required");
        }

        var trimmedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (trimmedNote is { Length: > 512 })
        {
            throw new ContractOperationException("fulfillment.return.note_invalid");
        }

        if (items is null || items.Count == 0)
        {
            throw new ContractOperationException("fulfillment.return.receipt_items_required");
        }

        var receipt = new ShipmentReturnReceipt
        {
            ShipmentReturnReceiptId = receiptId,
            ShipmentId = shipmentId,
            FulfillmentId = fulfillmentId,
            ReceivedAt = receivedAt,
            ReceivedByUserId = receivedByUserId,
            Note = trimmedNote,
            CreatedAt = now,
        };

        var seen = new HashSet<Guid>();
        foreach (var item in items)
        {
            if (item.Quantity <= 0m)
            {
                throw new ContractOperationException("fulfillment.return.receipt_qty_positive");
            }

            if (!seen.Add(item.OrderLineId))
            {
                throw new ContractOperationException("fulfillment.return.receipt_line_duplicate");
            }

            var shippedLine = shipmentItems.SingleOrDefault(x => x.OrderLineId == item.OrderLineId)
                ?? throw new ContractOperationException("fulfillment.return.receipt_line_foreign");

            // Receipt can never exceed what that parcel actually carried: a fabricated larger quantity must not
            // manufacture resend capacity.
            if (item.Quantity > shippedLine.Quantity)
            {
                throw new ContractOperationException("fulfillment.return.receipt_qty_exceeds");
            }

            if (!Enum.IsDefined(item.Condition))
            {
                throw new ContractOperationException("fulfillment.return.receipt_condition_invalid");
            }

            receipt._items.Add(ShipmentReturnReceiptItem.Create(
                newId(),
                receipt.ShipmentReturnReceiptId,
                item.OrderLineId,
                item.Quantity,
                item.Condition));
        }

        return receipt;
    }

    /// <summary>خطوط بارگذاری‌شده را وصل می‌کند.</summary>
    public void AttachLoadedItems(IEnumerable<ShipmentReturnReceiptItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
    }
}


/// <summary>TB-258: one received line of an origin receipt with its physical condition.</summary>
public sealed class ShipmentReturnReceiptItem
{
    private ShipmentReturnReceiptItem()
    {
    }

    /// <summary>شناسه خط رسید.</summary>
    public Guid ShipmentReturnReceiptItemId { get; init; }

    /// <summary>رسید مالک.</summary>
    public Guid ShipmentReturnReceiptId { get; init; }

    /// <summary>خط سفارش.</summary>
    public Guid OrderLineId { get; init; }

    /// <summary>تعداد دریافت‌شدهٔ فیزیکی.</summary>
    public decimal Quantity { get; init; }

    /// <summary>وضعیت فیزیکی.</summary>
    public ReturnReceiptCondition Condition { get; init; }

    internal static ShipmentReturnReceiptItem Create(
        Guid id,
        Guid receiptId,
        Guid orderLineId,
        decimal quantity,
        ReturnReceiptCondition condition) =>
        new()
        {
            ShipmentReturnReceiptItemId = id,
            ShipmentReturnReceiptId = receiptId,
            OrderLineId = orderLineId,
            Quantity = quantity,
            Condition = condition,
        };
}
