"use client";

import { useEffect, useMemo, useState } from "react";
import { Button } from "../../../design-system/primitives/core";
import { Dialog } from "../../../design-system/primitives/overlays";

/**
 * TB-258 — the ONE purposeful post-dispatch exception surface.
 *
 * After a parcel leaves the warehouse the dispatch history is immutable. A failed delivery is recorded
 * here as an explicit non-delivery fact (never a fake delivery and never a pre-dispatch cancellation),
 * the physical return to origin is started, the origin receipt is confirmed per line with its physical
 * condition, and a new resend parcel is allocated strictly against the USABLE received quantity.
 *
 * Ownership: this module is the feature-owned use-case surface. It declares its own narrow contract and
 * receives the facts/formatting the page already owns; it performs no IO and invents no quantity. The
 * server remains the authority on every bound (returned capacity, parcel lines, method, resolution).
 */

export type PostDispatchExceptionMode =
  | "record_non_delivery"
  | "start_return"
  | "record_return_receipt"
  | "resend_shipment";

/** One line of the parcel this exception applies to, with the returned capacity that funds a resend. */
export type PostDispatchParcelLine = {
  orderLineId: string;
  title: string;
  /** Quantity allocated into the original failed parcel. */
  parcelQuantity: number;
  /** Usable returned capacity still available for a resend of this line (0 when nothing was received). */
  resendableQuantity: number;
  /** Already recorded usable received quantity of this line (origin receipt). */
  returnedUsableQuantity: number;
  /** Already recorded damaged received quantity of this line. */
  returnedDamagedQuantity: number;
};

/** Server-owned non-delivery reasons (labels are display copy only). */
export const POST_DISPATCH_NON_DELIVERY_REASONS: ReadonlyArray<{ code: string; labelFa: string }> = [
  { code: "customer_unavailable", labelFa: "مشتری در آدرس در دسترس نبود" },
  { code: "address_not_found", labelFa: "آدرس پیدا نشد" },
  { code: "refused", labelFa: "گیرنده از دریافت خودداری کرد" },
  { code: "other", labelFa: "دلیل عملیاتی دیگر" },
];

export type PostDispatchReceiptLine = {
  orderLineId: string;
  quantity: number;
  condition: 0 | 1;
};

export type PostDispatchExceptionRequest =
  | { code: "record_non_delivery"; reason: string; note: string | null }
  | { code: "start_return" }
  | { code: "record_return_receipt"; receiptItems: PostDispatchReceiptLine[]; note: string | null }
  | { code: "resend_shipment"; selections: Array<{ orderLineId: string; quantity: number }>; idempotencyKey: string };

type Props = {
  open: boolean;
  mode: PostDispatchExceptionMode;
  /** Human label of the parcel (number/short id) used in the dialog copy and test ids. */
  parcelLabel: string;
  parcelKey: string;
  lines: PostDispatchParcelLine[];
  pending: boolean;
  errorFa?: string | null;
  onClose: () => void;
  onSubmit: (request: PostDispatchExceptionRequest) => void;
};

type ReceiptDraft = { quantity: string; condition: 0 | 1 };

const MODE_TITLE: Record<PostDispatchExceptionMode, string> = {
  record_non_delivery: "ثبت عدم تحویل",
  start_return: "شروع برگشت به مبدأ",
  record_return_receipt: "تأیید دریافت در مبدأ",
  resend_shipment: "ارسال مجدد",
};

function toNumber(value: string): number {
  const parsed = Number(value.replace(/[^\d.]/g, ""));
  return Number.isFinite(parsed) ? parsed : 0;
}

function clampQuantity(value: number, max: number): number {
  if (value <= 0) return 0;
  return max > 0 ? Math.min(value, max) : 0;
}

/**
 * Review surface for one post-dispatch exception. Quantities start at the exact remaining capacity the
 * server reported, so confirming without edits is the documented lawful action and no number is invented.
 */
