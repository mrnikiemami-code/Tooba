"use client";

/**
 * سه طرح نمایش کالا منطبق با فروشگاه مرجع Shopeiva (فقط خواندن مرجع):
 * - شگفت‌انگیز ← flashSales
 * - طرح زهره ← mostViewed
 * - طرح ماهور ← newProducts
 */

import { useEffect, useMemo, useState } from "react";
import { LocalizedLink as Link } from "../../lib/i18n/LocalizedLink.tsx";
import {
  Clock,
  Eye,
  Flame,
  Medal,
  Sparkles,
  Star,
  TrendingUp,
} from "lucide-react";
import { Swiper, SwiperSlide } from "swiper/react";
import { Autoplay, FreeMode } from "swiper/modules";
import "swiper/css";
import "swiper/css/free-mode";
import { formatOfferAmount, storefrontMediaUrl } from "./storefront-api.ts";
import { StorefrontProductCardView, STOREFRONT_ACCENT } from "./storefront-product-card.tsx";
import type { StorefrontProductCard } from "./storefront-model.ts";
import { PreviewSampleBadge } from "./preview-sample-badge.tsx";

function discountPercent(card: StorefrontProductCard): number | null {
  if (card.promotionalAmountExclusiveOfTax == null || card.offerAmountExclusiveOfTax <= 0) return null;
  const pct = Math.round((1 - card.promotionalAmountExclusiveOfTax / card.offerAmountExclusiveOfTax) * 100);
  return pct > 0 ? pct : null;
}

function displayPrice(card: StorefrontProductCard): number {
  return card.promotionalAmountExclusiveOfTax ?? card.offerAmountExclusiveOfTax;
}

/** شگفت‌انگیز — اسلایدر کارت کامل با تایمر شمارش معکوس (مرجع flashSales). */
export function HomeAmazingProductSection({
  products,
  title = "شگفت‌انگیزهای امروز",
  href = "/offers",
  previewLocale = "fa",
}: {
  products: StorefrontProductCard[];
  title?: string;
  href?: string;
  previewLocale?: string;
}) {
  const [timeLeft, setTimeLeft] = useState({ hours: 2, minutes: 14, seconds: 35 });

  useEffect(() => {
    const timer = setInterval(() => {
      setTimeLeft((prev) => {
        let { hours, minutes, seconds } = prev;
        seconds -= 1;
        if (seconds < 0) {
          seconds = 59;
          minutes -= 1;
          if (minutes < 0) {
            minutes = 59;
            hours -= 1;
            if (hours < 0) hours = 0;
          }
        }
        return { hours, minutes, seconds };
      });
    }, 1000);
    return () => clearInterval(timer);
  }, []);

  if (products.length === 0) return null;

  const pad = (n: number) => String(n).padStart(2, "0");
  const clock = `${pad(timeLeft.hours)}:${pad(timeLeft.minutes)}:${pad(timeLeft.seconds)}`;

  return (
    <section
      className="w-full bg-section-surface"
      data-testid="home-flash-sales"
      data-product-layout="amazing"
      data-storefront-surface-role="section"
    >
      <div className="w-full px-2 sm:px-4 py-8 md:py-10">
        <div className="mb-4 flex items-center justify-between gap-3">
          <div className="flex flex-wrap items-center gap-3 md:gap-4">
            <div className="flex items-center gap-2">
              <span className="h-6 w-1 rounded-full" style={{ backgroundColor: STOREFRONT_ACCENT }} />
              <h2 className="flex items-center gap-2 text-lg font-bold text-gray-900 md:text-xl">
                <Flame className="h-5 w-5" style={{ color: STOREFRONT_ACCENT }} />
                {title}
              </h2>
            </div>
            <div
              className="hidden items-center justify-center gap-1 rounded-full border px-3 py-2.5 md:flex"
              style={{
                backgroundColor: "color-mix(in srgb, rgb(var(--color-primary)) 10%, transparent)",
                borderColor: "color-mix(in srgb, rgb(var(--color-primary)) 20%, transparent)",
              }}
            >
              <Clock className="h-4 w-4" style={{ color: STOREFRONT_ACCENT }} />
              <span className="font-mono text-sm font-bold tabular-nums" style={{ color: STOREFRONT_ACCENT }}>
                {clock}
              </span>
            </div>
          </div>
          <Link href={href} className="flex items-center gap-1 text-xs font-bold hover:underline" style={{ color: STOREFRONT_ACCENT }}>
            مشاهده همه
          </Link>
        </div>
        <div
          className="mb-3 flex w-28 items-center justify-center gap-1 rounded-full border px-3 py-1.5 md:hidden"
          style={{
            backgroundColor: "color-mix(in srgb, rgb(var(--color-primary)) 10%, transparent)",
            borderColor: "color-mix(in srgb, rgb(var(--color-primary)) 20%, transparent)",
          }}
        >
          <Clock className="h-4 w-4" style={{ color: STOREFRONT_ACCENT }} />
          <span className="font-mono text-sm font-bold tabular-nums" style={{ color: STOREFRONT_ACCENT }}>
            {clock}
          </span>
        </div>

        <div className="relative -mx-1">
          <Swiper
            modules={[FreeMode, Autoplay]}
            slidesPerView="auto"
            spaceBetween={12}
            freeMode={{ sticky: true, momentumRatio: 0.5 }}
            autoplay={{ delay: 4000, disableOnInteraction: false, pauseOnMouseEnter: true }}
            dir="rtl"
            grabCursor
            className="!pb-2"
          >
            {products.map((card) => (
              <SwiperSlide key={card.productId} className="!w-[170px] md:!w-[210px]">
                <StorefrontProductCardView card={card} showHoverActions previewLocale={previewLocale} />
              </SwiperSlide>
            ))}
          </Swiper>
        </div>
      </div>
      <div className="h-px w-full bg-gradient-to-r from-transparent via-gray-200 to-transparent" />
    </section>
  );
}

