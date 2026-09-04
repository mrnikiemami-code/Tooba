"use client";

import Link from "next/link";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { toast } from "react-toastify";
import { Spinner, useAdminFormMode } from "../../design-system";
import { prepareAdminDevActor } from "./admin-api.ts";
import {
  capabilityPermissionIds,
  createAdminAccessApi,
  hasCapability,
} from "../access-control/access-control-api.ts";
import { fetchActiveContentAuthors, type ContentAuthorPickerItem } from "./content-author-api.ts";
import { fetchContentCategoryTree, type ContentCategoryTreeNodeDto } from "./content-category-api.ts";
import { ContentArticleCategoryPicker } from "./content-article-category-picker.tsx";
import { ContentArticleTagsPanel } from "./content-article-tags-panel.tsx";
import { MediaLibraryDialog } from "./media-library-dialog.tsx";
import { mediaPreviewUrl } from "./media-api.ts";
import { sanitizeArticleRichHtml } from "./article-rich-html.ts";
import { ContentArticleDestructiveDialog, type ArticleDestructiveKind } from "./content-article-destructive-dialog.tsx";
import { ContentArticleEditor } from "./content-article-editor.tsx";
import { ContentArticleMediaPanel } from "./content-article-media-panel.tsx";
import { ContentArticleReadinessSummary } from "./content-article-readiness-summary.tsx";
import { ContentArticleHistoryTimeline } from "./content-article-history-timeline.tsx";
import { ContentArticlePublishDateField } from "./content-article-publish-date-field.tsx";
import { ContentArticleCommentsPanel } from "./content-article-comments-panel.tsx";
import { ContentHelpAffordance } from "./content-help-affordance.tsx";
import { CONTENT_HELP_PAGE_HREF } from "./content-help-content.ts";
import { AdminSearchableCombobox } from "./admin-searchable-combobox.tsx";
import type {
  ArticleHistoryEntry,
  ArticlePublicationReadiness,
} from "./content-article-publication-model.ts";
import {
  assignArticleSeoImage,
  fetchArticleMediaWorkspace,
  mapArticleMediaMutationError,
  type ArticleMediaWorkspaceDto,
} from "./content-article-media-api.ts";
import { mapAdminErrorMessage } from "./admin-error-map.ts";
import { loadAdminLanguages } from "./language-api.ts";
import type { SupportedLocaleDefinition } from "../../lib/i18n/supported-locales.ts";
import {
  articleEditorDirection,
  archiveAdminArticle,
  canArchiveArticle,
  canHardDeleteArticle,
  deleteAdminArticle,
  formatArticleDate,
  formatArticleLocaleLabel,
  isArticleArchived,
  isArticleLocaleLocked,
  isArticlePublished,
  loadAdminArticle,
  loadArticleHistory,
  loadArticlePublishReadiness,
  publishAdminArticle,
  unpublishAdminArticle,
  updateAdminArticle,
  type AdminContentArticle,
} from "../content/content-api.ts";

const TABS = [
  { id: "full", label: "ویرایش کامل" },
  { id: "general", label: "عمومی" },
  { id: "content", label: "محتوا" },
  { id: "categories", label: "دسته‌بندی‌ها" },
  { id: "author", label: "نویسنده" },
  { id: "media", label: "رسانه" },
  { id: "seo", label: "جستجو و اشتراک" },
  { id: "publication", label: "انتشار" },
  { id: "comments", label: "نظرات" },
  { id: "history", label: "تاریخچه" },
] as const;

type TabId = (typeof TABS)[number]["id"];

type DamPickKind = "image" | "file" | "video";
type DamPickResult = {
  mediaAssetId: string;
  alt?: string;
  title?: string;
  fileName?: string;
} | null;

function languageOptionLabel(lang: SupportedLocaleDefinition): string {
  const native = lang.nativeName?.trim() ?? "";
  if (native && !/^\?+$/.test(native)) return native;
  return lang.displayName?.trim() || lang.code;
}

const LOCALE_LOCKED_MESSAGE =
  "زبان این مقاله به‌دلیل وجود محتوا یا وابستگی‌های ثبت‌شده قابل تغییر نیست.";

const HISTORY_PAGE_SIZE = 10;
const PRIOR_PUBLICATION_EVENT_TYPES = [
  "article.published",
  "article.republished",
  "article.scheduled",
] as const;

function isPublished(status: string): boolean {
  return isArticlePublished(status);
}

function historyHasPriorPublication(entries: ArticleHistoryEntry[]): boolean {
  return entries.some((e) =>
    (PRIOR_PUBLICATION_EVENT_TYPES as readonly string[]).includes(e.eventType),
  );
}

/** Fallback option so inactive/persisted article locale remains selectable in the dropdown. */
function inactiveLocaleOption(code: string): SupportedLocaleDefinition {
  const fa = code.trim().toLowerCase().startsWith("fa");
  const label = formatArticleLocaleLabel(code);
  return {
    code,
    urlPrefix: fa ? "fa" : "en",
    displayName: label,
    nativeName: label,
    direction: articleEditorDirection(code),
    culture: code,
    calendarDisplay: fa ? "jalali" : "gregorian",
    active: false,
    default: false,
    sortOrder: 9999,
  };
}

function statusLabel(status: string): string {
  if (isArticleArchived(status)) return "بایگانی";
  if (isPublished(status)) return "منتشر";
  return "پیش‌نویس";
}

