/**
 * ONE customer-receiving delivery range per DELIVERY AREA (region group), not per price row.
 *
 * The persisted wire format still repeats `minDeliveryDays`/`maxDeliveryDays` on every rate row, so this
 * module is an AREA-LEVEL view over those row values, not a second store:
 * - when every row of the area carries the same pair, the area HAS that range and nothing is rewritten;
 * - when rows disagree, that is a LEGACY CONFLICT: the operator sees it and the rows keep their values
 *   until a deliberate shared range is chosen (no first/min/max guess, no silent overwrite);
 * - an area without rows can stage a range locally before its first row exists; it is never serialized
 *   as `0/0` and never creates a row by itself.
 *
 * The range is the CUSTOMER RECEIVING duration promised from the order (§29), not a carrier handoff or a
 * seller preparation span. Nothing here converts or reinterprets those days.
 */
import type { EditorRateRow, EditorRegionGroup } from "./editor-document.ts";
import { RATE_LIMITS, draftFromRow, type RateRowDraft, type RatesFormState } from "./editor-rates.ts";

/** One distinct `min..max` pair found on the rows of an area. */
export interface AreaRangePair {
  readonly min: string;
  readonly max: string;
}

/** What the area range truthfully is right now. */
export type AreaRangeView =
  | { readonly kind: "empty" }
  | { readonly kind: "uniform"; readonly min: string; readonly max: string }
  | { readonly kind: "conflict"; readonly pairs: readonly AreaRangePair[] };

/** Stable local reason the shared range cannot be serialized. */
export type AreaRangeIssueCode = "durationRequired" | "durationInvalid";

const INTEGER = /^(?:0|[1-9][0-9]*)$/;

/**
 * The area-level range of the CURRENT rows.
 *
 * @param rows The area's rows in display order.
 */
export function areaRangeOf(rows: readonly RateRowDraft[]): AreaRangeView {
  if (rows.length === 0) return { kind: "empty" };
  const pairs: AreaRangePair[] = [];
  for (const row of rows) {
    const pair = { min: row.minDeliveryDays, max: row.maxDeliveryDays };
    if (!pairs.some((known) => known.min === pair.min && known.max === pair.max)) pairs.push(pair);
  }
  if (pairs.length === 1) return { kind: "uniform", min: pairs[0]!.min, max: pairs[0]!.max };
  return { kind: "conflict", pairs };
}

/** Text the area control shows when no operator text is staged for it. */
export function areaRangeText(range: AreaRangeView): AreaRangePair {
  return range.kind === "uniform" ? { min: range.min, max: range.max } : { min: "", max: "" };
}

/**
 * Validates one candidate shared range against the real domain bounds.
 *
 * @param min The typed minimum working days.
 * @param max The typed maximum working days.
 */
export function areaRangeIssue(min: string, max: string): AreaRangeIssueCode | null {
  if (min === "" || max === "") return "durationRequired";
  if (!INTEGER.test(min) || !INTEGER.test(max)) return "durationInvalid";
  const minValue = Number(min);
  const maxValue = Number(max);
  if (!Number.isSafeInteger(minValue) || !Number.isSafeInteger(maxValue)) return "durationInvalid";
  if (minValue < 0 || maxValue > RATE_LIMITS.daysMax) return "durationInvalid";
  if (maxValue < minValue) return "durationInvalid";
  return null;
}

/**
 * Applies a deliberate shared range to exactly the rows of ONE area.
 *
 * Every other field, identity and order is preserved, and no other area is touched. Rows keep their own
 * values when the pair is not lawful, so nothing is half-written.
 *
 * @param rows The area's rows.
 * @param min Minimum working days (canonical text).
 * @param max Maximum working days (canonical text).
 */
export function applyAreaRange(rows: readonly RateRowDraft[], min: string, max: string): readonly RateRowDraft[] {
  if (areaRangeIssue(min, max) !== null) return rows;
  if (areaRangeOf(rows).kind === "uniform") {
    const current = areaRangeOf(rows);
    if (current.kind === "uniform" && current.min === min && current.max === max) return rows;
  }
  return rows.map((row) => ({ ...row, minDeliveryDays: min, maxDeliveryDays: max }));
}

/**
 * Range a NEW row of this area must inherit: the area's own shared range only.
 *
 * A conflicting area has no shared range to inherit, and a row-less area inherits only an explicitly
 * staged lawful range — never another area's and never a default such as 1..3.
 *
 * @param rows The area's current rows.
 * @param staged Operator text staged for this area while it had no rows.
 */
export function inheritedAreaRange(
  rows: readonly RateRowDraft[],
  staged: AreaRangePair | null,
): AreaRangePair | null {
  const range = areaRangeOf(rows);
  if (range.kind === "uniform") return { min: range.min, max: range.max };
  if (range.kind === "conflict") return null;
  if (staged === null) return null;
  return areaRangeIssue(staged.min, staged.max) === null ? staged : null;
}

