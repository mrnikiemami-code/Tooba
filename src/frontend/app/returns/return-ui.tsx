"use client";

import {
  AlertTriangle,
  Calendar,
  CheckCircle,
  CreditCard,
  MessageSquare,
  Package,
  User,
  Wallet,
  X,
  XCircle,
} from "lucide-react";
import { useEffect, useState } from "react";
import type { FulfillmentItem } from "../fulfillment/fulfillment-api.ts";
import {
  createCustomerReturn,
  DEFAULT_REFUND_DESTINATION,
  formatRefundDestination,
  formatReturnDate,
  formatReturnStatus,
  formatRefundAttemptStatus,
  returnStatusBadgeClass,
  sellerApproveReturn,
  sellerRejectReturn,
  type RefundDestination,
  type ReturnSnapshot,
} from "./return-api.ts";
import { readSellerPartyId } from "../vendor-panel/seller-api.ts";

/** دلایل مرجوعی مطابق returnFormModal.jsx Shopeiva. */
export const RETURN_REASONS = [
  { id: "defective", label: "مشکل فنی یا خرابی کالا" },
  { id: "wrong", label: "تغییر نظر / عدم نیاز" },
  { id: "not_match", label: "مغایرت با مشخصات اعلامی" },
  { id: "damaged", label: "آسیب در حمل و نقل" },
  { id: "other", label: "سایر موارد" },
] as const;

/** badge وضعیت مرجوعی. */
export function ReturnStatusBadge({ status }: { status: string }) {
  return (
    <span className={`inline-flex items-center gap-1 rounded-full px-3 py-1 text-[10px] font-medium ${returnStatusBadgeClass(status)}`}>
      {formatReturnStatus(status)}
    </span>
  );
}

