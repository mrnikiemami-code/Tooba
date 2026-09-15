import type { CompositionSectionInstance, CompositionSurfaceRole } from "./types.ts";
import { getSectionType, getVariant } from "./registry.ts";
import { normalizeControlledSettings } from "./settings.ts";
import { landingSectionSurfaceRole } from "../storefront-appearance/surface-role.ts";

/** Legacy Landing PascalCase section types → shared registry keys. */
export const LANDING_SECTION_TYPE_MAP: Record<string, { sectionTypeKey: string; variantKey: string }> = {
  Hero: { sectionTypeKey: "HeroCarousel", variantKey: "hero.contained" },
  ProductCollection: { sectionTypeKey: "ProductShowcase", variantKey: "product.card-carousel" },
  CategoryGrid: { sectionTypeKey: "CategoryShowcase", variantKey: "category.image-cards" },
  BrandStrip: { sectionTypeKey: "BrandShowcase", variantKey: "brand.logo-rail" },
  PromoBanner: { sectionTypeKey: "PromoSection", variantKey: "promo.default" },
  ArticleList: { sectionTypeKey: "ArticleShowcase", variantKey: "article.magazine-rail" },
  Reviews: { sectionTypeKey: "ReviewsShowcase", variantKey: "reviews.card-carousel" },
  RichText: { sectionTypeKey: "RichText", variantKey: "richtext.default" },
  NavigationMenu: { sectionTypeKey: "NavigationMenu", variantKey: "nav.menu" },
};

/** Legacy Home snake_case section types → shared registry keys. */
export const HOME_SECTION_TYPE_MAP: Record<string, { sectionTypeKey: string; variantKey: string }> = {
  hero: { sectionTypeKey: "HeroCarousel", variantKey: "hero.full-width" },
  stories: { sectionTypeKey: "StoryRail", variantKey: "story.circle" },
  category_grid: { sectionTypeKey: "CategoryShowcase", variantKey: "category.image-cards" },
  product_rail_flash: { sectionTypeKey: "ProductShowcase", variantKey: "product.card-carousel" },
  best_sellers: { sectionTypeKey: "ProductShowcase", variantKey: "product.category-columns" },
  product_rail_most_viewed: { sectionTypeKey: "ProductShowcase", variantKey: "product.compact-rows" },
  middle_banners: { sectionTypeKey: "BannerShowcase", variantKey: "banner.one-large-two-small" },
  brands: { sectionTypeKey: "BrandShowcase", variantKey: "brand.logo-rail" },
  newest_products: { sectionTypeKey: "ProductShowcase", variantKey: "product.card-carousel" },
  customer_reviews: { sectionTypeKey: "ReviewsShowcase", variantKey: "reviews.card-carousel" },
  latest_articles: { sectionTypeKey: "ArticleShowcase", variantKey: "article.magazine-rail" },
};

export function adaptLandingSectionToComposition(input: {
  pageSectionId: string;
  sectionType: string;
  displayOrder: number;
  enabled?: boolean;
  config?: Record<string, unknown>;
}): CompositionSectionInstance {
  const mapped = LANDING_SECTION_TYPE_MAP[input.sectionType];
  if (!mapped) throw new Error(`Unsupported landing section type: ${input.sectionType}`);
  const section = getSectionType(mapped.sectionTypeKey);
  const variant = getVariant(mapped.variantKey);
  if (!section || !variant) throw new Error(`Registry missing mapping for ${input.sectionType}`);
  if (!section.landingAllowed) throw new Error(`${mapped.sectionTypeKey} not landing-allowed`);

  const config = input.config ?? {};
  const settingsRaw: Record<string, unknown> = {
    enabled: input.enabled ?? true,
    title: typeof config.title === "string" ? config.title : undefined,
    subtitle: typeof config.subtitle === "string" ? config.subtitle : undefined,
    ctaHref: typeof config.href === "string" ? config.href : undefined,
    itemCount: typeof config.take === "number" ? config.take : undefined,
    dataSource: typeof config.source === "string" ? mapLandingSource(config.source) : undefined,
    surfaceRole: landingSectionSurfaceRole(input.sectionType),
  };
  const cleaned = Object.fromEntries(Object.entries(settingsRaw).filter(([, v]) => v !== undefined));
  const settings = normalizeControlledSettings(section.settings, cleaned);

  return {
    id: input.pageSectionId,
    sectionTypeKey: mapped.sectionTypeKey,
    variantKey: mapped.variantKey,
    enabled: Boolean(settings.enabled ?? true),
    displayOrder: input.displayOrder,
    settings,
    surfaceRole: (settings.surfaceRole as CompositionSurfaceRole) ?? section.defaultSurfaceRole,
  };
}

function mapLandingSource(source: string): string {
  if (source === "Latest") return "LatestArticles";
  return source;
}

export function assertLandingTypesCovered(legacyTypes: readonly string[]): void {
  for (const type of legacyTypes) {
    if (!LANDING_SECTION_TYPE_MAP[type]) {
      throw new Error(`Landing type not mapped: ${type}`);
    }
    const mapped = LANDING_SECTION_TYPE_MAP[type];
    if (!getSectionType(mapped.sectionTypeKey) || !getVariant(mapped.variantKey)) {
      throw new Error(`Broken landing map for ${type}`);
    }
  }
}
