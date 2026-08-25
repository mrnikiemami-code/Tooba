import Link from "next/link";
import { StorefrontProductCardView } from "./storefront-product-card.tsx";
import type { StorefrontBrandItem, StorefrontCategoryItem, StorefrontProductCard } from "./storefront-model.ts";

const SLIDES = [
  { src: "/images/sliders/slider-1.jpg", alt: "بنر فروشگاهی یک" },
  { src: "/images/sliders/slider-2.jpg", alt: "بنر فروشگاهی دو" },
  { src: "/images/sliders/slider-3.jpg", alt: "بنر فروشگاهی سه" },
  { src: "/images/sliders/slider-4.jpg", alt: "بنر فروشگاهی چهار" },
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

/**
 * خانهٔ Shopeiva با اسلایدر/بنر قالب و ردیف‌های کالای زنده Catalog/Offer.
 */
export function StorefrontShopeivaHome({
  heroTitle,
  heroSubtitle,
  categories,
  specialOffers,
  campaignProducts,
  newArrivals,
  productRail,
  brands,
}: {
  heroTitle: string;
  heroSubtitle: string;
  categories: StorefrontCategoryItem[];
  specialOffers: StorefrontProductCard[];
  campaignProducts: StorefrontProductCard[];
  newArrivals: StorefrontProductCard[];
  productRail: StorefrontProductCard[];
  brands: StorefrontBrandItem[];
}) {
  const storyItems =
    categories.length > 0
      ? categories.slice(0, 8).map((category, index) => ({
          href: `/products?categoryId=${category.categoryId}`,
          name: category.name,
          src: STORY_IMAGES[index % STORY_IMAGES.length]!,
        }))
      : STORY_IMAGES.map((src, index) => ({
          href: "/products",
          name: `ویترین ${index + 1}`,
          src,
        }));

  return (
    <div className="py-4 md:py-6 space-y-6 md:space-y-8 overflow-x-hidden">
      <h1 className="sr-only">{heroTitle}</h1>

      <section aria-label="اسلایدر خانه">
        <div className="relative rounded-3xl overflow-hidden shadow-2xl bg-gray-100">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img src={SLIDES[0]!.src} alt={SLIDES[0]!.alt} className="w-full h-[220px] sm:h-[320px] lg:h-[420px] object-cover" />
          <div className="absolute inset-0 bg-gradient-to-l from-black/45 to-transparent" />
          <div className="absolute bottom-6 right-6 left-6 text-white max-w-xl">
            <p className="text-lg md:text-3xl font-black">{heroTitle}</p>
            <p className="text-sm md:text-base mt-2 opacity-90">{heroSubtitle}</p>
          </div>
        </div>
        <div className="hidden md:grid grid-cols-3 gap-3 mt-3">
          {SLIDES.slice(1).map((slide) => (
            // eslint-disable-next-line @next/next/no-img-element
            <img key={slide.src} src={slide.src} alt={slide.alt} className="h-28 w-full object-cover rounded-2xl" />
          ))}
        </div>
      </section>

      <section aria-label="استوری رده‌ها" className="flex gap-4 overflow-x-auto pb-2">
        {storyItems.map((story) => (
          <Link key={`${story.href}-${story.name}`} href={story.href} className="shrink-0 w-20 text-center">
            <div className="w-20 h-20 rounded-full p-[3px] bg-gradient-to-tr from-[#2563EB] to-amber-400">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={story.src} alt="" className="w-full h-full rounded-full object-cover bg-white p-0.5" />
            </div>
            <p className="text-[11px] mt-1 text-gray-700 line-clamp-2">{story.name}</p>
          </Link>
        ))}
      </section>

      <section aria-labelledby="home-categories-heading">
        <div className="flex items-center justify-between mb-4">
          <h2 id="home-categories-heading" className="text-lg md:text-xl font-black">
            دسته‌بندی‌ها
          </h2>
          <Link href="/products" className="text-xs text-[#2563EB] font-bold">
            همه
          </Link>
        </div>
        <div className="grid grid-cols-3 sm:grid-cols-4 lg:grid-cols-8 gap-3">
          {categories.map((category, index) => {
            const imageIndex = CATEGORY_IMAGE_INDEXES[index % CATEGORY_IMAGE_INDEXES.length]!;
            const extension = imageIndex === 10 ? "jpg" : "png";
            return (
              <Link
                key={category.categoryId}
                href={`/products?categoryId=${category.categoryId}`}
                className="bg-white rounded-2xl border border-gray-100 p-3 text-center hover:shadow-md"
              >
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img
                  src={`/images/categories/${imageIndex}.${extension}`}
                  alt=""
                  className="w-14 h-14 mx-auto object-contain mb-2"
                />
                <span className="text-[11px] font-bold text-gray-700 line-clamp-2">{category.name}</span>
              </Link>
            );
          })}
        </div>
      </section>

      {specialOffers.length > 0 ? (
        <ProductSection
          id="home-special-offers"
          title="پیشنهادهای ویژه"
          href="/products"
          linkLabel="همه کالاها"
          tone="accent"
          products={specialOffers}
          empty=""
        />
      ) : null}

      {campaignProducts.length > 0 ? (
        <ProductSection
          id="home-sale"
          title="فروش ویژه"
          href="/products"
          linkLabel="مشاهده"
          tone="plain"
          products={campaignProducts}
          empty=""
        />
      ) : null}

      <section className="grid grid-cols-1 md:grid-cols-2 gap-4" aria-label="بنرهای میانی">
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img src="/images/middleBanner/1.webp" alt="" className="w-full h-40 md:h-56 object-cover rounded-3xl" />
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img src="/images/middleBanner/2.webp" alt="" className="w-full h-40 md:h-56 object-cover rounded-3xl" />
      </section>

      <ProductSection
        id="home-new-arrivals"
        title="تازه‌های ویترین"
        href="/products"
        linkLabel="همه"
        tone="plain"
        products={newArrivals}
        empty="تازه‌ای برای نمایش نیست."
      />

      <ProductSection
        id="home-product-rail"
        title="پیشنهادهای بیشتر"
        href="/products"
        linkLabel="ویترین"
        tone="plain"
        products={productRail}
        empty="ردیفی برای نمایش نیست."
      />

      <section aria-labelledby="home-brands-heading">
        <div className="flex items-center justify-between mb-4">
          <h2 id="home-brands-heading" className="text-lg md:text-xl font-black">
            برندها
          </h2>
          <Link href="/products" className="text-xs text-[#2563EB] font-bold">
            همه کالاها
          </Link>
        </div>
        {brands.length === 0 ? (
          <p className="text-sm text-gray-500 bg-white rounded-2xl border border-gray-100 p-4">برند منتشرشده‌ای در Catalog نیست.</p>
        ) : (
          <div className="flex gap-3 overflow-x-auto pb-2">
            {brands.map((brand) => (
              <div
                key={brand.brandId}
                className="shrink-0 min-w-[120px] bg-white rounded-2xl border border-gray-100 px-4 py-5 text-center"
              >
                <p className="text-sm font-bold text-gray-800 line-clamp-2">{brand.name}</p>
              </div>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}

/**
 * ردیف کارت محصول با عنوان معنایی و پوستهٔ Shopeiva.
 */
function ProductSection({
  id,
  title,
  href,
  linkLabel,
  tone,
  products,
  empty,
  note,
}: {
  id: string;
  title: string;
  href: string;
  linkLabel: string;
  tone: "accent" | "plain";
  products: StorefrontProductCard[];
  empty: string;
  note?: string;
}) {
  const headingId = `${id}-heading`;
  if (tone === "accent") {
    return (
      <section id={id} aria-labelledby={headingId} className="bg-gradient-to-l from-[#2563EB] to-[#1d4ed8] rounded-3xl p-4 md:p-6">
        <div className="flex items-center justify-between mb-4 text-white">
          <h2 id={headingId} className="text-lg md:text-xl font-black">
            {title}
          </h2>
          <Link href={href} className="text-xs font-bold bg-white text-[#2563EB] px-3 py-1 rounded-lg">
            {linkLabel}
          </Link>
        </div>
        {note ? <p className="text-white/80 text-[11px] mb-3">{note}</p> : null}
        {products.length === 0 ? (
          <p className="text-white/90 text-sm">{empty}</p>
        ) : (
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-3">
            {products.map((card) => (
              <StorefrontProductCardView key={`${id}-${card.productId}`} card={card} />
            ))}
          </div>
        )}
      </section>
    );
  }

  return (
    <section id={id} aria-labelledby={headingId}>
      <div className="flex items-center justify-between mb-4">
        <h2 id={headingId} className="text-lg md:text-xl font-black">
          {title}
        </h2>
        <Link href={href} className="text-xs text-[#2563EB] font-bold">
          {linkLabel}
        </Link>
      </div>
      {note ? <p className="text-[11px] text-gray-500 mb-3">{note}</p> : null}
      {products.length === 0 ? (
        <p className="text-sm text-gray-500 bg-white rounded-2xl border border-gray-100 p-4">{empty}</p>
      ) : (
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-3">
          {products.map((card) => (
            <StorefrontProductCardView key={`${id}-${card.productId}`} card={card} />
          ))}
        </div>
      )}
    </section>
  );
}
