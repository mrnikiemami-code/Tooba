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
};

const variant = (
  key: string,
  sectionTypeKey: string,
  nameFa: string,
  descriptionFa: string,
  status: VariantStatus,
  opts: VariantOpts,
): VariantDefinition => ({
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
});

/** First-wave shared Section Types (prefer Variants over new Types). */
export const SECTION_TYPES: SectionTypeDefinition[] = [
  section("HeroCarousel", "اسلایدر اصلی", "بنر/کاروسل بالای صفحه", "hero.full-width", { home: true, landing: true }, "page"),
  section("StoryRail", "ریل استوری", "میانبرهای دایره‌ای یا کارت‌گرد", "story.circle", { home: true, landing: true }),
  section("CategoryShowcase", "نمایش دسته‌ها", "کارت/کاشی/ریل دسته", "category.image-cards", { home: true, landing: true }),
  section("ProductShowcase", "نمایش کالا", "کاروسل/شبکه/ردیف کالا", "product.card-carousel", { home: true, landing: true }),
  section("ProductRankedList", "رتبه‌بندی کالا", "پرفروش/پربازدید/داغ — فقط با منبع واقعی", "ranked.horizontal", { home: true, landing: true }),
  section("BannerShowcase", "نمایش بنر", "شبکه‌های بنر کنترل‌شده", "banner.single", { home: true, landing: true }, "alternate"),
  section("BrandShowcase", "نمایش برند", "ریل یا شبکه لوگو", "brand.logo-rail", { home: true, landing: true }),
  section("PromoSection", "پرومو", "بنر تبلیغاتی تکی", "promo.default", { home: true, landing: true }, "accent"),
  section("ArticleShowcase", "نمایش مقالات", "ریل یا شبکه مقاله", "article.magazine-rail", { home: true, landing: true }),
  section("ReviewsShowcase", "نظر خریداران", "کاروسل یا نقل فشرده", "reviews.card-carousel", { home: true, landing: true }),
  section("RichText", "متن ساده", "متن کنترل‌شده بدون HTML خام", "richtext.default", { home: false, landing: true }),
  section("NavigationMenu", "فهرست پیوند", "منوی فعال فروشگاه", "nav.menu", { home: false, landing: true }),
];

