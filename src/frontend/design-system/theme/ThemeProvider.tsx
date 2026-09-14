"use client";

import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import type { ColorScheme, TextDirection, ThemeContract } from "./types";

interface ThemeContextValue {
  theme: ThemeContract;
  setColorScheme: (scheme: ColorScheme) => void;
  setDirection: (direction: TextDirection) => void;
}

const ThemeContext = createContext<ThemeContextValue | null>(null);

function readDocumentScheme(): ColorScheme {
  if (typeof document === "undefined") {
    return "light";
  }
  return document.documentElement.classList.contains("dark") ? "dark" : "light";
}

/**
 * تم کلاس‌محور را با SSR هماهنگ می‌کند؛ اسکریپت تم از پایگاه‌داده اجرا نمی‌شود.
 */
export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setTheme] = useState<ThemeContract>({ colorScheme: "light", direction: "rtl" });

  useEffect(() => {
    setTheme((current) => ({ ...current, colorScheme: readDocumentScheme() }));
  }, []);

  const value = useMemo<ThemeContextValue>(
    () => ({
      theme,
      setColorScheme: (colorScheme) => setTheme((current) => ({ ...current, colorScheme })),
      setDirection: (direction) => setTheme((current) => ({ ...current, direction })),
    }),
    [theme],
  );

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

/**
 * قرارداد تم جاری را برای اجزای پوسته برمی‌گرداند.
 */
export function useTheme(): ThemeContextValue {
  const value = useContext(ThemeContext);
  if (!value) {
    throw new Error("useTheme must be used within ThemeProvider");
  }
  return value;
}
