namespace Tooba.Fulfillment.Contracts.Operations;

/// <summary>Fulfillment unit status exposed through the admin operations contract.</summary>
public enum FulfillmentOperationStatus
{
    /// <summary>Paid and ready for warehouse work.</summary>
    ReadyToFulfill = 0,

    /// <summary>Warehouse processing started.</summary>
    Processing = 1,

    /// <summary>Packed.</summary>
    Packed = 2,

    /// <summary>At least one shipment dispatched.</summary>
    Dispatched = 3,

    /// <summary>In transit.</summary>
    InTransit = 4,

    /// <summary>Delivered.</summary>
    Delivered = 5,

    /// <summary>Operationally failed.</summary>
    Failed = 6,

    /// <summary>Cancelled.</summary>
    Cancelled = 7,
}

/// <summary>Shipment status exposed through the admin operations contract.</summary>
public enum ShipmentOperationStatus
{
    /// <summary>Created, not dispatched.</summary>
    Created = 0,

    /// <summary>Dispatched.</summary>
    Dispatched = 1,

    /// <summary>In transit.</summary>
    InTransit = 2,

    /// <summary>Delivered.</summary>
    Delivered = 3,

    /// <summary>Failed.</summary>
    Failed = 4,

    /// <summary>Cancelled.</summary>
    Cancelled = 5,

    /// <summary>TB-258: post-dispatch non-delivery attempt recorded (not delivered, not cancelled).</summary>
    NonDelivered = 6,

    /// <summary>TB-258: the carrier is physically returning the parcel to origin.</summary>
    Returning = 7,

    /// <summary>TB-258: origin receipt confirmed; received usable quantity may be resent.</summary>
    Returned = 8,
}

/// <summary>Consolidated package status exposed through the admin operations contract.</summary>
public enum ConsolidatedPackageOperationStatus
{
    /// <summary>Created, can be cancelled or dispatched centrally.</summary>
    Created = 0,

    /// <summary>All member shipments dispatched.</summary>
    Dispatched = 1,

    /// <summary>All member shipments delivered.</summary>
    Delivered = 2,

    /// <summary>Cancelled before dispatch.</summary>
    Cancelled = 3,
}

/// <summary>One order line allocated into a shipment.</summary>
public sealed record ShipmentLineSnapshot(Guid OrderLineId, decimal Quantity);

/// <summary>Shipment projection for admin operations. Field order mirrors the wire contract.</summary>
public sealed record ShipmentSnapshot(
    Guid ShipmentId,
    ShipmentOperationStatus Status,
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
    /// <summary>
    /// TB-253-R3: durable occurrence-scoped allocation key recorded on the shipment itself.
    /// Recovery reads exact recorded parcels by this key instead of guessing by aggregate quantity.
    /// </summary>
    string? ShippingDispatchKey = null,
    /// <summary>TB-258: the failed/returned original this parcel resends; null for a first dispatch.</summary>
    Guid? ResendOfShipmentId = null,
    /// <summary>TB-258: non-delivery reason code recorded by an operator (internal fact, not a provider code).</summary>
    string? NonDeliveryReason = null,
    /// <summary>TB-258: operator note of the non-delivery attempt.</summary>
    string? NonDeliveryNote = null,
    /// <summary>TB-258: server-recorded non-delivery time.</summary>
    DateTimeOffset? NonDeliveredAt = null,
    /// <summary>TB-258: when the carrier started returning the parcel.</summary>
    DateTimeOffset? ReturnStartedAt = null,
    /// <summary>TB-258: when the origin receipt was confirmed.</summary>
    DateTimeOffset? ReturnedAt = null,
    /// <summary>TB-258: recorded physical origin receipt (null until the parcel is received back).</summary>
    ReturnReceiptSnapshot? ReturnReceipt = null);

/// <summary>TB-258: physical condition of a received returned line; only Usable may be resent.</summary>
public enum ReturnReceiptCondition
{
    /// <summary>Received and physically usable.</summary>
    Usable = 0,

    /// <summary>Received but damaged / not resendable.</summary>
    Damaged = 1,
}

