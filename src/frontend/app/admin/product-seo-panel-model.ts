/** آمادگی SEO محصول از Host. */
export type ProductSeoReadiness = {
  hasValidSlug: boolean;
  hasSeoTitleOrFallback: boolean;
  hasSeoDescription: boolean;
  hasLocalizedIdentity: boolean;
  isReady: boolean;
  messageFa: string | null;
};

/** جزئیات SEO محصول برای تب Workspace. */
export type ProductSeoDetail = {
  productId: string;
  locale: string;
  slug: string | null;
  seoTitle: string | null;
  seoDescription: string | null;
  productName: string | null;
  titleFallback: string | null;
  publicPath: string;
  readiness: ProductSeoReadiness;
  updatedAt: string;
};

/** پیش‌نویس ویرایش SEO. */
export type ProductSeoDraft = {
  slug: string;
  seoTitle: string;
  seoDescription: string;
  slugTouched: boolean;
};

export const SEO_LOCALES = ["fa-IR", "en"] as const;

export const SEO_LOCALE_DISPLAY: Record<string, string> = {
  "fa-IR": "فارسی",
  en: "English",
};

/** پیش‌نویس از پاسخ سرور. */
export function draftFromSeoDetail(detail: ProductSeoDetail): ProductSeoDraft {
  return {
    slug: detail.slug ?? "",
    seoTitle: detail.seoTitle ?? "",
    seoDescription: detail.seoDescription ?? "",
    slugTouched: true,
  };
}

/** آیا پیش‌نویس نسبت به سرور کثیف است؟ */
export function isSeoDraftDirty(detail: ProductSeoDetail | null, draft: ProductSeoDraft): boolean {
  if (!detail) return false;
  const baseline = draftFromSeoDetail(detail);
  return (
    draft.slug !== baseline.slug ||
    draft.seoTitle !== baseline.seoTitle ||
    draft.seoDescription !== baseline.seoDescription
  );
}

/** عنوان نمایشی SERP با fallback مستند. */
export function resolveSeoPreviewTitle(detail: ProductSeoDetail | null, draft: ProductSeoDraft): string {
  const title = draft.seoTitle.trim();
  if (title) return title;
  const fallback = detail?.titleFallback?.trim() || detail?.productName?.trim();
  return fallback || "—";
}

/** برچسب آمادگی انسانی. */
export function formatSeoReadinessLabel(readiness: ProductSeoReadiness | null): string {
  if (!readiness) return "—";
  if (readiness.isReady) return readiness.messageFa ?? "اطلاعات سئو کامل است";
  return readiness.messageFa ?? "ناقص";
}

/** نگاشت آمادگی خام Host. */
export function mapSeoReadiness(raw: Record<string, unknown>): ProductSeoReadiness {
  const messageRaw = raw.messageFa ?? raw.MessageFa;
  return {
    hasValidSlug: Boolean(raw.hasValidSlug ?? raw.HasValidSlug),
    hasSeoTitleOrFallback: Boolean(raw.hasSeoTitleOrFallback ?? raw.HasSeoTitleOrFallback),
    hasSeoDescription: Boolean(raw.hasSeoDescription ?? raw.HasSeoDescription),
    hasLocalizedIdentity: Boolean(raw.hasLocalizedIdentity ?? raw.HasLocalizedIdentity),
    isReady: Boolean(raw.isReady ?? raw.IsReady),
    messageFa: messageRaw == null ? null : String(messageRaw),
  };
}

/** نگاشت جزئیات SEO خام Host. */
export function mapSeoDetail(raw: Record<string, unknown>): ProductSeoDetail | null {
  const productId = raw.productId ?? raw.ProductId;
  if (productId == null) return null;
  const readinessRaw = (raw.readiness ?? raw.Readiness ?? {}) as Record<string, unknown>;
  const updatedAt = raw.updatedAt ?? raw.UpdatedAt;
  return {
    productId: String(productId),
    locale: String(raw.locale ?? raw.Locale ?? "fa-IR"),
    slug: raw.slug == null && raw.Slug == null ? null : String(raw.slug ?? raw.Slug ?? ""),
    seoTitle: raw.seoTitle == null && raw.SeoTitle == null ? null : String(raw.seoTitle ?? raw.SeoTitle ?? ""),
    seoDescription:
      raw.seoDescription == null && raw.SeoDescription == null
        ? null
        : String(raw.seoDescription ?? raw.SeoDescription ?? ""),
    productName:
      raw.productName == null && raw.ProductName == null ? null : String(raw.productName ?? raw.ProductName ?? ""),
    titleFallback:
      raw.titleFallback == null && raw.TitleFallback == null
        ? null
        : String(raw.titleFallback ?? raw.TitleFallback ?? ""),
    publicPath: String(raw.publicPath ?? raw.PublicPath ?? ""),
    readiness: mapSeoReadiness(readinessRaw),
    updatedAt: updatedAt == null ? "" : String(updatedAt),
  };
}
