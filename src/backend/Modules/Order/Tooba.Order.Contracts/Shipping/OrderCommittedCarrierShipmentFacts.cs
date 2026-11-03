namespace Tooba.Order.Contracts.Shipping;

/// <summary>Remaining packed quantity on one accepted group line available for a new dispatch.</summary>
/// <param name="OrderLineId">Accepted order line.</param>
/// <param name="RemainingPackedQuantity">Packed quantity not yet allocated to an open Created shipment.</param>
/// <param name="UnitPriceSnapshot">Historical unit price from the committed Order line.</param>
/// <param name="PackedQuantity">Total packed quantity for the line on the fulfillment unit.</param>
public sealed record OrderCommittedCarrierExecutableLine(
    Guid OrderLineId,
    decimal RemainingPackedQuantity,
    decimal UnitPriceSnapshot = 0m,
    decimal PackedQuantity = 0m);

/// <summary>Committed packed line value facts for declared-value / collect allocation.</summary>
public sealed record OrderCommittedCarrierLineValueFact(
    Guid OrderLineId,
    decimal PackedQuantity,
    decimal UnitPriceSnapshot);

/// <summary>
/// Read-only committed Order facts for a carrier shipment. Shipping never joins Order storage.
/// </summary>
public sealed record OrderCommittedCarrierShipmentFacts(
    Guid CheckoutId,
    Guid ShippingGroupId,
    Guid SellerPartyId,
    Guid SellerOrderId,
    string MethodCode,
    string MethodLabel,
    string TransportMode,
    int OwnerKind,
    string? OwnerTenantKey,
    Guid? OwnerSellerPartyId,
    string RecipientName,
    string RecipientMobile,
    string RecipientAddress,
    string? PostalCode,
    string ProvinceName,
    string CityName,
    string Currency,
    decimal PayableAtCheckoutAmount,
    decimal CarrierCollectAmount,
    bool HasCarrierQuote,
    Guid? CarrierAccountId,
    int? ConfigurationVersion,
    string? ProviderCode,
    string? ProviderServiceCode,
    string? QuoteInputIdentity,
    string? OriginLocationCode,
    string? DestinationLocationCode,
    decimal? WeightGrams,
    decimal? DeclaredValue,
    decimal? LengthCentimeters,
    decimal? WidthCentimeters,
    decimal? HeightCentimeters,
    string SellerOrderStatus,
    bool IsCancelled,
    bool PaymentLocked,
    bool CanExecuteCarrierShipment,
    string? IneligibilityReasonCode,
    IReadOnlyList<Guid> OrderLineIds,
    IReadOnlyList<OrderCommittedCarrierExecutableLine> ExecutableLines,
    Guid? FulfillmentId = null,
    /// <summary>Sum of packed quantities across group lines (eligibility / residual accounting).</summary>
    decimal? GroupPackedQuantityTotal = null,
    /// <summary>All packed group lines with unit prices for exact declared/collect allocation.</summary>
    IReadOnlyList<OrderCommittedCarrierLineValueFact>? GroupPackedLineValues = null,
    /// <summary>Committed catalogue service code (exact identity; not a registry prefix).</summary>
    string? ServiceCode = null,
    /// <summary>Committed catalogue option code (exact identity; not a registry prefix).</summary>
    string? OptionCode = null);

/// <summary>Contracts-only lookup of committed shipping groups. Empty when the order is not committed.</summary>
public interface IOrderCommittedCarrierShipmentFactsReader
{
    /// <summary>فکت‌های گروه متعهد را می‌خواند؛ نبود گروه یعنی واجد شرایط نیست.</summary>
    Task<OrderCommittedCarrierShipmentFacts?> GetAsync(
        Guid checkoutId,
        Guid shippingGroupId,
        CancellationToken cancellationToken);

    /// <summary>همهٔ گروه‌های متعهد یک checkout را برای اجرای حامل برمی‌گرداند.</summary>
    Task<IReadOnlyList<OrderCommittedCarrierShipmentFacts>> ListByCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken);
}
