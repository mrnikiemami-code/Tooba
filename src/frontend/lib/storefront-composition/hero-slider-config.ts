/** قرارداد اسلایدر اصلی — یک کامپوننت، شش Variant فقط برای layout/effect. */

import { heightPresetHeroClass, isSizePreset, SIZE_PRESET_CONTRACTS } from "./size-presets.ts";
import type { SizePreset } from "./types.ts";

export const HERO_SLIDER_MAX_SLIDES = 8;

/** ارتفاع‌های مجاز Hero (بدون Compact و بدون پیکسل خام). */
export const HERO_HEIGHT_PRESETS = ["Medium", "Large", "ExtraLarge"] as const;
export type HeroHeightPreset = (typeof HERO_HEIGHT_PRESETS)[number];

export const HERO_HEIGHT_PRESET_LABELS_FA: Record<HeroHeightPreset, string> = {
  Medium: "متوسط",
  Large: "بزرگ",
  ExtraLarge: "خیلی بزرگ",
};

/** اندازهٔ پنل مورب کیمیا (خیلی‌بزرگ = اندازهٔ فعلی). */
export const HERO_DIAGONAL_PANEL_SIZES = ["small", "medium", "large", "xlarge"] as const;
export type HeroDiagonalPanelSize = (typeof HERO_DIAGONAL_PANEL_SIZES)[number];

export const HERO_DIAGONAL_PANEL_SIZE_LABELS_FA: Record<HeroDiagonalPanelSize, string> = {
  small: "کوچک",
  medium: "متوسط",
  large: "بزرگ",
  xlarge: "خیلی بزرگ",
};

export const HERO_DIAGONAL_PANEL_DEFAULT_COLOR = "#0f172a";

/** صبا: رنگ پیش‌فرض پنل متن (نیمهٔ رنگی). */
export const HERO_SPLIT_PANEL_DEFAULT_COLOR = "#e8e0f5";

export const HERO_PANEL_SIDES = ["left", "right"] as const;
export type HeroPanelSide = (typeof HERO_PANEL_SIDES)[number];

export const HERO_PANEL_SIDE_LABELS_FA: Record<HeroPanelSide, string> = {
  left: "چپ",
  right: "راست",
};

export function isHeroPanelSide(value: unknown): value is HeroPanelSide {
  return typeof value === "string" && (HERO_PANEL_SIDES as readonly string[]).includes(value);
}

/** عرض پنل صبا (٪). بزرگ = ۵۰٪ همان نیمه‌نیمهٔ فعلی. */
export function heroSplitPanelPercent(size: HeroDiagonalPanelSize): number {
  switch (size) {
    case "small":
      return 28;
    case "medium":
      return 38;
    case "large":
      return 50;
    case "xlarge":
      return 62;
    default:
      return 50;
  }
}

export function clampPanelOpacity(value: unknown): number {
  const n = typeof value === "number" ? value : Number(value);
  if (!Number.isFinite(n)) return 100;
  return Math.max(0, Math.min(100, Math.round(n)));
}

export function isHeroDiagonalPanelSize(value: unknown): value is HeroDiagonalPanelSize {
  return typeof value === "string" && (HERO_DIAGONAL_PANEL_SIZES as readonly string[]).includes(value);
}

/**
 * برش تصویر اصلی (چپ) بر اساس اندازهٔ پنل مشکی/راست.
 * xlarge = هندسهٔ فعلی کیمیا.
 */
export function heroDiagonalImageClip(size: HeroDiagonalPanelSize): string {
  switch (size) {
    case "small":
      return "polygon(0 0, 94% 0, 82% 100%, 0 100%)";
    case "medium":
      return "polygon(0 0, 88% 0, 70% 100%, 0 100%)";
    case "large":
      return "polygon(0 0, 80% 0, 58% 100%, 0 100%)";
    case "xlarge":
    default:
      return "polygon(0 0, 72% 0, 48% 100%, 0 100%)";
  }
}

