import { storefrontHostOrigin } from "./storefront-api.ts";
import {
  appearanceCssVars,
  resolveBrandTokens,
  resolvePaletteKey,
  type StorefrontBrandTokens,
} from "../../lib/storefront-appearance/palette-registry.ts";
import { resolveThemeMode } from "../../lib/storefront-appearance/theme-mode.ts";
import { DEFAULT_PRODUCT_CARD_SKIN, resolveProductCardSkin } from "../../lib/storefront-appearance/product-card-skin.ts";

export interface StorefrontAppearanceProjection {
  storeScope: string;
  paletteKey: string;
  paletteKeyWasKnown: boolean;
  themeMode: string;
  productCardSkin: string;
  tokens: StorefrontBrandTokens;
}

const FALLBACK: StorefrontAppearanceProjection = {
  storeScope: "default",
  paletteKey: "tooba-blue",
  paletteKeyWasKnown: true,
  themeMode: "LightOnly",
  productCardSkin: DEFAULT_PRODUCT_CARD_SKIN,
  tokens: resolveBrandTokens("tooba-blue"),
};

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
      tokens?: Partial<StorefrontBrandTokens>;
    };
    const paletteKey = resolvePaletteKey(payload.paletteKey);
    const defaults = resolveBrandTokens(paletteKey);
    return {
      storeScope: payload.storeScope ?? FALLBACK.storeScope,
      paletteKey,
      paletteKeyWasKnown: payload.paletteKeyWasKnown !== false,
      themeMode: resolveThemeMode(payload.themeMode),
      productCardSkin: resolveProductCardSkin(payload.productCardSkin),
      tokens: {
        primaryRgb: payload.tokens?.primaryRgb ?? defaults.primaryRgb,
        primaryStrongRgb: payload.tokens?.primaryStrongRgb ?? defaults.primaryStrongRgb,
        onPrimaryRgb: payload.tokens?.onPrimaryRgb ?? defaults.onPrimaryRgb,
        focusRgb: payload.tokens?.focusRgb ?? defaults.focusRgb,
        primaryOnDarkRgb: payload.tokens?.primaryOnDarkRgb ?? defaults.primaryOnDarkRgb,
      },
    };
  } catch {
    return FALLBACK;
  }
}

export function storefrontAppearanceStyle(appearance: StorefrontAppearanceProjection): Record<string, string> {
  return appearanceCssVars(appearance.tokens);
}
