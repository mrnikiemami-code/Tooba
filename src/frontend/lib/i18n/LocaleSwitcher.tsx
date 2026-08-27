"use client";

import { useEffect, useState } from "react";
import { chromeMessagesFor } from "./messages.ts";
import { readBrowserLocaleCookie, writeBrowserLocaleCookie } from "./locale-cookie.ts";
import { DEFAULT_LOCALE, type Locale } from "./locale.ts";

/**
 * سوییچر فشرده FA|EN مطابق تراکم هدر Shopeiva.
 * فقط locale نمایش را عوض می‌کند؛ market/currency را تغییر نمی‌دهد.
 */
export function LocaleSwitcher({ className = "" }: { className?: string }) {
  const [locale, setLocale] = useState<Locale>(DEFAULT_LOCALE);

  useEffect(() => {
    setLocale(readBrowserLocaleCookie());
  }, []);

  const messages = chromeMessagesFor(locale);

  function select(next: Locale) {
    if (next === locale) return;
    writeBrowserLocaleCookie(next);
    setLocale(next);
    window.location.reload();
  }

  return (
    <div
      className={`inline-flex items-center gap-0.5 text-[11px] font-bold text-gray-500 ${className}`}
      role="group"
      aria-label={messages.localeSwitcherLabel}
      data-testid="locale-switcher"
    >
      <button
        type="button"
        className={`px-1.5 py-0.5 rounded transition-colors ${
          locale === "fa" ? "text-[#2563EB] bg-[#2563EB]/10" : "hover:text-gray-800"
        }`}
        aria-pressed={locale === "fa"}
        onClick={() => select("fa")}
      >
        {messages.localeFa}
      </button>
      <span className="text-gray-300" aria-hidden="true">
        |
      </span>
      <button
        type="button"
        className={`px-1.5 py-0.5 rounded transition-colors ${
          locale === "en" ? "text-[#2563EB] bg-[#2563EB]/10" : "hover:text-gray-800"
        }`}
        aria-pressed={locale === "en"}
        onClick={() => select("en")}
      >
        {messages.localeEn}
      </button>
    </div>
  );
}
