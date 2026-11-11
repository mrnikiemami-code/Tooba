using Tooba.Fulfillment.Application.Fulfillments.Models;
using Tooba.Fulfillment.Domain.ValueObjects;

namespace Tooba.Fulfillment.Application.Fulfillments.Ports;


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

    /// <summary>محموله می‌سازد و شناسهٔ دقیق همان محموله را برمی‌گرداند.</summary>
    Task<FulfillmentShipmentCreateResult> CreateShipmentAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        string carrierDisplayName,
        IReadOnlyList<ShipmentLineCommand> items,
        CancellationToken cancellationToken,
        string? shippingMethodCode = null,
        string? providerMetadataJson = null,
        Guid? shippingGroupId = null,
        string? shippingDispatchKey = null);

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

    /// <summary>
    /// TB-257: assigns the server-owned internal reference to a parcel that has none yet. A parcel that already
    /// carries a code is returned unchanged (reconcile, never replace). Collisions are retried internally with a
    /// bounded fresh candidate against the real database unique index.
    /// </summary>
    Task<FulfillmentSnapshot> AssignAutomaticTrackingAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
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
    /// TB-258: records a courier non-delivery attempt on a dispatched parcel. It is an internal audited fact — no
    /// provider cancellation, no fake delivery and no cancel-after-dispatch shortcut.
    /// </summary>
    Task<FulfillmentSnapshot> RecordNonDeliveryAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        string reason,
        string? note,
        CancellationToken cancellationToken);

    /// <summary>TB-258: the carrier started physically returning the failed parcel to origin.</summary>
    Task<FulfillmentSnapshot> StartReturnAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// TB-258: confirms the physical origin receipt. Only the received USABLE quantity becomes resend capacity, the
    /// receipt is recorded exactly once per parcel, and a repeated identical submission reconciles it.
    /// </summary>
    Task<FulfillmentSnapshot> RecordReturnReceiptAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        IReadOnlyList<ReturnReceiptLineCommand> items,
        string? note,
        CancellationToken cancellationToken);

    /// <summary>
    /// TB-258: allocates a NEW parcel occurrence for returned goods against the recorded origin receipt, reusing the
    /// accepted shipping group/method of the original and never the fresh packed remainder.
    /// </summary>
    Task<FulfillmentShipmentCreateResult> CreateResendShipmentAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        Guid originalShipmentId,
        IReadOnlyList<ShipmentLineCommand> items,
        CancellationToken cancellationToken,
        string? shippingDispatchKey = null);

    /// <summary>
    /// TB-258-R2: READ-ONLY exact recorded-intent lookup for the post-dispatch exception path. It answers whether
    /// this exact reviewed intent is already a durable fact of the owning unit, so a lost response can be
    /// reconciled through the public command after the fresh projected action disappeared. It never writes,
    /// never allocates, never authorizes and never weakens a readiness gate.
    /// </summary>
    Task<PostDispatchIntentRecord> ReconcilePostDispatchIntentAsync(
        PostDispatchIntent intent,
        CancellationToken cancellationToken);

    /// <summary>
    /// TB-262: records the durable operator decision for a DAMAGED quantity of one returned parcel line. For a
    /// replacement it also reserves NEW healthy stock of the same ordered offer and creates the linked replacement
    /// parcel. Damaged units never become resend capacity and never return to saleable stock; the decision is
    /// idempotent per occurrence key and bounded by the source parcel's own outstanding damaged quantity.
    /// </summary>
    Task<FulfillmentDamagedResolution> ResolveDamagedGoodsAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        Guid shipmentId,
        Guid orderLineId,
        decimal quantity,
        DamagedResolutionOutcome outcome,
        string? occurrenceKey,
        string? note,
        CancellationToken cancellationToken);

    /// <summary>
    /// TB-262: READ-ONLY exact-intent lookup of a recorded damaged-goods decision. It never writes, never reserves,
    /// never allocates and never authorizes; it only lets a lost response be reconciled through the public command.
    /// </summary>
    Task<FulfillmentDamagedIntentRecord> ReconcileDamagedResolutionAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid orderLineId,
        decimal quantity,
        DamagedResolutionOutcome outcome,
        string? occurrenceKey,
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
