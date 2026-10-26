/**
 * Exact money TEXT of one typed amount: grouping for DISPLAY, canonical token for the WIRE.
 *
 * Rules this module protects:
 * - the wire value is always a raw canonical decimal token (`120000.00`), never a grouped or localized
 *   string, so nothing about the transport changes;
 * - grouping is presentation only: it never rounds, never pads a fraction, never invents `.00`, and never
 *   raises the decimal scale;
 * - no `Number`/`parseFloat` touches the value, so a large lawful amount keeps every digit;
 * - Persian/Arabic-Indic digits, thousands separators and pasted grouped values are normalized
 *   deterministically so a paste cannot produce a different number than what the operator sees.
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

/**
 * Canonical raw token of arbitrary typed/pasted text: ASCII digits, at most one decimal point.
 *
 * Group separators (`,` `٬` `٬` `,`), Arabic decimal separator `٫`, spaces and any other character are
 * dropped; a leading decimal point becomes `0.`. The digits themselves and their order are preserved
 * exactly, so the result is the SAME number the operator sees, only ungrouped.
 *
 * @param text What the operator typed or pasted.
 */
export function sanitizeMoneyText(text: string): string {
  const normalized = toAsciiDigits(text).replace(/\u066B/g, ".");
  let whole = "";
  let fraction: string | null = null;
  for (const character of normalized) {
    if (isDigit(character)) {
      if (fraction === null) whole += character;
      else fraction += character;
      continue;
    }
    if (character === "." && fraction === null) fraction = "";
  }
  if (fraction === null) return whole;
  return `${whole === "" ? "0" : whole}.${fraction}`;
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
 * `120000.00` becomes `120,000.00`, `0.50` stays `0.50`, `1850000` becomes `1,850,000`, and an empty
 * value stays empty (a deliberate blank is not turned into `0`).
 *
 * @param raw Canonical token (ungrouped).
 */
export function groupMoneyText(raw: string): string {
  if (raw === "") return "";
  const dot = raw.indexOf(".");
  if (dot < 0) return groupDigits(raw);
  const whole = raw.slice(0, dot);
  return `${groupDigits(whole === "" ? "0" : whole)}.${raw.slice(dot + 1)}`;
}

/** Locale-aware grouping of a canonical token (grouping character is `,` in both supported locales). */
export function formatMoneyText(raw: string, _locale: AuthoringLocale): string {
  return groupMoneyText(raw);
}

/** Count of decimal digits in a text (used for caret restoration, not for value comparison). */
export function countDigits(text: string): number {
  let count = 0;
  for (const character of text) if (isDigit(character)) count += 1;
  return count;
}

/** How many digits precede `caret` — the caret anchor that survives re-grouping. */
export function digitIndexAt(text: string, caret: number): number {
  return countDigits(text.slice(0, Math.max(0, Math.min(caret, text.length))));
}

/** Display offset of the caret that follows the `digits`-th digit (`0` = before the first digit). */
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

/** One interpreted edit of a money input: canonical token plus the digit anchor of the caret. */
export interface MoneyEdit {
  readonly raw: string;
  readonly caretDigits: number;
}

/**
 * Interprets one input change exactly the way the field must: canonical token + stable caret anchor.
 *
 * The anchor is a DIGIT count (not a character offset), so re-grouping after a keystroke or a paste can
 * never move the caret to another number position.
 *
 * @param nextText The input's new text (already including the typed/pasted characters).
 * @param caret The caret offset the browser reported for that text.
 */
export function interpretMoneyEdit(nextText: string, caret: number): MoneyEdit {
  const raw = sanitizeMoneyText(nextText);
  const anchor = Math.min(digitIndexAt(nextText, caret), countDigits(raw));
  return { raw, caretDigits: anchor };
}

/** True when the token is a lawful canonical money token of the local contract (shape only). */
export function isCanonicalMoneyText(raw: string): boolean {
  return /^(?:0|[1-9][0-9]*)(?:\.[0-9]{1,2})?$/.test(raw);
}
