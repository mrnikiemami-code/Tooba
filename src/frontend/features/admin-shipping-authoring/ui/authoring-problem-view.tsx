"use client";

/**
 * Presentational problem panels of the authoring editor (kept out of the screen composition).
 *
 * Boundary: these components render only what the controller/session already decided — a typed transport
 * failure, the localized field placements of a server rejection, and the local intent issues that block
 * Apply. They never edit a value, never retry a write and never change the draft revision.
 */
import type { EditorTransportFailure } from "../api/editor-document-api.ts";
import { authoringFormat, type AuthoringMessageKey, type AuthoringMessages } from "../messages/authoring-messages.ts";
import type { AuthoringIntentIssues } from "../model/authoring-intent.ts";
import type { GroupIssue } from "../model/editor-groups.ts";
import type { EditorSaveStatus } from "../session/editor-session.ts";
import { areaRangeIssueMessage, rowIssueMessage } from "./authoring-rates-editor.tsx";
import { weekIssueMessage } from "./authoring-week-editor.tsx";
import type { ServerFieldIssue } from "./server-field-error.ts";

/** Localized prose of one typed transport failure (raw server codes never become prose). */
export function authoringFailureMessages(
  failure: EditorTransportFailure,
  messages: AuthoringMessages,
): readonly string[] {
  switch (failure.kind) {
    case "denied":
      return [messages.saveDenied];
    case "conflict":
      return [messages.saveConflict];
    case "validation":
      return [messages.saveValidation];
    case "unavailable":
      return [messages.saveUnavailable];
    case "outcomeUnknown":
      return [messages.saveUnknown];
    case "untrusted":
      return [messages.loadUntrusted];
    case "notFound":
      return [messages.loadNotFound];
    case "network":
      return [messages.saveUnavailable];
    default:
      return [messages.saveError];
  }
}

function groupIssueText(issue: GroupIssue, messages: AuthoringMessages): string {
  switch (issue.code) {
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

/** The typed rejection of the last save, placed on this screen's real fields. */
export function AuthoringSaveFailurePanel({
  failure,
  status,
  serverIssues,
  serverCodes,
  messages,
  commitUncertain,
  onReveal,
  onReconcile,
}: {
  failure: EditorTransportFailure | null;
  status: EditorSaveStatus;
  serverIssues: readonly ServerFieldIssue[];
  serverCodes: readonly string[];
  messages: AuthoringMessages;
  commitUncertain: boolean;
  onReveal: (issue: ServerFieldIssue) => void;
  onReconcile: () => void;
}) {
  if (!failure || status === "clean") return null;
  return (
    <div role="alert" className="rounded-xl border border-danger/40 bg-red-50 px-3 py-2 text-xs font-semibold text-danger">
      {authoringFailureMessages(failure, messages)
        .filter(() => !(failure.kind === "validation" && serverIssues.length > 0))
        .map((text) => (
          <p key={text} data-testid={`shipping-authoring-save-${failure.kind}`}>
            {text}
          </p>
        ))}
      {serverIssues.length > 0 ? (
        <>
          <p className="mt-1 font-normal text-gray-700">{messages.saveValidation}</p>
          <ul className="mt-1 grid gap-0.5 font-normal" data-testid="shipping-authoring-save-fields">
            {serverIssues.map((issue) => (
              <li key={`${issue.target}:${issue.rowIndex ?? "-"}:${issue.reason}`}>
                <button
                  type="button"
                  className="text-start underline decoration-dotted underline-offset-2"
                  data-testid={`shipping-authoring-save-field-${issue.target}`}
                  onClick={() => onReveal(issue)}
                >
                  {authoringFormat(messages.serverFieldErrorLine, { field: issue.field, reason: issue.reason })}
                </button>
              </li>
            ))}
          </ul>
        </>
      ) : null}
      {serverCodes.length > 0 ? (
        <details className="mt-1 font-normal" data-testid="shipping-authoring-save-diagnostics">
          <summary className="cursor-pointer text-[11px] text-gray-600">{messages.diagnosticsToggle}</summary>
          <p dir="ltr" className="mt-0.5 text-[11px] text-gray-500" data-testid="shipping-authoring-save-code">
            {authoringFormat(messages.serverErrorCodeNote, { code: serverCodes.join(", ") })}
          </p>
        </details>
      ) : null}
      {status === "outcomeUnknown" || commitUncertain ? (
        <button
          type="button"
          className="mt-2 inline-flex min-h-8 items-center rounded-lg border border-border px-2 text-gray-800"
          data-testid="shipping-authoring-reconcile"
          onClick={onReconcile}
        >
          {messages.reconcileAction}
        </button>
      ) : null}
    </div>
  );
}

/** The local issues that block Apply, in the same words the fields use. */
export function AuthoringLocalIssuesPanel({
  issues,
  groupProblems,
  messages,
}: {
  issues: AuthoringIntentIssues;
  groupProblems: readonly GroupIssue[];
  messages: AuthoringMessages;
}) {
  const lines = Array.from(
    new Set([
      ...issues.general.map((issue) =>
        issue.code === "nameRequired" ? messages.nameRequired : messages.settingsIncomplete,
      ),
      ...groupProblems.map((issue) => groupIssueText(issue, messages)),
      ...issues.rates.map((issue) => rowIssueMessage(issue.code, messages)),
      ...issues.areaRanges.map((issue) => areaRangeIssueMessage(issue.code, messages)),
      ...issues.week.map((issue) => weekIssueMessage(issue.code, messages)),
    ]),
  );
  if (lines.length === 0) return null;
  return (
    <div
      role="alert"
      className="rounded-xl border border-danger/40 bg-red-50 px-3 py-2 text-xs font-semibold text-danger"
      data-testid="shipping-authoring-local-issues"
    >
      <p className="font-black">{messages.applyBlockedLocal}</p>
      <ul className="mt-1 list-disc space-y-0.5 ps-4 font-normal">
        {lines.map((text: string) => (
          <li key={text}>{text}</li>
        ))}
      </ul>
    </div>
  );
}

/** Type-only re-export marker so the unused-import guard sees the message key space. */
export type ProblemMessageKey = AuthoringMessageKey;