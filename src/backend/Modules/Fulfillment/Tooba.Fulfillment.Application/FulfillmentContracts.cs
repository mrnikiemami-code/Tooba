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

    /// <summary>
    /// رزرو Held سفارش پرداخت‌شده را از TTL سبد خارج می‌کند (ExpiresAt=null). Idempotent.
    /// </summary>
    Task CommitReservationForPaidOrderAsync(Guid reservationId, CancellationToken cancellationToken);
}

/// <summary>
/// خط محموله در فرمان.
/// </summary>
public sealed record ShipmentLineCommand(Guid OrderLineId, decimal Quantity);

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

/// <summary>
/// snapshot خط fulfillment.
/// </summary>
public sealed record FulfillmentItemSnapshot(
    Guid FulfillmentItemId,
    Guid OrderLineId,
    decimal QuantityOrdered,
    decimal QuantityShipped,
    Guid? ReservationId,
    decimal QuantityPacked = 0,
    decimal QuantityProcessing = 0);

/// <summary>
/// انتخاب خط/تعداد برای عملیات seller-scoped.
/// </summary>
public sealed record FulfillmentSelectionCommand(Guid OrderLineId, decimal Quantity);

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
    string? PreviousTrackingReference = null);

/// <summary>
/// snapshot خط محموله.
/// </summary>
public sealed record ShipmentLineSnapshot(Guid OrderLineId, decimal Quantity);

/// <summary>
/// عضو بسته تجمیعی در snapshot.
/// </summary>
public sealed record ConsolidatedPackageMemberSnapshot(
    Guid ConsolidatedPackageMemberId,
    Guid ShipmentId,
    Guid SellerPartyId,
    Guid FulfillmentId,
    DateTimeOffset JoinedAt,
    DateTimeOffset? ReleasedAt);