/** برش مکمل برای پنل راست (رنگ یا تصویر دوم). */
export function heroDiagonalPanelClip(size: HeroDiagonalPanelSize): string {
  switch (size) {
    case "small":
      return "polygon(94% 0, 100% 0, 100% 100%, 82% 100%)";
    case "medium":
      return "polygon(88% 0, 100% 0, 100% 100%, 70% 100%)";
    case "large":
      return "polygon(80% 0, 100% 0, 100% 100%, 58% 100%)";
    case "xlarge":
    default:
      return "polygon(72% 0, 100% 0, 100% 100%, 48% 100%)";
  }
}

/** شناسه‌های پایدار Variant (بدون پکیج پولی UI Initiative). */
export const HERO_SLIDER_VARIANTS = [
  {
    id: "fullscreen",
    variantKey: "hero.fullscreen",
    nameFa: "الماس",
    designNameFa: "اسلایدر تمام‌عرض — طرح الماس",
    descriptionFa: "تصویر تمام‌عرض، متن روی تصویر؛ مناسب فروشگاه‌های عمومی",
    badgeFa: "عمومی",
    previewToneClass: "bg-sky-600/80",
  },
  {
    id: "shapes",
    variantKey: "hero.shapes",
    nameFa: "سیمین",
    designNameFa: "اسلایدر شکلی — طرح سیمین",
    descriptionFa: "لایه‌های گرافیکی و فرم‌های تزئینی؛ مناسب beauty / fashion / decor",
    badgeFa: "زیبایی و مد",
    previewToneClass: "bg-fuchsia-500/75",
  },
  {
    id: "diagonal",
    variantKey: "hero.diagonal",
    nameFa: "کیمیا",
    designNameFa: "اسلایدر مورب — طرح کیمیا",
    descriptionFa: "اسپلیت مورب و مدرن؛ مناسب tech / tools / auto",
    badgeFa: "فنی",
    previewToneClass: "bg-amber-500/80",
  },
  {
    id: "cinematic",
    variantKey: "hero.cinematic",
    nameFa: "فاخته",
    designNameFa: "اسلایدر سینمایی — طرح فاخته",
    descriptionFa: "تصویر بزرگ با عمق و transition سنگین‌تر؛ مناسب برندهای premium",
    badgeFa: "پرمیوم",
    previewToneClass: "bg-indigo-700/85",
  },
  {
    id: "split",
    variantKey: "hero.split",
    nameFa: "صبا",
    designNameFa: "بنر دوتکه — طرح صبا",
    descriptionFa: "متن و CTA یک سمت، تصویر سمت دیگر؛ تمیز و conversion-friendly",
    badgeFa: "تبدیل‌محور",
    previewToneClass: "bg-emerald-600/80",
  },
  {
    id: "editorial",
    variantKey: "hero.editorial",
    nameFa: "عقیق",
    designNameFa: "بنر تحریریه — طرح عقیق",
    descriptionFa: "حس مجله‌ای/لوکس با تایپوگرافی پررنگ؛ مناسب fashion / interior / lifestyle",
    badgeFa: "مجله‌ای",
    previewToneClass: "bg-rose-600/80",
  },
] as const;

export type HeroSliderVariantId = (typeof HERO_SLIDER_VARIANTS)[number]["id"];

export const HERO_DESTINATION_TYPES = [
  "none",
  "all-products",
  "product",
  "category",
  "custom-url",
] as const;

export type HeroDestinationType = (typeof HERO_DESTINATION_TYPES)[number];

export const HERO_DESTINATION_LABELS_FA: Record<HeroDestinationType, string> = {
  none: "بدون پیوند",
  "all-products": "همه محصولات",
  product: "محصول مشخص",
  category: "دسته‌بندی مشخص",
  "custom-url": "آدرس سفارشی",
};

export type HeroSlideFieldKey =
  | "image"
  | "alt"
  | "title"
  | "description"
  | "ctaLabel"
  | "destination"
  | "customUrl";

