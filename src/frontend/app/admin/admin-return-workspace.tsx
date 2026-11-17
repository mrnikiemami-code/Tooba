"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { MessageBox, ToastRegion } from "../../design-system";
import { formatQuantityDisplayFa } from "../../lib/quantity-display";
import { toPersianDigits } from "../../lib/persian-digits";
import { mapAdminErrorMessage } from "./admin-error-map";
import { executeAdminOrderOperation } from "./admin-order-operations";
import type { AdminResult } from "./admin-api";
import {
  adminRetryReturnRefund,
  formatRefundAttemptStatus,
  formatRefundDestination,
  formatReturnCondition,
  formatReturnDate,
  formatReturnStatus,
  formatUnitLabel,
  formatReturnWorkCategory,
  loadAdminReturnDetail,
  returnStatusBadgeClass,
  returnWorkBadgeClass,
  sendReviewedReturnCommand,
  type ReturnSnapshot,
  type ReturnWorkDescription,
} from "../returns/return-api";
import {
  buildReturnReviewIntent,
  clearReturnReviewIntentIfCurrent,
  createOccurrenceKey,
  discardUnreadableReturnReviewIntent,
  isProvenNoEffectRefusal,
  parseReturnReviewBody,
  readReturnReviewIntent,
  sameReturnReviewIntent,
  storeReturnReviewIntent,
  type IntentStorage,
  type ReturnReviewAction,
  type ReturnReviewIntent,
  type ReturnReviewIntentRead,
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

/** A short human name for the reviewed action, used by the unresolved-intent card. */
function actionLabel(action: ReturnReviewAction): string {
  switch (action) {
    case "receipt":
      return "ثبت دریافت کالا";
    case "approve":
      return "تأیید مرجوعی";
    case "reject":
      return "رد مرجوعی";
    case "resume_refund":
      return "پیگیری بازگشت وجه";
    case "retry_refund":
      return "تلاش دوبارهٔ بازگشت وجه";
    default:
      return "تعیین تکلیف کالا";
  }
}

function successLabel(action: ReturnReviewAction): string {
  switch (action) {
    case "receipt":
      return "دریافت کالا ثبت شد.";
    case "reject":
      return "مرجوعی رد شد.";
    case "approve":
      return "مرجوعی تأیید شد.";
    default:
      return "اقدام ثبت شد.";
  }
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
 * TB-275 / TB-276: the reviewed single-case workspace.
 *
 * Every action is offered ONLY when the server's own work description says it is executable for the stored facts, is
 * confirmed in a deliberate dialog, and is persisted — with a verified read-back of the EXACT reviewed bytes — before
 * the first possible request. TB-276 closes the connected gaps: nothing is cleared before the answer is inspected, an
 * unknown/transport/conflict answer keeps the frozen envelope visible and retryable, an answer for an old intent can
 * never touch a newer surface, and a fresh detail read can never erase retained pending evidence.
 */
export function AdminReturnWorkspace({ returnRequestId }: { returnRequestId: string }) {
  const [result, setResult] = useState<AdminResult<ReturnSnapshot>>({ state: "ok", data: null, status: 0 });
  const [pending, setPending] = useState<ReturnReviewAction | null>(null);
  const [stored, setStored] = useState<ReturnReviewIntentRead>({ kind: "none" });
  const [notice, setNotice] = useState<{ title: string; text: string } | null>(null);
  const [toast, setToast] = useState<string | null>(null);
  const [rejectReason, setRejectReason] = useState("");
  const [receiptLines, setReceiptLines] = useState<ReceiptDraftLine[]>([]);
  const inFlight = useRef(false);
  const epoch = useRef(0);

  const snapshot = result.data;
  const work = snapshot?.work ?? null;
  // The storage getter itself throws in some private modes; a render must never crash because of that.
  const storage = useMemo<IntentStorage | null>(() => {
    if (typeof window === "undefined") return null;
    try {
      return window.sessionStorage;
    } catch {
      return null;
    }
  }, []);

  const isCurrent = useCallback((started: number) => epoch.current === started, []);

  const readStored = useCallback(
    (): ReturnReviewIntentRead => readReturnReviewIntent(storage, returnRequestId),
    [returnRequestId, storage],
  );

  const loadAt = useCallback(
    async (started: number) => {
      const next = await loadAdminReturnDetail(returnRequestId);
      if (!isCurrent(started)) return next;
      setResult(next);
      return next;
    },
    [isCurrent, returnRequestId],
  );

  useEffect(() => {
    epoch.current += 1;
    const started = epoch.current;
    // A retarget must not show the previous case while the new read is in flight.
    setResult({ state: "ok", data: null, status: 0 });
    setPending(null);
    void loadAt(started);
    return () => {
      // Unmount/retarget bumps the epoch so every late answer is fenced out of the newer surface.
      epoch.current += 1;
    };
  }, [loadAt]);

  useEffect(() => {
    setStored(readStored());
  }, [readStored]);

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

  /**
   * TB-276: the answer of ONE reviewed command is applied only to the surface that sent it, and only to the exact
   * stored envelope it describes.
   */
  const settle = useCallback(
    async (intent: ReturnReviewIntent, answer: ReviewAnswer, started: number) => {
      if (!isCurrent(started)) return;
      if (answer.state === "applied" || answer.state === "refused") {
        // An authoritative answer resolves THIS exact reviewed command. A command answer is not "money/physical
        // complete": the owning read below publishes the real state.
        const cleared = clearReturnReviewIntentIfCurrent(storage, intent);
        setStored(cleared ? { kind: "none" } : readStored());
        if (answer.state === "applied") {
          setToast(successLabel(intent.action));
        } else {
          setNotice({
            title: "اقدام انجام نشد",
            text: mapAdminErrorMessage(answer.code, "fa"),
          });
        }
        await loadAt(started);
        return;
      }

      // Unknown / transport / 5xx / conflict / denied-without-proof / no answer at all: NOT proof of "no write".
      // The frozen envelope stays exactly as it was reviewed and the operator retries the SAME command.
      setStored(readStored());
      setNotice({
        title: "نتیجهٔ این اقدام قطعی نیست",
        text:
          "پاسخ سرور برای این اقدام قطعی دریافت نشد؛ ممکن است ثبت شده باشد. این اقدام به‌صورت «ناتمام» نگه داشته شد. " +
          "همان اقدام را با همین شناسه دوباره ارسال کنید تا سرور همان نتیجه را پاسخ دهد و اقدام دومی ساخته نشود.",
      });
      await loadAt(started);
    },
    [isCurrent, loadAt, readStored, storage],
  );

  /**
   * Sends ONE already-reviewed envelope. The exact bytes are written and read back before the executor runs, so a
   * double click, a lost answer or a reload recovers the identical command instead of minting a second one.
   */
  const send = useCallback(
    async (intent: ReturnReviewIntent) => {
      if (inFlight.current) return;
      if (!storage) {
        setNotice({
          title: "اقدام انجام نشد",
          text: "حافظهٔ موقت این مرورگر در دسترس نیست؛ برای جلوگیری از تکرار ناخواسته، اقدام ارسال نشد.",
        });
        return;
      }
      const current = readStored();
      if (current.kind === "unavailable") {
        setNotice({
          title: "اقدام انجام نشد",
          text: "خواندن حافظهٔ موقت این مرورگر ممکن نیست؛ برای جلوگیری از تکرار ناخواسته، اقدام ارسال نشد.",
        });
        return;
      }
      if (current.kind === "unreadable") {
        setStored(current);
        setNotice({
          title: "اقدام انجام نشد",
          text:
            "شواهد یک اقدام ناتمام روی این دستگاه هست ولی قابل خواندن نیست. تا بررسی آن، اقدام تازه‌ای ارسال نمی‌شود.",
        });
        return;
      }
      if (current.kind === "pending" && !sameReturnReviewIntent(current.intent, intent)) {
        // A possibly-sent command may not be replaced by a newer intent; the operator resolves it first.
        setStored(current);
        setNotice({
          title: "یک اقدام ناتمام برای این پرونده ثبت شده است",
          text:
            "تا زمانی که نتیجهٔ اقدام قبلی روشن نشود، اقدام تازه‌ای ارسال نمی‌شود. ابتدا همان اقدام ناتمام را از کارت " +
            "بالای صفحه تکرار کنید.",
        });
        return;
      }

      const started = epoch.current;
      inFlight.current = true;
      try {
        // Durable (verified read-back) BEFORE the first possible send.
        if (!storeReturnReviewIntent(storage, intent)) {
          setNotice({
            title: "اقدام انجام نشد",
            text: "ذخیرهٔ محلی این دستگاه در دسترس نیست؛ برای جلوگیری از تکرار ناخواسته، اقدام ارسال نشد.",
          });
          return;
        }
        if (!isCurrent(started)) return;
        setStored({ kind: "pending", intent });
        setPending(intent.action);
        const answer = await dispatch(intent);
        if (!isCurrent(started)) return;
        await settle(intent, answer, started);
      } finally {
        inFlight.current = false;
        // A newer surface owns its own pending lock: an old send may never release it.
        if (isCurrent(started)) setPending(null);
      }
    },
    [isCurrent, readStored, settle, storage],
  );

  const reviewReceipt = useCallback(() => {
    // Only lines the operator actually filled are sent, and each must carry an explicit inspection condition. A
    // quantity that is not exactly representable is refused here, before any intent is persisted or sent.
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
    const occurrenceKey = createOccurrenceKey();
    const intent = buildReturnReviewIntent({
      action: "receipt",
      target: "returns",
      returnRequestId,
      occurrenceKey,
      body: {
        occurrenceKey,
        items: filled.map((entry) => ({
          returnItemId: entry.line.returnItemId,
          quantity: entry.quantity as number,
          condition: entry.line.condition as "Usable" | "Damaged",
        })),
      },
    });
    if (!intent) {
      setNotice({ title: "دریافت ثبت نشد", text: "بستهٔ این اقدام کامل نشد؛ هیچ درخواستی ارسال نشد." });
      return;
    }
    void send(intent);
  }, [receiptLines, returnRequestId, send]);

  const reviewDecision = useCallback(
    (action: "approve" | "reject", reason: string | null) => {
      const checkoutId = snapshot?.checkoutId ?? "";
      if (!checkoutId) {
        // No owning route identity: the reviewed decision cannot be addressed, so nothing is stored or sent.
        setNotice({
          title: "اقدام انجام نشد",
          text: "مسیر سفارش این پرونده در دسترس نیست؛ برای جلوگیری از ارسال بی‌مقصد، اقدامی ثبت نشد.",
        });
        return;
      }
      const occurrenceKey = createOccurrenceKey();
      const intent = buildReturnReviewIntent({
        action,
        target: "order-operation",
        returnRequestId,
        routeTargetId: checkoutId,
        occurrenceKey,
        body: {
          code: action === "approve" ? "approve_return" : "reject_return",
          returnRequestId,
          reason,
          idempotencyKey: occurrenceKey,
        },
      });
      if (!intent) {
        setNotice({ title: "اقدام انجام نشد", text: "بستهٔ این اقدام کامل نشد؛ هیچ درخواستی ارسال نشد." });
        return;
      }
      void send(intent);
    },
    [returnRequestId, send, snapshot?.checkoutId],
  );

  const reviewRecovery = useCallback(
    (action: "resume_refund" | "retry_refund" | "settle_physical") => {
      let body: Record<string, unknown>;
      if (action === "resume_refund") {
        const attemptId = work?.pendingRefundAttemptId ?? null;
        if (!attemptId) {
          setNotice({
            title: "اقدام انجام نشد",
            text: "شناسهٔ تلاش بازگشت وجه در دسترس نیست؛ برای جلوگیری از اقدام ناشناخته، درخواستی ارسال نشد.",
          });
          return;
        }
        body = { kind: "resume_pending_refund", expectedRefundAttemptId: attemptId };
      } else if (action === "retry_refund") {
        const failedId = work?.failedRefundAttemptId ?? null;
        if (!failedId) {
          setNotice({
            title: "اقدام انجام نشد",
            text: "شناسهٔ تلاش ناموفق بازگشت وجه در دسترس نیست؛ برای جلوگیری از اقدام ناشناخته، درخواستی ارسال نشد.",
          });
          return;
        }
        body = { expectedFailedAttemptId: failedId };
      } else {
        body = { kind: "settle_physical_disposition" };
      }
      const intent = buildReturnReviewIntent({
        action,
        target: "returns",
        returnRequestId,
        body,
      });
      if (!intent) {
        setNotice({ title: "اقدام انجام نشد", text: "بستهٔ این اقدام کامل نشد؛ هیچ درخواستی ارسال نشد." });
        return;
      }
      void send(intent);
    },
    [returnRequestId, send, work?.failedRefundAttemptId, work?.pendingRefundAttemptId],
  );

  const discardUnreadable = useCallback(() => {
    if (!storage) return;
    if (discardUnreadableReturnReviewIntent(storage, returnRequestId)) {
      setStored(readStored());
    }
  }, [readStored, returnRequestId, storage]);

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

      {/*
        TB-276: retained evidence is rendered from STORAGE, before and independently of the fresh detail read. A failed
        refetch, a denied read or a retargeted surface therefore never hides a possibly-sent command.
      */}
      {stored.kind === "pending" && !pending ? (
        <section
          className="rounded-2xl border border-amber-300 bg-amber-50 p-4 text-sm text-amber-900"
          data-testid="admin-return-unresolved-intent"
        >
          <p className="font-semibold">یک اقدام ناتمام برای این پرونده ثبت شده است: {actionLabel(stored.intent.action)}</p>
          <p className="mt-1">
            پاسخ این اقدام به‌صورت قطعی دریافت نشده است. همان بستهٔ بررسی‌شده دوباره ارسال می‌شود؛ سرور همان شناسه را
            تکرار می‌کند و اقدام دومی ساخته نمی‌شود.
          </p>
          <div className="mt-3 flex flex-wrap gap-2">
            <button
              type="button"
              className="min-h-11 rounded-xl bg-primary px-4 text-sm font-bold text-primary-foreground"
              data-testid="admin-return-unresolved-retry"
              onClick={() => void send(stored.intent)}
            >
              تکرار همان اقدام
            </button>
          </div>
        </section>
      ) : null}

      {stored.kind === "unreadable" ? (
        <section
          className="rounded-2xl border border-red-300 bg-red-50 p-4 text-sm text-red-900"
          data-testid="admin-return-unreadable-intent"
        >
          <p className="font-semibold">شواهد یک اقدام ناتمام هست، ولی قابل خواندن نیست.</p>
          <p className="mt-1">
            تا زمانی که این شواهد بررسی نشده باشد، هیچ اقدام تازه‌ای ارسال نمی‌شود. وضعیت واقعی پرونده فقط از همین
            صفحه (خواندن سرور) معتبر است.
          </p>
          <div className="mt-3 flex flex-wrap gap-2">
            <button
              type="button"
              className="min-h-11 rounded-xl border border-red-300 bg-white px-4 text-sm font-bold text-red-800"
              data-testid="admin-return-unreadable-discard"
              onClick={discardUnreadable}
            >
              پاک‌کردن شواهد ناخوانا
            </button>
          </div>
        </section>
      ) : null}

      {stored.kind === "unavailable" ? (
        <section
          className="rounded-2xl border border-border bg-surface-elevated p-4 text-sm"
          data-testid="admin-return-storage-unavailable"
        >
          حافظهٔ موقت این مرورگر در دسترس نیست؛ امکان ثبت اقدام وجود ندارد و برای جلوگیری از تکرار ناخواسته هیچ درخواستی
          ارسال نمی‌شود.
        </section>
      ) : null}

      {result.state === "denied" ? (
        <section className="rounded-2xl border border-border bg-surface-elevated p-5 text-sm">
          دسترسی به این پرونده مجاز نیست.
        </section>
      ) : result.state === "error" ? (
        <section className="rounded-2xl border border-border bg-surface-elevated p-5 text-sm">
          <p>{mapAdminErrorMessage(result.message ?? "return.missing", "fa")}</p>
          <button
            type="button"
            className="mt-3 min-h-11 rounded-xl border border-border px-4"
            onClick={() => void loadAt(epoch.current)}
          >
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
                    <th className="py-2 text-start">کالا / واحد</th>
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
                        <td className="py-2" data-testid={`admin-return-item-product-${index}`}>
                          <span className="block">{item.productLabel ?? "کالای سفارش"}</span>
                          {/* The unit is shown only when the owning line answered it; a missing unit is unknown, not «واحد». */}
                          {item.unitLabel ? (
                            <span className="block text-xs text-muted">
                              واحد: {formatUnitLabel(item.unitLabel)}
                            </span>
                          ) : null}
                        </td>
                        <td className="py-2">{formatQuantityDisplayFa(line?.requested ?? item.quantity)}</td>
                        <td className="py-2">{formatQuantityDisplayFa(line?.received ?? 0)}</td>
                        <td className="py-2">{formatQuantityDisplayFa(line?.usable ?? 0)}</td>
                        <td className="py-2">{formatQuantityDisplayFa(line?.damaged ?? 0)}</td>
                        <td className="py-2">
                          {item.saleableRestockedQuantity == null
                            ? "—"
                            : formatQuantityDisplayFa(item.saleableRestockedQuantity)}
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
                      {occurrence.lines.map((line, lineIndex) => {
                        // TB-276: a receipt line names the product it belongs to when the owning read answered it.
                        const product = snapshot.items.find((item) => item.returnItemId === line.returnItemId)?.productLabel;
                        return (
                          <li key={`${occurrence.returnReceiptId}-${line.returnItemId}-${lineIndex}`}>
                            {product ? `${product} · ` : ""}
                            {formatQuantityDisplayFa(line.quantity)} · {formatReturnCondition(line.condition)}
                          </li>
                        );
                      })}
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
                    disabled={pending !== null || stored.kind !== "none"}
                  />
                ) : null}
                {work.canDecide ? (
                  <>
                    <ActionButton
                      id="approve"
                      label="تأیید مرجوعی"
                      onClick={() => setPending("approve")}
                      disabled={pending !== null || stored.kind !== "none"}
                    />
                    <ActionButton
                      id="reject"
                      label="رد مرجوعی"
                      tone="danger"
                      onClick={() => setPending("reject")}
                      disabled={pending !== null || stored.kind !== "none"}
                    />
                  </>
                ) : null}
                {work.canResumeRefund ? (
                  <ActionButton
                    id="resume-refund"
                    label="پیگیری بازگشت وجه"
                    onClick={() => reviewRecovery("resume_refund")}
                    disabled={pending !== null || stored.kind !== "none"}
                  />
                ) : null}
                {work.canRetryRefund ? (
                  <ActionButton
                    id="retry-refund"
                    label="تلاش دوبارهٔ بازگشت وجه"
                    onClick={() => reviewRecovery("retry_refund")}
                    disabled={pending !== null || stored.kind !== "none"}
                  />
                ) : null}
                {work.canSettlePhysical ? (
                  <ActionButton
                    id="settle-physical"
                    label="تعیین تکلیف کالا"
                    onClick={() => reviewRecovery("settle_physical")}
                    disabled={pending !== null || stored.kind !== "none"}
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
                  onClick={() => reviewDecision("approve", null)}
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
                    reviewDecision("reject", rejectReason.trim());
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
                        data-testid={`admin-return-receipt-qty-${index}`}
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
                        data-testid={`admin-return-receipt-condition-${index}`}
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
                    <span className="text-xs text-muted">باقی‌مانده: {formatQuantityDisplayFa(line.remaining)}</span>
                  </div>
                ))}
              </div>
              <div className="mt-3 flex flex-wrap gap-2">
                <button
                  type="button"
                  className="min-h-11 rounded-xl bg-primary px-4 text-sm font-bold text-primary-foreground disabled:opacity-50"
                  data-testid="admin-return-receipt-confirm"
                  disabled={pending !== "receipt"}
                  onClick={reviewReceipt}
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
 * TB-276: the three kinds of answer an owning route can give for one reviewed command.
 *
 * - `applied`: the command was accepted and its durable answer came back (a real stored snapshot / operation result);
 * - `refused`: the owning contract proves refusal happened before any durable write of this intent (typed token list);
 * - `unknown`: transport loss, 5xx, conflict, a malformed/empty success payload or any unproven refusal — this is
 *   NEVER "no write" and must keep the frozen envelope retryable.
 */
type ReviewAnswer =
  | { state: "applied" }
  | { state: "refused"; code: string }
  | { state: "unknown"; status: number };

function classifyReviewAnswer(answer: AdminResult<unknown> | null): ReviewAnswer {
  if (!answer) return { state: "unknown", status: 0 };
  if (answer.state === "ok") {
    // A success envelope WITHOUT its owning payload is malformed, never "applied".
    return answer.data != null ? { state: "applied" } : { state: "unknown", status: answer.status };
  }
  const code = answer.code ?? answer.message ?? "";
  return isProvenNoEffectRefusal(code)
    ? { state: "refused", code }
    : { state: "unknown", status: answer.status };
}

/**
 * Sends ONE reviewed intent through its owning route, using ONLY the frozen bytes and the frozen target captured at
 * review time. Approve/reject travel through the existing Order operations authority (which delegates the Returns
 * gates); everything else through the Returns-owned admin routes.
 */
async function dispatch(intent: ReturnReviewIntent): Promise<ReviewAnswer> {
  const body = parseReturnReviewBody(intent);
  if (!body) return { state: "unknown", status: 0 };
  switch (intent.action) {
    case "receipt":
    case "resume_refund":
    case "settle_physical":
      // The frozen reviewed bytes are the ONLY body sent: nothing is rebuilt from a newer detail read.
      return classifyReviewAnswer(
        await sendReviewedReturnCommand(intent.returnRequestId, intent.action === "receipt" ? "receipt" : "recover", body),
      );
    case "retry_refund":
      return classifyReviewAnswer(
        await adminRetryReturnRefund(
          intent.returnRequestId,
          (body.expectedFailedAttemptId as string | null) ?? null,
        ),
      );
    case "approve":
    case "reject": {
      // TB-276: the owning checkout is the FROZEN route identity of the reviewed intent, never the newer snapshot.
      if (!intent.routeTargetId) return { state: "unknown", status: 0 };
      return classifyReviewAnswer(
        await executeAdminOrderOperation(intent.routeTargetId, {
          code: String(body.code ?? ""),
          returnRequestId: intent.returnRequestId,
          reason: (body.reason as string | null) ?? null,
          idempotencyKey: String(body.idempotencyKey ?? intent.occurrenceKey),
        }),
      );
    }
    default:
      return { state: "unknown", status: 0 };
  }
}

/** Kept for the Admin screens barrel: the reviewed workspace replaces the legacy detail card + bare retry button. */
export function AdminReturnDetailScreen({ returnRequestId }: { returnRequestId: string }) {
  return <AdminReturnWorkspace returnRequestId={returnRequestId} />;
}
