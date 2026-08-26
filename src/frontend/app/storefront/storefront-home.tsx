"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { ChevronLeft, Flame } from "lucide-react";
import { StorefrontProductCardView } from "./storefront-product-card.tsx";
import type {
  StorefrontBestSellerColumn,
  StorefrontBrandItem,
  StorefrontCategoryItem,
  StorefrontProductCard,
} from "./storefront-model.ts";

const SLIDES = [
  { src: "/images/sliders/slider-1.jpg", href: "/offers", alt: "بنر فروشگاهی یک" },
  { src: "/images/sliders/slider-2.jpg", href: "/sale", alt: "بنر فروشگاهی دو" },
  { src: "/images/sliders/slider-3.jpg", href: "/new-products", alt: "بنر فروشگاهی سه" },
  { src: "/images/sliders/slider-4.jpg", href: "/products", alt: "بنر فروشگاهی چهار" },
];

const STORY_IMAGES = [
  "/images/stories/1.jpg",
  "/images/stories/2.jpg",
  "/images/stories/3.jpg",
  "/images/stories/5.jpg",
  "/images/stories/6.jpg",
  "/images/stories/7.jpg",
];

const CATEGORY_IMAGE_INDEXES = [2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16] as const;

const MIDDLE_BANNERS = [
  { src: "/images/middleBanner/1.webp", href: "/offers", title: "پیشنهادهای ویژه" },
  { src: "/images/middleBanner/2.webp", href: "/sale", title: "حراجی" },
  { src: "/images/middleBanner/1.webp", href: "/new-products", title: "تازه‌ها" },
  { src: "/images/middleBanner/2.webp", href: "/brands", title: "برندها" },
];

/**
 * خانهٔ Shopeiva با ترتیب و ریتم منبع؛ دادهٔ تجاری زنده از Host.
 */
export function StorefrontShopeivaHome({
  heroTitle,
  homeCategories,
  specialOffers,
  newArrivals,
  brands,
  bestSellerColumns,
  mostViewedProducts,
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
}) {
  const storyItems = homeCategories.slice(0, 12).map((category, index) => ({
    href: `/products?categoryId=${category.categoryId}`,
    name: category.name,
    src: STORY_IMAGES[index % STORY_IMAGES.length]!,
  }));

  return (
    <div className="py-6 space-y-6 overflow-x-hidden" data-testid="storefront-home">
      <h1 className="sr-only">{heroTitle}</h1>

      <HomeHeroSlider />

      <section aria-label="استوری‌ها" className="w-full px-2 sm:px-4 py-2" data-testid="home-stories">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg md:text-xl font-bold text-gray-900 flex items-center gap-2">
            <span className="w-1 h-5 bg-[#2563EB] rounded-full" />
            استوری‌ها
          </h3>
        </div>
        <div className="flex gap-3 overflow-x-auto pb-4 scrollbar-hide">
          {storyItems.map((story) => (
            <Link key={`${story.href}-${story.name}`} href={story.href} className="shrink-0 w-20 md:w-[100px] text-center">
              <div className="w-20 h-20 md:w-[100px] md:h-[100px] rounded-full p-[3px] bg-gradient-to-tr from-[#2563EB] to-amber-400">
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img src={story.src} alt="" className="w-full h-full rounded-full object-cover bg-white p-0.5" />
              </div>
              <p className="text-[11px] mt-1 text-gray-700 line-clamp-2">{story.name}</p>
            </Link>
          ))}
        </div>
      </section>

      <section aria-labelledby="home-categories-heading" className="w-full px-2 sm:px-4 py-8 md:py-10" data-testid="home-categories">
        <div className="flex items-center justify-between mb-4">
          <h2 id="home-categories-heading" className="text-lg md:text-xl font-bold text-gray-900 flex items-center gap-2">
            <span className="w-1 h-5 bg-[#2563EB] rounded-full" />
            دسته‌بندی‌ها
          </h2>
          <Link href="/products" className="text-xs text-[#2563EB] font-bold flex items-center gap-1">
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
                className="snap-start shrink-0 w-[160px] md:w-[180px] bg-white rounded-2xl border border-gray-100 overflow-hidden hover:shadow-md"
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

      {specialOffers.length > 0 ? (
        <ProductRailSection
          id="home-flash"
          title="پیشنهاد شگفت‌انگیز"
          href="/offers"
          linkLabel="همه"
          tone="accent"
          products={specialOffers}
          slideClassName="w-[170px] md:w-[210px]"
          testId="home-flash-sales"
        />
      ) : null}

      {bestSellerColumns.length > 0 ? (
        <section aria-labelledby="home-best-sellers-heading" className="w-full px-2 sm:px-4 py-8 md:py-10" data-testid="home-best-sellers">
          <div className="flex items-center justify-between mb-4">
            <h2 id="home-best-sellers-heading" className="text-lg md:text-xl font-bold text-gray-900 flex items-center gap-2">
              <span className="w-1 h-5 bg-[#2563EB] rounded-full" />
              پرفروش‌ترین‌ها
            </h2>
            <Link href="/best-seller" className="text-xs text-[#2563EB] font-bold">
              مشاهده
            </Link>
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 md:gap-5 max-h-[600px] overflow-y-auto">
            {bestSellerColumns.map((column) => (
              <div key={`${column.categoryId}-${column.categoryName}`} className="bg-white rounded-2xl border border-gray-100 p-3 space-y-3">
                <p className="text-sm font-black text-gray-900">{column.categoryName}</p>
                {column.products.map((card) => (
                  <Link
                    key={card.productId}
                    href={`/products/${card.slug}`}
                    className="flex gap-3 items-center hover:bg-gray-50 rounded-xl p-1"
                  >
                    <div className="w-14 h-14 rounded-xl bg-gradient-to-br from-[#2563EB] to-[#1e3a8a] text-white text-[10px] font-bold flex items-center justify-center shrink-0">
                      Tooba
                    </div>
                    <div className="min-w-0">
                      <p className="text-xs font-bold text-gray-800 line-clamp-2">{card.title}</p>
                      <p className="text-[11px] text-[#2563EB] font-bold mt-1">
                        {(card.promotionalAmountExclusiveOfTax ?? card.offerAmountExclusiveOfTax).toLocaleString("fa-IR")} {card.currency}
                      </p>
                    </div>
                  </Link>
                ))}
              </div>
            ))}
          </div>
        </section>
      ) : null}

      {mostViewedProducts.length > 0 ? (
        <ProductRailSection
          id="home-most-viewed"
          title="پربازدیدترین‌ها"
          href="/most-viewed"
          linkLabel="همه"
          tone="plain"
          products={mostViewedProducts}
          slideClassName="w-[170px] md:w-[220px]"
          testId="home-most-viewed"
        />
      ) : null}

      <section aria-label="بنرهای میانی" className="w-full px-2 sm:px-4 py-8 md:py-10" data-testid="home-middle-banners">
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

      <section aria-labelledby="home-brands-heading" className="w-full px-2 sm:px-4 py-8 md:py-10" data-testid="home-brands">
        <div className="flex items-center justify-between mb-4">
          <h2 id="home-brands-heading" className="text-lg md:text-xl font-bold text-gray-900 flex items-center gap-2">
            <span className="w-1 h-5 bg-[#2563EB] rounded-full" />
            برندها
          </h2>
          <Link href="/brands" className="text-xs text-[#2563EB] font-bold">
            همه برندها
          </Link>
        </div>
        {brands.length === 0 ? (
          <p className="text-sm text-gray-500 bg-white rounded-2xl border border-gray-100 p-4">برند منتشرشده‌ای در Catalog نیست.</p>
        ) : (
          <div className="flex gap-3 md:gap-4 overflow-x-auto pb-2">
            {brands.map((brand) => (
              <Link
                key={brand.brandId}
                href={`/brand/${brand.slug}`}
                className="shrink-0 w-[160px] md:w-[180px] bg-white rounded-2xl border border-gray-100 aspect-square flex flex-col items-center justify-center p-4 hover:shadow-md"
              >
                <div className="w-16 h-16 rounded-2xl bg-[#2563EB]/10 text-[#2563EB] font-black flex items-center justify-center mb-3">
                  {brand.name.slice(0, 1)}
                </div>
                <p className="text-sm font-bold text-gray-800 text-center line-clamp-2">{brand.name}</p>
                <p className="text-[10px] text-gray-500 mt-1">{brand.productCount.toLocaleString("fa-IR")} کالا</p>
              </Link>
            ))}
          </div>
        )}
      </section>

      <ProductRailSection
        id="home-new-arrivals"
        title="جدیدترین‌ها"
        href="/new-products"
        linkLabel="همه"
        tone="plain"
        products={newArrivals}
        slideClassName="w-[180px] md:w-[220px]"
        testId="home-new-products"
      />
    </div>
  );
}

