/**
 * TB-267 — presentation-only formatters for the operational shipping surface.
 *
 * The operator surface used to mix three date shapes on one promise (Gregorian ISO in the target frame,
 * `YYYY/MM/DD` Jalali in the group card and `Intl` Jalali in the deadline row) and repeated the raw
 * `Asia/Tehran` zone beside every instant. These helpers give ONE human Persian shape for a date and for an
 * accepted plan instant, and ONE human zone label printed once per block.
 *
 * Display only: nothing here reads, derives or changes an accepted instant. A value the plan does not carry
 * returns null so the caller can say «مشخص نشده» instead of inventing a date.
 */

const PERSIAN_DIGITS = ["۰", "۱", "۲", "۳", "۴", "۵", "۶", "۷", "۸", "۹"] as const;

/** A tiny dictionary of the zones this product actually plans in; any other zone falls back to its own id. */
const ZONE_CITY_FA: Record<string, string> = {
  tehran: "تهران",
  istanbul: "استانبول",
  dubai: "دبی",
  baghdad: "بغداد",
  doha: "دوحه",
  riyadh: "ریاض",
  "abu dhabi": "ابوظبی",
  muscat: "مسقط",
  kabul: "کابل",
  utc: "UTC",
};

function persianDigits(value: string): string {
  return value.replace(/\d/g, (digit) => PERSIAN_DIGITS[Number(digit)] ?? digit);
}

function instant(isoUtc: string | null | undefined): Date | null {
  const raw = (isoUtc ?? "").trim();
  if (!raw) return null;
  const when = new Date(raw);
  return Number.isNaN(when.getTime()) ? null : when;
}

/**
 * An accepted DATE-only value (`2026-11-17`) → «۲۶ آبان ۱۴۰۵».
 * Noon local time is used so a date-only commitment never shifts a day across a zone boundary.
 */
export function operationalDayLabel(isoDate: string | null | undefined): string | null {
  const raw = (isoDate ?? "").trim();
  if (!raw) return null;
  const when = new Date(raw.includes("T") ? raw : `${raw}T12:00:00`);
  if (Number.isNaN(when.getTime())) return null;
  try {
    return persianDigits(
      when.toLocaleDateString("fa-IR", { year: "numeric", month: "long", day: "numeric" }),
    );
  } catch {
    return null;
  }
}

/** An accepted plan INSTANT → «۲۴ آبان ۱۴۰۵، ۱۳:۰۰» rendered in the plan's own zone. */
export function operationalInstantLabel(
  isoUtc: string | null | undefined,
  zone: string | null | undefined,
): string | null {
  const when = instant(isoUtc);
  if (!when) return null;
  const timeZone = (zone ?? "").trim() || undefined;
  try {
    const datePart = when.toLocaleDateString("fa-IR", {
      year: "numeric",
      month: "long",
      day: "numeric",
      ...(timeZone ? { timeZone } : {}),
    });
    const timePart = when.toLocaleTimeString("fa-IR", {
      hour: "2-digit",
      minute: "2-digit",
      hour12: false,
      ...(timeZone ? { timeZone } : {}),
    });
    return `${persianDigits(datePart)}، ${persianDigits(timePart)}`;
  } catch {
    return null;
  }
}

/**
 * ONE human label for the zone the accepted plan was computed in → «به وقت تهران».
 * Derived from the plan's own zone id; an unknown id is shown as it is rather than guessed.
 */
export function operationalTimeZoneLabel(zone: string | null | undefined): string | null {
  const raw = (zone ?? "").trim();
  if (!raw) return null;
  const city = (raw.split("/").pop() ?? raw).replace(/_/g, " ").trim();
  if (!city) return null;
  const localized = ZONE_CITY_FA[city.toLowerCase()];
  return `به وقت ${localized ?? city}`;
}

/** «۰۹:۰۰–۱۰:۰۰» → Persian digits only; the range itself is rendered `dir="ltr"` by the caller. */
export function operationalTimeWindowLabel(window: string | null | undefined): string {
  return persianDigits((window ?? "").trim());
}
