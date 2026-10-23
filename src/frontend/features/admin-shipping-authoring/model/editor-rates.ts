/**
 * Local RATE-ROW intent of the EXISTING region groups of one draft.
 *
 * Only the rows under an existing group are editable here: group identity/order/areas/currency and
 * the geography stay read-only, and a group is never created, deleted or reordered. Values are kept
 * as the exact TEXT the operator typed/read (money is never a JS number), so an untouched decimal
 * token such as `1850000.00` is re-sent verbatim and a blank optional bound stays `null`.
 *
 * The limits below are the ones the shipping authoring domain already enforces
 * (`ShippingAuthoringRateRowRules` / `ShippingRatePolicy`): weight `0..1,000,000,000` grams, item
 * count `1..1,000,000`, money `0..1,000,000,000` with at most 2 decimals, delivery days
 * `0..365` (and `max >= min`), at most 200 rows per group. The server remains the final authority;
 * this module only prevents a knowingly invalid/partial body.
 */
import type { EditorRegionGroup, EditorRateRow } from "./editor-document.ts";
import type { RateRowEdit, RegionGroupEdit } from "./editor-edits.ts";

/** Domain limits mirrored from `ShippingAuthoringRateRowRules` (no new policy invented). */
export const RATE_LIMITS = {
  weightGramsMax: 1_000_000_000,
  itemCountMin: 1,
  itemCountMax: 1_000_000,
  amountMax: 1_000_000_000,
  amountScale: 2,
  daysMax: 365,
  maxRowsPerGroup: 200,
} as const;

/** One row exactly as edited in the UI (`""` = blank/absent bound). */
export interface RateRowDraft {
  /** Client-local identity of this row; NEVER serialized as a server id. */
  readonly rowKey: string;
  readonly rateRowId: string | null;
  readonly minWeightGrams: string;
  readonly maxWeightGrams: string;
  readonly minItemCount: string;
  readonly maxItemCount: string;
  readonly minMerchandiseAmount: string;
  readonly maxMerchandiseAmount: string;
  readonly freightAmount: string;
  readonly packagingAmount: string;
  readonly minDeliveryDays: string;
  readonly maxDeliveryDays: string;
}

/** Local rows of one existing group, keyed by the SERVER group id. */
export type RatesFormState = Readonly<Record<string, readonly RateRowDraft[]>>;

/** Stable local reason a row cannot be serialized (the screen localizes it). */
export type RateRowIssueCode =
  | "rowLimitExceeded"
  | "boundIncomplete"
  | "integerInvalid"
  | "boundRangeInvalid"
  | "amountRequired"
  | "amountInvalid"
  | "durationRequired"
  | "durationInvalid";

export interface RateRowIssue {
  readonly groupId: string;
  readonly rowKey: string;
  readonly code: RateRowIssueCode;
}

export type RateField = keyof Omit<RateRowDraft, "rowKey" | "rateRowId">;

const INTEGER = /^(?:0|[1-9][0-9]*)$/;
const DECIMAL = /^(?:0|[1-9][0-9]*)(?:\.[0-9]+)?$/;

function textOf(value: string | number | null | undefined): string {
  return value === null || value === undefined ? "" : String(value);
}

/** One stored row as its editable text draft (exact tokens, no reformatting). */
export function draftFromRow(row: EditorRateRow): RateRowDraft {
  return {
    rowKey: row.rateRowId,
    rateRowId: row.rateRowId,
    minWeightGrams: textOf(row.minWeightGrams),
    maxWeightGrams: textOf(row.maxWeightGrams),
    minItemCount: textOf(row.minItemCount),
    maxItemCount: textOf(row.maxItemCount),
    minMerchandiseAmount: row.minMerchandiseAmount ?? "",
    maxMerchandiseAmount: row.maxMerchandiseAmount ?? "",
    freightAmount: row.freightAmount,
    packagingAmount: row.packagingAmount,
    minDeliveryDays: textOf(row.minDeliveryDays),
    maxDeliveryDays: textOf(row.maxDeliveryDays),
  };
}

/** Initial rows of every existing group, in stored order. */
export function createRatesForm(document: { readonly regionGroups: readonly EditorRegionGroup[] }): RatesFormState {
  const state: Record<string, readonly RateRowDraft[]> = {};
  for (const group of document.regionGroups) state[group.groupId] = group.rows.map(draftFromRow);
  return state;
}

