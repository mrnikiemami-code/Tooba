"use client";

/**
 * Editable rows of ONE existing region group: identity, order, currency and geography are shown
 * read-only exactly once, while the operator can add, edit and remove the rate rows beneath it.
 *
 * TB-224: the shipping duration is ONE customer-receiving range of the WHOLE delivery area, placed above
 * the rows; the rows carry only freight/packaging and their optional weight/count/amount conditions. The
 * persisted wire still repeats the days per row, so the area control writes the chosen pair to exactly the
 * rows of this area (see `model/editor-area-range.ts`).
 *
 * TB-225: the rows are READ as compact summaries and are only written inside ONE isolated modal
 * (`authoring-rate-modal.tsx`). Nothing appears in the area until the modal is confirmed, and the parent
 * Apply stays the only persistence call.
 *
 * Pure presentation: no fetch, no session, no pricing/overlap policy.
 */
import { useRef, useState } from "react";
import type { AuthoringLocale, AuthoringMessages } from "../messages/authoring-messages.ts";
import { authoringFormat } from "../messages/authoring-messages.ts";
import type { EditorRegionGroup } from "../model/editor-document.ts";
import type { DestinationFactIndex } from "../model/editor-destination-ancestry.ts";
import {
  RATE_LIMITS,
  newRateRow,
  storedRateAmounts,
  type RateRowDraft,
  type RateRowIssue,
  type StoredRateAmounts,
} from "../model/editor-rates.ts";
import { type AreaRangePair, type AreaRangeView } from "../model/editor-area-range.ts";
import { displayMoneyText, moneyPrecisionPolicy } from "../model/money-text.ts";
import { rateDraftConditionText } from "./rate-presentation.ts";
import { areaRangeIssueMessage, rowIssueMessage } from "./rate-issue-messages.ts";
import { BoundField } from "./authoring-money-field.tsx";
import { formatNumber } from "./authoring-format.ts";
import { AuthoringRateModal } from "./authoring-rate-modal.tsx";

const AREA_KIND_LABEL = {
  Country: "areaCountry",
  AdministrativeArea: "areaAdministrativeArea",
  City: "areaCity",
  ServiceArea: "areaServiceArea",
} as const;

