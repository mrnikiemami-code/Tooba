"use client";

import { LocalizedLink as Link } from "../../lib/i18n/LocalizedLink.tsx";
import { StorefrontProductCardView } from "./storefront-product-card.tsx";
import {
  HomeArticlesSection,
  HomeBrandsSection,
  HomeTestimonialsSection,
} from "./storefront-home-repair-sections.tsx";
import type { StorefrontCategoryItem, StorefrontProductCard } from "./storefront-model.ts";
import type {
  LandingRenderContext,
  StorefrontLandingPage,
  StorefrontLandingSection,
} from "./storefront-landing-api.ts";

export type { LandingRenderContext };

function parseConfig(raw: string): Record<string, unknown> {
  try {
    const value = JSON.parse(raw) as unknown;
    return value && typeof value === "object" && !Array.isArray(value) ? value as Record<string, unknown> : {};
  } catch {
    return {};
  }
}

function asIds(config: Record<string, unknown>, ...keys: string[]): string[] {
  for (const key of keys) {
    const value = config[key];
    if (Array.isArray(value)) return value.map((item) => String(item));
  }
  return [];
}

function titleOf(config: Record<string, unknown>, fallback: string): string {
  return typeof config.title === "string" && config.title.trim() ? config.title.trim() : fallback;
}

export function StorefrontLandingSections({
  page,
  context,
  preview = false,
}: {
  page: StorefrontLandingPage;
  context: LandingRenderContext;
  preview?: boolean;
}) {
  return (
    <div className="space-y-6 py-6 overflow-x-hidden" data-testid="storefront-landing-page" data-landing-slug={page.slug} data-landing-preview={preview ? "1" : "0"}>
      <h1 className="sr-only">{page.title}</h1>
      {page.sections.map((section) => {
        const rendered = renderLandingSection(section, context);
        return rendered ? <div key={section.pageSectionId}>{rendered}</div> : null;
      })}
    </div>
  );
}

function renderLandingSection(section: StorefrontLandingSection, context: LandingRenderContext) {
  const config = parseConfig(section.config);
  switch (section.sectionType) {
    case "Hero":
      return <LandingHero config={config} />;
    case "ProductCollection":
      return <LandingProductRail section={section} config={config} products={context.products} />;
    case "CategoryGrid":
      return <LandingCategoryGrid config={config} categories={context.categories} />;
    case "BrandStrip": {
      const ids = asIds(config, "ids", "brandIds");
      const brands = ids.length ? context.brands.filter((item) => ids.includes(item.brandId)) : [];
      return brands.length ? <HomeBrandsSection brands={brands} /> : null;
    }
    case "PromoBanner":
      return <LandingPromo config={config} />;
    case "ArticleList": {
      const take = typeof config.take === "number" ? config.take : 6;
      const articles = context.articles.slice(0, take);
      return articles.length ? <HomeArticlesSection articles={articles} /> : null;
    }
    case "Reviews":
      return context.reviews.length ? <HomeTestimonialsSection reviews={context.reviews} /> : null;
    case "RichText":
      return <LandingRichText config={config} />;
    default:
      return null;
  }
}

function LandingHero({ config }: { config: Record<string, unknown> }) {
  const title = titleOf(config, "فروشگاه توبا");
  const subtitle = typeof config.subtitle === "string" ? config.subtitle : "";
  const href = typeof config.href === "string" && config.href.trim() ? config.href : "/products";
  return (
    <section className="px-2 sm:px-4" data-testid="landing-hero">
      <Link href={href} className="relative block overflow-hidden rounded-3xl bg-gradient-to-l from-primary to-primary-strong shadow-2xl">
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img src="/images/sliders/slider-1.jpg" alt="" className="h-[190px] w-full object-cover sm:h-[230px] md:h-[290px] lg:h-[350px]" />
        <div className="absolute inset-0 bg-black/35" />
        <div className="absolute inset-0 flex flex-col justify-end p-6 text-white">
          <h2 className="text-2xl font-black md:text-4xl">{title}</h2>
          {subtitle ? <p className="mt-2 max-w-xl text-sm md:text-base">{subtitle}</p> : null}
        </div>
      </Link>
    </section>
  );
}

function LandingPromo({ config }: { config: Record<string, unknown> }) {
  const title = titleOf(config, "پیشنهاد ویژه");
  const href = typeof config.href === "string" && config.href.trim() ? config.href : "/offers";
  return (
    <section className="px-2 sm:px-4" data-testid="landing-promo">
      <Link href={href} className="relative block overflow-hidden rounded-3xl bg-gray-100">
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img src="/images/middleBanner/1.webp" alt="" className="h-40 w-full object-cover md:h-52" />
        <span className="absolute bottom-4 right-4 text-sm font-bold text-white">{title}</span>
      </Link>
    </section>
  );
}

function LandingRichText({ config }: { config: Record<string, unknown> }) {
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

function LandingCategoryGrid({
  config,
  categories,
}: {
  config: Record<string, unknown>;
  categories: StorefrontCategoryItem[];
}) {
  const ids = asIds(config, "ids", "categoryIds");
  const items = ids.length ? categories.filter((item) => ids.includes(item.categoryId)) : [];
  if (items.length === 0) return null;
  const images = [2, 3, 4, 5, 6, 7, 8, 9] as const;
  return (
    <section className="w-full px-2 py-8 sm:px-4" data-testid="landing-categories">
      <h2 className="mb-4 text-lg font-bold">{titleOf(config, "دسته‌بندی‌ها")}</h2>
      <div className="flex gap-3 overflow-x-auto pb-2">
        {items.map((category, index) => {
          const imageIndex = images[index % images.length]!;
          return (
            <Link
              key={category.categoryId}
              href={`/products?categoryId=${category.categoryId}`}
              className="w-[160px] shrink-0 overflow-hidden rounded-2xl border border-gray-100 bg-white"
            >
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

function LandingProductRail({
  section,
  config,
  products,
}: {
  section: StorefrontLandingSection;
  config: Record<string, unknown>;
  products: StorefrontProductCard[];
}) {
  const wanted = new Set(section.items.map((item) => item.id).concat(section.items.map((item) => item.slug).filter(Boolean) as string[]));
  const cards = products.filter((card) => wanted.has(card.productId) || wanted.has(card.slug)).slice(0, typeof config.take === "number" ? config.take : 8);
  if (cards.length === 0) return null;
  const title = titleOf(config, "کالاها");
  return (
    <section className="w-full px-2 py-8 sm:px-4" data-testid="landing-products">
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-lg font-bold">{title}</h2>
        <Link href="/products" className="text-xs font-bold text-primary">همه</Link>
      </div>
      <div className="flex gap-3 overflow-x-auto pb-1">
        {cards.map((card) => (
          <div key={`${section.pageSectionId}-${card.productId}`} className="w-[170px] shrink-0 md:w-[220px]">
            <StorefrontProductCardView card={card} />
          </div>
        ))}
      </div>
    </section>
  );
}
