"use client";

/**
 * Editable rows of ONE existing region group: identity, order, currency and geography are shown
 * read-only exactly once, while the operator can add, remove and edit the rate rows beneath it.
 *
 * TB-224: the shipping duration is ONE customer-receiving range of the WHOLE delivery area, placed above
 * the rows; the rows carry only freight/packaging and their optional weight/count/amount conditions. The
 * persisted wire still repeats the days per row, so the area control writes the chosen pair to exactly the
 * rows of this area (see `model/editor-area-range.ts`).
 *
 * Pure presentation: no fetch, no session, no pricing/overlap policy. Money fields are TEXT inputs whose
 * DISPLAY is grouped while the model keeps the canonical ungrouped token, so `0.50` / `1850000.00` are
 * never transformed by `input[type=number]` or a float round-trip.
 */
import { useLayoutEffect, useRef } from "react";
import type { AuthoringLocale, AuthoringMessages } from "../messages/authoring-messages.ts";
import { authoringFormat } from "../messages/authoring-messages.ts";
import type { EditorRegionGroup } from "../model/editor-document.ts";
import type { DestinationFactIndex } from "../model/editor-destination-ancestry.ts";
import { RATE_LIMITS, type RateField, type RateRowDraft, type RateRowIssue } from "../model/editor-rates.ts";
import {
  type AreaRangeDraftIssueCode,
  type AreaRangePair,
  type AreaRangeView,
} from "../model/editor-area-range.ts";
import { caretOffsetFor, groupMoneyText, interpretMoneyEdit, isCanonicalMoneyText, type MoneyCaret } from "../model/money-text.ts";
import { rateDraftConditionText } from "./rate-presentation.ts";
import { formatDecimalText, formatNumber } from "./authoring-format.ts";

const AREA_KIND_LABEL = {
  Country: "areaCountry",
  AdministrativeArea: "areaAdministrativeArea",
  City: "areaCity",
  ServiceArea: "areaServiceArea",
} as const;

/** Issue message of one row (first blocking reason). */
export function rowIssueMessage(code: RateRowIssue["code"], messages: AuthoringMessages): string {
  switch (code) {
    case "rowLimitExceeded":
      return messages.rowLimitExceeded;
    case "boundIncomplete":
      return messages.boundIncomplete;
    case "integerInvalid":
      return messages.integerInvalid;
    case "boundRangeInvalid":
      return messages.boundRangeInvalid;
    case "amountRequired":
      return messages.amountRequired;
    case "amountInvalid":
      return messages.amountInvalid;
    case "durationRequired":
      return messages.durationRequired;
    case "durationInvalid":
      return messages.durationInvalid;
  }
}

/** Issue message of the shared area range. */
export function areaRangeIssueMessage(code: AreaRangeDraftIssueCode, messages: AuthoringMessages): string {
  if (code === "durationRequired") return messages.durationRequired;
  if (code === "durationInvalid") return messages.durationInvalid;
  return messages.areaRangeRowsRequired;
}

function BoundField({
  label,
  value,
  onChange,
  testId,
  inputMode,
  placeholder,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  testId: string;
  inputMode?: "numeric" | "decimal";
  placeholder?: string;
}) {
  return (
    <label className="grid gap-0.5">
      <span className="text-xs font-bold text-gray-700">{label}</span>
      <input
        type="text"
        inputMode={inputMode}
        dir="ltr"
        className="min-h-9 w-full rounded-lg border border-border bg-surface px-2 text-sm text-gray-900"
        value={value}
        placeholder={placeholder}
        onChange={(event) => onChange(event.target.value)}
        data-testid={testId}
      />
    </label>
  );
}

/**
 * Exact money field: grouped DISPLAY, canonical token to the model, caret anchored to a digit position.
 *
 * The displayed value is derived from the model token on every render, so grouping can never become part of
 * the committed value; an INVALID text is shown exactly as typed (never trimmed down to different digits)
 * and is refused by the row/amount validation, and the caret anchor keeps which side of the decimal
 * separator it is on, so typing `12`, `.`, `5` really yields `12.5`.
 */
