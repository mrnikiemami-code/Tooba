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
  previewNextId,
  previewRowFromRawInputs,
  previewRowInputErrors,
  previewRowInputsAreValid,
  type PreviewLocale,
  type PreviewRateRow,
  type PreviewRowInputErrors,
  type PreviewRowInputIssue,
  type PreviewRowRawInputs,
} from "./preview-model.ts";
import {
  previewCurrencyUnitLabel,
  type PreviewMessages,
} from "./preview-i18n.ts";

function numericInput(value: number | null): string {
  return value === null ? "" : String(value);
}

/** مقدار نمایشی کیلوگرم از گرم ذخیره‌شده؛ فقط نمایش/ویرایش، بدون تغییر دقت ذخیره. */
function kilogramInput(grams: number | null | undefined): string {
  return grams === null || grams === undefined ? "" : String(grams / 1000);
}

function initialRawInputs(row: PreviewRateRow | null): PreviewRowRawInputs {
  return {
    weightFromKg: kilogramInput(row?.weightGrams?.from),
    weightToKg: kilogramInput(row?.weightGrams?.to),
    countFrom: numericInput(row?.itemCount?.from ?? null),
    countTo: numericInput(row?.itemCount?.to ?? null),
    amountFrom: numericInput(row?.merchandiseAmount?.from ?? null),
    amountTo: numericInput(row?.merchandiseAmount?.to ?? null),
    freight: String(row?.freight ?? 0),
    packaging: String(row?.packaging ?? 0),
    deliveryMin: String(row?.delivery.minDays ?? 1),
    deliveryMax: String(row?.delivery.maxDays ?? 3),
  };
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
  const [raw, setRaw] = useState<PreviewRowRawInputs>(() => initialRawInputs(row));
  const [submitted, setSubmitted] = useState(false);

  const patchRaw = (key: keyof PreviewRowRawInputs, value: string): void => {
    setRaw((current) => ({ ...current, [key]: value }));
  };

  /** خطاها همیشه از ورودی خام همین رندر محاسبه می‌شوند؛ نه از مقدار مشتق‌شدهٔ رندر قبلی. */
  const errors: PreviewRowInputErrors = previewRowInputErrors(raw);

  const messageFor = (issue: PreviewRowInputIssue | null, inverted: string): string | undefined => {
    if (!submitted || issue === null) return undefined;
    if (issue === "inverted") return inverted;
    if (issue === "notInteger") return messages.rowErrorNotInteger;
    if (issue === "notNumber") return messages.rowErrorNotNumber;
    return messages.rowErrorNegative;
  };

  const weightError = messageFor(errors.weight, messages.rowErrorInverted);
  const countError = messageFor(errors.count, messages.rowErrorInverted);
  const amountError = messageFor(errors.amount, messages.rowErrorInverted);
  const deliveryError = messageFor(errors.delivery, messages.rowErrorDelivery);
  const freightError = messageFor(errors.freight, messages.rowErrorInverted);
  const packagingError = messageFor(errors.packaging, messages.rowErrorInverted);

  const weightRangeEmpty = raw.weightFromKg.trim().length === 0 && raw.weightToKg.trim().length === 0;
  const countRangeEmpty = raw.countFrom.trim().length === 0 && raw.countTo.trim().length === 0;
  const amountRangeEmpty = raw.amountFrom.trim().length === 0 && raw.amountTo.trim().length === 0;

  const submit = (): void => {
    // اعتبارسنجی همگام ورودی خام: اولین کلیک با min > max باید همان‌جا رد شود.
    const current = previewRowInputErrors(raw);
    if (!previewRowInputsAreValid(current)) {
      setSubmitted(true);
      return;
    }
    onApply(previewRowFromRawInputs(raw, row?.id ?? previewNextId("row")));
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
            value={raw.weightFromKg}
            disabled={readOnly}
            data-testid="shipping-preview-row-weight-from"
            onChange={(event) => patchRaw("weightFromKg", event.target.value)}
          />
        </Field>
        <Field id={`${fieldId}-wt`} label={messages.weightTo} error={weightError}>
          <Input
            type="number"
            min={0}
            step="0.001"
            dir="ltr"
            value={raw.weightToKg}
            disabled={readOnly}
            data-testid="shipping-preview-row-weight-to"
            onChange={(event) => patchRaw("weightToKg", event.target.value)}
          />
        </Field>
        <div className="self-end pb-1 text-[11px] text-gray-500">
          {weightRangeEmpty ? messages.noCondition : ""}
        </div>

        <Field id={`${fieldId}-cf`} label={messages.countFrom} error={countError}>
          <Input
            type="number"
            min={0}
            step="1"
            dir="ltr"
            value={raw.countFrom}
            disabled={readOnly}
            data-testid="shipping-preview-row-count-from"
            onChange={(event) => patchRaw("countFrom", event.target.value)}
          />
        </Field>
        <Field id={`${fieldId}-ct`} label={messages.countTo} error={countError}>
          <Input
            type="number"
            min={0}
            step="1"
            dir="ltr"
            value={raw.countTo}
            disabled={readOnly}
            data-testid="shipping-preview-row-count-to"
            onChange={(event) => patchRaw("countTo", event.target.value)}
          />
        </Field>
        <div className="self-end pb-1 text-[11px] text-gray-500">
          {countRangeEmpty ? messages.noCondition : ""}
        </div>

        <Field id={`${fieldId}-af`} label={messages.amountFrom} error={amountError}>
          <Input
            type="number"
            min={0}
            step="1"
            dir="ltr"
            value={raw.amountFrom}
            disabled={readOnly}
            data-testid="shipping-preview-row-amount-from"
            onChange={(event) => patchRaw("amountFrom", event.target.value)}
          />
        </Field>
        <Field id={`${fieldId}-at`} label={messages.amountTo} error={amountError}>
          <Input
            type="number"
            min={0}
            step="1"
            dir="ltr"
            value={raw.amountTo}
            disabled={readOnly}
            data-testid="shipping-preview-row-amount-to"
            onChange={(event) => patchRaw("amountTo", event.target.value)}
          />
        </Field>
        <div className="self-end pb-1 text-[11px] text-gray-500">
          {amountRangeEmpty ? messages.noCondition : ""}
        </div>

        <Field id={`${fieldId}-fr`} label={`${messages.freightField} (${previewCurrencyUnitLabel(locale)})`} error={freightError}>
          <Input
            type="number"
            min={0}
            step="1"
            dir="ltr"
            value={raw.freight}
            disabled={readOnly}
            data-testid="shipping-preview-row-freight"
            onChange={(event) => patchRaw("freight", event.target.value)}
          />
        </Field>
        <Field id={`${fieldId}-pk`} label={`${messages.packagingField} (${previewCurrencyUnitLabel(locale)})`} error={packagingError}>
          <Input
            type="number"
            min={0}
            step="1"
            dir="ltr"
            value={raw.packaging}
            disabled={readOnly}
            data-testid="shipping-preview-row-packaging"
            onChange={(event) => patchRaw("packaging", event.target.value)}
          />
        </Field>
        <div className="grid grid-cols-2 gap-2">
          <Field id={`${fieldId}-dm`} label={messages.deliveryMin} error={deliveryError}>
            <Input
              type="number"
              min={0}
              step="1"
              dir="ltr"
              value={raw.deliveryMin}
              disabled={readOnly}
              data-testid="shipping-preview-row-delivery-min"
              onChange={(event) => patchRaw("deliveryMin", event.target.value)}
            />
          </Field>
          <Field id={`${fieldId}-dx`} label={messages.deliveryMax} error={deliveryError}>
            <Input
              type="number"
              min={0}
              step="1"
              dir="ltr"
              value={raw.deliveryMax}
              disabled={readOnly}
              data-testid="shipping-preview-row-delivery-max"
              onChange={(event) => patchRaw("deliveryMax", event.target.value)}
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
