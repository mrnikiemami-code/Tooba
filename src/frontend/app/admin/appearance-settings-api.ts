import { adminHeaders } from "./admin-api";
import {
  isKnownPaletteKey,
  listStorefrontPalettes,
  resolveBrandTokens,
  resolvePaletteKey,
  resolveTintTokens,
  type StorefrontBrandTokens,
  type StorefrontPaletteDefinition,
  type StorefrontTintTokens,
} from "../../lib/storefront-appearance/palette-registry.ts";
import {
  DEFAULT_PRODUCT_CARD_SKIN,
  listProductCardSkins,
  resolveProductCardSkin,
  type ProductCardSkinDefinition,
} from "../../lib/storefront-appearance/product-card-skin.ts";
import { resolveThemeMode } from "../../lib/storefront-appearance/theme-mode.ts";
import { DEFAULT_BACKGROUND_STYLE, resolveBackgroundStyle } from "../../lib/storefront-appearance/background-style.ts";

export interface AppearanceSettingsView {
  storeScope: string;
  paletteKey: string;
  paletteKeyWasKnown: boolean;
  themeMode: string;
  productCardSkin: string;
  backgroundStyle: string;
  tokens: StorefrontBrandTokens;
  tint: StorefrontTintTokens;
  presets: StorefrontPaletteDefinition[];
  skins: ProductCardSkinDefinition[];
}

function mapTint(raw: unknown, paletteKey: string): StorefrontTintTokens {
  const row = raw && typeof raw === "object" ? raw as Record<string, unknown> : {};
  const fallback = resolveTintTokens(paletteKey);
  return {
    pageBackgroundRgb: String(row.pageBackgroundRgb ?? fallback.pageBackgroundRgb),
    sectionBackgroundRgb: String(row.sectionBackgroundRgb ?? fallback.sectionBackgroundRgb),
    sectionAlternateRgb: String(row.sectionAlternateRgb ?? fallback.sectionAlternateRgb),
    sectionAccentRgb: String(row.sectionAccentRgb ?? fallback.sectionAccentRgb),
    pageBackgroundDarkRgb: String(row.pageBackgroundDarkRgb ?? fallback.pageBackgroundDarkRgb),
    sectionBackgroundDarkRgb: String(row.sectionBackgroundDarkRgb ?? fallback.sectionBackgroundDarkRgb),
    sectionAlternateDarkRgb: String(row.sectionAlternateDarkRgb ?? fallback.sectionAlternateDarkRgb),
    sectionAccentDarkRgb: String(row.sectionAccentDarkRgb ?? fallback.sectionAccentDarkRgb),
  };
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
        const local = listStorefrontPalettes().find((item) => item.key === key);
        return {
          key,
          nameFa: String(preset.nameFa ?? local?.nameFa ?? key),
          nameEn: String(preset.nameEn ?? local?.nameEn ?? key),
          descriptionFa: local?.descriptionFa ?? "",
          tokens: mapTokens(preset.tokens),
          tint: mapTint(preset.tint, key),
        } satisfies StorefrontPaletteDefinition;
      })
      .filter((item): item is StorefrontPaletteDefinition => item !== null)
    : [...listStorefrontPalettes()];
  const skins = [...listProductCardSkins()];
  return {
    storeScope: String(row.storeScope ?? "default"),
    paletteKey,
    paletteKeyWasKnown: row.paletteKeyWasKnown !== false,
    themeMode: resolveThemeMode(typeof row.themeMode === "string" ? row.themeMode : null),
    productCardSkin: resolveProductCardSkin(typeof row.productCardSkin === "string" ? row.productCardSkin : DEFAULT_PRODUCT_CARD_SKIN),
    backgroundStyle: resolveBackgroundStyle(typeof row.backgroundStyle === "string" ? row.backgroundStyle : DEFAULT_BACKGROUND_STYLE),
    tokens: mapTokens(row.tokens),
    tint: mapTint(row.tint, paletteKey),
    presets: presets.length > 0 ? presets : [...listStorefrontPalettes()],
    skins: skins.length > 0 ? skins : [...listProductCardSkins()],
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
  productCardSkin: string,
  backgroundStyle: string,
): Promise<{ ok: true; data: AppearanceSettingsView } | { ok: false; denied?: boolean; message?: string }> {
  const response = await fetch("/v1/admin/settings/appearance", {
    method: "PUT",
    headers: { ...adminHeaders(), "Content-Type": "application/json" },
    body: JSON.stringify({ paletteKey, themeMode, productCardSkin, backgroundStyle }),
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
          : payload?.errorCode === "appearance.skin.invalid"
            ? "پوستهٔ کارت انتخاب‌شده مجاز نیست."
            : payload?.errorCode === "appearance.background.invalid"
              ? "پس‌زمینهٔ انتخاب‌شده مجاز نیست."
              : "ذخیرهٔ ظاهر فروشگاه انجام نشد.",
    };
  }
  const data = mapView(await response.json().catch(() => null));
  return data ? { ok: true, data } : { ok: false, message: "پاسخ تنظیم ظاهر نامعتبر است." };
}
