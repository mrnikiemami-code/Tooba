/**
 * Local editor session state for ONE existing shipping authoring draft.
 *
 * Pure reducer + selectors: no fetch, no rendering, no global store, no browser dialogs or storage.
 * It keeps the confirmed server baseline apart from the local edit intent and scopes every async
 * completion to a NEVER-REUSED session epoch plus the in-epoch save request id, so a late load/save
 * for another draft, an obsolete epoch or a reopened session can never overwrite current work.
 *
 * An unreadable/unreachable PUT commit (`outcomeUnknown`) stays latched across local edits: another
 * PUT is refused until an explicit reconciliation read confirms a new baseline, or the operator
 * discards and a fresh load re-establishes one. The previous write is never assumed to have failed.
 */
import type { EditorDocument } from "../model/editor-document.ts";
import type { AreaAncestryResolver, EditorEdits } from "../model/editor-edits.ts";
import type { EditorTransportFailure } from "../api/editor-document-api.ts";
import { buildEditorDocumentBody, type EditorRequestBody } from "../api/editor-document-request.ts";

/** Load lifecycle of the draft document. */
export type EditorLoadStatus = "idle" | "loading" | "loaded" | "denied" | "notfound" | "error";

/** Save lifecycle of the local edits. */
export type EditorSaveStatus = "clean" | "saving" | "validationFailed" | "conflict" | "outcomeUnknown" | "error";

/** Pending confirmation the UI must ask the operator before throwing work away. */
export type EditorDecision = "none" | "discardUnsaved";

/** State of an explicit reconciliation read after an unknown commit. */
export type EditorReconcileStatus = "idle" | "pending";

/** Immutable session state. */
export interface EditorSessionState {
  readonly draftId: string;
  /** Monotonic, never-reused identity of this editing session (never reset on switch/discard/reopen). */
  readonly epoch: number;
  readonly load: EditorLoadStatus;
  readonly loadFailure: EditorTransportFailure | null;
  readonly baseline: EditorDocument | null;
  readonly edits: EditorEdits;
  readonly save: EditorSaveStatus;
  readonly saveFailure: EditorTransportFailure | null;
  readonly saveRequestId: number;
  /**
   * True while the local form holds work that is not (yet) a serializable patch — a blanked name,
   * a description typed before its name, or a one-sided settings choice. It keeps the raw work
   * dirty so it can never look clean, be discarded silently or be resubmitted as an empty PUT.
   */
  readonly localDirty: boolean;
  readonly unsaved: boolean;
  readonly decision: EditorDecision;
  readonly pendingSwitchTo: string | null;
  /** Latched while a previous PUT's commit cannot be established. Blocks every further PUT. */
  readonly commitUncertain: boolean;
  readonly reconcile: EditorReconcileStatus;
  readonly reconcileFailure: EditorTransportFailure | null;
}

/** Actions the editor screen dispatches. */
export type EditorSessionAction =
  | { readonly type: "loadStarted"; readonly draftId: string }
  | {
      readonly type: "loadSucceeded";
      readonly draftId: string;
      readonly epoch: number;
      readonly document: EditorDocument;
    }
  | {
      readonly type: "loadFailed";
      readonly draftId: string;
      readonly epoch: number;
      readonly failure: EditorTransportFailure;
    }
  | {
      readonly type: "edited";
      readonly draftId: string;
      readonly edits: EditorEdits;
      /**
       * True when the raw form holds local work that is not all serializable yet. Defaults to false
       * so existing callers/tests keep the pure wire-patch semantics.
       */
      readonly localDirty?: boolean;
    }
  | {
      readonly type: "saveStarted";
      readonly draftId: string;
      readonly epoch: number;
      readonly requestId: number;
    }
  | {
      readonly type: "saveSucceeded";
      readonly draftId: string;
      readonly epoch: number;
      readonly requestId: number;
      readonly document: EditorDocument;
    }
  | {
      readonly type: "saveFailed";
      readonly draftId: string;
      readonly epoch: number;
      readonly requestId: number;
      readonly failure: EditorTransportFailure;
    }
  | { readonly type: "reconcileStarted"; readonly draftId: string; readonly epoch: number }
  | {
      readonly type: "reconcileSucceeded";
      readonly draftId: string;
      readonly epoch: number;
      readonly document: EditorDocument;
    }
  | {
      readonly type: "reconcileFailed";
      readonly draftId: string;
      readonly epoch: number;
      readonly failure: EditorTransportFailure;
    }
  | { readonly type: "switchRequested"; readonly draftId: string; readonly nextDraftId: string }
  | { readonly type: "closeRequested"; readonly draftId: string }
  | { readonly type: "discardConfirmed"; readonly draftId: string }
  | { readonly type: "discardCancelled"; readonly draftId: string };

