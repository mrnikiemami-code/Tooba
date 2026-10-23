"use client";

/**
 * Presentational GENERAL tab of one existing draft: real type, multilingual name/description, two
 * independent settings, and the ONE method-level schedule slot. No fetch, no session, no business rule.
 */
import type { ReactNode } from "react";
import { Field, Input, Select, Textarea } from "../../../design-system";
import type { AuthoringLocale, AuthoringMessages } from "../messages/authoring-messages.ts";
import { authoringFormat } from "../messages/authoring-messages.ts";
import type {
  AuthoringCalculationMode,
  EditorContentLanguage,
  EditorDocument,
  EditorShippingType,
  FreightPaymentMode,
} from "../model/editor-document.ts";
import { descriptionOf, nameOf, selectedShippingTypeValue, type EditorFormState } from "../model/editor-form.ts";
import type { AuthoringTypeChoice } from "../api/authoring-lookup-api.ts";
import { formatNumber } from "./authoring-format.ts";

const WEEKDAY_ANCHOR = Date.UTC(2024, 0, 1); // 2024-01-01 was a Monday (ISO weekday 1).

/** Localized weekday name of an ISO weekday (1 = Monday) in the interface locale. */
export function weekdayLabel(weekday: number, locale: AuthoringLocale): string {
  const date = new Date(WEEKDAY_ANCHOR + (weekday - 1) * 86_400_000);
  return new Intl.DateTimeFormat(locale === "fa" ? "fa-IR" : "en-US", { weekday: "long", timeZone: "UTC" }).format(date);
}

/** Real name of one type for the current content language; no code-as-name fallback. */
export function typeDisplayName(
  type: EditorShippingType,
  contentLanguageId: string,
  messages: AuthoringMessages,
): string {
  const key = contentLanguageId.toLowerCase();
  const exact = type.translations.find((translation) => translation.languageId.toLowerCase() === key);
  const name = exact?.name.trim() ?? "";
  return name.length > 0 ? name : messages.typeUnavailableName;
}

function languageLabel(language: EditorContentLanguage, locale: AuthoringLocale): string {
  const preferred = locale === "fa" ? language.displayName : language.nativeName;
  return (preferred || language.displayName || language.nativeName).trim();
}

