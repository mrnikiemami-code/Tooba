"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import {
  formatAdminShipmentStatus,
  loadAdminOrderDetail,
  type AdminOrderDetail,
  type AdminOperationalDeliveryGroup,
  type AdminResult,
  type AdminShipment,
} from "../../../admin-api";
import { AdminOrderItemsShippingPanel } from "../../../admin-order-items-shipping-panel";
import { mapAdminErrorMessage } from "../../../admin-error-map";
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

/** TB-261: the server-owned returned capacity/damage facts of ONE parcel, read from the owning detail. */
function parcelReturnFacts(
  parcel: AdminShipment,
  detail: AdminOrderDetail,
): { resendable: number; damaged: number; linkedResend: AdminShipment | null } {
  const lines = new Map<string, { resendableQuantity?: number | null }>();
  for (const seller of detail.sellerOrders) {
    for (const line of seller.lines ?? []) lines.set(line.orderLineId ?? "", line);
  }
  const resendable = (parcel.lines ?? []).reduce((sum, line) => {
    const source = lines.get(line.orderLineId);
    return sum + Math.max(0, source?.resendableQuantity ?? 0);
  }, 0);
  const damaged = (parcel.returnReceiptLines ?? [])
    .filter((receipt) => {
      const condition = (receipt.condition ?? "").trim();
      return condition === "Damaged" || condition === "1";
    })
    .reduce((sum, receipt) => sum + receipt.quantity, 0);
  const linkedResend =
    shipmentsOf(detail).find((candidate) => candidate.resendOfShipmentId === parcel.shipmentId) ?? null;
  return { resendable, damaged, linkedResend };
}

function parcelSummary(
  groups: AdminOperationalDeliveryGroup[],
  detail: AdminOrderDetail,
  focusedPackageNumber?: string | null,
): string {
  const parcels = groups.flatMap((group) => parcelsOfGroup(group, detail));
  const unique = Array.from(new Map(parcels.map((parcel) => [parcel.shipmentId, parcel])).values());
  if (unique.length === 0) return "بدون مرسوله";
  // TB-261: a carrier return is NOT «در مسیر». Each physical group is counted in its own words so the
  // summary can never claim a parcel is still travelling after it came back to origin.
  const moving = unique.filter((parcel) => ["Dispatched", "InTransit"].includes(parcel.status)).length;
  const exceptions = unique.filter((parcel) =>
    ["NonDelivered", "Returning", "Returned"].includes(parcel.status)).length;
  const delivered = unique.filter((parcel) => parcel.status === "Delivered").length;
  const created = unique.length - moving - exceptions - delivered;
  const parts = [`${unique.length.toLocaleString("fa-IR")} مرسوله`];
  if (created > 0) parts.push(`${created.toLocaleString("fa-IR")} در انتظار ارسال`);
  if (moving > 0) parts.push(`${moving.toLocaleString("fa-IR")} در مسیر`);
  if (exceptions > 0) parts.push(`${exceptions.toLocaleString("fa-IR")} بازگشتی/در مبدأ`);
  if (delivered > 0) parts.push(`${delivered.toLocaleString("fa-IR")} تحویل‌شده`);
  return focusedPackageNumber ? `بستهٔ تجمیعی ${focusedPackageNumber} · ${parts.join(" · ")}` : parts.join(" · ");
}

type Readiness = Pick<OrderOperationsTargetFact, "readinessLabel" | "readinessTone">;