/** Truthful read-only row: the same stored values as text, with no input and no mutation control. */
export function RateRowSummary({
  row,
  locale,
  messages,
  currencyCode,
}: {
  row: RateRowDraft;
  locale: AuthoringLocale;
  messages: AuthoringMessages;
  currencyCode: string | null;
}) {
  const policy = moneyPrecisionPolicy(currencyCode);
  return (
    <li
      className="grid gap-1 rounded-lg bg-surface px-2.5 py-1.5 text-xs text-gray-700 sm:grid-cols-2"
      data-testid="shipping-authoring-rate-row"
    >
      <span data-testid="shipping-authoring-rate-row-condition">
        {messages.conditionSummary}: {rateDraftConditionText(row, locale, messages)}
      </span>
      <span>
        {messages.freightLabel}:{" "}
        <span dir="ltr">{row.freightAmount ? displayMoneyText(row.freightAmount, policy) : "—"}</span>
        {" · "}
        {messages.packagingLabel}:{" "}
        <span dir="ltr">{row.packagingAmount ? displayMoneyText(row.packagingAmount, policy) : "—"}</span>
        {" · "}
        {messages.deliveryLabel}:{" "}
        <span dir="ltr">
          {row.minDeliveryDays === "" || row.maxDeliveryDays === ""
            ? "—"
            : `${formatNumber(Number(row.minDeliveryDays), locale)}–${formatNumber(Number(row.maxDeliveryDays), locale)}`}
        </span>
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
  issue: import("../model/editor-area-range.ts").AreaRangeDraftIssueCode | null;
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

/** The whole read-only group shell with its compact editable rows (used by the single-group screen). */
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
  currencyCode,
  rowServerErrors,
  onCommitRow,
  onRemoveRow,
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
  /** Server-owned currency of THIS area (drives the integer-rial entry policy); `null` when absent. */
  currencyCode: string | null;
  /** Server rejections keyed by the stable row key of this area. */
  rowServerErrors?: ReadonlyMap<string, string>;
  onCommitRow: (row: RateRowDraft) => void;
  onRemoveRow: (rowKey: string) => void;
}) {
  const [modal, setModal] = useState<
    {
      readonly title: string;
      readonly initial: RateRowDraft;
      readonly storedAmounts: StoredRateAmounts | null;
    } | null
  >(null);
  const newRowCounter = useRef(0);
  const title = group.titles.find((entry) => entry.languageId.toLowerCase() === contentLanguageId.toLowerCase());
  const titleText =
    title && title.title.trim().length > 0
      ? title.title
      : authoringFormat(messages.groupTitle, { index: formatNumber(position + 1, locale) });
  const policy = moneyPrecisionPolicy(currencyCode);

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
              className="inline-flex min-h-8 items-center rounded-lg border border-border bg-surface px-2 text-[11px] font-bold text-gray-800 disabled:opacity-50"
              data-testid="shipping-authoring-rate-add"
              disabled={rows.length >= RATE_LIMITS.maxRowsPerGroup}
              onClick={() => {
                newRowCounter.current += 1;
                setModal({
                  title: messages.rateAddTitle,
                  initial: newRateRow(`new-${group.groupId}-${newRowCounter.current}`),
                  storedAmounts: null,
                });
              }}
            >
              {messages.addRowAction}
            </button>
          ) : null}
        </div>
        {rows.length === 0 ? (
          <p className="mt-1 text-[11px] text-gray-500">{messages.noRows}</p>
        ) : (
          <ul className="mt-1 grid gap-2">
            {rows.map((row) => {
              const issue = issues.find(
                (candidate) =>
                  candidate.rowKey === row.rowKey &&
                  candidate.code !== "durationRequired" &&
                  candidate.code !== "durationInvalid",
              );
              const serverError = rowServerErrors?.get(row.rowKey);
              return (
                <li
                  key={row.rowKey}
                  className="grid gap-1 rounded-lg border border-border bg-surface px-2.5 py-1.5 text-xs text-gray-700"
                  data-testid="shipping-authoring-rate-row"
                >
                  <div className="flex flex-wrap items-start justify-between gap-2">
                    <span className="grid gap-0.5" data-testid="shipping-authoring-rate-row-condition">
                      <span>
                        {messages.conditionSummary}: {rateDraftConditionText(row, locale, messages)}
                      </span>
                      <span>
                        {messages.freightLabel}:{" "}
                        <span dir="ltr">{row.freightAmount ? displayMoneyText(row.freightAmount, policy) : "—"}</span>
                        {" · "}
                        {messages.packagingLabel}:{" "}
                        <span dir="ltr">{row.packagingAmount ? displayMoneyText(row.packagingAmount, policy) : "—"}</span>
                        {" · "}
                        {messages.deliveryLabel}:{" "}
                        <span dir="ltr">
                          {row.minDeliveryDays === "" || row.maxDeliveryDays === ""
                            ? "—"
                            : `${formatNumber(Number(row.minDeliveryDays), locale)}–${formatNumber(Number(row.maxDeliveryDays), locale)}`}
                        </span>
                      </span>
                    </span>
                    {editable ? (
                      <span className="flex items-center gap-1">
                        <button
                          type="button"
                          className="inline-flex min-h-7 items-center rounded-lg border border-border px-2 text-[11px] font-bold text-gray-700"
                          data-testid="shipping-authoring-rate-row-edit"
                          onClick={() =>
                            setModal({
                              title: messages.rateEditTitle,
                              initial: row,
                              storedAmounts: storedRateAmounts(
                                group.rows.find((candidate) => candidate.rateRowId === row.rateRowId) ?? null,
                              ),
                            })
                          }
                        >
                          {messages.rateEditAction}
                        </button>
                        <button
                          type="button"
                          className="inline-flex min-h-7 items-center rounded-lg border border-border px-2 text-[11px] font-bold text-danger"
                          data-testid="shipping-authoring-rate-row-remove"
                          onClick={() => onRemoveRow(row.rowKey)}
                        >
                          {messages.removeRowAction}
                        </button>
                      </span>
                    ) : null}
                  </div>
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
            })}
          </ul>
        )}
      </div>

      <AuthoringRateModal
        open={modal !== null}
        title={modal?.title ?? ""}
        initial={modal?.initial ?? null}
        currencyCode={currencyCode}
        otherRows={rows}
        storedAmounts={modal?.storedAmounts ?? null}
        editable={editable}
        messages={messages}
        onConfirm={(row) => {
          setModal(null);
          onCommitRow(row);
        }}
        onClose={() => setModal(null)}
      />
    </section>
  );
}
