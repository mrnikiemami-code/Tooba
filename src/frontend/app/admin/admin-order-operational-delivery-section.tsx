"use client";

import React, { useEffect, useMemo, useRef, useState } from "react";
import { Package, Truck } from "lucide-react";
import {
  formatAdminMoney,
  formatAdminShipmentStatus,
  formatAdminStatus,
  type AdminAcceptedShippingGroup,
  type AdminDamagedResolution,
  type AdminOperationalDeliveryGroup,
  type AdminOperationalDeliveryMember,
  type AdminOrderDetail,
  type AdminOrderLine,
  type AdminSellerOrder,
  type AdminShipment,
} from "./admin-api";
import { formatJalaliDate } from "../../design-system/app-data-grid/jalali";
import { MessageBox } from "../../design-system";
import {
  clearOperationalGroupAttempt,
  markOperationalGroupAttemptFailed,
  readOperationalGroupAttempt,
  resolveOperationalGroupAttempt,
  type OperationalGroupAttempt,
} from "./operational-group-occurrence-store";
import {
  AdminOperationalGroupWorkflowDialog,
  normalizePostDispatchOutcome,
  listPostDispatchAttemptsForParcel,
  OperationalDeliveryTimeline,
  POST_DISPATCH_MODE_LABEL_FA,
  PostDispatchExceptionDialog,
  type OperationalGroupDialogRequest,
  type OperationalGroupDialogRow,
  type PostDispatchAttempt,
  type PostDispatchExceptionMode,
  type PostDispatchExceptionRequest,
  type PostDispatchParcelLine,
  type PostDispatchSubmitOutcome,
} from "../../features/order-operational-groups";

export type OperationalCreateTarget = {
  seller: AdminSellerOrder;
  group: AdminAcceptedShippingGroup;
};

/** Permitted next actions of one parcel, derived by the owning panel from projected operations. */
export type OperationalParcelCapabilities = {
  canTrack: boolean;
  canDispatch: boolean;
  canDeliver: boolean;
  canCancel: boolean;
  /** TB-258: the dispatched, not-yet-delivered parcel may be recorded as non-delivered. */
  canRecordNonDelivery: boolean;
  /** TB-258: a recorded non-delivery may start the physical return to origin. */
  canStartReturn: boolean;
  /** TB-258: a returning parcel may confirm its physical origin receipt. */
  canRecordReturnReceipt: boolean;
  /** TB-258: usable returned quantity may fund a new resend parcel. */
  canResend: boolean;
  /** TB-262: damaged units received back on this parcel still owe one deliberate decision each. */
  canResolveDamagedGoods: boolean;
  /** TB-265: this courier-origin receipt may open a linked case in the EXISTING returns workflow. */
  canCreateCourierReturn: boolean;
};

/**
 * TB-253-R5/R6: explicit workflow outcome. A boolean/void return keeps meaning "completed" / "unknown".
 * No refusal code is accepted as proof of "nothing happened": the retained envelope is only cleared by
 * an authoritative completion.
 */
export type OperationalGroupWorkflowOutcome =
  | { completed: true }
  | { completed: false; errorFa?: string | null };

function outcomeCompleted(outcome: boolean | OperationalGroupWorkflowOutcome | void): boolean {
  if (outcome === true) return true;
  if (outcome && typeof outcome === "object") return outcome.completed === true;
  return false;
}

function outcomeError(outcome: boolean | OperationalGroupWorkflowOutcome | void): string | null {
  if (outcome && typeof outcome === "object" && !outcome.completed) return outcome.errorFa ?? null;
  return null;
}

/** TB-253-R2/R3: reviewed input of the one group-level parcel/package workflow. */
export type OperationalGroupWorkflowRequest = {
  operationalGroupShippingGroupIds: string[];
  selections: Array<{ orderLineId: string; quantity: number }>;
  reuseShipmentIds: string[];
  /** Prebound BEFORE the first mutation; retained on retry, renewed only after success. */
  operationalGroupOccurrenceId: string;
  /** TB-259: the reviewed physical packing choice, recorded in the same immutable envelope. */
  packingMode: "common" | "separate";
};

type Props = {
  detail: AdminOrderDetail;
  pendingCode: string | null;
  onCreateShipment: (target: OperationalCreateTarget) => void;
  /** One reviewed group workflow: constituent parcels (create/reuse) + their common package. */
  onPrepareOperationalGroup?: (
    group: AdminOperationalDeliveryGroup,
    request: OperationalGroupWorkflowRequest,
  ) => Promise<boolean | OperationalGroupWorkflowOutcome | void> | boolean | OperationalGroupWorkflowOutcome | void;
  /**
   * TB-253-R4: viewer/owner scope of a retained workflow occurrence. The prebound envelope is stored
   * per checkout/group/scope, so another admin session can never inherit it.
   */
  occurrenceScopeKey?: string | null;
  /**
   * TB-259: the packing choices the SERVER projected for this group's prepare action. The dialog renders
   * exactly these; a missing projection falls back to the two server-defined choices.
   */
  packingModes?: readonly ("common" | "separate")[] | null;
  /** TB-254: exactly one parcel/action owner — the group card shows the permitted next action. */
  parcelCapabilities?: (seller: AdminSellerOrder, shipment: AdminShipment) => OperationalParcelCapabilities;
  onParcelTrack?: (seller: AdminSellerOrder, shipment: AdminShipment) => void;
  onParcelOperation?: (seller: AdminSellerOrder, code: string, shipmentId: string) => void;
  /**
   * TB-258: the reviewed post-dispatch exception (non-delivery / return / origin receipt / resend /
   * damaged-goods decision). It answers the SERVER's semantic outcome: only `completed` closes the review;
   * a pending decision, a deterministic refusal and an unknown answer are different facts.
   */
  onParcelException?: (
    seller: AdminSellerOrder,
    shipmentId: string,
    request: PostDispatchExceptionRequest,
  ) => Promise<PostDispatchSubmitOutcome | boolean> | PostDispatchSubmitOutcome | boolean;
  /** TB-263: owning facts changed (a blocking refusal) — the caller re-reads the authoritative detail. */
  onFactsChanged?: () => void;
};

/** No permitted action: a cancelled, package-owned, or pending parcel exposes nothing here. */
const NO_PARCEL_ACTIONS: OperationalParcelCapabilities = {
  canTrack: false,
  canDispatch: false,
  canDeliver: false,
  canCancel: false,
  canRecordNonDelivery: false,
  canStartReturn: false,
  canRecordReturnReceipt: false,
  canResend: false,
  canResolveDamagedGoods: false,
  canCreateCourierReturn: false,
};

function lineKey(line: AdminOrderLine): string {
  return line.orderLineId || line.id;
}

function resolveAcceptedGroup(
  detail: AdminOrderDetail,
  shippingGroupId: string,
): AdminAcceptedShippingGroup | null {
  return (detail.shippingGroups ?? []).find((g) => g.shippingGroupId === shippingGroupId) ?? null;
}

function resolveSeller(detail: AdminOrderDetail, sellerOrderId: string): AdminSellerOrder | null {
  return detail.sellerOrders.find((s) => s.id === sellerOrderId) ?? null;
}

function memberShipments(seller: AdminSellerOrder, shippingGroupId: string): AdminShipment[] {
  return (seller.shipments ?? []).filter((s) => s.shippingGroupId === shippingGroupId);
}

