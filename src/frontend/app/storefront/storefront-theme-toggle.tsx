"use client";

import { Moon, Sun } from "lucide-react";
import { useEffect, useState } from "react";
import {
  USER_COLOR_SCHEME_COOKIE,
  USER_COLOR_SCHEME_STORAGE,
  parseUserColorScheme,
  resolveThemeMode,
  type StorefrontColorScheme,
} from "../../lib/storefront-appearance/theme-mode.ts";

function persistScheme(scheme: StorefrontColorScheme) {
  try {
    localStorage.setItem(USER_COLOR_SCHEME_STORAGE, scheme);
  } catch {
    /* preference is best-effort */
  }
  document.cookie = `${USER_COLOR_SCHEME_COOKIE}=${scheme}; path=/; max-age=31536000; samesite=lax`;
}

function applyScheme(scheme: StorefrontColorScheme) {
  document.documentElement.classList.toggle("dark", scheme === "dark");
  document.documentElement.setAttribute("data-storefront-color-scheme", scheme);
}

/**
 * کلید تم فقط در UserChoice؛ یک state برای دسکتاپ و موبایل.
 */
const SCHEME_EVENT = "tooba-storefront-color-scheme";

export function StorefrontThemeToggle({ compact = false }: { compact?: boolean }) {
  const [visible, setVisible] = useState(false);
  const [scheme, setScheme] = useState<StorefrontColorScheme>("light");

  useEffect(() => {
    const mode = resolveThemeMode(document.documentElement.getAttribute("data-storefront-theme-mode"));
    setVisible(mode === "UserChoice");
    const sync = () => {
      const current = document.documentElement.classList.contains("dark") ? "dark" : "light";
      setScheme(current);
    };
    sync();
    window.addEventListener(SCHEME_EVENT, sync);
    return () => window.removeEventListener(SCHEME_EVENT, sync);
  }, []);

  if (!visible) {
    return null;
  }

  function toggle() {
    const next: StorefrontColorScheme = scheme === "dark" ? "light" : "dark";
    persistScheme(next);
    applyScheme(next);
    setScheme(next);
    window.dispatchEvent(new Event(SCHEME_EVENT));
  }

  const label = scheme === "dark" ? "تغییر به تم روشن" : "تغییر به تم تاریک";
  return (
    <button
      type="button"
      className={compact
        ? "flex items-center gap-3 w-full p-3 rounded-xl hover:bg-gray-100 text-gray-700"
        : "w-10 h-10 rounded-xl hover:bg-gray-100 flex items-center justify-center text-gray-600"}
      aria-label={label}
      aria-pressed={scheme === "dark"}
      data-testid="storefront-theme-toggle"
      data-color-scheme={scheme}
      onClick={toggle}
    >
      {scheme === "dark" ? <Sun className="w-5 h-5" /> : <Moon className="w-5 h-5" />}
      {compact ? <span>{label}</span> : null}
    </button>
  );
}

export function readStoredUserScheme(): StorefrontColorScheme | null {
  try {
    return parseUserColorScheme(localStorage.getItem(USER_COLOR_SCHEME_STORAGE));
  } catch {
    return null;
  }
}
