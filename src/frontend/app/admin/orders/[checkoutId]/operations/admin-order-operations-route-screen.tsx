"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import {
  loadAdminOrderDetail,
  type AdminOrderDetail,
  type AdminOperationalDeliveryGroup,
  type AdminResult,
  type AdminShipment,
} from "../../../admin-api";
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
 * TB-260-R1 navigation contract from the work queue (all optional, all EXACT locators):
 *   /admin/orders/{checkoutId}/operations?fulfillmentId=…&shippingGroupId=…&shipmentId=…&packageId=…&from=queue
 * A locator that no longer resolves is reported truthfully instead of silently showing a different group, and
 * readiness describes the focused physical unit (parcel/package), never just "no remaining quantity".
 */
function shipmentsOf(detail: AdminOrderDetail): AdminShipment[] {
  return detail.sellerOrders.flatMap((seller) => seller.shipments ?? []);
}

function parcelsOfGroup(group: AdminOperationalDeliveryGroup, detail: AdminOrderDetail): AdminShipment[] {
  const groupIds = new Set(group.members.map((member) => member.shippingGroupId));
  return shipmentsOf(detail).filter(
    (shipment) => shipment.shippingGroupId != null && groupIds.has(shipment.shippingGroupId),
  );
}

function parcelSummary(
  groups: AdminOperationalDeliveryGroup[],
  detail: AdminOrderDetail,
  focusedPackageNumber?: string | null,
): string {
  const parcels = groups.flatMap((group) => parcelsOfGroup(group, detail));
  const unique = Array.from(new Map(parcels.map((parcel) => [parcel.shipmentId, parcel])).values());
  if (unique.length === 0) return "بدون مرسوله";
  const inTransit = unique.filter((parcel) =>
    ["Dispatched", "InTransit", "NonDelivered", "Returning", "Returned"].includes(parcel.status)).length;
  const delivered = unique.filter((parcel) => parcel.status === "Delivered").length;
  const created = unique.length - inTransit - delivered;
  const parts = [`${unique.length.toLocaleString("fa-IR")} مرسوله`];
  if (created > 0) parts.push(`${created.toLocaleString("fa-IR")} در انتظار ارسال`);
  if (inTransit > 0) parts.push(`${inTransit.toLocaleString("fa-IR")} در مسیر`);
  if (delivered > 0) parts.push(`${delivered.toLocaleString("fa-IR")} تحویل‌شده`);
  return focusedPackageNumber ? `بستهٔ تجمیعی ${focusedPackageNumber} · ${parts.join(" · ")}` : parts.join(" · ");
}

type Readiness = Pick<OrderOperationsTargetFact, "readinessLabel" | "readinessTone">;

/** Truthful state of ONE physical parcel — a Created/tracked parcel is real work, not "nothing left". */
function parcelReadiness(parcel: AdminShipment): Readiness {
  switch (parcel.status) {
    case "Created":
      return parcel.trackingReference
        ? { readinessLabel: "آمادهٔ ارسال به حامل", readinessTone: "ready" }
        : { readinessLabel: "در انتظار ثبت کد رهگیری", readinessTone: "ready" };
    case "Dispatched":
    case "InTransit":
      return { readinessLabel: "در مسیر تحویل — در انتظار ثبت تحویل", readinessTone: "ready" };
    case "NonDelivered":
      return { readinessLabel: "عدم تحویل ثبت شده — برگشت به مبدأ", readinessTone: "blocked" };
    case "Returning":
      return { readinessLabel: "در حال برگشت — در انتظار تأیید دریافت در مبدأ", readinessTone: "ready" };
    case "Returned":
      return { readinessLabel: "در مبدأ دریافت شد", readinessTone: "done" };
    case "Delivered":
      return { readinessLabel: "تحویل شد", readinessTone: "done" };
    case "Cancelled":
      return { readinessLabel: "مرسوله لغو شده", readinessTone: "blocked" };
    case "Failed":
      return { readinessLabel: "مرسوله ناموفق", readinessTone: "blocked" };
    default:
      return { readinessLabel: "کار فیزیکی باقی نمانده", readinessTone: "done" };
  }
}

