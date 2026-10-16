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
    Guid? ShippingGroupId = null);

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
