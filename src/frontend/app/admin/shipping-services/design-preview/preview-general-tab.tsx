"use client";

/**
 * TB-SHIPPING-ADMIN-UI-PREVIEW-186 — زبانهٔ «اطلاعات روش» در صفحهٔ کامل پیش‌نمایش.
 *
 * فقط presentation + ویرایش draft درون‌حافظه؛ هیچ درخواست شبکه‌ای، اعتبارسنجی سرور یا
 * ذخیره‌سازی وجود ندارد. زبان رابط (RTL/LTR) و زبان محتوای در حال ترجمه دو کنترل مستقل‌اند.
 */

import { useId, useState } from "react";
import { ChevronDown, ChevronUp } from "lucide-react";
import { Field, Input, Select, Textarea } from "../../../../design-system";
import {
  PREVIEW_CARRIERS,
  PREVIEW_CONTENT_LANGUAGE_IDS,
  PREVIEW_SHIPPING_TYPE_IDS,
  PREVIEW_STORE_CURRENCY,
  previewTextFor,
  previewTranslationStates,
  previewTranslationSummary,
  type PreviewCalculationMode,
  type PreviewFreightPayerMode,
  type PreviewLocale,
  type PreviewMethod,
  type PreviewShippingTypeId,
} from "./preview-model.ts";
import {
  PREVIEW_WEEKDAY_KEYS,
  formatPreviewMoney,
  formatPreviewNumber,
  formatPreviewTime,
  previewContentLanguageLabel,
  previewFormat,
  type PreviewMessages,
} from "./preview-i18n.ts";

function typeLabel(typeId: PreviewShippingTypeId, messages: PreviewMessages): string {
  switch (typeId) {
    case "heavy":
      return messages.typeHeavy;
    case "extra-heavy":
      return messages.typeExtraHeavy;
    case "grocery":
      return messages.typeGrocery;
    default:
      return messages.typeOrdinary;
  }
}

/** زبان‌های محتوا همه با همان شناسه‌های fixture برچسب می‌خورند؛ نام زبان ساخته نمی‌شود. */
const contentLanguageLabel = previewContentLanguageLabel;

