import type { VariantDefinition } from "./types.ts";
import { assertVariantCompatible, getVariant, isVariantImplemented } from "./registry.ts";

/** Accidental aliases remapped to the canonical selectable Variant. */
const VARIANT_ALIASES: Record<string, string> = {
  "banner.mosaic-2x2": "banner.four-grid",
};

export function canonicalizeVariantKey(variantKey: string): string {
  return VARIANT_ALIASES[variantKey] ?? variantKey;
}

export function resolveSharedVariant(
  sectionTypeKey: string,
  variantKey: string,
  options?: { strict?: boolean },
): VariantDefinition {
  const strict = options?.strict ?? true;
  const canonical = canonicalizeVariantKey(variantKey);
  const variant = assertVariantCompatible(sectionTypeKey, canonical);
  if (strict && !isVariantImplemented(canonical)) {
    throw new Error(`Variant not implemented: ${canonical}`);
  }
  // Prefer canonical definition; fall back to raw key metadata when alias kept for legacy.
  return getVariant(canonical) ?? variant;
}
