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
import { PreviewPlaceholderSurface } from "../../app/storefront/preview-placeholder-surface.tsx";
import { storefrontMediaUrl } from "../../app/storefront/storefront-api.ts";
import { resolveObjectPosition } from "./template-preview-context.ts";
import { resolveObjectPosition } from "./template-preview-context.ts";
import type { CompositionSectionInstance, CompositionSurfaceRole } from "./types.ts";
import { getVariant } from "./registry.ts";
import { canonicalizeVariantKey, resolveSharedVariant } from "./resolve-variant.ts";
import { surfaceRoleClass } from "../storefront-appearance/surface-role.ts";

export { resolveSharedVariant, canonicalizeVariantKey };

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
  /** Template preview mode — enables preview-only placeholders; never published. */
  preview?: boolean;
  /** sample | store — store forbids home/sample banner fallbacks. */
  previewSource?: "sample" | "store";
  previewLocale?: string;
};

function heroLayoutFromVariant(variantKey: string): HeroLayout {
  if (variantKey === "hero.contained") return "contained";
  if (variantKey === "hero.split") return "split";
  if (variantKey === "hero.side-promos") return "side-promos";
  if (variantKey === "hero.editorial") return "editorial";
  return "full-width";
}

function storyLayoutFromVariant(variantKey: string): "circle" | "image-circles" | "rounded-cards" | "icon-shortcuts" {
  if (variantKey === "story.image-circles") return "image-circles";
  if (variantKey === "story.rounded-cards") return "rounded-cards";
  if (variantKey === "story.icon-shortcuts") return "icon-shortcuts";
  return "circle";
}

function categoryLayoutFromVariant(variantKey: string): CategoryLayout {
  if (variantKey === "category.compact-tiles") return "compact-tiles";
  if (variantKey === "category.horizontal-rail") return "horizontal-rail";
  if (variantKey === "category.editorial-tiles") return "editorial-tiles";
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
    case "banner.mosaic-2x2":
      return "four-grid";
    case "banner.one-large-two-small":
      return "one-large-two-small";
    case "banner.one-large-four-small":
      return "one-large-four-small";
    case "banner.eight-compact":
      return "eight-compact";
    default:
      return "single";
  }
}

