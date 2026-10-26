/**
 * Exact money TEXT of one typed amount: grouping for DISPLAY, canonical token for the WIRE.
 *
 * Rules this module protects:
 * - the wire value is always a raw canonical decimal token (`120000.00`), never a grouped or localized
 *   string, so nothing about the transport changes;
 * - grouping is presentation only: it never rounds, never pads a fraction, never invents `.00`, and never
 *   raises the decimal scale;
 * - no `Number`/`parseFloat` touches the value, so a large lawful amount keeps every digit;
 * - ONLY harmless display normalizations are applied — Persian/Arabic-Indic digits become ASCII, a
 *   recognized well-formed grouping separator is removed, the Arabic decimal separator becomes `.` and
 *   boundary whitespace is trimmed. Anything else (`-100`, `1e3`, `abc100`, `12.3.4`, `12,34`, internal
 *   spaces) is kept VISIBLY INVALID and never turned into a different accepted amount: removing the
 *   unsupported parts and accepting the remaining digits as money is not authorized normalization.
 */
import type { AuthoringLocale } from "../messages/authoring-messages.ts";

const PERSIAN_ZERO = 0x06f0;
const PERSIAN_NINE = 0x06f9;
const ARABIC_ZERO = 0x0660;
const ARABIC_NINE = 0x0669;

/** Digits accepted as 0-9 in a typed/pasted amount (ASCII, Persian, Arabic-Indic). */
export function toAsciiDigits(text: string): string {
  let out = "";
  for (const character of text) {
    const code = character.codePointAt(0) ?? 0;
    if (code >= PERSIAN_ZERO && code <= PERSIAN_NINE) out += String.fromCharCode(48 + code - PERSIAN_ZERO);
    else if (code >= ARABIC_ZERO && code <= ARABIC_NINE) out += String.fromCharCode(48 + code - ARABIC_ZERO);
    else out += character;
  }
  return out;
}

/** True for a character that is a decimal digit after digit normalization. */
function isDigit(character: string): boolean {
  return character >= "0" && character <= "9";
}

/** Boundary whitespace an operator legitimately gets from a paste (never an internal space). */
const BOUNDARY_SPACE = /^[\s\u00a0\u2007\u202f]+|[\s\u00a0\u2007\u202f]+$/g;
/** Grouping separators a paste may carry between well-formed digit groups. */
const GROUP_SEPARATOR = /[\u002c\u066c]/g;
/** A plain digit run with an optional (possibly empty) fraction. */
const PLAIN = /^[0-9]+(?:\.[0-9]*)?$/;
/** A grouped whole part: 1-3 digits, then groups of exactly 3. `12,34` and `1,2,3` do not match. */
const GROUPED = /^[0-9]{1,3}(?:,[0-9]{3})+(?:\.[0-9]*)?$/;

/** One read of a typed/pasted amount: canonical token, or the invalid text kept as typed. */
export interface MoneyTextReading {
  /** Canonical ungrouped token when `valid`; otherwise the normalized text as the operator must see it. */
  readonly raw: string;
  /** True only when nothing but harmless display normalization was needed. */
  readonly valid: boolean;
}

/**
 * Reads arbitrary typed/pasted text as a money token.
 *
 * @param text What the operator typed or pasted.
 */
export function readMoneyText(text: string): MoneyTextReading {
  const trimmed = toAsciiDigits(text)
    .replace(/\u066b/g, ".")
    .replace(/\u066c/g, ",")
    .replace(BOUNDARY_SPACE, "");
  if (trimmed === "") return { raw: "", valid: true };
  // A leading decimal separator is the same number as `0.`; it is display normalization, not a guess.
  const normalized = trimmed.startsWith(".") ? `0${trimmed}` : trimmed;
  if (PLAIN.test(normalized)) return { raw: normalized, valid: true };
  if (GROUPED.test(normalized)) return { raw: normalized.replace(GROUP_SEPARATOR, ""), valid: true };
  return { raw: normalized, valid: false };
}

/**
 * Canonical raw token of lawful typed/pasted text, or the invalid text itself.
 *
 * @param text What the operator typed or pasted.
 */
export function sanitizeMoneyText(text: string): string {
  return readMoneyText(text).raw;
}

