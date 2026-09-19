"use client";

import { useCallback, useEffect, useRef, useState, type CSSProperties } from "react";
import { LocalizedLink as Link } from "../../lib/i18n/LocalizedLink.tsx";
import { ProductRailSection } from "./storefront-home-blocks.tsx";
import {
  HomeArticlesSection,
  HomeBrandsSection,
  HomeTestimonialsSection,
} from "./storefront-home-repair-sections.tsx";
import type { StorefrontCategoryItem, StorefrontProductCard } from "./storefront-model.ts";
import type { StorefrontLandingSection } from "./storefront-landing-api.ts";
import { StorefrontMenuLinks } from "./storefront-menu-tree.tsx";
import type { StorefrontMenuItem } from "./storefront-menu-api.ts";
import { heightPresetBannerClass } from "../../lib/storefront-composition/size-presets.ts";
import {
  heroDiagonalImageClip,
  heroDiagonalPanelClip,
  heroHeightClass,
  heroHeightPresetDesktopPx,
  heroSplitPanelPercent,
  heroVariantToSwiperEffect,
  readHeroSliderFields,
  resolveHeroSlideHref,
  type HeroSlideConfig,
  type HeroSliderVariantId,
} from "../../lib/storefront-composition/hero-slider-config.ts";
import { storefrontMediaUrl } from "./storefront-api.ts";
import { PreviewSampleBadge } from "./preview-sample-badge.tsx";
import { Swiper, SwiperSlide } from "swiper/react";
import {
  Autoplay,
  EffectCoverflow,
  EffectCreative,
  EffectCube,
  EffectFade,
  Pagination,
} from "swiper/modules";
import "swiper/css";
import "swiper/css/pagination";
import "swiper/css/effect-fade";
import "swiper/css/effect-coverflow";
import "swiper/css/effect-cube";
import "swiper/css/effect-creative";

export function asIds(config: Record<string, unknown>, ...keys: string[]): string[] {
  for (const key of keys) {
    const value = config[key];
    if (Array.isArray(value)) return value.map((item) => String(item));
  }
  return [];
}

export function titleOf(config: Record<string, unknown>, fallback: string): string {
  return typeof config.title === "string" && config.title.trim() ? config.title.trim() : fallback;
}

function slideImageSrc(slide: HeroSlideConfig): string {
  if (slide.mediaAssetId.trim()) return storefrontMediaUrl(slide.mediaAssetId.trim());
  if (slide.imageUrl.trim()) return slide.imageUrl.trim();
  return "";
}

function slideHref(slide: HeroSlideConfig): string {
  return resolveHeroSlideHref(slide) || slide.href.trim();
}

function hasConfiguredSlides(config: Record<string, unknown>): boolean {
  return Array.isArray(config.slides) && config.slides.length > 0;
}

function shellForVariant(variant: HeroSliderVariantId): { shellClass: string; rounded: string } {
  switch (variant) {
    case "shapes":
      return { shellClass: "px-2 sm:px-4 max-w-6xl mx-auto", rounded: "rounded-3xl" };
    case "split":
    case "diagonal":
      return { shellClass: "px-2 sm:px-4", rounded: "rounded-3xl" };
    case "editorial":
    case "cinematic":
    case "fullscreen":
    default:
      return { shellClass: "px-2 sm:px-4", rounded: "rounded-none md:rounded-3xl" };
  }
}

const DIAMOND_CROSSFADE_MS = 700;

function DiamondSlideFace({
  slide,
  overlayClass,
  interactive,
}: {
  slide: HeroSlideConfig;
  overlayClass: string;
  /** فقط لایهٔ رویی لینک/کلیک می‌گیرد تا لایهٔ زیر مزاحم نباشد. */
  interactive: boolean;
}) {
  const src = slideImageSrc(slide) || "/images/sliders/slider-1.jpg";
  const href = slideHref(slide);
  const title = slide.title.trim() || "فروشگاه توبا";
  const cta = slide.ctaLabel.trim();
  const inner = (
    <>
      {/* eslint-disable-next-line @next/next/no-img-element */}
      <img src={src} alt={slide.alt || title} className="h-full w-full object-cover" draggable={false} />
      <div className={`absolute inset-0 ${overlayClass}`} />
      <div className="absolute inset-0 flex flex-col justify-end p-6 text-white md:p-10">
        <h2 className="text-2xl font-black line-clamp-2 md:text-4xl">{title}</h2>
        {slide.description ? (
          <p className="mt-2 max-w-xl text-sm md:text-base line-clamp-2">{slide.description}</p>
        ) : null}
        {cta && href ? (
          <span className="mt-3 inline-flex w-fit rounded-xl bg-surface/95 px-3 py-1.5 text-xs font-bold text-gray-900">
            {cta}
          </span>
        ) : null}
      </div>
    </>
  );

  if (interactive && href) {
    return (
      <Link href={href} className="relative block h-full w-full" aria-label={cta || title} tabIndex={interactive ? 0 : -1}>
        {inner}
      </Link>
    );
  }
  return (
    <div className="relative block h-full w-full" aria-hidden={!interactive}>
      {inner}
    </div>
  );
}

