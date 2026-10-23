/**
 * ONE composed editor intent: General + rate rows + weekly schedule → a single `EditorEdits`.
 *
 * The screen keeps local drafts per capability but the controller has exactly one session authority
 * and receives ONE merged patch, so changing General after a rate/week edit (or the reverse) can
 * never drop the other pending capability. Absent wrappers mean PRESERVE, so untouched capabilities
 * are never echoed into the atomic PUT.
 */
import type { EditorDocument } from "./editor-document.ts";
import type { EditorEdits } from "./editor-edits.ts";
import { buildEditorEdits, formHasLocalChanges, type EditorFormIssue, type EditorFormState } from "./editor-form.ts";
import { buildRegionGroupsEdits, rateIssues, ratesDirty, type RateRowIssue, type RatesFormState } from "./editor-rates.ts";
import { buildWeeklyScheduleEdit, weekDirty, weekIssues, type WeekFormState, type WeekIssue } from "./editor-week.ts";

/** All local blocking problems of the composed intent. */
export interface AuthoringIntentIssues {
  readonly general: readonly EditorFormIssue[];
  readonly rates: readonly RateRowIssue[];
  readonly week: readonly WeekIssue[];
}

/** One composed result: the single patch plus everything that blocks Apply. */
export interface AuthoringIntent {
  readonly edits: EditorEdits;
  readonly issues: AuthoringIntentIssues;
  /** Raw local work of ANY capability (valid or not) — the session dirty signal. */
  readonly localDirty: boolean;
}

/** Nothing to block on. */
export function hasNoIssues(issues: AuthoringIntentIssues): boolean {
  return issues.general.length === 0 && issues.rates.length === 0 && issues.week.length === 0;
}

/**
 * Composes the one atomic intent from the confirmed baseline and the three local drafts.
 *
 * @param document Confirmed server baseline.
 * @param general Local General form.
 * @param rates Local rows of the existing groups.
 * @param week Local weekly schedule.
 */
export function buildAuthoringIntent(
  document: EditorDocument,
  general: EditorFormState,
  rates: RatesFormState,
  week: WeekFormState,
): AuthoringIntent {
  const generalResult = buildEditorEdits(document, general);
  const regionGroups = buildRegionGroupsEdits(document, rates);
  const weeklySchedule = buildWeeklyScheduleEdit(document.weeklySchedule, week);

  const edits: EditorEdits = {
    ...generalResult.edits,
    ...(regionGroups === undefined ? {} : { regionGroups }),
    ...(weeklySchedule === undefined ? {} : { weeklySchedule }),
  };

  return {
    edits,
    issues: {
      general: generalResult.issues,
      rates: rateIssues(document, rates),
      week: weekIssues(week),
    },
    localDirty:
      formHasLocalChanges(document, general) || ratesDirty(document, rates) || weekDirty(document.weeklySchedule, week),
  };
}

/** True when the composed patch actually carries a capability for the server. */
export function intentHasEdits(edits: EditorEdits): boolean {
  return (
    edits.methodTranslations !== undefined ||
    edits.methodSettings !== undefined ||
    edits.typeAssignment !== undefined ||
    edits.regionGroups !== undefined ||
    edits.weeklySchedule !== undefined
  );
}
