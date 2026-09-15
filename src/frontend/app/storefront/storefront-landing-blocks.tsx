"use client";

import type { ReactNode } from "react";
import { LocalizedLink as Link } from "../../lib/i18n/LocalizedLink.tsx";
import { StorefrontProductCardView } from "./storefront-product-card.tsx";
import {
  HomeArticlesSection,
  HomeBrandsSection,
  HomeTestimonialsSection,
} from "./storefront-home-repair-sections.tsx";
import type { StorefrontCategoryItem, StorefrontProductCard } from "./storefront-model.ts";
import type { StorefrontLandingSection } from "./storefront-landing-api.ts";
import { StorefrontMenuLinks } from "./storefront-menu-tree.tsx";
import type { StorefrontMenuItem } from "./storefront-menu-api.ts";
import { heightPresetBannerClass, heightPresetHeroClass } from "../../lib/storefront-composition/size-presets.ts";

export function asIds(config: Record<string, unknown>, ...keys: string[]): string[] {
  for (const key of keys) {
    const value = config[key];
    if (Array.isArray(value)) return value.map((item) => String(item));
  }
  return [];
}

export function titleOf(config: Record<string, unknown>, fallback: string): string {
  return typeof config.title === "string" && config.title.trim() ? config.title.trim() : fallback;
}

export function LandingHero({ config, layout = "contained" }: { config: Record<string, unknown>; layout?: "full-width" | "contained" | "split" | "side-promos" }) {
  const title = titleOf(config, "فروشگاه توبا");
  const subtitle = typeof config.subtitle === "string" ? config.subtitle : "";
  const href = typeof config.href === "string" && config.href.trim() ? config.href : "/products";
  const heightClass = heightPresetHeroClass(config.heightPreset);

  if (layout === "split") {
    return (
      <section className="px-2 sm:px-4" data-testid="landing-hero" data-hero-layout="split">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 overflow-hidden rounded-3xl border border-gray-100 bg-surface shadow-xl">
          <Link href={href} className="relative block min-h-[180px] bg-gray-100">
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img src="/images/sliders/slider-1.jpg" alt="" className={`h-full w-full object-cover ${heightClass}`} />
          </Link>
          <div className="flex flex-col justify-center p-6">
            <h2 className="text-2xl font-black md:text-4xl">{title}</h2>
            {subtitle ? <p className="mt-2 max-w-xl text-sm md:text-base">{subtitle}</p> : null}
            <Link href={href} className="mt-4 inline-flex w-fit rounded-xl bg-primary px-4 py-2 text-sm font-bold text-white">مشاهده</Link>
          </div>
        </div>
      </section>
    );
  }

  if (layout === "side-promos") {
    return (
      <section className="px-2 sm:px-4" data-testid="landing-hero" data-hero-layout="side-promos">
        <div className="grid grid-cols-1 gap-3 md:grid-cols-[2fr_1fr]">
          <Link href={href} className="relative block overflow-hidden rounded-3xl bg-gradient-to-l from-primary to-primary-strong shadow-2xl">
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img src="/images/sliders/slider-1.jpg" alt="" className={`w-full object-cover ${heightClass}`} />
            <div className="absolute inset-0 bg-black/35" />
            <div className="absolute inset-0 flex flex-col justify-end p-6 text-white">
              <h2 className="text-2xl font-black md:text-4xl">{title}</h2>
              {subtitle ? <p className="mt-2 max-w-xl text-sm md:text-base">{subtitle}</p> : null}
            </div>
          </Link>
          <div className="grid grid-cols-2 gap-3 md:grid-cols-1">
            {["/images/sliders/slider-2.jpg", "/images/sliders/slider-3.jpg"].map((src) => (
              <Link key={src} href={href} className="relative block overflow-hidden rounded-2xl bg-gray-100 aspect-[16/9] md:aspect-auto md:min-h-[120px]">
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img src={src} alt="" className="absolute inset-0 h-full w-full object-cover" />
              </Link>
            ))}
          </div>
        </div>
      </section>
    );
  }

  return (
    <section className={`px-2 sm:px-4 ${layout === "contained" ? "max-w-6xl mx-auto" : ""}`} data-testid="landing-hero" data-hero-layout={layout}>
      <Link href={href} className={`relative block overflow-hidden bg-gradient-to-l from-primary to-primary-strong shadow-2xl ${layout === "contained" ? "rounded-3xl" : "rounded-none md:rounded-3xl"}`}>
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img src="/images/sliders/slider-1.jpg" alt="" className={`w-full object-cover ${heightClass}`} />
        <div className="absolute inset-0 bg-black/35" />
        <div className="absolute inset-0 flex flex-col justify-end p-6 text-white">
          <h2 className="text-2xl font-black md:text-4xl">{title}</h2>
          {subtitle ? <p className="mt-2 max-w-xl text-sm md:text-base">{subtitle}</p> : null}
        </div>
      </Link>
    </section>
  );
}

