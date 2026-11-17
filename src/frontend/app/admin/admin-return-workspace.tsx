"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { Button, Dialog, MessageBox, ToastRegion } from "../../design-system";
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
  formatReturnDisposition,
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
import {
  MAX_QUANTITY_SCALE,
  normalizeQuantityInput,
  parsePositiveExactUnits,
  quantityStep,
  subtractUnits,
  unitsFromWireNumber,
  unitsToExactText,
  unitsToWireNumber,
} from "../returns/return-quantity";

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
    case "start_refund":
      return "شروع بازگشت وجه";
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
      // TB-277: approval is a decision only; the money message belongs to the separate refund action.
      return "تصمیم تأیید ثبت شد؛ بازگشت وجه هنوز آغاز نشده است.";
    case "start_refund":
      return "بازگشت وجه آغاز شد.";
    case "resume_refund":
      return "همان تلاش بازگشت وجه دوباره ارسال شد.";
    case "retry_refund":
      return "تلاش دوبارهٔ بازگشت وجه ثبت شد.";
    case "settle_physical":
      return "تعیین تکلیف فیزیکی کالا ثبت شد.";
    default:
      return "اقدام ثبت شد.";
  }
}

import {
  collectReceiptItems,
  describeQuantityProblem,
  evaluateQuantityEntry,
  formatCeilingFa,
  type QuantityProblem,
  type ReceiptDraftLine,
} from "../returns/return-receipt-quantities";
export {
  collectReceiptItems,
  describeQuantityProblem,
  evaluateQuantityEntry,
  formatCeilingFa,
};
export type { QuantityProblem, ReceiptDraftLine };
export {
  MAX_QUANTITY_SCALE,
  normalizeQuantityInput,
  quantityStep,
} from "../returns/return-quantity";

