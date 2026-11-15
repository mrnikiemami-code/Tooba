/**
 * کلاینت عملیات lifecycle سفارش Admin — فقط API-driven actions.
 */
import { adminHeaders, type AdminResult } from "./admin-api.ts";
import {
  extractAdminErrorCode,
  mapAdminErrorMessage,
  type AdminErrorLocale,
} from "./admin-error-map.ts";
import {
  filterOperationsForScope,
  GRID_EXCLUDED_OPERATION_CODES,
  orderOperationBodyFromAction,
  sellerQuickActionLabels,
  type AdminOrderOperationsScope,
} from "./admin-order-operations-scope.ts";
import { isDisplayableServerDetail } from "../../lib/i18n/displayable-server-detail.ts";

export type { AdminOrderOperationsScope };
export {
  filterOperationsForScope,
  GRID_EXCLUDED_OPERATION_CODES,
  orderOperationBodyFromAction,
  sellerQuickActionLabels,
};

export type AdminOrderOperationAction = {
  code: string;
  labelFa: string;
  labelEn: string;
  sellerOrderId: string | null;
  fulfillmentId: string | null;
  shipmentId: string | null;
  returnRequestId: string | null;
  orderLineId?: string | null;
  consolidatedPackageId?: string | null;
  /** TB-259: packing choices the server permits for this action (common/separate). */
  packingModes?: string[] | null;
  requiredPermission: string;
  requiresConfirm: boolean;
  confirmMessageFa: string | null;
};

export type AdminOrderLineCapability = {
  orderLineId: string;
  sellerOrderId: string;
  selectable: boolean;
  selectableQuantityMax: number;
  rowActionCodes: string[];
  bulkActionCodes: string[];
  shipmentEligibleQuantity: number;
  lockedReasonCode: string | null;
  lockedReasonFa: string | null;
};

export type AdminSellerCapability = {
  sellerOrderId: string;
  selectionAllowed: boolean;
  paymentLocked: boolean;
  infoMessageFa: string | null;
  wholeGroupActionCodes: string[];
  shipmentCreationPossible: boolean;
};

export type AdminOrderOperationsPage = {
  checkoutId: string;
  actions: AdminOrderOperationAction[];
  returnEligibility: unknown[];
  lineCapabilities: AdminOrderLineCapability[];
  sellerCapabilities: AdminSellerCapability[];
  inventoryRecoveryWarningFa?: string | null;
  inventoryRecoveryClass?: string | null;
  supplyStatus?: string | null;
  supplyMessageFa?: string | null;
  canConfirmDeposit?: boolean;
  canRecoverInventory?: boolean;
  canRetryReservation?: boolean;
  canExtendTimer?: boolean;
  supplyLines?: Array<{
    itemTitle: string | null;
    unitCode: string | null;
    required: number;
    available: number;
    shortage: number;
  }>;
  /**
   * TB-270-R2: stored OWN plans of linked resend/replacement parcels, read from Order rows. A stored plan is
   * immutable history: an elapsed promise is displayed as elapsed, never re-planned locally.
   */
  shipmentDispatchPlans?: AdminShipmentDispatchPlanStored[];
};

/** TB-270-R2: one stored own plan exactly as the owning Order read model reports it. */
export type AdminShipmentDispatchPlanStored = {
  shipmentId: string;
  planId: string;
  attemptKind: string;
  occurrenceKey: string;
  receivingDate: string;
  receivingOpensAt: string;
  receivingClosesAt: string;
  receivingTimeZone: string;
  handoffDate: string;
  handoffOpensAt: string;
  handoffClosesAt: string;
  handoffTimeZone: string;
  preparationStartUtc: string;
  transferDepartureUtc: string;
  handoffDeadlineUtc: string;
  preparationMinutes: number;
  handoffMinutes: number;
  transportWorkingDays: number;
  policyVersion: number;
  confirmedAtUtc: string;
};

/**
 * TB-270-R2: ONE feasible customer window the server computed from the seller's own captured inputs.
 * `receivingDate` + the slot strings are exactly what `plan_shipment_dispatch` must echo back.
 */
export type AdminShipmentDispatchPlanOption = {
  receivingDate: string;
  receivingOpensAt: string;
  receivingClosesAt: string;
  receivingTimeZone: string;
  handoffDate: string;
  handoffOpensAt: string;
  handoffClosesAt: string;
  handoffTimeZone: string;
  preparationStartUtc: string;
  transferDepartureUtc: string;
  handoffDeadlineUtc: string;
};

