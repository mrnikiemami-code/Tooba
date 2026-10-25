"use client";

/**
 * Small cohesive publication panel of ONE draft, composed into the editor screen.
 *
 * It reads the stored pointer once per draft, then lets a CLEAN VIEW-mode operator publish the confirmed
 * saved revision or withdraw the selection. It never reads or writes the editor document: a publication
 * write cannot reseed the form, advance the draft revision or discard typed work.
 *
 * Discipline kept in the model (`model/authoring-publication.ts`): a synchronous in-flight gate before the
 * first `await`, an explicit read after any unknown outcome, and no automatic retry after a rejection.
 */
import { useCallback, useEffect, useRef, useState } from "react";
import { Dialog } from "../../../design-system";
import {
  loadAuthoringPublication,
  publishAuthoringDraft,
  withdrawAuthoringPublication,
} from "../api/authoring-publication-api.ts";
import { authoringFormat, type AuthoringMessages } from "../messages/authoring-messages.ts";
import {
  beginRead,
  beginWrite,
  canPublish,
  canWithdraw,
  initialPublicationState,
  publicationBlockedReason,
  publicationPhase,
  publicationStateMessage,
  resolveRead,
  resolveWrite,
  type PublicationWrite,
} from "../model/authoring-publication.ts";

/** Reasons worth showing next to the actions; the rest are self-evident from the mode. */
const EXPLAINED_REASONS = new Set(["publicationUnknownBlock", "publicationStaleBlock", "publicationNoState", "publicationNoRevision", "publicationAlreadyCurrent"]);