/**
 * الماس: کراس‌فید هم‌زمان — تصویر قبلی آرام محو و تصویر جدید همزمان ظاهر می‌شود (بدون فلش پس‌زمینه).
 */
function HeroDiamondSequential({
  slides,
  autoplay,
  intervalSec,
  heightClass,
  legacyMinHeight,
  rounded,
  overlayClass,
  direction,
}: {
  slides: HeroSlideConfig[];
  autoplay: boolean;
  intervalSec: number;
  heightClass: string;
  legacyMinHeight?: number;
  rounded: string;
  overlayClass: string;
  direction: "rtl" | "ltr";
}) {
  const [active, setActive] = useState(0);
  /** لایهٔ در حال محو شدن؛ null یعنی فقط یک لایه. */
  const [outgoing, setOutgoing] = useState<number | null>(null);
  const [phase, setPhase] = useState<"idle" | "crossfade">("idle");
  const activeRef = useRef(0);
  const busyRef = useRef(false);
  const timerRef = useRef<number | null>(null);
  const pointerX = useRef<number | null>(null);
  const len = slides.length;

  useEffect(() => {
    return () => {
      if (timerRef.current != null) window.clearTimeout(timerRef.current);
    };
  }, []);

  const goTo = useCallback(
    (nextRaw: number) => {
      if (len < 2 || busyRef.current) return;
      const next = ((nextRaw % len) + len) % len;
      if (next === activeRef.current) return;
      busyRef.current = true;
      setOutgoing(activeRef.current);
      activeRef.current = next;
      setActive(next);
      setPhase("idle");
      // دو فریم: ابتدا هر دو لایه در جای خود (قدیمی ۱، جدید ۰)، بعد کراس‌فید
      window.requestAnimationFrame(() => {
        window.requestAnimationFrame(() => {
          setPhase("crossfade");
          if (timerRef.current != null) window.clearTimeout(timerRef.current);
          timerRef.current = window.setTimeout(() => {
            setOutgoing(null);
            setPhase("idle");
            busyRef.current = false;
            timerRef.current = null;
          }, DIAMOND_CROSSFADE_MS);
        });
      });
    },
    [len],
  );

  useEffect(() => {
    if (!autoplay || len < 2) return;
    const delay = Math.max(1000, Math.round(intervalSec * 1000));
    const id = window.setInterval(() => {
      goTo(activeRef.current + 1);
    }, delay);
    return () => window.clearInterval(id);
  }, [autoplay, intervalSec, len, goTo]);

  const fadeStyle = (role: "incoming" | "outgoing"): CSSProperties => {
    const transitioning = phase === "crossfade";
    if (role === "outgoing") {
      return {
        opacity: transitioning ? 0 : 1,
        transition: transitioning ? `opacity ${DIAMOND_CROSSFADE_MS}ms ease` : "none",
        zIndex: 1,
        pointerEvents: "none",
      };
    }
    // incoming / sole active
    const hasOutgoing = outgoing != null;
    return {
      opacity: hasOutgoing && !transitioning ? 0 : 1,
      transition: transitioning ? `opacity ${DIAMOND_CROSSFADE_MS}ms ease` : "none",
      zIndex: 2,
      pointerEvents: hasOutgoing && !transitioning ? "none" : "auto",
    };
  };

  return (
    <div
      className={`relative w-full overflow-hidden ${heightClass}`}
      style={legacyMinHeight ? { height: legacyMinHeight } : undefined}
      data-hero-diamond-crossfade=""
      dir={direction}
      onPointerDown={(e) => {
        if (e.button !== 0) return;
        pointerX.current = e.clientX;
      }}
      onPointerUp={(e) => {
        const start = pointerX.current;
        pointerX.current = null;
        if (start == null || len < 2) return;
        const dx = e.clientX - start;
        if (Math.abs(dx) < 48) return;
        if (direction === "rtl") {
          if (dx > 0) goTo(activeRef.current - 1);
          else goTo(activeRef.current + 1);
        } else if (dx > 0) {
          goTo(activeRef.current - 1);
        } else {
          goTo(activeRef.current + 1);
        }
      }}
      onPointerCancel={() => {
        pointerX.current = null;
      }}
    >
      {outgoing != null && outgoing !== active ? (
        <div className={`absolute inset-0 ${rounded} overflow-hidden`} style={fadeStyle("outgoing")}>
          <DiamondSlideFace slide={slides[outgoing]!} overlayClass={overlayClass} interactive={false} />
        </div>
      ) : null}
      <div className={`absolute inset-0 ${rounded} overflow-hidden`} style={fadeStyle("incoming")}>
        <DiamondSlideFace
          slide={slides[active]!}
          overlayClass={overlayClass}
          interactive={outgoing == null || phase === "crossfade"}
        />
      </div>
      {len > 1 ? (
        <div
          className="absolute inset-x-0 bottom-3 z-10 flex items-center justify-center gap-2"
          role="tablist"
          aria-label="اسلایدهای هیرو"
        >
          {slides.map((_, i) => (
            <button
              key={`diamond-dot-${i}`}
              type="button"
              role="tab"
              aria-selected={i === active}
              aria-label={`اسلاید ${i + 1}`}
              className={`h-2.5 w-2.5 rounded-full transition-colors ${
                i === active ? "bg-sky-500" : "bg-white/70"
              }`}
              onClick={() => goTo(i)}
            />
          ))}
        </div>
      ) : null}
    </div>
  );
}

