/**
 * Orchestration of ONE existing draft's editor: load, intentional edits, ONE atomic Apply, explicit
 * reconciliation after an unknown commit. No React, no DOM, no browser dialogs and no storage — the
 * rendered screen only dispatches, so the real flow is testable with controlled transport promises.
 *
 * Rules enforced here (not in the presentation):
 * - Apply is gated SYNCHRONOUSLY before the first await, so a double click sends exactly one PUT;
 * - a completion is scoped to the session epoch/request id kept by the reducer, so a late answer can
 *   never overwrite newer work;
 * - a latched unknown commit blocks every further PUT until an explicit reconciliation read or an
 *   explicit discard + fresh load; nothing is retried automatically.
 */
import type { EditorOutcome, EditorTransportFailure } from "../api/editor-document-api.ts";
import type { EditorDocument } from "../model/editor-document.ts";
import type { AreaAncestryResolver, EditorEdits } from "../model/editor-edits.ts";
import {
  createEditorSessionState,
  hasUnsavedWork,
  reduceEditorSession,
  selectApplyRequest,
  type EditorApplyRequest,
  type EditorSessionState,
} from "./editor-session.ts";

/** The two authorized calls the controller needs (a fake is a pair of functions). */
export interface EditorTransport {
  load(draftId: string, signal?: AbortSignal): Promise<EditorOutcome>;
  save(draftId: string, bodyText: string, signal?: AbortSignal): Promise<EditorOutcome>;
}

/** Observable controller state. */
export interface EditorControllerState {
  readonly session: EditorSessionState;
  /** True while the ONE Apply request is in flight (synchronous gate, not a rendered flag). */
  readonly applying: boolean;
  /** Revision of the last confirmed save, for the localized success notice. */
  readonly savedRevision: number | null;
  /** True after a first successful reconciliation of an unknown commit. */
  readonly reconciled: boolean;
}

/** Result of one Apply attempt. */
export type EditorApplyOutcome =
  | { readonly status: "blocked"; readonly reason: Extract<EditorApplyRequest, { status: "blocked" }>["reason"] }
  | { readonly status: "sent" }
  | { readonly status: "failed"; readonly failure: EditorTransportFailure };

/** Controller handle used by the screen. */
export interface EditorController {
  getState(): EditorControllerState;
  subscribe(listener: () => void): () => void;
  begin(): void;
  retryLoad(): void;
  edit(edits: EditorEdits): void;
  apply(resolver: AreaAncestryResolver): Promise<EditorApplyOutcome>;
  reconcile(): Promise<void>;
  requestClose(): boolean;
  confirmDiscard(): void;
  cancelDiscard(): void;
  dispose(): void;
}

/**
 * Creates the controller of one draft.
 *
 * @param draftId Draft id from the route (never a browser-supplied owner).
 * @param transport Authorized transport pair.
 */
export function createEditorController(draftId: string, transport: EditorTransport): EditorController {
  let state: EditorControllerState = {
    session: createEditorSessionState(draftId),
    applying: false,
    savedRevision: null,
    reconciled: false,
  };
  const listeners = new Set<() => void>();
  let requestCounter = 0;
  let disposed = false;
  let inFlight: AbortController | null = null;

  const emit = (): void => {
    if (disposed) return;
    for (const listener of listeners) listener();
  };
  const setState = (next: EditorControllerState): void => {
    state = next;
    emit();
  };
  const dispatch = (action: Parameters<typeof reduceEditorSession>[1]): EditorSessionState => {
    const session = reduceEditorSession(state.session, action);
    if (session !== state.session) setState({ ...state, session });
    return session;
  };

  const load = async (): Promise<void> => {
    const session = dispatch({ type: "loadStarted", draftId });
    if (session.load !== "loading") return;
    const epoch = session.epoch;
    const outcome = await transport.load(draftId);
    if (disposed) return;
    if (outcome.kind === "ok") {
      dispatch({ type: "loadSucceeded", draftId, epoch, document: outcome.document });
      return;
    }
    dispatch({ type: "loadFailed", draftId, epoch, failure: outcome.failure });
  };

  return {
    getState: () => state,
    subscribe: (listener) => {
      listeners.add(listener);
      return () => listeners.delete(listener);
    },
    begin: () => {
      if (state.session.load === "idle") void load();
    },
    retryLoad: () => {
      if (state.session.load === "loading" || state.session.commitUncertain) return;
      void load();
    },
    edit: (edits) => {
      dispatch({ type: "edited", draftId, edits });
    },
    apply: async (resolver) => {
      // Synchronous gate BEFORE any await: a second click in the same tick never sends a second PUT.
      if (inFlight !== null || disposed) return { status: "blocked", reason: "in-flight" };
      requestCounter += 1;
      const selection = selectApplyRequest(state.session, requestCounter, resolver);
      if (selection.status === "blocked") return { status: "blocked", reason: selection.reason };
      if (selection.body.status === "needsResolution") return { status: "blocked", reason: "needs-resolution" };
      if (selection.body.status === "invalidIntent") return { status: "blocked", reason: "invalid-intent" };

      const controller = new AbortController();
      inFlight = controller;
      const epoch = state.session.epoch;
      dispatch({ type: "saveStarted", draftId, epoch, requestId: selection.requestId });
      setState({ ...state, applying: true });
      let outcome: EditorOutcome;
      try {
        outcome = await transport.save(draftId, selection.body.text, controller.signal);
      } finally {
        if (inFlight === controller) inFlight = null;
      }
      if (disposed) return { status: "sent" };
      setState({ ...state, applying: false });
      if (outcome.kind === "ok") {
        dispatch({
          type: "saveSucceeded",
          draftId,
          epoch,
          requestId: selection.requestId,
          document: outcome.document,
        });
        setState({ ...state, savedRevision: outcome.document.revision });
        return { status: "sent" };
      }
      dispatch({ type: "saveFailed", draftId, epoch, requestId: selection.requestId, failure: outcome.failure });
      return { status: "failed", failure: outcome.failure };
    },
    reconcile: async () => {
      if (disposed || !state.session.commitUncertain || state.session.baseline === null) return;
      const epoch = state.session.epoch + 1;
      const started = dispatch({ type: "reconcileStarted", draftId, epoch });
      if (started.reconcile !== "pending") return;
      const outcome = await transport.load(draftId);
      if (disposed) return;
      if (outcome.kind === "ok") {
        dispatch({ type: "reconcileSucceeded", draftId, epoch, document: outcome.document });
        setState({ ...state, reconciled: true, savedRevision: outcome.document.revision });
        return;
      }
      dispatch({ type: "reconcileFailed", draftId, epoch, failure: outcome.failure });
    },
    requestClose: () => {
      if (!hasUnsavedWork(state.session)) return true;
      dispatch({ type: "closeRequested", draftId });
      return false;
    },
    confirmDiscard: () => {
      dispatch({ type: "discardConfirmed", draftId });
      // A discarded same-draft session must re-read the server truth, never reuse the old baseline.
      if (state.session.load === "idle") void load();
    },
    cancelDiscard: () => {
      dispatch({ type: "discardCancelled", draftId });
    },
    dispose: () => {
      disposed = true;
      inFlight?.abort();
      inFlight = null;
      listeners.clear();
    },
  };
}

/** The document currently confirmed by the server, if any. */
export function confirmedDocument(state: EditorControllerState): EditorDocument | null {
  return state.session.baseline;
}