function isCancelledShipment(status: string | null | undefined): boolean {
  return status === "Cancelled" || status === "Canceled";
}

function isPackageLocked(parcel: AdminShipment): boolean {
  return Boolean(parcel.activePackageNumber || parcel.packageLockedReasonFa);
}

function isCarrierException(status: string | null | undefined): boolean {
  return status === "NonDelivered" || status === "Returning" || status === "Returned";
}

/**
 * TB-261: the physical condition totals of an origin receipt. The server records one receipt line per
 * (line, condition); only «سالم» funds a resend, and «آسیب‌دیده» stays an explicit unresolved requirement.
 */
function receiptTotals(parcel: AdminShipment): { usable: number; damaged: number; outstanding: number } {
  let usable = 0;
  let damaged = 0;
  for (const line of parcel.returnReceiptLines ?? []) {
    const condition = (line.condition ?? "").trim();
    if (condition === "Damaged" || condition === "1") damaged += line.quantity;
    else usable += line.quantity;
  }
  // TB-262: what still owes a decision is the server's OWN per-parcel remaining damaged quantity. When the
  // server did not project it at all there is no resolution concept for this row, so the receipt total is
  // the honest statement; a projected zero must never be rewritten back to «needs a decision».
  const projected = (parcel.lines ?? []).filter((line) => line.outstandingDamagedQuantity != null);
  const outstanding = projected.length > 0
    ? projected.reduce((sum, line) => sum + Math.max(0, line.outstandingDamagedQuantity ?? 0), 0)
    : damaged;
  return { usable, damaged, outstanding };
}

/** Short, human identity of another parcel of the same group (no raw GUID in operator copy). */
function parcelShortId(parcel: AdminShipment | null | undefined, fallbackId: string): string {
  const label = (parcel?.trackingReference ?? "").trim();
  return label || fallbackId.slice(0, 8);
}

function remainingPacked(line: AdminOrderLine): number {
  const packed = line.quantityPacked ?? 0;
  const allocated = line.quantityAllocated ?? line.quantityShipped ?? 0;
  return Math.max(0, packed - allocated);
}

function groupItems(detail: AdminOrderDetail, member: AdminOperationalDeliveryMember): AdminOrderLine[] {
  const seller = resolveSeller(detail, member.sellerOrderId);
  if (!seller) return [];
  const ids = new Set(member.orderLineIds);
  return seller.lines.filter((line) => ids.has(lineKey(line)));
}

function jalaliDay(isoDate: string | null | undefined): string | null {
  const raw = (isoDate ?? "").trim();
  if (!raw) return null;
  return formatJalaliDate(raw.includes("T") ? raw : `${raw}T12:00:00`, "fa");
}

/**
 * TB-264: renders one operational deadline instant in the CARRIER time zone (the zone the accepted
 * plan was computed in), so an operator never reads a handoff deadline against their own clock. A
 * missing/invalid instant yields null (no invented deadline), and an unusable zone id degrades to the
 * operator's local zone instead of throwing.
 */
function carrierDeadlineLabel(isoUtc: string | null | undefined, zone: string | null): string | null {
  const raw = (isoUtc ?? "").trim();
  if (!raw) return null;
  const when = new Date(raw);
  if (Number.isNaN(when.getTime())) return null;
  const base: Intl.DateTimeFormatOptions = { dateStyle: "short", timeStyle: "short" };
  let formatted: string;
  try {
    formatted = new Intl.DateTimeFormat("fa-IR", zone ? { ...base, timeZone: zone } : base).format(when);
  } catch {
    formatted = new Intl.DateTimeFormat("fa-IR", base).format(when);
  }
  return zone ? `${formatted} (${zone})` : formatted;
}

function quantityText(value: number): string {
  return value.toLocaleString("fa-IR", { maximumFractionDigits: 3 });
}

/** Dialog review rows: exact remaining quantities + existing parcels that MAY be reused. */
function buildDialogRows(detail: AdminOrderDetail, group: AdminOperationalDeliveryGroup): OperationalGroupDialogRow[] {
  return group.members.map((member) => {
    const seller = resolveSeller(detail, member.sellerOrderId);
    const parcels = seller ? memberShipments(seller, member.shippingGroupId) : [];
    const reusable = parcels.filter(
      (parcel) =>
        parcel.status === "Created"
        && !isPackageLocked(parcel)
        && Boolean(parcel.shippingMethodCode),
    );
    const lines = member.lines && member.lines.length > 0
      ? member.lines
          .filter((line) => line.remainingPackedQuantity > 0)
          .map((line) => ({
            orderLineId: line.orderLineId,
            title: line.title,
            remaining: line.remainingPackedQuantity,
          }))
      : groupItems(detail, member)
          .map((line) => ({
            orderLineId: lineKey(line),
            title: line.title,
            remaining: remainingPacked(line),
          }))
          .filter((line) => line.remaining > 0);
    return {
      memberShippingGroupId: member.shippingGroupId,
      sellerDisplayName: member.sellerDisplayName,
      lines,
      parcels: reusable,
    };
  });
}

/**
 * TB-254-R1: titles of EVERY line of the group, regardless of remaining packed quantity. A retained
 * request must stay readable after partial server progress consumed the remaining quantity.
 */
function groupLineTitles(detail: AdminOrderDetail, group: AdminOperationalDeliveryGroup): Record<string, string> {
  const titles: Record<string, string> = {};
  for (const member of group.members) {
    if (member.lines && member.lines.length > 0) {
      for (const line of member.lines) titles[line.orderLineId] = line.title;
      continue;
    }
    for (const line of groupItems(detail, member)) titles[lineKey(line)] = line.title;
  }
  return titles;
}

/**
 * TB-253/TB-254: «ارسال سفارش» — one card per server-owned operational delivery group.
 *
 * This component is DISPLAY + contextual actions only. It never regroups client-side and it never
 * shows a selection form on the normal page: quantities and parcel reuse live inside the one
 * purposeful Marketplace group dialog (`AdminOperationalGroupWorkflowDialog`).
 */
export function AdminOrderOperationalDeliverySection({
  detail,
  pendingCode,
  onCreateShipment,
  onPrepareOperationalGroup,
  occurrenceScopeKey,
  packingModes,
  parcelCapabilities,
  onParcelTrack,
  onParcelOperation,
  onParcelException,
  onFactsChanged,
}: Props) {
  const groups = detail.operationalDeliveryGroups ?? [];
  if (groups.length === 0) {
    return (
      <div
        className="rounded-xl border border-amber-100 bg-amber-50/60 px-3 py-2.5 text-xs text-amber-900"
        data-testid="admin-order-operational-delivery-empty"
        dir="rtl"
      >
        اطلاعات گروه ارسال این سفارش در دسترس نیست. وضعیت سفارش را دوباره بارگذاری کنید.
      </div>
    );
  }

  const parcelCount = groups.reduce(
    (sum, group) =>
      sum
      + group.members.reduce((memberSum, member) => {
        const seller = resolveSeller(detail, member.sellerOrderId);
        return memberSum + (seller ? memberShipments(seller, member.shippingGroupId).length : 0);
      }, 0),
    0,
  );

  return (
    <section className="space-y-3" data-testid="admin-order-operational-delivery" dir="rtl">
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <h3 className="text-base font-black text-gray-900">ارسال سفارش</h3>
        <p className="text-xs text-gray-500" data-testid="admin-order-operational-summary">
          {`${groups.length.toLocaleString("fa-IR")} گروه ارسال · ${parcelCount.toLocaleString("fa-IR")} مرسوله`}
        </p>
      </div>
      <div className="space-y-3">
        {groups.map((group) => (
          <OperationalCard
            key={group.operationalGroupKey}
            group={group}
            detail={detail}
            pendingCode={pendingCode}
            onCreateShipment={onCreateShipment}
            onPrepareOperationalGroup={onPrepareOperationalGroup}
            occurrenceScopeKey={occurrenceScopeKey}
            packingModes={packingModes}
            parcelCapabilities={parcelCapabilities}
            onParcelTrack={onParcelTrack}
            onParcelOperation={onParcelOperation}
            onParcelException={onParcelException}
            onFactsChanged={onFactsChanged}
          />
        ))}
      </div>
    </section>
  );
}