export function MoneyField({
  label,
  value,
  onChange,
  testId,
  placeholder,
}: {
  label: string;
  value: string;
  /** Receives the CANONICAL ungrouped token, or the invalid text as typed. */
  onChange: (raw: string) => void;
  testId: string;
  placeholder?: string;
}) {
  const input = useRef<HTMLInputElement>(null);
  const pendingCaret = useRef<MoneyCaret | null>(null);

  useLayoutEffect(() => {
    const element = input.current;
    const anchor = pendingCaret.current;
    pendingCaret.current = null;
    if (element === null || anchor === null) return;
    const caret = caretOffsetFor(element.value, anchor);
    element.setSelectionRange(caret, caret);
  });

  return (
    <label className="grid gap-0.5">
      <span className="text-xs font-bold text-gray-700">{label}</span>
      <input
        ref={input}
        type="text"
        inputMode="decimal"
        dir="ltr"
        className="min-h-9 w-full rounded-lg border border-border bg-surface px-2 text-sm text-gray-900 aria-[invalid=true]:border-danger"
        value={groupMoneyText(value)}
        placeholder={placeholder}
        data-testid={testId}
        aria-invalid={value !== "" && !isCanonicalMoneyText(value)}
        onChange={(event) => {
          const element = event.target;
          const edit = interpretMoneyEdit(element.value, element.selectionStart ?? element.value.length);
          pendingCaret.current = edit.caret;
          onChange(edit.raw);
        }}
      />
    </label>
  );
}

