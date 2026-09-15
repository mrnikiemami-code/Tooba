"use client";

import { LocalizedLink as Link } from "../../lib/i18n/LocalizedLink.tsx";
import { useEffect, useState, type ReactNode } from "react";
import { ChevronLeft, Flame } from "lucide-react";
import { StorefrontProductCardView } from "./storefront-product-card.tsx";
import type { StorefrontCategoryItem, StorefrontProductCard } from "./storefront-model.ts";
import { heightPresetHeroClass } from "../../lib/storefront-composition/size-presets.ts";

const SLIDES = [
  { src: "/images/sliders/slider-1.jpg", href: "/offers", alt: "بنر فروشگاهی یک" },
  { src: "/images/sliders/slider-2.jpg", href: "/sale", alt: "بنر فروشگاهی دو" },
  { src: "/images/sliders/slider-3.jpg", href: "/new-products", alt: "بنر فروشگاهی سه" },
  { src: "/images/sliders/slider-4.jpg", href: "/products", alt: "بنر فروشگاهی چهار" },
];

const CATEGORY_IMAGE_INDEXES = [2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16] as const;

const MIDDLE_BANNERS = [
  { src: "/images/middleBanner/1.webp", href: "/offers", title: "پیشنهادهای ویژه" },
  { src: "/images/middleBanner/2.webp", href: "/sale", title: "حراجی" },
  { src: "/images/middleBanner/1.webp", href: "/new-products", title: "تازه‌ها" },
  { src: "/images/middleBanner/2.webp", href: "/brands", title: "برندها" },
  { src: "/images/middleBanner/1.webp", href: "/products", title: "کالاها" },
  { src: "/images/middleBanner/2.webp", href: "/offers", title: "پیشنهاد" },
  { src: "/images/middleBanner/1.webp", href: "/sale", title: "تخفیف" },
  { src: "/images/middleBanner/2.webp", href: "/new-products", title: "جدید" },
];

export type CategoryLayout = "image-cards" | "compact-tiles" | "horizontal-rail";
export type HeroLayout = "full-width" | "contained" | "split" | "side-promos";
export type BannerLayout =
  | "single"
  | "two-equal"
  | "two-asymmetric"
  | "three"
  | "four-grid"
  | "one-large-two-small"
  | "one-large-four-small"
  | "eight-compact"
  | "mosaic-2x2";

export function HomeCategoryGridSection({
  homeCategories,
  layout = "image-cards",
}: {
  homeCategories: StorefrontCategoryItem[];
  layout?: CategoryLayout;
}) {
  const heading = (
    <div className="flex items-center justify-between mb-4">
      <h2 id="home-categories-heading" className="text-lg md:text-xl font-bold text-gray-900 flex items-center gap-2">
        <span className="w-1 h-5 bg-primary rounded-full" />
        دسته‌بندی‌ها
      </h2>
      <Link href="/products" className="text-xs text-primary font-bold flex items-center gap-1">
        همه
        <ChevronLeft className="w-3.5 h-3.5" />
      </Link>
    </div>
  );

  const card = (category: StorefrontCategoryItem, index: number, className: string) => {
    const imageIndex = CATEGORY_IMAGE_INDEXES[index % CATEGORY_IMAGE_INDEXES.length]!;
    const extension = imageIndex === 10 ? "jpg" : "png";
    return (
      <Link
        key={category.categoryId}
        href={`/products?categoryId=${category.categoryId}`}
        className={className}
        data-testid="home-category-card"
      >
        <div className="aspect-square bg-gray-50">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img src={`/images/categories/${imageIndex}.${extension}`} alt="" className="w-full h-full object-contain p-4" />
        </div>
        <p className="text-sm font-bold text-gray-800 text-center px-2 py-3 line-clamp-2">{category.name}</p>
      </Link>
    );
  };

  if (layout === "compact-tiles") {
    return (
      <section
        aria-labelledby="home-categories-heading"
        className="w-full px-2 sm:px-4 py-8 md:py-10 bg-section-surface"
        data-testid="home-categories"
        data-category-layout="compact-tiles"
        data-storefront-surface-role="section"
      >
        {heading}
        <div className="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-6 gap-2 md:gap-3">
          {homeCategories.map((category, index) =>
            card(category, index, "bg-surface rounded-xl border border-gray-100 overflow-hidden hover:shadow-md"),
          )}
        </div>
      </section>
    );
  }

  if (layout === "horizontal-rail") {
    return (
      <section
        aria-labelledby="home-categories-heading"
        className="w-full px-2 sm:px-4 py-8 md:py-10 bg-section-surface"
        data-testid="home-categories"
        data-category-layout="horizontal-rail"
        data-storefront-surface-role="section"
      >
        {heading}
        <div className="flex gap-2 md:gap-3 overflow-x-auto pb-2 snap-x">
          {homeCategories.map((category, index) =>
            card(
              category,
              index,
              "snap-start shrink-0 w-[110px] md:w-[130px] bg-surface rounded-full border border-gray-100 overflow-hidden hover:shadow-md",
            ),
          )}
        </div>
      </section>
    );
  }

  return (
    <section
      aria-labelledby="home-categories-heading"
      className="w-full px-2 sm:px-4 py-8 md:py-10 bg-section-surface"
      data-testid="home-categories"
      data-category-layout="image-cards"
      data-storefront-surface-role="section"
    >
      {heading}
      <div className="flex gap-3 md:gap-4 overflow-x-auto pb-2 snap-x">
        {homeCategories.map((category, index) =>
          card(
            category,
            index,
            "snap-start shrink-0 w-[160px] md:w-[180px] bg-surface rounded-2xl border border-gray-100 overflow-hidden hover:shadow-md",
          ),
        )}
      </div>
      <p className="sr-only">تعداد ردهٔ ریل خانه: {homeCategories.length}</p>
    </section>
  );
}