function OperationalCard({
  group,
  detail,
  pendingCode,
  onCreateShipment,
  onPrepareOperationalGroup,
  occurrenceScopeKey,
  packingModes,
  parcelCapabilities,
  onParcelTrack,
  onParcelOperation,
  onParcelException,
  onFactsChanged,
}: {
  group: AdminOperationalDeliveryGroup;
  detail: AdminOrderDetail;
  pendingCode: string | null;
  onCreateShipment: (target: OperationalCreateTarget) => void;
  onPrepareOperationalGroup?: Props["onPrepareOperationalGroup"];
  occurrenceScopeKey?: string | null;
  packingModes?: Props["packingModes"];
  parcelCapabilities?: Props["parcelCapabilities"];
  onParcelTrack?: Props["onParcelTrack"];
  onParcelOperation?: Props["onParcelOperation"];
  onParcelException?: Props["onParcelException"];
  onFactsChanged?: Props["onFactsChanged"];
}) {
  /** TB-253-R4: retained attempt of this checkout/group/scope; it survives remounts. */
  const [retainedAttempt, setRetainedAttempt] = useState<OperationalGroupAttempt | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [errorFa, setErrorFa] = useState<string | null>(null);
  const inFlightRef = useRef(false);

  useEffect(() => {
    // TB-253-R5: a viewer/scope change must drop the previous viewer's in-memory refs — a stale ref is
    // never authorization, and the retained envelope belongs to the viewer that recorded it.
    setErrorFa(null);
    setRetainedAttempt(
      readOperationalGroupAttempt(detail.checkoutId, group.operationalGroupKey, occurrenceScopeKey),
    );
  }, [detail.checkoutId, group.operationalGroupKey, occurrenceScopeKey]);

  const day = jalaliDay(group.deliveryDate);
  const timeWindow = (group.deliveryTimeWindow ?? "").trim();
  const plannedDispatch = group.plannedDispatch ?? null;
  const handoffDay = jalaliDay(plannedDispatch?.handoffDate ?? null);
  const deadlineZone = plannedDispatch?.handoffTimeZone ?? null;
  const preparationStartLabel = carrierDeadlineLabel(plannedDispatch?.preparationStartUtc, deadlineZone);
  const transferDepartureLabel = carrierDeadlineLabel(plannedDispatch?.transferDepartureUtc, deadlineZone);
  const handoffDeadlineLabel = carrierDeadlineLabel(plannedDispatch?.handoffDeadlineUtc, deadlineZone);
  const showFeeBreakdown = group.feeBreakdown.length > 1;
  const actorMayManage = group.actorMayManageOperations !== false;
  const multiMember = group.members.length > 1;
  const hasRetained = Boolean(retainedAttempt);
  const workflowBlocked = !group.consolidateEligible || !actorMayManage || !onPrepareOperationalGroup;
  const dialogRows = useMemo(() => buildDialogRows(detail, group), [detail, group]);
  const lineTitleMap = useMemo(() => groupLineTitles(detail, group), [detail, group]);
  const canRunWorkflow = Boolean(onPrepareOperationalGroup)
    && group.isCrossSellerMarketplace
    && group.consolidateEligible
    && actorMayManage;

  async function runWorkflow(request: OperationalGroupDialogRequest) {
    if (!onPrepareOperationalGroup) return;
    if (inFlightRef.current) return;
    inFlightRef.current = true;
    setErrorFa(null);
    try {
      const resolution = resolveOperationalGroupAttempt({
        checkoutId: detail.checkoutId,
        operationalGroupKey: group.operationalGroupKey,
        scopeKey: occurrenceScopeKey,
        request: {
          members: group.members.map((member) => member.shippingGroupId),
          selections: request.selections,
          reuseShipmentIds: request.reuseShipmentIds,
          packingMode: request.packingMode,
        },
      });
      if (resolution.blockedByRetainedAttempt) {
        // R5: the retained envelope is unresolved. Nothing is minted or overwritten, and no request
        // is sent from a corrected review. Only the recorded operation may proceed.
        setRetainedAttempt(resolution.attempt);
        setErrorFa("درخواست ثبت‌شدهٔ قبلی هنوز تعیین تکلیف نشده است؛ همان عملیات را پیگیری کنید.");
        return;
      }
      if (!resolution.durable) {
        // TB-259: a NEW operation may only be sent when its complete reviewed envelope (including the
        // packing choice) is retained for reload/remount. Otherwise a lost response would be
        // indistinguishable from a fresh operation and could allocate a second parcel set.
        setErrorFa(
          "ذخیرهٔ مطمئن این درخواست در این مرورگر ممکن نشد؛ برای جلوگیری از ارسال تکراری، درخواست ارسال نشد. ذخیره‌سازی مرورگر را بررسی و دوباره تلاش کنید.",
        );
        return;
      }
      setRetainedAttempt(resolution.attempt);
      const outcome = await onPrepareOperationalGroup(group, {
        operationalGroupShippingGroupIds: resolution.attempt.members,
        selections: resolution.attempt.selections,
        reuseShipmentIds: resolution.attempt.reuseShipmentIds,
        operationalGroupOccurrenceId: resolution.occurrenceId,
        packingMode: resolution.attempt.packingMode,
      });
      if (outcomeCompleted(outcome)) {
        // Completed: the reviewed envelope is done, so the next deliberate run is a NEW one.
        setRetainedAttempt(null);
        setErrorFa(null);
        setDialogOpen(false);
        clearOperationalGroupAttempt(detail.checkoutId, group.operationalGroupKey, occurrenceScopeKey);
        return;
      }
      // TB-253-R6: failure / 5xx / lost response / cancellation / ANY typed refusal keeps the exact
      // envelope. Only an authoritative completed result clears it.
      markOperationalGroupAttemptFailed({ attempt: resolution.attempt });
      setRetainedAttempt(resolution.attempt);
      setErrorFa(
        outcomeError(outcome)
          ?? "نتیجهٔ قطعی این عملیات دریافت نشد؛ همان درخواست ثبت‌شده برای پیگیری نگه داشته شد.",
      );
    } finally {
      inFlightRef.current = false;
    }
  }

  const pending = pendingCode !== null;

  return (
    <article
      className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm"
      data-testid={`admin-order-operational-group-${group.operationalGroupKey}`}
      data-cross-seller={group.isCrossSellerMarketplace ? "true" : "false"}
      dir="rtl"
    >
      <header className="flex flex-wrap items-start justify-between gap-3 border-b border-gray-100 bg-gray-50/60 px-3 py-2.5 md:px-4">
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <h4 className="text-sm font-black text-gray-900">{group.methodLabel || "روش ارسال ثبت نشده"}</h4>
            <span className="rounded-full bg-white px-2 py-0.5 text-[11px] font-bold text-gray-600 ring-1 ring-gray-200">
              {group.isCrossSellerMarketplace ? "ارسال مشترک مارکت‌پلیس" : "ارسال مستقل"}
            </span>
          </div>
          <OperationalDeliveryTimeline
            groupKey={group.operationalGroupKey}
            deliveryDateLabel={day}
            deliveryTimeWindow={timeWindow}
            responsibilityLabelFa={group.responsibilityLabelFa}
            plannedDispatch={plannedDispatch}
            handoffDateLabel={handoffDay}
            preparationStartLabel={preparationStartLabel}
            transferDepartureLabel={transferDepartureLabel}
            handoffDeadlineLabel={handoffDeadlineLabel}
          />
          <p className="mt-1 text-xs text-gray-500">
            {multiMember
              ? `${group.members.length.toLocaleString("fa-IR")} فروشنده در این گروه`
              : group.members[0]?.sellerDisplayName ?? "فروشنده نامشخص"}
          </p>
        </div>
        <div className="text-start">
          <p className="text-sm font-bold tabular-nums text-gray-900">{formatAdminMoney(group.combinedFeeTotal, group.currency)}</p>
          <p className="text-[11px] text-gray-500">{showFeeBreakdown ? "جمع هزینهٔ ارسال گروه" : "هزینهٔ ارسال"}</p>
        </div>
      </header>

      <div className="space-y-3 px-3 py-3 md:px-4">
        {showFeeBreakdown ? (
          <ul
            className="space-y-0.5 rounded-lg border border-gray-100 bg-gray-50/60 px-2.5 py-1.5 text-xs text-gray-600"
            data-testid={`admin-order-operational-fee-breakdown-${group.operationalGroupKey}`}
          >
            {group.feeBreakdown.map((fee) => (
              <li key={fee.shippingGroupId} className="flex items-center justify-between gap-2">
                <span className="min-w-0 break-words pe-2">{fee.sellerDisplayName}</span>
                <span className="shrink-0 tabular-nums">{formatAdminMoney(fee.feeAmount, fee.currency)}</span>
              </li>
            ))}
          </ul>
        ) : null}

        <div className="grid gap-3 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
          <GroupItems detail={detail} group={group} multiMember={multiMember} />
          <GroupParcels
            detail={detail}
            group={group}
            multiMember={multiMember}
            pending={pending}
            actorMayManage={actorMayManage}
            viewerScopeKey={occurrenceScopeKey}
            parcelCapabilities={parcelCapabilities}
            onParcelTrack={onParcelTrack}
            onParcelOperation={onParcelOperation}
            onParcelException={onParcelException}
            onFactsChanged={onFactsChanged}
            onCreateShipment={onCreateShipment}
          />
        </div>

        {group.separateReasonFa ? (
          <p className="text-xs text-gray-600" role="status">
            {group.separateReasonFa}
          </p>
        ) : null}

        {canRunWorkflow && !hasRetained ? (
          <div className="flex flex-wrap items-center justify-between gap-2 border-t border-gray-100 pt-3">
            <p className="text-xs text-gray-500">
              {dialogRows.some((row) => row.parcels.length > 0) || dialogRows.some((row) => row.lines.length > 0)
                ? "مرسوله‌های این گروه را یک‌جا آماده و در یک بستهٔ مشترک ارسال کنید."
                : "برای این گروه مرسوله یا کالای قابل ارسالی باقی نمانده است."}
            </p>
            <button
              type="button"
              disabled={pending}
              onClick={() => {
                setErrorFa(null);
                setDialogOpen(true);
              }}
              className="min-h-11 rounded-lg bg-[#2563EB] px-4 text-sm font-bold text-white disabled:opacity-50"
              data-testid={`admin-order-operational-prepare-${group.operationalGroupKey}`}
            >
              {group.consolidateEligible && dialogRows.every((row) => row.lines.length === 0) && dialogRows.some((row) => row.parcels.length > 0)
                ? "تجمیع مرسوله‌ها"
                : "آماده‌سازی ارسال گروه"}
            </button>
          </div>
        ) : null}

        {hasRetained && retainedAttempt && actorMayManage && onPrepareOperationalGroup ? (
          <div
            className="flex flex-wrap items-center justify-between gap-2 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2"
            data-testid={`admin-order-operational-reconcile-${group.operationalGroupKey}`}
          >
            <div className="min-w-0">
              <p className="text-xs font-bold text-amber-900">نتیجهٔ عملیات ثبت‌شده هنوز مشخص نیست.</p>
              <p className="mt-0.5 text-[11px] text-amber-800">
                {`${retainedAttempt.selections.length.toLocaleString("fa-IR")} کالا · ${retainedAttempt.reuseShipmentIds.length.toLocaleString("fa-IR")} مرسولهٔ موجود · همین درخواست تکرار می‌شود و مرسولهٔ تازه ساخته نمی‌شود.`}
              </p>
            </div>
            <button
              type="button"
              disabled={pending}
              onClick={() => {
                setErrorFa(null);
                setDialogOpen(true);
              }}
              className="min-h-11 rounded-lg border border-amber-300 bg-white px-3 text-xs font-bold text-amber-900 disabled:opacity-50"
              data-testid={`admin-order-operational-retained-${group.operationalGroupKey}`}
            >
              پیگیری نتیجه
            </button>
          </div>
        ) : null}

        {group.isCrossSellerMarketplace && !group.consolidateEligible && group.consolidateBlockReasonFa && !hasRetained ? (
          <p className="text-xs text-gray-600" data-testid={`admin-order-operational-consolidate-note-${group.operationalGroupKey}`}>
            {group.consolidateBlockReasonFa}
          </p>
        ) : null}

        {group.isCrossSellerMarketplace && !actorMayManage ? (
          <p className="text-xs text-gray-600" data-testid={`admin-order-operational-no-authority-${group.operationalGroupKey}`}>
            مجوز عملیات ارسال مشترک برای این کاربر فعال نیست.
          </p>
        ) : null}
      </div>

      {canRunWorkflow || hasRetained ? (
        <AdminOperationalGroupWorkflowDialog
          open={dialogOpen}
          group={group}
          rows={dialogRows}
          pending={pending}
          errorFa={errorFa}
          retainedAttempt={retainedAttempt}
          lineTitles={lineTitleMap}
          allowedPackingModes={packingModes ?? undefined}
          formatMoney={formatAdminMoney}
          formatStatus={formatAdminStatus}
          onClose={() => setDialogOpen(false)}
          onSubmit={(request) => {
            void runWorkflow(request);
          }}
        />
      ) : null}
    </article>
  );
}