/** TB-270-R2: the plan already stored for this parcel — immutable history, never re-planned. */
export type AdminShipmentDispatchPlanConfirmed = {
  planId: string;
  occurrenceKey: string;
  receivingDate: string;
  receivingOpensAt: string;
  receivingClosesAt: string;
  receivingTimeZone: string;
  handoffDate: string;
  handoffOpensAt: string;
  handoffClosesAt: string;
  handoffTimeZone: string;
  preparationStartUtc: string;
  transferDepartureUtc: string;
  handoffDeadlineUtc: string;
  preparationMinutes: number;
  handoffMinutes: number;
  transportWorkingDays: number;
  policyVersion: number;
  confirmedByUserId: string;
  confirmedAtUtc: string;
};

/**
 * TB-270-R2: read-only preview of one existing resend/replacement parcel's OWN dispatch plan.
 * A non-empty `blockerCode` means "no actionable promise"; `options` is then empty.
 */
export type AdminShipmentDispatchPlanPreview = {
  checkoutId: string;
  sellerOrderId: string;
  fulfillmentId: string;
  shipmentId: string;
  shippingGroupId: string | null;
  attemptKind: string;
  sourceShipmentId: string | null;
  damagedResolutionId: string | null;
  replacementReservationId: string | null;
  sourceFingerprint: string;
  readinessSource: string | null;
  readinessAtUtc: string | null;
  methodCode: string | null;
  methodLabel: string | null;
  snapshotId: string | null;
  draftId: string | null;
  sourceRevision: number | null;
  formatVersion: number | null;
  areaGroupId: string | null;
  rateRowId: string | null;
  servedAtUtc: string;
  options: AdminShipmentDispatchPlanOption[];
  confirmedPlan: AdminShipmentDispatchPlanConfirmed | null;
  blockerCode: string | null;
  blockerFa: string | null;
  missingCapturedInputs: string[] | null;
  schedulingReasonCode: string | null;
  /**
   * TB-270-R3: the server-issued fingerprint of the reviewed context. It must be echoed on confirmation
   * so a changed source/decision/hold/capture is refused instead of silently replanned.
   */
  reviewFingerprint: string | null;
  /** TB-270-R3: the response describes the ALREADY confirmed plan (history); no new calendar, no action. */
  historyOnly: boolean;
};

/**
 * کدهایی که بدنهٔ درخواستشان «کد رهگیری» می‌خواهد.
 *
 * این‌ها تنها عملیاتی هستند که اجرایشان بدون ورودی کاربر ناقص است؛ بقیهٔ اقدام‌های
 * پراجکت‌شده فقط با شناسه‌های هویتی همان `action` اجرا می‌شوند. منبع قواعد:
 * `AdminOrderOperationsOrchestrator.Fulfillment.cs` (assign/correct tracking و
 * ثبت کد رهگیری بستهٔ تجمیعی). افزودن کد جدید سرور = یک سطر اینجا.
 */
export const TRACKING_INPUT_OPERATION_CODES = new Set([
  "assign_tracking",
  "correct_tracking",
  "assign_consolidated_package_tracking",
]);

/** آیا این عملیات پیش از اجرا کد رهگیری از کاربر می‌خواهد؟ */
export function requiresTrackingInput(code: string): boolean {
  return TRACKING_INPUT_OPERATION_CODES.has(code);
}

