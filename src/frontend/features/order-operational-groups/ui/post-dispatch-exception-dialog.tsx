"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { Button } from "../../../design-system/primitives/core";
import { Dialog } from "../../../design-system/primitives/overlays";
import {
  clearPostDispatchAttempt,
  markPostDispatchAttemptFailed,
  mintPostDispatchOccurrence,
  readPostDispatchAttempt,
  writePostDispatchAttempt,
  type PostDispatchAttempt,
  type PostDispatchAttemptScope,
} from "../session/post-dispatch-attempt-store.ts";

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
  | "resend_shipment"
  | "resolve_damaged_goods";

/** TB-262: the two outcomes the server accepts for damaged goods received back at origin. */
export type DamagedResolutionOutcomeCode = "replacement" | "referral";

export const DAMAGED_RESOLUTION_OUTCOMES: ReadonlyArray<{
  code: DamagedResolutionOutcomeCode;
  labelFa: string;
  hintFa: string;
}> = [
  {
    code: "replacement",
    labelFa: "جایگزینی از موجودی سالم",
    hintFa: "همین کالا از موجودی سالم با رزرو تازه تأمین و در مرسوله‌ای جدا ارسال می‌شود؛ کالای آسیب‌دیده فروختنی نمی‌شود.",
  },
  {
    code: "referral",
    labelFa: "ارجاع به خدمات مشتری",
    hintFa: "تصمیم مالی/خدماتی بعدی بر عهدهٔ خدمات مشتری است؛ هیچ بازپرداخت یا موجودی خودکاری ثبت نمی‌شود.",
  },
];

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
  /** TB-262: damaged quantity of this parcel line that still needs a durable decision. */
  damagedOutstandingQuantity: number;
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
  | { code: "resend_shipment"; selections: Array<{ orderLineId: string; quantity: number }>; idempotencyKey: string }
  | {
    code: "resolve_damaged_goods";
    orderLineId: string;
    quantity: number;
    outcome: DamagedResolutionOutcomeCode;
    note: string | null;
    idempotencyKey: string;
  };

type Props = {
  open: boolean;
  mode: PostDispatchExceptionMode;
  /** Human label of the parcel (number/short id) used in the dialog copy and test ids. */
  parcelLabel: string;
  parcelKey: string;
  /**
   * TB-258-R1: viewer + checkout + fulfillment + source parcel + mode. The reviewed request is retained
   * under exactly this scope, so a remount restores it and another viewer never inherits it.
   */
  attemptScope: PostDispatchAttemptScope;
  lines: PostDispatchParcelLine[];
  pending: boolean;
  errorFa?: string | null;
  /**
   * TB-262-R1: an EXACT damaged decision that is durably recorded but still owes its replacement parcel. The
   * server-owned facts (line, quantity, outcome, occurrence key) are replayed verbatim, so a reload can continue
   * that decision instead of minting a new incompatible one.
   */
  damagedResume?: {
    orderLineId: string;
    quantity: number;
    outcome: DamagedResolutionOutcomeCode;
    occurrenceKey: string;
  } | null;
  onClose: () => void;
  /**
   * Sends the reviewed request. Returning true means the server AUTHORITATIVELY completed it (only then
   * is the retained envelope cleared); anything else keeps the exact request for reconcile/retry.
   */
  onSubmit: (request: PostDispatchExceptionRequest) => Promise<boolean> | boolean;
};

/**
 * TB-258-R2: one parcel line can come back SPLIT across physical conditions, so the operator records the
 * usable and the damaged quantity side by side in the same review. The server receipt is keyed on
 * (line, condition) and validates the SUM against what that parcel line carried.
 */
type ReceiptDraft = { usable: string; damaged: string };

/** One inline refusal of the reviewed receipt. Never a silent clamp: the operator sees what to fix. */
type ReceiptIssue = { orderLineId: string | null; messageFa: string };

/**
 * The ONE shared display copy of each post-dispatch exit. The parcel card, the reconcile entry and the
 * review dialog all name the same action the same way.
 */