export function AuthoringGeneralTab({
  document,
  form,
  mode,
  locale,
  messages,
  contentLanguageId,
  onContentLanguageChange,
  typeChoices,
  typeChoicesState,
  nameIssueLanguageIds,
  settingsIncomplete,
  onName,
  onDescription,
  onFreightPaymentMode,
  onCalculationMode,
  onShippingType,
  scheduleSlot,
}: {
  document: EditorDocument;
  form: EditorFormState;
  mode: "view" | "edit";
  locale: AuthoringLocale;
  messages: AuthoringMessages;
  contentLanguageId: string;
  onContentLanguageChange: (languageId: string) => void;
  typeChoices: readonly AuthoringTypeChoice[];
  typeChoicesState: "loading" | "ok" | "error";
  nameIssueLanguageIds: readonly string[];
  settingsIncomplete: boolean;
  /** The ONE method-level schedule component, composed here so the section is not duplicated. */
  scheduleSlot: ReactNode;
  onName: (languageId: string, value: string) => void;
  onDescription: (languageId: string, value: string) => void;
  onFreightPaymentMode: (mode: FreightPaymentMode) => void;
  onCalculationMode: (mode: AuthoringCalculationMode) => void;
  onShippingType: (shippingTypeId: string | null) => void;
}) {
  const readOnly = mode === "view";
  const active = document.presentation.contentLanguages.find(
    (language) => language.languageId.toLowerCase() === contentLanguageId.toLowerCase(),
  );
  const storedTranslations = document.presentation.methodTranslations;
  const activeIds = new Set(document.presentation.contentLanguages.map((l) => l.languageId.toLowerCase()));
  const inactiveStored = storedTranslations.filter((t) => !activeIds.has(t.languageId.toLowerCase()));
  const translatedCount = document.presentation.contentLanguages.filter((language) => {
    const stored = storedTranslations.find(
      (entry) => entry.languageId.toLowerCase() === language.languageId.toLowerCase(),
    );
    return (stored?.name ?? "").trim().length > 0 || nameOf(form, language.languageId).trim().length > 0;
  }).length;
  const selectedTypeValue = selectedShippingTypeValue(form, document);
  const storedType = document.type.shippingType;
  const nameInvalid = active ? nameIssueLanguageIds.some((id) => id.toLowerCase() === active.languageId.toLowerCase()) : false;

  return (
    <div className="grid gap-4" data-testid="shipping-authoring-general-tab">
      <section className="rounded-xl border border-border bg-surface px-3 py-2 text-xs text-gray-600">
        <span className="font-bold text-gray-900">{messages.ownerLabel}: </span>
        {document.owner.kind === "Marketplace"
          ? messages.ownerMarketplace
          : document.owner.kind === "Seller"
            ? messages.ownerSeller
            : messages.ownerStore}
        <span className="mx-1">·</span>
        <span className="text-gray-500">{messages.ownerNote}</span>
      </section>

      <div className="grid gap-3 md:grid-cols-2">
        <Field id="authoring-type" label={messages.typeLabel} hint={messages.typeHint}>
          <Select
            value={selectedTypeValue}
            disabled={readOnly}
            aria-invalid={undefined}
            data-testid="shipping-authoring-type"
            onChange={(event) => onShippingType(event.target.value === "" ? null : event.target.value)}
          >
            {document.type.shippingTypeId === null && form.typeSelection.kind === "preserve" ? (
              <option value="">{messages.typeUnconfigured}</option>
            ) : null}
            {document.type.shippingTypeId !== null && typeChoices.every((type) => type.shippingTypeId !== document.type.shippingTypeId) ? (
              <option value={document.type.shippingTypeId}>
                {storedType ? typeDisplayName(storedType, contentLanguageId, messages) : messages.typeUnavailableName}
              </option>
            ) : null}
            {typeChoices.map((type) => (
              <option key={type.shippingTypeId} value={type.shippingTypeId}>
                {typeDisplayName(
                  {
                    shippingTypeId: type.shippingTypeId,
                    code: type.code,
                    sortOrder: type.sortOrder,
                    revision: type.revision,
                    translations: type.translations,
                  },
                  contentLanguageId,
                  messages,
                )}
              </option>
            ))}
            <option value="">{messages.typeClearOption}</option>
          </Select>
        </Field>

        <div className="rounded-xl border border-border bg-surface px-3 py-2 text-xs text-gray-600">
          <p data-testid="shipping-authoring-type-state">
            <span className="font-bold text-gray-900">{messages.typeLabel}: </span>
            {document.type.resolution === "Active"
              ? messages.typeActive
              : document.type.resolution === "Inactive"
                ? messages.typeInactiveStored
                : document.type.resolution === "Missing"
                  ? messages.typeMissingStored
                  : messages.typeUnconfigured}
          </p>
          {typeChoicesState === "loading" ? <p className="mt-1 text-gray-500">{messages.entryLoading}</p> : null}
          {typeChoicesState === "error" ? <p className="mt-1 text-danger">{messages.entryFailed}</p> : null}
          {typeChoicesState === "ok" && typeChoices.length === 0 ? (
            <p className="mt-1 text-gray-500">{messages.typeCatalogEmpty}</p>
          ) : null}
        </div>
      </div>

      <section className="rounded-xl border border-border bg-surface-elevated p-3 md:p-4">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <div className="flex flex-wrap items-center gap-1" role="group" aria-label={messages.contentLanguage}>
            {document.presentation.contentLanguages.map((language) => {
              const selected = language.languageId.toLowerCase() === contentLanguageId.toLowerCase();
              const stored = storedTranslations.find(
                (entry) => entry.languageId.toLowerCase() === language.languageId.toLowerCase(),
              );
              const translated = (stored?.name ?? "").trim().length > 0 || nameOf(form, language.languageId).trim().length > 0;
              return (
                <button
                  key={language.languageId}
                  type="button"
                  aria-pressed={selected}
                  data-testid={`shipping-authoring-content-language-${language.code}`}
                  className={
                    selected
                      ? "inline-flex items-center gap-1 rounded-lg bg-primary px-2.5 py-1 text-xs font-bold text-primary-foreground"
                      : "inline-flex items-center gap-1 rounded-lg border border-border px-2.5 py-1 text-xs font-bold text-gray-700"
                  }
                  onClick={() => onContentLanguageChange(language.languageId)}
                >
                  {languageLabel(language, locale)}
                  <span
                    aria-hidden
                    className={translated ? "size-1.5 rounded-full bg-emerald-400" : "size-1.5 rounded-full bg-amber-400"}
                  />
                  <span className="sr-only">{translated ? messages.defaultBadge : messages.notTranslated}</span>
                </button>
              );
            })}
          </div>
          <p className="text-[11px] text-gray-500" data-testid="shipping-authoring-translation-summary">
            {authoringFormat(messages.translationSummary, {
              translated: formatNumber(translatedCount, locale),
              total: formatNumber(document.presentation.contentLanguages.length, locale),
            })}
            {" · "}
            {document.presentation.hasDefaultLanguageName ? messages.defaultNamePresent : messages.defaultNameMissing}
          </p>
        </div>

        {active ? (
          <div className="mt-3 grid gap-3">
            <Field
              id="authoring-name"
              label={`${messages.nameLabel} — ${languageLabel(active, locale)}`}
              hint={messages.contentLanguageHint}
              error={nameInvalid ? messages.nameRequired : undefined}
            >
              <Input
                dir={active.direction}
                value={nameOf(form, active.languageId)}
                maxLength={128}
                disabled={readOnly}
                invalid={nameInvalid}
                data-testid="shipping-authoring-name"
                onChange={(event) => onName(active.languageId, event.target.value)}
              />
            </Field>
            <Field
              id="authoring-description"
              label={`${messages.descriptionLabel} — ${languageLabel(active, locale)}`}
            >
              <Textarea
                dir={active.direction}
                value={descriptionOf(form, active.languageId)}
                rows={3}
                maxLength={512}
                disabled={readOnly}
                data-testid="shipping-authoring-description"
                onChange={(event) => onDescription(active.languageId, event.target.value)}
              />
            </Field>
          </div>
        ) : (
          <p className="mt-3 text-xs text-danger">{messages.loadUntrusted}</p>
        )}

        {inactiveStored.length > 0 ? (
          <div className="mt-3 rounded-lg border border-border bg-surface px-2 py-1 text-[11px] text-gray-600">
            <p className="font-bold text-gray-800">{messages.storedInactiveTitle}</p>
            <ul className="mt-1 space-y-0.5">
              {inactiveStored.map((entry) => (
                <li key={entry.languageId} dir="auto" data-testid="shipping-authoring-inactive-stored">
                  {entry.name}
                  {entry.description ? ` — ${entry.description}` : ""}
                </li>
              ))}
            </ul>
            <p className="mt-1 text-gray-500">{messages.storedInactiveNote}</p>
          </div>
        ) : null}
      </section>

      <div className="grid gap-3 md:grid-cols-2">
        <fieldset className="rounded-xl border border-border bg-surface-elevated p-3">
          <legend className="px-1 text-xs font-black text-gray-900">{messages.freightTitle}</legend>
          <div className="mt-1 flex flex-wrap gap-4">
            {(["Prepaid", "CollectOnDelivery"] as FreightPaymentMode[]).map((mode) => (
              <label key={mode} className="inline-flex items-center gap-2 text-sm">
                <input
                  type="radio"
                  name="authoring-freight-payer"
                  value={mode}
                  checked={form.freightPaymentMode === mode}
                  disabled={readOnly}
                  data-testid={`shipping-authoring-freight-${mode}`}
                  onChange={() => onFreightPaymentMode(mode)}
                />
                {mode === "Prepaid" ? messages.freightPrepaid : messages.freightCollect}
              </label>
            ))}
          </div>
        </fieldset>

        <fieldset className="rounded-xl border border-border bg-surface-elevated p-3">
          <legend className="px-1 text-xs font-black text-gray-900">{messages.calculationTitle}</legend>
          <div className="mt-1 flex flex-wrap gap-4">
            {(["Manual", "CarrierApi", "Free"] as AuthoringCalculationMode[]).map((mode) => (
              <label key={mode} className="inline-flex items-center gap-2 text-sm">
                <input
                  type="radio"
                  name="authoring-calculation"
                  value={mode}
                  checked={form.calculationMode === mode}
                  disabled={readOnly}
                  data-testid={`shipping-authoring-calculation-${mode}`}
                  onChange={() => onCalculationMode(mode)}
                />
                {mode === "Manual"
                  ? messages.calculationManual
                  : mode === "CarrierApi"
                    ? messages.calculationCarrier
                    : messages.calculationFree}
              </label>
            ))}
          </div>
        </fieldset>
      </div>

      <p className="text-[11px] text-gray-500">
        {form.freightPaymentMode === null && form.calculationMode === null ? messages.settingsBlank : messages.settingsNote}
      </p>
      {settingsIncomplete ? (
        <p role="alert" className="rounded-lg border border-danger/40 bg-red-50 px-2 py-1 text-xs font-semibold text-danger">
          {messages.settingsIncomplete}
        </p>
      ) : null}

      {scheduleSlot}
    </div>
  );
}
