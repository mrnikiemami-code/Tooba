using Tooba.Fulfillment.Domain.ValueObjects;

using Tooba.BuildingBlocks;

namespace Tooba.Fulfillment.Domain.Aggregates;


/// <summary>
/// محموله fulfillment. چند محموله برای یک fulfillment مجاز است.
/// </summary>
public sealed class Shipment
{
    private readonly List<ShipmentItem> _items = [];

    private Shipment()
    {
    }

    /// <summary>شناسه محموله.</summary>
    public Guid ShipmentId { get; init; }

    /// <summary>fulfillment مالک.</summary>
    public Guid FulfillmentId { get; init; }

    /// <summary>وضعیت محموله.</summary>
    public ShipmentStatus Status { get; private set; }

    /// <summary>نام نمایشی carrier.</summary>
    public string CarrierDisplayName { get; init; } = string.Empty;

    /// <summary>کد روش ارسال (رجیستری).</summary>
    public string ShippingMethodCode { get; init; } = string.Empty;

    /// <summary>برچسب روش ارسال.</summary>
    public string ShippingMethodLabel { get; init; } = string.Empty;

    /// <summary>متادیتای نرمال‌شده provider (بدون secret).</summary>
    public string? ProviderMetadataJson { get; init; }

    /// <summary>نسخهٔ schema متادیتا.</summary>
    public int ProviderMetadataVersion { get; init; }

    /// <summary>کد/مرجع ردیابی.</summary>
    public string? TrackingReference { get; private set; }

    /// <summary>کد رهگیری قبلی پس از اصلاح پیش از dispatch.</summary>
    public string? PreviousTrackingReference { get; private set; }

    /// <summary>زمان dispatch.</summary>
    public DateTimeOffset? DispatchedAt { get; private set; }

    /// <summary>زمان تحویل.</summary>
    public DateTimeOffset? DeliveredAt { get; private set; }

    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>خطوط محموله.</summary>
    public IReadOnlyList<ShipmentItem> Items => _items;

    internal static Shipment Create(
        Guid shipmentId,
        Func<Guid> newId,
        Guid fulfillmentId,
        string carrierDisplayName,
        IReadOnlyList<(Guid OrderLineId, decimal Quantity)> items,
        IReadOnlyList<FulfillmentItem> fulfillmentItems,
        IReadOnlyList<Shipment> existingShipments,
        DateTimeOffset now,
        string? shippingMethodCode = null,
        string? shippingMethodLabel = null,
        string? providerMetadataJson = null,
        int providerMetadataVersion = 0)
    {
        if (string.IsNullOrWhiteSpace(carrierDisplayName))
        {
            throw new ContractOperationException("fulfillment.shipment.carrier_required");
        }

        var shipment = new Shipment
        {
            ShipmentId = shipmentId,
            FulfillmentId = fulfillmentId,
            Status = ShipmentStatus.Created,
            CarrierDisplayName = carrierDisplayName.Trim(),
            ShippingMethodCode = (shippingMethodCode ?? string.Empty).Trim(),
            ShippingMethodLabel = string.IsNullOrWhiteSpace(shippingMethodLabel)
                ? carrierDisplayName.Trim()
                : shippingMethodLabel.Trim(),
            ProviderMetadataJson = string.IsNullOrWhiteSpace(providerMetadataJson) ? null : providerMetadataJson.Trim(),
            ProviderMetadataVersion = providerMetadataVersion,
            CreatedAt = now,
        };
        foreach (var item in items)
        {
            var remaining = fulfillmentItems.Single(x => x.OrderLineId == item.OrderLineId);
            var openAllocated = existingShipments
                .Where(s => s.Status == ShipmentStatus.Created)
                .SelectMany(s => s.Items)
                .Where(x => x.OrderLineId == item.OrderLineId)
                .Sum(x => x.Quantity);
            var pendingShipmentQty = shipment._items.Where(x => x.OrderLineId == item.OrderLineId).Sum(x => x.Quantity);
            if (remaining.QuantityPacked < remaining.QuantityShipped + openAllocated + pendingShipmentQty + item.Quantity)
            {
                throw new ContractOperationException("fulfillment.shipment.qty_exceeds_packed");
            }

            if (remaining.QuantityShipped + openAllocated + pendingShipmentQty + item.Quantity > remaining.QuantityOrdered)
            {
                throw new ContractOperationException("fulfillment.shipment.qty_exceeds_ordered");
            }

            shipment._items.Add(ShipmentItem.Create(newId(), shipment.ShipmentId, item.OrderLineId, item.Quantity));
        }

        return shipment;
    }

