"use client";

/**
 * TB-SHIPPING-ADMIN-UI-PREVIEW-186 — ویرایشگر درون‌خطی یک ردیف شرط/هزینه در پیش‌نمایش.
 *
 * - هیچ انتخاب‌گر جغرافیایی در این ردیف نیست: ردیف از مناطق همان گروه استفاده می‌کند.
 * - وزن با واحد نمایش کیلوگرم ویرایش و با واحد ذخیرهٔ گرم نگه داشته می‌شود.
 * - اعتبارسنجی فقط «شکل بازه» است (منفی/معکوس/غیرصحیح)؛ هیچ موتور قیمت‌گذاری یا مالیات اینجا نیست.
 */

import { useId, useState } from "react";
import { Field, Input } from "../../../../design-system";
import {
  previewEmptyRange,
  previewNextId,
  previewRangeError,
  previewRangeIsEmpty,
  type PreviewLocale,
  type PreviewRange,
  type PreviewRateRow,
} from "./preview-model.ts";
import {
  previewCurrencyUnitLabel,
  type PreviewMessages,
} from "./preview-i18n.ts";

function numericInput(value: number | null): string {
  return value === null ? "" : String(value);
}

function parseInput(value: string): number | null {
  const trimmed = value.trim();
  if (trimmed.length === 0) return null;
  const parsed = Number(trimmed);
  return Number.isFinite(parsed) ? parsed : null;
}

function rangeFromInputs(fromText: string, toText: string, scale: number): PreviewRange | null {
  const from = parseInput(fromText);
  const to = parseInput(toText);
  if (from === null && to === null) return null;
  return { from: from === null ? null : Math.round(from * scale), to: to === null ? null : Math.round(to * scale) };
}

