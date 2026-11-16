"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { MessageBox, ToastRegion } from "../../design-system";
import { formatQuantityDisplay } from "../../lib/quantity-display";
import { toPersianDigits } from "../../lib/persian-digits";
import { mapAdminErrorMessage } from "./admin-error-map";
import { executeAdminOrderOperation } from "./admin-order-operations";
import type { AdminResult } from "./admin-api";
import {
  adminRecordReturnReceipt,
  adminRecoverReturn,
  adminRetryReturnRefund,
  formatRefundAttemptStatus,
  formatRefundDestination,
  formatReturnCondition,
  formatReturnDate,
  formatReturnStatus,
  formatReturnWorkCategory,
  loadAdminReturnDetail,
  returnStatusBadgeClass,
  returnWorkBadgeClass,
  type ReturnSnapshot,
  type ReturnWorkDescription,
} from "../returns/return-api";
import {
  buildReviewIntent,
  clearReviewIntent,
  createOccurrenceKey,
  readReviewIntent,
  storeReviewIntent,
  type IntentStorage,
  type ReturnReviewAction,
  type ReturnReviewIntent,
} from "../returns/return-review-intent";

export type { ReturnReviewAction, ReturnReviewIntent } from "../returns/return-review-intent";

function describeDetails(work: ReturnWorkDescription | null | undefined): string {
  if (!work) return "وضعیت کاری این پرونده خوانده نشد.";
  switch (work.category) {
    case "receiving":
      return "کالای مشتری باید دریافت و بازرسی شود؛ تا تکمیل آن، تصمیم و بازگشت وجه مجاز نیست.";
    case "decision":
      return "کالا دریافت شده است؛ تأیید یا رد مرجوعی تصمیم بعدی است.";
    case "refund":
      return "بازگشت وجه آغاز شده و نتیجهٔ نهایی هنوز ثبت نشده است.";
    case "refund_failed":
      return "بازگشت وجه به‌صورت قطعی ناموفق ثبت شده و نیازمند تلاش دوبارهٔ بررسی‌شده است.";
    case "disposition":
      return "وجه تأیید شده است؛ تعیین تکلیف فیزیکی کالای دریافت‌شده باقی مانده است.";
    case "settled":
      return "همهٔ آثار مالی و فیزیکی این پرونده ثبت شده است.";
    default:
      return "این پرونده بسته شده و کار اجرایی باقی‌مانده ندارد.";
  }
}

function formatRial(amount: number, currency: string): string {
  const formatted = new Intl.NumberFormat("fa-IR").format(Math.max(0, Math.round(amount)));
  return `${formatted} ${currency === "IRR" ? "ریال" : currency}`;
}

type ReceiptConditionChoice = "" | "Usable" | "Damaged";
type ReceiptDraftLine = {
  returnItemId: string;
  remaining: number;
  quantity: string;
  condition: ReceiptConditionChoice;
};

/**
 * TB-275: the persisted quantity is `numeric(18,6)`, so the operator's text is accepted only when it is EXACTLY
 * representable — up to 6 decimals, no exponent, no thousands separator. A value that would need rounding is refused
 * instead of being silently turned into a different quantity of goods.
 */
const EXACT_QUANTITY = /^\d{1,9}(?:\.\d{1,6})?$/;

export function parseExactQuantity(raw: string): number | null {
  const text = raw.trim().replace(/[۰-۹]/g, (digit) => String("۰۱۲۳۴۵۶۷۸۹".indexOf(digit)));
  if (!EXACT_QUANTITY.test(text)) return null;
  const value = Number(text);
  return Number.isFinite(value) && value > 0 ? value : null;
}

/**
 * TB-275: the reviewed single-case workspace. Every action is offered ONLY when the server's own work description
 * says it is executable for the stored facts, is confirmed in a deliberate dialog, and is persisted as the reviewed
 * intent before it is sent.
 */
