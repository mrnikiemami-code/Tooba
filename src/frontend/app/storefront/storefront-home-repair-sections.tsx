"use client";

import Link from "next/link";
import { useMemo } from "react";
import {
  ArrowLeft,
  BookOpen,
  CheckCircle,
  Eye,
  Heart,
  Medal,
  MessageCircle,
  Package,
  Quote,
  ShoppingBag,
  Sparkles,
  Star,
  TrendingUp,
  Trophy,
  Calendar,
  Clock,
  Flame,
} from "lucide-react";
import { Swiper, SwiperSlide } from "swiper/react";
import { Autoplay, FreeMode, Pagination } from "swiper/modules";
import "swiper/css";
import "swiper/css/free-mode";
import "swiper/css/pagination";
import { formatOfferAmount, storefrontMediaUrl } from "./storefront-api.ts";
import { StorefrontProductCardView, STOREFRONT_ACCENT } from "./storefront-product-card.tsx";
import type {
  StorefrontArticleItem,
  StorefrontBestSellerColumn,
  StorefrontBrandItem,
  StorefrontFeaturedReviewItem,
  StorefrontProductCard,
} from "./storefront-model.ts";
import { useStorefrontWishlist } from "./storefront-wishlist-provider.tsx";

function discountPercent(card: StorefrontProductCard): number | null {
  if (card.promotionalAmountExclusiveOfTax == null || card.offerAmountExclusiveOfTax <= 0) return null;
  const pct = Math.round((1 - card.promotionalAmountExclusiveOfTax / card.offerAmountExclusiveOfTax) * 100);
  return pct > 0 ? pct : null;
}

function formatReviewDate(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";
  return date.toLocaleDateString("fa-IR", { year: "numeric", month: "long", day: "numeric" });
}

function formatArticleDate(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";
  return date.toLocaleDateString("fa-IR", { year: "numeric", month: "long", day: "numeric" });
}