export function HomeMiddleBannersSection() {
  return (
    <section aria-label="بنرهای میانی" className="w-full px-2 sm:px-4 py-8 md:py-10 bg-section-alternate" data-testid="home-middle-banners" data-storefront-surface-role="alternate">
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 md:gap-5 md:grid-rows-2 md:[grid-template-areas:'large_s1''large_s2']">
        {MIDDLE_BANNERS.slice(0, 3).map((banner, index) => (
          <Link
            key={`${banner.href}-${banner.title}`}
            href={banner.href}
            className={`relative group rounded-3xl overflow-hidden bg-gray-100 ${index === 0 ? "sm:row-span-2 aspect-[21/9] sm:aspect-auto sm:min-h-full" : "aspect-[21/9] sm:aspect-[21/8]"}`}
          >
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img src={banner.src} alt="" className="absolute inset-0 w-full h-full object-cover" />
            <div className="absolute inset-0 bg-black/0 group-hover:bg-black/35 transition-colors" />
            <span className="absolute bottom-4 right-4 text-white text-sm font-bold opacity-0 group-hover:opacity-100 transition-opacity">
              {banner.title}
            </span>
          </Link>
        ))}
      </div>
    </section>
  );
}

export function HomeHeroSlider({
  heightPreset,
  layout = "full-width",
  title,
  subtitle,
  href,
}: {
  heightPreset?: unknown;
  layout?: HeroLayout;
  title?: string;
  subtitle?: string;
  href?: string;
} = {}) {
  const [index, setIndex] = useState(0);
  useEffect(() => {
    const timer = window.setInterval(() => setIndex((current) => (current + 1) % SLIDES.length), 4500);
    return () => window.clearInterval(timer);
  }, []);
  const slide = SLIDES[index]!;
  const heightClass = heightPresetHeroClass(heightPreset);
  const link = href ?? slide.href;

  if (layout === "split") {
    return (
      <section aria-label="اسلایدر خانه" className="px-2 sm:px-4 py-4 md:py-6 bg-section-accent" data-hero-layout="split" data-storefront-surface-role="accent">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 rounded-3xl overflow-hidden bg-surface shadow-xl border border-gray-100">
          <Link href={link} className="relative block bg-gray-100 min-h-[180px]">
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img src={slide.src} alt={slide.alt} className={`w-full h-full object-cover ${heightClass}`} />
          </Link>
          <div className="flex flex-col justify-center p-6 md:p-8">
            <h2 className="text-2xl md:text-3xl font-black text-gray-900">{title ?? "فروشگاه توبا"}</h2>
            {subtitle ? <p className="mt-2 text-sm text-gray-600">{subtitle}</p> : <p className="mt-2 text-sm text-gray-600">{slide.alt}</p>}
            <Link href={link} className="mt-4 inline-flex w-fit rounded-xl bg-primary px-4 py-2 text-sm font-bold text-white">مشاهده</Link>
          </div>
        </div>
      </section>
    );
  }

  if (layout === "side-promos") {
    return (
      <section aria-label="اسلایدر خانه" className="px-2 sm:px-4 py-4 md:py-6 bg-section-accent" data-hero-layout="side-promos" data-storefront-surface-role="accent">
        <div className="grid grid-cols-1 md:grid-cols-[2fr_1fr] gap-3">
          <Link href={link} className="relative block rounded-3xl overflow-hidden shadow-2xl bg-gray-100">
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img src={slide.src} alt={slide.alt} className={`w-full ${heightClass} object-cover`} />
          </Link>
          <div className="grid grid-cols-2 md:grid-cols-1 gap-3">
            {SLIDES.slice(1, 3).map((promo) => (
              <Link key={promo.src} href={promo.href} className="relative block rounded-2xl overflow-hidden bg-gray-100 aspect-[16/9] md:aspect-auto md:min-h-[120px]">
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img src={promo.src} alt={promo.alt} className="absolute inset-0 h-full w-full object-cover" />
              </Link>
            ))}
          </div>
        </div>
      </section>
    );
  }

  const shell = layout === "contained" ? "max-w-6xl mx-auto" : "w-full";
  return (
    <section
      aria-label="اسلایدر خانه"
      className={`px-2 sm:px-4 py-4 md:py-6 bg-section-accent ${shell}`}
      data-hero-layout={layout}
      data-storefront-surface-role="accent"
    >
      <Link href={link} className={`relative block overflow-hidden shadow-2xl bg-gray-100 ${layout === "contained" ? "rounded-3xl" : "rounded-none md:rounded-3xl"}`}>
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img src={slide.src} alt={slide.alt} className={`w-full ${heightClass} object-cover transition-opacity`} />
      </Link>
      <div className="flex justify-center gap-2 mt-3">
        {SLIDES.map((item, slideIndex) => (
          <button
            key={item.src}
            type="button"
            aria-label={`اسلاید ${slideIndex + 1}`}
            className={`h-2 rounded-full transition-all ${slideIndex === index ? "w-6 bg-primary" : "w-2 bg-gray-300"}`}
            onClick={() => setIndex(slideIndex)}
          />
        ))}
      </div>
    </section>
  );
}

