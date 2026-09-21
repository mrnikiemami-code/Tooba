using Tooba.Fulfillment.Domain.ValueObjects;

namespace Tooba.Fulfillment.Application.Models;


/// <summary>
/// snapshot خواندنی fulfillment.
/// </summary>
public sealed record FulfillmentSnapshot(
    Guid FulfillmentId,
    Guid SellerOrderId,
    Guid CheckoutId,
    Guid SellerPartyId,
    FulfillmentStatus Status,
    string RecipientName,
    string ContactMobile,
    string ProvinceName,
    string CityName,
    string PostalAddress,
    string PostalCode,
    string ShippingMethodCode,
    string ShippingMethodLabel,
    IReadOnlyList<FulfillmentItemSnapshot> Items,
    IReadOnlyList<ShipmentSnapshot> Shipments,
    DateTimeOffset CreatedAt = default,
    DateTimeOffset UpdatedAt = default,
    string? PreferredTrackingReference = null);