export function LandingPromo({ config }: { config: Record<string, unknown> }) {
  const title = titleOf(config, "پیشنهاد ویژه");
  const href = typeof config.href === "string" && config.href.trim() ? config.href : "/offers";
  const heightClass = heightPresetBannerClass(config.heightPreset);
  return (
    <section className="px-2 sm:px-4" data-testid="landing-promo">
      <Link href={href} className="relative block overflow-hidden rounded-3xl bg-gray-100">
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img src="/images/middleBanner/1.webp" alt="" className={`w-full object-cover ${heightClass}`} />
        <span className="absolute bottom-4 right-4 text-sm font-bold text-white">{title}</span>
      </Link>
    </section>
  );
}

export function LandingRichText({ config }: { config: Record<string, unknown> }) {
  const title = typeof config.title === "string" ? config.title : "";
  const text = typeof config.text === "string" ? config.text : "";
  if (!text) return null;
  return (
    <section className="mx-auto max-w-3xl px-4 py-8" data-testid="landing-rich-text">
      {title ? <h2 className="mb-3 text-xl font-black">{title}</h2> : null}
      <p className="whitespace-pre-wrap leading-8 text-foreground/80">{text}</p>
    </section>
  );
}

export function LandingCategoryGrid({
  config,
  categories,
  layout = "image-cards",
}: {
  config: Record<string, unknown>;
  categories: StorefrontCategoryItem[];
  layout?: "image-cards" | "compact-tiles" | "horizontal-rail";
}) {
  const ids = asIds(config, "ids", "categoryIds");
  const items = ids.length ? categories.filter((item) => ids.includes(item.categoryId)) : [];
  if (items.length === 0) return null;
  const images = [2, 3, 4, 5, 6, 7, 8, 9] as const;
  const listClass =
    layout === "compact-tiles"
      ? "grid grid-cols-3 sm:grid-cols-4 md:grid-cols-6 gap-2"
      : "flex gap-3 overflow-x-auto pb-2";
  const cardClass =
    layout === "compact-tiles"
      ? "overflow-hidden rounded-xl border border-gray-100 bg-surface"
      : layout === "horizontal-rail"
        ? "w-[110px] shrink-0 overflow-hidden rounded-full border border-gray-100 bg-surface"
        : "w-[160px] shrink-0 overflow-hidden rounded-2xl border border-gray-100 bg-surface";
  return (
    <section className="w-full px-2 py-8 sm:px-4" data-testid="landing-categories" data-category-layout={layout}>
      <h2 className="mb-4 text-lg font-bold">{titleOf(config, "دسته‌بندی‌ها")}</h2>
      <div className={listClass}>
        {items.map((category, index) => {
          const imageIndex = images[index % images.length]!;
          return (
            <Link key={category.categoryId} href={`/products?categoryId=${category.categoryId}`} className={cardClass}>
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={`/images/categories/${imageIndex}.png`} alt="" className="aspect-square w-full object-contain p-4" />
              <p className="px-2 py-3 text-center text-sm font-bold">{category.name}</p>
            </Link>
          );
        })}
      </div>
    </section>
  );
}

