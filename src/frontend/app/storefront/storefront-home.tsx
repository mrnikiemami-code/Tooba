import Link from "next/link";
import { StorefrontProductCardView } from "./storefront-product-card.tsx";
import type { StorefrontCategoryItem, StorefrontProductCard } from "./storefront-model.ts";

const SLIDES = [
  { src: "/images/sliders/slider-1.jpg", alt: "بنر فروشگاهی یک" },
  { src: "/images/sliders/slider-2.jpg", alt: "بنر فروشگاهی دو" },
  { src: "/images/sliders/slider-3.jpg", alt: "بنر فروشگاهی سه" },
  { src: "/images/sliders/slider-4.jpg", alt: "بنر فروشگاهی چهار" },
];

const STORIES = [
  { src: "/images/stories/1.jpg", name: "موبایل" },
  { src: "/images/stories/2.jpg", name: "لپ‌تاپ" },
  { src: "/images/stories/3.jpg", name: "ساعت" },
  { src: "/images/stories/5.jpg", name: "خانگی" },
  { src: "/images/stories/6.jpg", name: "پوشاک" },
  { src: "/images/stories/7.jpg", name: "کتاب" },
];

/**
 * خانهٔ Shopeiva با اسلایدر، استوری، رده، بنر میانی و ردیف کالای زنده.
 */
export function StorefrontShopeivaHome({
  heroTitle,
  heroSubtitle,
  categories,
  products,
}: {
  heroTitle: string;
  heroSubtitle: string;
  categories: StorefrontCategoryItem[];
  products: StorefrontProductCard[];
}) {
  return (
    <div className="py-4 md:py-6 space-y-6 md:space-y-8">
      <h1 className="sr-only">{heroTitle}</h1>

      <section className="w-full px-0">
        <div className="relative rounded-3xl overflow-hidden shadow-2xl bg-gray-100">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img src={SLIDES[0].src} alt={SLIDES[0].alt} className="w-full h-[220px] sm:h-[320px] lg:h-[420px] object-cover" />
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

      <section className="flex gap-4 overflow-x-auto pb-2">
        {STORIES.map((story) => (
          <div key={story.name} className="shrink-0 w-20 text-center">
            <div className="w-20 h-20 rounded-full p-[3px] bg-gradient-to-tr from-[#2563EB] to-amber-400">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={story.src} alt="" className="w-full h-full rounded-full object-cover bg-white p-0.5" />
            </div>
            <p className="text-[11px] mt-1 text-gray-700">{story.name}</p>
          </div>
        ))}
      </section>

      <section>
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg md:text-xl font-black">دسته‌بندی‌ها</h2>
          <Link href="/products" className="text-xs text-[#2563EB] font-bold">
            همه
          </Link>
        </div>
        <div className="grid grid-cols-3 sm:grid-cols-4 lg:grid-cols-8 gap-3">
          {categories.map((category, index) => (
            <Link
              key={category.categoryId}
              href={`/products?categoryId=${category.categoryId}`}
              className="bg-white rounded-2xl border border-gray-100 p-3 text-center hover:shadow-md"
            >
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img
                src={`/images/categories/${[2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16][index % 15]}.${index % 15 === 8 ? "jpg" : "png"}`}
                alt=""
                className="w-14 h-14 mx-auto object-contain mb-2"
              />
              <span className="text-[11px] font-bold text-gray-700 line-clamp-2">{category.name}</span>
            </Link>
          ))}
        </div>
      </section>

      <section className="bg-gradient-to-l from-[#2563EB] to-[#1d4ed8] rounded-3xl p-4 md:p-6">
        <div className="flex items-center justify-between mb-4 text-white">
          <h2 className="text-lg md:text-xl font-black">پیشنهادهای فروشگاه</h2>
          <Link href="/products" className="text-xs font-bold bg-white text-[#2563EB] px-3 py-1 rounded-lg">
            همه کالاها
          </Link>
        </div>
        {products.length === 0 ? (
          <p className="text-white/90 text-sm">کالای قابل‌فروش منتشرشده‌ای برای نمایش نیست.</p>
        ) : (
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-3">
            {products.map((card) => (
              <StorefrontProductCardView key={card.productId} card={card} />
            ))}
          </div>
        )}
      </section>

      <section className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img src="/images/middleBanner/1.webp" alt="" className="w-full h-40 md:h-56 object-cover rounded-3xl" />
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img src="/images/middleBanner/2.webp" alt="" className="w-full h-40 md:h-56 object-cover rounded-3xl" />
      </section>

      {products.length > 0 ? (
        <section>
          <h2 className="text-lg md:text-xl font-black mb-4">تازه‌های ویترین</h2>
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-3">
            {products.map((card) => (
              <StorefrontProductCardView key={`new-${card.productId}`} card={card} />
            ))}
          </div>
        </section>
      ) : null}
    </div>
  );
}
