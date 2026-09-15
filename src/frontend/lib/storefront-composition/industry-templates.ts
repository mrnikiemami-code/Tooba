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
      items: [{ imageUrl: "", title: "استوری ۱", href: "/products", enabled: true }],
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
  return `${template.sectionPresetList.length} بخش · ${template.sectionPresetList
    .map((p) => getVariant(p.variantKey)?.nameFa ?? p.variantKey)
    .slice(0, 4)
    .join("، ")}${template.sectionPresetList.length > 4 ? "…" : ""}`;
}