export type AdminOrderOperationRequest = {
  code: string;
  sellerOrderId?: string | null;
  fulfillmentId?: string | null;
  shipmentId?: string | null;
  returnRequestId?: string | null;
  carrierDisplayName?: string | null;
  trackingReference?: string | null;
  reason?: string | null;
  idempotencyKey?: string | null;
  selections?: Array<{ orderLineId: string; quantity: number }> | null;
  shippingMethodCode?: string | null;
  providerMetadataJson?: string | null;
  consolidatedPackageId?: string | null;
  shipmentIds?: string[] | null;
  note?: string | null;
  shippingGroupId?: string | null;
  operationalGroupShippingGroupIds?: string[] | null;
  reuseShipmentIds?: string[] | null;
  /** TB-253-R3: prebound workflow occurrence; retry replays, a new id is a new operation. */
  operationalGroupOccurrenceId?: string | null;
  /** TB-259: the reviewed physical packing choice of one operational group operation. */
  operationalGroupPackingMode?: string | null;
  /** TB-258: per-line physical receipt facts of one origin receipt (only usable quantity is resendable). */
  receiptItems?: Array<{ orderLineId: string; quantity: number; condition: number }> | null;
  /** TB-262: the returned parcel line whose damaged quantity is being resolved. */
  orderLineId?: string | null;
  /** TB-262: the damaged quantity this decision covers. */
  quantity?: number | null;
  /** TB-262: reviewed damaged-goods outcome — `replacement` or `referral`. */
  damagedOutcome?: string | null;
  /**
   * TB-265: the exact received quantity a courier-origin returns case claims, each line with its own physical
   * condition. Distinct from `returnItems` (the customer RMA lines of a different workflow).
   */
  courierReturnItems?: Array<{ orderLineId: string; quantity: number; condition: string }> | null;
  /**
   * TB-270-R2: the reviewed customer date/slot of ONE existing resend/replacement parcel's own dispatch plan.
   * The server re-validates them against a freshly computed feasible calendar; an expired selection is refused.
   */
  dispatchPlanReceivingDate?: string | null;
  dispatchPlanReceivingOpensAt?: string | null;
  dispatchPlanReceivingClosesAt?: string | null;
  /** TB-270-R3: the exact fingerprint of the preview the operator reviewed. */
  dispatchPlanReviewFingerprint?: string | null;
};

/**
 * نگاشت خطای عملیات سفارش به متن قابل نمایش (چندزبانه).
 *
 * ۱) کد پایدارِ شناخته‌شده در دیکشنری Admin → متن همان locale (قرارداد چندزبانه UI؛
 *    متن سرور نمی‌تواند زبان فالبک دیگری را روی UI فارسی تحمیل کند).
 * ۲) کد ناشناخته → عنوان/جزئیات سرور، فقط اگر به زبان locale فعال نوشته شده و واژهٔ
 *    فنی داخلی نداشته باشد.
 * ۳) وگرنه پیام عمومی همان locale — هیچ‌گاه کد خام، انگلیسی، یا متن فنی نمایش داده نمی‌شود.
 */
export function mapOrderOperationError(
  codeOrMessage: string | null | undefined,
  detail?: string | null,
  locale: AdminErrorLocale = "fa",
): string {
  const code = codeOrMessage ?? "";
  const serverDetail = detail ? detail.trim() : "";
  const knownCode = extractAdminErrorCode(code);
  if (!knownCode && serverDetail && isDisplayableServerDetail(serverDetail, locale)) {
    return serverDetail;
  }
  return mapAdminErrorMessage(code || null, locale);
}

function asString(value: unknown): string | null {
  return typeof value === "string" && value.length > 0 ? value : null;
}

function mapAction(raw: unknown): AdminOrderOperationAction | null {
  if (!raw || typeof raw !== "object") return null;
  const row = raw as Record<string, unknown>;
  const code = asString(row.code) ?? asString(row.Code);
  const labelFa = asString(row.labelFa) ?? asString(row.LabelFa);
  if (!code || !labelFa) return null;
  return {
    code,
    labelFa,
    labelEn: asString(row.labelEn) ?? asString(row.LabelEn) ?? code,
    sellerOrderId: asString(row.sellerOrderId) ?? asString(row.SellerOrderId),
    fulfillmentId: asString(row.fulfillmentId) ?? asString(row.FulfillmentId),
    shipmentId: asString(row.shipmentId) ?? asString(row.ShipmentId),
    returnRequestId: asString(row.returnRequestId) ?? asString(row.ReturnRequestId),
    orderLineId: asString(row.orderLineId) ?? asString(row.OrderLineId),
    consolidatedPackageId: asString(row.consolidatedPackageId) ?? asString(row.ConsolidatedPackageId),
    packingModes: Array.isArray(row.packingModes ?? row.PackingModes)
      ? ((row.packingModes ?? row.PackingModes) as unknown[])
        .map((mode) => asString(mode))
        .filter((mode): mode is string => Boolean(mode))
      : null,
    requiredPermission: asString(row.requiredPermission) ?? asString(row.RequiredPermission) ?? "",
    requiresConfirm: Boolean(row.requiresConfirm ?? row.RequiresConfirm),
    confirmMessageFa: asString(row.confirmMessageFa) ?? asString(row.ConfirmMessageFa),
  };
}

