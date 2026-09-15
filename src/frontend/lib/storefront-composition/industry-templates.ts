import {
  INDUSTRY_TEMPLATE_SEEDS,
  getSectionType,
  getVariant,
  isVariantImplemented,
  landingHostTypeForSection,
} from "./registry.ts";
import { ADMIN_SELECTABLE_DATA_SOURCES } from "./types.ts";

const TRUTHFUL = new Set<string>(ADMIN_SELECTABLE_DATA_SOURCES);

export function bannerSlotCountForVariant(variantKey: string | undefined): number {
  switch (variantKey) {
    case "banner.single":
      return 1;
    case "banner.two-equal":
    case "banner.two-asymmetric":
      return 2;
    case "banner.three":
    case "banner.one-large-two-small":
      return 3;
    case "banner.four-grid":
    case "banner.mosaic-2x2":
      return 4;
    case "banner.one-large-four-small":
      return 5;
    case "banner.eight-compact":
      return 8;
    default:
      return variantKey?.startsWith("banner.") ? 1 : 0;
  }
}

function defaultConfigForSection(sectionTypeKey: string, variantKey: string): Record<string, unknown> {
  const section = getSectionType(sectionTypeKey);
  const hostType = landingHostTypeForSection(sectionTypeKey);
  const base: Record<string, unknown> = {
    title: section?.nameFa ?? "بخش",
    variantKey,
  };
  if (hostType === "Hero") {
    return { ...base, title: "عنوان بنر", subtitle: "توضیح کوتاه", href: "/products", heightPreset: "Large" };
  }
  if (hostType === "ProductCollection") {
    return { ...base, title: "کالاها", source: "Newest", take: 8, productIds: [] };
  }
  if (hostType === "CategoryGrid") {
    return { ...base, title: "دسته‌ها", categoryIds: [] };
  }
  if (hostType === "BrandStrip") {
    return { ...base, title: "برندها", brandIds: [] };
  }
  if (hostType === "PromoBanner") {
    return { ...base, title: "پیشنهاد ویژه", href: "/offers" };
  }
  if (hostType === "ArticleList") {
    return { ...base, title: "آخرین مطالب", source: "Latest", take: 6 };
  }
  if (hostType === "Reviews") {
    return { ...base, title: "نظر خریداران" };
  }
  if (hostType === "StoryRail") {
    return {
      ...base,
      title: "استوری‌ها",
      take: 12,
      enabled: true,
      items: [],
    };
  }
  if (hostType === "BannerShowcase") {
    const slots = bannerSlotCountForVariant(variantKey);
    return {
      ...base,
      title: "بنرها",
      heightPreset: "Medium",
      items: Array.from({ length: Math.max(slots, 1) }, (_, i) => ({
        imageUrl: "",
        href: "/offers",
        title: `بنر ${i + 1}`,
      })),
    };
  }
  return base;
}

export type IndustryTemplateSeed = (typeof INDUSTRY_TEMPLATE_SEEDS)[number];

export function listIndustryTemplates(): IndustryTemplateSeed[] {
  return INDUSTRY_TEMPLATE_SEEDS;
}

export function getIndustryTemplate(templateKey: string): IndustryTemplateSeed | undefined {
  return INDUSTRY_TEMPLATE_SEEDS.find((t) => t.templateKey === templateKey);
}

export function assertIndustryTemplatesValid(): void {
  if (INDUSTRY_TEMPLATE_SEEDS.length !== 10) {
    throw new Error(`Expected 10 industry templates, got ${INDUSTRY_TEMPLATE_SEEDS.length}`);
  }
  const signatures = new Set<string>();
  for (const template of INDUSTRY_TEMPLATE_SEEDS) {
    if (template.sectionPresetList.length < 3) {
      throw new Error(`Template ${template.templateKey} must have at least 3 sections`);
    }
    for (const preset of template.sectionPresetList) {
      if (!isVariantImplemented(preset.variantKey)) {
        throw new Error(`Template ${template.templateKey} uses unimplemented variant ${preset.variantKey}`);
      }
      const variant = getVariant(preset.variantKey);
      if (!variant || variant.sectionTypeKey !== preset.sectionTypeKey) {
        throw new Error(`Template ${template.templateKey} variant/section mismatch for ${preset.variantKey}`);
      }
      if (!TRUTHFUL.has(preset.dataSourceIntent)) {
        throw new Error(`Template ${template.templateKey} uses non-truthful source ${preset.dataSourceIntent}`);
      }
      if (!(variant.dataSources as readonly string[]).includes(preset.dataSourceIntent)) {
        throw new Error(`Template ${template.templateKey} source ${preset.dataSourceIntent} not allowed for ${preset.variantKey}`);
      }
    }
    const signature = template.sectionPresetList.map((p) => `${p.sectionTypeKey}:${p.variantKey}`).join("|");
    if (signatures.has(signature)) {
      throw new Error(`Duplicate template composition signature: ${template.templateKey}`);
    }
    signatures.add(signature);
  }
}