export function renderSharedHomeSection(
  variantKey: string,
  context: HomeRenderContext,
  config: SectionDisplayConfig,
): ReactNode | null {
  const key = canonicalizeVariantKey(variantKey);
  const variant = getVariant(key);
  if (!variant || !variant.implemented) return null;

  switch (key) {
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
    case "hero.editorial":
      return (
        <div data-testid="home-hero">
          <HomeHeroSlider heightPreset={config.heightPreset} layout="editorial" title={config.title} subtitle={config.subtitle} href={config.href} />
        </div>
      );
    case "story.circle":
      return <HomeStoriesSection layout="circle" />;
    case "story.image-circles":
      return <HomeStoriesSection layout="image-circles" />;
    case "story.rounded-cards":
      return <HomeStoriesSection layout="rounded-cards" />;
    case "story.icon-shortcuts":
      return <HomeStoriesSection layout="icon-shortcuts" />;
    case "category.image-cards":
      return <HomeCategoryGridSection homeCategories={context.homeCategories} layout="image-cards" />;
    case "category.compact-tiles":
      return <HomeCategoryGridSection homeCategories={context.homeCategories} layout="compact-tiles" />;
    case "category.horizontal-rail":
      return <HomeCategoryGridSection homeCategories={context.homeCategories} layout="horizontal-rail" />;
    case "category.editorial-tiles":
      return <HomeCategoryGridSection homeCategories={context.homeCategories} layout="editorial-tiles" />;
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
    case "product.tabbed":
      return (
        <ProductRailSection
          id="home-product-tabbed"
          title={config.title ?? "کالاها"}
          href={config.href ?? "/products"}
          linkLabel="همه"
          tone="plain"
          products={context.newArrivals.length ? context.newArrivals : context.specialOffers}
          slideClassName="w-[170px] md:w-[210px]"
          testId="home-product-tabbed"
          layout="tabbed"
        />
      );
    case "product.large-cards":
      return (
        <ProductRailSection
          id="home-product-large-cards"
          title={config.title ?? "کارت‌های بزرگ"}
          href={config.href ?? "/products"}
          linkLabel="همه"
          tone="plain"
          products={context.newArrivals.length ? context.newArrivals : context.specialOffers}
          slideClassName="w-full"
          testId="home-product-large-cards"
          layout="large-cards"
        />
      );
    case "product.minimal-list":
      return (
        <ProductRailSection
          id="home-product-minimal-list"
          title={config.title ?? "فهرست کالا"}
          href={config.href ?? "/products"}
          linkLabel="همه"
          tone="plain"
          products={context.mostViewedProducts.length ? context.mostViewedProducts : context.newArrivals}
          slideClassName="w-full"
          testId="home-product-minimal-list"
          layout="minimal-list"
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
    case "ranked.ticker":
      return (
        <ProductRailSection
          id="home-ranked-ticker"
          title={config.title ?? "رتبه‌بندی فشرده"}
          href={config.href ?? "/products"}
          linkLabel="همه"
          tone="plain"
          products={context.mostViewedProducts.length ? context.mostViewedProducts : context.newArrivals}
          slideClassName="w-[220px]"
          testId="home-ranked-ticker"
          layout="ticker"
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
  const { composition, config, context, section, preview = false, previewSource, previewLocale = "fa" } = input;
  if (!composition.enabled) return null;
  const variantKey = canonicalizeVariantKey(composition.variantKey);
  const storePreview = Boolean(preview && previewSource === "store");
  const wantsPlaceholder = Boolean(config.previewPlaceholder);

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
    case "hero.editorial": {
      if (storePreview && wantsPlaceholder) {
        return <PreviewPlaceholderSurface kind="hero" locale={previewLocale} aspectClass="min-h-[200px] md:min-h-[280px]" />;
      }
      const heroConfig = { ...config };
      if (typeof heroConfig.mediaAssetId === "string" && heroConfig.mediaAssetId && !heroConfig.imageUrl) {
        heroConfig.imageUrl = storefrontMediaUrl(String(heroConfig.mediaAssetId));
      }
      if (heroConfig.focalPointX != null || heroConfig.focalPointY != null) {
        heroConfig.objectPosition = resolveObjectPosition(
          typeof heroConfig.focalPointX === "number" ? heroConfig.focalPointX : null,
          typeof heroConfig.focalPointY === "number" ? heroConfig.focalPointY : null,
        );
      }
      return <LandingHero config={heroConfig} layout={heroLayoutFromVariant(variantKey)} />;
    }
    case "story.circle":
    case "story.image-circles":
    case "story.rounded-cards":
    case "story.icon-shortcuts":
      if (storePreview && wantsPlaceholder) {
        return <PreviewPlaceholderSurface kind="story" locale={previewLocale} slots={4} aspectClass="aspect-square min-h-[96px]" />;
      }
      return <HomeStoriesSection layout={storyLayoutFromVariant(variantKey)} />;
    case "product.card-carousel":
      if (storePreview && wantsPlaceholder) {
        return <PreviewPlaceholderSurface kind="product" locale={previewLocale} slots={2} aspectClass="min-h-[160px]" />;
      }
      return <LandingProductRail section={section} config={config} products={context.products} layout="rail" />;
    case "product.grid":
      if (storePreview && wantsPlaceholder) {
        return <PreviewPlaceholderSurface kind="product" locale={previewLocale} slots={2} aspectClass="min-h-[160px]" />;
      }
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
    case "product.tabbed":
      return <LandingProductRail section={section} config={config} products={context.products} layout="tabbed" />;
    case "product.large-cards":
      return <LandingProductRail section={section} config={config} products={context.products} layout="large-cards" />;
    case "product.minimal-list":
      return <LandingProductRail section={section} config={config} products={context.products} layout="minimal-list" />;
    case "ranked.grid":
      return <LandingProductRail section={section} config={config} products={context.products} layout="ranked-grid" />;
    case "ranked.ticker":
      return <LandingProductRail section={section} config={config} products={context.products} layout="ticker" />;
    case "category.image-cards":
    case "category.compact-tiles":
    case "category.horizontal-rail":
    case "category.editorial-tiles":
      if (storePreview && wantsPlaceholder) {
        return <PreviewPlaceholderSurface kind="category" locale={previewLocale} slots={2} aspectClass="min-h-[140px]" />;
      }
      return <LandingCategoryGrid config={config} categories={context.categories} layout={categoryLayoutFromVariant(variantKey)} />;
    case "brand.logo-rail":
      if (storePreview && wantsPlaceholder) {
        return <PreviewPlaceholderSurface kind="brand" locale={previewLocale} slots={2} aspectClass="min-h-[100px]" />;
      }
      return <LandingBrandStrip config={config} brands={context.brands} layout="logo-rail" />;
    case "brand.logo-grid":
      if (storePreview && wantsPlaceholder) {
        return <PreviewPlaceholderSurface kind="brand" locale={previewLocale} slots={2} aspectClass="min-h-[100px]" />;
      }
      return <LandingBrandStrip config={config} brands={context.brands} layout="logo-grid" />;
    case "brand.featured":
      if (storePreview && wantsPlaceholder) {
        return <PreviewPlaceholderSurface kind="brand" locale={previewLocale} slots={2} aspectClass="min-h-[100px]" />;
      }
      return <LandingBrandStrip config={config} brands={context.brands} layout="featured" />;
    case "promo.default":
      if (storePreview && wantsPlaceholder) {
        return <PreviewPlaceholderSurface kind="promo" locale={previewLocale} />;
      }
      return <LandingPromo config={config} />;
    case "banner.single":
    case "banner.two-equal":
    case "banner.two-asymmetric":
    case "banner.three":
    case "banner.four-grid":
    case "banner.one-large-two-small":
    case "banner.one-large-four-small":
    case "banner.eight-compact": {
      const rawItems = Array.isArray(config.items) ? config.items : [];
      const items = rawItems
        .filter((item): item is Record<string, unknown> => Boolean(item) && typeof item === "object")
        .map((item) => {
          const mediaId = typeof item.mediaAssetId === "string" ? item.mediaAssetId : undefined;
          const imageUrl = typeof item.imageUrl === "string" ? item.imageUrl : undefined;
          const src = imageUrl?.trim() || (mediaId ? storefrontMediaUrl(mediaId) : undefined);
          return {
            src,
            href: typeof item.href === "string" ? item.href : undefined,
            title: typeof item.title === "string" ? item.title : undefined,
            objectPosition: resolveObjectPosition(
              typeof item.focalPointX === "number" ? item.focalPointX : null,
              typeof item.focalPointY === "number" ? item.focalPointY : null,
            ),
          };
        });
      return (
        <CompositionBannerGrid
          layout={bannerLayoutFromVariant(variantKey)}
          title={typeof config.title === "string" ? config.title : undefined}
          href={typeof config.href === "string" ? config.href : undefined}
          heightPreset={config.heightPreset}
          testId="landing-banner-showcase"
          items={items}
          allowHomeFallback={!storePreview}
          previewPlaceholder={storePreview && (wantsPlaceholder || items.length === 0 || items.every((i) => !i.src))}
          previewPlaceholderSlots={typeof config.previewPlaceholderSlots === "number" ? config.previewPlaceholderSlots : undefined}
          previewLocale={previewLocale}
        />
      );
    }
    case "article.magazine-rail":
      if (storePreview && wantsPlaceholder) {
        return <PreviewPlaceholderSurface kind="article" locale={previewLocale} slots={2} aspectClass="min-h-[140px]" />;
      }
      return <LandingArticleList config={config} articles={context.articles} layout="magazine-rail" />;
    case "article.grid":
      if (storePreview && wantsPlaceholder) {
        return <PreviewPlaceholderSurface kind="article" locale={previewLocale} slots={2} aspectClass="min-h-[140px]" />;
      }
      return <LandingArticleList config={config} articles={context.articles} layout="grid" />;
    case "article.featured-plus-list":
      if (storePreview && wantsPlaceholder) {
        return <PreviewPlaceholderSurface kind="article" locale={previewLocale} slots={2} aspectClass="min-h-[140px]" />;
      }
      return <LandingArticleList config={config} articles={context.articles} layout="featured-plus-list" />;
    case "reviews.card-carousel":
      if (storePreview && wantsPlaceholder) {
        return <PreviewPlaceholderSurface kind="reviews" locale={previewLocale} slots={2} aspectClass="min-h-[140px]" />;
      }
      return <LandingReviews reviews={context.reviews} layout="card-carousel" />;
    case "reviews.compact-quotes":
      if (storePreview && wantsPlaceholder) {
        return <PreviewPlaceholderSurface kind="reviews" locale={previewLocale} slots={2} aspectClass="min-h-[140px]" />;
      }
      return <LandingReviews reviews={context.reviews} layout="compact-quotes" />;
    case "richtext.default":
      if (storePreview && wantsPlaceholder) {
        return <PreviewPlaceholderSurface kind="richtext" locale={previewLocale} />;
      }
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
