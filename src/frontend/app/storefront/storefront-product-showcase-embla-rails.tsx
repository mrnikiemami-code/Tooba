"use client";

/**
 * Product Showcase Embla rails — additive variants only (LOCK-SF-375…390).
 * One shared rail engine + variant-driven composition/motion metadata.
 * Reuses StorefrontProductCardView unchanged; Embla + Autoplay scoped to these five keys.
 * R13-R4: calm motion, readable proportions, no sweep overlays, upright explorer.
 */

import { useCallback, useEffect, useMemo, useRef, useState, type CSSProperties } from "react";
import useEmblaCarousel from "embla-carousel-react";
import Autoplay from "embla-carousel-autoplay";
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

type RailContract = {
  designNameFa: string;
  testId: string;
  defaultTitle: string;
  defaultHref: string;
  align: "start" | "center";
  loop: boolean;
  dragFree: boolean;
  duration: number;
  autoplayDelayMs: number;
  slideBasis: string;
  gapClass: string;
  sectionClass: string;
  viewportClass: string;
  perspective: number | undefined;
  edgeFade: boolean;
  asymmetry: boolean;
  navCompact: boolean;
  /** Active scale / depth strength multipliers for composition. */
  activeScale: number;
  neighborScaleStep: number;
  rotateYDeg: number;
  translateZActive: number;
  translateZStep: number;
  elevateActivePx: number;
  neighborOpacityStep: number;
  warmGlow: boolean;
  commerceRing: boolean;
  vignette: boolean;
};

