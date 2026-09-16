import type {
  AdminSelectableDataSource,
  SectionTypeDefinition,
  VariantDefinition,
  VariantPreviewKind,
  VariantStatus,
} from "./types.ts";
import { BASE_SECTION_SETTINGS } from "./settings.ts";
import { RESPONSIVE_CONTRACTS } from "./responsive-contracts.ts";

const section = (
  key: string,
  nameFa: string,
  descriptionFa: string,
  defaultVariantKey: string,
  flags: { home: boolean; landing: boolean },
  defaultSurfaceRole: SectionTypeDefinition["defaultSurfaceRole"] = "section",
): SectionTypeDefinition => ({
  key,
  nameFa,
  descriptionFa,
  homeAllowed: flags.home,
  landingAllowed: flags.landing,
  defaultSurfaceRole,
  defaultVariantKey,
  settings: BASE_SECTION_SETTINGS,
});

type VariantOpts = {
  previewKind: VariantPreviewKind;
  recommendedUseFa?: string;
  sizePresetsSupported?: boolean;
  autoplaySupported?: boolean;
  dataSources?: readonly AdminSelectableDataSource[];
  previewMinItems?: number;
  previewTargetItems?: number;
};

/** Code-owned Store-preview slot counts (not Admin settings). */
const PREVIEW_CARDINALITY: Record<string, { min: number; target: number }> = {
  "hero.full-width": { min: 1, target: 1 },
  "hero.contained": { min: 1, target: 1 },
  "hero.split": { min: 1, target: 1 },
  "hero.side-promos": { min: 1, target: 1 },
  "hero.editorial": { min: 1, target: 1 },
  "story.circle": { min: 4, target: 6 },
  "story.image-circles": { min: 4, target: 6 },
  "story.rounded-cards": { min: 3, target: 5 },
  "story.icon-shortcuts": { min: 4, target: 6 },
  "category.image-cards": { min: 2, target: 4 },
  "category.compact-tiles": { min: 4, target: 6 },
  "category.horizontal-rail": { min: 3, target: 5 },
  "category.editorial-tiles": { min: 2, target: 4 },
  "product.card-carousel": { min: 2, target: 4 },
  "product.grid": { min: 4, target: 6 },
  "product.compact-rows": { min: 3, target: 5 },
  "product.category-columns": { min: 3, target: 6 },
  "product.featured-plus-rail": { min: 3, target: 5 },
  "product.tabbed": { min: 2, target: 4 },
  "product.large-cards": { min: 2, target: 3 },
  "product.minimal-list": { min: 3, target: 5 },
  "ranked.horizontal": { min: 2, target: 4 },
  "ranked.grid": { min: 4, target: 6 },
  "ranked.ticker": { min: 3, target: 5 },
  "ranked.multi-column": { min: 3, target: 6 },
  "banner.single": { min: 1, target: 1 },
  "banner.two-equal": { min: 2, target: 2 },
  "banner.two-asymmetric": { min: 2, target: 2 },
  "banner.three": { min: 3, target: 3 },
  "banner.four-grid": { min: 4, target: 4 },
  "banner.one-large-two-small": { min: 3, target: 3 },
  "banner.one-large-four-small": { min: 5, target: 5 },
  "banner.eight-compact": { min: 4, target: 8 },
  "banner.mosaic-2x2": { min: 4, target: 4 },
  "brand.logo-rail": { min: 4, target: 6 },
  "brand.logo-grid": { min: 4, target: 8 },
  "brand.featured": { min: 2, target: 3 },
  "reviews.card-carousel": { min: 2, target: 3 },
  "reviews.compact-quotes": { min: 2, target: 3 },
  "article.magazine-rail": { min: 2, target: 3 },
  "article.grid": { min: 2, target: 3 },
  "article.featured-plus-list": { min: 2, target: 3 },
  "promo.default": { min: 1, target: 1 },
  "richtext.default": { min: 1, target: 1 },
  "nav.menu": { min: 0, target: 0 },
};

