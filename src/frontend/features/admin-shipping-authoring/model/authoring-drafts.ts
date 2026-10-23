/**
 * ONE cohesive reseed of every local authoring draft from the SAME confirmed baseline.
 *
 * The screen seeds the drafts on an accepted load/save and, on a confirmed discard, must restore the
 * WHOLE document — not only the General form — otherwise discarded rate rows or a discarded week stay
 * on screen and can be resurrected into the next atomic PUT. Keeping that operation in one function
 * makes both paths provably identical.
 */
import type { EditorDocument } from "./editor-document.ts";
import type { EditorFormState } from "./editor-form.ts";
import type { RatesFormState } from "./editor-rates.ts";
import type { WeekFormState } from "./editor-week.ts";
import { createEditorForm } from "./editor-form.ts";
import { createRatesForm } from "./editor-rates.ts";
import { createWeekForm } from "./editor-week.ts";

/** All three local drafts of one confirmed document. */
export interface AuthoringDrafts {
  readonly form: EditorFormState;
  readonly rates: RatesFormState;
  readonly week: WeekFormState;
}

/** Seeds General, rate rows and the weekly schedule from the SAME confirmed baseline. */
export function seedAuthoringDrafts(document: EditorDocument): AuthoringDrafts {
  return {
    form: createEditorForm(document),
    rates: createRatesForm(document),
    week: createWeekForm(document.weeklySchedule),
  };
}

/** Explicit discard: every local capability goes back to the confirmed baseline (nothing survives). */
export function resetAuthoringDrafts(document: EditorDocument): AuthoringDrafts {
  return seedAuthoringDrafts(document);
}
