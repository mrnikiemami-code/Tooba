/**
 * کلاینت و نگاشت مرجوعی/بازگشت وجه مشترک بین Customer/Seller/Admin.
 * فقط دادهٔ واقعی Host؛ بدون وضعیت ساختگی.
 */

import { ADMIN_DEV_ACTOR_HEADER, type AdminResult } from "../admin/admin-api.ts";
import type { GridServerQuery } from "../../design-system/data-grid/types.ts";
import { postAdminGridQuery, type AdminGridQueryResult } from "../../design-system/app-data-grid/admin-grid-query-client.ts";
import { customerAuthHeaders } from "../customer-panel/customer-api.ts";
import {
  DEV_ACTOR_HEADER,
  SELLER_PARTY_HEADER,
  readActorUserId,
  type HostReadSource,
} from "../vendor-panel/seller-api.ts";

export interface ReturnItem {
  returnItemId: string;
  orderLineId: string;
  quantity: number;
  unitPriceSnapshot: number;
  currency: string;
  reservationId: string | null;
  /** TB-265: physical condition of the received unit (Usable/Damaged), server-owned. */
  condition?: string;
  /** TB-265: recorded physical disposition once the owning restock/non-saleable decision is durable. */
  dispositionCondition?: string | null;
  /**
   * TB-275: the EXACT inspected usable quantity this line actually restocked (null when no saleable restock was
   * recorded). A mixed line restocks only this portion, so the requested quantity never reads as wholly saleable.
   */
  saleableRestockedQuantity?: number | null;
  /** TB-265: the returned parcel this unit physically came back on. */
  sourceShipmentId?: string | null;
  /** TB-265: the single origin receipt that is reused, never re-received. */
  sourceReceiptId?: string | null;
}

export interface RefundAttempt {
  refundAttemptId: string;
  paymentId: string;
  amount: number;
  currency: string;
  status: string;
  idempotencyKey: string;
  providerReference: string | null;
  failureCode: string | null;
  createdAt: string;
  completedAt: string | null;
}

/** مقصد بازگشت وجه تایپ‌شده — بدون free-form. */
export type RefundDestination = "OriginalPayment" | "Wallet";

export const DEFAULT_REFUND_DESTINATION: RefundDestination = "OriginalPayment";

export interface ReturnSnapshot {
  returnRequestId: string;
  sellerOrderId: string;
  checkoutId: string;
  sellerPartyId: string;
  requestedByUserId: string;
  status: string;
  reason: string | null;
  currency: string;
  refundAmount: number;
  paymentId: string | null;
  /** مقصد بازگشت وجه؛ پیش‌فرض OriginalPayment اگر Host نفرستد. */
  destination: RefundDestination;
  createdAt: string;
  updatedAt: string;
  items: ReturnItem[];
  refundAttempts: RefundAttempt[];
  /** TB-265: courier-origin case source facts (CustomerDelivered for the existing customer RMA flow). */
  origin?: string;
  sourceFulfillmentId?: string | null;
  sourceShipmentId?: string | null;
  sourceReceiptId?: string | null;
  sourceClaimId?: string | null;
  /** TB-265: durable approval phase — obligation, stock, money — never inferred on the client. */
  closurePhase?: string;
  obligationClosedAt?: string | null;
  stockSettledAt?: string | null;
  releasedHeldQuantity?: number;
  /** TB-274: the reason recorded with the winning rejection decision (null for an approval). */
  decisionReason?: string | null;
  /** TB-275: the durable "Approve"/"Reject" decision row of this case, or null when none was recorded. */
  recordedDecision?: string | null;
  /** TB-275: every recorded customer receipt occurrence, oldest first. */
  receipts?: ReturnReceiptOccurrence[];
  /** TB-274: the receiving/inspection summary, computed only from recorded receipts (null when nothing arrived). */
  receiving?: ReturnReceivingSummary | null;
  /** TB-275: the ONE work stage/action description shared by the queue and this workspace. */
  work?: ReturnWorkDescription | null;
}

/** TB-274: the inspected coverage of one requested line. */
export interface ReturnReceivingLine {
  returnItemId: string;
  requested: number;
  received: number;
  usable: number;
  damaged: number;
}

/** TB-274: the stored receiving answer; completeness and coherence are separate facts. */
export interface ReturnReceivingSummary {
  lines: ReturnReceivingLine[];
  isComplete: boolean;
  isCoherent: boolean;
}

/** TB-275: the stable operator work-stage tokens of a return case. */
export type ReturnWorkCategory =
  | "receiving"
  | "decision"
  | "refund"
  | "refund_failed"
  | "disposition"
  | "settled"
  | "closed";

/** TB-275: what the operator owes on this case, as the owning module describes it. */
export interface ReturnWorkDescription {
  category: ReturnWorkCategory | string;
  blockerCode: string | null;
  pendingRefundAttemptId: string | null;
  failedRefundAttemptId: string | null;
  canRecordReceipt: boolean;
  canDecide: boolean;
  canResumeRefund: boolean;
  canRetryRefund: boolean;
  canSettlePhysical: boolean;
  isOpen: boolean;
  needsOperator: boolean;
  actionCodes: string[];
}

/** TB-275: one inspected line of a recorded customer receipt occurrence. */
export interface ReturnReceiptLine {
  returnItemId: string;
  quantity: number;
  condition: string;
}

