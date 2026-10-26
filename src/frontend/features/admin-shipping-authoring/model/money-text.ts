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
  readonly caret: MoneyCaret;
}

/**
 * Interprets one input change exactly the way the field must: the model token plus the caret anchor.
 *
 * @param nextText The input's new text (already including the typed/pasted characters).
 * @param caret The caret offset the browser reported for that text.
 */
export function interpretMoneyEdit(nextText: string, caret: number): MoneyEdit {
  const reading = readMoneyText(nextText);
  return { raw: reading.raw, valid: reading.valid, caret: caretAnchorAt(nextText, caret) };
}

/** True when the token is a lawful canonical money token of the local contract (shape only). */
export function isCanonicalMoneyText(raw: string): boolean {
  return /^(?:0|[1-9][0-9]*)(?:\.[0-9]{1,2})?$/.test(raw);
}
