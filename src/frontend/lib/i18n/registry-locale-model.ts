import type { Locale, TextDirection } from "./locale.ts";

export interface RegistryLocaleDefinition {
  code: string;
  urlPrefix: string;
  active: boolean;
  culture: string;
  language: string;
  direction: TextDirection;
  calendar: "jalali" | "gregorian";
  isDefault: boolean;
  nativeName: string;
}
export interface RegistryLocaleState {
  locale: RegistryLocaleDefinition;
  loaded: boolean;
  available: boolean;
  requestedPrefix: string | null;
  languages: readonly RegistryLocaleDefinition[];
  panel: boolean;
}

/** Legacy rendering fallback, never an enabled registry entry. */
export function renderingFallback(locale: Locale): RegistryLocaleDefinition {
  return locale === "en"
    ? { code: "en-US", urlPrefix: "en", active: false, culture: "en-US", language: "en", direction: "ltr", calendar: "gregorian", isDefault: false, nativeName: "English" }
    : { code: "fa-IR", urlPrefix: "fa", active: false, culture: "fa-IR", language: "fa", direction: "rtl", calendar: "jalali", isDefault: false, nativeName: "فارسی" };
}

export function parseRegistryLanguages(raw: unknown): RegistryLocaleDefinition[] {
  if (!Array.isArray(raw)) throw new Error("localization.registry.invalid");
  const rows: RegistryLocaleDefinition[] = [];
  const prefixes = new Set<string>();
  for (const value of raw) {
    if (!value || typeof value !== "object") throw new Error("localization.registry.invalid");
    const row = value as Record<string, unknown>;
    if (row.isActive !== true) continue;
    const code = typeof row.code === "string" ? row.code.trim() : "";
    const urlPrefix = typeof row.urlPrefix === "string" ? row.urlPrefix.trim() : "";
    const culture = typeof row.culture === "string" ? row.culture.trim() : "";
    const direction = typeof row.direction === "string" ? row.direction.toLowerCase() : "";
    const calendar = typeof row.calendarDisplay === "string" ? row.calendarDisplay.toLowerCase() : "";
    if (!code || !culture || !/^[a-z0-9-]+$/i.test(urlPrefix) || (direction !== "rtl" && direction !== "ltr") || (calendar !== "jalali" && calendar !== "gregorian") || prefixes.has(urlPrefix.toLowerCase()))
      throw new Error("localization.registry.invalid");
    const language = new Intl.Locale(culture).language;
    prefixes.add(urlPrefix.toLowerCase());
    rows.push({ code, urlPrefix, active: true, culture, language, direction, calendar, isDefault: row.isDefault === true, nativeName: typeof row.nativeName === "string" ? row.nativeName : code });
  }
  return rows;
}

/** Prefix matching is exact; disabled and unknown prefixes resolve only to an active registry default. */
export function resolveRegistryLanguage(rows: readonly RegistryLocaleDefinition[], requested: string | null): RegistryLocaleDefinition | null {
  const needle = requested?.trim().toLowerCase();
  const active = rows.filter(row => row.active);
  return active.find(row => row.urlPrefix.toLowerCase() === needle)
    ?? active.find(row => row.isDefault) ?? active[0] ?? null;
}

export async function loadActiveRegistryLanguages(): Promise<RegistryLocaleDefinition[]> {
  const response = await fetch("/v1/languages", { headers: { Accept: "application/json" }, cache: "no-store" });
  if (!response.ok) throw new Error("localization.registry.unavailable");
  return parseRegistryLanguages(await response.json());
}
