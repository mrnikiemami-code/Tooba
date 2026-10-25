/**
 * Local state model of the publication action of ONE draft (no rendering, no transport).
 *
 * The model exists so the panel cannot invent what it does not know:
 * - a pointer version comes ONLY from a successful read or a validated write response;
 * - a definite rejection (403/409/validation) makes the held token unusable (`stale`) and requires a
 *   fresh, explicit read before another write — it is never retried automatically;
 * - a write whose outcome could not be established latches `unknown`, which blocks every further
 *   publication write until the operator explicitly reads the status;
 * - an in-flight write is refused synchronously (`beginWrite` returns `null`), so a double click can
 *   never produce two POSTs even before React re-renders.
 *
 * Nothing here touches the editor document, the form or the draft revision: publication is intent only.
 */
import type { AuthoringMessageKey } from "../messages/authoring-messages.ts";
import type { AuthoringPublicationView, PublicationOutcome } from "../api/authoring-publication-api.ts";

/** Read status of the publication state. */
export type PublicationReadState = "idle" | "loading" | "ok" | "denied" | "notfound" | "unavailable" | "error" | "untrusted";

/** A publication write the operator can ask for. */
export type PublicationWrite = "publish" | "withdraw";

/** Honest phase of the stored pointer relative to the CONFIRMED current revision. */
export type PublicationPhase = "unknown" | "absent" | "cleared" | "current" | "older";

/** Everything the panel knows; every field is truthful, none is inferred from a failure. */
export interface PublicationPanelState {
  readonly read: PublicationReadState;
  readonly view: AuthoringPublicationView | null;
  readonly stale: boolean;
  readonly unknown: boolean;
  readonly pending: PublicationWrite | null;
}

/** The zero state: nothing read yet. */
export const initialPublicationState: PublicationPanelState = {
  read: "idle",
  view: null,
  stale: false,
  unknown: false,
  pending: null,
};

/** Starts a read without discarding the last known view (the read result decides what remains). */
export function beginRead(state: PublicationPanelState): PublicationPanelState {
  if (state.pending !== null) return state;
  return { ...state, read: "loading" };
}

/**
 * Applies a read result. Only a successful read clears `stale`/`unknown`; a failed read keeps them,
 * because a failure proves nothing about the pointer.
 *
 * @param state Current state.
 * @param outcome Transport outcome of the read.
 */
export function resolveRead(state: PublicationPanelState, outcome: PublicationOutcome): PublicationPanelState {
  if (outcome.kind === "ok") {
    return { read: "ok", view: outcome.view, stale: false, unknown: false, pending: null };
  }
  const kind = outcome.failure.kind;
  const read: PublicationReadState =
    kind === "denied"
      ? "denied"
      : kind === "notFound"
        ? "notfound"
        : kind === "untrusted"
          ? "untrusted"
          : kind === "unavailable" || kind === "network" || kind === "outcomeUnknown"
            ? "unavailable"
            : "error";
  return { read, view: null, stale: state.stale, unknown: state.unknown, pending: null };
}

/**
 * Refuses a write unless every precondition of this task holds, and otherwise marks it in flight.
 *
 * @param state Current state.
 * @param action Requested write.
 * @param mode Editor mode; publication is only lawful from clean VIEW.
 * @param busy True while the editor is saving/reconciling.
 * @param currentRevision Confirmed baseline revision, or `null` when it is not known.
 * @returns The in-flight state, or `null` when the write must not be sent.
 */
export function beginWrite(
  state: PublicationPanelState,
  action: PublicationWrite,
  mode: "view" | "edit",
  busy: boolean,
  currentRevision: number | null,
): PublicationPanelState | null {
  if (publicationBlockedReason(state, action, mode, busy, currentRevision) !== null) return null;
  return { ...state, pending: action };
}

