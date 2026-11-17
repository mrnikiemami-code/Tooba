/**
 * TB-276 — the operator display of one Returns work-queue eligibility label.
 *
 * The owning Returns read model composes the label (e.g. «5 روز باقی‌مانده · مهلت 2026-11-21») from the line's own
 * return-window snapshot. The stored deadline is a real instant, but the operator surface must show it in the same
 * Jalali calendar and the same Persian digits as every other operational value on this screen, so the label's
 * Gregorian day token is rendered through the shared `operationalDayLabel` helper and the remaining digits are
 * normalised with the shared digit helper.
 *
 * Display only: the deadline is never recomputed, shifted or invented, and the label's own words are preserved.
 * An unrecognised label is still shown (with its digits normalised), and an absent label stays «—».
 */

import { operationalDayLabel } from "../../lib/operational-format.ts";
import { toPersianDigits } from "../../lib/persian-digits.ts";

const ISO_DAY_TOKEN = /\b(\d{4})-(\d{2})-(\d{2})\b/g;

/** The operator label of one eligibility summary: Jalali deadline, Persian digits, unchanged words. */
export function formatEligibilityLabel(summary: string | null | undefined): string {
  const raw = (summary ?? "").trim();
  if (!raw) return "—";
  const jalali = raw.replace(ISO_DAY_TOKEN, (token) => operationalDayLabel(token) ?? token);
  return toPersianDigits(jalali);
}