    /// <summary>خطوط محموله بارگذاری‌شده را وصل می‌کند.</summary>
    public void AttachLoadedItems(IEnumerable<ShipmentItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
    }

    internal void AssignTracking(string trackingReference, DateTimeOffset now)
    {
        var normalized = trackingReference.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ContractOperationException("fulfillment.tracking.required");
        }

        if (TrackingReference is not null
            && !string.Equals(TrackingReference, normalized, StringComparison.Ordinal))
        {
            throw new ContractOperationException("fulfillment.tracking.already_set");
        }

        TrackingReference = normalized;
        _ = now;
    }

    internal void CorrectTracking(string trackingReference, DateTimeOffset now)
    {
        if (Status is ShipmentStatus.Dispatched or ShipmentStatus.InTransit or ShipmentStatus.Delivered
            || DispatchedAt is not null)
        {
            throw new ContractOperationException("fulfillment.tracking.locked_after_dispatch");
        }

        if (Status != ShipmentStatus.Created)
        {
            throw new ContractOperationException("fulfillment.tracking.invalid_state");
        }

        var normalized = trackingReference.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ContractOperationException("fulfillment.tracking.required");
        }

        if (string.IsNullOrWhiteSpace(TrackingReference))
        {
            throw new ContractOperationException("fulfillment.tracking.nothing_to_correct");
        }

        if (string.Equals(TrackingReference, normalized, StringComparison.Ordinal))
        {
            return;
        }

        PreviousTrackingReference = TrackingReference;
        TrackingReference = normalized;
        _ = now;
    }

    internal void EnsureDispatched(DateTimeOffset now)
    {
        if (Status == ShipmentStatus.Dispatched || Status == ShipmentStatus.InTransit || Status == ShipmentStatus.Delivered)
        {
            return;
        }

        if (Status != ShipmentStatus.Created)
        {
            throw new ContractOperationException("fulfillment.dispatch.invalid_status");
        }

        if (string.IsNullOrWhiteSpace(TrackingReference))
        {
            throw new ContractOperationException("fulfillment.dispatch.tracking_required");
        }

        Status = ShipmentStatus.Dispatched;
        DispatchedAt = now;
    }

    internal void EnsureDelivered(DateTimeOffset now)
    {
        if (Status == ShipmentStatus.Delivered)
        {
            return;
        }

        if (Status is not (ShipmentStatus.Dispatched or ShipmentStatus.InTransit))
        {
            throw new ContractOperationException("fulfillment.deliver.invalid_status");
        }

        Status = ShipmentStatus.Delivered;
        DeliveredAt = now;
    }

    internal void CancelPreDispatch(DateTimeOffset now)
    {
        if (Status is ShipmentStatus.Dispatched or ShipmentStatus.InTransit or ShipmentStatus.Delivered)
        {
            throw new ContractOperationException("fulfillment.shipment.cancel_invalid");
        }

        if (Status == ShipmentStatus.Cancelled)
        {
            return;
        }

        if (Status != ShipmentStatus.Created)
        {
            throw new ContractOperationException("fulfillment.shipment.cancel_invalid");
        }

        Status = ShipmentStatus.Cancelled;
        _ = now;
    }
}
