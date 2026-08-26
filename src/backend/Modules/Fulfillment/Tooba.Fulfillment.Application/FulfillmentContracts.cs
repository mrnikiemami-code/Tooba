using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Domain;

namespace Tooba.Fulfillment.Application;

/// <summary>
/// درز موجودی برای dispatch؛ Fulfillment مستقیم Inventory DbContext باز نمی‌کند.
/// </summary>
public interface IFulfillmentInventoryGateway
{
    /// <summary>
    /// رزرو را پس از dispatch کامل خط مصرف می‌کند.
    /// </summary>
    Task ConsumeReservationAsync(Guid reservationId, CancellationToken cancellationToken);
}

/// <summary>
/// خط محموله در فرمان.
/// </summary>
public sealed record ShipmentLineCommand(Guid OrderLineId, int Quantity);

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
    IReadOnlyList<ShipmentSnapshot> Shipments);

/// <summary>
/// snapshot خط fulfillment.
/// </summary>
public sealed record FulfillmentItemSnapshot(
    Guid FulfillmentItemId,
    Guid OrderLineId,
    int QuantityOrdered,
    int QuantityShipped,
    Guid? ReservationId);

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
    IReadOnlyList<ShipmentLineSnapshot> Items);

/// <summary>
/// snapshot خط محموله.
/// </summary>
public sealed record ShipmentLineSnapshot(Guid OrderLineId, int Quantity);

/// <summary>
/// ارکستراسیون fulfillment.
/// </summary>
public interface IFulfillmentDirectory
{
    /// <summary>fulfillment را می‌خواند.</summary>
    Task<FulfillmentSnapshot?> GetAsync(Guid fulfillmentId, CancellationToken cancellationToken);

    /// <summary>fulfillment را با SellerOrder می‌خواند.</summary>
    Task<FulfillmentSnapshot?> GetBySellerOrderAsync(Guid sellerOrderId, CancellationToken cancellationToken);

    /// <summary>فهرست fulfillment یک فروشنده.</summary>
    Task<IReadOnlyList<FulfillmentSnapshot>> ListForSellerAsync(Guid sellerPartyId, CancellationToken cancellationToken);

    /// <summary>فهرست همه fulfillmentها برای admin.</summary>
    Task<IReadOnlyList<FulfillmentSnapshot>> ListAllAsync(CancellationToken cancellationToken);

    /// <summary>fulfillmentهای یک checkout.</summary>
    Task<IReadOnlyList<FulfillmentSnapshot>> ListForCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken);

    /// <summary>به Processing می‌رود.</summary>
    Task<FulfillmentSnapshot> MarkProcessingAsync(Guid fulfillmentId, Guid actorUserId, CancellationToken cancellationToken);

    /// <summary>به Packed می‌رود.</summary>
    Task<FulfillmentSnapshot> MarkPackedAsync(Guid fulfillmentId, Guid actorUserId, CancellationToken cancellationToken);

    /// <summary>محموله می‌سازد.</summary>
    Task<FulfillmentSnapshot> CreateShipmentAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        string carrierDisplayName,
        IReadOnlyList<ShipmentLineCommand> items,
        CancellationToken cancellationToken);

    /// <summary>tracking idempotent ثبت می‌کند.</summary>
    Task<FulfillmentSnapshot> AssignTrackingAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        string trackingReference,
        CancellationToken cancellationToken);

    /// <summary>محموله را dispatch می‌کند.</summary>
    Task<FulfillmentSnapshot> DispatchShipmentAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    /// <summary>محموله را delivered علامت می‌زند.</summary>
    Task<FulfillmentSnapshot> DeliverShipmentAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        CancellationToken cancellationToken);
}

/// <summary>
/// snapshot eligibility مرجوعی از fulfillment.
/// </summary>
public sealed record FulfillmentReturnEligibilitySnapshot(
    Guid SellerOrderId,
    IReadOnlyDictionary<Guid, int> DeliveredQuantities,
    DateTimeOffset? LastDeliveredAt);

/// <summary>
/// خواندن evidence تحویل برای Returns بدون cross-DbContext.
/// </summary>
public interface IFulfillmentReturnReader
{
    /// <summary>
    /// snapshot eligibility مرجوعی را برمی‌گرداند.
    /// </summary>
    Task<FulfillmentReturnEligibilitySnapshot?> GetEligibilityAsync(
        Guid sellerOrderId,
        CancellationToken cancellationToken);
}

/// <summary>
/// نگهبان use-case fulfillment.
/// </summary>
public interface IFulfillmentUseCaseGuard
{
    /// <summary>اجازهٔ mutate را بررسی می‌کند.</summary>
    Task EnsureCanMutateAsync(CancellationToken cancellationToken);
}

/// <summary>
/// رویداد Outbox fulfillment.created.v1
/// </summary>
public sealed class FulfillmentCreatedIntegrationEvent : IIntegrationEvent
{
    /// <summary>نام قرارداد.</summary>
    public const string EventTypeName = "fulfillment.created.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>شناسه fulfillment.</summary>
    public Guid FulfillmentId { get; set; }

    /// <summary>سفارش فروشنده.</summary>
    public Guid SellerOrderId { get; set; }

    /// <summary>checkout مرجع.</summary>
    public Guid CheckoutId { get; set; }
}

/// <summary>
/// رویداد Outbox shipment.dispatched.v1
/// </summary>
public sealed class ShipmentDispatchedIntegrationEvent : IIntegrationEvent
{
    /// <summary>نام قرارداد.</summary>
    public const string EventTypeName = "shipment.dispatched.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>شناسه fulfillment.</summary>
    public Guid FulfillmentId { get; set; }

    /// <summary>شناسه محموله.</summary>
    public Guid ShipmentId { get; set; }

    /// <summary>سفارش فروشنده.</summary>
    public Guid SellerOrderId { get; set; }
}