export function HomeBestSellersSection({ columns }: { columns: StorefrontBestSellerColumn[] }) {
  const wishlist = useStorefrontWishlist();
  if (columns.length === 0) return null;

  return (
    <section
      aria-labelledby="home-best-sellers-heading"
      className="w-full bg-gradient-to-b from-white to-gray-50/50 py-8 md:py-10 px-2 sm:px-4"
      data-testid="home-best-sellers"
    >
      <div className="flex flex-wrap items-center justify-between gap-3 mb-4">
        <div className="flex items-center gap-3">
          <div className="flex items-center gap-2">
            <span className="w-1 h-6 rounded-full" style={{ backgroundColor: STOREFRONT_ACCENT }} />
            <h2 id="home-best-sellers-heading" className="text-lg md:text-xl font-extrabold text-gray-900 flex items-center gap-2">
              <Trophy className="w-5 h-5 text-amber-500 fill-amber-500" />
              پرفروش‌ترین‌ها
            </h2>
          </div>
          <div className="hidden sm:flex items-center gap-1 bg-amber-500/10 px-3 py-1 rounded-full border border-amber-500/20">
            <Sparkles className="w-3.5 h-3.5 text-amber-500" />
            <span className="text-[10px] font-bold text-amber-600">
              {columns.reduce((sum, column) => sum + column.products.length, 0).toLocaleString("fa-IR")} محصول
            </span>
          </div>
        </div>
        <Link href="/best-seller" className="text-xs font-medium rounded-lg transition-all" style={{ color: STOREFRONT_ACCENT }}>
          مشاهده همه
        </Link>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 md:gap-5 max-h-[600px] overflow-y-auto p-1">
        {columns.map((column) => (
          <div
            key={`${column.categoryId}-${column.categoryName}`}
            className="bg-white mb-4 rounded-2xl border border-gray-200 overflow-hidden hover:shadow-xl hover:-translate-y-1 transition-all duration-300 group/col"
          >
            <div className="p-4 pb-3 border-b border-gray-100 bg-gradient-to-r from-gray-50/50 to-white">
              <div className="flex items-center justify-between">
                <h4 className="text-sm font-bold text-gray-900 flex items-center gap-2">
                  <Package className="w-3.5 h-3.5" style={{ color: STOREFRONT_ACCENT }} />
                  {column.categoryName}
                </h4>
                <span className="text-[10px] px-2.5 py-0.5 rounded-full font-bold" style={{ color: STOREFRONT_ACCENT, backgroundColor: `${STOREFRONT_ACCENT}1a` }}>
                  {column.products.length.toLocaleString("fa-IR")} کالا
                </span>
              </div>
              <p className="text-[10px] text-gray-400 flex items-center gap-1 mt-0.5">
                <TrendingUp className="w-3 h-3" /> محبوب‌ترین‌های این دسته
              </p>
            </div>

            <div className="p-3 space-y-3">
              {column.products.map((card, index) => {
                const discount = discountPercent(card);
                const saved = wishlist.membership.has(card.productId);
                return (
                  <div
                    key={card.productId}
                    className="group/product flex items-center gap-3 p-2.5 py-0.5 rounded-xl hover:bg-gray-50 transition-all hover:scale-[1.02] hover:shadow-md border border-transparent hover:border-gray-200"
                  >
                    <div className="flex-shrink-0 w-7 h-7 rounded-full bg-gray-100 flex items-center justify-center">
                      <Medal className={`w-3.5 h-3.5 ${index === 0 ? "text-amber-500" : index === 1 ? "text-gray-400" : "text-amber-700"}`} />
                    </div>
                    <Link href={`/products/${card.slug}`} className="relative w-16 h-16 sm:w-20 sm:h-20 rounded-xl overflow-hidden bg-gray-100 flex-shrink-0">
                      {/* eslint-disable-next-line @next/next/no-img-element */}
                      <img
                        src={storefrontMediaUrl(card.mediaAssetId)}
                        alt=""
                        className="absolute inset-0 w-full h-full object-contain p-1.5 group-hover/product:scale-110 transition-transform duration-500"
                      />
                      {discount !== null ? (
                        <span className="absolute -top-0.5 -right-0.5 text-white text-[9px] font-bold px-2 py-0.5 rounded-bl-xl shadow-lg" style={{ backgroundColor: STOREFRONT_ACCENT }}>
                          {discount.toLocaleString("fa-IR")}%
                        </span>
                      ) : null}
                    </Link>
                    <Link href={`/products/${card.slug}`} className="flex-1 min-w-0">
                      <h5 className="text-sm font-bold text-gray-800 truncate group-hover/product:transition-colors" style={{ color: undefined }}>
                        <span className="group-hover/product:text-[#2563EB]">{card.title}</span>
                      </h5>
                      {card.reviewCount > 0 && card.averageRating !== null ? (
                        <div className="flex items-center gap-1.5 mt-0.5">
                          <div className="flex items-center gap-0.5">
                            {[1, 2, 3, 4, 5].map((star) => (
                              <Star key={star} className={`w-3 h-3 ${star <= Math.round(card.averageRating!) ? "fill-amber-400 text-amber-400" : "text-gray-300"}`} />
                            ))}
                          </div>
                          <span className="text-[10px] text-gray-400 font-medium">({card.reviewCount.toLocaleString("fa-IR")})</span>
                        </div>
                      ) : null}
                      <div className="flex items-center gap-2 mt-0.5">
                        <span className="text-sm font-black" style={{ color: STOREFRONT_ACCENT }}>
                          {formatOfferAmount(card.promotionalAmountExclusiveOfTax ?? card.offerAmountExclusiveOfTax, card.currency)}
                        </span>
                        {card.promotionalAmountExclusiveOfTax !== null ? (
                          <span className="text-[10px] text-gray-400 line-through">
                            {formatOfferAmount(card.offerAmountExclusiveOfTax, card.currency)}
                          </span>
                        ) : null}
                      </div>
                    </Link>
                    <div className="flex flex-col gap-1.5 opacity-0 group-hover/product:opacity-100 transition-all duration-300 translate-x-2 group-hover/product:translate-x-0">
                      <button
                        type="button"
                        className="p-1.5 rounded-full hover:bg-red-50 transition-colors"
                        aria-label="علاقه‌مندی"
                        onClick={() => void wishlist.toggle(card.productId)}
                      >
                        <Heart className={`w-4 h-4 ${saved ? "fill-[#2563EB] text-[#2563EB]" : "text-gray-400 hover:text-[#2563EB]"}`} />
                      </button>
                      <Link href={`/products/${card.slug}`} className="p-1.5 rounded-full hover:bg-blue-50 transition-colors">
                        <ShoppingBag className="w-4 h-4 text-gray-400 hover:text-[#2563EB]" />
                      </Link>
                      <Link href={`/products/${card.slug}`} className="p-1.5 rounded-full hover:bg-blue-50 transition-colors">
                        <Eye className="w-4 h-4 text-gray-400 hover:text-blue-500" />
                      </Link>
                    </div>
                  </div>
                );
              })}
            </div>

            {column.categoryId ? (
              <div className="p-3 pt-0 border-t border-gray-100 mt-1">
                <Link
                  href={`/products?categoryId=${column.categoryId}`}
                  className="flex items-center justify-center gap-2 text-xs font-bold w-full py-2 rounded-xl transition-colors group/btn"
                  style={{ color: STOREFRONT_ACCENT }}
                >
                  مشاهده همه محصولات این دسته
                  <ArrowLeft className="w-4 h-4 transition-transform group-hover/btn:-translate-x-1" />
                </Link>
              </div>
            ) : null}
          </div>
        ))}
      </div>
      <div className="w-full h-px bg-gradient-to-r from-transparent via-gray-200 to-transparent mt-2" />
    </section>
  );
}