/// <summary>TB-258: the recorded origin receipt of one returned parcel.</summary>
public sealed record ReturnReceiptSnapshot(
    Guid ReceivedReceiptId,
    DateTimeOffset ReceivedAt,
    Guid ReceivedByUserId,
    string? Note,
    IReadOnlyList<ReturnReceiptLineSnapshot> Items)
{
    /// <summary>Physically usable received quantity of the whole receipt.</summary>
    public decimal UsableQuantity => Items
        .Where(x => x.Condition == ReturnReceiptCondition.Usable)
        .Sum(x => x.Quantity);
}

/// <summary>TB-258: one received line of an origin receipt.</summary>
public sealed record ReturnReceiptLineSnapshot(
    Guid OrderLineId,
    decimal Quantity,
    ReturnReceiptCondition Condition);

/// <summary>TB-258: one physically received line submitted with an origin receipt.</summary>
public sealed record ReturnReceiptLineCommand(
    Guid OrderLineId,
    decimal Quantity,
    ReturnReceiptCondition Condition = ReturnReceiptCondition.Usable);

/// <summary>TB-258: operator reason codes for a post-dispatch non-delivery attempt.</summary>
public static class FulfillmentNonDeliveryReasons
{
    /// <summary>Customer was not reachable at the address.</summary>
    public const string CustomerUnavailable = "customer_unavailable";

    /// <summary>Address could not be located.</summary>
    public const string AddressNotFound = "address_not_found";

    /// <summary>Recipient refused to accept the parcel.</summary>
    public const string Refused = "refused";

    /// <summary>Any other documented operational reason.</summary>
    public const string Other = "other";

    /// <summary>The complete allowed set; the server rejects anything else.</summary>
    public static readonly IReadOnlyList<string> All =
    [
        CustomerUnavailable,
        AddressNotFound,
        Refused,
        Other,
    ];

    /// <summary>True when the code is one of the server-owned reasons.</summary>
    public static bool IsKnown(string? reason) =>
        reason is not null && All.Contains(reason.Trim(), StringComparer.Ordinal);
}

/// <summary>Fulfillment line projection with per-stage quantities.</summary>
public sealed record FulfillmentItemSnapshot(
    Guid FulfillmentItemId,
    Guid OrderLineId,
    decimal QuantityOrdered,
    decimal QuantityShipped,
    Guid? ReservationId,
    decimal QuantityPacked = 0,
    decimal QuantityProcessing = 0,
    Guid? ShippingGroupId = null,
    /// <summary>TB-258: physically received usable returned quantity of this line.</summary>
    decimal ReturnedUsableQuantity = 0,
    /// <summary>TB-258: returned capacity committed to non-cancelled resend parcels.</summary>
    decimal ResendCommittedQuantity = 0,
    /// <summary>TB-258: returned capacity still available for a new resend parcel (separate from packed remainder).</summary>
    decimal ResendableQuantity = 0,
    /// <summary>TB-258: quantity actually delivered (derived from delivered parcels, not from shipped).</summary>
    decimal DeliveredQuantity = 0);

