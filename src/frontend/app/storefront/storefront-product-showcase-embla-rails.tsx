"use client";

/**
 * Product Showcase Embla rails — additive variants only (LOCK-SF-375…385).
 * One shared rail engine + variant-driven composition/motion metadata.
 * Reuses StorefrontProductCardView unchanged; Embla + Autoplay scoped to these five keys.
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
    duration: 28,
    autoplayDelayMs: 5000,
    slideBasis: "basis-[58%] sm:basis-[38%] md:basis-[26%] lg:basis-[20%]",
    gapClass: "gap-4 md:gap-5",
    sectionClass: "bg-gradient-to-b from-amber-50/50 via-section-surface to-section-surface",
    viewportClass: "py-3",
    perspective: undefined,
    edgeFade: true,
    asymmetry: false,
    navCompact: false,
    activeScale: 1.06,
    neighborScaleStep: 0.02,
    rotateYDeg: 0,
    translateZActive: 0,
    translateZStep: 0,
    elevateActivePx: 10,
    neighborOpacityStep: 0.04,
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
    duration: 16,
    autoplayDelayMs: 3600,
    slideBasis: "basis-[64%] sm:basis-[42%] md:basis-[28%] lg:basis-[22%]",
    gapClass: "gap-2 md:gap-2.5",
    sectionClass: "bg-gradient-to-b from-orange-50/70 via-amber-50/30 to-section-surface",
    viewportClass: "py-2",
    perspective: undefined,
    edgeFade: false,
    asymmetry: false,
    navCompact: true,
    activeScale: 1.1,
    neighborScaleStep: 0.05,
    rotateYDeg: 0,
    translateZActive: 0,
    translateZStep: 0,
    elevateActivePx: 0,
    neighborOpacityStep: 0.1,
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
    duration: 32,
    autoplayDelayMs: 6000,
    slideBasis: "basis-[72%] sm:basis-[48%] md:basis-[34%] lg:basis-[28%]",
    gapClass: "gap-1 md:gap-2",
    sectionClass: "bg-gradient-to-b from-slate-100/80 via-section-surface to-section-surface",
    viewportClass: "py-6 md:py-8",
    perspective: 1100,
    edgeFade: true,
    asymmetry: false,
    navCompact: false,
    activeScale: 1.08,
    neighborScaleStep: 0.06,
    rotateYDeg: 4.5,
    translateZActive: 36,
    translateZStep: 22,
    elevateActivePx: 0,
    neighborOpacityStep: 0.12,
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
    duration: 40,
    autoplayDelayMs: 6800,
    slideBasis: "basis-[78%] sm:basis-[52%] md:basis-[36%] lg:basis-[30%]",
    gapClass: "gap-0 md:gap-1",
    sectionClass: "bg-gradient-to-b from-slate-200/70 via-slate-50/40 to-section-surface",
    viewportClass: "py-8 md:py-10",
    perspective: 1400,
    edgeFade: true,
    asymmetry: false,
    navCompact: false,
    activeScale: 1.14,
    neighborScaleStep: 0.09,
    rotateYDeg: 6,
    translateZActive: 56,
    translateZStep: 34,
    elevateActivePx: 0,
    neighborOpacityStep: 0.16,
    warmGlow: false,
    commerceRing: false,
    vignette: true,
  },
  explorer: {
    designNameFa: "کاشف",
    testId: "product-showcase-explorer",
    defaultTitle: "کاشف",
    defaultHref: "/products",
    align: "center",
    loop: true,
    dragFree: false,
    duration: 24,
    autoplayDelayMs: 4500,
    slideBasis: "basis-[62%] sm:basis-[40%] md:basis-[27%] lg:basis-[21%]",
    gapClass: "gap-3 md:gap-4",
    sectionClass: "bg-gradient-to-l from-sky-50/50 via-section-surface to-section-surface",
    viewportClass: "py-2 ps-1 pe-8 md:pe-16",
    perspective: undefined,
    edgeFade: false,
    asymmetry: true,
    navCompact: false,
    activeScale: 1.02,
    neighborScaleStep: 0.01,
    rotateYDeg: 0,
    translateZActive: 0,
    translateZStep: 0,
    elevateActivePx: 0,
    neighborOpacityStep: 0.02,
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
      return { opacity: Math.max(0.75, 1 - abs * 0.08) };
    }

    if (isDepth) {
      if (offset === 0) {
        return {
          transform: `translate3d(0, 0, ${contract.translateZActive}px) scale(${contract.activeScale})`,
          zIndex: 5,
          boxShadow:
            variant === "cinematic-plus"
              ? "0 28px 48px rgba(15, 23, 42, 0.22)"
              : "0 20px 40px rgba(15, 23, 42, 0.16)",
        };
      }
      // RTL: positive index is visually to the left; flip rotate sign for natural stage.
      const rotate = (offset > 0 ? 1 : -1) * contract.rotateYDeg * Math.min(abs, 2);
      const scale = Math.max(0.72, 1 - abs * contract.neighborScaleStep);
      const opacity = Math.max(0.45, 1 - abs * contract.neighborOpacityStep);
      const xShift =
        variant === "cinematic-plus" ? (offset > 0 ? 10 : -10) * abs : (offset > 0 ? 4 : -4) * abs;
      return {
        transform: `translate3d(${xShift}px, ${abs * 6}px, ${-abs * contract.translateZStep}px) rotateY(${rotate}deg) scale(${scale})`,
        opacity,
        zIndex: 4 - abs,
        boxShadow: abs === 1 ? "0 12px 28px rgba(15, 23, 42, 0.12)" : undefined,
        filter: abs >= 2 ? "brightness(0.92)" : undefined,
      };
    }

    if (variant === "sunny") {
      if (offset === 0) {
        return {
          transform: `translate3d(0, -${contract.elevateActivePx}px, 0) scale(${contract.activeScale})`,
          zIndex: 3,
          filter: "drop-shadow(0 12px 20px rgba(251, 191, 36, 0.28))",
        };
      }
      return {
        transform: `scale(${1 - abs * contract.neighborScaleStep})`,
        opacity: Math.max(0.82, 1 - abs * contract.neighborOpacityStep),
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
        transform: `scale(${Math.max(0.86, 1 - abs * contract.neighborScaleStep)})`,
        opacity: Math.max(0.55, 1 - abs * contract.neighborOpacityStep),
        filter: abs >= 1 ? "saturate(0.92)" : undefined,
      };
    }

    // Explorer — asymmetrical discovery peeks
    if (offset === 0) {
      return {
        transform: `translate3d(0, 0, 0) scale(${contract.activeScale}) rotate(-0.6deg)`,
        zIndex: 3,
      };
    }
    if (offset === 1) {
      return {
        transform: "translate3d(-6px, 14px, 0) scale(0.94) rotate(1.8deg)",
        opacity: 0.92,
        zIndex: 2,
      };
    }
    if (offset === -1) {
      return {
        transform: "translate3d(12px, -8px, 0) scale(0.9) rotate(-2.2deg)",
        opacity: 0.78,
        zIndex: 1,
      };
    }
    if (offset > 1) {
      return {
        transform: `translate3d(${-8 * abs}px, ${10 * abs}px, 0) scale(${0.88 - abs * 0.02})`,
        opacity: Math.max(0.5, 0.85 - abs * 0.1),
      };
    }
    return {
      transform: `translate3d(${12 * abs}px, ${-4 * abs}px, 0) scale(0.86)`,
      opacity: 0.55,
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
            className="pointer-events-none absolute inset-x-0 top-16 bottom-8 bg-[radial-gradient(ellipse_at_center,transparent_42%,rgba(15,23,42,0.12)_100%)]"
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

        {/* Decorative overlays must stay outside emblaRef — Embla uses viewport's first child as container. */}
        <div className="relative" data-rail-asymmetry={contract.asymmetry ? "1" : "0"}>
          {contract.warmGlow ? (
            <div
              aria-hidden
              className="pointer-events-none absolute inset-y-4 start-0 z-10 w-16 bg-gradient-to-l from-amber-200/40 to-transparent"
              data-sunny-edge-glow="1"
            />
          ) : null}
          {contract.asymmetry ? (
            <div
              aria-hidden
              className="pointer-events-none absolute inset-y-0 end-0 z-10 w-10 md:w-20 bg-gradient-to-r from-transparent to-sky-100/80"
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
                minHeight: isDepth ? "20rem" : "16.5rem",
              }}
            >
              {products.map((card, index) => (
                <div
                  key={`${contract.testId}-${card.productId}`}
                  className={`min-w-0 shrink-0 grow-0 ${contract.slideBasis} transition-[transform,opacity,filter] duration-500 ease-out`}
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
                        ? "rounded-2xl bg-amber-50/70 p-1.5 shadow-[0_0_0_1px_rgba(251,191,36,0.35)]"
                        : contract.warmGlow
                          ? "rounded-2xl bg-surface/70 p-1"
                          : ""
                    } ${
                      contract.commerceRing && index === selectedIndex
                        ? "rounded-xl ring-[3px] ring-orange-400/80 shadow-md"
                        : ""
                    } ${
                      isDepth && index === selectedIndex ? "rounded-2xl" : ""
                    } ${
                      contract.asymmetry && index === selectedIndex
                        ? "rounded-2xl border border-sky-200/80 bg-sky-50/40 p-1"
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
