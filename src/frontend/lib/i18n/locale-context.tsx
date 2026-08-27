"use client";

import { createContext, useCallback, useContext, useMemo, type ReactNode } from "react";
import { DEFAULT_LOCALE, type Locale } from "./locale.ts";
import { localePath, stripLocalePrefix } from "./routing.ts";

const LocaleContext = createContext<{
  locale: Locale;
  localizePath: (internalPath: string) => string;
  switchLocalePath: (target: Locale) => string;
}>({
  locale: DEFAULT_LOCALE,
  localizePath: (path) => localePath(DEFAULT_LOCALE, path),
  switchLocalePath: (target) => localePath(target, "/"),
});

/** locale فعال از URL (توسط layout/parent تزریق می‌شود). */
export function LocaleProvider({ locale, children }: { locale: Locale; children: ReactNode }) {
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
