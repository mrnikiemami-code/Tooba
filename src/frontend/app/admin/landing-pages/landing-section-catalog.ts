import { getVariant } from "../../../lib/storefront-composition/registry.ts";
import { bannerSlotCountForVariant } from "../../../lib/storefront-composition/industry-templates.ts";
import { defaultHeroSliderConfig } from "../../../lib/storefront-composition/hero-slider-config.ts";

export { bannerSlotCountForVariant };

export const LANDING_SECTION_TYPES = [
  "Hero",
  "ProductCollection",
  "CategoryGrid",
  "BrandStrip",
  "PromoBanner",
  "ArticleList",
  "Reviews",
  "RichText",
  "NavigationMenu",
  "StoryRail",
  "BannerShowcase",
] as const;

export type LandingSectionType = (typeof LANDING_SECTION_TYPES)[number];

export type LandingSectionChoice = {
  type: LandingSectionType;
  label: string;
  description: string;
  testId: string;
};

export const LANDING_SECTION_CHOICES: LandingSectionChoice[] = [
  { type: "Hero", label: "اسلایدر اصلی", description: "چند اسلاید با تصویر، عنوان و Alt", testId: "add-section-hero" },
  { type: "ProductCollection", label: "مجموعه کالا", description: "نمایش کالا از انتخاب دستی، دسته، برند، تازه‌ها یا پیشنهاد شگفت‌انگیز", testId: "add-section-products" },
  { type: "CategoryGrid", label: "شبکهٔ دسته‌ها", description: "چند دستهٔ فروشگاه را در یک ردیف نشان می‌دهد", testId: "add-section-categories" },
  { type: "BrandStrip", label: "نوار برند", description: "برندهای انتخاب‌شده را در ویترین می‌چیند", testId: "add-section-brands" },
  { type: "PromoBanner", label: "بنر تبلیغاتی", description: "یک بنر با عنوان و پیوند کنترل‌شده", testId: "add-section-promo" },
  { type: "ArticleList", label: "فهرست مطالب", description: "آخرین مقاله‌های منتشرشده", testId: "add-section-articles" },
  { type: "Reviews", label: "نظر خریداران", description: "نظرهای تأییدشدهٔ فروشگاه", testId: "add-section-reviews" },
  { type: "RichText", label: "متن آزاد", description: "یک بلوک متن ساده بدون قالب‌بندی خام", testId: "add-section-text" },
  { type: "NavigationMenu", label: "فهرست پیوند", description: "نمایش یک منوی فعال فروشگاه در بدنهٔ صفحه", testId: "add-section-menu" },
  { type: "StoryRail", label: "میانبر استوری", description: "میانبرهای دایره‌ای یا کارت‌گرد", testId: "add-section-stories" },
  { type: "BannerShowcase", label: "نمایش بنر", description: "شبکه‌های بنر کنترل‌شده با جایگاه مشخص", testId: "add-section-banners" },
];

export const PRODUCT_SOURCE_CHOICES = [
  { value: "Manual", label: "انتخاب دستی" },
  { value: "Category", label: "از یک دسته" },
  { value: "Brand", label: "از یک برند" },
  { value: "Newest", label: "جدیدترین کالاها" },
  { value: "PromotionCampaign", label: "پیشنهاد شگفت‌انگیز" },
] as const;

/** برچسب انسانی منبع کالا برای Review (بدون کد فنی). */
export function productSourceLabelFa(source: unknown): string {
  if (typeof source !== "string") return "جدیدترین کالاها";
  return PRODUCT_SOURCE_CHOICES.find((item) => item.value === source)?.label ?? "منبع کالا";
}

export function landingSectionLabel(type: string): string {
  return LANDING_SECTION_CHOICES.find((item) => item.type === type)?.label ?? "بخش";
}

export function defaultLandingSectionConfig(type: LandingSectionType): Record<string, unknown> {
  switch (type) {
    case "Hero":
      return defaultHeroSliderConfig();
    case "ProductCollection":
      return { title: "کالاها", source: "Newest", take: 8, productIds: [] };
    case "CategoryGrid":
      return { title: "دسته‌ها", categoryIds: [] };
    case "BrandStrip":
      return { title: "برندها", brandIds: [] };
    case "PromoBanner":
      return { title: "پیشنهاد ویژه", href: "/offers" };
    case "ArticleList":
      return { title: "آخرین مطالب", source: "Latest", take: 6 };
    case "Reviews":
      return { title: "نظر خریداران" };
    case "RichText":
      return { title: "متن صفحه", text: "متن ساده برای این بخش" };
    case "NavigationMenu":
      return { title: "فهرست پیوندها", menuId: "" };
    case "StoryRail":
      return {
        title: "استوری‌ها",
        variantKey: "story.circle",
        take: 12,
        enabled: true,
        items: [],
      };
    case "BannerShowcase":
      return {
        title: "بنرها",
        variantKey: "banner.single",
        heightPreset: "Medium",
        items: [{ imageUrl: "", href: "/offers", title: "بنر ۱" }],
      };
  }
}

export function parseLandingConfig(raw: string | null | undefined): Record<string, unknown> {
  if (!raw) return {};
  try {
    const value = JSON.parse(raw) as unknown;
    return value && typeof value === "object" && !Array.isArray(value) ? value as Record<string, unknown> : {};
  } catch {
    return {};
  }
}