/** One editable row of one group (shared with the group-collection editor). */
export function RateRowEditor({
  row,
  index,
  locale,
  messages,
  issue,
  serverError,
  onChange,
  onRemove,
}: {
  row: RateRowDraft;
  index: number;
  locale: AuthoringLocale;
  messages: AuthoringMessages;
  issue: RateRowIssue | undefined;
  /** Localized reason placed by the SERVER on this exact row (never a generic banner). */
  serverError?: string;
  onChange: (field: RateField, value: string) => void;
  onRemove: () => void;
}) {
  return (
    <li
      className="grid gap-2 rounded-lg border border-border bg-surface p-2"
      data-testid="shipping-authoring-rate-row-edit"
      data-row-index={index}
    >
      <div className="flex items-center justify-between gap-2">
        <span className="text-[11px] font-black text-gray-800">
          {authoringFormat(messages.rowLabel, { index: formatNumber(index + 1, locale) })}
        </span>
        <button
          type="button"
          className="inline-flex min-h-7 items-center rounded-lg border border-border px-2 text-[11px] font-bold text-danger"
          data-testid="shipping-authoring-rate-row-remove"
          onClick={onRemove}
        >
          {messages.removeRowAction}
        </button>
      </div>
      {/* Only the money of THIS row: the customer-receiving duration belongs to the whole area above. */}
      <div className="grid gap-2 sm:grid-cols-2">
        <MoneyField
          label={messages.freightLabel}
          value={row.freightAmount}
          onChange={(value) => onChange("freightAmount", value)}
          testId="shipping-authoring-rate-freight"
        />
        <MoneyField
          label={messages.packagingLabel}
          value={row.packagingAmount}
          onChange={(value) => onChange("packagingAmount", value)}
          testId="shipping-authoring-rate-packaging"
        />
      </div>

      {/* Optional axes stay collapsed but are never hidden: the summary line states the active
          condition, so a configured weight/count/merchandise range is always visible at a glance.
          Opening or closing this disclosure only changes presentation, never the values. */}
      <details className="rounded-lg bg-surface px-2 py-1" data-testid="shipping-authoring-rate-conditions">
        <summary className="cursor-pointer text-[11px] font-bold text-gray-700">
          {messages.conditionsToggle}
          <span className="ms-2 font-normal text-gray-500">
            {messages.conditionSummary}: {rateDraftConditionText(row, locale, messages)}
          </span>
        </summary>
        <div className="mt-2 grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
          <div className="grid grid-cols-2 gap-1">
            <BoundField
              label={`${messages.weightLabel} ${messages.minLabel}`}
              value={row.minWeightGrams}
              inputMode="numeric"
              onChange={(value) => onChange("minWeightGrams", value)}
              testId="shipping-authoring-rate-min-weight"
            />
            <BoundField
              label={`${messages.weightLabel} ${messages.maxLabel}`}
              value={row.maxWeightGrams}
              inputMode="numeric"
              onChange={(value) => onChange("maxWeightGrams", value)}
              testId="shipping-authoring-rate-max-weight"
            />
          </div>
          <div className="grid grid-cols-2 gap-1">
            <BoundField
              label={`${messages.countLabel} ${messages.minLabel}`}
              value={row.minItemCount}
              inputMode="numeric"
              onChange={(value) => onChange("minItemCount", value)}
              testId="shipping-authoring-rate-min-count"
            />
            <BoundField
              label={`${messages.countLabel} ${messages.maxLabel}`}
              value={row.maxItemCount}
              inputMode="numeric"
              onChange={(value) => onChange("maxItemCount", value)}
              testId="shipping-authoring-rate-max-count"
            />
          </div>
          <div className="grid grid-cols-2 gap-1">
            <MoneyField
              label={`${messages.amountLabel} ${messages.minLabel}`}
              value={row.minMerchandiseAmount}
              onChange={(value) => onChange("minMerchandiseAmount", value)}
              testId="shipping-authoring-rate-min-amount"
            />
            <MoneyField
              label={`${messages.amountLabel} ${messages.maxLabel}`}
              value={row.maxMerchandiseAmount}
              onChange={(value) => onChange("maxMerchandiseAmount", value)}
              testId="shipping-authoring-rate-max-amount"
            />
          </div>
        </div>
      </details>
      {issue ? (
        <p role="alert" className="text-[11px] font-semibold text-danger" data-testid="shipping-authoring-rate-row-issue">
          {rowIssueMessage(issue.code, messages)}
        </p>
      ) : null}
      {serverError ? (
        <p role="alert" className="text-[11px] font-semibold text-danger" data-testid="shipping-authoring-rate-row-server-error">
          {serverError}
        </p>
      ) : null}
    </li>
  );
}

/** Truthful read-only row: the same stored values as text, with no input and no mutation control. */
export function RateRowSummary({
  row,
  locale,
  messages,
}: {
  row: RateRowDraft;
  locale: AuthoringLocale;
  messages: AuthoringMessages;
}) {
  return (
    <li
      className="grid gap-1 rounded-lg bg-surface px-2.5 py-1.5 text-xs text-gray-700 sm:grid-cols-2"
      data-testid="shipping-authoring-rate-row"
    >
      <span data-testid="shipping-authoring-rate-row-condition">
        {messages.conditionSummary}: {rateDraftConditionText(row, locale, messages)}
      </span>
      <span>
        {messages.freightLabel}: <span dir="ltr">{row.freightAmount ? formatDecimalText(row.freightAmount, locale) : "—"}</span>
        {" · "}
        {messages.packagingLabel}: <span dir="ltr">{row.packagingAmount ? formatDecimalText(row.packagingAmount, locale) : "—"}</span>
      </span>
    </li>
  );
}

/** Honest localized text of the shared range (unicode dashes, no invented value). */
export function areaRangeSummary(range: AreaRangeView, locale: AuthoringLocale, messages: AuthoringMessages): string {
  if (range.kind === "empty") return messages.areaRangeUnset;
  if (range.kind === "conflict") return messages.areaRangeConflict;
  return authoringFormat(messages.deliveryRange, {
    min: range.min === "" ? "—" : formatNumber(Number(range.min), locale),
    max: range.max === "" ? "—" : formatNumber(Number(range.max), locale),
  });
}

