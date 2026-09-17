import {
  SECTION_TYPES,
  getSectionType,
  getVariant,
  implementedVariantsForSection,
  landingHostTypeForSection,
} from "../../../lib/storefront-composition/registry.ts";
import type { VariantDefinition, VariantPreviewKind } from "../../../lib/storefront-composition/types.ts";
import {
  defaultLandingSectionConfig,
  type LandingSectionType,
} from "./landing-section-catalog.ts";
import { bannerSlotCountForVariant } from "../../../lib/storefront-composition/industry-templates.ts";

export type AdminCompositionSectionChoice = {
  sectionTypeKey: string;
  hostType: LandingSectionType;
  nameFa: string;
  descriptionFa: string;
  previewKind: VariantPreviewKind;
  testId: string;
  defaultVariantKey: string;
};

export type AdminCompositionVariantChoice = {
  variantKey: string;
  nameFa: string;
  descriptionFa: string;
  previewKind: VariantPreviewKind;
  recommendedUseFa?: string;
  sizePresetsSupported: boolean;
  autoplaySupported: boolean;
};

const SECTION_TEST_IDS: Record<string, string> = {
  HeroCarousel: "add-section-hero",
  StoryRail: "add-section-stories",
  CategoryShowcase: "add-section-categories",
  ProductShowcase: "add-section-products",
  ProductRankedList: "add-section-ranked",
  BannerShowcase: "add-section-banners",
  BrandShowcase: "add-section-brands",
  PromoSection: "add-section-promo",
  ArticleShowcase: "add-section-articles",
  ReviewsShowcase: "add-section-reviews",
  RichText: "add-section-text",
  NavigationMenu: "add-section-menu",
};

/** Landing-allowed SectionTypes that map to a Host type — admin chooser catalog. */
export function adminSelectableSectionTypes(): AdminCompositionSectionChoice[] {
  return SECTION_TYPES
    .filter((s) => s.landingAllowed)
    .map((s) => {
      const hostType = landingHostTypeForSection(s.key) as LandingSectionType | undefined;
      if (!hostType) return null;
      const defaultVariant = getVariant(s.defaultVariantKey);
      return {
        sectionTypeKey: s.key,
        hostType,
        nameFa: s.nameFa,
        descriptionFa: s.descriptionFa,
        previewKind: defaultVariant?.previewKind ?? "promo",
        testId: SECTION_TEST_IDS[s.key] ?? `add-section-${s.key}`,
        defaultVariantKey: s.defaultVariantKey,
      } satisfies AdminCompositionSectionChoice;
    })
    .filter((item): item is AdminCompositionSectionChoice => item != null);
}

export function adminImplementedVariants(sectionTypeKey: string): AdminCompositionVariantChoice[] {
  return implementedVariantsForSection(sectionTypeKey).map((v) => ({
    variantKey: v.key,
    nameFa: v.nameFa,
    descriptionFa: v.descriptionFa,
    previewKind: v.previewKind,
    recommendedUseFa: v.recommendedUseFa,
    sizePresetsSupported: v.sizePresetsSupported,
    autoplaySupported: v.autoplaySupported,
  }));
}

export function defaultConfigForCompositionSection(
  sectionTypeKey: string,
  variantKey?: string,
): Record<string, unknown> {
  const hostType = landingHostTypeForSection(sectionTypeKey) as LandingSectionType | undefined;
  const section = getSectionType(sectionTypeKey);
  const key = variantKey ?? section?.defaultVariantKey ?? "";
  const variant = getVariant(key);
  const base = hostType ? defaultLandingSectionConfig(hostType) : { title: section?.nameFa ?? "بخش" };
  const next: Record<string, unknown> = {
    ...base,
    variantKey: variant?.implemented ? variant.key : section?.defaultVariantKey,
  };
  if (sectionTypeKey === "BannerShowcase" || hostType === "BannerShowcase") {
    const slots = bannerSlotCountForVariant(typeof next.variantKey === "string" ? next.variantKey : key);
    const existing = Array.isArray(next.items) ? next.items : [];
    next.heightPreset = typeof next.heightPreset === "string" ? next.heightPreset : "Medium";
    next.items = Array.from({ length: Math.max(slots, 1) }, (_, i) => {
      const row = existing[i];
      if (row && typeof row === "object") return row;
      return { imageUrl: "", href: "/offers", title: `بنر ${i + 1}` };
    });
  }
  return next;
}

export function variantLabelFa(variantKey: string | undefined | null): string | null {
  if (!variantKey) return null;
  return getVariant(variantKey)?.nameFa ?? null;
}

export function sectionSupportsHeightPreset(sectionTypeKey: string, variantKey?: string): boolean {
  const v = variantKey ? getVariant(variantKey) : getSectionType(sectionTypeKey)
    ? getVariant(getSectionType(sectionTypeKey)!.defaultVariantKey)
    : undefined;
  return Boolean(v?.sizePresetsSupported);
}

