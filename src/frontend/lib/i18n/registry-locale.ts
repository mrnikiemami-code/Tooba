"use client";

import { useEffect, useMemo, useState } from "react";
import { useLocale } from "./locale-context.tsx";
import { LOCALE_COOKIE_NAME, type TextDirection } from "./locale.ts";

export interface RegistryLocaleDefinition {
  code: string;
  urlPrefix: string;
  active: boolean;
  culture: string;
  language: string;
  direction: TextDirection;
  calendar: "jalali" | "gregorian";
  isDefault: boolean;
}

export interface RegistryLocaleState {
  locale: RegistryLocaleDefinition;
  loaded: boolean;
}

function appLocaleDefinition(locale: "fa" | "en"): RegistryLocaleDefinition {
  return locale === "en"
    ? { code: "en-US", urlPrefix: "en", active: true, culture: "en-US", language: "en", direction: "ltr", calendar: "gregorian", isDefault: false }
    : { code: "fa-IR", urlPrefix: "fa", active: true, culture: "fa-IR", language: "fa", direction: "rtl", calendar: "jalali", isDefault: true };
}

function cookieLocale(): string | null {
  const prefix = typeof document === "undefined" ? null : window.location.pathname.match(/^\/([a-z]{2})(?:\/|$)/)?.[1];
  if (prefix) return prefix;
  if (typeof document === "undefined") return null;
  const value = document.cookie
    .split(";")
    .map((part) => part.trim())
    .find((part) => part.startsWith(`${LOCALE_COOKIE_NAME}=`))
    ?.split("=")
    .slice(1)
    .join("=");
  return value ? decodeURIComponent(value).trim() : null;
}

export async function loadActiveRegistryLanguages(): Promise<RegistryLocaleDefinition[]> {
  try {
    const response = await fetch("/v1/languages", { headers: { Accept: "application/json" } });
    if (!response.ok) return [];
    const rows = await response.json();
    if (!Array.isArray(rows)) return [];
    return rows.flatMap((raw): RegistryLocaleDefinition[] => {
      if (!raw || typeof raw !== "object") return [];
      const row = raw as Record<string, unknown>;
      const code = String(row.code ?? "").trim();
      if (!code) return [];
      const culture = String(row.culture ?? code).trim();
      const calendarRaw = String(row.calendarDisplay ?? "Gregorian").toLowerCase();
      return [{
        code,
        urlPrefix: String(row.urlPrefix ?? "").trim(),
        active: Boolean(row.isActive ?? row.active ?? false),
        culture,
        language: (culture || code).split("-")[0] ?? code,
        direction: String(row.direction ?? "rtl") === "ltr" ? "ltr" : "rtl",
        calendar: calendarRaw.startsWith("jal") ? "jalali" : "gregorian",
        isDefault: Boolean(row.isDefault ?? row.default ?? false),
      }];
    });
  } catch {
    return [];
  }
}

/** Shared registry-backed locale for offer-owned surfaces; locale comes from the project cookie, not a feature query parameter. */
export function useRegistryLocale(): RegistryLocaleState {
  const app = appLocaleDefinition(useLocale());
  const requested = typeof document === "undefined" ? null : cookieLocale();
  const [resolved, setResolved] = useState<RegistryLocaleDefinition | null>(null);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    let active = true;
    void loadActiveRegistryLanguages().then((rows) => {
      if (!active) return;
      const needle = (requested ?? "").toLowerCase();
      const exact = rows.find((item) => item.active && (
        item.urlPrefix.toLowerCase() === needle
        || item.code.toLowerCase() === needle
        || item.code.toLowerCase().split("-")[0] === needle));
      const row = exact ?? rows.find((item) => item.active && item.isDefault) ?? rows.find((item) => item.active);
      setResolved(row ?? null);
      setLoaded(true);
    });
    return () => { active = false; };
  }, [requested]);

  return useMemo(
    () => ({ locale: resolved ?? app, loaded }),
    [app, loaded, resolved],
  );
}