/** یک کامپوننت؛ فقط variant رفتار layout/effect را عوض می‌کند. */
export function HeroSlider({
  variant,
  slides,
  autoplay = true,
  direction = "rtl",
  heightPreset = "Medium",
  /** Legacy numeric height — used only when preset missing and legacy px present. */
  heightPx,
  intervalSec = 5,
  previewLocale = "fa",
  showPreviewBadge = false,
}: {
  variant: HeroSliderVariantId;
  slides: HeroSlideConfig[];
  autoplay?: boolean;
  direction?: "rtl" | "ltr";
  heightPreset?: string;
  heightPx?: number;
  intervalSec?: number;
  previewLocale?: string;
  showPreviewBadge?: boolean;
}) {
  const mapping = heroVariantToSwiperEffect(variant);
  const effectModules = {
    EffectCreative,
    EffectFade,
    EffectCoverflow,
    EffectCube,
  } as const;
  const modules = [
    Autoplay,
    Pagination,
    ...mapping.modules.map((name) => effectModules[name]),
  ];
  const { shellClass, rounded } = shellForVariant(variant);
  const badge = showPreviewBadge ? <PreviewSampleBadge locale={previewLocale} className="top-3 left-3 z-20" /> : null;
  const heightClass = heroHeightClass(heightPreset);
  const legacyMinHeight =
    typeof heightPx === "number" && heightPx > 0 ? heightPx : undefined;
  const safeSlides = slides.length > 0 ? slides : [{
    mediaAssetId: "",
    imageUrl: "/images/sliders/slider-1.jpg",
    title: "فروشگاه توبا",
    alt: "فروشگاه توبا",
    description: "",
    ctaLabel: "مشاهده",
    destinationType: "all-products" as const,
    targetId: "",
    targetSlug: "",
    targetLabel: "",
    customUrl: "",
    href: "/products",
    panelMediaAssetId: "",
    panelImageUrl: "",
    panelColor: "#0f172a",
    panelSize: "xlarge" as const,
    panelOpacity: 100,
    panelSide: "left" as const,
  }];

  const creativeProps =
    mapping.effect === "creative"
      ? {
          // سیمین: اسلاید قبلی کامل خارج شود (بدون کارت زیرین)؛ حرکت اسلاید بعدی حفظ شود.
          creativeEffect: {
            limitProgress: 1,
            prev: { shadow: false, translate: ["-100%", 0, 0], opacity: 0 },
            next: { translate: ["100%", 0, 0], opacity: 1 },
          },
        }
      : {};

  const coverflowProps =
    mapping.effect === "coverflow"
      ? { coverflowEffect: { rotate: 28, stretch: 0, depth: 120, modifier: 1, slideShadows: true } }
      : {};

  const overlayClass =
    variant === "shapes"
      ? "bg-gradient-to-tr from-fuchsia-900/45 via-black/25 to-amber-400/20"
      : variant === "cinematic"
        ? "bg-gradient-to-t from-black/70 via-black/25 to-transparent"
        : "bg-black/35";

  const diamondSequential =
    variant === "fullscreen" ? (
      <HeroDiamondSequential
        slides={safeSlides}
        autoplay={autoplay}
        intervalSec={intervalSec}
        heightClass={heightClass}
        legacyMinHeight={legacyMinHeight}
        rounded={rounded}
        overlayClass={overlayClass}
        direction={direction}
      />
    ) : null;

  const swiper = diamondSequential ? null : (
    <Swiper
      modules={modules}
      effect={mapping.effect === "slide" ? undefined : mapping.effect}
      speed={mapping.speed}
      grabCursor
      loop={safeSlides.length > 1}
      autoplay={
        autoplay
          ? { delay: Math.max(1000, Math.round(intervalSec * 1000)), disableOnInteraction: false }
          : false
      }
      pagination={{ clickable: true, dynamicBullets: true }}
      dir={direction}
      className={`w-full overflow-hidden ${variant === "split" ? "" : heightClass}`}
      style={variant === "split" ? undefined : legacyMinHeight ? { height: legacyMinHeight } : undefined}
      fadeEffect={mapping.effect === "fade" ? { crossFade: true } : undefined}
      {...creativeProps}
      {...coverflowProps}
    >
      {safeSlides.map((slide, index) => {
        const src = slideImageSrc(slide) || "/images/sliders/slider-1.jpg";
        const href = slideHref(slide);
        const title = slide.title.trim() || "فروشگاه توبا";
        const cta = slide.ctaLabel.trim();
        const key = `hero-slide-${index}-${slide.mediaAssetId || src}`;

        if (variant === "split") {
          const panelSize =
            slide.panelSize === "xlarge"
            || slide.panelSize === "small"
            || slide.panelSize === "medium"
            || slide.panelSize === "large"
              ? slide.panelSize
              : "large";
          const panelPct = heroSplitPanelPercent(panelSize);
          const panelSide = slide.panelSide === "right" ? "right" : "left";
          const panelOpacity = Math.max(0, Math.min(100, slide.panelOpacity ?? 100)) / 100;
          const panelColor = slide.panelColor?.trim() || "#e8e0f5";
          const panelSrc = slide.panelMediaAssetId.trim()
            ? storefrontMediaUrl(slide.panelMediaAssetId.trim())
            : slide.panelImageUrl.trim();
          const cols =
            panelSide === "left"
              ? `${panelPct}% ${100 - panelPct}%`
              : `${100 - panelPct}% ${panelPct}%`;
          const imageCol = (
            <div
              className={`relative block min-h-[180px] bg-gray-100 ${heightClass}`}
              style={legacyMinHeight ? { minHeight: legacyMinHeight } : undefined}
            >
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={src} alt={slide.alt || title} className="h-full w-full object-cover" />
            </div>
          );
          const panelCol = (
            <div
              className={`relative flex flex-col justify-center gap-3 overflow-hidden p-6 md:p-10 ${heightClass}`}
              style={{
                ...(legacyMinHeight ? { minHeight: legacyMinHeight } : {}),
                backgroundColor:
                  panelSrc || panelOpacity <= 0
                    ? "transparent"
                    : panelOpacity >= 1
                      ? panelColor
                      : "transparent",
              }}
            >
              {panelSrc ? (
                // eslint-disable-next-line @next/next/no-img-element
                <img
                  src={panelSrc}
                  alt=""
                  className="absolute inset-0 h-full w-full object-cover"
                  style={{ opacity: panelOpacity }}
                  aria-hidden
                />
              ) : panelOpacity > 0 && panelOpacity < 1 ? (
                <div
                  className="absolute inset-0"
                  style={{ backgroundColor: panelColor, opacity: panelOpacity }}
                  aria-hidden
                />
              ) : null}
              <div className="relative z-10 flex flex-col gap-3" dir="rtl">
                <h2 className="text-2xl font-black md:text-4xl">{title}</h2>
                {slide.description ? (
                  <p className="max-w-xl text-sm md:text-base text-foreground/80">{slide.description}</p>
                ) : null}
                {cta && href ? (
                  <span className="inline-flex w-fit rounded-xl bg-primary px-4 py-2 text-sm font-bold text-white">
                    {cta}
                  </span>
                ) : null}
              </div>
            </div>
          );
          const body = (
            <div
              className={`grid grid-cols-1 gap-0 overflow-hidden border border-gray-100 shadow-xl md:grid-cols-[var(--saba-cols)] ${rounded}`}
              style={{ ["--saba-cols" as string]: cols }}
              data-hero-saba-split=""
              data-panel-side={panelSide}
              data-panel-size={panelSize}
              dir="ltr"
            >
              {panelSide === "left" ? (
                <>
                  {panelCol}
                  {imageCol}
                </>
              ) : (
                <>
                  {imageCol}
                  {panelCol}
                </>
              )}
            </div>
          );
          return (
            <SwiperSlide key={key}>
              {href ? (
                <Link href={href} className="block" aria-label={cta || title}>{body}</Link>
              ) : (
                body
              )}
            </SwiperSlide>
          );
        }

        if (variant === "diagonal") {
          const panelSize = slide.panelSize || "xlarge";
          const panelColor = slide.panelColor?.trim() || "#0f172a";
          const panelSrc = slide.panelMediaAssetId.trim()
            ? storefrontMediaUrl(slide.panelMediaAssetId.trim())
            : slide.panelImageUrl.trim();
          const body = (
            <div
              className={`relative overflow-hidden shadow-2xl ${rounded} ${heightClass}`}
              style={{
                ...(legacyMinHeight ? { minHeight: legacyMinHeight } : {}),
                backgroundColor: panelSrc ? "#0f172a" : panelColor,
              }}
            >
              <div
                className="absolute inset-0"
                style={{ clipPath: heroDiagonalImageClip(panelSize) }}
              >
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img src={src} alt={slide.alt || title} className="h-full w-full object-cover opacity-90" />
              </div>
              <div
                className="absolute inset-0"
                style={{ clipPath: heroDiagonalPanelClip(panelSize) }}
                aria-hidden={panelSrc ? undefined : true}
              >
                {panelSrc ? (
                  // eslint-disable-next-line @next/next/no-img-element
                  <img src={panelSrc} alt="" className="h-full w-full object-cover" />
                ) : (
                  <div className="h-full w-full" style={{ backgroundColor: panelColor }} />
                )}
              </div>
              <div className="relative z-10 flex h-full min-h-[inherit] items-center justify-end p-6 md:p-12">
                <div className="max-w-md rounded-2xl bg-surface/95 p-6 text-gray-900 shadow-lg backdrop-blur md:p-8">
                  <h2 className="text-2xl font-black md:text-4xl">{title}</h2>
                  {slide.description ? <p className="mt-2 text-sm md:text-base">{slide.description}</p> : null}
                  {cta && href ? (
                    <span className="mt-4 inline-flex rounded-xl bg-primary px-4 py-2 text-sm font-bold text-white">{cta}</span>
                  ) : null}
                </div>
              </div>
            </div>
          );
          return (
            <SwiperSlide key={key}>
              {href ? (
                <Link href={href} className="block h-full" aria-label={cta || title}>{body}</Link>
              ) : (
                body
              )}
            </SwiperSlide>
          );
        }

        if (variant === "editorial") {
          const body = (
            <div className={`relative overflow-hidden bg-gray-100 shadow-2xl ${rounded}`}>
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img
                src={src}
                alt={slide.alt || title}
                className={`w-full object-cover ${heightClass}`}
                style={legacyMinHeight ? { height: legacyMinHeight } : undefined}
              />
              <div className="absolute inset-0 bg-gradient-to-l from-black/80 via-black/35 to-transparent" />
              <div className="absolute inset-y-0 right-0 flex w-full max-w-xl flex-col justify-end md:justify-center p-6 md:p-10 text-white">
                <p className="mb-2 text-[11px] font-bold text-white/80 md:text-xs">ویترین انتخابی</p>
                <h2 className="text-3xl font-black leading-tight md:text-5xl line-clamp-3">{title}</h2>
                {slide.description ? <p className="mt-3 text-sm text-white/90 md:text-base line-clamp-3">{slide.description}</p> : null}
                {cta ? (
                  <span className="mt-5 inline-flex min-h-11 w-fit items-center rounded-xl bg-surface px-4 py-2 text-sm font-bold text-gray-900">{cta}</span>
                ) : null}
              </div>
            </div>
          );
          return (
            <SwiperSlide key={key}>
              {href ? (
                <Link href={href} className="block" aria-label={cta || title}>{body}</Link>
              ) : (
                body
              )}
            </SwiperSlide>
          );
        }

        const inner = (
          <>
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              src={src}
              alt={slide.alt || title}
              className={`h-full w-full object-cover ${variant === "cinematic" ? "scale-105" : ""}`}
            />
            <div className={`absolute inset-0 ${overlayClass}`} />
            <div className="absolute inset-0 flex flex-col justify-end p-6 text-white md:p-10">
              <h2 className={`font-black line-clamp-2 ${variant === "cinematic" ? "text-3xl md:text-5xl" : "text-2xl md:text-4xl"}`}>
                {title}
              </h2>
              {slide.description ? (
                <p className="mt-2 max-w-xl text-sm md:text-base line-clamp-2">{slide.description}</p>
              ) : null}
              {cta && href ? (
                <span className="mt-3 inline-flex w-fit rounded-xl bg-surface/95 px-3 py-1.5 text-xs font-bold text-gray-900">
                  {cta}
                </span>
              ) : null}
            </div>
          </>
        );
        return (
          <SwiperSlide key={key} className={variant === "shapes" ? "!overflow-hidden" : undefined}>
            {href ? (
              <Link href={href} className="relative block h-full w-full overflow-hidden" aria-label={cta || title}>
                {inner}
              </Link>
            ) : (
              <div className="relative block h-full w-full overflow-hidden">{inner}</div>
            )}
          </SwiperSlide>
        );
      })}
    </Swiper>
  );

  return (
    <section className={shellClass} data-testid="landing-hero" data-hero-variant={variant} data-height-preset={heightPreset}>
      <div className={`relative overflow-hidden ${variant === "split" || variant === "diagonal" || variant === "editorial" ? "" : `bg-gray-100 shadow-2xl ${rounded}`}`}>
        {badge}
        {variant === "shapes" ? (
          <>
            <div className="pointer-events-none absolute -left-8 top-6 z-10 h-24 w-24 rounded-full bg-surface/20 blur-sm" />
            <div className="pointer-events-none absolute bottom-8 right-10 z-10 h-16 w-16 rotate-12 rounded-2xl bg-surface/15" />
          </>
        ) : null}
        {diamondSequential ?? swiper}
      </div>
    </section>
  );
}