/** جزئیات مرجوعی — card مطابق returnDetailModal Shopeiva (read-only). */
export function ReturnDetailCard({ snapshot }: { snapshot: ReturnSnapshot }) {
  const [reasonLabel, description] = splitReturnReason(snapshot.reason);

  return (
    <div className="rounded-2xl border border-gray-100 bg-white p-4 md:p-6 shadow-sm space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <ReturnStatusBadge status={snapshot.status} />
      </div>
      <div className="grid grid-cols-2 gap-3">
        <div className="bg-gray-50 rounded-xl p-3">
          <p className="text-[10px] text-gray-500">تاریخ درخواست</p>
          <p className="text-sm font-bold text-gray-900 flex items-center gap-1 mt-1">
            <Calendar className="w-3.5 h-3.5 text-[#2563EB]" />
            {formatReturnDate(snapshot.createdAt)}
          </p>
        </div>
        <div className="bg-gray-50 rounded-xl p-3">
          <p className="text-[10px] text-gray-500">شناسه</p>
          <p className="text-sm font-bold font-mono mt-1">{snapshot.returnRequestId.slice(0, 8)}</p>
        </div>
      </div>
      {reasonLabel ? (
        <div>
          <h4 className="text-sm font-bold text-gray-900 mb-2 flex items-center gap-1">
            <AlertTriangle className="w-4 h-4 text-[#2563EB]" />
            دلیل مرجوعی
          </h4>
          <div className="bg-gray-50 rounded-xl p-3">
            <p className="text-sm text-gray-700 font-medium">{reasonLabel}</p>
          </div>
        </div>
      ) : null}
      {description ? (
        <div>
          <h4 className="text-sm font-bold text-gray-900 mb-2 flex items-center gap-1">
            <MessageSquare className="w-4 h-4 text-[#2563EB]" />
            توضیحات مشتری
          </h4>
          <div className="bg-gray-50 rounded-xl p-3">
            <p className="text-sm text-gray-600">{description}</p>
          </div>
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
      <div className="bg-gray-50 rounded-xl p-3" data-testid="return-destination-display">
        <p className="text-[10px] text-gray-500">مقصد بازپرداخت</p>
        <p className="text-sm font-bold text-gray-900 mt-1 flex items-center gap-1.5">
          {snapshot.destination === "Wallet" ? (
            <Wallet className="w-3.5 h-3.5 text-violet-500" />
          ) : (
            <CreditCard className="w-3.5 h-3.5 text-[#2563EB]" />
          )}
          {formatRefundDestination(snapshot.destination)}
        </p>
      </div>
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
    </div>
  );
}

function splitReturnReason(reason: string | null): [string, string] {
  if (!reason) return ["", ""];
  const parts = reason.split("\n");
  return [parts[0] ?? "", parts.slice(1).join("\n").trim()];
}

export interface ReturnFormLine {
  orderLineId: string;
  label: string;
  maxQuantity: number;
  quantity: number;
}

/**
 * مودال درخواست مرجوعی — port از returnFormModal.jsx Shopeiva
 * (overlay blur, sticky header, amber notice, reason select, description min 10, success step).
 */
export function ReturnFormModal({
  open,
  onClose,
  sellerOrderId,
  orderReference,
  fulfillmentItems,
  lineLabels,
  onSubmitted,
}: {
  open: boolean;
  onClose: () => void;
  sellerOrderId: string;
  orderReference?: string;
  fulfillmentItems: FulfillmentItem[];
  lineLabels?: Record<string, string>;
  onSubmitted?: (snapshot: ReturnSnapshot) => void;
}) {
  const [reasonId, setReasonId] = useState("");
  const [description, setDescription] = useState("");
  const [destination, setDestination] = useState<RefundDestination>(DEFAULT_REFUND_DESTINATION);
  const [lines, setLines] = useState<ReturnFormLine[]>([]);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [step, setStep] = useState<"form" | "success">("form");

  useEffect(() => {
    if (!open) return;
    setLines(
      fulfillmentItems
        .filter((item) => item.quantityShipped > 0)
        .map((item) => ({
          orderLineId: item.orderLineId,
          label: lineLabels?.[item.orderLineId] ?? `خط ${item.orderLineId.slice(0, 8)}`,
          maxQuantity: item.quantityShipped,
          quantity: item.quantityShipped,
        })),
    );
    setReasonId("");
    setDescription("");
    setDestination(DEFAULT_REFUND_DESTINATION);
    setError(null);
    setStep("form");
  }, [open, fulfillmentItems, lineLabels]);

  if (!open) return null;

  const selectedReason = RETURN_REASONS.find((item) => item.id === reasonId)?.label ?? "";

  async function submit() {
    if (!reasonId) {
      setError("لطفاً دلیل مرجوعی را انتخاب کنید.");
      return;
    }
    if (description.trim().length < 10) {
      setError("لطفاً توضیحات را وارد کنید (حداقل ۱۰ کاراکتر).");
      return;
    }
    const selected = lines.filter((line) => line.quantity > 0);
    if (selected.length === 0) {
      setError("حداقل یک قلم با تعداد بیشتر از صفر انتخاب کنید.");
      return;
    }
    setSubmitting(true);
    setError(null);
    const result = await createCustomerReturn({
      sellerOrderId,
      reason: `${selectedReason}\n${description.trim()}`,
      destination,
      items: selected.map((line) => ({ orderLineId: line.orderLineId, quantity: line.quantity })),
    });
    setSubmitting(false);
    if (!result.ok) {
      setError(result.errorCode);
      return;
    }
    onSubmitted?.(result.snapshot);
    setStep("success");
  }

  return (
    <div className="fixed inset-0 z-[9999] flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
      <div className="relative bg-white rounded-2xl max-w-lg w-full max-h-[90vh] overflow-hidden shadow-2xl border border-gray-200">
        <div className="sticky top-0 z-10 bg-white border-b border-gray-200 p-4 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <div className="w-10 h-10 rounded-xl bg-blue-50 flex items-center justify-center">
              <Package className="w-5 h-5 text-[#2563EB]" />
            </div>
            <div>
              <h3 className="text-lg font-bold text-gray-900">درخواست مرجوعی</h3>
              <p className="text-xs text-gray-500">{orderReference ?? sellerOrderId.slice(0, 8)}</p>
            </div>
          </div>
          <button type="button" onClick={onClose} className="p-2 rounded-lg hover:bg-gray-100 transition-colors" aria-label="بستن">
            <X className="w-5 h-5" />
          </button>
        </div>

        {step === "form" ? (
          <div className="p-4 md:p-6 overflow-y-auto max-h-[calc(90vh-80px)] space-y-5">
            <div className="bg-amber-50 border border-amber-200 rounded-xl p-3 flex items-start gap-2">
              <AlertTriangle className="w-4 h-4 text-amber-500 mt-0.5 shrink-0" />
              <p className="text-xs text-amber-700">
                تنها سفارشاتی که وضعیت «تحویل شده» دارند و در بازهٔ مجاز مرجوعی هستند، قابل ثبت درخواست می‌باشند.
              </p>
            </div>

            {lines.length > 0 ? (
              <div className="bg-gray-50 rounded-xl p-3 space-y-2">
                {lines.slice(0, 2).map((line) => (
                  <p key={line.orderLineId} className="text-sm text-gray-700">
                    {line.quantity.toLocaleString("fa-IR")}× {line.label}
                  </p>
                ))}
                {lines.length > 2 ? (
                  <p className="text-xs text-gray-500">+ {lines.length - 2} قلم دیگر</p>
                ) : null}
              </div>
            ) : (
              <p className="text-sm text-gray-500">قلمی برای مرجوعی واجد شرایط نیست.</p>
            )}

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1.5">دلیل مرجوعی</label>
              <select
                value={reasonId}
                onChange={(event) => setReasonId(event.target.value)}
                className="w-full px-4 py-2.5 bg-white rounded-xl text-sm text-gray-900 border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#2563EB]"
              >
                <option value="">انتخاب کنید...</option>
                {RETURN_REASONS.map((item) => (
                  <option key={item.id} value={item.id}>{item.label}</option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1.5">توضیحات</label>
              <textarea
                value={description}
                onChange={(event) => setDescription(event.target.value)}
                placeholder="لطفاً توضیح دهید چرا قصد مرجوع کردن کالا را دارید..."
                className="w-full px-4 py-2.5 bg-white rounded-xl text-sm text-gray-900 border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#2563EB] resize-none"
                rows={4}
              />
              <p className="text-xs text-gray-400 mt-1">حداقل ۱۰ کاراکتر</p>
            </div>

            <RefundDestinationSelector value={destination} onChange={setDestination} />

            {error ? <p className="text-sm text-red-600">{error}</p> : null}

            <div className="flex gap-3 pt-2">
              <button
                type="button"
                disabled={submitting || lines.length === 0}
                onClick={() => void submit()}
                className="flex-1 py-2.5 bg-[#2563EB] text-white rounded-xl text-sm font-bold hover:bg-blue-700 transition-colors shadow-lg shadow-blue-500/30 disabled:opacity-50"
              >
                {submitting ? "در حال ثبت..." : "ثبت درخواست مرجوعی"}
              </button>
              <button type="button" onClick={onClose} className="px-6 py-2.5 bg-gray-100 text-gray-700 rounded-xl text-sm font-medium hover:bg-gray-200 transition-colors">
                انصراف
              </button>
            </div>
          </div>
        ) : (
          <div className="p-4 md:p-6 text-center space-y-4">
            <div className="w-16 h-16 rounded-full bg-emerald-50 flex items-center justify-center mx-auto">
              <CheckCircle className="w-8 h-8 text-emerald-500" />
            </div>
            <div>
              <h4 className="text-lg font-bold text-gray-900">درخواست شما ثبت شد</h4>
              <p className="text-sm text-gray-500 mt-1">پس از بررسی فروشنده، نتیجه اطلاع داده خواهد شد.</p>
            </div>
            <button type="button" onClick={onClose} className="w-full py-2.5 bg-[#2563EB] text-white rounded-xl text-sm font-bold hover:bg-blue-700 transition-colors">
              باشه
            </button>
          </div>
        )}
      </div>
    </div>
  );
}

/**
 * مودال بررسی مرجوعی فروشنده — port از returnDetailModal.jsx Shopeiva.
 */
export function ReturnReviewModal({
  open,
  snapshot,
  onClose,
  onUpdated,
}: {
  open: boolean;
  snapshot: ReturnSnapshot | null;
  onClose: () => void;
  onUpdated?: (snapshot: ReturnSnapshot) => void;
}) {
  const [adminReason, setAdminReason] = useState("");
  const [destination, setDestination] = useState<RefundDestination>(DEFAULT_REFUND_DESTINATION);
  const [action, setAction] = useState<"approved" | "rejected" | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) return;
    setAdminReason("");
    setDestination(snapshot?.destination ?? DEFAULT_REFUND_DESTINATION);
    setAction(null);
    setError(null);
  }, [open, snapshot?.returnRequestId, snapshot?.destination]);

  if (!open || !snapshot) return null;

  const [reasonLabel, description] = splitReturnReason(snapshot.reason);
  const canDecide = snapshot.status === "Requested";

  async function runAction(kind: "approved" | "rejected") {
    const current = snapshot;
    if (!current) return;
    const sellerPartyId = readSellerPartyId(window.location.search);
    if (!sellerPartyId) {
      setError("seller.identity.missing");
      return;
    }
    if (kind === "rejected" && !adminReason.trim()) {
      setError("لطفاً دلیل رد درخواست را وارد کنید.");
      return;
    }
    setBusy(true);
    setError(null);
    const result = kind === "approved"
      ? await sellerApproveReturn(sellerPartyId, current.returnRequestId, destination)
      : await sellerRejectReturn(sellerPartyId, current.returnRequestId, adminReason.trim());
    setBusy(false);
    if (!result.ok) {
      setError(result.errorCode);
      return;
    }
    onUpdated?.(result.snapshot);
    onClose();
  }

  return (
    <div className="fixed inset-0 z-[9999] flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
      <div className="relative bg-white rounded-2xl max-w-lg w-full max-h-[90vh] overflow-hidden shadow-2xl border border-gray-200">
        <div className="sticky top-0 z-10 bg-white border-b border-gray-200 p-4 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <div className="w-10 h-10 rounded-xl bg-blue-50 flex items-center justify-center">
              <Package className="w-5 h-5 text-[#2563EB]" />
            </div>
            <div>
              <h3 className="text-lg font-bold text-gray-900">جزئیات درخواست مرجوعی</h3>
              <p className="text-xs text-gray-500">{snapshot.sellerOrderId.slice(0, 8)}</p>
            </div>
          </div>
          <button type="button" onClick={onClose} className="p-2 rounded-lg hover:bg-gray-100 transition-colors" aria-label="بستن">
            <X className="w-5 h-5" />
          </button>
        </div>

        <div className="p-4 md:p-6 overflow-y-auto max-h-[calc(90vh-80px)] space-y-5">
          <ReturnStatusBadge status={snapshot.status} />

          <div className="grid grid-cols-2 gap-3">
            <div className="bg-gray-50 rounded-xl p-3">
              <p className="text-[10px] text-gray-500">تاریخ درخواست</p>
              <p className="text-sm font-bold flex items-center gap-1 mt-1">
                <Calendar className="w-3.5 h-3.5 text-[#2563EB]" />
                {formatReturnDate(snapshot.createdAt)}
              </p>
            </div>
            <div className="bg-gray-50 rounded-xl p-3">
              <p className="text-[10px] text-gray-500">مشتری</p>
              <p className="text-sm font-bold flex items-center gap-1 mt-1">
                <User className="w-3.5 h-3.5 text-[#2563EB]" />
                {snapshot.requestedByUserId.slice(0, 8)}
              </p>
            </div>
          </div>

          {reasonLabel ? (
            <div>
              <h4 className="text-sm font-bold mb-2 flex items-center gap-1">
                <AlertTriangle className="w-4 h-4 text-[#2563EB]" />
                دلیل مرجوعی
              </h4>
              <div className="bg-gray-50 rounded-xl p-3 text-sm">{reasonLabel}</div>
            </div>
          ) : null}

          {description ? (
            <div>
              <h4 className="text-sm font-bold mb-2 flex items-center gap-1">
                <MessageSquare className="w-4 h-4 text-[#2563EB]" />
                توضیحات مشتری
              </h4>
              <div className="bg-gray-50 rounded-xl p-3 text-sm text-gray-600">{description}</div>
            </div>
          ) : null}

          {canDecide ? (
            <>
              <RefundDestinationSelector value={destination} onChange={setDestination} />
              {action === "rejected" ? (
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1.5">دلیل رد درخواست (الزامی)</label>
                  <textarea
                    value={adminReason}
                    onChange={(event) => setAdminReason(event.target.value)}
                    className="w-full px-4 py-2.5 bg-white rounded-xl text-sm border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#2563EB] resize-none"
                    rows={3}
                  />
                </div>
              ) : null}
              {error ? <p className="text-sm text-red-600">{error}</p> : null}
              <div className="flex gap-3 pt-2">
                <button
                  type="button"
                  disabled={busy}
                  onClick={() => {
                    if (action === "approved") void runAction("approved");
                    else setAction("approved");
                  }}
                  className="flex-1 py-2.5 bg-emerald-500 text-white rounded-xl text-sm font-bold hover:bg-emerald-600 transition-colors disabled:opacity-50 flex items-center justify-center gap-1"
                >
                  <CheckCircle className="w-4 h-4" />
                  {busy && action === "approved" ? "در حال تأیید..." : "تأیید درخواست"}
                </button>
                <button
                  type="button"
                  disabled={busy}
                  onClick={() => {
                    if (action === "rejected") void runAction("rejected");
                    else setAction("rejected");
                  }}
                  className="flex-1 py-2.5 bg-red-500 text-white rounded-xl text-sm font-bold hover:bg-red-600 transition-colors disabled:opacity-50 flex items-center justify-center gap-1"
                >
                  <XCircle className="w-4 h-4" />
                  {busy && action === "rejected" ? "در حال رد..." : "رد درخواست"}
                </button>
              </div>
            </>
          ) : (
            <div className="bg-gray-50 rounded-xl p-3 text-sm" data-testid="return-review-destination">
              مقصد بازپرداخت: <strong>{formatRefundDestination(snapshot.destination)}</strong>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

/** انتخابگر مقصد بازپرداخت — OriginalPayment (پیش‌فرض) یا Wallet. */
export function RefundDestinationSelector({
  value,
  onChange,
}: {
  value: RefundDestination;
  onChange: (next: RefundDestination) => void;
}) {
  return (
    <div data-testid="refund-destination-selector">
      <label className="block text-sm font-medium text-gray-700 mb-1.5">مقصد بازپرداخت</label>
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
        <button
          type="button"
          onClick={() => onChange("OriginalPayment")}
          data-testid="refund-destination-original"
          className={`flex items-center gap-2 p-3 rounded-xl border-2 text-right transition-all ${
            value === "OriginalPayment"
              ? "border-[#2563EB] bg-[#2563EB]/5"
              : "border-gray-200 hover:border-gray-300"
          }`}
        >
          <CreditCard className="w-4 h-4 text-[#2563EB] shrink-0" />
          <div className="min-w-0">
            <p className="text-sm font-bold text-gray-900">پرداخت اصلی</p>
            <p className="text-[10px] text-gray-500">بازگشت به روش پرداخت اولیه</p>
          </div>
        </button>
        <button
          type="button"
          onClick={() => onChange("Wallet")}
          data-testid="refund-destination-wallet"
          className={`flex items-center gap-2 p-3 rounded-xl border-2 text-right transition-all ${
            value === "Wallet"
              ? "border-violet-500 bg-violet-50"
              : "border-gray-200 hover:border-gray-300"
          }`}
        >
          <Wallet className="w-4 h-4 text-violet-500 shrink-0" />
          <div className="min-w-0">
            <p className="text-sm font-bold text-gray-900">کیف پول</p>
            <p className="text-[10px] text-gray-500">اعتبار فوری به کیف پول مشتری</p>
          </div>
        </button>
      </div>
    </div>
  );
}