/** Groups one digit run in threes, without any numeric conversion. */
function groupDigits(digits: string): string {
  let out = "";
  for (let index = 0; index < digits.length; index += 1) {
    if (index > 0 && (digits.length - index) % 3 === 0) out += ",";
    out += digits[index];
  }
  return out;
}

/**
 * Display text of a canonical raw token: thousands grouped, fraction kept EXACTLY as stored.
 *
 * `120000.00` becomes `120,000.00`, `0.50` stays `0.50`, `1850000` becomes `1,850,000`, an empty value
 * stays empty (a deliberate blank is not turned into `0`), and text that is not a lawful token is shown
 * exactly as it is, so an invalid amount can never be displayed as a different valid one.
 *
 * @param raw Canonical token (ungrouped).
 */
export function groupMoneyText(raw: string): string {
  if (raw === "") return "";
  const dot = raw.indexOf(".");
  const whole = dot < 0 ? raw : raw.slice(0, dot);
  if (!/^[0-9]+$/.test(whole)) return raw;
  const grouped = groupDigits(whole === "" ? "0" : whole);
  return dot < 0 ? grouped : `${grouped}.${raw.slice(dot + 1)}`;
}

/** Locale-aware grouping of a canonical token (grouping character is `,` in both supported locales). */
export function formatMoneyText(raw: string, _locale: AuthoringLocale): string {
  return groupMoneyText(raw);
}

/** Count of decimal digits in a text, after local digit normalization (Persian digits count too). */
export function countDigits(text: string): number {
  let count = 0;
  for (const character of toAsciiDigits(text)) if (isDigit(character)) count += 1;
  return count;
}

/**
 * Position of the caret as the number of digits BEFORE the decimal separator plus the digits AFTER it.
 *
 * `inFraction` records which side of the separator the caret is on, so `12|.` and `12.|` are NOT the same
 * position: a digit-only count cannot tell them apart, and mistaking them turns `12` + `.` + `5` into
 * `125` instead of `12.5`.
 */
export interface MoneyCaret {
  readonly whole: number;
  readonly fraction: number;
  readonly inFraction: boolean;
}

/** Caret anchor of one display text at one offset. */
export function caretAnchorAt(text: string, caret: number): MoneyCaret {
  const normalized = toAsciiDigits(text).replace(/\u066b/g, ".");
  const offset = Math.max(0, Math.min(caret, normalized.length));
  const dot = normalized.indexOf(".");
  if (dot < 0) return { whole: countDigits(normalized.slice(0, offset)), fraction: 0, inFraction: false };
  if (offset <= dot) return { whole: countDigits(normalized.slice(0, offset)), fraction: 0, inFraction: false };
  return {
    whole: countDigits(normalized.slice(0, dot)),
    fraction: countDigits(normalized.slice(dot + 1, offset)),
    inFraction: true,
  };
}

/** Display offset of the caret that follows `digits` digits (never inside a group separator). */
export function caretForDigitIndex(display: string, digits: number): number {
  if (digits <= 0) return 0;
  let seen = 0;
  for (let index = 0; index < display.length; index += 1) {
    if (!isDigit(display[index]!)) continue;
    seen += 1;
    if (seen === digits) return index + 1;
  }
  return display.length;
}

/** Display offset of a caret anchor, rebuilt against the CURRENTLY displayed text. */
export function caretOffsetFor(display: string, anchor: MoneyCaret): number {
  if (!anchor.inFraction) return caretForDigitIndex(display, anchor.whole);
  const dot = display.indexOf(".");
  if (dot < 0) return caretForDigitIndex(display, anchor.whole + anchor.fraction);
  return caretForDigitIndex(display.slice(dot + 1), anchor.fraction) + dot + 1;
}

/** One interpreted edit of a money input: canonical-or-invalid token plus the stable caret anchor. */
export interface MoneyEdit {
  /** The exact text the model must keep: canonical token, or the visible invalid text. */
  readonly raw: string;
  /** True only for a lawful amount (an invalid text is preserved and left to validation to refuse). */
  readonly valid: boolean;
  /**
   * True when this text (or the text it came from) was supplied by the OPERATOR in an unsupported shape, so
   * its separators are the operator's own characters and must never be read as the field's display grouping.
   * The field stores this together with `raw` and hands it back on the next edit.
   */
  readonly untrusted: boolean;
  readonly caret: MoneyCaret;
}

