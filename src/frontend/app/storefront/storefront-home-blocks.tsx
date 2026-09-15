"use client";

import { LocalizedLink as Link } from "../../lib/i18n/LocalizedLink.tsx";
import { useEffect, useState } from "react";
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
];

export function HomeCategoryGridSection({ homeCategories }: { homeCategories: StorefrontCategoryItem[] }) {
  return (
    <section aria-labelledby="home-categories-heading" className="w-full px-2 sm:px-4 py-8 md:py-10 bg-section-surface" data-testid="home-categories" data-storefront-surface-role="section">
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
      <div className="flex gap-3 md:gap-4 overflow-x-auto pb-2 snap-x">
        {homeCategories.map((category, index) => {
          const imageIndex = CATEGORY_IMAGE_INDEXES[index % CATEGORY_IMAGE_INDEXES.length]!;
          const extension = imageIndex === 10 ? "jpg" : "png";
          return (
            <Link
              key={category.categoryId}
              href={`/products?categoryId=${category.categoryId}`}
              className="snap-start shrink-0 w-[160px] md:w-[180px] bg-surface rounded-2xl border border-gray-100 overflow-hidden hover:shadow-md" data-storefront-surface-role="card"
              data-testid="home-category-card"
            >
              <div className="aspect-square bg-gray-50">
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img
                  src={`/images/categories/${imageIndex}.${extension}`}
                  alt=""
                  className="w-full h-full object-contain p-4"
                />
              </div>
              <p className="text-sm font-bold text-gray-800 text-center px-2 py-3 line-clamp-2">{category.name}</p>
            </Link>
          );
        })}
      </div>
      <p className="sr-only">تعداد ردهٔ ریل خانه: {homeCategories.length}</p>
    </section>
  );
}

export function HomeMiddleBannersSection() {
  return (
    <section aria-label="بنرهای میانی" className="w-full px-2 sm:px-4 py-8 md:py-10 bg-section-alternate" data-testid="home-middle-banners" data-storefront-surface-role="alternate">
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 md:gap-5">
        {MIDDLE_BANNERS.map((banner) => (
          <Link
            key={`${banner.href}-${banner.title}`}
            href={banner.href}
            className="relative group rounded-3xl overflow-hidden aspect-[21/9] sm:aspect-[21/8] md:aspect-[21/7] bg-gray-100"
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

export function HomeHeroSlider({ heightPreset }: { heightPreset?: unknown } = {}) {
  const [index, setIndex] = useState(0);
  useEffect(() => {
    const timer = window.setInterval(() => setIndex((current) => (current + 1) % SLIDES.length), 4500);
    return () => window.clearInterval(timer);
  }, []);
  const slide = SLIDES[index]!;
  const heightClass = heightPresetHeroClass(heightPreset);
  return (
    <section aria-label="اسلایدر خانه" className="px-2 sm:px-4 py-4 md:py-6 bg-section-accent" data-storefront-surface-role="accent">
      <Link href={slide.href} className="relative block rounded-3xl overflow-hidden shadow-2xl bg-gray-100">
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img
          src={slide.src}
          alt={slide.alt}
          className={`w-full ${heightClass} object-cover transition-opacity`}
        />
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
}: {
  id: string;
  title: string;
  href: string;
  linkLabel: string;
  tone: "accent" | "plain";
  products: StorefrontProductCard[];
  slideClassName: string;
  testId: string;
}) {
  const headingId = `${id}-heading`;
  if (products.length === 0) {
    return null;
  }
  if (tone === "accent") {
    return (
      <section id={id} aria-labelledby={headingId} className="w-full px-2 sm:px-4 bg-section-accent" data-testid={testId} data-storefront-surface-role="accent">
        <div className="bg-gradient-to-l from-primary to-primary-strong rounded-3xl p-4 md:p-6">
          <div className="flex items-center justify-between mb-4 text-white">
            <h2 id={headingId} className="text-lg md:text-xl font-black flex items-center gap-2">
              <Flame className="w-5 h-5" />
              {title}
            </h2>
            <Link href={href} className="text-xs font-bold bg-surface text-primary px-3 py-1 rounded-lg">
              {linkLabel}
            </Link>
          </div>
          <div className="flex gap-3 overflow-x-auto pb-1">
            {products.map((card) => (
              <div key={`${id}-${card.productId}`} className={`shrink-0 ${slideClassName}`}>
                <StorefrontProductCardView card={card} />
              </div>
            ))}
          </div>
        </div>
      </section>
    );
  }

  return (
    <section id={id} aria-labelledby={headingId} className="w-full px-2 sm:px-4 py-8 md:py-10 bg-section-surface" data-testid={testId} data-storefront-surface-role="section">
      <div className="flex items-center justify-between mb-4">
        <h2 id={headingId} className="text-lg md:text-xl font-bold text-gray-900 flex items-center gap-2">
          <span className="w-1 h-5 bg-primary rounded-full" />
          {title}
        </h2>
        <Link href={href} className="text-xs text-primary font-bold">
          {linkLabel}
        </Link>
      </div>
      <div className="flex gap-3 md:gap-4 overflow-x-auto pb-1">
        {products.map((card) => (
          <div key={`${id}-${card.productId}`} className={`shrink-0 ${slideClassName}`}>
            <StorefrontProductCardView card={card} />
          </div>
        ))}
      </div>
    </section>
  );
}

export function CompositionBannerGrid({
  layout,
  title,
  href,
  heightPreset,
  testId,
}: {
  layout: "single" | "two-equal" | "three" | "four-grid" | "one-large-two-small" | "mosaic-2x2";
  title?: string;
  href?: string;
  heightPreset?: unknown;
  testId?: string;
}) {
  const banners = MIDDLE_BANNERS;
  const link = href ?? "/offers";
  if (layout === "single") {
    const banner = banners[0]!;
    return (
      <section className="w-full px-2 sm:px-4 py-6" data-testid={testId ?? "composition-banner-single"}>
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
      <section className="w-full px-2 sm:px-4 py-6" data-testid={testId ?? "composition-banner-two"}>
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
  if (layout === "three") {
    return (
      <section className="w-full px-2 sm:px-4 py-6" data-testid={testId ?? "composition-banner-three"}>
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
      <section className="w-full px-2 sm:px-4 py-6" data-testid={testId ?? "composition-banner-four"}>
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
  return <HomeMiddleBannersSection />;
}
