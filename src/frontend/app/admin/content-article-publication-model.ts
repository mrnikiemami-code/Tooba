/** مدل آمادگی انتشار مقاله — قرارداد backend-authoritative. */

export type ArticlePublicationCheck = {
  key: string;
  labelKey: string;
  required: boolean;
  satisfied: boolean;
  detail?: string | null;
  actionTarget?: string | null;
};

export type ArticlePublicationReadiness = {
  canPublish: boolean;
  checks: ArticlePublicationCheck[];
  requiredMissing: ArticlePublicationCheck[];
  recommendedMissing: ArticlePublicationCheck[];
  score: number | null;
};

export type ArticleHistoryEntry = {
  historyId: string;
  articleId: string;
  eventType: string;
  eventLabelFa: string;
  eventLabelEn: string;
  summaryFa: string;
  summaryEn: string;
  previousState: string | null;
  newState: string | null;
  actorUserId: string | null;
  actorDisplayName: string;
  occurredAt: string;
};

export type ArticleHistoryPage = {
  items: ArticleHistoryEntry[];
  totalCount: number;
  skip: number;
  take: number;
};

export type ArticlePreviewSnapshot = {
  articleId: string;
  slug: string;
  title: string;
  excerpt: string;
  body: string;
  locale: string;
  seoTitle: string | null;
  seoDescription: string | null;
  category: string | null;
  categoryId: string | null;
  authorId: string | null;
  coverMediaAssetId: string | null;
  seoImageMediaAssetId: string | null;
  authorDisplayName: string;
  tags: string[];
  isFeatured: boolean;
  status: string;
  publishDate: string;
  categorySlug: string | null;
  authorSlug: string | null;
  canonicalPath: string | null;
  isPreview: boolean;
  robotsNoIndex: boolean;
};

const LABEL_FA: Record<string, string> = {
  "content.publish.check.title": "عنوان خالی است",
  "content.publish.check.excerpt": "چکیده خالی است",
  "content.publish.check.body": "متن مقاله خالی است",
  "content.publish.check.author": "نویسنده انتخاب نشده",
  "content.publish.check.category": "دسته‌بندی انتخاب نشده",
  "content.publish.check.featured_image": "تصویر شاخص انتخاب نشده",
  "content.publish.check.seo_title": "عنوان SEO تکمیل نشده",
  "content.publish.check.seo_description": "توضیح SEO تکمیل نشده",
  "content.publish.check.seo_image": "تصویر SEO/اجتماعی یا شاخص وجود ندارد",
  "content.publish.check.language_active": "زبان مقاله فعال نیست",
  "content.publish.check.slug": "نشانی صفحه (slug) معتبر نیست",
  "content.publish.check.schedule": "زمان انتشار معتبر نیست",
  "content.publish.check.not_archived": "مقاله بایگانی‌شده قابل انتشار نیست",
};

const LABEL_EN: Record<string, string> = {
  "content.publish.check.title": "Title is missing",
  "content.publish.check.excerpt": "Excerpt is missing",
  "content.publish.check.body": "Article body is empty",
  "content.publish.check.author": "Author is not selected",
  "content.publish.check.category": "Category is not selected",
  "content.publish.check.featured_image": "Featured image is not selected",
  "content.publish.check.seo_title": "SEO title is incomplete",
  "content.publish.check.seo_description": "SEO description is incomplete",
  "content.publish.check.seo_image": "SEO/social image or featured fallback is missing",
  "content.publish.check.language_active": "Article language is inactive",
  "content.publish.check.slug": "Slug/URL is invalid",
  "content.publish.check.schedule": "Publish schedule is invalid",
  "content.publish.check.not_archived": "Archived articles cannot be published",
};

function readProp(item: Record<string, unknown>, a: string, b: string): unknown {
  return item[a] ?? item[b];
}

function mapCheck(raw: unknown): ArticlePublicationCheck | null {
  if (!raw || typeof raw !== "object") return null;
  const r = raw as Record<string, unknown>;
  const key = String(readProp(r, "key", "Key") ?? "");
  if (!key) return null;
  return {
    key,
    labelKey: String(readProp(r, "labelKey", "LabelKey") ?? ""),
    required: Boolean(readProp(r, "required", "Required")),
    satisfied: Boolean(readProp(r, "satisfied", "Satisfied")),
    detail: (readProp(r, "detail", "Detail") as string | null | undefined) ?? null,
    actionTarget: (readProp(r, "actionTarget", "ActionTarget") as string | null | undefined) ?? null,
  };
}

/** نگاشت پاسخ Host به مدل آمادگی. */
export function mapArticlePublicationReadiness(raw: unknown): ArticlePublicationReadiness | null {
  if (!raw || typeof raw !== "object") return null;
  const item = raw as Record<string, unknown>;
  const mapList = (value: unknown) =>
    Array.isArray(value)
      ? value.map(mapCheck).filter((row): row is ArticlePublicationCheck => row !== null)
      : [];
  const scoreRaw = readProp(item, "score", "Score");
  return {
    canPublish: Boolean(readProp(item, "canPublish", "CanPublish")),
    checks: mapList(readProp(item, "checks", "Checks")),
    requiredMissing: mapList(readProp(item, "requiredMissing", "RequiredMissing")),
    recommendedMissing: mapList(readProp(item, "recommendedMissing", "RecommendedMissing")),
    score: typeof scoreRaw === "number" ? scoreRaw : scoreRaw == null ? null : Number(scoreRaw),
  };
}

