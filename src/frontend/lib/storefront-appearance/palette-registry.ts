export const DEFAULT_PALETTE_KEY = "tooba-blue";
export const DEFAULT_PRIMARY_HEX = "#2563EB";
export const DEFAULT_PRIMARY_STRONG_HEX = "#1d4ed8";

export interface StorefrontBrandTokens {
  primaryRgb: string;
  primaryStrongRgb: string;
  onPrimaryRgb: string;
  focusRgb: string;
}

const TOOBA_BLUE: StorefrontBrandTokens = {
  primaryRgb: "37 99 235",
  primaryStrongRgb: "29 78 216",
  onPrimaryRgb: "255 255 255",
  focusRgb: "37 99 235",
};

const REGISTRY: Record<string, StorefrontBrandTokens> = {
  [DEFAULT_PALETTE_KEY]: TOOBA_BLUE,
};

/** کلید ناشناخته به پالت پیش‌فرض برمی‌گردد؛ CSS اجرایی از دیتابیس پذیرفته نمی‌شود. */
export function resolvePaletteKey(raw: string | null | undefined): string {
  const key = raw?.trim().toLowerCase();
  return key && REGISTRY[key] ? key : DEFAULT_PALETTE_KEY;
}

export function resolveBrandTokens(raw: string | null | undefined): StorefrontBrandTokens {
  return REGISTRY[resolvePaletteKey(raw)] ?? TOOBA_BLUE;
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
