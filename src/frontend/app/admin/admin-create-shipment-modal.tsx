"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { Button } from "../../design-system/primitives/core";
import { Dialog } from "../../design-system/primitives/overlays";
import { formatJalaliDate } from "../../design-system/app-data-grid/jalali";
import { mapAdminErrorMessage } from "./admin-error-map";
import { executeAdminOrderOperation } from "./admin-order-operations";
import type {
  AdminAcceptedShippingGroup,
  AdminOrderDetail,
  AdminOrderLine,
  AdminSellerOrder,
} from "./admin-api";

export type CreateShipmentLineSelection = {
  line: AdminOrderLine;
  quantity: number;
};

function formatAdminMoney(amount: number, currency = "IRR"): string {
  try {
    return new Intl.NumberFormat("fa-IR", {
      style: "currency",
      currency,
      maximumFractionDigits: 0,
    }).format(amount);
  } catch {
    return `${amount.toLocaleString("fa-IR")} ${currency}`;
  }
}

function formatDeliveryDateFa(isoDate: string | null | undefined): string | null {
  if (!isoDate || !String(isoDate).trim()) return null;
  const raw = String(isoDate).trim();
  const withTime = raw.includes("T") ? raw : `${raw}T12:00:00`;
  const jalali = formatJalaliDate(withTime, "fa");
  try {
    const d = new Date(withTime);
    if (!Number.isNaN(d.getTime())) {
      const weekday = new Intl.DateTimeFormat("fa-IR", { weekday: "short" }).format(d);
      return `${weekday} ${jalali}`;
    }
  } catch {
    /* keep jalali only */
  }
  return jalali;
}

function formatFee(group: AdminAcceptedShippingGroup): string {
  const amount =
    group.payableAtCheckoutAmount != null
      ? group.payableAtCheckoutAmount
      : group.shippingChargeSnapshot;
  return formatAdminMoney(amount ?? 0, group.currency || "IRR");
}

type Props = {
  open: boolean;
  onClose: () => void;
  checkoutId: string;
  sellerOrder: AdminSellerOrder;
  fulfillmentId: string | null;
  shippingGroup: AdminAcceptedShippingGroup;
  selectedLines: CreateShipmentLineSelection[];
  orderDetail?: AdminOrderDetail | null;
  onCompleted?: () => void;
  /** Test seam — defaults to executeAdminOrderOperation. */
  operationExecutor?: typeof executeAdminOrderOperation;
};

function remainingShippable(line: AdminOrderLine): number {
  const packed = line.quantityPacked ?? 0;
  const allocated = line.quantityAllocated ?? line.quantityShipped ?? 0;
  return Math.max(0, packed - allocated);
}

function addressSummary(detail: AdminOrderDetail | null | undefined): string {
  if (!detail) return "";
  return [detail.provinceName, detail.cityName, detail.postalAddress, detail.postalCode]
    .filter((x) => x && String(x).trim())
    .join("، ");
}

/**
 * مودال ایجاد مرسوله از یک گروه ارسال پذیرفته‌شده — روش/زمان فقط‌خواندنی.
 * TB-252-R2: compact localized copy; trusted server metadata (no client ProviderMetadataJson); one inline error.
 */