/** طرح زهره — ستون‌های افقی پربازدید (مرجع mostViewed). */
export function HomeZohrehProductSection({
  products,
  title = "پربازدیدترین‌ها",
  href = "/most-viewed",
  previewLocale = "fa",
}: {
  products: StorefrontProductCard[];
  title?: string;
  href?: string;
  previewLocale?: string;
}) {
  const grouped = useMemo(() => {
    const groups: StorefrontProductCard[][] = [];
    for (let i = 0; i < products.length; i += 3) {
      groups.push(products.slice(i, i + 3));
    }
    return groups;
  }, [products]);

  if (products.length === 0) return null;

  const rankColor = (index: number) => {
    if (index === 0) return "text-amber-500";
    if (index === 1) return "text-gray-400";
    if (index === 2) return "text-amber-700";
    return "text-gray-400";
  };

  const rankBadge = (index: number) => {
    if (index === 0) return <Medal className="h-4 w-4 text-amber-500" />;
    if (index === 1) return <Medal className="h-4 w-4 text-gray-400" />;
    if (index === 2) return <Medal className="h-4 w-4 text-amber-700" />;
    return `#${index + 1}`;
  };

  return (
    <section
      className="w-full bg-gradient-to-b from-white via-gray-50/30 to-gray-100/20"
      data-testid="home-most-viewed"
      data-product-layout="zohreh"
      data-storefront-surface-role="alternate"
    >
      <div className="w-full px-2 sm:px-4 py-8 md:py-10">
        <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
          <div className="flex flex-wrap items-center gap-3">
            <div className="flex items-center gap-2">
              <span className="h-6 w-1 rounded-full" style={{ backgroundColor: STOREFRONT_ACCENT }} />
              <h2 className="flex items-center gap-2 text-lg font-extrabold text-gray-900 md:text-xl">
                <Eye className="h-5 w-5" style={{ color: STOREFRONT_ACCENT }} />
                {title}
              </h2>
            </div>
            <div
              className="flex items-center gap-2 rounded-full border px-3 py-1.5"
              style={{
                backgroundColor: "color-mix(in srgb, rgb(var(--color-primary)) 10%, transparent)",
                borderColor: "color-mix(in srgb, rgb(var(--color-primary)) 20%, transparent)",
              }}
            >
              <TrendingUp className="h-3 w-3" style={{ color: STOREFRONT_ACCENT }} />
              <span className="text-[10px] font-bold" style={{ color: STOREFRONT_ACCENT }}>
                {products.length.toLocaleString("fa-IR")} محصول
              </span>
            </div>
            {products.some((card) => card.reviewCount > 0) ? (
              <div className="hidden items-center gap-1 rounded-full border border-amber-500/20 bg-amber-500/10 px-3 py-1 sm:flex">
                <Flame className="h-3 w-3 text-amber-500" />
                <span className="text-[10px] font-bold text-amber-600">
                  {products.reduce((sum, card) => sum + card.reviewCount, 0).toLocaleString("fa-IR")} نظر
                </span>
              </div>
            ) : null}
          </div>
          <Link href={href} className="text-xs font-medium hover:underline" style={{ color: STOREFRONT_ACCENT }}>
            مشاهده همه
          </Link>
        </div>

        <div className="relative -mx-1">
          <Swiper
            modules={[FreeMode, Autoplay]}
            spaceBetween={16}
            slidesPerView={1}
            freeMode={{ enabled: true, sticky: true }}
            autoplay={{ delay: 5000, disableOnInteraction: false, pauseOnMouseEnter: true }}
            breakpoints={{
              640: { slidesPerView: 1.2, spaceBetween: 14 },
              768: { slidesPerView: 2, spaceBetween: 16 },
              1024: { slidesPerView: 2.5, spaceBetween: 18 },
              1280: { slidesPerView: 3, spaceBetween: 20 },
            }}
            dir="rtl"
            className="!pb-2"
          >
            {grouped.map((group, groupIndex) => (
              <SwiperSlide key={`zohreh-group-${groupIndex}`} className="!h-auto">
                <div className="space-y-3">
                  {group.map((card, idx) => {
                    const discount = discountPercent(card);
                    const rating = Math.round(card.averageRating ?? 0);
                    return (
                      <Link
                        key={card.productId}
                        href={`/products/${card.slug}`}
                        className="group/item relative flex items-center gap-3 rounded-xl border border-gray-200 bg-surface p-3 transition-all duration-200 hover:border-[color-mix(in_srgb,rgb(var(--color-primary))_30%,transparent)] hover:bg-gray-50 hover:shadow-md"
                      >
                        {card.previewFake ? <PreviewSampleBadge locale={previewLocale} /> : null}
                        <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-gray-100">
                          <span className={`text-sm font-bold ${rankColor(idx)}`}>{rankBadge(idx)}</span>
                        </div>
                        <div className="relative h-14 w-14 shrink-0 overflow-hidden rounded-lg bg-gray-100 sm:h-16 sm:w-16">
                          {card.mediaAssetId ? (
                            // eslint-disable-next-line @next/next/no-img-element
                            <img
                              src={storefrontMediaUrl(card.mediaAssetId)}
                              alt={card.title}
                              className="h-full w-full object-contain p-1 transition-transform duration-300 group-hover/item:scale-110"
                              loading="lazy"
                            />
                          ) : null}
                          {discount != null ? (
                            <span
                              className="absolute right-0 top-0 rounded-bl-lg px-1.5 py-0.5 text-[8px] font-bold text-white shadow-lg"
                              style={{ backgroundColor: STOREFRONT_ACCENT }}
                            >
                              {discount.toLocaleString("fa-IR")}٪
                            </span>
                          ) : null}
                        </div>
                        <div className="min-w-0 flex-1">
                          <h3 className="truncate text-xs font-bold text-gray-800 transition-colors group-hover/item:text-[rgb(var(--color-primary))] sm:text-sm">
                            {card.title}
                          </h3>
                          <div className="mt-0.5 flex items-center gap-2">
                            <div className="flex items-center gap-0.5">
                              {[1, 2, 3, 4, 5].map((star) => (
                                <Star
                                  key={star}
                                  className={`h-2.5 w-2.5 ${star <= rating ? "fill-amber-400 text-amber-400" : "text-gray-300"}`}
                                />
                              ))}
                            </div>
                            <span className="text-[9px] text-gray-400">
                              ({card.reviewCount.toLocaleString("fa-IR")})
                            </span>
                          </div>
                          <div className="mt-0.5 flex items-center gap-2">
                            <span className="text-xs font-bold" style={{ color: STOREFRONT_ACCENT }}>
                              {formatOfferAmount(displayPrice(card), card.currency)}
                            </span>
                            {card.promotionalAmountExclusiveOfTax != null ? (
                              <span className="text-[9px] text-gray-400 line-through">
                                {formatOfferAmount(card.offerAmountExclusiveOfTax, card.currency)}
                              </span>
                            ) : null}
                          </div>
                        </div>
                      </Link>
                    );
                  })}
                </div>
              </SwiperSlide>
            ))}
          </Swiper>
        </div>
      </div>
      <div className="h-px w-full bg-gradient-to-r from-transparent via-gray-200 to-transparent" />
    </section>
  );
}

