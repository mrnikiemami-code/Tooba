/**
 * Locale-aware DISPLAY helpers of the editor slice (no business meaning, no FX conversion).
 */
import type { AuthoringLocale } from "../messages/authoring-messages.ts";
import type { DecimalText } from "../model/editor-document.ts";
import { groupMoneyText } from "../model/money-text.ts";

function tag(locale: AuthoringLocale): string {
  return locale === "fa" ? "fa-IR" : "en-US";
}

/** Locale digits for a whole number; a non-finite input renders the empty string. */
export function formatNumber(value: number, locale: AuthoringLocale): string {
  if (!Number.isFinite(value)) return "";
  return new Intl.NumberFormat(tag(locale)).format(value);
}

/**
 * Exact stored decimal TEXT, re-grouped for display only (never rounded by the editor).
 *
 * Uses the string-based money grouping, so even a very large lawful amount keeps every digit.
 */
export function formatDecimalText(amount: DecimalText, locale: AuthoringLocale): string {
  return groupMoneyText(amount);
}

/** Keeps the stored `HH:mm:ss[.fffffff]` text, dropping only the seconds when they are zero. */
export function formatTimeText(text: string): string {
  const match = /^(\d{2}):(\d{2}):(\d{2})(?:\.\d+)?$/.exec(text);
  if (!match) return text;
  return match[3] === "00" ? `${match[1]}:${match[2]}` : `${match[1]}:${match[2]}:${match[3]}`;
}