export function LandingHero({
  config,
  layout = "fullscreen",
  previewLocale = "fa",
  showPreviewBadge = false,
}: {
  config: Record<string, unknown>;
  /** Variant id یا کلید legacy layout. */
  layout?: HeroSliderVariantId | "full-width" | "contained" | "side-promos";
  previewLocale?: string;
  showPreviewBadge?: boolean;
}) {
  const variant =
    layout === "full-width"
      ? "fullscreen"
      : layout === "contained"
        ? "shapes"
        : layout === "side-promos"
          ? "diagonal"
          : (layout as HeroSliderVariantId);

  if (hasConfiguredSlides(config)) {
    const fields = readHeroSliderFields(config);
    return (
      <HeroSlider
        variant={variant}
        slides={fields.slides}
        autoplay
        direction="rtl"
        heightPreset={fields.heightPreset}
        heightPx={fields.displayHeightPx ?? undefined}
        intervalSec={fields.slideIntervalSec}
        previewLocale={previewLocale}
        showPreviewBadge={showPreviewBadge}
      />
    );
  }

  // سازگاری با config قدیمی تک‌تصویری
  const title = titleOf(config, "فروشگاه توبا");
  const subtitle = typeof config.subtitle === "string" ? config.subtitle : "";
  const href = typeof config.href === "string" && config.href.trim() ? config.href : "/products";
  const mediaId = typeof config.mediaAssetId === "string" ? config.mediaAssetId.trim() : "";
  const configuredImage =
    typeof config.imageUrl === "string" && config.imageUrl.trim()
      ? config.imageUrl.trim()
      : mediaId
        ? storefrontMediaUrl(mediaId)
        : "";
  const fields = readHeroSliderFields(config);

  return (
    <HeroSlider
      variant={variant}
      slides={[
        {
          mediaAssetId: mediaId,
          imageUrl: configuredImage || "/images/sliders/slider-1.jpg",
          title,
          alt: title,
          description: subtitle,
          ctaLabel: "مشاهده",
          destinationType: href === "/products" ? "all-products" : "custom-url",
          targetId: "",
          targetSlug: "",
          targetLabel: "",
          customUrl: href === "/products" ? "" : href,
          href,
        },
      ]}
      autoplay
      direction="rtl"
      heightPreset={fields.heightPreset}
      heightPx={fields.displayHeightPx ?? heroHeightPresetDesktopPx(fields.heightPreset)}
      intervalSec={5}
      previewLocale={previewLocale}
      showPreviewBadge={showPreviewBadge}
    />
  );
}