/**
 * The operator's OWN typed text of every area, keyed by the SAME stable group key as the rows.
 *
 * This is the form's shared-range state: it is controlled, it survives re-render, it is compared with
 * the CONFIRMED baseline for dirty/issue purposes, and it is reseeded from that same baseline on a
 * cancel/discard. It is not a second persisted store — only the rows are ever serialized.
 */
export type AreaRangeDrafts = Readonly<Record<string, AreaRangePair>>;

/** Stable local reason a typed shared range blocks Apply. */
export type AreaRangeDraftIssueCode = AreaRangeIssueCode | "rowsRequired";

export interface AreaRangeDraftIssue {
  readonly groupKey: string;
  readonly code: AreaRangeDraftIssueCode;
}

/** Text of one area derived from its CURRENT local rows (`""`/`""` unless every row agrees). */
export function areaRangeTextOf(rows: readonly RateRowDraft[]): AreaRangePair {
  return areaRangeText(areaRangeOf(rows));
}

/** Text of one area derived from its STORED rows (same derivation, exact stored tokens). */
export function storedAreaRangeText(rows: readonly EditorRateRow[]): AreaRangePair {
  return areaRangeText(areaRangeOf(rows.map(draftFromRow)));
}

/** The operator text of one area, falling back to the text its rows already carry. */
export function areaRangeDraftOf(
  drafts: AreaRangeDrafts,
  groupKey: string,
  rows: readonly RateRowDraft[],
): AreaRangePair {
  return drafts[groupKey] ?? areaRangeTextOf(rows);
}

/** Seeds the typed text of every stored area from the CONFIRMED baseline rows. */
export function createAreaRangeDrafts(
  document: { readonly regionGroups: readonly EditorRegionGroup[] },
): AreaRangeDrafts {
  const drafts: Record<string, AreaRangePair> = {};
  for (const group of document.regionGroups) drafts[group.groupId] = storedAreaRangeText(group.rows);
  return drafts;
}

/**
 * ONE operator entry of one area: the typed TEXT is kept verbatim and a LAWFUL pair is written to the
 * rows of exactly that area through the same rows state the rest of the editor uses.
 *
 * A partial/invalid pair changes only the text, so the stored row values are never normalized, defaulted
 * or half-written behind the operator's back — and the text still blocks Apply.
 */
export function areaRangeEdit(
  rates: RatesFormState,
  drafts: AreaRangeDrafts,
  groupKey: string,
  min: string,
  max: string,
): { readonly drafts: AreaRangeDrafts; readonly rates: RatesFormState } {
  const rows = rates[groupKey];
  const applied = rows === undefined || rows.length === 0 ? rows : applyAreaRange(rows, min, max);
  return {
    drafts: { ...drafts, [groupKey]: { min, max } },
    rates: applied === undefined || applied === rows ? rates : { ...rates, [groupKey]: applied },
  };
}

/**
 * Every typed shared range that blocks Apply, in draft order.
 *
 * Text the CURRENT rows already carry is the applied state (a legacy conflict or an area with no rows is
 * not an error by itself). An area that no longer exists locally is ignored, so a removed/stale key can
 * never block or leak into another area; text that cannot be persisted without rows is said explicitly.
 */
export function areaRangeDraftIssues(
  drafts: AreaRangeDrafts,
  rowsOf: (groupKey: string) => readonly RateRowDraft[] | undefined,
): readonly AreaRangeDraftIssue[] {
  const issues: AreaRangeDraftIssue[] = [];
  for (const [groupKey, pair] of Object.entries(drafts)) {
    const rows = rowsOf(groupKey);
    if (rows === undefined) continue;
    const carried = areaRangeTextOf(rows);
    if (pair.min === carried.min && pair.max === carried.max) continue;
    const code = areaRangeIssue(pair.min, pair.max);
    if (code !== null) issues.push({ groupKey, code });
    else if (rows.length === 0) issues.push({ groupKey, code: "rowsRequired" });
  }
  return issues;
}

/**
 * True when at least one typed shared range differs from the CONFIRMED baseline text of its area.
 *
 * This is raw operator work (valid or not), so the session knows the form is dirty even when the rows
 * were never touched; a uniform untouched area, a legacy conflict and a row-less area stay clean.
 */
export function areaRangeDraftsDirty(
  document: { readonly regionGroups: readonly EditorRegionGroup[] },
  drafts: AreaRangeDrafts,
  rates: RatesFormState,
): boolean {
  const baseline = new Map(document.regionGroups.map((group) => [group.groupId, storedAreaRangeText(group.rows)]));
  return Object.entries(drafts).some(([groupKey, pair]) => {
    if (rates[groupKey] === undefined) return false; // a removed area is not pending work of its own
    const stored = baseline.get(groupKey);
    return stored === undefined
      ? pair.min !== "" || pair.max !== ""
      : pair.min !== stored.min || pair.max !== stored.max;
  });
}
