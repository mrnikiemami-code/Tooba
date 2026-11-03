namespace Tooba.Order.Contracts.Shipping;

/// <summary>Remaining packed quantity on one accepted group line available for a new dispatch.</summary>
public sealed record OrderCommittedCarrierExecutableLine(Guid OrderLineId, decimal RemainingPackedQuantity);

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
    Guid? FulfillmentId = null);

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
