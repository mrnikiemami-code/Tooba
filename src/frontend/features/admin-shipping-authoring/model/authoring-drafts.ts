/**
 * ONE cohesive reseed of every local authoring draft from the SAME confirmed baseline.
 *
 * The screen seeds the drafts on an accepted load/save and, on a confirmed discard, must restore the
 * WHOLE document — not only the General form — otherwise discarded rate rows, group topology,
 * title patches or destination chips stay on screen and can be resurrected into the next atomic PUT.
 * Keeping that operation in one function makes both paths provably identical.
 */
import type { EditorDocument } from "./editor-document.ts";
import type { EditorFormState } from "./editor-form.ts";
import type { GroupsFormState } from "./editor-groups.ts";
import type { RatesFormState } from "./editor-rates.ts";
import type { WeekFormState } from "./editor-week.ts";
import { createEditorForm } from "./editor-form.ts";
import { createGroupsForm } from "./editor-groups.ts";
import { createRatesForm } from "./editor-rates.ts";
import { createWeekForm } from "./editor-week.ts";

/** All local drafts of one confirmed document. */
export interface AuthoringDrafts {
  readonly form: EditorFormState;
  readonly groups: GroupsFormState;
  readonly rates: RatesFormState;
  readonly week: WeekFormState;
}

/**
 * Seeds General, the ordered group collection (areas + titles), rate rows and the weekly schedule
 * from the SAME confirmed baseline.
 *
 * @param document Confirmed document.
 * @param activeLanguageIds Active content language ids of the registry, in registry order.
 */
export function seedAuthoringDrafts(
  document: EditorDocument,
  activeLanguageIds: readonly string[] = [],
): AuthoringDrafts {
  return {
    form: createEditorForm(document),
    groups: createGroupsForm(document, activeLanguageIds),
    rates: createRatesForm(document),
    week: createWeekForm(document.weeklySchedule),
  };
}

/** Explicit discard: every local capability goes back to the confirmed baseline (nothing survives). */
export function resetAuthoringDrafts(
  document: EditorDocument,
  activeLanguageIds: readonly string[] = [],
): AuthoringDrafts {
  return seedAuthoringDrafts(document, activeLanguageIds);
}
