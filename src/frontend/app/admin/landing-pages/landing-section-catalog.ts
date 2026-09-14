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
] as const;

export type LandingSectionType = (typeof LANDING_SECTION_TYPES)[number];

export type LandingSectionChoice = {
  type: LandingSectionType;
  label: string;
  description: string;
  testId: string;
};

export const LANDING_SECTION_CHOICES: LandingSectionChoice[] = [
  { type: "Hero", label: "بنر اصلی", description: "عنوان، توضیح کوتاه و دکمهٔ دعوت به اقدام", testId: "add-section-hero" },
  { type: "ProductCollection", label: "مجموعه کالا", description: "نمایش کالا از انتخاب دستی، دسته، برند یا تازه‌ها", testId: "add-section-products" },
  { type: "CategoryGrid", label: "شبکهٔ دسته‌ها", description: "چند دستهٔ فروشگاه را در یک ردیف نشان می‌دهد", testId: "add-section-categories" },
  { type: "BrandStrip", label: "نوار برند", description: "برندهای انتخاب‌شده را در ویترین می‌چیند", testId: "add-section-brands" },
  { type: "PromoBanner", label: "بنر تبلیغاتی", description: "یک بنر با عنوان و پیوند کنترل‌شده", testId: "add-section-promo" },
  { type: "ArticleList", label: "فهرست مطالب", description: "آخرین مقاله‌های منتشرشده", testId: "add-section-articles" },
  { type: "Reviews", label: "نظر خریداران", description: "نظرهای تأییدشدهٔ فروشگاه", testId: "add-section-reviews" },
  { type: "RichText", label: "متن آزاد", description: "یک بلوک متن ساده بدون HTML", testId: "add-section-text" },
  { type: "NavigationMenu", label: "فهرست پیوند", description: "نمایش یک منوی فعال فروشگاه در بدنهٔ صفحه", testId: "add-section-menu" },
];

export const PRODUCT_SOURCE_CHOICES = [
  { value: "Manual", label: "انتخاب دستی" },
  { value: "Category", label: "دسته‌بندی" },
  { value: "Brand", label: "برند" },
  { value: "Newest", label: "جدیدترین‌ها" },
] as const;

export function landingSectionLabel(type: string): string {
  return LANDING_SECTION_CHOICES.find((item) => item.type === type)?.label ?? "بخش";
}

export function defaultLandingSectionConfig(type: LandingSectionType): Record<string, unknown> {
  switch (type) {
    case "Hero":
      return { title: "عنوان بنر", subtitle: "توضیح کوتاه", href: "/products" };
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
  if (type === "ProductCollection") {
    const source = PRODUCT_SOURCE_CHOICES.find((item) => item.value === config.source)?.label ?? "منبع کالا";
    const take = typeof config.take === "number" ? config.take : 8;
    return title ? `${title} · ${source} · ${take} کالا` : `${source} · ${take} کالا`;
  }
  if (type === "ArticleList") {
    const take = typeof config.take === "number" ? config.take : 6;
    return title ? `${title} · آخرین مطالب · ${take} مورد` : `آخرین مطالب · ${take} مورد`;
  }
  return title ?? landingSectionLabel(type);
}