export function LandingProductRail({
  section,
  config,
  products,
  layout = "rail",
}: {
  section: StorefrontLandingSection;
  config: Record<string, unknown>;
  products: StorefrontProductCard[];
  layout?: "rail" | "grid" | "compact-rows" | "columns" | "featured-plus-rail" | "ranked-grid";
}) {
  const wanted = new Set(section.items.map((item) => item.id).concat(section.items.map((item) => item.slug).filter(Boolean) as string[]));
  const cards = products.filter((card) => wanted.has(card.productId) || wanted.has(card.slug)).slice(0, typeof config.take === "number" ? config.take : 8);
  if (cards.length === 0) return null;
  const title = titleOf(config, "کالاها");
  let body: ReactNode;
  if (layout === "grid" || layout === "ranked-grid") {
    body = (
      <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-3" data-product-layout={layout}>
        {cards.map((card) => (
          <StorefrontProductCardView key={`${section.pageSectionId}-${card.productId}`} card={card} />
        ))}
      </div>
    );
  } else if (layout === "compact-rows") {
    body = (
      <div className="space-y-2" data-product-layout="compact-rows">
        {cards.map((card) => (
          <div key={`${section.pageSectionId}-${card.productId}`} className="rounded-xl border border-gray-100 p-2">
            <StorefrontProductCardView card={card} />
          </div>
        ))}
      </div>
    );
  } else if (layout === "featured-plus-rail") {
    const [featured, ...rest] = cards;
    body = (
      <div className="grid grid-cols-1 md:grid-cols-[1.2fr_2fr] gap-4" data-product-layout="featured-plus-rail">
        {featured ? <StorefrontProductCardView card={featured} /> : null}
        <div className="flex gap-3 overflow-x-auto pb-1">
          {rest.map((card) => (
            <div key={`${section.pageSectionId}-${card.productId}`} className="w-[170px] shrink-0 md:w-[200px]">
              <StorefrontProductCardView card={card} />
            </div>
          ))}
        </div>
      </div>
    );
  } else if (layout === "columns") {
    body = (
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4" data-product-layout="columns">
        {cards.map((card) => (
          <StorefrontProductCardView key={`${section.pageSectionId}-${card.productId}`} card={card} />
        ))}
      </div>
    );
  } else {
    body = (
      <div className="flex gap-3 overflow-x-auto pb-1" data-product-layout="rail">
        {cards.map((card) => (
          <div key={`${section.pageSectionId}-${card.productId}`} className="w-[170px] shrink-0 md:w-[220px]">
            <StorefrontProductCardView card={card} />
          </div>
        ))}
      </div>
    );
  }
  return (
    <section className="w-full px-2 py-8 sm:px-4" data-testid="landing-products">
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-lg font-bold">{title}</h2>
        <Link href="/products" className="text-xs font-bold text-primary">همه</Link>
      </div>
      {body}
    </section>
  );
}

export function LandingBrandStrip({
  config,
  brands,
  layout = "logo-rail",
}: {
  config: Record<string, unknown>;
  brands: Parameters<typeof HomeBrandsSection>[0]["brands"];
  layout?: "logo-rail" | "logo-grid" | "featured";
}) {
  const ids = asIds(config, "ids", "brandIds");
  const filtered = ids.length ? brands.filter((item) => ids.includes(item.brandId)) : [];
  return filtered.length ? <HomeBrandsSection brands={filtered} layout={layout} /> : null;
}

export function LandingArticleList({
  config,
  articles,
  layout = "magazine-rail",
}: {
  config: Record<string, unknown>;
  articles: Parameters<typeof HomeArticlesSection>[0]["articles"];
  layout?: "magazine-rail" | "grid" | "featured-plus-list";
}) {
  const take = typeof config.take === "number" ? config.take : 6;
  const slice = articles.slice(0, take);
  return slice.length ? <HomeArticlesSection articles={slice} layout={layout} /> : null;
}

export function LandingReviews({
  reviews,
  layout = "card-carousel",
}: {
  reviews: Parameters<typeof HomeTestimonialsSection>[0]["reviews"];
  layout?: "card-carousel" | "compact-quotes";
}) {
  return reviews.length ? <HomeTestimonialsSection reviews={reviews} layout={layout} /> : null;
}

export function LandingNavigationMenu({
  config,
  menus,
}: {
  config: Record<string, unknown>;
  menus: Record<string, StorefrontMenuItem[]>;
}) {
  const menuId = typeof config.menuId === "string" ? config.menuId : "";
  const items = menuId ? menus[menuId] ?? [] : [];
  return items.length ? <StorefrontMenuLinks items={items} title={titleOf(config, "")} /> : null;
}