const variant = (
  key: string,
  sectionTypeKey: string,
  nameFa: string,
  descriptionFa: string,
  status: VariantStatus,
  opts: VariantOpts,
): VariantDefinition => {
  const card = PREVIEW_CARDINALITY[key];
  return {
    key,
    sectionTypeKey,
    nameFa,
    descriptionFa,
    status,
    implemented: status === "Existing" || status === "ReusableViaAdapter",
    previewKind: opts.previewKind,
    recommendedUseFa: opts.recommendedUseFa,
    sizePresetsSupported: opts.sizePresetsSupported ?? false,
    autoplaySupported: opts.autoplaySupported ?? false,
    dataSources: opts.dataSources ?? (["Manual"] as const),
    responsiveContractKey: key,
    settings: BASE_SECTION_SETTINGS,
    previewMinItems: opts.previewMinItems ?? card?.min,
    previewTargetItems: opts.previewTargetItems ?? card?.target,
  };
};

/** First-wave shared Section Types (prefer Variants over new Types). */
export const SECTION_TYPES: SectionTypeDefinition[] = [
  section("HeroCarousel", "اسلایدر اصلی", "بنر متحرک بالای صفحه", "hero.full-width", { home: true, landing: true }, "page"),
  section("StoryRail", "میانبر استوری", "میانبرهای دایره‌ای یا کارت‌گرد", "story.circle", { home: true, landing: true }),
  section("CategoryShowcase", "نمایش دسته‌ها", "کارت، کاشی یا ردیف افقی دسته", "category.image-cards", { home: true, landing: true }),
  section("ProductShowcase", "نمایش کالا", "اسلایدر، شبکه یا ردیف کالا", "product.card-carousel", { home: true, landing: true }),
  section("ProductRankedList", "رتبه‌بندی کالا", "فهرست رتبه‌دار با منبع واقعی", "ranked.horizontal", { home: true, landing: true }),
  section("BannerShowcase", "نمایش بنر", "شبکه‌های بنر کنترل‌شده", "banner.single", { home: true, landing: true }, "alternate"),
  section("BrandShowcase", "نمایش برند", "ردیف یا شبکه لوگو", "brand.logo-rail", { home: true, landing: true }),
  section("PromoSection", "پرومو", "بنر تبلیغاتی تکی", "promo.default", { home: true, landing: true }, "accent"),
  section("ArticleShowcase", "نمایش مقالات", "ردیف یا شبکه مقاله", "article.magazine-rail", { home: true, landing: true }),
  section("ReviewsShowcase", "نظر خریداران", "اسلایدر یا نقل فشرده", "reviews.card-carousel", { home: true, landing: true }),
  section("RichText", "متن ساده", "متن کنترل‌شده بدون قالب‌بندی خام", "richtext.default", { home: false, landing: true }),
  section("NavigationMenu", "فهرست پیوند", "منوی فعال فروشگاه", "nav.menu", { home: false, landing: true }),
];

const DS_MANUAL = ["Manual"] as const satisfies readonly AdminSelectableDataSource[];
const DS_PRODUCT = ["Manual", "Category", "Brand", "Newest"] as const satisfies readonly AdminSelectableDataSource[];
const DS_CATEGORY = ["Manual", "Category"] as const satisfies readonly AdminSelectableDataSource[];
const DS_BRAND = ["Manual", "Brand"] as const satisfies readonly AdminSelectableDataSource[];
const DS_ARTICLE = ["LatestArticles", "Manual"] as const satisfies readonly AdminSelectableDataSource[];
const DS_REVIEW = ["ApprovedReviews"] as const satisfies readonly AdminSelectableDataSource[];