export type HeroSlideConfig = {
  mediaAssetId: string;
  imageUrl: string;
  title: string;
  alt: string;
  /** Visible subtitle/description (optional). Legacy seoDescription maps here. */
  description: string;
  ctaLabel: string;
  destinationType: HeroDestinationType;
  /** Product or category identity — never shown as raw GUID in ordinary UI. */
  targetId: string;
  /** Product slug for canonical route resolution. */
  targetSlug: string;
  /** Human label for selected product/category (display cache only). */
  targetLabel: string;
  customUrl: string;
  /** Resolved href for runtime/compat (derived from destination). */
  href: string;
  /** کیمیا/صبا: تصویر اختیاری پنل دوم. */
  panelMediaAssetId: string;
  panelImageUrl: string;
  /** کیمیا/صبا: رنگ پنل وقتی تصویر دوم نیست. */
  panelColor: string;
  /** کیمیا/صبا: اندازهٔ پنل. */
  panelSize: HeroDiagonalPanelSize;
  /** صبا: شفافیت پنل ۰–۱۰۰ (۰ = تم فروشگاه دیده می‌شود). */
  panelOpacity: number;
  /** صبا: سمت پنل رنگی/تصویر دوم. */
  panelSide: HeroPanelSide;
};

export type HeroSliderConfigFields = {
  heightPreset: HeroHeightPreset;
  /** Legacy numeric height retained for compat rendering only. */
  displayHeightPx: number | null;
  slideIntervalSec: number;
  slideCount: number;
  slides: HeroSlideConfig[];
  autoplay: true;
};

export type HeroSlideFieldError = {
  field: HeroSlideFieldKey;
  messageFa: string;
};

export type HeroSlideValidation = {
  slideIndex: number;
  errors: HeroSlideFieldError[];
};

export type HeroSliderValidationResult = {
  ok: boolean;
  summaryFa: string | null;
  heightErrorFa: string | null;
  intervalErrorFa: string | null;
  slides: HeroSlideValidation[];
  firstInvalidSlideIndex: number | null;
  firstInvalidField: HeroSlideFieldKey | null;
};

export function emptyHeroSlide(): HeroSlideConfig {
  return {
    mediaAssetId: "",
    imageUrl: "",
    title: "",
    alt: "",
    description: "",
    ctaLabel: "",
    destinationType: "none",
    targetId: "",
    targetSlug: "",
    targetLabel: "",
    customUrl: "",
    href: "",
    panelMediaAssetId: "",
    panelImageUrl: "",
    panelColor: HERO_DIAGONAL_PANEL_DEFAULT_COLOR,
    panelSize: "xlarge",
    panelOpacity: 100,
    panelSide: "left",
  };
}

export function defaultHeroSliderConfig(): Record<string, unknown> {
  return {
    heightPreset: "Medium",
    slideIntervalSec: 5,
    slideCount: 1,
    slides: [emptyHeroSlide()],
    autoplay: true,
    title: "",
    subtitle: "",
    href: "",
  };
}

export function isHeroSliderVariantId(value: unknown): value is HeroSliderVariantId {
  return typeof value === "string" && HERO_SLIDER_VARIANTS.some((row) => row.id === value);
}

export function isHeroHeightPreset(value: unknown): value is HeroHeightPreset {
  return typeof value === "string" && (HERO_HEIGHT_PRESETS as readonly string[]).includes(value);
}

export function isHeroDestinationType(value: unknown): value is HeroDestinationType {
  return typeof value === "string" && (HERO_DESTINATION_TYPES as readonly string[]).includes(value);
}

/** نگاشت کلید Variant رجیستری → شناسه layout/effect. */
export function heroVariantIdFromKey(variantKey: string): HeroSliderVariantId {
  const key = variantKey.trim();
  const hit = HERO_SLIDER_VARIANTS.find((row) => row.variantKey === key);
  if (hit) return hit.id;
  // legacy keys
  if (key === "hero.full-width") return "fullscreen";
  if (key === "hero.contained") return "shapes";
  if (key === "hero.side-promos") return "diagonal";
  if (key === "hero.split") return "split";
  if (key === "hero.editorial") return "editorial";
  return "fullscreen";
}

/** Map legacy pixel height to nearest Medium/Large/ExtraLarge preset. */
export function mapLegacyHeightPxToPreset(px: number): HeroHeightPreset {
  if (!(px > 0)) return "Medium";
  if (px < 360) return "Medium";
  if (px < 460) return "Large";
  return "ExtraLarge";
}

