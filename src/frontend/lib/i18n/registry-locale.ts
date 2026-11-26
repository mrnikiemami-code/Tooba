"use client";

import { createContext, createElement, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { usePathname } from "next/navigation";
import { useLocale } from "./locale-context.tsx";
import { DEFAULT_LOCALE, LOCALE_COOKIE_NAME, type TextDirection } from "./locale.ts";
import { parseRegistryPanelPrefix } from "./routing.ts";

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
  requestedPrefix: string | null;
}

const RegistryLocaleContext = createContext<RegistryLocaleState | null>(null);

function fallback(locale: "fa" | "en" = DEFAULT_LOCALE): RegistryLocaleDefinition {
  return locale === "en"
    ? { code: "en-US", urlPrefix: "en", active: true, culture: "en-US", language: "en", direction: "ltr", calendar: "gregorian", isDefault: false }
    : { code: "fa-IR", urlPrefix: "fa", active: true, culture: "fa-IR", language: "fa", direction: "rtl", calendar: "jalali", isDefault: true };
}

function cookieValue(): string | null {
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

function resolve(rows: RegistryLocaleDefinition[], requested: string | null): RegistryLocaleDefinition {
  const needle = (requested ?? "").toLowerCase();
  const exact = rows.find((item) => item.active && (
    item.urlPrefix.toLowerCase() === needle
    || item.code.toLowerCase() === needle
    || item.code.toLowerCase().split("-")[0] === needle));
  return exact ?? rows.find((item) => item.active && item.isDefault) ?? rows.find((item) => item.active) ?? fallback();
}

/** Single shared registry authority for panels, grids, feedback, headers and shell. */
export function RegistryLocaleProvider({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const panelPrefix = parseRegistryPanelPrefix(pathname ?? "")?.prefix ?? null;
  const [rows, setRows] = useState<RegistryLocaleDefinition[]>([]);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    let active = true;
    void loadActiveRegistryLanguages().then((next) => {
      if (!active) return;
      setRows(next);
      setLoaded(true);
    });
    return () => { active = false; };
  }, []);

  const value = useMemo<RegistryLocaleState>(() => {
    const requested = panelPrefix ?? cookieValue();
    const locale = resolve(rows, requested);
    return { locale, loaded, requestedPrefix: requested };
  }, [loaded, panelPrefix, rows]);

  useEffect(() => {
    if (!loaded) return;
    document.documentElement.lang = value.locale.culture || value.locale.language;
    document.documentElement.dir = value.locale.direction;
  }, [loaded, value.locale]);

  return createElement(RegistryLocaleContext.Provider, { value }, children);
}

export function useRegistryLocale(): RegistryLocaleState {
  const context = useContext(RegistryLocaleContext);
  const app = useLocale();
  const pathname = usePathname();
  const fallbackState = useMemo<RegistryLocaleState>(() => ({
    locale: fallback(app),
    loaded: false,
    requestedPrefix: parseRegistryPanelPrefix(pathname ?? "")?.prefix ?? cookieValue(),
  }), [app, pathname]);
  return context ?? fallbackState;
}