/** Substantial initial Variant catalog; most marked NewRequiredLater until built. */
export const VARIANTS: VariantDefinition[] = [
  variant("hero.full-width", "HeroCarousel", "تمام‌عرض", "اسلاید تمام‌عرض", "Existing", {
    previewKind: "hero-slider", recommendedUseFa: "بالای صفحه اصلی", sizePresetsSupported: true, autoplaySupported: true, dataSources: DS_MANUAL,
  }),
  variant("hero.contained", "HeroCarousel", "داخل کانتینر", "اسلاید با گوشه گرد", "ReusableViaAdapter", {
    previewKind: "hero-contained", recommendedUseFa: "لندینگ و صفحات داخلی", sizePresetsSupported: true, autoplaySupported: true, dataSources: DS_MANUAL,
  }),
  variant("hero.split", "HeroCarousel", "دو ستون", "تصویر + متن", "ReusableViaAdapter", {
    previewKind: "hero-contained", sizePresetsSupported: true, dataSources: DS_MANUAL,
  }),
  variant("hero.side-promos", "HeroCarousel", "اسلایدر با پروموی کناری", "اسلایدر اصلی + دو پرومو", "ReusableViaAdapter", {
    previewKind: "hero-slider", sizePresetsSupported: true, dataSources: DS_MANUAL,
  }),
  variant("hero.editorial", "HeroCarousel", "تحریریه", "تصویر تمام‌عرض با عنوان و متن برجسته", "Existing", {
    previewKind: "hero-contained", recommendedUseFa: "مناسب فروشگاه‌های تصویری", sizePresetsSupported: true, dataSources: DS_MANUAL,
  }),

  variant("story.circle", "StoryRail", "دایره استوری", "دایره‌های افقی با حاشیه", "Existing", {
    previewKind: "story-circles", recommendedUseFa: "میانبر سریع بالای صفحه", autoplaySupported: true, dataSources: DS_MANUAL,
  }),
  variant("story.image-circles", "StoryRail", "دایره پرتصویر", "دایرهٔ پر از تصویر بدون حاشیهٔ خالی", "ReusableViaAdapter", {
    previewKind: "story-circles", recommendedUseFa: "مناسب فروشگاه‌های تصویری", autoplaySupported: true, dataSources: DS_MANUAL,
  }),
  variant("story.rounded-cards", "StoryRail", "کارت گرد", "میانبر کارت‌گرد", "ReusableViaAdapter", {
    previewKind: "story-cards", recommendedUseFa: "مناسب کمپین", dataSources: DS_MANUAL,
  }),
  variant("story.icon-shortcuts", "StoryRail", "میانبر آیکون", "آیکون مربعی + برچسب کوتاه", "Existing", {
    previewKind: "story-cards", recommendedUseFa: "مناسب نمایش فشرده", dataSources: DS_MANUAL,
  }),

  variant("category.image-cards", "CategoryShowcase", "کارت تصویری", "کارت دسته با تصویر", "Existing", {
    previewKind: "category-cards", recommendedUseFa: "ویترین دسته‌ها", dataSources: DS_CATEGORY,
  }),
  variant("category.compact-tiles", "CategoryShowcase", "کاشی فشرده", "شبکه فشرده", "ReusableViaAdapter", {
    previewKind: "category-tiles", recommendedUseFa: "مناسب نمایش فشرده", dataSources: DS_CATEGORY,
  }),
  variant("category.horizontal-rail", "CategoryShowcase", "ردیف افقی", "اسکرول افقی", "ReusableViaAdapter", {
    previewKind: "category-rail", dataSources: DS_CATEGORY,
  }),
  variant("category.editorial-tiles", "CategoryShowcase", "کاشی تحریریه", "کاشی بزرگ با عنوان روی تصویر", "Existing", {
    previewKind: "category-tiles", recommendedUseFa: "مناسب فروشگاه‌های تصویری", dataSources: DS_CATEGORY,
  }),

  variant("product.card-carousel", "ProductShowcase", "اسلایدر کارت محصول", "ردیف افقی کارت کالا", "Existing", {
    previewKind: "product-carousel", recommendedUseFa: "پیشنهاد و تازه‌ها", autoplaySupported: true, dataSources: DS_PRODUCT,
  }),
  variant("product.grid", "ProductShowcase", "شبکه کالا", "شبکه چندستونه", "ReusableViaAdapter", {
    previewKind: "product-grid", recommendedUseFa: "مناسب نمایش فشرده", dataSources: DS_PRODUCT,
  }),
  variant("product.compact-rows", "ProductShowcase", "ردیف‌های فشرده", "لیست فشرده", "Existing", {
    previewKind: "product-rows", recommendedUseFa: "مناسب نمایش فشرده", dataSources: DS_PRODUCT,
  }),
  variant("product.category-columns", "ProductShowcase", "ستون‌های دسته‌بندی", "چند ستون کالا", "Existing", {
    previewKind: "product-columns", recommendedUseFa: "نمایش چندستونه", dataSources: DS_PRODUCT,
  }),
  variant("product.featured-plus-rail", "ProductShowcase", "ویژه + ردیف", "یک کالای ویژه کنار ردیف", "ReusableViaAdapter", {
    previewKind: "product-carousel", recommendedUseFa: "مناسب فروشگاه‌های تصویری", dataSources: DS_PRODUCT,
  }),
  variant("product.tabbed", "ProductShowcase", "با زبانه", "چند زبانه کالا با یک ردیف مشترک", "Existing", {
    previewKind: "product-carousel", recommendedUseFa: "مناسب کمپین", autoplaySupported: true, dataSources: DS_PRODUCT,
  }),
  variant("product.large-cards", "ProductShowcase", "کارت بزرگ", "کارت درشت با تصویر بزرگ", "Existing", {
    previewKind: "product-grid", recommendedUseFa: "مناسب فروشگاه‌های تصویری", dataSources: DS_PRODUCT,
  }),
  variant("product.minimal-list", "ProductShowcase", "فهرست ساده", "لیست ساده عنوان و قیمت", "Existing", {
    previewKind: "product-rows", recommendedUseFa: "مناسب نمایش فشرده", dataSources: DS_PRODUCT,
  }),

  variant("ranked.horizontal", "ProductRankedList", "افقی رتبه‌دار", "ردیف افقی با رتبه", "ReusableViaAdapter", {
    previewKind: "ranked-rail", dataSources: DS_PRODUCT,
  }),
  variant("ranked.grid", "ProductRankedList", "شبکه رتبه‌دار", "شبکه با رتبه", "ReusableViaAdapter", {
    previewKind: "product-grid", dataSources: DS_PRODUCT,
  }),
  variant("ranked.ticker", "ProductRankedList", "نوار فشرده", "نوار افقی فشرده با شماره رتبه", "Existing", {
    previewKind: "ranked-rail", recommendedUseFa: "مناسب نمایش فشرده", dataSources: DS_PRODUCT,
  }),
  variant("ranked.multi-column", "ProductRankedList", "چند ستون", "چند ستون رتبه", "ReusableViaAdapter", {
    previewKind: "ranked-columns", dataSources: DS_PRODUCT,
  }),

  variant("banner.single", "BannerShowcase", "تک بنر", "یک بنر تمام‌عرض", "Existing", {
    previewKind: "banner-single", recommendedUseFa: "مناسب کمپین", sizePresetsSupported: true, dataSources: DS_MANUAL,
  }),
  variant("banner.two-equal", "BannerShowcase", "دو بنر مساوی", "دو بنر برابر", "ReusableViaAdapter", {
    previewKind: "banner-two", sizePresetsSupported: true, dataSources: DS_MANUAL,
  }),
  variant("banner.two-asymmetric", "BannerShowcase", "دو نامتقارن", "دو بنر ناهمسان", "ReusableViaAdapter", {
    previewKind: "banner-two", sizePresetsSupported: true, dataSources: DS_MANUAL,
  }),
  variant("banner.three", "BannerShowcase", "سه تایی", "سه بنر هم‌ارتفاع در یک ردیف", "Existing", {
    previewKind: "banner-three", recommendedUseFa: "مناسب کمپین", sizePresetsSupported: true, dataSources: DS_MANUAL,
  }),
  variant("banner.four-grid", "BannerShowcase", "چهارخانه", "چهار بنر در شبکه منظم", "ReusableViaAdapter", {
    previewKind: "banner-four", sizePresetsSupported: true, dataSources: DS_MANUAL,
  }),
  variant("banner.one-large-two-small", "BannerShowcase", "یک بزرگ دو کوچک", "ترکیب سه‌تایی با یک بنر برجسته", "ReusableViaAdapter", {
    previewKind: "banner-mosaic", sizePresetsSupported: true, dataSources: DS_MANUAL,
  }),
  variant("banner.one-large-four-small", "BannerShowcase", "یک بزرگ چهار کوچک", "ترکیب پنج‌تایی با یک بنر برجسته", "ReusableViaAdapter", {
    previewKind: "banner-mosaic", sizePresetsSupported: true, dataSources: DS_MANUAL,
  }),
  variant("banner.eight-compact", "BannerShowcase", "هشت فشرده", "هشت کاشی فشرده", "ReusableViaAdapter", {
    previewKind: "banner-four", sizePresetsSupported: true, dataSources: DS_MANUAL,
  }),
  // Hidden alias of banner.four-grid (identical layout). Kept for legacy drafts; not admin-selectable.
  variant("banner.mosaic-2x2", "BannerShowcase", "چهارخانه (قدیمی)", "هم‌ارز چهارخانه — مخفی از انتخاب", "NewRequiredLater", {
    previewKind: "banner-four", sizePresetsSupported: true, dataSources: DS_MANUAL,
  }),

  variant("brand.logo-rail", "BrandShowcase", "ردیف لوگو", "اسکرول افقی لوگو", "Existing", {
    previewKind: "brand-rail", autoplaySupported: true, dataSources: DS_BRAND,
  }),
  variant("brand.logo-grid", "BrandShowcase", "شبکه لوگو", "شبکه برند", "ReusableViaAdapter", {
    previewKind: "brand-grid", dataSources: DS_BRAND,
  }),
  variant("brand.featured", "BrandShowcase", "برند ویژه", "کارت برند بزرگ با تأکید بصری", "Existing", {
    previewKind: "brand-grid", recommendedUseFa: "مناسب فروشگاه‌های تصویری", dataSources: DS_BRAND,
  }),

  variant("reviews.card-carousel", "ReviewsShowcase", "اسلایدر نظر", "کارت نظر", "Existing", {
    previewKind: "reviews-carousel", autoplaySupported: true, dataSources: DS_REVIEW,
  }),
  variant("reviews.compact-quotes", "ReviewsShowcase", "نقل فشرده", "نقل کوتاه در شبکه آرام", "Existing", {
    previewKind: "reviews-carousel", recommendedUseFa: "مناسب نمایش فشرده", dataSources: DS_REVIEW,
  }),

  variant("article.magazine-rail", "ArticleShowcase", "ردیف مجله", "کارت مقاله افقی", "Existing", {
    previewKind: "article-rail", autoplaySupported: true, dataSources: DS_ARTICLE,
  }),
  variant("article.grid", "ArticleShowcase", "شبکه مقاله", "شبکه مطالب", "ReusableViaAdapter", {
    previewKind: "article-grid", dataSources: DS_ARTICLE,
  }),
  variant("article.featured-plus-list", "ArticleShowcase", "ویژه + فهرست", "یک مطلب برجسته کنار فهرست", "Existing", {
    previewKind: "article-rail", recommendedUseFa: "مناسب فروشگاه‌های تصویری", dataSources: DS_ARTICLE,
  }),

  variant("promo.default", "PromoSection", "پروموی پیش‌فرض", "بنر پرومو تکی", "Existing", {
    previewKind: "promo", sizePresetsSupported: true, dataSources: DS_MANUAL,
  }),
  variant("richtext.default", "RichText", "متن ساده", "متن ساده بدون قالب‌بندی خام", "Existing", {
    previewKind: "richtext", dataSources: DS_MANUAL,
  }),
  variant("nav.menu", "NavigationMenu", "منوی پیوند", "منوی فعال", "Existing", {
    previewKind: "nav", dataSources: DS_MANUAL,
  }),
];

