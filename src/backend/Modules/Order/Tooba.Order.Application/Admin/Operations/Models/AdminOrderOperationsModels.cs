using Tooba.Returns.Contracts.Operations;

namespace Tooba.Order.Application.Admin.Operations.Models;

/// <summary>یک اقدام lifecycle قابل‌نمایش برای سفارش ادمین.</summary>
public sealed record AdminOrderOperationAction(
    string Code,
    string LabelFa,
    string LabelEn,
    Guid? SellerOrderId,
    Guid? FulfillmentId,
    Guid? ShipmentId,
    Guid? ReturnRequestId,
    string RequiredPermission,
    bool RequiresConfirm,
    string? ConfirmMessageFa,
    Guid? OrderLineId = null,
    Guid? ConsolidatedPackageId = null,
    /// <summary>
    /// TB-259: packing choices the SERVER permits for this action, in operator order. The UI renders
    /// exactly these choices and never decides availability itself.
    /// </summary>
    IReadOnlyList<string>? PackingModes = null);

/// <summary>قابلیت lifecycle یک خط — منبع انتخاب/کebab/مرسوله.</summary>
public sealed record AdminOrderLineCapability(
    Guid OrderLineId,
    Guid SellerOrderId,
    bool Selectable,
    decimal SelectableQuantityMax,
    IReadOnlyList<string> RowActionCodes,
    IReadOnlyList<string> BulkActionCodes,
    decimal ShipmentEligibleQuantity,
    string? LockedReasonCode = null,
    string? LockedReasonFa = null);

/// <summary>قابلیت تجمیعی فروشنده برای اقلام و ارسال.</summary>
public sealed record AdminSellerCapability(
    Guid SellerOrderId,
    bool SelectionAllowed,
    bool PaymentLocked,
    string? InfoMessageFa,
    IReadOnlyList<string> WholeGroupActionCodes,
    bool ShipmentCreationPossible);

/// <summary>صفحهٔ عملیات سفارش برای یک checkout.</summary>
public sealed record AdminOrderOperationsPage(
    Guid CheckoutId,
    IReadOnlyList<AdminOrderOperationAction> Actions,
    IReadOnlyList<ReturnEligibilityResult> ReturnEligibility,
    IReadOnlyList<AdminOrderLineCapability>? LineCapabilities = null,
    IReadOnlyList<AdminSellerCapability>? SellerCapabilities = null,
    string? InventoryRecoveryWarningFa = null,
    string? InventoryRecoveryClass = null,
    string? SupplyStatus = null,
    string? SupplyMessageFa = null,
    bool CanConfirmDeposit = false,
    bool CanRecoverInventory = false,
    IReadOnlyList<AdminSupplyLineShortage>? SupplyLines = null,
    /// <summary>
    /// TB-270-R2: each linked resend/replacement parcel's OWN confirmed plan, so the detail surface renders the stored
    /// promise that the owning read model holds instead of re-deriving a local date.
    /// </summary>
    IReadOnlyList<AdminShipmentDispatchPlanStored>? ShipmentDispatchPlans = null);

/// <summary>
/// TB-270-R2: one stored own plan as the operator view reads it. It is immutable history: an elapsed promise is shown
/// as elapsed, never silently re-planned.
/// </summary>
public sealed record AdminShipmentDispatchPlanStored(
    Guid ShipmentId,
    Guid PlanId,
    string AttemptKind,
    string OccurrenceKey,
    DateOnly ReceivingDate,
    string ReceivingOpensAt,
    string ReceivingClosesAt,
    string ReceivingTimeZone,
    DateOnly HandoffDate,
    string HandoffOpensAt,
    string HandoffClosesAt,
    string HandoffTimeZone,
    DateTimeOffset PreparationStartUtc,
    DateTimeOffset TransferDepartureUtc,
    DateTimeOffset HandoffDeadlineUtc,
    int PreparationMinutes,
    int HandoffMinutes,
    int TransportWorkingDays,
    int PolicyVersion,
    DateTimeOffset ConfirmedAtUtc);

/// <summary>کمبود خط تأمین برای Admin — بدون GUID رزرو.</summary>
public sealed record AdminSupplyLineShortage(
    string? ItemTitle,
    string? UnitCode,
    decimal Required,
    decimal Available,
    decimal Shortage);

/// <summary>انتخاب خط/تعداد برای عملیات seller-scoped.</summary>
public sealed record AdminOrderLineSelection(Guid OrderLineId, decimal Quantity);