/** New blank row with a client-local key and NO server id. */
export function newRateRow(rowKey: string): RateRowDraft {
  return {
    rowKey,
    rateRowId: null,
    minWeightGrams: "",
    maxWeightGrams: "",
    minItemCount: "",
    maxItemCount: "",
    minMerchandiseAmount: "",
    maxMerchandiseAmount: "",
    freightAmount: "",
    packagingAmount: "",
    minDeliveryDays: "",
    maxDeliveryDays: "",
  };
}

function replaceRows(state: RatesFormState, groupId: string, rows: readonly RateRowDraft[]): RatesFormState {
  return { ...state, [groupId]: rows };
}

/** Appends one blank row to an existing group only. */
export function withAddedRow(state: RatesFormState, groupId: string, rowKey: string): RatesFormState {
  const current = state[groupId];
  if (current === undefined) return state;
  return replaceRows(state, groupId, [...current, newRateRow(rowKey)]);
}

/** Removes exactly one row; removing the last one leaves `[]` for THAT group only. */
export function withRemovedRow(state: RatesFormState, groupId: string, rowKey: string): RatesFormState {
  const current = state[groupId];
  if (current === undefined) return state;
  return replaceRows(state, groupId, current.filter((row) => row.rowKey !== rowKey));
}

/** Replaces one text field of one row (no coercion, no trimming, no rounding). */
export function withRowField(
  state: RatesFormState,
  groupId: string,
  rowKey: string,
  field: RateField,
  value: string,
): RatesFormState {
  const current = state[groupId];
  if (current === undefined) return state;
  return replaceRows(
    state,
    groupId,
    current.map((row) => (row.rowKey === rowKey ? { ...row, [field]: value } : row)),
  );
}

interface ParsedInteger {
  readonly value: number | null;
  readonly ok: boolean;
}

/** Blank stays absent; anything else must be a safe integer inside the real domain. */
function parseInteger(text: string, minimum: number, maximum: number): ParsedInteger {
  if (text === "") return { value: null, ok: true };
  if (!INTEGER.test(text)) return { value: null, ok: false };
  const value = Number(text);
  if (!Number.isSafeInteger(value) || value < minimum || value > maximum) return { value: null, ok: false };
  return { value, ok: true };
}

interface ParsedAmount {
  readonly value: string | null;
  readonly ok: boolean;
}

/** Money keeps its exact token text; only representable 2-decimal values are lawful. */
function parseAmount(text: string, required: boolean): ParsedAmount {
  if (text === "") return { value: null, ok: !required };
  if (!DECIMAL.test(text)) return { value: null, ok: false };
  const [whole, fraction = ""] = text.split(".");
  if (fraction.length > RATE_LIMITS.amountScale) return { value: null, ok: false };
  if (whole.length > 12) return { value: null, ok: false };
  if (Number(text) > RATE_LIMITS.amountMax) return { value: null, ok: false };
  return { value: text, ok: true };
}

/** True when the two numeric texts are strict, half-open bounds (`min < max`). */
function orderedPair(min: string, max: string): boolean {
  if (max === "") return true;
  if (min === "") return false; // an upper bound alone is a half-written axis
  return Number(max) > Number(min);
}

/** Validates ONE row against the real contract and returns the first blocking issue (or none). */
export function rowIssue(row: RateRowDraft): RateRowIssueCode | null {
  const weightMin = parseInteger(row.minWeightGrams, 0, RATE_LIMITS.weightGramsMax);
  const weightMax = parseInteger(row.maxWeightGrams, 0, RATE_LIMITS.weightGramsMax);
  const countMin = parseInteger(row.minItemCount, RATE_LIMITS.itemCountMin, RATE_LIMITS.itemCountMax);
  const countMax = parseInteger(row.maxItemCount, RATE_LIMITS.itemCountMin, RATE_LIMITS.itemCountMax);
  const amountMin = parseAmount(row.minMerchandiseAmount, false);
  const amountMax = parseAmount(row.maxMerchandiseAmount, false);

  if (!weightMin.ok || !weightMax.ok || !countMin.ok || !countMax.ok) return "integerInvalid";
  if (!amountMin.ok || !amountMax.ok) return "amountInvalid";
  if (
    (row.maxWeightGrams !== "" && row.minWeightGrams === "") ||
    (row.maxItemCount !== "" && row.minItemCount === "") ||
    (row.maxMerchandiseAmount !== "" && row.minMerchandiseAmount === "")
  ) {
    return "boundIncomplete";
  }
  if (!orderedPair(row.minWeightGrams, row.maxWeightGrams)) return "boundRangeInvalid";
  if (!orderedPair(row.minItemCount, row.maxItemCount)) return "boundRangeInvalid";
  if (amountMin.value !== null && amountMax.value !== null && Number(amountMax.value) <= Number(amountMin.value)) {
    return "boundRangeInvalid";
  }
  const freight = parseAmount(row.freightAmount, true);
  const packaging = parseAmount(row.packagingAmount, true);
  if (row.freightAmount === "" || row.packagingAmount === "") return "amountRequired";
  if (!freight.ok || !packaging.ok) return "amountInvalid";

  const minDays = parseInteger(row.minDeliveryDays, 0, RATE_LIMITS.daysMax);
  const maxDays = parseInteger(row.maxDeliveryDays, 0, RATE_LIMITS.daysMax);
  if (!minDays.ok || !maxDays.ok) return "durationInvalid";
  if (minDays.value === null || maxDays.value === null) return "durationRequired";
  if (maxDays.value < minDays.value) return "durationInvalid";
  return null;
}