export const INDUSTRY_TEMPLATE_SEEDS: Array<{
  templateKey: string;
  nameFa: string;
  industry: string;
  descriptionFa: string;
  sectionPresetList: Array<{ sectionTypeKey: string; variantKey: string; dataSourceIntent: string }>;
}> = [
  {
    templateKey: "fashion",
    nameFa: "پوشاک",
    industry: "Fashion",
    descriptionFa: "افتتاح تحریریه‌ای، کشف استوری و دسته، ویترین کالای تصویری و کمپین دوتایی",
    sectionPresetList: [
      { sectionTypeKey: "HeroCarousel", variantKey: "hero.editorial", dataSourceIntent: "Manual" },
      { sectionTypeKey: "StoryRail", variantKey: "story.circle", dataSourceIntent: "Manual" },
      { sectionTypeKey: "CategoryShowcase", variantKey: "category.editorial-tiles", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ProductShowcase", variantKey: "product.large-cards", dataSourceIntent: "Newest" },
      { sectionTypeKey: "BannerShowcase", variantKey: "banner.two-equal", dataSourceIntent: "Manual" },
      { sectionTypeKey: "BrandShowcase", variantKey: "brand.logo-rail", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ReviewsShowcase", variantKey: "reviews.compact-quotes", dataSourceIntent: "ApprovedReviews" },
    ],
  },
  {
    templateKey: "auto-parts",
    nameFa: "لوازم یدکی خودرو",
    industry: "AutoParts",
    descriptionFa: "اولویت دسته و برند، فهرست فشرده کالا و تیکر رتبه بدون شلوغی تحریریه‌ای",
    sectionPresetList: [
      { sectionTypeKey: "HeroCarousel", variantKey: "hero.contained", dataSourceIntent: "Manual" },
      { sectionTypeKey: "CategoryShowcase", variantKey: "category.compact-tiles", dataSourceIntent: "Manual" },
      { sectionTypeKey: "BrandShowcase", variantKey: "brand.logo-grid", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ProductShowcase", variantKey: "product.minimal-list", dataSourceIntent: "Category" },
      { sectionTypeKey: "ProductRankedList", variantKey: "ranked.ticker", dataSourceIntent: "Newest" },
      { sectionTypeKey: "BannerShowcase", variantKey: "banner.single", dataSourceIntent: "Manual" },
    ],
  },
  {
    templateKey: "building-supplies",
    nameFa: "لوازم ساختمانی",
    industry: "BuildingSupplies",
    descriptionFa: "دسته سنگین، شبکه کاربردی کالا، بنر سه‌تایی و اعتبار برند",
    sectionPresetList: [
      { sectionTypeKey: "CategoryShowcase", variantKey: "category.image-cards", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ProductShowcase", variantKey: "product.grid", dataSourceIntent: "Category" },
      { sectionTypeKey: "BannerShowcase", variantKey: "banner.three", dataSourceIntent: "Manual" },
      { sectionTypeKey: "BrandShowcase", variantKey: "brand.featured", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ArticleShowcase", variantKey: "article.grid", dataSourceIntent: "LatestArticles" },
    ],
  },
  {
    templateKey: "tools-hardware",
    nameFa: "ابزار و یراق",
    industry: "ToolsHardware",
    descriptionFa: "ریتم فشرده دسته، ردیف کالا، رتبه چندستونه و بنر فشرده",
    sectionPresetList: [
      { sectionTypeKey: "CategoryShowcase", variantKey: "category.compact-tiles", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ProductShowcase", variantKey: "product.compact-rows", dataSourceIntent: "Brand" },
      { sectionTypeKey: "ProductRankedList", variantKey: "ranked.multi-column", dataSourceIntent: "Newest" },
      { sectionTypeKey: "BannerShowcase", variantKey: "banner.eight-compact", dataSourceIntent: "Manual" },
      { sectionTypeKey: "BrandShowcase", variantKey: "brand.logo-rail", dataSourceIntent: "Manual" },
    ],
  },
  {
    templateKey: "tile-ceramic",
    nameFa: "کاشی و سرامیک",
    industry: "TileCeramic",
    descriptionFa: "تصویر بزرگ، تأکید کلکسیون/دسته و بنرهای موزاییکی با تراکم کالای محدود",
    sectionPresetList: [
      { sectionTypeKey: "HeroCarousel", variantKey: "hero.side-promos", dataSourceIntent: "Manual" },
      { sectionTypeKey: "CategoryShowcase", variantKey: "category.editorial-tiles", dataSourceIntent: "Manual" },
      { sectionTypeKey: "BannerShowcase", variantKey: "banner.one-large-four-small", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ProductShowcase", variantKey: "product.large-cards", dataSourceIntent: "Newest" },
      { sectionTypeKey: "BannerShowcase", variantKey: "banner.four-grid", dataSourceIntent: "Manual" },
    ],
  },
  {
    templateKey: "interior-decor",
    nameFa: "دکوراسیون داخلی",
    industry: "InteriorDecor",
    descriptionFa: "تصویر بزرگ، کاشی تحریریه، کالای ویژه و فاصله آرام",
    sectionPresetList: [
      { sectionTypeKey: "HeroCarousel", variantKey: "hero.split", dataSourceIntent: "Manual" },
      { sectionTypeKey: "CategoryShowcase", variantKey: "category.editorial-tiles", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ProductShowcase", variantKey: "product.featured-plus-rail", dataSourceIntent: "Newest" },
      { sectionTypeKey: "ArticleShowcase", variantKey: "article.featured-plus-list", dataSourceIntent: "LatestArticles" },
      { sectionTypeKey: "BannerShowcase", variantKey: "banner.one-large-two-small", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ReviewsShowcase", variantKey: "reviews.card-carousel", dataSourceIntent: "ApprovedReviews" },
    ],
  },
  {
    templateKey: "home-appliance",
    nameFa: "لوازم خانگی",
    industry: "HomeAppliance",
    descriptionFa: "برندمحور، مقایسه تب‌دار کالا و بنرهای کاربردی",
    sectionPresetList: [
      { sectionTypeKey: "HeroCarousel", variantKey: "hero.contained", dataSourceIntent: "Manual" },
      { sectionTypeKey: "BrandShowcase", variantKey: "brand.featured", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ProductShowcase", variantKey: "product.tabbed", dataSourceIntent: "Category" },
      { sectionTypeKey: "ProductRankedList", variantKey: "ranked.grid", dataSourceIntent: "Newest" },
      { sectionTypeKey: "BannerShowcase", variantKey: "banner.two-equal", dataSourceIntent: "Manual" },
    ],
  },
  {
    templateKey: "shoes",
    nameFa: "کفش",
    industry: "Shoes",
    descriptionFa: "کمپین تصویری بالا، کشف دسته و استوری، ردیف کالای بصری متمایز از پوشاک",
    sectionPresetList: [
      { sectionTypeKey: "HeroCarousel", variantKey: "hero.full-width", dataSourceIntent: "Manual" },
      { sectionTypeKey: "StoryRail", variantKey: "story.rounded-cards", dataSourceIntent: "Manual" },
      { sectionTypeKey: "CategoryShowcase", variantKey: "category.horizontal-rail", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ProductShowcase", variantKey: "product.card-carousel", dataSourceIntent: "Newest" },
      { sectionTypeKey: "BannerShowcase", variantKey: "banner.two-asymmetric", dataSourceIntent: "Manual" },
    ],
  },
  {
    templateKey: "plants",
    nameFa: "گل و گیاه",
    industry: "Plants",
    descriptionFa: "فضای آرام تصویری، کشف دسته، ترکیب تحریریه و کالا با تراکم نرم",
    sectionPresetList: [
      { sectionTypeKey: "HeroCarousel", variantKey: "hero.editorial", dataSourceIntent: "Manual" },
      { sectionTypeKey: "StoryRail", variantKey: "story.icon-shortcuts", dataSourceIntent: "Manual" },
      { sectionTypeKey: "CategoryShowcase", variantKey: "category.horizontal-rail", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ProductShowcase", variantKey: "product.featured-plus-rail", dataSourceIntent: "Newest" },
      { sectionTypeKey: "ArticleShowcase", variantKey: "article.featured-plus-list", dataSourceIntent: "LatestArticles" },
      { sectionTypeKey: "ReviewsShowcase", variantKey: "reviews.compact-quotes", dataSourceIntent: "ApprovedReviews" },
    ],
  },
  {
    templateKey: "beauty",
    nameFa: "آرایشی بهداشتی",
    industry: "Beauty",
    descriptionFa: "ریتم برند، استوری و کالای چندزبانه با بنر کمپین و نظرات فشرده",
    sectionPresetList: [
      { sectionTypeKey: "StoryRail", variantKey: "story.image-circles", dataSourceIntent: "Manual" },
      { sectionTypeKey: "BrandShowcase", variantKey: "brand.featured", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ProductShowcase", variantKey: "product.tabbed", dataSourceIntent: "Category" },
      { sectionTypeKey: "BannerShowcase", variantKey: "banner.three", dataSourceIntent: "Manual" },
      { sectionTypeKey: "PromoSection", variantKey: "promo.default", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ReviewsShowcase", variantKey: "reviews.compact-quotes", dataSourceIntent: "ApprovedReviews" },
    ],
  },
];

/** Shared SectionType → Host Landing PascalCase type (storage). */
export const SECTION_TO_LANDING_HOST: Record<string, string> = {
  HeroCarousel: "Hero",
  StoryRail: "StoryRail",
  CategoryShowcase: "CategoryGrid",
  ProductShowcase: "ProductCollection",
  ProductRankedList: "ProductCollection",
  BannerShowcase: "BannerShowcase",
  BrandShowcase: "BrandStrip",
  PromoSection: "PromoBanner",
  ArticleShowcase: "ArticleList",
  ReviewsShowcase: "Reviews",
  RichText: "RichText",
  NavigationMenu: "NavigationMenu",
};

export function getSectionType(key: string): SectionTypeDefinition | undefined {
  return SECTION_TYPES.find((s) => s.key === key);
}

export function getVariant(key: string): VariantDefinition | undefined {
  return VARIANTS.find((v) => v.key === key);
}

export function variantsForSection(sectionTypeKey: string): VariantDefinition[] {
  return VARIANTS.filter((v) => v.sectionTypeKey === sectionTypeKey);
}

export function isVariantImplemented(key: string): boolean {
  return Boolean(getVariant(key)?.implemented);
}

export function implementedVariantsForSection(sectionTypeKey: string): VariantDefinition[] {
  return variantsForSection(sectionTypeKey).filter((v) => v.implemented);
}

export function assertVariantCompatible(sectionTypeKey: string, variantKey: string): VariantDefinition {
  const v = getVariant(variantKey);
  if (!v) throw new Error(`Unknown variant: ${variantKey}`);
  if (v.sectionTypeKey !== sectionTypeKey) {
    throw new Error(`Variant ${variantKey} is not compatible with section ${sectionTypeKey}`);
  }
  return v;
}

export function landingHostTypeForSection(sectionTypeKey: string): string | undefined {
  return SECTION_TO_LANDING_HOST[sectionTypeKey];
}

export function assertRegistryIntegrity(): void {
  const sectionKeys = new Set<string>();
  for (const s of SECTION_TYPES) {
    if (sectionKeys.has(s.key)) throw new Error(`Duplicate section type: ${s.key}`);
    sectionKeys.add(s.key);
    if (!VARIANTS.some((v) => v.key === s.defaultVariantKey && v.sectionTypeKey === s.key)) {
      throw new Error(`Default variant missing for ${s.key}`);
    }
  }
  const variantKeys = new Set<string>();
  for (const v of VARIANTS) {
    if (variantKeys.has(v.key)) throw new Error(`Duplicate variant: ${v.key}`);
    variantKeys.add(v.key);
    if (!sectionKeys.has(v.sectionTypeKey)) throw new Error(`Orphan variant ${v.key}`);
    if (!RESPONSIVE_CONTRACTS[v.responsiveContractKey]) {
      throw new Error(`Missing responsive contract for ${v.key}`);
    }
    for (const ds of v.dataSources) {
      if (["BestSelling", "MostViewed", "Discounted", "Featured", "HotTrending"].includes(ds)) {
        throw new Error(`Variant ${v.key} exposes unsupported admin data source ${ds}`);
      }
    }
  }
}