export function summarizeLandingSection(type: string, config: Record<string, unknown>): string {
  const title = typeof config.title === "string" && config.title.trim() ? config.title.trim() : null;
  const variantKey = typeof config.variantKey === "string" ? config.variantKey : undefined;
  const variantFa = variantKey ? getVariant(variantKey)?.nameFa ?? null : null;
  const variantSuffix = variantFa ? ` · ${variantFa}` : "";
  if (type === "ProductCollection") {
    const source = productSourceLabelFa(config.source);
    const take = typeof config.take === "number" ? config.take : 8;
    return title ? `${title}${variantSuffix} · ${source} · ${take} کالا` : `${source} · ${take} کالا${variantSuffix}`;
  }
  if (type === "ArticleList") {
    const take = typeof config.take === "number" ? config.take : 6;
    const source = typeof config.source === "string" ? config.source : "Latest";
    const sourceLabel = source === "Manual" ? "انتخاب دستی" : "جدیدترین مطالب";
    const manualCount = Array.isArray(config.articleIds) ? config.articleIds.length : 0;
    if (source === "Manual") {
      return title
        ? `${title}${variantSuffix} · ${sourceLabel} · ${manualCount.toLocaleString("fa-IR")} مطلب`
        : `${sourceLabel} · ${manualCount.toLocaleString("fa-IR")} مطلب${variantSuffix}`;
    }
    return title ? `${title}${variantSuffix} · ${sourceLabel} · ${take} مورد` : `${sourceLabel} · ${take} مورد${variantSuffix}`;
  }
  if (type === "BannerShowcase" || (variantKey?.startsWith("banner.") ?? false)) {
    const slots = bannerSlotCountForVariant(variantKey);
    return title ? `${title}${variantSuffix} · ${slots} جایگاه` : `بنر${variantSuffix} · ${slots} جایگاه`;
  }
  if (type === "StoryRail" || (variantKey?.startsWith("story.") ?? false)) {
    const take = typeof config.take === "number" ? config.take : 12;
    return title
      ? `${title}${variantSuffix} · نمایش تا ${take.toLocaleString("fa-IR")} استوری تأییدشده`
      : `نمایش استوری تأییدشده${variantSuffix} · تا ${take.toLocaleString("fa-IR")}`;
  }
  if (type === "CategoryGrid") {
    const count = Array.isArray(config.categoryIds)
      ? config.categoryIds.length
      : Array.isArray(config.ids)
        ? config.ids.length
        : 0;
    const countLabel = count > 0 ? `${count.toLocaleString("fa-IR")} دسته` : "بدون دسته";
    return title ? `${title}${variantSuffix} · ${countLabel}` : `${countLabel}${variantSuffix}`;
  }
  if (type === "BrandStrip") {
    const count = Array.isArray(config.brandIds)
      ? config.brandIds.length
      : Array.isArray(config.ids)
        ? config.ids.length
        : 0;
    const countLabel = count > 0 ? `${count.toLocaleString("fa-IR")} برند` : "بدون برند";
    return title ? `${title}${variantSuffix} · ${countLabel}` : `${countLabel}${variantSuffix}`;
  }
  if (title) return `${title}${variantSuffix}`;
  return `${landingSectionLabel(type)}${variantSuffix}`;
}

/** هشدار منبع ناقص برای فضای کار بخش‌ها (بدون مسدود کردن ذخیره). */
export function incompleteSourceWarning(type: string, config: Record<string, unknown>): string | null {
  if (type === "ProductCollection") {
    const source = typeof config.source === "string" ? config.source : "Newest";
    if (source === "Manual" && (!Array.isArray(config.productIds) || config.productIds.length === 0)) {
      return "منبع کالا ناقص است — حداقل یک کالا انتخاب کنید.";
    }
    if (source === "Category" && !(typeof config.categoryId === "string" && config.categoryId)) {
      return "منبع کالا ناقص است — یک دسته انتخاب کنید.";
    }
    if (source === "Brand" && !(typeof config.brandId === "string" && config.brandId)) {
      return "منبع کالا ناقص است — یک برند انتخاب کنید.";
    }
  }
  if (type === "ArticleList") {
    const source = typeof config.source === "string" ? config.source : "Latest";
    if (source === "Manual" && (!Array.isArray(config.articleIds) || config.articleIds.length === 0)) {
      return "منبع مطلب ناقص است.";
    }
  }
  if (type === "CategoryGrid") {
    const ids = Array.isArray(config.categoryIds) ? config.categoryIds : Array.isArray(config.ids) ? config.ids : [];
    if (ids.length === 0) return "هنوز دسته‌ای انتخاب نشده است.";
  }
  if (type === "BrandStrip") {
    const ids = Array.isArray(config.brandIds) ? config.brandIds : Array.isArray(config.ids) ? config.ids : [];
    if (ids.length === 0) return "هنوز برندی انتخاب نشده است.";
  }
  if (type === "BannerShowcase") {
    const items = Array.isArray(config.items) ? config.items : [];
    const empty = items.length === 0 || items.every((item) => {
      if (!item || typeof item !== "object") return true;
      const imageUrl = (item as { imageUrl?: unknown }).imageUrl;
      return typeof imageUrl !== "string" || !imageUrl.trim();
    });
    if (empty) return "تصویر بنر هنوز کامل نشده است.";
  }
  return null;
}
