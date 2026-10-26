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
import { moneyHasFraction, moneyPrecisionPolicy } from "./money-text.ts";

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
  | "rialFractionInvalid"
  | "overlapConflict"
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

/**
 * Appends one row that inherits the AREA's own delivery range (never another area's, never a default).
 *
 * Used by the area-level delivery range: a new row of an area with a chosen shared range starts with that
 * range, so adding a row cannot silently create a second, different delivery promise.
 *
 * @param state Local rows.
 * @param groupId The area (group) the row belongs to.
 * @param rowKey Client-local row identity.
 * @param range The area's shared range, or `null` when the area has none to inherit.
 */
export function withAddedRowInheriting(
  state: RatesFormState,
  groupId: string,
  rowKey: string,
  range: { readonly min: string; readonly max: string } | null,
): RatesFormState {
  const current = state[groupId];
  if (current === undefined) return state;
  const row = range === null
    ? newRateRow(rowKey)
    : { ...newRateRow(rowKey), minDeliveryDays: range.min, maxDeliveryDays: range.max };
  return replaceRows(state, groupId, [...current, row]);
}

/** Removes exactly one row; removing the last one leaves `[]` for THAT group only. */
export function withRemovedRow(state: RatesFormState, groupId: string, rowKey: string): RatesFormState {
  const current = state[groupId];
  if (current === undefined) return state;
  return replaceRows(state, groupId, current.filter((row) => row.rowKey !== rowKey));
}

/**
 * Writes ONE confirmed row of the modal back into the area.
 *
 * A row that already exists keeps its position and identity (replace by stable `rowKey`), and a new row is
 * appended; no other row is reordered, dropped or rewritten. Confirmation is LOCAL — the modal never sends a
 * request, only the parent Apply persists.
 *
 * @param state Local rows.
 * @param groupId The area the row belongs to.
 * @param row The confirmed row (already validated by the caller).
 */
export function withConfirmedRow(state: RatesFormState, groupId: string, row: RateRowDraft): RatesFormState {
  const current = state[groupId];
  if (current === undefined) return state;
  const exists = current.some((candidate) => candidate.rowKey === row.rowKey);
  return replaceRows(
    state,
    groupId,
    exists ? current.map((candidate) => (candidate.rowKey === row.rowKey ? row : candidate)) : [...current, row],
  );
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
  /** Set when the token is representable but the currency's customer precision refuses it (IRR). */
  readonly fractional: boolean;
}

/**
 * Money keeps its exact token text; only representable 2-decimal values are lawful.
 *
 * Under the integer-rial policy of `IRR` a NON-zero fraction is refused with its own reason (never rounded,
 * truncated or converted), while an explicit `.00` stays lawful because it is the same whole amount. A
 * fraction that the caller explicitly TOLERATES (a stored IRR amount the operator never touched) keeps its
 * exact text and its numeric value here; only its entry-level acceptance is up to the caller.
 */
function parseAmount(text: string, required: boolean, policy: "integer" | "scaled" = "scaled"): ParsedAmount {
  if (text === "") return { value: null, ok: !required, fractional: false };
  if (!DECIMAL.test(text)) return { value: null, ok: false, fractional: false };
  const [whole, fraction = ""] = text.split(".");
  if (fraction.length > RATE_LIMITS.amountScale) return { value: null, ok: false, fractional: false };
  if (whole.length > 12) return { value: null, ok: false, fractional: false };
  if (Number(text) > RATE_LIMITS.amountMax) return { value: null, ok: false, fractional: false };
  if (policy === "integer" && /[1-9]/.test(fraction)) {
    return { value: text, ok: false, fractional: true };
  }
  return { value: text, ok: true, fractional: false };
}

/** A money reading is acceptable when it is lawful OR its only objection was tolerated by the caller. */
function amountAccepted(parsed: ParsedAmount, field: RateField, tolerated: readonly RateField[]): boolean {
  return parsed.ok || (parsed.fractional && tolerated.includes(field));
}

/** True when the two numeric texts are strict, half-open bounds (`min < max`). */
function orderedPair(min: string, max: string): boolean {
  if (max === "") return true;
  if (min === "") return false; // an upper bound alone is a half-written axis
  return Number(max) > Number(min);
}

