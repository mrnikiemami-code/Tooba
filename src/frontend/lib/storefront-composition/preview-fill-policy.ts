/**
 * Central Store-preview fill policy (LOCK-SF-306…311).
 * Fills only missing visual slots with in-memory fakes — never Template Catalog fallback.
 */

import { getVariant } from "./registry.ts";
import { RESPONSIVE_CONTRACTS } from "./responsive-contracts.ts";
import { canonicalizeVariantKey } from "./resolve-variant.ts";

export type PreviewFillResult<T> = {
  items: T[];
  realCount: number;
  fakeCount: number;
  targetCount: number;
};

/** Parse responsive itemVisible desktop token into a positive integer target. */
export function parseItemVisibleCount(token: string | undefined): number | null {
  if (!token) return null;
  const trimmed = token.trim();
  if (/^\d+$/.test(trimmed)) return Number(trimmed);
  const range = trimmed.match(/^(\d+)\s*-\s*(\d+)$/);
  if (range) {
    const a = Number(range[1]);
    const b = Number(range[2]);
    return Math.max(1, Math.min(a, b));
  }
  const plus = trimmed.match(/^(\d+)\s*\+\s*(\d+)$/);
  if (plus) return Number(plus[1]) + Number(plus[2]);
  const perCol = trimmed.match(/per-col\s+(\d+)/i);
  if (perCol) return Number(perCol[1]) * 2;
  return null;
}

const SECTION_DEFAULT_TARGET: Record<string, number> = {
  HeroCarousel: 1,
  StoryRail: 6,
  CategoryShowcase: 4,
  ProductShowcase: 4,
  ProductRankedList: 4,
  BannerShowcase: 2,
  BrandShowcase: 6,
  PromoSection: 1,
  ArticleShowcase: 3,
  ReviewsShowcase: 3,
  RichText: 1,
  NavigationMenu: 0,
};

/**
 * Variant contract → preview cardinality (code-owned; not user-configurable).
 */
export function resolveVariantPreviewCardinality(variantKey: string): {
  previewMinItems: number;
  previewTargetItems: number;
} {
  const key = canonicalizeVariantKey(variantKey);
  const variant = getVariant(key);
  if (variant?.previewTargetItems != null && variant.previewMinItems != null) {
    return {
      previewMinItems: Math.max(0, variant.previewMinItems),
      previewTargetItems: Math.max(variant.previewMinItems, variant.previewTargetItems),
    };
  }
  const fromContract = parseItemVisibleCount(RESPONSIVE_CONTRACTS[key]?.itemVisible.desktop);
  const sectionDefault = variant ? SECTION_DEFAULT_TARGET[variant.sectionTypeKey] ?? 3 : 3;
  const target = variant?.previewTargetItems ?? fromContract ?? sectionDefault;
  const min = variant?.previewMinItems ?? Math.min(2, target);
  return {
    previewMinItems: Math.max(0, min),
    previewTargetItems: Math.max(min, target),
  };
}

/**
 * Apply preview-only fill when enabled (Store preview).
 * - sufficient real → real only
 * - partial → keep all real + fill missing slots
 * - zero → all fake slots
 * When disabled (Sample / published), returns real items unchanged.
 */
export function applyPreviewFill<T>(args: {
  enabled: boolean;
  variantKey: string;
  realItems: readonly T[];
  createFake: (index: number) => T;
  targetOverride?: number;
}): PreviewFillResult<T> {
  const real = [...args.realItems];
  if (!args.enabled) {
    return { items: real, realCount: real.length, fakeCount: 0, targetCount: 0 };
  }
  const { previewTargetItems } = resolveVariantPreviewCardinality(args.variantKey);
  const target = Math.max(0, args.targetOverride ?? previewTargetItems);
  if (real.length >= target) {
    return { items: real, realCount: real.length, fakeCount: 0, targetCount: target };
  }
  const needed = target - real.length;
  const fakes = Array.from({ length: needed }, (_, index) => args.createFake(index));
  return {
    items: [...real, ...fakes],
    realCount: real.length,
    fakeCount: needed,
    targetCount: target,
  };
}

/** True only for admin Store template preview — never published storefront. */
export function isStorePreviewFillEnabled(preview?: boolean, previewSource?: "sample" | "store"): boolean {
  return Boolean(preview && previewSource === "store");
}
