"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { MapPin, Package, Truck } from "lucide-react";
import { toast } from "react-toastify";
import { Button } from "../../design-system/primitives/core";
import { Dialog } from "../../design-system/primitives/overlays";
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

function ownerLabel(group: AdminAcceptedShippingGroup): string | null {
  const kind = (group.ownerKind ?? "").trim().toLowerCase();
  if (!kind) return null;
  if (kind === "store") return group.ownerTenantKey ? `مسئولیت: فروشگاه (${group.ownerTenantKey})` : "مسئولیت: فروشگاه";
  if (kind === "marketplace") return "مسئولیت: مارکت‌پلیس";
  if (kind === "seller") return "مسئولیت: فروشنده";
  return `مسئولیت: ${group.ownerKind}`;
}

/**
 * مودال ایجاد مرسوله از یک گروه ارسال پذیرفته‌شده — روش/زمان فقط‌خواندنی.
 * TB-252-R1: synchronous in-flight gate + try/finally; no providerMetadataJson.
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
        toast.error(fa);
        return;
      }
      toast.success("عملیات ایجاد مرسوله انجام شد.");
      onClose();
      onCompleted?.();
    } catch (thrown) {
      const fa = mapAdminErrorMessage(
        thrown instanceof Error && thrown.message ? thrown.message : "order.operation.failed",
        "fa",
      );
      setError(fa);
      toast.error(fa);
    } finally {
      inFlightRef.current = false;
      setPending(false);
    }
  }

  const responsibility = ownerLabel(shippingGroup);
  const deliveryBits = [
    shippingGroup.deliveryDate,
    shippingGroup.deliveryTimeWindow,
    shippingGroup.deliveryTimeZone,
  ].filter((x) => x && String(x).trim());

  return (
    <Dialog title="ایجاد مرسوله" open={open} onClose={onClose} showCloseButton={false} size="lg">
      <div className="flex max-h-[min(82vh,40rem)] flex-col text-sm" data-testid="admin-create-shipment-modal">
        <div className="min-h-0 flex-1 space-y-3 overflow-y-auto pe-1">
          <div className="flex items-start gap-3 rounded-2xl border border-blue-100 bg-gradient-to-l from-blue-50 to-white p-3">
            <span className="inline-flex size-11 items-center justify-center rounded-2xl bg-blue-600 text-white shadow-sm">
              <Truck className="size-5" aria-hidden />
            </span>
            <div>
              <p className="text-base font-black text-gray-900">ایجاد مرسوله از گروه پذیرفته‌شده</p>
              <p className="mt-0.5 text-xs text-gray-600">
                فروشنده: <strong>{sellerOrder.sellerDisplayName}</strong>
              </p>
            </div>
          </div>

          <div
            className="rounded-2xl border border-indigo-100 bg-indigo-50/40 p-3"
            data-testid="admin-create-shipment-accepted-summary"
          >
            <p className="text-xs font-black text-indigo-950">تعهد ارسال (فقط خواندنی)</p>
            <p className="mt-1 text-sm font-bold text-gray-900">{shippingGroup.methodLabel || shippingGroup.methodCode}</p>
            {responsibility ? <p className="mt-1 text-xs text-gray-600">{responsibility}</p> : null}
            <p className="mt-1 text-xs text-gray-700">
              زمان تحویل:{" "}
              <strong>{deliveryBits.length > 0 ? deliveryBits.join(" · ") : "—"}</strong>
            </p>
            <p className="mt-1 text-xs text-gray-700">
              هزینه گروه:{" "}
              <strong>
                {formatAdminMoney(shippingGroup.payableAtCheckoutAmount || shippingGroup.shippingChargeSnapshot, shippingGroup.currency)}
              </strong>
            </p>
          </div>

          {orderDetail ? (
            <div className="rounded-2xl border border-emerald-100 bg-emerald-50/50 p-3">
              <div className="flex items-start gap-2">
                <span className="inline-flex size-8 items-center justify-center rounded-xl bg-emerald-100 text-emerald-700 ring-1 ring-emerald-200">
                  <MapPin className="size-4" aria-hidden />
                </span>
                <div className="text-xs text-gray-700">
                  <p>
                    گیرنده: <strong>{orderDetail.recipientName || "—"}</strong>
                    {orderDetail.contactMobile ? ` · ${orderDetail.contactMobile}` : ""}
                  </p>
                  <p className="mt-1">{addressSummary(orderDetail) || "آدرس ثبت‌شده ندارد"}</p>
                </div>
              </div>
            </div>
          ) : null}

          <div className="rounded-2xl border border-amber-100 bg-amber-50/40 p-3">
            <div className="mb-2 flex items-center gap-2">
              <span className="inline-flex size-8 items-center justify-center rounded-xl bg-amber-100 text-amber-700 ring-1 ring-amber-200">
                <Package className="size-4" aria-hidden />
              </span>
              <p className="text-xs font-black text-gray-800">اقلام همین گروه</p>
            </div>
            {groupLines.length === 0 ? (
              <p className="text-xs text-gray-500">قلم قابل ارسال باقی نمانده است.</p>
            ) : (
              <ul className="space-y-2">
                {groupLines.map(({ line, max }) => {
                  const id = line.orderLineId || line.id;
                  return (
                    <li key={id} className="flex items-center justify-between gap-2 text-xs">
                      <span className="min-w-0 flex-1 truncate">{line.title}</span>
                      <label className="inline-flex items-center gap-1 shrink-0">
                        <span className="text-gray-500">حداکثر {max.toLocaleString("fa-IR")}</span>
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
                          className="w-20 rounded-lg border border-gray-200 px-2 py-1 text-end tabular-nums"
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
            <p className="rounded-xl border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-900" role="status">
              {shippingGroup.createParcelBlockReasonFa}
            </p>
          ) : null}

          {error ? (
            <p
              className="rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-xs text-red-700"
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
