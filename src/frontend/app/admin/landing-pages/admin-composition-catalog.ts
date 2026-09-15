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
    .filter((item): item is AdminCompositionSectionChoice => item != null)
    // Prefer unique Host mappings: keep first occurrence per hostType for StoryRail/Banner proxy collisions
    .filter((item, index, all) => {
      // Allow BannerShowcase + PromoSection both (same Host PromoBanner) — both selectable
      // Allow StoryRail + CategoryShowcase both (same Host CategoryGrid)
      return all.findIndex((x) => x.sectionTypeKey === item.sectionTypeKey) === index;
    });
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
  return {
    ...base,
    variantKey: variant?.implemented ? variant.key : section?.defaultVariantKey,
  };
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

export type { VariantDefinition };