/**
 * Validates the SHAPE of ONE row (conditions + the two amounts) and returns its first issue.
 *
 * @param row Local draft row.
 * @param currencyCode Server-owned currency of the area.
 * @param tolerated Money fields whose NON-ZERO fraction is preserved from storage (never rounded); every
 *   other objection of those fields — blank, unsupported text, ordering — is still reported.
 */
export function rowConditionIssue(
  row: RateRowDraft,
  currencyCode?: string | null,
  tolerated: readonly RateField[] = [],
): RateRowIssueCode | null {
  const policy = moneyPrecisionPolicy(currencyCode);
  const weightMin = parseInteger(row.minWeightGrams, 0, RATE_LIMITS.weightGramsMax);
  const weightMax = parseInteger(row.maxWeightGrams, 0, RATE_LIMITS.weightGramsMax);
  const countMin = parseInteger(row.minItemCount, RATE_LIMITS.itemCountMin, RATE_LIMITS.itemCountMax);
  const countMax = parseInteger(row.maxItemCount, RATE_LIMITS.itemCountMin, RATE_LIMITS.itemCountMax);
  const amountMin = parseAmount(row.minMerchandiseAmount, false, policy);
  const amountMax = parseAmount(row.maxMerchandiseAmount, false, policy);

  if (!weightMin.ok || !weightMax.ok || !countMin.ok || !countMax.ok) return "integerInvalid";
  if (!amountAccepted(amountMin, "minMerchandiseAmount", tolerated)) {
    return amountMin.fractional ? "rialFractionInvalid" : "amountInvalid";
  }
  if (!amountAccepted(amountMax, "maxMerchandiseAmount", tolerated)) {
    return amountMax.fractional ? "rialFractionInvalid" : "amountInvalid";
  }
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
  if (row.freightAmount === "" || row.packagingAmount === "") return "amountRequired";
  const freight = parseAmount(row.freightAmount, true, policy);
  const packaging = parseAmount(row.packagingAmount, true, policy);
  if (!amountAccepted(freight, "freightAmount", tolerated)) {
    return freight.fractional ? "rialFractionInvalid" : "amountInvalid";
  }
  if (!amountAccepted(packaging, "packagingAmount", tolerated)) {
    return packaging.fractional ? "rialFractionInvalid" : "amountInvalid";
  }
  return null;
}

/** Validates ONE row against the real contract and returns the first blocking issue (or none). */
export function rowIssue(
  row: RateRowDraft,
  currencyCode?: string | null,
  tolerated: readonly RateField[] = [],
): RateRowIssueCode | null {
  const shape = rowConditionIssue(row, currencyCode, tolerated);
  if (shape !== null) return shape;

  const minDays = parseInteger(row.minDeliveryDays, 0, RATE_LIMITS.daysMax);
  const maxDays = parseInteger(row.maxDeliveryDays, 0, RATE_LIMITS.daysMax);
  if (!minDays.ok || !maxDays.ok) return "durationInvalid";
  if (minDays.value === null || maxDays.value === null) return "durationRequired";
  if (maxDays.value < minDays.value) return "durationInvalid";
  return null;
}

/**
 * The EXACT stored money tokens of one row — the baseline a stored fraction must still be equal to.
 *
 * Kept as its own value so a modal can re-evaluate tolerance against the CURRENT row instead of a list
 * computed when the modal opened (an edited amount must never inherit the tolerance of the stored one).
 */
export interface StoredRateAmounts {
  readonly freightAmount: string;
  readonly packagingAmount: string;
  readonly minMerchandiseAmount: string;
  readonly maxMerchandiseAmount: string;
}

/**
 * Baseline money tokens of a stored row, or `null` for a new/unknown row.
 *
 * @param stored The stored row of the same stable identity, or `null`.
 */
export function storedRateAmounts(stored: EditorRateRow | null): StoredRateAmounts | null {
  if (stored === null) return null;
  return {
    freightAmount: stored.freightAmount,
    packagingAmount: stored.packagingAmount,
    minMerchandiseAmount: stored.minMerchandiseAmount ?? "",
    maxMerchandiseAmount: stored.maxMerchandiseAmount ?? "",
  };
}

