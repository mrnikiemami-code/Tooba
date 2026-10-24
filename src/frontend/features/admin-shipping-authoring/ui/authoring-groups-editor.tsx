"use client";

/**
 * Ordered GROUP-COLLECTION editor of the existing draft: add an empty group, delete one deliberately
 * (with confirmation), move up/down, edit the title of the ACTIVE selected content language, keep or
 * remove real destination chips, and edit the nested rate rows.
 *
 * Currency and every identity stay server-owned and read-only. New groups carry no currency and no
 * server id. Pure presentation: fetch/policy live in the picker, the models and the session.
 */
import { useState } from "react";
import type { AuthoringLocale, AuthoringMessages } from "../messages/authoring-messages.ts";
import { authoringFormat } from "../messages/authoring-messages.ts";
import type { EditorDocument, SelectedAreaRead } from "../model/editor-document.ts";
import type { DestinationFactIndex } from "../model/editor-destination-ancestry.ts";
import type { ResolvedAreaAncestry } from "../model/editor-edits.ts";
import { groupIssues, type GroupDraft, type GroupIssue } from "../model/editor-groups.ts";
import type { RateField, RateRowDraft, RateRowIssue, RatesFormState } from "../model/editor-rates.ts";
import type { UnresolvedDraftSelection } from "../model/destination-choice.ts";
import { areaIdentity } from "../model/destination-choice.ts";
import { AuthoringDestinationPicker } from "./authoring-destination-picker.tsx";
import { RateRowEditor } from "./authoring-rates-editor.tsx";
import { formatNumber } from "./authoring-format.ts";

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
  onAddGroup,
  onRemoveGroup,
  onMoveGroup,
  onChangeTitle,
  onRemoveArea,
  onAddArea,
  onAddRow,
  onRemoveRow,
  onChangeRowField,
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
  onAddGroup: () => void;
  onRemoveGroup: (groupKey: string) => void;
  onMoveGroup: (groupKey: string, delta: -1 | 1) => void;
  onChangeTitle: (groupKey: string, languageId: string, value: string) => void;
  onRemoveArea: (groupKey: string, area: SelectedAreaRead) => void;
  onAddArea: (groupKey: string, area: SelectedAreaRead, ancestry: ResolvedAreaAncestry, label: string) => void;
  onAddRow: (groupKey: string) => void;
  onRemoveRow: (groupKey: string, rowKey: string) => void;
  onChangeRowField: (groupKey: string, rowKey: string, field: RateField, value: string) => void;
}) {
  const [pendingRemoval, setPendingRemoval] = useState<string | null>(null);
  const issues = groupIssues(groups);

  return (
    <div className="grid gap-3" data-testid="shipping-authoring-groups-panel">
      <div className="grid gap-1 rounded-xl border border-border bg-surface px-3 py-2 text-xs text-gray-600">
        <p className="font-bold text-gray-800">{messages.regionsIntro}</p>
        <p>{editable ? messages.groupsEditNote : messages.regionsNote}</p>
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
        const titleEntry = group.titles.find((entry) => entry.languageId.toLowerCase() === contentLanguageId.toLowerCase());
        const storedTitle = stored?.titles.find((entry) => entry.languageId.toLowerCase() === contentLanguageId.toLowerCase());
        const inactiveTitles = (stored?.titles ?? []).filter(
          (entry) => !activeLanguageIds.some((id) => id.toLowerCase() === entry.languageId.toLowerCase()),
        );

        return (
          <section
            key={group.key}
            className="rounded-xl border border-border bg-surface-elevated p-3"
            data-testid="shipping-authoring-group-draft"
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
                    onClick={() => setPendingRemoval(null)}
                  >
                    {messages.groupRemoveCancelAction}
                  </button>
                </div>
              </div>
            ) : null}

            <label className="mt-2 grid gap-0.5">
              <span className="text-[10px] font-bold text-gray-600">{messages.groupTitleLabel}</span>
              <input
                type="text"
                className="min-h-8 w-full rounded-lg border border-border bg-surface px-2 text-xs text-gray-900"
                value={titleEntry?.title ?? ""}
                placeholder={storedTitle?.title ?? ""}
                disabled={!editable}
                data-testid="shipping-authoring-group-title"
                onChange={(event) => onChangeTitle(group.key, contentLanguageId, event.target.value)}
              />
            </label>
            {inactiveTitles.length > 0 ? (
              <p className="mt-1 text-[10px] text-gray-500">
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
                    const label = index.labelOf(area);
                    const isUnresolved = groupUnresolved.some((entry) => areaIdentity(entry.area) === areaIdentity(area));
                    return (
                      <li
                        key={areaIdentity(area)}
                        className="inline-flex items-center gap-1 rounded-full bg-surface px-2 py-0.5 text-[11px] text-gray-700"
                        data-testid={`shipping-authoring-area-${area.kind}`}
                      >
                        <span className="text-gray-500">{messages[AREA_KIND_LABEL_OF[area.kind]]}: </span>
                        {label ?? (
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
                <AuthoringDestinationPicker
                  languageId={contentLanguageId}
                  locale={locale}
                  messages={messages}
                  disabled={!editable}
                  isSelected={(area) => groups.some((candidate) => candidate.areas.some((entry) => areaIdentity(entry) === areaIdentity(area)))}
                  onAdd={(area, ancestry, label) => onAddArea(group.key, area, ancestry, label)}
                />
              </div>
            ) : null}

            <div className="mt-2">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <p className="text-[11px] font-bold text-gray-800">{messages.ratesTitle}</p>
                {editable ? (
                  <button
                    type="button"
                    className="inline-flex min-h-8 items-center rounded-lg border border-border bg-surface px-2 text-[11px] font-bold text-gray-800"
                    data-testid="shipping-authoring-rate-add"
                    onClick={() => onAddRow(group.key)}
                  >
                    {messages.addRowAction}
                  </button>
                ) : null}
              </div>
              {(rates[group.key] ?? []).length === 0 ? (
                <p className="mt-1 text-[11px] text-gray-500">{messages.noRows}</p>
              ) : (
                <ul className="mt-1 grid gap-2">
                  {(rates[group.key] ?? []).map((row: RateRowDraft, rowIndex: number) => (
                    <RateRowEditor
                      key={row.rowKey}
                      row={row}
                      index={rowIndex}
                      locale={locale}
                      messages={messages}
                      issue={groupRateIssues.find((issue) => issue.rowKey === row.rowKey)}
                      onChange={(field, value) => onChangeRowField(group.key, row.rowKey, field, value)}
                      onRemove={() => onRemoveRow(group.key, row.rowKey)}
                    />
                  ))}
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
    </div>
  );
}

const AREA_KIND_LABEL_OF = {
  Country: "areaCountry",
  AdministrativeArea: "areaAdministrativeArea",
  City: "areaCity",
  ServiceArea: "areaServiceArea",
} as const;
