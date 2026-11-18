"use client";

import {
  AlertTriangle,
  CheckCircle,
  Package,
  RefreshCw,
  RotateCcw,
  X,
} from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import { toast } from "react-toastify";
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
  const [retained, setRetained] = useState<ReturnSubmission | null>(null);
  const [reasonId, setReasonId] = useState("");
  const [description, setDescription] = useState("");
  const [destination, setDestination] = useState<RefundDestination>(DEFAULT_REFUND_DESTINATION);
  const [lines, setLines] = useState<Record<string, LineState>>({});
  const [busy, setBusy] = useState(false);
  const [fieldError, setFieldError] = useState<string | null>(null);
  const [successCount, setSuccessCount] = useState<number | null>(null);
  const inFlight = useRef(false);

  useEffect(() => {
    if (!open) return;
    const nextStorage = typeof window === "undefined" ? null : window.sessionStorage;
    setStorage(nextStorage);
    const read = readReturnSubmission(nextStorage, checkoutId, viewerId);
    setRetained(read.kind === "found" ? read.submission : null);
    setReasonId("");
    setDescription("");
    setDestination(DEFAULT_REFUND_DESTINATION);
    setLines({});
    setFieldError(null);
    setSuccessCount(null);
    inFlight.current = false;
  }, [open, checkoutId, viewerId]);

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
  const canSubmit = !busy && storage !== null && selectedCount > 0 && !hasLineError;

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
   * Returns the recorded snapshots so the surface can refresh the customer's own return list once.
   */
  async function dispatchChildren(act: ReturnSubmission, children: ReturnSubmissionChild[]) {
    const storageRef = storage;
    const recordedSnapshots: ReturnSnapshot[] = [];
    const refused: ReturnSubmissionChild[] = [];
    let possiblyWritten = 0;
    let current: ReturnSubmission | null = act;
    for (const child of children) {
      const answer = await sendCustomerReturnBody(child.body);
      if (answer.ok) {
        const updated = recordChildOutcome(storageRef, act.submissionKey, child.key, {
          kind: "recorded",
          caseId: answer.snapshot.returnRequestId,
          caseNumber: null,
        });
        if (updated) current = updated;
        recordedSnapshots.push(answer.snapshot);
        continue;
      }
      const provesNoEffect = SUBMISSION_NO_EFFECT_REFUSALS.has(answer.errorCode);
      const updated = recordChildOutcome(storageRef, act.submissionKey, child.key, provesNoEffect
        ? { kind: "refused", refusalCode: answer.errorCode }
        : { kind: "unknown", refusalCode: answer.errorCode });
      if (updated) current = updated;
      if (provesNoEffect) {
        refused.push(child);
      } else {
        possiblyWritten += 1;
      }
    }

    const latest = current;
    setRetained(latest);
    const finalSummary = latest ? summarizeReturnSubmission(latest) : null;
    if (latest) {
      const summary = summarizeReturnSubmission(latest);
      if (summary.resolved) {
        clearReturnSubmissionIfCurrent(storageRef, latest, viewerId);
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
    if (inFlight.current) return;
    if (!storage) {
      toast.error("ثبت امن درخواست در این مرورگر ممکن نیست؛ درخواستی ارسال نشد.");
      return;
    }
    const selections = validateAct();
    if (!selections) return;
    const act = buildReturnSubmission({
      checkoutId,
      destination,
      viewerId: viewerId ?? null,
      selections,
    });
    if (!storeReturnSubmission(storage, act, viewerId)) {
      toast.error("ذخیرهٔ امن درخواست ممکن نشد؛ برای جلوگیری از ثبت تکراری چیزی ارسال نشد.");
      return;
    }
    setRetained(act);
    inFlight.current = true;
    setBusy(true);
    try {
      await dispatchChildren(act, unresolvedChildren(act));
    } finally {
      inFlight.current = false;
      setBusy(false);
    }
  }

  async function resume() {
    if (inFlight.current || !storage || !retained) return;
    const pending = unresolvedChildren(retained);
    if (pending.length === 0) return;
    inFlight.current = true;
    setBusy(true);
    try {
      await dispatchChildren(retained, pending);
    } finally {
      inFlight.current = false;
      setBusy(false);
    }
  }

  return (
    <div
      className="fixed inset-0 z-[9999] flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm"
      data-testid="customer-return-request-modal"
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-label="درخواست مرجوعی"
        className="relative bg-white rounded-2xl max-w-2xl w-full max-h-[90vh] overflow-hidden shadow-2xl border border-gray-200"
      >
        <div className="sticky top-0 z-10 bg-white border-b border-gray-200 p-4 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <div className="w-10 h-10 rounded-xl bg-blue-50 flex items-center justify-center">
              <RotateCcw className="w-5 h-5 text-[#2563EB]" />
            </div>
            <div>
              <h3 className="text-lg font-bold text-gray-900">درخواست مرجوعی</h3>
              <p className="text-xs text-gray-500">{orderReference ?? checkoutId.slice(0, 8)}</p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="p-2 min-h-11 min-w-11 rounded-lg hover:bg-gray-100 transition-colors"
            aria-label="بستن"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {successCount !== null ? (
          <div className="p-4 md:p-6 text-center space-y-4">
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
          <div className="p-4 md:p-6 overflow-y-auto max-h-[calc(90vh-80px)] space-y-5">
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
    </div>
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
