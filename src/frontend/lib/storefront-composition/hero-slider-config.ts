/** قرارداد اسلایدر اصلی — یک کامپوننت، شش Variant فقط برای layout/effect. */

export const HERO_SLIDER_MAX_SLIDES = 8;

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

export type HeroSlideConfig = {
  mediaAssetId: string;
  imageUrl: string;
  title: string;
  alt: string;
  seoTitle: string;
  seoDescription: string;
  href: string;
};

export type HeroSliderConfigFields = {
  displayHeightPx: number;
  slideIntervalSec: number;
  slideCount: number;
  slides: HeroSlideConfig[];
  autoplay: true;
};

export function emptyHeroSlide(): HeroSlideConfig {
  return {
    mediaAssetId: "",
    imageUrl: "",
    title: "",
    alt: "",
    seoTitle: "",
    seoDescription: "",
    href: "",
  };
}

export function defaultHeroSliderConfig(): Record<string, unknown> {
  return {
    displayHeightPx: 420,
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

export function normalizeHeroSlides(value: unknown, count: number): HeroSlideConfig[] {
  const raw = Array.isArray(value) ? value : [];
  const n = Math.max(1, Math.min(HERO_SLIDER_MAX_SLIDES, Math.floor(count) || 1));
  return Array.from({ length: n }, (_, index) => {
    const item = raw[index];
    if (!item || typeof item !== "object") return emptyHeroSlide();
    const row = item as Record<string, unknown>;
    return {
      mediaAssetId: typeof row.mediaAssetId === "string" ? row.mediaAssetId : "",
      imageUrl: typeof row.imageUrl === "string" ? row.imageUrl : "",
      title: typeof row.title === "string" ? row.title : "",
      alt: typeof row.alt === "string" ? row.alt : typeof row.altText === "string" ? row.altText : "",
      seoTitle: typeof row.seoTitle === "string" ? row.seoTitle : "",
      seoDescription: typeof row.seoDescription === "string" ? row.seoDescription : "",
      href: typeof row.href === "string" ? row.href : "",
    };
  });
}

export function readHeroSliderFields(config: Record<string, unknown>): HeroSliderConfigFields {
  const slideCountRaw = typeof config.slideCount === "number" ? config.slideCount : Number(config.slideCount);
  const slideCount = Number.isFinite(slideCountRaw)
    ? Math.max(1, Math.min(HERO_SLIDER_MAX_SLIDES, Math.floor(slideCountRaw)))
    : 1;
  const heightRaw = typeof config.displayHeightPx === "number" ? config.displayHeightPx : Number(config.displayHeightPx);
  const intervalRaw =
    typeof config.slideIntervalSec === "number" ? config.slideIntervalSec : Number(config.slideIntervalSec);
  return {
    displayHeightPx: Number.isFinite(heightRaw) && heightRaw > 0 ? Math.floor(heightRaw) : 420,
    slideIntervalSec: Number.isFinite(intervalRaw) && intervalRaw > 0 ? intervalRaw : 5,
    slideCount,
    slides: normalizeHeroSlides(config.slides, slideCount),
    autoplay: true,
  };
}

export function isHeroSlideComplete(slide: HeroSlideConfig): boolean {
  return Boolean(
    (slide.mediaAssetId.trim() || slide.imageUrl.trim())
      && slide.title.trim()
      && slide.alt.trim()
      && slide.seoTitle.trim()
      && slide.seoDescription.trim(),
  );
}

export function validateHeroSliderSettings(config: Record<string, unknown>): string | null {
  const fields = readHeroSliderFields(config);
  if (!(fields.displayHeightPx > 0)) return "اندازه نمایش باید عددی بزرگ‌تر از صفر باشد.";
  if (!(fields.slideIntervalSec > 0)) return "زمان تغییر اسلایدر باید برحسب ثانیه و بزرگ‌تر از صفر باشد.";
  if (fields.slideCount < 1 || fields.slideCount > HERO_SLIDER_MAX_SLIDES) {
    return `تعداد اسلایدر باید بین ۱ و ${HERO_SLIDER_MAX_SLIDES.toLocaleString("fa-IR")} باشد.`;
  }
  for (let i = 0; i < fields.slides.length; i += 1) {
    const slide = fields.slides[i]!;
    if (!isHeroSlideComplete(slide)) {
      return `اسلاید ${(i + 1).toLocaleString("fa-IR")} ناقص است — تصویر، عنوان، alt و فیلدهای سئو الزامی‌اند.`;
    }
  }
  return null;
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
