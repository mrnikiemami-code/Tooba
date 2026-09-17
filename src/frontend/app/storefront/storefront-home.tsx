"use client";

import { Fragment, type ReactNode } from "react";
import {
  defaultHomeCompositionSections,
  parseSectionDisplayConfig,
  type HomeCompositionSectionItem,
  type SectionDisplayConfig,
} from "../composition/composition-api.ts";
import type {
  StorefrontArticleItem,
  StorefrontBestSellerColumn,
  StorefrontBrandItem,
  StorefrontCategoryItem,
  StorefrontFeaturedReviewItem,
  StorefrontProductCard,
} from "./storefront-model.ts";
import { HOME_SECTION_TYPE_MAP } from "../../lib/storefront-composition/landing-adapter.ts";
import {
  type HomeRenderContext,
  renderSharedHomeSectionForLegacyType,
} from "../../lib/storefront-composition/shared-composition-renderer.tsx";

export { DEFAULT_HOME_SECTION_ORDER } from "../composition/composition-api.ts";
export type { HomeRenderContext };

/**
 * خانهٔ Shopeiva با ترتیب Page Composition؛ دادهٔ تجاری زنده از Host.
 * Sections resolve through the shared composition renderer (LOCK-SF-229).
 */
export function StorefrontShopeivaHome({
  heroTitle,
  homeCategories,
  specialOffers,
  newArrivals,
  brands,
  bestSellerColumns,
  mostViewedProducts,
  featuredReviews,
  latestArticles,
  compositionSections,
}: {
  heroTitle: string;
  heroSubtitle: string;
  categories: StorefrontCategoryItem[];
  homeCategories: StorefrontCategoryItem[];
  specialOffers: StorefrontProductCard[];
  campaignProducts: StorefrontProductCard[];
  newArrivals: StorefrontProductCard[];
  productRail: StorefrontProductCard[];
  brands: StorefrontBrandItem[];
  bestSellerColumns: StorefrontBestSellerColumn[];
  mostViewedProducts: StorefrontProductCard[];
  featuredReviews: StorefrontFeaturedReviewItem[];
  latestArticles: StorefrontArticleItem[];
  compositionSections?: HomeCompositionSectionItem[];
}) {
  const renderContext: HomeRenderContext = {
    heroTitle,
    homeCategories,
    specialOffers,
    bestSellerColumns,
    mostViewedProducts,
    brands,
    newArrivals,
    featuredReviews,
    latestArticles,
  };

  const sections = (compositionSections?.length ? compositionSections : defaultHomeCompositionSections())
    .slice()
    .sort((left, right) => left.displayOrder - right.displayOrder);

  return (
    <div className="py-6 space-y-6 overflow-x-hidden" data-testid="storefront-home" data-storefront-surface-role="page">
      <h1 className="sr-only">{heroTitle}</h1>
      {sections.map((section) => {
        const rendered = renderHomeSection(
          section.sectionType,
          renderContext,
          parseSectionDisplayConfig(section.configurationJson),
        );
        if (!rendered) return null;
        return <Fragment key={section.pageSectionId}>{rendered}</Fragment>;
      })}
    </div>
  );
}

function renderHomeSection(
  sectionType: string,
  context: HomeRenderContext,
  config: SectionDisplayConfig,
): ReactNode | null {
  const mapped = HOME_SECTION_TYPE_MAP[sectionType];
  const variantKey = mapped?.variantKey;
  switch (sectionType) {
    case "hero":
      return renderSharedHomeSectionForLegacyType("hero", variantKey ?? "hero.full-width", context, config);
    case "stories":
      return renderSharedHomeSectionForLegacyType("stories", variantKey ?? "story.circle", context, config);
    case "category_grid":
      return renderSharedHomeSectionForLegacyType("category_grid", variantKey ?? "category.image-cards", context, config);
    case "product_rail_flash":
      return renderSharedHomeSectionForLegacyType("product_rail_flash", variantKey ?? "product.amazing", context, config);
    case "best_sellers":
      return renderSharedHomeSectionForLegacyType("best_sellers", variantKey ?? "product.category-columns", context, config);
    case "product_rail_most_viewed":
      return renderSharedHomeSectionForLegacyType("product_rail_most_viewed", variantKey ?? "product.zohreh", context, config);
    case "middle_banners":
      return renderSharedHomeSectionForLegacyType("middle_banners", variantKey ?? "banner.one-large-two-small", context, config);
    case "brands":
      return renderSharedHomeSectionForLegacyType("brands", variantKey ?? "brand.logo-rail", context, config);
    case "newest_products":
      return renderSharedHomeSectionForLegacyType("newest_products", variantKey ?? "product.mahoor", context, config);
    case "customer_reviews":
      return renderSharedHomeSectionForLegacyType("customer_reviews", variantKey ?? "reviews.card-carousel", context, config);
    case "latest_articles":
      return renderSharedHomeSectionForLegacyType("latest_articles", variantKey ?? "article.magazine-rail", context, config);
    default:
      return null;
  }
}