export function ProductRailSection({
  id,
  title,
  href,
  linkLabel,
  tone,
  products,
  slideClassName,
  testId,
  layout = "rail",
}: {
  id: string;
  title: string;
  href: string;
  linkLabel: string;
  tone: "accent" | "plain";
  products: StorefrontProductCard[];
  slideClassName: string;
  testId: string;
  layout?: "rail" | "grid" | "compact-rows" | "featured-plus-rail" | "ranked-grid";
}) {
  const headingId = `${id}-heading`;
  if (products.length === 0) {
    return null;
  }

  const header = (
    <div className="flex items-center justify-between mb-4">
      <h2 id={headingId} className={`text-lg md:text-xl font-bold flex items-center gap-2 ${tone === "accent" ? "text-white" : "text-gray-900"}`}>
        {tone === "accent" ? <Flame className="w-5 h-5" /> : <span className="w-1 h-5 bg-primary rounded-full" />}
        {title}
      </h2>
      <Link href={href} className={tone === "accent" ? "text-xs font-bold bg-surface text-primary px-3 py-1 rounded-lg" : "text-xs text-primary font-bold"}>
        {linkLabel}
      </Link>
    </div>
  );

  let body: ReactNode;
  if (layout === "grid" || layout === "ranked-grid") {
    body = (
      <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-3 md:gap-4" data-product-layout={layout}>
        {products.map((card) => (
          <StorefrontProductCardView key={`${id}-${card.productId}`} card={card} />
        ))}
      </div>
    );
  } else if (layout === "compact-rows") {
    body = (
      <div className="space-y-2" data-product-layout="compact-rows">
        {products.map((card, index) => (
          <div key={`${id}-${card.productId}`} className="flex items-center gap-3 rounded-xl border border-gray-100 bg-surface p-2">
            <span className="w-6 text-center text-xs font-bold text-muted">{index + 1}</span>
            <div className="min-w-0 flex-1">
              <StorefrontProductCardView card={card} />
            </div>
          </div>
        ))}
      </div>
    );
  } else if (layout === "featured-plus-rail") {
    const [featured, ...rest] = products;
    body = (
      <div className="grid grid-cols-1 md:grid-cols-[1.2fr_2fr] gap-4" data-product-layout="featured-plus-rail">
        {featured ? (
          <div className="md:min-h-full">
            <StorefrontProductCardView card={featured} />
          </div>
        ) : null}
        <div className="flex gap-3 overflow-x-auto pb-1">
          {rest.map((card) => (
            <div key={`${id}-${card.productId}`} className={`shrink-0 ${slideClassName}`}>
              <StorefrontProductCardView card={card} />
            </div>
          ))}
        </div>
      </div>
    );
  } else {
    body = (
      <div className="flex gap-3 md:gap-4 overflow-x-auto pb-1" data-product-layout="rail">
        {products.map((card) => (
          <div key={`${id}-${card.productId}`} className={`shrink-0 ${slideClassName}`}>
            <StorefrontProductCardView card={card} />
          </div>
        ))}
      </div>
    );
  }

  if (tone === "accent") {
    return (
      <section id={id} aria-labelledby={headingId} className="w-full px-2 sm:px-4 bg-section-accent" data-testid={testId} data-storefront-surface-role="accent">
        <div className="bg-gradient-to-l from-primary to-primary-strong rounded-3xl p-4 md:p-6">
          {header}
          {body}
        </div>
      </section>
    );
  }

  return (
    <section id={id} aria-labelledby={headingId} className="w-full px-2 sm:px-4 py-8 md:py-10 bg-section-surface" data-testid={testId} data-storefront-surface-role="section">
      {header}
      {body}
    </section>
  );
}

