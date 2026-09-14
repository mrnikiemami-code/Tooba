import { derivedSurfaceCssVars } from "./derived-surface.ts";

export const DEFAULT_PALETTE_KEY = "tooba-blue";
export const DEFAULT_PRIMARY_HEX = "#2563EB";
export const DEFAULT_PRIMARY_STRONG_HEX = "#1d4ed8";
/** Current storefront shell canvas (#f3f5f8) for Neutral. */
export const NEUTRAL_PAGE_BACKGROUND_RGB = "243 245 248";
export const NEUTRAL_SECTION_SURFACE_RGB = "255 255 255";
export const NEUTRAL_SECTION_ALTERNATE_RGB = "247 248 250";
export const NEUTRAL_SECTION_ACCENT_RGB = "238 242 247";
export const NEUTRAL_PAGE_BACKGROUND_DARK_RGB = "12 12 14";
export const NEUTRAL_SECTION_SURFACE_DARK_RGB = "28 28 32";
export const NEUTRAL_SECTION_ALTERNATE_DARK_RGB = "22 22 26";
export const NEUTRAL_SECTION_ACCENT_DARK_RGB = "34 38 48";

export interface StorefrontBrandTokens {
  primaryRgb: string;
  primaryStrongRgb: string;
  onPrimaryRgb: string;
  focusRgb: string;
  /** Brand-emphasis / text-primary / link / focus on dark paper. CTA stays primaryRgb. */
  primaryOnDarkRgb: string;
}

export interface StorefrontTintTokens {
  pageBackgroundRgb: string;
  /** T015 name; SectionSurface. */
  sectionBackgroundRgb: string;
  sectionAlternateRgb: string;
  sectionAccentRgb: string;
  pageBackgroundDarkRgb: string;
  sectionBackgroundDarkRgb: string;
  sectionAlternateDarkRgb: string;
  sectionAccentDarkRgb: string;
}

export interface StorefrontPaletteDefinition {
  key: string;
  nameFa: string;
  nameEn: string;
  descriptionFa: string;
  tokens: StorefrontBrandTokens;
  tint: StorefrontTintTokens;
}

const TOOBA_BLUE: StorefrontBrandTokens = {
  primaryRgb: "37 99 235",
  primaryStrongRgb: "29 78 216",
  onPrimaryRgb: "255 255 255",
  focusRgb: "37 99 235",
  primaryOnDarkRgb: "59 115 237",
};

const TOOBA_BLUE_TINT: StorefrontTintTokens = {
  pageBackgroundRgb: "228 236 248",
  sectionBackgroundRgb: "247 250 253",
  sectionAlternateRgb: "216 228 244",
  sectionAccentRgb: "188 210 236",
  pageBackgroundDarkRgb: "12 15 22",
  sectionBackgroundDarkRgb: "22 26 36",
  sectionAlternateDarkRgb: "15 18 27",
  sectionAccentDarkRgb: "32 40 56",
};

