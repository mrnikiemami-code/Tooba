import { storefrontHostOrigin } from "./storefront-api.ts";
import {
  appearanceCssVars,
  resolveBrandTokens,
  resolvePaletteKey,
  resolveTintTokens,
  type StorefrontBrandTokens,
  type StorefrontTintTokens,
} from "../../lib/storefront-appearance/palette-registry.ts";
import { resolveThemeMode } from "../../lib/storefront-appearance/theme-mode.ts";
import { DEFAULT_PRODUCT_CARD_SKIN, resolveProductCardSkin } from "../../lib/storefront-appearance/product-card-skin.ts";
import { DEFAULT_BACKGROUND_STYLE, resolveBackgroundStyle } from "../../lib/storefront-appearance/background-style.ts";

export interface StorefrontAppearanceProjection {
  storeScope: string;
  paletteKey: string;
  paletteKeyWasKnown: boolean;
  themeMode: string;
  productCardSkin: string;
  backgroundStyle: string;
  tokens: StorefrontBrandTokens;
  tint: StorefrontTintTokens;
}

const FALLBACK: StorefrontAppearanceProjection = {
  storeScope: "default",
  paletteKey: "tooba-blue",
  paletteKeyWasKnown: true,
  themeMode: "LightOnly",
  productCardSkin: DEFAULT_PRODUCT_CARD_SKIN,
  backgroundStyle: DEFAULT_BACKGROUND_STYLE,
  tokens: resolveBrandTokens("tooba-blue"),
  tint: resolveTintTokens("tooba-blue"),
};

function mapTint(raw: Partial<StorefrontTintTokens> | undefined, paletteKey: string): StorefrontTintTokens {
  const fallback = resolveTintTokens(paletteKey);
  return {
    pageBackgroundRgb: raw?.pageBackgroundRgb ?? fallback.pageBackgroundRgb,
    sectionBackgroundRgb: raw?.sectionBackgroundRgb ?? fallback.sectionBackgroundRgb,
    sectionAlternateRgb: raw?.sectionAlternateRgb ?? fallback.sectionAlternateRgb,
    sectionAccentRgb: raw?.sectionAccentRgb ?? fallback.sectionAccentRgb,
    pageBackgroundDarkRgb: raw?.pageBackgroundDarkRgb ?? fallback.pageBackgroundDarkRgb,
    sectionBackgroundDarkRgb: raw?.sectionBackgroundDarkRgb ?? fallback.sectionBackgroundDarkRgb,
    sectionAlternateDarkRgb: raw?.sectionAlternateDarkRgb ?? fallback.sectionAlternateDarkRgb,
    sectionAccentDarkRgb: raw?.sectionAccentDarkRgb ?? fallback.sectionAccentDarkRgb,
  };
}

/** ظاهر مؤثر Store را برای SSR ریشه می‌خواند؛ شکست = پالت پیش‌فرض بدون flash. */
export async function loadStorefrontAppearance(): Promise<StorefrontAppearanceProjection> {
  try {
    const response = await fetch(`${storefrontHostOrigin()}/v1/storefront/appearance`, {
      cache: "no-store",
      headers: { Accept: "application/json" },
    });
    if (!response.ok) {
      return FALLBACK;
    }
    const payload = await response.json() as {
      storeScope?: string;
      paletteKey?: string;
      paletteKeyWasKnown?: boolean;
      themeMode?: string;
      productCardSkin?: string;
      backgroundStyle?: string;
      tokens?: Partial<StorefrontBrandTokens>;
      tint?: Partial<StorefrontTintTokens>;
    };
    const paletteKey = resolvePaletteKey(payload.paletteKey);
    const defaults = resolveBrandTokens(paletteKey);
    return {
      storeScope: payload.storeScope ?? FALLBACK.storeScope,
      paletteKey,
      paletteKeyWasKnown: payload.paletteKeyWasKnown !== false,
      themeMode: resolveThemeMode(payload.themeMode),
      productCardSkin: resolveProductCardSkin(payload.productCardSkin),
      backgroundStyle: resolveBackgroundStyle(payload.backgroundStyle),
      tokens: {
        primaryRgb: payload.tokens?.primaryRgb ?? defaults.primaryRgb,
        primaryStrongRgb: payload.tokens?.primaryStrongRgb ?? defaults.primaryStrongRgb,
        onPrimaryRgb: payload.tokens?.onPrimaryRgb ?? defaults.onPrimaryRgb,
        focusRgb: payload.tokens?.focusRgb ?? defaults.focusRgb,
        primaryOnDarkRgb: payload.tokens?.primaryOnDarkRgb ?? defaults.primaryOnDarkRgb,
      },
      tint: mapTint(payload.tint, paletteKey),
    };
  } catch {
    return FALLBACK;
  }
}

export function storefrontAppearanceStyle(appearance: StorefrontAppearanceProjection): Record<string, string> {
  return appearanceCssVars(appearance.tokens, appearance.tint);
}
