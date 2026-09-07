"use client";

import { useMemo, useState } from "react";
import { toast } from "react-toastify";
import { Button, Dialog } from "../../design-system";
import { mapAdminErrorMessage } from "./admin-error-map";
import { executeAdminOrderOperation } from "./admin-order-operations";
import type { AdminOrderLine, AdminSellerOrder } from "./admin-api";

const CARRIER_OPTIONS = ["پست", "تیپاکس", "پیک موتوری", "سایر"] as const;

export type CreateShipmentLineSelection = {
  line: AdminOrderLine;
  quantity: number;
};

type Props = {
  open: boolean;
  onClose: () => void;
  checkoutId: string;
  sellerOrder: AdminSellerOrder;
  fulfillmentId: string | null;
  /** وقتی خالی باشد یعنی کل اقلام باقی‌مانده بسته‌بندی‌شده و تخصیص‌نشده. */
  selectedLines: CreateShipmentLineSelection[];
  onCompleted?: () => void;
};

function remainingShippable(line: AdminOrderLine): number {
  const packed = line.quantityPacked ?? 0;
  const allocated = line.quantityAllocated ?? line.quantityShipped ?? 0;
  return Math.max(0, packed - allocated);
}

/**
 * مودال ایجاد مرسوله با Dialog دیزاین‌سیستم — بدون prompt مرورگر.
 * انتخاب جزئی خط/تعداد به Host ارسال می‌شود.
 */
export function AdminCreateShipmentModal({
  open,
  onClose,
  checkoutId,
  sellerOrder,
  fulfillmentId,
  selectedLines,
  onCompleted,
}: Props) {
  const [carrier, setCarrier] = useState<string>(CARRIER_OPTIONS[0]);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const remainingLines = useMemo(
    () =>
      sellerOrder.lines
        .map((line) => ({ line, quantity: remainingShippable(line) }))
        .filter((x) => x.quantity > 0),
    [sellerOrder.lines],
  );

  const displayLines = selectedLines.length > 0 ? selectedLines : remainingLines;

  const canSubmit = Boolean(fulfillmentId) && displayLines.length > 0 && carrier.trim().length > 0;

  async function submit() {
    if (!canSubmit || !fulfillmentId) return;
    setPending(true);
    setError(null);
    const selections =
      selectedLines.length > 0
        ? selectedLines.map((x) => ({
            orderLineId: x.line.orderLineId || x.line.id,
            quantity: x.quantity,
          }))
        : null;
    const result = await executeAdminOrderOperation(checkoutId, {
      code: "create_shipment",
      sellerOrderId: sellerOrder.id,
      fulfillmentId,
      carrierDisplayName: carrier.trim(),
      selections,
    });
    setPending(false);
    if (result.state !== "ok") {
      const raw = result.message ?? "order.operation.failed";
      const fa = /^[a-z0-9._-]+$/i.test(raw) ? mapAdminErrorMessage(raw, "fa") : raw;
      setError(fa);
      toast.error(fa);
      return;
    }
    toast.success("عملیات ایجاد مرسوله انجام شد.");
    onClose();
    onCompleted?.();
  }

  return (
    <Dialog title="ایجاد مرسوله" open={open} onClose={onClose} showCloseButton={false}>
      <div className="space-y-3 text-sm" data-testid="admin-create-shipment-modal">
        <p>
          فروشنده: <strong>{sellerOrder.sellerDisplayName}</strong>
        </p>
        <div className="rounded-lg border border-gray-200 bg-gray-50/60 p-2">
          <p className="mb-1 text-xs font-bold text-gray-600">اقلام انتخاب‌شده</p>
          {displayLines.length === 0 ? (
            <p className="text-xs text-gray-500">قلم قابل ارسال باقی نمانده است.</p>
          ) : (
            <ul className="divide-y divide-gray-200">
              {displayLines.map(({ line, quantity }) => (
                <li key={line.orderLineId ?? line.id} className="flex justify-between gap-2 py-1.5 text-xs">
                  <span className="truncate">{line.title}</span>
                  <span className="shrink-0 tabular-nums text-gray-600">× {quantity.toLocaleString("fa-IR")}</span>
                </li>
              ))}
            </ul>
          )}
        </div>
        <label className="block space-y-1">
          <span className="text-xs font-bold text-gray-600">روش / حمل‌کننده</span>
          <select
            value={carrier}
            onChange={(e) => setCarrier(e.target.value)}
            className="w-full rounded-lg border border-gray-200 bg-white px-3 py-2 text-sm"
            data-testid="admin-create-shipment-carrier"
          >
            {CARRIER_OPTIONS.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
        </label>
        {!fulfillmentId ? (
          <p className="rounded-lg bg-amber-50 px-3 py-2 text-xs text-amber-800">
            برای این فروشنده fulfillment ثبت نشده است؛ ایجاد مرسوله ممکن نیست.
          </p>
        ) : null}
        {error ? <p className="text-xs text-danger">{error}</p> : null}
        <div className="flex justify-end gap-2 pt-1">
          <Button type="button" tone="secondary" onClick={onClose} disabled={pending}>
            انصراف
          </Button>
          <Button
            type="button"
            tone="primary"
            disabled={!canSubmit || pending}
            onClick={() => void submit()}
            data-testid="admin-create-shipment-submit"
          >
            ایجاد مرسوله
          </Button>
        </div>
      </div>
    </Dialog>
  );
}
