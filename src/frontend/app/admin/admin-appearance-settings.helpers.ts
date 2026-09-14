import { appearanceCssVars, resolveBrandTokens, resolvePaletteKey, resolveTintTokens, NEUTRAL_PAGE_BACKGROUND_RGB, NEUTRAL_SECTION_SURFACE_RGB, NEUTRAL_SECTION_ALTERNATE_RGB, NEUTRAL_SECTION_ACCENT_RGB, NEUTRAL_PAGE_BACKGROUND_DARK_RGB, NEUTRAL_SECTION_SURFACE_DARK_RGB, NEUTRAL_SECTION_ALTERNATE_DARK_RGB, NEUTRAL_SECTION_ACCENT_DARK_RGB } from "../../lib/storefront-appearance/palette-registry.ts";
import { resolveProductCardSkin } from "../../lib/storefront-appearance/product-card-skin.ts";
import { resolveThemeMode, type StorefrontThemeMode } from "../../lib/storefront-appearance/theme-mode.ts";
import { resolveBackgroundStyle, type StorefrontBackgroundStyle } from "../../lib/storefront-appearance/background-style.ts";

export function appearanceIsDirty(
  savedKey: string,
  draftKey: string,
  savedTheme?: string,
  draftTheme?: string,
  savedSkin?: string,
  draftSkin?: string,
  savedBackground?: string,
  draftBackground?: string,
): boolean {
  const paletteDirty = resolvePaletteKey(savedKey) !== resolvePaletteKey(draftKey);
  const themeDirty = savedTheme == null && draftTheme == null
    ? false
    : resolveThemeMode(savedTheme) !== resolveThemeMode(draftTheme);
  const skinDirty = savedSkin == null && draftSkin == null
    ? false
    : resolveProductCardSkin(savedSkin) !== resolveProductCardSkin(draftSkin);
  const backgroundDirty = savedBackground == null && draftBackground == null
    ? false
    : resolveBackgroundStyle(savedBackground) !== resolveBackgroundStyle(draftBackground);
  return paletteDirty || themeDirty || skinDirty || backgroundDirty;
}

export function appearancePreviewStyle(
  paletteKey: string,
  backgroundStyle?: string,
  themeMode?: string,
): Record<string, string> {
  const tokens = resolveBrandTokens(paletteKey);
  const tint = resolveTintTokens(paletteKey);
  const vars = appearanceCssVars(tokens, tint);
  const style = resolveBackgroundStyle(backgroundStyle);
  const dark = resolveThemeMode(themeMode) === "DarkOnly";
  const page = style === "PaletteTint"
    ? (dark ? tint.pageBackgroundDarkRgb : tint.pageBackgroundRgb)
    : (dark ? NEUTRAL_PAGE_BACKGROUND_DARK_RGB : NEUTRAL_PAGE_BACKGROUND_RGB);
  const section = style === "PaletteTint"
    ? (dark ? tint.sectionBackgroundDarkRgb : tint.sectionBackgroundRgb)
    : (dark ? NEUTRAL_SECTION_SURFACE_DARK_RGB : NEUTRAL_SECTION_SURFACE_RGB);
  const alternate = style === "PaletteTint"
    ? (dark ? tint.sectionAlternateDarkRgb : tint.sectionAlternateRgb)
    : (dark ? NEUTRAL_SECTION_ALTERNATE_DARK_RGB : NEUTRAL_SECTION_ALTERNATE_RGB);
  const accent = style === "PaletteTint"
    ? (dark ? tint.sectionAccentDarkRgb : tint.sectionAccentRgb)
    : (dark ? NEUTRAL_SECTION_ACCENT_DARK_RGB : NEUTRAL_SECTION_ACCENT_RGB);
  return {
    ...vars,
    "--color-page-background": page,
    "--color-section-background": section,
    "--color-section-surface": section,
    "--color-section-alternate": alternate,
    "--color-section-accent": accent,
  };
}

export const THEME_MODE_OPTIONS: { key: StorefrontThemeMode; title: string; body: string }[] = [
  { key: "LightOnly", title: "فقط روشن", body: "فروشگاه همیشه روشن است و کلید کاربر نمایش داده نمی‌شود." },
  { key: "DarkOnly", title: "فقط تاریک", body: "فروشگاه همیشه تاریک است و کلید کاربر نمایش داده نمی‌شود." },
  { key: "System", title: "سیستم", body: "از ترجیح روشن/تاریک دستگاه پیروی می‌کند." },
  { key: "UserChoice", title: "انتخاب کاربر", body: "کلید تم در هدر ظاهر می‌شود و ترجیح روی همین دستگاه می‌ماند." },
];

export const BACKGROUND_STYLE_OPTIONS: { key: StorefrontBackgroundStyle; title: string; body: string }[] = [
  { key: "Neutral", title: "خنثی", body: "پس‌زمینهٔ خاکستری روشن فعلی. پالت فقط روی دکمه‌ها و پیوندها دیده می‌شود." },
  { key: "PaletteTint", title: "رنگی ملایم", body: "ته‌رنگ خیلی کم از خانوادهٔ پالت انتخاب‌شده. کارت‌ها سفید می‌مانند." },
];
