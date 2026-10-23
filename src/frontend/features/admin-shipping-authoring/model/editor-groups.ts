/**
 * Cohesive local GROUP-COLLECTION draft of the EXISTING authoring draft.
 *
 * One model owns the ordered group list, each group's destination selections and its
 * selected-language title patches; rate ROWS keep living in `RatesFormState` keyed by the SAME local
 * group key (an existing group's key is its server `groupId`), so rates are not a competing grouping
 * store. The baseline document is never mutated: retained groups/rows/selections keep their exact
 * server ids, a new group is a client-local key with `groupId: null` and no currency, and a title
 * pair is only sent when the operator actually changed it.
 */
import type { EditorDocument, EditorRegionGroup, SelectedAreaRead } from "./editor-document.ts";
import type { GroupTitleEdit, RateRowEdit, RegionGroupEdit } from "./editor-edits.ts";
import type { RateRowDraft, RatesFormState } from "./editor-rates.ts";
import { RATE_LIMITS, draftFromRow, rowIssue } from "./editor-rates.ts";

/** Domain limits mirrored from `ShippingAuthoringDraft` (no new policy invented). */
export const GROUP_LIMITS = {
  maxGroups: 50,
  maxSelectionsPerGroup: 200,
  maxSelectionsPerDraft: 500,
  titleMaxLength: 200,
} as const;

/** One selected-language title of a group. `""` after an operator clear means "clear this pair". */
export interface GroupTitleDraft {
  readonly languageId: string;
  readonly title: string;
}

/** One group of the local ordered collection. */
export interface GroupDraft {
  /** Client-local identity; `groupId` for existing groups, a session key for new ones. Never sent. */
  readonly key: string;
  readonly groupId: string | null;
  /** Server-owned currency of an existing group; `null` for a new group (never fabricated). */
  readonly currencyCode: string | null;
  /** Selections as currently proposed (retained + deliberately added, minus deliberately removed). */
  readonly areas: readonly SelectedAreaRead[];
  /** Titles of the ACTIVE configured languages only, in registry order. */
  readonly titles: readonly GroupTitleDraft[];
}

export type GroupsFormState = readonly GroupDraft[];

/** Stable local reason the group collection cannot be serialized (the screen localizes it). */
export type GroupIssueCode =
  | "groupLimitExceeded"
  | "selectionLimitExceeded"
  | "duplicateSelection"
  | "titleTooLong";

export interface GroupIssue {
  readonly groupKey: string;
  readonly code: GroupIssueCode;
}

function areaKey(area: SelectedAreaRead): string {
  return `${area.kind}:${area.id.toLowerCase()}`;
}

/** Selected-language title drafts of one stored group (missing pairs are simply absent). */
function titleDrafts(group: EditorRegionGroup, languageIds: readonly string[]): readonly GroupTitleDraft[] {
  return languageIds.map((languageId) => ({
    languageId,
    title: group.titles.find((entry) => entry.languageId === languageId)?.title ?? "",
  }));
}

/**
 * Seeds the ordered collection from the confirmed baseline document.
 *
 * @param document The validated editor document.
 * @param activeLanguageIds Content language ids of the registry that are active now, in registry order.
 */
export function createGroupsForm(
  document: EditorDocument,
  activeLanguageIds: readonly string[],
): GroupsFormState {
  return document.regionGroups.map((group): GroupDraft => ({
    key: group.groupId,
    groupId: group.groupId,
    currencyCode: group.currencyCode,
    areas: group.areas,
    titles: titleDrafts(group, activeLanguageIds),
  }));
}

/** Adds one EMPTY new group in place. No currency, no fabricated id. */
export function withAddedGroup(state: GroupsFormState, key: string): GroupsFormState {
  if (state.length >= GROUP_LIMITS.maxGroups) return state;
  return [...state, { key, groupId: null, currencyCode: null, areas: [], titles: [] }];
}

/** Deliberately removes one group (its rows go with it). Other groups' drafts stay untouched. */
export function withRemovedGroup(state: GroupsFormState, key: string): GroupsFormState {
  return state.filter((group) => group.key !== key);
}

/** Moves one group one step up (-1) / down (+1) inside the local order. */
export function withMovedGroup(state: GroupsFormState, key: string, delta: -1 | 1): GroupsFormState {
  const index = state.findIndex((group) => group.key === key);
  if (index < 0) return state;
  const target = index + delta;
  if (target < 0 || target >= state.length) return state;
  const next = [...state];
  const [moved] = next.splice(index, 1);
  next.splice(target, 0, moved!);
  return next;
}

