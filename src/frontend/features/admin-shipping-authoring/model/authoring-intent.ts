/**
 * ONE composed editor intent: General + group collection (topology/areas/titles + rate rows) + week.
 *
 * The screen keeps local drafts per capability but the controller has exactly one session authority
 * and receives ONE merged patch, so changing General after a rate/week/group edit (or the reverse)
 * can never drop the other pending capability. Absent wrappers mean PRESERVE, so untouched
 * capabilities are never echoed into the atomic PUT.
 */
import type { EditorDocument } from "./editor-document.ts";
import type { EditorEdits } from "./editor-edits.ts";
import { buildEditorEdits, formHasLocalChanges, type EditorFormIssue, type EditorFormState } from "./editor-form.ts";
import { buildGroupsEdits, groupIssues, groupRateIssues, groupsDirty, type GroupIssue, type GroupsFormState } from "./editor-groups.ts";
import { rateIssues as rateIssuesOf, ratesDirty, type RateRowIssue, type RatesFormState } from "./editor-rates.ts";
import { buildWeeklyScheduleEdit, weekDirty, weekIssues, type WeekFormState, type WeekIssue } from "./editor-week.ts";

/** All local blocking problems of the composed intent. */
export interface AuthoringIntentIssues {
  readonly general: readonly EditorFormIssue[];
  readonly groups: readonly GroupIssue[];
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
  return (
    issues.general.length === 0 &&
    issues.groups.length === 0 &&
    issues.rates.length === 0 &&
    issues.week.length === 0
  );
}

/**
 * Composes the one atomic intent from the confirmed baseline and the local drafts.
 *
 * @param document Confirmed server baseline.
 * @param general Local General form.
 * @param rates Local rows keyed by LOCAL group key (an existing group's key is its server id).
 * @param week Local weekly schedule.
 * @param groups Local ordered group collection; absent means the baseline topology (read-only view).
 */
export function buildAuthoringIntent(
  document: EditorDocument,
  general: EditorFormState,
  rates: RatesFormState,
  week: WeekFormState,
  groups: GroupsFormState | null = null,
): AuthoringIntent {
  const generalResult = buildEditorEdits(document, general);
  const regionGroups =
    groups === null
      ? buildGroupsEdits(document, baselineGroups(document), rates)
      : buildGroupsEdits(document, groups, rates);
  const weeklySchedule = buildWeeklyScheduleEdit(document.weeklySchedule, week);

  const edits: EditorEdits = {
    ...generalResult.edits,
    ...(regionGroups === undefined ? {} : { regionGroups }),
    ...(weeklySchedule === undefined ? {} : { weeklySchedule }),
  };

  const rateIssues: readonly RateRowIssue[] =
    groups === null
      ? rateIssuesOf(document, rates)
      : groupRateIssues(groups, rates).map((issue) => ({
          groupId: issue.groupKey,
          rowKey: issue.rowKey,
          code: issue.code,
        }));

  return {
    edits,
    issues: {
      general: generalResult.issues,
      groups: groups === null ? [] : groupIssues(groups),
      rates: rateIssues,
      week: weekIssues(week),
    },
    localDirty:
      formHasLocalChanges(document, general) ||
      ratesDirty(document, rates) ||
      weekDirty(document.weeklySchedule, week) ||
      (groups !== null && groupsDirty(document, groups)),
  };
}

/** Baseline collection as an ordered draft (existing keys/ids only). */
function baselineGroups(document: EditorDocument): GroupsFormState {
  return document.regionGroups.map((group) => ({
    key: group.groupId,
    groupId: group.groupId,
    currencyCode: group.currencyCode,
    areas: group.areas,
    titles: [],
  }));
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
