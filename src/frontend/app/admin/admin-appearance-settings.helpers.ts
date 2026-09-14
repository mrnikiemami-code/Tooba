import { appearanceCssVars, resolveBrandTokens, resolvePaletteKey } from "../../lib/storefront-appearance/palette-registry.ts";
import { resolveThemeMode, type StorefrontThemeMode } from "../../lib/storefront-appearance/theme-mode.ts";

export function appearanceIsDirty(
  savedKey: string,
  draftKey: string,
  savedTheme?: string,
  draftTheme?: string,
): boolean {
  const paletteDirty = resolvePaletteKey(savedKey) !== resolvePaletteKey(draftKey);
  if (savedTheme == null && draftTheme == null) {
    return paletteDirty;
  }
  return paletteDirty || resolveThemeMode(savedTheme) !== resolveThemeMode(draftTheme);
}

export function appearancePreviewStyle(paletteKey: string): Record<string, string> {
  return appearanceCssVars(resolveBrandTokens(paletteKey));
}

export const THEME_MODE_OPTIONS: { key: StorefrontThemeMode; title: string; body: string }[] = [
  { key: "LightOnly", title: "فقط روشن", body: "فروشگاه همیشه روشن است و کلید کاربر نمایش داده نمی‌شود." },
  { key: "DarkOnly", title: "فقط تاریک", body: "فروشگاه همیشه تاریک است و کلید کاربر نمایش داده نمی‌شود." },
  { key: "System", title: "سیستم", body: "از ترجیح روشن/تاریک دستگاه پیروی می‌کند." },
  { key: "UserChoice", title: "انتخاب کاربر", body: "کلید تم در هدر ظاهر می‌شود و ترجیح روی همین دستگاه می‌ماند." },
];
