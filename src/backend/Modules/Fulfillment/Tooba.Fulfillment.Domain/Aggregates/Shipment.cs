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

    /// <summary>
    /// گروه ارسال پذیرفته‌شدهٔ مالک این محموله؛ null برای سفارش‌های قدیمی بدون گروه‌بندی.
    /// </summary>
    public Guid? ShippingGroupId { get; init; }

    /// <summary>
    /// Durable Shipping dispatch intent key for idempotent create after link-write failure.
    /// </summary>
    public string? ShippingDispatchKey { get; init; }

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

    /// <summary>TB-258: reason code of the recorded non-delivery attempt.</summary>
    public string? NonDeliveryReason { get; private set; }

    /// <summary>TB-258: operator note of the non-delivery attempt.</summary>
    public string? NonDeliveryNote { get; private set; }

    /// <summary>TB-258: when the courier reported the non-delivery attempt.</summary>
    public DateTimeOffset? NonDeliveredAt { get; private set; }

    /// <summary>TB-258: when the carrier started physically returning the parcel to origin.</summary>
    public DateTimeOffset? ReturnStartedAt { get; private set; }

    /// <summary>TB-258: when the origin receipt was confirmed.</summary>
    public DateTimeOffset? ReturnedAt { get; private set; }

    /// <summary>TB-258-R1: durable operator identity of the creating request (server-side actor).</summary>
    public Guid? CreatedByUserId { get; init; }

    /// <summary>TB-258-R1: durable operator identity that recorded the non-delivery attempt.</summary>
    public Guid? NonDeliveredByUserId { get; private set; }

    /// <summary>TB-258-R1: durable operator identity that started the physical return.</summary>
    public Guid? ReturnStartedByUserId { get; private set; }

    /// <summary>
    /// TB-258: the failed/returned original this parcel resends. Null for an original parcel, so a resend can
    /// never be mistaken for a first dispatch and its history stays linked.
    /// </summary>
    public Guid? ResendOfShipmentId { get; init; }

    /// <summary>TB-258: a resend parcel consumes returned capacity, never the packed remainder.</summary>
    public bool IsResend => ResendOfShipmentId is not null;

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
        int providerMetadataVersion = 0,
        Guid? shippingGroupId = null,
        string? shippingDispatchKey = null)
    {
        if (string.IsNullOrWhiteSpace(carrierDisplayName))
        {
            throw new ContractOperationException("fulfillment.shipment.carrier_required");
        }

        if (shippingGroupId == Guid.Empty)
        {
            throw new ContractOperationException(FulfillmentShippingGroupFault.IdentityRequired);
        }

        var normalizedDispatchKey = string.IsNullOrWhiteSpace(shippingDispatchKey)
            ? null
            : shippingDispatchKey.Trim();
        if (normalizedDispatchKey is { Length: > 128 })
        {
            throw new ContractOperationException("fulfillment.shipment.dispatch_key_invalid");
        }

        var shipment = new Shipment
        {
            ShipmentId = shipmentId,
            FulfillmentId = fulfillmentId,
            ShippingGroupId = shippingGroupId,
            ShippingDispatchKey = normalizedDispatchKey,
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
            // TB-258: a Created RESEND parcel is funded by returned capacity, not by the packed remainder, so it is
            // never counted as an open first allocation here.
            var openAllocated = existingShipments
                .Where(s => s.Status == ShipmentStatus.Created && !s.IsResend)
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

    /// <summary>
    /// TB-258: creates a resend parcel bound to the failed/returned original. Quantity is validated by the owning
    /// unit against the physically received usable capacity, so the packed/ordered guards of a first allocation
    /// are deliberately not applied here — the returned goods already left the warehouse once.
    /// </summary>
    internal static Shipment CreateResend(
        Guid shipmentId,
        Func<Guid> newId,
        Guid fulfillmentId,
        Guid resendOfShipmentId,
        string carrierDisplayName,
        IReadOnlyList<(Guid OrderLineId, decimal Quantity)> items,
        DateTimeOffset now,
        string? shippingMethodCode,
        string? shippingMethodLabel,
        Guid? shippingGroupId,
        string? shippingDispatchKey,
        Guid actorUserId)
    {
        if (resendOfShipmentId == Guid.Empty)
        {
            throw new ContractOperationException("fulfillment.resend.original_required");
        }

        if (actorUserId == Guid.Empty)
        {
            throw new ContractOperationException("fulfillment.resend.actor_required");
        }

        if (string.IsNullOrWhiteSpace(carrierDisplayName))
        {
            throw new ContractOperationException("fulfillment.shipment.carrier_required");
        }

        if (shippingGroupId == Guid.Empty)
        {
            throw new ContractOperationException(FulfillmentShippingGroupFault.IdentityRequired);
        }

        // A resend occurrence is a prebound reviewed intent: without it a retried/re-opened request could allocate
        // a second parcel, so a blank key is a typed refusal rather than a non-idempotent fallback.
        var normalizedDispatchKey = string.IsNullOrWhiteSpace(shippingDispatchKey)
            ? null
            : shippingDispatchKey.Trim();
        if (normalizedDispatchKey is null)
        {
            throw new ContractOperationException("fulfillment.resend.occurrence_required");
        }

        if (normalizedDispatchKey.Length > 128)
        {
            throw new ContractOperationException("fulfillment.shipment.dispatch_key_invalid");
        }

        var shipment = new Shipment
        {
            ShipmentId = shipmentId,
            FulfillmentId = fulfillmentId,
            ShippingGroupId = shippingGroupId,
            ShippingDispatchKey = normalizedDispatchKey,
            ResendOfShipmentId = resendOfShipmentId,
            Status = ShipmentStatus.Created,
            CreatedByUserId = actorUserId,
            CarrierDisplayName = carrierDisplayName.Trim(),
            ShippingMethodCode = (shippingMethodCode ?? string.Empty).Trim(),
            ShippingMethodLabel = string.IsNullOrWhiteSpace(shippingMethodLabel)
                ? carrierDisplayName.Trim()
                : shippingMethodLabel.Trim(),
            CreatedAt = now,
        };
        foreach (var item in items)
        {
            shipment._items.Add(ShipmentItem.Create(newId(), shipment.ShipmentId, item.OrderLineId, item.Quantity));
        }

        return shipment;
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

        // TB-258: a genuine later delivery may resolve a recorded non-delivery (the customer was reached on a
        // retry). It can never resolve a parcel the carrier is physically bringing back.
        if (Status is not (ShipmentStatus.Dispatched or ShipmentStatus.InTransit or ShipmentStatus.NonDelivered))
        {
            throw new ContractOperationException("fulfillment.deliver.invalid_status");
        }

        Status = ShipmentStatus.Delivered;
        DeliveredAt = now;
    }

    /// <summary>
    /// TB-258: records a courier non-delivery attempt after dispatch. It neither pretends delivery nor cancels the
    /// parcel: the goods have physically left the warehouse and stay in the shipped side of every quantity reader.
    /// </summary>
    internal void RecordNonDelivery(string reason, string? note, Guid actorUserId, DateTimeOffset now)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new ContractOperationException("fulfillment.nondelivery.actor_required");
        }

        var normalizedReason = (reason ?? string.Empty).Trim();
        if (normalizedReason.Length == 0)
        {
            throw new ContractOperationException("fulfillment.nondelivery.reason_required");
        }

        if (normalizedReason.Length > 64)
        {
            throw new ContractOperationException("fulfillment.nondelivery.reason_invalid");
        }

        var trimmedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (trimmedNote is { Length: > 512 })
        {
            throw new ContractOperationException("fulfillment.nondelivery.note_invalid");
        }

        switch (Status)
        {
            case ShipmentStatus.Delivered:
                throw new ContractOperationException("fulfillment.nondelivery.after_delivered");
            case ShipmentStatus.Cancelled:
            case ShipmentStatus.Created:
                throw new ContractOperationException("fulfillment.nondelivery.invalid_status");
            case ShipmentStatus.Returning:
            case ShipmentStatus.Returned:
                throw new ContractOperationException("fulfillment.nondelivery.already_returning");
            case ShipmentStatus.NonDelivered:
                if (!string.Equals(NonDeliveryReason, normalizedReason, StringComparison.Ordinal)
                    || !string.Equals(NonDeliveryNote, trimmedNote, StringComparison.Ordinal))
                {
                    throw new ContractOperationException("fulfillment.nondelivery.conflict");
                }

                // An exact replay is the same operator fact: never reattribute it to another actor.
                return;
        }

        Status = ShipmentStatus.NonDelivered;
        NonDeliveryReason = normalizedReason;
        NonDeliveryNote = trimmedNote;
        NonDeliveredByUserId = actorUserId;
        NonDeliveredAt = now;
    }

    /// <summary>TB-258: the carrier started bringing the failed parcel back.</summary>
    internal void StartReturn(Guid actorUserId, DateTimeOffset now)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new ContractOperationException("fulfillment.return.actor_required");
        }

        if (Status == ShipmentStatus.Returning)
        {
            return;
        }

        if (Status == ShipmentStatus.Returned)
        {
            throw new ContractOperationException("fulfillment.return.already_received");
        }

        if (Status != ShipmentStatus.NonDelivered)
        {
            throw new ContractOperationException("fulfillment.return.invalid_status");
        }

        Status = ShipmentStatus.Returning;
        ReturnStartedByUserId = actorUserId;
        ReturnStartedAt = now;
    }

    /// <summary>TB-258: origin receipt confirmed; the parcel is now eligible for controlled resend capacity.</summary>
    internal void EnsureReturned(DateTimeOffset now)
    {
        if (Status == ShipmentStatus.Returned)
        {
            return;
        }

        if (Status != ShipmentStatus.Returning)
        {
            throw new ContractOperationException("fulfillment.return.receipt_invalid_status");
        }

        Status = ShipmentStatus.Returned;
        ReturnedAt = now;
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