function GroupItems({
  detail,
  group,
  multiMember,
}: {
  detail: AdminOrderDetail;
  group: AdminOperationalDeliveryGroup;
  multiMember: boolean;
}) {
  const blocks = group.members.map((member) => ({
    member,
    items: groupItems(detail, member),
  }));
  const totalItems = blocks.reduce((sum, block) => sum + block.items.length, 0);

  return (
    <section className="min-w-0" data-testid={`admin-order-operational-items-${group.operationalGroupKey}`}>
      <h5 className="mb-1.5 flex items-center gap-1.5 text-xs font-bold text-gray-700">
        <Package className="size-3.5 text-gray-400" aria-hidden />
        کالاهای این گروه
      </h5>
      {totalItems === 0 ? (
        <p className="rounded-lg border border-dashed border-gray-200 px-3 py-2.5 text-xs text-gray-500">
          اقلام این گروه در دسترس نیست.
        </p>
      ) : (
        <div className="space-y-2">
          {blocks.map(({ member, items }) =>
            items.length === 0 ? null : (
              <div key={member.shippingGroupId}>
                {multiMember ? (
                  <p className="mb-1 text-xs font-semibold text-gray-500">{member.sellerDisplayName}</p>
                ) : null}
                <ul
                  className="divide-y divide-gray-100 rounded-lg border border-gray-100"
                  data-testid={`admin-order-operational-member-${member.shippingGroupId}`}
                >
                  {items.map((line) => (
                    <li
                      key={lineKey(line)}
                      className="flex items-start justify-between gap-2 px-3 py-2"
                      data-testid={`admin-order-operational-item-${lineKey(line)}`}
                    >
                      <span className="min-w-0 flex-1 break-words text-sm leading-6 text-gray-800">{line.title}</span>
                      <span className="shrink-0 text-xs tabular-nums text-gray-600">
                        {`${quantityText(line.quantity)} عدد`}
                      </span>
                      <span className="shrink-0 rounded-full bg-gray-100 px-2 py-0.5 text-[11px] font-bold text-gray-600">
                        {formatAdminShipmentStatus(line.operationalStatus ?? "NotApplicable")}
                      </span>
                    </li>
                  ))}
                </ul>
              </div>
            ),
          )}
        </div>
      )}
    </section>
  );
}