export function LandingPromo({
  config,
  previewLocale = "fa",
  showPreviewBadge = false,
}: {
  config: Record<string, unknown>;
  previewLocale?: string;
  showPreviewBadge?: boolean;
}) {
  const title = titleOf(config, "پیشنهاد ویژه");
  const href = typeof config.href === "string" && config.href.trim() ? config.href : "/offers";
  const heightClass = heightPresetBannerClass(config.heightPreset);
  const imageUrl =
    typeof config.imageUrl === "string" && config.imageUrl.trim()
      ? config.imageUrl.trim()
      : "/images/middleBanner/1.webp";
  return (
    <section className="px-2 sm:px-4" data-testid="landing-promo">
      <Link href={href} className="relative block overflow-hidden rounded-3xl bg-gray-100">
        {showPreviewBadge ? <PreviewSampleBadge locale={previewLocale} className="top-3 left-3" /> : null}
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img src={imageUrl} alt="" className={`w-full object-cover ${heightClass}`} />
        <span className="absolute bottom-4 right-4 text-sm font-bold text-white">{title}</span>
      </Link>
    </section>
  );
}

export function LandingRichText({
  config,
  previewLocale = "fa",
  showPreviewBadge = false,
}: {
  config: Record<string, unknown>;
  previewLocale?: string;
  showPreviewBadge?: boolean;
}) {
  const title = typeof config.title === "string" ? config.title : "";
  const text = typeof config.text === "string" ? config.text : "";
  if (!text) return null;
  return (
    <section className="relative mx-auto max-w-3xl px-4 py-8" data-testid="landing-rich-text">
      {showPreviewBadge ? <PreviewSampleBadge locale={previewLocale} className="top-2 left-4" /> : null}
      {title ? <h2 className="mb-3 text-xl font-black">{title}</h2> : null}
      <p className="whitespace-pre-wrap leading-8 text-foreground/80">{text}</p>
    </section>
  );
}