/** Truthful state of ONE physical parcel — a Created/tracked parcel is real work, not "nothing left". */
function parcelReadiness(parcel: AdminShipment, detail: AdminOrderDetail): Readiness {
  switch (parcel.status) {
    case "Created":
      return parcel.trackingReference
        ? { readinessLabel: "آمادهٔ ارسال به حامل", readinessTone: "ready" }
        : { readinessLabel: "در انتظار ثبت کد رهگیری", readinessTone: "ready" };
    case "Dispatched":
    case "InTransit":
      return { readinessLabel: "در مسیر تحویل — در انتظار ثبت تحویل", readinessTone: "ready" };
    case "NonDelivered":
      return { readinessLabel: "تحویل ناموفق — در انتظار بازگشت به مبدأ", readinessTone: "blocked" };
    case "Returning":
      return { readinessLabel: "در حال برگشت به مبدأ — در انتظار تأیید دریافت", readinessTone: "ready" };
    case "Returned": {
      // TB-261: a parcel physically back at origin still owns real work. Whether that is a resend or a
      // damaged-goods determination is an owning fact of ITS OWN receipt, never "no physical work left".
      const { resendable, damaged, linkedResend } = parcelReturnFacts(parcel, detail);
      if (resendable > 0) return { readinessLabel: "آمادهٔ ارسال مجدد", readinessTone: "ready" };
      if (linkedResend) return { readinessLabel: "ارسال مجدد ثبت شده — پیگیری مرسولهٔ جدید", readinessTone: "ready" };
      return damaged > 0
        ? { readinessLabel: "دریافت‌شده در مبدأ — اقلام آسیب‌دیده نیازمند تعیین تکلیف", readinessTone: "blocked" }
        : { readinessLabel: "دریافت‌شده در مبدأ — ظرفیت سالمی برای ارسال مجدد نمانده", readinessTone: "blocked" };
    }
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
    return parcelReadiness(focus.shipment, detail);
  }
  if (focus.packageId) {
    const fromPackage = packageReadiness(focus.packageId, detail);
    if (fromPackage) return fromPackage;
  }

  const parcels = parcelsOfGroup(group, detail);
  if (parcels.some((parcel) => ["NonDelivered", "Returning"].includes(parcel.status))) {
    const returning = parcels.find((parcel) => parcel.status === "Returning");
    return returning
      ? parcelReadiness(returning, detail)
      : { readinessLabel: "تحویل ناموفق — در انتظار بازگشت به مبدأ", readinessTone: "blocked" };
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
  if (created) return parcelReadiness(created, detail);

  const moving = parcels.find((parcel) => ["Dispatched", "InTransit"].includes(parcel.status));
  if (moving) return parcelReadiness(moving, detail);

  if (parcels.length > 0 && parcels.every((parcel) => parcel.status === "Delivered")) {
    return { readinessLabel: "تحویل شد", readinessTone: "done" };
  }

  /**
   * TB-261: goods physically received back at origin are real remaining work — a resend when own usable
   * capacity survives, otherwise an explicit damaged-goods determination. Zero fresh-creation remainder
   * must never be reported as «کار فیزیکی باقی نمانده».
   */
  const returned = parcels.filter((parcel) => parcel.status === "Returned");
  if (returned.length > 0 && returned.length === parcels.length) {
    return parcelReadiness(returned[0]!, detail);
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
        // TB-266: accept BOTH identities a locator may carry — the seller order's own fulfillment id, and the seller
        // order id itself (older queue links) — instead of only `fulfillmentId ?? id`.
        .filter((seller) => seller.fulfillmentId === fulfillmentId || (seller.fulfillmentId ?? seller.id) === fulfillmentId)
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
    // TB-266: a COARSE fulfillment-only locator (no group/parcel/package identity) that no longer matches any
    // seller order used to resolve to an empty target list, so a valid queue row could dead-end on
    // «گروه… پیدا نشد». A coarse hint is a hint: when it resolves nothing, the order's own operational groups are
    // still shown. A PRECISE locator that matches nothing keeps the honest not-found message.
    if (matched.length === 0 && fulfillmentId && !shippingGroupId && !shipmentId && !packageId) {
      return groups.map((group) => toTargetFact(group, detail, focus));
    }
    return matched.map((group) => toTargetFact(group, detail, focus));
  }, [detail, focus, fulfillmentId, packageId, shipmentId, shippingGroupId, targetSellerOrderIds]);

  /**
   * TB-266: a lawful queue row can point at a seller FULFILLMENT unit that has no Order-captured shipping group
   * yet (`workUnitKey = group:{fulfillmentId}`, `workUnitState = Ungrouped`). Such a row is exact, not stale, so
   * the target card names that one fulfillment unit — the same unit the work queue grouped — instead of the
   * «پیدا نشد» message. Nothing is invented: the summary comes from the seller order the locator matched, and a
   * locator that matches no seller order still resolves to nothing.
   */
  const sellerTargets = useMemo((): OrderOperationsTargetFact[] => {
    if (!detail || targets.length > 0) return [];
    // Only a COARSE fulfillment-only locator may fall back to the seller unit. A precise parcel/package/group
    // locator that resolved to nothing stays an honest not-found, never a different unit's card.
    if (!fulfillmentId || shippingGroupId || shipmentId || packageId) return [];
    if (!targetSellerOrderIds || targetSellerOrderIds.size === 0) return [];
    const delivery = (detail.shippingGroups ?? [])
      .filter((group) => targetSellerOrderIds.has(group.sellerOrderId))
      .map((group) => [group.deliveryDate, group.deliveryTimeWindow].filter(Boolean).join(" · "))
      .filter(Boolean)
      .join(" | ");
    return detail.sellerOrders
      .filter((seller) => targetSellerOrderIds.has(seller.id))
      .map((seller): OrderOperationsTargetFact => {
        const parcels = seller.shipments ?? [];
        const open = parcels.filter((parcel) => parcel.status !== "Delivered" && parcel.status !== "Returned");
        const state = seller.fulfillmentStatus || seller.status;
        return {
          groupKey: `fulfillment:${seller.fulfillmentId ?? seller.id}`,
          methodLabel: detail.shippingMethodLabel ?? "",
          customerDeliverySummary: delivery,
          plannedDispatchSummary: null,
          parcelSummary: parcels.length === 0
            ? "بدون مرسولهٔ ساخته‌شده"
            : parcels.length === open.length
              ? `${parcels.length.toLocaleString("fa-IR")} مرسوله — ${formatAdminShipmentStatus(open[0]!.status)}`
              : `${parcels.length.toLocaleString("fa-IR")} مرسوله — ${open.length.toLocaleString("fa-IR")} باز`,
          readinessLabel: formatAdminShipmentStatus(state),
          readinessTone: state === "Delivered" || state === "Cancelled"
            ? "done"
            : seller.status === "Paid"
              ? "ready"
              : "blocked",
        };
      });
  }, [detail, fulfillmentId, packageId, shipmentId, shippingGroupId, targetSellerOrderIds, targets.length]);

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
      errorMessage={result.message ? mapAdminErrorMessage(result.message, "fa") : null}
      onRetry={() => setReloadToken((value) => value + 1)}
      backHref={backFilter ? `/admin/fulfillments?filter=${encodeURIComponent(backFilter)}` : "/admin/fulfillments"}
      fullOrderHref={`/admin/orders/${checkoutId}`}
      recipientName={detail?.recipientName}
      recipientMobile={detail?.contactMobile}
      destinationLine={destinationLine}
      methodLabel={detail?.shippingMethodLabel}
      targetRequested={targetRequested}
      targets={targets.length > 0 ? targets : sellerTargets}
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