export function AuthoringPublicationPanel({
  draftId,
  currentRevision,
  mode,
  busy,
  messages,
}: {
  draftId: string;
  currentRevision: number | null;
  mode: "view" | "edit";
  busy: boolean;
  messages: AuthoringMessages;
}) {
  const [state, setState] = useState(initialPublicationState);
  const [confirm, setConfirm] = useState<PublicationWrite | null>(null);
  const [notice, setNotice] = useState<{ tone: "success" | "error"; text: string } | null>(null);
  /** Bumped on every draft switch so a late response can never touch another draft's panel. */
  const epoch = useRef(0);
  /** Synchronous write gate: a second click cannot reach a second POST, even before a re-render. */
  const inFlight = useRef(false);
  const latest = useRef(state);
  latest.current = state;

  useEffect(() => {
    epoch.current += 1;
    const mine = epoch.current;
    const controller = new AbortController();
    inFlight.current = false;
    setState(initialPublicationState);
    setConfirm(null);
    setNotice(null);
    void (async () => {
      const outcome = await loadAuthoringPublication(draftId, controller.signal);
      if (controller.signal.aborted || mine !== epoch.current) return;
      setState((current) => resolveRead(current, outcome));
    })();
    return () => controller.abort();
  }, [draftId]);

  /** Explicit operator read: the ONLY way out of a stale/unknown latch. */
  const readStatus = useCallback(async () => {
    if (inFlight.current) return;
    const mine = epoch.current;
    setState((current) => beginRead(current));
    const outcome = await loadAuthoringPublication(draftId);
    if (mine !== epoch.current) return;
    setState((current) => resolveRead(current, outcome));
  }, [draftId]);

  const send = useCallback(
    async (action: PublicationWrite) => {
      setConfirm(null);
      if (inFlight.current) return;
      const before = latest.current;
      const next = beginWrite(before, action, mode, busy, currentRevision);
      if (next === null || before.view === null) return;
      // Captured BEFORE the await: the request carries exactly the tokens the operator confirmed.
      const expectedSelectionVersion = before.view.selectionVersion;
      const expectedRevision = currentRevision;
      if (action === "publish" && expectedRevision === null) return;

      inFlight.current = true;
      const mine = epoch.current;
      setState(next);
      setNotice(null);
      try {
        const outcome = action === "publish"
          ? await publishAuthoringDraft(draftId, expectedRevision as number, expectedSelectionVersion)
          : await withdrawAuthoringPublication(draftId, expectedSelectionVersion);
        if (mine !== epoch.current) return;
        setState((current) => resolveWrite(current, outcome));
        if (outcome.kind === "ok") {
          setNotice({
            tone: "success",
            text: action === "publish"
              ? authoringFormat(messages.publicationPublished, { revision: outcome.view.currentDraftRevision })
              : messages.publicationDidWithdraw,
          });
        }
      } finally {
        inFlight.current = false;
      }
    },
    [busy, currentRevision, draftId, mode, messages.publicationDidWithdraw, messages.publicationPublished],
  );

  const publishAvailable = canPublish(state, mode, busy, currentRevision);
  const withdrawAvailable = canWithdraw(state, mode, busy, currentRevision);
  const blocked = publicationBlockedReason(state, "publish", mode, busy, currentRevision);
  const explained = blocked !== null && EXPLAINED_REASONS.has(blocked) ? messages[blocked] : null;
  const view = state.read === "ok" ? state.view : null;
  const phase = view ? publicationPhase(view, currentRevision) : null;

  return (
    <section
      className="rounded-xl border border-border bg-surface px-3 py-2"
      data-testid="shipping-authoring-publication"
      aria-label={messages.publicationTitle}
    >
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div className="min-w-0 space-y-0.5">
          <h2 className="text-xs font-black text-gray-900">{messages.publicationTitle}</h2>
          <p className="text-xs font-semibold text-gray-600" data-testid="shipping-authoring-publication-status">
            {messages[publicationStateMessage(state)]}
          </p>
          {phase === "current" && view ? (
            <p className="text-[11px] text-gray-500" data-testid="shipping-authoring-publication-current">
              {authoringFormat(messages.publicationCurrentRevision, { revision: String(view.selectedSourceRevision ?? "") })}
            </p>
          ) : null}
          {phase === "older" && view ? (
            <p className="text-[11px] text-amber-800" data-testid="shipping-authoring-publication-older">
              {authoringFormat(messages.publicationOlderRevision, {
                revision: String(view.selectedSourceRevision ?? ""),
                current: String(currentRevision ?? ""),
              })}
            </p>
          ) : null}
          {view && view.state !== "absent" ? (
            <p className="text-[11px] text-gray-500" data-testid="shipping-authoring-publication-version">
              {authoringFormat(messages.publicationVersionText, { version: String(view.selectionVersion) })}
            </p>
          ) : null}
          {view && view.state === "selected" && view.selectedSnapshotId ? (
            <p dir="ltr" className="text-[11px] text-gray-400" data-testid="shipping-authoring-publication-snapshot">
              {authoringFormat(messages.publicationSnapshotText, { snapshot: view.selectedSnapshotId })}
            </p>
          ) : null}
        </div>
        <div className="flex flex-wrap items-center gap-2">
          {state.stale || state.unknown || state.read === "error" || state.read === "unavailable" || state.read === "notfound" ? (
            <button
              type="button"
              className="inline-flex min-h-8 items-center rounded-lg border border-border px-2.5 text-[11px] font-bold text-gray-700 disabled:opacity-50"
              data-testid="shipping-authoring-publication-read"
              disabled={state.pending !== null}
              onClick={() => void readStatus()}
            >
              {messages.publicationReadAction}
            </button>
          ) : null}
          <button
            type="button"
            className="inline-flex min-h-8 items-center rounded-lg border border-border px-2.5 text-[11px] font-bold text-gray-800 disabled:opacity-40"
            data-testid="shipping-authoring-publication-publish"
            disabled={!publishAvailable}
            onClick={() => setConfirm("publish")}
          >
            {state.pending === "publish" ? messages.publicationPublishing : messages.publishAction}
          </button>
          <button
            type="button"
            className="inline-flex min-h-8 items-center rounded-lg border border-danger/40 px-2.5 text-[11px] font-bold text-danger disabled:opacity-40"
            data-testid="shipping-authoring-publication-withdraw"
            disabled={!withdrawAvailable}
            onClick={() => setConfirm("withdraw")}
          >
            {state.pending === "withdraw" ? messages.publicationWithdrawing : messages.withdrawAction}
          </button>
        </div>
      </div>

      {explained ? (
        <p role="alert" className="mt-1 text-[11px] font-semibold text-amber-800" data-testid="shipping-authoring-publication-blocked">
          {explained}
        </p>
      ) : null}
      <p className="mt-1 text-[11px] text-gray-500" data-testid="shipping-authoring-publication-note">
        {messages.publicationNote}
      </p>
      {notice ? (
        <p
          role={notice.tone === "error" ? "alert" : "status"}
          className="mt-1 text-[11px] font-semibold text-emerald-800"
          data-testid="shipping-authoring-publication-notice"
        >
          {notice.text}
        </p>
      ) : null}

      <Dialog
        open={confirm !== null}
        onClose={() => setConfirm(null)}
        title={confirm === "withdraw" ? messages.publicationWithdrawConfirmTitle : messages.publicationPublishConfirmTitle}
        showCloseButton
      >
        <div className="space-y-3 text-sm text-gray-700">
          <p data-testid="shipping-authoring-publication-confirm-text">
            {confirm === "withdraw"
              ? messages.publicationWithdrawConfirmText
              : authoringFormat(messages.publicationPublishConfirmText, { revision: String(currentRevision ?? "") })}
          </p>
          <div className="flex flex-wrap justify-end gap-2">
            <button
              type="button"
              className="inline-flex min-h-9 items-center rounded-lg border border-border px-3 text-xs font-bold text-gray-700"
              data-testid="shipping-authoring-publication-confirm-cancel"
              onClick={() => setConfirm(null)}
            >
              {messages.publicationCancelAction}
            </button>
            <button
              type="button"
              className="inline-flex min-h-9 items-center rounded-lg bg-primary px-3 text-xs font-bold text-primary-foreground"
              data-testid="shipping-authoring-publication-confirm-accept"
              onClick={() => (confirm === null ? undefined : void send(confirm))}
            >
              {messages.publicationConfirmAction}
            </button>
          </div>
        </div>
      </Dialog>
    </section>
  );
}
