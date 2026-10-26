"use client";

/**
 * COMPACT publication action of ONE draft, composed into the editor screen.
 *
 * The semantics are the accepted TB-221 ones (recovered, not re-invented): one real status read, an exact
 * `expectedRevision` + `expectedSelectionVersion` pair captured BEFORE the await, an explicit confirmation,
 * a synchronous in-flight gate, and the stale/unknown latches that only an explicit successful read clears.
 * A publication write never reseeds the editor document, advances the draft revision or discards typed work.
 *
 * Presentation is deliberately small: a status line, the two actions and one honest note. Snapshot ids,
 * selection-version counters and format versions stay OUT of the normal UI — the operator acts on the method,
 * not on transport internals.
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
  type PublicationPanelState,
  type PublicationWrite,
} from "../model/authoring-publication.ts";

/** Reasons worth one short line next to the actions; the rest are self-evident from the disabled state. */
const EXPLAINED_REASONS = new Set([
  "publicationUnknownBlock",
  "publicationStaleBlock",
  "publicationNoState",
  "publicationAlreadyCurrent",
  "publicationViewOnly",
]);

export function AuthoringPublicationPanel({
  draftId,
  currentRevision,
  mode,
  busy,
  messages,
  onPublished,
}: {
  draftId: string;
  currentRevision: number | null;
  mode: "view" | "edit";
  busy: boolean;
  messages: AuthoringMessages;
  /** Reported after a confirmed write so the screen can show its own notice; nothing is reloaded here. */
  onPublished: (text: string) => void;
}) {
  const [state, setState] = useState<PublicationPanelState>(initialPublicationState);
  const [confirm, setConfirm] = useState<PublicationWrite | null>(null);
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
      try {
        const outcome = action === "publish"
          ? await publishAuthoringDraft(draftId, expectedRevision as number, expectedSelectionVersion)
          : await withdrawAuthoringPublication(draftId, expectedSelectionVersion);
        if (mine !== epoch.current) return;
        setState((current) => resolveWrite(current, outcome));
        if (outcome.kind === "ok") {
          onPublished(
            action === "publish"
              ? authoringFormat(messages.publicationPublished, { revision: String(outcome.view.currentDraftRevision) })
              : messages.publicationDidWithdraw,
          );
        }
      } finally {
        inFlight.current = false;
      }
    },
    [
      busy,
      currentRevision,
      draftId,
      messages.publicationDidWithdraw,
      messages.publicationPublished,
      mode,
      onPublished,
    ],
  );

  const publishAvailable = canPublish(state, mode, busy, currentRevision);
  const withdrawAvailable = canWithdraw(state, mode, busy, currentRevision);
  const blocked = publicationBlockedReason(state, "publish", mode, busy, currentRevision);
  const explained = blocked !== null && EXPLAINED_REASONS.has(blocked) ? messages[blocked] : null;
  const view = state.read === "ok" ? state.view : null;
  const phase = view ? publicationPhase(view, currentRevision) : null;
  const needsRead =
    state.stale || state.unknown || state.read === "error" || state.read === "unavailable" || state.read === "notfound";

  return (
    <section
      className="flex flex-wrap items-center justify-between gap-x-3 gap-y-1 rounded-xl border border-border bg-surface px-3 py-2"
      data-testid="shipping-authoring-publication"
      aria-label={messages.publicationTitle}
    >
      <div className="min-w-0">
        <p className="text-xs font-black text-gray-900" data-testid="shipping-authoring-publication-status">
          {messages[publicationStateMessage(state)]}
        </p>
        {phase === "older" ? (
          <p className="text-[11px] font-semibold text-amber-800" data-testid="shipping-authoring-publication-older">
            {messages.publicationSelectedOlder}
          </p>
        ) : null}
        {explained ? (
          <p role="alert" className="text-[11px] font-semibold text-amber-800" data-testid="shipping-authoring-publication-blocked">
            {explained}
          </p>
        ) : null}
        <p className="text-[11px] text-gray-500" data-testid="shipping-authoring-publication-note">
          {messages.publicationNote}
        </p>
      </div>
      <div className="flex flex-wrap items-center gap-2">
        {needsRead ? (
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
