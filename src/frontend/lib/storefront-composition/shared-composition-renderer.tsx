"use client";

import type { ReactNode } from "react";
import type { SectionDisplayConfig } from "../../app/composition/composition-api.ts";
import type {
  StorefrontArticleItem,
  StorefrontBestSellerColumn,
  StorefrontBrandItem,
  StorefrontCategoryItem,
  StorefrontFeaturedReviewItem,
  StorefrontProductCard,
} from "../../app/storefront/storefront-model.ts";
import type { LandingRenderContext, StorefrontLandingSection } from "../../app/storefront/storefront-landing-api.ts";
import {
  HomeArticlesSection,
  HomeBestSellersSection,
  HomeBrandsSection,
  HomeNewProductsSection,
  HomeTestimonialsSection,
} from "../../app/storefront/storefront-home-repair-sections.tsx";
import { HomeStoriesSection } from "../../app/storefront/stories/home-stories.tsx";
import {
  CompositionBannerGrid,
  HomeCategoryGridSection,
  HomeHeroSlider,
  HomeMiddleBannersSection,
  ProductRailSection,
} from "../../app/storefront/storefront-home-blocks.tsx";
import {
  LandingArticleList,
  LandingBrandStrip,
  LandingCategoryGrid,
  LandingHero,
  LandingNavigationMenu,
  LandingProductRail,
  LandingPromo,
  LandingReviews,
  LandingRichText,
} from "../../app/storefront/storefront-landing-blocks.tsx";
import type { CompositionSectionInstance, CompositionSurfaceRole } from "./types.ts";
import { getVariant } from "./registry.ts";
import { resolveSharedVariant } from "./resolve-variant.ts";
import { surfaceRoleClass } from "../storefront-appearance/surface-role.ts";

export { resolveSharedVariant };

export type HomeRenderContext = {
  heroTitle: string;
  homeCategories: StorefrontCategoryItem[];
  specialOffers: StorefrontProductCard[];
  bestSellerColumns: StorefrontBestSellerColumn[];
  mostViewedProducts: StorefrontProductCard[];
  brands: StorefrontBrandItem[];
  newArrivals: StorefrontProductCard[];
  featuredReviews: StorefrontFeaturedReviewItem[];
  latestArticles: StorefrontArticleItem[];
};

export type SharedLandingRenderInput = {
  section: StorefrontLandingSection;
  composition: CompositionSectionInstance;
  config: Record<string, unknown>;
  context: LandingRenderContext;
};

export function renderSharedHomeSection(
  variantKey: string,
  context: HomeRenderContext,
  config: SectionDisplayConfig,
): ReactNode | null {
  const variant = getVariant(variantKey);
  if (!variant || !variant.implemented) return null;

  switch (variantKey) {
    case "hero.full-width":
    case "hero.contained":
      return (
        <div data-testid="home-hero">
          <HomeHeroSlider heightPreset={config.heightPreset} />
        </div>
      );
    case "story.circle":
    case "story.image-circles":
    case "story.rounded-cards":
      return <HomeStoriesSection />;
    case "category.image-cards":
    case "category.compact-tiles":
    case "category.horizontal-rail":
      return <HomeCategoryGridSection homeCategories={context.homeCategories} />;
    case "product.card-carousel":
      if (config.href === "/new-products" || config.title === "جدیدترین‌ها") {
        return <HomeNewProductsSection products={context.newArrivals} />;
      }
      return context.specialOffers.length > 0 ? (
        <ProductRailSection
          id="home-flash"
          title={config.title ?? "پیشنهاد شگفت‌انگیز"}
          href={config.href ?? "/offers"}
          linkLabel="همه"
          tone="accent"
          products={context.specialOffers}
          slideClassName="w-[170px] md:w-[210px]"
          testId="home-flash-sales"
        />
      ) : null;
    case "product.category-columns":
    case "ranked.multi-column":
      return <HomeBestSellersSection columns={context.bestSellerColumns} />;
    case "product.compact-rows":
    case "ranked.horizontal":
      return context.mostViewedProducts.length > 0 ? (
        <ProductRailSection
          id="home-most-viewed"
          title={config.title ?? "پربازدیدترین‌ها"}
          href={config.href ?? "/most-viewed"}
          linkLabel="همه"
          tone="plain"
          products={context.mostViewedProducts}
          slideClassName="w-[170px] md:w-[220px]"
          testId="home-most-viewed"
        />
      ) : null;
    case "product.grid":
      return <HomeNewProductsSection products={context.newArrivals} />;
    case "banner.one-large-two-small":
      return <HomeMiddleBannersSection />;
    case "banner.single":
      return <CompositionBannerGrid layout="single" heightPreset={config.heightPreset} />;
    case "banner.two-equal":
      return <CompositionBannerGrid layout="two-equal" heightPreset={config.heightPreset} />;
    case "banner.three":
      return <CompositionBannerGrid layout="three" heightPreset={config.heightPreset} />;
    case "banner.four-grid":
    case "banner.mosaic-2x2":
      return <CompositionBannerGrid layout="four-grid" heightPreset={config.heightPreset} />;
    case "brand.logo-rail":
    case "brand.logo-grid":
      return <HomeBrandsSection brands={context.brands} />;
    case "reviews.card-carousel":
      return <HomeTestimonialsSection reviews={context.featuredReviews} />;
    case "article.magazine-rail":
    case "article.grid":
      return <HomeArticlesSection articles={context.latestArticles} />;
    default:
      return null;
  }
}

