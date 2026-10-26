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
import { RATE_LIMITS, type RateRowDraft } from "./editor-rates.ts";

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
