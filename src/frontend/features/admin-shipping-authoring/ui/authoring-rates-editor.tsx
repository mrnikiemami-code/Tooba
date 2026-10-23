"use client";

/**
 * Editable rows of ONE existing region group: identity, order, currency and geography are shown
 * read-only exactly once, while the operator can add, remove and edit the rate rows beneath it.
 *
 * Pure presentation: no fetch, no session, no pricing/overlap policy. Money and integer fields are
 * TEXT inputs so `0.50` / `1850000.00` are never transformed by `input[type=number]` or a float
 * round-trip; the local issues come from the model.
 */
import type { AuthoringLocale, AuthoringMessages } from "../messages/authoring-messages.ts";
import { authoringFormat } from "../messages/authoring-messages.ts";
import type { EditorRegionGroup } from "../model/editor-document.ts";import type { DestinationFactIndex } from "../model/editor-destination-ancestry.ts";
import { RATE_LIMITS, type RateField, type RateRowDraft, type RateRowIssue } from "../model/editor-rates.ts";
import { rateDraftConditionText } from "./rate-presentation.ts";
import { formatNumber } from "./authoring-format.ts";

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
      <span className="text-[10px] font-bold text-gray-600">{label}</span>
      <input
        type="text"
        inputMode={inputMode}
        dir="ltr"
        className="min-h-8 w-full rounded-lg border border-border bg-surface px-2 text-xs text-gray-900"
        value={value}
        placeholder={placeholder}
        onChange={(event) => onChange(event.target.value)}
        data-testid={testId}
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
  onChange,
  onRemove,
}: {
  row: RateRowDraft;
  index: number;
  locale: AuthoringLocale;
  messages: AuthoringMessages;
  issue: RateRowIssue | undefined;
  onChange: (field: RateField, value: string) => void;
  onRemove: () => void;
}) {
  return (
    <li className="grid gap-2 rounded-lg border border-border bg-surface p-2" data-testid="shipping-authoring-rate-row-edit">
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
      <div className="grid gap-2 md:grid-cols-4">
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
          <BoundField
            label={`${messages.amountLabel} ${messages.minLabel}`}
            value={row.minMerchandiseAmount}
            inputMode="decimal"
            onChange={(value) => onChange("minMerchandiseAmount", value)}
            testId="shipping-authoring-rate-min-amount"
          />
          <BoundField
            label={`${messages.amountLabel} ${messages.maxLabel}`}
            value={row.maxMerchandiseAmount}
            inputMode="decimal"
            onChange={(value) => onChange("maxMerchandiseAmount", value)}
            testId="shipping-authoring-rate-max-amount"
          />
        </div>
        <div className="grid grid-cols-2 gap-1">
          <BoundField
            label={messages.freightLabel}
            value={row.freightAmount}
            inputMode="decimal"
            onChange={(value) => onChange("freightAmount", value)}
            testId="shipping-authoring-rate-freight"
          />
          <BoundField
            label={messages.packagingLabel}
            value={row.packagingAmount}
            inputMode="decimal"
            onChange={(value) => onChange("packagingAmount", value)}
            testId="shipping-authoring-rate-packaging"
          />
        </div>
        <div className="grid grid-cols-2 gap-1">
          <BoundField
            label={`${messages.deliveryLabel} ${messages.minLabel}`}
            value={row.minDeliveryDays}
            inputMode="numeric"
            onChange={(value) => onChange("minDeliveryDays", value)}
            testId="shipping-authoring-rate-min-days"
          />
          <BoundField
            label={`${messages.deliveryLabel} ${messages.maxLabel}`}
            value={row.maxDeliveryDays}
            inputMode="numeric"
            onChange={(value) => onChange("maxDeliveryDays", value)}
            testId="shipping-authoring-rate-max-days"
          />
        </div>
      </div>
      {issue ? (
        <p role="alert" className="text-[11px] font-semibold text-danger" data-testid="shipping-authoring-rate-row-issue">
          {rowIssueMessage(issue.code, messages)}
        </p>
      ) : null}
    </li>
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
                  className="grid gap-1 rounded-lg bg-surface px-2 py-1 text-[11px] text-gray-700 md:grid-cols-4"
                  data-testid="shipping-authoring-rate-row"
                >
                  <span className="md:col-span-2" data-testid="shipping-authoring-rate-row-condition">
                    {rateDraftConditionText(row, locale, messages)}
                  </span>
                  <span>
                    {messages.freightLabel}: <span dir="ltr">{row.freightAmount || "—"}</span>
                  </span>
                  <span>
                    {messages.packagingLabel}: <span dir="ltr">{row.packagingAmount || "—"}</span>
                    {" · "}
                    {authoringFormat(messages.deliveryRange, {
                      min: row.minDeliveryDays || "—",
                      max: row.maxDeliveryDays || "—",
                    })}
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