export const STOREFRONT_PALETTES: readonly StorefrontPaletteDefinition[] = [
  { key: "tooba-blue", nameFa: "آبی توبا", nameEn: "Tooba Blue", descriptionFa: "پالت پیش‌فرض برند؛ آبی روشن و قابل اعتماد برای فروش روزمره.", tokens: TOOBA_BLUE, tint: TOOBA_BLUE_TINT },
  { key: "forest-green", nameFa: "سبز جنگلی", nameEn: "Forest Green", descriptionFa: "فضای طبیعی و آرام برای کالای سلامت یا فضای سبز.", tokens: { primaryRgb: "21 128 61", primaryStrongRgb: "22 101 52", onPrimaryRgb: "255 255 255", focusRgb: "21 128 61", primaryOnDarkRgb: "42 139 78" }, tint: { pageBackgroundRgb: "228 242 232", sectionBackgroundRgb: "247 251 247", sectionAlternateRgb: "214 234 220", sectionAccentRgb: "188 224 200", pageBackgroundDarkRgb: "12 16 13", sectionBackgroundDarkRgb: "20 26 22", sectionAlternateDarkRgb: "14 20 16", sectionAccentDarkRgb: "26 38 28" } },
  { key: "wine-burgundy", nameFa: "شرابی تیره", nameEn: "Wine Burgundy", descriptionFa: "حس لوکس و رسمی برای کالای ویژه و مناسبتی.", tokens: { primaryRgb: "159 18 57", primaryStrongRgb: "136 19 55", onPrimaryRgb: "255 255 255", focusRgb: "159 18 57", primaryOnDarkRgb: "189 91 118" }, tint: { pageBackgroundRgb: "244 232 236", sectionBackgroundRgb: "252 248 249", sectionAlternateRgb: "236 218 224", sectionAccentRgb: "222 192 204", pageBackgroundDarkRgb: "18 12 15", sectionBackgroundDarkRgb: "26 16 20", sectionAlternateDarkRgb: "20 13 16", sectionAccentDarkRgb: "36 18 26" } },
  { key: "slate-navy", nameFa: "سرمه‌ای سنگی", nameEn: "Slate Navy", descriptionFa: "حرفه‌ای و خنثی برای فروشگاه‌های سازمانی.", tokens: { primaryRgb: "30 58 95", primaryStrongRgb: "23 37 84", onPrimaryRgb: "255 255 255", focusRgb: "30 58 95", primaryOnDarkRgb: "104 123 148" }, tint: { pageBackgroundRgb: "228 234 246", sectionBackgroundRgb: "246 248 252", sectionAlternateRgb: "216 224 238", sectionAccentRgb: "190 206 228", pageBackgroundDarkRgb: "12 14 20", sectionBackgroundDarkRgb: "22 25 34", sectionAlternateDarkRgb: "15 17 24", sectionAccentDarkRgb: "30 36 48" } },
  { key: "amber-gold", nameFa: "کهربایی", nameEn: "Amber Gold", descriptionFa: "گرم و دعوت‌کننده برای کالای خانگی یا هدیه.", tokens: { primaryRgb: "180 83 9", primaryStrongRgb: "146 64 14", onPrimaryRgb: "255 255 255", focusRgb: "180 83 9", primaryOnDarkRgb: "187 98 31" }, tint: { pageBackgroundRgb: "246 238 224", sectionBackgroundRgb: "252 249 244", sectionAlternateRgb: "240 226 206", sectionAccentRgb: "228 208 176", pageBackgroundDarkRgb: "18 14 10", sectionBackgroundDarkRgb: "26 20 14", sectionAlternateDarkRgb: "20 15 11", sectionAccentDarkRgb: "38 26 16" } },
  { key: "teal-lagoon", nameFa: "سبزآبی مرداب", nameEn: "Teal Lagoon", descriptionFa: "تازه و مدرن برای کالای دیجیتال یا سفر.", tokens: { primaryRgb: "15 118 110", primaryStrongRgb: "17 94 89", onPrimaryRgb: "255 255 255", focusRgb: "15 118 110", primaryOnDarkRgb: "46 136 129" }, tint: { pageBackgroundRgb: "226 240 238", sectionBackgroundRgb: "244 250 249", sectionAlternateRgb: "210 232 228", sectionAccentRgb: "182 220 214", pageBackgroundDarkRgb: "11 17 17", sectionBackgroundDarkRgb: "18 26 26", sectionAlternateDarkRgb: "13 20 20", sectionAccentDarkRgb: "22 36 34" } },
  { key: "violet-royal", nameFa: "بنفش سلطنتی", nameEn: "Royal Violet", descriptionFa: "متمایز و خلاق برای برندهای جسور؛ اشباع کنترل‌شده.", tokens: { primaryRgb: "124 58 237", primaryStrongRgb: "109 40 217", onPrimaryRgb: "255 255 255", focusRgb: "124 58 237", primaryOnDarkRgb: "144 88 240" }, tint: { pageBackgroundRgb: "236 228 248", sectionBackgroundRgb: "248 245 252", sectionAlternateRgb: "224 212 242", sectionAccentRgb: "206 190 234", pageBackgroundDarkRgb: "15 12 22", sectionBackgroundDarkRgb: "24 18 34", sectionAlternateDarkRgb: "17 13 24", sectionAccentDarkRgb: "34 24 46" } },
];

