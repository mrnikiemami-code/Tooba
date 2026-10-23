"use client";

/**
 * Regions tab of one existing draft: the stored group topology and geography are shown truthfully
 * ONCE per group and stay READ-ONLY, while the rate rows beneath each existing group are editable in
 * edit mode. No group create/delete/reorder, no geography editing and no duplicated geography.
 */
import type { AuthoringLocale, AuthoringMessages } from "../messages/authoring-messages.ts";
import type { EditorDocument } from "../model/editor-document.ts";
import type { DestinationFactIndex } from "../model/editor-destination-ancestry.ts";
import type { RateField, RateRowIssue, RatesFormState } from "../model/editor-rates.ts";
import { AuthoringRatesEditor } from "./authoring-rates-editor.tsx";

export function AuthoringRegionsPanel({
  document,
  index,
  locale,
  messages,
  contentLanguageId,
  rates,
  issues,
  editable,
  onAddRow,
  onRemoveRow,
  onChangeField,
}: {
  document: EditorDocument;
  index: DestinationFactIndex;
  locale: AuthoringLocale;
  messages: AuthoringMessages;
  contentLanguageId: string;
  rates: RatesFormState;
  issues: readonly RateRowIssue[];
  editable: boolean;
  onAddRow: (groupId: string) => void;
  onRemoveRow: (groupId: string, rowKey: string) => void;
  onChangeField: (groupId: string, rowKey: string, field: RateField, value: string) => void;
}) {
  return (
    <div className="grid gap-3" data-testid="shipping-authoring-regions-panel">
      <p className="rounded-xl border border-border bg-surface px-3 py-2 text-xs text-gray-600">
        {editable ? messages.ratesEditNote : messages.regionsNote}
      </p>

      {document.regionGroups.length === 0 ? (
        <p className="text-xs text-gray-500">{messages.noGroups}</p>
      ) : (
        document.regionGroups.map((group, position) => (
          <AuthoringRatesEditor
            key={group.groupId}
            group={group}
            position={position}
            rows={rates[group.groupId] ?? []}
            issues={issues.filter((issue) => issue.groupId === group.groupId)}
            index={index}
            locale={locale}
            messages={messages}
            contentLanguageId={contentLanguageId}
            editable={editable}
            onAddRow={() => onAddRow(group.groupId)}
            onRemoveRow={(rowKey) => onRemoveRow(group.groupId, rowKey)}
            onChangeField={(rowKey, field, value) => onChangeField(group.groupId, rowKey, field, value)}
          />
        ))
      )}
    </div>
  );
}