function asNumber(value: unknown): number | null {
  if (typeof value === "number" && Number.isFinite(value)) return value;
  if (typeof value === "string" && value.trim().length > 0) {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : null;
  }
  return null;
}

function mapDispatchPlanOption(raw: unknown): AdminShipmentDispatchPlanOption | null {
  if (!raw || typeof raw !== "object") return null;
  const row = raw as Record<string, unknown>;
  const receivingDate = asString(row.receivingDate) ?? asString(row.ReceivingDate);
  const receivingOpensAt = asString(row.receivingOpensAt) ?? asString(row.ReceivingOpensAt);
  const receivingClosesAt = asString(row.receivingClosesAt) ?? asString(row.ReceivingClosesAt);
  const receivingTimeZone = asString(row.receivingTimeZone) ?? asString(row.ReceivingTimeZone);
  const preparationStartUtc = asString(row.preparationStartUtc) ?? asString(row.PreparationStartUtc);
  const transferDepartureUtc = asString(row.transferDepartureUtc) ?? asString(row.TransferDepartureUtc);
  const handoffDeadlineUtc = asString(row.handoffDeadlineUtc) ?? asString(row.HandoffDeadlineUtc);
  if (
    !receivingDate || !receivingOpensAt || !receivingClosesAt || !receivingTimeZone
    || !preparationStartUtc || !transferDepartureUtc || !handoffDeadlineUtc
  ) {
    return null;
  }
  return {
    receivingDate,
    receivingOpensAt,
    receivingClosesAt,
    receivingTimeZone,
    handoffDate: asString(row.handoffDate) ?? asString(row.HandoffDate) ?? "",
    handoffOpensAt: asString(row.handoffOpensAt) ?? asString(row.HandoffOpensAt) ?? "",
    handoffClosesAt: asString(row.handoffClosesAt) ?? asString(row.HandoffClosesAt) ?? "",
    handoffTimeZone: asString(row.handoffTimeZone) ?? asString(row.HandoffTimeZone) ?? "",
    preparationStartUtc,
    transferDepartureUtc,
    handoffDeadlineUtc,
  };
}

function mapConfirmedPlan(raw: unknown): AdminShipmentDispatchPlanConfirmed | null {
  if (!raw || typeof raw !== "object") return null;
  const row = raw as Record<string, unknown>;
  const planId = asString(row.planId) ?? asString(row.PlanId);
  const receivingDate = asString(row.receivingDate) ?? asString(row.ReceivingDate);
  const confirmedAtUtc = asString(row.confirmedAtUtc) ?? asString(row.ConfirmedAtUtc);
  if (!planId || !receivingDate || !confirmedAtUtc) return null;
  return {
    planId,
    occurrenceKey: asString(row.occurrenceKey) ?? asString(row.OccurrenceKey) ?? "",
    receivingDate,
    receivingOpensAt: asString(row.receivingOpensAt) ?? asString(row.ReceivingOpensAt) ?? "",
    receivingClosesAt: asString(row.receivingClosesAt) ?? asString(row.ReceivingClosesAt) ?? "",
    receivingTimeZone: asString(row.receivingTimeZone) ?? asString(row.ReceivingTimeZone) ?? "",
    handoffDate: asString(row.handoffDate) ?? asString(row.HandoffDate) ?? "",
    handoffOpensAt: asString(row.handoffOpensAt) ?? asString(row.HandoffOpensAt) ?? "",
    handoffClosesAt: asString(row.handoffClosesAt) ?? asString(row.HandoffClosesAt) ?? "",
    handoffTimeZone: asString(row.handoffTimeZone) ?? asString(row.HandoffTimeZone) ?? "",
    preparationStartUtc: asString(row.preparationStartUtc) ?? asString(row.PreparationStartUtc) ?? "",
    transferDepartureUtc: asString(row.transferDepartureUtc) ?? asString(row.TransferDepartureUtc) ?? "",
    handoffDeadlineUtc: asString(row.handoffDeadlineUtc) ?? asString(row.HandoffDeadlineUtc) ?? "",
    preparationMinutes: asNumber(row.preparationMinutes ?? row.PreparationMinutes) ?? 0,
    handoffMinutes: asNumber(row.handoffMinutes ?? row.HandoffMinutes) ?? 0,
    transportWorkingDays: asNumber(row.transportWorkingDays ?? row.TransportWorkingDays) ?? 0,
    policyVersion: asNumber(row.policyVersion ?? row.PolicyVersion) ?? 0,
    confirmedByUserId: asString(row.confirmedByUserId) ?? asString(row.ConfirmedByUserId) ?? "",
    confirmedAtUtc,
  };
}