/**
 * The edit provenance of one money field: the token it last committed AND whether that token is the
 * operator's own unsupported text.
 *
 * This is real field state, not something re-inferred from the text later: `12,34` + `5` yields the
 * structurally well-grouped `12,345`, which a text-only reader would call lawful — but its comma is still the
 * operator's, so it must stay invalid through ANY further keystrokes until the operator removes it.
 */
export interface MoneyInputProvenance {
  readonly raw: string;
  readonly untrusted: boolean;
}

/** Provenance of an externally received token (an authoritative reseed): only crime is what the text is. */
export function readMoneyProvenance(raw: string): MoneyInputProvenance {
  return { raw, untrusted: !readMoneyText(raw).valid };
}

/** Normalizes the optional convenience form used by pure callers into real provenance. */
function asProvenance(previous: MoneyInputProvenance | string): MoneyInputProvenance {
  return typeof previous === "string" ? readMoneyProvenance(previous) : previous;
}

/**
 * Interprets one input change exactly the way the field must: the model token plus the caret anchor.
 *
 * @param nextText The input's new text (already including the typed/pasted characters).
 * @param caret The caret offset the browser reported for that text.
 */
export function interpretMoneyEdit(nextText: string, caret: number): MoneyEdit {
  const reading = readMoneyText(nextText);
  return {
    raw: reading.raw,
    valid: reading.valid,
    untrusted: !reading.valid,
    caret: caretAnchorAt(nextText, caret),
  };
}

/**
 * One Backspace pressed directly after a grouping separator.
 *
 * Display grouping re-inserts that separator on every render, so letting the browser delete it alone would
 * leave the same text and the same caret forever: the key would look broken. A Backspace there therefore
 * removes the separator AND the digit it follows, which is exactly the digit the operator is deleting; the
 * remaining digit run is kept verbatim (nothing is guessed, padded or re-scaled).
 *
 * @param text The CURRENTLY DISPLAYED text of the field (grouped or invalid).
 * @param caret The caret offset the browser reported.
 * @returns The interpreted edit, or `null` when a plain browser edit must stay in charge.
 */
export function interpretMoneyBackspace(text: string, caret: number): MoneyEdit | null {
  const normalized = toAsciiDigits(text).replace(/\u066b/g, ".").replace(/\u066c/g, ",");
  if (caret <= 0 || caret > normalized.length) return null;
  const previous = normalized[caret - 1];
  if (previous !== ",") return null;
  const nextText = `${normalized.slice(0, caret - 2)}${normalized.slice(caret)}`;
  const reading = readMoneyText(nextText);
  return {
    raw: reading.raw,
    valid: reading.valid,
    untrusted: !reading.valid,
    caret: caretAnchorAt(nextText, Math.max(0, caret - 2)),
  };
}

/** True when the token is a lawful canonical money token of the local contract (shape only). */
export function isCanonicalMoneyText(raw: string): boolean {
  return /^(?:0|[1-9][0-9]*)(?:\.[0-9]{1,2})?$/.test(raw);
}

/**
 * One typed keystroke (NOT a paste) inside the grouped field.
 *
 * A separator the browser reports belongs to the field's OWN display grouping ONLY when the value the field
 * last committed was both lawful AND not the operator's own unsupported text. Then a middle insert or a delete
 * around an existing group can leave text like `1,200000`, and reading those characters as user-supplied
 * grouping would refuse a perfectly lawful amount (the reported screenshot defect) — so they are dropped.
 *
 * When the previous value was UNTRUSTED (whether plainly invalid like `12,34`, or structurally well-grouped
 * but still the operator's own text like `12,345` after one appended digit), its separators are the operator's
 * characters: dropping them would heal `12,34` + `5` + `6` into the accepted `123456`. That text stays exactly
 * as typed and invalid through any number of appended keys until the operator removes the offending grouping.
 *
 * The caret is mapped through the SAME separator removal (logical offset), never applied to a shorter text.
 *
 * @param previous Provenance the FIELD owns (the token it last committed plus its trust), or a bare token
 *   for a pure caller with no field history.
 * @param nextText The field's new display text, exactly as the browser reported it.
 * @param caret The caret offset the browser reported for that text.
 */
