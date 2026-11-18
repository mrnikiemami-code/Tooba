"use client";

import {
  AlertTriangle,
  CheckCircle,
  Package,
  RefreshCw,
  RotateCcw,
} from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import { toast } from "react-toastify";
import { Dialog } from "../../design-system/primitives/overlays";
import {
  DEFAULT_REFUND_DESTINATION,
  formatReturnEligibilityBlocker,
  formatUnitLabel,
  sendCustomerReturnBody,
  type CustomerReturnEligibilityLine,
  type CustomerReturnEligibilitySnapshot,
  type RefundDestination,
  type ReturnSnapshot,
} from "./return-api";
import {
  isAlignedToUnit,
  normalizeQuantityInput,
  parsePositiveExactUnits,
  quantityStep,
  unitsFromWireNumber,
  unitsToExactText,
  unitsToWireNumber,
} from "./return-quantity";
import {
  buildReturnSubmission,
  clearReturnSubmissionIfCurrent,
  readReturnSubmission,
  recordChildOutcome,
  serializeReturnSubmission,
  storeReturnSubmission,
  SUBMISSION_NO_EFFECT_REFUSALS,
  summarizeReturnSubmission,
  unresolvedChildren,
  type ReturnSubmission,
  type ReturnSubmissionChild,
  type SubmissionSelection,
  type SubmissionStorage,
} from "./return-submission";
import { RefundDestinationSelector, RETURN_REASONS } from "./return-ui";

/**
 * TB-278 — the customer's ONE deliberate multi-seller return request.
 *
 * The customer sees each seller order of THIS checkout with the owning Returns authority's per-line eligibility:
 * the real product label, the line's own unit, the exact remaining returnable ceiling and the line's own accepted
 * quantity precision. Nothing is preselected and nothing is auto-filled: the customer chooses which lines (possibly
 * from DIFFERENT sellers), how much of each, and which reason, leaving the correct items untouched.
 *
 * The submission is frozen as a durable act BEFORE the first request (see `return-submission.ts`), each child keeps its
 * own idempotency key and exact bytes, and only unresolved children are retried — so a partially successful act keeps
 * its successes and a possibly-sent child is never confused with a definite one.
 */
export interface ReturnRequestSeller {
  sellerOrderId: string;
  sellerDisplayName: string;
  orderNumber: string;
  eligibility: CustomerReturnEligibilitySnapshot;
}

interface LineState {
  selected: boolean;
  quantityText: string;
  /** Empty = follow the act's shared reason. */
  reasonId: string;
  /**
   * TB-279: an OPTIONAL second portion of the SAME line with a DIFFERENT reason. Two portions of one line are two child
   * cases at the owning directory's own grain (line + reason), so a customer can return part of a line as "damaged" and
   * the rest as another reason without touching the correct goods. Empty = no second portion.
   */
  secondQuantityText: string;
  secondReasonId: string;
}

export interface ReturnRequestModalProps {
  open: boolean;
  onClose: () => void;
  checkoutId: string;
  orderReference?: string;
  /** TB-279: the signed-in customer this durable act belongs to; the act is scoped to this viewer only. */
  viewerId?: string | null;
  sellers: ReturnRequestSeller[];
  onSubmitted?: (snapshots: ReturnSnapshot[]) => void;
}

/** Persian copy of a typed customer return error (never a raw token). */
export function formatCustomerReturnError(code: string): string {
  switch (code) {
    case "return.quantity_invalid":
      return "مقدار واردشده معتبر نیست.";
    case "return.quantity_precision":
      return "تعداد اعشار این مقدار بیش از دقت مجاز است.";
    case "return.quantity_unit_invalid":
      return "این تعداد با واحد این قلم سازگار نیست؛ تعداد صحیح یا مضربی از گام واحد وارد کنید.";
    case "return.quantity_exceeded":
      return "مقدار واردشده از باقی‌ماندهٔ قابل مرجوعی این قلم بیشتر است.";
    case "return.nothing_returnable":
      return "مقدار قابل مرجوعی این قلم تمام شده است.";
    case "return.window_expired":
      return "بازهٔ مجاز مرجوعی این قلم به پایان رسیده است.";
    case "return.duplicate_line":
      return "این قلم قبلاً در همین درخواست انتخاب شده است.";
    case "return.idempotency_conflict":
      return "این درخواست با اطلاعات متفاوتی قبلاً ثبت شده است؛ صفحه را تازه کنید و دوباره تلاش کنید.";
    case "return.not_owner":
      return "این سفارش متعلق به حساب شما نیست.";
    case "return.missing":
      return "سفارش یا قلم انتخاب‌شده پیدا نشد؛ صفحه را تازه کنید.";
    case "host-unreachable":
      return "ارتباط با سرور برقرار نشد.";
    default:
      return "ثبت درخواست مرجوعی انجام نشد.";
  }
}