export function HomeBrandsSection({ brands }: { brands: StorefrontBrandItem[] }) {
  if (brands.length === 0) {
    return (
      <section aria-labelledby="home-brands-heading" className="w-full px-2 sm:px-4 py-8 md:py-10" data-testid="home-brands">
        <p className="text-sm text-gray-500 bg-white rounded-2xl border border-gray-100 p-4">برند منتشرشده‌ای در Catalog نیست.</p>
      </section>
    );
  }

  return (
    <section aria-labelledby="home-brands-heading" className="w-full bg-white py-8 md:py-10 px-2 sm:px-4" data-testid="home-brands">
      <div className="flex items-center justify-between mb-4">
        <h2 id="home-brands-heading" className="text-lg md:text-xl font-bold text-gray-900 flex items-center gap-2">
          <span className="w-1 h-5 rounded-full" style={{ backgroundColor: STOREFRONT_ACCENT }} />
          برندهای محبوب
        </h2>
        <Link href="/brands" className="text-xs hover:underline font-medium" style={{ color: STOREFRONT_ACCENT }}>
          مشاهده همه
        </Link>
      </div>
      <div className="relative">
        <Swiper modules={[FreeMode]} slidesPerView="auto" spaceBetween={12} freeMode={{ sticky: true, momentumRatio: 0.5 }} dir="rtl" grabCursor className="!pb-2">
          {brands.map((brand) => (
            <SwiperSlide key={brand.brandId} className="!w-[160px] md:!w-[180px]">
              <Link href={`/brand/${brand.slug}`} className="group w-full block text-right outline-none">
                <div className="relative w-full aspect-square rounded-2xl overflow-hidden bg-gray-100 shadow-sm hover:shadow-xl transition-all duration-300 border border-gray-200" style={{ boxShadow: undefined }}>
                  <div className="absolute inset-0 z-0 flex items-center justify-center bg-gray-100">
                    {/* eslint-disable-next-line @next/next/no-img-element */}
                    <img
                      src={storefrontMediaUrl(brand.logoMediaAssetId)}
                      alt=""
                      className="absolute inset-0 w-full h-full object-contain p-3 transition-transform duration-500 group-hover:scale-105"
                    />
                    <div className="absolute inset-0 bg-gradient-to-t from-black/60 via-transparent to-transparent" />
                  </div>
                  <div className="absolute bottom-0 left-0 right-0 pb-1 p-3 z-10">
                    <div className="transform transition-all duration-300 group-hover:scale-105 origin-bottom-right">
                      <h3 className="text-white font-bold text-sm md:text-lg drop-shadow-lg line-clamp-1 text-right">{brand.name}</h3>
                      <span className="text-[8px] md:text-[9px] text-gray-300/80 bg-black/30 backdrop-blur-sm px-2 py-0.5 rounded-full inline-block mt-0.5">
                        {brand.productCount.toLocaleString("fa-IR")} محصول
                      </span>
                    </div>
                  </div>
                  <div className="absolute inset-0 z-10 flex items-center justify-center gap-3 opacity-0 group-hover:opacity-100 transition-all duration-300 bg-black/40 backdrop-blur-sm">
                    <div className="bg-white/20 backdrop-blur-md rounded-full p-2.5 border border-white/30 hover:scale-110 transition-all duration-300 shadow-lg" style={{ borderColor: undefined }}>
                      <Eye className="w-5 h-5 text-white" />
                    </div>
                  </div>
                </div>
              </Link>
            </SwiperSlide>
          ))}
        </Swiper>
      </div>
      <div className="w-full h-px bg-gradient-to-r from-transparent via-gray-200 to-transparent mt-2" />
    </section>
  );
}

