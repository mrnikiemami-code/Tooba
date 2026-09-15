import type { VariantDefinition } from "./types.ts";
import { assertVariantCompatible, isVariantImplemented } from "./registry.ts";

export function resolveSharedVariant(
  sectionTypeKey: string,
  variantKey: string,
  options?: { strict?: boolean },
): VariantDefinition {
  const strict = options?.strict ?? true;
  const variant = assertVariantCompatible(sectionTypeKey, variantKey);
  if (strict && !isVariantImplemented(variantKey)) {
    throw new Error(`Variant not implemented: ${variantKey}`);
  }
  return variant;
}