function HomeHeroSlider() {
  const [index, setIndex] = useState(0);
  useEffect(() => {
    const timer = window.setInterval(() => setIndex((current) => (current + 1) % SLIDES.length), 4500);
    return () => window.clearInterval(timer);
  }, []);
  const slide = SLIDES[index]!;
  return (
    <section aria-label="اسلایدر خانه" className="px-2 sm:px-4" data-testid="home-hero">
      <Link href={slide.href} className="relative block rounded-3xl overflow-hidden shadow-2xl bg-gray-100">
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img
          src={slide.src}
          alt={slide.alt}
          className="w-full h-[190px] sm:h-[230px] md:h-[290px] lg:h-[350px] object-cover transition-opacity"
        />
      </Link>
      <div className="flex justify-center gap-2 mt-3">
        {SLIDES.map((item, slideIndex) => (
          <button
            key={item.src}
            type="button"
            aria-label={`اسلاید ${slideIndex + 1}`}
            className={`h-2 rounded-full transition-all ${slideIndex === index ? "w-6 bg-[#2563EB]" : "w-2 bg-gray-300"}`}
            onClick={() => setIndex(slideIndex)}
          />
        ))}
      </div>
    </section>
  );
}

function ProductRailSection({
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
      <section id={id} aria-labelledby={headingId} className="w-full px-2 sm:px-4" data-testid={testId}>
        <div className="bg-gradient-to-l from-[#2563EB] to-[#1d4ed8] rounded-3xl p-4 md:p-6">
          <div className="flex items-center justify-between mb-4 text-white">
            <h2 id={headingId} className="text-lg md:text-xl font-black flex items-center gap-2">
              <Flame className="w-5 h-5" />
              {title}
            </h2>
            <Link href={href} className="text-xs font-bold bg-white text-[#2563EB] px-3 py-1 rounded-lg">
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
    <section id={id} aria-labelledby={headingId} className="w-full px-2 sm:px-4 py-8 md:py-10" data-testid={testId}>
      <div className="flex items-center justify-between mb-4">
        <h2 id={headingId} className="text-lg md:text-xl font-bold text-gray-900 flex items-center gap-2">
          <span className="w-1 h-5 bg-[#2563EB] rounded-full" />
          {title}
        </h2>
        <Link href={href} className="text-xs text-[#2563EB] font-bold">
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