export function normalizeHeroHeightPreset(config: Record<string, unknown>): HeroHeightPreset {
  const raw = config.heightPreset;
  if (isHeroHeightPreset(raw)) return raw;
  // Accept lowercase aliases from task wording
  if (typeof raw === "string") {
    const lower = raw.trim().toLowerCase();
    if (lower === "medium") return "Medium";
    if (lower === "large") return "Large";
    if (lower === "xlarge" || lower === "extra-large" || lower === "extralarge") return "ExtraLarge";
  }
  if (isSizePreset(raw) && raw === "Compact") return "Medium";
  if (isSizePreset(raw) && (raw === "Medium" || raw === "Large" || raw === "ExtraLarge")) {
    return raw;
  }
  const heightRaw =
    typeof config.displayHeightPx === "number" ? config.displayHeightPx : Number(config.displayHeightPx);
  if (Number.isFinite(heightRaw) && heightRaw > 0) return mapLegacyHeightPxToPreset(heightRaw);
  return "Medium";
}

export function heroHeightClass(preset: HeroHeightPreset | unknown): string {
  return heightPresetHeroClass(isHeroHeightPreset(preset) ? preset : "Medium");
}

/** Approximate desktop px for guidance / legacy inline fallback. */
export function heroHeightPresetDesktopPx(preset: HeroHeightPreset): number {
  switch (preset) {
    case "Large":
      return 420;
    case "ExtraLarge":
      return 500;
    case "Medium":
    default:
      return 350;
  }
}

export type HeroImageGuidance = {
  aspectRatio: string;
  minWidth: number;
  minHeight: number;
  focalFa: string;
  summaryFa: string;
};

const HEIGHT_SCALE: Record<HeroHeightPreset, number> = {
  Medium: 1,
  Large: 1.15,
  ExtraLarge: 1.3,
};

export function heroImageGuidance(
  variant: HeroSliderVariantId,
  heightPreset: HeroHeightPreset,
): HeroImageGuidance {
  const scale = HEIGHT_SCALE[heightPreset];
  const base = (() => {
    switch (variant) {
      case "shapes":
        return {
          aspectRatio: "16:9",
          minWidth: 1600,
          minHeight: 900,
          focalFa: "سوژه اصلی نزدیک مرکز؛ حاشیه برای لایه‌های شکلی آزاد بماند",
        };
      case "diagonal":
        return {
          aspectRatio: "3:2",
          minWidth: 1500,
          minHeight: 1000,
          focalFa: "سوژه در سمت راست/چپ تصویر (نیمهٔ غیرمورب) قرار گیرد",
        };
      case "cinematic":
        return {
          aspectRatio: "21:9",
          minWidth: 1920,
          minHeight: 820,
          focalFa: "افق و سوژه در یک‌سوم میانی؛ فضای بالا/پایین برای عمق سینمایی",
        };
      case "split":
        return {
          aspectRatio: "4:5",
          minWidth: 1200,
          minHeight: 1500,
          focalFa: "سوژه در مرکز عمودی نیمه‌تصویر؛ مناسب برش پرتره",
        };
      case "editorial":
        return {
          aspectRatio: "3:2",
          minWidth: 1600,
          minHeight: 1060,
          focalFa: "سوژه در یک‌سوم کناری؛ فضای متن روی گرادیان مقابل آزاد بماند",
        };
      case "fullscreen":
      default:
        return {
          aspectRatio: "16:9",
          minWidth: 1600,
          minHeight: 900,
          focalFa: "سوژه اصلی نزدیک مرکز تصویر",
        };
    }
  })();
  const minWidth = Math.round(base.minWidth * scale);
  const minHeight = Math.round(base.minHeight * scale);
  return {
    ...base,
    minWidth,
    minHeight,
    summaryFa: `پیشنهاد تصویر: نسبت ${base.aspectRatio} — حداقل ${minWidth.toLocaleString("fa-IR")}×${minHeight.toLocaleString("fa-IR")} — ${base.focalFa}`,
  };
}

function inferDestinationFromHref(href: string): Pick<
  HeroSlideConfig,
  "destinationType" | "targetId" | "targetSlug" | "customUrl" | "href"