export function PreviewRateRowEditor({
  row,
  readOnly,
  locale,
  messages,
  onApply,
  onCancel,
}: {
  row: PreviewRateRow | null;
  readOnly: boolean;
  locale: PreviewLocale;
  messages: PreviewMessages;
  onApply: (row: PreviewRateRow) => void;
  onCancel: () => void;
}) {
  const fieldId = useId();
  const [weightFrom, setWeightFrom] = useState(row?.weightGrams?.from != null ? String(row.weightGrams.from / 1000) : "");
  const [weightTo, setWeightTo] = useState(row?.weightGrams?.to != null ? String(row.weightGrams.to / 1000) : "");
  const [countFrom, setCountFrom] = useState(numericInput(row?.itemCount?.from ?? null));
  const [countTo, setCountTo] = useState(numericInput(row?.itemCount?.to ?? null));
  const [amountFrom, setAmountFrom] = useState(numericInput(row?.merchandiseAmount?.from ?? null));
  const [amountTo, setAmountTo] = useState(numericInput(row?.merchandiseAmount?.to ?? null));
  const [freight, setFreight] = useState(String(row?.freight ?? 0));
  const [packaging, setPackaging] = useState(String(row?.packaging ?? 0));
  const [deliveryMin, setDeliveryMin] = useState(String(row?.delivery.minDays ?? 1));
  const [deliveryMax, setDeliveryMax] = useState(String(row?.delivery.maxDays ?? 3));
  const [submitted, setSubmitted] = useState(false);

  const weightRange = rangeFromInputs(weightFrom, weightTo, 1000);
  const countRange = rangeFromInputs(countFrom, countTo, 1);
  const amountRange = rangeFromInputs(amountFrom, amountTo, 1);

  const errorFor = (range: PreviewRange | null, integer: boolean): string | undefined => {
    if (!submitted) return undefined;
    const code = previewRangeError(range, { integer });
    if (code === "inverted") return messages.rowErrorInverted;
    if (code === "notInteger") return messages.rowErrorNotInteger;
    if (code === "negative") return messages.rowErrorNegative;
    return undefined;
  };

  const minDays = parseInput(deliveryMin);
  const maxDays = parseInput(deliveryMax);
  const deliveryError =
    submitted && minDays !== null && maxDays !== null && maxDays < minDays ? messages.rowErrorDelivery : undefined;

  const weightError = errorFor(weightRange, false);
  const countError = errorFor(countRange, true);
  const amountError = errorFor(amountRange, true);

  const submit = (): void => {
    setSubmitted(true);
    const invalid =
      previewRangeError(weightRange, { integer: false }) !== null ||
      previewRangeError(countRange, { integer: true }) !== null ||
      previewRangeError(amountRange, { integer: true }) !== null ||
      deliveryError !== undefined;
    if (invalid) return;
    onApply({
      id: row?.id ?? previewNextId("row"),
      weightGrams: weightRange ?? previewEmptyRange(),
      itemCount: countRange ?? previewEmptyRange(),
      merchandiseAmount: amountRange ?? previewEmptyRange(),
      freight: parseInput(freight) ?? 0,
      packaging: parseInput(packaging) ?? 0,
      delivery: { minDays: minDays ?? 1, maxDays: maxDays ?? Math.max(minDays ?? 1, 1) },
    });
  };

  return (
    <form
      className="mt-3 space-y-3 rounded-xl border border-border bg-surface p-3"
      data-testid="shipping-preview-row-editor"
      onSubmit={(event) => {
        event.preventDefault();
        submit();
      }}
    >
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h4 className="text-sm font-black text-gray-900">{messages.rowEditorTitle}</h4>
        <p className="text-[11px] text-gray-500">{messages.rowEditorSameGeography}</p>
      </div>

      <div className="grid gap-2 md:grid-cols-3">
        <Field id={`${fieldId}-wf`} label={messages.weightFrom} error={weightError}>
          <Input
            type="number"
            min={0}
            step="0.001"
            dir="ltr"
            value={weightFrom}
            disabled={readOnly}
            data-testid="shipping-preview-row-weight-from"
            onChange={(event) => setWeightFrom(event.target.value)}
          />
        </Field>
        <Field id={`${fieldId}-wt`} label={messages.weightTo} error={weightError}>
          <Input
            type="number"
            min={0}
            step="0.001"
            dir="ltr"
            value={weightTo}
            disabled={readOnly}
            data-testid="shipping-preview-row-weight-to"
            onChange={(event) => setWeightTo(event.target.value)}
          />
        </Field>
        <div className="self-end pb-1 text-[11px] text-gray-500">
          {previewRangeIsEmpty(weightRange) ? messages.noCondition : ""}
        </div>

        <Field id={`${fieldId}-cf`} label={messages.countFrom} error={countError}>
          <Input
            type="number"
            min={0}
            step="1"
            dir="ltr"
            value={countFrom}
            disabled={readOnly}
            data-testid="shipping-preview-row-count-from"
            onChange={(event) => setCountFrom(event.target.value)}
          />
        </Field>
        <Field id={`${fieldId}-ct`} label={messages.countTo} error={countError}>
          <Input
            type="number"
            min={0}
            step="1"
            dir="ltr"
            value={countTo}
            disabled={readOnly}
            data-testid="shipping-preview-row-count-to"
            onChange={(event) => setCountTo(event.target.value)}
          />
        </Field>
        <div className="self-end pb-1 text-[11px] text-gray-500">
          {previewRangeIsEmpty(countRange) ? messages.noCondition : ""}
        </div>

        <Field id={`${fieldId}-af`} label={messages.amountFrom} error={amountError}>
          <Input
            type="number"
            min={0}
            step="1"
            dir="ltr"
            value={amountFrom}
            disabled={readOnly}
            data-testid="shipping-preview-row-amount-from"
            onChange={(event) => setAmountFrom(event.target.value)}
          />
        </Field>
        <Field id={`${fieldId}-at`} label={messages.amountTo} error={amountError}>
          <Input
            type="number"
            min={0}
            step="1"
            dir="ltr"
            value={amountTo}
            disabled={readOnly}
            data-testid="shipping-preview-row-amount-to"
            onChange={(event) => setAmountTo(event.target.value)}
          />
        </Field>
        <div className="self-end pb-1 text-[11px] text-gray-500">
          {previewRangeIsEmpty(amountRange) ? messages.noCondition : ""}
        </div>

        <Field id={`${fieldId}-fr`} label={`${messages.freightField} (${previewCurrencyUnitLabel(locale)})`}>
          <Input
            type="number"
            min={0}
            step="1"
            dir="ltr"
            value={freight}
            disabled={readOnly}
            data-testid="shipping-preview-row-freight"
            onChange={(event) => setFreight(event.target.value)}
          />
        </Field>
        <Field id={`${fieldId}-pk`} label={`${messages.packagingField} (${previewCurrencyUnitLabel(locale)})`}>
          <Input
            type="number"
            min={0}
            step="1"
            dir="ltr"
            value={packaging}
            disabled={readOnly}
            data-testid="shipping-preview-row-packaging"
            onChange={(event) => setPackaging(event.target.value)}
          />
        </Field>
        <div className="grid grid-cols-2 gap-2">
          <Field id={`${fieldId}-dm`} label={messages.deliveryMin} error={deliveryError}>
            <Input
              type="number"
              min={0}
              step="1"
              dir="ltr"
              value={deliveryMin}
              disabled={readOnly}
              data-testid="shipping-preview-row-delivery-min"
              onChange={(event) => setDeliveryMin(event.target.value)}
            />
          </Field>
          <Field id={`${fieldId}-dx`} label={messages.deliveryMax} error={deliveryError}>
            <Input
              type="number"
              min={0}
              step="1"
              dir="ltr"
              value={deliveryMax}
              disabled={readOnly}
              data-testid="shipping-preview-row-delivery-max"
              onChange={(event) => setDeliveryMax(event.target.value)}
            />
          </Field>
        </div>
      </div>

      <p className="text-[11px] text-gray-500">{messages.ratesBoundsNote}</p>

      <div className="flex flex-wrap items-center justify-end gap-2">
        <button
          type="button"
          className="inline-flex min-h-9 items-center rounded-lg border border-border px-3 text-xs font-bold text-gray-700"
          data-testid="shipping-preview-row-cancel"
          onClick={onCancel}
        >
          {readOnly ? messages.rowClose : messages.rowCancel}
        </button>
        {readOnly ? null : (
          <button
            type="submit"
            className="inline-flex min-h-9 items-center rounded-lg bg-primary px-3 text-xs font-bold text-primary-foreground"
            data-testid="shipping-preview-row-apply"
          >
            {messages.rowApply}
          </button>
        )}
      </div>
      <p className="text-[11px] text-gray-500">{messages.ratesAndNote}</p>
    </form>
  );
}