const DS_MANUAL = ["Manual"] as const satisfies readonly AdminSelectableDataSource[];
const DS_PRODUCT = ["Manual", "Category", "Brand", "Newest"] as const satisfies readonly AdminSelectableDataSource[];
const DS_CATEGORY = ["Manual", "Category"] as const satisfies readonly AdminSelectableDataSource[];
const DS_BRAND = ["Manual", "Brand"] as const satisfies readonly AdminSelectableDataSource[];
const DS_ARTICLE = ["LatestArticles"] as const satisfies readonly AdminSelectableDataSource[];
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
  variant("hero.side-promos", "HeroCarousel", "هیرو با پروموی کناری", "هیرو + دو پرومو", "ReusableViaAdapter", {
    previewKind: "hero-slider", sizePresetsSupported: true, dataSources: DS_MANUAL,
  }),
  variant("hero.editorial", "HeroCarousel", "تحریریه", "کپشن قوی", "NewRequiredLater", { previewKind: "hero-contained", dataSources: DS_MANUAL }),

  variant("story.circle", "StoryRail", "دایره استوری", "دایره‌های افقی", "Existing", {
    previewKind: "story-circles", recommendedUseFa: "میانبر سریع بالای صفحه", autoplaySupported: true, dataSources: DS_MANUAL,
  }),
  variant("story.image-circles", "StoryRail", "دایره تصویری", "پر از تصویر", "ReusableViaAdapter", {
    previewKind: "story-circles", autoplaySupported: true, dataSources: DS_MANUAL,
  }),
  variant("story.rounded-cards", "StoryRail", "کارت گرد", "میانبر کارت‌گرد", "ReusableViaAdapter", {
    previewKind: "story-cards", dataSources: DS_MANUAL,
  }),
  variant("story.icon-shortcuts", "StoryRail", "میانبر آیکون", "آیکون + برچسب", "NewRequiredLater", { previewKind: "story-cards", dataSources: DS_MANUAL }),

  variant("category.image-cards", "CategoryShowcase", "کارت تصویری", "کارت دسته با تصویر", "Existing", {
    previewKind: "category-cards", recommendedUseFa: "ویترین دسته‌ها", dataSources: DS_CATEGORY,
  }),
  variant("category.compact-tiles", "CategoryShowcase", "کاشی فشرده", "شبکه فشرده", "ReusableViaAdapter", {
    previewKind: "category-tiles", dataSources: DS_CATEGORY,
  }),
  variant("category.horizontal-rail", "CategoryShowcase", "ریل افقی", "اسکرول افقی", "ReusableViaAdapter", {
    previewKind: "category-rail", dataSources: DS_CATEGORY,
  }),
  variant("category.editorial-tiles", "CategoryShowcase", "کاشی تحریریه", "کاشی بزرگ", "NewRequiredLater", { previewKind: "category-tiles", dataSources: DS_CATEGORY }),

  variant("product.card-carousel", "ProductShowcase", "اسلایدر کارت محصول", "ریل کارت کالا", "Existing", {
    previewKind: "product-carousel", recommendedUseFa: "پیشنهاد و تازه‌ها", autoplaySupported: true, dataSources: DS_PRODUCT,
  }),
  variant("product.grid", "ProductShowcase", "شبکه کالا", "شبکه چندستونه", "ReusableViaAdapter", {
    previewKind: "product-grid", dataSources: DS_PRODUCT,
  }),
  variant("product.compact-rows", "ProductShowcase", "ردیف‌های فشرده", "لیست فشرده", "Existing", {
    previewKind: "product-rows", recommendedUseFa: "پربازدید فشرده", dataSources: DS_PRODUCT,
  }),
  variant("product.category-columns", "ProductShowcase", "ستون‌های دسته‌بندی", "پرفروش ستونی", "Existing", {
    previewKind: "product-columns", recommendedUseFa: "پرفروش چندستونه", dataSources: DS_PRODUCT,
  }),
  variant("product.featured-plus-rail", "ProductShowcase", "ویژه + ریل", "یک ویژه + ریل", "ReusableViaAdapter", {
    previewKind: "product-carousel", dataSources: DS_PRODUCT,
  }),
  variant("product.tabbed", "ProductShowcase", "تب‌دار", "چند تب کالا", "NewRequiredLater", { previewKind: "product-carousel", dataSources: DS_PRODUCT }),
  variant("product.large-cards", "ProductShowcase", "کارت بزرگ", "کارت درشت", "NewRequiredLater", { previewKind: "product-grid", dataSources: DS_PRODUCT }),
  variant("product.minimal-list", "ProductShowcase", "فهرست مینیمال", "لیست ساده", "NewRequiredLater", { previewKind: "product-rows", dataSources: DS_PRODUCT }),

  variant("ranked.horizontal", "ProductRankedList", "افقی رتبه‌دار", "ریل رتبه", "ReusableViaAdapter", {
    previewKind: "ranked-rail", dataSources: DS_PRODUCT,
  }),
  variant("ranked.grid", "ProductRankedList", "شبکه رتبه‌دار", "شبکه با رتبه", "ReusableViaAdapter", {
    previewKind: "product-grid", dataSources: DS_PRODUCT,
  }),
  variant("ranked.ticker", "ProductRankedList", "تیکر فشرده", "نوار فشرده", "NewRequiredLater", { previewKind: "ranked-rail", dataSources: DS_PRODUCT }),
  variant("ranked.multi-column", "ProductRankedList", "چند ستون", "چند ستون رتبه", "ReusableViaAdapter", {
    previewKind: "ranked-columns", dataSources: DS_PRODUCT,
  }),

  variant("banner.single", "BannerShowcase", "تک بنر", "یک بنر", "Existing", {
    previewKind: "banner-single", sizePresetsSupported: true, dataSources: DS_MANUAL,
  }),
  variant("banner.two-equal", "BannerShowcase", "دو بنر مساوی", "دو بنر برابر", "ReusableViaAdapter", {
    previewKind: "banner-two", sizePresetsSupported: true, dataSources: DS_MANUAL,
  }),
  variant("banner.two-asymmetric", "BannerShowcase", "دو نامتقارن", "دو بنر ناهمسان", "ReusableViaAdapter", {
    previewKind: "banner-two", sizePresetsSupported: true, dataSources: DS_MANUAL,
  }),
  variant("banner.three", "BannerShowcase", "سه تایی", "سه بنر", "ReusableViaAdapter", {
    previewKind: "banner-three", sizePresetsSupported: true, dataSources: DS_MANUAL,
  }),
  variant("banner.four-grid", "BannerShowcase", "چهارتایی", "۲×۲", "ReusableViaAdapter", {
    previewKind: "banner-four", sizePresetsSupported: true, dataSources: DS_MANUAL,
  }),
  variant("banner.one-large-two-small", "BannerShowcase", "۱ بزرگ ۲ کوچک", "موزاییک ۳", "ReusableViaAdapter", {
    previewKind: "banner-mosaic", sizePresetsSupported: true, dataSources: DS_MANUAL,
  }),
  variant("banner.one-large-four-small", "BannerShowcase", "۱ بزرگ ۴ کوچک", "موزاییک ۵", "ReusableViaAdapter", {
    previewKind: "banner-mosaic", sizePresetsSupported: true, dataSources: DS_MANUAL,
  }),
  variant("banner.eight-compact", "BannerShowcase", "هشت فشرده", "۸ کاشی", "ReusableViaAdapter", {
    previewKind: "banner-four", sizePresetsSupported: true, dataSources: DS_MANUAL,
  }),
  variant("banner.mosaic-2x2", "BannerShowcase", "موزاییک ۲×۲", "موزاییک مساوی", "ReusableViaAdapter", {
    previewKind: "banner-four", sizePresetsSupported: true, dataSources: DS_MANUAL,
  }),

  variant("brand.logo-rail", "BrandShowcase", "ریل لوگو", "اسکرول لوگو", "Existing", {
    previewKind: "brand-rail", autoplaySupported: true, dataSources: DS_BRAND,
  }),
  variant("brand.logo-grid", "BrandShowcase", "شبکه لوگو", "شبکه برند", "ReusableViaAdapter", {
    previewKind: "brand-grid", dataSources: DS_BRAND,
  }),
  variant("brand.featured", "BrandShowcase", "برند ویژه", "کارت برند", "ReusableViaAdapter", {
    previewKind: "brand-grid", dataSources: DS_BRAND,
  }),

  variant("reviews.card-carousel", "ReviewsShowcase", "کاروسل نظر", "کارت نظر", "Existing", {
    previewKind: "reviews-carousel", autoplaySupported: true, dataSources: DS_REVIEW,
  }),
  variant("reviews.compact-quotes", "ReviewsShowcase", "نقل فشرده", "نقل کوتاه", "ReusableViaAdapter", {
    previewKind: "reviews-carousel", dataSources: DS_REVIEW,
  }),

  variant("article.magazine-rail", "ArticleShowcase", "ریل مجله", "کارت مقاله افقی", "Existing", {
    previewKind: "article-rail", autoplaySupported: true, dataSources: DS_ARTICLE,
  }),
  variant("article.grid", "ArticleShowcase", "شبکه مقاله", "شبکه مطالب", "ReusableViaAdapter", {
    previewKind: "article-grid", dataSources: DS_ARTICLE,
  }),
  variant("article.featured-plus-list", "ArticleShowcase", "ویژه + فهرست", "یک ویژه + لیست", "ReusableViaAdapter", {
    previewKind: "article-rail", dataSources: DS_ARTICLE,
  }),

  variant("promo.default", "PromoSection", "پروموی پیش‌فرض", "بنر پرومو تکی", "Existing", {
    previewKind: "promo", sizePresetsSupported: true, dataSources: DS_MANUAL,
  }),
  variant("richtext.default", "RichText", "متن ساده", "بدون HTML خام", "Existing", {
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
    descriptionFa: "ویژوال و استوری: هیرو تمام‌عرض، دایره استوری، دسته تصویری، کاروسل کالا و بنر دوتایی",
    sectionPresetList: [
      { sectionTypeKey: "HeroCarousel", variantKey: "hero.full-width", dataSourceIntent: "Manual" },
      { sectionTypeKey: "StoryRail", variantKey: "story.circle", dataSourceIntent: "Manual" },
      { sectionTypeKey: "CategoryShowcase", variantKey: "category.image-cards", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ProductShowcase", variantKey: "product.card-carousel", dataSourceIntent: "Newest" },
      { sectionTypeKey: "BannerShowcase", variantKey: "banner.two-equal", dataSourceIntent: "Manual" },
      { sectionTypeKey: "BrandShowcase", variantKey: "brand.logo-rail", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ReviewsShowcase", variantKey: "reviews.compact-quotes", dataSourceIntent: "ApprovedReviews" },
    ],
  },
  {
    templateKey: "auto-parts",
    nameFa: "لوازم یدکی خودرو",
    industry: "AutoParts",
    descriptionFa: "چگالی دسته/برند/کالا: هیرو کانتینر، کاشی فشرده، ردیف فشرده، شبکه برند و رتبه افقی",
    sectionPresetList: [
      { sectionTypeKey: "HeroCarousel", variantKey: "hero.contained", dataSourceIntent: "Manual" },
      { sectionTypeKey: "CategoryShowcase", variantKey: "category.compact-tiles", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ProductShowcase", variantKey: "product.compact-rows", dataSourceIntent: "Category" },
      { sectionTypeKey: "BrandShowcase", variantKey: "brand.logo-grid", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ProductRankedList", variantKey: "ranked.horizontal", dataSourceIntent: "Newest" },
      { sectionTypeKey: "BannerShowcase", variantKey: "banner.single", dataSourceIntent: "Manual" },
    ],
  },
  {
    templateKey: "building-supplies",
    nameFa: "لوازم ساختمانی",
    industry: "BuildingSupplies",
    descriptionFa: "دسته قوی + شبکه کالا + بنر سه‌تایی و مقالات شبکه‌ای",
    sectionPresetList: [
      { sectionTypeKey: "CategoryShowcase", variantKey: "category.image-cards", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ProductShowcase", variantKey: "product.grid", dataSourceIntent: "Category" },
      { sectionTypeKey: "BannerShowcase", variantKey: "banner.three", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ArticleShowcase", variantKey: "article.grid", dataSourceIntent: "LatestArticles" },
      { sectionTypeKey: "BrandShowcase", variantKey: "brand.logo-rail", dataSourceIntent: "Manual" },
    ],
  },
  {
    templateKey: "tools-hardware",
    nameFa: "ابزار و یراق",
    industry: "ToolsHardware",
    descriptionFa: "ترکیب فشرده: کاشی دسته، ردیف کالا، رتبه چندستونه و بنر هشت‌تایی",
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
    descriptionFa: "کلکسیون تصویری: هیرو با پروموی کناری، شبکه کالا و موزاییک بنر",
    sectionPresetList: [
      { sectionTypeKey: "HeroCarousel", variantKey: "hero.side-promos", dataSourceIntent: "Manual" },
      { sectionTypeKey: "BannerShowcase", variantKey: "banner.one-large-four-small", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ProductShowcase", variantKey: "product.grid", dataSourceIntent: "Newest" },
      { sectionTypeKey: "BannerShowcase", variantKey: "banner.mosaic-2x2", dataSourceIntent: "Manual" },
      { sectionTypeKey: "CategoryShowcase", variantKey: "category.horizontal-rail", dataSourceIntent: "Manual" },
    ],
  },
  {
    templateKey: "interior-decor",
    nameFa: "دکوراسیون داخلی",
    industry: "InteriorDecor",
    descriptionFa: "تصویر بزرگ و انتخاب‌شده: هیرو دو ستونه، ویژه+ریل، مقالات مجله‌ای",
    sectionPresetList: [
      { sectionTypeKey: "HeroCarousel", variantKey: "hero.split", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ProductShowcase", variantKey: "product.featured-plus-rail", dataSourceIntent: "Newest" },
      { sectionTypeKey: "ArticleShowcase", variantKey: "article.magazine-rail", dataSourceIntent: "LatestArticles" },
      { sectionTypeKey: "BannerShowcase", variantKey: "banner.one-large-two-small", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ReviewsShowcase", variantKey: "reviews.card-carousel", dataSourceIntent: "ApprovedReviews" },
    ],
  },
  {
    templateKey: "home-appliance",
    nameFa: "لوازم خانگی",
    industry: "HomeAppliance",
    descriptionFa: "دسته + ستون کالا + برند ویژه و شبکه رتبه",
    sectionPresetList: [
      { sectionTypeKey: "HeroCarousel", variantKey: "hero.contained", dataSourceIntent: "Manual" },
      { sectionTypeKey: "CategoryShowcase", variantKey: "category.image-cards", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ProductShowcase", variantKey: "product.category-columns", dataSourceIntent: "Category" },
      { sectionTypeKey: "BrandShowcase", variantKey: "brand.featured", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ProductRankedList", variantKey: "ranked.grid", dataSourceIntent: "Newest" },
    ],
  },
  {
    templateKey: "shoes",
    nameFa: "کفش",
    industry: "Shoes",
    descriptionFa: "استوری کارت‌گرد، کاروسل کالا و بنر نامتقارن",
    sectionPresetList: [
      { sectionTypeKey: "StoryRail", variantKey: "story.rounded-cards", dataSourceIntent: "Manual" },
      { sectionTypeKey: "HeroCarousel", variantKey: "hero.full-width", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ProductShowcase", variantKey: "product.card-carousel", dataSourceIntent: "Newest" },
      { sectionTypeKey: "BannerShowcase", variantKey: "banner.two-asymmetric", dataSourceIntent: "Manual" },
      { sectionTypeKey: "CategoryShowcase", variantKey: "category.horizontal-rail", dataSourceIntent: "Manual" },
    ],
  },
  {
    templateKey: "plants",
    nameFa: "گل و گیاه",
    industry: "Plants",
    descriptionFa: "هیرو + ریل دسته + مقالات ویژه+فهرست و نظرات",
    sectionPresetList: [
      { sectionTypeKey: "HeroCarousel", variantKey: "hero.full-width", dataSourceIntent: "Manual" },
      { sectionTypeKey: "CategoryShowcase", variantKey: "category.horizontal-rail", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ProductShowcase", variantKey: "product.featured-plus-rail", dataSourceIntent: "Newest" },
      { sectionTypeKey: "ArticleShowcase", variantKey: "article.featured-plus-list", dataSourceIntent: "LatestArticles" },
      { sectionTypeKey: "ReviewsShowcase", variantKey: "reviews.card-carousel", dataSourceIntent: "ApprovedReviews" },
    ],
  },
  {
    templateKey: "beauty",
    nameFa: "آرایشی بهداشتی",
    industry: "Beauty",
    descriptionFa: "برند و استوری: دایره تصویری، کاروسل کالا، برند ویژه و پرومو",
    sectionPresetList: [
      { sectionTypeKey: "StoryRail", variantKey: "story.image-circles", dataSourceIntent: "Manual" },
      { sectionTypeKey: "BrandShowcase", variantKey: "brand.featured", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ProductShowcase", variantKey: "product.card-carousel", dataSourceIntent: "Category" },
      { sectionTypeKey: "BannerShowcase", variantKey: "banner.four-grid", dataSourceIntent: "Manual" },
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
