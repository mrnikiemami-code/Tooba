"use client";

/**
 * ONE modal for adding or editing a cost row of a delivery area.
 *
 * The main form is never mutated by an unconfirmed modal: Add starts from an isolated draft (no blank row
 * appears in the area), Edit starts from a COPY of that exact row (matched by stable `rowKey`, never by
 * position), Cancel closes without touching anything, and Confirm validates the row — including its
 * relationship with the OTHER rows of the SAME area — before replacing exactly that row in place.
 *
 * The delivery duration is NOT part of the modal: it belongs to the area's one shared working-day range.
 * Confirmation is local; the parent Apply remains the only persistence call.
 */
import { useEffect, useState } from "react";
import { Dialog } from "../../../design-system";
import type { AuthoringMessages } from "../messages/authoring-messages.ts";
import {
  rateConfirmIssue,
  type RateRowDraft,
  type StoredRateAmounts,
} from "../model/editor-rates.ts";
import { moneyPrecisionPolicy } from "../model/money-text.ts";
import { MoneyField } from "./authoring-money-field.tsx";
import { rowIssueMessage } from "./rate-issue-messages.ts";
import { AuthoringMessageBox } from "./authoring-message-box.tsx";

function BoundInput({
  label,
  value,
  inputMode,
  testId,
  onChange,
}: {
  label: string;
  value: string;
  inputMode: "numeric" | "decimal";
  testId: string;
  onChange: (value: string) => void;
}) {
  return (
    <label className="grid gap-0.5">
      <span className="text-[11px] font-bold text-gray-700">{label}</span>
      <input
        type="text"
        inputMode={inputMode}
        dir="ltr"
        className="min-h-10 w-full rounded-lg border border-border bg-surface px-2 text-sm text-gray-900"
        value={value}
        data-testid={testId}
        onChange={(event) => onChange(event.target.value)}
      />
    </label>
  );
}