/** A new group gets the configured title slots of the active languages (empty text). */
export function withGroupTitleSlots(
  state: GroupsFormState,
  key: string,
  activeLanguageIds: readonly string[],
): GroupsFormState {
  return state.map((group) => {
    if (group.key !== key || group.titles.length > 0) return group;
    return { ...group, titles: activeLanguageIds.map((languageId) => ({ languageId, title: "" })) };
  });
}

/** Replaces the text of exactly one (group, language) title. */
export function withGroupTitle(
  state: GroupsFormState,
  key: string,
  languageId: string,
  title: string,
): GroupsFormState {
  return state.map((group) => {
    if (group.key !== key) return group;
    const known = group.titles.some((entry) => entry.languageId === languageId);
    const titles = known
      ? group.titles.map((entry) => (entry.languageId === languageId ? { ...entry, title } : entry))
      : [...group.titles, { languageId, title }];
    return { ...group, titles };
  });
}

/** Appends one verified terminal selection unless that exact `(kind, id)` already exists anywhere. */
export function withGroupAreaAdded(
  state: GroupsFormState,
  key: string,
  area: SelectedAreaRead,
): GroupsFormState {
  const exists = state.some((group) => group.areas.some((entry) => areaKey(entry) === areaKey(area)));
  if (exists) return state;
  return state.map((group) =>
    group.key === key ? { ...group, areas: [...group.areas, { kind: area.kind, id: area.id }] } : group,
  );
}

/** Removes exactly one selection of one group. */
export function withGroupAreaRemoved(
  state: GroupsFormState,
  key: string,
  area: SelectedAreaRead,
): GroupsFormState {
  return state.map((group) =>
    group.key === key
      ? { ...group, areas: group.areas.filter((entry) => areaKey(entry) !== areaKey(area)) }
      : group,
  );
}

function selectionTotal(state: GroupsFormState): number {
  return state.reduce((total, group) => total + group.areas.length, 0);
}

/** Every blocking issue of the proposed collection, in local order. */
export function groupIssues(state: GroupsFormState): readonly GroupIssue[] {
  const issues: GroupIssue[] = [];
  if (state.length > GROUP_LIMITS.maxGroups) {
    issues.push({ groupKey: state[GROUP_LIMITS.maxGroups]!.key, code: "groupLimitExceeded" });
  }
  if (selectionTotal(state) > GROUP_LIMITS.maxSelectionsPerDraft) {
    issues.push({ groupKey: state[0]?.key ?? "", code: "selectionLimitExceeded" });
  }
  const seen = new Set<string>();
  for (const group of state) {
    if (group.areas.length > GROUP_LIMITS.maxSelectionsPerGroup) {
      issues.push({ groupKey: group.key, code: "selectionLimitExceeded" });
    }
    for (const area of group.areas) {
      const key = areaKey(area);
      if (seen.has(key)) issues.push({ groupKey: group.key, code: "duplicateSelection" });
      seen.add(key);
    }
    for (const title of group.titles) {
      if (title.title.trim().length > GROUP_LIMITS.titleMaxLength) {
        issues.push({ groupKey: group.key, code: "titleTooLong" });
      }
    }
  }
  return issues;
}

function sameAreas(left: readonly SelectedAreaRead[], right: readonly SelectedAreaRead[]): boolean {
  return left.length === right.length && left.every((area, index) => areaKey(area) === areaKey(right[index]!));
}

/** Touched title pairs of one group: `[]` untouched, `title: null` clears exactly that pair. */
function titlePatches(group: GroupDraft, baseline: EditorRegionGroup | null): readonly GroupTitleEdit[] {
  const patches: GroupTitleEdit[] = [];
  for (const entry of group.titles) {
    const stored = baseline?.titles.find((title) => title.languageId === entry.languageId)?.title ?? null;
    const trimmed = entry.title.trim();
    const next = trimmed.length === 0 ? null : trimmed;
    if (next === stored) continue;
    if (next === null && stored === null) continue;
    patches.push({ languageId: entry.languageId, title: next });
  }
  return patches;
}

/** True when the proposed collection differs from the baseline topology/areas/titles. */
export function groupsDirty(document: EditorDocument, state: GroupsFormState): boolean {
  const byId = new Map(document.regionGroups.map((group) => [group.groupId, group]));
  const ids = document.regionGroups.map((group) => group.groupId);
  const localIds = state.map((group) => group.groupId);
  if (ids.length !== state.length) return true;
  if (ids.some((id, index) => id !== localIds[index])) return true;
  return state.some((group) => {
    const baseline = group.groupId === null ? null : (byId.get(group.groupId) ?? null);
    if (baseline !== null && !sameAreas(group.areas, baseline.areas)) return true;
    return titlePatches(group, baseline).length > 0;
  });
}