const VARIANT_CONTRACTS: Record<ProductShowcaseEmblaVariant, RailContract> = {
  sunny: {
    designNameFa: "سانی",
    testId: "product-showcase-sunny",
    defaultTitle: "سانی",
    defaultHref: "/products",
    align: "center",
    loop: true,
    dragFree: false,
    // Embla duration ticks — higher = calmer (~500–800ms feel with CSS ease)
    duration: 26,
    autoplayDelayMs: 5200,
    slideBasis: "basis-[56%] sm:basis-[36%] md:basis-[24%] lg:basis-[19%]",
    gapClass: "gap-4 md:gap-5",
    sectionClass: "bg-gradient-to-b from-amber-50/40 via-section-surface to-section-surface",
    viewportClass: "py-2",
    perspective: undefined,
    edgeFade: false,
    asymmetry: false,
    navCompact: false,
    activeScale: 1.02,
    neighborScaleStep: 0.012,
    rotateYDeg: 0,
    translateZActive: 0,
    translateZStep: 0,
    elevateActivePx: 4,
    neighborOpacityStep: 0.03,
    // Local active-only warm ring — no traveling edge sweep (LOCK-SF-387)
    warmGlow: true,
    commerceRing: false,
    vignette: false,
  },
  money: {
    designNameFa: "مانی",
    testId: "product-showcase-money",
    defaultTitle: "مانی",
    defaultHref: "/products",
    align: "center",
    loop: true,
    dragFree: false,
    duration: 22,
    autoplayDelayMs: 4000,
    slideBasis: "basis-[58%] sm:basis-[38%] md:basis-[25%] lg:basis-[20%]",
    gapClass: "gap-2 md:gap-2.5",
    sectionClass: "bg-gradient-to-b from-orange-50/50 via-amber-50/20 to-section-surface",
    viewportClass: "py-2",
    perspective: undefined,
    edgeFade: false,
    asymmetry: false,
    navCompact: true,
    activeScale: 1.03,
    neighborScaleStep: 0.02,
    rotateYDeg: 0,
    translateZActive: 0,
    translateZStep: 0,
    elevateActivePx: 0,
    neighborOpacityStep: 0.05,
    warmGlow: false,
    commerceRing: true,
    vignette: false,
  },
  cinematic: {
    designNameFa: "سینمایی",
    testId: "product-showcase-cinematic",
    defaultTitle: "سینمایی",
    defaultHref: "/products",
    align: "center",
    loop: true,
    dragFree: false,
    duration: 30,
    autoplayDelayMs: 6000,
    slideBasis: "basis-[62%] sm:basis-[40%] md:basis-[28%] lg:basis-[22%]",
    gapClass: "gap-2 md:gap-3",
    sectionClass: "bg-gradient-to-b from-slate-100/70 via-section-surface to-section-surface",
    viewportClass: "py-4 md:py-5",
    perspective: 1200,
    edgeFade: true,
    asymmetry: false,
    navCompact: false,
    activeScale: 1.03,
    neighborScaleStep: 0.025,
    rotateYDeg: 3,
    translateZActive: 18,
    translateZStep: 14,
    elevateActivePx: 0,
    neighborOpacityStep: 0.08,
    warmGlow: false,
    commerceRing: false,
    vignette: true,
  },
  "cinematic-plus": {
    designNameFa: "سینمایی پلاس",
    testId: "product-showcase-cinematic-plus",
    defaultTitle: "سینمایی پلاس",
    defaultHref: "/products",
    align: "center",
    loop: true,
    dragFree: false,
    duration: 34,
    autoplayDelayMs: 6800,
    // Same commercial basis as cinematic — richness via depth/shadow, not size (LOCK-SF-388)
    slideBasis: "basis-[62%] sm:basis-[40%] md:basis-[28%] lg:basis-[22%]",
    gapClass: "gap-2.5 md:gap-3.5",
    sectionClass: "bg-gradient-to-b from-slate-200/60 via-slate-50/30 to-section-surface",
    viewportClass: "py-5 md:py-6",
    perspective: 1300,
    edgeFade: true,
    asymmetry: false,
    navCompact: false,
    activeScale: 1.035,
    neighborScaleStep: 0.035,
    rotateYDeg: 3.5,
    translateZActive: 28,
    translateZStep: 20,
    elevateActivePx: 0,
    neighborOpacityStep: 0.1,
    warmGlow: false,
    commerceRing: false,
    vignette: true,
  },
  explorer: {
    designNameFa: "کاشف",
    testId: "product-showcase-explorer",
    defaultTitle: "کاشف",
    defaultHref: "/products",
    align: "start",
    loop: true,
    dragFree: false,
    duration: 26,
    autoplayDelayMs: 4800,
    slideBasis: "basis-[58%] sm:basis-[38%] md:basis-[25%] lg:basis-[20%]",
    gapClass: "gap-3 md:gap-4",
    sectionClass: "bg-gradient-to-l from-sky-50/40 via-section-surface to-section-surface",
    viewportClass: "py-2 ps-1 pe-10 md:pe-20",
    perspective: undefined,
    edgeFade: false,
    asymmetry: true,
    navCompact: false,
    activeScale: 1.01,
    neighborScaleStep: 0.01,
    rotateYDeg: 0,
    translateZActive: 0,
    translateZStep: 0,
    elevateActivePx: 0,
    neighborOpacityStep: 0.03,
    warmGlow: false,
    commerceRing: false,
    vignette: false,
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
  enableAutoplay = true,
}: {
  variant: ProductShowcaseEmblaVariant;
  products: StorefrontProductCard[];
  title?: string;
  href?: string;
  previewLocale?: string;
  /** Storefront rails autoplay; Admin picker/Review previews keep motion off (LOCK-SF-383). */
  enableAutoplay?: boolean;
}) {
  const contract = VARIANT_CONTRACTS[variant];
  const sectionTitle = title?.trim() || contract.defaultTitle;
  const sectionHref = href?.trim() || contract.defaultHref;
  const headingId = `${contract.testId}-heading`;
  const reducedMotion = useReducedMotionFlag();
  const isDepth = variant === "cinematic" || variant === "cinematic-plus";
  const rootRef = useRef<HTMLElement | null>(null);
  const autoplayAllowed = enableAutoplay && !reducedMotion;

  const autoplayPlugin = useMemo(() => {
    if (!autoplayAllowed) return null;
    return Autoplay({
      delay: contract.autoplayDelayMs,
      stopOnInteraction: false,
      // Hover pause is handled via section mouse enter/leave so Playwright proofs
      // and first paint are not blocked by the default cursor position.
      stopOnMouseEnter: false,
      stopOnFocusIn: false,
      playOnInit: true,
    });
  }, [autoplayAllowed, contract.autoplayDelayMs]);

  const plugins = useMemo(() => (autoplayPlugin ? [autoplayPlugin] : []), [autoplayPlugin]);

  const [emblaRef, emblaApi] = useEmblaCarousel(
    {
      direction: "rtl",
      align: contract.align,
      loop: contract.loop && products.length > 2,
      // Loop + trimSnaps conflicts in Embla and can freeze autoplay for some rails.
      containScroll: contract.loop && products.length > 2 ? false : contract.asymmetry ? false : "trimSnaps",
      dragFree: contract.dragFree && !reducedMotion,
      skipSnaps: false,
      duration: reducedMotion ? 10 : contract.duration,
    },
    plugins,
  );

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
    const onPointerDown = () => {
      autoplayPlugin?.stop();
    };
    const onPointerUp = () => {
      if (!reducedMotion) autoplayPlugin?.play();
    };
    emblaApi.on("pointerDown", onPointerDown);
    emblaApi.on("pointerUp", onPointerUp);
    return () => {
      emblaApi.off("select", onSelect);
      emblaApi.off("reInit", onSelect);
      emblaApi.off("pointerDown", onPointerDown);
      emblaApi.off("pointerUp", onPointerUp);
      autoplayPlugin?.stop();
    };
  }, [emblaApi, onSelect, autoplayPlugin, reducedMotion]);

  const scrollPrev = useCallback(() => emblaApi?.scrollPrev(), [emblaApi]);
  const scrollNext = useCallback(() => emblaApi?.scrollNext(), [emblaApi]);

  const slideStyle = (index: number): CSSProperties => {
    const offset = index - selectedIndex;
    const abs = Math.min(Math.abs(offset), 3);

    if (reducedMotion) {
      if (offset === 0) return { zIndex: 2, transform: `scale(${1 + (contract.activeScale - 1) * 0.35})` };
      return { opacity: Math.max(0.8, 1 - abs * 0.06) };
    }

    if (isDepth) {
      if (offset === 0) {
        return {
          transform: `translate3d(0, 0, ${contract.translateZActive}px) scale(${contract.activeScale})`,
          zIndex: 5,
          boxShadow:
            variant === "cinematic-plus"
              ? "0 18px 36px rgba(15, 23, 42, 0.18)"
              : "0 12px 28px rgba(15, 23, 42, 0.12)",
        };
      }
      // Active stays front-facing; only neighbors rotate (LOCK-SF-386/388).
      const rotate = (offset > 0 ? 1 : -1) * contract.rotateYDeg * Math.min(abs, 2);
      const scale = Math.max(0.9, 1 - abs * contract.neighborScaleStep);
      const opacity = Math.max(0.62, 1 - abs * contract.neighborOpacityStep);
      const xShift =
        variant === "cinematic-plus" ? (offset > 0 ? 6 : -6) * abs : (offset > 0 ? 3 : -3) * abs;
      return {
        transform: `translate3d(${xShift}px, ${abs * 3}px, ${-abs * contract.translateZStep}px) rotateY(${rotate}deg) scale(${scale})`,
        opacity,
        zIndex: 4 - abs,
        boxShadow: abs === 1 ? "0 8px 20px rgba(15, 23, 42, 0.1)" : undefined,
      };
    }

    if (variant === "sunny") {
      if (offset === 0) {
        return {
          transform: `translate3d(0, -${contract.elevateActivePx}px, 0) scale(${contract.activeScale})`,
          zIndex: 3,
        };
      }
      return {
        transform: `scale(${Math.max(0.97, 1 - abs * contract.neighborScaleStep)})`,
        opacity: Math.max(0.88, 1 - abs * contract.neighborOpacityStep),
      };
    }

    if (variant === "money") {
      if (offset === 0) {
        return {
          transform: `scale(${contract.activeScale})`,
          zIndex: 3,
        };
      }
      return {
        transform: `scale(${Math.max(0.96, 1 - abs * contract.neighborScaleStep)})`,
        opacity: Math.max(0.78, 1 - abs * contract.neighborOpacityStep),
      };
    }

    // Explorer — positional asymmetry / peek only; active upright (LOCK-SF-389).
    if (offset === 0) {
      return {
        transform: `translate3d(0, 0, 0) scale(${contract.activeScale})`,
        zIndex: 3,
      };
    }
    if (offset === 1) {
      return {
        transform: "translate3d(-10px, 6px, 0) scale(0.98)",
        opacity: 0.94,
        zIndex: 2,
      };
    }
    if (offset === -1) {
      return {
        transform: "translate3d(14px, 2px, 0) scale(0.97)",
        opacity: 0.86,
        zIndex: 1,
      };
    }
    if (offset > 1) {
      return {
        transform: `translate3d(${-12 * abs}px, ${4 * abs}px, 0) scale(${0.96 - abs * 0.01})`,
        opacity: Math.max(0.7, 0.9 - abs * 0.08),
      };
    }
    return {
      transform: `translate3d(${14 * abs}px, ${2 * abs}px, 0) scale(0.95)`,
      opacity: 0.72,
    };
  };

  if (products.length === 0) return null;

  const edgeMask = contract.edgeFade
    ? ({
        maskImage: "linear-gradient(to left, transparent 0%, #000 5%, #000 95%, transparent 100%)",
        WebkitMaskImage: "linear-gradient(to left, transparent 0%, #000 5%, #000 95%, transparent 100%)",
      } as CSSProperties)
    : undefined;

  const navBtnClass = contract.navCompact
    ? "inline-flex h-9 w-9 items-center justify-center rounded-lg border border-amber-200 bg-amber-50 text-amber-900 disabled:opacity-40"
    : "inline-flex h-11 w-11 items-center justify-center rounded-xl border border-gray-200 bg-surface text-gray-700 disabled:opacity-40";

  return (
    <section
      ref={rootRef}
      className={`w-full bg-section-surface ${contract.sectionClass}`}
      data-testid={contract.testId}
      data-product-layout={variant}
      data-product-showcase-rail="embla"
      data-product-showcase-autoplay={autoplayAllowed ? "on" : "off"}
      data-product-showcase-autoplay-delay={String(contract.autoplayDelayMs)}
      data-storefront-surface-role="section"
      onMouseEnter={() => {
        if (autoplayAllowed) autoplayPlugin?.stop();
      }}
      onMouseLeave={() => {
        if (autoplayAllowed) autoplayPlugin?.play();
      }}
      onFocusCapture={() => {
        if (autoplayAllowed) autoplayPlugin?.stop();
      }}
      onBlurCapture={(event) => {
        if (!autoplayAllowed) return;
        const next = event.relatedTarget as Node | null;
        if (next && rootRef.current?.contains(next)) return;
        autoplayPlugin?.play();
      }}
      style={{
        perspective: isDepth && !reducedMotion ? contract.perspective : undefined,
      }}
    >
      <div className="relative w-full px-2 sm:px-4 py-8 md:py-10">
        {contract.vignette ? (
          <div
            aria-hidden
            className="pointer-events-none absolute inset-x-0 top-16 bottom-8 bg-[radial-gradient(ellipse_at_center,transparent_48%,rgba(15,23,42,0.08)_100%)]"
            data-rail-vignette="1"
          />
        ) : null}
        <div className="relative mb-4 flex items-center justify-between gap-3">
          <h2 id={headingId} className="flex min-w-0 items-center gap-2 text-lg font-bold text-gray-900 md:text-xl">
            <span
              className={`h-5 w-1 shrink-0 rounded-full ${variant === "money" ? "w-1.5" : ""}`}
              style={{
                backgroundColor:
                  variant === "sunny"
                    ? "#f59e0b"
                    : variant === "money"
                      ? "#ea580c"
                      : variant === "explorer"
                        ? "#0284c7"
                        : STOREFRONT_ACCENT,
              }}
            />
            <span className="truncate">{sectionTitle}</span>
          </h2>
          <div className="flex items-center gap-2">
            <div className="hidden items-center gap-1 sm:flex" role="group" aria-label="ناوبری ردیف کالا">
              <button
                type="button"
                className={navBtnClass}
                onClick={scrollNext}
                disabled={!canNext && !contract.loop}
                aria-label="قبلی"
                data-rail-nav="prev"
              >
                <ChevronRight className={contract.navCompact ? "h-3.5 w-3.5" : "h-4 w-4"} />
              </button>
              <button
                type="button"
                className={navBtnClass}
                onClick={scrollPrev}
                disabled={!canPrev && !contract.loop}
                aria-label="بعدی"
                data-rail-nav="next"
              >
                <ChevronLeft className={contract.navCompact ? "h-3.5 w-3.5" : "h-4 w-4"} />
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

        {/* Decorative overlays must stay outside emblaRef — Embla uses viewport's first child as container.
            No traveling edge-sweep glow (LOCK-SF-387); peek cue is static positional only. */}
        <div className="relative" data-rail-asymmetry={contract.asymmetry ? "1" : "0"}>
          {contract.asymmetry ? (
            <div
              aria-hidden
              className="pointer-events-none absolute inset-y-0 end-0 z-10 w-8 md:w-14 bg-gradient-to-r from-transparent to-sky-100/50"
              data-explorer-peek-cue="1"
            />
          ) : null}
          <div
            className={`relative overflow-hidden ${contract.viewportClass}`}
            style={edgeMask}
            ref={emblaRef}
            data-embla-viewport=""
          >
            <div
              className={`flex touch-pan-y ${contract.gapClass}`}
              data-embla-container=""
              style={{
                transformStyle: isDepth && !reducedMotion ? "preserve-3d" : undefined,
                minHeight: isDepth ? "18rem" : "16rem",
              }}
            >
              {products.map((card, index) => (
                <div
                  key={`${contract.testId}-${card.productId}`}
                  className={`min-w-0 shrink-0 grow-0 ${contract.slideBasis} transition-[transform,opacity,filter,box-shadow] duration-700 ease-[cubic-bezier(0.22,1,0.36,1)]`}
                  data-embla-slide=""
                  data-slide-index={index}
                  data-slide-active={index === selectedIndex ? "true" : "false"}
                  style={{
                    ...slideStyle(index),
                    backfaceVisibility: "hidden",
                  }}
                >
                  <div
                    className={`h-full ${
                      contract.warmGlow && index === selectedIndex
                        ? "rounded-2xl bg-amber-50/40 p-1 shadow-[0_0_0_1px_rgba(251,191,36,0.22)]"
                        : ""
                    } ${
                      contract.commerceRing && index === selectedIndex
                        ? "rounded-xl ring-2 ring-orange-300/70 shadow-sm"
                        : ""
                    } ${
                      isDepth && index === selectedIndex ? "rounded-2xl" : ""
                    } ${
                      contract.asymmetry && index === selectedIndex
                        ? "rounded-2xl border border-sky-100/90 bg-sky-50/25 p-0.5"
                        : ""
                    }`}
                  >
                    <StorefrontProductCardView card={card} previewLocale={previewLocale} />
                  </div>
                </div>
              ))}
            </div>
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

export function productShowcaseEmblaContract(variant: ProductShowcaseEmblaVariant): RailContract {
  return VARIANT_CONTRACTS[variant];
}
