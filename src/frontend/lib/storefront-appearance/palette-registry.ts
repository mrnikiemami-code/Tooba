export const DEFAULT_PALETTE_KEY = "tooba-blue";
export const DEFAULT_PRIMARY_HEX = "#2563EB";
export const DEFAULT_PRIMARY_STRONG_HEX = "#1d4ed8";

export interface StorefrontBrandTokens {
  primaryRgb: string;
  primaryStrongRgb: string;
  onPrimaryRgb: string;
  focusRgb: string;
  /** Brand-emphasis / text-primary / link / focus on dark paper. CTA stays primaryRgb. */
  primaryOnDarkRgb: string;
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
  primaryOnDarkRgb: "59 115 237",
};

export const STOREFRONT_PALETTES: readonly StorefrontPaletteDefinition[] = [
  { key: "tooba-blue", nameFa: "آبی توبا", nameEn: "Tooba Blue", tokens: TOOBA_BLUE },
  { key: "forest-green", nameFa: "سبز جنگلی", nameEn: "Forest Green", tokens: { primaryRgb: "21 128 61", primaryStrongRgb: "22 101 52", onPrimaryRgb: "255 255 255", focusRgb: "21 128 61", primaryOnDarkRgb: "42 139 78" } },
  { key: "wine-burgundy", nameFa: "شرابی تیره", nameEn: "Wine Burgundy", tokens: { primaryRgb: "159 18 57", primaryStrongRgb: "136 19 55", onPrimaryRgb: "255 255 255", focusRgb: "159 18 57", primaryOnDarkRgb: "189 91 118" } },
  { key: "slate-navy", nameFa: "سرمه‌ای سنگی", nameEn: "Slate Navy", tokens: { primaryRgb: "30 58 95", primaryStrongRgb: "23 37 84", onPrimaryRgb: "255 255 255", focusRgb: "30 58 95", primaryOnDarkRgb: "104 123 148" } },
  { key: "amber-gold", nameFa: "کهربایی", nameEn: "Amber Gold", tokens: { primaryRgb: "180 83 9", primaryStrongRgb: "146 64 14", onPrimaryRgb: "255 255 255", focusRgb: "180 83 9", primaryOnDarkRgb: "187 98 31" } },
  { key: "teal-lagoon", nameFa: "سبزآبی مرداب", nameEn: "Teal Lagoon", tokens: { primaryRgb: "15 118 110", primaryStrongRgb: "17 94 89", onPrimaryRgb: "255 255 255", focusRgb: "15 118 110", primaryOnDarkRgb: "46 136 129" } },
  { key: "violet-royal", nameFa: "بنفش سلطنتی", nameEn: "Royal Violet", tokens: { primaryRgb: "124 58 237", primaryStrongRgb: "109 40 217", onPrimaryRgb: "255 255 255", focusRgb: "124 58 237", primaryOnDarkRgb: "144 88 240" } },
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

export function appearanceCssVars(tokens: StorefrontBrandTokens): Record<string, string> {
  return {
    "--color-primary": tokens.primaryRgb,
    "--color-primary-strong": tokens.primaryStrongRgb,
    "--color-primary-foreground": tokens.onPrimaryRgb,
    "--color-focus": tokens.focusRgb,
    "--color-primary-on-dark": tokens.primaryOnDarkRgb,
    "--ref-brand": tokens.primaryRgb,
  };
}
