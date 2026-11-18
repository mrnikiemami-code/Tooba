"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { toast } from "react-toastify";
import { Button, Dialog, MessageBox } from "../../design-system";
import { formatQuantityDisplayFa } from "../../lib/quantity-display";
import { toPersianDigits } from "../../lib/persian-digits";
import { mapAdminErrorMessage } from "./admin-error-map";
import { executeAdminOrderOperation } from "./admin-order-operations";
import type { AdminResult } from "./admin-api";
import {
  adminRetryReturnRefund,
  adminStartReturnRefund,
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
  compareUnits,
  subtractUnits,
  unitsFromWireNumber,
  unitsToExactText,
  unitsToWireNumber,
} from "../returns/return-quantity";

export type { ReturnReviewAction, ReturnReviewIntent } from "../returns/return-review-intent";

/**
 * TB-280-R1 UX3: presence of any received goods is a boolean fact. Unlike units (pcs vs grams) must never be added into
 * one operator-facing quantity.
 */
function hasAnyReceivedGoods(snapshot: ReturnSnapshot | null | undefined): boolean {
  return (snapshot?.receiving?.lines ?? []).some((line) => (unitsFromWireNumber(line.received) ?? 0) > 0);
}

/** TB-280-R1 UX3: per-product received/requested with that product's own unit — never “501 of 501” across units. */
function describeReceivedGoodsSummary(snapshot: ReturnSnapshot): string {
  if (!hasAnyReceivedGoods(snapshot)) return "هنوز کالایی دریافت نشده است";
  return snapshot.items
    .map((item) => {
      const line = snapshot.receiving?.lines.find((entry) => entry.returnItemId === item.returnItemId);
      const received = line?.received ?? 0;
      const requested = line?.requested ?? item.quantity;
      const unit = formatUnitLabel(item.unitLabel);
      const name = item.productLabel?.trim() || "کالا";
      return `${name}: ${formatQuantityDisplayFa(received)} از ${formatQuantityDisplayFa(requested)} ${unit}`;
    })
    .join(" · ");
}

/** TB-280: the money answer in business words, never a raw PSP/attempt token. */
function describeRefundMoney(snapshot: ReturnSnapshot): string {
  const attempts = snapshot.refundAttempts ?? [];
  if (attempts.length === 0) return "هنوز آغاز نشده است";
  const succeeded = attempts.filter((attempt) => String(attempt.status).toLowerCase() === "succeeded");
  if (succeeded.length > 0) {
    const total = succeeded.reduce((sum, attempt) => sum + attempt.amount, 0);
    return `${formatRial(total, snapshot.currency)} بازگردانده شد`;
  }
  if (attempts.some((attempt) => String(attempt.status).toLowerCase() === "pending")) {
    return "در حال انجام؛ نتیجهٔ نهایی هنوز مشخص نیست";
  }
  return "ناموفق؛ نیازمند بررسی";
}

/**
 * TB-280-R1 UX1: the money a financial confirmation may honestly show. An approved zero stays zero — never replaced by
 * requested money through a truthiness fallback. A missing authoritative amount refuses the review instead of inventing one.
 */
type ReviewedMoneyDisplay =
  | { kind: "amount"; amount: number; currency: string }
  | { kind: "unavailable"; message: string };

function firstRefundReviewMoney(snapshot: ReturnSnapshot): ReviewedMoneyDisplay {
  if (snapshot.approvedAmount !== null && snapshot.approvedAmount !== undefined) {
    return { kind: "amount", amount: snapshot.approvedAmount, currency: snapshot.currency };
  }
  // Legacy null approvedAmount: only reuse the reviewed-plan frozen amount when that plan is the owning read fact.
  if (
    snapshot.resolution?.fromReviewedPlan === true
    && snapshot.resolution.approvedAmount !== null
    && snapshot.resolution.approvedAmount !== undefined
  ) {
    return { kind: "amount", amount: snapshot.resolution.approvedAmount, currency: snapshot.currency };
  }
  return {
    kind: "unavailable",
    message: "مبلغ تأییدشده برای بازگشت وجه در این پرونده مشخص نیست؛ تا مشخص شدن آن اقدامی ارسال نمی‌شود.",
  };
}

function boundAttemptReviewMoney(
  snapshot: ReturnSnapshot,
  attemptId: string | null | undefined,
  missingMessage: string,
): ReviewedMoneyDisplay {
  if (!attemptId) {
    return { kind: "unavailable", message: missingMessage };
  }
  const matches = (snapshot.refundAttempts ?? []).filter((attempt) => attempt.refundAttemptId === attemptId);
  if (matches.length !== 1) {
    return {
      kind: "unavailable",
      message: "اطلاعات مبلغ این بازگشت وجه با پرونده هم‌خوان نیست؛ برای جلوگیری از مبلغ نادرست، اقدامی ارسال نمی‌شود.",
    };
  }
  const attempt = matches[0]!;
  return { kind: "amount", amount: attempt.amount, currency: attempt.currency || snapshot.currency };
}

/**
 * TB-280: the ONE sentence that answers "what is this case waiting for?", in the words a shopkeeper uses. It is a
 * presentation of the SAME stored facts the actions read — it never invents a step the server would refuse.
 */
function describeNextStep(snapshot: ReturnSnapshot | null | undefined, work: ReturnWorkDescription | null | undefined): string {
  if (!work) return "وضعیت این پرونده خوانده نشد.";
  switch (work.category) {
    case "receiving":
      return hasAnyReceivedGoods(snapshot)
        ? "بخشی از کالا دریافت شده است. اگر باقی کالا برنمی‌گردد، با «بررسی مرجوعی» تکلیف مقدار دریافت‌شده را مشخص کنید."
        : "هنوز کالایی دریافت نشده است؛ ابتدا دریافت کالا را ثبت کنید.";
    case "decision":
      return "کالا دریافت شده است؛ قدم بعدی تأیید یا رد مرجوعی است.";
    case "refund":
      return work.canStartRefund
        ? "مرجوعی تأیید شده است، ولی بازگشت وجه هنوز آغاز نشده است."
        : "بازگشت وجه آغاز شده و نتیجهٔ نهایی هنوز ثبت نشده است.";
    case "refund_failed":
      return "بازگشت وجه ناموفق بوده است؛ یک بار دیگر با بررسی ثبت می‌شود.";
    case "disposition":
      return "پول تأیید شده است؛ وضعیت کالاهای دریافت‌شده باید مشخص شود.";
    case "settled":
      return "همهٔ کارهای مالی و کالای این پرونده انجام شده است.";
    default:
      return "این پرونده بسته شده و کار باقی‌مانده‌ای ندارد.";
  }
}

function formatRial(amount: number, currency: string): string {
  const formatted = new Intl.NumberFormat("fa-IR").format(Math.max(0, Math.round(amount)));
  return `${formatted} ${currency === "IRR" ? "ریال" : currency}`;
}

/** TB-280-R1 UX2: short ceiling refusal beside the field — no decimal-precision lesson. */
function describeAcceptedCeilingProblem(ceiling: number, unitLabel: string | null | undefined): string {
  return `بیشتر از ${formatQuantityDisplayFa(unitsToWireNumber(ceiling) ?? 0)} ${formatUnitLabel(unitLabel)} وارد نکنید.`;
}

/**
 * TB-280: the reviewed reason of one resolution line is REQUIRED by the owning writer (a non-empty, trimmed value of at
 * most 512 chars). The dialog therefore enforces the same rule locally; labelling it "optional" and sending `null`
 * only produced a server refusal after the operator had typed everything.
 */
const RESOLUTION_REASON_MIN = 3;
function RESOLUTION_REASON_ACCEPTABLE(raw: string): boolean {
  const trimmed = raw.trim();
  return trimmed.length >= RESOLUTION_REASON_MIN && trimmed.length <= 512;
}

/** A short human name for the reviewed action, used by the unresolved-intent card and the action buttons. */
function actionLabel(action: ReturnReviewAction): string {
  switch (action) {
    case "receipt":
      return "ثبت دریافت کالا";
    case "approve":
      return "تأیید مرجوعی";
    case "reject":
      return "رد مرجوعی";
    case "start_refund":
      return "بازگشت وجه";
    case "resume_refund":
      return "پیگیری بازگشت وجه";
    case "retry_refund":
      return "تلاش دوبارهٔ بازگشت وجه";
    case "reopen_for_review":
      return "بازگشایی برای بررسی دوباره";
    case "publish_resolution":
      return "بررسی مرجوعی";
    case "record_custody":
      return "تحویل کالای ردشده به مشتری";
    default:
      return "تعیین تکلیف کالا";
  }
}