export function PostDispatchExceptionDialog({
  open,
  mode,
  parcelLabel,
  parcelKey,
  lines,
  pending,
  errorFa,
  onClose,
  onSubmit,
}: Props) {
  const [reason, setReason] = useState<string>(POST_DISPATCH_NON_DELIVERY_REASONS[0]!.code);
  const [note, setNote] = useState("");
  const [receiptDrafts, setReceiptDrafts] = useState<Record<string, ReceiptDraft>>({});
  const [resendDrafts, setResendDrafts] = useState<Record<string, string>>({});
  const [idempotencyKey, setIdempotencyKey] = useState<string>("");
  const [localErrorFa, setLocalErrorFa] = useState<string | null>(null);

  const resendCandidates = useMemo(
    () => lines.filter((line) => line.resendableQuantity > 0),
    [lines],
  );

  useEffect(() => {
    if (!open) return;
    setLocalErrorFa(null);
    setNote("");
    setReason(POST_DISPATCH_NON_DELIVERY_REASONS[0]!.code);
    // A fresh occurrence per opened review: a retried submit replays this exact key, a new review is new.
    setIdempotencyKey(
      typeof crypto !== "undefined" && "randomUUID" in crypto
        ? crypto.randomUUID()
        : `tb258-${Date.now()}-${Math.floor(Math.random() * 1e6)}`,
    );

    const receipt: Record<string, ReceiptDraft> = {};
    for (const line of lines) {
      receipt[line.orderLineId] = {
        quantity: String(line.parcelQuantity),
        condition: 0,
      };
    }
    setReceiptDrafts(receipt);

    const resend: Record<string, string> = {};
    for (const line of lines) {
      resend[line.orderLineId] = line.resendableQuantity > 0 ? String(line.resendableQuantity) : "0";
    }
    setResendDrafts(resend);
  }, [open, mode, parcelKey, lines]);

  const receiptTotal = useMemo(
    () => lines.reduce((sum, line) => sum + clampQuantity(toNumber(receiptDrafts[line.orderLineId]?.quantity ?? "0"), line.parcelQuantity), 0),
    [lines, receiptDrafts],
  );

  const resendTotal = useMemo(
    () => resendCandidates.reduce((sum, line) => sum + clampQuantity(toNumber(resendDrafts[line.orderLineId] ?? "0"), line.resendableQuantity), 0),
    [resendCandidates, resendDrafts],
  );

  function submit() {
    setLocalErrorFa(null);
    if (mode === "record_non_delivery") {
      if (!POST_DISPATCH_NON_DELIVERY_REASONS.some((option) => option.code === reason)) {
        setLocalErrorFa("دلیل عدم تحویل را انتخاب کنید.");
        return;
      }
      onSubmit({ code: "record_non_delivery", reason, note: note.trim() || null });
      return;
    }

    if (mode === "start_return") {
      onSubmit({ code: "start_return" });
      return;
    }

    if (mode === "record_return_receipt") {
      if (receiptTotal <= 0) {
        setLocalErrorFa("دست‌کم یک قلم سالم یا آسیب‌دیده باید در رسید ثبت شود.");
        return;
      }
      const receiptItems: PostDispatchReceiptLine[] = lines
        .map((line) => ({
          orderLineId: line.orderLineId,
          quantity: clampQuantity(toNumber(receiptDrafts[line.orderLineId]?.quantity ?? "0"), line.parcelQuantity),
          condition: (receiptDrafts[line.orderLineId]?.condition ?? 0) as 0 | 1,
        }))
        .filter((line) => line.quantity > 0);
      if (receiptItems.length === 0) {
        setLocalErrorFa("دست‌کم یک قلم باید مقدار دریافتی داشته باشد.");
        return;
      }
      onSubmit({ code: "record_return_receipt", receiptItems, note: note.trim() || null });
      return;
    }

    if (resendTotal <= 0) {
      setLocalErrorFa("مقدار قابل ارسال مجدد از اقلام سالم دریافتی صفر است.");
      return;
    }
    const selections = resendCandidates
      .map((line) => ({
        orderLineId: line.orderLineId,
        quantity: clampQuantity(toNumber(resendDrafts[line.orderLineId] ?? "0"), line.resendableQuantity),
      }))
      .filter((line) => line.quantity > 0);
    if (selections.length === 0) {
      setLocalErrorFa("دست‌کم یک قلم برای ارسال مجدد انتخاب کنید.");
      return;
    }
    onSubmit({ code: "resend_shipment", selections, idempotencyKey });
  }

  const submitLabel =
    mode === "record_non_delivery"
      ? "ثبت عدم تحویل"
      : mode === "start_return"
        ? "شروع برگشت"
        : mode === "record_return_receipt"
          ? "تأیید دریافت در مبدأ"
          : "ایجاد مرسولهٔ ارسال مجدد";

  const disabled =
    pending
    || (mode === "record_return_receipt" && receiptTotal <= 0)
    || (mode === "resend_shipment" && resendTotal <= 0);

  return (
    <Dialog open={open} onClose={onClose} showCloseButton={false} size="md" title={MODE_TITLE[mode]}>
      <div
        className="flex max-h-[min(82vh,40rem)] flex-col text-sm"
        data-testid={`admin-order-post-dispatch-dialog-${parcelKey}`}
        data-mode={mode}
        dir="rtl"
      >
        <div className="min-h-0 flex-1 space-y-3 overflow-y-auto pe-1">
          <p className="text-xs leading-6 text-gray-600">{`مرسولهٔ ${parcelLabel}`}</p>

          {mode === "record_non_delivery" ? (
            <>
              <p className="text-xs leading-6 text-gray-600">
                عدم تحویل بدون حذف سابقهٔ ارسال و کد رهگیری ثبت می‌شود. مشتری تحویل‌شده محسوب نمی‌شود و ابطال پیش از ارسال به کار نمی‌رود.
              </p>
              <fieldset className="space-y-1.5">
                <legend className="mb-1 text-xs font-bold text-gray-700">دلیل عدم تحویل</legend>
                {POST_DISPATCH_NON_DELIVERY_REASONS.map((option) => (
                  <label
                    key={option.code}
                    className="flex min-h-11 cursor-pointer items-center gap-2 rounded-lg border border-gray-200 px-3 text-sm text-gray-800"
                    data-testid={`admin-order-post-dispatch-reason-${option.code}`}
                  >
                    <input
                      type="radio"
                      name={`nondelivery-${parcelKey}`}
                      value={option.code}
                      checked={reason === option.code}
                      disabled={pending}
                      onChange={() => setReason(option.code)}
                      className="size-4"
                    />
                    <span>{option.labelFa}</span>
                  </label>
                ))}
              </fieldset>
              <label className="block space-y-1">
                <span className="text-xs font-bold text-gray-700">توضیح (اختیاری)</span>
                <textarea
                  value={note}
                  disabled={pending}
                  onChange={(event) => setNote(event.target.value)}
                  maxLength={500}
                  rows={2}
                  className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm"
                  data-testid={`admin-order-post-dispatch-note-${parcelKey}`}
                />
              </label>
            </>
          ) : null}

          {mode === "start_return" ? (
            <p className="text-xs leading-6 text-gray-600">
              با شروع برگشت، مرسوله در مسیر بازگشت به مبدأ ثبت می‌شود. تأیید دریافت در مبدأ مرحلهٔ بعدی است و تنها مقدار سالم دریافتی قابل ارسال مجدد است.
            </p>
          ) : null}

          {mode === "record_return_receipt" ? (
            <>
              <p className="text-xs leading-6 text-gray-600">
                فقط مقدار «سالم» دریافتی ظرفیت ارسال مجدد می‌سازد. اقلام آسیب‌دیده ثبت می‌شوند اما قابل ارسال مجدد نیستند.
              </p>
              <ul className="space-y-2">
                {lines.map((line) => {
                  const draft = receiptDrafts[line.orderLineId] ?? { quantity: "0", condition: 0 as 0 | 1 };
                  return (
                    <li
                      key={line.orderLineId}
                      className="rounded-lg border border-gray-200 px-3 py-2"
                      data-testid={`admin-order-post-dispatch-receipt-line-${line.orderLineId}`}
                    >
                      <p className="break-words text-sm text-gray-800">{line.title}</p>
                      <p className="mt-0.5 text-[11px] text-gray-500">{`در این مرسوله: ${line.parcelQuantity.toLocaleString("fa-IR")}`}</p>
                      <div className="mt-2 flex flex-wrap items-center gap-2">
                        <label className="flex items-center gap-1.5 text-xs text-gray-700">
                          <span>دریافتی</span>
                          <input
                            type="number"
                            inputMode="decimal"
                            min={0}
                            max={line.parcelQuantity}
                            step="any"
                            value={draft.quantity}
                            disabled={pending}
                            onChange={(event) =>
                              setReceiptDrafts((previous) => ({
                                ...previous,
                                [line.orderLineId]: { ...draft, quantity: event.target.value },
                              }))
                            }
                            className="min-h-11 w-24 rounded-lg border border-gray-200 px-2 text-sm tabular-nums"
                            data-testid={`admin-order-post-dispatch-receipt-qty-${line.orderLineId}`}
                          />
                        </label>
                        <select
                          value={draft.condition}
                          disabled={pending}
                          onChange={(event) =>
                            setReceiptDrafts((previous) => ({
                              ...previous,
                              [line.orderLineId]: { ...draft, condition: Number(event.target.value) === 1 ? 1 : 0 },
                            }))
                          }
                          className="min-h-11 rounded-lg border border-gray-200 px-2 text-sm"
                          data-testid={`admin-order-post-dispatch-receipt-condition-${line.orderLineId}`}
                        >
                          <option value={0}>سالم</option>
                          <option value={1}>آسیب‌دیده</option>
                        </select>
                      </div>
                    </li>
                  );
                })}
              </ul>
            </>
          ) : null}

          {mode === "resend_shipment" ? (
            <>
              <p className="text-xs leading-6 text-gray-600">
                ارسال مجدد فقط از ظرفیت اقلام سالم دریافتی در مبدأ تأمین می‌شود و باقیماندهٔ بسته‌بندی انبار را مصرف نمی‌کند.
              </p>
              {resendCandidates.length === 0 ? (
                <p
                  className="rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-900"
                  data-testid={`admin-order-post-dispatch-resend-empty-${parcelKey}`}
                >
                  ظرفیت سالمی برای ارسال مجدد باقی نمانده است.
                </p>
              ) : (
                <ul className="space-y-2">
                  {resendCandidates.map((line) => (
                    <li
                      key={line.orderLineId}
                      className="rounded-lg border border-gray-200 px-3 py-2"
                      data-testid={`admin-order-post-dispatch-resend-line-${line.orderLineId}`}
                    >
                      <p className="break-words text-sm text-gray-800">{line.title}</p>
                      <p className="mt-0.5 text-[11px] text-gray-500">
                        {`سالم دریافتی: ${line.returnedUsableQuantity.toLocaleString("fa-IR")} · آسیب‌دیده: ${line.returnedDamagedQuantity.toLocaleString("fa-IR")} · قابل ارسال: ${line.resendableQuantity.toLocaleString("fa-IR")}`}
                      </p>
                      <label className="mt-2 flex items-center gap-1.5 text-xs text-gray-700">
                        <span>مقدار ارسال مجدد</span>
                        <input
                          type="number"
                          inputMode="decimal"
                          min={0}
                          max={line.resendableQuantity}
                          step="any"
                          value={resendDrafts[line.orderLineId] ?? "0"}
                          disabled={pending}
                          onChange={(event) =>
                            setResendDrafts((previous) => ({ ...previous, [line.orderLineId]: event.target.value }))
                          }
                          className="min-h-11 w-24 rounded-lg border border-gray-200 px-2 text-sm tabular-nums"
                          data-testid={`admin-order-post-dispatch-resend-qty-${line.orderLineId}`}
                        />
                      </label>
                    </li>
                  ))}
                </ul>
              )}
            </>
          ) : null}

          {localErrorFa || errorFa ? (
            <p className="text-xs font-bold text-red-700" role="alert" data-testid={`admin-order-post-dispatch-error-${parcelKey}`}>
              {localErrorFa ?? errorFa}
            </p>
          ) : null}
        </div>

        <div className="mt-3 flex flex-wrap items-center justify-end gap-2 border-t border-gray-100 pt-3">
          <Button type="button" tone="secondary" disabled={pending} onClick={onClose}>
            بستن
          </Button>
          <Button
            type="button"
            disabled={disabled}
            onClick={submit}
            data-testid={`admin-order-post-dispatch-submit-${parcelKey}`}
          >
            {pending ? "در حال ثبت…" : submitLabel}
          </Button>
        </div>
      </div>
    </Dialog>
  );
}