export function LandingCategoryGrid({
  config,
  categories,
  layout = "image-cards",
  previewLocale = "fa",
}: {
  config: Record<string, unknown>;
  categories: StorefrontCategoryItem[];
  layout?: "image-cards" | "compact-tiles" | "horizontal-rail" | "editorial-tiles";
  previewLocale?: string;
}) {
  const ids = asIds(config, "ids", "categoryIds");
  const items = ids.length ? categories.filter((item) => ids.includes(item.categoryId)) : categories;
  if (items.length === 0) {
    return (
      <section className="w-full px-2 py-8 sm:px-4" data-testid="landing-categories" data-category-layout={layout} data-empty="true">
        <h2 className="mb-4 text-lg font-bold">{titleOf(config, "دسته‌بندی‌ها")}</h2>
        <p className="rounded-2xl border border-dashed border-gray-200 bg-surface px-4 py-6 text-center text-sm text-gray-500">
          دسته‌ای برای این بخش انتخاب نشده یا دیگر در دسترس نیست.
        </p>
      </section>
    );
  }
  const images = [2, 3, 4, 5, 6, 7, 8, 9] as const;
  const resolveCategoryImage = (category: StorefrontCategoryItem, index: number) => {
    if (category.imageUrl && category.imageUrl.trim()) return category.imageUrl.trim();
    if (category.imageMediaAssetId) {
      const resolved = storefrontMediaUrl(category.imageMediaAssetId);
      if (resolved && !resolved.includes("/v1/storefront/media/")) return resolved;
      if (resolved) return resolved;
    }
    return `/images/categories/${images[index % images.length]!}.png`;
  };
  if (layout === "editorial-tiles") {
    return (
      <section className="w-full px-2 py-8 sm:px-4" data-testid="landing-categories" data-category-layout="editorial-tiles">
        <h2 className="mb-4 text-lg font-bold">{titleOf(config, "دسته‌بندی‌ها")}</h2>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4 md:gap-4">
          {items.slice(0, 8).map((category, index) => {
            return (
              <Link key={category.categoryId} href={`/products?categoryId=${category.categoryId}`} className="group relative min-h-[160px] overflow-hidden rounded-3xl bg-gray-100 md:min-h-[200px]">
                {category.previewFake ? <PreviewSampleBadge locale={previewLocale} className="top-2 left-2" /> : null}
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img src={resolveCategoryImage(category, index)} alt="" className="absolute inset-0 h-full w-full object-cover transition-transform duration-500 group-hover:scale-105" data-category-media={category.imageMediaAssetId ? "template" : "placeholder"} />
                <div className="absolute inset-0 bg-gradient-to-t from-black/70 via-black/20 to-transparent" />
                <p className="absolute bottom-4 right-4 left-4 text-base font-black text-white line-clamp-2 drop-shadow md:text-lg">{category.name}</p>
              </Link>
            );
          })}
        </div>
      </section>
    );
  }
  const listClass =
    layout === "compact-tiles"
      ? "grid grid-cols-3 sm:grid-cols-4 md:grid-cols-6 gap-2"
      : "flex gap-3 overflow-x-auto pb-2";
  const cardClass =
    layout === "compact-tiles"
      ? "relative overflow-hidden rounded-xl border border-gray-100 bg-surface"
      : layout === "horizontal-rail"
        ? "relative w-[110px] shrink-0 overflow-hidden rounded-full border border-gray-100 bg-surface"
        : "relative w-[160px] shrink-0 overflow-hidden rounded-2xl border border-gray-100 bg-surface";
  return (
    <section className="w-full px-2 py-8 sm:px-4" data-testid="landing-categories" data-category-layout={layout}>
      <h2 className="mb-4 text-lg font-bold">{titleOf(config, "دسته‌بندی‌ها")}</h2>
      <div className={listClass}>
        {items.map((category, index) => {
          return (
            <Link key={category.categoryId} href={`/products?categoryId=${category.categoryId}`} className={cardClass}>
              {category.previewFake ? <PreviewSampleBadge locale={previewLocale} className="top-1 left-1" /> : null}
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={resolveCategoryImage(category, index)} alt="" className="aspect-square w-full object-cover" data-category-media={category.imageMediaAssetId ? "template" : "placeholder"} />
              <p className="px-2 py-3 text-center text-sm font-bold">{category.name}</p>
            </Link>
          );
        })}
      </div>
    </section>
  );
}