/**
 * The money fields whose NON-ZERO fraction is preserved verbatim from storage.
 *
 * These are the exact fields validation must tolerate (never round): only a field the operator has actually
 * edited, or one of a NEW row, may be refused as a fractional rial amount. The comparison is against the
 * EXACT stored token, so editing `100.50` into `200.75` drops the tolerance of that field.
 *
 * @param row The local draft.
 * @param stored The stored baseline tokens, or `null` for a new row.
 */
export function toleratedStoredFractions(
  row: RateRowDraft,
  stored: StoredRateAmounts | null,
): readonly RateField[] {
  if (stored === null) return [];
  const pairs: readonly (readonly [RateField, string, string])[] = [
    ["freightAmount", row.freightAmount, stored.freightAmount],
    ["packagingAmount", row.packagingAmount, stored.packagingAmount],
    ["minMerchandiseAmount", row.minMerchandiseAmount, stored.minMerchandiseAmount],
    ["maxMerchandiseAmount", row.maxMerchandiseAmount, stored.maxMerchandiseAmount],
  ];
  const tolerated: RateField[] = [];
  for (const [field, draft, baseline] of pairs) {
    if (moneyHasFraction(draft) && draft === baseline) tolerated.push(field);
  }
  return tolerated;
}

/** One refusal reason of a row modal's confirm: an owning field issue, a same-area duplicate/overlap, or a
 * view/busy state that changed while the modal was open (commit is then refused silently). */
export type RateConfirmRefusal =
  | { readonly kind: "shape"; readonly code: RateRowIssueCode }
  | { readonly kind: "duplicate" }
  | { readonly kind: "overlap" }
  | { readonly kind: "blocked" };

/**
 * ONE confirm decision of the rate modal, on the CURRENT draft.
 *
 * Tolerance is re-derived here from the current values against the EXACT stored tokens, so an amount the
 * operator edited into a non-zero fraction can never inherit the tolerance of the stored one; self is
 * excluded by stable row key (never by index) and the duplicate/overlap scope stays the SAME area.
 *
 * @param row The modal's current draft.
 * @param otherRows EVERY row of the same area, including the one being edited.
 * @param currencyCode Server-owned currency of the area.
 * @param stored Exact stored money tokens of this row, or `null` for a new row.
 * @param editable Whether the screen may still be edited (the disabled button is presentation only).
 */
export function rateConfirmIssue(
  row: RateRowDraft,
  otherRows: readonly RateRowDraft[],
  currencyCode: string | null,
  stored: StoredRateAmounts | null,
  editable = true,
): RateConfirmRefusal | null {
  if (!editable) return { kind: "blocked" };
  const shape = rowConditionIssue(row, currencyCode, toleratedStoredFractions(row, stored));
  if (shape !== null) return { kind: "shape", code: shape };
  const tuple = conditionTuple(row, currencyCode);
  const duplicate = otherRows.some(
    (other) => other.rowKey !== row.rowKey && tuple !== null && conditionTuple(other, currencyCode) === tuple,
  );
  if (duplicate) return { kind: "duplicate" };
  if (conflictingRowKey(row, otherRows, currencyCode) !== null) return { kind: "overlap" };
  return null;
}

/** True when the three money texts of a row are exactly the stored ones (an untouched amount). */
export function moneyUnchangedFromStored(row: RateRowDraft, stored: EditorRateRow): boolean {
  return (
    row.freightAmount === stored.freightAmount &&
    row.packagingAmount === stored.packagingAmount &&
    row.minMerchandiseAmount === (stored.minMerchandiseAmount ?? "") &&
    row.maxMerchandiseAmount === (stored.maxMerchandiseAmount ?? "")
  );
}

/** One step of an axis interval: `null` bounds are unbounded, exactly as the domain reads them. */
interface AxisInterval {
  readonly min: number | null;
  readonly max: number | null;
}

