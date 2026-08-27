"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { DEFAULT_LOCALE, dirForLocale, langForLocale, type Locale } from "./locale.ts";
import { localePath, parseLocalePrefix, stripLocalePrefix } from "./routing.ts";

const LocaleContext = createContext<{
  locale: Locale;
  localizePath: (internalPath: string) => string;
  switchLocalePath: (target: Locale) => string;
}>({
  locale: DEFAULT_LOCALE,
  localizePath: (path) => localePath(DEFAULT_LOCALE, path),
  switchLocalePath: (target) => localePath(target, "/"),
});

function readPublicLocale(fallback: Locale): Locale {
  if (typeof window === "undefined") return fallback;
  return parseLocalePrefix(window.location.pathname)?.locale ?? fallback;
}

/** locale فعال از URL عمومی (prefix) — canonical برای لینک و dir/lang. */
export function LocaleProvider({ locale: serverLocale, children }: { locale: Locale; children: ReactNode }) {
  const [locale, setLocale] = useState<Locale>(serverLocale);

  useEffect(() => {
    setLocale(readPublicLocale(serverLocale));
  }, [serverLocale]);

  useEffect(() => {
    const syncFromUrl = () => setLocale(readPublicLocale(serverLocale));
    window.addEventListener("popstate", syncFromUrl);
    return () => window.removeEventListener("popstate", syncFromUrl);
  }, [serverLocale]);

  useEffect(() => {
    document.documentElement.lang = langForLocale(locale);
    document.documentElement.dir = dirForLocale(locale);
  }, [locale]);

  const localizePath = useCallback((internalPath: string) => localePath(locale, internalPath), [locale]);
  const switchLocalePath = useCallback(
    (target: Locale) => {
      if (typeof window === "undefined") return localePath(target, "/");
      const bare = stripLocalePrefix(window.location.pathname);
      return localePath(target, bare);
    },
    [],
  );
  const value = useMemo(
    () => ({ locale, localizePath, switchLocalePath }),
    [locale, localizePath, switchLocalePath],
  );

  return <LocaleContext.Provider value={value}>{children}</LocaleContext.Provider>;
}

export function useLocale(): Locale {
  return useContext(LocaleContext).locale;
}

export function useLocalizedPath(): (internalPath: string) => string {
  return useContext(LocaleContext).localizePath;
}

export function useSwitchLocalePath(): (target: Locale) => string {
  return useContext(LocaleContext).switchLocalePath;
}
