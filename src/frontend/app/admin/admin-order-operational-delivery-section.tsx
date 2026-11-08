"use client";

import React from "react";
import { Package } from "lucide-react";
import {
  formatAdminMoney,
  type AdminAcceptedShippingGroup,
  type AdminOperationalDeliveryGroup,
  type AdminOrderDetail,
  type AdminOrderLine,
  type AdminSellerOrder,
  type AdminShipment,
} from "./admin-api";
import { formatJalaliDate } from "../../design-system/app-data-grid/jalali";

export type OperationalCreateTarget = {
  seller: AdminSellerOrder;
  group: AdminAcceptedShippingGroup;
};

type Props = {
  detail: AdminOrderDetail;
  pendingCode: string | null;
  onCreateShipment: (target: OperationalCreateTarget) => void;
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

/**
 * TB-253: primary «ارسال سفارش» cards from server-owned operationalDeliveryGroups.
 * Does not regroup client-side; only renders the projection.
 */
export function AdminOrderOperationalDeliverySection({
  detail,
  pendingCode,
  onCreateShipment,
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
      <div className="space-y-3">
        {groups.map((group) => (
          <OperationalCard
            key={group.operationalGroupKey}
            group={group}
            detail={detail}
            pendingCode={pendingCode}
            onCreateShipment={onCreateShipment}
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
  selectedBySeller,
  qtyByLine,
  shippableQty,
}: {
  group: AdminOperationalDeliveryGroup;
  detail: AdminOrderDetail;
  pendingCode: string | null;
  onCreateShipment: (target: OperationalCreateTarget) => void;
  selectedBySeller: Record<string, string[]>;
  qtyByLine: Record<string, number>;
  shippableQty: (line: AdminOrderLine) => number;
}) {
  const dateRaw = (group.deliveryDate ?? "").trim();
  const dateFa = dateRaw
    ? formatJalaliDate(dateRaw.includes("T") ? dateRaw : `${dateRaw}T12:00:00`, "fa")
    : null;
  const timeWindow = (group.deliveryTimeWindow ?? "").trim();
  const showFeeBreakdown = group.feeBreakdown.length > 1;

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
              جمع تعهدهای این گروه
            </span>
          ) : null}
        </div>
      </div>

      {showFeeBreakdown ? (
        <ul className="mt-2 space-y-0.5 rounded-lg bg-white/70 px-2 py-1.5 text-[10px] text-gray-600" data-testid={`admin-order-operational-fee-breakdown-${group.operationalGroupKey}`}>
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
        {group.members.map((member) => {
          const seller = resolveSeller(detail, member.sellerOrderId);
          const accepted = resolveAcceptedGroup(detail, member.shippingGroupId);
          const lines = seller ? memberLines(seller, member.orderLineIds) : [];
          const shipments = seller ? memberShipments(seller, member.shippingGroupId) : [];
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
            && selectionOk;

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
              <ul className="mt-1.5 space-y-1">
                {lines.map((line) => {
                  const key = lineKey(line);
                  const remaining = shippableQty(line);
                  const selectedQty = selected.includes(key)
                    ? Math.min(qtyByLine[key] ?? remaining, remaining)
                    : remaining;
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
                      <span className="tabular-nums text-gray-500">
                        باقی {selectedQty.toLocaleString("fa-IR")}
                      </span>
                    </li>
                  );
                })}
              </ul>
              {shipments.length > 0 ? (
                <p className="mt-1.5 text-[10px] text-gray-500">
                  مرسوله: {shipments.length.toLocaleString("fa-IR")}
                  {shipments[0]?.trackingReference
                    ? ` · ${shipments[0].trackingReference}`
                    : ""}
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
                  + ایجاد مرسوله
                </button>
              ) : (
                <p className="mt-2 text-[10px] text-amber-800" role="status">
                  {member.createParcelBlockReasonFa
                    ?? (!selectionOk
                      ? "انتخاب فعلی شامل اقلام خارج از این گروه است."
                      : "ایجاد مرسوله برای این بخش در دسترس نیست.")}
                </p>
              )}
            </div>
          );
        })}
      </div>

      {group.isCrossSellerMarketplace && group.consolidateBlockReasonFa ? (
        <p className="mt-2 text-[10px] text-gray-500" data-testid={`admin-order-operational-consolidate-note-${group.operationalGroupKey}`}>
          {group.consolidateBlockReasonFa}
        </p>
      ) : null}
    </article>
  );
}
