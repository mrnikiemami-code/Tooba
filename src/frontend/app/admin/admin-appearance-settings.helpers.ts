import { appearanceCssVars, resolveBrandTokens, resolvePaletteKey } from "../../lib/storefront-appearance/palette-registry.ts";

export function appearanceIsDirty(savedKey: string, draftKey: string): boolean {
  return resolvePaletteKey(savedKey) !== resolvePaletteKey(draftKey);
}

export function appearancePreviewStyle(paletteKey: string): Record<string, string> {
  return appearanceCssVars(resolveBrandTokens(paletteKey));
}
