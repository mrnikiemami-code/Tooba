/**
 * Types and routing helpers for Language/Locale.
 * Authoritative registry is persisted in Host `localization.languages` (see /v1/admin/languages).
 */
import { type Locale, type TextDirection } from "./locale.ts";

export type CalendarDisplayPolicy = "jalali" | "gregorian";

export interface SupportedLocaleDefinition {
  code: string;
  urlPrefix: Locale;
  displayName: string;
  nativeName: string;
  direction: TextDirection;
  culture: string;
  calendarDisplay: CalendarDisplayPolicy;
  active: boolean;
  default: boolean;
  sortOrder: number;
}

/** Routing bootstrap fallback only — Admin reads authoritative data from /v1/admin/languages. */
export const SUPPORTED_LOCALE_DEFINITIONS: SupportedLocaleDefinition[] = [
  {
    code: "fa-IR",
    urlPrefix: "fa",
    displayName: "Persian (Iran)",
    nativeName: "فارسی (ایران)",
    direction: "rtl",
    culture: "fa-IR",
    calendarDisplay: "jalali",
    active: true,
    default: true,
    sortOrder: 0,
  },
  {
    code: "en-US",
    urlPrefix: "en",
    displayName: "English (United States)",
    nativeName: "English (US)",
    direction: "ltr",
    culture: "en-US",
    calendarDisplay: "gregorian",
    active: true,
    default: false,
    sortOrder: 1,
  },
];

export function contentApiLocaleForUrlPrefix(prefix: Locale): string {
  const row = SUPPORTED_LOCALE_DEFINITIONS.find((item) => item.urlPrefix === prefix);
  return row?.code ?? (prefix === "fa" ? "fa-IR" : "en-US");
}

export function urlPrefixForContentLocale(code: string): Locale {
  const normalized = code.trim().toLowerCase();
  const row = SUPPORTED_LOCALE_DEFINITIONS.find((item) => item.code.toLowerCase() === normalized);
  return row?.urlPrefix ?? "fa";
}

export function mapSupportedLocale(payload: unknown): SupportedLocaleDefinition | null {
  if (!payload || typeof payload !== "object") return null;
  const item = payload as Record<string, unknown>;
  const code = String(item.code ?? item.Code ?? "").trim();
  if (!code) return null;
  const urlPrefixRaw = String(item.urlPrefix ?? item.UrlPrefix ?? "fa").trim();
  const urlPrefix: Locale = urlPrefixRaw === "en" ? "en" : "fa";
  const calendarRaw = String(item.calendarDisplay ?? item.CalendarDisplay ?? "jalali").toLowerCase();
  return {
    code,
    urlPrefix,
    displayName: String(item.displayName ?? item.DisplayName ?? code),
    nativeName: String(item.nativeName ?? item.NativeName ?? code),
    direction: String(item.direction ?? item.Direction ?? "rtl") === "ltr" ? "ltr" : "rtl",
    culture: String(item.culture ?? item.Culture ?? code),
    calendarDisplay: calendarRaw.startsWith("greg") ? "gregorian" : "jalali",
    active: Boolean(item.active ?? item.Active ?? true),
    default: Boolean(item.default ?? item.isDefault ?? item.IsDefault ?? false),
    sortOrder: Number(item.sortOrder ?? item.SortOrder ?? 0),
  };
}