export function AdminCreateShipmentModal({
  open,
  onClose,
  checkoutId,
  sellerOrder,
  fulfillmentId,
  shippingGroup,
  selectedLines,
  orderDetail,
  onCompleted,
  operationExecutor = executeAdminOrderOperation,
}: Props) {
  const [lineQty, setLineQty] = useState<Record<string, number>>({});
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const inFlightRef = useRef(false);
  const operationExecutorRef = useRef(operationExecutor);
  operationExecutorRef.current = operationExecutor;

  const groupLineIdSet = useMemo(
    () => new Set(shippingGroup.orderLineIds),
    [shippingGroup.orderLineIds],
  );

  const groupLines = useMemo(
    () =>
      sellerOrder.lines
        .filter((line) => groupLineIdSet.has(line.orderLineId || line.id))
        .map((line) => ({ line, max: remainingShippable(line) }))
        .filter((x) => x.max > 0),
    [sellerOrder.lines, groupLineIdSet],
  );

  const presetByLine = useMemo(() => {
    const map = new Map<string, number>();
    for (const sel of selectedLines) {
      const id = sel.line.orderLineId || sel.line.id;
      if (!groupLineIdSet.has(id)) continue;
      map.set(id, sel.quantity);
    }
    return map;
  }, [selectedLines, groupLineIdSet]);

  useEffect(() => {
    if (!open) return;
    const next: Record<string, number> = {};
    for (const { line, max } of groupLines) {
      const id = line.orderLineId || line.id;
      const preset = presetByLine.get(id);
      next[id] = Math.min(preset != null && preset > 0 ? preset : max, max);
    }
    setLineQty(next);
    setError(null);
    inFlightRef.current = false;
    setPending(false);
  }, [open, groupLines, presetByLine]);

  const selections = useMemo(
    () =>
      groupLines
        .map(({ line, max }) => {
          const id = line.orderLineId || line.id;
          const quantity = Math.min(Math.max(0, lineQty[id] ?? 0), max);
          return { orderLineId: id, quantity, title: line.title };
        })
        .filter((x) => x.quantity > 0),
    [groupLines, lineQty],
  );

  const canSubmit =
    Boolean(fulfillmentId)
    && shippingGroup.createParcelEligible
    && shippingGroup.transportMode !== "CarrierApi"
    && selections.length > 0
    && !pending;

  async function submit() {
    if (!fulfillmentId || !canSubmit) return;
    if (inFlightRef.current) return;
    inFlightRef.current = true;
    setPending(true);
    setError(null);
    try {
      const result = await operationExecutorRef.current(checkoutId, {
        code: "create_shipment",
        sellerOrderId: sellerOrder.id,
        fulfillmentId,
        shippingGroupId: shippingGroup.shippingGroupId,
        carrierDisplayName: shippingGroup.methodLabel || shippingGroup.methodCode,
        shippingMethodCode: shippingGroup.methodCode,
        selections: selections.map((s) => ({
          orderLineId: s.orderLineId,
          quantity: s.quantity,
        })),
      });
      if (result.state !== "ok") {
        const fa = mapAdminErrorMessage(result.message ?? "order.operation.failed", "fa");
        setError(fa);
        return;
      }
      onClose();
      onCompleted?.();
    } catch (thrown) {
      const fa = mapAdminErrorMessage(
        thrown instanceof Error && thrown.message ? thrown.message : "order.operation.failed",
        "fa",
      );
      setError(fa);
    } finally {
      inFlightRef.current = false;
      setPending(false);
    }
  }

  const deliveryDateFa = formatDeliveryDateFa(shippingGroup.deliveryDate);
  const timeWindow = (shippingGroup.deliveryTimeWindow ?? "").trim();

  return (
    <Dialog title="ایجاد مرسوله" open={open} onClose={onClose} showCloseButton={false} size="lg">
      <div className="flex max-h-[min(82vh,40rem)] flex-col text-sm" data-testid="admin-create-shipment-modal">
        <div className="min-h-0 flex-1 space-y-3 overflow-y-auto pe-1">
          <div data-testid="admin-create-shipment-accepted-summary" className="space-y-1 text-xs text-gray-700">
            <p className="text-sm font-bold text-gray-900">
              {shippingGroup.methodLabel || shippingGroup.methodCode}
              {sellerOrder.sellerDisplayName ? (
                <span className="ms-2 font-normal text-gray-600">· {sellerOrder.sellerDisplayName}</span>
              ) : null}
            </p>
            <p>
              {deliveryDateFa ?? "—"}
              {timeWindow ? (
                <>
                  {" · "}
                  <bdi dir="ltr">{timeWindow}</bdi>
                </>
              ) : null}
              {" · "}
              {formatFee(shippingGroup)}
            </p>
          </div>

          {orderDetail ? (
            <div className="space-y-0.5 text-xs text-gray-700" data-testid="admin-create-shipment-recipient">
              <p>
                <strong>{orderDetail.recipientName || "—"}</strong>
                {orderDetail.contactMobile ? (
                  <>
                    {" · "}
                    <bdi dir="ltr">{orderDetail.contactMobile}</bdi>
                  </>
                ) : null}
              </p>
              <p>{addressSummary(orderDetail) || "آدرس ثبت‌شده ندارد"}</p>
            </div>
          ) : null}

          <div>
            {groupLines.length === 0 ? (
              <p className="text-xs text-gray-500">قلم قابل ارسال باقی نمانده است.</p>
            ) : (
              <ul className="divide-y divide-gray-100 rounded-lg border border-gray-200">
                <li className="grid grid-cols-[1fr_auto] gap-2 bg-gray-50 px-3 py-1.5 text-[11px] font-bold text-gray-600">
                  <span>قلم</span>
                  <span>تعداد</span>
                </li>
                {groupLines.map(({ line, max }) => {
                  const id = line.orderLineId || line.id;
                  return (
                    <li key={id} className="grid grid-cols-[1fr_auto] items-center gap-2 px-3 py-2 text-xs">
                      <span className="min-w-0 truncate">{line.title}</span>
                      <label className="inline-flex items-center gap-1 shrink-0">
                        <span className="text-gray-400">/{max.toLocaleString("fa-IR")}</span>
                        <input
                          type="number"
                          min={0}
                          max={max}
                          step="any"
                          value={lineQty[id] ?? 0}
                          onChange={(e) => {
                            const n = Number(e.target.value);
                            setLineQty((prev) => ({
                              ...prev,
                              [id]: Number.isFinite(n) ? Math.min(Math.max(0, n), max) : 0,
                            }));
                          }}
                          className="w-16 rounded border border-gray-200 px-2 py-1 text-end tabular-nums"
                          data-testid={`admin-create-shipment-qty-${id}`}
                        />
                      </label>
                    </li>
                  );
                })}
              </ul>
            )}
          </div>

          {!shippingGroup.createParcelEligible && shippingGroup.createParcelBlockReasonFa ? (
            <p className="rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-900" role="status">
              {shippingGroup.createParcelBlockReasonFa}
            </p>
          ) : null}

          {error ? (
            <p
              className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-xs text-red-700"
              role="alert"
              data-testid="admin-create-shipment-error"
            >
              {error}
            </p>
          ) : null}
        </div>

        <div className="mt-3 flex items-center justify-end gap-2 border-t border-gray-100 pt-3">
          <Button type="button" tone="secondary" onClick={onClose} disabled={pending}>
            انصراف
          </Button>
          <Button
            type="button"
            onClick={() => void submit()}
            disabled={!canSubmit}
            data-testid="admin-create-shipment-submit"
          >
            {pending ? "در حال ایجاد…" : "ایجاد مرسوله"}
          </Button>
        </div>
      </div>
    </Dialog>
  );
}