/// <summary>
/// snapshot بسته تجمیعی.
/// </summary>
public sealed record ConsolidatedPackageSnapshot(
    Guid ConsolidatedPackageId,
    string PackageNumber,
    Guid CheckoutId,
    ConsolidatedPackageStatus Status,
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

/// <summary>
/// عضویت فعال مرسوله در بسته تجمیعی.
/// </summary>
public sealed record ActivePackageMembershipSnapshot(
    Guid ShipmentId,
    Guid ConsolidatedPackageId,
    string PackageNumber,
    ConsolidatedPackageStatus PackageStatus);

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

    /// <summary>پردازش انتخاب‌شده.</summary>
    Task<FulfillmentSnapshot> ProcessSelectionsAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        IReadOnlyList<FulfillmentSelectionCommand> selections,
        CancellationToken cancellationToken);

    /// <summary>برگشت از پردازش برای تعداد بسته‌بندی‌نشده.</summary>
    Task<FulfillmentSnapshot> UnprocessSelectionsAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        IReadOnlyList<FulfillmentSelectionCommand> selections,
        CancellationToken cancellationToken);

    /// <summary>به Packed می‌رود.</summary>
    Task<FulfillmentSnapshot> MarkPackedAsync(Guid fulfillmentId, Guid actorUserId, CancellationToken cancellationToken);

    /// <summary>بسته‌بندی انتخاب‌شده (یا کل باقیمانده وقتی selections خالی است در لایهٔ Host).</summary>
    Task<FulfillmentSnapshot> PackSelectionsAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        IReadOnlyList<FulfillmentSelectionCommand> selections,
        CancellationToken cancellationToken);

    /// <summary>بازگشت از بسته‌بندی برای تعداد تخصیص‌نشده.</summary>
    Task<FulfillmentSnapshot> UnpackSelectionsAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        IReadOnlyList<FulfillmentSelectionCommand> selections,
        CancellationToken cancellationToken);

    /// <summary>محموله می‌سازد.</summary>
    Task<FulfillmentSnapshot> CreateShipmentAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        string carrierDisplayName,
        IReadOnlyList<ShipmentLineCommand> items,
        CancellationToken cancellationToken,
        string? shippingMethodCode = null,
        string? providerMetadataJson = null);

    /// <summary>ابطال مرسوله پیش از dispatch.</summary>
    Task<FulfillmentSnapshot> CancelShipmentAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    /// <summary>tracking idempotent ثبت می‌کند.</summary>
    Task<FulfillmentSnapshot> AssignTrackingAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        string trackingReference,
        CancellationToken cancellationToken);

    /// <summary>کد رهگیری را پیش از dispatch اصلاح می‌کند.</summary>
    Task<FulfillmentSnapshot> CorrectTrackingAsync(
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

    /// <summary>
    /// fulfillment شروع‌نشده را پس از برگشت تأیید پرداخت حذف می‌کند.
    /// </summary>
    Task VoidUnstartedForCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken);

    /// <summary>
    /// برای سفارش‌های Paid که هنوز fulfillment ندارند، واحد ReadyToFulfill می‌سازد.
    /// </summary>
    Task EnsureCreatedForPaidCheckoutAsync(
        Guid checkoutId,
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// مرسوله‌های پیش از Dispatch را باطل، پیشرفت انبار ارسال‌نشده را بازنشانی و واحدها را Cancelled می‌کند؛ idempotent.
    /// </summary>
    Task AbortForCheckoutCancelAsync(Guid checkoutId, CancellationToken cancellationToken);

    /// <summary>
    /// واحدهای Cancelled را با انبار صفر به ReadyToFulfill برمی‌گرداند و مرجع رزرو فعال را
    /// از Order handoff فعلی بازمی‌بندد؛ مرسوله‌های باطل‌شده را زنده نمی‌کند.
    /// </summary>
    Task ReactivateAfterOrderRestoreAsync(Guid checkoutId, CancellationToken cancellationToken);

    /// <summary>
    /// فقط مرجع رزرو فعال را از Order handoff فعلی بازمی‌بندد (بدون تغییر وضعیت Cancelled).
    /// </summary>
    Task RebindActiveReservationsFromOrderAsync(Guid checkoutId, CancellationToken cancellationToken);

    /// <summary>بسته‌های تجمیعی یک checkout (شامل تاریخی).</summary>
    Task<IReadOnlyList<ConsolidatedPackageSnapshot>> GetPackagesForCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken);

    /// <summary>عضویت فعال مرسوله‌ها در بستهٔ غیر Cancelled.</summary>
    Task<IReadOnlyList<ActivePackageMembershipSnapshot>> GetActiveMembershipByShipmentIdsAsync(
        IReadOnlyList<Guid> shipmentIds,
        CancellationToken cancellationToken);

    /// <summary>آیا مرسوله توسط بستهٔ فعال (Created/Dispatched) قفل است.</summary>
    Task<bool> IsShipmentLockedByPackageAsync(Guid shipmentId, CancellationToken cancellationToken);

    /// <summary>
    /// بسته تجمیعی چندفروشنده‌ای می‌سازد.
    /// روش ارسال از مرسوله‌های عضو inherit می‌شود؛ در صورت ارسال کد، باید با همان روش مشترک یکی باشد.
    /// </summary>
    Task<ConsolidatedPackageSnapshot> CreateConsolidatedPackageAsync(
        Guid checkoutId,
        IReadOnlyList<Guid> shipmentIds,
        string? shippingMethodCode,
        string? trackingReference,
        string? note,
        Guid actorUserId,
        CancellationToken cancellationToken);

    /// <summary>ابطال بسته پیش از ارسال مرکزی و آزادسازی قفل اعضا.</summary>
    Task<ConsolidatedPackageSnapshot> CancelConsolidatedPackageAsync(
        Guid consolidatedPackageId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    /// <summary>ثبت/به‌روزرسانی کد رهگیری مرکزی بسته (فقط قبل از ارسال).</summary>
    Task<ConsolidatedPackageSnapshot> AssignConsolidatedPackageTrackingAsync(
        Guid consolidatedPackageId,
        string trackingReference,
        Guid actorUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// ارسال مرکزی: برای هر عضو <see cref="DispatchShipmentAsync"/> را صدا می‌زند؛
    /// بسته فقط وقتی همه اعضا ارسال شدند Dispatched می‌شود.
    /// </summary>
    Task<ConsolidatedPackageSnapshot> DispatchConsolidatedPackageAsync(
        Guid consolidatedPackageId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// تحویل مرکزی: برای هر عضو <see cref="DeliverShipmentAsync"/> را صدا می‌زند؛
    /// بسته فقط وقتی همه اعضا تحویل شدند Delivered می‌شود.
    /// </summary>
    Task<ConsolidatedPackageSnapshot> DeliverConsolidatedPackageAsync(
        Guid consolidatedPackageId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// بسته‌های Created فعال checkout را باطل می‌کند (مسیر لغو سفارش).
    /// </summary>
    Task VoidActivePackagesForCheckoutCancelAsync(Guid checkoutId, CancellationToken cancellationToken);
}

/// <summary>
/// برش تحویل یک خط در یک مرسولهٔ Delivered (ساعت مرجوعی per-slice).
/// </summary>
public sealed record LineDeliverySlice(
    Guid OrderLineId,
    decimal Quantity,
    DateTimeOffset DeliveredAt);

/// <summary>
/// snapshot eligibility مرجوعی از fulfillment.
/// </summary>
public sealed record FulfillmentReturnEligibilitySnapshot(
    Guid SellerOrderId,
    IReadOnlyDictionary<Guid, decimal> DeliveredQuantities,
    DateTimeOffset? LastDeliveredAt,
    IReadOnlyDictionary<Guid, DateTimeOffset>? LineDeliveredAt = null,
    IReadOnlyList<LineDeliverySlice>? DeliverySlices = null);

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
