import { adminHeaders } from "./admin-api";
import {
  isKnownPaletteKey,
  listStorefrontPalettes,
  resolveBrandTokens,
  resolvePaletteKey,
  type StorefrontBrandTokens,
  type StorefrontPaletteDefinition,
} from "../../lib/storefront-appearance/palette-registry.ts";
import { resolveThemeMode } from "../../lib/storefront-appearance/theme-mode.ts";

export interface AppearanceSettingsView {
  storeScope: string;
  paletteKey: string;
  paletteKeyWasKnown: boolean;
  themeMode: string;
  tokens: StorefrontBrandTokens;
  presets: StorefrontPaletteDefinition[];
}

function mapTokens(raw: unknown): StorefrontBrandTokens {
  const row = raw && typeof raw === "object" ? raw as Record<string, unknown> : {};
  const fallback = resolveBrandTokens("tooba-blue");
  return {
    primaryRgb: String(row.primaryRgb ?? fallback.primaryRgb),
    primaryStrongRgb: String(row.primaryStrongRgb ?? fallback.primaryStrongRgb),
    onPrimaryRgb: String(row.onPrimaryRgb ?? fallback.onPrimaryRgb),
    focusRgb: String(row.focusRgb ?? fallback.focusRgb),
    primaryOnDarkRgb: String(row.primaryOnDarkRgb ?? fallback.primaryOnDarkRgb),
  };
}

function mapView(payload: unknown): AppearanceSettingsView | null {
  if (!payload || typeof payload !== "object") {
    return null;
  }
  const row = payload as Record<string, unknown>;
  const paletteKey = resolvePaletteKey(typeof row.paletteKey === "string" ? row.paletteKey : null);
  const presets = Array.isArray(row.presets)
    ? row.presets
      .map((item) => {
        if (!item || typeof item !== "object") {
          return null;
        }
        const preset = item as Record<string, unknown>;
        const key = typeof preset.key === "string" ? preset.key : "";
        if (!isKnownPaletteKey(key)) {
          return null;
        }
        return {
          key,
          nameFa: String(preset.nameFa ?? key),
          nameEn: String(preset.nameEn ?? key),
          tokens: mapTokens(preset.tokens),
        } satisfies StorefrontPaletteDefinition;
      })
      .filter((item): item is StorefrontPaletteDefinition => item !== null)
    : [...listStorefrontPalettes()];
  return {
    storeScope: String(row.storeScope ?? "default"),
    paletteKey,
    paletteKeyWasKnown: row.paletteKeyWasKnown !== false,
    themeMode: resolveThemeMode(typeof row.themeMode === "string" ? row.themeMode : null),
    tokens: mapTokens(row.tokens),
    presets: presets.length > 0 ? presets : [...listStorefrontPalettes()],
  };
}

export async function loadAppearanceSettings(): Promise<
  { ok: true; data: AppearanceSettingsView } | { ok: false; denied?: boolean }
> {
  const response = await fetch("/v1/admin/settings/appearance", { headers: adminHeaders() });
  if (response.status === 401 || response.status === 403) {
    return { ok: false, denied: true };
  }
  if (!response.ok) {
    return { ok: false };
  }
  const data = mapView(await response.json().catch(() => null));
  return data ? { ok: true, data } : { ok: false };
}

export async function saveAppearanceSettings(
  paletteKey: string,
  themeMode: string,
): Promise<{ ok: true; data: AppearanceSettingsView } | { ok: false; denied?: boolean; message?: string }> {
  const response = await fetch("/v1/admin/settings/appearance", {
    method: "PUT",
    headers: { ...adminHeaders(), "Content-Type": "application/json" },
    body: JSON.stringify({ paletteKey, themeMode }),
  });
  if (response.status === 401 || response.status === 403) {
    return { ok: false, denied: true };
  }
  if (!response.ok) {
    const payload = await response.json().catch(() => null) as { errorCode?: string } | null;
    return {
      ok: false,
      message: payload?.errorCode === "appearance.palette.invalid"
        ? "پالت انتخاب‌شده مجاز نیست."
        : payload?.errorCode === "appearance.theme.invalid"
          ? "حالت تم انتخاب‌شده مجاز نیست."
          : "ذخیرهٔ ظاهر فروشگاه انجام نشد.",
    };
  }
  const data = mapView(await response.json().catch(() => null));
  return data ? { ok: true, data } : { ok: false, message: "پاسخ تنظیم ظاهر نامعتبر است." };
}