export function interpretMoneyTyping(
  previous: MoneyInputProvenance | string,
  nextText: string,
  caret: number,
): MoneyEdit {
  const provenance = asProvenance(previous);
  const display = visibleMoneyText(nextText);
  const offset = Math.max(0, Math.min(caret, display.length));
  const trusted = !provenance.untrusted && readMoneyText(provenance.raw).valid;
  if (!trusted) {
    // The operator's OWN text: a separator in it was NOT produced by our display grouping, so dropping it
    // would silently heal it into a different accepted amount. It stays exactly as typed and invalid.
    if (hasGroupSeparator(display)) {
      return { raw: display, valid: false, untrusted: true, caret: caretAnchorAt(display, offset) };
    }
    const reading = readMoneyText(display);
    return { raw: reading.raw, valid: reading.valid, untrusted: !reading.valid, caret: caretAnchorAt(display, offset) };
  }
  // Trusted display: every separator in it belongs to OUR grouping, so dropping it is display-only.
  const stripped = withoutDisplayGrouping(display);
  const reading = readMoneyText(stripped.text);
  return {
    raw: reading.raw,
    valid: reading.valid,
    untrusted: !reading.valid,
    caret: caretAnchorAt(stripped.text, stripped.offsets[offset] ?? stripped.text.length),
  };
}

/** Normalized VISIBLE text: local digits become ASCII and the Arabic decimal separator becomes `.`. */
function visibleMoneyText(text: string): string {
  return toAsciiDigits(text).replace(/\u066b/g, ".");
}

/** True when the text carries a GROUPING separator (ours, or one the operator supplied). */
function hasGroupSeparator(text: string): boolean {
  return text.includes(",") || text.includes("\u066c");
}

/** The text with the display grouping dropped, plus the logical offset of EVERY display offset. */
function withoutDisplayGrouping(display: string): { text: string; offsets: readonly number[] } {
  let text = "";
  const offsets: number[] = [0];
  for (const character of display) {
    if (character !== "," && character !== "\u066c") text += character;
    offsets.push(text.length);
  }
  return { text, offsets };
}

/**
 * One Delete pressed directly BEFORE a grouping separator.
 *
 * Symmetric to `interpretMoneyBackspace`: the separator is re-inserted by display grouping on every render,
 * so deleting it alone would change nothing forever. The digit AFTER it is the character the operator is
 * deleting, so both go; the remaining digit run is kept verbatim.
 *
 * @param text The CURRENTLY DISPLAYED text of the field (grouped or invalid).
 * @param caret The caret offset the browser reported.
 * @returns The interpreted edit, or `null` when a plain browser edit must stay in charge.
 */
export function interpretMoneyDelete(text: string, caret: number): MoneyEdit | null {
  const normalized = visibleMoneyText(text).replace(/\u066c/g, ",");
  if (caret < 0 || caret >= normalized.length) return null;
  if (normalized[caret] !== ",") return null;
  const nextText = `${normalized.slice(0, caret)}${normalized.slice(caret + 2)}`;
  const reading = readMoneyText(nextText);
  return { raw: reading.raw, valid: reading.valid, untrusted: !reading.valid, caret: caretAnchorAt(nextText, caret) };
}

/** True when a canonical token carries a NON-zero fraction (an amount that is not a whole unit). */
export function moneyHasFraction(raw: string): boolean {
  const dot = raw.indexOf(".");
  if (dot < 0) return false;
  return /[1-9]/.test(raw.slice(dot + 1));
}

/**
 * Currency precision policy of the customer-facing money input.
 *
 * `IRR` is quoted to customers in whole rial, so the operator's requested input is an integer. This is a
 * DISPLAY/entry policy only: the stored token is never rounded, padded or truncated, and a currency the
 * server metadata does not mark this way keeps the existing two-decimal contract.
 *
 * @param currencyCode Server-owned currency of the area (`null` while a new area has none).
 */
export function moneyPrecisionPolicy(currencyCode: string | null | undefined): "integer" | "scaled" {
  return currencyCode?.trim().toUpperCase() === "IRR" ? "integer" : "scaled";
}

/** Display text under a currency policy: an integral IRR token shows no `.00`, a fraction is kept. */
export function displayMoneyText(raw: string, policy: "integer" | "scaled"): string {
  if (policy === "integer") {
    const grouped = groupMoneyText(raw);
    return moneyHasFraction(raw) ? grouped : grouped.replace(/\.0+$/, "");
  }
  return groupMoneyText(raw);
}