export function AuthoringRateModal({
  open,
  title,
  initial,
  currencyCode,
  otherRows,
  storedAmounts,
  editable,
  messages,
  onConfirm,
  onClose,
}: {
  open: boolean;
  title: string;
  /** The isolated draft this modal starts from (a copy: the parent row is untouched until Confirm). */
  initial: RateRowDraft | null;
  /** Server-owned currency of the area (drives the integer-rial entry policy). */
  currencyCode: string | null;
  /** EVERY row of the same area, including the one being edited (self is excluded by stable key). */
  otherRows: readonly RateRowDraft[];
  /**
   * The EXACT stored money tokens of the row being edited. Fraction tolerance is recomputed against these on
   * every confirm, so an amount the operator edits here can never inherit the stored tolerance.
   */
  storedAmounts: StoredRateAmounts | null;
  editable: boolean;
  messages: AuthoringMessages;
  onConfirm: (row: RateRowDraft) => void;
  onClose: () => void;
}) {
  const [row, setRow] = useState<RateRowDraft | null>(initial);
  const [refusal, setRefusal] = useState<string | null>(null);
  const policy = moneyPrecisionPolicy(currencyCode);

  // Re-seed from the exact row this open was requested for; a stale draft never leaks into a new open.
  useEffect(() => {
    if (open) {
      setRow(initial);
      setRefusal(null);
    }
  }, [open, initial]);

  const update = (field: keyof RateRowDraft, value: string) => {
    setRow((previous) => (previous === null ? previous : { ...previous, [field]: value }));
  };

  const confirm = () => {
    if (row === null) return;
    // The disabled button is presentation: a view/busy state that changed while the modal was open must
    // make the HANDLER refuse, without a dialog and without touching the parent.
    const refusalIssue = rateConfirmIssue(row, otherRows, currencyCode, storedAmounts, editable);
    if (refusalIssue === null) {
      // The delivery duration belongs to the whole area, so the modal never carries its own pair.
      setRefusal(null);
      onConfirm(row);
      return;
    }
    if (refusalIssue.kind === "blocked") return;
    setRefusal(
      refusalIssue.kind === "shape"
        ? rowIssueMessage(refusalIssue.code, messages)
        : refusalIssue.kind === "duplicate"
          ? messages.rateDuplicateConflict
          : messages.rateOverlapConflict,
    );
  };

  return (
    <>
      <Dialog open={open} onClose={onClose} title={title} showCloseButton={false} size="lg">
        <div className="flex max-h-[70vh] flex-col gap-3 overflow-y-auto pe-1 text-sm text-gray-700">
          <p className="text-xs text-gray-600">{messages.rateModalIntro}</p>

          <div className="grid gap-2 sm:grid-cols-2">
            <MoneyField
              label={messages.freightLabel}
              value={row?.freightAmount ?? ""}
              policy={policy}
              messages={messages}
              onChange={(value) => update("freightAmount", value)}
              testId="shipping-authoring-rate-freight"
            />
            <MoneyField
              label={messages.packagingLabel}
              value={row?.packagingAmount ?? ""}
              policy={policy}
              messages={messages}
              onChange={(value) => update("packagingAmount", value)}
              testId="shipping-authoring-rate-packaging"
            />
          </div>

          <fieldset className="grid gap-2 rounded-lg border border-border bg-surface p-2">
            <legend className="px-1 text-[11px] font-bold text-gray-700">{messages.conditionsToggle}</legend>
            <div className="grid grid-cols-2 gap-2">
              <BoundInput
                label={`${messages.weightLabel} ${messages.minLabel}`}
                value={row?.minWeightGrams ?? ""}
                inputMode="numeric"
                onChange={(value) => update("minWeightGrams", value)}
                testId="shipping-authoring-rate-min-weight"
              />
              <BoundInput
                label={`${messages.weightLabel} ${messages.maxLabel}`}
                value={row?.maxWeightGrams ?? ""}
                inputMode="numeric"
                onChange={(value) => update("maxWeightGrams", value)}
                testId="shipping-authoring-rate-max-weight"
              />
              <BoundInput
                label={`${messages.countLabel} ${messages.minLabel}`}
                value={row?.minItemCount ?? ""}
                inputMode="numeric"
                onChange={(value) => update("minItemCount", value)}
                testId="shipping-authoring-rate-min-count"
              />
              <BoundInput
                label={`${messages.countLabel} ${messages.maxLabel}`}
                value={row?.maxItemCount ?? ""}
                inputMode="numeric"
                onChange={(value) => update("maxItemCount", value)}
                testId="shipping-authoring-rate-max-count"
              />
              <MoneyField
                label={`${messages.amountLabel} ${messages.minLabel}`}
                value={row?.minMerchandiseAmount ?? ""}
                policy={policy}
                messages={messages}
                onChange={(value) => update("minMerchandiseAmount", value)}
                testId="shipping-authoring-rate-min-amount"
              />
              <MoneyField
                label={`${messages.amountLabel} ${messages.maxLabel}`}
                value={row?.maxMerchandiseAmount ?? ""}
                policy={policy}
                messages={messages}
                onChange={(value) => update("maxMerchandiseAmount", value)}
                testId="shipping-authoring-rate-max-amount"
              />
            </div>
            <p className="text-[11px] text-gray-500">{messages.rateInheritAreaDuration}</p>
          </fieldset>

          <div className="sticky bottom-0 flex flex-wrap justify-end gap-2 border-t border-border bg-surface pt-2">
            <button
              type="button"
              className="inline-flex min-h-10 items-center rounded-lg border border-border px-3 text-xs font-bold text-gray-700"
              data-testid="shipping-authoring-rate-modal-cancel"
              onClick={onClose}
            >
              {messages.rateModalCancel}
            </button>
            <button
              type="button"
              className="inline-flex min-h-10 items-center rounded-lg bg-primary px-3 text-xs font-bold text-primary-foreground disabled:opacity-50"
              disabled={!editable || row === null}
              data-testid="shipping-authoring-rate-modal-confirm"
              onClick={confirm}
            >
              {messages.rateModalConfirm}
            </button>
          </div>
        </div>
      </Dialog>

      <AuthoringMessageBox
        open={refusal !== null}
        title={messages.rateModalInvalidTitle}
        text={refusal ?? ""}
        closeLabel={messages.rateModalCancel}
        testId="shipping-authoring-rate-refusal"
        onClose={() => setRefusal(null)}
      />
    </>
  );
}