export function HomeNewProductsSection({ products }: { products: StorefrontProductCard[] }) {
  if (products.length === 0) return null;

  return (
    <section aria-labelledby="home-new-products-heading" className="w-full bg-gradient-to-b from-white to-gray-50/50 py-8 md:py-10 px-2 sm:px-4" data-testid="home-new-products">
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-3">
          <div className="flex items-center gap-2">
            <span className="w-1 h-6 rounded-full" style={{ backgroundColor: STOREFRONT_ACCENT }} />
            <h2 id="home-new-products-heading" className="text-lg md:text-xl font-extrabold text-gray-900 flex items-center gap-2">
              <Sparkles className="w-5 h-5 text-emerald-500" />
              جدیدترین محصولات
            </h2>
          </div>
          <div className="hidden sm:flex items-center gap-1 bg-emerald-500/10 px-3 py-1 rounded-full border border-emerald-500/20">
            <span className="text-[10px] font-bold text-emerald-600">{products.length.toLocaleString("fa-IR")} محصول جدید</span>
          </div>
        </div>
        <Link href="/new-products" className="text-xs hover:underline font-medium" style={{ color: STOREFRONT_ACCENT }}>
          مشاهده همه
        </Link>
      </div>
      <div className="relative -mx-1" data-testid="home-new-products-carousel">
        <Swiper
          modules={[FreeMode, Autoplay]}
          slidesPerView="auto"
          spaceBetween={16}
          freeMode={{ sticky: true, momentumRatio: 0.5 }}
          autoplay={{ delay: 4000, disableOnInteraction: false, pauseOnMouseEnter: true }}
          dir="rtl"
          grabCursor
          className="!pb-2"
        >
          {products.map((card) => (
            <SwiperSlide key={card.productId} className="!w-[180px] mb-3 md:!w-[220px]">
              <StorefrontProductCardView card={card} showNew showHoverActions />
            </SwiperSlide>
          ))}
        </Swiper>
      </div>
      <div className="w-full h-px bg-gradient-to-r from-transparent via-gray-200 to-transparent mt-2" />
    </section>
  );
}