export function AdminReturnWorkspace({ returnRequestId }: { returnRequestId: string }) {
  const [result, setResult] = useState<AdminResult<ReturnSnapshot>>({ state: "ok", data: null, status: 0 });
  const [pending, setPending] = useState<ReturnReviewAction | null>(null);
  const [unresolved, setUnresolved] = useState<ReturnReviewIntent | null>(null);
  const [notice, setNotice] = useState<{ title: string; text: string } | null>(null);
  const [toast, setToast] = useState<string | null>(null);
  const [rejectReason, setRejectReason] = useState("");
  const [receiptLines, setReceiptLines] = useState<ReceiptDraftLine[]>([]);
  const inFlight = useRef(false);
  const epoch = useRef(0);

  const snapshot = result.data;
  const work = snapshot?.work ?? null;
  const storage = useMemo<IntentStorage | null>(() => {
    if (typeof window === "undefined") return null;
    return window.sessionStorage;
  }, []);

  const load = useCallback(async () => {
    const next = await loadAdminReturnDetail(returnRequestId);
    setResult(next);
    return next;
  }, [returnRequestId]);

  useEffect(() => {
    epoch.current += 1;
    const started = epoch.current;
    void loadAdminReturnDetail(returnRequestId).then((next) => {
      if (epoch.current !== started) return;
      setResult(next);
    });
    return () => {
      epoch.current += 1;
    };
  }, [returnRequestId]);

  useEffect(() => {
    if (!storage) return;
    const stored = readReviewIntent(storage, returnRequestId);
    setUnresolved(stored);
  }, [storage, returnRequestId]);

  useEffect(() => {
    if (!snapshot) return;
    // TB-275: nothing is preselected. The operator states the real delivered quantity of each line and its inspection
    // condition, because a pre-filled "everything arrived, everything usable" would record goods nobody inspected.
    const draft = snapshot.items.map((item) => {
      const line = snapshot.receiving?.lines.find((entry) => entry.returnItemId === item.returnItemId);
      const requested = line?.requested ?? item.quantity;
      const received = line?.received ?? 0;
      return {
        returnItemId: item.returnItemId,
        remaining: Math.max(0, requested - received),
        quantity: "",
        condition: "" as ReceiptConditionChoice,
      };
    });
    setReceiptLines(draft);
  }, [snapshot]);

  const finish = useCallback(
    async (action: ReturnReviewAction, intent: ReturnReviewIntent, answered: AdminResult<ReturnSnapshot> | null) => {
      if (storage) clearReviewIntent(storage, returnRequestId, intent.occurrenceKey);
      setUnresolved(null);
      setPending(null);
      if (answered && answered.state !== "ok") {
        setNotice({
          title: "اقدام انجام نشد",
          text: mapAdminErrorMessage(answered.message ?? "return.rejected", "fa"),
        });
        await load();
        return;
      }
      setToast(
        action === "receipt"
          ? "دریافت کالا ثبت شد."
          : action === "reject"
            ? "مرجوعی رد شد."
            : action === "approve"
              ? "مرجوعی تأیید شد."
              : "اقدام ثبت شد.",
      );
      await load();
    },
    [load, returnRequestId, storage],
  );

  const run = useCallback(
    async (action: ReturnReviewAction, body: Record<string, unknown>, occurrenceKey?: string) => {
      if (inFlight.current) return;
      inFlight.current = true;
      const intent = buildReviewIntent(action, body, occurrenceKey);
      try {
        // TB-275: the reviewed envelope exists BEFORE the first possible request. Without an available durable store
        // there is no proof of which occurrence was sent, so the action is refused instead of being sent blind.
        if (!storage) {
          setNotice({
            title: "اقدام انجام نشد",
            text: "حافظهٔ موقت این مرورگر در دسترس نیست؛ برای جلوگیری از تکرار ناخواسته، اقدام ارسال نشد.",
          });
          return;
        }
        try {
          storeReviewIntent(storage, returnRequestId, intent);
        } catch {
          setNotice({
            title: "اقدام انجام نشد",
            text: "ذخیرهٔ محلی این دستگاه در دسترس نیست؛ برای جلوگیری از تکرار ناخواسته، اقدام ارسال نشد.",
          });
          return;
        }
        setUnresolved(intent);
        setPending(action);
        const answered = await dispatch(action, intent, returnRequestId, snapshot?.checkoutId ?? null);
        await finish(action, intent, answered);
      } finally {
        inFlight.current = false;
      }
    },
    [finish, returnRequestId, snapshot?.checkoutId, storage],
  );

  const headerBadge = snapshot ? (
    <span className={returnWorkBadgeClass(work?.category)} data-testid="admin-return-workspace-stage">
      {formatReturnWorkCategory(work?.category)}
    </span>
  ) : null;

  return (
    <main data-testid="admin-return-workspace" dir="rtl" className="space-y-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <p className="text-sm text-muted">خانه / مرجوعی‌ها / بررسی پرونده</p>
          <div className="mt-1 flex flex-wrap items-center gap-2">
            <h1 className="text-2xl font-semibold tracking-tight">بررسی مرجوعی</h1>
            {headerBadge}
          </div>
          <p className="mt-1 text-base text-muted">
            {snapshot
              ? formatReturnDate(snapshot.createdAt)
              : result.status === 404
                ? "پرونده‌ای برای نمایش نیست"
                : "در حال بارگذاری…"}
          </p>
        </div>
        <Link
          className="inline-flex min-h-11 items-center rounded-xl border border-border px-4 text-sm font-semibold hover:bg-secondary"
          href="/admin/returns"
        >
          بازگشت به صف کار
        </Link>
      </div>

      <ToastRegion message={toast} />

      {result.state === "denied" ? (
        <section className="rounded-2xl border border-border bg-surface-elevated p-5 text-sm">
          دسترسی به این پرونده مجاز نیست.
        </section>
      ) : result.state === "error" ? (
        <section className="rounded-2xl border border-border bg-surface-elevated p-5 text-sm">
          <p>{mapAdminErrorMessage(result.message ?? "return.missing", "fa")}</p>
          <button type="button" className="mt-3 min-h-11 rounded-xl border border-border px-4" onClick={() => void load()}>
            تلاش دوباره
          </button>
        </section>
      ) : !snapshot && result.status === 404 ? (
        // TB-275: an unknown case is an honest "not found", never a permanent loading state.
        <section className="rounded-2xl border border-border bg-surface-elevated p-5 text-sm" data-testid="admin-return-missing">
          <p>این پروندهٔ مرجوعی یافت نشد.</p>
          <Link
            className="mt-3 inline-flex min-h-11 items-center rounded-xl border border-border px-4 text-sm font-semibold hover:bg-secondary"
            href="/admin/returns"
          >
            بازگشت به صف کار
          </Link>
        </section>
      ) : !snapshot ? (
        <p className="text-sm text-muted">در حال بارگذاری…</p>
      ) : (
        <>
          {unresolved && !pending ? (
            <section
              className="rounded-2xl border border-amber-300 bg-amber-50 p-4 text-sm text-amber-900"
              data-testid="admin-return-unresolved-intent"
            >
              <p className="font-semibold">یک اقدام ناتمام برای این پرونده ثبت شده است.</p>
              <p className="mt-1">
                اگر پاسخ آن را دریافت نکردید، همان اقدام را دوباره ارسال کنید؛ سرور همان شناسهٔ بررسی‌شده را تکرار
                می‌کند و اقدام دوم ساخته نمی‌شود.
              </p>
              <div className="mt-3 flex flex-wrap gap-2">
                <button
                  type="button"
                  className="min-h-11 rounded-xl bg-primary px-4 text-sm font-bold text-primary-foreground"
                  data-testid="admin-return-unresolved-retry"
                  onClick={() => void run(unresolved.action, unresolved.body, unresolved.occurrenceKey)}
                >
                  تکرار همان اقدام
                </button>
                <button
                  type="button"
                  className="min-h-11 rounded-xl border border-border px-4 text-sm"
                  onClick={() => {
                    if (storage) clearReviewIntent(storage, returnRequestId, unresolved.occurrenceKey);
                    setUnresolved(null);
                  }}
                >
                  نادیده گرفتن
                </button>
              </div>
            </section>
          ) : null}

          <section className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm">
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div>
                <h2 className="font-semibold">وضعیت کار</h2>
                <p className="mt-1 text-sm text-muted" data-testid="admin-return-workspace-detail">
                  {describeDetails(work)}
                </p>
                {work?.blockerCode ? (
                  <p className="mt-2 text-sm text-amber-700">{mapAdminErrorMessage(work.blockerCode, "fa")}</p>
                ) : null}
              </div>
              <dl className="grid grid-cols-2 gap-x-6 gap-y-2 text-sm">
                <dt className="text-muted">مبلغ مرجوعی</dt>
                <dd>{formatRial(snapshot.refundAmount, snapshot.currency)}</dd>
                <dt className="text-muted">مقصد بازگشت وجه</dt>
                <dd>{formatRefundDestination(snapshot.destination)}</dd>
                <dt className="text-muted">منشأ مرجوعی</dt>
                <dd>{snapshot.origin === "CourierOrigin" ? "برگشتی از حامل" : "تحویل مشتری به فروشنده"}</dd>
                <dt className="text-muted">وضعیت مرجوعی</dt>
                <dd><span className={returnStatusBadgeClass(snapshot.status)}>{formatReturnStatus(snapshot.status)}</span></dd>
                <dt className="text-muted">تصمیم ثبت‌شده</dt>
                <dd>
                  {snapshot.recordedDecision === "Approve"
                    ? "تأیید"
                    : snapshot.recordedDecision === "Reject"
                      ? "رد"
                      : "بدون تصمیم"}
                </dd>
              </dl>
            </div>
            {snapshot.reason ? <p className="mt-3 text-sm">دلیل درخواست مشتری: {snapshot.reason}</p> : null}
            {snapshot.decisionReason ? <p className="mt-1 text-sm">دلیل تصمیم اپراتور: {snapshot.decisionReason}</p> : null}
          </section>

          <section className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm">
            <h2 className="font-semibold">اقلام درخواست و دریافت</h2>
            <div className="mt-3 overflow-x-auto">
              <table className="w-full min-w-[420px] text-sm">
                <thead className="text-muted">
                  <tr>
                    <th className="py-2 text-start">قلم</th>
                    <th className="py-2 text-start">درخواست</th>
                    <th className="py-2 text-start">دریافت‌شده</th>
                    <th className="py-2 text-start">سالم</th>
                    <th className="py-2 text-start">معیوب</th>
                    <th className="py-2 text-start">قابل‌فروش پس از برگشت</th>
                    <th className="py-2 text-start">تعیین تکلیف</th>
                  </tr>
                </thead>
                <tbody>
                  {snapshot.items.map((item, index) => {
                    const line = snapshot.receiving?.lines.find((entry) => entry.returnItemId === item.returnItemId);
                    return (
                      <tr key={item.returnItemId} className="border-t border-border">
                        <td className="py-2">قلم {toPersianDigits(index + 1)}</td>
                        <td className="py-2">{formatQuantityDisplay(line?.requested ?? item.quantity)}</td>
                        <td className="py-2">{formatQuantityDisplay(line?.received ?? 0)}</td>
                        <td className="py-2">{formatQuantityDisplay(line?.usable ?? 0)}</td>
                        <td className="py-2">{formatQuantityDisplay(line?.damaged ?? 0)}</td>
                        <td className="py-2">
                          {item.saleableRestockedQuantity == null
                            ? "—"
                            : formatQuantityDisplay(item.saleableRestockedQuantity)}
                        </td>
                        <td className="py-2">{item.dispositionCondition ? formatReturnCondition(item.dispositionCondition) : "باقی‌مانده"}</td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
            {snapshot.receiving && !snapshot.receiving.isComplete ? (
              <p className="mt-3 text-sm text-amber-700">دریافت کامل نشده است؛ باقی‌مانده باید ثبت شود.</p>
            ) : null}
            {snapshot.receiving && !snapshot.receiving.isCoherent ? (
              <p className="mt-3 text-sm text-red-700">اطلاعات دریافت با درخواست هم‌خوان نیست و نیاز به بررسی دارد.</p>
            ) : null}
          </section>

          <section className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm">
            <h2 className="font-semibold">سابقهٔ دریافت‌ها</h2>
            {snapshot.receipts && snapshot.receipts.length > 0 ? (
              <ol className="mt-3 space-y-3">
                {snapshot.receipts.map((occurrence, index) => (
                  <li key={occurrence.returnReceiptId} className="rounded-xl border border-border p-3">
                    <p className="text-sm font-semibold">
                      دریافت {toPersianDigits(index + 1)} · {formatReturnDate(occurrence.recordedAt)}
                    </p>
                    <ul className="mt-1 space-y-1 text-sm text-muted">
                      {occurrence.lines.map((line, lineIndex) => (
                        <li key={`${occurrence.returnReceiptId}-${line.returnItemId}-${lineIndex}`}>
                          {formatQuantityDisplay(line.quantity)} · {formatReturnCondition(line.condition)}
                        </li>
                      ))}
                    </ul>
                  </li>
                ))}
              </ol>
            ) : (
              <p className="mt-2 text-sm text-muted">هنوز دریافت فیزیکی ثبت نشده است.</p>
            )}
          </section>

          <section className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm">
            <h2 className="font-semibold">تلاش‌های بازگشت وجه</h2>
            {snapshot.refundAttempts.length > 0 ? (
              <ul className="mt-3 space-y-2 text-sm">
                {snapshot.refundAttempts.map((attempt) => (
                  <li key={attempt.refundAttemptId} className="flex flex-wrap items-center justify-between gap-2 rounded-xl border border-border px-3 py-2">
                    <span>{formatRial(attempt.amount, attempt.currency)}</span>
                    <span>{formatRefundAttemptStatus(attempt.status)}</span>
                    <span className="text-muted">{formatReturnDate(attempt.createdAt)}</span>
                    {attempt.failureCode ? <span className="text-red-700">{attempt.failureCode}</span> : null}
                  </li>
                ))}
              </ul>
            ) : (
              <p className="mt-2 text-sm text-muted">هنوز تلاش بازگشت وجهی ثبت نشده است.</p>
            )}
          </section>

          <section className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm">
            <h2 className="font-semibold">اقدام‌های مجاز</h2>
            {work && work.actionCodes.length > 0 ? (
              <div className="mt-3 flex flex-wrap gap-2">
                {work.canRecordReceipt ? (
                  <ActionButton
                    id="record-receipt"
                    label="ثبت دریافت کالا"
                    onClick={() => setPending("receipt")}
                    disabled={pending !== null}
                  />
                ) : null}
                {work.canDecide ? (
                  <>
                    <ActionButton
                      id="approve"
                      label="تأیید مرجوعی"
                      onClick={() => setPending("approve")}
                      disabled={pending !== null}
                    />
                    <ActionButton
                      id="reject"
                      label="رد مرجوعی"
                      tone="danger"
                      onClick={() => setPending("reject")}
                      disabled={pending !== null}
                    />
                  </>
                ) : null}
                {work.canResumeRefund ? (
                  <ActionButton
                    id="resume-refund"
                    label="پیگیری بازگشت وجه"
                    onClick={() =>
                      void run("resume_refund", {
                        kind: "resume_pending_refund",
                        expectedRefundAttemptId: work.pendingRefundAttemptId,
                      })
                    }
                    disabled={pending !== null}
                  />
                ) : null}
                {work.canRetryRefund ? (
                  <ActionButton
                    id="retry-refund"
                    label="تلاش دوبارهٔ بازگشت وجه"
                    onClick={() =>
                      void run("retry_refund", {
                        expectedFailedAttemptId: work.failedRefundAttemptId,
                      })
                    }
                    disabled={pending !== null}
                  />
                ) : null}
                {work.canSettlePhysical ? (
                  <ActionButton
                    id="settle-physical"
                    label="تعیین تکلیف کالا"
                    onClick={() => void run("settle_physical", { kind: "settle_physical_disposition" })}
                    disabled={pending !== null}
                  />
                ) : null}
              </div>
            ) : (
              <p className="mt-2 text-sm text-muted">برای وضعیت فعلی این پرونده اقدام اجرایی وجود ندارد.</p>
            )}
            {pending ? <p className="mt-3 text-sm text-muted">در حال ثبت اقدام…</p> : null}
          </section>

          {pending === "approve" ? (
            <section className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm" data-testid="admin-return-approve-form">
              <h2 className="font-semibold">تأیید مرجوعی</h2>
              <p className="mt-1 text-sm text-muted">
                با تأیید، بازگشت وجه به «{formatRefundDestination(snapshot.destination)}» آغاز می‌شود. این تصمیم پس از ثبت
                قابل بازگشت نیست.
              </p>
              <div className="mt-3 flex flex-wrap gap-2">
                <button
                  type="button"
                  className="min-h-11 rounded-xl bg-primary px-4 text-sm font-bold text-primary-foreground"
                  data-testid="admin-return-approve-confirm"
                  onClick={() => {
                    const occurrenceKey = createOccurrenceKey();
                    void run(
                      "approve",
                      {
                        code: "approve_return",
                        returnRequestId,
                        idempotencyKey: occurrenceKey,
                      },
                      occurrenceKey,
                    );
                  }}
                >
                  ثبت تأیید
                </button>
                <button type="button" className="min-h-11 rounded-xl border border-border px-4 text-sm" onClick={() => setPending(null)}>
                  انصراف
                </button>
              </div>
            </section>
          ) : null}

          {pending === "reject" ? (
            <section className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm" data-testid="admin-return-reject-form">
              <h2 className="font-semibold">دلیل رد مرجوعی</h2>
              <p className="mt-1 text-sm text-muted">دلیل ثبت‌شده همراه تصمیم ذخیره می‌شود و برای پیگیری قابل استناد است.</p>
              <textarea
                className="mt-3 min-h-24 w-full rounded-xl border border-border bg-surface p-3 text-sm"
                value={rejectReason}
                onChange={(event) => setRejectReason(event.target.value)}
                data-testid="admin-return-reject-reason"
              />
              <div className="mt-3 flex flex-wrap gap-2">
                <button
                  type="button"
                  className="min-h-11 rounded-xl bg-red-600 px-4 text-sm font-bold text-white"
                  disabled={pending !== "reject" || rejectReason.trim().length < 3}
                  onClick={() => {
                    if (rejectReason.trim().length < 3) return;
                    void run("reject", {
                      code: "reject_return",
                      returnRequestId,
                      reason: rejectReason.trim(),
                      idempotencyKey: createOccurrenceKey(),
                    });
                  }}
                >
                  ثبت رد
                </button>
                <button type="button" className="min-h-11 rounded-xl border border-border px-4 text-sm" onClick={() => setPending(null)}>
                  انصراف
                </button>
              </div>
            </section>
          ) : null}

          {pending === "receipt" ? (
            <section className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm" data-testid="admin-return-receipt-form">
              <h2 className="font-semibold">ثبت دریافت و بازرسی</h2>
              <p className="mt-1 text-sm text-muted">هر قلم را با مقدار واقعی تحویل‌شده و وضعیت بازرسی ثبت کنید.</p>
              <p className="mt-1 text-xs text-muted">
                مقدار را دقیقاً همان‌قدری که تحویل گرفته‌اید وارد کنید (تا ۶ رقم اعشار). مقداری که دقیق قابل ثبت نباشد گرد
                نمی‌شود و ثبت نخواهد شد.
              </p>
              <div className="mt-3 space-y-2">
                {receiptLines.map((line, index) => (
                  <div key={line.returnItemId} className="flex flex-wrap items-end gap-2">
                    <label className="text-sm">
                      <span className="block text-muted">قلم {toPersianDigits(index + 1)}</span>
                      <input
                        className="mt-1 min-h-11 w-28 rounded-xl border border-border bg-surface px-3 text-sm"
                        inputMode="decimal"
                        value={line.quantity}
                        onChange={(event) => {
                          const value = event.target.value;
                          setReceiptLines((current) =>
                            current.map((entry) =>
                              entry.returnItemId === line.returnItemId ? { ...entry, quantity: value } : entry,
                            ),
                          );
                        }}
                      />
                    </label>
                    <label className="text-sm">
                      <span className="block text-muted">وضعیت</span>
                      <select
                        className="mt-1 min-h-11 rounded-xl border border-border bg-surface px-3 text-sm"
                        value={line.condition}
                        onChange={(event) => {
                          const value: ReceiptConditionChoice =
                            event.target.value === "Damaged"
                              ? "Damaged"
                              : event.target.value === "Usable"
                                ? "Usable"
                                : "";
                          setReceiptLines((current) =>
                            current.map((entry) =>
                              entry.returnItemId === line.returnItemId ? { ...entry, condition: value } : entry,
                            ),
                          );
                        }}
                      >
                        <option value="" disabled>
                          انتخاب کنید
                        </option>
                        <option value="Usable">سالم</option>
                        <option value="Damaged">معیوب</option>
                      </select>
                    </label>
                    <span className="text-xs text-muted">باقی‌مانده: {formatQuantityDisplay(line.remaining)}</span>
                  </div>
                ))}
              </div>
              <div className="mt-3 flex flex-wrap gap-2">
                <button
                  type="button"
                  className="min-h-11 rounded-xl bg-primary px-4 text-sm font-bold text-primary-foreground disabled:opacity-50"
                  disabled={pending !== "receipt"}
                  onClick={() => {
                    // TB-275: only lines the operator actually filled are sent, and each must carry an explicit
                    // inspection condition. A quantity that is not exactly representable is refused here, before any
                    // intent is persisted or sent, so a rounding error can never change the goods on record.
                    const parsed = receiptLines.map((line) => ({
                      line,
                      quantity: line.quantity.trim() === "" ? null : parseExactQuantity(line.quantity),
                    }));
                    const filled = parsed.filter((entry) => entry.line.quantity.trim() !== "");
                    if (filled.length === 0) {
                      setNotice({
                        title: "دریافت ثبت نشد",
                        text: "برای ثبت دریافت، مقدار واقعی دست‌کم یک قلم را وارد کنید.",
                      });
                      return;
                    }
                    if (filled.some((entry) => entry.quantity === null)) {
                      setNotice({
                        title: "دریافت ثبت نشد",
                        text: "مقدار هر قلم باید بزرگ‌تر از صفر و حداکثر با ۶ رقم اعشار باشد؛ مقدار بیش از این دقت گرد نمی‌شود.",
                      });
                      return;
                    }
                    if (filled.some((entry) => entry.line.condition === "")) {
                      setNotice({
                        title: "دریافت ثبت نشد",
                        text: "وضعیت بازرسی (سالم یا معیوب) هر قلم واردشده را مشخص کنید.",
                      });
                      return;
                    }
                    const items = filled.map((entry) => ({
                      returnItemId: entry.line.returnItemId,
                      quantity: entry.quantity as number,
                      condition: entry.line.condition as "Usable" | "Damaged",
                    }));
                    const occurrenceKey = createOccurrenceKey();
                    void run("receipt", { occurrenceKey, items }, occurrenceKey);
                  }}
                >
                  ثبت دریافت
                </button>
                <button type="button" className="min-h-11 rounded-xl border border-border px-4 text-sm" onClick={() => setPending(null)}>
                  انصراف
                </button>
              </div>
            </section>
          ) : null}
        </>
      )}

      <MessageBox
        open={notice !== null}
        title={notice?.title ?? ""}
        text={notice?.text ?? ""}
        testId="admin-return-notice"
        onClose={() => setNotice(null)}
      />
    </main>
  );
}

function ActionButton({
  id,
  label,
  onClick,
  disabled,
  tone = "primary",
}: {
  id: string;
  label: string;
  onClick: () => void;
  disabled?: boolean;
  tone?: "primary" | "danger";
}) {
  return (
    <button
      type="button"
      data-testid={`admin-return-action-${id}`}
      className={
        tone === "danger"
          ? "min-h-11 rounded-xl border border-red-300 px-4 text-sm font-bold text-red-700 disabled:opacity-50"
          : "min-h-11 rounded-xl bg-primary px-4 text-sm font-bold text-primary-foreground disabled:opacity-50"
      }
      disabled={disabled}
      onClick={onClick}
    >
      {label}
    </button>
  );
}

/**
 * Sends ONE reviewed intent through its owning route. Approve/reject travel through the existing Order operations
 * authority (which delegates the Returns gates), everything else through the Returns-owned admin routes.
 */
async function dispatch(
  action: ReturnReviewAction,
  intent: ReturnReviewIntent,
  returnRequestId: string,
  checkoutId: string | null,
): Promise<AdminResult<ReturnSnapshot> | null> {
  switch (action) {
    case "receipt":
      return adminRecordReturnReceipt({
        returnRequestId,
        occurrenceKey: String(intent.body.occurrenceKey ?? intent.occurrenceKey),
        items: (intent.body.items as { returnItemId: string; quantity: number; condition: "Usable" | "Damaged" }[]) ?? [],
      });
    case "resume_refund":
      return adminRecoverReturn({
        returnRequestId,
        kind: "resume_pending_refund",
        expectedRefundAttemptId: (intent.body.expectedRefundAttemptId as string | null) ?? null,
      });
    case "settle_physical":
      return adminRecoverReturn({
        returnRequestId,
        kind: "settle_physical_disposition",
      });
    case "retry_refund":
      return adminRetryReturnRefund(
        returnRequestId,
        (intent.body.expectedFailedAttemptId as string | null) ?? null,
      );
    case "approve":
    case "reject": {
      if (!checkoutId) return null;
      const answered = await executeAdminOrderOperation(checkoutId, {
        code: String(intent.body.code ?? ""),
        returnRequestId,
        reason: (intent.body.reason as string | null) ?? null,
        idempotencyKey: String(intent.body.idempotencyKey ?? intent.occurrenceKey),
      });
      if (answered.state === "ok") return { state: "ok", data: null, status: answered.status };
      return { state: answered.state, data: null, status: answered.status, message: answered.message };
    }
    default:
      return null;
  }
}

/** Kept for the Admin screens barrel: the reviewed workspace replaces the legacy detail card + bare retry button. */
export function AdminReturnDetailScreen({ returnRequestId }: { returnRequestId: string }) {
  return <AdminReturnWorkspace returnRequestId={returnRequestId} />;
}