/** برچسب انسانی بررسی آمادگی. */
export function articleReadinessCheckLabel(check: ArticlePublicationCheck, locale: string): string {
  const fa = locale.trim().toLowerCase().startsWith("fa");
  const map = fa ? LABEL_FA : LABEL_EN;
  return map[check.labelKey] ?? map[check.key] ?? (fa ? "مورد ناقص" : "Missing item");
}

/** نگاشت تاریخچه. */
export function mapArticleHistoryPage(raw: unknown): ArticleHistoryPage | null {
  if (!raw || typeof raw !== "object") return null;
  const item = raw as Record<string, unknown>;
  const itemsRaw = readProp(item, "items", "Items");
  const items = Array.isArray(itemsRaw)
    ? itemsRaw
        .map((row) => {
          if (!row || typeof row !== "object") return null;
          const r = row as Record<string, unknown>;
          const historyId = String(readProp(r, "historyId", "HistoryId") ?? "");
          if (!historyId) return null;
          return {
            historyId,
            articleId: String(readProp(r, "articleId", "ArticleId") ?? ""),
            eventType: String(readProp(r, "eventType", "EventType") ?? ""),
            eventLabelFa: String(readProp(r, "eventLabelFa", "EventLabelFa") ?? ""),
            eventLabelEn: String(readProp(r, "eventLabelEn", "EventLabelEn") ?? ""),
            summaryFa: String(readProp(r, "summaryFa", "SummaryFa") ?? ""),
            summaryEn: String(readProp(r, "summaryEn", "SummaryEn") ?? ""),
            previousState: (readProp(r, "previousState", "PreviousState") as string | null) ?? null,
            newState: (readProp(r, "newState", "NewState") as string | null) ?? null,
            actorUserId: (readProp(r, "actorUserId", "ActorUserId") as string | null) ?? null,
            actorDisplayName: String(readProp(r, "actorDisplayName", "ActorDisplayName") ?? ""),
            occurredAt: String(readProp(r, "occurredAt", "OccurredAt") ?? ""),
          } satisfies ArticleHistoryEntry;
        })
        .filter((row): row is ArticleHistoryEntry => row !== null)
    : [];
  return {
    items,
    totalCount: Number(readProp(item, "totalCount", "TotalCount") ?? items.length),
    skip: Number(readProp(item, "skip", "Skip") ?? 0),
    take: Number(readProp(item, "take", "Take") ?? items.length),
  };
}

/** نگاشت پیش‌نمایش Admin. */
export function mapArticlePreviewSnapshot(raw: unknown): ArticlePreviewSnapshot | null {
  if (!raw || typeof raw !== "object") return null;
  const r = raw as Record<string, unknown>;
  const articleId = String(readProp(r, "articleId", "ArticleId") ?? "");
  if (!articleId) return null;
  const tagsRaw = readProp(r, "tags", "Tags");
  return {
    articleId,
    slug: String(readProp(r, "slug", "Slug") ?? ""),
    title: String(readProp(r, "title", "Title") ?? ""),
    excerpt: String(readProp(r, "excerpt", "Excerpt") ?? ""),
    body: String(readProp(r, "body", "Body") ?? ""),
    locale: String(readProp(r, "locale", "Locale") ?? "fa-IR"),
    seoTitle: (readProp(r, "seoTitle", "SeoTitle") as string | null) ?? null,
    seoDescription: (readProp(r, "seoDescription", "SeoDescription") as string | null) ?? null,
    category: (readProp(r, "category", "Category") as string | null) ?? null,
    categoryId: (readProp(r, "categoryId", "CategoryId") as string | null) ?? null,
    authorId: (readProp(r, "authorId", "AuthorId") as string | null) ?? null,
    coverMediaAssetId: (readProp(r, "coverMediaAssetId", "CoverMediaAssetId") as string | null) ?? null,
    seoImageMediaAssetId: (readProp(r, "seoImageMediaAssetId", "SeoImageMediaAssetId") as string | null) ?? null,
    authorDisplayName: String(readProp(r, "authorDisplayName", "AuthorDisplayName") ?? ""),
    tags: Array.isArray(tagsRaw) ? tagsRaw.map((t) => String(t)) : [],
    isFeatured: Boolean(readProp(r, "isFeatured", "IsFeatured")),
    status: String(readProp(r, "status", "Status") ?? ""),
    publishDate: String(readProp(r, "publishDate", "PublishDate") ?? ""),
    categorySlug: (readProp(r, "categorySlug", "CategorySlug") as string | null) ?? null,
    authorSlug: (readProp(r, "authorSlug", "AuthorSlug") as string | null) ?? null,
    canonicalPath: (readProp(r, "canonicalPath", "CanonicalPath") as string | null) ?? null,
    isPreview: Boolean(readProp(r, "isPreview", "IsPreview") ?? true),
    robotsNoIndex: Boolean(readProp(r, "robotsNoIndex", "RobotsNoIndex") ?? true),
  };
}
