"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { loadAdminOrderDetail, type AdminOrderDetail, type AdminOperationalDeliveryGroup, type AdminResult } from "../../../admin-api";
import { AdminOrderItemsShippingPanel } from "../../../admin-order-items-shipping-panel";
import {
  OrderOperationsView,
  type OrderOperationsTargetFact,
} from "../../../../../features/order-operational-groups";

/**
 * TB-260 — the shared operational order view.
 *
 * This route renders the SAME `AdminOrderItemsShippingPanel` the full order page renders: one component, one
 * set of server capabilities, one retained envelope, one dialogue owner. Everything financial (payments,
 * settlement, refunds, wallet, seller share) is deliberately absent here; the full order page keeps it.
 *
 * Navigation contract from the work queue (all optional, all locators only):
 *   /admin/orders/{checkoutId}/operations?fulfillmentId=…&shippingGroupId=…&from=queue
 * A locator that no longer resolves is reported truthfully instead of silently showing a different group.
 */
function parcelSummary(groups: AdminOperationalDeliveryGroup[], detail: AdminOrderDetail): string {
  const shipmentIds = new Set(groups.flatMap((group) => group.members.map((member) => member.shippingGroupId)));
  const parcels = detail.sellerOrders.flatMap((seller) => seller.shipments ?? []).filter(
    (shipment) => shipment.shippingGroupId != null && shipmentIds.has(shipment.shippingGroupId),
  );
  if (parcels.length === 0) return "بدون مرسوله";
  const inTransit = parcels.filter((p) =>
    ["Dispatched", "InTransit", "NonDelivered", "Returning", "Returned"].includes(p.status)).length;
  const delivered = parcels.filter((p) => p.status === "Delivered").length;
  const created = parcels.length - inTransit - delivered;
  const parts = [`${parcels.length.toLocaleString("fa-IR")} مرسوله`];
  if (created > 0) parts.push(`${created.toLocaleString("fa-IR")} در انتظار ارسال`);
  if (inTransit > 0) parts.push(`${inTransit.toLocaleString("fa-IR")} در مسیر`);
  if (delivered > 0) parts.push(`${delivered.toLocaleString("fa-IR")} تحویل‌شده`);
  return parts.join(" · ");
}

function readinessOf(group: AdminOperationalDeliveryGroup): Pick<
  OrderOperationsTargetFact,
  "readinessLabel" | "readinessTone"
> {
  const remaining = group.members.reduce(
    (sum, member) => sum + (member.lines ?? []).reduce((lineSum, line) => lineSum + line.remainingPackedQuantity, 0),
    0,
  );
  if (remaining > 0) {
    const eligible = group.members.some((member) => member.createParcelEligible);
    return eligible
      ? { readinessLabel: "آمادهٔ اقدام", readinessTone: "ready" }
      : {
          readinessLabel: group.consolidateBlockReasonFa ?? "در انتظار شرط عملیاتی",
          readinessTone: "blocked",
        };
  }
  return { readinessLabel: "کار فیزیکی باقی نمانده", readinessTone: "done" };
}

function toTargetFact(group: AdminOperationalDeliveryGroup, detail: AdminOrderDetail): OrderOperationsTargetFact {
  const delivery = [group.deliveryDate, group.deliveryTimeWindow].filter(Boolean).join(" · ");
  const plan = group.plannedDispatch
    ? [
        group.plannedDispatch.handoffDate,
        group.plannedDispatch.handoffTimeWindow,
        group.plannedDispatch.handoffTimeZone ? `(${group.plannedDispatch.handoffTimeZone})` : null,
      ]
        .filter(Boolean)
        .join(" · ")
    : null;
  return {
    groupKey: group.operationalGroupKey,
    methodLabel: group.methodLabel,
    customerDeliverySummary: delivery,
    plannedDispatchSummary: plan,
    parcelSummary: parcelSummary([group], detail),
    ...readinessOf(group),
  };
}

export function AdminOrderOperationsRouteScreen({
  checkoutId,
  fulfillmentId,
  shippingGroupId,
  fromQueue = false,
  backFilter = null,
}: {
  checkoutId: string;
  fulfillmentId: string | null;
  shippingGroupId: string | null;
  fromQueue?: boolean;
  /** The quick filter the operator left behind, so «بازگشت به صف کار» restores the same queue view. */
  backFilter?: string | null;
}) {
  const [result, setResult] = useState<AdminResult<AdminOrderDetail>>({ state: "ok", data: null, status: 0 });
  const [reloadToken, setReloadToken] = useState(0);

  const refresh = useCallback(() => {
    void loadAdminOrderDetail(checkoutId).then(setResult);
  }, [checkoutId]);

  useEffect(() => {
    void reloadToken;
    refresh();
  }, [refresh, reloadToken]);

  const detail = result.data;
  const targetRequested = Boolean(fulfillmentId || shippingGroupId) || fromQueue;

  const targetSellerOrderIds = useMemo(() => {
    if (!detail || !fulfillmentId) return null;
    return new Set(
      detail.sellerOrders
        .filter((seller) => (seller.fulfillmentId ?? seller.id) === fulfillmentId)
        .map((seller) => seller.id),
    );
  }, [detail, fulfillmentId]);

  const targets = useMemo((): OrderOperationsTargetFact[] => {
    if (!detail) return [];
    const groups = detail.operationalDeliveryGroups ?? [];
    const matched = groups.filter((group) => {
      if (shippingGroupId) {
        return group.members.some((member) => member.shippingGroupId === shippingGroupId);
      }
      if (targetSellerOrderIds) {
        return group.members.some((member) => targetSellerOrderIds.has(member.sellerOrderId));
      }
      return true;
    });
    return matched.map((group) => toTargetFact(group, detail));
  }, [detail, shippingGroupId, targetSellerOrderIds]);

  const viewState = result.state === "denied"
    ? "denied"
    : result.state === "error"
      ? "error"
      : detail
        ? "ready"
        : "loading";

  const destinationLine = detail
    ? [detail.provinceName, detail.cityName, detail.postalAddress].filter(Boolean).join("، ")
    : "";

  return (
    <OrderOperationsView
      checkoutId={checkoutId}
      reference={detail?.reference ?? ""}
      state={viewState}
      errorMessage={result.message ?? null}
      onRetry={() => setReloadToken((value) => value + 1)}
      backHref={backFilter ? `/admin/fulfillments?filter=${encodeURIComponent(backFilter)}` : "/admin/fulfillments"}
      fullOrderHref={`/admin/orders/${checkoutId}`}
      recipientName={detail?.recipientName}
      recipientMobile={detail?.contactMobile}
      destinationLine={destinationLine}
      methodLabel={detail?.shippingMethodLabel}
      targetRequested={targetRequested}
      targets={targets}
    >
      {detail ? (
        <AdminOrderItemsShippingPanel
          detail={detail}
          checkoutId={checkoutId}
          onCompleted={() => setReloadToken((value) => value + 1)}
        />
      ) : null}
    </OrderOperationsView>
  );
}