const REGISTRY: Record<string, StorefrontPaletteDefinition> = Object.fromEntries(
  STOREFRONT_PALETTES.map((item) => [item.key, item]),
);

/** کلید ناشناخته به پالت پیش‌فرض برمی‌گردد؛ CSS اجرایی از دیتابیس پذیرفته نمی‌شود. */
export function resolvePaletteKey(raw: string | null | undefined): string {
  const key = raw?.trim().toLowerCase();
  return key && REGISTRY[key] ? key : DEFAULT_PALETTE_KEY;
}

export function isKnownPaletteKey(raw: string | null | undefined): boolean {
  const key = raw?.trim().toLowerCase();
  return Boolean(key && REGISTRY[key]);
}

export function resolveBrandTokens(raw: string | null | undefined): StorefrontBrandTokens {
  return REGISTRY[resolvePaletteKey(raw)]?.tokens ?? TOOBA_BLUE;
}

export function resolveTintTokens(raw: string | null | undefined): StorefrontTintTokens {
  return REGISTRY[resolvePaletteKey(raw)]?.tint ?? TOOBA_BLUE_TINT;
}

export function listStorefrontPalettes(): readonly StorefrontPaletteDefinition[] {
  return STOREFRONT_PALETTES;
}

export function hexToRgbTriple(hex: string): string {
  const value = hex.replace("#", "");
  const n = Number.parseInt(value, 16);
  return `${(n >> 16) & 255} ${(n >> 8) & 255} ${n & 255}`;
}

function channelLuminance(value: number): number {
  const channel = value / 255;
  return channel <= 0.03928 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4;
}

/** درخشندگی نسبی WCAG برای سه‌گانه RGB فاصله‌دار. */
export function relativeLuminance(rgb: string): number {
  const [r, g, b] = rgb.split(" ").map((part) => Number.parseInt(part, 10));
  return 0.2126 * channelLuminance(r ?? 0) + 0.7152 * channelLuminance(g ?? 0) + 0.0722 * channelLuminance(b ?? 0);
}

/** نسبت کنتراست WCAG بین دو سه‌گانه RGB. */
export function contrastRatio(first: string, second: string): number {
  const a = relativeLuminance(first);
  const b = relativeLuminance(second);
  const lighter = Math.max(a, b);
  const darker = Math.min(a, b);
  return (lighter + 0.05) / (darker + 0.05);
}

export function appearanceCssVars(tokens: StorefrontBrandTokens, tint: StorefrontTintTokens = TOOBA_BLUE_TINT): Record<string, string> {
  return {
    "--color-primary": tokens.primaryRgb,
    "--color-primary-strong": tokens.primaryStrongRgb,
    "--color-primary-foreground": tokens.onPrimaryRgb,
    "--color-focus": tokens.focusRgb,
    "--color-primary-on-dark": tokens.primaryOnDarkRgb,
    "--ref-brand": tokens.primaryRgb,
    "--color-page-tint": tint.pageBackgroundRgb,
    "--color-section-tint": tint.sectionBackgroundRgb,
    "--color-section-surface-tint": tint.sectionBackgroundRgb,
    "--color-section-alternate-tint": tint.sectionAlternateRgb,
    "--color-section-accent-tint": tint.sectionAccentRgb,
    "--color-page-tint-dark": tint.pageBackgroundDarkRgb,
    "--color-section-tint-dark": tint.sectionBackgroundDarkRgb,
    "--color-section-surface-tint-dark": tint.sectionBackgroundDarkRgb,
    "--color-section-alternate-tint-dark": tint.sectionAlternateDarkRgb,
    "--color-section-accent-tint-dark": tint.sectionAccentDarkRgb,
    ...derivedSurfaceCssVars(tint),
  };
}
