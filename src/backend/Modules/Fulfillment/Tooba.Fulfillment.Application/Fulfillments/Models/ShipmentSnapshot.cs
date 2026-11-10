using Tooba.Fulfillment.Domain.ValueObjects;

namespace Tooba.Fulfillment.Application.Fulfillments.Models;


/// <summary>
/// snapshot محموله.
/// </summary>
public sealed record ShipmentSnapshot(
    Guid ShipmentId,
    ShipmentStatus Status,
    string CarrierDisplayName,
    string? TrackingReference,
    DateTimeOffset? DispatchedAt,
    DateTimeOffset? DeliveredAt,
    IReadOnlyList<ShipmentLineSnapshot> Items,
    DateTimeOffset CreatedAt = default,
    string ShippingMethodCode = "",
    string ShippingMethodLabel = "",
    string? ProviderMetadataJson = null,
    int ProviderMetadataVersion = 0,
    string? PreviousTrackingReference = null,
    Guid? ShippingGroupId = null,
    /// <summary>TB-253-R3: occurrence-scoped durable allocation key recorded on the shipment.</summary>
    string? ShippingDispatchKey = null,
    /// <summary>TB-258: the failed/returned original this parcel resends; null for a first dispatch.</summary>
    Guid? ResendOfShipmentId = null,
    /// <summary>TB-258: recorded non-delivery reason code of this parcel.</summary>
    string? NonDeliveryReason = null,
    /// <summary>TB-258: operator note of the recorded non-delivery attempt.</summary>
    string? NonDeliveryNote = null,
    /// <summary>TB-258: server-recorded non-delivery attempt time.</summary>
    DateTimeOffset? NonDeliveredAt = null,
    /// <summary>TB-258: when the carrier started returning the parcel to origin.</summary>
    DateTimeOffset? ReturnStartedAt = null,
    /// <summary>TB-258: when origin receipt was confirmed.</summary>
    DateTimeOffset? ReturnedAt = null,
    /// <summary>TB-258: the immutable origin receipt of this parcel (null until physically received).</summary>
    ShipmentReturnReceiptSnapshot? ReturnReceipt = null);

/// <summary>TB-258: the recorded physical origin receipt of one returned parcel.</summary>
public sealed record ShipmentReturnReceiptSnapshot(
    Guid ShipmentReturnReceiptId,
    DateTimeOffset ReceivedAt,
    Guid ReceivedByUserId,
    string? Note,
    IReadOnlyList<ShipmentReturnReceiptLineSnapshot> Items)
{
    /// <summary>Physically usable received quantity of the whole receipt.</summary>
    public decimal UsableQuantity => Items
        .Where(x => x.Condition == ReturnReceiptCondition.Usable)
        .Sum(x => x.Quantity);
}

/// <summary>TB-258: one received line with its physical condition.</summary>
public sealed record ShipmentReturnReceiptLineSnapshot(
    Guid OrderLineId,
    decimal Quantity,
    ReturnReceiptCondition Condition);

/// <summary>
/// snapshot گروه ارسال پذیرفته‌شدهٔ یک واحد fulfillment؛ عضویت خطوط برای تصمیم‌گیری canonical است.
/// </summary>
public sealed record FulfillmentShippingGroupSnapshot(
    Guid ShippingGroupId,
    string MethodCode,
    string? ServiceCode,
    string? OptionCode,
    string MethodLabel,
    string? OriginContext,
    DateOnly MinimumDeliveryDate,
    DateOnly? RequestedDeliveryDate,
    string? RequestedDeliveryTimeWindow,
    int LeadDays,
    IReadOnlyList<Guid> OrderLineIds);
