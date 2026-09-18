"use client";

/**
 * Product Showcase Embla rails — additive variants only (LOCK-SF-375…379).
 * Reuses StorefrontProductCardView unchanged; Embla scoped to these five keys.
 */

import { useCallback, useEffect, useMemo, useState, type CSSProperties } from "react";
import useEmblaCarousel from "embla-carousel-react";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { LocalizedLink as Link } from "../../lib/i18n/LocalizedLink.tsx";
import { StorefrontProductCardView, STOREFRONT_ACCENT } from "./storefront-product-card.tsx";
import type { StorefrontProductCard } from "./storefront-model.ts";

export type ProductShowcaseEmblaVariant =
  | "sunny"
  | "money"
  | "cinematic"
  | "cinematic-plus"
  | "explorer";

const VARIANT_META: Record<
  ProductShowcaseEmblaVariant,
  { designNameFa: string; testId: string; defaultTitle: string; defaultHref: string }
> = {
  sunny: {
    designNameFa: "سانی",
    testId: "product-showcase-sunny",
    defaultTitle: "سانی",
    defaultHref: "/products",
  },
  money: {
    designNameFa: "مانی",
    testId: "product-showcase-money",
    defaultTitle: "مانی",
    defaultHref: "/products",
  },
  cinematic: {
    designNameFa: "سینمایی",
    testId: "product-showcase-cinematic",
    defaultTitle: "سینمایی",
    defaultHref: "/products",
  },
  "cinematic-plus": {
    designNameFa: "سینمایی پلاس",
    testId: "product-showcase-cinematic-plus",
    defaultTitle: "سینمایی پلاس",
    defaultHref: "/products",
  },
  explorer: {
    designNameFa: "کاشف",
    testId: "product-showcase-explorer",
    defaultTitle: "کاشف",
    defaultHref: "/products",
  },
};

function useReducedMotionFlag(): boolean {
  const [reduced, setReduced] = useState(false);
  useEffect(() => {
    const mq = window.matchMedia("(prefers-reduced-motion: reduce)");
    const sync = () => setReduced(mq.matches);
    sync();
    mq.addEventListener("change", sync);
    return () => mq.removeEventListener("change", sync);
  }, []);
  return reduced;
}