export function PreviewGeneralTab({
  draft,
  readOnly,
  locale,
  messages,
  contentLanguageId,
  onContentLanguageChange,
  onChange,
}: {
  draft: PreviewMethod;
  readOnly: boolean;
  locale: PreviewLocale;
  messages: PreviewMessages;
  contentLanguageId: string;
  onContentLanguageChange: (languageId: string) => void;
  onChange: (next: PreviewMethod) => void;
}) {
  const fieldId = useId();
  const [scheduleOpen, setScheduleOpen] = useState(false);

  const nameStates = previewTranslationStates(draft.names);
  const summary = previewTranslationSummary(draft.names);
  const activeName = previewTextFor(draft.names, contentLanguageId);
  const activeDescription = previewTextFor(draft.descriptions, contentLanguageId);
  const referenceLanguage = PREVIEW_CONTENT_LANGUAGE_IDS[0];
  const nameError =
    !readOnly && contentLanguageId === referenceLanguage && activeName.trim().length === 0
      ? messages.textRequiredError
      : undefined;

  const patch = (partial: Partial<PreviewMethod>): void => onChange({ ...draft, ...partial });

  const setLocalized = (
    field: "names" | "descriptions",
    languageId: string,
    value: string,
  ): void => patch({ [field]: { ...draft[field], [languageId]: value } } as Partial<PreviewMethod>);

  const totalRows = draft.regionGroups.reduce((total, group) => total + group.rows.length, 0);
  const scheduleDays = draft.schedule.weekdays
    .map((day) => messages[PREVIEW_WEEKDAY_KEYS[day]] ?? "")
    .filter((label) => label.length > 0)
    .join("، ");

  return (
    <div className="grid gap-5" data-testid="shipping-preview-general-tab">
      {readOnly ? (
        <p className="rounded-xl border border-gray-200 bg-gray-50 px-3 py-2 text-xs font-semibold text-gray-600">
          {messages.viewReadOnly}
        </p>
      ) : null}

      {/* نوع ارسال بالای نام روش — همان ترتیب تصمیم محصول */}
      <div className="grid gap-3 md:grid-cols-2">
        <Field id={`${fieldId}-type`} label={messages.shippingType} hint={messages.shippingTypeHint}>
          <Select
            value={draft.shippingTypeId}
            disabled={readOnly}
            data-testid="shipping-preview-type"
            onChange={(event) => patch({ shippingTypeId: event.target.value as PreviewShippingTypeId })}
          >
            {PREVIEW_SHIPPING_TYPE_IDS.map((typeId) => (
              <option key={typeId} value={typeId}>
                {typeLabel(typeId, messages)}
              </option>
            ))}
          </Select>
        </Field>

        <div className="rounded-xl border border-border bg-surface px-3 py-2 text-xs text-gray-600">
          <span className="font-bold text-gray-900">{messages.currencyLabel}: </span>
          {formatPreviewMoney(0, locale, PREVIEW_STORE_CURRENCY)}
          <span className="mx-1">·</span>
          <span className="text-gray-500">{messages.currencyNote}</span>
        </div>
      </div>

      {/* ویرایشگر محتوای چندزبانه با زبانه‌های فشردهٔ زبان */}
      <section className="rounded-2xl border border-border bg-surface-elevated p-3 md:p-4">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <div className="flex flex-wrap items-center gap-1" role="group" aria-label={messages.contentLanguage}>
            {nameStates.map((state) => {
              const selected = state.languageId === contentLanguageId;
              return (
                <button
                  key={state.languageId}
                  type="button"
                  aria-pressed={selected}
                  data-testid={`shipping-preview-content-lang-${state.languageId}`}
                  onClick={() => onContentLanguageChange(state.languageId)}
                  className={
                    selected
                      ? "inline-flex items-center gap-1 rounded-lg bg-primary px-2.5 py-1 text-xs font-bold text-primary-foreground"
                      : "inline-flex items-center gap-1 rounded-lg border border-border px-2.5 py-1 text-xs font-bold text-gray-700"
                  }
                >
                  {contentLanguageLabel(state.languageId, locale)}
                  <span
                    aria-hidden
                    className={state.translated ? "size-1.5 rounded-full bg-emerald-400" : "size-1.5 rounded-full bg-amber-400"}
                  />
                  <span className="sr-only">{state.translated ? messages.translated : messages.notTranslated}</span>
                </button>
              );
            })}
          </div>
          <p className="text-[11px] text-gray-500" data-testid="shipping-preview-translation-summary">
            {previewFormat(messages.translationSummary, {
              translated: formatPreviewNumber(summary.translated, locale),
              total: formatPreviewNumber(summary.total, locale),
            })}
          </p>
        </div>

        <div className="mt-3 grid gap-3">
          <Field
            id={`${fieldId}-name`}
            label={`${messages.methodName} — ${contentLanguageLabel(contentLanguageId, locale)}`}
            error={nameError}
            hint={
              contentLanguageId === referenceLanguage
                ? `${messages.referenceLanguage} · ${messages.contentLanguageHint}`
                : messages.contentLanguageHint
            }
          >
            <Input
              dir={contentLanguageId === "fa" ? "rtl" : "ltr"}
              value={activeName}
              maxLength={128}
              disabled={readOnly}
              data-testid="shipping-preview-method-name"
              onChange={(event) => setLocalized("names", contentLanguageId, event.target.value)}
            />
          </Field>

          <Field
            id={`${fieldId}-description`}
            label={`${messages.methodDescription} — ${contentLanguageLabel(contentLanguageId, locale)}`}
          >
            <Textarea
              dir={contentLanguageId === "fa" ? "rtl" : "ltr"}
              value={activeDescription}
              rows={3}
              maxLength={512}
              disabled={readOnly}
              data-testid="shipping-preview-method-description"
              onChange={(event) => setLocalized("descriptions", contentLanguageId, event.target.value)}
            />
          </Field>
        </div>

        <ul className="mt-3 flex flex-wrap gap-2 text-[11px]">
          {nameStates.map((state) => (
            <li
              key={state.languageId}
              className={
                state.translated
                  ? "rounded-full bg-emerald-50 px-2 py-0.5 font-bold text-emerald-700"
                  : "rounded-full bg-amber-50 px-2 py-0.5 font-bold text-amber-700"
              }
            >
              {contentLanguageLabel(state.languageId, locale)}:{" "}
              {state.translated ? messages.translated : messages.notTranslated}
              {state.isReference ? ` · ${messages.referenceLanguage}` : ""}
            </li>
          ))}
        </ul>
      </section>

      {/* دو محور مستقل: زمان پرداخت کرایه و نحوهٔ محاسبه */}
      <div className="grid gap-3 md:grid-cols-2">
        <fieldset className="rounded-2xl border border-border bg-surface-elevated p-3">
          <legend className="px-1 text-xs font-black text-gray-900">{messages.freightPayerTitle}</legend>
          <div className="mt-1 flex flex-wrap gap-4">
            {(["prepaid", "collect"] as PreviewFreightPayerMode[]).map((mode) => (
              <label key={mode} className="inline-flex items-center gap-2 text-sm">
                <input
                  type="radio"
                  name={`${fieldId}-payer`}
                  value={mode}
                  checked={draft.freightPayerMode === mode}
                  disabled={readOnly}
                  data-testid={`shipping-preview-payer-${mode}`}
                  onChange={() => patch({ freightPayerMode: mode })}
                />
                {mode === "prepaid" ? messages.freightPayerPrepaid : messages.freightPayerCollect}
              </label>
            ))}
          </div>
          <p className="mt-2 text-[11px] text-gray-500">{messages.freightPayerHint}</p>
        </fieldset>

        <fieldset className="rounded-2xl border border-border bg-surface-elevated p-3">
          <legend className="px-1 text-xs font-black text-gray-900">{messages.calculationTitle}</legend>
          <div className="mt-1 flex flex-wrap gap-4">
            {(["manual", "carrier", "free"] as PreviewCalculationMode[]).map((mode) => (
              <label key={mode} className="inline-flex items-center gap-2 text-sm">
                <input
                  type="radio"
                  name={`${fieldId}-calc`}
                  value={mode}
                  checked={draft.calculationMode === mode}
                  disabled={readOnly}
                  data-testid={`shipping-preview-calc-${mode}`}
                  onChange={() => patch({ calculationMode: mode })}
                />
                {mode === "manual"
                  ? messages.calculationManual
                  : mode === "carrier"
                    ? messages.calculationCarrier
                    : messages.calculationFree}
              </label>
            ))}
          </div>
          <p className="mt-2 text-[11px] text-gray-500">{messages.calculationHint}</p>
        </fieldset>
      </div>

      {/* پیکربندی وابسته به نحوهٔ محاسبه */}
      <section className="rounded-2xl border border-border bg-surface p-3 md:p-4" data-testid={`shipping-preview-calc-panel-${draft.calculationMode}`}>
        {draft.calculationMode === "manual" ? (
          <div className="space-y-1">
            <h3 className="text-sm font-black text-gray-900">{messages.manualTitle}</h3>
            <p className="text-xs text-gray-600">{messages.manualNote}</p>
            <p className="text-[11px] text-gray-500">
              {previewFormat(messages.manualRowsHint, {
                count: formatPreviewNumber(totalRows, locale),
                groups: formatPreviewNumber(draft.regionGroups.length, locale),
              })}
            </p>
          </div>
        ) : null}

        {draft.calculationMode === "carrier" ? (
          <div className="grid gap-3 md:grid-cols-2">
            <Field id={`${fieldId}-carrier`} label={messages.carrierSelect}>
              <Select
                value={draft.carrierId ?? ""}
                disabled={readOnly}
                data-testid="shipping-preview-carrier"
                onChange={(event) => patch({ carrierId: event.target.value || null })}
              >
                <option value="">{messages.dash}</option>
                {PREVIEW_CARRIERS.map((carrier) => (
                  <option key={carrier.id} value={carrier.id}>
                    {carrier.labels[locale] ?? carrier.labels.fa ?? carrier.id}
                  </option>
                ))}
              </Select>
            </Field>
            <div className="text-xs text-gray-600">
              <p className="font-bold text-gray-900">{messages.carrierStatus}</p>
              <p className="mt-1">{messages.carrierStatusSample}</p>
            </div>
            <p className="md:col-span-2 text-[11px] text-gray-500">{messages.carrierCoordinates}</p>
          </div>
        ) : null}

        {draft.calculationMode === "free" ? (
          <div className="space-y-1">
            <h3 className="text-sm font-black text-gray-900">{messages.freeTitle}</h3>
            <p className="text-xs text-gray-600">{messages.freeNote}</p>
            <p className="text-xs text-gray-700">
              <span className="font-bold">{messages.freeFunding}: </span>
              {messages.ownerMarketplace}
            </p>
            <p className="text-[11px] text-gray-500">{messages.freePromotionNote}</p>
          </div>
        ) : null}
      </section>

      {/* برنامهٔ کاری مشترک — جمع‌شده با بازشدن اختیاری */}
      <section className="rounded-2xl border border-border bg-surface-elevated p-3 md:p-4">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <div>
            <h3 className="text-sm font-black text-gray-900">{messages.scheduleTitle}</h3>
            <p className="mt-1 text-xs text-gray-600" data-testid="shipping-preview-schedule-summary">
              {previewFormat(messages.scheduleSummary, {
                days: scheduleDays || messages.dash,
                from: formatPreviewTime(draft.schedule.fromTime, locale),
                to: formatPreviewTime(draft.schedule.toTime, locale),
                cutoff: formatPreviewNumber(draft.schedule.cutoffHours, locale),
              })}
            </p>
          </div>
          <button
            type="button"
            className="inline-flex min-h-9 items-center gap-1 rounded-lg border border-border px-3 text-xs font-bold text-gray-700"
            aria-expanded={scheduleOpen}
            data-testid="shipping-preview-schedule-toggle"
            onClick={() => setScheduleOpen((current) => !current)}
          >
            {scheduleOpen ? messages.scheduleCollapse : messages.scheduleExpand}
            {scheduleOpen ? <ChevronUp className="size-3.5" aria-hidden /> : <ChevronDown className="size-3.5" aria-hidden />}
          </button>
        </div>

        {scheduleOpen ? (
          <div className="mt-3 grid gap-3">
            <div>
              <p className="text-xs font-bold text-gray-900">{messages.scheduleDays}</p>
              <div className="mt-1 flex flex-wrap gap-2">
                {PREVIEW_WEEKDAY_KEYS.map((key, day) => {
                  const checked = draft.schedule.weekdays.includes(day);
                  return (
                    <label key={key} className="inline-flex items-center gap-1 text-xs">
                      <input
                        type="checkbox"
                        checked={checked}
                        disabled={readOnly}
                        data-testid={`shipping-preview-weekday-${day}`}
                        onChange={() =>
                          patch({
                            schedule: {
                              ...draft.schedule,
                              weekdays: checked
                                ? draft.schedule.weekdays.filter((value) => value !== day)
                                : [...draft.schedule.weekdays, day].sort((a, b) => a - b),
                            },
                          })
                        }
                      />
                      {messages[key]}
                    </label>
                  );
                })}
              </div>
            </div>
            <div className="grid gap-3 md:grid-cols-3">
              <Field id={`${fieldId}-from`} label={messages.scheduleFrom}>
                <Input
                  type="time"
                  value={draft.schedule.fromTime}
                  disabled={readOnly}
                  dir="ltr"
                  onChange={(event) =>
                    patch({ schedule: { ...draft.schedule, fromTime: event.target.value } })
                  }
                />
              </Field>
              <Field id={`${fieldId}-to`} label={messages.scheduleTo}>
                <Input
                  type="time"
                  value={draft.schedule.toTime}
                  disabled={readOnly}
                  dir="ltr"
                  onChange={(event) => patch({ schedule: { ...draft.schedule, toTime: event.target.value } })}
                />
              </Field>
              <Field id={`${fieldId}-cutoff`} label={messages.scheduleCutoff}>
                <Input
                  type="number"
                  min={0}
                  max={48}
                  value={draft.schedule.cutoffHours}
                  disabled={readOnly}
                  dir="ltr"
                  onChange={(event) =>
                    patch({ schedule: { ...draft.schedule, cutoffHours: Number(event.target.value) } })
                  }
                />
              </Field>
            </div>
          </div>
        ) : null}

        <p className="mt-2 text-[11px] text-gray-500">{messages.scheduleSharedNote}</p>
        <p className="mt-1 text-[11px] text-gray-500">{messages.preparationNote}</p>
      </section>
    </div>
  );
}
