"use client";

import React, { useEffect, useMemo, useRef, useState } from "react";
import { Package } from "lucide-react";
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
  isOperationalGroupNoClaimRefusal,
  markOperationalGroupAttemptFailed,
  operationalGroupRequestFingerprint,
  readOperationalGroupAttempt,
  releaseOperationalGroupAttemptAfterNoClaimRefusal,
  resolveOperationalGroupAttempt,
  type OperationalGroupAttempt,
} from "./operational-group-occurrence-store";

export type OperationalCreateTarget = {
  seller: AdminSellerOrder;
  group: AdminAcceptedShippingGroup;
};

/**
 * TB-253-R5: explicit workflow outcome. A boolean/void return keeps meaning "completed" / "unknown".
 * A typed refusal that proves NO durable claim may carry its code so the caller can take the explicit
 * safe transition; anything else (network, 5xx, timeout) stays uncertainty.
 */
export type OperationalGroupWorkflowOutcome =
  | { completed: true }
  | { completed: false; noClaimRefusalCode?: string | null };

function outcomeCompleted(outcome: boolean | OperationalGroupWorkflowOutcome | void): boolean {
  if (outcome === true) return true;
  if (outcome && typeof outcome === "object") return outcome.completed === true;
  return false;
}

function outcomeNoClaimRefusalCode(
  outcome: boolean | OperationalGroupWorkflowOutcome | void,
): string | null {
  if (!outcome || typeof outcome !== "object" || outcome.completed) return null;
  const code = outcome.noClaimRefusalCode;
  return isOperationalGroupNoClaimRefusal(code) ? String(code) : null;
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
  workflowErrorFa?: string | null;
  /**
   * TB-253-R4: viewer/owner scope of a retained workflow occurrence. The prebound envelope is stored
   * per checkout/group/scope, so another admin session can never inherit it.
   */
  occurrenceScopeKey?: string | null;
  selectedBySeller: Record<string, string[]>;
  qtyByLine: Record<string, number>;
  shippableQty: (line: AdminOrderLine) => number;
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

function memberLines(
  seller: AdminSellerOrder,
  orderLineIds: string[],
): AdminOrderLine[] {
  const set = new Set(orderLineIds);
  return seller.lines.filter((line) => set.has(line.orderLineId || line.id));
}

function memberShipments(seller: AdminSellerOrder, shippingGroupId: string): AdminShipment[] {
  return (seller.shipments ?? []).filter((s) => s.shippingGroupId === shippingGroupId);
}

function eligibleReusableParcels(seller: AdminSellerOrder, shippingGroupId: string): AdminShipment[] {
  return memberShipments(seller, shippingGroupId).filter(
    (s) => s.status === "Created" && !s.activePackageNumber && !s.packageLockedReasonFa && Boolean(s.shippingMethodCode),
  );
}

function memberReviewLines(member: AdminOperationalDeliveryMember): Array<{
  orderLineId: string;
  title: string;
  remaining: number;
}> {
  const fromServer = member.lines ?? [];
  if (fromServer.length > 0) {
    return fromServer.map((line) => ({
      orderLineId: line.orderLineId,
      title: line.title,
      remaining: Math.max(0, line.remainingPackedQuantity),
    }));
  }
  return member.orderLineIds.map((id) => ({
    orderLineId: id,
    title: "کالای سفارش",
    remaining: 0,
  }));
}

/**
 * TB-253: primary «ارسال سفارش» cards from server-owned operationalDeliveryGroups.
 * Does not regroup client-side; only renders the projection.
 */
export function AdminOrderOperationalDeliverySection({
  detail,
  pendingCode,
  onCreateShipment,
  onPrepareOperationalGroup,
  workflowErrorFa,
  occurrenceScopeKey,
  selectedBySeller,
  qtyByLine,
  shippableQty,
}: Props) {
  const groups = detail.operationalDeliveryGroups ?? [];
  if (groups.length === 0) {
    return (
      <div
        className="rounded-xl border border-amber-100 bg-amber-50/60 px-3 py-2.5 text-xs text-amber-900"
        data-testid="admin-order-operational-delivery-empty"
        dir="rtl"
      >
        گروه عملیاتی ارسال برای این سفارش در دسترس نیست.
      </div>
    );
  }

  return (
    <section className="space-y-3" data-testid="admin-order-operational-delivery" dir="rtl">
      <div className="flex items-center justify-between gap-2">
        <h3 className="text-sm font-black text-gray-900">ارسال سفارش</h3>
        <span className="text-[11px] text-gray-500">
          {groups.length.toLocaleString("fa-IR")} گروه عملیاتی
        </span>
      </div>
      {workflowErrorFa ? (
        <p
          className="rounded-lg border border-rose-200 bg-rose-50 px-2.5 py-1.5 text-[11px] font-semibold text-rose-900"
          role="alert"
          data-testid="admin-order-operational-workflow-error"
        >
          {workflowErrorFa}
        </p>
      ) : null}
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
            selectedBySeller={selectedBySeller}
            qtyByLine={qtyByLine}
            shippableQty={shippableQty}
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
  selectedBySeller,
  qtyByLine,
  shippableQty,
}: {
  group: AdminOperationalDeliveryGroup;
  detail: AdminOrderDetail;
  pendingCode: string | null;
  onCreateShipment: (target: OperationalCreateTarget) => void;
  onPrepareOperationalGroup?: (
    group: AdminOperationalDeliveryGroup,
    request: OperationalGroupWorkflowRequest,
  ) => Promise<boolean | OperationalGroupWorkflowOutcome | void> | boolean | OperationalGroupWorkflowOutcome | void;
  occurrenceScopeKey?: string | null;
  selectedBySeller: Record<string, string[]>;
  qtyByLine: Record<string, number>;
  shippableQty: (line: AdminOrderLine) => number;
}) {
  const [excludedParcelIds, setExcludedParcelIds] = useState<Record<string, boolean>>({});
  const [qtyByOrderLine, setQtyByOrderLine] = useState<Record<string, number>>({});
  // TB-253-R4: the retained attempt of this checkout/group/scope. It keeps the COMPLETE reviewed
  // envelope across a lost response, so retry reconciles the recorded occurrence; a changed review
  // automatically becomes a NEW occurrence instead of a typed server-side intent conflict.
  const attemptRef = useRef<OperationalGroupAttempt | null>(null);
  const inFlightRef = useRef(false);
  const [reconcileHint, setReconcileHint] = useState<{ occurrenceId: string } | null>(null);
  // TB-253-R4: a retained envelope survives remounts and projected-quantity changes, so an explicit
  // reconcile of THAT operation stays available even when no fresh create is currently ready.
  const [retainedAttempt, setRetainedAttempt] = useState<OperationalGroupAttempt | null>(null);

  useEffect(() => {
    // TB-253-R5: a viewer/scope change must drop the previous viewer's in-memory refs — a stale ref is
    // never authorization, and the retained envelope belongs to the viewer that recorded it.
    attemptRef.current = null;
    setReconcileHint(null);
    const recorded = readOperationalGroupAttempt(
      detail.checkoutId,
      group.operationalGroupKey,
      occurrenceScopeKey,
    );
    setRetainedAttempt(recorded);
  }, [detail.checkoutId, group.operationalGroupKey, occurrenceScopeKey]);
  const dateRaw = (group.deliveryDate ?? "").trim();
  const dateFa = dateRaw
    ? formatJalaliDate(dateRaw.includes("T") ? dateRaw : `${dateRaw}T12:00:00`, "fa")
    : null;
  const timeWindow = (group.deliveryTimeWindow ?? "").trim();
  const showFeeBreakdown = group.feeBreakdown.length > 1;
  const actorMayManage = group.actorMayManageOperations !== false;

  const review = useMemo(() => {
    return group.members.map((member) => {
      const seller = resolveSeller(detail, member.sellerOrderId);
      return {
        member,
        seller,
        parcels: seller ? eligibleReusableParcels(seller, member.shippingGroupId) : [],
        lines: memberReviewLines(member),
      };
    });
  }, [detail, group.members]);

  let selectionCount = 0;
  const selections: Array<{ orderLineId: string; quantity: number }> = [];
  const reuseShipmentIds: string[] = [];
  for (const row of review) {
    for (const parcel of row.parcels) {
      if (!excludedParcelIds[parcel.shipmentId]) {
        reuseShipmentIds.push(parcel.shipmentId);
        selectionCount += 1;
      }
    }
    for (const line of row.lines) {
      const value = qtyByOrderLine[line.orderLineId] ?? (row.parcels.length > 0 ? 0 : line.remaining);
      const qty = Math.max(0, Math.min(value, line.remaining));
      if (qty > 0) {
        selections.push({ orderLineId: line.orderLineId, quantity: qty });
        selectionCount += 1;
      }
    }
  }
  const canRunWorkflow = Boolean(onPrepareOperationalGroup)
    && group.isCrossSellerMarketplace
    && group.consolidateEligible
    && actorMayManage
    && pendingCode === null;

  // TB-253-R5: while an unresolved attempt exists it is the ONLY lawful operation for this group.
  // A changed review is therefore blocked at the state boundary (not merely visually), and the
  // retained envelope stays untouched until authoritative resolution.
  const reviewFingerprint = operationalGroupRequestFingerprint({
    members: group.members.map((member) => member.shippingGroupId),
    selections,
    reuseShipmentIds,
  });
  const retainedBlocksNewOperation = Boolean(retainedAttempt)
    && retainedAttempt!.fingerprint !== reviewFingerprint;

  return (
    <article
      className="rounded-xl border border-indigo-100 bg-indigo-50/20 p-3"
      data-testid={`admin-order-operational-group-${group.operationalGroupKey}`}
      data-cross-seller={group.isCrossSellerMarketplace ? "true" : "false"}
    >
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div>
          <p className="text-sm font-black text-indigo-950">{group.methodLabel || group.methodCode}</p>
          <p className="mt-0.5 text-[11px] text-gray-600">
            {dateFa || timeWindow ? (
              <>
                {dateFa ?? "—"}
                {timeWindow ? (
                  <>
                    {" · "}
                    <bdi dir="ltr">{timeWindow}</bdi>
                  </>
                ) : null}
              </>
            ) : (
              "زمان تحویل ثبت نشده"
            )}
          </p>
          {group.responsibilityLabelFa ? (
            <p className="mt-0.5 text-[11px] text-gray-500">{group.responsibilityLabelFa}</p>
          ) : null}
        </div>
        <div className="text-left text-[11px] font-bold text-gray-800" dir="ltr">
          {formatAdminMoney(group.combinedFeeTotal, group.currency)}
          {showFeeBreakdown ? (
            <span className="block text-[10px] font-semibold text-gray-500" dir="rtl">
              جمع هزینه ارسال
            </span>
          ) : null}
        </div>
      </div>

      {showFeeBreakdown ? (
        <ul
          className="mt-2 space-y-0.5 rounded-lg bg-white/70 px-2 py-1.5 text-[10px] text-gray-600"
          data-testid={`admin-order-operational-fee-breakdown-${group.operationalGroupKey}`}
        >
          {group.feeBreakdown.map((fee) => (
            <li key={fee.shippingGroupId} className="flex justify-between gap-2">
              <span>{fee.sellerDisplayName}</span>
              <span dir="ltr">{formatAdminMoney(fee.feeAmount, fee.currency)}</span>
            </li>
          ))}
        </ul>
      ) : null}

      {group.separateReasonFa ? (
        <p className="mt-2 text-[11px] font-semibold text-amber-800" role="status">
          {group.separateReasonFa}
        </p>
      ) : null}

      <div className="mt-3 space-y-2">
        {group.isCrossSellerMarketplace ? (
          <p className="text-[11px] font-semibold text-indigo-900" data-testid={`admin-order-operational-review-${group.operationalGroupKey}`}>
            بازبینی گروه: مرسوله‌های موجود یا مقدار باقی‌ماندهٔ هر عضو را انتخاب کنید.
          </p>
        ) : null}
        {review.map(({ member, seller, parcels, lines }) => {
          const accepted = resolveAcceptedGroup(detail, member.shippingGroupId);
          const selected = selectedBySeller[member.sellerOrderId] ?? [];
          const selectionOk = selected.length === 0 || selected.every((id) => {
            const line = seller?.lines.find((l) => lineKey(l) === id);
            const lineId = line ? (line.orderLineId || line.id) : id;
            return member.orderLineIds.includes(lineId);
          });
          const canCreate = Boolean(seller)
            && Boolean(accepted)
            && member.createParcelEligible
            && member.transportMode !== "CarrierApi"
            && selectionOk
            && actorMayManage;

          return (
            <div
              key={member.shippingGroupId}
              className="rounded-lg border border-white bg-white/90 p-2.5"
              data-testid={`admin-order-operational-member-${member.shippingGroupId}`}
            >
              <div className="flex flex-wrap items-center justify-between gap-2">
                <p className="text-xs font-black text-gray-900">{member.sellerDisplayName}</p>
                <p className="text-[10px] text-gray-500" dir="ltr">
                  {formatAdminMoney(member.feeAmount, member.currency)}
                </p>
              </div>

              {parcels.length > 0 ? (
                <ul className="mt-1.5 space-y-1" data-testid={`admin-order-operational-reuse-${member.shippingGroupId}`}>
                  {parcels.map((parcel) => (
                    <li key={parcel.shipmentId} className="flex items-center gap-2 text-[11px] text-gray-700">
                      <input
                        type="checkbox"
                        className="size-4 shrink-0"
                        checked={!excludedParcelIds[parcel.shipmentId]}
                        onChange={(event) => {
                          const checked = event.currentTarget.checked;
                          setExcludedParcelIds((prev) => ({ ...prev, [parcel.shipmentId]: !checked }));
                        }}
                        aria-label={`استفاده از مرسولهٔ موجود ${parcel.shipmentId}`}
                        data-testid={`admin-order-operational-reuse-parcel-${parcel.shipmentId}`}
                      />
                      <span className="min-w-0 flex-1 truncate font-semibold">مرسولهٔ آمادهٔ موجود</span>
                      <span className="tabular-nums text-gray-500">
                        {parcel.itemCount.toLocaleString("fa-IR")} قلم
                      </span>
                    </li>
                  ))}
                </ul>
              ) : null}

              <ul className="mt-1.5 space-y-1">
                {lines.map((line) => {
                  const key = line.orderLineId;
                  const defaultValue = parcels.length > 0 ? 0 : line.remaining;
                  const value = Math.max(0, Math.min(qtyByOrderLine[key] ?? defaultValue, line.remaining));
                  return (
                    <li
                      key={key}
                      className="flex items-center gap-2 text-[11px] text-gray-700"
                      data-testid={`admin-order-operational-line-${key}`}
                    >
                      <span className="inline-flex size-6 shrink-0 items-center justify-center rounded bg-gray-100 text-gray-400">
                        <Package className="size-3" aria-hidden />
                      </span>
                      <span className="min-w-0 flex-1 truncate font-semibold">{line.title}</span>
                      {line.remaining > 0 ? (
                        <input
                          type="number"
                          min={0}
                          max={line.remaining}
                          value={value}
                          onChange={(event) => {
                            const next = Math.max(0, Math.min(Number(event.currentTarget.value) || 0, line.remaining));
                            setQtyByOrderLine((prev) => ({ ...prev, [key]: next }));
                          }}
                          aria-label={`مقدار ${line.title}`}
                          className="w-14 rounded border border-gray-200 px-1 py-0.5 text-center tabular-nums"
                          data-testid={`admin-order-operational-qty-${key}`}
                        />
                      ) : (
                        <span className="tabular-nums text-gray-500">۰</span>
                      )}
                      <span className="tabular-nums text-gray-500">
                        از {line.remaining.toLocaleString("fa-IR")}
                      </span>
                    </li>
                  );
                })}
              </ul>

              {parcels.length === 0 && lines.every((line) => line.remaining === 0) ? (
                <p className="mt-1.5 text-[10px] text-amber-800" role="status">
                  مقدار بسته‌بندی‌شدهٔ باقی‌مانده‌ای برای این عضو نیست.
                </p>
              ) : null}

              {canCreate && seller && accepted ? (
                <button
                  type="button"
                  disabled={pendingCode !== null}
                  onClick={() => onCreateShipment({ seller, group: accepted })}
                  className="mt-2 w-full rounded-lg bg-[#2563EB] px-3 py-1.5 text-xs font-bold text-white disabled:opacity-50"
                  data-testid={`admin-order-operational-create-${member.shippingGroupId}`}
                >
                  + ایجاد مرسولهٔ مستقل
                </button>
              ) : member.createParcelEligible === false && parcels.length === 0 ? (
                <p className="mt-2 text-[10px] text-amber-800" role="status">
                  {member.createParcelBlockReasonFa
                    ?? (!selectionOk
                      ? "انتخاب فعلی شامل اقلام خارج از این گروه است."
                      : "ایجاد مرسوله برای این بخش در دسترس نیست.")}
                </p>
              ) : null}
            </div>
          );
        })}
      </div>

      {group.isCrossSellerMarketplace && group.consolidateEligible && onPrepareOperationalGroup && actorMayManage ? (
        <button
          type="button"
          disabled={!canRunWorkflow || selectionCount === 0 || retainedBlocksNewOperation}
          onClick={async () => {
            if (!onPrepareOperationalGroup) return;
            // Synchronous single-flight: a same-tick second click cannot mint a second attempt.
            if (!canRunWorkflow || selectionCount === 0 || inFlightRef.current) return;
            inFlightRef.current = true;
            try {
              const resolution = resolveOperationalGroupAttempt({
                checkoutId: detail.checkoutId,
                operationalGroupKey: group.operationalGroupKey,
                scopeKey: occurrenceScopeKey,
                request: {
                  members: group.members.map((m) => m.shippingGroupId),
                  selections,
                  reuseShipmentIds,
                },
              });
              if (resolution.blockedByRetainedAttempt) {
                // R5: the retained envelope is unresolved. Nothing is minted or overwritten, and no
                // request is sent from a corrected review. Only the recorded operation may proceed.
                attemptRef.current = resolution.attempt;
                setRetainedAttempt(resolution.attempt);
                setReconcileHint({ occurrenceId: resolution.occurrenceId });
                return;
              }
              attemptRef.current = resolution.attempt;
              const outcome = await onPrepareOperationalGroup(group, {
                operationalGroupShippingGroupIds: resolution.attempt.members,
                selections: resolution.attempt.selections,
                reuseShipmentIds: resolution.attempt.reuseShipmentIds,
                operationalGroupOccurrenceId: resolution.occurrenceId,
              });
              if (outcomeCompleted(outcome)) {
                // Completed: the reviewed envelope is done, so the next deliberate run is a NEW one.
                attemptRef.current = null;
                setReconcileHint(null);
                setRetainedAttempt(null);
                clearOperationalGroupAttempt(
                  detail.checkoutId,
                  group.operationalGroupKey,
                  occurrenceScopeKey,
                );
                return;
              }

              // A typed refusal that proves no claim/side effect (thrown before the durable claim) is
              // the ONLY explicit safe transition back to a correctable review.
              const noClaimCode = outcomeNoClaimRefusalCode(outcome);
              if (noClaimCode && releaseOperationalGroupAttemptAfterNoClaimRefusal({
                checkoutId: detail.checkoutId,
                operationalGroupKey: group.operationalGroupKey,
                scopeKey: occurrenceScopeKey,
                code: noClaimCode,
              })) {
                attemptRef.current = null;
                setRetainedAttempt(null);
                setReconcileHint(null);
                return;
              }

              // Failure / 5xx / lost response / cancellation: KEEP the exact envelope and offer a
              // reconcile of that same occurrence. A cancelled or thrown call is uncertainty, never a
              // reason to mint a new operation.
              markOperationalGroupAttemptFailed({ attempt: resolution.attempt });
              attemptRef.current = resolution.attempt;
              setRetainedAttempt(resolution.attempt);
              setReconcileHint({ occurrenceId: resolution.occurrenceId });
            } finally {
              inFlightRef.current = false;
            }
          }}
          className="mt-2 w-full rounded-lg border border-emerald-200 bg-emerald-50 px-3 py-1.5 text-xs font-bold text-emerald-900 disabled:opacity-50"
          data-testid={`admin-order-operational-prepare-${group.operationalGroupKey}`}
        >
          {reconcileHint ? "همگام‌سازی مجدد همان عملیات" : "ایجاد مرسوله‌ها و بستهٔ مشترک این گروه"}
        </button>
      ) : null}
      {retainedBlocksNewOperation && retainedAttempt && actorMayManage ? (
        <p
          className="mt-2 rounded-lg border border-amber-200 bg-amber-50 px-2.5 py-1.5 text-[10px] font-semibold text-amber-900"
          role="status"
          data-testid={`admin-order-operational-retained-block-${group.operationalGroupKey}`}
        >
          نتیجهٔ عملیات قبلی هنوز نامشخص است؛ تا همگام‌سازی همان عملیات، درخواست جدیدی شروع نمی‌شود.
        </p>
      ) : null}
      {retainedAttempt && actorMayManage && onPrepareOperationalGroup ? (
        <div
          className="mt-2 rounded-lg border border-emerald-100 bg-emerald-50/60 px-2.5 py-1.5"
          data-testid={`admin-order-operational-reconcile-${group.operationalGroupKey}`}
          title={retainedAttempt.occurrenceId}
        >
          <p className="text-[10px] font-semibold text-emerald-900" role="status">
            نتیجهٔ عملیات نامشخص است؛ تلاش دوباره همان عملیات ثبت‌شده را همگام می‌کند و مرسولهٔ تازه نمی‌سازد.
          </p>
          <button
            type="button"
            disabled={pendingCode !== null}
            onClick={async () => {
              // Reconcile the RETAINED envelope, independent of projected remaining quantity or of a
              // currently-enabled create action.
              if (pendingCode !== null || inFlightRef.current) return;
              inFlightRef.current = true;
              try {
                const outcome = await onPrepareOperationalGroup(group, {
                  operationalGroupShippingGroupIds: retainedAttempt.members,
                  selections: retainedAttempt.selections,
                  reuseShipmentIds: retainedAttempt.reuseShipmentIds,
                  operationalGroupOccurrenceId: retainedAttempt.occurrenceId,
                });
                if (outcomeCompleted(outcome)) {
                  setRetainedAttempt(null);
                  setReconcileHint(null);
                  attemptRef.current = null;
                  clearOperationalGroupAttempt(
                    detail.checkoutId,
                    group.operationalGroupKey,
                    occurrenceScopeKey,
                  );
                  return;
                }
                const noClaimCode = outcomeNoClaimRefusalCode(outcome);
                if (noClaimCode && releaseOperationalGroupAttemptAfterNoClaimRefusal({
                  checkoutId: detail.checkoutId,
                  operationalGroupKey: group.operationalGroupKey,
                  scopeKey: occurrenceScopeKey,
                  code: noClaimCode,
                })) {
                  attemptRef.current = null;
                  setRetainedAttempt(null);
                  setReconcileHint(null);
                  return;
                }
                markOperationalGroupAttemptFailed({ attempt: retainedAttempt });
                setReconcileHint({ occurrenceId: retainedAttempt.occurrenceId });
              } finally {
                inFlightRef.current = false;
              }
            }}
            className="mt-1 w-full rounded-md border border-emerald-300 bg-white px-2 py-1 text-[11px] font-bold text-emerald-900 disabled:opacity-50"
            data-testid={`admin-order-operational-reconcile-retry-${group.operationalGroupKey}`}
          >
            همگام‌سازی مجدد همان عملیات
          </button>
          <p
            className="mt-1 text-[10px] text-emerald-800"
            data-testid={`admin-order-operational-reconcile-envelope-${group.operationalGroupKey}`}
            dir="ltr"
          >
            {`${retainedAttempt.members.length} ${retainedAttempt.selections.length} ${retainedAttempt.reuseShipmentIds.length}`}
          </p>
        </div>
      ) : null}
      {group.isCrossSellerMarketplace && group.consolidateBlockReasonFa ? (
        <p
          className="mt-2 text-[10px] text-gray-500"
          data-testid={`admin-order-operational-consolidate-note-${group.operationalGroupKey}`}
        >
          {group.consolidateBlockReasonFa}
        </p>
      ) : null}
      {group.isCrossSellerMarketplace && !actorMayManage ? (
        <p
          className="mt-2 text-[10px] text-gray-500"
          data-testid={`admin-order-operational-no-authority-${group.operationalGroupKey}`}
        >
          مجوز عملیات تجمیع برای این کاربر فعال نیست.
        </p>
      ) : null}
    </article>
  );
}
