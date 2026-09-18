import { incompleteSourceWarning, parseLandingConfig } from "./landing-section-catalog.ts";
import type { AdminLandingSection, StorePageType } from "./admin-landing-pages-api.ts";

export type LandingReadinessGroup = "page" | "seo" | "sections";

export type LandingPublishCheck = {
  key: string;
  group: LandingReadinessGroup;
  required: boolean;
  satisfied: boolean;
  titleFa: string;
  titleEn: string;
  descriptionFa: string;
  descriptionEn: string;
  actionTarget?: LandingReadinessGroup;
};

export type LandingPublishReadiness = {
  checks: LandingPublishCheck[];
  missing: LandingPublishCheck[];
  readyCount: number;
  totalCount: number;
  canPublish: boolean;
  pageTabIncomplete: boolean;
  seoTabIncomplete: boolean;
};

export type LandingReadinessInput = {
  title: string;
  slug: string;
  pageType: StorePageType;
  isHome: boolean;
  seoTitle: string;
  seoDescription: string;
  sections: AdminLandingSection[];
};

function check(
  partial: Omit<LandingPublishCheck, "required"> & { required?: boolean },
): LandingPublishCheck {
  return { required: partial.required ?? true, ...partial };
}

/** چک‌لیست انتشار صفحه فروشگاه — منبع واحد برای ذخیرهٔ پیش‌نویس و دروازهٔ انتشار. */
export function buildLandingPublishReadiness(input: LandingReadinessInput): LandingPublishReadiness {
  const enabled = input.sections.filter((row) => row.isEnabled);
  const checks: LandingPublishCheck[] = [
    check({
      key: "title",
      group: "page",
      satisfied: Boolean(input.title.trim()),
      titleFa: "عنوان صفحه",
      titleEn: "Page title",
      descriptionFa: "عنوان صفحه برای نمایش در فهرست و ویترین الزامی است.",
      descriptionEn: "A page title is required for admin lists and the storefront.",
      actionTarget: "page",
    }),
    check({
      key: "seoTitle",
      group: "seo",
      satisfied: Boolean(input.seoTitle.trim()),
      titleFa: "عنوان سئو",
      titleEn: "SEO title",
      descriptionFa: "عنوان سئو برای نتیجهٔ جستجو و اشتراک‌گذاری لازم است.",
      descriptionEn: "An SEO title is required for search results and sharing.",
      actionTarget: "seo",
    }),
    check({
      key: "seoDescription",
      group: "seo",
      satisfied: Boolean(input.seoDescription.trim()),
      titleFa: "توضیح سئو",
      titleEn: "SEO description",
      descriptionFa: "توضیح سئو باید پر شود تا صفحه برای انتشار آماده باشد.",
      descriptionEn: "An SEO description is required before the page can be published.",
      actionTarget: "seo",
    }),
    check({
      key: "enabledSection",
      group: "sections",
      satisfied: enabled.length > 0,
      titleFa: "حداقل یک بخش فعال",
      titleEn: "At least one enabled section",
      descriptionFa: "برای انتشار باید دست‌کم یک بخش فعال در صفحه وجود داشته باشد.",
      descriptionEn: "Publishing requires at least one enabled section on the page.",
      actionTarget: "sections",
    }),
  ];

  for (const row of enabled) {
    if (!sectionParticipatesInSourceReadiness(row.sectionType)) continue;
    const config = parseLandingConfig(row.config);
    const warning = incompleteSourceWarning(row.sectionType, config);
    checks.push(
      check({
        key: `section-source-${row.pageSectionId}`,
        group: "sections",
        satisfied: warning == null,
        titleFa: `منبع بخش «${landingSectionTypeLabelFa(row.sectionType)}»`,
        titleEn: `Section source (${row.sectionType})`,
        descriptionFa: warning ?? "منبع این بخش کامل است.",
        descriptionEn: warning ?? "This section source is complete.",
        actionTarget: "sections",
      }),
    );
  }

  const required = checks.filter((item) => item.required);
  const missing = required.filter((item) => !item.satisfied);
  const readyCount = required.filter((item) => item.satisfied).length;
  const totalCount = required.length;

  return {
    checks,
    missing,
    readyCount,
    totalCount,
    canPublish: missing.length === 0,
    pageTabIncomplete: missing.some((item) => item.group === "page"),
    seoTabIncomplete: missing.some((item) => item.group === "seo"),
  };
}

/** بخش‌هایی که منبع محتوایشان در چک‌لیست انتشار می‌ماند (حتی پس از تکمیل). */
function sectionParticipatesInSourceReadiness(sectionType: string): boolean {
  return (
    sectionType === "ProductCollection"
    || sectionType === "ArticleList"
    || sectionType === "CategoryGrid"
    || sectionType === "BrandStrip"
    || sectionType === "BannerShowcase"
  );
}

function landingSectionTypeLabelFa(sectionType: string): string {
  switch (sectionType) {
    case "ProductCollection":
      return "مجموعه کالا";
    case "ArticleList":
      return "فهرست مطالب";
    case "CategoryGrid":
      return "شبکه دسته";
    case "BrandStrip":
      return "نوار برند";
    case "BannerShowcase":
      return "بنر";
    case "Hero":
      return "اسلایدر اصلی";
    default:
      return sectionType;
  }
}

export function landingCheckTitle(check: LandingPublishCheck, locale: string): string {
  return locale.trim().toLowerCase().startsWith("en") ? check.titleEn : check.titleFa;
}

export function landingCheckDescription(check: LandingPublishCheck, locale: string): string {
  return locale.trim().toLowerCase().startsWith("en") ? check.descriptionEn : check.descriptionFa;
}