function mapShipmentDispatchPlanPreview(raw: unknown): AdminShipmentDispatchPlanPreview | null {
  if (!raw || typeof raw !== "object") return null;
  const row = raw as Record<string, unknown>;
  const checkoutId = asString(row.checkoutId) ?? asString(row.CheckoutId);
  const shipmentId = asString(row.shipmentId) ?? asString(row.ShipmentId);
  const attemptKind = asString(row.attemptKind) ?? asString(row.AttemptKind);
  if (!checkoutId || !shipmentId || !attemptKind) return null;
  const optionsRaw = row.options ?? row.Options;
  const missingRaw = row.missingCapturedInputs ?? row.MissingCapturedInputs;
  return {
    checkoutId,
    sellerOrderId: asString(row.sellerOrderId) ?? asString(row.SellerOrderId) ?? "",
    fulfillmentId: asString(row.fulfillmentId) ?? asString(row.FulfillmentId) ?? "",
    shipmentId,
    shippingGroupId: asString(row.shippingGroupId) ?? asString(row.ShippingGroupId),
    attemptKind,
    sourceShipmentId: asString(row.sourceShipmentId) ?? asString(row.SourceShipmentId),
    damagedResolutionId: asString(row.damagedResolutionId) ?? asString(row.DamagedResolutionId),
    replacementReservationId:
      asString(row.replacementReservationId) ?? asString(row.ReplacementReservationId),
    sourceFingerprint: asString(row.sourceFingerprint) ?? asString(row.SourceFingerprint) ?? "",
    readinessSource: asString(row.readinessSource) ?? asString(row.ReadinessSource),
    readinessAtUtc: asString(row.readinessAtUtc) ?? asString(row.ReadinessAtUtc),
    methodCode: asString(row.methodCode) ?? asString(row.MethodCode),
    methodLabel: asString(row.methodLabel) ?? asString(row.MethodLabel),
    snapshotId: asString(row.snapshotId) ?? asString(row.SnapshotId),
    draftId: asString(row.draftId) ?? asString(row.DraftId),
    sourceRevision: asNumber(row.sourceRevision ?? row.SourceRevision),
    formatVersion: asNumber(row.formatVersion ?? row.FormatVersion),
    areaGroupId: asString(row.areaGroupId) ?? asString(row.AreaGroupId),
    rateRowId: asString(row.rateRowId) ?? asString(row.RateRowId),
    servedAtUtc: asString(row.servedAtUtc) ?? asString(row.ServedAtUtc) ?? "",
    options: Array.isArray(optionsRaw)
      ? (optionsRaw as unknown[])
        .map(mapDispatchPlanOption)
        .filter((option): option is AdminShipmentDispatchPlanOption => option !== null)
      : [],
    confirmedPlan: mapConfirmedPlan(row.confirmedPlan ?? row.ConfirmedPlan),
    blockerCode: asString(row.blockerCode) ?? asString(row.BlockerCode),
    blockerFa: asString(row.blockerFa) ?? asString(row.BlockerFa),
    missingCapturedInputs: Array.isArray(missingRaw)
      ? (missingRaw as unknown[])
        .map((value) => asString(value))
        .filter((value): value is string => Boolean(value))
      : null,
    schedulingReasonCode: asString(row.schedulingReasonCode) ?? asString(row.SchedulingReasonCode),
    reviewFingerprint: asString(row.reviewFingerprint) ?? asString(row.ReviewFingerprint),
    historyOnly: (row.historyOnly ?? row.HistoryOnly) === true,
  };
}