export function LandingProductRail({
  section,
  config,
  products,
  layout = "rail",
  previewLocale = "fa",
}: {
  section: StorefrontLandingSection;
  config: Record<string, unknown>;
  products: StorefrontProductCard[];
  layout?: "rail" | "grid" | "compact-rows" | "columns" | "featured-plus-rail" | "ranked-grid" | "tabbed" | "large-cards" | "minimal-list" | "ticker";
  previewLocale?: string;
}) {
  const wanted = section.items;
  const byKey = new Map<string, (typeof products)[number]>();
  for (const card of products) {
    byKey.set(card.productId, card);
    if (card.slug) byKey.set(card.slug, card);
  }
  const ordered = wanted.length
    ? wanted
      .map((item) => byKey.get(item.id) ?? (item.slug ? byKey.get(item.slug) : undefined))
      .filter((card): card is (typeof products)[number] => Boolean(card))
    : products;
  const cards = ordered.slice(0, typeof config.take === "number" ? config.take : 8);
  const title = titleOf(config, "کالاها");
  const source = typeof config.source === "string" ? config.source : "";
  if (cards.length === 0) {
    return (
      <section className="w-full px-2 py-8 sm:px-4" data-testid="landing-products" data-empty="true">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-bold">{title}</h2>
        </div>
        <p className="rounded-2xl border border-dashed border-gray-200 bg-surface px-4 py-6 text-center text-sm text-gray-500">
          {source === "PromotionCampaign"
            ? "در حال حاضر پیشنهاد شگفت‌انگیز فعالی برای نمایش نیست."
            : "کالایی برای نمایش در این بخش یافت نشد."}
        </p>
      </section>
    );
  }
  return (
    <ProductRailSection
      id={`landing-${section.pageSectionId}`}
      title={title}
      href="/products"
      linkLabel="همه"
      tone="plain"
      products={cards}
      slideClassName="w-[170px] shrink-0 md:w-[220px]"
      testId="landing-products"
      layout={layout}
      previewLocale={previewLocale}
    />
  );
}

