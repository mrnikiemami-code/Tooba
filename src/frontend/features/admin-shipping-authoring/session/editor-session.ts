/**
 * Local editor session state for ONE existing shipping authoring draft.
 *
 * Pure reducer + selectors: no fetch, no rendering, no global store, no browser dialogs or storage.
 * It keeps the confirmed server baseline apart from the local edit intent, scopes every async result
 * by draft id + generation + save request id so a late response for another/obsolete session cannot
 * overwrite current work, and never discards unsaved edits without an explicit UI decision.
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

/** Mutable-looking but immutable session state. */
export interface EditorSessionState {
  readonly draftId: string;
  readonly generation: number;
  readonly load: EditorLoadStatus;
  readonly loadFailure: EditorTransportFailure | null;
  readonly baseline: EditorDocument | null;
  readonly edits: EditorEdits;
  readonly save: EditorSaveStatus;
  readonly saveFailure: EditorTransportFailure | null;
  readonly saveRequestId: number;
  readonly unsaved: boolean;
  readonly decision: EditorDecision;
  readonly pendingSwitchTo: string | null;
}

/** Actions the editor screen dispatches. */
export type EditorSessionAction =
  | { readonly type: "loadStarted"; readonly draftId: string }
  | { readonly type: "loadSucceeded"; readonly draftId: string; readonly generation: number; readonly document: EditorDocument }
  | {
      readonly type: "loadFailed";
      readonly draftId: string;
      readonly generation: number;
      readonly failure: EditorTransportFailure;
    }
  | { readonly type: "edited"; readonly draftId: string; readonly edits: EditorEdits }
  | { readonly type: "saveStarted"; readonly draftId: string; readonly requestId: number }
  | {
      readonly type: "saveSucceeded";
      readonly draftId: string;
      readonly requestId: number;
      readonly document: EditorDocument;
    }
  | {
      readonly type: "saveFailed";
      readonly draftId: string;
      readonly requestId: number;
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
    generation: 0,
    load: "idle",
    loadFailure: null,
    baseline: null,
    edits: {},
    save: "clean",
    saveFailure: null,
    saveRequestId: 0,
    unsaved: false,
    decision: "none",
    pendingSwitchTo: null,
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

/**
 * Reduces one editor session action.
 *
 * Stale results (other draft, older generation, obsolete save request) return the SAME state object,
 * so a slow load/save can never overwrite current work. Unsaved work is only dropped on an explicit
 * `discardConfirmed`.
 */
export function reduceEditorSession(state: EditorSessionState, action: EditorSessionAction): EditorSessionState {
  if (action.type === "loadStarted") {
    if (state.unsaved || state.save === "saving") {
      // Switching away from dirty work always surfaces the decision first.
      return state.draftId === action.draftId
        ? state
        : { ...state, decision: "discardUnsaved", pendingSwitchTo: action.draftId };
    }
    const sameSession = sameDraft(state, action.draftId);
    return {
      ...createEditorSessionState(action.draftId),
      generation: sameSession ? state.generation + 1 : 1,
      load: "loading",
    };
  }

  if (!sameDraft(state, action.draftId)) return state;

  switch (action.type) {
    case "loadSucceeded":
      if (action.generation !== state.generation || state.load !== "loading") return state;
      return {
        ...state,
        load: "loaded",
        loadFailure: null,
        baseline: action.document,
        edits: {},
        save: "clean",
        saveFailure: null,
        saveRequestId: 0,
        unsaved: false,
      };
    case "loadFailed": {
      if (action.generation !== state.generation || state.load !== "loading") return state;
      const load =
        action.failure.kind === "denied" ? "denied" : action.failure.kind === "notFound" ? "notfound" : "error";
      return { ...state, load, loadFailure: action.failure, baseline: null };
    }
    case "edited":
      if (state.load !== "loaded" || state.save === "saving") return state;
      return { ...state, edits: action.edits, unsaved: isDirty(action.edits), save: "clean", saveFailure: null };
    case "saveStarted":
      if (state.load !== "loaded" || !state.unsaved) return state;
      if (state.save === "saving") return state; // duplicate Apply is gated, not queued
      return { ...state, save: "saving", saveRequestId: action.requestId, saveFailure: null };
    case "saveSucceeded":
      if (action.requestId !== state.saveRequestId || state.save !== "saving") return state;
      return {
        ...state,
        baseline: action.document,
        edits: {},
        unsaved: false,
        save: "clean",
        saveFailure: null,
        saveRequestId: 0,
      };
    case "saveFailed":
      if (action.requestId !== state.saveRequestId || state.save !== "saving") return state;
      return { ...state, save: saveFailureStatus(action.failure), saveFailure: action.failure, saveRequestId: 0 };
    case "switchRequested":
      if (action.nextDraftId === state.draftId) return state;
      return { ...state, decision: "discardUnsaved", pendingSwitchTo: action.nextDraftId };
    case "closeRequested":
      return state.unsaved ? { ...state, decision: "discardUnsaved" } : { ...state, decision: "none" };
    case "discardCancelled":
      return { ...state, decision: "none", pendingSwitchTo: null };
    case "discardConfirmed": {
      if (state.pendingSwitchTo === null) {
        return { ...state, edits: {}, unsaved: false, save: "clean", saveFailure: null, decision: "none" };
      }
      return {
        ...createEditorSessionState(state.pendingSwitchTo),
        generation: 1,
        load: "idle",
      };
    }
    default:
      return state;
  }
}

/** A save the caller must perform, or the reason Apply is currently blocked. */
export type EditorApplyRequest =
  | { readonly status: "ready"; readonly requestId: number; readonly expectedRevision: number; readonly body: EditorRequestBody }
  | { readonly status: "blocked"; readonly reason: "not-loaded" | "no-changes" | "in-flight" | "needs-resolution" };

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
  if (state.save === "saving") return { status: "blocked", reason: "in-flight" };
  if (!state.unsaved) return { status: "blocked", reason: "no-changes" };
  const body = buildEditorDocumentBody(state.baseline.revision, state.edits, resolver);
  if (body.status === "needsResolution") return { status: "blocked", reason: "needs-resolution" };
  return { status: "ready", requestId, expectedRevision: state.baseline.revision, body };
}

/** True while the session has work that a close/switch would lose. */
export function hasUnsavedWork(state: EditorSessionState): boolean {
  return state.unsaved;
}
