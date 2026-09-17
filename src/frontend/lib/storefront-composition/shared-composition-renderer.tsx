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
  HomeTestimonialsSection,
} from "../../app/storefront/storefront-home-repair-sections.tsx";
import {
  HomeAmazingProductSection,
  HomeMahoorProductSection,
  HomeZohrehProductSection,
} from "../../app/storefront/storefront-home-shopeiva-product-layouts.tsx";
import { HomeStoriesSection } from "../../app/storefront/stories/home-stories.tsx";
import {
  CompositionBannerGrid,
  HomeCategoryGridSection,
  HomeHeroSlider,
  HomeMiddleBannersSection,
  ProductRailSection,
  type BannerLayout,
  type CategoryLayout,
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
import { storefrontMediaUrl } from "../../app/storefront/storefront-api.ts";
import { resolveObjectPosition } from "./template-preview-context.ts";
import type { CompositionSectionInstance, CompositionSurfaceRole } from "./types.ts";
import { getVariant } from "./registry.ts";
import { canonicalizeVariantKey, resolveSharedVariant } from "./resolve-variant.ts";
import { heroVariantIdFromKey, type HeroSliderVariantId } from "./hero-slider-config.ts";
import { surfaceRoleClass } from "../storefront-appearance/surface-role.ts";
import {
  createFakeArticle,
  createFakeBanner,
  createFakeBrand,
  createFakeCategory,
  createFakeHeroConfig,
  createFakeProduct,
  createFakePromoConfig,
  createFakeReview,
  createFakeRichTextConfig,
} from "./preview-fake-data.ts";
import { applyPreviewFill, isStorePreviewFillEnabled } from "./preview-fill-policy.ts";
import { asIds } from "../../app/storefront/storefront-landing-blocks.tsx";

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

function heroLayoutFromVariant(variantKey: string): HeroSliderVariantId {
  return heroVariantIdFromKey(variantKey);
}

function homeHeroLayoutFromVariant(
  variantKey: string,
): "full-width" | "contained" | "split" | "side-promos" | "editorial" {
  const id = heroVariantIdFromKey(variantKey);
  switch (id) {
    case "shapes":
      return "contained";
    case "diagonal":
      return "side-promos";
    case "split":
      return "split";
    case "editorial":
    case "cinematic":
      return "editorial";
    case "fullscreen":
    default:
      return "full-width";
  }
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
    case "hero.fullscreen":
    case "hero.full-width":
      return (
        <div data-testid="home-hero">
          <HomeHeroSlider heightPreset={config.heightPreset} layout={homeHeroLayoutFromVariant(key)} />
        </div>
      );
    case "hero.shapes":
    case "hero.contained":
      return (
        <div data-testid="home-hero">
          <HomeHeroSlider heightPreset={config.heightPreset} layout={homeHeroLayoutFromVariant(key)} />
        </div>
      );
    case "hero.split":
      return (
        <div data-testid="home-hero">
          <HomeHeroSlider heightPreset={config.heightPreset} layout="split" title={config.title} />
        </div>
      );
    case "hero.diagonal":
    case "hero.side-promos":
      return (
        <div data-testid="home-hero">
          <HomeHeroSlider heightPreset={config.heightPreset} layout={homeHeroLayoutFromVariant(key)} />
        </div>
      );
    case "hero.cinematic":
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
        return <HomeMahoorProductSection products={context.newArrivals} />;
      }
      return context.specialOffers.length > 0 ? (
        <HomeAmazingProductSection products={context.specialOffers} title={config.title} href={config.href} />
      ) : null;
    case "product.amazing":
      return <HomeAmazingProductSection products={context.specialOffers} title={config.title} href={config.href} />;
    case "product.zohreh":
      return <HomeZohrehProductSection products={context.mostViewedProducts} title={config.title} href={config.href} />;
    case "product.mahoor":
      return <HomeMahoorProductSection products={context.newArrivals} title={config.title} href={config.href} />;
    case "product.category-columns":
      return <HomeBestSellersSection columns={context.bestSellerColumns} />;
    case "ranked.multi-column":
      return <HomeBestSellersSection columns={context.bestSellerColumns} />;
    case "product.compact-rows":
      return context.mostViewedProducts.length > 0 ? (
        <HomeZohrehProductSection products={context.mostViewedProducts} title={config.title ?? "پربازدیدترین‌ها"} href={config.href} />
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

/** Home path: flash / newest / most-viewed use Shopeiva product layouts. */
export function renderSharedHomeSectionForLegacyType(
  homeSectionType: string,
  variantKey: string,
  context: HomeRenderContext,
  config: SectionDisplayConfig,
): ReactNode | null {
  if (homeSectionType === "product_rail_flash") {
    return (
      <HomeAmazingProductSection
        products={context.specialOffers}
        title={config.title ?? "شگفت‌انگیزهای امروز"}
        href={config.href ?? "/offers"}
      />
    );
  }
  if (homeSectionType === "product_rail_most_viewed") {
    return (
      <HomeZohrehProductSection
        products={context.mostViewedProducts}
        title={config.title ?? "پربازدیدترین‌ها"}
        href={config.href ?? "/most-viewed"}
      />
    );
  }
  if (homeSectionType === "newest_products") {
    return (
      <HomeMahoorProductSection
        products={context.newArrivals}
        title={config.title ?? "جدیدترین محصولات"}
        href={config.href ?? "/new-products"}
      />
    );
  }
  return renderSharedHomeSection(variantKey, context, config);
}

export function renderSharedLandingSection(input: SharedLandingRenderInput): ReactNode | null {
  const { composition, config, context, section, preview = false, previewSource, previewLocale = "fa" } = input;
  if (!composition.enabled) return null;
  const variantKey = canonicalizeVariantKey(composition.variantKey);
  const storePreview = isStorePreviewFillEnabled(preview, previewSource);
  const locale = previewLocale || "fa";

  try {
    resolveSharedVariant(composition.sectionTypeKey, variantKey, { strict: true });
  } catch {
    return null;
  }

  const fillProducts = (layout: Parameters<typeof LandingProductRail>[0]["layout"]) => {
    const wanted = new Set(
      section.items.map((item) => item.id).concat(section.items.map((item) => item.slug).filter(Boolean) as string[]),
    );
    const realSelected = wanted.size
      ? context.products.filter((card) => wanted.has(card.productId) || wanted.has(card.slug))
      : context.products;
    const filled = applyPreviewFill({
      enabled: storePreview,
      variantKey,
      realItems: realSelected,
      createFake: (index) => createFakeProduct(index, locale),
    });
    const sectionFilled = {
      ...section,
      items: filled.items.map((card) => ({ id: card.productId, slug: card.slug })),
    };
    const configFilled = {
      ...config,
      productIds: filled.items.map((card) => card.productId),
      take: Math.max(typeof config.take === "number" ? config.take : 0, filled.items.length),
    };
    delete (configFilled as { previewPlaceholder?: boolean }).previewPlaceholder;
    return (
      <LandingProductRail
        section={sectionFilled}
        config={configFilled}
        products={filled.items}
        layout={layout}
        previewLocale={locale}
      />
    );
  };

  switch (variantKey) {
    case "hero.fullscreen":
    case "hero.shapes":
    case "hero.diagonal":
    case "hero.cinematic":
    case "hero.split":
    case "hero.editorial":
    case "hero.full-width":
    case "hero.contained":
    case "hero.side-promos": {
      const hasSlideMedia =
        Array.isArray(config.slides)
        && config.slides.some((item) => {
          if (!item || typeof item !== "object") return false;
          const row = item as Record<string, unknown>;
          return (
            (typeof row.imageUrl === "string" && row.imageUrl.trim().length > 0)
            || (typeof row.mediaAssetId === "string" && row.mediaAssetId.trim().length > 0)
          );
        });
      const hasMedia =
        hasSlideMedia
        || (typeof config.imageUrl === "string" && config.imageUrl.trim().length > 0)
        || (typeof config.mediaAssetId === "string" && config.mediaAssetId.trim().length > 0);
      const heroConfig = {
        ...(storePreview && !hasMedia ? createFakeHeroConfig(locale) : {}),
        ...config,
      };
      if (storePreview && !hasMedia) {
        Object.assign(heroConfig, createFakeHeroConfig(locale));
      }
      if (typeof heroConfig.mediaAssetId === "string" && heroConfig.mediaAssetId && !heroConfig.imageUrl) {
        heroConfig.imageUrl = storefrontMediaUrl(String(heroConfig.mediaAssetId));
      }
      if (heroConfig.focalPointX != null || heroConfig.focalPointY != null) {
        heroConfig.objectPosition = resolveObjectPosition(
          typeof heroConfig.focalPointX === "number" ? heroConfig.focalPointX : null,
          typeof heroConfig.focalPointY === "number" ? heroConfig.focalPointY : null,
        );
      }
      delete (heroConfig as { previewPlaceholder?: boolean }).previewPlaceholder;
      return (
        <LandingHero
          config={heroConfig}
          layout={heroLayoutFromVariant(variantKey)}
          previewLocale={locale}
          showPreviewBadge={Boolean(storePreview && heroConfig.previewFake)}
        />
      );
    }
    case "story.circle":
    case "story.image-circles":
    case "story.rounded-cards":
    case "story.icon-shortcuts":
      return (
        <HomeStoriesSection
          layout={storyLayoutFromVariant(variantKey)}
          storePreviewFill={storePreview}
          previewLocale={locale}
          previewVariantKey={variantKey}
        />
      );
    case "product.card-carousel":
      return fillProducts("rail");
    case "product.amazing": {
      const wanted = new Set(
        section.items.map((item) => item.id).concat(section.items.map((item) => item.slug).filter(Boolean) as string[]),
      );
      const realSelected = wanted.size
        ? context.products.filter((card) => wanted.has(card.productId) || wanted.has(card.slug))
        : context.products.filter((card) => card.promotionalAmountExclusiveOfTax != null || card.promotionLabel);
      const filled = applyPreviewFill({
        enabled: storePreview,
        variantKey,
        realItems: realSelected.length ? realSelected : context.products,
        createFake: (index) => createFakeProduct(index, locale),
      });
      return (
        <HomeAmazingProductSection
          products={filled.items}
          title={typeof config.title === "string" ? config.title : "شگفت‌انگیزهای امروز"}
          href={typeof config.href === "string" ? config.href : "/offers"}
          previewLocale={locale}
        />
      );
    }
    case "product.zohreh": {
      const wanted = new Set(
        section.items.map((item) => item.id).concat(section.items.map((item) => item.slug).filter(Boolean) as string[]),
      );
      const realSelected = wanted.size
        ? context.products.filter((card) => wanted.has(card.productId) || wanted.has(card.slug))
        : context.products;
      const filled = applyPreviewFill({
        enabled: storePreview,
        variantKey,
        realItems: realSelected,
        createFake: (index) => createFakeProduct(index, locale),
      });
      return (
        <HomeZohrehProductSection
          products={filled.items}
          title={typeof config.title === "string" ? config.title : "پربازدیدترین‌ها"}
          href={typeof config.href === "string" ? config.href : "/most-viewed"}
          previewLocale={locale}
        />
      );
    }
    case "product.mahoor": {
      const wanted = new Set(
        section.items.map((item) => item.id).concat(section.items.map((item) => item.slug).filter(Boolean) as string[]),
      );
      const realSelected = wanted.size
        ? context.products.filter((card) => wanted.has(card.productId) || wanted.has(card.slug))
        : context.products;
      const filled = applyPreviewFill({
        enabled: storePreview,
        variantKey,
        realItems: realSelected,
        createFake: (index) => createFakeProduct(index, locale),
      });
      return (
        <HomeMahoorProductSection
          products={filled.items}
          title={typeof config.title === "string" ? config.title : "جدیدترین محصولات"}
          href={typeof config.href === "string" ? config.href : "/new-products"}
          previewLocale={locale}
        />
      );
    }
    case "product.grid":
      return fillProducts("grid");
    case "product.compact-rows":
      return fillProducts("compact-rows");
    case "ranked.horizontal":
      return fillProducts("rail");
    case "product.category-columns":
    case "ranked.multi-column":
      return fillProducts("columns");
    case "product.featured-plus-rail":
      return fillProducts("featured-plus-rail");
    case "product.tabbed":
      return fillProducts("tabbed");
    case "product.large-cards":
      return fillProducts("large-cards");
    case "product.minimal-list":
      return fillProducts("minimal-list");
    case "ranked.grid":
      return fillProducts("ranked-grid");
    case "ranked.ticker":
      return fillProducts("ticker");
    case "category.image-cards":
    case "category.compact-tiles":
    case "category.horizontal-rail":
    case "category.editorial-tiles": {
      const ids = asIds(config, "ids", "categoryIds");
      const realSelected = ids.length
        ? context.categories.filter((item) => ids.includes(item.categoryId))
        : context.categories;
      const filled = applyPreviewFill({
        enabled: storePreview,
        variantKey,
        realItems: realSelected,
        createFake: (index) => createFakeCategory(index, locale),
      });
      const configFilled = {
        ...config,
        categoryIds: filled.items.map((item) => item.categoryId),
      };
      delete (configFilled as { previewPlaceholder?: boolean }).previewPlaceholder;
      return (
        <LandingCategoryGrid
          config={configFilled}
          categories={filled.items}
          layout={categoryLayoutFromVariant(variantKey)}
          previewLocale={locale}
        />
      );
    }
    case "brand.logo-rail":
    case "brand.logo-grid":
    case "brand.featured": {
      const ids = asIds(config, "ids", "brandIds");
      const realSelected = ids.length
        ? context.brands.filter((item) => ids.includes(item.brandId))
        : context.brands;
      const filled = applyPreviewFill({
        enabled: storePreview,
        variantKey,
        realItems: realSelected,
        createFake: (index) => createFakeBrand(index, locale),
      });
      const configFilled = {
        ...config,
        brandIds: filled.items.map((item) => item.brandId),
      };
      delete (configFilled as { previewPlaceholder?: boolean }).previewPlaceholder;
      const layout =
        variantKey === "brand.logo-grid" ? "logo-grid" : variantKey === "brand.featured" ? "featured" : "logo-rail";
      return (
        <LandingBrandStrip
          config={configFilled}
          brands={filled.items}
          layout={layout}
          previewLocale={locale}
        />
      );
    }
    case "promo.default": {
      const hasContent = typeof config.title === "string" && config.title.trim().length > 0;
      const promoConfig = storePreview && !hasContent
        ? { ...createFakePromoConfig(locale), ...config, ...createFakePromoConfig(locale) }
        : { ...config };
      delete (promoConfig as { previewPlaceholder?: boolean }).previewPlaceholder;
      return <LandingPromo config={promoConfig} previewLocale={locale} showPreviewBadge={Boolean(storePreview && promoConfig.previewFake)} />;
    }
    case "banner.single":
    case "banner.two-equal":
    case "banner.two-asymmetric":
    case "banner.three":
    case "banner.four-grid":
    case "banner.one-large-two-small":
    case "banner.one-large-four-small":
    case "banner.eight-compact": {
      const rawItems = Array.isArray(config.items) ? config.items : [];
      const realItems = rawItems
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
            previewFake: false as boolean | undefined,
          };
        })
        .filter((item) => Boolean(item.src));
      const filled = applyPreviewFill({
        enabled: storePreview,
        variantKey,
        realItems,
        createFake: (index) => createFakeBanner(index, locale),
      });
      return (
        <CompositionBannerGrid
          layout={bannerLayoutFromVariant(variantKey)}
          title={typeof config.title === "string" ? config.title : undefined}
          href={typeof config.href === "string" ? config.href : undefined}
          heightPreset={config.heightPreset}
          testId="landing-banner-showcase"
          items={filled.items}
          allowHomeFallback={!storePreview}
          previewLocale={locale}
        />
      );
    }
    case "article.magazine-rail":
    case "article.grid":
    case "article.featured-plus-list": {
      const take = typeof config.take === "number" ? config.take : 6;
      const source = typeof config.source === "string" ? config.source : "Latest";
      const articleIds = Array.isArray(config.articleIds)
        ? config.articleIds.map((id) => String(id)).filter(Boolean)
        : [];
      const realSelected = source === "Manual" && articleIds.length > 0
        ? articleIds
          .map((id) => context.articles.find((article) => article.articleId === id))
          .filter((article): article is (typeof context.articles)[number] => Boolean(article))
        : context.articles;
      const filled = applyPreviewFill({
        enabled: storePreview,
        variantKey,
        realItems: realSelected.slice(0, take),
        createFake: (index) => createFakeArticle(index, locale),
      });
      const layout =
        variantKey === "article.grid"
          ? "grid"
          : variantKey === "article.featured-plus-list"
            ? "featured-plus-list"
            : "magazine-rail";
      return (
        <LandingArticleList
          config={{ ...config, take: filled.items.length, source: "Latest" }}
          articles={filled.items}
          layout={layout}
          previewLocale={locale}
        />
      );
    }
    case "reviews.card-carousel":
    case "reviews.compact-quotes": {
      const filled = applyPreviewFill({
        enabled: storePreview,
        variantKey,
        realItems: context.reviews,
        createFake: (index) => createFakeReview(index, locale),
      });
      return (
        <LandingReviews
          reviews={filled.items}
          layout={variantKey === "reviews.compact-quotes" ? "compact-quotes" : "card-carousel"}
          previewLocale={locale}
        />
      );
    }
    case "richtext.default": {
      const hasText = typeof config.text === "string" && config.text.trim().length > 0;
      const richConfig = storePreview && !hasText
        ? { ...createFakeRichTextConfig(locale) }
        : { ...config };
      delete (richConfig as { previewPlaceholder?: boolean }).previewPlaceholder;
      return <LandingRichText config={richConfig} previewLocale={locale} showPreviewBadge={Boolean(storePreview && richConfig.previewFake)} />;
    }
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
