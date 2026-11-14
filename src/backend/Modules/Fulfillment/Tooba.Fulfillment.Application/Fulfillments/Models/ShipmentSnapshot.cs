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
    ShipmentReturnReceiptSnapshot? ReturnReceipt = null,
    /// <summary>TB-262: the damaged-goods decision that created this parcel, when it is a replacement.</summary>
    Guid? DamagedResolutionId = null,
    /// <summary>TB-262: this parcel's OWN purpose-bound healthy-stock reservation (already consumed after dispatch).</summary>
    Guid? ReplacementReservationId = null,
    /// <summary>TB-262: true only for a parcel created by an authorized damaged-goods replacement decision.</summary>
    bool IsReplacement = false,
    /// <summary>TB-262: durable damaged-goods decisions that belong to THIS returned parcel.</summary>
    IReadOnlyList<DamagedResolutionSnapshot>? DamagedResolutions = null,
    /// <summary>TB-265-R1: courier-origin Returns cases durably linked to this parcel's received quantity.</summary>
    IReadOnlyList<CourierReturnCaseSnapshot>? CourierReturnCases = null);

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
/// TB-262: one durable damaged-goods decision of a returned parcel, as the owning unit reports it.
/// </summary>
public sealed record DamagedResolutionSnapshot(
    Guid DamagedGoodsResolutionId,
    Guid ShipmentId,
    Guid ShipmentReturnReceiptId,
    Guid OrderLineId,
    decimal Quantity,
    DamagedResolutionOutcome Outcome,
    DamagedResolutionStatus Status,
    Guid DecidedByUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    Guid? ReplacementReservationId = null,
    Guid? ReplacementShipmentId = null,
    string? Note = null,
    /// <summary>TB-262-R1: exact reviewed occurrence identity, so a pending decision can be resumed after reload.</summary>
    string OccurrenceKey = "");

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