export const POST_DISPATCH_MODE_LABEL_FA: Record<PostDispatchExceptionMode, string> = {
  record_non_delivery: "ثبت عدم تحویل",
  start_return: "شروع برگشت به مبدأ",
  record_return_receipt: "تأیید دریافت در مبدأ",
  resend_shipment: "ارسال مجدد",
  resolve_damaged_goods: "تعیین تکلیف کالای آسیب‌دیده",
};

const MODE_TITLE: Record<PostDispatchExceptionMode, string> = POST_DISPATCH_MODE_LABEL_FA;

function toNumber(value: string): number {
  const parsed = Number(value.replace(/[^\d.]/g, ""));
  return Number.isFinite(parsed) ? parsed : 0;
}

function clampQuantity(value: number, max: number): number {
  if (value <= 0) return 0;
  return max > 0 ? Math.min(value, max) : 0;
}

/** A review quantity is either a non-negative decimal or unparsable — never guessed, never truncated. */
function parseReceiptQuantity(value: string | undefined): number | null {
  const normalized = (value ?? "").trim();
  if (!normalized) return 0;
  const parsed = Number(normalized);
  return Number.isFinite(parsed) && parsed >= 0 ? parsed : null;
}

/**
 * The reviewed receipt must be representable in one request: positive-condition quantities only, and per
 * line the usable + damaged SUM may never exceed the quantity that parcel line actually carried.
 */
function receiptIssues(
  lines: PostDispatchParcelLine[],
  drafts: Record<string, ReceiptDraft>,
): ReceiptIssue[] {
  const issues: ReceiptIssue[] = [];
  let recorded = 0;
  for (const line of lines) {
    const draft = drafts[line.orderLineId];
    const usable = parseReceiptQuantity(draft?.usable);
    const damaged = parseReceiptQuantity(draft?.damaged);
    if (usable === null || damaged === null) {
      issues.push({ orderLineId: line.orderLineId, messageFa: "مقدار واردشده معتبر نیست." });
      continue;
    }
    recorded += usable + damaged;
    if (usable + damaged > line.parcelQuantity) {
      issues.push({
        orderLineId: line.orderLineId,
        messageFa: "مجموع «سالم» و «آسیب‌دیده» از مقدار این قلم در مرسوله بیشتر است.",
      });
    }
  }

  if (issues.length === 0 && recorded <= 0) {
    issues.push({ orderLineId: null, messageFa: "دست‌کم یک قلم سالم یا آسیب‌دیده باید در رسید ثبت شود." });
  }

  return issues;
}

/**
 * TB-258-R1: read-only description of a retained reviewed request. It is rendered from the RECORDED
 * envelope (not from fresh capacity), so a review can still be reconciled after the parcel changed.
 */
