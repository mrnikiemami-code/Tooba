import type { SectionWizardStep } from "./admin-landing-section-forms-types.ts";

export type { SectionWizardStep };

function asStringArray(value: unknown): string[] {
  if (!Array.isArray(value)) return [];
  return value.map((item) => String(item)).filter(Boolean);
}

export function validateWizardStep(
  step: SectionWizardStep,
  draft: {
    sectionTypeKey?: string | null;
    variantKey?: string | null;
    hostType?: string | null;
    config: Record<string, unknown>;
  },
): string | null {
  if (step === "type" && !draft.sectionTypeKey) return "یک نوع بخش انتخاب کنید.";
  if (step === "variant" && !draft.variantKey) return "یک ظاهر انتخاب کنید.";
  if (step === "source") {
    const host = draft.hostType;
    const config = draft.config;
    if (host === "ProductCollection") {
      const source = typeof config.source === "string" ? config.source : "Newest";
      if (source === "Manual" && asStringArray(config.productIds).length === 0) return "حداقل یک کالا انتخاب کنید.";
      if (source === "Category" && !(typeof config.categoryId === "string" && config.categoryId)) return "یک دسته انتخاب کنید.";
      if (source === "Brand" && !(typeof config.brandId === "string" && config.brandId)) return "یک برند انتخاب کنید.";
    }
    if (host === "ArticleList") {
      const source = typeof config.source === "string" ? config.source : "Latest";
      if (source === "Manual" && asStringArray(config.articleIds).length === 0) return "حداقل یک مطلب انتخاب کنید.";
    }
    if (host === "CategoryGrid" && asStringArray(config.categoryIds ?? config.ids).length === 0) {
      return "حداقل یک دسته انتخاب کنید.";
    }
    if (host === "BrandStrip" && asStringArray(config.brandIds ?? config.ids).length === 0) {
      return "حداقل یک برند انتخاب کنید.";
    }
  }
  return null;
}

export function wizardStepsForHost(hostType: string | null | undefined): SectionWizardStep[] {
  const base: SectionWizardStep[] = ["type", "variant"];
  if (!hostType) return [...base, "preview"];
  if (hostType === "StoryRail") return [...base, "settings", "preview"];
  if (hostType === "Reviews" || hostType === "RichText" || hostType === "Hero" || hostType === "PromoBanner" || hostType === "NavigationMenu") {
    return [...base, "settings", "preview"];
  }
  if (hostType === "BannerShowcase") return [...base, "settings", "preview"];
  return [...base, "source", "settings", "preview"];
}
