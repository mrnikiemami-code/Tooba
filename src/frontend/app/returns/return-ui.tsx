"use client";

import { RotateCcw, X } from "lucide-react";
import { useEffect, useState } from "react";
import type { FulfillmentItem } from "../fulfillment/fulfillment-api.ts";
import {
  createCustomerReturn,
  formatReturnDate,
  formatReturnStatus,
  formatRefundAttemptStatus,
  returnStatusBadgeClass,
  type ReturnSnapshot,
} from "./return-api.ts";

/** badge وضعیت مرجوعی. */
export function ReturnStatusBadge({ status }: { status: string }) {
  return (
    <span className={`inline-flex items-center gap-1 rounded-full px-3 py-1 text-[10px] font-medium ${returnStatusBadgeClass(status)}`}>
      {formatReturnStatus(status)}
    </span>
  );
}

/** جزئیات مرجوعی — card مطابق الگوی orderDetail Shopeiva. */
export function ReturnDetailCard({ snapshot }: { snapshot: ReturnSnapshot }) {
  return (
    <div className="rounded-2xl border border-gray-100 bg-white p-4 md:p-6 shadow-sm space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <p className="text-xs text-gray-400">شناسه مرجوعی</p>
          <p className="font-mono font-bold text-sm mt-1">{snapshot.returnRequestId.slice(0, 8)}</p>
        </div>
        <ReturnStatusBadge status={snapshot.status} />
      </div>
      {snapshot.reason ? (
        <div className="bg-gray-50 rounded-xl p-4 text-sm text-gray-700">
          <strong className="block text-gray-900 mb-1">دلیل مشتری</strong>
          {snapshot.reason}
        </div>
      ) : null}
      <ul className="divide-y divide-gray-100 rounded-xl border border-gray-100 overflow-hidden">
        {snapshot.items.map((item) => (
          <li key={item.returnItemId} className="p-3 flex items-center justify-between text-sm hover:bg-gray-50 transition-colors">
            <span className="text-gray-600">خط {item.orderLineId.slice(0, 8)}</span>
            <span className="font-bold">× {item.quantity.toLocaleString("fa-IR")}</span>
          </li>
        ))}
      </ul>
      {snapshot.refundAmount > 0 ? (
        <p className="text-sm">
          مبلغ بازپرداخت: <strong className="text-[#2563EB]">{snapshot.refundAmount.toLocaleString("fa-IR")} {snapshot.currency}</strong>
        </p>
      ) : null}
      {snapshot.refundAttempts.length > 0 ? (
        <div className="space-y-2">
          <h4 className="text-sm font-bold">تلاش‌های بازپرداخت</h4>
          {snapshot.refundAttempts.map((attempt) => (
            <div key={attempt.refundAttemptId} className="rounded-xl bg-gray-50 p-3 text-xs text-gray-600 flex flex-wrap gap-2 justify-between">
              <span>{formatRefundAttemptStatus(attempt.status)}</span>
              <span>{formatReturnDate(attempt.completedAt ?? attempt.createdAt)}</span>
              {attempt.failureCode ? <span className="text-red-600">{attempt.failureCode}</span> : null}
            </div>
          ))}
        </div>
      ) : null}
      <p className="text-xs text-gray-400">ثبت: {formatReturnDate(snapshot.createdAt)}</p>
    </div>
  );
}

export interface ReturnFormLine {
  orderLineId: string;
  label: string;
  maxQuantity: number;
  quantity: number;
}

/**
 * مودال درخواست مرجوعی — ساختار returnFormModal Shopeiva
 * (fixed overlay + rounded-2xl card + transition-opacity).
 */
