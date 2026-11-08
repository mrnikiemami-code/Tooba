"use client";

import React, { useEffect, useMemo, useRef, useState } from "react";
import { CalendarDays, Clock, Package, Truck } from "lucide-react";
import {
  formatAdminMoney,
  formatAdminStatus,
  type AdminAcceptedShippingGroup,
  type AdminOperationalDeliveryGroup,
  type AdminOperationalDeliveryMember,
  type AdminOrderDetail,
  type AdminOrderLine,
  type AdminSellerOrder,
  type AdminShipment,
} from "./admin-api";
import { formatJalaliDate } from "../../design-system/app-data-grid/jalali";
import {
  clearOperationalGroupAttempt,
  markOperationalGroupAttemptFailed,
  readOperationalGroupAttempt,
  resolveOperationalGroupAttempt,
  type OperationalGroupAttempt,
} from "./operational-group-occurrence-store";
import {
  AdminOperationalGroupWorkflowDialog,
  type OperationalGroupDialogRequest,
  type OperationalGroupDialogRow,
} from "./admin-operational-group-workflow-dialog";

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
  /** TB-254: exactly one parcel/action owner — the group card shows the permitted next action. */
  parcelCapabilities?: (seller: AdminSellerOrder, shipment: AdminShipment) => OperationalParcelCapabilities;
  onParcelTrack?: (seller: AdminSellerOrder, shipment: AdminShipment) => void;
  onParcelOperation?: (seller: AdminSellerOrder, code: string, shipmentId: string) => void;
};

const NO_PARCEL_ACTIONS: OperationalParcelCapabilities = {
  canTrack: false,
  canDispatch: false,
  canDeliver: false,
  canCancel: false,
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
  parcelCapabilities,
  onParcelTrack,
  onParcelOperation,
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
            parcelCapabilities={parcelCapabilities}
            onParcelTrack={onParcelTrack}
            onParcelOperation={onParcelOperation}
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
  parcelCapabilities,
  onParcelTrack,
  onParcelOperation,
}: {
  group: AdminOperationalDeliveryGroup;
  detail: AdminOrderDetail;
  pendingCode: string | null;
  onCreateShipment: (target: OperationalCreateTarget) => void;
  onPrepareOperationalGroup?: Props["onPrepareOperationalGroup"];
  occurrenceScopeKey?: string | null;
  parcelCapabilities?: Props["parcelCapabilities"];
  onParcelTrack?: Props["onParcelTrack"];
  onParcelOperation?: Props["onParcelOperation"];
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
  const showFeeBreakdown = group.feeBreakdown.length > 1;
  const actorMayManage = group.actorMayManageOperations !== false;
  const multiMember = group.members.length > 1;
  const hasRetained = Boolean(retainedAttempt);
  const workflowBlocked = !group.consolidateEligible || !actorMayManage || !onPrepareOperationalGroup;
  const dialogRows = useMemo(() => buildDialogRows(detail, group), [detail, group]);
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
        },
      });
      if (resolution.blockedByRetainedAttempt) {
        // R5: the retained envelope is unresolved. Nothing is minted or overwritten, and no request
        // is sent from a corrected review. Only the recorded operation may proceed.
        setRetainedAttempt(resolution.attempt);
        setErrorFa("درخواست ثبت‌شدهٔ قبلی هنوز تعیین تکلیف نشده است؛ همان عملیات را پیگیری کنید.");
        return;
      }
      setRetainedAttempt(resolution.attempt);
      const outcome = await onPrepareOperationalGroup(group, {
        operationalGroupShippingGroupIds: resolution.attempt.members,
        selections: resolution.attempt.selections,
        reuseShipmentIds: resolution.attempt.reuseShipmentIds,
        operationalGroupOccurrenceId: resolution.occurrenceId,
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
          <p className="mt-1 flex flex-wrap items-center gap-x-2 gap-y-1 text-xs text-gray-600">
            <span className="inline-flex items-center gap-1">
              <CalendarDays className="size-3.5 shrink-0 text-gray-400" aria-hidden />
              {day ?? "تاریخ تحویل نامشخص"}
            </span>
            {timeWindow ? (
              <span className="inline-flex items-center gap-1">
                <Clock className="size-3.5 shrink-0 text-gray-400" aria-hidden />
                <bdi dir="ltr">{timeWindow}</bdi>
              </span>
            ) : null}
            {group.responsibilityLabelFa ? <span>{group.responsibilityLabelFa}</span> : null}
          </p>
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
                <span className="truncate">{fee.sellerDisplayName}</span>
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
            parcelCapabilities={parcelCapabilities}
            onParcelTrack={onParcelTrack}
            onParcelOperation={onParcelOperation}
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
                      className="flex items-center justify-between gap-2 px-3 py-2"
                      data-testid={`admin-order-operational-item-${lineKey(line)}`}
                    >
                      <span className="min-w-0 flex-1 truncate text-sm text-gray-800">{line.title}</span>
                      <span className="shrink-0 text-xs tabular-nums text-gray-600">
                        {`${quantityText(line.quantity)} عدد`}
                      </span>
                      <span className="shrink-0 rounded-full bg-gray-100 px-2 py-0.5 text-[11px] font-bold text-gray-600">
                        {formatAdminStatus(line.operationalStatus ?? "NotApplicable")}
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

function GroupParcels({
  detail,
  group,
  multiMember,
  pending,
  actorMayManage,
  parcelCapabilities,
  onParcelTrack,
  onParcelOperation,
  onCreateShipment,
}: {
  detail: AdminOrderDetail;
  group: AdminOperationalDeliveryGroup;
  multiMember: boolean;
  pending: boolean;
  actorMayManage: boolean;
  parcelCapabilities?: Props["parcelCapabilities"];
  onParcelTrack?: Props["onParcelTrack"];
  onParcelOperation?: Props["onParcelOperation"];
  onCreateShipment: (target: OperationalCreateTarget) => void;
}) {
  const rows = group.members.flatMap((member) => {
    const seller = resolveSeller(detail, member.sellerOrderId);
    if (!seller) return [];
    return memberShipments(seller, member.shippingGroupId).map((shipment) => ({ member, seller, shipment }));
  });

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
            const caps = cancelled || pending
              ? NO_PARCEL_ACTIONS
              : parcelCapabilities?.(row.seller, parcel) ?? NO_PARCEL_ACTIONS;
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
                        : "shrink-0 rounded-full bg-blue-50 px-2 py-0.5 text-[11px] font-bold text-blue-700"
                    }
                  >
                    {formatAdminStatus(parcel.status)}
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
    </section>
  );
}