/** Build Host section payloads for a template — normal editable Draft sections. */
export function buildTemplateSectionPayloads(templateKey: string): Array<{
  hostType: string;
  config: Record<string, unknown>;
}> {
  const template = getIndustryTemplate(templateKey);
  if (!template) throw new Error(`Unknown template: ${templateKey}`);
  assertIndustryTemplatesValid();

  return template.sectionPresetList.map((preset) => {
    const hostType = landingHostTypeForSection(preset.sectionTypeKey);
    if (!hostType) throw new Error(`No host type for ${preset.sectionTypeKey}`);
    const config = defaultConfigForSection(preset.sectionTypeKey, preset.variantKey);
    if (
      (preset.sectionTypeKey === "ProductShowcase" || preset.sectionTypeKey === "ProductRankedList")
      && (preset.dataSourceIntent === "Newest"
        || preset.dataSourceIntent === "Category"
        || preset.dataSourceIntent === "Brand"
        || preset.dataSourceIntent === "Manual")
    ) {
      config.source = preset.dataSourceIntent;
    }
    if (preset.dataSourceIntent === "LatestArticles") {
      config.source = "Latest";
    }
    return { hostType, config };
  });
}

export function templateSectionSummaryFa(template: IndustryTemplateSeed): string {
  const counts = {
    hero: 0,
    story: 0,
    category: 0,
    product: 0,
    banner: 0,
    brand: 0,
    article: 0,
    reviews: 0,
    promo: 0,
    other: 0,
  };
  for (const preset of template.sectionPresetList) {
    if (preset.sectionTypeKey === "HeroCarousel") counts.hero += 1;
    else if (preset.sectionTypeKey === "StoryRail") counts.story += 1;
    else if (preset.sectionTypeKey === "CategoryShowcase") counts.category += 1;
    else if (preset.sectionTypeKey === "ProductShowcase" || preset.sectionTypeKey === "ProductRankedList") counts.product += 1;
    else if (preset.sectionTypeKey === "BannerShowcase") counts.banner += 1;
    else if (preset.sectionTypeKey === "BrandShowcase") counts.brand += 1;
    else if (preset.sectionTypeKey === "ArticleShowcase") counts.article += 1;
    else if (preset.sectionTypeKey === "ReviewsShowcase") counts.reviews += 1;
    else if (preset.sectionTypeKey === "PromoSection") counts.promo += 1;
    else counts.other += 1;
  }
  const parts: string[] = [];
  if (counts.hero) parts.push(counts.hero === 1 ? "هدر تصویری" : `${counts.hero.toLocaleString("fa-IR")} هدر تصویری`);
  if (counts.story) parts.push(counts.story === 1 ? "استوری" : `${counts.story.toLocaleString("fa-IR")} استوری`);
  if (counts.category) parts.push(counts.category === 1 ? "دسته" : `${counts.category.toLocaleString("fa-IR")} بخش دسته`);
  if (counts.product) parts.push(`${counts.product.toLocaleString("fa-IR")} بخش محصول`);
  if (counts.banner) parts.push(counts.banner === 1 ? "بنر" : `${counts.banner.toLocaleString("fa-IR")} بنر`);
  if (counts.brand) parts.push(counts.brand === 1 ? "برند" : `${counts.brand.toLocaleString("fa-IR")} برند`);
  if (counts.article) parts.push(counts.article === 1 ? "مقاله" : `${counts.article.toLocaleString("fa-IR")} مقاله`);
  if (counts.reviews) parts.push("نظر خریداران");
  if (counts.promo) parts.push("پرومو");
  if (counts.other) parts.push(`${counts.other.toLocaleString("fa-IR")} بخش دیگر`);
  return `${template.sectionPresetList.length.toLocaleString("fa-IR")} بخش · ${parts.join(" + ")}`;
}

export function templateCompositionMiniature(template: IndustryTemplateSeed): string[] {
  return template.sectionPresetList.map((preset) => {
    if (preset.sectionTypeKey === "HeroCarousel") return "hero";
    if (preset.sectionTypeKey === "StoryRail") return "story";
    if (preset.sectionTypeKey === "CategoryShowcase") return "category";
    if (preset.sectionTypeKey === "ProductShowcase" || preset.sectionTypeKey === "ProductRankedList") return "product";
    if (preset.sectionTypeKey === "BannerShowcase") return "banner";
    if (preset.sectionTypeKey === "BrandShowcase") return "brand";
    if (preset.sectionTypeKey === "ArticleShowcase") return "article";
    if (preset.sectionTypeKey === "ReviewsShowcase") return "reviews";
    return "other";
  });
}