/** The decimal value of an exact integer-units quantity, for display and compatibility callers. */
export function parseExactQuantity(raw: string): number | null {
  const units = parsePositiveExactUnits(raw);
  return units === null ? null : unitsToWireNumber(units);
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
  /**
   * TB-277: the ONE review dialog currently open. Every operator action is reviewed in a design-system `Dialog`;
   * no action form is ever rendered inline on the page.
   */
  const [reviewing, setReviewing] = useState<ReturnReviewAction | null>(null);
  /** The action whose command is actually on the wire (single in-flight lock). */
  const [pending, setPending] = useState<ReturnReviewAction | null>(null);
  const [stored, setStored] = useState<ReturnReviewIntentRead>({ kind: "none" });
  const [notice, setNotice] = useState<{ title: string; text: string } | null>(null);
  const [toast, setToast] = useState<{ text: string; seq: number } | null>(null);
  const [rejectReason, setRejectReason] = useState("");
  const [receiptLines, setReceiptLines] = useState<ReceiptDraftLine[]>([]);
  /** TB-277: the blocked-quantity reason of the receipt dialog, shown inside the dialog instead of sending. */
  const [receiptProblem, setReceiptProblem] = useState<string | null>(null);
  const inFlight = useRef(false);
  const epoch = useRef(0);
  // TB-277: a repeated identical success must still announce itself, so every publish carries a new sequence.
  const toastSeq = useRef(1);

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
    setReviewing(null);
    setReceiptProblem(null);
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
    if (!toast) return;
    // The shared ToastRegion is a passive region; the feature clears its own message so the SAME success can be
    // announced again and a stale notice never sits on the page indefinitely.
    const timer = setTimeout(() => setToast(null), 6000);
    return () => clearTimeout(timer);
  }, [toast]);

  useEffect(() => {
    if (!snapshot) return;
    // TB-275/TB-277: nothing is preselected. The operator states the real delivered quantity of each line — split
    // between the inspected usable and damaged portions, under ONE shared remaining ceiling — because a pre-filled
    // "everything arrived, everything usable" would record goods nobody inspected.
    const draft = snapshot.items.map((item) => {
      const line = snapshot.receiving?.lines.find((entry) => entry.returnItemId === item.returnItemId);
      const requested = line?.requested ?? item.quantity;
      const received = line?.received ?? 0;
      const answeredPlaces = item.unitDecimalPlaces;
      // TB-277-R1: the remaining ceiling is an EXACT subtraction of the authoritative decimals, never a floating
      // point `requested - received` (which produces 0.19999999999999998 for 0.3 - 0.1).
      const requestedUnits = unitsFromWireNumber(requested);
      const receivedUnits = unitsFromWireNumber(received);
      const remainingUnits =
        requestedUnits === null || receivedUnits === null
          ? null
          : Math.max(0, subtractUnits(requestedUnits, receivedUnits));
      return {
        returnItemId: item.returnItemId,
        productLabel: item.productLabel ?? null,
        unitLabel: item.unitLabel ?? null,
        // An unanswered unit precision is HONESTLY BLOCKED — the persisted scale is a storage cap, never a guessed
        // permission to receive a fraction of a piece.
        decimalPlaces:
          typeof answeredPlaces === "number" && answeredPlaces >= 0
            ? Math.min(Math.trunc(answeredPlaces), MAX_QUANTITY_SCALE)
            : null,
        ceilingUnits: remainingUnits ?? 0,
        usable: "",
        damaged: "",
      };
    });
    setReceiptLines(draft);
    setReceiptProblem(null);
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
          setToast({ text: successLabel(intent.action), seq: toastSeq.current++ });
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
    // TB-277: each entered portion is bounded by the UNIT's own precision and by the line's remaining coverage, and
    // usable + damaged share that ONE ceiling. Everything is refused here — inside the dialog, before any intent is
    // persisted or sent — so an over-ceiling or mis-precision quantity can never leave this surface.
    if (receiptLines.length === 0) {
      setReceiptProblem("برای این پرونده قلمی برای دریافت وجود ندارد.");
      return;
    }
    const collected = collectReceiptItems(receiptLines);
    if (collected.problem || collected.sumProblem) {
      setReceiptProblem(collected.problem ?? collected.sumProblem);
      return;
    }
    if (collected.items.length === 0) {
      setReceiptProblem("برای ثبت دریافت، مقدار واقعی دست‌کم یک قلم را وارد کنید.");
      return;
    }
    const items = collected.items;

    const occurrenceKey = createOccurrenceKey();
    const intent = buildReturnReviewIntent({
      action: "receipt",
      target: "returns",
      returnRequestId,
      occurrenceKey,
      body: { occurrenceKey, items },
    });
    if (!intent) {
      setReceiptProblem("بستهٔ این اقدام کامل نشد؛ هیچ درخواستی ارسال نشد.");
      return;
    }
    setReceiptProblem(null);
    setReviewing(null);
    void send(intent);
  }, [receiptLines, returnRequestId, send]);

  /**
   * TB-277: the FIRST refund is a reviewed financial action of its own. It is never part of the approval dialog, and
   * it is offered only while the owning work description publishes it (`canStartRefund`).
   */
  const reviewStartRefund = useCallback(() => {
    const intent = buildReturnReviewIntent({
      action: "start_refund",
      target: "returns",
      returnRequestId,
      body: { kind: "start_refund" },
    });
    if (!intent) {
      setNotice({ title: "اقدام انجام نشد", text: "بستهٔ این اقدام کامل نشد؛ هیچ درخواستی ارسال نشد." });
      return;
    }
    setReviewing(null);
    void send(intent);
  }, [returnRequestId, send]);

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
      setReviewing(null);
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
      setReviewing(null);
      void send(intent);
    },
    [returnRequestId, send, work?.failedRefundAttemptId, work?.pendingRefundAttemptId],
  );

  const closeReview = useCallback(() => {
    setReviewing(null);
    setReceiptProblem(null);
  }, []);

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

  /**
   * TB-277: every action is blocked while a command is on the wire OR while a durable unresolved envelope exists, so a
   * second reviewed command can never be built on top of a possibly-applied one.
   */
  const commandsBlocked = pending !== null || stored.kind !== "none";

  // ONE local authority for the receipt dialog's submit state: an over-ceiling or mis-precision amount disables the
  // confirm button instead of being discovered after a send.
  const receiptCollected = collectReceiptItems(receiptLines);
  const receiptSubmittable =
    receiptCollected.problem === null && receiptCollected.sumProblem === null && receiptCollected.items.length > 0;

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

      <ToastRegion key={toast?.seq ?? 0} message={toast?.text ?? null} />

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
                        <td className="py-2" data-testid={`admin-return-item-disposition-${index}`}>
                          {formatReturnDisposition(item.disposition, item.saleableRestockedQuantity)}
                        </td>
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
              <>
                <p className="mt-1 text-sm text-muted">
                  هر اقدام در پنجرهٔ بررسی خودش باز می‌شود و تا تأیید شما هیچ درخواستی ارسال نمی‌شود.
                </p>
                <div className="mt-3 flex flex-wrap gap-2">
                  {work.canRecordReceipt ? (
                    <ActionButton
                      id="record-receipt"
                      label="ثبت دریافت کالا"
                      onClick={() => setReviewing("receipt")}
                      disabled={commandsBlocked}
                    />
                  ) : null}
                  {work.canDecide ? (
                    <>
                      <ActionButton
                        id="approve"
                        label="تأیید مرجوعی"
                        onClick={() => setReviewing("approve")}
                        disabled={commandsBlocked}
                      />
                      <ActionButton
                        id="reject"
                        label="رد مرجوعی"
                        tone="danger"
                        onClick={() => setReviewing("reject")}
                        disabled={commandsBlocked}
                      />
                    </>
                  ) : null}
                  {/*
                    TB-277: approval records a DECISION only. The money is a separate, deliberate action that the owning
                    work description publishes on its own; it never rides on "approve".
                  */}
                  {work.canStartRefund ? (
                    <ActionButton
                      id="start-refund"
                      label="شروع بازگشت وجه"
                      onClick={() => setReviewing("start_refund")}
                      disabled={commandsBlocked}
                    />
                  ) : null}
                  {work.canResumeRefund ? (
                    <ActionButton
                      id="resume-refund"
                      label="پیگیری بازگشت وجه"
                      onClick={() => setReviewing("resume_refund")}
                      disabled={commandsBlocked}
                    />
                  ) : null}
                  {work.canRetryRefund ? (
                    <ActionButton
                      id="retry-refund"
                      label="تلاش دوبارهٔ بازگشت وجه"
                      onClick={() => setReviewing("retry_refund")}
                      disabled={commandsBlocked}
                    />
                  ) : null}
                  {work.canSettlePhysical ? (
                    <ActionButton
                      id="settle-physical"
                      label="تعیین تکلیف کالا"
                      onClick={() => setReviewing("settle_physical")}
                      disabled={commandsBlocked}
                    />
                  ) : null}
                </div>
              </>
            ) : (
              <p className="mt-2 text-sm text-muted">برای وضعیت فعلی این پرونده اقدام اجرایی وجود ندارد.</p>
            )}
            {pending ? (
              <p className="mt-3 text-sm text-muted" data-testid="admin-return-command-pending">
                در حال ثبت اقدام…
              </p>
            ) : null}
          </section>

          {/*
            TB-277: the reviewed decision is a DIALOG. Its copy says exactly what it does — record the decision, move no
            money — so an operator can never read approval as "the refund happened".
          */}
          {reviewing === "approve" ? (
            <Dialog open title="تأیید مرجوعی" onClose={closeReview} showCloseButton={false}>
              <div className="space-y-3 text-sm" data-testid="admin-return-approve-form">
                <p>
                  با تأیید، فقط <strong>تصمیم تأیید</strong> روی این پرونده ثبت می‌شود.
                </p>
                <p className="text-muted">
                  در همین مرحله هیچ وجهی جابه‌جا نمی‌شود و هیچ کالایی به موجودی قابل‌فروش برنمی‌گردد. بازگشت وجه به «
                  {formatRefundDestination(snapshot.destination)}» یک اقدام مالی جداگانه است که پس از ثبت تأیید، با دکمهٔ
                  «شروع بازگشت وجه» انجام می‌شود.
                </p>
                <p className="text-muted">این تصمیم پس از ثبت قابل بازگشت نیست.</p>
                <div className="flex flex-wrap justify-end gap-2">
                  <Button tone="secondary" type="button" onClick={closeReview} data-testid="admin-return-approve-cancel">
                    انصراف
                  </Button>
                  <Button type="button" onClick={() => reviewDecision("approve", null)} data-testid="admin-return-approve-confirm">
                    ثبت تأیید
                  </Button>
                </div>
              </div>
            </Dialog>
          ) : null}

          {reviewing === "reject" ? (
            <Dialog open title="رد مرجوعی" onClose={closeReview} showCloseButton={false}>
              <div className="space-y-3 text-sm" data-testid="admin-return-reject-form">
                <p>دلیل ثبت‌شده همراه تصمیم ذخیره می‌شود و برای پیگیری قابل استناد است.</p>
                <p className="text-muted">
                  با رد، بازگشت وجه انجام نمی‌شود. کالای دریافت‌شده در همین وضعیت باقی می‌ماند و تعیین تکلیف فیزیکی آن
                  اقدام جداگانه‌ای است.
                </p>
                <label className="block">
                  <span className="block text-muted">دلیل رد (حداقل ۳ نویسه)</span>
                  <textarea
                    className="mt-1 min-h-24 w-full rounded-xl border border-border bg-surface p-3 text-sm"
                    value={rejectReason}
                    onChange={(event) => setRejectReason(event.target.value)}
                    data-testid="admin-return-reject-reason"
                  />
                </label>
                <div className="flex flex-wrap justify-end gap-2">
                  <Button tone="secondary" type="button" onClick={closeReview}>
                    انصراف
                  </Button>
                  <Button
                    tone="danger"
                    type="button"
                    disabled={rejectReason.trim().length < 3}
                    onClick={() => {
                      if (rejectReason.trim().length < 3) return;
                      reviewDecision("reject", rejectReason.trim());
                    }}
                    data-testid="admin-return-reject-confirm"
                  >
                    ثبت رد
                  </Button>
                </div>
              </div>
            </Dialog>
          ) : null}

          {reviewing === "start_refund" ? (
            <Dialog open title="شروع بازگشت وجه" onClose={closeReview} showCloseButton={false}>
              <div className="space-y-3 text-sm" data-testid="admin-return-start-refund-form">
                <p>
                  این اقدام، <strong>اولین تلاش بازگشت وجه</strong> این پرونده را ثبت و به درگاه پرداخت ارسال می‌کند:
                </p>
                <dl className="grid grid-cols-2 gap-x-4 gap-y-1 rounded-xl border border-border p-3">
                  <dt className="text-muted">مبلغ</dt>
                  <dd>{formatRial(snapshot.refundAmount, snapshot.currency)}</dd>
                  <dt className="text-muted">مقصد</dt>
                  <dd>{formatRefundDestination(snapshot.destination)}</dd>
                  <dt className="text-muted">پرداخت مبدأ</dt>
                  <dd>{snapshot.paymentId ? "به همین پرونده متصل است" : "ثبت نشده"}</dd>
                  <dt className="text-muted">تصمیم ثبت‌شده</dt>
                  <dd>تأیید مرجوعی</dd>
                </dl>
                <p className="text-muted">
                  تأیید مرجوعی به‌تنهایی هیچ وجهی جابه‌جا نمی‌کند؛ این پنجره همان اقدام مالی جداگانه است. شناسهٔ این تلاش
                  از خود پرونده ساخته می‌شود، بنابراین کلیک دوباره یا قطع پاسخ، تلاش دومی نمی‌سازد.
                </p>
                <div className="flex flex-wrap justify-end gap-2">
                  <Button tone="secondary" type="button" onClick={closeReview}>
                    انصراف
                  </Button>
                  <Button type="button" onClick={reviewStartRefund} data-testid="admin-return-start-refund-confirm">
                    شروع بازگشت وجه
                  </Button>
                </div>
              </div>
            </Dialog>
          ) : null}

          {/*
            TB-277: receiving is a DIALOG with one bounded pair of fields per product line. Each field states the unit,
            the unit's precision and the line's remaining ceiling, and an over-ceiling / mis-precision amount is refused
            inside this dialog — nothing is stored or sent for an invalid quantity.
          */}
          {reviewing === "receipt" ? (
            <Dialog open size="lg" title="ثبت دریافت و بازرسی" onClose={closeReview} showCloseButton={false}>
              <div className="space-y-4 text-sm" data-testid="admin-return-receipt-form">
                <p className="text-muted">
                  هر قلم را با مقدار واقعی تحویل‌شده ثبت کنید: بخش سالم و بخش معیوب جدا وارد می‌شوند و مجموع آن دو از
                  باقی‌ماندهٔ همان قلم بیشتر نمی‌شود.
                </p>
                {receiptLines.length === 0 ? (
                  <p className="text-amber-700">درخواست این پرونده قلمی برای دریافت ندارد.</p>
                ) : (
                  <div className="space-y-3">
                    {receiptLines.map((line, index) => {
                      const usableCheck = evaluateQuantityEntry(line.usable, line.decimalPlaces, line.ceilingUnits);
                      const damagedCheck = evaluateQuantityEntry(line.damaged, line.decimalPlaces, line.ceilingUnits);
                      // TB-277-R1: an unanswered owning unit precision is a BLOCKED line, not a six-decimal guess.
                      const unitUnavailable = line.decimalPlaces === null;
                      const usableEntered = normalizeQuantityInput(line.usable) !== "";
                      const damagedEntered = normalizeQuantityInput(line.damaged) !== "";
                      const update = (patch: Partial<ReceiptDraftLine>) =>
                        setReceiptLines((current) =>
                          current.map((entry) => (entry.returnItemId === line.returnItemId ? { ...entry, ...patch } : entry)),
                        );
                      return (
                        <fieldset
                          key={line.returnItemId}
                          className="rounded-xl border border-border p-3"
                          data-testid={`admin-return-receipt-line-${index}`}
                        >
                          <legend className="px-1 text-sm font-semibold">
                            {line.productLabel ?? "کالای سفارش"}
                          </legend>
                          <div className="flex flex-wrap items-center justify-between gap-x-4 gap-y-1 text-xs text-muted">
                            <span>واحد: {formatUnitLabel(line.unitLabel)}</span>
                            <span data-testid={`admin-return-receipt-ceiling-${index}`}>
                              باقی‌ماندهٔ قابل دریافت: {formatCeilingFa(line)}
                            </span>
                            <span>
                              {line.decimalPlaces === null
                                ? "دقت واحد تأیید نشد"
                                : line.decimalPlaces === 0
                                  ? "فقط عدد صحیح"
                                  : `حداکثر ${toPersianDigits(line.decimalPlaces)} رقم اعشار`}
                            </span>
                          </div>
                          {unitUnavailable ? (
                            <p
                              className="mt-2 rounded-lg bg-amber-50 p-2 text-xs text-amber-800"
                              data-testid={`admin-return-receipt-unit-unavailable-${index}`}
                              role="alert"
                            >
                              دقت واحد این قلم از خط سفارش تأیید نشد؛ دریافت این قلم تا پاسخ‌گویی واحد مسدود است.
                            </p>
                          ) : null}
                          <div className="mt-2 grid gap-2 sm:grid-cols-2">
                            <label className="text-sm">
                              <span className="block text-muted">سالم</span>
                              <input
                                type="text"
                                inputMode="decimal"
                                autoComplete="off"
                                maxLength={12}
                                placeholder="۰"
                                className="mt-1 min-h-11 w-full rounded-xl border border-border bg-surface px-3 text-sm"
                                aria-invalid={usableEntered && usableCheck.problem !== null ? true : undefined}
                                data-testid={`admin-return-receipt-qty-${index}`}
                                data-max={line.ceilingUnits === null ? undefined : unitsToExactText(line.ceilingUnits)}
                                data-step={line.decimalPlaces === null ? undefined : quantityStep(line.decimalPlaces)}
                                disabled={unitUnavailable || line.ceilingUnits === null}
                                value={line.usable}
                                onChange={(event) => update({ usable: event.target.value })}
                              />
                              {usableEntered && usableCheck.problem ? (
                                <span className="mt-1 block text-xs text-red-700" role="alert">
                                  {describeQuantityProblem(usableCheck.problem, line)}
                                </span>
                              ) : null}
                            </label>
                            <label className="text-sm">
                              <span className="block text-muted">معیوب</span>
                              <input
                                type="text"
                                inputMode="decimal"
                                autoComplete="off"
                                maxLength={12}
                                placeholder="۰"
                                className="mt-1 min-h-11 w-full rounded-xl border border-border bg-surface px-3 text-sm"
                                aria-invalid={damagedEntered && damagedCheck.problem !== null ? true : undefined}
                                data-testid={`admin-return-receipt-damaged-${index}`}
                                data-max={line.ceilingUnits === null ? undefined : unitsToExactText(line.ceilingUnits)}
                                data-step={line.decimalPlaces === null ? undefined : quantityStep(line.decimalPlaces)}
                                disabled={unitUnavailable || line.ceilingUnits === null}
                                value={line.damaged}
                                onChange={(event) => update({ damaged: event.target.value })}
                              />
                              {damagedEntered && damagedCheck.problem ? (
                                <span className="mt-1 block text-xs text-red-700" role="alert">
                                  {describeQuantityProblem(damagedCheck.problem, line)}
                                </span>
                              ) : null}
                            </label>
                          </div>
                        </fieldset>
                      );
                    })}
                  </div>
                )}
                {receiptCollected.sumProblem ? (
                  <p className="text-sm text-red-700" role="alert" data-testid="admin-return-receipt-sum-problem">
                    {receiptCollected.sumProblem}
                  </p>
                ) : null}
                {receiptProblem ? (
                  <p className="text-sm text-red-700" role="alert" data-testid="admin-return-receipt-problem">
                    {receiptProblem}
                  </p>
                ) : null}
                <div className="flex flex-wrap justify-end gap-2">
                  <Button tone="secondary" type="button" onClick={closeReview}>
                    انصراف
                  </Button>
                  <Button
                    type="button"
                    disabled={!receiptSubmittable}
                    onClick={reviewReceipt}
                    data-testid="admin-return-receipt-confirm"
                  >
                    ثبت دریافت
                  </Button>
                </div>
              </div>
            </Dialog>
          ) : null}

          {reviewing === "resume_refund" ? (
            <Dialog open title="پیگیری بازگشت وجه" onClose={closeReview} showCloseButton={false}>
              <div className="space-y-3 text-sm" data-testid="admin-return-resume-form">
                <p>
                  تلاش بازگشت وجهِ در جریان این پرونده با همان شناسهٔ ثبت‌شده و همان مبلغ دوباره ارسال می‌شود؛ تلاش تازه‌ای
                  ساخته نمی‌شود.
                </p>
                <dl className="grid grid-cols-2 gap-x-4 gap-y-1 rounded-xl border border-border p-3">
                  <dt className="text-muted">مبلغ</dt>
                  <dd>{formatRial(snapshot.refundAmount, snapshot.currency)}</dd>
                  <dt className="text-muted">مقصد</dt>
                  <dd>{formatRefundDestination(snapshot.destination)}</dd>
                </dl>
                <div className="flex flex-wrap justify-end gap-2">
                  <Button tone="secondary" type="button" onClick={closeReview}>
                    انصراف
                  </Button>
                  <Button type="button" onClick={() => reviewRecovery("resume_refund")} data-testid="admin-return-resume-confirm">
                    ارسال دوبارهٔ همان تلاش
                  </Button>
                </div>
              </div>
            </Dialog>
          ) : null}

          {reviewing === "retry_refund" ? (
            <Dialog open title="تلاش دوبارهٔ بازگشت وجه" onClose={closeReview} showCloseButton={false}>
              <div className="space-y-3 text-sm" data-testid="admin-return-retry-form">
                <p>
                  تنها تلاش <strong>قطعی ناموفق</strong> این پرونده بررسی و تلاش وابستهٔ آن ساخته می‌شود؛ شناسهٔ تلاش
                  دوباره از همان تلاش ناموفق مشتق می‌شود.
                </p>
                <dl className="grid grid-cols-2 gap-x-4 gap-y-1 rounded-xl border border-border p-3">
                  <dt className="text-muted">مبلغ</dt>
                  <dd>{formatRial(snapshot.refundAmount, snapshot.currency)}</dd>
                  <dt className="text-muted">مقصد</dt>
                  <dd>{formatRefundDestination(snapshot.destination)}</dd>
                </dl>
                <div className="flex flex-wrap justify-end gap-2">
                  <Button tone="secondary" type="button" onClick={closeReview}>
                    انصراف
                  </Button>
                  <Button type="button" onClick={() => reviewRecovery("retry_refund")} data-testid="admin-return-retry-confirm">
                    ثبت تلاش دوباره
                  </Button>
                </div>
              </div>
            </Dialog>
          ) : null}

          {reviewing === "settle_physical" ? (
            <Dialog open title="تعیین تکلیف کالا" onClose={closeReview} showCloseButton={false}>
              <div className="space-y-3 text-sm" data-testid="admin-return-settle-form">
                <p>
                  کالای دریافت‌شدهٔ این پرونده تعیین تکلیف می‌شود: بخش سالم به موجودی قابل‌فروش برمی‌گردد و بخش معیوب
                  غیرقابل‌فروش می‌ماند.
                </p>
                <p className="text-muted">این اقدام هیچ وجهی جابه‌جا نمی‌کند و تلاش بازگشت وجهی نمی‌سازد.</p>
                <div className="flex flex-wrap justify-end gap-2">
                  <Button tone="secondary" type="button" onClick={closeReview}>
                    انصراف
                  </Button>
                  <Button type="button" onClick={() => reviewRecovery("settle_physical")} data-testid="admin-return-settle-confirm">
                    ثبت تعیین تکلیف
                  </Button>
                </div>
              </div>
            </Dialog>
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
    case "start_refund":
      // TB-277: the deliberate FIRST-refund command; its reviewed bytes travel unchanged on a retry.
      return classifyReviewAnswer(
        await sendReviewedReturnCommand(intent.returnRequestId, "start-refund", body),
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