/**
 * Why a write is not available right now, as a message key; `null` means it is available.
 *
 * @param state Current state.
 * @param action Requested write.
 * @param mode Editor mode.
 * @param busy Editor in-flight flag.
 * @param currentRevision Confirmed baseline revision.
 */
export function publicationBlockedReason(
  state: PublicationPanelState,
  action: PublicationWrite,
  mode: "view" | "edit",
  busy: boolean,
  currentRevision: number | null,
): AuthoringMessageKey | null {
  if (state.pending !== null) return "publicationInFlight";
  if (state.unknown) return "publicationUnknownBlock";
  if (state.stale) return "publicationStaleBlock";
  if (mode !== "view") return "publicationViewOnly";
  if (busy) return "publicationBusy";
  if (state.read !== "ok" || state.view === null) return "publicationNoState";
  if (action === "publish") {
    if (currentRevision === null) return "publicationNoRevision";
    if (publicationPhase(state.view, currentRevision) === "current") return "publicationAlreadyCurrent";
    return null;
  }
  if (state.view.state !== "selected") return "publicationNothingToWithdraw";
  return null;
}

/** True when the publish action is currently available. */
export function canPublish(
  state: PublicationPanelState,
  mode: "view" | "edit",
  busy: boolean,
  currentRevision: number | null,
): boolean {
  return publicationBlockedReason(state, "publish", mode, busy, currentRevision) === null;
}

/** True when the withdraw action is currently available. */
export function canWithdraw(
  state: PublicationPanelState,
  mode: "view" | "edit",
  busy: boolean,
  currentRevision: number | null,
): boolean {
  return publicationBlockedReason(state, "withdraw", mode, busy, currentRevision) === null;
}

/**
 * Applies a write result. A validated response is adopted as-is (no editor reload, no form reseed).
 *
 * @param state In-flight state.
 * @param outcome Transport outcome of the write.
 */
export function resolveWrite(state: PublicationPanelState, outcome: PublicationOutcome): PublicationPanelState {
  if (outcome.kind === "ok") {
    return { read: "ok", view: outcome.view, stale: false, unknown: false, pending: null };
  }
  const kind = outcome.failure.kind;
  if (kind === "outcomeUnknown") {
    // The write MAY have been applied: keep the last known view but block every further write.
    return { ...state, pending: null, unknown: true };
  }
  if (kind === "denied" || kind === "conflict" || kind === "validation" || kind === "notFound") {
    return { ...state, pending: null, stale: true };
  }
  return { ...state, pending: null, stale: true };
}

/**
 * Phase of the stored selection relative to the confirmed revision.
 *
 * Documented rule: comparing the confirmed baseline revision with the selected source revision is the
 * lawful equality test after a save; the server's own flag is used only when no baseline is known.
 *
 * @param view Decoded publication view.
 * @param currentRevision Confirmed baseline revision, or `null`.
 */
export function publicationPhase(view: AuthoringPublicationView, currentRevision: number | null): PublicationPhase {
  if (view.state === "absent") return "absent";
  if (view.state === "cleared") return "cleared";
  const matches = currentRevision === null
    ? view.selectedMatchesCurrentDraftRevision
    : view.selectedSourceRevision === currentRevision;
  return matches ? "current" : "older";
}

/** Localized summary line of the stored state. */
export function publicationStateMessage(state: PublicationPanelState): AuthoringMessageKey {
  if (state.read === "idle" || state.read === "loading") return "publicationLoading";
  if (state.read === "denied") return "publicationDenied";
  if (state.read === "notfound") return "publicationNotFound";
  if (state.read === "untrusted") return "publicationUntrusted";
  if (state.read !== "ok" || state.view === null) return "publicationUnavailable";
  if (state.stale) return "publicationStale";
  if (state.unknown) return "publicationUnknown";
  switch (state.view.state) {
    case "absent":
      return "publicationAbsent";
    case "cleared":
      return "publicationCleared";
    default:
      return "publicationSelected";
  }
}