function formatQuantity(units: number | null): string {
  return units === null ? "—" : unitsToExactText(units);
}

/** Exact units of the line's whole-unit-only form when the owning precision did not answer. */
function ceilingUnits(line: CustomerReturnEligibilityLine): number | null {
  return unitsFromWireNumber(line.remainingReturnableQuantity);
}

/**
 * TB-279-R1: safe storage acquisition. Both the `window.sessionStorage` property getter and a probe write can throw
 * (privacy mode, quota, hardened browsers); each failure means the surface can never PROVE a durable act, so it must
 * refuse to send instead of turning unreadable evidence into "nothing pending".
 */
export function acquireSubmissionStorage(): SubmissionStorage | null {
  try {
    if (typeof window === "undefined") return null;
    const store = window.sessionStorage;
    if (!store) return null;
    const probe = "__tb279_storage_probe__";
    store.setItem(probe, "1");
    const readable = store.getItem(probe) === "1";
    store.removeItem(probe);
    return readable ? store : null;
  } catch {
    return null;
  }
}

export function ReturnRequestModal({
  open,
  onClose,
  checkoutId,
  orderReference,
  viewerId,
  sellers,
  onSubmitted,
}: ReturnRequestModalProps) {
  const [storage, setStorage] = useState<SubmissionStorage | null>(null);
  const [storageBlocked, setStorageBlocked] = useState(false);
  const [viewerMissing, setViewerMissing] = useState(false);
  const [unreadable, setUnreadable] = useState(false);
  // TB-279-R2 (F): an older viewer-less act exists for this checkout. It is preserved untouched and blocks a new act.
  const [legacyPending, setLegacyPending] = useState(false);
  const [retained, setRetained] = useState<ReturnSubmission | null>(null);
  const [reasonId, setReasonId] = useState("");
  const [description, setDescription] = useState("");
  const [destination, setDestination] = useState<RefundDestination>(DEFAULT_REFUND_DESTINATION);
  const [lines, setLines] = useState<Record<string, LineState>>({});
  const [busy, setBusy] = useState(false);
  const [fieldError, setFieldError] = useState<string | null>(null);
  const [successCount, setSuccessCount] = useState<number | null>(null);
  // TB-279-R1: presentation epoch. Every late read/answer/state update compares it, so a stale act can never paint on,
  // toast on, or release a newer surface.
  const epoch = useRef(0);
  // Counts UNRESOLVED sends, independent of the epoch: a possibly-sent act is never sendable twice, and a stale
  // completion can never release a lock that a newer send owns.
  const activeSends = useRef(0);
  const viewer = viewerId?.trim() ?? "";

  function isCurrent(started: number): boolean {
    return epoch.current === started;
  }

  // TB-279-R2 (F): unmount invalidates the epoch so no in-flight answer can paint, notify or clear after the surface
  // is gone (React may run this cleanup twice in development; the mount effect claims a fresh epoch either way).
  useEffect(() => {
    return () => {
      epoch.current += 1;
    };
  }, []);

  useEffect(() => {
    // A closed surface invalidates the epoch too: reopening, retargeting or closing can never let a late answer win.
    if (!open) {
      epoch.current += 1;
      return;
    }
    const started = ++epoch.current;
    const nextStorage = acquireSubmissionStorage();
    if (!isCurrent(started)) return;
    setStorage(nextStorage);
    setStorageBlocked(nextStorage === null);
    if (viewer === "") {
      setViewerMissing(true);
      setUnreadable(false);
      setLegacyPending(false);
      setRetained(null);
    } else {
      setViewerMissing(false);
      const read = readReturnSubmission(nextStorage, checkoutId, viewer);
      if (!isCurrent(started)) return;
      setRetained(read.kind === "found" ? read.submission : null);
      setUnreadable(read.kind === "unreadable");
      setLegacyPending(read.kind === "legacy_unresolved");
    }
    setReasonId("");
    setDescription("");
    setDestination(DEFAULT_REFUND_DESTINATION);
    setLines({});
    setFieldError(null);
    setSuccessCount(null);
  }, [open, checkoutId, viewer]);

  const eligibleLines = useMemo(() => {
    const rows: Array<{ seller: ReturnRequestSeller; line: CustomerReturnEligibilityLine }> = [];
    for (const seller of sellers) {
      for (const line of seller.eligibility.lines) {
        if (line.selectable && ceilingUnits(line) !== null && ceilingUnits(line)! > 0) {
          rows.push({ seller, line });
        }
      }
    }
    return rows;
  }, [sellers]);

  const selectedLineErrors = useMemo(() => {
    const errors: Record<string, string | null> = {};
    for (const { line } of eligibleLines) {
      const state = lines[line.orderLineId];
      if (!state?.selected) continue;
      const units = parsePositiveExactUnits(state.quantityText);
      const ceiling = ceilingUnits(line);
      if (units === null) {
        errors[line.orderLineId] = "مقداری بزرگ‌تر از صفر وارد کنید.";
      } else if (ceiling !== null && units > ceiling) {
        errors[line.orderLineId] = "مقدار واردشده از باقی‌ماندهٔ قابل مرجوعی بیشتر است.";
      } else if (line.unitDecimalPlaces !== null && !isAlignedToUnit(units, line.unitDecimalPlaces)) {
        errors[line.orderLineId] = "این تعداد با واحد این قلم سازگار نیست.";
      } else if (line.unitDecimalPlaces === null && units % 1_000_000 !== 0) {
        errors[line.orderLineId] = "دقت واحد این قلم مشخص نیست؛ فقط تعداد صحیح مجاز است.";
      } else {
        errors[line.orderLineId] = null;
      }
    }
    return errors;
  }, [eligibleLines, lines]);

  const selectedCount = Object.values(lines).filter((state) => state.selected).length;
  const hasLineError = Object.values(selectedLineErrors).some((value) => value !== null);
  const canSubmit =
    !busy &&
    storage !== null &&
    !storageBlocked &&
    viewer !== "" &&
    !unreadable &&
    !legacyPending &&
    selectedCount > 0 &&
    !hasLineError;

  if (!open) return null;

  function lineState(orderLineId: string): LineState {
    return lines[orderLineId] ?? { selected: false, quantityText: "", reasonId: "", secondQuantityText: "", secondReasonId: "" };
  }

  function updateLine(orderLineId: string, patch: Partial<LineState>) {
    setLines((current) => ({
      ...current,
      [orderLineId]: { ...lineState(orderLineId), ...patch },
    }));
  }

  function resolveReasonText(state: LineState): string | null {
    const effectiveId = state.reasonId || reasonId;
    const label = RETURN_REASONS.find((item) => item.id === effectiveId)?.label;
    if (!label) return null;
    return description.trim() ? `${label}\n${description.trim()}` : label;
  }

  function validateAct(): SubmissionSelection[] | null {
    const shared = RETURN_REASONS.find((item) => item.id === reasonId);
    const selected = eligibleLines.filter(({ line }) => lineState(line.orderLineId).selected);
    if (selected.length === 0) {
      setFieldError("حداقل یک قلم قابل مرجوعی را انتخاب کنید.");
      return null;
    }
    if (description.trim().length < 10) {
      setFieldError("لطفاً توضیحات را وارد کنید (حداقل ۱۰ کاراکتر).");
      return null;
    }
    if (!shared && selected.some(({ line }) => !lineState(line.orderLineId).reasonId)) {
      setFieldError("برای هر قلم دلیل مرجوعی را انتخاب کنید.");
      return null;
    }
    const selections: SubmissionSelection[] = [];
    for (const { seller, line } of selected) {
      const state = lineState(line.orderLineId);
      const units = parsePositiveExactUnits(state.quantityText);
      if (units === null) {
        setFieldError(`مقدار «${line.productLabel ?? "قلم انتخابی"}» معتبر نیست.`);
        return null;
      }
      const reason = resolveReasonText(state);
      if (!reason) {
        setFieldError("دلیل مرجوعی این قلم انتخاب نشده است.");
        return null;
      }
      selections.push({
        sellerOrderId: seller.sellerOrderId,
        orderLineId: line.orderLineId,
        productLabel: line.productLabel,
        unitLabel: line.unitLabel,
        unitDecimalPlaces: line.unitDecimalPlaces,
        quantityUnits: units,
        quantityText: unitsToExactText(units),
        reason,
      });

      // TB-279: an optional SECOND portion of the same line with a DIFFERENT reason. It is a separate child case, and
      // the two portions together may never exceed the line's authoritative remaining ceiling.
      if (state.secondQuantityText.trim() === "") continue;
      const secondUnits = parsePositiveExactUnits(state.secondQuantityText);
      if (secondUnits === null) {
        setFieldError(`مقدار بخش دوم «${line.productLabel ?? "قلم انتخابی"}» معتبر نیست.`);
        return null;
      }
      const secondReasonId = state.secondReasonId || reasonId;
      const secondLabel = RETURN_REASONS.find((item) => item.id === secondReasonId)?.label;
      if (!secondLabel) {
        setFieldError(`برای بخش دوم «${line.productLabel ?? "قلم انتخابی"}» یک دلیل متفاوت انتخاب کنید.`);
        return null;
      }
      const secondReason = description.trim() ? `${secondLabel}\n${description.trim()}` : secondLabel;
      if (secondReason === reason) {
        setFieldError(`بخش دوم «${line.productLabel ?? "قلم انتخابی"}» باید دلیل متفاوتی از بخش اول داشته باشد.`);
        return null;
      }
      const lineCeiling = ceilingUnits(line);
      if (lineCeiling === null || units + secondUnits > lineCeiling) {
        setFieldError(`مجموع دو بخش «${line.productLabel ?? "قلم انتخابی"}» از باقی‌ماندهٔ قابل مرجوعی بیشتر است.`);
        return null;
      }
      selections.push({
        sellerOrderId: seller.sellerOrderId,
        orderLineId: line.orderLineId,
        productLabel: line.productLabel,
        unitLabel: line.unitLabel,
        unitDecimalPlaces: line.unitDecimalPlaces,
        quantityUnits: secondUnits,
        quantityText: unitsToExactText(secondUnits),
        reason: secondReason,
      });
    }
    setFieldError(null);
    return selections;
  }

  /**
   * Sends ONLY unsettled children of one frozen act, in order, and records each answer against the exact act.
   *
   * The epoch fence is applied AFTER each answer so a closed/retargeted surface never paints, notifies or clears on a
   * newer surface — while the answer itself is still written as exact evidence for the act it belongs to.
   */
  async function dispatchChildren(act: ReturnSubmission, children: ReturnSubmissionChild[], started: number) {
    const storageRef = storage;
    if (!storageRef) return;
    const scope = { checkoutId: act.checkoutId, viewerId: act.viewerId };
    const recordedSnapshots: ReturnSnapshot[] = [];
    const refused: ReturnSubmissionChild[] = [];
    let possiblyWritten = 0;
    let current: ReturnSubmission | null = act;

    /**
     * TB-279-R2 (F): before ANY child is put on the wire, the stored act must still be THIS exact act and must still
     * carry this child's exact frozen bytes. A replaced act, a cleared act or a tampered child stops the send.
     */
    function exactStoredEnvelope(child: ReturnSubmissionChild): string | null {
      const read = readReturnSubmission(storageRef, act.checkoutId, act.viewerId);
      if (read.kind !== "found") return null;
      const stored = read.submission;
      if (stored.submissionKey !== act.submissionKey) return null;
      const target = stored.children.find((item) => item.key === child.key);
      if (!target || target.body !== child.body || target.idempotencyKey !== child.idempotencyKey) return null;
      return serializeReturnSubmission(stored);
    }

    for (const child of children) {
      // Stale surface: never start another send.
      if (!isCurrent(started)) return;
      const envelope = exactStoredEnvelope(child);
      if (envelope === null) {
        if (isCurrent(started)) {
          const reread = readReturnSubmission(storageRef, act.checkoutId, act.viewerId);
          setRetained(reread.kind === "found" ? reread.submission : null);
          setUnreadable(reread.kind === "unreadable");
          toast.error("شاهد ذخیره‌شدهٔ درخواست تغییر کرده است؛ برای جلوگیری از ثبت تکراری چیزی ارسال نشد.");
        }
        return;
      }
      const answer = await sendCustomerReturnBody(child.body);
      const expected = { envelope, childBody: child.body, idempotencyKey: child.idempotencyKey };
      const updated = answer.ok
        ? recordChildOutcome(
            storageRef,
            scope,
            act.submissionKey,
            child.key,
            {
              kind: "recorded",
              caseId: answer.snapshot.returnRequestId,
              caseNumber: null,
            },
            expected,
          )
        : SUBMISSION_NO_EFFECT_REFUSALS.has(answer.errorCode)
          ? recordChildOutcome(
              storageRef,
              scope,
              act.submissionKey,
              child.key,
              {
                kind: "refused",
                refusalCode: answer.errorCode,
              },
              expected,
            )
          : recordChildOutcome(
              storageRef,
              scope,
              act.submissionKey,
              child.key,
              {
                kind: "unknown",
                refusalCode: answer.errorCode,
              },
              expected,
            );
      if (updated) current = updated;
      if (answer.ok) {
        recordedSnapshots.push(answer.snapshot);
      } else if (SUBMISSION_NO_EFFECT_REFUSALS.has(answer.errorCode)) {
        refused.push(child);
      } else {
        possiblyWritten += 1;
      }
      // Stale surface: keep the durable outcome, stop sending the remainder and never touch the newer UI.
      if (!isCurrent(started)) return;
    }

    const latest = current;
    if (!isCurrent(started)) return;
    setRetained(latest);
    const finalSummary = latest ? summarizeReturnSubmission(latest) : null;
    if (latest) {
      if (finalSummary?.resolved) {
        clearReturnSubmissionIfCurrent(storageRef, latest, viewer);
        setRetained(null);
      }
    }

    if (recordedSnapshots.length > 0) {
      onSubmitted?.(recordedSnapshots);
    }
    if (finalSummary?.resolved && refused.length === 0 && possiblyWritten === 0) {
      const total = finalSummary.recorded;
      toast.success(
        total === 1
          ? "درخواست مرجوعی ثبت شد."
          : `${total.toLocaleString("fa-IR")} درخواست مرجوعی ثبت شد.`,
      );
      setSuccessCount(total);
      return;
    }
    if (possiblyWritten > 0) {
      toast.error(
        "نتیجهٔ ثبت بعضی اقلام نامشخص است. درخواست‌های باقی‌مانده در بالای فرم نگه داشته شد؛ دوباره تلاش کنید.",
      );
    }
    if (refused.length > 0) {
      toast.error(
        refused.length === 1
          ? formatCustomerReturnError(refused[0].refusalCode ?? "return.rejected")
          : `${refused.length.toLocaleString("fa-IR")} قلم پذیرفته نشد؛ توضیح هر مورد در فهرست باقی‌مانده آمده است.`,
      );
    }
  }

  async function submit() {
    if (activeSends.current > 0) return;
    const started = epoch.current;
    if (viewer === "") {
      setFieldError("برای ثبت درخواست مرجوعی باید وارد حساب کاربری خود شوید.");
      return;
    }
    if (!storage) {
      toast.error("ثبت امن درخواست در این مرورگر ممکن نیست؛ درخواستی ارسال نشد.");
      return;
    }
    // TB-279-R1: a possibly-sent or unreadable act is NEVER overwritten by a new one. Re-read the exact scope first.
    const existing = readReturnSubmission(storage, checkoutId, viewer);
    if (existing.kind === "legacy_unresolved") {
      setLegacyPending(true);
      setFieldError("شاهد ثبت قدیمی‌تری برای این سفارش وجود دارد که قابل انتساب به حساب شما نیست؛ برای جلوگیری از ثبت تکراری درخواستی ارسال نشد.");
      return;
    }
    if (existing.kind === "unreadable") {
      setUnreadable(true);
      setFieldError("شاهد ثبت قبلی در این مرورگر خوانا نیست؛ برای جلوگیری از ثبت تکراری درخواستی ارسال نشد.");
      return;
    }
    if (existing.kind === "unavailable") {
      toast.error("ثبت امن درخواست در این مرورگر ممکن نیست؛ درخواستی ارسال نشد.");
      return;
    }
    if (existing.kind === "found") {
      if (!isCurrent(started)) return;
      const pending = unresolvedChildren(existing.submission);
      if (pending.length > 0) {
        setRetained(existing.submission);
        setFieldError("یک درخواست ناتمام از قبل ثبت شده است؛ ابتدا همان را ادامه دهید.");
        return;
      }
      // Every child is resolved: the exact act is cleared before a new deliberate act may take its place.
      if (!clearReturnSubmissionIfCurrent(storage, existing.submission, viewer)) {
        setFieldError("درخواست قبلی قابل پاک‌سازی نبود؛ برای جلوگیری از تداخل چیزی ارسال نشد.");
        return;
      }
      setRetained(null);
    }
    const selections = validateAct();
    if (!selections) return;
    const act = buildReturnSubmission({
      checkoutId,
      destination,
      viewerId: viewer,
      selections,
    });
    if (!act) {
      setFieldError("ثبت درخواست بدون شناسهٔ حساب کاربری ممکن نیست.");
      return;
    }
    if (!storeReturnSubmission(storage, act)) {
      toast.error("ذخیرهٔ امن درخواست ممکن نشد؛ برای جلوگیری از ثبت تکراری چیزی ارسال نشد.");
      return;
    }
    if (!isCurrent(started)) return;
    setRetained(act);
    setUnreadable(false);
    setLegacyPending(false);
    activeSends.current += 1;
    setBusy(true);
    try {
      await dispatchChildren(act, unresolvedChildren(act), started);
    } finally {
      activeSends.current -= 1;
      if (isCurrent(started)) setBusy(false);
    }
  }

  async function resume() {
    if (activeSends.current > 0 || !storage || !retained) return;
    if (viewer === "" || retained.viewerId !== viewer) return;
    const pending = unresolvedChildren(retained);
    if (pending.length === 0) return;
    const started = epoch.current;
    activeSends.current += 1;
    setBusy(true);
    try {
      await dispatchChildren(retained, pending, started);
    } finally {
      activeSends.current -= 1;
      if (isCurrent(started)) setBusy(false);
    }
  }

  return (
    <Dialog open={open} onClose={onClose} title="درخواست مرجوعی" size="lg" showCloseButton={false}>
      <div dir="rtl" data-testid="customer-return-request-modal" className="space-y-4">
        <div className="flex items-center gap-2">
          <div className="w-10 h-10 rounded-xl bg-blue-50 flex items-center justify-center">
            <RotateCcw className="w-5 h-5 text-[#2563EB]" />
          </div>
          <p className="text-xs text-gray-500">{orderReference ?? checkoutId.slice(0, 8)}</p>
        </div>

        {viewerMissing ? (
          <p className="rounded-xl border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800" role="alert">
            برای ثبت درخواست مرجوعی باید وارد حساب کاربری خود شوید؛ چیزی ارسال نشد.
          </p>
        ) : null}
        {storageBlocked ? (
          <p className="rounded-xl border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800" role="alert">
            ثبت امن درخواست در این مرورگر ممکن نیست؛ چیزی ارسال نشد.
          </p>
        ) : null}
        {legacyPending ? (
          <p
            data-testid="customer-return-legacy-evidence"
            className="rounded-xl border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800"
            role="alert"
          >
            یک شاهد ثبت قدیمی‌تر برای این سفارش در این مرورگر نگه داشته شده است و به حساب شما قابل انتساب نیست؛
            برای جلوگیری از ثبت تکراری، درخواست جدیدی ارسال نمی‌شود. اگر مطمئن نیستید، با پشتیبانی تماس بگیرید.
          </p>
        ) : null}
        {unreadable ? (
          <p className="rounded-xl border border-red-200 bg-red-50 p-3 text-sm text-red-700" role="alert">
            شاهد ثبت قبلی در این مرورگر خوانا نیست. برای جلوگیری از ثبت تکراری، درخواست جدیدی ارسال نمی‌شود.
          </p>
        ) : null}

        {successCount !== null ? (
          <div className="p-2 text-center space-y-4">
            <div className="w-16 h-16 rounded-full bg-emerald-50 flex items-center justify-center mx-auto">
              <CheckCircle className="w-8 h-8 text-emerald-500" />
            </div>
            <div>
              <h4 className="text-lg font-bold text-gray-900">
                {successCount === 1 ? "درخواست شما ثبت شد" : `${successCount.toLocaleString("fa-IR")} درخواست شما ثبت شد`}
              </h4>
              <p className="text-sm text-gray-500 mt-1">هر قلم به‌صورت جداگانه بررسی و نتیجه اطلاع داده خواهد شد.</p>
            </div>
            <button
              type="button"
              onClick={onClose}
              className="w-full py-3 bg-[#2563EB] text-white rounded-xl text-sm font-bold hover:bg-blue-700 transition-colors min-h-11"
            >
              باشه
            </button>
          </div>
        ) : (
          <div className="space-y-5">
            <div className="bg-amber-50 border border-amber-200 rounded-xl p-3 flex items-start gap-2">
              <AlertTriangle className="w-4 h-4 text-amber-500 mt-0.5 shrink-0" />
              <p className="text-xs text-amber-700">
                فقط اقلام تحویل‌شده و داخل بازهٔ مجاز قابل انتخاب‌اند. هر قلمی که انتخاب نکنید مرجوع نمی‌شود.
              </p>
            </div>

            {retained ? <RetainedSubmissionCard submission={retained} busy={busy} onResume={() => void resume()} /> : null}

            {sellers.length === 0 ? (
              <p className="text-sm text-gray-500">قلمی برای مرجوعی واجد شرایط نیست.</p>
            ) : (
              sellers.map((seller) => (
                <section key={seller.sellerOrderId} className="rounded-2xl border border-gray-100 overflow-hidden">
                  <header className="bg-gray-50 px-4 py-3 flex flex-wrap items-center gap-2">
                    <Package className="w-4 h-4 text-[#2563EB]" />
                    <strong className="text-sm">{seller.sellerDisplayName}</strong>
                    <span className="text-xs text-gray-400">{seller.orderNumber}</span>
                  </header>
                  <ul className="divide-y divide-gray-100">
                    {seller.eligibility.lines.map((line) => {
                      const state = lineState(line.orderLineId);
                      const ceiling = ceilingUnits(line);
                      const selectable = line.selectable && ceiling !== null && ceiling > 0;
                      return (
                        <li key={line.orderLineId} className="p-3 space-y-2">
                          <div className="flex flex-wrap items-center gap-2">
                            <label className="flex items-center gap-2 min-h-11 cursor-pointer">
                              <input
                                type="checkbox"
                                className="w-5 h-5 accent-[#2563EB]"
                                checked={state.selected}
                                disabled={!selectable}
                                data-testid={`return-line-select-${line.orderLineId}`}
                                onChange={(event) =>
                                  updateLine(line.orderLineId, {
                                    selected: event.target.checked,
                                    quantityText: event.target.checked ? "" : "",
                                  })
                                }
                              />
                              <span className="text-sm font-medium text-gray-900">
                                {line.productLabel ?? "قلم سفارش"}
                              </span>
                            </label>
                            <span className="text-[11px] text-gray-500">
                              {`واحد: ${formatUnitLabel(line.unitLabel)}`}
                            </span>
                            <span className="text-[11px] text-gray-500" data-testid={`return-line-ceiling-${line.orderLineId}`}>
                              {`حداکثر قابل مرجوعی: ${formatQuantity(ceiling)}`}
                            </span>
                          </div>
                          {!selectable ? (
                            <p className="text-[11px] text-gray-500">
                              {formatReturnEligibilityBlocker(line.blockerCode ?? "nothing_returnable")}
                            </p>
                          ) : state.selected ? (
                            <div className="flex flex-wrap items-center gap-2">
                              <label className="text-xs text-gray-600" htmlFor={`return-line-qty-${line.orderLineId}`}>
                                مقدار
                              </label>
                              <input
                                id={`return-line-qty-${line.orderLineId}`}
                                type="text"
                                inputMode="decimal"
                                dir="ltr"
                                value={state.quantityText}
                                onChange={(event) =>
                                  updateLine(line.orderLineId, { quantityText: normalizeQuantityInput(event.target.value) })
                                }
                                data-testid={`return-line-quantity-${line.orderLineId}`}
                                className="w-28 px-3 py-2 bg-white rounded-lg text-sm border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#2563EB] min-h-11"
                                placeholder="0"
                              />
                              {line.unitDecimalPlaces !== null ? (
                                <span className="text-[11px] text-gray-400">
                                  {`گام مجاز: ${quantityStep(line.unitDecimalPlaces)}`}
                                </span>
                              ) : null}
                              <select
                                value={state.reasonId}
                                onChange={(event) => updateLine(line.orderLineId, { reasonId: event.target.value })}
                                aria-label="دلیل مرجوعی این قلم"
                                data-testid={`return-line-reason-${line.orderLineId}`}
                                className="px-3 py-2 bg-white rounded-lg text-xs border border-gray-200 min-h-11"
                              >
                                <option value="">{reasonId ? "همان دلیل کلی" : "دلیل را انتخاب کنید"}</option>
                                {RETURN_REASONS.map((reason) => (
                                  <option key={reason.id} value={reason.id}>{reason.label}</option>
                                ))}
                              </select>
                            </div>
                          ) : null}
                          {selectable && state.selected ? (
                            <div className="flex flex-wrap items-center gap-2 border-t border-dashed border-gray-200 pt-2">
                              <span className="text-[11px] text-gray-500">بخش دوم (دلیل متفاوت، اختیاری)</span>
                              <input
                                type="text"
                                inputMode="decimal"
                                dir="ltr"
                                aria-label="مقدار بخش دوم"
                                value={state.secondQuantityText}
                                onChange={(event) =>
                                  updateLine(line.orderLineId, {
                                    secondQuantityText: normalizeQuantityInput(event.target.value),
                                  })
                                }
                                data-testid={`return-line-second-quantity-${line.orderLineId}`}
                                className="w-28 px-3 py-2 bg-white rounded-lg text-sm border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#2563EB] min-h-11"
                                placeholder="0"
                              />
                              <select
                                value={state.secondReasonId}
                                onChange={(event) => updateLine(line.orderLineId, { secondReasonId: event.target.value })}
                                aria-label="دلیل بخش دوم این قلم"
                                data-testid={`return-line-second-reason-${line.orderLineId}`}
                                className="px-3 py-2 bg-white rounded-lg text-xs border border-gray-200 min-h-11"
                              >
                                <option value="">دلیل بخش دوم</option>
                                {RETURN_REASONS.map((reason) => (
                                  <option key={reason.id} value={reason.id}>{reason.label}</option>
                                ))}
                              </select>
                            </div>
                          ) : null}
                          {selectedLineErrors[line.orderLineId] ? (
                            <p className="text-xs text-red-600">{selectedLineErrors[line.orderLineId]}</p>
                          ) : null}
                        </li>
                      );
                    })}
                  </ul>
                </section>
              ))
            )}

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1.5" htmlFor="return-shared-reason">
                دلیل کلی مرجوعی
              </label>
              <select
                id="return-shared-reason"
                value={reasonId}
                onChange={(event) => setReasonId(event.target.value)}
                className="w-full px-4 py-3 bg-white rounded-xl text-sm text-gray-900 border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#2563EB] min-h-11"
              >
                <option value="">انتخاب کنید...</option>
                {RETURN_REASONS.map((item) => (
                  <option key={item.id} value={item.id}>{item.label}</option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1.5" htmlFor="return-description">
                توضیحات
              </label>
              <textarea
                id="return-description"
                value={description}
                onChange={(event) => setDescription(event.target.value)}
                placeholder="لطفاً توضیح دهید چرا قصد مرجوع کردن کالا را دارید..."
                className="w-full px-4 py-2.5 bg-white rounded-xl text-sm text-gray-900 border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#2563EB] resize-none"
                rows={3}
              />
              <p className="text-xs text-gray-400 mt-1">حداقل ۱۰ کاراکتر</p>
            </div>

            <RefundDestinationSelector value={destination} onChange={setDestination} />

            {fieldError ? <p className="text-sm text-red-600">{fieldError}</p> : null}

            <div className="flex flex-wrap gap-3 pt-2">
              <button
                type="button"
                disabled={!canSubmit}
                onClick={() => void submit()}
                className="flex-1 py-3 bg-[#2563EB] text-white rounded-xl text-sm font-bold hover:bg-blue-700 transition-colors shadow-lg shadow-blue-500/30 disabled:opacity-50 min-h-11"
              >
                {busy ? "در حال ثبت..." : "ثبت درخواست مرجوعی"}
              </button>
              <button
                type="button"
                onClick={onClose}
                className="px-6 py-3 bg-gray-100 text-gray-700 rounded-xl text-sm font-medium hover:bg-gray-200 transition-colors min-h-11"
              >
                انصراف
              </button>
            </div>
          </div>
        )}
      </div>
    </Dialog>
  );
}

const CHILD_STATE_LABEL: Record<ReturnSubmissionChild["state"], string> = {
  pending: "ارسال‌نشده",
  unknown: "نتیجه نامشخص",
  recorded: "ثبت‌شده",
  refused: "پذیرفته‌نشده",
};

function RetainedSubmissionCard({
  submission,
  busy,
  onResume,
}: {
  submission: ReturnSubmission;
  busy: boolean;
  onResume: () => void;
}) {
  const summary = summarizeReturnSubmission(submission);
  const unresolved = unresolvedChildren(submission);
  return (
    <div className="rounded-2xl border border-amber-200 bg-amber-50 p-3 space-y-2" data-testid="retained-return-submission">
      <p className="text-sm font-bold text-amber-900">
        {`ثبت مرجوعی ناتمام — ${summary.total.toLocaleString("fa-IR")} قلم، ${summary.recorded.toLocaleString("fa-IR")} ثبت‌شده`}
      </p>
      <ul className="space-y-1">
        {submission.children.map((child) => (
          <li key={child.key} className="flex flex-wrap items-center gap-2 text-[11px] text-amber-900">
            <span>{child.productLabel ?? child.orderLineId.slice(0, 8)}</span>
            <span>{`× ${formatQuantity(child.quantityUnits)}`}</span>
            <span className="rounded-full bg-white/70 px-2 py-0.5">{CHILD_STATE_LABEL[child.state]}</span>
            {child.refusalCode ? <span>{formatCustomerReturnError(child.refusalCode)}</span> : null}
          </li>
        ))}
      </ul>
      {unresolved.length > 0 ? (
        <button
          type="button"
          disabled={busy}
          onClick={onResume}
          data-testid="resume-return-submission"
          className="inline-flex items-center gap-2 rounded-xl border border-amber-300 bg-white px-4 py-2.5 text-sm font-bold text-amber-900 disabled:opacity-50 min-h-11"
        >
          <RefreshCw className="w-4 h-4" />
          {`ادامهٔ ثبت ${unresolved.length.toLocaleString("fa-IR")} قلم باقی‌مانده`}
        </button>
      ) : null}
    </div>
  );
}