export function HomeTestimonialsSection({ reviews }: { reviews: StorefrontFeaturedReviewItem[] }) {
  const summary = useMemo(() => {
    if (reviews.length === 0) return null;
    const average = reviews.reduce((sum, item) => sum + item.rating, 0) / reviews.length;
    return { average, verified: reviews.filter((item) => item.verifiedPurchase).length };
  }, [reviews]);

  if (reviews.length === 0) return null;

  return (
    <section aria-labelledby="home-testimonials-heading" className="w-full bg-gradient-to-b from-white via-gray-50/30 to-gray-100/20 py-8 md:py-10 px-2 sm:px-4" data-testid="home-testimonials">
      <div className="flex flex-wrap items-center justify-between gap-3 mb-4">
        <div className="flex items-center gap-3 flex-wrap">
          <div className="flex items-center gap-2">
            <span className="w-1 h-6 rounded-full" style={{ backgroundColor: STOREFRONT_ACCENT }} />
            <h2 id="home-testimonials-heading" className="text-lg md:text-xl font-extrabold text-gray-900 flex items-center gap-2">
              <MessageCircle className="w-5 h-5" style={{ color: STOREFRONT_ACCENT }} />
              نظرات مشتریان
            </h2>
          </div>
          {summary ? (
            <div className="flex items-center gap-2 px-3 py-1.5 rounded-full border" style={{ backgroundColor: `${STOREFRONT_ACCENT}1a`, borderColor: `${STOREFRONT_ACCENT}33` }}>
              <Star className="w-3 h-3 text-amber-400 fill-amber-400" />
              <span className="text-[11px] font-bold text-gray-800">{summary.average.toLocaleString("fa-IR", { maximumFractionDigits: 1 })}</span>
              <span className="text-[10px] text-gray-500">({reviews.length.toLocaleString("fa-IR")} نظر)</span>
            </div>
          ) : null}
          {summary && summary.verified > 0 ? (
            <div className="hidden sm:flex items-center gap-2 bg-emerald-500/10 px-3 py-1 rounded-full border border-emerald-500/20">
              <CheckCircle className="w-3 h-3 text-emerald-500" />
              <span className="text-[10px] font-bold text-emerald-600">{summary.verified.toLocaleString("fa-IR")} خرید تأییدشده</span>
            </div>
          ) : null}
        </div>
      </div>

      <div className="relative -mx-1">
        <Swiper
          modules={[Autoplay, Pagination]}
          slidesPerView={1}
          spaceBetween={16}
          autoplay={{ delay: 4000, disableOnInteraction: false, pauseOnMouseEnter: true }}
          pagination={{ clickable: true, dynamicBullets: true }}
          dir="rtl"
          className="!pb-8"
          breakpoints={{
            480: { slidesPerView: 1.1, spaceBetween: 12 },
            640: { slidesPerView: 1.5, spaceBetween: 14 },
            768: { slidesPerView: 2, spaceBetween: 16 },
            1024: { slidesPerView: 2.5, spaceBetween: 18 },
            1280: { slidesPerView: 3, spaceBetween: 20 },
          }}
        >
          {reviews.map((item) => (
            <SwiperSlide key={item.publicId} className="!h-auto">
              <article className="bg-white rounded-2xl p-4 md:p-5 border border-gray-200 shadow-md hover:shadow-2xl transition-all duration-400 h-full flex flex-col group relative overflow-hidden">
                <div className="absolute top-0 left-0 right-0 h-1 bg-gradient-to-r opacity-0 group-hover:opacity-100 transition-opacity duration-500" style={{ backgroundImage: `linear-gradient(to right, ${STOREFRONT_ACCENT}, #fbbf24, ${STOREFRONT_ACCENT})` }} />
                <Quote className="absolute bottom-3 right-3 w-10 h-10 rotate-180 group-hover:scale-110 transition-all duration-500" style={{ color: `${STOREFRONT_ACCENT}0d` }} />
                <div className="flex items-center gap-3 mb-3">
                  <div className="relative w-12 h-12 rounded-full overflow-hidden bg-gray-200 flex-shrink-0 border-2 group-hover:border-[#2563EB]/50 transition-colors duration-300" style={{ borderColor: `${STOREFRONT_ACCENT}33` }}>
                    <div className="w-full h-full flex items-center justify-center text-sm font-bold text-gray-500">
                      {item.authorDisplayName.slice(0, 1)}
                    </div>
                    {item.verifiedPurchase ? (
                      <div className="absolute -bottom-0.5 -right-0.5 bg-emerald-500 rounded-full p-0.5 border-2 border-white shadow-md">
                        <CheckCircle className="w-2.5 h-2.5 text-white fill-emerald-500" />
                      </div>
                    ) : null}
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 flex-wrap">
                      <h4 className="text-sm font-bold text-gray-900 truncate">{item.authorDisplayName}</h4>
                      {item.verifiedPurchase ? (
                        <span className="text-[8px] bg-emerald-500 text-white px-1.5 py-0.5 rounded-full font-bold flex items-center gap-0.5">
                          <CheckCircle className="w-2 h-2" />
                          تایید
                        </span>
                      ) : null}
                    </div>
                    <div className="flex items-center gap-2 flex-wrap">
                      <div className="flex items-center gap-0.5">
                        {[1, 2, 3, 4, 5].map((star) => (
                          <Star key={star} className={`w-3.5 h-3.5 ${star <= item.rating ? "fill-amber-400 text-amber-400" : "text-gray-300"}`} />
                        ))}
                      </div>
                      <span className="text-[10px] text-gray-400">•</span>
                      <div className="flex items-center gap-0.5 text-[10px] text-gray-500">
                        <Clock className="w-3 h-3" />
                        {formatReviewDate(item.createdAt)}
                      </div>
                    </div>
                  </div>
                </div>
                <p className="text-sm text-gray-600 leading-relaxed flex-1 line-clamp-3 group-hover:line-clamp-none transition-all duration-300 pr-6">
                  &quot;{item.body}&quot;
                </p>
                <div className="mt-3 pt-3 border-t border-gray-100">
                  <div className="flex items-center justify-between flex-wrap gap-2">
                    <div className="flex items-center gap-2">
                      <span className="text-[10px] text-gray-500">خرید:</span>
                      <Link href={`/products/${item.productSlug}`} className="text-[10px] font-semibold" style={{ color: STOREFRONT_ACCENT }}>
                        {item.productTitle}
                      </Link>
                    </div>
                    {/* Decorative only — no client-side fake engagement toggle. */}
                    <span className="flex items-center gap-1 text-[10px] text-gray-300 px-1.5 py-0.5" aria-hidden="true">
                      <Heart className="w-3.5 h-3.5" />
                    </span>
                  </div>
                </div>
              </article>
            </SwiperSlide>
          ))}
        </Swiper>
      </div>
      <div className="w-full h-px bg-gradient-to-r from-transparent via-gray-200 to-transparent mt-2" />
    </section>
  );
}

