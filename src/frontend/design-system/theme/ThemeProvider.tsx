"use client";

import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import type { ColorScheme, TextDirection, ThemeContract } from "./types";

interface ThemeContextValue {
  theme: ThemeContract;
  setColorScheme: (scheme: ColorScheme) => void;
  setDirection: (direction: TextDirection) => void;
}

const ThemeContext = createContext<ThemeContextValue | null>(null);

/**
 * تم کلاس‌محور را روی `html` اعمال می‌کند تا توکن معنایی light/dark و bidi فعال شود.
 * اسکریپت تم از پایگاه‌داده اجرا نمی‌شود.
 */
export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setTheme] = useState<ThemeContract>({ colorScheme: "light", direction: "rtl" });

  useEffect(() => {
    const root = document.documentElement;
    root.classList.toggle("dark", theme.colorScheme === "dark");
    root.dir = theme.direction;
    root.lang = theme.direction === "rtl" ? "fa" : "en";
  }, [theme]);

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
