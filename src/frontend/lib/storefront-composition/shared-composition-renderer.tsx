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
  type BannerLayout,
  type CategoryLayout,
  type HeroLayout,
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

function heroLayoutFromVariant(variantKey: string): HeroLayout {
  if (variantKey === "hero.contained") return "contained";
  if (variantKey === "hero.split") return "split";
  if (variantKey === "hero.side-promos") return "side-promos";
  return "full-width";
}

function storyLayoutFromVariant(variantKey: string): "circle" | "image-circles" | "rounded-cards" {
  if (variantKey === "story.image-circles") return "image-circles";
  if (variantKey === "story.rounded-cards") return "rounded-cards";
  return "circle";
}

function categoryLayoutFromVariant(variantKey: string): CategoryLayout {
  if (variantKey === "category.compact-tiles") return "compact-tiles";
  if (variantKey === "category.horizontal-rail") return "horizontal-rail";
  return "image-cards";
}

function bannerLayoutFromVariant(variantKey: string): BannerLayout {
  switch (variantKey) {
    case "banner.two-equal":
      return "two-equal";
    case "banner.two-asymmetric":
      return "two-asymmetric";
    case "banner.three":
      return "three";
    case "banner.four-grid":
      return "four-grid";
    case "banner.one-large-two-small":
      return "one-large-two-small";
    case "banner.one-large-four-small":
      return "one-large-four-small";
    case "banner.eight-compact":
      return "eight-compact";
    case "banner.mosaic-2x2":
      return "mosaic-2x2";
    default:
      return "single";
  }
}