function mapPage(raw: unknown): AdminOrderOperationsPage | null {
  if (!raw || typeof raw !== "object") return null;
  const row = raw as Record<string, unknown>;
  const checkoutId = asString(row.checkoutId) ?? asString(row.CheckoutId);
  const actionsRaw = row.actions ?? row.Actions;
  if (!checkoutId || !Array.isArray(actionsRaw)) return null;
  return {
    checkoutId,
    actions: actionsRaw.map(mapAction).filter((x): x is AdminOrderOperationAction => x !== null),
    returnEligibility: Array.isArray(row.returnEligibility ?? row.ReturnEligibility)
      ? ((row.returnEligibility ?? row.ReturnEligibility) as unknown[])
      : [],
    lineCapabilities: mapLineCaps(row.lineCapabilities ?? row.LineCapabilities),
    sellerCapabilities: mapSellerCaps(row.sellerCapabilities ?? row.SellerCapabilities),
    inventoryRecoveryWarningFa:
      asString(row.inventoryRecoveryWarningFa) ?? asString(row.InventoryRecoveryWarningFa),
    inventoryRecoveryClass:
      asString(row.inventoryRecoveryClass) ?? asString(row.InventoryRecoveryClass),
    supplyStatus: asString(row.supplyStatus) ?? asString(row.SupplyStatus),
    supplyMessageFa: asString(row.supplyMessageFa) ?? asString(row.SupplyMessageFa),
    canConfirmDeposit: Boolean(row.canConfirmDeposit ?? row.CanConfirmDeposit),
    canRecoverInventory: Boolean(row.canRecoverInventory ?? row.CanRecoverInventory),
    canRetryReservation: Boolean(row.canRetryReservation ?? row.CanRetryReservation),
    canExtendTimer: Boolean(row.canExtendTimer ?? row.CanExtendTimer),
    supplyLines: Array.isArray(row.supplyLines ?? row.SupplyLines)
      ? ((row.supplyLines ?? row.SupplyLines) as unknown[]).flatMap((item) => {
          if (!item || typeof item !== "object") return [];
          const line = item as Record<string, unknown>;
          return [{
            itemTitle: asString(line.itemTitle) ?? asString(line.ItemTitle),
            unitCode: asString(line.unitCode) ?? asString(line.UnitCode),
            required: Number(line.required ?? line.Required ?? 0),
            available: Number(line.available ?? line.Available ?? 0),
            shortage: Number(line.shortage ?? line.Shortage ?? 0),
          }];
        })
      : [],
    shipmentDispatchPlans: mapStoredPlans(row.shipmentDispatchPlans ?? row.ShipmentDispatchPlans),
  };
}

function mapStoredPlans(raw: unknown): AdminShipmentDispatchPlanStored[] {
  if (!Array.isArray(raw)) return [];
  return raw.flatMap((item) => {
    if (!item || typeof item !== "object") return [];
    const row = item as Record<string, unknown>;
    const shipmentId = asString(row.shipmentId) ?? asString(row.ShipmentId);
    const planId = asString(row.planId) ?? asString(row.PlanId);
    const receivingDate = asString(row.receivingDate) ?? asString(row.ReceivingDate);
    if (!shipmentId || !planId || !receivingDate) return [];
    return [{
      shipmentId,
      planId,
      attemptKind: asString(row.attemptKind) ?? asString(row.AttemptKind) ?? "",
      occurrenceKey: asString(row.occurrenceKey) ?? asString(row.OccurrenceKey) ?? "",
      receivingDate,
      receivingOpensAt: asString(row.receivingOpensAt) ?? asString(row.ReceivingOpensAt) ?? "",
      receivingClosesAt: asString(row.receivingClosesAt) ?? asString(row.ReceivingClosesAt) ?? "",
      receivingTimeZone: asString(row.receivingTimeZone) ?? asString(row.ReceivingTimeZone) ?? "",
      handoffDate: asString(row.handoffDate) ?? asString(row.HandoffDate) ?? "",
      handoffOpensAt: asString(row.handoffOpensAt) ?? asString(row.HandoffOpensAt) ?? "",
      handoffClosesAt: asString(row.handoffClosesAt) ?? asString(row.HandoffClosesAt) ?? "",
      handoffTimeZone: asString(row.handoffTimeZone) ?? asString(row.HandoffTimeZone) ?? "",
      preparationStartUtc: asString(row.preparationStartUtc) ?? asString(row.PreparationStartUtc) ?? "",
      transferDepartureUtc: asString(row.transferDepartureUtc) ?? asString(row.TransferDepartureUtc) ?? "",
      handoffDeadlineUtc: asString(row.handoffDeadlineUtc) ?? asString(row.HandoffDeadlineUtc) ?? "",
      preparationMinutes: asNumber(row.preparationMinutes ?? row.PreparationMinutes) ?? 0,
      handoffMinutes: asNumber(row.handoffMinutes ?? row.HandoffMinutes) ?? 0,
      transportWorkingDays: asNumber(row.transportWorkingDays ?? row.TransportWorkingDays) ?? 0,
      policyVersion: asNumber(row.policyVersion ?? row.PolicyVersion) ?? 0,
      confirmedAtUtc: asString(row.confirmedAtUtc) ?? asString(row.ConfirmedAtUtc) ?? "",
    }];
  });
}

