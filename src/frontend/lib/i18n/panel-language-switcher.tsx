"use client";

import { useRouter } from "next/navigation";
import { useRegistryLocale, useSwitchRegistryPath } from "./locale-context";

/** Shared panel control; registry supplies every available choice, never the resource fallback. */
export function PanelLanguageSwitcher() {
  const registry = useRegistryLocale();
  const switchPath = useSwitchRegistryPath();
  const router = useRouter();
  if (!registry.available || registry.languages.length < 2) return null;
  const label = registry.locale.language === "fa" ? "زبان" : registry.locale.language === "ru" ? "Язык" : "Language";
  return (
    <select
      className="min-h-11 max-w-[6.5rem] sm:max-w-[10rem] rounded-lg border border-gray-200 bg-white px-2 text-sm text-gray-800"
      aria-label={label}
      title={label}
      data-testid="panel-language-select"
      value={registry.locale.urlPrefix}
      onChange={event => {
        const next = switchPath(event.target.value);
        if (next) router.push(next, { scroll: false });
      }}
    >
      {registry.languages.map(row => (
        <option key={row.urlPrefix} value={row.urlPrefix} lang={row.culture}>{row.nativeName}</option>
      ))}
    </select>
  );
}