export function ProductShowcaseEmblaRail({
  variant,
  products,
  title,
  href,
  previewLocale = "fa",
}: {
  variant: ProductShowcaseEmblaVariant;
  products: StorefrontProductCard[];
  title?: string;
  href?: string;
  previewLocale?: string;
}) {
  const meta = VARIANT_META[variant];
  const sectionTitle = title?.trim() || meta.defaultTitle;
  const sectionHref = href?.trim() || meta.defaultHref;
  const headingId = `${meta.testId}-heading`;
  const reducedMotion = useReducedMotionFlag();
  const isDepth = variant === "cinematic" || variant === "cinematic-plus";
  const isExplorer = variant === "explorer";
  const isMoney = variant === "money";
  const isSunny = variant === "sunny";

  const align = isDepth || isMoney ? ("center" as const) : isExplorer ? ("start" as const) : ("start" as const);

  const [emblaRef, emblaApi] = useEmblaCarousel({
    direction: "rtl",
    align,
    containScroll: "trimSnaps",
    dragFree: isExplorer && !reducedMotion,
    skipSnaps: false,
    duration: reducedMotion ? 10 : 22,
  });

  const [selectedIndex, setSelectedIndex] = useState(0);
  const [canPrev, setCanPrev] = useState(false);
  const [canNext, setCanNext] = useState(false);

  const onSelect = useCallback(() => {
    if (!emblaApi) return;
    setSelectedIndex(emblaApi.selectedScrollSnap());
    setCanPrev(emblaApi.canScrollPrev());
    setCanNext(emblaApi.canScrollNext());
  }, [emblaApi]);

  useEffect(() => {
    if (!emblaApi) return;
    onSelect();
    emblaApi.on("select", onSelect);
    emblaApi.on("reInit", onSelect);
    return () => {
      emblaApi.off("select", onSelect);
      emblaApi.off("reInit", onSelect);
    };
  }, [emblaApi, onSelect]);

  const scrollPrev = useCallback(() => emblaApi?.scrollPrev(), [emblaApi]);
  const scrollNext = useCallback(() => emblaApi?.scrollNext(), [emblaApi]);

  const slideBasis = useMemo(() => {
    if (isMoney) return "basis-[72%] sm:basis-[46%] md:basis-[30%] lg:basis-[24%]";
    if (isDepth) return "basis-[78%] sm:basis-[48%] md:basis-[32%] lg:basis-[26%]";
    if (isExplorer) return "basis-[68%] sm:basis-[42%] md:basis-[28%] lg:basis-[22%]";
    return "basis-[70%] sm:basis-[44%] md:basis-[28%] lg:basis-[22%]";
  }, [isDepth, isExplorer, isMoney]);

  const slideStyle = (index: number): CSSProperties => {
    if (reducedMotion || !isDepth) {
      if (isSunny && index === selectedIndex) {
        return { transform: "translate3d(0, -6px, 0)", zIndex: 2 };
      }
      if (isMoney && index === selectedIndex) {
        return { transform: "scale(1.04)", zIndex: 2 };
      }
      if (isExplorer) {
        const offset = index - selectedIndex;
        if (offset === 1) return { transform: "translate3d(0, 10px, 0)" };
        if (offset === -1) return { transform: "translate3d(0, -4px, 0)" };
      }
      return {};
    }

    const offset = index - selectedIndex;
    const strength = variant === "cinematic-plus" ? 1.35 : 1;
    if (offset === 0) {
      return {
        transform: `translate3d(0, 0, ${28 * strength}px) scale(${1.06 + (strength - 1) * 0.02})`,
        zIndex: 3,
        boxShadow: "0 18px 36px rgba(15, 23, 42, 0.14)",
      };
    }
    const abs = Math.min(Math.abs(offset), 3);
    const rotate = (offset > 0 ? -1 : 1) * (variant === "cinematic-plus" ? 5.5 : 4) * Math.min(abs, 2);
    const scale = 1 - abs * (variant === "cinematic-plus" ? 0.045 : 0.035);
    const opacity = 1 - abs * 0.08;
    return {
      transform: `translate3d(0, ${abs * 4}px, ${-abs * 18 * strength}px) rotateY(${rotate}deg) scale(${scale})`,
      opacity,
      zIndex: 2 - abs,
      boxShadow: abs === 1 ? "0 10px 24px rgba(15, 23, 42, 0.1)" : undefined,
    };
  };

  if (products.length === 0) return null;

  const edgeMask =
    isSunny || isDepth
      ? {
          maskImage:
            "linear-gradient(to left, transparent 0%, #000 4%, #000 96%, transparent 100%)",
          WebkitMaskImage:
            "linear-gradient(to left, transparent 0%, #000 4%, #000 96%, transparent 100%)",
        }
      : undefined;

  return (
    <section
      className={`w-full bg-section-surface ${isMoney ? "bg-gradient-to-b from-amber-50/40 to-section-surface" : ""}`}
      data-testid={meta.testId}
      data-product-layout={variant}
      data-product-showcase-rail="embla"
      data-storefront-surface-role="section"
      style={{ perspective: isDepth && !reducedMotion ? 1200 : undefined }}
    >
      <div className="w-full px-2 sm:px-4 py-8 md:py-10">
        <div className="mb-4 flex items-center justify-between gap-3">
          <h2 id={headingId} className="flex min-w-0 items-center gap-2 text-lg font-bold text-gray-900 md:text-xl">
            <span className="h-5 w-1 shrink-0 rounded-full" style={{ backgroundColor: STOREFRONT_ACCENT }} />
            <span className="truncate">{sectionTitle}</span>
          </h2>
          <div className="flex items-center gap-2">
            <div className="hidden items-center gap-1 sm:flex" role="group" aria-label="ناوبری ردیف کالا">
              <button
                type="button"
                className="inline-flex h-11 w-11 items-center justify-center rounded-xl border border-gray-200 bg-surface text-gray-700 disabled:opacity-40"
                onClick={scrollNext}
                disabled={!canNext}
                aria-label="قبلی"
              >
                <ChevronRight className="h-4 w-4" />
              </button>
              <button
                type="button"
                className="inline-flex h-11 w-11 items-center justify-center rounded-xl border border-gray-200 bg-surface text-gray-700 disabled:opacity-40"
                onClick={scrollPrev}
                disabled={!canPrev}
                aria-label="بعدی"
              >
                <ChevronLeft className="h-4 w-4" />
              </button>
            </div>
            <Link
              href={sectionHref}
              className="inline-flex min-h-11 items-center text-xs font-bold hover:underline"
              style={{ color: STOREFRONT_ACCENT }}
            >
              مشاهده همه
            </Link>
          </div>
        </div>

        <div
          className={`overflow-hidden ${isDepth ? "py-4" : "py-1"}`}
          style={edgeMask}
          ref={emblaRef}
          data-embla-viewport=""
        >
          <div
            className={`flex touch-pan-y ${isExplorer ? "gap-2 md:gap-3" : "gap-3 md:gap-4"}`}
            data-embla-container=""
            style={{
              transformStyle: isDepth && !reducedMotion ? "preserve-3d" : undefined,
              minHeight: "18rem",
            }}
          >
            {products.map((card, index) => (
              <div
                key={`${meta.testId}-${card.productId}`}
                className={`min-w-0 shrink-0 grow-0 ${slideBasis} transition-transform duration-300 ease-out`}
                data-embla-slide=""
                data-slide-index={index}
                data-slide-active={index === selectedIndex ? "true" : "false"}
                style={{
                  ...slideStyle(index),
                  backfaceVisibility: "hidden",
                }}
              >
                <div
                  className={`h-full ${isSunny ? "rounded-2xl bg-surface/80 p-1 shadow-sm" : ""} ${
                    isMoney && index === selectedIndex ? "rounded-2xl ring-2 ring-amber-300/70" : ""
                  }`}
                >
                  <StorefrontProductCardView card={card} previewLocale={previewLocale} />
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </section>
  );
}

export function productShowcaseEmblaVariantFromKey(
  variantKey: string,
): ProductShowcaseEmblaVariant | null {
  switch (variantKey) {
    case "product.sunny":
    case "sunny":
      return "sunny";
    case "product.money":
    case "money":
      return "money";
    case "product.cinematic":
    case "cinematic":
      return "cinematic";
    case "product.cinematic-plus":
    case "cinematic-plus":
      return "cinematic-plus";
    case "product.explorer":
    case "explorer":
      return "explorer";
    default:
      return null;
  }
}