/// <summary>بدنهٔ اجرای یک عملیات سفارش.</summary>
public sealed record AdminOrderOperationRequest(
    string Code,
    Guid? SellerOrderId,
    Guid? FulfillmentId,
    Guid? ShipmentId,
    Guid? ReturnRequestId,
    string? CarrierDisplayName,
    string? TrackingReference,
    string? Reason,
    string? IdempotencyKey,
    IReadOnlyList<ReturnLineCommand>? ReturnItems,
    IReadOnlyList<AdminOrderLineSelection>? Selections = null,
    string? ShippingMethodCode = null,
    string? ProviderMetadataJson = null,
    Guid? ConsolidatedPackageId = null,
    IReadOnlyList<Guid>? ShipmentIds = null,
    Guid? ShippingGroupId = null,
    /// <summary>TB-253-R2: constituent accepted groups of one operational group workflow.</summary>
    IReadOnlyList<Guid>? OperationalGroupShippingGroupIds = null,
    /// <summary>TB-253-R2: existing unattached parcels explicitly reviewed for reuse.</summary>
    IReadOnlyList<Guid>? ReuseShipmentIds = null,
    /// <summary>
    /// TB-253-R3: the workflow occurrence this request belongs to. Prebound by the client before the
    /// first mutation and retained on retry/remount; same occurrence replays, a new occurrence is an
    /// explicit new operation.
    /// </summary>
    Guid? OperationalGroupOccurrenceId = null,
    /// <summary>TB-258: optional operator note recorded with a non-delivery attempt or origin receipt.</summary>
    string? Note = null,
    /// <summary>
    /// TB-258: the physically received lines of one origin receipt, each with its own condition. Only usable
    /// quantity funds a later resend; damaged/missing quantity never does.
    /// </summary>
    IReadOnlyList<AdminReturnReceiptLineSelection>? ReceiptItems = null,
    /// <summary>
    /// TB-259: the operator's reviewed physical packing choice inside one compatible group:
    /// «common» (one common package) or «separate» (independent member parcels). A changed choice under
    /// the same occurrence is a typed intent conflict; an absent value means the legacy common package.
    /// </summary>
    string? OperationalGroupPackingMode = null,
    /// <summary>TB-262: the returned parcel line whose damaged quantity is being resolved.</summary>
    Guid? OrderLineId = null,
    /// <summary>TB-262: the damaged quantity this decision covers (bounded by the source parcel's own remainder).</summary>
    decimal? Quantity = null,
    /// <summary>
    /// TB-262: the reviewed damaged-goods outcome — <c>replacement</c> (new healthy-stock reservation and a linked
    /// replacement parcel) or <c>referral</c> (left for customer service / existing financial handling).
    /// </summary>
    string? DamagedOutcome = null,
    /// <summary>
    /// TB-265: the exact received quantity of a returned parcel that a courier-origin Returns case claims. Each line
    /// carries its own physical condition; usable and damaged quantity are separate pools and the server refuses a
    /// quantity that is already funding a resend, a replacement or another case.
    /// </summary>
    IReadOnlyList<AdminCourierReturnLineSelection>? CourierReturnItems = null,
    /// <summary>
    /// TB-270-R2: the operator's reviewed customer date for THIS parcel's own plan (ISO <c>yyyy-MM-dd</c>). It is
    /// re-validated against a freshly computed feasible calendar; an expired/changed selection is refused and never
    /// silently clamped to another date.
    /// </summary>
    string? DispatchPlanReceivingDate = null,
    /// <summary>TB-270-R2: the reviewed slot start in the captured receiving zone (<c>HH:mm</c>).</summary>
    string? DispatchPlanReceivingOpensAt = null,
    /// <summary>TB-270-R2: the reviewed slot end in the captured receiving zone (<c>HH:mm</c>, exclusive).</summary>
    string? DispatchPlanReceivingClosesAt = null,
    /// <summary>
    /// TB-270-R3: the exact review fingerprint the server issued with the preview the operator reviewed. The server
    /// recomputes it from the same owning facts and refuses (<c>order.shipment_plan.review_context_changed</c>) when
    /// the source parcel/decision/hold/capture/readiness changed between review and confirmation. It is never used as
    /// an idempotency key, and it is not sent on an exact replay of an already-confirmed key.
    /// </summary>
    string? DispatchPlanReviewFingerprint = null,
    /// <summary>
    /// TB-279-R2 (D): the EFFECTIVE Returns decision revision the operator reviewed when approving/rejecting. A newer
    /// revision refuses the stale review instead of deciding over a correction the operator never saw.
    /// </summary>
    int? ExpectedDecisionVersion = null,
    /// <summary>
    /// TB-279-R2 (D): the reviewed Returns PLAN revision the decision was taken against. Required when the case owns a
    /// reviewed plan, so an approval can never bless a plan version the operator did not read.
    /// </summary>
    int? ExpectedResolutionVersion = null);

/// <summary>TB-265: one claimed line of a courier-origin Returns case.</summary>
public sealed record AdminCourierReturnLineSelection(
    Guid OrderLineId,
    decimal Quantity,
    string Condition = "usable");

/// <summary>TB-258: one physically received line submitted with an origin receipt.</summary>
public sealed record AdminReturnReceiptLineSelection(
    Guid OrderLineId,
    decimal Quantity,
    int Condition = 0);

