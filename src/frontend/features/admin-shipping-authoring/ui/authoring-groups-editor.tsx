"use client";

/**
 * Ordered GROUP-COLLECTION editor of the existing draft: add an empty group, delete one deliberately
 * (with confirmation), move up/down, edit the title of the ACTIVE selected content language, keep or
 * remove real destination chips, and edit the nested rate rows.
 *
 * Currency and every identity stay server-owned and read-only. New groups carry no currency and no
 * server id. Pure presentation: fetch/policy live in the picker, the models and the session.
 */
import { useRef, useState } from "react";
import type { AuthoringLocale, AuthoringMessages } from "../messages/authoring-messages.ts";
import { authoringFormat } from "../messages/authoring-messages.ts";
import type { EditorDocument, SelectedAreaRead } from "../model/editor-document.ts";
import type { DestinationFactIndex } from "../model/editor-destination-ancestry.ts";
import type { ResolvedAreaAncestry } from "../model/editor-edits.ts";
import { groupIssues, type GroupDraft, type GroupIssue } from "../model/editor-groups.ts";
import {
  RATE_LIMITS,
  newRateRow,
  toleratedStoredFractions,
  type RateField,
  type RateRowDraft,
  type RateRowIssue,
  type RatesFormState,
} from "../model/editor-rates.ts";
import type { UnresolvedDraftSelection } from "../model/destination-choice.ts";
import { areaIdentity } from "../model/destination-choice.ts";
import {
  areaRangeDraftOf,
  areaRangeOf,
  type AreaRangeDraftIssue,
  type AreaRangeDrafts,
} from "../model/editor-area-range.ts";
import { AuthoringDestinationModal } from "./authoring-destination-modal.tsx";
import { AreaDeliveryRange, RateRowSummary } from "./authoring-rates-editor.tsx";
import { AuthoringRateModal } from "./authoring-rate-modal.tsx";
import { displayMoneyText, moneyPrecisionPolicy } from "../model/money-text.ts";
import { rateDraftConditionText } from "./rate-presentation.ts";
import { formatNumber } from "./authoring-format.ts";
import { rateRowKey } from "./server-field-error.ts";
import { rowIssueMessage } from "./rate-issue-messages.ts";

function groupIssueMessage(code: GroupIssue["code"], messages: AuthoringMessages): string {
  switch (code) {
    case "groupLimitExceeded":
      return messages.groupLimitExceeded;
    case "selectionLimitExceeded":
      return messages.selectionLimitExceeded;
    case "duplicateSelection":
      return messages.duplicateSelection;
    case "titleTooLong":
      return messages.titleTooLong;
  }
}

