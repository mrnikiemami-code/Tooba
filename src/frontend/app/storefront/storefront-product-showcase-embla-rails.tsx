"use client";

/**
 * Product Showcase Embla rails — additive variants only (LOCK-SF-375…390).
 * Sunny/Money/Cinematic: Embla rails. Cinematic Plus: cards deck.
 * Explorer: Beauty Cosmetics–style editorial peek rail (no per-slide transform).
 */

import { useCallback, useEffect, useMemo, useRef, useState, type CSSProperties, type PointerEvent as ReactPointerEvent } from "react";
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
  align: "start" | "center" | "end";
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
    designNameFa: "آفتابی",
    testId: "product-showcase-sunny",
    defaultTitle: "آفتابی",
    defaultHref: "/products",
    align: "start",
    loop: true,
    dragFree: false,
    duration: 34,
    autoplayDelayMs: 5600,
    slideBasis: "basis-[58%] sm:basis-[38%] md:basis-[25%] lg:basis-[20%]",
    gapClass: "gap-3 md:gap-4",
    sectionClass: "bg-gradient-to-b from-amber-50/60 via-section-surface to-section-surface",
    viewportClass: "py-3",
    perspective: undefined,
    edgeFade: false,
    asymmetry: false,
    navCompact: true,
    activeScale: 1.04,
    neighborScaleStep: 0.015,
    rotateYDeg: 0,
    translateZActive: 0,
    translateZStep: 0,
    elevateActivePx: 6,
    neighborOpacityStep: 0.04,
    warmGlow: true,
    commerceRing: false,
    vignette: false,
  },
  money: {
    designNameFa: "پولی",
    testId: "product-showcase-money",
    defaultTitle: "پولی",
    defaultHref: "/products",
    align: "start",
    loop: true,
    dragFree: false,
    duration: 32,
    autoplayDelayMs: 5400,
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
    duration: 42,
    autoplayDelayMs: 6500,
    slideBasis: "basis-[58%] sm:basis-[38%] md:basis-[25%] lg:basis-[20%]",
    gapClass: "gap-6 md:gap-8",
    sectionClass: "bg-gradient-to-b from-slate-100/70 via-section-surface to-section-surface",
    viewportClass: "py-5 md:py-6",
    perspective: 1400,
    edgeFade: true,
    asymmetry: false,
    navCompact: false,
    activeScale: 1.0,
    neighborScaleStep: 0.06,
    rotateYDeg: 16,
    translateZActive: 12,
    translateZStep: 28,
    elevateActivePx: 0,
    neighborOpacityStep: 0.14,
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
    duration: 36,
    autoplayDelayMs: 7000,
    // Same commercial card width — deck via absolute stack, not rail/size
    slideBasis: "w-[58%] sm:w-[38%] md:w-[25%] lg:w-[20%]",
    gapClass: "gap-0",
    sectionClass: "bg-gradient-to-b from-slate-200/50 via-section-surface to-section-surface",
    viewportClass: "py-8 md:py-10",
    perspective: undefined,
    edgeFade: false,
    asymmetry: false,
    navCompact: false,
    activeScale: 1.0,
    neighborScaleStep: 0.025,
    rotateYDeg: 0,
    translateZActive: 0,
    translateZStep: 0,
    elevateActivePx: 0,
    neighborOpacityStep: 0.12,
    warmGlow: false,
    commerceRing: false,
    vignette: false,
  },
  explorer: {
    designNameFa: "کاشف",
    testId: "product-showcase-explorer",
    defaultTitle: "کاشف",
    defaultHref: "/products",
    // Beauty peek rail; transparent shell so user/theme section background shows
    align: "start",
    loop: true,
    dragFree: false,
    duration: 28,
    autoplayDelayMs: 5200,
    // Banner + slides share column width; a bit wider, height capped on banner
    slideBasis: "basis-[13.5rem] sm:basis-[15rem] md:basis-[16.5rem] lg:basis-[18rem]",
    gapClass: "gap-3 md:gap-4",
    sectionClass: "",
    viewportClass: "py-0",
    perspective: undefined,
    edgeFade: false,
    asymmetry: false,
    navCompact: false,
    activeScale: 1,
    neighborScaleStep: 0,
    rotateYDeg: 0,
    translateZActive: 0,
    translateZStep: 0,
    elevateActivePx: 0,
    neighborOpacityStep: 0,
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

/** Shortest signed distance on a circular index ring (seamless end→start). */
function circularOffset(index: number, selected: number, len: number): number {
  if (len <= 0) return 0;
  let d = index - selected;
  const half = Math.floor(len / 2);
  if (d > half) d -= len;
  if (d < -half) d += len;
  return d;
}

/**
 * Explorer side banner: same rem column as product cards; height via flex stretch.
 * No post-paint resize (avoids F5 large→small flash). Grab/chevron to collapse.
 */
function ExplorerCollapsibleBanner({
  imageUrl,
  href,
  widthClass,
}: {
  imageUrl: string;
  href?: string;
  widthClass: string;
}) {
  const [collapsed, setCollapsed] = useState(false);
  const [dragX, setDragX] = useState(0);
  const [dragging, setDragging] = useState(false);
  const startX = useRef<number | null>(null);
  const moved = useRef(false);

  const finishDrag = useCallback(
    (clientX: number) => {
      const start = startX.current;
      startX.current = null;
      setDragging(false);
      setDragX(0);
      if (start == null) return;
      const dx = clientX - start;
      if (collapsed) {
        if (dx < -40) setCollapsed(false);
      } else if (dx > 48) {
        setCollapsed(true);
      }
    },
    [collapsed],
  );

  const onPointerDown = (e: ReactPointerEvent<HTMLDivElement>) => {
    if (e.button !== 0) return;
    startX.current = e.clientX;
    moved.current = false;
    setDragging(true);
    setDragX(0);
    e.currentTarget.setPointerCapture(e.pointerId);
  };

  const onPointerMove = (e: ReactPointerEvent<HTMLDivElement>) => {
    if (startX.current == null) return;
    const dx = e.clientX - startX.current;
    if (Math.abs(dx) > 6) moved.current = true;
    if (collapsed) setDragX(Math.min(0, dx * 0.55));
    else setDragX(Math.max(0, dx * 0.55));
  };

  const onPointerUp = (e: ReactPointerEvent<HTMLDivElement>) => {
    if (e.currentTarget.hasPointerCapture(e.pointerId)) {
      e.currentTarget.releasePointerCapture(e.pointerId);
    }
    finishDrag(e.clientX);
  };

  const onPointerCancel = (e: ReactPointerEvent<HTMLDivElement>) => {
    if (e.currentTarget.hasPointerCapture(e.pointerId)) {
      e.currentTarget.releasePointerCapture(e.pointerId);
    }
    startX.current = null;
    setDragging(false);
    setDragX(0);
  };

  return (
    <div
      className={`relative z-20 shrink-0 grow-0 self-stretch pe-1 ps-0.5 ${collapsed ? "w-9" : widthClass}`}
      data-explorer-side-banner-slot=""
      data-explorer-banner-collapsed={collapsed ? "1" : "0"}
      onPointerDown={onPointerDown}
      onPointerMove={onPointerMove}
      onPointerUp={onPointerUp}
      onPointerCancel={onPointerCancel}
      onClickCapture={(e) => {
        if (moved.current) {
          e.preventDefault();
          e.stopPropagation();
          moved.current = false;
          return;
        }
        if (collapsed) {
          e.preventDefault();
          e.stopPropagation();
          setCollapsed(false);
        }
      }}
      style={{
        transform: `translate3d(${dragX}px, 0, 0)`,
        transition: dragging ? "none" : "transform 420ms cubic-bezier(0.22, 1, 0.36, 1)",
        cursor: dragging ? "grabbing" : "grab",
        touchAction: "none",
      }}
    >
      {collapsed ? (
        <div
          className="flex h-full min-h-[16rem] w-full flex-col items-center justify-center gap-1 rounded-xl border border-black/10 bg-surface/95 shadow-[0_14px_28px_-8px_rgba(15,23,42,0.3)] ring-1 ring-black/5"
          data-explorer-side-banner-peek=""
          role="img"
          aria-label="بنر جمع‌شده — بکشید یا بزنید تا باز شود"
        >
          <ChevronLeft className="h-4 w-4 text-gray-600 rtl:rotate-180" aria-hidden />
          <span className="text-[10px] font-bold text-muted [writing-mode:vertical-rl]">بنر</span>
        </div>
      ) : (
        <div
          className="relative h-full min-h-[16rem] w-full overflow-hidden rounded-xl border border-black/10 bg-surface shadow-[0_18px_36px_-10px_rgba(15,23,42,0.32),0_8px_14px_-6px_rgba(15,23,42,0.16)] ring-1 ring-black/5"
          data-explorer-side-banner=""
        >
          {href?.trim() ? (
            <Link href={href.trim()} className="absolute inset-0 block" aria-label="بنر بخش">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={imageUrl} alt="" className="absolute inset-0 h-full w-full object-cover" draggable={false} />
            </Link>
          ) : (
            // eslint-disable-next-line @next/next/no-img-element
            <img src={imageUrl} alt="" className="absolute inset-0 h-full w-full object-cover" draggable={false} />
          )}
          <button
            type="button"
            className="absolute inset-y-0 end-0 z-30 flex w-9 items-center justify-center bg-gradient-to-r from-transparent to-black/35"
            data-explorer-banner-swipe-cue=""
            aria-label="جمع کردن بنر"
            onPointerDown={(e) => {
              e.stopPropagation();
            }}
            onClick={(e) => {
              e.preventDefault();
              e.stopPropagation();
              setCollapsed(true);
            }}
          >
            <span className="flex h-9 w-5 items-center justify-center rounded-full bg-white/95 text-gray-700 shadow-md">
              <ChevronRight className="h-3.5 w-3.5 rtl:rotate-180" />
            </span>
          </button>
        </div>
      )}
    </div>
  );
}

type ShowcaseProps = {
  variant: ProductShowcaseEmblaVariant;
  products: StorefrontProductCard[];
  title?: string;
  href?: string;
  previewLocale?: string;
  enableAutoplay?: boolean;
  /** Explorer side banner (Beauty layout) — same visual width as a card. */
  bannerImageUrl?: string;
  bannerHref?: string;
};

type CardsStageKind = "cinematic-plus";

/**
 * In-place card stage (no Embla rail) — cinematic-plus fan deck (accepted).
 */
function ProductShowcaseCardsStage({
  kind = "cinematic-plus",
  products,
  title,
  href,
  previewLocale = "fa",
  enableAutoplay = true,
}: Omit<ShowcaseProps, "variant"> & { kind?: CardsStageKind }) {
  const contract = VARIANT_CONTRACTS["cinematic-plus"];
  const sectionTitle = title?.trim() || contract.defaultTitle;
  const sectionHref = href?.trim() || contract.defaultHref;
  const headingId = `${contract.testId}-heading`;
  const reducedMotion = useReducedMotionFlag();
  const rootRef = useRef<HTMLElement | null>(null);
  const hoverPaused = useRef(false);
  const dragPaused = useRef(false);
  const pointerStartX = useRef<number | null>(null);
  const dragMoved = useRef(false);
  const autoplayAllowed = enableAutoplay && !reducedMotion;

  const [selectedIndex, setSelectedIndex] = useState(0);
  const [dragX, setDragX] = useState(0);
  const [dragging, setDragging] = useState(false);
  const len = products.length;

  const go = useCallback(
    (delta: number) => {
      if (len <= 0) return;
      setSelectedIndex((i) => ((i + delta) % len + len) % len);
    },
    [len],
  );

  const scrollPrev = useCallback(() => go(1), [go]);
  const scrollNext = useCallback(() => go(-1), [go]);

  useEffect(() => {
    if (!autoplayAllowed || len < 2) return;
    const id = window.setInterval(() => {
      if (hoverPaused.current || dragPaused.current) return;
      setSelectedIndex((i) => (i + 1) % len);
    }, contract.autoplayDelayMs);
    return () => window.clearInterval(id);
  }, [autoplayAllowed, len, contract.autoplayDelayMs]);

  const finishDrag = useCallback(
    (clientX: number) => {
      const start = pointerStartX.current;
      pointerStartX.current = null;
      dragPaused.current = false;
      setDragging(false);
      if (start == null) {
        setDragX(0);
        return;
      }
      const dx = clientX - start;
      setDragX(0);
      if (Math.abs(dx) < 48) return;
      if (dx > 0) go(-1);
      else go(1);
    },
    [go],
  );

  const onPointerDown = (e: ReactPointerEvent<HTMLDivElement>) => {
    if (e.button !== 0) return;
    pointerStartX.current = e.clientX;
    dragMoved.current = false;
    dragPaused.current = true;
    setDragging(true);
    setDragX(0);
    e.currentTarget.setPointerCapture(e.pointerId);
  };

  const onPointerMove = (e: ReactPointerEvent<HTMLDivElement>) => {
    if (pointerStartX.current == null) return;
    const dx = e.clientX - pointerStartX.current;
    if (Math.abs(dx) > 6) dragMoved.current = true;
    setDragX(dx * 0.55);
  };

  const onPointerUp = (e: ReactPointerEvent<HTMLDivElement>) => {
    if (e.currentTarget.hasPointerCapture(e.pointerId)) {
      e.currentTarget.releasePointerCapture(e.pointerId);
    }
    finishDrag(e.clientX);
  };

  const onPointerCancel = (e: ReactPointerEvent<HTMLDivElement>) => {
    if (e.currentTarget.hasPointerCapture(e.pointerId)) {
      e.currentTarget.releasePointerCapture(e.pointerId);
    }
    pointerStartX.current = null;
    dragPaused.current = false;
    setDragging(false);
    setDragX(0);
  };

  const cardStyle = (index: number): CSSProperties => {
    const offset = circularOffset(index, selectedIndex, len);
    const abs = Math.abs(offset);
    const transition = dragging
      ? "none"
      : "transform 520ms cubic-bezier(0.22, 1, 0.36, 1), opacity 520ms ease-out, box-shadow 520ms ease-out";

    if (reducedMotion) {
      if (offset === 0) return { zIndex: 10, transform: "translate3d(0, 0, 0) scale(1)" };
      return { opacity: 0, visibility: "hidden", zIndex: 0 };
    }

    if (abs > 3) {
      return {
        opacity: 0,
        pointerEvents: "none",
        transform: "translate3d(0, 24px, 0) scale(0.9)",
        zIndex: 0,
        visibility: "hidden",
      };
    }

    if (offset === 0) {
      const grabRotate = dragging ? dragX * 0.04 : 0;
      return {
        transform: `translate3d(${dragX}px, 0, 0) rotate(${grabRotate}deg) scale(1)`,
        zIndex: 10,
        opacity: 1,
        boxShadow: "0 18px 36px rgba(15, 23, 42, 0.16)",
        pointerEvents: "auto",
        transition,
        cursor: dragging ? "grabbing" : "grab",
      };
    }

    const dir = offset > 0 ? 1 : -1;
    const stack = Math.min(abs, 3);
    const follow = dragging ? dragX * (0.18 / stack) : 0;
    return {
      transform: `translate3d(${dir * stack * 14 + follow}px, ${stack * 10}px, 0) rotate(${dir * stack * 6}deg) scale(${Math.max(0.92, 1 - stack * contract.neighborScaleStep)})`,
      opacity: Math.max(0.5, 1 - stack * contract.neighborOpacityStep),
      zIndex: 10 - stack,
      boxShadow: "0 10px 22px rgba(15, 23, 42, 0.1)",
      pointerEvents: "none",
      transition,
    };
  };

  if (len === 0) return null;

  const navBtnClass =
    "inline-flex h-11 w-11 items-center justify-center rounded-xl border border-gray-200 bg-surface text-gray-700";

  return (
    <section
      ref={rootRef}
      className={`w-full bg-section-surface ${contract.sectionClass}`}
      data-testid={contract.testId}
      data-product-layout={kind}
      data-product-showcase-rail="cards-deck"
      data-product-showcase-autoplay={autoplayAllowed ? "on" : "off"}
      data-product-showcase-autoplay-delay={String(contract.autoplayDelayMs)}
      data-storefront-surface-role="section"
      onMouseEnter={() => {
        hoverPaused.current = true;
      }}
      onMouseLeave={() => {
        hoverPaused.current = false;
      }}
      onFocusCapture={() => {
        hoverPaused.current = true;
      }}
      onBlurCapture={(event) => {
        const next = event.relatedTarget as Node | null;
        if (next && rootRef.current?.contains(next)) return;
        hoverPaused.current = false;
      }}
    >
      <div className="relative w-full px-2 sm:px-4 py-8 md:py-10">
        <div className="relative mb-4 flex items-center justify-between gap-3">
          <h2 id={headingId} className="flex min-w-0 items-center gap-2 text-lg font-bold text-gray-900 md:text-xl">
            <span className="h-5 w-1 shrink-0 rounded-full" style={{ backgroundColor: STOREFRONT_ACCENT }} />
            <span className="truncate">{sectionTitle}</span>
          </h2>
          <div className="flex items-center gap-2">
            <div className="hidden items-center gap-1 sm:flex" role="group" aria-label="ناوبری دسته کارت">
              <button type="button" className={navBtnClass} onClick={scrollPrev} aria-label="قبلی" data-rail-nav="prev">
                <ChevronRight className="h-4 w-4" />
              </button>
              <button type="button" className={navBtnClass} onClick={scrollNext} aria-label="بعدی" data-rail-nav="next">
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
          className={`relative mx-auto flex justify-center overflow-visible px-4 select-none ${contract.viewportClass}`}
          data-cards-deck-stage=""
          data-rail-cards="1"
          onPointerDown={onPointerDown}
          onPointerMove={onPointerMove}
          onPointerUp={onPointerUp}
          onPointerCancel={onPointerCancel}
          onClickCapture={(e) => {
            if (dragMoved.current) {
              e.preventDefault();
              e.stopPropagation();
              dragMoved.current = false;
            }
          }}
          style={{ touchAction: "pan-y", cursor: dragging ? "grabbing" : "grab" }}
        >
          <div className={`relative ${contract.slideBasis}`} data-cards-deck-anchor="">
            <div className="invisible pointer-events-none" aria-hidden>
              <StorefrontProductCardView card={products[selectedIndex]!} previewLocale={previewLocale} />
            </div>
            {products.map((card, index) => (
              <div
                key={`${contract.testId}-${card.productId}`}
                className="absolute inset-x-0 top-0"
                data-cards-deck-slide=""
                data-slide-index={index}
                data-slide-active={index === selectedIndex ? "true" : "false"}
                style={cardStyle(index)}
              >
                <div className={index === selectedIndex ? "rounded-2xl" : undefined}>
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

function ProductShowcaseEmblaRailInner({
  variant,
  products,
  title,
  href,
  previewLocale = "fa",
  enableAutoplay = true,
  bannerImageUrl,
  bannerHref,
}: ShowcaseProps) {
  const contract = VARIANT_CONTRACTS[variant];
  const sectionTitle = title?.trim() || contract.defaultTitle;
  const sectionHref = href?.trim() || contract.defaultHref;
  const headingId = `${contract.testId}-heading`;
  const reducedMotion = useReducedMotionFlag();
  const isDepth = variant === "cinematic";
  const isBeauty = variant === "explorer";
  const rootRef = useRef<HTMLElement | null>(null);
  const autoplayAllowed = enableAutoplay && !reducedMotion;

  const autoplayPlugin = useMemo(() => {
    if (!autoplayAllowed) return null;
    return Autoplay({
      delay: contract.autoplayDelayMs,
      stopOnInteraction: false,
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
          filter: "none",
          boxShadow: "0 14px 32px rgba(15, 23, 42, 0.14)",
        };
      }
      const rotate = (offset > 0 ? 1 : -1) * contract.rotateYDeg * Math.min(abs, 2);
      const scale = Math.max(0.86, 1 - abs * contract.neighborScaleStep);
      const opacity = Math.max(0.55, 1 - abs * contract.neighborOpacityStep);
      const xShift = (offset > 0 ? 10 : -10) * abs;
      const gray = Math.min(0.85, 0.35 + abs * 0.28);
      return {
        transform: `translate3d(${xShift}px, ${abs * 8}px, ${-abs * contract.translateZStep}px) rotateY(${rotate}deg) scale(${scale})`,
        opacity,
        zIndex: 4 - abs,
        filter: `grayscale(${gray})`,
        boxShadow: abs === 1 ? "0 10px 24px rgba(15, 23, 42, 0.12)" : undefined,
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

    if (isBeauty) {
      // Beauty Cosmetics: no per-slide transform — Embla alone scrolls (avoids glitch).
      return {};
    }

    if (offset === 0) {
      return {
        transform: `translate3d(0, 0, 0) scale(${contract.activeScale})`,
        zIndex: 3,
      };
    }
    return {
      transform: `scale(${Math.max(0.96, 1 - abs * 0.02)})`,
      opacity: Math.max(0.75, 1 - abs * 0.08),
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
      className={`w-full ${isBeauty ? "bg-transparent" : `bg-section-surface ${contract.sectionClass}`}`}
      data-testid={contract.testId}
      data-product-layout={variant}
      data-product-showcase-rail={isBeauty ? "beauty" : "embla"}
      data-product-showcase-autoplay={autoplayAllowed ? "on" : "off"}
      data-product-showcase-autoplay-delay={String(contract.autoplayDelayMs)}
      data-storefront-surface-role={isBeauty ? "inherit" : "section"}
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
        perspectiveOrigin: isDepth ? "50% 45%" : undefined,
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
              className={`h-5 w-1 shrink-0 rounded-full ${variant === "money" ? "w-1.5" : ""} ${isBeauty ? "rounded-sm" : ""}`}
              style={{
                backgroundColor:
                  variant === "sunny"
                    ? "#f59e0b"
                    : variant === "money"
                      ? "#ea580c"
                      : STOREFRONT_ACCENT,
              }}
            />
            <span className={`truncate ${isBeauty ? "font-semibold tracking-wide" : ""}`}>
              {sectionTitle}
            </span>
          </h2>
          <div className="flex items-center gap-2">
            <div className="hidden items-center gap-1 sm:flex" role="group" aria-label="ناوبری ردیف کالا">
              <button
                type="button"
                className={
                  isBeauty
                    ? "inline-flex h-11 w-11 items-center justify-center rounded-full border border-gray-200 bg-surface text-gray-700 disabled:opacity-40"
                    : navBtnClass
                }
                onClick={scrollNext}
                disabled={!canNext && !contract.loop}
                aria-label="قبلی"
                data-rail-nav="prev"
              >
                <ChevronRight className={contract.navCompact ? "h-3.5 w-3.5" : "h-4 w-4"} />
              </button>
              <button
                type="button"
                className={
                  isBeauty
                    ? "inline-flex h-11 w-11 items-center justify-center rounded-full border border-gray-200 bg-surface text-gray-700 disabled:opacity-40"
                    : navBtnClass
                }
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

        <div
          className={
            isBeauty && bannerImageUrl?.trim()
              ? "relative flex items-stretch gap-3 md:gap-4"
              : "relative"
          }
          data-rail-asymmetry={contract.asymmetry ? "1" : "0"}
          data-explorer-banner={isBeauty && bannerImageUrl?.trim() ? "1" : "0"}
        >
          {contract.asymmetry ? (
            <div
              aria-hidden
              className="pointer-events-none absolute inset-y-0 end-0 z-10 w-8 md:w-14 bg-gradient-to-r from-transparent to-sky-100/50"
              data-explorer-peek-cue="1"
            />
          ) : null}
          {isBeauty && bannerImageUrl?.trim() ? (
            <ExplorerCollapsibleBanner
              imageUrl={bannerImageUrl.trim()}
              href={bannerHref}
              widthClass={contract.slideBasis}
            />
          ) : null}
          <div
            className={
              isBeauty && bannerImageUrl?.trim()
                ? "relative z-0 min-w-0 flex-1"
                : "relative"
            }
          >
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
                    className={`min-w-0 shrink-0 grow-0 ${contract.slideBasis} ${
                      isDepth
                        ? "transition-[opacity,filter,box-shadow] duration-500 ease-out"
                        : isBeauty
                          ? ""
                          : "transition-[transform,opacity,filter,box-shadow] duration-700 ease-[cubic-bezier(0.22,1,0.36,1)]"
                    }`}
                    data-embla-slide=""
                    data-slide-index={index}
                    data-slide-active={index === selectedIndex ? "true" : "false"}
                    style={{
                      ...slideStyle(index),
                      backfaceVisibility: "hidden",
                      transformStyle: isDepth && !reducedMotion ? "preserve-3d" : undefined,
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
                      } ${isDepth && index === selectedIndex ? "rounded-2xl" : ""} ${
                        isBeauty
                          ? "rounded-xl bg-surface/80 p-0.5 shadow-sm ring-1 ring-black/5"
                          : ""
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
      </div>
    </section>
  );
}

export function ProductShowcaseEmblaRail(props: ShowcaseProps) {
  if (props.variant === "cinematic-plus") {
    return <ProductShowcaseCardsStage kind="cinematic-plus" {...props} />;
  }
  return <ProductShowcaseEmblaRailInner {...props} />;
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
