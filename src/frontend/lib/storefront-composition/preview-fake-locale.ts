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
      fa: `محصول نمونه ${n}`,
      en: `Sample product ${n}`,
      ar: `منتج نموذجي ${n}`,
      default: `Sample product ${n}`,
    },
    locale,
  );
}

export function previewFakeCategoryName(index: number, locale: string | null | undefined): string {
  const n = index + 1;
  return pick(
    {
      fa: `دسته‌بندی نمونه ${n}`,
      en: `Sample category ${n}`,
      ar: `فئة نموذجية ${n}`,
      default: `Sample category ${n}`,
    },
    locale,
  );
}

export function previewFakeBrandName(index: number, locale: string | null | undefined): string {
  const n = index + 1;
  return pick(
    {
      fa: `برند نمونه ${n}`,
      en: `Sample brand ${n}`,
      ar: `علامة نموذجية ${n}`,
      default: `Sample brand ${n}`,
    },
    locale,
  );
}

export function previewFakeReviewBody(index: number, locale: string | null | undefined): string {
  const bodies: LocaleMap[] = [
    {
      fa: "متن نمونه برای نمایش نظر مشتری در پیش‌نمایش فروشگاه.",
      en: "Sample text to demonstrate a customer review in store preview.",
      ar: "نص نموذجي لعرض رأي العميل في معاينة المتجر.",
      default: "Sample text to demonstrate a customer review in store preview.",
    },
    {
      fa: "متن نمونه برای نمایش نظر مشتری — تجربه خرید نمایشی.",
      en: "Sample customer review copy for preview layout only.",
      ar: "نص نموذجي لرأي العميل — للمعاينة فقط.",
      default: "Sample customer review copy for preview layout only.",
    },
    {
      fa: "متن نمونه برای نمایش نظر مشتری در کارت نظرات.",
      en: "Sample review body for the reviews showcase layout.",
      ar: "نص نموذجي لبطاقة آراء العملاء.",
      default: "Sample review body for the reviews showcase layout.",
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
      ar: `مشتري نموذجي ${n}`,
      default: `Sample buyer ${n}`,
    },
    locale,
  );
}

export function previewFakeArticleTitle(index: number, locale: string | null | undefined): string {
  const n = index + 1;
  return pick(
    {
      fa: `عنوان نمونه مقاله ${n}`,
      en: `Sample article title ${n}`,
      ar: `عنوان مقال نموذجي ${n}`,
      default: `Sample article title ${n}`,
    },
    locale,
  );
}

export function previewFakeArticleExcerpt(index: number, locale: string | null | undefined): string {
  return pick(
    {
      fa: "متن کوتاه نمونه برای نمایش چیدمان کارت مقاله در پیش‌نمایش فروشگاه.",
      en: "Short sample copy to demonstrate article card layout in store preview.",
      ar: "نص نموذجي قصير لعرض تخطيط بطاقة المقال.",
      default: "Short sample copy to demonstrate article card layout in store preview.",
    },
    locale,
  );
}

export function previewFakeBannerTitle(index: number, locale: string | null | undefined): string {
  const n = index + 1;
  return pick(
    {
      fa: n === 1 ? "تصویر بنر فروشگاه شما" : `تصویر بنر فروشگاه شما ${n}`,
      en: n === 1 ? "Your store banner image" : `Your store banner image ${n}`,
      ar: n === 1 ? "صورة بانر متجرك" : `صورة بانر متجرك ${n}`,
      default: n === 1 ? "Your store banner image" : `Your store banner image ${n}`,
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