function describeRetained(
  request: PostDispatchExceptionRequest,
  lines: PostDispatchParcelLine[],
): Array<{ key: string; text: string }> {
  const titleOf = (orderLineId: string) =>
    lines.find((line) => line.orderLineId === orderLineId)?.title ?? orderLineId.slice(0, 8);

  if (request.code === "record_non_delivery") {
    const label = POST_DISPATCH_NON_DELIVERY_REASONS.find((option) => option.code === request.reason)?.labelFa
      ?? request.reason;
    return [{ key: "reason", text: `دلیل: ${label}` }];
  }

  if (request.code === "record_return_receipt") {
    return request.receiptItems.map((item) => ({
      key: `${item.orderLineId}:${item.condition}`,
      text: `${titleOf(item.orderLineId)} · ${item.quantity.toLocaleString("fa-IR")} · ${item.condition === 0 ? "سالم" : "آسیب‌دیده"}`,
    }));
  }

  if (request.code === "resend_shipment") {
    return request.selections.map((selection) => ({
      key: selection.orderLineId,
      text: `${titleOf(selection.orderLineId)} · ${selection.quantity.toLocaleString("fa-IR")}`,
    }));
  }

  if (request.code === "resolve_damaged_goods") {
    const outcomeLabel = DAMAGED_RESOLUTION_OUTCOMES.find((option) => option.code === request.outcome)?.labelFa
      ?? request.outcome;
    return [
      {
        key: request.orderLineId,
        text: `${titleOf(request.orderLineId)} · ${request.quantity.toLocaleString("fa-IR")} · ${outcomeLabel}`,
      },
    ];
  }

  return [];
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
  attemptScope,
  lines,
  pending,
  errorFa,
  damagedResume,
  onClose,
  onSubmit,
}: Props) {
  const scope = useMemo<PostDispatchAttemptScope>(
    () => ({
      viewerScopeKey: attemptScope.viewerScopeKey ?? null,
      checkoutId: attemptScope.checkoutId,
      fulfillmentId: attemptScope.fulfillmentId,
      sourceShipmentId: attemptScope.sourceShipmentId,
      mode,
    }),
    [
      attemptScope.viewerScopeKey,
      attemptScope.checkoutId,
      attemptScope.fulfillmentId,
      attemptScope.sourceShipmentId,
      mode,
    ],
  );
  const scopeSignature = `${scope.viewerScopeKey ?? ""}|${scope.checkoutId}|${scope.fulfillmentId}|${scope.sourceShipmentId}|${scope.mode}`;

  const [reason, setReason] = useState<string>(POST_DISPATCH_NON_DELIVERY_REASONS[0]!.code);
  const [note, setNote] = useState("");
  const [receiptDrafts, setReceiptDrafts] = useState<Record<string, ReceiptDraft>>({});
  const [resendDrafts, setResendDrafts] = useState<Record<string, string>>({});
  const [idempotencyKey, setIdempotencyKey] = useState<string>("");
  const [localErrorFa, setLocalErrorFa] = useState<string | null>(null);
  /** TB-262: the reviewed damaged-goods outcome and its per-line quantity draft. */
  const [damagedOutcome, setDamagedOutcome] = useState<DamagedResolutionOutcomeCode>("replacement");
  const [damagedDrafts, setDamagedDrafts] = useState<Record<string, string>>({});
  /** TB-258-R1: an unresolved reviewed request that must be reconciled verbatim before anything new. */
  const [retained, setRetained] = useState<PostDispatchAttempt | null>(null);
  /** Which review scope is already initialized — a lines rerender must never reset an open review. */
  const [initializedScope, setInitializedScope] = useState<string | null>(null);
  /** Synchronous single-flight: two same-tick submits can never send two mutations. */
  const submitInFlight = useRef(false);
  const linesRef = useRef(lines);
  linesRef.current = lines;

  const resendCandidates = useMemo(
    () => lines.filter((line) => line.resendableQuantity > 0),
    [lines],
  );

  useEffect(() => {
    if (!open) {
      setInitializedScope(null);
      return;
    }

    // Same reviewed surface: keep every edit, the key and the in-flight envelope untouched.
    if (initializedScope === scopeSignature) return;

    setLocalErrorFa(null);
    const recorded = readPostDispatchAttempt(scope);
    setInitializedScope(scopeSignature);
    if (recorded) {
      // Close/reopen/remount restores the EXACT reviewed request instead of minting another occurrence.
      setRetained(recorded);
      return;
    }

    setRetained(null);
    setNote("");
    setReason(POST_DISPATCH_NON_DELIVERY_REASONS[0]!.code);
    // A fresh occurrence per newly reviewed action: a retried submit replays this exact key.
    // TB-262-R1: a RECORDED pending decision reuses its OWN key, so the continuation answers that exact record.
    const resumeKey = mode === "resolve_damaged_goods" ? damagedResume?.occurrenceKey ?? null : null;
    setIdempotencyKey(resumeKey ?? mintPostDispatchOccurrence());

    const receipt: Record<string, ReceiptDraft> = {};
    for (const line of linesRef.current) {
      receipt[line.orderLineId] = {
        usable: String(line.parcelQuantity),
        damaged: "0",
      };
    }
    setReceiptDrafts(receipt);

    const resend: Record<string, string> = {};
    for (const line of linesRef.current) {
      resend[line.orderLineId] = line.resendableQuantity > 0 ? String(line.resendableQuantity) : "0";
    }
    setResendDrafts(resend);

    // TB-262: the damaged review starts at this parcel's own remaining damaged quantity, never a guess.
    const resumeDamage = mode === "resolve_damaged_goods" ? damagedResume ?? null : null;
    setDamagedOutcome(resumeDamage?.outcome ?? "replacement");
    const damaged: Record<string, string> = {};
    for (const line of linesRef.current) {
      damaged[line.orderLineId] = resumeDamage && resumeDamage.orderLineId === line.orderLineId
        ? String(resumeDamage.quantity)
        : line.damagedOutstandingQuantity > 0
          ? String(line.damagedOutstandingQuantity)
          : "0";
    }
    setDamagedDrafts(damaged);
  }, [open, scopeSignature, scope, initializedScope, mode, damagedResume]);

  const receiptProblems = useMemo(
    () => receiptIssues(lines, receiptDrafts),
    [lines, receiptDrafts],
  );

  const receiptProblemByLine = useMemo(() => {
    const byLine = new Map<string, string>();
    for (const issue of receiptProblems) {
      if (issue.orderLineId) byLine.set(issue.orderLineId, issue.messageFa);
    }
    return byLine;
  }, [receiptProblems]);

  const receiptTotal = useMemo(
    () => lines.reduce((sum, line) => {
      const draft = receiptDrafts[line.orderLineId];
      return sum + (parseReceiptQuantity(draft?.usable) ?? 0) + (parseReceiptQuantity(draft?.damaged) ?? 0);
    }, 0),
    [lines, receiptDrafts],
  );

  const resendTotal = useMemo(
    () => resendCandidates.reduce((sum, line) => sum + clampQuantity(toNumber(resendDrafts[line.orderLineId] ?? "0"), line.resendableQuantity), 0),
    [resendCandidates, resendDrafts],
  );

  // TB-262: only a line that still OWES a damaged decision is reviewable, and one reviewed request resolves
  // exactly one line so its occurrence identity stays exact.
  const damagedCandidates = useMemo(
    () => lines.filter((line) => line.damagedOutstandingQuantity > 0),
    [lines],
  );
  /** TB-262-R1: a recorded decision continued verbatim — it owns its line, quantity, outcome and occurrence key. */
  const resumeDamage = mode === "resolve_damaged_goods" ? damagedResume ?? null : null;
  const resumeLine = resumeDamage
    ? lines.find((line) => line.orderLineId === resumeDamage.orderLineId) ?? null
    : null;
  const [damagedLineId, setDamagedLineId] = useState<string | null>(null);
  const damagedLine = resumeDamage
    ? null
    : damagedCandidates.find((line) => line.orderLineId === damagedLineId)
      ?? damagedCandidates[0]
      ?? null;
  const damagedQuantity = resumeDamage
    ? resumeDamage.quantity
    : damagedLine
      ? parseReceiptQuantity(damagedDrafts[damagedLine.orderLineId] ?? "")
      : 0;
  const damagedInvalid = resumeDamage
    ? false
    : damagedLine === null
      || damagedQuantity === null
      || damagedQuantity <= 0
      || damagedQuantity > damagedLine.damagedOutstandingQuantity;

  /**
   * TB-258-R3: a NEW mutation is refused unless the COMPLETE reviewed request is reload-durable. The message
   * is actionable and the reviewed values stay in the form, so the operator can retry after freeing storage.
   */
  const STORAGE_NOT_DURABLE_FA =
    "ذخیرهٔ مطمئن این درخواست در این مرورگر ممکن نشد؛ برای جلوگیری از ارسال تکراری، درخواست ارسال نشد. حافظهٔ مرورگر را آزاد کنید و دوباره تلاش کنید.";

  async function send(request: PostDispatchExceptionRequest) {
    if (submitInFlight.current) return;
    submitInFlight.current = true;
    setLocalErrorFa(null);
    try {
      // The COMPLETE reviewed request is persisted BEFORE the first mutation, so an uncertain outcome can
      // only reconcile this exact envelope instead of creating a second operation. A memory-only write does
      // not survive a reload, so it can NEVER authorize a POST: the boundary fails closed here.
      const persisted = writePostDispatchAttempt({
        scope,
        request,
        attempts: (retained?.attempts ?? 0) + 1,
      });
      if (!persisted || !persisted.durable) {
        setLocalErrorFa(STORAGE_NOT_DURABLE_FA);
        return;
      }
      const attempt = persisted.attempt;

      const accepted = await onSubmit(request);
      if (accepted) {
        // Authoritative completion: only now may a deliberate new occurrence be minted.
        clearPostDispatchAttempt(scope);
        setRetained(null);
        return;
      }
      setRetained(markPostDispatchAttemptFailed(scope, attempt));
    } finally {
      submitInFlight.current = false;
    }
  }

  function submit() {
    // An unresolved recorded request may only be reconciled verbatim — never replaced or reminted.
    if (retained) {
      void send(retained.request);
      return;
    }

    setLocalErrorFa(null);
    if (mode === "record_non_delivery") {
      if (!POST_DISPATCH_NON_DELIVERY_REASONS.some((option) => option.code === reason)) {
        setLocalErrorFa("دلیل عدم تحویل را انتخاب کنید.");
        return;
      }
      void send({ code: "record_non_delivery", reason, note: note.trim() || null });
      return;
    }

    if (mode === "start_return") {
      void send({ code: "start_return" });
      return;
    }

    if (mode === "record_return_receipt") {
      // Explicit, not clamped: an over-count or an unparsable quantity is refused inline and never sent.
      const issues = receiptIssues(lines, receiptDrafts);
      if (issues.length > 0) {
        setLocalErrorFa(issues[0]!.messageFa);
        return;
      }

      const receiptItems: PostDispatchReceiptLine[] = [];
      for (const line of lines) {
        const draft = receiptDrafts[line.orderLineId];
        const usable = parseReceiptQuantity(draft?.usable) ?? 0;
        const damaged = parseReceiptQuantity(draft?.damaged) ?? 0;
        if (usable > 0) receiptItems.push({ orderLineId: line.orderLineId, quantity: usable, condition: 0 });
        if (damaged > 0) receiptItems.push({ orderLineId: line.orderLineId, quantity: damaged, condition: 1 });
      }
      if (receiptItems.length === 0) {
        setLocalErrorFa("دست‌کم یک قلم باید مقدار دریافتی داشته باشد.");
        return;
      }
      void send({ code: "record_return_receipt", receiptItems, note: note.trim() || null });
      return;
    }

    if (mode === "resolve_damaged_goods") {
      // TB-262-R1: an EXACT recorded decision is continued with its own line/quantity/outcome/key. No new
      // decision is composed and no new occurrence is minted, so the server answers that same durable record.
      if (resumeDamage) {
        void send({
          code: "resolve_damaged_goods",
          orderLineId: resumeDamage.orderLineId,
          quantity: resumeDamage.quantity,
          outcome: resumeDamage.outcome,
          note: note.trim() || null,
          idempotencyKey: resumeDamage.occurrenceKey,
        });
        return;
      }
      if (damagedLine === null) {
        setLocalErrorFa("قلمی با کالای آسیب‌دیدهٔ تعیین‌نشده در این مرسوله باقی نمانده است.");
        return;
      }
      if (damagedQuantity === null || damagedQuantity <= 0) {
        setLocalErrorFa("مقدار کالای آسیب‌دیده باید عددی بزرگ‌تر از صفر باشد.");
        return;
      }
      if (damagedQuantity > damagedLine.damagedOutstandingQuantity) {
        setLocalErrorFa(
          `مقدار تعیین تکلیف از باقیماندهٔ آسیب‌دیدهٔ این قلم (${damagedLine.damagedOutstandingQuantity.toLocaleString("fa-IR")}) بیشتر است.`,
        );
        return;
      }
      if (!DAMAGED_RESOLUTION_OUTCOMES.some((option) => option.code === damagedOutcome)) {
        setLocalErrorFa("نتیجهٔ تعیین تکلیف کالای آسیب‌دیده را انتخاب کنید.");
        return;
      }
      void send({
        code: "resolve_damaged_goods",
        orderLineId: damagedLine.orderLineId,
        quantity: damagedQuantity,
        outcome: damagedOutcome,
        note: note.trim() || null,
        idempotencyKey,
      });
      return;
    }

    if (mode === "resend_shipment") {
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
      void send({ code: "resend_shipment", selections, idempotencyKey });
    }
  }

  const submitLabel = retained
    ? "ارسال دوبارهٔ همان درخواست"
    : mode === "record_non_delivery"
      ? "ثبت عدم تحویل"
      : mode === "start_return"
        ? "شروع برگشت"
        : mode === "record_return_receipt"
          ? "تأیید دریافت در مبدأ"
          : mode === "resolve_damaged_goods"
            ? "ثبت تعیین تکلیف"
            : "ایجاد مرسولهٔ ارسال مجدد";

  const disabled =
    pending
    || (!retained
      && ((mode === "record_return_receipt" && receiptProblems.length > 0)
        || (mode === "resend_shipment" && resendTotal <= 0)
        || (mode === "resolve_damaged_goods" && damagedInvalid)));

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

          {retained ? (
            <div
              className="space-y-1.5 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2"
              data-testid={`admin-order-post-dispatch-retained-${parcelKey}`}
            >
              <p className="text-xs font-bold text-amber-900">
                پاسخ قطعی این درخواست ثبت نشده است. همان درخواست قبلی حفظ شده و فقط همان قابل ارسال دوباره است.
              </p>
              {describeRetained(retained.request, lines).map((row) => (
                <p key={row.key} className="break-words text-xs text-amber-900">{row.text}</p>
              ))}
              {retained.request.code === "record_non_delivery" && retained.request.note ? (
                <p className="break-words text-xs text-amber-900">{`توضیح: ${retained.request.note}`}</p>
              ) : null}
              {retained.request.code === "record_return_receipt" && retained.request.note ? (
                <p className="break-words text-xs text-amber-900">{`توضیح: ${retained.request.note}`}</p>
              ) : null}
            </div>
          ) : null}

          {!retained && mode === "record_non_delivery" ? (
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

          {!retained && mode === "start_return" ? (
            <p className="text-xs leading-6 text-gray-600">
              با شروع برگشت، مرسوله در مسیر بازگشت به مبدأ ثبت می‌شود. تأیید دریافت در مبدأ مرحلهٔ بعدی است و تنها مقدار سالم دریافتی قابل ارسال مجدد است.
            </p>
          ) : null}

          {!retained && mode === "record_return_receipt" ? (
            <>
              <p className="text-xs leading-6 text-gray-600">
                برای هر قلم، مقدار «سالم» و مقدار «آسیب‌دیده» را جدا ثبت کنید. فقط مقدار «سالم» دریافتی ظرفیت ارسال مجدد می‌سازد؛ اقلام آسیب‌دیده ثبت می‌شوند اما قابل ارسال مجدد نیستند.
              </p>
              <ul className="space-y-2">
                {lines.map((line) => {
                  const draft = receiptDrafts[line.orderLineId] ?? { usable: "0", damaged: "0" };
                  const lineProblem = receiptProblemByLine.get(line.orderLineId);
                  return (
                    <li
                      key={line.orderLineId}
                      className="rounded-lg border border-gray-200 px-3 py-2"
                      data-testid={`admin-order-post-dispatch-receipt-line-${line.orderLineId}`}
                    >
                      <p className="break-words text-sm text-gray-800">{line.title}</p>
                      <p className="mt-0.5 text-[11px] text-gray-500">{`در این مرسوله: ${line.parcelQuantity.toLocaleString("fa-IR")}`}</p>
                      <div className="mt-2 flex flex-wrap items-center gap-3">
                        <label className="flex items-center gap-1.5 text-xs text-gray-700">
                          <span>سالم</span>
                          <input
                            type="number"
                            inputMode="decimal"
                            min={0}
                            max={line.parcelQuantity}
                            step="any"
                            value={draft.usable}
                            disabled={pending}
                            onChange={(event) =>
                              setReceiptDrafts((previous) => ({
                                ...previous,
                                [line.orderLineId]: { ...draft, usable: event.target.value },
                              }))
                            }
                            className="min-h-11 w-24 rounded-lg border border-gray-200 px-2 text-sm tabular-nums"
                            data-testid={`admin-order-post-dispatch-receipt-usable-${line.orderLineId}`}
                          />
                        </label>
                        <label className="flex items-center gap-1.5 text-xs text-gray-700">
                          <span>آسیب‌دیده</span>
                          <input
                            type="number"
                            inputMode="decimal"
                            min={0}
                            max={line.parcelQuantity}
                            step="any"
                            value={draft.damaged}
                            disabled={pending}
                            onChange={(event) =>
                              setReceiptDrafts((previous) => ({
                                ...previous,
                                [line.orderLineId]: { ...draft, damaged: event.target.value },
                              }))
                            }
                            className="min-h-11 w-24 rounded-lg border border-gray-200 px-2 text-sm tabular-nums"
                            data-testid={`admin-order-post-dispatch-receipt-damaged-${line.orderLineId}`}
                          />
                        </label>
                      </div>
                      {lineProblem ? (
                        <p
                          className="mt-1 text-[11px] font-bold text-red-700"
                          role="alert"
                          data-testid={`admin-order-post-dispatch-receipt-line-error-${line.orderLineId}`}
                        >
                          {lineProblem}
                        </p>
                      ) : null}
                    </li>
                  );
                })}
              </ul>
              <p className="text-[11px] text-gray-500" data-testid={`admin-order-post-dispatch-receipt-total-${parcelKey}`}>
                {`مجموع ثبت‌شده: ${receiptTotal.toLocaleString("fa-IR")}`}
              </p>
            </>
          ) : null}

          {!retained && mode === "resend_shipment" ? (
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

          {!retained && mode === "resolve_damaged_goods" ? (
            <>
              <p className="text-xs leading-6 text-gray-600">
                کالای آسیب‌دیدهٔ دریافت‌شده در مبدأ فروختنی نیست و موجودی انبار را افزایش نمی‌دهد. برای هر قلم فقط به اندازهٔ باقیماندهٔ آسیب‌دیدهٔ همان مرسوله می‌توان یک بار تعیین تکلیف ثبت کرد.
              </p>
              {resumeDamage ? (
                <div
                  className="space-y-1.5 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-900"
                  data-testid={`admin-order-post-dispatch-damaged-resume-${parcelKey}`}
                >
                  <p className="font-bold">همان تصمیم ثبت‌شده ادامه می‌یابد؛ تصمیم، رزرو یا رخداد تازه‌ای ساخته نمی‌شود.</p>
                  <p className="break-words">
                    {`${resumeLine?.title ?? resumeDamage.orderLineId.slice(0, 8)} · مقدار ${resumeDamage.quantity.toLocaleString("fa-IR")} · ${DAMAGED_RESOLUTION_OUTCOMES.find((option) => option.code === resumeDamage.outcome)?.labelFa ?? resumeDamage.outcome}`}
                  </p>
                </div>
              ) : null}
              {!resumeDamage && damagedCandidates.length === 0 ? (
                <p
                  className="rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-900"
                  data-testid={`admin-order-post-dispatch-damaged-empty-${parcelKey}`}
                >
                  کالای آسیب‌دیدهٔ تعیین‌نشده‌ای برای این مرسوله باقی نمانده است.
                </p>
              ) : (
                <>
                  {resumeDamage ? null : (
                    <>
                  {damagedCandidates.length > 1 ? (
                    <fieldset className="space-y-1.5">
                      <legend className="mb-1 text-xs font-bold text-gray-700">قلم کالای آسیب‌دیده</legend>
                      {damagedCandidates.map((line) => (
                        <label
                          key={line.orderLineId}
                          className="flex min-h-11 cursor-pointer items-center gap-2 rounded-lg border border-gray-200 px-3 text-sm text-gray-800"
                          data-testid={`admin-order-post-dispatch-damaged-line-${line.orderLineId}`}
                        >
                          <input
                            type="radio"
                            name={`damaged-line-${parcelKey}`}
                            value={line.orderLineId}
                            checked={damagedLine?.orderLineId === line.orderLineId}
                            disabled={pending}
                            onChange={() => setDamagedLineId(line.orderLineId)}
                            className="size-4"
                          />
                          <span className="break-words">
                            {`${line.title} · باقیماندهٔ آسیب‌دیده: ${line.damagedOutstandingQuantity.toLocaleString("fa-IR")}`}
                          </span>
                        </label>
                      ))}
                    </fieldset>
                  ) : (
                    <p className="break-words text-sm text-gray-800">{damagedLine?.title}</p>
                  )}

                  {damagedLine ? (
                    <label className="block space-y-1">
                      <span className="text-xs font-bold text-gray-700">
                        {`مقدار آسیب‌دیدهٔ تعیین‌شده (حداکثر ${damagedLine.damagedOutstandingQuantity.toLocaleString("fa-IR")})`}
                      </span>
                      <input
                        type="number"
                        inputMode="decimal"
                        min={0}
                        max={damagedLine.damagedOutstandingQuantity}
                        step="any"
                        value={damagedDrafts[damagedLine.orderLineId] ?? ""}
                        disabled={pending}
                        onChange={(event) =>
                          setDamagedDrafts((previous) => ({
                            ...previous,
                            [damagedLine.orderLineId]: event.target.value,
                          }))
                        }
                        className="min-h-11 w-28 rounded-lg border border-gray-200 px-2 text-sm tabular-nums"
                        data-testid={`admin-order-post-dispatch-damaged-qty-${parcelKey}`}
                      />
                    </label>
                  ) : null}

                  <fieldset className="space-y-1.5">
                    <legend className="mb-1 text-xs font-bold text-gray-700">نتیجهٔ تعیین تکلیف</legend>
                    {DAMAGED_RESOLUTION_OUTCOMES.map((option) => (
                      <label
                        key={option.code}
                        className="flex min-h-11 cursor-pointer items-start gap-2 rounded-lg border border-gray-200 px-3 py-2 text-sm text-gray-800"
                        data-testid={`admin-order-post-dispatch-damaged-outcome-${option.code}`}
                      >
                        <input
                          type="radio"
                          name={`damaged-outcome-${parcelKey}`}
                          value={option.code}
                          checked={damagedOutcome === option.code}
                          disabled={pending}
                          onChange={() => setDamagedOutcome(option.code)}
                          className="mt-1 size-4"
                        />
                        <span className="space-y-0.5">
                          <span className="block font-bold">{option.labelFa}</span>
                          <span className="block text-xs leading-5 text-gray-600">{option.hintFa}</span>
                        </span>
                      </label>
                    ))}
                  </fieldset>
                    </>
                  )}

                  <label className="block space-y-1">
                    <span className="text-xs font-bold text-gray-700">توضیح (اختیاری)</span>
                    <textarea
                      value={note}
                      disabled={pending}
                      onChange={(event) => setNote(event.target.value)}
                      maxLength={500}
                      rows={2}
                      className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm"
                      data-testid={`admin-order-post-dispatch-damaged-note-${parcelKey}`}
                    />
                  </label>
                </>
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