function rowsEdit(rows: readonly RateRowDraft[] | undefined, baseline: EditorRegionGroup | null): readonly RateRowEdit[] {
  if (rows === undefined) return (baseline?.rows ?? []).map((row) => toRateEdit(draftFromRow(row)));
  return rows.map(toRateEdit);
}

function toRateEdit(row: RateRowDraft): RateRowEdit {
  return {
    rowKey: row.rowKey,
    rateRowId: row.rateRowId,
    minWeightGrams: row.minWeightGrams === "" ? null : Number(row.minWeightGrams),
    maxWeightGrams: row.maxWeightGrams === "" ? null : Number(row.maxWeightGrams),
    minItemCount: row.minItemCount === "" ? null : Number(row.minItemCount),
    maxItemCount: row.maxItemCount === "" ? null : Number(row.maxItemCount),
    minMerchandiseAmount: row.minMerchandiseAmount === "" ? null : row.minMerchandiseAmount,
    maxMerchandiseAmount: row.maxMerchandiseAmount === "" ? null : row.maxMerchandiseAmount,
    freightAmount: row.freightAmount === "" ? null : row.freightAmount,
    packagingAmount: row.packagingAmount === "" ? null : row.packagingAmount,
    minDeliveryDays: row.minDeliveryDays === "" ? null : Number(row.minDeliveryDays),
    maxDeliveryDays: row.maxDeliveryDays === "" ? null : Number(row.maxDeliveryDays),
  };
}

/**
 * Builds the ordered `regionGroups` replacement of the ONE atomic PUT.
 *
 * `undefined` means PRESERVE, so a General/week-only edit never rewrites the tree. When anything in
 * the collection (order, add/remove, areas, titles) or in its rate rows changed, the FULL ordered
 * tree is emitted: retained groups keep exact ids and exact untouched rows, new groups carry
 * `groupId: null` and no local key, and only touched title pairs are patched.
 */
export function buildGroupsEdits(
  document: EditorDocument,
  groups: GroupsFormState,
  rates: RatesFormState,
): readonly RegionGroupEdit[] | undefined {
  const byId = new Map(document.regionGroups.map((group) => [group.groupId, group]));
  if (!groupsDirty(document, groups) && !anyRatesDirty(document, groups, rates)) return undefined;
  return groups.map((group): RegionGroupEdit => {
    const baseline = group.groupId === null ? null : (byId.get(group.groupId) ?? null);
    return {
      groupKey: group.key,
      groupId: group.groupId,
      areas: group.areas,
      rows: rowsEdit(rates[group.key], baseline),
      titles: titlePatches(group, baseline),
    };
  });
}

function anyRatesDirty(document: EditorDocument, groups: GroupsFormState, rates: RatesFormState): boolean {
  return groups.some((group) => {
    const rows = rates[group.key];
    if (rows === undefined) return false;
    const baseline = group.groupId === null ? null : document.regionGroups.find((entry) => entry.groupId === group.groupId);
    if (baseline === null || baseline === undefined) return rows.length > 0;
    if (rows.length !== baseline.rows.length) return true;
    return rows.some((row, index) => rowDiffers(baseline.rows[index]!, row));
  });
}

function rowDiffers(stored: EditorRegionGroup["rows"][number], row: RateRowDraft): boolean {
  const expected = draftFromRow(stored);
  return (Object.keys(expected) as (keyof RateRowDraft)[]).some((field) => expected[field] !== row[field]);
}

/** Every local rate-row issue of the collection, in local group order. */
export function groupRateIssues(
  groups: GroupsFormState,
  rates: RatesFormState,
): readonly { readonly groupKey: string; readonly rowKey: string; readonly code: NonNullable<ReturnType<typeof rowIssue>> }[] {
  const issues: { groupKey: string; rowKey: string; code: NonNullable<ReturnType<typeof rowIssue>> }[] = [];
  for (const group of groups) {
    const rows = rates[group.key] ?? [];
    if (rows.length > RATE_LIMITS.maxRowsPerGroup) {
      issues.push({ groupKey: group.key, rowKey: rows[RATE_LIMITS.maxRowsPerGroup]!.rowKey, code: "rowLimitExceeded" });
    }
    for (const row of rows) {
      const code = rowIssue(row);
      if (code) issues.push({ groupKey: group.key, rowKey: row.rowKey, code });
    }
  }
  return issues;
}
