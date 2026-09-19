import { getVariant, getSectionType } from "./registry.ts";
import { isDataSourceSupportedForLanding } from "./size-presets.ts";
import type { AdminSelectableDataSource } from "./types.ts";
import { ADMIN_SELECTABLE_DATA_SOURCES } from "./types.ts";

export type ResourceFamily = "products" | "articles" | "brands" | "categories" | "none";

export type SourceCapability = {
  variantKey: string;
  sectionTypeKey: string;
  resourceType: ResourceFamily;
  strategies: AdminSelectableDataSource[];
  manualSupported: boolean;
  multiSelect: boolean;
  defaultCount: number;
  maxCount: number;
  selectorGridProfile: "orders-canonical";
};

const RESOURCE_BY_SECTION: Record<string, ResourceFamily> = {
  ProductShowcase: "products",
  ProductRankedList: "products",
  ArticleShowcase: "articles",
  BrandShowcase: "brands",
  CategoryShowcase: "categories",
  StoryRail: "none",
  BannerShowcase: "none",
  HeroCarousel: "none",
  PromoSection: "none",
  ReviewsShowcase: "none",
  RichText: "none",
  NavigationMenu: "none",
};

const MULTI_SELECT_SECTIONS = new Set([
  "ProductShowcase",
  "ProductRankedList",
  "ArticleShowcase",
  "BrandShowcase",
  "CategoryShowcase",
]);

function resourceForSection(sectionTypeKey: string): ResourceFamily {
  return RESOURCE_BY_SECTION[sectionTypeKey] ?? "none";
}

/** Truthful admin strategies only — HeuristicHomeOnly / Deferred never appear. */
export function truthfulStrategiesForVariant(variantKey: string): AdminSelectableDataSource[] {
  const variant = getVariant(variantKey);
  if (!variant) return [];
  return variant.dataSources.filter(
    (kind) =>
      (ADMIN_SELECTABLE_DATA_SOURCES as readonly string[]).includes(kind)
      && isDataSourceSupportedForLanding(kind),
  );
}

export function sourceCapabilityForVariant(variantKey: string): SourceCapability | null {
  const variant = getVariant(variantKey);
  if (!variant) return null;
  const section = getSectionType(variant.sectionTypeKey);
  const strategies = truthfulStrategiesForVariant(variantKey);
  const defaultCount = typeof section?.settings.defaults.itemCount === "number"
    ? section.settings.defaults.itemCount
    : 8;
  return {
    variantKey: variant.key,
    sectionTypeKey: variant.sectionTypeKey,
    resourceType: resourceForSection(variant.sectionTypeKey),
    strategies,
    manualSupported: strategies.includes("Manual"),
    multiSelect: MULTI_SELECT_SECTIONS.has(variant.sectionTypeKey),
    defaultCount,
    maxCount: 48,
    selectorGridProfile: "orders-canonical",
  };
}

export function sourceCapabilityForSectionType(sectionTypeKey: string, variantKey?: string): SourceCapability | null {
  const section = getSectionType(sectionTypeKey);
  const key = variantKey ?? section?.defaultVariantKey;
  if (!key) return null;
  return sourceCapabilityForVariant(key);
}

/** Persian labels for truthful dynamic strategies (no fake BestSelling/MostViewed). */
export function strategyLabelFa(kind: AdminSelectableDataSource): string {
  switch (kind) {
    case "Manual":
      return "انتخاب دستی";
    case "Newest":
      return "جدیدترین کالاها";
    case "Category":
      return "از یک دسته";
    case "Brand":
      return "از یک برند";
    case "PromotionCampaign":
      return "پیشنهاد شگفت‌انگیز";
    case "LatestArticles":
      return "جدیدترین مطالب";
    case "ApprovedReviews":
      return "نظرهای تأییدشده";
    default:
      return kind;
  }
}