function mapLineCaps(raw: unknown): AdminOrderLineCapability[] {
  if (!Array.isArray(raw)) return [];
  return raw.flatMap((item) => {
    if (!item || typeof item !== "object") return [];
    const row = item as Record<string, unknown>;
    const orderLineId = asString(row.orderLineId) ?? asString(row.OrderLineId);
    const sellerOrderId = asString(row.sellerOrderId) ?? asString(row.SellerOrderId);
    if (!orderLineId || !sellerOrderId) return [];
    return [{
      orderLineId,
      sellerOrderId,
      selectable: Boolean(row.selectable ?? row.Selectable),
      selectableQuantityMax: Number(row.selectableQuantityMax ?? row.SelectableQuantityMax ?? 0),
      rowActionCodes: stringList(row.rowActionCodes ?? row.RowActionCodes),
      bulkActionCodes: stringList(row.bulkActionCodes ?? row.BulkActionCodes),
      shipmentEligibleQuantity: Number(row.shipmentEligibleQuantity ?? row.ShipmentEligibleQuantity ?? 0),
      lockedReasonCode: asString(row.lockedReasonCode) ?? asString(row.LockedReasonCode),
      lockedReasonFa: asString(row.lockedReasonFa) ?? asString(row.LockedReasonFa),
    }];
  });
}

function mapSellerCaps(raw: unknown): AdminSellerCapability[] {
  if (!Array.isArray(raw)) return [];
  return raw.flatMap((item) => {
    if (!item || typeof item !== "object") return [];
    const row = item as Record<string, unknown>;
    const sellerOrderId = asString(row.sellerOrderId) ?? asString(row.SellerOrderId);
    if (!sellerOrderId) return [];
    return [{
      sellerOrderId,
      selectionAllowed: Boolean(row.selectionAllowed ?? row.SelectionAllowed),
      paymentLocked: Boolean(row.paymentLocked ?? row.PaymentLocked),
      infoMessageFa: asString(row.infoMessageFa) ?? asString(row.InfoMessageFa),
      wholeGroupActionCodes: stringList(row.wholeGroupActionCodes ?? row.WholeGroupActionCodes),
      shipmentCreationPossible: Boolean(row.shipmentCreationPossible ?? row.ShipmentCreationPossible),
    }];
  });
}

function stringList(raw: unknown): string[] {
  return Array.isArray(raw) ? raw.filter((x): x is string => typeof x === "string") : [];
}

async function parseError(response: Response): Promise<{ code: string; detail: string | null }> {
  const payload = await response.json().catch(() => null);
  if (!payload || typeof payload !== "object") {
    return { code: `admin.http.${response.status}`, detail: null };
  }
  const row = payload as Record<string, unknown>;
  const code =
    asString(row.errorCode) ??
    asString(row.ErrorCode) ??
    `admin.http.${response.status}`;
  const detail = asString(row.detail) ?? asString(row.Detail) ?? asString(row.title) ?? asString(row.Title);
  return { code, detail };
}

/** عملیات مجاز را از Host می‌خواند. */
export async function loadAdminOrderOperations(
  checkoutId: string,
): Promise<AdminResult<AdminOrderOperationsPage>> {
  try {
    const response = await fetch(`/v1/admin/orders/${encodeURIComponent(checkoutId)}/operations`, {
      headers: adminHeaders(),
    });
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      const err = await parseError(response);
      return {
        state: "error",
        data: null,
        status: response.status,
        message: mapOrderOperationError(err.code, err.detail),
      };
    }
    const payload = await response.json().catch(() => null);
    const data = mapPage(payload);
    return data
      ? { state: "ok", data, status: response.status }
      : { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
  } catch {
    return { state: "error", data: null, status: 0, message: mapOrderOperationError("host-unreachable") };
  }
}

/**
 * TB-270-R2: پیش‌نمایش فقط‌خواندنی «برنامهٔ ارسال» یک مرسولهٔ ارسال مجدد/جایگزین موجود.
 *
 * این خواندن هیچ مرسوله‌ای نمی‌سازد، موجودی رزرو نمی‌کند و برنامه‌ای ثبت نمی‌کند؛ فقط
 * گزینه‌های واقعیِ پنجرهٔ مشتری را از ورودی‌های ثبت‌شدهٔ همان فروشنده برمی‌گرداند.
 */
