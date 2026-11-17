/**
 * TB-277-R1: EXACT decimal quantity arithmetic for the physical receiving surface.
 *
 * TB-277 added the unit rule but still compared quantities with JavaScript `Number`: `0.1 + 0.2` is
 * `0.30000000000000004`, so a legitimate 0.1 usable + 0.2 damaged split against a 0.3 ceiling was rejected, and
 * `requested - received` could produce `0.19999999999999998`. Rounding, `toFixed` clamping or an epsilon tolerance is
 * NOT an acceptable fix — the operator typed a number of things, and that number must be compared exactly.
 *
 * Every quantity is therefore carried as an INTEGER number of millionths (`numeric(18,6)` units):
 * `0.3` → `300000`, `1` and `1.000000` → `1000000` (the same value, never a different representation problem).
 * Nine integer digits plus six decimals is at most `999999999999999`, which is far below `Number.MAX_SAFE_INTEGER`
 * (9007199254740991), so the integer representation is exact — no BigInt and no floating-point drift.
 *
 * The persisted scale is a storage cap only. The OWNING UNIT quantum (0 for whole pieces, 3 for grams, …) decides
 * which of those integers a line may actually receive.
 */

/** The persisted scale of every return quantity: `numeric(18,6)` may never be rounded to fit. */
export const MAX_QUANTITY_SCALE = 6;

/** How many integer units make one whole thing at the persisted scale. */
export const QUANTITY_UNITS_PER_ONE = 1_000_000;

/**
 * The accepted shape of an operator-entered quantity: an optional sign-free decimal with at most six decimals and at
 * most nine integer digits (the safe exact range of `QUANTITY_UNITS_PER_ONE`). No exponent, no thousands separator.
 */
const EXACT_QUANTITY = /^\d{1,9}(?:\.\d{1,6})?$/;

/**
 * Persian/Arabic digits and the Persian decimal separator are the normal way an Iranian operator types a quantity, so
 * they are normalized before parsing — never rejected as "invalid number".
 */
export function normalizeQuantityInput(raw: string): string {
  return raw
    .replace(/[۰-۹]/g, (digit) => String("۰۱۲۳۴۵۶۷۸۹".indexOf(digit)))
    .replace(/[٠-٩]/g, (digit) => String("٠١٢٣٤٥٦٧٨٩".indexOf(digit)))
    .replace(/٫/g, ".")
    .replace(/[\u066c\u060c,\s]/g, "")
    .trim();
}

/** The decimal places actually written in a normalized quantity text (0 when it has no fractional part). */
export function writtenScale(normalized: string): number {
  const dot = normalized.indexOf(".");
  return dot < 0 ? 0 : normalized.length - dot - 1;
}

/**
 * Parses an already-normalized text into exact integer units, WITHOUT any floating-point step: the integer and
 * fractional parts are read as strings and combined. `null` means the text is not an exactly representable quantity.
 * Zero is accepted here (a received quantity may legitimately be zero); callers that need a positive amount check it.
 */
export function parseExactUnits(raw: string): number | null {
  const text = normalizeQuantityInput(raw);
  if (!EXACT_QUANTITY.test(text)) return null;
  const dot = text.indexOf(".");
  const wholePart = dot < 0 ? text : text.slice(0, dot);
  const fractionPart = dot < 0 ? "" : text.slice(dot + 1);
  const whole = Number(wholePart);
  const fraction = Number(fractionPart.padEnd(MAX_QUANTITY_SCALE, "0") || "0");
  const units = whole * QUANTITY_UNITS_PER_ONE + fraction;
  return Number.isSafeInteger(units) ? units : null;
}

/** Parses a positive exact quantity into integer units (`null` for empty/malformed/zero/over-scale input). */
export function parsePositiveExactUnits(raw: string): number | null {
  const units = parseExactUnits(raw);
  return units !== null && units > 0 ? units : null;
}

/**
 * Converts an authoritative wire quantity (a JSON number) into exact units. `toFixed(6)` renders the decimal the
 * server actually sent (a 6-decimal value round-trips through its nearest double), so the value is reconstructed
 * exactly instead of being carried as a lossy `Number` into an addition/subtraction.
 */
export function unitsFromWireNumber(value: number): number | null {
  if (typeof value !== "number" || !Number.isFinite(value) || value < 0) return null;
  const text = value.toFixed(MAX_QUANTITY_SCALE);
  return parseExactUnits(text);
}

/** The exact decimal text of an integer-units quantity, with trailing zeros removed (`300000` → `"0.3"`). */
export function unitsToExactText(units: number): string {
  if (!Number.isFinite(units)) return "";
  const sign = units < 0 ? "-" : "";
  const absolute = Math.abs(Math.trunc(units));
  const whole = Math.trunc(absolute / QUANTITY_UNITS_PER_ONE);
  const fraction = String(absolute % QUANTITY_UNITS_PER_ONE)
    .padStart(MAX_QUANTITY_SCALE, "0")
    .replace(/0+$/, "");
  return fraction.length === 0 ? `${sign}${whole}` : `${sign}${whole}.${fraction}`;
}

/**
 * The JSON body value for an integer-units quantity: the double nearest to the SAME exact decimal the operator
 * reviewed (`0.3` stays `0.3`). It never introduces a rounded quantity, because the text it comes from has at most
 * six decimals and the value is inside the safe exact range.
 */
export function unitsToWireNumber(units: number): number {
  return Number(unitsToExactText(units));
}

/** Exact sum of two integer-units quantities. */
export function addUnits(left: number, right: number): number {
  return left + right;
}

/** Exact difference of two integer-units quantities (never negative when clamped by the caller). */
export function subtractUnits(left: number, right: number): number {
  return left - right;
}

/** Exact comparison: negative when `left < right`, zero when equal, positive when greater. */
export function compareUnits(left: number, right: number): number {
  return left - right;
}

/** The number of integer units in one quantum of a unit that accepts `decimalPlaces` decimals. */
export function unitsPerQuantum(decimalPlaces: number): number {
  const places = Math.max(0, Math.min(decimalPlaces, MAX_QUANTITY_SCALE));
  return Math.pow(10, MAX_QUANTITY_SCALE - places);
}

/** True when the quantity lands exactly on the owning unit's quantum (0.5 of a piece is not a piece). */
export function isAlignedToUnit(units: number, decimalPlaces: number): boolean {
  if (!Number.isInteger(units) || units <= 0) return false;
  const quantum = unitsPerQuantum(decimalPlaces);
  return units % quantum === 0;
}

/** The `step` attribute for one unit-aware quantity field. */
export function quantityStep(decimalPlaces: number): string {
  const places = Math.max(0, Math.min(decimalPlaces, MAX_QUANTITY_SCALE));
  return places === 0 ? "1" : `0.${"0".repeat(places - 1)}1`;
}
