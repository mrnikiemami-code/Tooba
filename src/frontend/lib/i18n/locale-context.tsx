"use client";

import { createContext, useCallback, useContext, useEffect, useLayoutEffect, useMemo, useState, type ReactNode } from "react";
import { usePathname, useRouter } from "next/navigation";
import { DEFAULT_LOCALE, dirForLocale, langForLocale, LOCALE_COOKIE_NAME, type Locale } from "./locale.ts";
import { localePath, parseLocalePrefix, parseRegistryPanelPrefix, registryPanelSwitchPath, stripLocalePrefix } from "./routing.ts";
import { loadActiveRegistryLanguages, renderingFallback, resolveRegistryLanguage, type RegistryLocaleDefinition, type RegistryLocaleState } from "./registry-locale-model.ts";

const initialRegistry: RegistryLocaleState = { locale: renderingFallback(DEFAULT_LOCALE), loaded: false, available: false, requestedPrefix: null, languages: [], panel: false };
const LocaleContext = createContext<{
  locale: Locale;
  registry: RegistryLocaleState;
  localizePath: (internalPath: string) => string;
  switchLocalePath: (target: Locale) => string;
  switchRegistryPath: (target: string) => string | null;
}>({
  locale: DEFAULT_LOCALE,
  registry: initialRegistry,
  localizePath: path => localePath(DEFAULT_LOCALE, path),
  switchLocalePath: target => localePath(target, "/"),
  switchRegistryPath: () => null,
});

function isPanel(pathname: string) {
  return /^\/(admin|vendor-panel)(?:\/|$)/.test(pathname) || Boolean(parseRegistryPanelPrefix(pathname));
}

function browserCookie(): string | null {
  if (typeof document === "undefined") return null;
  const raw = document.cookie.split(";").map(x => x.trim()).find(x => x.startsWith(LOCALE_COOKIE_NAME + "="))?.slice(LOCALE_COOKIE_NAME.length + 1);
  try { return raw ? decodeURIComponent(raw) : null; } catch { return null; }
}

function panelPath(prefix: string, pathname: string): string {
  const bare = parseRegistryPanelPrefix(pathname)?.pathname ?? pathname;
  return "/" + prefix + bare;
}

/** One locale provider. Panels use authoritative registry metadata; public SEO keeps its existing resource contract. */
export function LocaleProvider({ locale: serverLocale, requestedPrefix = null, children }: { locale: Locale; requestedPrefix?: string | null; children: ReactNode }) {
  const route = usePathname() ?? "/";
  const router = useRouter();
  const [path, setPath] = useState(route);
  const [preferred, setPreferred] = useState(requestedPrefix);
  const [rows, setRows] = useState<RegistryLocaleDefinition[] | null>(null);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    const sync = () => { setPath(window.location.pathname); setPreferred(browserCookie() ?? requestedPrefix); };
    sync();
    window.addEventListener("popstate", sync);
    return () => window.removeEventListener("popstate", sync);
  }, [route, requestedPrefix]);
  const panel = isPanel(path);
  const publicLocale = parseLocalePrefix(path)?.locale ?? serverLocale;
  useEffect(() => {
    if (!panel) return;
    let active = true;
    void loadActiveRegistryLanguages().then(next => {
      if (active) { setRows(next); setLoaded(true); }
    }).catch(() => {
      if (active) { setRows(null); setLoaded(true); }
    });
    return () => { active = false; };
  }, [panel]);
  const requested = parseRegistryPanelPrefix(path)?.prefix ?? preferred;
  const selected = panel && rows ? resolveRegistryLanguage(rows, requested) : null;
  const registry = useMemo<RegistryLocaleState>(() => ({
    locale: selected ?? renderingFallback(publicLocale), loaded: panel ? loaded : true,
    available: selected !== null, requestedPrefix: requested, languages: panel ? rows ?? [] : [], panel,
  }), [selected, publicLocale, loaded, requested, rows, panel]);
  // Resource fallback is not the authority for enabled languages, culture, calendar or direction.
  const locale: Locale = panel ? (registry.locale.language === "fa" ? "fa" : "en") : publicLocale;

  useLayoutEffect(() => {
    document.documentElement.lang = panel ? registry.locale.culture : langForLocale(locale);
    document.documentElement.dir = panel ? registry.locale.direction : dirForLocale(locale);
  }, [panel, registry.locale, locale]);

  useEffect(() => {
    if (!panel || !registry.available) return;
    const prefix = registry.locale.urlPrefix;
    const secure = window.location.protocol === "https:" ? "; Secure" : "";
    document.cookie = LOCALE_COOKIE_NAME + "=" + encodeURIComponent(prefix) + "; Path=/; Max-Age=31536000; SameSite=Lax" + secure;
    const actual = parseRegistryPanelPrefix(window.location.pathname);
    if (actual && actual.prefix !== prefix) router.replace(panelPath(prefix, actual.pathname) + window.location.search + window.location.hash);
  }, [panel, registry.available, registry.locale.urlPrefix, path, router]);

  const localizePath = useCallback((internalPath: string) => {
    if (isPanel(internalPath)) return registry.available ? panelPath(registry.locale.urlPrefix, internalPath) : (parseRegistryPanelPrefix(internalPath)?.pathname ?? internalPath);
    return localePath(locale, internalPath);
  }, [registry.available, registry.locale.urlPrefix, locale]);
  const switchLocalePath = useCallback((target: Locale) => localePath(target, typeof window === "undefined" ? "/" : stripLocalePrefix(window.location.pathname)), []);
  const switchRegistryPath = useCallback((target: string) => {
    const entry = registry.languages.find(row => row.active && row.urlPrefix === target);
    if (!panel || !entry) return null;
    return typeof window === "undefined"
      ? registryPanelSwitchPath(entry.urlPrefix, path)
      : registryPanelSwitchPath(entry.urlPrefix, window.location.pathname, window.location.search, window.location.hash);
  }, [registry.languages, panel, path]);
  const value = useMemo(() => ({ locale, registry, localizePath, switchLocalePath, switchRegistryPath }), [locale, registry, localizePath, switchLocalePath, switchRegistryPath]);
  return <LocaleContext.Provider value={value}>{children}</LocaleContext.Provider>;
}

export function useLocale(): Locale { return useContext(LocaleContext).locale; }
export function useRegistryLocale(): RegistryLocaleState { return useContext(LocaleContext).registry; }
export function useLocalizedPath(): (internalPath: string) => string { return useContext(LocaleContext).localizePath; }
export function useSwitchLocalePath(): (target: Locale) => string { return useContext(LocaleContext).switchLocalePath; }
export function useSwitchRegistryPath(): (target: string) => string | null { return useContext(LocaleContext).switchRegistryPath; }