export function CompositionBannerGrid({
  layout,
  title,
  href,
  heightPreset,
  testId,
  items,
}: {
  layout: BannerLayout;
  title?: string;
  href?: string;
  heightPreset?: unknown;
  testId?: string;
  items?: Array<{ src?: string; href?: string; title?: string }>;
}) {
  const banners = (items && items.length > 0
    ? items.map((item, index) => ({
      src: item.src || MIDDLE_BANNERS[index % MIDDLE_BANNERS.length]!.src,
      href: item.href || "/offers",
      title: item.title || MIDDLE_BANNERS[index % MIDDLE_BANNERS.length]!.title,
    }))
    : MIDDLE_BANNERS);
  const link = href ?? "/offers";

  if (layout === "single") {
    const banner = banners[0]!;
    return (
      <section className="w-full px-2 sm:px-4 py-6" data-testid={testId ?? "composition-banner-single"} data-banner-layout="single">
        <Link href={link} className="relative block overflow-hidden rounded-3xl bg-gray-100">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img src={banner.src} alt="" className={`w-full object-cover ${heightPresetHeroClass(heightPreset)}`} />
          <span className="absolute bottom-4 right-4 text-sm font-bold text-white">{title ?? banner.title}</span>
        </Link>
      </section>
    );
  }

  if (layout === "two-equal") {
    return (
      <section className="w-full px-2 sm:px-4 py-6" data-testid={testId ?? "composition-banner-two"} data-banner-layout="two-equal">
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          {banners.slice(0, 2).map((banner) => (
            <Link key={banner.title} href={banner.href} className="relative overflow-hidden rounded-3xl aspect-[21/9] bg-gray-100">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={banner.src} alt="" className="absolute inset-0 h-full w-full object-cover" />
              <span className="absolute bottom-3 right-3 text-xs font-bold text-white">{banner.title}</span>
            </Link>
          ))}
        </div>
      </section>
    );
  }

  if (layout === "two-asymmetric") {
    return (
      <section className="w-full px-2 sm:px-4 py-6" data-testid={testId ?? "composition-banner-two-asymmetric"} data-banner-layout="two-asymmetric">
        <div className="grid grid-cols-1 sm:grid-cols-[2fr_1fr] gap-4">
          {banners.slice(0, 2).map((banner, index) => (
            <Link key={banner.title} href={banner.href} className={`relative overflow-hidden rounded-3xl bg-gray-100 ${index === 0 ? "aspect-[21/9]" : "aspect-[4/3] sm:aspect-auto"}`}>
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={banner.src} alt="" className="absolute inset-0 h-full w-full object-cover" />
            </Link>
          ))}
        </div>
      </section>
    );
  }

  if (layout === "three") {
    return (
      <section className="w-full px-2 sm:px-4 py-6" data-testid={testId ?? "composition-banner-three"} data-banner-layout="three">
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
          {banners.slice(0, 3).map((banner) => (
            <Link key={banner.title} href={banner.href} className="relative overflow-hidden rounded-2xl aspect-[16/9] bg-gray-100">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={banner.src} alt="" className="absolute inset-0 h-full w-full object-cover" />
            </Link>
          ))}
        </div>
      </section>
    );
  }

  if (layout === "four-grid" || layout === "mosaic-2x2") {
    return (
      <section className="w-full px-2 sm:px-4 py-6" data-testid={testId ?? "composition-banner-four"} data-banner-layout={layout}>
        <div className="grid grid-cols-2 gap-3 md:gap-4">
          {banners.slice(0, 4).map((banner) => (
            <Link key={`${banner.href}-${banner.title}`} href={banner.href} className="relative overflow-hidden rounded-2xl aspect-[21/10] bg-gray-100">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={banner.src} alt="" className="absolute inset-0 h-full w-full object-cover" />
            </Link>
          ))}
        </div>
      </section>
    );
  }

  if (layout === "one-large-two-small") {
    return (
      <section className="w-full px-2 sm:px-4 py-6" data-testid={testId ?? "composition-banner-mosaic-3"} data-banner-layout="one-large-two-small">
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 md:gap-4">
          {banners.slice(0, 3).map((banner, index) => (
            <Link
              key={`${banner.href}-${banner.title}-${index}`}
              href={banner.href}
              className={`relative overflow-hidden rounded-2xl bg-gray-100 ${index === 0 ? "sm:row-span-2 aspect-[16/10] sm:aspect-auto sm:min-h-full" : "aspect-[21/9]"}`}
            >
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={banner.src} alt="" className="absolute inset-0 h-full w-full object-cover" />
            </Link>
          ))}
        </div>
      </section>
    );
  }

  if (layout === "one-large-four-small") {
    return (
      <section className="w-full px-2 sm:px-4 py-6" data-testid={testId ?? "composition-banner-mosaic-5"} data-banner-layout="one-large-four-small">
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
          {banners.slice(0, 5).map((banner, index) => (
            <Link
              key={`${banner.href}-${banner.title}-${index}`}
              href={banner.href}
              className={`relative overflow-hidden rounded-2xl bg-gray-100 aspect-[16/10] ${index === 0 ? "col-span-2 row-span-2 md:aspect-auto md:min-h-[220px]" : ""}`}
            >
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={banner.src} alt="" className="absolute inset-0 h-full w-full object-cover" />
            </Link>
          ))}
        </div>
      </section>
    );
  }

  if (layout === "eight-compact") {
    return (
      <section className="w-full px-2 sm:px-4 py-6" data-testid={testId ?? "composition-banner-eight"} data-banner-layout="eight-compact">
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-2">
          {banners.slice(0, 8).map((banner, index) => (
            <Link key={`${banner.href}-${banner.title}-${index}`} href={banner.href} className="relative overflow-hidden rounded-xl aspect-[16/9] bg-gray-100">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={banner.src} alt="" className="absolute inset-0 h-full w-full object-cover" />
            </Link>
          ))}
        </div>
      </section>
    );
  }

  return <HomeMiddleBannersSection />;
}