/** The three condition axes of one row, or `null` when the row's own shape is not lawful. */
function rowAxes(row: RateRowDraft, policy: "integer" | "scaled"): readonly AxisInterval[] | null {
  const weightMin = parseInteger(row.minWeightGrams, 0, RATE_LIMITS.weightGramsMax);
  const weightMax = parseInteger(row.maxWeightGrams, 0, RATE_LIMITS.weightGramsMax);
  const countMin = parseInteger(row.minItemCount, RATE_LIMITS.itemCountMin, RATE_LIMITS.itemCountMax);
  const countMax = parseInteger(row.maxItemCount, RATE_LIMITS.itemCountMin, RATE_LIMITS.itemCountMax);
  const amountMin = parseAmount(row.minMerchandiseAmount, false, policy);
  const amountMax = parseAmount(row.maxMerchandiseAmount, false, policy);
  if (!weightMin.ok || !weightMax.ok || !countMin.ok || !countMax.ok) return null;
  if (!amountMin.ok || !amountMax.ok) return null;
  return [
    { min: weightMin.value, max: weightMax.value },
    { min: countMin.value, max: countMax.value },
    { min: amountMin.value === null ? null : Number(amountMin.value), max: amountMax.value === null ? null : Number(amountMax.value) },
  ];
}

/** Half-open `[min, max)` overlap of one axis; touching endpoints (`[1000,2000)` and `[2000,3000)`) do not. */
function axisOverlaps(left: AxisInterval, right: AxisInterval): boolean {
  const leftEnd = left.max;
  const rightEnd = right.max;
  const leftStart = left.min ?? Number.NEGATIVE_INFINITY;
  const rightStart = right.min ?? Number.NEGATIVE_INFINITY;
  return (leftEnd === null || rightStart < leftEnd) && (rightEnd === null || leftStart < rightEnd);
}

/**
 * Whether two rows of the SAME area describe an ambiguous price: every AND axis of the two rows must
 * overlap for the pair to be ambiguous; an unconditional axis spans the whole domain, exactly like the
 * domain's `ShippingAuthoringRateRowValue.Overlaps`.
 */
export function rowsOverlap(left: RateRowDraft, right: RateRowDraft, currencyCode?: string | null): boolean {
  const policy = moneyPrecisionPolicy(currencyCode);
  const leftAxes = rowAxes(left, policy);
  const rightAxes = rowAxes(right, policy);
  if (leftAxes === null || rightAxes === null) return false;
  return leftAxes.every((axis, index) => axisOverlaps(axis, rightAxes[index]!));
}

/**
 * The condition tuple of one row, in a comparable canonical text (only lawful rows have one).
 *
 * Used to name an EXACT duplicate: the same three axes on two rows is ambiguous even when the prices differ.
 */
export function conditionTuple(row: RateRowDraft, currencyCode?: string | null): string | null {
  const axes = rowAxes(row, moneyPrecisionPolicy(currencyCode));
  if (axes === null) return null;
  return axes.map((axis) => `${axis.min ?? ""}:${axis.max ?? ""}`).join("|");
}

/**
 * The first OTHER row of the same area this candidate would conflict with.
 *
 * The candidate is compared with every other row of the SAME group only: self is excluded by the caller's
 * stable row key (never by index) and rows of another area/owner are never inspected. This mirrors the
 * domain ambiguity rule; it is not a new pricing authority — the server still enforces it at Apply.
 *
 * @param candidate Row being confirmed in the modal.
 * @param othersRows Every row of the same area (including the candidate itself when editing).
 * @param currencyCode Server-owned currency of that area.
 */
export function conflictingRowKey(
  candidate: RateRowDraft,
  othersRows: readonly RateRowDraft[],
  currencyCode?: string | null,
): string | null {
  for (const other of othersRows) {
    if (other.rowKey === candidate.rowKey) continue;
    if (rowsOverlap(candidate, other, currencyCode)) return other.rowKey;
  }
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
      const stored = row.rateRowId === null
        ? null
        : group.rows.find((candidate) => candidate.rateRowId === row.rateRowId) ?? null;
      // A STORED amount with a non-zero fraction is preserved verbatim and never rounded; only the fields
      // the operator actually edited (or a new row's) are refused. Every OTHER objection still surfaces.
      const code = rowIssue(row, group.currencyCode, toleratedStoredFractions(row, storedRateAmounts(stored)));
      if (code !== null) {
        issues.push({ groupId: group.groupId, rowKey: row.rowKey, code });
        continue;
      }
      // Two lawful rows of the same area must not describe the same conditions; the server refuses it.
      if (rows.some((other) => other.rowKey !== row.rowKey && rowsOverlap(row, other, group.currencyCode))) {
        issues.push({ groupId: group.groupId, rowKey: row.rowKey, code: "overlapConflict" });
      }
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