> {
  const trimmed = href.trim();
  if (!trimmed) {
    return { destinationType: "none", targetId: "", targetSlug: "", customUrl: "", href: "" };
  }
  if (trimmed === "/products" || trimmed === "/products/") {
    return {
      destinationType: "all-products",
      targetId: "",
      targetSlug: "",
      customUrl: "",
      href: "/products",
    };
  }
  const productMatch = trimmed.match(/^\/products\/([^/?#]+)\/?$/i);
  if (productMatch?.[1]) {
    return {
      destinationType: "product",
      targetId: "",
      targetSlug: decodeURIComponent(productMatch[1]),
      customUrl: "",
      href: `/products/${productMatch[1]}`,
    };
  }
  const categoryMatch = trimmed.match(/^\/products\/?\?(?:.*&)?categoryId=([^&]+)/i);
  if (categoryMatch?.[1]) {
    return {
      destinationType: "category",
      targetId: decodeURIComponent(categoryMatch[1]),
      targetSlug: "",
      customUrl: "",
      href: `/products?categoryId=${categoryMatch[1]}`,
    };
  }
  return {
    destinationType: "custom-url",
    targetId: "",
    targetSlug: "",
    customUrl: trimmed,
    href: trimmed,
  };
}

export function resolveHeroSlideHref(slide: Pick<
  HeroSlideConfig,
  "destinationType" | "targetId" | "targetSlug" | "customUrl" | "href"
>): string {
  switch (slide.destinationType) {
    case "none":
      return "";
    case "all-products":
      return "/products";
    case "product":
      if (slide.targetSlug.trim()) return `/products/${encodeURIComponent(slide.targetSlug.trim())}`;
      if (slide.href.trim()) return slide.href.trim();
      return "";
    case "category":
      if (slide.targetId.trim()) {
        return `/products?categoryId=${encodeURIComponent(slide.targetId.trim())}`;
      }
      if (slide.href.trim()) return slide.href.trim();
      return "";
    case "custom-url":
      return slide.customUrl.trim() || slide.href.trim();
    default:
      return slide.href.trim();
  }
}

export function isValidHeroCustomUrl(value: string): boolean {
  const trimmed = value.trim();
  if (!trimmed) return false;
  if (trimmed.startsWith("/")) {
    if (trimmed.startsWith("//")) return false;
    return !/\s/.test(trimmed);
  }
  try {
    const url = new URL(trimmed);
    return url.protocol === "http:" || url.protocol === "https:";
  } catch {
    return false;
  }
}

export function normalizeHeroSlides(value: unknown, count: number): HeroSlideConfig[] {
  const raw = Array.isArray(value) ? value : [];
  const n = Math.max(1, Math.min(HERO_SLIDER_MAX_SLIDES, Math.floor(count) || 1));
  return Array.from({ length: n }, (_, index) => {
    const item = raw[index];
    if (!item || typeof item !== "object") return emptyHeroSlide();
    const row = item as Record<string, unknown>;
    const title = typeof row.title === "string" ? row.title : "";
    const alt =
      typeof row.alt === "string"
        ? row.alt
        : typeof row.altText === "string"
          ? row.altText
          : "";
    // Prefer visible description; fall back to legacy seoDescription / seoTitle-as-title already in title.
    const description =
      typeof row.description === "string"
        ? row.description
        : typeof row.seoDescription === "string"
          ? row.seoDescription
          : typeof row.subtitle === "string"
            ? row.subtitle
            : "";
    // If legacy seoTitle differed and title empty, promote seoTitle to visible title.
    const resolvedTitle =
      title.trim()
        ? title
        : typeof row.seoTitle === "string"
          ? row.seoTitle
          : "";
    const ctaLabel = typeof row.ctaLabel === "string" ? row.ctaLabel : "";
    const legacyHref = typeof row.href === "string" ? row.href : "";
    const destinationType = isHeroDestinationType(row.destinationType)
      ? row.destinationType
      : inferDestinationFromHref(legacyHref).destinationType;
    const inferred = inferDestinationFromHref(legacyHref);
    const targetId =
      typeof row.targetId === "string" && row.targetId.trim()
        ? row.targetId
        : inferred.targetId;
    const targetSlug =
      typeof row.targetSlug === "string" && row.targetSlug.trim()
        ? row.targetSlug
        : inferred.targetSlug;
    const targetLabel = typeof row.targetLabel === "string" ? row.targetLabel : "";
    const customUrl =
      typeof row.customUrl === "string" && row.customUrl.trim()
        ? row.customUrl
        : destinationType === "custom-url"
          ? inferred.customUrl || legacyHref
          : "";
    const normalized: HeroSlideConfig = {
      mediaAssetId: typeof row.mediaAssetId === "string" ? row.mediaAssetId : "",
      imageUrl: typeof row.imageUrl === "string" ? row.imageUrl : "",
      title: resolvedTitle,
      alt,
      description,
      ctaLabel,
      destinationType,
      targetId,
      targetSlug,
      targetLabel,
      customUrl,
      href: "",
      panelMediaAssetId: typeof row.panelMediaAssetId === "string" ? row.panelMediaAssetId : "",
      panelImageUrl: typeof row.panelImageUrl === "string" ? row.panelImageUrl : "",
      panelColor:
        typeof row.panelColor === "string" && /^#[0-9A-Fa-f]{6}$/.test(row.panelColor.trim())
          ? row.panelColor.trim()
          : HERO_DIAGONAL_PANEL_DEFAULT_COLOR,
      panelSize: isHeroDiagonalPanelSize(row.panelSize) ? row.panelSize : "xlarge",
      panelOpacity: clampPanelOpacity(row.panelOpacity),
      panelSide: isHeroPanelSide(row.panelSide) ? row.panelSide : "left",
    };
    normalized.href = resolveHeroSlideHref(normalized) || legacyHref;
    return normalized;
  });
}

export function readHeroSliderFields(config: Record<string, unknown>): HeroSliderConfigFields {
  const slideCountRaw = typeof config.slideCount === "number" ? config.slideCount : Number(config.slideCount);
  const slideCount = Number.isFinite(slideCountRaw)
    ? Math.max(1, Math.min(HERO_SLIDER_MAX_SLIDES, Math.floor(slideCountRaw)))
    : 1;
  const heightPreset = normalizeHeroHeightPreset(config);
  const heightRaw =
    typeof config.displayHeightPx === "number" ? config.displayHeightPx : Number(config.displayHeightPx);
  const intervalRaw =
    typeof config.slideIntervalSec === "number" ? config.slideIntervalSec : Number(config.slideIntervalSec);
  return {
    heightPreset,
    displayHeightPx: Number.isFinite(heightRaw) && heightRaw > 0 ? Math.floor(heightRaw) : null,
    slideIntervalSec: Number.isFinite(intervalRaw) && intervalRaw > 0 ? intervalRaw : 5,
    slideCount,
    slides: normalizeHeroSlides(config.slides, slideCount),
    autoplay: true,
  };
}

export function validateHeroSlide(slide: HeroSlideConfig): HeroSlideFieldError[] {
  const errors: HeroSlideFieldError[] = [];
  if (!(slide.mediaAssetId.trim() || slide.imageUrl.trim())) {
    errors.push({ field: "image", messageFa: "تصویر اسلاید الزامی است." });
  }
  if (!slide.alt.trim()) {
    errors.push({ field: "alt", messageFa: "متن جایگزین تصویر (Alt) الزامی است." });
  }
  if (!slide.title.trim()) {
    errors.push({ field: "title", messageFa: "عنوان قابل‌نمایش الزامی است." });
  }

  // CTA label و مقصد «بدون پیوند» اختیاری‌اند و نباید خطا بدهند.
  // فقط وقتی مقصد مشخص انتخاب شده، شناسه/آدرس مربوطه لازم است.
  if (slide.destinationType === "product" && !(slide.targetId.trim() || slide.targetSlug.trim())) {
    errors.push({ field: "destination", messageFa: "یک محصول انتخاب کنید." });
  }
  if (slide.destinationType === "category" && !slide.targetId.trim()) {
    errors.push({ field: "destination", messageFa: "یک دسته‌بندی انتخاب کنید." });
  }
  if (slide.destinationType === "custom-url") {
    if (!slide.customUrl.trim()) {
      errors.push({ field: "customUrl", messageFa: "آدرس سفارشی را وارد کنید." });
    } else if (!isValidHeroCustomUrl(slide.customUrl)) {
      errors.push({ field: "customUrl", messageFa: "آدرس سفارشی معتبر نیست (مسیر داخلی یا http/https)." });
    }
  }

  return errors;
}

export function validateHeroSliderDetailed(config: Record<string, unknown>): HeroSliderValidationResult {
  const fields = readHeroSliderFields(config);
  const heightErrorFa = isHeroHeightPreset(fields.heightPreset)
    ? null
    : "اندازه نمایش را از میان متوسط، بزرگ یا خیلی بزرگ انتخاب کنید.";
  const intervalErrorFa =
    fields.slideIntervalSec > 0 ? null : "زمان تغییر اسلایدر باید برحسب ثانیه و بزرگ‌تر از صفر باشد.";

  const slides: HeroSlideValidation[] = fields.slides.map((slide, slideIndex) => ({
    slideIndex,
    errors: validateHeroSlide(slide),
  }));

  const invalidSlides = slides.filter((row) => row.errors.length > 0);
  const firstInvalid = invalidSlides[0] ?? null;
  const invalidLabels = invalidSlides.map(
    (row) => `اسلاید ${(row.slideIndex + 1).toLocaleString("fa-IR")}`,
  );

  let summaryFa: string | null = null;
  if (heightErrorFa || intervalErrorFa || invalidSlides.length > 0) {
    if (invalidSlides.length === 1) {
      summaryFa = `اطلاعات ${invalidLabels[0]} ناقص است. لطفاً فیلدهای مشخص‌شده را تکمیل کنید.`;
    } else if (invalidSlides.length > 1) {
      summaryFa = `اطلاعات ${invalidLabels.join("، ")} ناقص است. لطفاً فیلدهای مشخص‌شده را تکمیل کنید.`;
    } else {
      summaryFa = heightErrorFa ?? intervalErrorFa ?? "تنظیمات اسلایدر ناقص است.";
    }
  }

  return {
    ok: !heightErrorFa && !intervalErrorFa && invalidSlides.length === 0,
    summaryFa,
    heightErrorFa,
    intervalErrorFa,
    slides,
    firstInvalidSlideIndex: firstInvalid?.slideIndex ?? null,
    firstInvalidField: firstInvalid?.errors[0]?.field ?? null,
  };
}

export function isHeroSlideComplete(slide: HeroSlideConfig): boolean {
  return validateHeroSlide(slide).length === 0;
}

export function validateHeroSliderSettings(config: Record<string, unknown>): string | null {
  return validateHeroSliderDetailed(config).summaryFa;
}

export function heroHeightPresetLabelFa(preset: HeroHeightPreset): string {
  return HERO_HEIGHT_PRESET_LABELS_FA[preset] ?? SIZE_PRESET_CONTRACTS[preset as SizePreset]?.nameFa ?? preset;
}

/**
 * افکت محلی Swiper برای هر Variant (رایگان؛ بدون پکیج UI Initiative).
 */
export function heroVariantToSwiperEffect(variant: HeroSliderVariantId): {
  effect: "slide" | "creative" | "fade" | "coverflow" | "cube";
  modules: Array<"EffectCreative" | "EffectFade" | "EffectCoverflow" | "EffectCube">;
  speed: number;
} {
  switch (variant) {
    case "shapes":
      return { effect: "creative", modules: ["EffectCreative"], speed: 700 };
    case "diagonal":
      return { effect: "coverflow", modules: ["EffectCoverflow"], speed: 800 };
    case "cinematic":
      return { effect: "cube", modules: ["EffectCube"], speed: 1100 };
    case "editorial":
      return { effect: "fade", modules: ["EffectFade"], speed: 900 };
    case "split":
      return { effect: "slide", modules: [], speed: 600 };
    case "fullscreen":
    default:
      return { effect: "fade", modules: ["EffectFade"], speed: 650 };
  }
}