/** Home path: flash vs newest both use product.card-carousel — disambiguate by home section key. */
export function renderSharedHomeSectionForLegacyType(
  homeSectionType: string,
  variantKey: string,
  context: HomeRenderContext,
  config: SectionDisplayConfig,
): ReactNode | null {
  if (homeSectionType === "product_rail_flash") {
    return context.specialOffers.length > 0 ? (
      <ProductRailSection
        id="home-flash"
        title={config.title ?? "پیشنهاد شگفت‌انگیز"}
        href={config.href ?? "/offers"}
        linkLabel="همه"
        tone="accent"
        products={context.specialOffers}
        slideClassName="w-[170px] md:w-[210px]"
        testId="home-flash-sales"
      />
    ) : null;
  }
  if (homeSectionType === "newest_products") {
    return <HomeNewProductsSection products={context.newArrivals} />;
  }
  return renderSharedHomeSection(variantKey, context, config);
}

export function renderSharedLandingSection(input: SharedLandingRenderInput): ReactNode | null {
  const { composition, config, context, section } = input;
  if (!composition.enabled) return null;
  const { variantKey } = composition;

  try {
    resolveSharedVariant(composition.sectionTypeKey, variantKey, { strict: true });
  } catch {
    return null;
  }

  switch (variantKey) {
    case "hero.full-width":
    case "hero.contained":
      return <LandingHero config={config} />;
    case "story.circle":
    case "story.image-circles":
    case "story.rounded-cards":
      return <HomeStoriesSection />;
    case "product.card-carousel":
    case "product.grid":
    case "product.compact-rows":
    case "ranked.horizontal":
    case "product.category-columns":
    case "ranked.multi-column":
      return <LandingProductRail section={section} config={config} products={context.products} />;
    case "category.image-cards":
    case "category.compact-tiles":
    case "category.horizontal-rail":
      return <LandingCategoryGrid config={config} categories={context.categories} />;
    case "brand.logo-rail":
    case "brand.logo-grid":
      return <LandingBrandStrip config={config} brands={context.brands} />;
    case "promo.default":
      return <LandingPromo config={config} />;
    case "banner.single":
      return <CompositionBannerGrid layout="single" title={typeof config.title === "string" ? config.title : undefined} href={typeof config.href === "string" ? config.href : undefined} heightPreset={config.heightPreset} testId="landing-promo" />;
    case "banner.two-equal":
      return <CompositionBannerGrid layout="two-equal" heightPreset={config.heightPreset} testId="landing-promo" />;
    case "banner.three":
      return <CompositionBannerGrid layout="three" heightPreset={config.heightPreset} testId="landing-promo" />;
    case "banner.four-grid":
    case "banner.mosaic-2x2":
      return <CompositionBannerGrid layout="four-grid" heightPreset={config.heightPreset} testId="landing-promo" />;
    case "banner.one-large-two-small":
      return <HomeMiddleBannersSection />;
    case "article.magazine-rail":
    case "article.grid":
      return <LandingArticleList config={config} articles={context.articles} />;
    case "reviews.card-carousel":
      return <LandingReviews reviews={context.reviews} />;
    case "richtext.default":
      return <LandingRichText config={config} />;
    case "nav.menu":
      return <LandingNavigationMenu config={config} menus={context.menus} />;
    default:
      return null;
  }
}

export function wrapWithSurfaceRole(node: ReactNode, role: CompositionSurfaceRole, sectionType: string): ReactNode {
  if (!node) return null;
  return (
    <div className={surfaceRoleClass(role)} data-storefront-surface-role={role} data-landing-section-type={sectionType}>
      {node}
    </div>
  );
}
