using Tooba.Fulfillment.Application.Models;
using Tooba.Fulfillment.Domain.ValueObjects;

namespace Tooba.Fulfillment.Application.Ports;


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
