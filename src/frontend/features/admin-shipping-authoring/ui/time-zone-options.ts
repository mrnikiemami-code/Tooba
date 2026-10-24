/**
 * Feature-owned list of time-zone identifiers offered by the weekly-schedule editor.
 *
 * The operator must never have to know a raw IANA id, and a human label must never be SENT as if it
 * were an id. Each option therefore carries the exact identifier the server resolves
 * (`TimeZoneInfo.FindSystemTimeZoneById`, so IANA ids are authoritative) plus a human label. The list
 * is deliberately small and explicit — no browser/geo inference, no silent default and no second
 * time-zone authority: the server still rejects anything it cannot resolve.
 */
import type { AuthoringLocale } from "../messages/authoring-messages.ts";

/** One selectable time zone: the identifier that is sent, and its human label. */
export interface TimeZoneOption {
  readonly id: string;
  readonly label: Record<AuthoringLocale, string>;
}

/** The offered identifiers, ordered by relevance to the configured markets. */
export const TIME_ZONE_OPTIONS: readonly TimeZoneOption[] = [
  { id: "Asia/Tehran", label: { fa: "تهران (ایران)", en: "Tehran (Iran)" } },
  { id: "Asia/Dubai", label: { fa: "دوبی (امارات)", en: "Dubai (UAE)" } },
  { id: "Asia/Muscat", label: { fa: "مسقط (عمان)", en: "Muscat (Oman)" } },
  { id: "Asia/Riyadh", label: { fa: "ریاض (عربستان)", en: "Riyadh (Saudi Arabia)" } },
  { id: "Asia/Kabul", label: { fa: "کابل (افغانستان)", en: "Kabul (Afghanistan)" } },
  { id: "Asia/Baghdad", label: { fa: "بغداد (عراق)", en: "Baghdad (Iraq)" } },
  { id: "Asia/Istanbul", label: { fa: "استانبول (ترکیه)", en: "Istanbul (Türkiye)" } },
  { id: "Europe/London", label: { fa: "لندن", en: "London" } },
  { id: "Europe/Berlin", label: { fa: "برلین", en: "Berlin" } },
  { id: "UTC", label: { fa: "زمان جهانی (UTC)", en: "Coordinated Universal Time (UTC)" } },
];

/** Human label of an id, or `null` when the stored id is outside this list. */
export function timeZoneLabel(id: string, locale: AuthoringLocale): string | null {
  const match = TIME_ZONE_OPTIONS.find((option) => option.id === id);
  return match ? match.label[locale] : null;
}

/** True when this exact id is one of the offered options. */
export function isOfferedTimeZone(id: string): boolean {
  return TIME_ZONE_OPTIONS.some((option) => option.id === id);
}