/** TB-275: one recorded physical customer receipt (header + inspected lines). */
export interface ReturnReceiptOccurrence {
  returnReceiptId: string;
  occurrenceKey: string;
  recordedByUserId: string;
  recordedAt: string;
  lines: ReturnReceiptLine[];
}

export interface ReturnListRow {
  id: string;
  returnRequestId: string;
  sellerOrderId: string;
  checkoutId: string;
  status: string;
  refundAmount: number;
  currency: string;
  itemCount: number;
  createdAt: string;
}

function record(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : null;
}

function prop(item: Record<string, unknown>, camel: string, pascal: string): unknown {
  return item[camel] ?? item[pascal];
}

function text(value: unknown, fallback = ""): string {
  return value == null ? fallback : String(value);
}

function number(value: unknown): number {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
}

function nullableText(value: unknown): string | null {
  return value == null || String(value).length === 0 ? null : String(value);
}

/** TB-275: a genuinely absent numeric fact stays null instead of being coerced into a believable zero. */
function nullableNumber(value: unknown): number | null {
  if (value == null || value === "") return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function bool(value: unknown): boolean {
  return value === true || value === "true";
}

function mapReturnWork(value: unknown): ReturnWorkDescription | null {
  const work = record(value);
  if (!work) return null;
  const category = text(prop(work, "categoryCode", "CategoryCode")).trim();
  if (!category) return null;
  return {
    category,
    blockerCode: nullableText(prop(work, "blockerCode", "BlockerCode")),
    pendingRefundAttemptId: nullableText(prop(work, "pendingRefundAttemptId", "PendingRefundAttemptId")),
    failedRefundAttemptId: nullableText(prop(work, "failedRefundAttemptId", "FailedRefundAttemptId")),
    canRecordReceipt: bool(prop(work, "canRecordReceipt", "CanRecordReceipt")),
    canDecide: bool(prop(work, "canDecide", "CanDecide")),
    canResumeRefund: bool(prop(work, "canResumeRefund", "CanResumeRefund")),
    canRetryRefund: bool(prop(work, "canRetryRefund", "CanRetryRefund")),
    canSettlePhysical: bool(prop(work, "canSettlePhysical", "CanSettlePhysical")),
    isOpen: bool(prop(work, "isOpen", "IsOpen")),
    needsOperator: bool(prop(work, "needsOperator", "NeedsOperator")),
    actionCodes: stringList(prop(work, "actionCodes", "ActionCodes")),
  };
}

function mapReturnReceiving(value: unknown): ReturnReceivingSummary | null {
  const receiving = record(value);
  if (!receiving) return null;
  const linesRaw = prop(receiving, "lines", "Lines");
  return {
    lines: Array.isArray(linesRaw)
      ? linesRaw.flatMap((raw): ReturnReceivingLine[] => {
          const line = record(raw);
          const returnItemId = text(prop(line ?? {}, "returnItemId", "ReturnItemId"));
          if (!line || !returnItemId) return [];
          return [{
            returnItemId,
            requested: number(prop(line, "requested", "Requested")),
            received: number(prop(line, "received", "Received")),
            usable: number(prop(line, "usable", "Usable")),
            damaged: number(prop(line, "damaged", "Damaged")),
          }];
        })
      : [],
    isComplete: bool(prop(receiving, "isComplete", "IsComplete")),
    isCoherent: bool(prop(receiving, "isCoherent", "IsCoherent")),
  };
}

function mapReturnReceiptLine(value: unknown): ReturnReceiptLine | null {
  const line = record(value);
  if (!line) return null;
  const returnItemId = text(prop(line, "returnItemId", "ReturnItemId"));
  if (!returnItemId) return null;
  return {
    returnItemId,
    quantity: number(prop(line, "quantity", "Quantity")),
    condition: text(prop(line, "condition", "Condition"), "Usable"),
  };
}

function mapReturnReceiptOccurrence(value: unknown): ReturnReceiptOccurrence | null {
  const occurrence = record(value);
  if (!occurrence) return null;
  const returnReceiptId = text(prop(occurrence, "returnReceiptId", "ReturnReceiptId"));
  if (!returnReceiptId) return null;
  const linesRaw = prop(occurrence, "lines", "Lines");
  return {
    returnReceiptId,
    occurrenceKey: text(prop(occurrence, "occurrenceKey", "OccurrenceKey")),
    recordedByUserId: text(prop(occurrence, "recordedByUserId", "RecordedByUserId")),
    recordedAt: text(prop(occurrence, "recordedAt", "RecordedAt")),
    lines: Array.isArray(linesRaw)
      ? linesRaw.map(mapReturnReceiptLine).filter((row): row is ReturnReceiptLine => row !== null)
      : [],
  };
}

/** مقصد بازگشت وجه را نرمال می‌کند؛ مقدار ناشناخته → OriginalPayment. */
export function normalizeRefundDestination(value: unknown): RefundDestination {
  // Host may serialize enum as number (Wallet=1) or string.
  if (typeof value === "number") {
    return value === 1 ? "Wallet" : "OriginalPayment";
  }
  const raw = text(value).trim();
  if (raw === "1" || raw === "Wallet" || raw === "wallet") return "Wallet";
  return "OriginalPayment";
}

/** برچسب فارسی مقصد بازگشت وجه. */
export function formatRefundDestination(destination: RefundDestination | string): string {
  return normalizeRefundDestination(destination) === "Wallet" ? "کیف پول" : "پرداخت اصلی";
}

function mapReturnItem(value: unknown): ReturnItem | null {
  const item = record(value);
  if (!item) return null;
  const returnItemId = text(prop(item, "returnItemId", "ReturnItemId"));
  const orderLineId = text(prop(item, "orderLineId", "OrderLineId"));
  if (!returnItemId || !orderLineId) return null;
  return {
    returnItemId,
    orderLineId,
    quantity: number(prop(item, "quantity", "Quantity")),
    unitPriceSnapshot: number(prop(item, "unitPriceSnapshot", "UnitPriceSnapshot")),
    currency: text(prop(item, "currency", "Currency"), "IRR"),
    reservationId: nullableText(prop(item, "reservationId", "ReservationId")),
    condition: text(prop(item, "condition", "Condition"), "Usable"),
    dispositionCondition: nullableText(prop(item, "dispositionCondition", "DispositionCondition")),
    saleableRestockedQuantity: nullableNumber(prop(item, "saleableRestockedQuantity", "SaleableRestockedQuantity")),
    sourceShipmentId: nullableText(prop(item, "sourceShipmentId", "SourceShipmentId")),
    sourceReceiptId: nullableText(prop(item, "sourceReceiptId", "SourceReceiptId")),
  };
}

function mapRefundAttempt(value: unknown): RefundAttempt | null {
  const item = record(value);
  if (!item) return null;
  const refundAttemptId = text(prop(item, "refundAttemptId", "RefundAttemptId"));
  const paymentId = text(prop(item, "paymentId", "PaymentId"));
  if (!refundAttemptId || !paymentId) return null;
  return {
    refundAttemptId,
    paymentId,
    amount: number(prop(item, "amount", "Amount")),
    currency: text(prop(item, "currency", "Currency"), "IRR"),
    status: text(prop(item, "status", "Status")),
    idempotencyKey: text(prop(item, "idempotencyKey", "IdempotencyKey")),
    providerReference: nullableText(prop(item, "providerReference", "ProviderReference")),
    failureCode: nullableText(prop(item, "failureCode", "FailureCode")),
    createdAt: text(prop(item, "createdAt", "CreatedAt")),
    completedAt: nullableText(prop(item, "completedAt", "CompletedAt")),
  };
}

/** snapshot مرجوعی را از JSON Host نگاشت می‌کند. */
export function mapReturnSnapshot(value: unknown): ReturnSnapshot | null {
  const item = record(value);
  if (!item) return null;
  const returnRequestId = text(prop(item, "returnRequestId", "ReturnRequestId"));
  if (!returnRequestId) return null;
  const itemsRaw = prop(item, "items", "Items");
  const attemptsRaw = prop(item, "refundAttempts", "RefundAttempts");
  return {
    returnRequestId,
    sellerOrderId: text(prop(item, "sellerOrderId", "SellerOrderId")),
    checkoutId: text(prop(item, "checkoutId", "CheckoutId")),
    sellerPartyId: text(prop(item, "sellerPartyId", "SellerPartyId")),
    requestedByUserId: text(prop(item, "requestedByUserId", "RequestedByUserId")),
    status: text(prop(item, "status", "Status")),
    reason: nullableText(prop(item, "reason", "Reason")),
    currency: text(prop(item, "currency", "Currency"), "IRR"),
    refundAmount: number(prop(item, "refundAmount", "RefundAmount")),
    paymentId: nullableText(prop(item, "paymentId", "PaymentId")),
    destination: normalizeRefundDestination(
      prop(item, "refundDestination", "RefundDestination") ?? prop(item, "destination", "Destination"),
    ),
    createdAt: text(prop(item, "createdAt", "CreatedAt")),
    updatedAt: text(prop(item, "updatedAt", "UpdatedAt")),
    items: Array.isArray(itemsRaw)
      ? itemsRaw.map(mapReturnItem).filter((row): row is ReturnItem => row !== null)
      : [],
    refundAttempts: Array.isArray(attemptsRaw)
      ? attemptsRaw.map(mapRefundAttempt).filter((row): row is RefundAttempt => row !== null)
      : [],
    origin: text(prop(item, "origin", "Origin"), "CustomerDelivered"),
    sourceFulfillmentId: nullableText(prop(item, "sourceFulfillmentId", "SourceFulfillmentId")),
    sourceShipmentId: nullableText(prop(item, "sourceShipmentId", "SourceShipmentId")),
    sourceReceiptId: nullableText(prop(item, "sourceReceiptId", "SourceReceiptId")),
    sourceClaimId: nullableText(prop(item, "sourceClaimId", "SourceClaimId")),
    closurePhase: text(prop(item, "closurePhase", "ClosurePhase"), "None"),
    obligationClosedAt: nullableText(prop(item, "obligationClosedAt", "ObligationClosedAt")),
    stockSettledAt: nullableText(prop(item, "stockSettledAt", "StockSettledAt")),
    releasedHeldQuantity: number(prop(item, "releasedHeldQuantity", "ReleasedHeldQuantity")),
    decisionReason: nullableText(prop(item, "decisionReason", "DecisionReason")),
    recordedDecision: nullableText(prop(item, "recordedDecision", "RecordedDecision")),
    receipts: Array.isArray(prop(item, "receipts", "Receipts"))
      ? (prop(item, "receipts", "Receipts") as unknown[])
          .map(mapReturnReceiptOccurrence)
          .filter((row): row is ReturnReceiptOccurrence => row !== null)
      : [],
    receiving: mapReturnReceiving(prop(item, "receiving", "Receiving")),
    work: mapReturnWork(prop(item, "work", "Work")),
  };
}

/** فهرست مرجوعی را برای grid نگاشت می‌کند. */
export function mapReturnList(value: unknown): ReturnListRow[] {
  const items = Array.isArray(value) ? value : [];
  return items.flatMap((raw): ReturnListRow[] => {
    const snapshot = mapReturnSnapshot(raw);
    if (!snapshot) return [];
    return [{
      id: snapshot.returnRequestId,
      returnRequestId: snapshot.returnRequestId,
      sellerOrderId: snapshot.sellerOrderId,
      checkoutId: snapshot.checkoutId,
      status: snapshot.status,
      refundAmount: snapshot.refundAmount,
      currency: snapshot.currency,
      itemCount: snapshot.items.length,
      createdAt: snapshot.createdAt,
    }];
  });
}

export interface ReturnWorkQueueLine {
  productLabel: string;
  quantity: number;
  unitLabel: string;
  condition: string;
}

export interface ReturnWorkQueueRow {
  id: string;
  returnRequestId: string;
  sellerOrderId: string;
  checkoutId: string;
  sellerPartyId: string;
  returnReference: string;
  orderReference: string;
  customerDisplayName: string;
  sellerDisplayName: string;
  productLabel: string;
  quantityRequested: number;
  unitLabel: string;
  returnStatus: string;
  refundStatus: string;
  eligibilitySummary: string;
  createdAt: string;
  updatedAt: string;
  availableActionCodes: string[];
  /** TB-275: every requested line with its OWN unit, so two incompatible units are never summed. */
  lines: ReturnWorkQueueLine[];
  /** TB-275: the ONE work stage token this row owns. */
  workCategory: string;
  workBlockerCode: string | null;
  needsOperator: boolean;
  pendingRefundAttemptId: string | null;
  failedRefundAttemptId: string | null;
}

/** TB-275: the fast work-strip counts, counted before paging over the same constrained set. */
export interface AdminReturnWorkCounts {
  allOpen: number;
  receiving: number;
  decision: number;
  refund: number;
  refundFailed: number;
  disposition: number;
  history: number;
}

/**
 * TB-275: the queue selector tokens. `open` is the active work set and `history` the closed/settled set; both are
 * answered by the server's ONE classification, so a badge and its rows can never disagree.
 */
export type ReturnWorkQueueSelection =
  | "open"
  | "receiving"
  | "decision"
  | "refund"
  | "refund_failed"
  | "disposition"
  | "history";

export const RETURN_WORK_STRIPS: { id: ReturnWorkQueueSelection; labelFa: string; countKey: keyof AdminReturnWorkCounts }[] = [
  { id: "open", labelFa: "همهٔ کارهای باز", countKey: "allOpen" },
  { id: "receiving", labelFa: "دریافت کالا", countKey: "receiving" },
  { id: "decision", labelFa: "بررسی و تصمیم", countKey: "decision" },
  { id: "refund", labelFa: "پیگیری بازگشت وجه", countKey: "refund" },
  { id: "refund_failed", labelFa: "بازگشت وجه ناموفق", countKey: "refundFailed" },
  { id: "disposition", labelFa: "تعیین تکلیف کالا", countKey: "disposition" },
  { id: "history", labelFa: "سابقه", countKey: "history" },
];

/**
 * TB-275: ONE canonical selection token from any legacy queue token or new stage token. It mirrors the server's own
 * translation, so a bookmarked `?queueFilter=refund_needed` link lands on the same strip the server would select, and
 * an unknown token falls back to the honest active set instead of "everything".
 */
export function normalizeReturnWorkSelection(raw: string | null | undefined): ReturnWorkQueueSelection {
  const token = (raw ?? "").trim().toLowerCase();
  switch (token) {
    case "":
    case "all":
      return "open";
    case "pending_review":
      return "decision";
    case "approved":
    case "refund_needed":
    case "refund_pending":
      return "refund";
    case "refund_failed":
      return "refund_failed";
    case "completed":
    case "rejected":
      return "history";
    case "open":
    case "receiving":
    case "decision":
    case "refund":
    case "disposition":
    case "history":
      return token;
    default:
      return "open";
  }
}

/** Reads the initial selection of a queue page from its URL (legacy tokens included), once. */
export function readReturnWorkSelectionFromUrl(search: string): ReturnWorkQueueSelection {
  const params = new URLSearchParams(search.startsWith("?") ? search : `?${search}`);
  return normalizeReturnWorkSelection(params.get("workCategory") ?? params.get("queueFilter"));
}

/**
 * Writes the current selection into the URL as ONE canonical token and drops the legacy one, so returning from the
 * case workspace restores the same strip without ever carrying both an old and a new filter.
 */
export function writeReturnWorkSelectionToUrl(search: string, selection: ReturnWorkQueueSelection): string {
  const params = new URLSearchParams(search.startsWith("?") ? search : `?${search}`);
  params.delete("queueFilter");
  if (selection === "open") params.delete("workCategory");
  else params.set("workCategory", selection);
  const query = params.toString();
  return query ? `?${query}` : "";
}

/** TB-275: the Persian label of one work stage (the queue row badge and the workspace heading share it). */export function formatReturnWorkCategory(category: string | null | undefined): string {
  const labels: Record<string, string> = {
    receiving: "دریافت کالا",
    decision: "بررسی و تصمیم",
    refund: "بازگشت وجه",
    refund_failed: "بازگشت وجه ناموفق",
    disposition: "تعیین تکلیف کالا",
    settled: "تسویه‌شده",
    closed: "بسته‌شده",
  };
  return labels[category ?? ""] ?? "نامشخص";
}

/** TB-275: the badge tone of one work stage. */
export function returnWorkBadgeClass(category: string | null | undefined): string {
  const tone =
    category === "settled"
      ? "bg-emerald-50 text-emerald-700"
      : category === "refund_failed"
        ? "bg-red-50 text-red-700"
        : category === "disposition" || category === "receiving"
          ? "bg-amber-50 text-amber-700"
          : category === "refund" || category === "decision"
            ? "bg-blue-50 text-[#2563EB]"
            : "bg-gray-50 text-gray-600";
  return `inline-flex rounded-xl px-3 py-1 text-xs font-bold ${tone}`;
}

/** TB-275: the inspection token of one received line. */
export function formatReturnCondition(condition: string): string {
  return condition === "Damaged" ? "معیوب" : "سالم";
}

export type ReturnQueueFilter =
  | "all"
  | "pending_review"
  | "approved"
  | "refund_needed"
  | "refund_pending"
  | "refund_failed"
  | "completed"
  | "rejected";

export const RETURN_QUEUE_FILTERS: { id: ReturnQueueFilter; labelFa: string }[] = [
  { id: "all", labelFa: "همه" },
  { id: "pending_review", labelFa: "در انتظار بررسی" },
  { id: "approved", labelFa: "تأییدشده" },
  { id: "refund_needed", labelFa: "نیازمند بازگشت وجه" },
  { id: "refund_pending", labelFa: "بازگشت وجه در انتظار" },
  { id: "refund_failed", labelFa: "بازگشت وجه ناموفق" },
  { id: "completed", labelFa: "تکمیل‌شده" },
  { id: "rejected", labelFa: "ردشده" },
];

function stringList(raw: unknown): string[] {
  return Array.isArray(raw) ? raw.filter((x): x is string => typeof x === "string") : [];
}

/** ردیف صف کار Admin را از DTO Host نگاشت می‌کند. */
export function mapReturnWorkQueueRow(value: unknown): ReturnWorkQueueRow | null {
  const item = record(value);
  if (!item) return null;
  const returnRequestId = text(prop(item, "returnRequestId", "ReturnRequestId"));
  const checkoutId = text(prop(item, "checkoutId", "CheckoutId"));
  if (!returnRequestId || !checkoutId) return null;
  return {
    id: returnRequestId,
    returnRequestId,
    sellerOrderId: text(prop(item, "sellerOrderId", "SellerOrderId")),
    checkoutId,
    sellerPartyId: text(prop(item, "sellerPartyId", "SellerPartyId")),
    returnReference: text(prop(item, "returnReference", "ReturnReference")),
    orderReference: text(prop(item, "orderReference", "OrderReference")),
    customerDisplayName: text(prop(item, "customerDisplayName", "CustomerDisplayName")),
    sellerDisplayName: text(prop(item, "sellerDisplayName", "SellerDisplayName")),
    productLabel: text(prop(item, "productLabel", "ProductLabel")),
    quantityRequested: number(prop(item, "quantityRequested", "QuantityRequested")),
    unitLabel: text(prop(item, "unitLabel", "UnitLabel"), "واحد"),
    returnStatus: text(prop(item, "returnStatus", "ReturnStatus")),
    refundStatus: text(prop(item, "refundStatus", "RefundStatus")),
    eligibilitySummary: text(prop(item, "eligibilitySummary", "EligibilitySummary")),
    createdAt: text(prop(item, "createdAt", "CreatedAt")),
    updatedAt: text(prop(item, "updatedAt", "UpdatedAt")),
    availableActionCodes: stringList(prop(item, "availableActionCodes", "AvailableActionCodes")),
    lines: mapReturnWorkQueueLines(prop(item, "lines", "Lines")),
    workCategory: text(prop(item, "workCategory", "WorkCategory"), "closed"),
    workBlockerCode: nullableText(prop(item, "workBlockerCode", "WorkBlockerCode")),
    needsOperator: bool(prop(item, "needsOperator", "NeedsOperator")),
    pendingRefundAttemptId: nullableText(prop(item, "pendingRefundAttemptId", "PendingRefundAttemptId")),
    failedRefundAttemptId: nullableText(prop(item, "failedRefundAttemptId", "FailedRefundAttemptId")),
  };
}

function mapReturnWorkQueueLines(raw: unknown): ReturnWorkQueueLine[] {
  if (!Array.isArray(raw)) return [];
  return raw.flatMap((value): ReturnWorkQueueLine[] => {
    const line = record(value);
    if (!line) return [];
    return [{
      productLabel: text(prop(line, "productLabel", "ProductLabel")),
      quantity: number(prop(line, "quantity", "Quantity")),
      unitLabel: text(prop(line, "unitLabel", "UnitLabel"), "واحد"),
      condition: text(prop(line, "condition", "Condition"), "Usable"),
    }];
  });
}

/** TB-275: the pre-paging counts of one constrained queue request. */
export function mapAdminReturnWorkCounts(payload: unknown): AdminReturnWorkCounts | undefined {
  const counts = record(prop(record(payload) ?? {}, "counts", "Counts"));
  if (!counts) return undefined;
  return {
    allOpen: number(prop(counts, "allOpen", "AllOpen")),
    receiving: number(prop(counts, "receiving", "Receiving")),
    decision: number(prop(counts, "decision", "Decision")),
    refund: number(prop(counts, "refund", "Refund")),
    refundFailed: number(prop(counts, "refundFailed", "RefundFailed")),
    disposition: number(prop(counts, "disposition", "Disposition")),
    history: number(prop(counts, "history", "History")),
  };
}

/** فهرست صف کار Admin. */
export function mapReturnWorkQueueList(value: unknown): ReturnWorkQueueRow[] {
  const items = Array.isArray(value) ? value : [];
  return items.flatMap((raw) => {
    const row = mapReturnWorkQueueRow(raw);
    return row ? [row] : [];
  });
}

/** وضعیت مرجوعی را برای UI فارسی می‌کند. */
export function formatReturnStatus(status: string): string {
  const labels: Record<string, string> = {
    Requested: "در انتظار بررسی",
    Approved: "تأیید شده",
    Rejected: "رد شده",
    RefundProcessing: "در حال بازگشت وجه",
    Completed: "تکمیل شده",
    RefundFailed: "بازگشت وجه ناموفق",
    Cancelled: "لغو شده",
  };
  return labels[status] ?? (status || "نامشخص");
}

/** وضعیت جداگانهٔ بازگشت وجه در صف کار Admin. */
export function formatRefundLifecycleStatus(status: string): string {
  const labels: Record<string, string> = {
    none: "بدون بازگشت وجه",
    pending: "در انتظار بازگشت وجه",
    failed: "بازگشت وجه ناموفق",
    completed: "بازگشت وجه تکمیل‌شده",
  };
  return labels[status] ?? (status || "نامشخص");
}

/** badge وضعیت بازگشت وجه در صف کار. */
export function refundLifecycleBadgeClass(status: string): string {
  const tone =
    status === "completed"
      ? "bg-emerald-50 text-emerald-700"
      : status === "failed"
        ? "bg-red-50 text-red-700"
        : status === "pending"
          ? "bg-blue-50 text-[#2563EB]"
          : "bg-gray-50 text-gray-600";
  return `inline-flex rounded-xl px-3 py-1 text-xs font-bold ${tone}`;
}

/** وضعیت تلاش refund را فارسی می‌کند. */
export function formatRefundAttemptStatus(status: string): string {
  const labels: Record<string, string> = {
    Pending: "در انتظار",
    Succeeded: "موفق",
    Failed: "ناموفق",
  };
  return labels[status] ?? (status || "نامشخص");
}

function returnStatusClasses(status: string): string {
  switch (status) {
    case "Completed":
      return "bg-emerald-50 text-emerald-700";
    case "Rejected":
    case "RefundFailed":
    case "Cancelled":
      return "bg-red-50 text-red-700";
    case "RefundProcessing":
    case "Approved":
      return "bg-blue-50 text-[#2563EB]";
    default:
      return "bg-amber-50 text-amber-700";
  }
}

/** badge وضعیت مرجوعی. */
export function returnStatusBadgeClass(status: string): string {
  return `inline-flex rounded-xl px-3 py-1 text-xs font-bold ${returnStatusClasses(status)}`;
}

/** تاریخ ISO را برای نمایش فارسی برمی‌گرداند. */
export function formatReturnDate(value: string | null | undefined): string {
  if (!value) return "—";
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? value
    : new Intl.DateTimeFormat("fa-IR", { year: "numeric", month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit" }).format(date);
}

/** فهرست مرجوعی‌های مشتری. */
export async function loadCustomerReturns(): Promise<ReturnSnapshot[] | null> {
  try {
    const response = await fetch("/api/customer/returns", {
      credentials: "include",
      headers: customerAuthHeaders(),
    });
    if (response.status === 404) return [];
    if (!response.ok) return null;
    const payload = await response.json();
    if (!Array.isArray(payload)) return null;
    return payload.map(mapReturnSnapshot).filter((row): row is ReturnSnapshot => row !== null);
  } catch {
    return null;
  }
}

/** جزئیات مرجوعی مشتری. */
export async function loadCustomerReturnDetail(returnRequestId: string): Promise<ReturnSnapshot | null> {
  try {
    const response = await fetch(`/api/customer/returns/${encodeURIComponent(returnRequestId)}`, {
      credentials: "include",
      headers: customerAuthHeaders(),
    });
    if (response.status === 404) return null;
    if (!response.ok) return null;
    return mapReturnSnapshot(await response.json());
  } catch {
    return null;
  }
}

export interface CreateReturnLineInput {
  orderLineId: string;
  quantity: number;
}

/** درخواست مرجوعی مشتری را ثبت می‌کند. */
export async function createCustomerReturn(input: {
  sellerOrderId: string;
  reason?: string;
  items: CreateReturnLineInput[];
  destination?: RefundDestination;
  idempotencyKey?: string;
}): Promise<{ ok: true; snapshot: ReturnSnapshot } | { ok: false; errorCode: string }> {
  try {
    const response = await fetch("/api/customer/returns", {
      method: "POST",
      credentials: "include",
      headers: { ...customerAuthHeaders(), "Content-Type": "application/json" },
      body: JSON.stringify({
        sellerOrderId: input.sellerOrderId,
        idempotencyKey: input.idempotencyKey ?? crypto.randomUUID(),
        reason: input.reason ?? null,
        destination: input.destination ?? DEFAULT_REFUND_DESTINATION,
        refundDestination: input.destination ?? DEFAULT_REFUND_DESTINATION,
        items: input.items,
      }),
    });
    if (!response.ok) {
      const payload = record(await response.json().catch(() => null));
      return { ok: false, errorCode: text(prop(payload ?? {}, "errorCode", "ErrorCode"), "return.rejected") };
    }
    const snapshot = mapReturnSnapshot(await response.json());
    if (!snapshot) return { ok: false, errorCode: "return.invalid-response" };
    return { ok: true, snapshot };
  } catch {
    return { ok: false, errorCode: "host-unreachable" };
  }
}

function sellerHeaders(sellerPartyId: string, json = false): Record<string, string> {
  const headers: Record<string, string> = {
    Accept: "application/json",
    [SELLER_PARTY_HEADER]: sellerPartyId,
  };
  if (json) headers["Content-Type"] = "application/json";
  const actor = readActorUserId();
  if (actor) headers[DEV_ACTOR_HEADER] = actor;
  return headers;
}

function adminActorHeader(json = false): Record<string, string> {
  const actor = typeof window !== "undefined"
    ? window.localStorage.getItem("tooba.adminActorUserId") ?? ""
    : "";
  const headers: Record<string, string> = { Accept: "application/json", [ADMIN_DEV_ACTOR_HEADER]: actor };
  if (json) headers["Content-Type"] = "application/json";
  return headers;
}

/** فهرست مرجوعی فروشنده. */
export async function loadSellerReturns(
  sellerPartyId: string,
): Promise<{ source: HostReadSource; rows: ReturnListRow[]; message?: string; denied?: boolean }> {
  try {
    const response = await fetch("/v1/seller/returns", { headers: sellerHeaders(sellerPartyId) });
    if (response.status === 401 || response.status === 403) {
      return { source: "error", rows: [], message: "seller.authorization.denied", denied: true };
    }
    if (!response.ok) return { source: "error", rows: [], message: `seller-return-http-${response.status}` };
    return { source: "host", rows: mapReturnList(await response.json()) };
  } catch {
    return { source: "error", rows: [], message: "host-unreachable" };
  }
}

/** جزئیات مرجوعی فروشنده. */
export async function loadSellerReturnDetail(
  sellerPartyId: string,
  returnRequestId: string,
): Promise<{ source: HostReadSource; snapshot: ReturnSnapshot | null; message?: string; denied?: boolean }> {
  try {
    const response = await fetch(`/v1/seller/returns/${encodeURIComponent(returnRequestId)}`, {
      headers: sellerHeaders(sellerPartyId),
    });
    if (response.status === 401 || response.status === 403 || response.status === 404) {
      return { source: "error", snapshot: null, message: "return.missing", denied: true };
    }
    if (!response.ok) return { source: "error", snapshot: null, message: `seller-return-http-${response.status}` };
    return { source: "host", snapshot: mapReturnSnapshot(await response.json()) };
  } catch {
    return { source: "error", snapshot: null, message: "host-unreachable" };
  }
}

async function sellerMutate(
  sellerPartyId: string,
  path: string,
  body?: unknown,
): Promise<{ ok: true; snapshot: ReturnSnapshot } | { ok: false; errorCode: string; denied?: boolean }> {
  try {
    const response = await fetch(path, {
      method: "POST",
      headers: sellerHeaders(sellerPartyId, Boolean(body)),
      body: body ? JSON.stringify(body) : undefined,
    });
    if (response.status === 401 || response.status === 403 || response.status === 404) {
      return { ok: false, errorCode: "seller.authorization.denied", denied: true };
    }
    if (!response.ok) {
      const payload = record(await response.json().catch(() => null));
      return { ok: false, errorCode: text(prop(payload ?? {}, "errorCode", "ErrorCode"), "return.rejected") };
    }
    const snapshot = mapReturnSnapshot(await response.json());
    if (!snapshot) return { ok: false, errorCode: "return.invalid-response" };
    return { ok: true, snapshot };
  } catch {
    return { ok: false, errorCode: "host-unreachable" };
  }
}

export function sellerApproveReturn(
  sellerPartyId: string,
  returnRequestId: string,
  destination?: RefundDestination,
) {
  const dest = destination ?? DEFAULT_REFUND_DESTINATION;
  return sellerMutate(sellerPartyId, `/v1/seller/returns/${returnRequestId}/approve`, {
    destination: dest,
    refundDestination: dest,
  });
}

export function sellerRejectReturn(sellerPartyId: string, returnRequestId: string, reason?: string) {
  return sellerMutate(sellerPartyId, `/v1/seller/returns/${returnRequestId}/reject`, { reason: reason ?? null });
}

/** فهرست مرجوعی Admin. */
export async function loadAdminReturns(): Promise<AdminResult<ReturnListRow[]>> {
  try {
    const response = await fetch("/v1/admin/returns", { headers: adminActorHeader() });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) return { state: "error", data: null, status: response.status, message: `admin.http.${response.status}` };
    return { state: "ok", data: mapReturnList(payload), status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** Server GridQuery — صف کار مرجوعی Admin. */
export function queryAdminReturnsGrid(query: GridServerQuery): Promise<AdminGridQueryResult<ReturnWorkQueueRow>> {
  return postAdminGridQuery("/v1/admin/returns/query", query, adminActorHeader(), mapReturnWorkQueueRow);
}

/**
 * TB-275: Server GridQuery of the work queue WITH the pre-paging work-strip counts.
 * <para>
 * The selection is sent as the ONE `workCategory` cell filter (never inside the advanced-filter expression, which the
 * server refuses) and the counts come from the same constrained classification, so a badge can never disagree with
 * the rows it selects. An absent selection is the honest active work set, not "everything".
 * </para>
 */
export function queryAdminReturnsWorkQueue(
  query: GridServerQuery,
  selection: ReturnWorkQueueSelection,
): Promise<AdminGridQueryResult<ReturnWorkQueueRow> & { meta?: AdminReturnWorkCounts }> {
  const filters = { ...query.filters };
  delete filters.queueFilter;
  if (selection !== "open") {
    filters.workCategory = { kind: "status", values: [selection] };
  } else {
    delete filters.workCategory;
  }
  return postAdminGridQuery(
    "/v1/admin/returns/query",
    { ...query, filters },
    adminActorHeader(),
    mapReturnWorkQueueRow,
    mapAdminReturnWorkCounts,
  );
}

/** جزئیات مرجوعی Admin. */
export async function loadAdminReturnDetail(returnRequestId: string): Promise<AdminResult<ReturnSnapshot>> {
  try {
    const response = await fetch(`/v1/admin/returns/${encodeURIComponent(returnRequestId)}`, {
      headers: adminActorHeader(),
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (response.status === 404) return { state: "ok", data: null, status: 404 };
    if (!response.ok) return { state: "error", data: null, status: response.status, message: `admin.http.${response.status}` };
    return { state: "ok", data: mapReturnSnapshot(payload), status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** retry refund توسط Admin. */
export async function adminRetryReturnRefund(
  returnRequestId: string,
  expectedFailedAttemptId?: string | null,
): Promise<AdminResult<ReturnSnapshot>> {
  const query = expectedFailedAttemptId ? `?expectedFailedAttemptId=${encodeURIComponent(expectedFailedAttemptId)}` : "";
  try {
    const response = await fetch(
      `/v1/admin/returns/${encodeURIComponent(returnRequestId)}/retry-refund${query}`,
      {
        method: "POST",
        headers: adminActorHeader(),
      },
    );
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: text(prop(record(payload) ?? {}, "errorCode", "ErrorCode"), "return.rejected") };
    }
    return { state: "ok", data: mapReturnSnapshot(payload), status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/**
 * TB-275: record ONE physical customer receipt occurrence (inspected quantities only).
 * <para>
 * The occurrence key is the reviewed identity of that delivery, so a duplicate click or a lost response resolves to
 * the SAME stored occurrence instead of creating a second physical fact.
 * </para>
 */
export async function adminRecordReturnReceipt(input: {
  returnRequestId: string;
  occurrenceKey: string;
  items: { returnItemId: string; quantity: number; condition: "Usable" | "Damaged" }[];
}): Promise<AdminResult<ReturnSnapshot>> {
  return adminReturnsPost(`/v1/admin/returns/${encodeURIComponent(input.returnRequestId)}/receipt`, {
    occurrenceKey: input.occurrenceKey,
    items: input.items,
  });
}

/** TB-275: the reviewed recovery intent (`resume_pending_refund` / `settle_physical_disposition`). */
export async function adminRecoverReturn(input: {
  returnRequestId: string;
  kind: "resume_pending_refund" | "settle_physical_disposition";
  expectedRefundAttemptId?: string | null;
}): Promise<AdminResult<ReturnSnapshot>> {
  return adminReturnsPost(`/v1/admin/returns/${encodeURIComponent(input.returnRequestId)}/recover`, {
    kind: input.kind,
    expectedRefundAttemptId: input.expectedRefundAttemptId ?? null,
  });
}

async function adminReturnsPost(path: string, body: unknown): Promise<AdminResult<ReturnSnapshot>> {
  try {
    const response = await fetch(path, {
      method: "POST",
      headers: adminActorHeader(true),
      body: JSON.stringify(body),
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return {
        state: "error",
        data: null,
        status: response.status,
        message: text(prop(record(payload) ?? {}, "errorCode", "ErrorCode"), "return.rejected"),
      };
    }
    return { state: "ok", data: mapReturnSnapshot(payload), status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}