/**
 * The ONE delivery range of a whole delivery area, placed OUTSIDE the repeated price rows.
 *
 * CONTROLLED by the screen's shared-range drafts: the operator's text (including blank/partial/invalid)
 * is the form state, so it drives dirty/Apply/issues even when the rows still carry an older lawful pair.
 * When the stored rows disagree, the control starts empty and states the conflict, so the stored per-row
 * values are only normalized by a deliberate lawful entry.
 */
export function AreaDeliveryRange({
  range,
  value,
  issue,
  editable,
  locale,
  messages,
  onChange,
}: {
  range: AreaRangeView;
  /** The operator's own typed pair of THIS area. */
  value: AreaRangePair;
  /** Blocking reason computed by the composed intent for this area, if any. */
  issue: AreaRangeDraftIssueCode | null;
  editable: boolean;
  locale: AuthoringLocale;
  messages: AuthoringMessages;
  onChange: (min: string, max: string) => void;
}) {
  const { min, max } = value;
  if (!editable) {
    return (
      <p className="text-[11px] font-bold text-gray-800" data-testid="shipping-authoring-area-range">
        {messages.deliveryLabel}: <span data-testid="shipping-authoring-area-range-value">{areaRangeSummary(range, locale, messages)}</span>
      </p>
    );
  }
  /** A stored row that carries no duration yet blocks Apply; say it ONCE here instead of on a price row. */
  const missing = range.kind === "uniform" && (range.min === "" || range.max === "");
  const blocking = issue ?? (missing ? "durationRequired" : null);
  return (
    <div className="rounded-lg border border-border bg-surface px-2 py-1.5" data-testid="shipping-authoring-area-range">
      <p className="text-[11px] font-bold text-gray-800">{messages.areaRangeTitle}</p>
      <p className="text-[11px] text-gray-500">{messages.areaRangeHint}</p>
      <div className="mt-1 grid max-w-xs grid-cols-2 gap-2">
        <BoundField
          label={messages.minLabel}
          value={min}
          inputMode="numeric"
          onChange={(value2) => onChange(value2, max)}
          testId="shipping-authoring-area-range-min"
        />
        <BoundField
          label={messages.maxLabel}
          value={max}
          inputMode="numeric"
          onChange={(value2) => onChange(min, value2)}
          testId="shipping-authoring-area-range-max"
        />
      </div>
      {range.kind === "conflict" ? (
        <p role="alert" className="mt-1 text-[11px] font-semibold text-amber-800" data-testid="shipping-authoring-area-range-conflict">
          {authoringFormat(messages.areaRangeConflict, {
            pairs: range.pairs
              .map((pair) =>
                authoringFormat(messages.deliveryRange, {
                  min: pair.min === "" ? "—" : pair.min,
                  max: pair.max === "" ? "—" : pair.max,
                }),
              )
              .join(" · "),
          })}
        </p>
      ) : null}
      {blocking !== null ? (
        <p role="alert" className="mt-1 text-[11px] font-semibold text-danger" data-testid="shipping-authoring-area-range-issue">
          {areaRangeIssueMessage(blocking, messages)}
        </p>
      ) : null}
      {range.kind === "empty" ? (
        <p className="mt-1 text-[11px] text-gray-500" data-testid="shipping-authoring-area-range-empty">
          {messages.areaRangeEmpty}
        </p>
      ) : null}
    </div>
  );
}