export async function loadShipmentDispatchPlanPreview(
  checkoutId: string,
  shipmentId: string,
): Promise<AdminResult<AdminShipmentDispatchPlanPreview>> {
  try {
    const response = await fetch(
      `/v1/admin/orders/${encodeURIComponent(checkoutId)}/shipment-plan/${encodeURIComponent(shipmentId)}`,
      { headers: adminHeaders() },
    );
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      const err = await parseError(response);
      return {
        state: "error",
        data: null,
        status: response.status,
        message: mapOrderOperationError(err.code, err.detail),
      };
    }
    const payload = await response.json().catch(() => null);
    const data = mapShipmentDispatchPlanPreview(payload);
    return data
      ? { state: "ok", data, status: response.status }
      : { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
  } catch {
    return { state: "error", data: null, status: 0, message: mapOrderOperationError("host-unreachable") };
  }
}

/** یک عملیات را اجرا می‌کند. */
export async function executeAdminOrderOperation(
  checkoutId: string,
  body: AdminOrderOperationRequest,
): Promise<AdminResult<unknown>> {
  try {
    const response = await fetch(`/v1/admin/orders/${encodeURIComponent(checkoutId)}/operations`, {
      method: "POST",
      headers: adminHeaders({ "content-type": "application/json" }),
      body: JSON.stringify({
        code: body.code,
        sellerOrderId: body.sellerOrderId ?? null,
        fulfillmentId: body.fulfillmentId ?? null,
        shipmentId: body.shipmentId ?? null,
        returnRequestId: body.returnRequestId ?? null,
        carrierDisplayName: body.carrierDisplayName ?? null,
        trackingReference: body.trackingReference ?? null,
        reason: body.reason ?? null,
        idempotencyKey: body.idempotencyKey ?? null,
        shippingMethodCode: body.shippingMethodCode ?? null,
        providerMetadataJson: body.providerMetadataJson ?? null,
        consolidatedPackageId: body.consolidatedPackageId ?? null,
        shipmentIds: body.shipmentIds ?? null,
        note: body.note ?? null,
        shippingGroupId: body.shippingGroupId ?? null,
        selections: body.selections?.map((s) => ({
          orderLineId: s.orderLineId,
          quantity: s.quantity,
        })) ?? null,
        operationalGroupShippingGroupIds: body.operationalGroupShippingGroupIds ?? null,
        reuseShipmentIds: body.reuseShipmentIds ?? null,
        operationalGroupOccurrenceId: body.operationalGroupOccurrenceId ?? null,
        operationalGroupPackingMode: body.operationalGroupPackingMode ?? null,
        receiptItems: body.receiptItems?.map((line) => ({
          orderLineId: line.orderLineId,
          quantity: line.quantity,
          condition: line.condition,
        })) ?? null,
        orderLineId: body.orderLineId ?? null,
        quantity: body.quantity ?? null,
        damagedOutcome: body.damagedOutcome ?? null,
        courierReturnItems: body.courierReturnItems?.map((line) => ({
          orderLineId: line.orderLineId,
          quantity: line.quantity,
          condition: line.condition,
        })) ?? null,
        dispatchPlanReceivingDate: body.dispatchPlanReceivingDate ?? null,
        dispatchPlanReceivingOpensAt: body.dispatchPlanReceivingOpensAt ?? null,
        dispatchPlanReceivingClosesAt: body.dispatchPlanReceivingClosesAt ?? null,
        dispatchPlanReviewFingerprint: body.dispatchPlanReviewFingerprint ?? null,
      }),
    });
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "order.operation.denied", code: "order.operation.denied" };
    }
    if (!response.ok) {
      const err = await parseError(response);
      return {
        state: "error",
        data: null,
        status: response.status,
        message: mapOrderOperationError(err.code, err.detail),
        code: err.code ?? null,
      };
    }
    const payload = await response.json().catch(() => null);
    return { state: "ok", data: payload, status: response.status };
  } catch {
    // A transport failure has no server code on purpose: it is uncertainty, never "no claim".
    return { state: "error", data: null, status: 0, message: mapOrderOperationError("host-unreachable") };
  }
}