/**
 * TB-262-R1: a durable damaged decision that still OWES its replacement parcel. The server owns this fact, so the
 * UI never guesses it from leftover quantities (the outstanding damaged quantity is already consumed by the decision).
 */
function isPendingDamageResolution(resolution: AdminDamagedResolution): boolean {
  return resolution.outcome === "Replacement"
    && (resolution.status === "Recorded" || resolution.status === "Reserved")
    && !resolution.replacementShipmentId;
}

/** TB-262-R1: durable, non-transient state sentence of one damaged decision (never a success banner). */
function damagedDecisionText(resolution: AdminDamagedResolution): string {
  if (resolution.outcome === "Referral") {
    return "ارجاع کالای آسیب‌دیده ثبت شد — در انتظار بررسی خدمات مشتری.";
  }

  if (resolution.replacementShipmentId) {
    return "جایگزینی از موجودی سالم ثبت شد و مرسولهٔ جایگزین ساخته شده است.";
  }

  if (isPendingDamageResolution(resolution)) {
    return resolution.status === "Reserved"
      ? "جایگزینی ثبت شد و موجودی سالم برای همین سفارش نگه داشته شده؛ مرسولهٔ جایگزین هنوز ساخته نشده است."
      : "جایگزینی ثبت شد؛ موجودی سالم هنوز تأمین نشده و مرسولهٔ جایگزین ساخته نشده است.";
  }

  if (resolution.status === "Cancelled") {
    return "تصمیم جایگزینی این قلم پیش از ارسال لغو شده است.";
  }

  return "";
}

/**
 * TB-263: the retained recovery entry states the TRUE last outcome. A durably recorded pending decision is
 * never described as a lost response, and a deterministic refusal is never described as uncertain.
 */
function retainedAttemptNoteFa(attempt: PostDispatchAttempt): string {
  if (attempt.lastOutcomeKind === "pending") {
    return attempt.lastMessageFa?.trim()
      || "تصمیم ثبت شد و در انتظار تکمیل است؛ همان درخواست ادامه می‌یابد.";
  }
  if (attempt.lastOutcomeKind === "refused") {
    return attempt.lastMessageFa?.trim() || "این درخواست پذیرفته نشد؛ دلیل آن در همان پنجره آمده است.";
  }
  return "پاسخ قطعی این درخواست ثبت نشده است؛ همان درخواست قبلی حفظ شده و فقط همان ارسال می‌شود.";
}

/** The modes the fresh projection still offers as ordinary buttons. */
function permittedExceptionModes(caps: OperationalParcelCapabilities): PostDispatchExceptionMode[] {  const modes: PostDispatchExceptionMode[] = [];
  if (caps.canRecordNonDelivery) modes.push("record_non_delivery");
  if (caps.canStartReturn) modes.push("start_return");
  if (caps.canRecordReturnReceipt) modes.push("record_return_receipt");
  if (caps.canResend) modes.push("resend_shipment");
  if (caps.canResolveDamagedGoods) modes.push("resolve_damaged_goods");
  if (caps.canCreateCourierReturn) modes.push("create_courier_return");
  return modes;
}