export function AuthoringGroupsEditor({
  document,
  index,
  locale,
  messages,
  contentLanguageId,
  activeLanguageIds,
  groups,
  rates,
  rateIssues,
  unresolved,
  editable,
  serverRateErrors,
  areaRanges,
  areaRangeIssues,
  onAddGroup,
  onRemoveGroup,
  onMoveGroup,
  onChangeTitle,
  onRemoveArea,
  onAddArea,
  onCommitRow,
  onRemoveRow,
  onChangeAreaRange,
}: {
  document: EditorDocument;
  index: DestinationFactIndex;
  locale: AuthoringLocale;
  messages: AuthoringMessages;
  contentLanguageId: string;
  activeLanguageIds: readonly string[];
  groups: readonly GroupDraft[];
  rates: RatesFormState;
  rateIssues: readonly RateRowIssue[];
  unresolved: readonly UnresolvedDraftSelection[];
  editable: boolean;
  /** Server rejections keyed by composite group+row ownership (`rateRowKey`), never row alone. */
  serverRateErrors?: ReadonlyMap<string, string>;
  /** The operator's typed shared delivery range per area (the ONE form state of that control). */
  areaRanges?: AreaRangeDrafts;
  /** Blocking reasons of those typed ranges, from the composed intent. */
  areaRangeIssues?: readonly AreaRangeDraftIssue[];
  onAddGroup: () => void;
  onRemoveGroup: (groupKey: string) => void;
  onMoveGroup: (groupKey: string, delta: -1 | 1) => void;
  onChangeTitle: (groupKey: string, languageId: string, value: string) => void;
  onRemoveArea: (groupKey: string, area: SelectedAreaRead) => void;
  onAddArea: (
    groupKey: string,
    area: SelectedAreaRead,
    ancestry: ResolvedAreaAncestry,
    label: string,
    segments: readonly string[],
  ) => void;
  /** ONE confirmed row of an area: a new row when its key is new, or exactly that stored row replaced. */
  onCommitRow: (groupKey: string, row: RateRowDraft) => void;
  onRemoveRow: (groupKey: string, rowKey: string) => void;
  /** Deliberate shared-range entry of one area: applied to exactly that area's rows. */
  onChangeAreaRange?: (groupKey: string, min: string, max: string) => void;
}) {
  const [pendingRemoval, setPendingRemoval] = useState<string | null>(null);
  /** The area (group) whose destination modal is open; the modal is not a per-row inline form. */
  const [destinationGroup, setDestinationGroup] = useState<string | null>(null);
  /** The ONE open rate modal: an isolated new row, or a COPY of exactly one stored row. */
  const [rateModal, setRateModal] = useState<
    {
      readonly groupKey: string;
      readonly title: string;
      readonly initial: RateRowDraft;
      readonly tolerated: readonly RateField[];
    } | null
  >(null);
  const newRowCounter = useRef(0);
  const issues = groupIssues(groups);

  return (
    <div className="grid gap-3" data-testid="shipping-authoring-groups-panel">
      <div className="grid gap-1 rounded-xl border border-border bg-surface px-3 py-2 text-xs text-gray-600">
        <p className="font-bold text-gray-800">{messages.regionsIntro}</p>
      </div>

      {editable ? (
        <button
          type="button"
          className="inline-flex min-h-8 w-fit items-center rounded-lg border border-border bg-surface px-2 text-[11px] font-bold text-gray-800"
          data-testid="shipping-authoring-group-add"
          onClick={onAddGroup}
        >
          {messages.addGroupAction}
        </button>
      ) : null}

      {groups.length === 0 ? <p className="text-xs text-gray-500">{messages.noGroups}</p> : null}

      {groups.map((group, position) => {
        const groupRateIssues = rateIssues.filter((issue) => issue.groupId === group.key);
        const groupGroupIssues = issues.filter((issue) => issue.groupKey === group.key);
        const groupUnresolved = unresolved.filter((entry) => entry.groupKey === group.key);
        const stored = group.groupId === null ? null : document.regionGroups.find((entry) => entry.groupId === group.groupId) ?? null;
        const groupRows = rates[group.key] ?? [];
        /** ONE delivery range of the WHOLE area, derived from the rows the wire still repeats it on. */
        const range = areaRangeOf(groupRows);
        const rangeIssue = areaRangeIssues?.find((issue) => issue.groupKey === group.key) ?? null;
        const titleEntry = group.titles.find((entry) => entry.languageId.toLowerCase() === contentLanguageId.toLowerCase());
        const storedTitle = stored?.titles.find((entry) => entry.languageId.toLowerCase() === contentLanguageId.toLowerCase());
        const inactiveTitles = (stored?.titles ?? []).filter(
          (entry) => !activeLanguageIds.some((id) => id.toLowerCase() === entry.languageId.toLowerCase()),
        );

        return (
          <section
            key={group.key}
            className="rounded-xl border border-border bg-surface-elevated p-3"
            /* Dense areas stay distinguishable: the WHOLE card alternates by current display position,
               using the existing theme-aware alternate-surface token. Children keep their own surfaces. */
            style={
              position % 2 === 1
                ? { backgroundColor: "rgb(var(--color-section-alternate))" }
                : undefined
            }
            data-testid="shipping-authoring-group-draft"
            data-group-index={position}
          >
            <header className="flex flex-wrap items-center justify-between gap-2">
              <h3 className="text-sm font-black text-gray-900">
                {group.groupId === null ? messages.newGroupTitle : authoringFormat(messages.groupTitle, { index: formatNumber(position + 1, locale) })}
              </h3>
              <div className="flex flex-wrap items-center gap-1">
                <span className="text-[11px] text-gray-500">
                  {messages.currencyLabel}: <span dir="ltr">{group.currencyCode ?? messages.groupNoCurrency}</span>
                </span>
                {editable ? (
                  <>
                    <button
                      type="button"
                      className="inline-flex min-h-7 items-center rounded-lg border border-border px-2 text-[11px] font-bold text-gray-700"
                      data-testid="shipping-authoring-group-up"
                      onClick={() => onMoveGroup(group.key, -1)}
                    >
                      {messages.moveGroupUpAction}
                    </button>
                    <button
                      type="button"
                      className="inline-flex min-h-7 items-center rounded-lg border border-border px-2 text-[11px] font-bold text-gray-700"
                      data-testid="shipping-authoring-group-down"
                      onClick={() => onMoveGroup(group.key, 1)}
                    >
                      {messages.moveGroupDownAction}
                    </button>
                    <button
                      type="button"
                      className="inline-flex min-h-7 items-center rounded-lg border border-border px-2 text-[11px] font-bold text-danger"
                      data-testid="shipping-authoring-group-remove"
                      onClick={() => setPendingRemoval(group.key)}
                    >
                      {messages.removeGroupAction}
                    </button>
                  </>
                ) : null}
              </div>
            </header>

            {pendingRemoval === group.key ? (
              <div className="mt-2 grid gap-2 rounded-lg border border-danger/40 bg-surface p-2" role="alertdialog">
                <p className="text-[11px] font-semibold text-danger">{messages.groupRemoveConfirm}</p>
                <div className="flex gap-1">
                  <button
                    type="button"
                    className="inline-flex min-h-7 items-center rounded-lg border border-danger px-2 text-[11px] font-bold text-danger"
                    data-testid="shipping-authoring-group-remove-confirm"
                    onClick={() => {
                      setPendingRemoval(null);
                      onRemoveGroup(group.key);
                    }}
                  >
                    {messages.groupRemoveConfirmAction}
                  </button>
                  <button
                    type="button"
                    className="inline-flex min-h-7 items-center rounded-lg border border-border px-2 text-[11px] font-bold text-gray-700"
                    data-testid="shipping-authoring-group-remove-cancel"
                    onClick={() => setPendingRemoval(null)}
                  >
                    {messages.groupRemoveCancelAction}
                  </button>
                </div>
              </div>
            ) : null}

            {editable ? (
              <label className="mt-2 grid gap-0.5">
                <span className="text-xs font-bold text-gray-700">{messages.groupTitleLabel}</span>
                <input
                  type="text"
                  className="min-h-9 w-full rounded-lg border border-border bg-surface px-2 text-sm text-gray-900"
                  value={titleEntry?.title ?? ""}
                  placeholder={storedTitle?.title ?? ""}
                  data-testid="shipping-authoring-group-title"
                  onChange={(event) => onChangeTitle(group.key, contentLanguageId, event.target.value)}
                />
              </label>
            ) : (
              <p className="mt-2 text-sm font-bold text-gray-900" data-testid="shipping-authoring-group-title-text">
                {(storedTitle?.title ?? titleEntry?.title ?? "").trim() || messages.noAreaTitle}
              </p>
            )}
            {inactiveTitles.length > 0 ? (
              <p className="mt-1 text-xs text-gray-500">
                {inactiveTitles.map((entry) => (
                  <span key={entry.languageId} dir="auto" className="me-2">
                    {entry.title}
                  </span>
                ))}
              </p>
            ) : null}

            <div className="mt-2">
              <p className="text-[11px] font-bold text-gray-800">{messages.areasLabel}</p>
              {group.areas.length === 0 ? (
                <p className="text-[11px] text-gray-500">{messages.noAreas}</p>
              ) : (
                <ul className="mt-1 flex flex-wrap gap-1">
                  {group.areas.map((area) => {
                    const segments = index.segmentsOf(area);
                    const label = segments.length > 0 ? segments.join(" › ") : index.labelOf(area);
                    const isUnresolved = groupUnresolved.some((entry) => areaIdentity(entry.area) === areaIdentity(area));
                    return (
                      <li
                        key={areaIdentity(area)}
                        className="inline-flex items-center gap-1 rounded-full bg-surface px-2 py-0.5 text-[11px] text-gray-700"
                        data-testid={`shipping-authoring-area-${area.kind}`}
                      >
                        <span className="text-gray-500">{messages[AREA_KIND_LABEL_OF[area.kind]]}: </span>
                        {/* The verified BREADCRUMB of the pick, never a bare leaf or a raw GUID. */}
                        {label !== null && label.trim().length > 0 ? (
                          <span dir="auto" data-testid="shipping-authoring-area-breadcrumb">
                            {label}
                          </span>
                        ) : (
                          <span className="font-semibold text-amber-700" title={messages.destinationNameUnavailable}>
                            {messages.unavailableName}
                          </span>
                        )}
                        {editable && !isUnresolved ? (
                          <button
                            type="button"
                            className="text-[11px] font-bold text-danger"
                            aria-label={messages.removeAreaAction}
                            data-testid="shipping-authoring-area-remove"
                            onClick={() => onRemoveArea(group.key, area)}
                          >
                            ×
                          </button>
                        ) : null}
                        {editable && isUnresolved ? (
                          <button
                            type="button"
                            className="text-[11px] font-bold text-danger underline"
                            data-testid="shipping-authoring-area-remove-unresolved"
                            onClick={() => onRemoveArea(group.key, area)}
                          >
                            {messages.removeAreaAction}
                          </button>
                        ) : null}
                      </li>
                    );
                  })}
                </ul>
              )}
              {groupUnresolved.length > 0 ? (
                <p role="alert" className="mt-1 text-[11px] font-semibold text-danger" data-testid="shipping-authoring-group-unresolved">
                  {messages.unresolvedDraftSelection}
                </p>
              ) : null}
            </div>

            {editable ? (
              <div className="mt-2">
                <button
                  type="button"
                  className="inline-flex min-h-8 items-center rounded-lg border border-border bg-surface px-2 text-[11px] font-bold text-gray-800"
                  data-testid="shipping-authoring-area-add"
                  onClick={() => setDestinationGroup(group.key)}
                >
                  {messages.destinationOpenAction}
                </button>
              </div>
            ) : null}

            <div className="mt-2">
              <AreaDeliveryRange
                range={range}
                value={areaRangeDraftOf(areaRanges ?? {}, group.key, groupRows)}
                issue={rangeIssue?.code ?? null}
                editable={editable}
                locale={locale}
                messages={messages}
                onChange={(min, max) => onChangeAreaRange?.(group.key, min, max)}
              />
            </div>

            <div className="mt-2">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <p className="text-[11px] font-bold text-gray-800">{messages.ratesTitle}</p>
                {editable ? (
                  <button
                    type="button"
                    className="inline-flex min-h-8 items-center rounded-lg border border-border bg-surface px-2 text-[11px] font-bold text-gray-800 disabled:opacity-50"
                    data-testid="shipping-authoring-rate-add"
                    disabled={groupRows.length >= RATE_LIMITS.maxRowsPerGroup}
                    onClick={() => {
                      newRowCounter.current += 1;
                      setRateModal({
                        groupKey: group.key,
                        title: messages.rateAddTitle,
                        initial: newRateRow(`new-${group.key}-${newRowCounter.current}`),
                        tolerated: [],
                      });
                    }}
                  >
                    {messages.addRowAction}
                  </button>
                ) : null}
              </div>
              {groupRows.length === 0 ? (
                <p className="mt-1 text-[11px] text-gray-500">{messages.noRows}</p>
              ) : (
                <ul className="mt-1 grid gap-2">
                  {groupRows.map((row: RateRowDraft, rowIndex: number) => {
                    const issue = groupRateIssues.find(
                      (candidate) =>
                        candidate.rowKey === row.rowKey &&
                        candidate.code !== "durationRequired" &&
                        candidate.code !== "durationInvalid",
                    );
                    const serverError = serverRateErrors?.get(rateRowKey(position, rowIndex));
                    if (!editable) {
                      return (
                        <RateRowSummary
                          key={row.rowKey}
                          row={row}
                          locale={locale}
                          messages={messages}
                          currencyCode={group.currencyCode}
                        />
                      );
                    }
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
                              <span dir="ltr">
                                {row.freightAmount ? displayMoneyText(row.freightAmount, moneyPrecisionPolicy(group.currencyCode)) : "—"}
                              </span>
                              {" · "}
                              {messages.packagingLabel}:{" "}
                              <span dir="ltr">
                                {row.packagingAmount ? displayMoneyText(row.packagingAmount, moneyPrecisionPolicy(group.currencyCode)) : "—"}
                              </span>
                              {" · "}
                              {messages.deliveryLabel}:{" "}
                              <span dir="ltr">
                                {row.minDeliveryDays === "" || row.maxDeliveryDays === ""
                                  ? "—"
                                  : `${formatNumber(Number(row.minDeliveryDays), locale)}–${formatNumber(Number(row.maxDeliveryDays), locale)}`}
                              </span>
                            </span>
                          </span>
                          <span className="flex items-center gap-1">
                            <button
                              type="button"
                              className="inline-flex min-h-7 items-center rounded-lg border border-border px-2 text-[11px] font-bold text-gray-700"
                              data-testid="shipping-authoring-rate-row-edit"
                              onClick={() =>
                                setRateModal({
                                  groupKey: group.key,
                                  title: messages.rateEditTitle,
                                  initial: row,
                                  tolerated: toleratedStoredFractions(
                                    row,
                                    stored?.rows.find((candidate) => candidate.rateRowId === row.rateRowId) ?? null,
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
                              onClick={() => onRemoveRow(group.key, row.rowKey)}
                            >
                              {messages.removeRowAction}
                            </button>
                          </span>
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

            {groupGroupIssues.length > 0 ? (
              <ul className="mt-1 grid gap-0.5">
                {groupGroupIssues.map((issue, issueIndex) => (
                  <li key={`${issue.code}-${issueIndex}`} role="alert" className="text-[11px] font-semibold text-danger">
                    {groupIssueMessage(issue.code, messages)}
                  </li>
                ))}
              </ul>
            ) : null}
          </section>
        );
      })}

      {destinationGroup !== null ? (
        <AuthoringDestinationModal
          open
          languageId={contentLanguageId}
          messages={messages}
          disabled={!editable}
          isSelected={(area) =>
            groups.some((candidate) => candidate.areas.some((entry) => areaIdentity(entry) === areaIdentity(area)))
          }
          onAdd={(area, ancestry, label, segments) =>
            onAddArea(destinationGroup, area, ancestry, label, segments.map((segment) => segment.label))
          }
          onClose={() => setDestinationGroup(null)}
        />
      ) : null}

      {/* ONE isolated row draft: a new row does not appear in the area, and an edit works on a copy. */}      {rateModal !== null ? (
        <AuthoringRateModal
          open
          title={rateModal.title}
          initial={rateModal.initial}
          currencyCode={groups.find((entry) => entry.key === rateModal.groupKey)?.currencyCode ?? null}
          otherRows={rates[rateModal.groupKey] ?? []}
          tolerated={rateModal.tolerated}
          editable={editable}
          messages={messages}
          onConfirm={(row) => {
            const groupKey = rateModal.groupKey;
            setRateModal(null);
            onCommitRow(groupKey, row);
          }}
          onClose={() => setRateModal(null)}
        />
      ) : null}
    </div>
  );
}

const AREA_KIND_LABEL_OF = {
  Country: "areaCountry",
  AdministrativeArea: "areaAdministrativeArea",
  City: "areaCity",
  ServiceArea: "areaServiceArea",
} as const;