function packageReadiness(
  packageId: string,
  detail: AdminOrderDetail,
): Readiness | null {
  const pack = (detail.consolidatedPackages ?? []).find((item) => item.consolidatedPackageId === packageId);
  if (!pack) return null;
  switch (pack.status) {
    case "Created":
      return pack.trackingReference
        ? { readinessLabel: "بسته آمادهٔ ارسال به حامل", readinessTone: "ready" }
        : { readinessLabel: "در انتظار ثبت کد رهگیری بسته", readinessTone: "ready" };
    case "Dispatched":
      return { readinessLabel: "بسته در مسیر — در انتظار ثبت تحویل", readinessTone: "ready" };
    case "Delivered":
      return { readinessLabel: "بسته تحویل شد", readinessTone: "done" };
    case "Cancelled":
      return { readinessLabel: "بسته لغو شده", readinessTone: "blocked" };
    default:
      return { readinessLabel: "کار فیزیکی باقی نمانده", readinessTone: "done" };
  }
}

/**
 * Readiness of the exact focused target, then of the group's own physical units, then of its remaining
 * new-parcel quantity. A group with a Created parcel but zero fresh-creation remainder is still real work.
 */
function readinessOf(
  group: AdminOperationalDeliveryGroup,
  detail: AdminOrderDetail,
  focus: { shipment: AdminShipment | null; packageId: string | null },
): Readiness {
  if (focus.shipment) {
    return parcelReadiness(focus.shipment);
  }
  if (focus.packageId) {
    const fromPackage = packageReadiness(focus.packageId, detail);
    if (fromPackage) return fromPackage;
  }

  const parcels = parcelsOfGroup(group, detail);
  if (parcels.some((parcel) => ["NonDelivered", "Returning"].includes(parcel.status))) {
    const returning = parcels.find((parcel) => parcel.status === "Returning");
    return returning
      ? parcelReadiness(returning)
      : { readinessLabel: "عدم تحویل ثبت شده — برگشت به مبدأ", readinessTone: "blocked" };
  }

  const remaining = group.members.reduce(
    (sum, member) => sum + (member.lines ?? []).reduce((lineSum, line) => lineSum + line.remainingPackedQuantity, 0),
    0,
  );
  if (remaining > 0) {
    const eligible = group.members.some((member) => member.createParcelEligible);
    return eligible
      ? { readinessLabel: "آمادهٔ ایجاد مرسولهٔ باقی‌مانده", readinessTone: "ready" }
      : {
          readinessLabel: group.consolidateBlockReasonFa ?? "در انتظار شرط عملیاتی",
          readinessTone: "blocked",
        };
  }

  const created = parcels.find((parcel) => parcel.status === "Created");
  if (created) return parcelReadiness(created);

  const moving = parcels.find((parcel) => ["Dispatched", "InTransit"].includes(parcel.status));
  if (moving) return parcelReadiness(moving);

  if (parcels.length > 0 && parcels.every((parcel) => parcel.status === "Delivered")) {
    return { readinessLabel: "تحویل شد", readinessTone: "done" };
  }

  return { readinessLabel: "کار فیزیکی باقی نمانده", readinessTone: "done" };
}

function toTargetFact(
  group: AdminOperationalDeliveryGroup,
  detail: AdminOrderDetail,
  focus: { shipment: AdminShipment | null; packageId: string | null },
): OrderOperationsTargetFact {
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
  const focusedPackageNumber = focus.packageId
    ? (detail.consolidatedPackages ?? []).find((item) => item.consolidatedPackageId === focus.packageId)?.packageNumber ?? null
    : null;
  return {
    groupKey: group.operationalGroupKey,
    methodLabel: group.methodLabel,
    customerDeliverySummary: delivery,
    plannedDispatchSummary: plan,
    parcelSummary: parcelSummary([group], detail, focusedPackageNumber),
    ...readinessOf(group, detail, focus),
  };
}