/** Creates the initial session state for one draft. */
export function createEditorSessionState(draftId: string): EditorSessionState {
  return {
    draftId,
    epoch: 0,
    load: "idle",
    loadFailure: null,
    baseline: null,
    edits: {},
    save: "clean",
    saveFailure: null,
    saveRequestId: 0,
    localDirty: false,
    unsaved: false,
    decision: "none",
    pendingSwitchTo: null,
    commitUncertain: false,
    reconcile: "idle",
    reconcileFailure: null,
  };
}

function sameDraft(state: EditorSessionState, draftId: string): boolean {
  return state.draftId === draftId;
}

function isDirty(edits: EditorEdits): boolean {
  if (edits.regionGroups !== undefined) return true;
  if (edits.methodTranslations !== undefined) return true;
  if (edits.weeklySchedule !== undefined) return true;
  if (edits.methodSettings !== undefined) return true;
  return edits.typeAssignment !== undefined;
}

function saveFailureStatus(failure: EditorTransportFailure): EditorSaveStatus {
  if (failure.kind === "validation") return "validationFailed";
  if (failure.kind === "conflict") return "conflict";
  if (failure.kind === "outcomeUnknown") return "outcomeUnknown";
  return "error";
}

/** Session state after leaving the current draft, with a strictly greater epoch. */
function switchedState(from: EditorSessionState, nextDraftId: string): EditorSessionState {
  return {
    ...createEditorSessionState(nextDraftId),
    epoch: from.epoch + 1,
    load: "idle",
  };
}

/**
 * Reduces one editor session action.
 *
 * A completion whose draft/epoch (or in-epoch request id) does not match the current session returns
 * the SAME state object, so a slow or reopened session's result can never overwrite current work.
 * Unsaved work is only dropped on an explicit `discardConfirmed`.
 */
export function reduceEditorSession(state: EditorSessionState, action: EditorSessionAction): EditorSessionState {
  if (action.type === "loadStarted") {
    // A latched unknown commit needs an explicit reconciliation, not a silent reload.
    if (state.commitUncertain && state.baseline !== null) return state;
    if (state.unsaved || state.save === "saving") {
      if (state.draftId !== action.draftId) {
        return { ...state, decision: "discardUnsaved", pendingSwitchTo: action.draftId };
      }
      // A same-draft refresh must not restart the live session: it would discard dirty work on the
      // next success and invalidate an in-flight save. Only an explicit discard/reconcile recovers.
      return state;
    }
    const sameSession = sameDraft(state, action.draftId);
    return {
      draftId: action.draftId,
      epoch: state.epoch + 1,
      load: "loading",
      loadFailure: null,
      baseline: sameSession ? state.baseline : null,
      edits: sameSession ? state.edits : {},
      save: sameSession ? state.save : "clean",
      saveFailure: sameSession ? state.saveFailure : null,
      saveRequestId: 0,
      localDirty: sameSession ? state.localDirty : false,
      unsaved: sameSession ? state.unsaved : false,
      decision: "none",
      pendingSwitchTo: null,
      commitUncertain: state.commitUncertain,
      reconcile: "idle",
      reconcileFailure: null,
    };
  }

  if (!sameDraft(state, action.draftId)) return state;

  switch (action.type) {
    case "loadSucceeded":
      if (action.epoch !== state.epoch || state.load !== "loading") return state;
      return {
        ...state,
        load: "loaded",
        loadFailure: null,
        baseline: action.document,
        edits: {},
        save: "clean",
        saveFailure: null,
        saveRequestId: 0,
        localDirty: false,
        unsaved: false,
        commitUncertain: false,
        reconcile: "idle",
        reconcileFailure: null,
      };
    case "loadFailed": {
      if (action.epoch !== state.epoch || state.load !== "loading") return state;
      const load =
        action.failure.kind === "denied" ? "denied" : action.failure.kind === "notFound" ? "notfound" : "error";
      return { ...state, load, loadFailure: action.failure, baseline: null };
    }
    case "edited": {
      if (state.load !== "loaded" || state.save === "saving" || state.reconcile === "pending") return state;
      // A local edit never clears latched uncertainty or pretends the previous PUT failed.
      const localDirty = action.localDirty === true;
      return {
        ...state,
        edits: action.edits,
        localDirty,
        unsaved: isDirty(action.edits) || localDirty,
        save: state.commitUncertain ? "outcomeUnknown" : "clean",
        saveFailure: state.commitUncertain ? state.saveFailure : null,
      };
    }
    case "saveStarted":
      if (action.epoch !== state.epoch) return state;
      // Only a serializable patch can be sent: raw-only (invalid) work is blocked here too.
      if (state.load !== "loaded" || !isDirty(state.edits) || state.commitUncertain) return state;
      if (state.save === "saving") return state; // duplicate Apply is gated, not queued
      return { ...state, save: "saving", saveRequestId: action.requestId, saveFailure: null };
    case "saveSucceeded":
      if (action.epoch !== state.epoch) return state;
      if (action.requestId !== state.saveRequestId || state.save !== "saving") return state;
      return {
        ...state,
        baseline: action.document,
        edits: {},
        localDirty: false,
        unsaved: false,
        save: "clean",
        saveFailure: null,
        saveRequestId: 0,
        commitUncertain: false,
      };
    case "saveFailed": {
      if (action.epoch !== state.epoch) return state;
      if (action.requestId !== state.saveRequestId || state.save !== "saving") return state;
      const uncertain = action.failure.kind === "outcomeUnknown";
      return {
        ...state,
        save: saveFailureStatus(action.failure),
        saveFailure: action.failure,
        saveRequestId: 0,
        commitUncertain: uncertain || state.commitUncertain,
      };
    }
    case "reconcileStarted":
      if (!state.commitUncertain || state.baseline === null) return state;
      if (action.epoch !== state.epoch + 1) return state;
      return { ...state, epoch: action.epoch, reconcile: "pending", reconcileFailure: null };
    case "reconcileSucceeded":
      if (action.epoch !== state.epoch || state.reconcile !== "pending") return state;
      return {
        ...state,
        reconcile: "idle",
        reconcileFailure: null,
        baseline: action.document,
        commitUncertain: false,
        save: "clean",
        saveFailure: null,
        saveRequestId: 0,
        localDirty: state.localDirty,
        unsaved: isDirty(state.edits) || state.localDirty,
      };
    case "reconcileFailed":
      if (action.epoch !== state.epoch || state.reconcile !== "pending") return state;
      return { ...state, reconcile: "idle", reconcileFailure: action.failure };
    case "switchRequested":
      if (action.nextDraftId === state.draftId) return state;
      if (state.unsaved || state.save === "saving" || state.commitUncertain) {
        return { ...state, decision: "discardUnsaved", pendingSwitchTo: action.nextDraftId };
      }
      return switchedState(state, action.nextDraftId);
    case "closeRequested":
      return state.unsaved || state.commitUncertain
        ? { ...state, decision: "discardUnsaved" }
        : { ...state, decision: "none" };
    case "discardCancelled":
      return { ...state, decision: "none", pendingSwitchTo: null };
    case "discardConfirmed": {
      if (state.pendingSwitchTo !== null) return switchedState(state, state.pendingSwitchTo);
      // Same-draft discard: the old baseline must not become resubmittable while a commit is unknown.
      return {
        draftId: state.draftId,
        epoch: state.epoch + 1,
        load: "idle",
        loadFailure: null,
        baseline: null,
        edits: {},
        localDirty: false,
        save: state.commitUncertain ? "outcomeUnknown" : "clean",
        saveFailure: null,
        saveRequestId: 0,
        unsaved: false,
        decision: "none",
        pendingSwitchTo: null,
        commitUncertain: state.commitUncertain,
        reconcile: "idle",
        reconcileFailure: null,
      };
    }
    default:
      return state;
  }
}