/** طرح ماهور — اسلایدر جدیدترین‌ها (مرجع newProducts). */
export function HomeMahoorProductSection({
  products,
  title = "جدیدترین محصولات",
  href = "/new-products",
  previewLocale = "fa",
}: {
  products: StorefrontProductCard[];
  title?: string;
  href?: string;
  previewLocale?: string;
}) {
  if (products.length === 0) return null;

  return (
    <section
      className="w-full bg-gradient-to-b from-white to-gray-50/50"
      data-testid="home-new-products"
      data-product-layout="mahoor"
      data-storefront-surface-role="alternate"
    >
      <div className="w-full px-2 sm:px-4 py-8 md:py-10">
        <div className="mb-4 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="flex items-center gap-2">
              <span className="h-6 w-1 rounded-full" style={{ backgroundColor: STOREFRONT_ACCENT }} />
              <h2 className="flex items-center gap-2 text-lg font-extrabold text-gray-900 md:text-xl">
                <Sparkles className="h-5 w-5 text-emerald-500" />
                {title}
              </h2>
            </div>
            <div className="hidden items-center gap-1 rounded-full border border-emerald-500/20 bg-emerald-500/10 px-3 py-1 sm:flex">
              <span className="text-[10px] font-bold text-emerald-600">
                {products.length.toLocaleString("fa-IR")} محصول جدید
              </span>
            </div>
          </div>
          <Link href={href} className="text-xs font-medium hover:underline" style={{ color: STOREFRONT_ACCENT }}>
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
                <StorefrontProductCardView card={card} showNew showHoverActions previewLocale={previewLocale} />
              </SwiperSlide>
            ))}
          </Swiper>
        </div>
      </div>
      <div className="h-px w-full bg-gradient-to-r from-transparent via-gray-200 to-transparent" />
    </section>
  );
}
