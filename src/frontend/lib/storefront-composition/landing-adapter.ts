import type { CompositionSectionInstance, CompositionSurfaceRole } from "./types.ts";
import { getSectionType, getVariant, isVariantImplemented } from "./registry.ts";
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
  StoryRail: { sectionTypeKey: "StoryRail", variantKey: "story.circle" },
  BannerShowcase: { sectionTypeKey: "BannerShowcase", variantKey: "banner.single" },
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

/** T019 proxy migration: CategoryGrid/PromoBanner configs carrying story.* / banner.* variantKey. */
export function migrateProxySectionType(
  hostSectionType: string,
  configVariantKey: string | undefined,
): { sectionTypeKey: string; variantKey: string } | null {
  if (!configVariantKey) return null;
  const override = getVariant(configVariantKey);
  if (!override || !isVariantImplemented(configVariantKey)) return null;

  if (hostSectionType === "CategoryGrid" && configVariantKey.startsWith("story.")) {
    return { sectionTypeKey: "StoryRail", variantKey: override.key };
  }
  if (hostSectionType === "PromoBanner" && configVariantKey.startsWith("banner.")) {
    return { sectionTypeKey: "BannerShowcase", variantKey: override.key };
  }
  return null;
}

export function adaptLandingSectionToComposition(input: {
  pageSectionId: string;
  sectionType: string;
  displayOrder: number;
  enabled?: boolean;
  config?: Record<string, unknown>;
}): CompositionSectionInstance {
  const mapped = LANDING_SECTION_TYPE_MAP[input.sectionType];
  if (!mapped) throw new Error(`Unsupported landing section type: ${input.sectionType}`);

  const config = input.config ?? {};
  const configVariantKey = typeof config.variantKey === "string" ? config.variantKey : undefined;
  let sectionTypeKey = mapped.sectionTypeKey;
  let variantKey = mapped.variantKey;

  const migrated = migrateProxySectionType(input.sectionType, configVariantKey);
  if (migrated) {
    sectionTypeKey = migrated.sectionTypeKey;
    variantKey = migrated.variantKey;
  } else if (configVariantKey) {
    const override = getVariant(configVariantKey);
    if (override && isVariantImplemented(configVariantKey)) {
      sectionTypeKey = override.sectionTypeKey;
      variantKey = override.key;
    }
  }

  const section = getSectionType(sectionTypeKey);
  const variant = getVariant(variantKey);
  if (!section || !variant) throw new Error(`Registry missing mapping for ${input.sectionType}`);
  if (!section.landingAllowed) throw new Error(`${sectionTypeKey} not landing-allowed`);

  const settingsRaw: Record<string, unknown> = {
    enabled: input.enabled ?? true,
    title: typeof config.title === "string" ? config.title : undefined,
    subtitle: typeof config.subtitle === "string" ? config.subtitle : undefined,
    ctaHref: typeof config.href === "string" ? config.href : undefined,
    itemCount: typeof config.take === "number" ? config.take : undefined,
    dataSource: typeof config.source === "string" ? mapLandingSource(config.source) : undefined,
    heightPreset: typeof config.heightPreset === "string" ? config.heightPreset : undefined,
    autoplay: typeof config.autoplay === "boolean" ? config.autoplay : undefined,
    surfaceRole: landingSectionSurfaceRole(input.sectionType),
  };
  const cleaned = Object.fromEntries(Object.entries(settingsRaw).filter(([, v]) => v !== undefined));
  const settings = normalizeControlledSettings(section.settings, cleaned);

  return {
    id: input.pageSectionId,
    sectionTypeKey,
    variantKey,
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
