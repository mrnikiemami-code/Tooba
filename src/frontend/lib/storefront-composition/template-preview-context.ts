/**
 * Canonical Fashion/template preview context — shared by iframe and full-page routes.
 * LOCK-SF-300 / LOCK-SF-302.
 */

export type TemplatePreviewSourceMode = "sample" | "store";
export type TemplatePreviewDeviceMode = "desktop" | "tablet" | "mobile" | "fullPage";

export type TemplatePreviewContext = {
  templateKey: string;
  sourceMode: TemplatePreviewSourceMode;
  /** BCP-47 / registry code from language tables (e.g. fa-IR, en-US). */
  locale: string;
  deviceMode: TemplatePreviewDeviceMode;
};

export type PreviewPlaceholderKind =
  | "hero"
  | "story"
  | "category"
  | "product"
  | "banner"
  | "brand"
  | "reviews"
  | "article"
  | "promo"
  | "richtext";

export const PREVIEW_PLACEHOLDER_MARK = "preview-placeholder" as const;

/** Non-persistent preview-only guidance copy (fa/en). Never written to DB. */
export function previewPlaceholderCopy(
  kind: PreviewPlaceholderKind,
  locale: string,
): { title: string; body: string } {
  const en = locale.toLowerCase().startsWith("en");
  switch (kind) {
    case "banner":
      return en
        ? { title: "Store banner", body: "Your store banner image will appear in this area." }
        : { title: "بنر فروشگاه", body: "تصویر بنر فروشگاه شما در این قسمت نمایش داده می‌شود" };
    case "hero":
      return en
        ? { title: "Store hero", body: "Your store hero image will appear in this area." }
        : { title: "هیرو فروشگاه", body: "تصویر هیرو فروشگاه شما در این قسمت نمایش داده می‌شود" };
    case "reviews":
      return en
        ? { title: "Layout preview", body: "Customer reviews from your store will appear here." }
        : { title: "پیش‌نمایش چیدمان", body: "نظرات خریداران فروشگاه شما در این قسمت نمایش داده می‌شود" };
    case "article":
      return en
        ? { title: "Layout preview", body: "Your store articles will appear in this area." }
        : { title: "پیش‌نمایش چیدمان", body: "مقالات فروشگاه شما در این قسمت نمایش داده می‌شود" };
    case "product":
      return en
        ? { title: "Layout preview", body: "Your store products will appear in this area." }
        : { title: "پیش‌نمایش چیدمان", body: "محصولات فروشگاه شما در این قسمت نمایش داده می‌شود" };
    case "category":
      return en
        ? { title: "Layout preview", body: "Your store categories will appear in this area." }
        : { title: "پیش‌نمایش چیدمان", body: "دسته‌بندی‌های فروشگاه شما در این قسمت نمایش داده می‌شود" };
    case "brand":
      return en
        ? { title: "Layout preview", body: "Your store brands will appear in this area." }
        : { title: "پیش‌نمایش چیدمان", body: "برندهای فروشگاه شما در این قسمت نمایش داده می‌شود" };
    case "story":
      return en
        ? { title: "Layout preview", body: "Your store stories will appear in this area." }
        : { title: "پیش‌نمایش چیدمان", body: "استوری‌های فروشگاه شما در این قسمت نمایش داده می‌شود" };
    case "promo":
    case "richtext":
      return en
        ? { title: "Layout preview", body: "Your store content will appear in this area." }
        : { title: "پیش‌نمایش چیدمان", body: "محتوای فروشگاه شما در این قسمت نمایش داده می‌شود" };
  }
}

export function parsePreviewSourceMode(value: string | null | undefined): TemplatePreviewSourceMode {
  return value === "store" ? "store" : "sample";
}

export function parsePreviewDeviceMode(value: string | null | undefined): TemplatePreviewDeviceMode {
  if (value === "tablet" || value === "mobile" || value === "fullPage") return value;
  return "desktop";
}

/** Build query string shared by iframe and full-page. */
export function buildTemplatePreviewQuery(ctx: Pick<TemplatePreviewContext, "sourceMode" | "locale">): string {
  const params = new URLSearchParams();
  params.set("source", ctx.sourceMode);
  if (ctx.locale) params.set("locale", ctx.locale);
  return params.toString();
}

export function fashionPreviewPath(
  fullPage: boolean,
  ctx: Pick<TemplatePreviewContext, "sourceMode" | "locale">,
): string {
  return industryTemplatePreviewPath("fashion", fullPage, ctx);
}

/** Canonical preview path for any industry template key (Fashion + Batch A). */
export function industryTemplatePreviewPath(
  templateKey: string,
  fullPage: boolean,
  ctx: Pick<TemplatePreviewContext, "sourceMode" | "locale">,
): string {
  const base = fullPage
    ? `/template-preview/${templateKey}/full`
    : `/template-preview/${templateKey}`;
  return `${base}?${buildTemplatePreviewQuery(ctx)}`;
}

export function previewStatusLabel(sourceMode: TemplatePreviewSourceMode, locale: string): string {
  const en = locale.toLowerCase().startsWith("en");
  if (sourceMode === "store") {
    return en ? "Preview with your store data" : "پیش‌نمایش با اطلاعات فروشگاه شما";
  }
  return en ? "Preview with sample data" : "پیش‌نمایش با داده نمونه";
}

/** Normalize focal 0..1; null/undefined => center 0.5. */
export function resolveObjectPosition(focalX?: number | null, focalY?: number | null): string {
  const x = focalX == null || Number.isNaN(focalX) ? 0.5 : Math.min(1, Math.max(0, focalX));
  const y = focalY == null || Number.isNaN(focalY) ? 0.5 : Math.min(1, Math.max(0, focalY));
  return `${(x * 100).toFixed(2)}% ${(y * 100).toFixed(2)}%`;
}