/** The whole read-only group shell with its editable rows. */
export function AuthoringRatesEditor({
  group,
  position,
  rows,
  issues,
  index,
  locale,
  messages,
  contentLanguageId,
  editable,
  onAddRow,
  onRemoveRow,
  onChangeField,
}: {
  group: EditorRegionGroup;
  position: number;
  rows: readonly RateRowDraft[];
  issues: readonly RateRowIssue[];
  index: DestinationFactIndex;
  locale: AuthoringLocale;
  messages: AuthoringMessages;
  contentLanguageId: string;
  editable: boolean;
  onAddRow: () => void;
  onRemoveRow: (rowKey: string) => void;
  onChangeField: (rowKey: string, field: RateField, value: string) => void;
}) {
  const title = group.titles.find((entry) => entry.languageId.toLowerCase() === contentLanguageId.toLowerCase());
  const titleText =
    title && title.title.trim().length > 0
      ? title.title
      : authoringFormat(messages.groupTitle, { index: formatNumber(position + 1, locale) });

  return (
    <section
      key={group.groupId}
      className="rounded-xl border border-border bg-surface-elevated p-3"
      data-testid={`shipping-authoring-group-${group.order}`}
    >
      <header className="flex flex-wrap items-center justify-between gap-2">
        <h3 className="text-sm font-black text-gray-900">{titleText}</h3>
        <span className="text-[11px] text-gray-500">
          {authoringFormat(messages.regionsGroupOrder, { order: formatNumber(group.order, locale) })}
          {" · "}
          {messages.currencyLabel}: <span dir="ltr">{group.currencyCode ?? "—"}</span>
        </span>
      </header>

      <div className="mt-2">
        <p className="text-[11px] font-bold text-gray-800">{messages.areasLabel}</p>
        {group.areas.length === 0 ? (
          <p className="text-[11px] text-gray-500">{messages.noAreas}</p>
        ) : (
          <ul className="mt-1 flex flex-wrap gap-1">
            {group.areas.map((area) => (
              <li
                key={`${area.kind}:${area.id}`}
                className="rounded-full bg-surface px-2 py-0.5 text-[11px] text-gray-700"
                data-testid={`shipping-authoring-area-${area.kind}`}
              >
                <span className="text-gray-500">{messages[AREA_KIND_LABEL[area.kind]]}: </span>
                {index.labelOf(area) ?? <span className="font-semibold text-amber-700">{messages.unavailableName}</span>}
              </li>
            ))}
          </ul>
        )}
      </div>

      <div className="mt-2">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <p className="text-[11px] font-bold text-gray-800">{messages.ratesTitle}</p>
          {editable ? (
            <button
              type="button"
              className="inline-flex min-h-8 items-center rounded-lg border border-border bg-surface px-2 text-[11px] font-bold text-gray-800"
              data-testid="shipping-authoring-rate-add"
              onClick={onAddRow}
            >
              {messages.addRowAction}
            </button>
          ) : null}
        </div>
        {rows.length === 0 ? (
          <p className="mt-1 text-[11px] text-gray-500">{messages.noRows}</p>
        ) : (
          <ul className="mt-1 grid gap-2">
            {rows.map((row, rowIndex) =>
              editable ? (
                <RateRowEditor
                  key={row.rowKey}
                  row={row}
                  index={rowIndex}
                  locale={locale}
                  messages={messages}
                  issue={issues.find((issue) => issue.rowKey === row.rowKey)}
                  onChange={(field, value) => onChangeField(row.rowKey, field, value)}
                  onRemove={() => onRemoveRow(row.rowKey)}
                />
              ) : (
                <li
                  key={row.rowKey}
                  className="grid gap-1 rounded-lg bg-surface px-2 py-1 text-[11px] text-gray-700 md:grid-cols-2"
                  data-testid="shipping-authoring-rate-row"
                >
                  <span data-testid="shipping-authoring-rate-row-condition">
                    {rateDraftConditionText(row, locale, messages)}
                  </span>
                  <span>
                    {messages.freightLabel}: <span dir="ltr">{row.freightAmount ? formatDecimalText(row.freightAmount, locale) : "—"}</span>
                    {" · "}
                    {messages.packagingLabel}: <span dir="ltr">{row.packagingAmount ? formatDecimalText(row.packagingAmount, locale) : "—"}</span>
                  </span>
                </li>
              ),
            )}
          </ul>
        )}
      </div>
    </section>
  );
}