export function ReturnFormModal({
  open,
  onClose,
  sellerOrderId,
  fulfillmentItems,
  onSubmitted,
}: {
  open: boolean;
  onClose: () => void;
  sellerOrderId: string;
  fulfillmentItems: FulfillmentItem[];
  onSubmitted?: (snapshot: ReturnSnapshot) => void;
}) {
  const [reason, setReason] = useState("");
  const [lines, setLines] = useState<ReturnFormLine[]>([]);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) return;
    setLines(
      fulfillmentItems
        .filter((item) => item.quantityShipped > 0)
        .map((item) => ({
          orderLineId: item.orderLineId,
          label: `خط ${item.orderLineId.slice(0, 8)}`,
          maxQuantity: item.quantityShipped,
          quantity: 0,
        })),
    );
    setReason("");
    setError(null);
  }, [open, fulfillmentItems]);

  if (!open) return null;

  const selected = lines.filter((line) => line.quantity > 0);

  async function submit() {
    if (selected.length === 0) {
      setError("حداقل یک قلم با تعداد بیشتر از صفر انتخاب کنید.");
      return;
    }
    setSubmitting(true);
    setError(null);
    const result = await createCustomerReturn({
      sellerOrderId,
      reason: reason.trim() || undefined,
      items: selected.map((line) => ({ orderLineId: line.orderLineId, quantity: line.quantity })),
    });
    setSubmitting(false);
    if (!result.ok) {
      setError(result.errorCode);
      return;
    }
    onSubmitted?.(result.snapshot);
    onClose();
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40 transition-opacity">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between p-4 border-b border-gray-100">
          <h2 className="font-black flex items-center gap-2">
            <RotateCcw className="w-5 h-5 text-[#2563EB]" />
            درخواست مرجوعی
          </h2>
          <button type="button" onClick={onClose} className="p-2 rounded-xl hover:bg-gray-100 transition-colors" aria-label="بستن">
            <X className="w-5 h-5" />
          </button>
        </div>
        <div className="p-4 space-y-4">
          {lines.length === 0 ? (
            <p className="text-sm text-gray-500">قلمی برای مرجوعی واجد شرایط نیست.</p>
          ) : (
            <ul className="space-y-2">
              {lines.map((line) => (
                <li key={line.orderLineId} className="flex items-center gap-3 rounded-xl border border-gray-100 p-3">
                  <span className="flex-1 text-sm font-medium">{line.label}</span>
                  <input
                    type="number"
                    min={0}
                    max={line.maxQuantity}
                    value={line.quantity}
                    onChange={(event) => {
                      const qty = Math.min(line.maxQuantity, Math.max(0, Number(event.target.value) || 0));
                      setLines((prev) => prev.map((row) => (row.orderLineId === line.orderLineId ? { ...row, quantity: qty } : row)));
                    }}
                    className="w-20 rounded-xl border border-gray-200 px-3 py-2 text-sm text-center"
                  />
                  <span className="text-xs text-gray-400">از {line.maxQuantity.toLocaleString("fa-IR")}</span>
                </li>
              ))}
            </ul>
          )}
          <label className="block text-sm">
            <span className="font-bold text-gray-700">دلیل مرجوعی</span>
            <textarea
              value={reason}
              onChange={(event) => setReason(event.target.value)}
              rows={3}
              className="mt-2 w-full rounded-xl border border-gray-200 p-3 text-sm"
              placeholder="توضیح کوتاه..."
            />
          </label>
          {error ? <p className="text-sm text-red-600">{error}</p> : null}
        </div>
        <div className="p-4 border-t border-gray-100 flex gap-2 justify-end">
          <button type="button" onClick={onClose} className="rounded-xl px-4 py-2 text-sm font-bold bg-gray-100 hover:bg-gray-200 transition-colors">
            انصراف
          </button>
          <button
            type="button"
            disabled={submitting || lines.length === 0}
            onClick={() => void submit()}
            className="rounded-xl px-4 py-2 text-sm font-bold bg-[#2563EB] text-white hover:bg-blue-700 transition-colors disabled:opacity-50"
          >
            {submitting ? "در حال ثبت..." : "ثبت درخواست"}
          </button>
        </div>
      </div>
    </div>
  );
}