export function LandingBrandStrip({
  config,
  brands,
  layout = "logo-rail",
  previewLocale = "fa",
}: {
  config: Record<string, unknown>;
  brands: Parameters<typeof HomeBrandsSection>[0]["brands"];
  layout?: "logo-rail" | "logo-grid" | "featured";
  previewLocale?: string;
}) {
  const ids = asIds(config, "ids", "brandIds");
  const filtered = ids.length ? brands.filter((item) => ids.includes(item.brandId)) : brands;
  if (filtered.length === 0) {
    return (
      <section className="w-full px-2 py-8 sm:px-4" data-testid="landing-brands-empty" data-empty="true">
        <p className="rounded-2xl border border-dashed border-gray-200 bg-surface px-4 py-6 text-center text-sm text-gray-500">
          برندی برای این بخش انتخاب نشده یا دیگر در دسترس نیست.
        </p>
      </section>
    );
  }
  return <HomeBrandsSection brands={filtered} layout={layout} previewLocale={previewLocale} />;
}

export function LandingArticleList({
  config,
  articles,
  layout = "magazine-rail",
  previewLocale = "fa",
}: {
  config: Record<string, unknown>;
  articles: Parameters<typeof HomeArticlesSection>[0]["articles"];
  layout?: "magazine-rail" | "grid" | "featured-plus-list";
  previewLocale?: string;
}) {
  const take = typeof config.take === "number" ? config.take : 6;
  const source = typeof config.source === "string" ? config.source : "Latest";
  const articleIds = Array.isArray(config.articleIds)
    ? config.articleIds.map((id) => String(id)).filter(Boolean)
    : [];
  const filtered = source === "Manual" && articleIds.length > 0
    ? articleIds
      .map((id) => articles.find((article) => article.articleId === id))
      .filter((article): article is (typeof articles)[number] => Boolean(article))
    : articles;
  const slice = filtered.slice(0, take);
  if (slice.length === 0) {
    return (
      <section className="w-full px-2 py-8 sm:px-4" data-testid="landing-articles-empty" data-empty="true">
        <p className="rounded-2xl border border-dashed border-gray-200 bg-surface px-4 py-6 text-center text-sm text-gray-500">
          مطلبی برای نمایش نیست.
        </p>
      </section>
    );
  }
  return <HomeArticlesSection articles={slice} layout={layout} previewLocale={previewLocale} />;
}

export function LandingReviews({
  reviews,
  layout = "card-carousel",
  previewLocale = "fa",
}: {
  reviews: Parameters<typeof HomeTestimonialsSection>[0]["reviews"];
  layout?: "card-carousel" | "compact-quotes";
  previewLocale?: string;
}) {
  return reviews.length ? (
    <HomeTestimonialsSection reviews={reviews} layout={layout} previewLocale={previewLocale} />
  ) : (
    <section className="w-full px-2 py-8 sm:px-4" data-testid="landing-reviews-empty" data-empty="true">
      <p className="rounded-2xl border border-dashed border-gray-200 bg-surface px-4 py-6 text-center text-sm text-gray-500">
        نظری برای نمایش نیست.
      </p>
    </section>
  );
}

export function LandingNavigationMenu({
  config,
  menus,
}: {
  config: Record<string, unknown>;
  menus: Record<string, StorefrontMenuItem[]>;
}) {
  const menuId = typeof config.menuId === "string" ? config.menuId : "";
  const items = menuId ? menus[menuId] ?? [] : [];
  return items.length ? <StorefrontMenuLinks items={items} title={titleOf(config, "")} /> : null;
}