/// <summary>Seller fulfillment projection for admin operations.</summary>
public sealed record FulfillmentSnapshot(
    Guid FulfillmentId,
    Guid SellerOrderId,
    Guid CheckoutId,
    Guid SellerPartyId,
    FulfillmentOperationStatus Status,
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

/// <summary>Exact shipment identity created by one CreateShipment allocation.</summary>
public sealed record FulfillmentShipmentCreateResult(
    FulfillmentSnapshot Snapshot,
    Guid CreatedShipmentId);

/// <summary>Member shipment of a consolidated package.</summary>
public sealed record ConsolidatedPackageMemberSnapshot(
    Guid ConsolidatedPackageMemberId,
    Guid ShipmentId,
    Guid SellerPartyId,
    Guid FulfillmentId,
    DateTimeOffset JoinedAt,
    DateTimeOffset? ReleasedAt);

/// <summary>Consolidated multi-seller package projection.</summary>
public sealed record ConsolidatedPackageSnapshot(
    Guid ConsolidatedPackageId,
    string PackageNumber,
    Guid CheckoutId,
    ConsolidatedPackageOperationStatus Status,
    string ShippingMethodCode,
    string ShippingMethodLabel,
    string? TrackingReference,
    string? Note,
    Guid? CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? DispatchedAt,
    DateTimeOffset? DeliveredAt,
    DateTimeOffset? CancelledAt,
    IReadOnlyList<ConsolidatedPackageMemberSnapshot> Members);

/// <summary>Active membership of one shipment inside a non-cancelled package.</summary>
public sealed record ActivePackageMembershipSnapshot(
    Guid ShipmentId,
    Guid ConsolidatedPackageId,
    string PackageNumber,
    ConsolidatedPackageOperationStatus PackageStatus);

/// <summary>Line/quantity selection for a seller-scoped fulfillment operation.</summary>
public sealed record FulfillmentSelectionCommand(Guid OrderLineId, decimal Quantity);

/// <summary>Line/quantity allocation for a shipment.</summary>
public sealed record ShipmentLineCommand(Guid OrderLineId, decimal Quantity);

/// <summary>Enabled store shipping method exposed for admin shipment creation.</summary>
public sealed record ShippingMethodOption(string Code, string LabelFa, string ProviderKind);

/// <summary>
/// Fulfillment operations required by admin order lifecycle, exposed without owner
/// Application/Domain types. Implemented by Fulfillment.Infrastructure.
/// </summary>
public interface IFulfillmentAdminOperations
{
    /// <summary>One fulfillment unit.</summary>
    Task<FulfillmentSnapshot?> GetAsync(Guid fulfillmentId, CancellationToken cancellationToken);

    /// <summary>Seller fulfillment units of one checkout.</summary>
    Task<IReadOnlyList<FulfillmentSnapshot>> ListForCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken);

    /// <summary>Consolidated packages of one checkout, including historical ones.</summary>
    Task<IReadOnlyList<ConsolidatedPackageSnapshot>> GetPackagesForCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken);

    /// <summary>Active package membership for the given shipments.</summary>
    Task<IReadOnlyList<ActivePackageMembershipSnapshot>> GetActiveMembershipByShipmentIdsAsync(
        IReadOnlyList<Guid> shipmentIds,
        CancellationToken cancellationToken);

    /// <summary>Enabled store shipping methods.</summary>
    IReadOnlyList<ShippingMethodOption> ListEnabledShippingMethods();

    /// <summary>Display label of a shipping method code, falling back to the supplied name.</summary>
    string ResolveShippingMethodLabel(string? code, string? fallbackDisplayName);

    /// <summary>Move the whole unit to processing.</summary>
    Task<FulfillmentSnapshot> MarkProcessingAsync(Guid fulfillmentId, Guid actorUserId, CancellationToken cancellationToken);

    /// <summary>Process the selected quantities.</summary>
    Task<FulfillmentSnapshot> ProcessSelectionsAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        IReadOnlyList<FulfillmentSelectionCommand> selections,
        CancellationToken cancellationToken);

    /// <summary>Revert processing for unpacked quantities.</summary>
    Task<FulfillmentSnapshot> UnprocessSelectionsAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        IReadOnlyList<FulfillmentSelectionCommand> selections,
        CancellationToken cancellationToken);

    /// <summary>Pack the selected quantities.</summary>
    Task<FulfillmentSnapshot> PackSelectionsAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        IReadOnlyList<FulfillmentSelectionCommand> selections,
        CancellationToken cancellationToken);

    /// <summary>Unpack the selected quantities.</summary>
    Task<FulfillmentSnapshot> UnpackSelectionsAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        IReadOnlyList<FulfillmentSelectionCommand> selections,
        CancellationToken cancellationToken);

    /// <summary>Create a shipment for the given allocations and return its exact identity.</summary>
    /// <param name="shippingDispatchKey">Optional durable Shipping intent key for idempotent reuse after link-write failure.</param>
    Task<FulfillmentShipmentCreateResult> CreateShipmentAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        string carrierDisplayName,
        IReadOnlyList<ShipmentLineCommand> items,
        string? shippingMethodCode,
        string? providerMetadataJson,
        CancellationToken cancellationToken,
        Guid? shippingGroupId = null,
        string? shippingDispatchKey = null);

    /// <summary>Void a shipment before dispatch.</summary>
    Task<FulfillmentSnapshot> CancelShipmentAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    /// <summary>Assign a tracking reference idempotently.</summary>
    Task<FulfillmentSnapshot> AssignTrackingAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        string trackingReference,
        CancellationToken cancellationToken);

    /// <summary>
    /// TB-257: allocate the server-owned internal tracking reference to a parcel that has none. Deterministic per
    /// parcel, reconciled on retry, retried internally on a real database collision.
    /// </summary>
    Task<FulfillmentSnapshot> AssignAutomaticTrackingAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    /// <summary>Correct a tracking reference before dispatch.</summary>
    Task<FulfillmentSnapshot> CorrectTrackingAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        string trackingReference,
        CancellationToken cancellationToken);

    /// <summary>Record shipment dispatch.</summary>
    Task<FulfillmentSnapshot> DispatchShipmentAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    /// <summary>Record shipment delivery.</summary>
    Task<FulfillmentSnapshot> DeliverShipmentAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// TB-258: record a post-dispatch courier non-delivery attempt. Internal audited fact — never a provider
    /// cancellation, never a fake delivery and never a cancel-after-dispatch shortcut.
    /// </summary>
    Task<FulfillmentSnapshot> RecordNonDeliveryAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        string reason,
        string? note,
        CancellationToken cancellationToken);

    /// <summary>TB-258: the carrier started bringing the failed parcel back to origin.</summary>
    Task<FulfillmentSnapshot> StartReturnAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// TB-258: confirm the physical origin receipt exactly once. Only usable received quantity becomes resendable;
    /// an identical replay reconciles, a different payload is a typed conflict.
    /// </summary>
    Task<FulfillmentSnapshot> RecordReturnReceiptAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        IReadOnlyList<ReturnReceiptLineCommand> items,
        string? note,
        CancellationToken cancellationToken);

    /// <summary>
    /// TB-258: allocate a NEW resend parcel occurrence for returned goods against the recorded origin receipt,
    /// reusing the accepted shipping group/method of the original and never the fresh packed remainder.
    /// </summary>
    Task<FulfillmentShipmentCreateResult> CreateResendShipmentAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        Guid originalShipmentId,
        IReadOnlyList<ShipmentLineCommand> items,
        CancellationToken cancellationToken,
        string? shippingDispatchKey = null);

    /// <summary>Create a multi-seller consolidated package.</summary>
    Task<ConsolidatedPackageSnapshot> CreateConsolidatedPackageAsync(
        Guid checkoutId,
        IReadOnlyList<Guid> shipmentIds,
        string? shippingMethodCode,
        string? trackingReference,
        string? note,
        Guid actorUserId,
        CancellationToken cancellationToken);

    /// <summary>Void a consolidated package before central dispatch.</summary>
    Task<ConsolidatedPackageSnapshot> CancelConsolidatedPackageAsync(
        Guid consolidatedPackageId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    /// <summary>Assign the central tracking reference of a consolidated package.</summary>
    Task<ConsolidatedPackageSnapshot> AssignConsolidatedPackageTrackingAsync(
        Guid consolidatedPackageId,
        string trackingReference,
        Guid actorUserId,
        CancellationToken cancellationToken);

    /// <summary>Dispatch a consolidated package centrally.</summary>
    Task<ConsolidatedPackageSnapshot> DispatchConsolidatedPackageAsync(
        Guid consolidatedPackageId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    /// <summary>Deliver a consolidated package centrally.</summary>
    Task<ConsolidatedPackageSnapshot> DeliverConsolidatedPackageAsync(
        Guid consolidatedPackageId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    /// <summary>Abort pre-dispatch fulfillment work when the order is cancelled.</summary>
    Task AbortForCheckoutCancelAsync(Guid checkoutId, CancellationToken cancellationToken);

    /// <summary>Reactivate cancelled units after an order restore.</summary>
    Task ReactivateAfterOrderRestoreAsync(Guid checkoutId, CancellationToken cancellationToken);

    /// <summary>Create missing units for paid seller orders.</summary>
    Task EnsureCreatedForPaidCheckoutAsync(
        Guid checkoutId,
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken);

    /// <summary>Remove not-started units after a deposit confirmation is reverted.</summary>
    Task VoidUnstartedForCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken);

    /// <summary>
    /// Rebind active fulfillment item reservations from current Order line bindings
    /// without reactivating cancelled units.
    /// </summary>
    Task RebindActiveReservationsFromOrderAsync(Guid checkoutId, CancellationToken cancellationToken);

    /// <summary>Batch QuantityShipped by OrderLineId for the given checkouts.</summary>
    Task<IReadOnlyDictionary<Guid, decimal>> GetShippedByOrderLineIdsForCheckoutsAsync(
        IReadOnlyList<Guid> checkoutIds,
        CancellationToken cancellationToken);
}