/** A save the caller must perform, or the reason Apply is currently blocked. */
export type EditorApplyRequest =
  | {
      readonly status: "ready";
      readonly requestId: number;
      readonly expectedRevision: number;
      readonly body: EditorRequestBody;
    }
  | {
      readonly status: "blocked";
      readonly reason: "not-loaded" | "no-changes" | "in-flight" | "needs-resolution" | "invalid-intent" | "uncertain";
    };

/**
 * Builds the single Apply request from the confirmed baseline + local edits.
 *
 * @param state Current session.
 * @param requestId Monotonic id the caller assigns to this attempt.
 * @param resolver Authoritative Geography ancestry resolution.
 */
export function selectApplyRequest(
  state: EditorSessionState,
  requestId: number,
  resolver: AreaAncestryResolver,
): EditorApplyRequest {
  if (state.load !== "loaded" || state.baseline === null) return { status: "blocked", reason: "not-loaded" };
  if (state.commitUncertain) return { status: "blocked", reason: "uncertain" };
  if (state.reconcile === "pending") return { status: "blocked", reason: "in-flight" };
  if (state.save === "saving") return { status: "blocked", reason: "in-flight" };
  if (!isDirty(state.edits)) return { status: "blocked", reason: "no-changes" };
  const body = buildEditorDocumentBody(state.baseline, state.edits, resolver);
  if (body.status === "needsResolution") return { status: "blocked", reason: "needs-resolution" };
  if (body.status === "invalidIntent") return { status: "blocked", reason: "invalid-intent" };
  return { status: "ready", requestId, expectedRevision: state.baseline.revision, body };
}

/**
 * True while the session holds work that a close/switch would lose.
 *
 * Includes an unresolved commit (`commitUncertain`) and an in-flight save: uncertainty must not look
 * like "nothing at risk" merely because the editable values are currently empty.
 */
export function hasUnsavedWork(state: EditorSessionState): boolean {
  return state.unsaved || state.commitUncertain || state.save === "saving";
}