export function previewMosaicClass(kind: VariantPreviewKind): string {
  switch (kind) {
    case "hero-slider":
    case "hero-contained":
      return "from-sky-400 to-indigo-500";
    case "story-circles":
    case "story-cards":
      return "from-rose-300 to-orange-400";
    case "category-cards":
    case "category-tiles":
    case "category-rail":
      return "from-emerald-300 to-teal-500";
    case "product-carousel":
    case "product-grid":
    case "product-rows":
    case "product-columns":
    case "ranked-rail":
    case "ranked-columns":
      return "from-amber-300 to-orange-500";
    case "banner-single":
    case "banner-two":
    case "banner-three":
    case "banner-four":
    case "banner-mosaic":
      return "from-violet-300 to-fuchsia-500";
    case "brand-rail":
    case "brand-grid":
      return "from-slate-300 to-slate-500";
    case "reviews-carousel":
      return "from-yellow-200 to-amber-400";
    case "article-rail":
    case "article-grid":
      return "from-cyan-300 to-blue-500";
    case "promo":
      return "from-pink-400 to-rose-600";
    case "richtext":
      return "from-stone-200 to-stone-400";
    case "nav":
      return "from-blue-200 to-blue-400";
    default:
      return "from-gray-200 to-gray-400";
  }
}

/** Distinct structural fingerprint for Admin preview canvases (not CSS free-form). */
export function variantPreviewFingerprint(variantKey: string): string {
  const parts = variantKey.split(".");
  return `${parts[0] ?? "x"}:${parts[1] ?? "default"}`;
}

export function variantPreviewStructure(variantKey: string): Array<{ className: string }> {
  switch (variantKey) {
    case "hero.editorial":
    case "hero.cinematic":
      return [
        { className: "col-span-3 row-span-2 rounded-lg bg-white/40" },
        { className: "col-span-2 row-span-2 rounded-lg bg-white/80" },
      ];
    case "hero.split":
    case "hero.diagonal":
      return [
        { className: "col-span-3 row-span-2 rounded-lg bg-white/55" },
        { className: "col-span-2 row-span-2 rounded-lg bg-white/85" },
      ];
    case "hero.shapes":
    case "hero.fullscreen":
      return [{ className: "col-span-5 row-span-2 rounded-lg bg-white/70" }];
    case "hero.side-promos":
      return [
        { className: "col-span-3 row-span-2 rounded-lg bg-white/70" },
        { className: "col-span-2 rounded-md bg-white/50" },
        { className: "col-span-2 rounded-md bg-white/40" },
      ];
    case "story.icon-shortcuts":
      return Array.from({ length: 5 }, () => ({ className: "rounded-md bg-white/70 aspect-square" }));
    case "story.rounded-cards":
      return Array.from({ length: 4 }, () => ({ className: "rounded-xl bg-white/65 h-10" }));
    case "story.circle":
    case "story.image-circles":
      return Array.from({ length: 5 }, () => ({ className: "rounded-full bg-white/70 aspect-square" }));
    case "category.editorial-tiles":
      return Array.from({ length: 4 }, () => ({ className: "rounded-lg bg-white/60 h-8" }));
    case "category.compact-tiles":
      return Array.from({ length: 6 }, () => ({ className: "rounded-md bg-white/55 h-5" }));
    case "product.tabbed":
      return [
        { className: "col-span-5 h-3 rounded bg-white/80" },
        { className: "col-span-5 h-8 rounded-lg bg-white/55" },
      ];
    case "product.large-cards":
      return Array.from({ length: 3 }, () => ({ className: "rounded-xl bg-white/65 h-12" }));
    case "product.minimal-list":
      return Array.from({ length: 4 }, () => ({ className: "col-span-5 h-3 rounded bg-white/60" }));
    case "ranked.ticker":
      return Array.from({ length: 4 }, () => ({ className: "rounded-md bg-white/70 h-6" }));
    case "banner.three":
      return Array.from({ length: 3 }, () => ({ className: "rounded-lg bg-white/60 h-8" }));
    case "banner.single":
      return [{ className: "col-span-5 h-10 rounded-xl bg-white/70" }];
    case "brand.featured":
      return Array.from({ length: 3 }, () => ({ className: "rounded-2xl bg-white/65 h-10" }));
    case "article.featured-plus-list":
      return [
        { className: "col-span-3 row-span-2 rounded-xl bg-white/70" },
        { className: "col-span-2 h-3 rounded bg-white/55" },
        { className: "col-span-2 h-3 rounded bg-white/45" },
        { className: "col-span-2 h-3 rounded bg-white/40" },
      ];
    case "reviews.compact-quotes":
      return Array.from({ length: 3 }, () => ({ className: "rounded-lg bg-white/60 h-8" }));
    default: {
      const kind = getVariant(variantKey)?.previewKind ?? "promo";
      if (kind.startsWith("banner")) {
        return Array.from({ length: 4 }, () => ({ className: "rounded-md bg-white/55 h-6" }));
      }
      if (kind.startsWith("product") || kind.startsWith("ranked")) {
        return Array.from({ length: 4 }, () => ({ className: "rounded-lg bg-white/60 h-8" }));
      }
      return Array.from({ length: 3 }, () => ({ className: "rounded-lg bg-white/55 h-7" }));
    }
  }
}

export type { VariantDefinition };
