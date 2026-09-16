/**
 * Code-owned localized preview-fill strings.
 * Keyed by locale code prefix (fa / en / ar / …) — not a hardcoded language picker list.
 */

export type PreviewFakeLocaleBucket = "fa" | "en" | "ar" | "default";

export function previewFakeLocaleBucket(locale: string | null | undefined): PreviewFakeLocaleBucket {
  const code = (locale ?? "fa").trim().toLowerCase();
  if (code.startsWith("fa")) return "fa";
  if (code.startsWith("en")) return "en";
  if (code.startsWith("ar")) return "ar";
  return "default";
}

type LocaleMap = Record<PreviewFakeLocaleBucket, string>;

function pick(map: LocaleMap, locale: string | null | undefined): string {
  const bucket = previewFakeLocaleBucket(locale);
  return map[bucket] ?? map.default;
}

/** Subtle badge on fake Store-preview items — never on real/published rows. */
export function previewSampleBadgeLabel(locale: string | null | undefined): string {
  return pick(
    {
      fa: "نمونه نمایشی",
      en: "Sample preview",
      ar: "معاينة تجريبية",
      default: "Sample preview",
    },
    locale,
  );
}

export function previewFakeProductTitle(index: number, locale: string | null | undefined): string {
  const n = index + 1;
  return pick(
    {
      fa: `کالای نمایشی ${n}`,
      en: `Preview product ${n}`,
      ar: `منتج تجريبي ${n}`,
      default: `Preview product ${n}`,
    },
    locale,
  );
}

export function previewFakeCategoryName(index: number, locale: string | null | undefined): string {
  const n = index + 1;
  return pick(
    {
      fa: `دسته نمایشی ${n}`,
      en: `Preview category ${n}`,
      ar: `فئة تجريبية ${n}`,
      default: `Preview category ${n}`,
    },
    locale,
  );
}

export function previewFakeBrandName(index: number, locale: string | null | undefined): string {
  const n = index + 1;
  return pick(
    {
      fa: `برند نمایشی ${n}`,
      en: `Preview brand ${n}`,
      ar: `علامة تجريبية ${n}`,
      default: `Preview brand ${n}`,
    },
    locale,
  );
}

export function previewFakeReviewBody(index: number, locale: string | null | undefined): string {
  const bodies: LocaleMap[] = [
    {
      fa: "کیفیت خوب و ارسال به‌موقع بود؛ تجربه خرید رضایت‌بخشی داشتم.",
      en: "Good quality and on-time delivery — a satisfying purchase.",
      ar: "جودة جيدة وتوصيل في الوقت المناسب.",
      default: "Good quality and on-time delivery — a satisfying purchase.",
    },
    {
      fa: "بسته‌بندی مرتب بود و کالا با توضیحات صفحه هم‌خوانی داشت.",
      en: "Packaging was neat and the item matched the listing.",
      ar: "التغليف مرتب والمنتج مطابق للوصف.",
      default: "Packaging was neat and the item matched the listing.",
    },
    {
      fa: "نسبت قیمت به کیفیت مناسب است؛ دوباره خرید می‌کنم.",
      en: "Fair value for money — I would buy again.",
      ar: "قيمة جيدة مقابل السعر.",
      default: "Fair value for money — I would buy again.",
    },
  ];
  return pick(bodies[index % bodies.length]!, locale);
}

export function previewFakeReviewAuthor(index: number, locale: string | null | undefined): string {
  const n = index + 1;
  return pick(
    {
      fa: `خریدار نمونه ${n}`,
      en: `Sample buyer ${n}`,
      ar: `مشتري تجريبي ${n}`,
      default: `Sample buyer ${n}`,
    },
    locale,
  );
}

export function previewFakeArticleTitle(index: number, locale: string | null | undefined): string {
  const n = index + 1;
  return pick(
    {
      fa: `مقاله نمایشی ${n}`,
      en: `Preview article ${n}`,
      ar: `مقال تجريبي ${n}`,
      default: `Preview article ${n}`,
    },
    locale,
  );
}

export function previewFakeArticleExcerpt(index: number, locale: string | null | undefined): string {
  return pick(
    {
      fa: "متن کوتاه نمایشی برای نشان‌دادن چیدمان کارت مقاله در پیش‌نمایش فروشگاه.",
      en: "Short preview copy to demonstrate article card layout in store preview.",
      ar: "نص تجريبي قصير لعرض تخطيط بطاقة المقال.",
      default: "Short preview copy to demonstrate article card layout in store preview.",
    },
    locale,
  );
}

export function previewFakeBannerTitle(index: number, locale: string | null | undefined): string {
  const n = index + 1;
  return pick(
    {
      fa: `بنر نمایشی ${n}`,
      en: `Preview banner ${n}`,
      ar: `بانر تجريبي ${n}`,
      default: `Preview banner ${n}`,
    },
    locale,
  );
}

export function previewFakeHeroTitle(locale: string | null | undefined): string {
  return pick(
    {
      fa: "ویترین نمایشی فروشگاه",
      en: "Store preview showcase",
      ar: "واجهة معاينة المتجر",
      default: "Store preview showcase",
    },
    locale,
  );
}

export function previewFakeHeroSubtitle(locale: string | null | undefined): string {
  return pick(
    {
      fa: "این محتوا فقط برای پیش‌نمایش چیدمان است و در ویترین منتشرشده نمایش داده نمی‌شود.",
      en: "Preview-only layout content — never shown on the published storefront.",
      ar: "محتوى معاينة فقط — لا يظهر في الواجهة المنشورة.",
      default: "Preview-only layout content — never shown on the published storefront.",
    },
    locale,
  );
}

export function previewFakePromoTitle(locale: string | null | undefined): string {
  return pick(
    {
      fa: "پیشنهاد نمایشی",
      en: "Preview promo",
      ar: "عرض تجريبي",
      default: "Preview promo",
    },
    locale,
  );
}

export function previewFakeRichText(locale: string | null | undefined): { title: string; text: string } {
  return {
    title: pick(
      {
        fa: "متن نمایشی",
        en: "Preview text",
        ar: "نص تجريبي",
        default: "Preview text",
      },
      locale,
    ),
    text: pick(
      {
        fa: "این بلوک متن فقط برای پیش‌نمایش چیدمان بخش متنی است و ذخیره یا منتشر نمی‌شود.",
        en: "This text block is preview-only layout content and is never persisted or published.",
        ar: "كتلة نص للمعاينة فقط ولا تُحفظ أو تُنشر.",
        default: "This text block is preview-only layout content and is never persisted or published.",
      },
      locale,
    ),
  };
}

export function previewFakeStoryTitle(index: number, locale: string | null | undefined): string {
  const n = index + 1;
  return pick(
    {
      fa: `استوری نمایشی ${n}`,
      en: `Preview story ${n}`,
      ar: `قصة تجريبية ${n}`,
      default: `Preview story ${n}`,
    },
    locale,
  );
}