export function HomeArticlesSection({ articles }: { articles: StorefrontArticleItem[] }) {
  if (articles.length === 0) return null;

  return (
    <section aria-labelledby="home-articles-heading" className="w-full bg-gradient-to-b from-white via-gray-50/30 to-gray-100/20 py-8 md:py-10 px-2 sm:px-4" data-testid="home-articles">
      <div className="flex flex-wrap items-center justify-between gap-3 mb-4">
        <div className="flex items-center gap-3 flex-wrap">
          <div className="flex items-center gap-2">
            <span className="w-1 h-6 rounded-full" style={{ backgroundColor: STOREFRONT_ACCENT }} />
            <h2 id="home-articles-heading" className="text-lg md:text-xl font-extrabold text-gray-900 flex items-center gap-2">
              <BookOpen className="w-5 h-5" style={{ color: STOREFRONT_ACCENT }} />
              آخرین مقالات
            </h2>
          </div>
          <div className="flex items-center gap-2 px-3 py-1.5 rounded-full border" style={{ backgroundColor: `${STOREFRONT_ACCENT}1a`, borderColor: `${STOREFRONT_ACCENT}33` }}>
            <Sparkles className="w-3 h-3" style={{ color: STOREFRONT_ACCENT }} />
            <span className="text-[10px] font-bold" style={{ color: STOREFRONT_ACCENT }}>
              {articles.length.toLocaleString("fa-IR")} مقاله جدید
            </span>
          </div>
        </div>
        <Link href="/blogs" className="text-xs hover:underline font-medium" style={{ color: STOREFRONT_ACCENT }}>
          مشاهده همه
        </Link>
      </div>

      <div className="relative -mx-1">
        <Swiper
          modules={[Autoplay, Pagination]}
          slidesPerView="auto"
          spaceBetween={16}
          autoplay={{ delay: 5000, disableOnInteraction: false, pauseOnMouseEnter: true }}
          pagination={{ clickable: true, dynamicBullets: true }}
          dir="rtl"
          className="!pb-8"
        >
          {articles.map((post) => (
            <SwiperSlide key={post.articleId} className="!w-[260px] sm:!w-[280px] md:!w-[300px] lg:!w-[320px] !h-auto">
              <Link href={`/blogs/${post.slug}`} className="group block h-full">
                <article className="bg-white rounded-2xl overflow-hidden border border-gray-200 shadow-sm hover:shadow-2xl hover:-translate-y-2 transition-all duration-400 h-full flex flex-col">
                  <div className="relative overflow-hidden aspect-[16/10] bg-gray-100">
                    {/* eslint-disable-next-line @next/next/no-img-element */}
                    <img
                      src={storefrontMediaUrl(post.coverMediaAssetId)}
                      alt=""
                      className="absolute inset-0 w-full h-full object-cover transition-transform duration-700 group-hover:scale-110"
                    />
                    {post.tags[0] ? (
                      <div className="absolute top-2 left-2">
                        <span className="px-2 py-0.5 text-[9px] font-medium bg-white/95 backdrop-blur-sm rounded-lg shadow-sm border border-gray-200" style={{ color: STOREFRONT_ACCENT }}>
                          {post.tags[0]}
                        </span>
                      </div>
                    ) : null}
                    {post.isFeatured ? (
                      <div className="absolute top-2 right-2">
                        <span className="px-1.5 py-0.5 text-[9px] font-bold bg-gradient-to-r from-amber-500 to-orange-500 text-white rounded-lg shadow-md flex items-center gap-1">
                          <Flame className="w-2.5 h-2.5" />
                          ویژه
                        </span>
                      </div>
                    ) : null}
                    <div className="absolute inset-0 bg-gradient-to-t from-black/70 via-black/20 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-500">
                      <div className="absolute bottom-2 left-2 right-2">
                        <span className="block w-full py-1.5 bg-white text-gray-900 rounded-lg text-[10px] font-medium text-center group-hover:text-white transition-colors duration-200 group-hover:bg-[#2563EB]">
                          مطالعه مقاله
                        </span>
                      </div>
                    </div>
                  </div>
                  <div className="p-3 flex-1 flex flex-col">
                    <div className="flex items-center gap-1.5 mb-1.5">
                      <span className="text-[9px] text-gray-600 font-medium line-clamp-1">{post.authorDisplayName}</span>
                      <span className="text-gray-300">•</span>
                      <div className="flex items-center gap-0.5 text-[8px] text-gray-500">
                        <Calendar className="w-2.5 h-2.5" />
                        <span>{formatArticleDate(post.publishDate)}</span>
                      </div>
                    </div>
                    <h3 className="text-xs sm:text-sm font-bold text-gray-900 mb-1 line-clamp-2 group-hover:text-[#2563EB] transition-colors duration-200">
                      {post.title}
                    </h3>
                    <p className="text-[10px] text-gray-600 leading-relaxed line-clamp-2 mb-1.5 flex-1">{post.excerpt}</p>
                    <div className="flex flex-wrap gap-0.5 mb-2">
                      {post.tags.slice(0, 2).map((tag) => (
                        <span key={tag} className="px-1 py-0.5 text-[7px] bg-gray-100 text-gray-600 rounded-md">
                          #{tag}
                        </span>
                      ))}
                    </div>
                    <div className="mt-auto pt-1.5 border-t border-gray-100 flex items-center justify-between">
                      <div className="flex items-center gap-0.5 text-[9px] text-gray-500">
                        <Clock className="w-2.5 h-2.5" />
                        <span>{Math.max(1, Math.ceil(post.excerpt.length / 120)).toLocaleString("fa-IR")} دقیقه</span>
                      </div>
                      <span className="text-[9px] font-bold" style={{ color: STOREFRONT_ACCENT }}>مطالعه</span>
                    </div>
                  </div>
                </article>
              </Link>
            </SwiperSlide>
          ))}
        </Swiper>
      </div>
      <div className="w-full h-px bg-gradient-to-r from-transparent via-gray-200 to-transparent mt-2" />
    </section>
  );
}
