import type { SectionTypeDefinition, VariantDefinition } from "./types.ts";
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

const variant = (
  key: string,
  sectionTypeKey: string,
  nameFa: string,
  descriptionFa: string,
  status: VariantDefinition["status"],
): VariantDefinition => ({
  key,
  sectionTypeKey,
  nameFa,
  descriptionFa,
  status,
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

/** Substantial initial Variant catalog; most marked NewRequiredLater until built. */
export const VARIANTS: VariantDefinition[] = [
  variant("hero.full-width", "HeroCarousel", "تمام‌عرض", "اسلاید تمام‌عرض", "Existing"),
  variant("hero.contained", "HeroCarousel", "داخل کانتینر", "اسلاید با گوشه گرد", "ReusableViaAdapter"),
  variant("hero.split", "HeroCarousel", "دو ستون", "تصویر + متن", "NewRequiredLater"),
  variant("hero.side-promos", "HeroCarousel", "هیرو با پروموی کناری", "هیرو + دو پرومو", "NewRequiredLater"),
  variant("hero.editorial", "HeroCarousel", "تحریریه", "کپشن قوی", "NewRequiredLater"),

  variant("story.circle", "StoryRail", "دایره استوری", "دایره‌های افقی", "Existing"),
  variant("story.image-circles", "StoryRail", "دایره تصویری", "پر از تصویر", "ReusableViaAdapter"),
  variant("story.rounded-cards", "StoryRail", "کارت گرد", "میانبر کارت‌گرد", "ReusableViaAdapter"),
  variant("story.icon-shortcuts", "StoryRail", "میانبر آیکون", "آیکون + برچسب", "NewRequiredLater"),

  variant("category.image-cards", "CategoryShowcase", "کارت تصویری", "کارت دسته با تصویر", "Existing"),
  variant("category.compact-tiles", "CategoryShowcase", "کاشی فشرده", "شبکه فشرده", "ReusableViaAdapter"),
  variant("category.horizontal-rail", "CategoryShowcase", "ریل افقی", "اسکرول افقی", "ReusableViaAdapter"),
  variant("category.editorial-tiles", "CategoryShowcase", "کاشی تحریریه", "کاشی بزرگ", "NewRequiredLater"),

  variant("product.card-carousel", "ProductShowcase", "کاروسل کارت", "ریل کارت کالا", "Existing"),
  variant("product.grid", "ProductShowcase", "شبکه کالا", "شبکه چندستونه", "ReusableViaAdapter"),
  variant("product.compact-rows", "ProductShowcase", "ردیف فشرده", "لیست فشرده", "Existing"),
  variant("product.category-columns", "ProductShowcase", "ستون دسته‌ای", "پرفروش ستونی", "Existing"),
  variant("product.featured-plus-rail", "ProductShowcase", "ویژه + ریل", "یک ویژه + ریل", "NewRequiredLater"),
  variant("product.tabbed", "ProductShowcase", "تب‌دار", "چند تب کالا", "NewRequiredLater"),
  variant("product.large-cards", "ProductShowcase", "کارت بزرگ", "کارت درشت", "NewRequiredLater"),
  variant("product.minimal-list", "ProductShowcase", "فهرست مینیمال", "لیست ساده", "NewRequiredLater"),

  variant("ranked.horizontal", "ProductRankedList", "افقی رتبه‌دار", "ریل رتبه", "ReusableViaAdapter"),
  variant("ranked.grid", "ProductRankedList", "شبکه رتبه‌دار", "شبکه با رتبه", "NewRequiredLater"),
  variant("ranked.ticker", "ProductRankedList", "تیکر فشرده", "نوار فشرده", "NewRequiredLater"),
  variant("ranked.multi-column", "ProductRankedList", "چند ستون", "چند ستون رتبه", "ReusableViaAdapter"),

  variant("banner.single", "BannerShowcase", "تکی", "یک بنر", "Existing"),
  variant("banner.two-equal", "BannerShowcase", "دو مساوی", "دو بنر برابر", "ReusableViaAdapter"),
  variant("banner.two-asymmetric", "BannerShowcase", "دو نامتقارن", "دو بنر ناهمسان", "NewRequiredLater"),
  variant("banner.three", "BannerShowcase", "سه تایی", "سه بنر", "ReusableViaAdapter"),
  variant("banner.four-grid", "BannerShowcase", "چهار شبکه", "۲×۲", "ReusableViaAdapter"),
  variant("banner.one-large-two-small", "BannerShowcase", "۱ بزرگ ۲ کوچک", "موزاییک ۳", "ReusableViaAdapter"),
  variant("banner.one-large-four-small", "BannerShowcase", "۱ بزرگ ۴ کوچک", "موزاییک ۵", "NewRequiredLater"),
  variant("banner.eight-compact", "BannerShowcase", "هشت فشرده", "۸ کاشی", "NewRequiredLater"),
  variant("banner.mosaic-2x2", "BannerShowcase", "موزاییک ۲×۲", "موزاییک مساوی", "ReusableViaAdapter"),

  variant("brand.logo-rail", "BrandShowcase", "ریل لوگو", "اسکرول لوگو", "Existing"),
  variant("brand.logo-grid", "BrandShowcase", "شبکه لوگو", "شبکه برند", "ReusableViaAdapter"),
  variant("brand.featured", "BrandShowcase", "برند ویژه", "کارت برند", "NewRequiredLater"),

  variant("reviews.card-carousel", "ReviewsShowcase", "کاروسل نظر", "کارت نظر", "Existing"),
  variant("reviews.compact-quotes", "ReviewsShowcase", "نقل فشرده", "نقل کوتاه", "NewRequiredLater"),

  variant("article.magazine-rail", "ArticleShowcase", "ریل مجله", "کارت مقاله افقی", "Existing"),
  variant("article.grid", "ArticleShowcase", "شبکه مقاله", "شبکه مطالب", "ReusableViaAdapter"),
  variant("article.featured-plus-list", "ArticleShowcase", "ویژه + فهرست", "یک ویژه + لیست", "NewRequiredLater"),

  variant("promo.default", "PromoSection", "پروموی پیش‌فرض", "بنر پرومو تکی", "Existing"),
  variant("richtext.default", "RichText", "متن ساده", "بدون HTML خام", "Existing"),
  variant("nav.menu", "NavigationMenu", "منوی پیوند", "منوی فعال", "Existing"),
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
    descriptionFa: "هیرو + استوری + دسته + کاروسل کالا + بنر + برند",
    sectionPresetList: [
      { sectionTypeKey: "HeroCarousel", variantKey: "hero.full-width", dataSourceIntent: "Manual" },
      { sectionTypeKey: "StoryRail", variantKey: "story.circle", dataSourceIntent: "Manual" },
      { sectionTypeKey: "CategoryShowcase", variantKey: "category.image-cards", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ProductShowcase", variantKey: "product.card-carousel", dataSourceIntent: "Newest" },
      { sectionTypeKey: "BannerShowcase", variantKey: "banner.two-equal", dataSourceIntent: "Manual" },
      { sectionTypeKey: "BrandShowcase", variantKey: "brand.logo-rail", dataSourceIntent: "Manual" },
    ],
  },
  {
    templateKey: "auto-parts",
    nameFa: "لوازم یدکی خودرو",
    industry: "AutoParts",
    descriptionFa: "جست‌وجوی سریع با دسته و کالا",
    sectionPresetList: [
      { sectionTypeKey: "HeroCarousel", variantKey: "hero.contained", dataSourceIntent: "Manual" },
      { sectionTypeKey: "CategoryShowcase", variantKey: "category.compact-tiles", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ProductShowcase", variantKey: "product.compact-rows", dataSourceIntent: "Category" },
      { sectionTypeKey: "BrandShowcase", variantKey: "brand.logo-grid", dataSourceIntent: "Manual" },
    ],
  },
  {
    templateKey: "building-supplies",
    nameFa: "لوازم ساختمانی",
    industry: "BuildingSupplies",
    descriptionFa: "دسته قوی + شبکه کالا + بنر",
    sectionPresetList: [
      { sectionTypeKey: "CategoryShowcase", variantKey: "category.editorial-tiles", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ProductShowcase", variantKey: "product.grid", dataSourceIntent: "Category" },
      { sectionTypeKey: "BannerShowcase", variantKey: "banner.three", dataSourceIntent: "Manual" },
    ],
  },
  {
    templateKey: "tools-hardware",
    nameFa: "ابزار و یراق",
    industry: "ToolsHardware",
    descriptionFa: "دسته فشرده + ردیف کالا",
    sectionPresetList: [
      { sectionTypeKey: "CategoryShowcase", variantKey: "category.compact-tiles", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ProductShowcase", variantKey: "product.compact-rows", dataSourceIntent: "Brand" },
      { sectionTypeKey: "BrandShowcase", variantKey: "brand.logo-rail", dataSourceIntent: "Manual" },
    ],
  },
  {
    templateKey: "tile-ceramic",
    nameFa: "کاشی و سرامیک",
    industry: "TileCeramic",
    descriptionFa: "هیرو تصویری + شبکه بزرگ کالا",
    sectionPresetList: [
      { sectionTypeKey: "HeroCarousel", variantKey: "hero.editorial", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ProductShowcase", variantKey: "product.large-cards", dataSourceIntent: "Newest" },
      { sectionTypeKey: "BannerShowcase", variantKey: "banner.mosaic-2x2", dataSourceIntent: "Manual" },
    ],
  },
  {
    templateKey: "interior-decor",
    nameFa: "دکوراسیون داخلی",
    industry: "InteriorDecor",
    descriptionFa: "هیرو تحریریه + مقالات + کالا",
    sectionPresetList: [
      { sectionTypeKey: "HeroCarousel", variantKey: "hero.split", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ProductShowcase", variantKey: "product.featured-plus-rail", dataSourceIntent: "Featured" },
      { sectionTypeKey: "ArticleShowcase", variantKey: "article.magazine-rail", dataSourceIntent: "LatestArticles" },
    ],
  },
  {
    templateKey: "home-appliance",
    nameFa: "لوازم خانگی",
    industry: "HomeAppliance",
    descriptionFa: "دسته + پرفروش ستونی + برند",
    sectionPresetList: [
      { sectionTypeKey: "CategoryShowcase", variantKey: "category.image-cards", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ProductShowcase", variantKey: "product.category-columns", dataSourceIntent: "BestSelling" },
      { sectionTypeKey: "BrandShowcase", variantKey: "brand.featured", dataSourceIntent: "Manual" },
    ],
  },
  {
    templateKey: "shoes",
    nameFa: "کفش",
    industry: "Shoes",
    descriptionFa: "استوری + کاروسل + بنر",
    sectionPresetList: [
      { sectionTypeKey: "StoryRail", variantKey: "story.rounded-cards", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ProductShowcase", variantKey: "product.card-carousel", dataSourceIntent: "Newest" },
      { sectionTypeKey: "BannerShowcase", variantKey: "banner.two-asymmetric", dataSourceIntent: "Manual" },
    ],
  },
  {
    templateKey: "plants",
    nameFa: "گل و گیاه",
    industry: "Plants",
    descriptionFa: "هیرو + دسته + نظرات",
    sectionPresetList: [
      { sectionTypeKey: "HeroCarousel", variantKey: "hero.full-width", dataSourceIntent: "Manual" },
      { sectionTypeKey: "CategoryShowcase", variantKey: "category.horizontal-rail", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ReviewsShowcase", variantKey: "reviews.card-carousel", dataSourceIntent: "ApprovedReviews" },
    ],
  },
  {
    templateKey: "beauty",
    nameFa: "آرایشی بهداشتی",
    industry: "Beauty",
    descriptionFa: "استوری + تب کالا + پرومو",
    sectionPresetList: [
      { sectionTypeKey: "StoryRail", variantKey: "story.image-circles", dataSourceIntent: "Manual" },
      { sectionTypeKey: "ProductShowcase", variantKey: "product.tabbed", dataSourceIntent: "Category" },
      { sectionTypeKey: "PromoSection", variantKey: "promo.default", dataSourceIntent: "Manual" },
    ],
  },
];

export function getSectionType(key: string): SectionTypeDefinition | undefined {
  return SECTION_TYPES.find((s) => s.key === key);
}

export function getVariant(key: string): VariantDefinition | undefined {
  return VARIANTS.find((v) => v.key === key);
}

export function variantsForSection(sectionTypeKey: string): VariantDefinition[] {
  return VARIANTS.filter((v) => v.sectionTypeKey === sectionTypeKey);
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
  }
}