export function AdminOrderOperationsRouteScreen({
  checkoutId,
  fulfillmentId,
  shippingGroupId,
  shipmentId = null,
  packageId = null,
  fromQueue = false,
  backFilter = null,
}: {
  checkoutId: string;
  fulfillmentId: string | null;
  shippingGroupId: string | null;
  /** TB-260-R1: exact parcel locator from a parcel work row. */
  shipmentId?: string | null;
  /** TB-260-R1: exact consolidated-package locator from a package work row. */
  packageId?: string | null;
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
  const targetRequested = Boolean(fulfillmentId || shippingGroupId || shipmentId || packageId) || fromQueue;

  const targetSellerOrderIds = useMemo(() => {
    if (!detail || !fulfillmentId) return null;
    return new Set(
      detail.sellerOrders
        .filter((seller) => (seller.fulfillmentId ?? seller.id) === fulfillmentId)
        .map((seller) => seller.id),
    );
  }, [detail, fulfillmentId]);

  // TB-260-R1: the exact physical target the queue row pointed at.
  const focus = useMemo(() => {
    if (!detail) return { shipment: null as AdminShipment | null, packageId: null as string | null };
    const shipment = shipmentId
      ? shipmentsOf(detail).find((item) => item.shipmentId === shipmentId) ?? null
      : null;
    return { shipment, packageId };
  }, [detail, shipmentId, packageId]);

  const targets = useMemo((): OrderOperationsTargetFact[] => {
    if (!detail) return [];
    const groups = detail.operationalDeliveryGroups ?? [];

    // A package locator resolves to the groups whose parcels are locked into that package.
    if (packageId) {
      const pack = (detail.consolidatedPackages ?? []).find((item) => item.consolidatedPackageId === packageId);
      if (!pack) return [];
      const shipmentGroupIds = new Set(
        pack.members
          .map((member) => shipmentsOf(detail).find((shipment) => shipment.shipmentId === member.shipmentId)?.shippingGroupId)
          .filter((value): value is string => Boolean(value)),
      );
      const matched = groups.filter((group) =>
        group.members.some((member) => shipmentGroupIds.has(member.shippingGroupId)));
      return matched.map((group) => toTargetFact(group, detail, focus));
    }

    // A shipment locator resolves to its own accepted group; an ungrouped parcel falls back to its seller order.
    if (shipmentId) {
      const shipment = focus.shipment;
      if (!shipment) return [];
      const matched = groups.filter((group) => {
        if (shipment.shippingGroupId) {
          return group.members.some((member) => member.shippingGroupId === shipment.shippingGroupId);
        }
        return targetSellerOrderIds
          ? group.members.some((member) => targetSellerOrderIds.has(member.sellerOrderId))
          : false;
      });
      return matched.map((group) => toTargetFact(group, detail, focus));
    }

    const matched = groups.filter((group) => {
      if (shippingGroupId) {
        return group.members.some((member) => member.shippingGroupId === shippingGroupId);
      }
      if (targetSellerOrderIds) {
        return group.members.some((member) => targetSellerOrderIds.has(member.sellerOrderId));
      }
      // A fulfillment locator that resolves to nothing must not silently become "the whole order".
      return !fulfillmentId;
    });
    return matched.map((group) => toTargetFact(group, detail, focus));
  }, [detail, focus, fulfillmentId, packageId, shipmentId, shippingGroupId, targetSellerOrderIds]);

  const focusedPackageNumber = useMemo(
    () => (packageId
      ? (detail?.consolidatedPackages ?? []).find((item) => item.consolidatedPackageId === packageId)?.packageNumber ?? null
      : null),
    [detail, packageId],
  );

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
      targetPackageNumber={focusedPackageNumber}
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