export function renderSharedHomeSection(
  variantKey: string,
  context: HomeRenderContext,
  config: SectionDisplayConfig,
): ReactNode | null {
  const variant = getVariant(variantKey);
  if (!variant || !variant.implemented) return null;

  switch (variantKey) {
    case "hero.full-width":
      return (
        <div data-testid="home-hero">
          <HomeHeroSlider heightPreset={config.heightPreset} layout="full-width" />
        </div>
      );
    case "hero.contained":
      return (
        <div data-testid="home-hero">
          <HomeHeroSlider heightPreset={config.heightPreset} layout="contained" />
        </div>
      );
    case "hero.split":
      return (
        <div data-testid="home-hero">
          <HomeHeroSlider heightPreset={config.heightPreset} layout="split" title={config.title} />
        </div>
      );
    case "hero.side-promos":
      return (
        <div data-testid="home-hero">
          <HomeHeroSlider heightPreset={config.heightPreset} layout="side-promos" />
        </div>
      );
    case "story.circle":
      return <HomeStoriesSection layout="circle" />;
    case "story.image-circles":
      return <HomeStoriesSection layout="image-circles" />;
    case "story.rounded-cards":
      return <HomeStoriesSection layout="rounded-cards" />;
    case "category.image-cards":
      return <HomeCategoryGridSection homeCategories={context.homeCategories} layout="image-cards" />;
    case "category.compact-tiles":
      return <HomeCategoryGridSection homeCategories={context.homeCategories} layout="compact-tiles" />;
    case "category.horizontal-rail":
      return <HomeCategoryGridSection homeCategories={context.homeCategories} layout="horizontal-rail" />;
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
          layout="rail"
        />
      ) : null;
    case "product.category-columns":
      return <HomeBestSellersSection columns={context.bestSellerColumns} />;
    case "ranked.multi-column":
      return <HomeBestSellersSection columns={context.bestSellerColumns} />;
    case "product.compact-rows":
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
          layout="compact-rows"
        />
      ) : null;
    case "ranked.horizontal":
      return context.mostViewedProducts.length > 0 ? (
        <ProductRailSection
          id="home-ranked-horizontal"
          title={config.title ?? "رتبه‌بندی"}
          href={config.href ?? "/products"}
          linkLabel="همه"
          tone="plain"
          products={context.mostViewedProducts}
          slideClassName="w-[170px] md:w-[220px]"
          testId="home-ranked-horizontal"
          layout="rail"
        />
      ) : null;
    case "product.grid":
      return (
        <ProductRailSection
          id="home-product-grid"
          title={config.title ?? "شبکه کالا"}
          href={config.href ?? "/products"}
          linkLabel="همه"
          tone="plain"
          products={context.newArrivals}
          slideClassName="w-[170px]"
          testId="home-product-grid"
          layout="grid"
        />
      );
    case "product.featured-plus-rail":
      return (
        <ProductRailSection
          id="home-featured-plus-rail"
          title={config.title ?? "ویژه + ریل"}
          href={config.href ?? "/products"}
          linkLabel="همه"
          tone="plain"
          products={context.newArrivals.length ? context.newArrivals : context.specialOffers}
          slideClassName="w-[170px] md:w-[200px]"
          testId="home-featured-plus-rail"
          layout="featured-plus-rail"
        />
      );
    case "ranked.grid":
      return (
        <ProductRailSection
          id="home-ranked-grid"
          title={config.title ?? "شبکه رتبه‌دار"}
          href={config.href ?? "/products"}
          linkLabel="همه"
          tone="plain"
          products={context.mostViewedProducts.length ? context.mostViewedProducts : context.newArrivals}
          slideClassName="w-[170px]"
          testId="home-ranked-grid"
          layout="ranked-grid"
        />
      );
    case "banner.one-large-two-small":
      return <HomeMiddleBannersSection />;
    case "banner.single":
      return <CompositionBannerGrid layout="single" heightPreset={config.heightPreset} />;
    case "banner.two-equal":
      return <CompositionBannerGrid layout="two-equal" heightPreset={config.heightPreset} />;
    case "banner.two-asymmetric":
      return <CompositionBannerGrid layout="two-asymmetric" heightPreset={config.heightPreset} />;
    case "banner.three":
      return <CompositionBannerGrid layout="three" heightPreset={config.heightPreset} />;
    case "banner.four-grid":
      return <CompositionBannerGrid layout="four-grid" heightPreset={config.heightPreset} />;
    case "banner.mosaic-2x2":
      return <CompositionBannerGrid layout="mosaic-2x2" heightPreset={config.heightPreset} />;
    case "banner.one-large-four-small":
      return <CompositionBannerGrid layout="one-large-four-small" heightPreset={config.heightPreset} />;
    case "banner.eight-compact":
      return <CompositionBannerGrid layout="eight-compact" heightPreset={config.heightPreset} />;
    case "brand.logo-rail":
      return <HomeBrandsSection brands={context.brands} layout="logo-rail" />;
    case "brand.logo-grid":
      return <HomeBrandsSection brands={context.brands} layout="logo-grid" />;
    case "brand.featured":
      return <HomeBrandsSection brands={context.brands} layout="featured" />;
    case "reviews.card-carousel":
      return <HomeTestimonialsSection reviews={context.featuredReviews} layout="card-carousel" />;
    case "reviews.compact-quotes":
      return <HomeTestimonialsSection reviews={context.featuredReviews} layout="compact-quotes" />;
    case "article.magazine-rail":
      return <HomeArticlesSection articles={context.latestArticles} layout="magazine-rail" />;
    case "article.grid":
      return <HomeArticlesSection articles={context.latestArticles} layout="grid" />;
    case "article.featured-plus-list":
      return <HomeArticlesSection articles={context.latestArticles} layout="featured-plus-list" />;
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
        layout="rail"
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
    case "hero.split":
    case "hero.side-promos":
      return <LandingHero config={config} layout={heroLayoutFromVariant(variantKey)} />;
    case "story.circle":
    case "story.image-circles":
    case "story.rounded-cards":
      return <HomeStoriesSection layout={storyLayoutFromVariant(variantKey)} />;
    case "product.card-carousel":
      return <LandingProductRail section={section} config={config} products={context.products} layout="rail" />;
    case "product.grid":
      return <LandingProductRail section={section} config={config} products={context.products} layout="grid" />;
    case "product.compact-rows":
      return <LandingProductRail section={section} config={config} products={context.products} layout="compact-rows" />;
    case "ranked.horizontal":
      return <LandingProductRail section={section} config={config} products={context.products} layout="rail" />;
    case "product.category-columns":
    case "ranked.multi-column":
      return <LandingProductRail section={section} config={config} products={context.products} layout="columns" />;
    case "product.featured-plus-rail":
      return <LandingProductRail section={section} config={config} products={context.products} layout="featured-plus-rail" />;
    case "ranked.grid":
      return <LandingProductRail section={section} config={config} products={context.products} layout="ranked-grid" />;
    case "category.image-cards":
    case "category.compact-tiles":
    case "category.horizontal-rail":
      return <LandingCategoryGrid config={config} categories={context.categories} layout={categoryLayoutFromVariant(variantKey)} />;
    case "brand.logo-rail":
      return <LandingBrandStrip config={config} brands={context.brands} layout="logo-rail" />;
    case "brand.logo-grid":
      return <LandingBrandStrip config={config} brands={context.brands} layout="logo-grid" />;
    case "brand.featured":
      return <LandingBrandStrip config={config} brands={context.brands} layout="featured" />;
    case "promo.default":
      return <LandingPromo config={config} />;
    case "banner.single":
    case "banner.two-equal":
    case "banner.two-asymmetric":
    case "banner.three":
    case "banner.four-grid":
    case "banner.mosaic-2x2":
    case "banner.one-large-two-small":
    case "banner.one-large-four-small":
    case "banner.eight-compact": {
      const rawItems = Array.isArray(config.items) ? config.items : [];
      const items = rawItems
        .filter((item): item is Record<string, unknown> => Boolean(item) && typeof item === "object")
        .map((item) => ({
          src: typeof item.imageUrl === "string" ? item.imageUrl : undefined,
          href: typeof item.href === "string" ? item.href : undefined,
          title: typeof item.title === "string" ? item.title : undefined,
        }));
      return (
        <CompositionBannerGrid
          layout={bannerLayoutFromVariant(variantKey)}
          title={typeof config.title === "string" ? config.title : undefined}
          href={typeof config.href === "string" ? config.href : undefined}
          heightPreset={config.heightPreset}
          testId="landing-banner-showcase"
          items={items}
        />
      );
    }
    case "article.magazine-rail":
      return <LandingArticleList config={config} articles={context.articles} layout="magazine-rail" />;
    case "article.grid":
      return <LandingArticleList config={config} articles={context.articles} layout="grid" />;
    case "article.featured-plus-list":
      return <LandingArticleList config={config} articles={context.articles} layout="featured-plus-list" />;
    case "reviews.card-carousel":
      return <LandingReviews reviews={context.reviews} layout="card-carousel" />;
    case "reviews.compact-quotes":
      return <LandingReviews reviews={context.reviews} layout="compact-quotes" />;
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