/** Every blocking issue of the whole local rates state, in group/row order. */
export function rateIssues(
  document: { readonly regionGroups: readonly EditorRegionGroup[] },
  state: RatesFormState,
): readonly RateRowIssue[] {
  const issues: RateRowIssue[] = [];
  for (const group of document.regionGroups) {
    const rows = state[group.groupId] ?? [];
    if (rows.length > RATE_LIMITS.maxRowsPerGroup) {
      issues.push({ groupId: group.groupId, rowKey: rows[RATE_LIMITS.maxRowsPerGroup]!.rowKey, code: "rowLimitExceeded" });
    }
    for (const row of rows) {
      const code = rowIssue(row);
      if (code) issues.push({ groupId: group.groupId, rowKey: row.rowKey, code });
    }
  }
  return issues;
}

function sameText(left: string, right: string): boolean {
  return left === right;
}

function rowsEqualDraft(row: EditorRateRow, draft: RateRowDraft): boolean {
  return (
    row.rateRowId === draft.rateRowId &&
    sameText(textOf(row.minWeightGrams), draft.minWeightGrams) &&
    sameText(textOf(row.maxWeightGrams), draft.maxWeightGrams) &&
    sameText(textOf(row.minItemCount), draft.minItemCount) &&
    sameText(textOf(row.maxItemCount), draft.maxItemCount) &&
    sameText(row.minMerchandiseAmount ?? "", draft.minMerchandiseAmount) &&
    sameText(row.maxMerchandiseAmount ?? "", draft.maxMerchandiseAmount) &&
    sameText(row.freightAmount, draft.freightAmount) &&
    sameText(row.packagingAmount, draft.packagingAmount) &&
    sameText(textOf(row.minDeliveryDays), draft.minDeliveryDays) &&
    sameText(textOf(row.maxDeliveryDays), draft.maxDeliveryDays)
  );
}

/** True when at least one group's local rows differ from the confirmed baseline. */
export function ratesDirty(document: { readonly regionGroups: readonly EditorRegionGroup[] }, state: RatesFormState): boolean {
  return document.regionGroups.some((group) => {
    const rows = state[group.groupId];
    if (rows === undefined) return false;
    if (rows.length !== group.rows.length) return true;
    return rows.some((row, index) => !rowsEqualDraft(group.rows[index]!, row));
  });
}

function toEdit(row: RateRowDraft): RateRowEdit {
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
 * Builds the `regionGroups` intent for the ONE atomic PUT.
 *
 * `undefined` means PRESERVE (no group selected for a rate change), so a General-only edit never
 * touches the stored tree. When any group changed, the FULL ordered tree is emitted: untouched
 * groups keep their stored ids/areas/order and their stored rows verbatim, touched groups carry
 * their local rows, and titles stay `[]` (no title changes here). Geography is never rebuilt.
 */
export function buildRegionGroupsEdits(
  document: { readonly regionGroups: readonly EditorRegionGroup[] },
  state: RatesFormState,
): readonly RegionGroupEdit[] | undefined {
  if (!ratesDirty(document, state)) return undefined;
  return document.regionGroups.map((group): RegionGroupEdit => {
    const rows = state[group.groupId];
    const unchanged = rows === undefined || (rows.length === group.rows.length && rows.every((row, index) => rowsEqualDraft(group.rows[index]!, row)));
    return {
      groupKey: group.groupId,
      groupId: group.groupId,
      areas: group.areas,
      rows: unchanged ? group.rows.map((row) => toEdit(draftFromRow(row))) : rows!.map(toEdit),
      titles: [],
    };
  });
}