/**
 * TB-279: the EFFECTIVE decision after the append-only revision history, falling back to the immutable baseline row.
 * A correction that returned the case to review reports "در انتظار بررسی دوباره" instead of an invented approval.
 */
function formatReturnEffectiveDecision(effective: string | null | undefined, baseline: string | null | undefined): string {
  const value = effective ?? baseline ?? null;
  if (!value) return "بدون تصمیم";
  if (value === "Approve") return "تأیید";
  if (value === "Reject") return "رد";
  if (value === "Reopen" || value === "Review") return "در انتظار بررسی دوباره";
  return value;
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
      return "همان درخواست بازگشت وجه دوباره ارسال شد.";
    case "retry_refund":
      return "تلاش دوبارهٔ بازگشت وجه ثبت شد.";
    case "reopen_for_review":
      return "پرونده برای بررسی دوباره بازگشایی شد.";
    case "publish_resolution":
      return "بررسی مرجوعی ثبت شد. حالا می‌توانید مرجوعی را تأیید یا رد کنید.";
    case "record_custody":
      return "تحویل کالای رد‌شده به مشتری ثبت شد.";
    case "settle_physical":
      return "وضعیت کالا مشخص شد.";
    default:
      return "اقدام ثبت شد.";
  }
}

import {
  collectReceiptItems,
  describeQuantityProblem,
  evaluateAcceptedPortion,
  evaluateQuantityEntry,
  formatCeilingFa,
  type QuantityProblem,
  type ReceiptDraftLine,
} from "../returns/return-receipt-quantities";
export {
  collectReceiptItems,
  describeQuantityProblem,
  evaluateAcceptedPortion,
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
  const [rejectReason, setRejectReason] = useState("");
  /** TB-279: the reviewed reason of an audited decision correction. */
  const [correctionReason, setCorrectionReason] = useState("");
  /**
   * TB-279 / TB-279-R2: the reviewed per-item/condition split of a legacy multi-item case. Nothing is preselected: the
   * operator states how much of EACH inspected condition is accepted, and the declined portions follow exactly from the
   * stored receipt (received − accepted) instead of being typed as an unrelated total.
   */
  const [resolutionDraft, setResolutionDraft] = useState<
    {
      returnItemId: string;
      label: string;
      unitLabel: string;
      requested: number;
      received: number;
      usable: number;
      damaged: number;
      acceptedUsable: string;
      acceptedDamaged: string;
      reason: string;
    }[]
  >([]);
  /** TB-279: the reviewed handback of ONE rejected received portion. */
  const [custodyDraft, setCustodyDraft] = useState<{
    returnItemId: string;
    quantity: string;
    condition: "Usable" | "Damaged";
    proof: string;
  }>({ returnItemId: "", quantity: "", condition: "Usable", proof: "" });
  const [receiptLines, setReceiptLines] = useState<ReceiptDraftLine[]>([]);
  /** TB-277: the blocked-quantity reason of the receipt dialog, shown inside the dialog instead of sending. */
  const [receiptProblem, setReceiptProblem] = useState<string | null>(null);
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
          toast.success(successLabel(intent.action));
        } else {
          toast.error(mapAdminErrorMessage(answer.code, "fa"));
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
          "نتیجهٔ این اقدام قطعی مشخص نیست؛ ممکن است ثبت شده باشد. این اقدام به‌صورت «ناتمام» نگه داشته شد. " +
          "همان اقدام را دوباره بفرستید؛ اگر قبلاً ثبت شده باشد، دوباره انجام نمی‌شود.",
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
    if (!snapshot) return;
    const money = firstRefundReviewMoney(snapshot);
    if (money.kind === "unavailable") {
      setNotice({ title: "اقدام انجام نشد", text: money.message });
      return;
    }
    const intent = buildReturnReviewIntent({
      action: "start_refund",
      target: "returns",
      returnRequestId,
      body: {
        kind: "start_refund",
        // TB-279-R2 (R1-A): the reviewed EFFECTIVE decision revision is frozen with the command, so a correction
        // published after this review refuses the start instead of sending money against an unread plan.
        expectedDecisionVersion: snapshot.decisionVersion ?? 0,
      },
    });
    if (!intent) {
      setNotice({ title: "اقدام انجام نشد", text: "بستهٔ این اقدام کامل نشد؛ هیچ درخواستی ارسال نشد." });
      return;
    }
    setReviewing(null);
    void send(intent);
  }, [returnRequestId, send, snapshot]);

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
          // TB-279-R2 (D): the reviewed decision and PLAN revisions are frozen with the decision, so a correction
          // committed after this review refuses it instead of blessing a plan/revision the operator never read.
          expectedDecisionVersion: snapshot?.decisionVersion ?? 0,
          expectedResolutionVersion: snapshot?.resolution?.version ?? 0,
        },
      });
      if (!intent) {
        setNotice({ title: "اقدام انجام نشد", text: "بستهٔ این اقدام کامل نشد؛ هیچ درخواستی ارسال نشد." });
        return;
      }
      setReviewing(null);
      void send(intent);
    },
    [returnRequestId, send, snapshot?.checkoutId, snapshot?.decisionVersion, snapshot?.resolution?.version],
  );

  const reviewRecovery = useCallback(
    (action: "resume_refund" | "retry_refund" | "settle_physical") => {
      let body: Record<string, unknown>;
      if (action === "resume_refund") {
        const attemptId = work?.pendingRefundAttemptId ?? null;
        const money = snapshot
          ? boundAttemptReviewMoney(
              snapshot,
              attemptId,
              "اطلاعات بازگشت وجه در جریان کامل نیست؛ برای جلوگیری از اقدام ناشناخته، درخواستی ارسال نشد.",
            )
          : { kind: "unavailable" as const, message: "اطلاعات پرونده برای پیگیری بازگشت وجه در دسترس نیست." };
        if (money.kind === "unavailable") {
          setNotice({ title: "اقدام انجام نشد", text: money.message });
          return;
        }
        body = { kind: "resume_pending_refund", expectedRefundAttemptId: attemptId };
      } else if (action === "retry_refund") {
        const failedId = work?.failedRefundAttemptId ?? null;
        const money = snapshot
          ? boundAttemptReviewMoney(
              snapshot,
              failedId,
              "اطلاعات بازگشت وجه ناموفق کامل نیست؛ برای جلوگیری از اقدام ناشناخته، درخواستی ارسال نشد.",
            )
          : { kind: "unavailable" as const, message: "اطلاعات پرونده برای تلاش دوباره در دسترس نیست." };
        if (money.kind === "unavailable") {
          setNotice({ title: "اقدام انجام نشد", text: money.message });
          return;
        }
        body = {
          expectedFailedAttemptId: failedId,
          // TB-279-R2 (R1-A): the reviewed decision revision travels with the retry too.
          expectedDecisionVersion: snapshot?.decisionVersion ?? 0,
        };
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
    [returnRequestId, send, work?.failedRefundAttemptId, work?.pendingRefundAttemptId, snapshot],
  );

  /**
   * TB-279: opens the correction review with a FRESH draft built from the stored facts. Nothing is preselected: the
   * operator states the reviewed reason, and the reviewed version is the one this page actually read.
   */
  const openCorrection = useCallback(() => {
    setCorrectionReason("");
    setReviewing("reopen_for_review");
  }, []);

  /**
   * TB-279: publishes the reviewed per-item split. Every line is bounded locally by the stored received quantity and by
   * the owning unit's own precision before anything is persisted or sent; the server re-checks the same facts.
   */
  const reviewResolution = useCallback(() => {
    if (!snapshot) return;
    const lines: {
      returnItemId: string;
      acceptedUsable: number;
      acceptedDamaged: number;
      rejectedReceivedUsable: number;
      rejectedReceivedDamaged: number;
      rejectedUnreceived: number;
      reason: string | null;
    }[] = [];
    for (const line of resolutionDraft) {
      const item = snapshot.items.find((entry) => entry.returnItemId === line.returnItemId);
      const places =
        item && typeof item.unitDecimalPlaces === "number" ? Math.min(Math.trunc(item.unitDecimalPlaces), MAX_QUANTITY_SCALE) : null;
      // TB-279-R2: each accepted portion is bounded by ITS OWN inspected condition, not by a shared total.
      const usableCeiling = unitsFromWireNumber(line.usable);
      const damagedCeiling = unitsFromWireNumber(line.damaged);
      // Empty accepted fields mean deliberate zero. A zero-received item has no quantity inputs and is accounted as
      // rejected-unreceived with its required reason; never invent a quantity or a generic reason.
      const acceptedUsable = evaluateAcceptedPortion(line.acceptedUsable.trim() === "" ? "0" : line.acceptedUsable, places, usableCeiling);
      const acceptedDamaged = evaluateAcceptedPortion(line.acceptedDamaged.trim() === "" ? "0" : line.acceptedDamaged, places, damagedCeiling);
      if (acceptedUsable.problem || acceptedDamaged.problem) {
        setNotice({
          title: "مقدار این قلم معتبر نیست",
          text: `قلم «${line.label}»: ${describeAcceptedCeilingProblem(
            (acceptedUsable.problem ? usableCeiling : damagedCeiling) ?? 0,
            line.unitLabel,
          )}`,
        });
        return;
      }
      // The declined RECEIVED portions are the exact complement of the accepted ones per condition, and the
      // never-received remainder is the stored physical fact. Nothing here is a free third total.
      const receivedUsable = usableCeiling ?? 0;
      const receivedDamaged = damagedCeiling ?? 0;
      const acceptedUsableUnits = acceptedUsable.units ?? 0;
      const acceptedDamagedUnits = acceptedDamaged.units ?? 0;
      if (compareUnits(acceptedUsableUnits, receivedUsable) > 0 || compareUnits(acceptedDamagedUnits, receivedDamaged) > 0) {
        setNotice({
          title: "مقدار پذیرفته‌شده بیشتر از دریافت‌شده است",
          text: `قلم «${line.label}»: مقدار پذیرفته‌شده نمی‌تواند از مقدار دریافت‌شدهٔ همان وضعیت بیشتر باشد.`,
        });
        return;
      }
      // TB-280: the owning writer refuses a plan line without a meaningful reason, so the SAME requirement is enforced
      // here — before any bytes are stored or sent — instead of labelling the field "optional".
      if (!RESOLUTION_REASON_ACCEPTABLE(line.reason)) {
        setNotice({
          title: "دلیل بررسی لازم است",
          text: `برای قلم «${line.label}» دلیل بررسی را بنویسید (دست‌کم ۳ نویسه).`,
        });
        return;
      }
      const requestedUnits = unitsFromWireNumber(line.requested) ?? 0;
      const receivedUnits = unitsFromWireNumber(line.received) ?? 0;
      lines.push({
        returnItemId: line.returnItemId,
        acceptedUsable: acceptedUsable.value ?? 0,
        acceptedDamaged: acceptedDamaged.value ?? 0,
        rejectedReceivedUsable: unitsToWireNumber(subtractUnits(receivedUsable, acceptedUsableUnits)) ?? 0,
        rejectedReceivedDamaged: unitsToWireNumber(subtractUnits(receivedDamaged, acceptedDamagedUnits)) ?? 0,
        rejectedUnreceived:
          unitsToWireNumber(
            compareUnits(receivedUnits, requestedUnits) >= 0 ? 0 : subtractUnits(requestedUnits, receivedUnits),
          ) ?? 0,
        reason: line.reason.trim(),
      });
    }
    if (lines.length === 0) {
      setNotice({ title: "بررسی خالی است", text: "برای ثبت بررسی، دست‌کم یک قلم لازم است." });
      return;
    }
    const occurrenceKey = createOccurrenceKey();
    const intent = buildReturnReviewIntent({
      action: "publish_resolution",
      target: "returns",
      returnRequestId,
      occurrenceKey,
      body: {
        occurrenceKey,
        expectedResolutionVersion: snapshot.resolution?.version ?? 0,
        lines,
      },
    });
    if (!intent) {
      setNotice({ title: "اقدام انجام نشد", text: "بستهٔ این اقدام کامل نشد؛ هیچ درخواستی ارسال نشد." });
      return;
    }
    setReviewing(null);
    void send(intent);
  }, [resolutionDraft, returnRequestId, send, snapshot]);

  /**
   * TB-279: records ONE actual handback occurrence of rejected received goods. The quantity is bounded by the rejected
   * portion this page read, and the proof reference is required — a handback without evidence is not a fact.
   */
  const reviewCustody = useCallback(() => {
    if (!snapshot) return;
    const line = snapshot.resolution?.lines.find((entry) => entry.returnItemId === custodyDraft.returnItemId);
    const item = snapshot.items.find((entry) => entry.returnItemId === custodyDraft.returnItemId);
    if (!line || !item) {
      setNotice({ title: "قلم انتخاب نشده است", text: "برای ثبت تحویل، قلم رد‌شده را انتخاب کنید." });
      return;
    }
    // TB-279-R2 (R1-A): the ceiling is the outstanding handback of THIS condition. A usable handback can never
    // discharge a damaged obligation, and the never-received remainder is not a handback at all.
    const alreadyHanded = (snapshot.custody ?? [])
      .filter(
        (entry) =>
          entry.returnItemId === custodyDraft.returnItemId
          && String(entry.condition).toLowerCase() === custodyDraft.condition.toLowerCase(),
      )
      .reduce((sum, entry) => sum + (unitsFromWireNumber(entry.quantity) ?? 0), 0);
    const owedOfCondition =
      custodyDraft.condition === "Damaged" ? line.rejectedReceivedDamaged : line.rejectedReceivedUsable;
    const owedUnits = unitsFromWireNumber(owedOfCondition) ?? 0;
    const remaining = compareUnits(alreadyHanded, owedUnits) >= 0 ? 0 : subtractUnits(owedUnits, alreadyHanded);
    const places = typeof item.unitDecimalPlaces === "number" ? Math.min(Math.trunc(item.unitDecimalPlaces), MAX_QUANTITY_SCALE) : null;
    const checked = evaluateQuantityEntry(custodyDraft.quantity, places, remaining);
    if (checked.problem) {
      setNotice({
        title: "مقدار تحویل معتبر نیست",
        text: "مقدار باید با دقت واحد همین قلم و در محدودهٔ باقی‌ماندهٔ رد‌شدهٔ همان وضعیت باشد.",
      });
      return;
    }
    if (custodyDraft.proof.trim().length < 3) {
      setNotice({ title: "شاهد تحویل لازم است", text: "شمارهٔ رسید/شاهد تحویل کالا را وارد کنید." });
      return;
    }
    const occurrenceKey = createOccurrenceKey();
    const intent = buildReturnReviewIntent({
      action: "record_custody",
      target: "returns",
      returnRequestId,
      occurrenceKey,
      body: {
        occurrenceKey,
        returnItemId: custodyDraft.returnItemId,
        quantity: checked.value ?? 0,
        condition: custodyDraft.condition,
        proofReference: custodyDraft.proof.trim(),
        // TB-279-R2: the reviewed proof kind is frozen with the command. Only an operator attestation is an available
        // owning fact today; a carrier delivered-back claim has no reader behind it and is refused by the server.
        proofKind: "operator_attestation",
      },
    });
    if (!intent) {
      setNotice({ title: "اقدام انجام نشد", text: "بستهٔ این اقدام کامل نشد؛ هیچ درخواستی ارسال نشد." });
      return;
    }
    setReviewing(null);
    void send(intent);
  }, [custodyDraft, returnRequestId, send, snapshot]);

  const closeReview = useCallback(() => {
    setReviewing(null);
    setReceiptProblem(null);
  }, []);  const discardUnreadable = useCallback(() => {
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

  // TB-279: an action is offered only when the owning work description published its exact code. The correction also
  // requires the owning assessment to still say it is available (a recorded money/physical/source effect refuses it).
  const actionCodes = work?.actionCodes ?? [];
  const correctionOffered = actionCodes.includes("reopen_for_review") && snapshot?.correction?.canReopenForReview !== false;
  const resolutionOffered = actionCodes.includes("publish_resolution");
  const custodyOffered =
    actionCodes.includes("record_custody") && (snapshot?.resolution?.lines.some((line) => line.rejected > 0) ?? false);

  // TB-280-R1 UX2: initialize from the EXACT per-condition accepted facts (never aggregate `accepted` into usable).
  // Empty fields remain a deliberate zero; only conditions with a positive received ceiling are editable later.
  const beginResolution = () => {
    if (!snapshot) return;
    setResolutionDraft(
      snapshot.items.map((item) => {
        const line = snapshot.resolution?.lines.find((entry) => entry.returnItemId === item.returnItemId);
        const receivedLine = snapshot.receiving?.lines.find((entry) => entry.returnItemId === item.returnItemId);
        const usable = receivedLine?.usable ?? 0;
        const damaged = receivedLine?.damaged ?? 0;
        const usableCeiling = unitsFromWireNumber(usable) ?? 0;
        const damagedCeiling = unitsFromWireNumber(damaged) ?? 0;
        return {
          returnItemId: item.returnItemId,
          label: item.productLabel ?? "کالای سفارش",
          unitLabel: item.unitLabel ?? "",
          requested: receivedLine?.requested ?? item.quantity,
          received: receivedLine?.received ?? 0,
          usable,
          damaged,
          acceptedUsable:
            usableCeiling > 0 && line && line.acceptedUsable !== null && line.acceptedUsable !== undefined
              ? formatQuantityDisplayFa(line.acceptedUsable)
              : "",
          acceptedDamaged:
            damagedCeiling > 0 && line && line.acceptedDamaged !== null && line.acceptedDamaged !== undefined
              ? formatQuantityDisplayFa(line.acceptedDamaged)
              : "",
          reason: line?.reason ?? "",
        };
      }),
    );
    setReviewing("publish_resolution");
  };

  const beginCustody = () => {
    if (!snapshot) return;
    const firstRejected = snapshot.resolution?.lines.find((entry) => entry.rejected > 0);
    setCustodyDraft({
      returnItemId: firstRejected?.returnItemId ?? "",
      quantity: "",
      condition: "Usable",
      proof: "",
    });
    setReviewing("record_custody");
  };

  /*
    TB-280: the shopkeeper sees ONE obvious next step instead of a wall of equally-weighted buttons. The order follows
    the real shop workflow (goods in → inspect → decide → money → handback), and every entry is still gated by the
    owning work description, so this surface can never offer an action the backend did not publish.
  */
  const actionPlan: Array<{
    id: string;
    label: string;
    hint: string;
    tone: "primary" | "danger";
    offered: boolean;
    start: () => void;
  }> = [
    {
      id: "record-receipt",
      label: "ثبت دریافت کالا",
      hint: "کالایی که مشتری آورده است را ثبت کنید.",
      tone: "primary",
      offered: work?.canRecordReceipt === true,
      start: () => setReviewing("receipt"),
    },
    {
      id: "publish-resolution",
      label: "بررسی مرجوعی",
      hint: "مشخص کنید چه مقدار از کالای دریافت‌شده سالم است و چه مقدار رد می‌شود.",
      tone: "primary",
      offered: resolutionOffered,
      start: beginResolution,
    },
    {
      id: "approve",
      label: "تأیید مرجوعی",
      hint: "تأیید فقط تصمیم را ثبت می‌کند؛ بازگشت وجه جداگانه انجام می‌شود.",
      tone: "primary",
      offered: work?.canDecide === true,
      start: () => setReviewing("approve"),
    },
    {
      id: "reject",
      label: "رد مرجوعی",
      hint: "درخواست مرجوعی مشتری رد می‌شود.",
      tone: "danger",
      offered: work?.canDecide === true,
      start: () => setReviewing("reject"),
    },
    {
      id: "start-refund",
      label: "بازگشت وجه",
      hint: "مبلغ تأییدشده به مشتری برگردانده می‌شود.",
      tone: "primary",
      offered: work?.canStartRefund === true,
      start: () => setReviewing("start_refund"),
    },
    {
      id: "resume-refund",
      label: "پیگیری بازگشت وجه",
      hint: "نتیجهٔ بازگشت وجهی که در جریان است دوباره بررسی می‌شود.",
      tone: "primary",
      offered: work?.canResumeRefund === true,
      start: () => setReviewing("resume_refund"),
    },
    {
      id: "retry-refund",
      label: "تلاش دوبارهٔ بازگشت وجه",
      hint: "بازگشت وجه قبلی ناموفق بوده و یک بار دیگر انجام می‌شود.",
      tone: "primary",
      offered: work?.canRetryRefund === true,
      start: () => setReviewing("retry_refund"),
    },
    {
      id: "record-custody",
      label: "تحویل کالای رد‌شده به مشتری",
      hint: "کالایی که پذیرفته نشد به مشتری برگردانده می‌شود.",
      tone: "primary",
      offered: custodyOffered,
      start: beginCustody,
    },
    {
      id: "settle-physical",
      label: "تعیین تکلیف کالا",
      hint: "وضعیت نهایی کالاهای دریافت‌شده ثبت می‌شود.",
      tone: "primary",
      offered: work?.canSettlePhysical === true,
      start: () => setReviewing("settle_physical"),
    },
    {
      id: "reopen-for-review",
      label: "بازگشایی برای بررسی دوباره",
      hint: "تصمیم قبلی دوباره بررسی می‌شود؛ سابقهٔ قبلی پاک نمی‌شود.",
      tone: "primary",
      offered: correctionOffered,
      start: openCorrection,
    },
  ];
  const offeredActions = actionPlan.filter((entry) => entry.offered);

  /*
    TB-280: what the company still owes back to the customer, per item and condition. It is the stored rejected portion
    minus the handbacks already recorded — the SAME numbers the handback ceiling uses, so the page and the command can
    never disagree.
  */
  const custodyOutstanding: Array<{
    returnItemId: string;
    productLabel: string;
    usable: number;
    damaged: number;
  }> = (snapshot?.resolution?.lines ?? [])
    .map((line) => {
      const handedUsable = (snapshot?.custody ?? [])
        .filter((entry) => entry.returnItemId === line.returnItemId && String(entry.condition).toLowerCase() === "usable")
        .reduce((sum, entry) => sum + (unitsFromWireNumber(entry.quantity) ?? 0), 0);
      const handedDamaged = (snapshot?.custody ?? [])
        .filter((entry) => entry.returnItemId === line.returnItemId && String(entry.condition).toLowerCase() === "damaged")
        .reduce((sum, entry) => sum + (unitsFromWireNumber(entry.quantity) ?? 0), 0);
      const owedUsable = unitsFromWireNumber(line.rejectedReceivedUsable) ?? 0;
      const owedDamaged = unitsFromWireNumber(line.rejectedReceivedDamaged) ?? 0;
      return {
        returnItemId: line.returnItemId,
        productLabel: snapshot?.items.find((item) => item.returnItemId === line.returnItemId)?.productLabel ?? "کالای سفارش",
        usable: compareUnits(handedUsable, owedUsable) >= 0 ? 0 : unitsToWireNumber(subtractUnits(owedUsable, handedUsable)) ?? 0,
        damaged:
          compareUnits(handedDamaged, owedDamaged) >= 0 ? 0 : unitsToWireNumber(subtractUnits(owedDamaged, handedDamaged)) ?? 0,
      };
    })
    .filter((entry) => entry.usable > 0 || entry.damaged > 0);

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
            نتیجهٔ این اقدام قطعی مشخص نیست. با زدن دکمهٔ زیر همان اقدام دوباره فرستاده می‌شود؛ اگر قبلاً ثبت شده باشد،
            دوباره انجام نمی‌شود.
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
            <div className="flex flex-wrap items-start justify-between gap-4">
              <div className="min-w-0">
                <h2 className="font-semibold">وضعیت پرونده</h2>
                <p className="mt-1 text-sm text-muted" data-testid="admin-return-workspace-detail">
                  {describeNextStep(snapshot, work)}
                </p>
                {work?.blockerCode ? (
                  <p className="mt-2 text-sm text-amber-700">{mapAdminErrorMessage(work.blockerCode, "fa")}</p>
                ) : null}
              </div>
              <dl className="grid grid-cols-1 gap-x-6 gap-y-2 text-sm sm:grid-cols-2">
                <dt className="text-muted">مبلغ درخواستی مشتری</dt>
                <dd data-testid="admin-return-requested-amount">{formatRial(snapshot.refundAmount, snapshot.currency)}</dd>
                {snapshot.approvedAmount !== null && snapshot.approvedAmount !== undefined ? (
                  <>
                    <dt className="text-muted">مبلغ تأییدشده</dt>
                    <dd data-testid="admin-return-approved-amount">{formatRial(snapshot.approvedAmount, snapshot.currency)}</dd>
                  </>
                ) : null}
                <dt className="text-muted">بازگشت وجه</dt>
                <dd data-testid="admin-return-refund-money">{describeRefundMoney(snapshot)}</dd>
                <dt className="text-muted">مقصد بازگشت وجه</dt>
                <dd>{formatRefundDestination(snapshot.destination)}</dd>
                <dt className="text-muted">دریافت کالا</dt>
                <dd data-testid="admin-return-received-summary">{describeReceivedGoodsSummary(snapshot)}</dd>
                <dt className="text-muted">وضعیت مرجوعی</dt>
                <dd><span className={returnStatusBadgeClass(snapshot.status)}>{formatReturnStatus(snapshot.status)}</span></dd>
                <dt className="text-muted">تصمیم</dt>
                <dd>{formatReturnEffectiveDecision(snapshot.effectiveDecision, snapshot.recordedDecision)}</dd>
                <dt className="text-muted">منشأ مرجوعی</dt>
                <dd>{snapshot.origin === "CourierOrigin" ? "برگشتی از حامل" : "تحویل مشتری به فروشنده"}</dd>
              </dl>
            </div>
            {snapshot.reason ? <p className="mt-3 text-sm">دلیل درخواست مشتری: {snapshot.reason}</p> : null}
            {snapshot.decisionReason ? <p className="mt-1 text-sm">دلیل تصمیم اپراتور: {snapshot.decisionReason}</p> : null}
          </section>

          <section
            className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm"
            data-testid="admin-return-next-step"
          >
            <h2 className="font-semibold">قدم بعدی</h2>
            {work && offeredActions.length > 0 ? (
              <>
                <p className="mt-1 text-sm text-muted">
                  هر اقدام در پنجرهٔ خودش باز می‌شود و تا تأیید شما هیچ درخواستی ارسال نمی‌شود.
                </p>
                {(() => {
                  const [primaryAction, ...otherActions] = offeredActions;
                  return (
                    <>
                      {/*
                        TB-280: the ONE obvious action is a full-width, labelled button with its own sentence — a
                        shopkeeper never has to guess which of ten buttons is the safe next step.
                      */}
                      <button
                        type="button"
                        data-testid={`admin-return-action-${primaryAction.id}`}
                        className={
                          primaryAction.tone === "danger"
                            ? "mt-3 w-full min-h-12 rounded-2xl border border-red-300 bg-red-50 px-5 py-3 text-start text-red-800 disabled:opacity-50"
                            : "mt-3 w-full min-h-12 rounded-2xl bg-primary px-5 py-3 text-start text-primary-foreground disabled:opacity-50"
                        }
                        disabled={commandsBlocked}
                        onClick={primaryAction.start}
                      >
                        <span className="block text-base font-bold">{primaryAction.label}</span>
                        <span className="mt-1 block text-xs font-normal opacity-90">{primaryAction.hint}</span>
                      </button>
                      {otherActions.length > 0 ? (
                        <div className="mt-4" data-testid="admin-return-other-actions">
                          <p className="text-xs font-semibold text-muted">اقدام‌های دیگر</p>
                          <div className="mt-2 flex flex-wrap gap-2">
                            {otherActions.map((action) => (
                              <ActionButton
                                key={action.id}
                                id={action.id}
                                label={action.label}
                                tone={action.tone === "danger" ? "danger" : "secondary"}
                                onClick={action.start}
                                disabled={commandsBlocked}
                              />
                            ))}
                          </div>
                        </div>
                      ) : null}
                    </>
                  );
                })()}
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
          <section className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm">
            <h2 className="font-semibold">کالاها</h2>
            <div className="mt-3 overflow-x-auto">
              <table className="w-full min-w-[420px] text-sm">
                <thead className="text-muted">
                  <tr>
                    <th className="py-2 text-start">کالا / واحد</th>
                    <th className="py-2 text-start">درخواست</th>
                    <th className="py-2 text-start">دریافت‌شده</th>
                    <th className="py-2 text-start">باقی‌ماندهٔ دریافت</th>
                    <th className="py-2 text-start">سالم</th>
                    <th className="py-2 text-start">معیوب</th>
                    <th className="py-2 text-start">وضعیت کالا</th>
                  </tr>
                </thead>
                <tbody>
                  {snapshot.items.map((item, index) => {
                    const line = snapshot.receiving?.lines.find((entry) => entry.returnItemId === item.returnItemId);
                    const requestedUnits = unitsFromWireNumber(line?.requested ?? item.quantity) ?? 0;
                    const receivedUnits = unitsFromWireNumber(line?.received ?? 0) ?? 0;
                    const remainingUnits =
                      compareUnits(receivedUnits, requestedUnits) >= 0 ? 0 : subtractUnits(requestedUnits, receivedUnits);
                    return (
                      <tr key={item.returnItemId} className="border-t border-border">
                        <td className="py-2" data-testid={`admin-return-item-product-${index}`}>
                          <span className="block font-semibold">{item.productLabel ?? "کالای سفارش"}</span>
                          {/* The unit is shown only when the owning line answered it; a missing unit is unknown, not «واحد». */}
                          {item.unitLabel ? (
                            <span className="block text-xs text-muted">واحد: {formatUnitLabel(item.unitLabel)}</span>
                          ) : null}
                        </td>
                        <td className="py-2">{formatQuantityDisplayFa(unitsToWireNumber(requestedUnits))}</td>
                        <td className="py-2">{formatQuantityDisplayFa(unitsToWireNumber(receivedUnits))}</td>
                        <td className="py-2">{formatQuantityDisplayFa(unitsToWireNumber(remainingUnits))}</td>
                        <td className="py-2">{formatQuantityDisplayFa(line?.usable ?? 0)}</td>
                        <td className="py-2">{formatQuantityDisplayFa(line?.damaged ?? 0)}</td>
                        <td className="py-2" data-testid={`admin-return-item-disposition-${index}`}>
                          {formatReturnDisposition(item.disposition, item.saleableRestockedQuantity)}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
            {snapshot.receiving && !snapshot.receiving.isCoherent ? (
              <p className="mt-3 text-sm text-red-700">اطلاعات دریافت با درخواست هم‌خوان نیست و نیاز به بررسی دارد.</p>
            ) : null}
          </section>

          {/*
            TB-280: outstanding rejected goods stay VISIBLE — a shopkeeper must see what the company still owes back to
            the customer. The numbers are the SAME stored rejected portions and recorded handbacks, only the wording
            changed from "تفکیک" vocabulary to the goods that are still with the company.
          */}
          {custodyOutstanding.length > 0 ? (
            <section
              className="rounded-2xl border border-amber-300 bg-amber-50 p-5 text-sm text-amber-900 shadow-sm"
              data-testid="admin-return-custody-remaining"
            >
              <h2 className="font-semibold">کالای ردشده که باید به مشتری برگردد</h2>
              <ul className="mt-3 space-y-2">
                {custodyOutstanding.map((entry) => (
                  <li key={`custody-outstanding-${entry.returnItemId}`} className="flex flex-wrap items-center gap-x-3 gap-y-1">
                    <span className="font-semibold">{entry.productLabel}</span>
                    {entry.usable > 0 ? (
                      <span>
                        سالم: {formatQuantityDisplayFa(unitsToWireNumber(entry.usable))} در اختیار شرکت
                      </span>
                    ) : null}
                    {entry.damaged > 0 ? (
                      <span>
                        معیوب: {formatQuantityDisplayFa(unitsToWireNumber(entry.damaged))} در اختیار شرکت
                      </span>
                    ) : null}
                  </li>
                ))}
              </ul>
              <p className="mt-2 text-xs">با ثبت هر تحویل، مقدار باقی‌مانده کم می‌شود.</p>
            </section>
          ) : null}

          <details
            className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm"
            data-testid="admin-return-history"
          >
            <summary className="cursor-pointer text-base font-semibold">سابقه</summary>
            <p className="mt-1 text-sm text-muted">
              برای پیگیری؛ همهٔ این موارد ثبت‌شده‌اند و تغییر نمی‌کنند.
            </p>
            <div className="mt-4">
              <h3 className="font-semibold">دریافت‌های ثبت‌شده</h3>
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

              <h3 className="mt-5 font-semibold">بازگشت وجه</h3>
            {snapshot.refundAttempts.length > 0 ? (
              <ul className="mt-3 space-y-2 text-sm">
                {snapshot.refundAttempts.map((attempt) => (
                  <li key={attempt.refundAttemptId} className="flex flex-wrap items-center justify-between gap-2 rounded-xl border border-border px-3 py-2">
                    <span>{formatRial(attempt.amount, attempt.currency)}</span>
                    <span>{formatRefundAttemptStatus(attempt.status)}</span>
                    <span className="text-muted">{formatReturnDate(attempt.createdAt)}</span>
                    {attempt.failureCode ? <span className="text-red-700">{mapAdminErrorMessage(attempt.failureCode, "fa")}</span> : null}
                  </li>
                ))}
              </ul>
            ) : (
              <p className="mt-2 text-sm text-muted">هنوز بازگشت وجهی ثبت نشده است.</p>
            )}

          {snapshot.resolution || (snapshot.decisionHistory?.length ?? 0) > 0 || (snapshot.custody?.length ?? 0) > 0 ? (
            <div>
              <h3 className="mt-5 font-semibold">بررسی و تصمیم‌های ثبت‌شده</h3>
              {(snapshot.decisionHistory?.length ?? 0) > 0 ? (
                <div className="mt-3">
                  <h4 className="text-sm font-semibold text-muted">تصمیم‌ها</h4>
                  <ul className="mt-2 space-y-1 text-sm">
                    {snapshot.decisionHistory!.map((revision) => (
                      <li key={`${revision.version}-${revision.recordedAt}`} className="flex flex-wrap items-center gap-2">
                        <span>{formatReturnEffectiveDecision(revision.decision, null)}</span>
                        {revision.reason ? <span className="text-muted">— {revision.reason}</span> : null}
                        <span className="text-muted">{formatReturnDate(revision.recordedAt)}</span>
                      </li>
                    ))}
                  </ul>
                </div>
              ) : null}
              {snapshot.resolution ? (
                <div className="mt-4 overflow-x-auto">
                  <h4 className="text-sm font-semibold text-muted">بررسی ثبت‌شده</h4>
                  <table className="mt-2 w-full min-w-[420px] text-sm">
                    <thead className="text-muted">
                      <tr>
                        <th className="py-2 text-start">کالا</th>
                        <th className="py-2 text-start">دریافت‌شده</th>
                        <th className="py-2 text-start">پذیرفته‌شده</th>
                        <th className="py-2 text-start">ردشده</th>
                        <th className="py-2 text-start">دلیل</th>
                      </tr>
                    </thead>
                    <tbody>
                      {snapshot.resolution.lines.map((line) => {
                        const item = snapshot.items.find((entry) => entry.returnItemId === line.returnItemId);
                        return (
                          <tr key={line.returnItemId} className="border-t border-border">
                            <td className="py-2">{item?.productLabel ?? "کالای سفارش"}</td>
                            <td className="py-2">{formatQuantityDisplayFa(line.received)}</td>
                            <td className="py-2">{formatQuantityDisplayFa(line.accepted)}</td>
                            <td className="py-2">{formatQuantityDisplayFa(line.rejected)}</td>
                            <td className="py-2">{line.reason ?? "—"}</td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                  {snapshot.resolution.blockerCode ? (
                    <p className="mt-2 text-sm text-amber-700">
                      {mapAdminErrorMessage(snapshot.resolution.blockerCode, "fa")}
                    </p>
                  ) : null}
                </div>
              ) : null}
              {(snapshot.custody?.length ?? 0) > 0 ? (
                <div className="mt-4">
                  <h3 className="text-sm font-semibold text-muted">تحویل کالای رد‌شده</h3>
                  <ul className="mt-2 space-y-1 text-sm">
                    {snapshot.custody!.map((entry) => {
                      const item = snapshot.items.find((row) => row.returnItemId === entry.returnItemId);
                      return (
                        <li key={entry.returnCustodyId} className="flex flex-wrap items-center gap-2">
                          <span>{item?.productLabel ?? "کالای سفارش"}</span>
                          <span>{formatQuantityDisplayFa(entry.quantity)}</span>
                          <span>{entry.condition === "Usable" ? "سالم" : "معیوب"}</span>
                          <span className="text-muted">شاهد: {entry.proofReference}</span>
                          <span className="text-muted">{formatReturnDate(entry.recordedAt)}</span>
                        </li>
                      );
                    })}
                  </ul>
                </div>
              ) : null}
            </div>
          ) : null}
            </div>
          </details>


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
                  «بازگشت وجه» انجام می‌شود.
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
            <Dialog open title="بازگشت وجه" onClose={closeReview} showCloseButton={false}>
              <div className="space-y-3 text-sm" data-testid="admin-return-start-refund-form">
                {(() => {
                  const money = firstRefundReviewMoney(snapshot);
                  return (
                    <>
                      <p>
                        این اقدام، <strong>بازگشت وجه این پرونده</strong> را ثبت و به درگاه پرداخت ارسال می‌کند:
                      </p>
                      <dl className="grid grid-cols-2 gap-x-4 gap-y-1 rounded-xl border border-border p-3">
                        <dt className="text-muted">مبلغ</dt>
                        <dd data-testid="admin-return-start-refund-amount">
                          {money.kind === "amount" ? formatRial(money.amount, money.currency) : "نامشخص"}
                        </dd>
                        <dt className="text-muted">مقصد</dt>
                        <dd>{formatRefundDestination(snapshot.destination)}</dd>
                        <dt className="text-muted">پرداخت مبدأ</dt>
                        <dd>{snapshot.paymentId ? "به همین پرونده متصل است" : "ثبت نشده"}</dd>
                        <dt className="text-muted">تصمیم ثبت‌شده</dt>
                        <dd>تأیید مرجوعی</dd>
                      </dl>
                      {money.kind === "unavailable" ? (
                        <p className="text-amber-800" role="alert" data-testid="admin-return-start-refund-unavailable">
                          {money.message}
                        </p>
                      ) : (
                        <p className="text-muted">
                          تأیید مرجوعی به‌تنهایی هیچ وجهی جابه‌جا نمی‌کند؛ این پنجره همان اقدام مالی جداگانه است. اگر دوباره
                          کلیک کنید یا پاسخ قطع شود، همان درخواست تکرار می‌شود و مبلغ دوباره کم نمی‌شود.
                        </p>
                      )}
                      <div className="flex flex-wrap justify-end gap-2">
                        <Button tone="secondary" type="button" onClick={closeReview}>
                          انصراف
                        </Button>
                        <Button
                          type="button"
                          disabled={money.kind === "unavailable"}
                          onClick={reviewStartRefund}
                          data-testid="admin-return-start-refund-confirm"
                        >
                          بازگشت وجه
                        </Button>
                      </div>
                    </>
                  );
                })()}
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
                {(() => {
                  const money = boundAttemptReviewMoney(
                    snapshot,
                    work?.pendingRefundAttemptId,
                    "اطلاعات بازگشت وجه در جریان کامل نیست؛ برای جلوگیری از اقدام ناشناخته، درخواستی ارسال نشد.",
                  );
                  return (
                    <>
                      <p>
                        نتیجهٔ بازگشت وجهی که در جریان است دوباره بررسی می‌شود؛ همین مبلغ پیگیری می‌شود و بازگشت وجه تازه‌ای
                        ساخته نمی‌شود.
                      </p>
                      <dl className="grid grid-cols-2 gap-x-4 gap-y-1 rounded-xl border border-border p-3">
                        <dt className="text-muted">مبلغ</dt>
                        <dd data-testid="admin-return-resume-amount">
                          {money.kind === "amount" ? formatRial(money.amount, money.currency) : "نامشخص"}
                        </dd>
                        <dt className="text-muted">مقصد</dt>
                        <dd>{formatRefundDestination(snapshot.destination)}</dd>
                      </dl>
                      {money.kind === "unavailable" ? (
                        <p className="text-amber-800" role="alert" data-testid="admin-return-resume-unavailable">
                          {money.message}
                        </p>
                      ) : null}
                      <div className="flex flex-wrap justify-end gap-2">
                        <Button tone="secondary" type="button" onClick={closeReview}>
                          انصراف
                        </Button>
                        <Button
                          type="button"
                          disabled={money.kind === "unavailable"}
                          onClick={() => reviewRecovery("resume_refund")}
                          data-testid="admin-return-resume-confirm"
                        >
                          ارسال دوبارهٔ همان تلاش
                        </Button>
                      </div>
                    </>
                  );
                })()}
              </div>
            </Dialog>
          ) : null}

          {reviewing === "retry_refund" ? (
            <Dialog open title="تلاش دوبارهٔ بازگشت وجه" onClose={closeReview} showCloseButton={false}>
              <div className="space-y-3 text-sm" data-testid="admin-return-retry-form">
                {(() => {
                  const money = boundAttemptReviewMoney(
                    snapshot,
                    work?.failedRefundAttemptId,
                    "اطلاعات بازگشت وجه ناموفق کامل نیست؛ برای جلوگیری از اقدام ناشناخته، درخواستی ارسال نشد.",
                  );
                  return (
                    <>
                      <p>
                        بازگشت وجهی که قبلاً <strong>ناموفق</strong> بوده یک بار دیگر انجام می‌شود؛ مبلغ دوباره کم نمی‌شود.
                      </p>
                      <dl className="grid grid-cols-2 gap-x-4 gap-y-1 rounded-xl border border-border p-3">
                        <dt className="text-muted">مبلغ</dt>
                        <dd data-testid="admin-return-retry-amount">
                          {money.kind === "amount" ? formatRial(money.amount, money.currency) : "نامشخص"}
                        </dd>
                        <dt className="text-muted">مقصد</dt>
                        <dd>{formatRefundDestination(snapshot.destination)}</dd>
                      </dl>
                      {money.kind === "unavailable" ? (
                        <p className="text-amber-800" role="alert" data-testid="admin-return-retry-unavailable">
                          {money.message}
                        </p>
                      ) : null}
                      <div className="flex flex-wrap justify-end gap-2">
                        <Button tone="secondary" type="button" onClick={closeReview}>
                          انصراف
                        </Button>
                        <Button
                          type="button"
                          disabled={money.kind === "unavailable"}
                          onClick={() => reviewRecovery("retry_refund")}
                          data-testid="admin-return-retry-confirm"
                        >
                          ثبت تلاش دوباره
                        </Button>
                      </div>
                    </>
                  );
                })()}
              </div>
            </Dialog>
          ) : null}

          {reviewing === "reopen_for_review" ? (
            <Dialog open title="بازگشایی پرونده برای بررسی دوباره" onClose={closeReview} showCloseButton={false}>
              <div className="space-y-3 text-sm" data-testid="admin-return-reopen-form">
                <p>
                  با این اقدام، پرونده از وضعیت تصمیم‌گرفته‌شده به <strong>بررسی دوباره</strong> برمی‌گردد و
                  <strong> تصمیم تازه</strong> در سابقهٔ همین پرونده ثبت می‌شود. تصمیم‌های قبلی پاک نمی‌شوند.
                </p>
                <p className="text-muted">
                  این اقدام فقط تا زمانی ممکن است که هیچ اثر مالی یا فیزیکی ثبت نشده باشد. اگر بازگشت وجه یا تعیین تکلیف
                  کالا قبلاً ثبت شده باشد، سرور این درخواست را با دلیل مشخص رد می‌کند و چیزی تغییر نمی‌کند.
                </p>
                <dl className="grid grid-cols-2 gap-x-4 gap-y-1 rounded-xl border border-border p-3">
                  <dt className="text-muted">تصمیم فعلی</dt>
                  <dd>{formatReturnEffectiveDecision(snapshot.effectiveDecision, snapshot.recordedDecision)}</dd>
                </dl>
                <label className="block">
                  <span className="block text-muted">دلیل بازگشایی (حداقل ۳ نویسه)</span>
                  <textarea
                    className="mt-1 min-h-24 w-full rounded-xl border border-border bg-surface p-3 text-sm"
                    value={correctionReason}
                    onChange={(event) => setCorrectionReason(event.target.value)}
                    data-testid="admin-return-reopen-reason"
                  />
                </label>
                {correctionReason.trim().length > 0 && correctionReason.trim().length < 3 ? (
                  <p className="text-sm text-red-700">دلیل بازگشایی باید دست‌کم ۳ نویسه باشد.</p>
                ) : null}
                <div className="flex flex-wrap justify-end gap-2">
                  <Button tone="secondary" type="button" onClick={closeReview}>
                    انصراف
                  </Button>
                  <Button
                    type="button"
                    disabled={correctionReason.trim().length < 3}
                    onClick={() => {
                      const reason = correctionReason.trim();
                      if (reason.length < 3) return;
                      const occurrenceKey = createOccurrenceKey();
                      const intent = buildReturnReviewIntent({
                        action: "reopen_for_review",
                        target: "returns",
                        returnRequestId,
                        occurrenceKey,
                        body: {
                          occurrenceKey,
                          reason,
                          expectedDecisionVersion: snapshot.decisionVersion ?? 0,
                        },
                      });
                      if (!intent) {
                        setNotice({ title: "اقدام انجام نشد", text: "بستهٔ این اقدام کامل نشد؛ هیچ درخواستی ارسال نشد." });
                        return;
                      }
                      setReviewing(null);
                      void send(intent);
                    }}
                    data-testid="admin-return-reopen-confirm"
                  >
                    ثبت بازگشایی
                  </Button>
                </div>
              </div>
            </Dialog>
          ) : null}

          {reviewing === "publish_resolution" ? (
            <Dialog open size="lg" title="بررسی مرجوعی" onClose={closeReview} showCloseButton={false}>
              <div className="space-y-3 text-sm" data-testid="admin-return-resolution-form">
                <p>
                  برای هر قلم مشخص کنید چه مقدار از کالای <strong>دریافت‌شده</strong> پذیرفته می‌شود. مبلغ قابل بازگشت فقط از
                  مقدار پذیرفته‌شده و قیمت ثبت‌شدهٔ همان قلم ساخته می‌شود؛ بخشی که پذیرفته نشود خودکار محاسبه می‌شود و مبلغی
                  دستی وارد نمی‌کنید.
                </p>
                <div className="max-h-80 space-y-3 overflow-y-auto pe-1">
                  {resolutionDraft.map((line, index) => {
                    const places = (() => {
                      const item = snapshot.items.find((entry) => entry.returnItemId === line.returnItemId);
                      return typeof item?.unitDecimalPlaces === "number"
                        ? Math.min(Math.trunc(item.unitDecimalPlaces), MAX_QUANTITY_SCALE)
                        : null;
                    })();
                    const usableCeiling = unitsFromWireNumber(line.usable);
                    const damagedCeiling = unitsFromWireNumber(line.damaged);
                    // TB-280: an EMPTY portion is "zero accepted", not an error — a red message on an untouched dialog
                    // is exactly the noise this task removes. Only a value the operator actually typed is checked.
                    const acceptedUsableCheck =
                      line.acceptedUsable.trim() === ""
                        ? null
                        : evaluateAcceptedPortion(line.acceptedUsable, places, usableCeiling);
                    const acceptedDamagedCheck =
                      line.acceptedDamaged.trim() === ""
                        ? null
                        : evaluateAcceptedPortion(line.acceptedDamaged, places, damagedCeiling);
                    // TB-279-R2: the declined portions come from the EXACT entered units (never a wire string re-parse).
                    const acceptedUsableUnits = acceptedUsableCheck?.units ?? 0;
                    const acceptedDamagedUnits = acceptedDamagedCheck?.units ?? 0;
                    const rejectedReceivedUsable =
                      compareUnits(acceptedUsableUnits, usableCeiling ?? 0) > 0
                        ? 0
                        : unitsToWireNumber(subtractUnits(usableCeiling ?? 0, acceptedUsableUnits));
                    const rejectedReceivedDamaged =
                      compareUnits(acceptedDamagedUnits, damagedCeiling ?? 0) > 0
                        ? 0
                        : unitsToWireNumber(subtractUnits(damagedCeiling ?? 0, acceptedDamagedUnits));
                    const rejectedUnreceived =
                      compareUnits(
                        unitsFromWireNumber(line.received) ?? 0,
                        unitsFromWireNumber(line.requested) ?? 0,
                      ) >= 0
                        ? 0
                        : unitsToWireNumber(
                            subtractUnits(
                              unitsFromWireNumber(line.requested) ?? 0,
                              unitsFromWireNumber(line.received) ?? 0,
                            ),
                          ) ?? 0;
                    const showUsable = (usableCeiling ?? 0) > 0;
                    const showDamaged = (damagedCeiling ?? 0) > 0;
                    const nothingReceived = !showUsable && !showDamaged;
                    return (
                      <div key={line.returnItemId} className="rounded-xl border border-border p-3" data-testid={`admin-return-resolution-line-${index}`}>
                        <p className="font-semibold">{line.label}</p>
                        <p className="text-muted">
                          واحد: {formatUnitLabel(line.unitLabel)} · درخواست: {formatQuantityDisplayFa(line.requested)} ·
                          دریافت‌شده: {formatQuantityDisplayFa(line.received)}
                        </p>
                        {nothingReceived ? (
                          <p className="mt-2 rounded-lg bg-amber-50 p-2 text-xs text-amber-800" data-testid={`admin-return-resolution-not-received-${index}`}>
                            این قلم دریافت نشده است؛ مقدار پذیرفته‌شده برای آن صفر می‌ماند و باقی درخواست به‌عنوان دریافت‌نشده
                            حساب می‌شود.
                          </p>
                        ) : (
                          <div className="mt-2 grid grid-cols-1 gap-2 sm:grid-cols-2">
                            {showUsable ? (
                              <label className="block">
                                <span className="block text-muted">
                                  پذیرفته‌شدهٔ سالم (حداکثر {formatQuantityDisplayFa(line.usable)} {formatUnitLabel(line.unitLabel)})
                                </span>
                                <input
                                  className="mt-1 min-h-11 w-full rounded-xl border border-border bg-surface px-3 text-sm"
                                  inputMode="decimal"
                                  value={line.acceptedUsable}
                                  aria-invalid={acceptedUsableCheck?.problem ? true : undefined}
                                  onChange={(event) => {
                                    const next = [...resolutionDraft];
                                    next[index] = { ...line, acceptedUsable: event.target.value };
                                    setResolutionDraft(next);
                                  }}
                                  data-testid={`admin-return-resolution-accepted-usable-${index}`}
                                />
                                {acceptedUsableCheck?.problem ? (
                                  <span className="mt-1 block text-xs text-red-700" role="alert">
                                    {describeAcceptedCeilingProblem(usableCeiling ?? 0, line.unitLabel)}
                                  </span>
                                ) : null}
                              </label>
                            ) : null}
                            {showDamaged ? (
                              <label className="block">
                                <span className="block text-muted">
                                  پذیرفته‌شدهٔ معیوب (حداکثر {formatQuantityDisplayFa(line.damaged)} {formatUnitLabel(line.unitLabel)})
                                </span>
                                <input
                                  className="mt-1 min-h-11 w-full rounded-xl border border-border bg-surface px-3 text-sm"
                                  inputMode="decimal"
                                  value={line.acceptedDamaged}
                                  aria-invalid={acceptedDamagedCheck?.problem ? true : undefined}
                                  onChange={(event) => {
                                    const next = [...resolutionDraft];
                                    next[index] = { ...line, acceptedDamaged: event.target.value };
                                    setResolutionDraft(next);
                                  }}
                                  data-testid={`admin-return-resolution-accepted-damaged-${index}`}
                                />
                                {acceptedDamagedCheck?.problem ? (
                                  <span className="mt-1 block text-xs text-red-700" role="alert">
                                    {describeAcceptedCeilingProblem(damagedCeiling ?? 0, line.unitLabel)}
                                  </span>
                                ) : null}
                              </label>
                            ) : null}
                          </div>
                        )}
                        <p className="mt-2 text-xs text-muted" data-testid={`admin-return-resolution-derived-${index}`}>
                          رد‌شدهٔ دریافت‌شده: سالم {formatQuantityDisplayFa(rejectedReceivedUsable)} · معیوب{" "}
                          {formatQuantityDisplayFa(rejectedReceivedDamaged)} · دریافت‌نشده{" "}
                          {formatQuantityDisplayFa(rejectedUnreceived)}
                        </p>
                        <label className="mt-2 block">
                          <span className="block text-muted">دلیل این قلم (الزامی)</span>
                          <input
                            className="mt-1 min-h-11 w-full rounded-xl border border-border bg-surface px-3 text-sm"
                            value={line.reason}
                            onChange={(event) => {
                              const next = [...resolutionDraft];
                              next[index] = { ...line, reason: event.target.value };
                              setResolutionDraft(next);
                            }}
                            data-testid={`admin-return-resolution-reason-${index}`}
                          />
                          <span className="mt-1 block text-xs text-muted">
                            چرا این مقدار پذیرفته یا رد شد؟ دست‌کم ۳ نویسه.
                          </span>
                          {line.reason.length > 0 && !RESOLUTION_REASON_ACCEPTABLE(line.reason) ? (
                            <span className="mt-1 block text-xs text-red-700">دلیل باید دست‌کم ۳ نویسه باشد.</span>
                          ) : null}
                        </label>
                      </div>
                    );
                  })}
                </div>
                <div className="flex flex-wrap justify-end gap-2">
                  <Button tone="secondary" type="button" onClick={closeReview}>
                    انصراف
                  </Button>
                  <Button type="button" onClick={reviewResolution} data-testid="admin-return-resolution-confirm">
                    ثبت بررسی
                  </Button>
                </div>
              </div>
            </Dialog>
          ) : null}

          {reviewing === "record_custody" ? (
            <Dialog open title="تحویل کالای رد‌شده به مشتری" onClose={closeReview} showCloseButton={false}>
              <div className="space-y-3 text-sm" data-testid="admin-return-custody-form">
                <p>
                  با این اقدام، بخش <strong>رد‌شدهٔ دریافت‌شده</strong> به‌صورت واقعی به مشتری تحویل داده می‌شود و همین
                  واقعه با شاهد ثبت می‌شود. این اقدام هیچ وجهی جابه‌جا نمی‌کند.
                </p>
                <label className="block">
                  <span className="block text-muted">قلم</span>
                  <select
                    className="mt-1 min-h-11 w-full rounded-xl border border-border bg-surface px-3 text-sm"
                    value={custodyDraft.returnItemId}
                    onChange={(event) => setCustodyDraft({ ...custodyDraft, returnItemId: event.target.value })}
                    data-testid="admin-return-custody-item"
                  >
                    <option value="">انتخاب کنید…</option>
                    {(snapshot.resolution?.lines ?? [])
                      .filter(
                        (line) =>
                          (unitsFromWireNumber(line.rejectedReceivedUsable) ?? 0) > 0
                          || (unitsFromWireNumber(line.rejectedReceivedDamaged) ?? 0) > 0,
                      )
                      .map((line) => {
                        const item = snapshot.items.find((entry) => entry.returnItemId === line.returnItemId);
                        return (
                          <option key={line.returnItemId} value={line.returnItemId}>
                            {item?.productLabel ?? "کالای سفارش"}
                          </option>
                        );
                      })}
                  </select>
                </label>
                <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
                  <label className="block">
                    <span className="block text-muted">مقدار تحویل</span>
                    <input
                      className="mt-1 min-h-11 w-full rounded-xl border border-border bg-surface px-3 text-sm"
                      inputMode="decimal"
                      value={custodyDraft.quantity}
                      onChange={(event) => setCustodyDraft({ ...custodyDraft, quantity: event.target.value })}
                      data-testid="admin-return-custody-quantity"
                    />
                  </label>
                  <label className="block">
                    <span className="block text-muted">وضعیت کالا</span>
                    <select
                      className="mt-1 min-h-11 w-full rounded-xl border border-border bg-surface px-3 text-sm"
                      value={custodyDraft.condition}
                      onChange={(event) =>
                        setCustodyDraft({ ...custodyDraft, condition: event.target.value as "Usable" | "Damaged" })
                      }
                      data-testid="admin-return-custody-condition"
                    >
                      <option value="Usable">سالم</option>
                      <option value="Damaged">معیوب</option>
                    </select>
                  </label>
                </div>
                <label className="block">
                  <span className="block text-muted">شاهد تحویل (شمارهٔ رسید/صورت‌جلسه)</span>
                  <input
                    className="mt-1 min-h-11 w-full rounded-xl border border-border bg-surface px-3 text-sm"
                    value={custodyDraft.proof}
                    onChange={(event) => setCustodyDraft({ ...custodyDraft, proof: event.target.value })}
                    data-testid="admin-return-custody-proof"
                  />
                </label>
                <div className="flex flex-wrap justify-end gap-2">
                  <Button tone="secondary" type="button" onClick={closeReview}>
                    انصراف
                  </Button>
                  <Button type="button" onClick={reviewCustody} data-testid="admin-return-custody-confirm">
                    ثبت تحویل
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
  tone?: "primary" | "secondary" | "danger";
}) {
  return (
    <button
      type="button"
      data-testid={`admin-return-action-${id}`}
      className={
        tone === "danger"
          ? "min-h-11 rounded-xl border border-red-300 px-4 text-sm font-bold text-red-700 disabled:opacity-50"
          : tone === "secondary"
            ? "min-h-11 rounded-xl border border-border px-4 text-sm font-semibold hover:bg-secondary disabled:opacity-50"
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
    case "reopen_for_review":
      return classifyReviewAnswer(await sendReviewedReturnCommand(intent.returnRequestId, "reopen", body));
    case "publish_resolution":
      return classifyReviewAnswer(await sendReviewedReturnCommand(intent.returnRequestId, "resolution", body));
    case "record_custody":
      return classifyReviewAnswer(await sendReviewedReturnCommand(intent.returnRequestId, "custody", body));
    case "start_refund":
      // TB-277 / TB-279-R2 (R1-A): the deliberate FIRST-refund command carries the reviewed EFFECTIVE decision
      // revision on its reviewed route, so a newer correction refuses it before any money.
      return classifyReviewAnswer(
        await adminStartReturnRefund(
          intent.returnRequestId,
          typeof body.expectedDecisionVersion === "number" ? body.expectedDecisionVersion : null,
        ),
      );
    case "retry_refund":
      return classifyReviewAnswer(
        await adminRetryReturnRefund(
          intent.returnRequestId,
          (body.expectedFailedAttemptId as string | null) ?? null,
          typeof body.expectedDecisionVersion === "number" ? body.expectedDecisionVersion : null,
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
          // TB-279-R2 (D): the reviewed revisions ride with the frozen decision bytes.
          expectedDecisionVersion:
            typeof body.expectedDecisionVersion === "number" ? body.expectedDecisionVersion : null,
          expectedResolutionVersion:
            typeof body.expectedResolutionVersion === "number" ? body.expectedResolutionVersion : null,
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