/** workspace ویرایش مقاله — تب‌های عمومی/محتوا/دسته/نویسنده/رسانه/SEO/انتشار/تاریخچه. */
export function ContentArticleAdminScreen() {
  const params = useParams<{ articleId?: string }>();
  const router = useRouter();
  const searchParams = useSearchParams();
  const articleId = typeof params.articleId === "string" ? params.articleId : null;
  const [article, setArticle] = useState<AdminContentArticle | null>(null);
  const [tab, setTab] = useState<TabId>("full");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [categoryOptions, setCategoryOptions] = useState<ContentCategoryTreeNodeDto[]>([]);
  const [authorOptions, setAuthorOptions] = useState<ContentAuthorPickerItem[]>([]);
  const [languageOptions, setLanguageOptions] = useState<SupportedLocaleDefinition[]>([]);
  const [damPickKind, setDamPickKind] = useState<DamPickKind | null>(null);
  const [seoImageOpen, setSeoImageOpen] = useState(false);
  const [mediaWorkspace, setMediaWorkspace] = useState<ArticleMediaWorkspaceDto | null>(null);
  const [useFeaturedForSeo, setUseFeaturedForSeo] = useState(true);
  const [destructiveKind, setDestructiveKind] = useState<ArticleDestructiveKind | null>(null);
  const damPickResolveRef = useRef<((value: DamPickResult) => void) | null>(null);

  const [draftTitle, setDraftTitle] = useState("");
  const [draftExcerpt, setDraftExcerpt] = useState("");
  const [draftBody, setDraftBody] = useState("");
  // Article identity locale — empty until applyArticle; never seed fa-IR / Admin UI locale.
  // Admin UI shell may stay Persian; Article identity is the persisted locale from Host.
  const [draftLocale, setDraftLocale] = useState("");
  const [draftIsFeatured, setDraftIsFeatured] = useState(false);
  const [draftCategoryId, setDraftCategoryId] = useState("");
  const [draftCategory, setDraftCategory] = useState("");
  const [draftAuthorId, setDraftAuthorId] = useState("");
  const [draftSeoTitle, setDraftSeoTitle] = useState("");
  const [draftSeoDescription, setDraftSeoDescription] = useState("");
  const [draftPublishDate, setDraftPublishDate] = useState("");
  const [tagsEpoch, setTagsEpoch] = useState(0);
  const [readiness, setReadiness] = useState<ArticlePublicationReadiness | null>(null);
  const [historyEntries, setHistoryEntries] = useState<ArticleHistoryEntry[]>([]);
  const [historyPage, setHistoryPage] = useState(1);
  const [historyTotal, setHistoryTotal] = useState(0);
  const [historyLoading, setHistoryLoading] = useState(false);
  const [hasPriorPublication, setHasPriorPublication] = useState(false);
  const [commentsPendingCount, setCommentsPendingCount] = useState(0);

  const [canEditContent, setCanEditContent] = useState(true);
  useEffect(() => {
    void createAdminAccessApi()
      .getMyCapabilities()
      .then((effective) => {
        const caps = capabilityPermissionIds(effective);
        setCanEditContent(hasCapability(caps, "content.edit"));
      })
      .catch(() => setCanEditContent(true));
  }, []);
  const form = useAdminFormMode({ canView: true, canEdit: canEditContent });
  const editorDir = articleEditorDirection(draftLocale);
  const localeLocked = article
    ? isArticleLocaleLocked({
        ...article,
        seoImageMediaAssetId: mediaWorkspace?.seoImageMediaAssetId ?? article.seoImageMediaAssetId,
      })
    : false;
  const archived = article ? isArticleArchived(article.status) : false;
  const authorComboboxOptions = useMemo(() => {
    const options = authorOptions.map((row) => ({
      value: row.authorId,
      label: row.slug?.trim() ? `${row.displayName} · ${row.slug}` : row.displayName,
    }));
    if (
      draftAuthorId &&
      !options.some((row) => row.value === draftAuthorId) &&
      (article?.authorDisplayName || article?.authorId === draftAuthorId)
    ) {
      const inactiveName = article?.authorDisplayName?.trim() || draftAuthorId;
      options.push({
        value: draftAuthorId,
        label: `${inactiveName} · غیرفعال`,
      });
    }
    return options;
  }, [article?.authorDisplayName, article?.authorId, authorOptions, draftAuthorId]);
  const selectedAuthorLabel =
    authorComboboxOptions.find((row) => row.value === draftAuthorId)?.label ||
    article?.authorDisplayName ||
    "—";
  // Language badge/header: Article locale only (draftLocale after apply / article.locale) — never Admin UI locale.
  // Do NOT apply searchParams.language to overwrite existing article locale identity.
  const selectedLanguageLabel = (() => {
    const identityLocale = draftLocale || article?.locale || "";
    if (!identityLocale) return "—";
    const row = languageOptions.find((item) => item.code === identityLocale);
    return row ? languageOptionLabel(row) : formatArticleLocaleLabel(identityLocale);
  })();

  const localeSelectOptions = useMemo(() => {
    const options = languageOptions.slice();
    if (draftLocale && !options.some((row) => row.code === draftLocale)) {
      options.push(inactiveLocaleOption(draftLocale));
    }
    return options;
  }, [draftLocale, languageOptions]);

  const historyTotalPages = Math.max(1, Math.ceil(historyTotal / HISTORY_PAGE_SIZE) || 1);
  const historyPagerFa = !(draftLocale || article?.locale || "").toLowerCase().startsWith("en");

  const effectiveSeoAssetId =
    mediaWorkspace?.effectiveSeoImageMediaAssetId ??
    mediaWorkspace?.seoImageMediaAssetId ??
    mediaWorkspace?.featuredMediaAssetId ??
    null;

  const requestedMode = searchParams.get("mode");
  useEffect(() => {
    if (requestedMode === "edit" && !archived && form.mode !== "edit") {
      form.onEdit();
    }
  }, [requestedMode, archived, form.mode, form.onEdit]);

  const applyArticle = useCallback((data: AdminContentArticle) => {
    setArticle(data);
    setDraftTitle(data.title);
    setDraftExcerpt(data.excerpt);
    setDraftBody(data.body);
    // Always take persisted article locale — ignore URL language query mismatches.
    setDraftLocale(data.locale);
    setDraftIsFeatured(data.isFeatured);
    setDraftCategoryId(data.categoryId ?? "");
    setDraftCategory(data.category ?? "");
    setDraftAuthorId(data.authorId ?? "");
    setDraftSeoTitle(data.seoTitle ?? "");
    setDraftSeoDescription(data.seoDescription ?? "");
    setDraftPublishDate(data.publishDate || "");
    setTagsEpoch((n) => n + 1);
  }, []);

  const refreshReadiness = useCallback(async (id: string) => {
    const result = await loadArticlePublishReadiness(id);
    if (result.state === "ok" && result.data) setReadiness(result.data);
  }, []);

  const refreshPriorPublication = useCallback(async (id: string, status: string) => {
    if (isArticlePublished(status) || isArticleArchived(status)) {
      setHasPriorPublication(true);
      return;
    }
    const result = await loadArticleHistory(id, 0, 20);
    if (result.state === "ok" && result.data) {
      setHasPriorPublication(historyHasPriorPublication(result.data.items));
    }
  }, []);

  const refreshHistory = useCallback(async (id: string, page = 1) => {
    setHistoryLoading(true);
    try {
      const skip = (Math.max(1, page) - 1) * HISTORY_PAGE_SIZE;
      const result = await loadArticleHistory(id, skip, HISTORY_PAGE_SIZE);
      if (result.state === "ok" && result.data) {
        setHistoryEntries(result.data.items);
        setHistoryTotal(result.data.totalCount);
        setHistoryPage(Math.max(1, page));
        if (historyHasPriorPublication(result.data.items)) {
          setHasPriorPublication(true);
        }
      }
    } finally {
      setHistoryLoading(false);
    }
  }, []);

  const refreshArticle = useCallback(
    async (id: string) => {
      const result = await loadAdminArticle(id);
      if (result.state !== "ok" || !result.data) {
        setArticle(null);
        return;
      }
      applyArticle(result.data);
      await Promise.all([
        refreshReadiness(id),
        refreshPriorPublication(id, result.data.status),
      ]);
    },
    [applyArticle, refreshPriorPublication, refreshReadiness],
  );

  const refreshMediaWorkspace = useCallback(async (id: string) => {
    const result = await fetchArticleMediaWorkspace(id);
    if (result.state === "ok" && result.data) {
      setMediaWorkspace(result.data);
      setUseFeaturedForSeo(!result.data.seoImageMediaAssetId);
    }
  }, []);

  const handleMediaWorkspaceChange = useCallback((workspace: ArticleMediaWorkspaceDto) => {
    setMediaWorkspace(workspace);
    setUseFeaturedForSeo(!workspace.seoImageMediaAssetId);
  }, []);

  useEffect(() => {
    if (!articleId) {
      setArticle(null);
      setLoading(false);
      return;
    }
    setLoading(true);
    void prepareAdminDevActor()
      .then(() =>
        Promise.all([
          refreshArticle(articleId),
          refreshMediaWorkspace(articleId),
          fetchActiveContentAuthors(),
          loadAdminLanguages(),
        ]),
      )
      .then(([, , authors, languages]) => {
        if (authors.state === "ok" && authors.data) setAuthorOptions(authors.data);
        if (languages.state === "ok" && languages.data) {
          setLanguageOptions(
            languages.data
              .filter((row) => row.active)
              .slice()
              .sort((a, b) => a.sortOrder - b.sortOrder || a.code.localeCompare(b.code)),
          );
        }
      })
      .finally(() => setLoading(false));
  }, [articleId, refreshArticle, refreshMediaWorkspace]);

  useEffect(() => {
    if (!draftLocale) return;
    void fetchContentCategoryTree(draftLocale).then((result) => {
      if (result.state === "ok" && result.data) setCategoryOptions(result.data);
    });
  }, [draftLocale]);

  useEffect(() => {
    if (!articleId) return;
    setHistoryPage(1);
    setHistoryEntries([]);
    setHistoryTotal(0);
  }, [articleId]);

  useEffect(() => {
    if (!articleId || tab !== "history") return;
    void refreshHistory(articleId, historyPage);
  }, [articleId, tab, historyPage, refreshHistory]);

  const resolveDamPick = useCallback((value: DamPickResult) => {
    const resolve = damPickResolveRef.current;
    damPickResolveRef.current = null;
    resolve?.(value);
  }, []);

  const pickDamImage = useCallback(() => {
    return new Promise<DamPickResult>((resolve) => {
      damPickResolveRef.current = resolve;
      setDamPickKind("image");
    });
  }, []);

  const pickDamFile = useCallback(() => {
    return new Promise<DamPickResult>((resolve) => {
      damPickResolveRef.current = resolve;
      setDamPickKind("file");
    });
  }, []);

  const pickDamVideo = useCallback(() => {
    return new Promise<DamPickResult>((resolve) => {
      damPickResolveRef.current = resolve;
      setDamPickKind("video");
    });
  }, []);

  const savePayload = useMemo(
    () => ({
      title: draftTitle,
      excerpt: draftExcerpt,
      body: sanitizeArticleRichHtml(draftBody),
      authorId: draftAuthorId || null,
      categoryId: draftCategoryId || null,
      category: draftCategory || null,
      // از workspace رسانهٔ همگام‌شده — نه مقدار کهنهٔ قبل از اختصاص شاخص
      coverMediaAssetId:
        mediaWorkspace?.featuredMediaAssetId ?? article?.coverMediaAssetId ?? null,
      seoTitle: draftSeoTitle || null,
      seoDescription: draftSeoDescription || null,
      tags: [],
      isFeatured: draftIsFeatured,
      locale: draftLocale,
      publishDate: draftPublishDate ? new Date(draftPublishDate).toISOString() : null,
    }),
    [
      article?.coverMediaAssetId,
      mediaWorkspace?.featuredMediaAssetId,
      draftAuthorId,
      draftBody,
      draftCategory,
      draftCategoryId,
      draftExcerpt,
      draftIsFeatured,
      draftLocale,
      draftPublishDate,
      draftSeoDescription,
      draftSeoTitle,
      draftTitle,
    ],
  );

  const save = useCallback(async () => {
    if (!article) return;
    const languageActive = languageOptions.some((row) => row.code === draftLocale && row.active);
    if (languageOptions.length > 0 && !languageActive) {
      toast.error(mapAdminErrorMessage("localization.language.inactive", "fa"));
      return;
    }
    if (draftCategoryId) {
      const selected = categoryOptions.find((row) => row.id === draftCategoryId);
      if (!selected || selected.languageCode !== draftLocale) {
        toast.error(mapAdminErrorMessage("content.category.language_mismatch", "fa"));
        return;
      }
    }
    setSaving(true);
    const result = await updateAdminArticle(article.articleId, savePayload);
    setSaving(false);
    if (!result.ok || !result.article) {
      toast.error(mapAdminErrorMessage(result.message ?? "content.update.rejected", "fa"));
      return;
    }
    toast.success("ذخیره شد");
    form.onSaved();
    applyArticle(result.article);
    await refreshMediaWorkspace(article.articleId);
    await refreshReadiness(article.articleId);
  }, [
    applyArticle,
    article,
    categoryOptions,
    draftCategoryId,
    draftLocale,
    form,
    languageOptions,
    refreshMediaWorkspace,
    refreshReadiness,
    savePayload,
  ]);

  const togglePublish = useCallback(async () => {
    if (!article) return;
    setSaving(true);
    const publishing = !isPublished(article.status);
    const result = publishing
      ? await publishAdminArticle(article.articleId)
      : await unpublishAdminArticle(article.articleId);
    setSaving(false);
    if (!result.ok) {
      toast.error(mapAdminErrorMessage(result.message ?? "content.publish.not_ready", article.locale.startsWith("fa") ? "fa" : "en"));
      return;
    }
    toast.success(
      publishing
        ? hasPriorPublication
          ? "منتشر مجدد شد"
          : "منتشر شد"
        : "انتشار لغو شد",
    );
    setDestructiveKind(null);
    await refreshArticle(article.articleId);
  }, [article, hasPriorPublication, refreshArticle]);

  const handlePreview = useCallback(() => {
    if (!article) return;
    if (form.mode === "edit" && form.isDirty) {
      toast.info("برای پیش‌نمایش ابتدا تغییرات را ذخیره کنید.");
      return;
    }
    window.open(`/admin/content/articles/${article.articleId}/preview`, "_blank", "noopener,noreferrer");
  }, [article, form.isDirty, form.mode]);

  const openPublishDialog = useCallback(() => {
    if (!article || archived) return;
    if (isPublished(article.status)) {
      setDestructiveKind("unpublish");
      return;
    }
    setDestructiveKind(hasPriorPublication ? "republish" : "publish");
  }, [article, archived, hasPriorPublication]);

  const publishDateIsFuture = useMemo(() => {
    if (!draftPublishDate) return false;
    const t = new Date(draftPublishDate).getTime();
    return Number.isFinite(t) && t > Date.now();
  }, [draftPublishDate]);

  const handleCancel = useCallback(() => {
    if (!article) return;
    if (!form.confirmDiscardIfDirty()) return;
    applyArticle(article);
    form.onCancel();
  }, [applyArticle, article, form]);

  const handleDelete = useCallback(async () => {
    if (!article || !canHardDeleteArticle(article.status)) return;
    setSaving(true);
    const result = await deleteAdminArticle(article.articleId);
    setSaving(false);
    if (!result.ok) {
      toast.error(mapAdminErrorMessage(result.message ?? "admin.error.generic", "fa"));
      return;
    }
    toast.success("مقاله حذف شد");
    setDestructiveKind(null);
    router.push("/admin/content");
  }, [article, router]);

  const handleArchive = useCallback(async () => {
    if (!article || !canArchiveArticle(article.status)) return;
    setSaving(true);
    const result = await archiveAdminArticle(article.articleId);
    setSaving(false);
    if (!result.ok) {
      toast.error(mapAdminErrorMessage(result.message ?? "admin.error.generic", "fa"));
      return;
    }
    toast.success("مقاله بایگانی شد");
    setDestructiveKind(null);
    await refreshArticle(article.articleId);
    form.resetToView();
  }, [article, form, refreshArticle]);

  const applySeoWorkspace = useCallback((data: ArticleMediaWorkspaceDto) => {
    setMediaWorkspace(data);
    setUseFeaturedForSeo(!data.seoImageMediaAssetId);
  }, []);

  if (!articleId) {
    return (
      <main className="p-4">
        <p>شناسهٔ مقاله نامعتبر است.</p>
      </main>
    );
  }

  if (loading) {
    return (
      <main className="p-4" data-testid="content-article-admin-loading">
        <p className="text-muted">در حال بارگذاری…</p>
      </main>
    );
  }

  if (!article) {
    return (
      <main className="p-4" data-testid="content-article-admin-missing">
        <p>مقاله یافت نشد.</p>
        <Link href="/admin/content" className="text-[#2563EB] underline">
          بازگشت به فهرست
        </Link>
      </main>
    );
  }

  const dirty = form.mode === "edit" && form.isDirty;
  const saveStateLabel =
    form.mode === "view" ? "فقط مشاهده" : saving ? "در حال ذخیره…" : dirty ? "تغییرات ذخیره‌نشده" : "ذخیره‌شده";

  return (
    <main className="w-full p-4" data-testid="content-article-admin">
      <div
        className="mb-4 rounded-2xl border bg-surface-elevated p-4"
        data-testid="content-article-workspace-header"
      >
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div className="min-w-0 flex-1 space-y-2">
            <div className="flex flex-wrap items-center gap-3">
              <Link href="/admin/content" className="text-sm text-[#2563EB] underline">
                بازگشت به فهرست مقالات
              </Link>
              <Link
                href={CONTENT_HELP_PAGE_HREF}
                className="text-sm text-muted underline"
                data-testid="content-article-help-link"
              >
                راهنمای محتوا
              </Link>
            </div>
            <h1 className="truncate text-xl font-bold" data-testid="content-article-workspace-title">
              {draftTitle || article.title || "بدون عنوان"}
            </h1>
            <div className="flex flex-wrap items-center gap-2 text-sm">
              <span
                className="rounded-full border px-2.5 py-0.5 text-xs font-semibold"
                data-testid="content-article-workspace-status"
              >
                {statusLabel(article.status)}
              </span>
              <span
                className="rounded-full border px-2.5 py-0.5 text-xs"
                data-testid="content-article-workspace-language"
              >
                زبان: {selectedLanguageLabel}
              </span>
              <span
                className={
                  dirty
                    ? "rounded-full border border-amber-300 bg-amber-50 px-2.5 py-0.5 text-xs text-amber-900"
                    : "rounded-full border px-2.5 py-0.5 text-xs text-muted"
                }
                data-testid="content-article-workspace-save-state"
              >
                {saveStateLabel}
              </span>
              <span className="text-xs text-muted" dir="ltr">
                {article.slug}
              </span>
              <span
                className="rounded-full border px-2.5 py-0.5 text-xs font-medium"
                data-testid="content-article-workspace-mode"
              >
                {form.mode === "view" ? "VIEW" : "EDIT"}
              </span>
              <ContentArticleReadinessSummary
                readiness={readiness}
                locale={draftLocale || article.locale}
                onNavigate={(target) => setTab(target as TabId)}
              />
            </div>
          </div>

          <div className="flex flex-wrap items-center gap-2" data-testid="content-article-primary-actions">
            {form.mode === "edit" ? (
              <button
                type="button"
                className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-semibold text-white"
                disabled={saving}
                data-testid="content-article-save"
                onClick={() => void save()}
              >
                {saving ? "در حال ذخیره…" : "ذخیره"}
              </button>
            ) : null}
            <button
              type="button"
              className="rounded-xl border px-4 py-2 text-sm"
              data-testid="content-article-preview"
              onClick={handlePreview}
            >
              پیش‌نمایش
            </button>
            {!archived ? (
              <button
                type="button"
                className="rounded-xl border border-[#2563EB] px-4 py-2 text-sm font-semibold text-[#2563EB] disabled:opacity-50"
                disabled={saving || form.mode === "view"}
                data-testid="content-article-publish-header"
                onClick={openPublishDialog}
              >
                {isPublished(article.status)
                  ? "لغو انتشار"
                  : hasPriorPublication
                    ? "انتشار مجدد"
                    : "انتشار"}
              </button>
            ) : null}
            {form.mode === "view" ? (
              !archived ? (
                <button
                  type="button"
                  className="rounded-xl border px-4 py-2 text-sm"
                  data-testid="content-article-enter-edit"
                  onClick={() => form.onEdit()}
                >
                  ویرایش
                </button>
              ) : null
            ) : (
              <button type="button" className="rounded-xl border px-4 py-2 text-sm" onClick={handleCancel}>
                انصراف
              </button>
            )}
          </div>
        </div>

        {(article && canArchiveArticle(article.status)) || (article && canHardDeleteArticle(article.status)) ? (
          <div
            className="mt-3 flex flex-wrap gap-2 border-t border-dashed border-danger/30 pt-3"
            data-testid="content-article-destructive-actions"
          >
            {article && canArchiveArticle(article.status) ? (
              <button
                type="button"
                className="rounded-xl border border-danger/40 px-4 py-2 text-sm text-danger"
                disabled={saving}
                data-testid="content-article-archive"
                onClick={() => setDestructiveKind("archive")}
              >
                بایگانی
              </button>
            ) : null}
            {article && canHardDeleteArticle(article.status) ? (
              <button
                type="button"
                className="rounded-xl border border-danger/40 px-4 py-2 text-sm text-danger"
                disabled={saving}
                data-testid="content-article-delete"
                onClick={() => setDestructiveKind("delete")}
              >
                حذف
              </button>
            ) : null}
          </div>
        ) : null}
      </div>

      <div className="mb-4 flex flex-wrap gap-2 border-b pb-2" data-testid="content-article-tabs">
        {TABS.map((item) => (
          <button
            key={item.id}
            type="button"
            data-testid={`content-article-tab-${item.id}`}
            className={
              tab === item.id
                ? "rounded-lg bg-[#2563EB] px-3 py-1.5 text-sm font-semibold text-white"
                : "rounded-lg border px-3 py-1.5 text-sm"
            }
            onClick={() => setTab(item.id)}
          >
            {item.label}
            {item.id === "comments" && commentsPendingCount > 0 ? (
              <span className="ms-1 inline-flex min-w-[1.25rem] justify-center rounded-full bg-amber-100 px-1.5 text-[10px] font-bold text-amber-900">
                {commentsPendingCount}
              </span>
            ) : null}
          </button>
        ))}
      </div>

      <section className="rounded-2xl border bg-surface-elevated p-4">
        {tab === "full" ? (
          <div
            className="grid gap-4 lg:grid-cols-[minmax(0,1.6fr)_minmax(240px,20rem)]"
            data-testid="content-article-full-edit"
          >
            <div className="space-y-3">
              <label className="block text-sm">
                <span className="mb-1 block text-muted">عنوان</span>
                <input
                  className="w-full rounded-xl border px-3 py-2"
                  value={draftTitle}
                  disabled={form.mode === "view"}
                  onChange={(e) => {
                    setDraftTitle(e.target.value);
                    form.markDirty();
                  }}
                />
              </label>
              <label className="block text-sm">
                <span className="mb-1 block text-muted">چکیده</span>
                <textarea
                  className="w-full rounded-xl border px-3 py-2"
                  rows={2}
                  dir={editorDir}
                  value={draftExcerpt}
                  disabled={form.mode === "view"}
                  onChange={(e) => {
                    setDraftExcerpt(e.target.value);
                    form.markDirty();
                  }}
                />
              </label>
              <div>
                <span className="mb-1 flex items-center gap-2 text-sm text-muted">
                  بدنه
                  <ContentHelpAffordance helpKey="inlineImage" />
                </span>
                {form.mode === "view" ? (
                  <div
                    className="prose prose-neutral max-w-none rounded-xl border bg-slate-50 p-4 text-sm leading-8 min-h-[28rem]"
                    dir={editorDir}
                    data-testid="content-article-body-view-full"
                    dangerouslySetInnerHTML={{
                      __html: sanitizeArticleRichHtml(draftBody) || `<p>${draftExcerpt}</p>`,
                    }}
                  />
                ) : (
                  <ContentArticleEditor
                    value={draftBody}
                    onChange={(value) => {
                      setDraftBody(value);
                      form.markDirty();
                    }}
                    disabled={false}
                    dir={editorDir}
                    placeholder={editorDir === "rtl" ? "متن مقاله را بنویسید…" : "Write article body…"}
                    testId="content-article-rich-editor-full"
                    className="min-h-[28rem]"
                    onPickDamImage={pickDamImage}
                    onPickDamFile={pickDamFile}
                    onPickDamVideo={pickDamVideo}
                  />
                )}
              </div>
              {articleId ? (
                <div>
                  <div className="mb-2 flex items-center gap-2 text-sm text-muted">
                    برچسب‌ها
                    <ContentHelpAffordance helpKey="tags" />
                  </div>
                  <ContentArticleTagsPanel
                    key={`full-${articleId}-${draftLocale}-${tagsEpoch}`}
                    articleId={articleId}
                    languageCode={draftLocale}
                    canEdit={form.mode !== "view" && canEditContent}
                    onChanged={() => form.markDirty()}
                  />
                </div>
              ) : null}
            </div>

            <aside className="space-y-3 lg:sticky lg:top-4">
              <div className="rounded-xl border p-2.5">
                <div className="mb-1 flex items-center gap-2 text-xs text-muted">
                  نویسنده
                  <ContentHelpAffordance helpKey="author" />
                </div>
                {form.mode === "view" ? (
                  <p className="text-sm" data-testid="content-article-author-readonly-full">
                    {selectedAuthorLabel}
                  </p>
                ) : (
                  <AdminSearchableCombobox
                    value={draftAuthorId || null}
                    options={authorComboboxOptions}
                    noneOption={{ value: "", label: "— انتخاب نویسنده —" }}
                    placeholder="جستجو و انتخاب نویسنده…"
                    testId="content-article-author-combobox"
                    emptyLabel="نویسنده‌ای یافت نشد"
                    onChange={(next) => {
                      setDraftAuthorId(next ?? "");
                      form.markDirty();
                    }}
                  />
                )}
              </div>

              <div className="rounded-xl border p-2.5">
                <span className="mb-1 flex items-center gap-2 text-xs text-muted">
                  دسته
                  <ContentHelpAffordance helpKey="category" />
                </span>
                <ContentArticleCategoryPicker
                  rows={categoryOptions}
                  value={draftCategoryId}
                  disabled={form.mode === "view"}
                  onChange={(id, name) => {
                    setDraftCategoryId(id);
                    setDraftCategory(name);
                    form.markDirty();
                  }}
                />
              </div>

              <div className="rounded-xl border p-2.5" data-testid="content-article-home-feature-full">
                <label className="flex items-start gap-2 text-sm">
                  <input
                    type="checkbox"
                    className="mt-1"
                    checked={draftIsFeatured}
                    disabled={form.mode === "view"}
                    onChange={(e) => {
                      setDraftIsFeatured(e.target.checked);
                      form.markDirty();
                    }}
                  />
                  <span className="space-y-0.5">
                    <span className="flex items-center gap-2 text-sm font-medium">
                      نمایش در بخش مقالات صفحه اصلی
                      <ContentHelpAffordance helpKey="homeFeature" />
                    </span>
                    <span className="block text-xs text-muted">
                      اگر فعال باشد، این مقاله می‌تواند در بخش مقالات صفحه اصلی دیده شود.
                    </span>
                  </span>
                </label>
              </div>

              <div className="rounded-xl border p-2.5 space-y-1.5">
                <div className="flex flex-wrap items-center gap-2 text-sm">
                  <span>
                    وضعیت: <strong>{statusLabel(article.status)}</strong>
                  </span>
                  <ContentArticleReadinessSummary
                    readiness={readiness}
                    locale={draftLocale || article.locale}
                    onNavigate={(target) => setTab(target as TabId)}
                  />
                </div>
                {draftAuthorId &&
                dirty &&
                readiness?.checks.some(
                  (check) =>
                    !check.satisfied &&
                    (check.key === "content.publish.author" ||
                      check.labelKey === "content.publish.check.author" ||
                      check.actionTarget === "author"),
                ) ? (
                  <p className="text-xs text-muted" data-testid="content-article-author-pending-readiness">
                    نویسنده انتخاب شده — پس از ذخیره در آمادگی لحاظ می‌شود
                  </p>
                ) : null}
              </div>

              <div className="rounded-xl border p-2.5 space-y-1.5" data-testid="content-article-full-featured-summary">
                <div className="flex items-center justify-between gap-2">
                  <h3 className="text-xs font-semibold">تصویر شاخص</h3>
                  <button
                    type="button"
                    className="text-xs text-[#2563EB] underline"
                    onClick={() => setTab("media")}
                  >
                    فضای رسانه
                  </button>
                </div>
                {mediaWorkspace?.featuredMediaAssetId ? (
                  <img
                    src={mediaPreviewUrl(mediaWorkspace.featuredMediaAssetId) ?? ""}
                    alt=""
                    className="max-h-28 w-full rounded-lg border object-cover"
                  />
                ) : (
                  <p className="text-xs text-muted">تصویر شاخص اختصاص داده نشده است.</p>
                )}
              </div>
            </aside>
          </div>
        ) : null}

        {tab === "general" ? (
          <div className="space-y-3">
            <label className="block text-sm">
              <span className="mb-1 block text-muted">عنوان</span>
              <input
                className="w-full rounded-xl border px-3 py-2"
                value={draftTitle}
                disabled={form.mode === "view"}
                onChange={(e) => {
                  setDraftTitle(e.target.value);
                  form.markDirty();
                }}
              />
            </label>
            <label className="block text-sm">
              <span className="mb-1 block text-muted">نشانی صفحه (slug)</span>
              <input
                className="w-full rounded-xl border bg-slate-50 px-3 py-2 font-mono text-sm"
                dir="ltr"
                value={article.slug}
                readOnly
              />
            </label>
            <label className="block text-sm">
              <span className="mb-1 flex items-center gap-2 text-muted">
                زبان
                <ContentHelpAffordance helpKey="language" />
              </span>
              {form.mode === "view" || localeLocked ? (
                <input
                  className="w-full rounded-xl border bg-slate-50 px-3 py-2"
                  value={selectedLanguageLabel}
                  readOnly
                  data-testid="content-article-locale-readonly"
                />
              ) : (
                <select
                  className="w-full rounded-xl border px-3 py-2"
                  value={draftLocale}
                  data-testid="content-article-locale-select"
                  onChange={(e) => {
                    const next = e.target.value;
                    setDraftLocale(next);
                    setDraftCategoryId("");
                    setDraftCategory("");
                    setTagsEpoch((n) => n + 1);
                    form.markDirty();
                  }}
                >
                  {localeSelectOptions.map((option) => (
                    <option key={option.code} value={option.code}>
                      {languageOptionLabel(option)}
                      {!option.active ? " · غیرفعال" : ""}
                    </option>
                  ))}
                </select>
              )}
              {localeLocked ? (
                <p className="mt-1 text-xs text-muted" data-testid="content-article-locale-locked-message">
                  {LOCALE_LOCKED_MESSAGE}
                </p>
              ) : null}
            </label>
            <div className="rounded-xl border p-3" data-testid="content-article-home-feature">
              <label className="flex items-start gap-2 text-sm">
                <input
                  type="checkbox"
                  className="mt-1"
                  checked={draftIsFeatured}
                  disabled={form.mode === "view"}
                  onChange={(e) => {
                    setDraftIsFeatured(e.target.checked);
                    form.markDirty();
                  }}
                />
                <span className="space-y-1">
                  <span className="flex items-center gap-2 font-medium">
                    نمایش در بخش مقالات صفحه اصلی
                    <ContentHelpAffordance helpKey="homeFeature" />
                  </span>
                  <span className="block text-xs text-muted">
                    اگر فعال باشد، این مقاله می‌تواند در بخش مقالات صفحه اصلی دیده شود. ترتیب دقیق نمایش طبق قوانین فعلی فروشگاه است و با این گزینه عوض نمی‌شود.
                  </span>
                </span>
              </label>
            </div>
          </div>
        ) : null}

        {tab === "content" ? (
          <div className="space-y-4">
            <label className="block text-sm">
              <span className="mb-1 block text-muted">چکیده</span>
              <textarea
                className="w-full rounded-xl border px-3 py-2"
                rows={3}
                dir={editorDir}
                value={draftExcerpt}
                disabled={form.mode === "view"}
                onChange={(e) => {
                  setDraftExcerpt(e.target.value);
                  form.markDirty();
                }}
              />
            </label>
            <div>
              <span className="mb-1 flex items-center gap-2 text-sm text-muted">
                بدنه
                <ContentHelpAffordance helpKey="inlineImage" />
              </span>
              <p className="mb-2 text-xs text-muted">
                تصویر داخل متن از کتابخانهٔ رسانهٔ توبا درج می‌شود؛ فایل اصلی با حذف از مقاله پاک نمی‌شود.
              </p>
              {form.mode === "view" ? (
                <div
                  className="prose prose-neutral max-w-none rounded-xl border bg-slate-50 p-4 text-sm leading-8"
                  dir={editorDir}
                  data-testid="content-article-body-view"
                  dangerouslySetInnerHTML={{
                    __html: sanitizeArticleRichHtml(draftBody) || `<p>${draftExcerpt}</p>`,
                  }}
                />
              ) : (
                <ContentArticleEditor
                  value={draftBody}
                  onChange={(value) => {
                    setDraftBody(value);
                    form.markDirty();
                  }}
                  disabled={false}
                  dir={editorDir}
                  placeholder={editorDir === "rtl" ? "متن مقاله را بنویسید…" : "Write article body…"}
                  testId="content-article-rich-editor"
                  onPickDamImage={pickDamImage}
                  onPickDamFile={pickDamFile}
                  onPickDamVideo={pickDamVideo}
                />
              )}
            </div>
          </div>
        ) : null}

        {tab === "categories" ? (
          <div className="space-y-3">
            <label className="block text-sm">
              <span className="mb-1 flex items-center gap-2 text-muted">
                دسته (همان زبان مقاله)
                <ContentHelpAffordance helpKey="category" />
              </span>
              <ContentArticleCategoryPicker
                rows={categoryOptions}
                value={draftCategoryId}
                disabled={form.mode === "view"}
                onChange={(id, name) => {
                  setDraftCategoryId(id);
                  setDraftCategory(name);
                  form.markDirty();
                }}
              />
            </label>
            {!draftCategoryId ? (
              <p className="text-sm text-muted" data-testid="content-article-category-empty">
                هنوز دسته‌ای انتخاب نشده است.
              </p>
            ) : null}
          </div>
        ) : null}

        {tab === "author" ? (
          <div className="space-y-3">
            <div className="mb-1 flex items-center gap-2 text-sm text-muted">
              نویسنده
              <ContentHelpAffordance helpKey="author" />
            </div>
            {form.mode === "view" ? (
              <p className="text-sm" data-testid="content-article-author-readonly">
                {selectedAuthorLabel}
              </p>
            ) : (
              <AdminSearchableCombobox
                value={draftAuthorId || null}
                options={authorComboboxOptions}
                noneOption={{ value: "", label: "— انتخاب نویسنده —" }}
                placeholder="جستجو و انتخاب نویسنده…"
                testId="content-article-author-combobox"
                emptyLabel="نویسنده‌ای یافت نشد"
                onChange={(next) => {
                  setDraftAuthorId(next ?? "");
                  form.markDirty();
                }}
              />
            )}
            {!draftAuthorId ? (
              <p className="text-sm text-muted" data-testid="content-article-author-empty">
                هنوز نویسنده‌ای انتخاب نشده است.
              </p>
            ) : null}
            <p className="text-sm text-muted">نام نمایشی از پروفایل نویسنده هنگام ذخیره همگام می‌شود.</p>
          </div>
        ) : null}

        {tab === "media" && articleId ? (
          <ContentArticleMediaPanel
            articleId={articleId}
            editable={form.mode !== "view"}
            onWorkspaceChange={handleMediaWorkspaceChange}
          />
        ) : null}

        {tab === "seo" ? (
          <div className="space-y-4">
            <div className="flex items-center gap-2">
              <h3 className="text-sm font-semibold">جستجو و اشتراک‌گذاری</h3>
              <ContentHelpAffordance helpKey="seoSocial" />
            </div>
            <p className="text-xs text-muted">
              عنوان و توضیح کوتاه کمک می‌کند مطلب در نتایج جستجو و پیش‌نمایش لینک بهتر فهمیده شود. رفتار دقیق هر موتور یا شبکه تضمین نمی‌شود.
            </p>
            <div className="rounded-xl border p-3" data-testid="content-article-seo-image-panel">
              <h3 className="mb-2 flex items-center gap-2 text-sm font-semibold">
                تصویر اشتراک‌گذاری
                <ContentHelpAffordance helpKey="shareImage" />
              </h3>
              <label className="mb-3 flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={useFeaturedForSeo}
                  disabled={form.mode === "view"}
                  data-testid="content-article-seo-use-featured"
                  onChange={(e) => {
                    const checked = e.target.checked;
                    setUseFeaturedForSeo(checked);
                    if (checked) {
                      void assignArticleSeoImage(article.articleId, null).then((result) => {
                        if (result.state === "ok" && result.data) applySeoWorkspace(result.data);
                        else toast.error(mapArticleMediaMutationError(result));
                      });
                    }
                  }}
                />
                <span>استفاده از تصویر شاخص مقاله</span>
              </label>
              {!useFeaturedForSeo ? (
                <div className="space-y-2">
                  <p className="text-sm text-muted">انتخاب تصویر جداگانه برای اشتراک‌گذاری</p>
                  {mediaWorkspace?.seoImageMediaAssetId ? (
                    <img
                      src={mediaPreviewUrl(mediaWorkspace.seoImageMediaAssetId) ?? ""}
                      alt=""
                      className="max-h-40 rounded-xl border object-cover"
                    />
                  ) : (
                    <p className="text-sm text-muted">تصویر جداگانه انتخاب نشده است.</p>
                  )}
                  {form.mode !== "view" ? (
                    <div className="flex gap-2">
                      <button
                        type="button"
                        className="rounded-xl border px-3 py-2 text-sm"
                        data-testid="content-article-seo-pick"
                        onClick={() => setSeoImageOpen(true)}
                      >
                        انتخاب از کتابخانه
                      </button>
                      {mediaWorkspace?.seoImageMediaAssetId ? (
                        <button
                          type="button"
                          className="rounded-xl border px-3 py-2 text-sm"
                          onClick={() =>
                            void assignArticleSeoImage(article.articleId, null).then((result) => {
                              if (result.state === "ok" && result.data) applySeoWorkspace(result.data);
                              else toast.error(mapArticleMediaMutationError(result));
                            })
                          }
                        >
                          حذف اختصاص
                        </button>
                      ) : null}
                    </div>
                  ) : null}
                </div>
              ) : null}
              <div className="mt-3 rounded-lg bg-slate-50 p-3" data-testid="content-article-seo-effective-preview">
                <p className="mb-2 text-xs text-muted">پیش‌نمایش تصویر فعلی اشتراک‌گذاری</p>
                {effectiveSeoAssetId ? (
                  <img
                    src={mediaPreviewUrl(effectiveSeoAssetId) ?? ""}
                    alt=""
                    className="max-h-36 rounded-xl border object-cover"
                  />
                ) : (
                  <p className="text-sm text-muted">هنوز تصویری برای اشتراک‌گذاری تنظیم نشده است.</p>
                )}
              </div>
            </div>
            <label className="block text-sm">
              <span className="mb-1 block text-muted">عنوان نمایش در نتایج جستجو</span>
              <input
                className="w-full rounded-xl border px-3 py-2"
                value={draftSeoTitle}
                disabled={form.mode === "view"}
                onChange={(e) => {
                  setDraftSeoTitle(e.target.value);
                  form.markDirty();
                }}
              />
              <span className="mt-1 block text-xs text-muted">اگر خالی باشد معمولاً از عنوان مقاله استفاده می‌شود.</span>
            </label>
            <label className="block text-sm">
              <span className="mb-1 block text-muted">توضیح کوتاه برای نتایج جستجو</span>
              <textarea
                className="w-full rounded-xl border px-3 py-2"
                rows={3}
                value={draftSeoDescription}
                disabled={form.mode === "view"}
                onChange={(e) => {
                  setDraftSeoDescription(e.target.value);
                  form.markDirty();
                }}
              />
              <span className="mt-1 block text-xs text-muted">یک یا دو جمله دربارهٔ موضوع مطلب بنویسید.</span>
            </label>
            {articleId ? (
              <div>
                <div className="mb-2 flex items-center gap-2 text-sm text-muted">
                  برچسب‌ها
                  <ContentHelpAffordance helpKey="tags" />
                </div>
                <ContentArticleTagsPanel
                  key={`${articleId}-${draftLocale}-${tagsEpoch}`}
                  articleId={articleId}
                  languageCode={draftLocale}
                  canEdit={form.mode !== "view" && canEditContent}
                  onChanged={() => form.markDirty()}
                />
              </div>
            ) : null}
          </div>
        ) : null}

        {tab === "publication" ? (
          <div className="space-y-4">
            <div className="flex flex-wrap items-center gap-2">
              <p className="text-sm">
                وضعیت: <strong>{statusLabel(article.status)}</strong>
              </p>
              <ContentHelpAffordance helpKey="draftPublished" />
              <ContentHelpAffordance helpKey="publishSchedule" />
              <ContentHelpAffordance helpKey="unpublishRepublish" />
              <ContentHelpAffordance helpKey="readiness" />
              <ContentHelpAffordance helpKey="preview" />
            </div>
            <ContentArticlePublishDateField
              locale={draftLocale || article.locale}
              valueIso={draftPublishDate || article.publishDate}
              disabled={form.mode === "view"}
              onChangeIso={(iso) => {
                setDraftPublishDate(iso);
                form.markDirty();
              }}
            />
            <p className="text-sm text-muted">
              {publishDateIsFuture
                ? "زمان در آینده است: پس از انتشار، تا موعد در مسیر عمومی دیده نمی‌شود."
                : "زمان گذشته/اکنون: انتشار فوری در مسیر عمومی."}
            </p>
            <button
              type="button"
              className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
              disabled={saving || form.mode === "view" || archived}
              data-testid="content-article-publish-toggle"
              onClick={openPublishDialog}
            >
              {isPublished(article.status)
                ? "لغو انتشار"
                : hasPriorPublication
                  ? "انتشار مجدد"
                  : "انتشار"}
            </button>
          </div>
        ) : null}

        {tab === "comments" && articleId ? (
          <ContentArticleCommentsPanel
            articleId={articleId}
            canModerate={canEditContent}
            onPendingCountChange={setCommentsPendingCount}
          />
        ) : null}

        {tab === "history" ? (
          <div className="space-y-3">
            <div className="flex items-center gap-2">
              <h3 className="text-sm font-semibold">تاریخچه</h3>
              <ContentHelpAffordance helpKey="history" />
              {historyLoading ? <Spinner /> : null}
            </div>
            <ContentArticleHistoryTimeline
              entries={historyEntries}
              locale={draftLocale || article.locale}
            />
            <div
              className="flex flex-wrap items-center justify-between gap-2 border-t pt-3"
              data-testid="content-article-history-pager"
            >
              <button
                type="button"
                className="min-h-10 rounded-xl border px-3 text-sm disabled:opacity-50"
                disabled={historyLoading || historyPage <= 1}
                data-testid="content-article-history-prev"
                onClick={() => setHistoryPage((p) => Math.max(1, p - 1))}
              >
                {historyPagerFa ? "قبلی" : "Previous"}
              </button>
              <span className="text-sm text-muted" data-testid="content-article-history-page-label">
                {historyPagerFa
                  ? `صفحه ${historyPage} از ${historyTotalPages}`
                  : `Page ${historyPage} of ${historyTotalPages}`}
              </span>
              <button
                type="button"
                className="min-h-10 rounded-xl border px-3 text-sm disabled:opacity-50"
                disabled={historyLoading || historyPage >= historyTotalPages}
                data-testid="content-article-history-next"
                onClick={() => setHistoryPage((p) => p + 1)}
              >
                {historyPagerFa ? "بعدی" : "Next"}
              </button>
            </div>
          </div>
        ) : null}
      </section>

      <ContentArticleDestructiveDialog
        kind={destructiveKind}
        target={
          article
            ? { articleId: article.articleId, title: article.title, locale: article.locale }
            : null
        }
        open={destructiveKind !== null}
        pending={saving}
        readiness={readiness}
        scheduled={publishDateIsFuture}
        onNavigate={(target) => setTab(target as TabId)}
        onClose={() => {
          if (!saving) setDestructiveKind(null);
        }}
        onConfirm={() => {
          if (destructiveKind === "delete") return handleDelete();
          if (destructiveKind === "archive") return handleArchive();
          if (
            destructiveKind === "publish" ||
            destructiveKind === "unpublish" ||
            destructiveKind === "republish"
          ) {
            return togglePublish();
          }
        }}
      />
      <MediaLibraryDialog
        open={damPickKind !== null}
        title={
          damPickKind === "file"
            ? "انتخاب فایل از کتابخانه"
            : damPickKind === "video"
              ? "انتخاب ویدیو از کتابخانه"
              : "انتخاب تصویر از کتابخانه"
        }
        assetKind={damPickKind ?? "image"}
        selectionMode="single"
        onClose={() => {
          setDamPickKind(null);
          resolveDamPick(null);
        }}
        onConfirm={(assets) => {
          const kind = damPickKind;
          setDamPickKind(null);
          const picked = assets[0];
          if (!picked) {
            resolveDamPick(null);
            return;
          }
          if (kind === "file") {
            resolveDamPick({
              mediaAssetId: picked.mediaAssetId,
              fileName: picked.originalFileName,
              title: picked.originalFileName,
            });
            return;
          }
          if (kind === "video") {
            resolveDamPick({
              mediaAssetId: picked.mediaAssetId,
              fileName: picked.originalFileName,
              title: picked.originalFileName,
            });
            return;
          }
          resolveDamPick({
            mediaAssetId: picked.mediaAssetId,
            alt: picked.originalFileName,
          });
        }}
      />
      <MediaLibraryDialog
        open={seoImageOpen}
        assetKind="image"
        selectionMode="single"
        onClose={() => setSeoImageOpen(false)}
        onConfirm={async (assets) => {
          setSeoImageOpen(false);
          const picked = assets[0];
          if (!picked || !article) return;
          setUseFeaturedForSeo(false);
          const result = await assignArticleSeoImage(article.articleId, picked.mediaAssetId);
          if (result.state === "ok" && result.data) applySeoWorkspace(result.data);
          else toast.error(mapArticleMediaMutationError(result));
        }}
      />
    </main>
  );
}
