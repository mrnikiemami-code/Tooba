namespace Tooba.Fulfillment.Contracts.History;

/// <summary>Shipped quantity of one order line inside a shipment.</summary>
public sealed record FulfillmentHistoryShipmentLine(Guid OrderLineId, decimal Quantity);

/// <summary>Operational milestones of a single shipment. Statuses are stable strings, not owner enums.</summary>
public sealed record FulfillmentHistoryShipment(
    Guid ShipmentId,
    string Status,
    string CarrierDisplayName,
    string ShippingMethodLabel,
    string? TrackingReference,
    string? PreviousTrackingReference,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DispatchedAt,
    DateTimeOffset? DeliveredAt,
    IReadOnlyList<FulfillmentHistoryShipmentLine> Items);

/// <summary>Ordered and packed quantity of one order line inside a fulfillment.</summary>
public sealed record FulfillmentHistoryItem(Guid OrderLineId, decimal QuantityOrdered, decimal QuantityPacked);

/// <summary>Operational milestones of one seller fulfillment and its shipments.</summary>
public sealed record FulfillmentHistoryRecord(
    Guid FulfillmentId,
    Guid SellerOrderId,
    Guid SellerPartyId,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<FulfillmentHistoryItem> Items,
    IReadOnlyList<FulfillmentHistoryShipment> Shipments);

/// <summary>Membership of one seller shipment in a consolidated package.</summary>
public sealed record ConsolidatedPackageHistoryMember(Guid ShipmentId, Guid SellerPartyId, DateTimeOffset JoinedAt);

/// <summary>Operational milestones of one consolidated package.</summary>
public sealed record ConsolidatedPackageHistoryRecord(
    Guid ConsolidatedPackageId,
    string PackageNumber,
    Guid? CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DispatchedAt,
    DateTimeOffset? DeliveredAt,
    DateTimeOffset? CancelledAt,
    IReadOnlyList<ConsolidatedPackageHistoryMember> Members);

/// <summary>
/// Read-only Fulfillment history for a checkout, consumed by admin operational timelines.
/// </summary>
public interface IFulfillmentHistoryReader
{
    /// <summary>Seller fulfillments and their shipments for one checkout.</summary>
    Task<IReadOnlyList<FulfillmentHistoryRecord>> ListFulfillmentsForCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken);

    /// <summary>Consolidated packages for one checkout, including historical ones.</summary>
    Task<IReadOnlyList<ConsolidatedPackageHistoryRecord>> ListConsolidatedPackagesForCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken);
}

/// <summary>Stable fulfillment status strings exposed through the history contract.</summary>
public static class FulfillmentHistoryStatuses
{
    /// <summary>Fulfillment is being prepared.</summary>
    public const string Processing = "Processing";

    /// <summary>Fulfillment is packed.</summary>
    public const string Packed = "Packed";

    /// <summary>Fulfillment is dispatched.</summary>
    public const string Dispatched = "Dispatched";

    /// <summary>Fulfillment is in transit.</summary>
    public const string InTransit = "InTransit";

    /// <summary>Fulfillment is delivered.</summary>
    public const string Delivered = "Delivered";

    /// <summary>Shipment is cancelled.</summary>
    public const string Cancelled = "Cancelled";
}
