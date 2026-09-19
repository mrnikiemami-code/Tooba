/**
 * Code-owned Admin presentation metadata for Section Variants (LOCK-SF-335…337).
 * Persisted identity remains the stable variant key — these strings are display-only.
 */

export type VariantDesignMeta = {
  variantKey: string;
  /** Human-friendly Persian design name (primary Admin label). */
  designNameFa: string;
  /** Short understandable description for ordinary sellers. */
  descriptionFa: string;
  /** Optional short badge / suitability category. */
  badgeFa?: string;
};

/**
 * Complete mapping for every registered Variant (including hidden alias).
 * Style: «خانواده قابل‌فهم — طرح [نام]».
 */
export const VARIANT_DESIGN_NAMES: Record<string, VariantDesignMeta> = {
  "hero.fullscreen": {
    variantKey: "hero.fullscreen",
    designNameFa: "اسلایدر تمام‌عرض — طرح الماس",
    descriptionFa: "تصویر تمام‌عرض، متن روی تصویر؛ مناسب فروشگاه‌های عمومی",
    badgeFa: "عمومی",
  },
  "hero.shapes": {
    variantKey: "hero.shapes",
    designNameFa: "اسلایدر شکلی — طرح سیمین",
    descriptionFa: "لایه‌های گرافیکی و فرم‌های تزئینی؛ مناسب beauty / fashion / decor",
    badgeFa: "زیبایی و مد",
  },
  "hero.diagonal": {
    variantKey: "hero.diagonal",
    designNameFa: "اسلایدر مورب — طرح کیمیا",
    descriptionFa: "اسپلیت مورب و مدرن؛ مناسب tech / tools / auto",
    badgeFa: "فنی",
  },
  "hero.cinematic": {
    variantKey: "hero.cinematic",
    designNameFa: "اسلایدر سینمایی — طرح فاخته",
    descriptionFa: "تصویر بزرگ با عمق و transition سنگین‌تر؛ مناسب برندهای premium",
    badgeFa: "پرمیوم",
  },
  "hero.split": {
    variantKey: "hero.split",
    designNameFa: "بنر دوتکه — طرح صبا",
    descriptionFa: "متن و CTA یک سمت، تصویر سمت دیگر؛ تمیز و conversion-friendly",
    badgeFa: "تبدیل‌محور",
  },
  "hero.editorial": {
    variantKey: "hero.editorial",
    designNameFa: "بنر تحریریه — طرح عقیق",
    descriptionFa: "حس مجله‌ای/لوکس با تایپوگرافی پررنگ؛ مناسب fashion / interior / lifestyle",
    badgeFa: "مجله‌ای",
  },

  "story.circle": {
    variantKey: "story.circle",
    designNameFa: "استوری دایره‌ای — طرح یاقوت",
    descriptionFa: "دایره‌های افقی با حاشیه رنگی",
    badgeFa: "میانبر سریع",
  },
  "story.image-circles": {
    variantKey: "story.image-circles",
    designNameFa: "دایره‌های تصویری — طرح زمرد",
    descriptionFa: "دایره پر از تصویر بدون حاشیه خالی",
    badgeFa: "تصویری",
  },
  "story.rounded-cards": {
    variantKey: "story.rounded-cards",
    designNameFa: "کارت‌های گرد — طرح فیروزه",
    descriptionFa: "میانبرهای کارت‌گرد افقی",
    badgeFa: "کمپین",
  },
  "story.icon-shortcuts": {
    variantKey: "story.icon-shortcuts",
    designNameFa: "میانبر آیکونی — طرح عقیق",
    descriptionFa: "آیکون مربعی با برچسب کوتاه",
    badgeFa: "فشرده",
  },

  "category.image-cards": {
    variantKey: "category.image-cards",
    designNameFa: "دسته‌بندی تصویری — طرح نگین",
    descriptionFa: "کارت دسته با تصویر بزرگ",
    badgeFa: "ویترین",
  },
  "category.compact-tiles": {
    variantKey: "category.compact-tiles",
    designNameFa: "کاشی فشرده دسته — طرح مرجان",
    descriptionFa: "شبکه فشرده دسته‌ها",
    badgeFa: "فشرده",
  },
  "category.horizontal-rail": {
    variantKey: "category.horizontal-rail",
    designNameFa: "ردیف افقی دسته — طرح کهربا",
    descriptionFa: "اسکرول افقی دسته‌ها",
  },
  "category.editorial-tiles": {
    variantKey: "category.editorial-tiles",
    designNameFa: "کاشی تحریریه دسته — طرح لعل",
    descriptionFa: "کاشی بزرگ با عنوان روی تصویر",
    badgeFa: "تصویری",
  },

  "product.card-carousel": {
    variantKey: "product.card-carousel",
    designNameFa: "ویترین محصولات — طرح آریا",
    descriptionFa: "ردیف افقی کارت کالا با اسلاید",
    badgeFa: "پیشنهاد",
  },
  "product.amazing": {
    variantKey: "product.amazing",
    designNameFa: "طرح شگفت‌انگیز",
    descriptionFa: "اسلایدر کارت کامل با تایمر شمارش معکوس و نشان تخفیف",
    badgeFa: "پیشنهاد شگفت‌انگیز",
  },
  "product.zohreh": {
    variantKey: "product.zohreh",
    designNameFa: "طرح زهره",
    descriptionFa: "ستون‌های افقی پربازدید با رتبه و قیمت",
    badgeFa: "پربازدید",
  },
  "product.mahoor": {
    variantKey: "product.mahoor",
    designNameFa: "طرح ماهور",
    descriptionFa: "اسلایدر جدیدترین محصولات با نشان جدید",
    badgeFa: "تازه‌ها",
  },
  "product.sunny": {
    variantKey: "product.sunny",
    designNameFa: "سانی",
    descriptionFa: "ردیف روشن و تمیز با کارت فعال کمی بالاتر و محو لبه نرم",
    badgeFa: "سانی",
  },
  "product.money": {
    variantKey: "product.money",
    designNameFa: "مانی",
    descriptionFa: "ردیف فروش‌محور با تمرکز قوی مرکز و تأکید تجاری بدون تغییر کارت کالا",
    badgeFa: "مانی",
  },
  "product.cinematic": {
    variantKey: "product.cinematic",
    designNameFa: "سینمایی",
    descriptionFa: "ردیف سینمایی پریمیوم با عمق ملایم و چرخش جزئی همسایه‌ها",
    badgeFa: "سینمایی",
  },
  "product.cinematic-plus": {
    variantKey: "product.cinematic-plus",
    designNameFa: "سینمایی پلاس",
    descriptionFa: "عمق سینمایی غنی‌تر با برجستگی مرکز کنترل‌شده و بدون افکت اغراق‌آمیز",
    badgeFa: "سینمایی پلاس",
  },
  "product.explorer": {
    variantKey: "product.explorer",
    designNameFa: "کاشف",
    descriptionFa: "بنر ثابت کنار ردیف؛ کارت‌ها از پشت بنر به سمت راست حرکت می‌کنند",
    badgeFa: "کاشف",
  },
  "product.grid": {
    variantKey: "product.grid",
    designNameFa: "شبکه محصولات — طرح پارسا",
    descriptionFa: "شبکه چندستونه کالا",
    badgeFa: "فشرده",
  },
  "product.compact-rows": {
    variantKey: "product.compact-rows",
    designNameFa: "ردیف فشرده کالا — طرح نیکان",
    descriptionFa: "لیست فشرده عنوان و قیمت",
    badgeFa: "فشرده",
  },
  "product.category-columns": {
    variantKey: "product.category-columns",
    designNameFa: "ستون‌های دسته کالا — طرح آرمین",
    descriptionFa: "چند ستون کالا کنار هم",
  },
  "product.featured-plus-rail": {
    variantKey: "product.featured-plus-rail",
    designNameFa: "ویژه و ردیف — طرح کیان",
    descriptionFa: "یک کالای ویژه کنار ردیف افقی",
    badgeFa: "تصویری",
  },
  "product.tabbed": {
    variantKey: "product.tabbed",
    designNameFa: "محصولات زبانه‌دار — طرح سامان",
    descriptionFa: "چند زبانه با یک ردیف مشترک کالا",
    badgeFa: "کمپین",
  },
  "product.large-cards": {
    variantKey: "product.large-cards",
    designNameFa: "کارت‌های بزرگ کالا — طرح رامان",
    descriptionFa: "کارت درشت با تصویر بزرگ",
    badgeFa: "تصویری",
  },
  "product.minimal-list": {
    variantKey: "product.minimal-list",
    designNameFa: "فهرست ساده کالا — طرح آرین",
    descriptionFa: "لیست ساده عنوان و قیمت",
    badgeFa: "فشرده",
  },

  "ranked.horizontal": {
    variantKey: "ranked.horizontal",
    designNameFa: "رتبه افقی — طرح پویا",
    descriptionFa: "ردیف افقی کالا با شماره رتبه",
  },
  "ranked.grid": {
    variantKey: "ranked.grid",
    designNameFa: "شبکه رتبه‌دار — طرح سپهر",
    descriptionFa: "شبکه کالا با رتبه",
  },
  "ranked.ticker": {
    variantKey: "ranked.ticker",
    designNameFa: "نوار رتبه فشرده — طرح آوا",
    descriptionFa: "نوار افقی فشرده با شماره رتبه",
    badgeFa: "فشرده",
  },
  "ranked.multi-column": {
    variantKey: "ranked.multi-column",
    designNameFa: "رتبه چندستونه — طرح نیکو",
    descriptionFa: "چند ستون کالا رتبه‌دار",
  },

  "banner.single": {
    variantKey: "banner.single",
    designNameFa: "بنر تکی — طرح هما",
    descriptionFa: "یک بنر تمام‌عرض",
    badgeFa: "کمپین",
  },
  "banner.two-equal": {
    variantKey: "banner.two-equal",
    designNameFa: "بنر دوتایی — طرح هما",
    descriptionFa: "دو بنر مساوی کنار هم",
  },
  "banner.two-asymmetric": {
    variantKey: "banner.two-asymmetric",
    designNameFa: "بنر دوتایی نامتوازن — طرح شیدا",
    descriptionFa: "دو بنر با اندازه متفاوت",
  },
  "banner.three": {
    variantKey: "banner.three",
    designNameFa: "بنر سه‌تایی — طرح سارا",
    descriptionFa: "سه بنر هم‌ارتفاع در یک ردیف",
    badgeFa: "کمپین",
  },
  "banner.four-grid": {
    variantKey: "banner.four-grid",
    designNameFa: "بنر چهارخانه — طرح نیکا",
    descriptionFa: "چهار بنر در شبکه منظم",
  },
  "banner.one-large-two-small": {
    variantKey: "banner.one-large-two-small",
    designNameFa: "بنر یک‌بزرگ دودوچک — طرح رویا",
    descriptionFa: "یک بنر برجسته با دو بنر کوچک",
  },
  "banner.one-large-four-small": {
    variantKey: "banner.one-large-four-small",
    designNameFa: "بنر یک‌بزرگ چهارکوچک — طرح مهسا",
    descriptionFa: "یک بنر برجسته با چهار بنر کوچک",
  },
  "banner.eight-compact": {
    variantKey: "banner.eight-compact",
    designNameFa: "بنر هشت‌تایی فشرده — طرح الهام",
    descriptionFa: "هشت کاشی بنر فشرده",
  },
  "banner.mosaic-2x2": {
    variantKey: "banner.mosaic-2x2",
    designNameFa: "بنر چهارخانه (قدیمی) — طرح نیکا",
    descriptionFa: "هم‌ارز چهارخانه — مخفی از انتخاب",
  },

  "brand.logo-rail": {
    variantKey: "brand.logo-rail",
    designNameFa: "ردیف لوگوی برند — طرح آناهیتا",
    descriptionFa: "اسکرول افقی لوگوهای برند",
  },
  "brand.logo-grid": {
    variantKey: "brand.logo-grid",
    designNameFa: "شبکه لوگوی برند — طرح آتوسا",
    descriptionFa: "شبکه لوگوهای برند",
  },
  "brand.featured": {
    variantKey: "brand.featured",
    designNameFa: "برندهای ویژه — طرح آتنا",
    descriptionFa: "نمایش برجسته چند برند منتخب",
  },

  "reviews.card-carousel": {
    variantKey: "reviews.card-carousel",
    designNameFa: "اسلایدر نظرات — طرح دلارا",
    descriptionFa: "کارت نظر خریداران با اسلاید",
  },
  "reviews.compact-quotes": {
    variantKey: "reviews.compact-quotes",
    designNameFa: "نقل‌قول فشرده — طرح باران",
    descriptionFa: "نقل‌قول‌های کوتاه خریداران",
  },

  "article.magazine-rail": {
    variantKey: "article.magazine-rail",
    designNameFa: "ردیف مجله مقالات — طرح سپیده",
    descriptionFa: "ردیف افقی مقاله به سبک مجله",
  },
  "article.grid": {
    variantKey: "article.grid",
    designNameFa: "شبکه مقالات — طرح گلشن",
    descriptionFa: "شبکه کارت مقاله",
  },
  "article.featured-plus-list": {
    variantKey: "article.featured-plus-list",
    designNameFa: "مقاله ویژه و فهرست — طرح نسیم",
    descriptionFa: "یک مقاله ویژه کنار فهرست",
  },

  "promo.default": {
    variantKey: "promo.default",
    designNameFa: "پروموی تکی — طرح شعله",
    descriptionFa: "بنر تبلیغاتی تکی",
  },
  "richtext.default": {
    variantKey: "richtext.default",
    designNameFa: "متن ساده — طرح کاغذ",
    descriptionFa: "متن کنترل‌شده بدون قالب خام",
  },
  "nav.menu": {
    variantKey: "nav.menu",
    designNameFa: "فهرست پیوند — طرح نقشه",
    descriptionFa: "منوی فعال فروشگاه",
  },
};

export function getVariantDesignMeta(variantKey: string): VariantDesignMeta | undefined {
  return VARIANT_DESIGN_NAMES[variantKey];
}

/** Primary Admin display label — never persist this string as identity. */
export function variantDesignNameFa(variantKey: string): string {
  return VARIANT_DESIGN_NAMES[variantKey]?.designNameFa ?? variantKey;
}

export function variantDesignDescriptionFa(variantKey: string): string {
  return VARIANT_DESIGN_NAMES[variantKey]?.descriptionFa ?? "";
}
