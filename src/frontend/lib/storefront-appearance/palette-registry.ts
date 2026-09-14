export const DEFAULT_PALETTE_KEY = "tooba-blue";
export const DEFAULT_PRIMARY_HEX = "#2563EB";
export const DEFAULT_PRIMARY_STRONG_HEX = "#1d4ed8";

export interface StorefrontBrandTokens {
  primaryRgb: string;
  primaryStrongRgb: string;
  onPrimaryRgb: string;
  focusRgb: string;
}

export interface StorefrontPaletteDefinition {
  key: string;
  nameFa: string;
  nameEn: string;
  tokens: StorefrontBrandTokens;
}

const TOOBA_BLUE: StorefrontBrandTokens = {
  primaryRgb: "37 99 235",
  primaryStrongRgb: "29 78 216",
  onPrimaryRgb: "255 255 255",
  focusRgb: "37 99 235",
};

export const STOREFRONT_PALETTES: readonly StorefrontPaletteDefinition[] = [
  { key: "tooba-blue", nameFa: "آبی توبا", nameEn: "Tooba Blue", tokens: TOOBA_BLUE },
  { key: "forest-green", nameFa: "سبز جنگلی", nameEn: "Forest Green", tokens: { primaryRgb: "21 128 61", primaryStrongRgb: "22 101 52", onPrimaryRgb: "255 255 255", focusRgb: "21 128 61" } },
  { key: "wine-burgundy", nameFa: "شرابی تیره", nameEn: "Wine Burgundy", tokens: { primaryRgb: "159 18 57", primaryStrongRgb: "136 19 55", onPrimaryRgb: "255 255 255", focusRgb: "159 18 57" } },
  { key: "slate-navy", nameFa: "سرمه‌ای سنگی", nameEn: "Slate Navy", tokens: { primaryRgb: "30 58 95", primaryStrongRgb: "23 37 84", onPrimaryRgb: "255 255 255", focusRgb: "30 58 95" } },
  { key: "amber-gold", nameFa: "کهربایی", nameEn: "Amber Gold", tokens: { primaryRgb: "217 119 6", primaryStrongRgb: "180 83 9", onPrimaryRgb: "255 255 255", focusRgb: "217 119 6" } },
  { key: "teal-lagoon", nameFa: "سبزآبی مرداب", nameEn: "Teal Lagoon", tokens: { primaryRgb: "15 118 110", primaryStrongRgb: "17 94 89", onPrimaryRgb: "255 255 255", focusRgb: "15 118 110" } },
  { key: "violet-royal", nameFa: "بنفش سلطنتی", nameEn: "Royal Violet", tokens: { primaryRgb: "124 58 237", primaryStrongRgb: "109 40 217", onPrimaryRgb: "255 255 255", focusRgb: "124 58 237" } },
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

export function listStorefrontPalettes(): readonly StorefrontPaletteDefinition[] {
  return STOREFRONT_PALETTES;
}

export function hexToRgbTriple(hex: string): string {
  const value = hex.replace("#", "");
  const n = Number.parseInt(value, 16);
  return `${(n >> 16) & 255} ${(n >> 8) & 255} ${n & 255}`;
}

export function appearanceCssVars(tokens: StorefrontBrandTokens): Record<string, string> {
  return {
    "--color-primary": tokens.primaryRgb,
    "--color-primary-strong": tokens.primaryStrongRgb,
    "--color-primary-foreground": tokens.onPrimaryRgb,
    "--color-focus": tokens.focusRgb,
    "--ref-brand": tokens.primaryRgb,
  };
}