function GroupParcels({
  detail,
  group,
  multiMember,
  pending,
  actorMayManage,
  viewerScopeKey,
  parcelCapabilities,
  onParcelTrack,
  onParcelOperation,
  onParcelException,
  onFactsChanged,
  onCreateShipment,
}: {
  detail: AdminOrderDetail;
  group: AdminOperationalDeliveryGroup;
  multiMember: boolean;
  pending: boolean;
  actorMayManage: boolean;
  /** TB-258-R1: viewer/owner scope of the retained post-dispatch request. */
  viewerScopeKey?: string | null;
  parcelCapabilities?: Props["parcelCapabilities"];
  onParcelTrack?: Props["onParcelTrack"];
  onParcelOperation?: Props["onParcelOperation"];
  onParcelException?: Props["onParcelException"];
  onFactsChanged?: Props["onFactsChanged"];
  onCreateShipment: (target: OperationalCreateTarget) => void;
}) {
  /** TB-258: the ONE open post-dispatch review; it is bound to one parcel and one mode at a time. */
  const [exception, setException] = useState<{
    seller: AdminSellerOrder;
    shipment: AdminShipment;
    mode: PostDispatchExceptionMode;
  } | null>(null);
  /**
   * TB-263: the ONE blocking feedback surface. A refusal that ends the review (authority, ownership, a
   * conflict with recorded state) is acknowledged in the existing MessageBox — never stacked over the
   * review dialog and never duplicated by a toast or an inline paragraph.
   */
  const [blockingMessageFa, setBlockingMessageFa] = useState<string | null>(null);
  const [exceptionPending, setExceptionPending] = useState(false);
  const exceptionSubmitInFlight = useRef(false);

  const exceptionLines = useMemo<PostDispatchParcelLine[]>(() => {
    if (!exception) return [];
    const sellerLines = new Map(exception.seller.lines.map((line) => [lineKey(line), line]));
    return exception.shipment.lines.map((line) => {
      const source = sellerLines.get(line.orderLineId);
      return {
        orderLineId: line.orderLineId,
        title: source?.title ?? "قلم سفارش",
        parcelQuantity: line.quantity,
        resendableQuantity: Math.max(0, source?.resendableQuantity ?? 0),
        returnedUsableQuantity: Math.max(0, source?.returnedUsableQuantity ?? 0),
        returnedDamagedQuantity: Math.max(
          0,
          (exception.shipment.returnReceiptLines ?? [])
            .filter((r) => r.orderLineId === line.orderLineId)
            .filter((r) => r.condition === "Damaged" || r.condition === "1")
            .reduce((sum, r) => sum + r.quantity, 0),
        ),
        // TB-262: this parcel's OWN remaining damaged units; a sibling parcel can never lend or borrow.
        damagedOutstandingQuantity: Math.max(0, line.outstandingDamagedQuantity ?? 0),
      };
    });
  }, [exception]);

  async function submitException(request: PostDispatchExceptionRequest): Promise<PostDispatchSubmitOutcome> {
    if (!exception || !onParcelException) return normalizePostDispatchOutcome(false);
    // Synchronous single-flight: a second same-tick press can never send a second mutation.
    if (exceptionSubmitInFlight.current) return normalizePostDispatchOutcome(false);
    exceptionSubmitInFlight.current = true;
    setExceptionPending(true);
    try {
      const outcome = normalizePostDispatchOutcome(
        await onParcelException(exception.seller, exception.shipment.shipmentId, request),
      );
      if (outcome.kind === "completed") {
        setException(null);
        return outcome;
      }
      if (outcome.kind === "refused" && outcome.blocking) {
        // The review cannot proceed: close it (the exact envelope stays retained and replayable) and put
        // the ONE exact reason on the existing blocking surface.
        setException(null);
        setBlockingMessageFa(outcome.messageFa);
        onFactsChanged?.();
        return outcome;
      }
      // `pending` and inline `refused` keep the review open with one truthful inline surface; `unknown`
      // keeps it open with the retained-envelope notice. The dialog owns that single surface.
      return outcome;
    } finally {
      exceptionSubmitInFlight.current = false;
      setExceptionPending(false);
    }
  }

  function openException(
    seller: AdminSellerOrder,
    shipment: AdminShipment,
    mode: PostDispatchExceptionMode,
  ) {
    setException({ seller, shipment, mode });
  }

  /**
   * TB-258-R2: the ONE retained review still owed for this parcel, when the fresh projection no longer
   * offers its action. The store re-checks viewer/checkout/fulfillment/parcel/mode, so another viewer's
   * or another parcel's envelope can never surface here. This is a RECOVERY entry, never authorization:
   * the server still validates the actor and the exact recorded intent.
   */
  function retainedReviewFor(
    seller: AdminSellerOrder,
    shipment: AdminShipment,
    projected: OperationalParcelCapabilities,
  ): PostDispatchAttempt | null {
    const permitted = permittedExceptionModes(projected);
    const retained = listPostDispatchAttemptsForParcel({
      viewerScopeKey: viewerScopeKey ?? null,
      checkoutId: detail.checkoutId,
      fulfillmentId: seller.fulfillmentId ?? seller.id,
      sourceShipmentId: shipment.shipmentId,
    });
    return retained.find((attempt) => !permitted.includes(attempt.mode)) ?? null;
  }

  const rows = group.members.flatMap((member) => {
    const seller = resolveSeller(detail, member.sellerOrderId);
    if (!seller) return [];
    return memberShipments(seller, member.shippingGroupId).map((shipment) => ({ member, seller, shipment }));
  });

  /**
   * TB-261: a returned parcel's usable capacity can already be committed to ITS OWN resend parcel. Naming
   * that existing parcel is the honest answer to «چرا ارسال مجدد نیست؟» — and it forbids a duplicate.
   */
  const resendByOriginalId = new Map<string, AdminShipment>();
  for (const row of rows) {
    const originalId = row.shipment.resendOfShipmentId;
    if (originalId) resendByOriginalId.set(originalId, row.shipment);
  }

  const standalone = rows.filter((row) => !isPackageLocked(row.shipment));
  const locked = rows.filter((row) => isPackageLocked(row.shipment));
  const lockedPackages = new Map<string, number>();
  for (const row of locked) {
    const key = row.shipment.activePackageNumber || "بستهٔ مشترک";
    lockedPackages.set(key, (lockedPackages.get(key) ?? 0) + 1);
  }

  const standaloneMembers = group.members.filter((member) => {
    const seller = resolveSeller(detail, member.sellerOrderId);
    const accepted = resolveAcceptedGroup(detail, member.shippingGroupId);
    return Boolean(seller)
      && Boolean(accepted)
      && member.createParcelEligible
      && member.transportMode !== "CarrierApi"
      && actorMayManage
      && !multiMember;
  });

  return (
    <section className="min-w-0" data-testid={`admin-order-operational-parcels-${group.operationalGroupKey}`}>
      <div className="mb-1.5 flex flex-wrap items-center justify-between gap-2">
        <h5 className="flex items-center gap-1.5 text-xs font-bold text-gray-700">
          <Truck className="size-3.5 text-gray-400" aria-hidden />
          مرسوله‌ها
        </h5>
        {standaloneMembers.length > 0 ? (
          <button
            type="button"
            disabled={pending}
            onClick={() => {
              const member = standaloneMembers[0]!;
              const seller = resolveSeller(detail, member.sellerOrderId);
              const accepted = resolveAcceptedGroup(detail, member.shippingGroupId);
              if (!seller || !accepted) return;
              onCreateShipment({ seller, group: accepted });
            }}
            className="min-h-11 rounded-lg border border-gray-200 bg-white px-3 text-xs font-bold text-gray-700 disabled:opacity-50"
            data-testid={`admin-order-operational-create-${standaloneMembers[0]!.shippingGroupId}`}
          >
            ایجاد مرسوله
          </button>
        ) : null}
      </div>

      {rows.length === 0 ? (
        <p
          className="rounded-lg border border-dashed border-gray-200 px-3 py-2.5 text-xs text-gray-500"
          data-testid={`admin-order-operational-parcels-empty-${group.operationalGroupKey}`}
        >
          هنوز مرسوله‌ای برای این گروه ساخته نشده است.
        </p>
      ) : (
        <ul className="space-y-2">
          {standalone.map((row, index) => {
            const parcel = row.shipment;
            const cancelled = isCancelledShipment(parcel.status);
            const projectedCaps = cancelled
              ? NO_PARCEL_ACTIONS
              : parcelCapabilities?.(row.seller, parcel) ?? NO_PARCEL_ACTIONS;
            const caps = cancelled || pending ? NO_PARCEL_ACTIONS : projectedCaps;
            // TB-258-R2: a retained reviewed request keeps ONE recovery entry even after the projection
            // stopped offering the action (refresh, state transition, exhausted capacity).
            const retainedReview = onParcelException && !cancelled
              ? retainedReviewFor(row.seller, parcel, projectedCaps)
              : null;
            // TB-261: the honest physical facts of THIS parcel's own receipt and ITS own resend.
            const receipt = receiptTotals(parcel);
            const resendChild = resendByOriginalId.get(parcel.shipmentId) ?? null;
            // TB-262-R1: the server-owned durable damaged decisions of this parcel. A recorded decision that still
            // owes its replacement parcel stays resumable even after the outstanding quantity was consumed by it.
            const damagedDecisions = parcel.damagedResolutions ?? [];
            const pendingDamage = damagedDecisions.find(isPendingDamageResolution) ?? null;
            return (
              <li
                key={parcel.shipmentId}
                className={
                  cancelled
                    ? "rounded-lg border border-red-100 bg-red-50/40 px-3 py-2"
                    : "rounded-lg border border-gray-200 bg-white px-3 py-2"
                }
                data-testid={`admin-order-operational-parcel-${parcel.shipmentId}`}
              >
                <div className="flex flex-wrap items-start justify-between gap-2">
                  <div className="min-w-0">
                    <p className="text-sm font-bold text-gray-900">
                      {`مرسولهٔ ${(index + 1).toLocaleString("fa-IR")}`}
                      {multiMember ? (
                        <span className="ms-2 text-xs font-normal text-gray-500">{row.member.sellerDisplayName}</span>
                      ) : null}
                    </p>
                    <p className="mt-0.5 text-xs text-gray-500">
                      {parcel.carrierDisplayName}
                      {" · "}
                      {`${parcel.itemCount.toLocaleString("fa-IR")} قلم`}
                    </p>
                  </div>
                  <span
                    className={
                      cancelled
                        ? "shrink-0 rounded-full bg-red-100 px-2 py-0.5 text-[11px] font-bold text-red-700"
                        : isCarrierException(parcel.status)
                          ? "shrink-0 rounded-full bg-amber-100 px-2 py-0.5 text-[11px] font-bold text-amber-900"
                          : "shrink-0 rounded-full bg-blue-50 px-2 py-0.5 text-[11px] font-bold text-blue-700"
                    }
                  >
                    {formatAdminShipmentStatus(parcel.status)}
                  </span>
                </div>
                <p className="mt-1 text-xs text-gray-600">
                  {parcel.trackingReference ? (
                    <>
                      <span className="text-gray-500">کد رهگیری: </span>
                      <bdi dir="ltr" className="font-semibold">
                        {parcel.trackingReference}
                      </bdi>
                    </>
                  ) : (
                    <span className="text-gray-500">کد رهگیری ثبت نشده است.</span>
                  )}
                </p>
                {caps.canTrack || caps.canDispatch || caps.canDeliver || caps.canCancel ? (
                  <div className="mt-2 flex flex-wrap gap-1.5">
                    {caps.canTrack ? (
                      <button
                        type="button"
                        disabled={pending}
                        onClick={() => onParcelTrack?.(row.seller, parcel)}
                        className="min-h-11 rounded-lg border border-gray-200 px-3 text-xs font-bold text-gray-700 disabled:opacity-50"
                        data-testid={`admin-order-shipment-assign-tracking-${parcel.shipmentId}`}
                      >
                        ثبت کد رهگیری
                      </button>
                    ) : null}
                    {caps.canDispatch ? (
                      <button
                        type="button"
                        disabled={pending}
                        onClick={() => onParcelOperation?.(row.seller, "dispatch_shipment", parcel.shipmentId)}
                        className="min-h-11 rounded-lg border border-gray-200 px-3 text-xs font-bold text-gray-700 disabled:opacity-50"
                        data-testid={`admin-order-shipment-dispatch-${parcel.shipmentId}`}
                      >
                        ارسال
                      </button>
                    ) : null}
                    {caps.canDeliver ? (
                      <button
                        type="button"
                        disabled={pending}
                        onClick={() => onParcelOperation?.(row.seller, "deliver_shipment", parcel.shipmentId)}
                        className="min-h-11 rounded-lg border border-gray-200 px-3 text-xs font-bold text-gray-700 disabled:opacity-50"
                        data-testid={`admin-order-shipment-deliver-${parcel.shipmentId}`}
                      >
                        ثبت تحویل
                      </button>
                    ) : null}
                    {caps.canCancel ? (
                      <button
                        type="button"
                        disabled={pending}
                        onClick={() => onParcelOperation?.(row.seller, "cancel_shipment", parcel.shipmentId)}
                        className="min-h-11 rounded-lg border border-red-200 px-3 text-xs font-bold text-red-700 disabled:opacity-50"
                        data-testid={`admin-order-shipment-cancel-${parcel.shipmentId}`}
                      >
                        ابطال مرسوله
                      </button>
                    ) : null}
                  </div>
                ) : null}
                {onParcelException && (
                  caps.canRecordNonDelivery
                  || caps.canStartReturn
                  || caps.canRecordReturnReceipt
                  || caps.canResend
                  || caps.canResolveDamagedGoods
                  || caps.canCreateCourierReturn
                ) ? (
                  <div
                    className="mt-2 flex flex-wrap gap-1.5 border-t border-gray-100 pt-2"
                    data-testid={`admin-order-post-dispatch-actions-${parcel.shipmentId}`}
                  >
                    {caps.canRecordNonDelivery ? (
                      <button
                        type="button"
                        disabled={pending || exceptionPending}
                        onClick={() => openException(row.seller, parcel, "record_non_delivery")}
                        className="min-h-11 rounded-lg border border-amber-300 px-3 text-xs font-bold text-amber-900 disabled:opacity-50"
                        data-testid={`admin-order-post-dispatch-nondelivery-${parcel.shipmentId}`}
                      >
                        ثبت عدم تحویل
                      </button>
                    ) : null}
                    {caps.canStartReturn ? (
                      <button
                        type="button"
                        disabled={pending || exceptionPending}
                        onClick={() => openException(row.seller, parcel, "start_return")}
                        className="min-h-11 rounded-lg border border-gray-200 px-3 text-xs font-bold text-gray-700 disabled:opacity-50"
                        data-testid={`admin-order-post-dispatch-start-return-${parcel.shipmentId}`}
                      >
                        شروع برگشت به مبدأ
                      </button>
                    ) : null}
                    {caps.canRecordReturnReceipt ? (
                      <button
                        type="button"
                        disabled={pending || exceptionPending}
                        onClick={() => openException(row.seller, parcel, "record_return_receipt")}
                        className="min-h-11 rounded-lg border border-gray-200 px-3 text-xs font-bold text-gray-700 disabled:opacity-50"
                        data-testid={`admin-order-post-dispatch-receipt-${parcel.shipmentId}`}
                      >
                        تأیید دریافت در مبدأ
                      </button>
                    ) : null}
                    {caps.canResend ? (
                      <button
                        type="button"
                        disabled={pending || exceptionPending}
                        onClick={() => openException(row.seller, parcel, "resend_shipment")}
                        className="min-h-11 rounded-lg bg-[#2563EB] px-3 text-xs font-bold text-white disabled:opacity-50"
                        data-testid={`admin-order-post-dispatch-resend-${parcel.shipmentId}`}
                      >
                        ارسال مجدد
                      </button>
                    ) : null}
                    {caps.canResolveDamagedGoods ? (
                      <button
                        type="button"
                        disabled={pending || exceptionPending}
                        onClick={() => openException(row.seller, parcel, "resolve_damaged_goods")}
                        className="min-h-11 rounded-lg border border-amber-300 px-3 text-xs font-bold text-amber-900 disabled:opacity-50"
                        data-testid={`admin-order-post-dispatch-damaged-action-${parcel.shipmentId}`}
                      >
                        {pendingDamage && receipt.outstanding === 0
                          ? "ادامهٔ تعیین تکلیف کالای آسیب‌دیده"
                          : "تعیین تکلیف کالای آسیب‌دیده"}
                      </button>
                    ) : null}
                    {caps.canCreateCourierReturn ? (
                      <button
                        type="button"
                        disabled={pending || exceptionPending}
                        onClick={() => openException(row.seller, parcel, "create_courier_return")}
                        className="min-h-11 rounded-lg border border-gray-200 px-3 text-xs font-bold text-gray-700 disabled:opacity-50"
                        data-testid={`admin-order-post-dispatch-courier-return-${parcel.shipmentId}`}
                      >
                        ایجاد پروندهٔ مرجوعی
                      </button>
                    ) : null}
                  </div>
                ) : null}
                {retainedReview ? (
                  <div
                    className="mt-2 flex flex-wrap items-center gap-2 border-t border-amber-100 pt-2"
                    data-testid={`admin-order-post-dispatch-reconcile-row-${parcel.shipmentId}`}
                  >
                    <button
                      type="button"
                      disabled={exceptionPending}
                      onClick={() => openException(row.seller, parcel, retainedReview.mode)}
                      className="min-h-11 rounded-lg border border-amber-300 bg-amber-50 px-3 text-xs font-bold text-amber-900 disabled:opacity-50"
                      data-testid={`admin-order-post-dispatch-reconcile-${parcel.shipmentId}`}
                    >
                      {`پیگیری «${POST_DISPATCH_MODE_LABEL_FA[retainedReview.mode]}»`}
                    </button>
                    <span className="break-words text-[11px] text-amber-800">
                      {retainedAttemptNoteFa(retainedReview)}
                    </span>
                  </div>
                ) : null}
                {isCarrierException(parcel.status) ? (
                  <div
                    className="mt-2 space-y-1 rounded-lg border border-amber-100 bg-amber-50/60 px-2.5 py-1.5 text-[11px] text-amber-900"
                    data-testid={`admin-order-parcel-exception-${parcel.shipmentId}`}
                  >
                    {parcel.status === "Returned" ? (
                      <>
                        {receipt.outstanding > 0 ? (
                          <p data-testid={`admin-order-post-dispatch-damaged-${parcel.shipmentId}`}>
                            {`${quantityText(receipt.outstanding)} قلم آسیب‌دیده — نیازمند تعیین تکلیف`}
                          </p>
                        ) : null}
                        {damagedDecisions.map((decision) => {
                          const text = damagedDecisionText(decision);
                          return text ? (
                            <p
                              key={decision.damagedGoodsResolutionId}
                              data-testid={`admin-order-post-dispatch-damaged-decision-${decision.damagedGoodsResolutionId}`}
                            >
                              {text}
                            </p>
                          ) : null;
                        })}
                        {receipt.usable > 0 && !caps.canResend ? (
                          <p data-testid={`admin-order-post-dispatch-capacity-consumed-${parcel.shipmentId}`}>
                            {resendChild
                              ? `ظرفیت سالم دریافتی این مرسوله در مرسولهٔ ارسال مجدد ${parcelShortId(resendChild, resendChild.shipmentId)} مصرف شده است.`
                              : "ظرفیت سالم دریافتی این مرسوله در دسترس نیست؛ تا تعیین تکلیف، ارسال مجدد ممکن نیست."}
                          </p>
                        ) : null}
                        {resendChild && !caps.canResend ? (
                          <p className="flex flex-wrap items-center gap-1">
                            <span>پیگیری ارسال مجدد:</span>
                            <bdi dir="ltr" className="font-semibold">
                              {parcelShortId(resendChild, resendChild.shipmentId)}
                            </bdi>
                            <span>{`· ${formatAdminShipmentStatus(resendChild.status)}`}</span>
                          </p>
                        ) : null}
                        {receipt.usable === 0 && receipt.damaged === 0 && !resendChild ? (
                          <p>رسید فیزیکی این مرسوله بدون مقدار سالم یا آسیب‌دیده ثبت شده است.</p>
                        ) : null}
                      </>
                    ) : parcel.status === "NonDelivered" ? (
                      <p>عدم تحویل ثبت شده است؛ مرحلهٔ بعد شروع برگشت به مبدأ است.</p>
                    ) : (
                      <p>مرسوله در مسیر برگشت به مبدأ است؛ تأیید دریافت فیزیکی مرحلهٔ بعدی است.</p>
                    )}
                  </div>
                ) : null}
                {parcel.nonDeliveredAt || parcel.returnedAt || parcel.resendOfShipmentId ? (
                  <p className="mt-1 text-[11px] text-gray-500" data-testid={`admin-order-post-dispatch-facts-${parcel.shipmentId}`}>
                    {parcel.resendOfShipmentId ? "مرسولهٔ ارسال مجدد · " : ""}
                    {parcel.nonDeliveredAt ? "عدم تحویل ثبت شده" : ""}
                    {parcel.returnedAt ? " · دریافت در مبدأ تأیید شده" : ""}
                  </p>
                ) : null}
              </li>
            );
          })}
          {[...lockedPackages.entries()].map(([packageNumber, count]) => (
            <li
              key={`pkg:${packageNumber}`}
              className="rounded-lg border border-indigo-100 bg-indigo-50/40 px-3 py-2"
              data-testid={`admin-order-operational-package-lock-${packageNumber}`}
            >
              <p className="text-sm font-bold text-indigo-950">
                {packageNumber === "بستهٔ مشترک" ? packageNumber : `بستهٔ ارسال مشترک ${packageNumber}`}
              </p>
              <p className="mt-0.5 text-xs text-indigo-900">
                {`${count.toLocaleString("fa-IR")} مرسوله در این بسته · عملیات ارسال از بخش «بستهٔ ارسال مشترک» انجام می‌شود.`}
              </p>
            </li>
          ))}
        </ul>
      )}

      {exception ? (
        <PostDispatchExceptionDialog
          open
          mode={exception.mode}
          parcelLabel={exception.shipment.trackingReference || exception.shipment.shipmentId.slice(0, 8)}
          parcelKey={exception.shipment.shipmentId}
          attemptScope={{
            viewerScopeKey: viewerScopeKey ?? null,
            checkoutId: detail.checkoutId,
            fulfillmentId: exception.seller.fulfillmentId ?? exception.seller.id,
            sourceShipmentId: exception.shipment.shipmentId,
            mode: exception.mode,
          }}
          lines={exceptionLines}
          pending={exceptionPending || pending}
          errorFa={null}
          damagedResume={
            exception.mode === "resolve_damaged_goods"
              ? (() => {
                const pending = (exception.shipment.damagedResolutions ?? []).find(isPendingDamageResolution);
                return pending
                  ? {
                    orderLineId: pending.orderLineId,
                    quantity: pending.quantity,
                    outcome: pending.outcome === "Referral" ? "referral" : "replacement",
                    occurrenceKey: pending.occurrenceKey,
                  }
                  : null;
              })()
              : null
          }
          onClose={() => {
            setException(null);
          }}
          onSubmit={submitException}
        />
      ) : null}

      {/*
        TB-263: the existing blocking surface (design-system MessageBox). It is raised ONLY when the review
        was closed, so nothing is stacked over an open review and the same reason is never shown twice.
      */}
      <MessageBox
        open={blockingMessageFa !== null}
        title="این درخواست انجام نشد"
        text={blockingMessageFa ?? ""}
        testId="admin-order-post-dispatch-blocking"
        onClose={() => setBlockingMessageFa(null)}
      />
    </section>
  );
}