/// <summary>
/// TB-270-R2: one feasible customer receiving slot for a specific parcel's OWN plan, with the three operational
/// deadlines that the existing scheduling authority derived for exactly that slot.
/// </summary>
/// <param name="ReceivingDate">Customer date (local to the captured receiving zone).</param>
/// <param name="ReceivingOpensAt">Slot start <c>HH:mm</c> in the receiving zone.</param>
/// <param name="ReceivingClosesAt">Slot end <c>HH:mm</c> (exclusive) in the receiving zone.</param>
/// <param name="ReceivingTimeZone">The captured receiving zone id.</param>
/// <param name="HandoffDate">Carrier acceptance date in the carrier zone.</param>
/// <param name="HandoffOpensAt">Carrier window opens <c>HH:mm</c>.</param>
/// <param name="HandoffClosesAt">Carrier window closes <c>HH:mm</c> (exclusive).</param>
/// <param name="HandoffTimeZone">The captured carrier zone id.</param>
/// <param name="PreparationStartUtc">Last instant to START physical preparation (not "packing finished").</param>
/// <param name="TransferDepartureUtc">Last instant to leave toward the carrier.</param>
/// <param name="HandoffDeadlineUtc">Carrier acceptance close instant (exclusive).</param>
public sealed record AdminShipmentDispatchPlanOption(
    DateOnly ReceivingDate,
    string ReceivingOpensAt,
    string ReceivingClosesAt,
    string ReceivingTimeZone,
    DateOnly HandoffDate,
    string HandoffOpensAt,
    string HandoffClosesAt,
    string HandoffTimeZone,
    DateTimeOffset PreparationStartUtc,
    DateTimeOffset TransferDepartureUtc,
    DateTimeOffset HandoffDeadlineUtc);

/// <summary>TB-270-R2: the confirmed plan already stored for this parcel (immutable history).</summary>
public sealed record AdminShipmentDispatchPlanConfirmed(
    Guid PlanId,
    string OccurrenceKey,
    DateOnly ReceivingDate,
    string ReceivingOpensAt,
    string ReceivingClosesAt,
    string ReceivingTimeZone,
    DateOnly HandoffDate,
    string HandoffOpensAt,
    string HandoffClosesAt,
    string HandoffTimeZone,
    DateTimeOffset PreparationStartUtc,
    DateTimeOffset TransferDepartureUtc,
    DateTimeOffset HandoffDeadlineUtc,
    int PreparationMinutes,
    int HandoffMinutes,
    int TransportWorkingDays,
    int PolicyVersion,
    Guid ConfirmedByUserId,
    DateTimeOffset ConfirmedAtUtc);

/// <summary>
/// TB-270-R2: read-only preview of one parcel's OWN dispatch plan — the exact source/binding/readiness context plus
/// the feasible customer options the existing scheduling authority yields from the captured inputs.
/// </summary>
/// <remarks>
/// A returned row is a read: it never writes, never reserves stock, never creates a parcel and never commits a plan.
/// <see cref="BlockerCode"/> non-empty means "no actionable promise" and <see cref="Options"/> is empty.
/// </remarks>
public sealed record AdminShipmentDispatchPlanPreview(
    Guid CheckoutId,
    Guid SellerOrderId,
    Guid FulfillmentId,
    Guid ShipmentId,
    Guid? ShippingGroupId,
    string AttemptKind,
    Guid? SourceShipmentId,
    Guid? DamagedResolutionId,
    Guid? ReplacementReservationId,
    string SourceFingerprint,
    string? ReadinessSource,
    DateTimeOffset? ReadinessAtUtc,
    string? MethodCode,
    string? MethodLabel,
    Guid? SnapshotId,
    Guid? DraftId,
    long? SourceRevision,
    int? FormatVersion,
    Guid? AreaGroupId,
    Guid? RateRowId,
    DateTimeOffset ServedAtUtc,
    IReadOnlyList<AdminShipmentDispatchPlanOption> Options,
    AdminShipmentDispatchPlanConfirmed? ConfirmedPlan = null,
    string? BlockerCode = null,
    string? BlockerFa = null,
    IReadOnlyList<string>? MissingCapturedInputs = null,
    string? SchedulingReasonCode = null,
    /// <summary>
    /// TB-270-R3: opaque server-issued fingerprint of the reviewed context (owning parcel identity, source
    /// decision/hold identity, accepted capture identity, readiness anchor and line fingerprint). The confirmation
    /// request must echo it unchanged; a changed context is refused instead of silently replanned.
    /// </summary>
    string? ReviewFingerprint = null,
    /// <summary>
    /// TB-270-R3: true when the response describes the ALREADY confirmed plan (history) and therefore offers no new
    /// calendar and no action. A read of a dispatched/delivered parcel still reports its stored promise.
    /// </summary>
    bool HistoryOnly = false);
