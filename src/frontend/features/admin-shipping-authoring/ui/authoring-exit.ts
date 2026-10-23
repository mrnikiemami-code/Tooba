/**
 * Pure decision of an explicit "Cancel Edit" / "Back" request in the GENERAL editor.
 *
 * No React state lives here: the screen supplies the session facts and executes the returned plan.
 * - `blocked`: an Apply or the reconciliation read is actually in flight; resetting or leaving now
 *   would present obsolete state as success, so the control is inert.
 * - `direct`: nothing is at risk (clean form), the destination can be entered immediately.
 * - `confirm`: local work (including invalid raw work or an unresolved commit) is at risk and the
 *   operator must see the existing discard decision first.
 */
import type { EditorSaveStatus } from "../session/editor-session.ts";

/** Where an explicit exit wants to go: back to the confirmed document view, or out of the screen. */
export type AuthoringExitDestination = "view" | "back";

/** What the screen must do for one exit request. */
export type AuthoringExitPlan = "blocked" | "direct" | "confirm";

/** Session facts an exit request depends on (never the rendered form). */
export interface AuthoringExitInputs {
  readonly save: EditorSaveStatus;
  readonly reconciling: boolean;
  /** Session work that would be lost: `unsaved` (including raw-only) or an uncertain commit. */
  readonly hasLocalWork: boolean;
}

export function planAuthoringExit(inputs: AuthoringExitInputs): AuthoringExitPlan {
  if (inputs.save === "saving" || inputs.reconciling) return "blocked";
  return inputs.hasLocalWork ? "confirm" : "direct";
}
