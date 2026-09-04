"use client";

import Link from "next/link";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { toast } from "react-toastify";
import { formatJalaliDate, useAdminFormMode } from "../../design-system";
import { prepareAdminDevActor } from "./admin-api.ts";
import {
  capabilityPermissionIds,
  createAdminAccessApi,
  hasCapability,
} from "../access-control/access-control-api.ts";
import { fetchActiveContentAuthors, type ContentAuthorPickerItem } from "./content-author-api.ts";
import { fetchContentCategoryTree, type ContentCategoryTreeNodeDto } from "./content-category-api.ts";
import { MediaLibraryDialog } from "./media-library-dialog.tsx";
import { mediaPreviewUrl } from "./media-api.ts";
import { sanitizeArticleRichHtml } from "./article-rich-html.ts";
import { ContentArticleDestructiveDialog, type ArticleDestructiveKind } from "./content-article-destructive-dialog.tsx";
import { ContentArticleRichTextEditor } from "./content-article-rich-text-editor.tsx";
import { ContentArticleMediaPanel } from "./content-article-media-panel.tsx";
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
  publishAdminArticle,
  unpublishAdminArticle,
  updateAdminArticle,
  type AdminContentArticle,
} from "../content/content-api.ts";

const TABS = [
  { id: "general", label: "عمومی" },
  { id: "content", label: "محتوا" },
  { id: "categories", label: "دسته‌بندی‌ها" },
  { id: "author", label: "نویسنده" },
  { id: "media", label: "رسانه" },
  { id: "seo", label: "SEO" },
  { id: "publication", label: "انتشار" },
  { id: "history", label: "تاریخچه" },
] as const;

type TabId = (typeof TABS)[number]["id"];

type DamPickResult = { mediaAssetId: string; alt?: string; title?: string } | null;

function languageOptionLabel(lang: SupportedLocaleDefinition): string {
  return lang.nativeName?.trim() || lang.displayName?.trim() || lang.code;
}

const LOCALE_LOCKED_MESSAGE =
  "زبان این مقاله به‌دلیل وجود محتوا یا وابستگی‌های ثبت‌شده قابل تغییر نیست.";

function isPublished(status: string): boolean {
  return isArticlePublished(status);
}

function tagsToString(tags: string[]): string {
  return tags.join(", ");
}

function tagsFromString(value: string): string[] {
  return value
    .split(",")
    .map((tag) => tag.trim())
    .filter(Boolean);
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
  const [tab, setTab] = useState<TabId>("general");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [categoryOptions, setCategoryOptions] = useState<ContentCategoryTreeNodeDto[]>([]);
  const [authorOptions, setAuthorOptions] = useState<ContentAuthorPickerItem[]>([]);
  const [authorFilter, setAuthorFilter] = useState("");
  const [languageOptions, setLanguageOptions] = useState<SupportedLocaleDefinition[]>([]);
  const [inlineImageOpen, setInlineImageOpen] = useState(false);
  const [seoImageOpen, setSeoImageOpen] = useState(false);
  const [mediaWorkspace, setMediaWorkspace] = useState<ArticleMediaWorkspaceDto | null>(null);
  const [useFeaturedForSeo, setUseFeaturedForSeo] = useState(true);
  const [destructiveKind, setDestructiveKind] = useState<ArticleDestructiveKind | null>(null);
  const damPickResolveRef = useRef<((value: DamPickResult) => void) | null>(null);

  const [draftTitle, setDraftTitle] = useState("");
  const [draftExcerpt, setDraftExcerpt] = useState("");
  const [draftBody, setDraftBody] = useState("");
  const [draftLocale, setDraftLocale] = useState("fa-IR");
  const [draftIsFeatured, setDraftIsFeatured] = useState(false);
  const [draftCategoryId, setDraftCategoryId] = useState("");
  const [draftCategory, setDraftCategory] = useState("");
  const [draftAuthorId, setDraftAuthorId] = useState("");
  const [draftSeoTitle, setDraftSeoTitle] = useState("");
  const [draftSeoDescription, setDraftSeoDescription] = useState("");
  const [draftTags, setDraftTags] = useState("");
  const [draftPublishDate, setDraftPublishDate] = useState("");

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
  const filteredAuthors = useMemo(() => {
    const q = authorFilter.trim().toLowerCase();
    if (!q) return authorOptions;
    return authorOptions.filter(
      (row) =>
        row.displayName.toLowerCase().includes(q) ||
        row.slug.toLowerCase().includes(q),
    );
  }, [authorFilter, authorOptions]);
  const selectedAuthorLabel =
    authorOptions.find((row) => row.authorId === draftAuthorId)?.displayName ||
    article?.authorDisplayName ||
    "—";
  const selectedLanguageLabel = (() => {
    const row = languageOptions.find((item) => item.code === draftLocale);
    return row ? languageOptionLabel(row) : formatArticleLocaleLabel(draftLocale);
  })();

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
    setDraftLocale(data.locale);
    setDraftIsFeatured(data.isFeatured);
    setDraftCategoryId(data.categoryId ?? "");
    setDraftCategory(data.category ?? "");
    setDraftAuthorId(data.authorId ?? "");
    setDraftSeoTitle(data.seoTitle ?? "");
    setDraftSeoDescription(data.seoDescription ?? "");
    setDraftTags(tagsToString(data.tags));
    setDraftPublishDate(data.publishDate ? data.publishDate.slice(0, 16) : "");
  }, []);

  const refreshArticle = useCallback(
    async (id: string) => {
      const result = await loadAdminArticle(id);
      if (result.state !== "ok" || !result.data) {
        setArticle(null);
        return;
      }
      applyArticle(result.data);
    },
    [applyArticle],
  );

  const refreshMediaWorkspace = useCallback(async (id: string) => {
    const result = await fetchArticleMediaWorkspace(id);
    if (result.state === "ok" && result.data) {
      setMediaWorkspace(result.data);
      setUseFeaturedForSeo(!result.data.seoImageMediaAssetId);
    }
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

  const resolveDamPick = useCallback((value: DamPickResult) => {
    const resolve = damPickResolveRef.current;
    damPickResolveRef.current = null;
    resolve?.(value);
  }, []);

  const pickDamImage = useCallback(() => {
    return new Promise<DamPickResult>((resolve) => {
      damPickResolveRef.current = resolve;
      setInlineImageOpen(true);
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
      tags: tagsFromString(draftTags),
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
      draftTags,
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
  }, [
    applyArticle,
    article,
    categoryOptions,
    draftCategoryId,
    draftLocale,
    form,
    languageOptions,
    refreshMediaWorkspace,
    savePayload,
  ]);

  const togglePublish = useCallback(async () => {
    if (!article) return;
    setSaving(true);
    const publishing = !isPublished(article.status);
    const ok = publishing
      ? await publishAdminArticle(article.articleId)
      : await unpublishAdminArticle(article.articleId);
    setSaving(false);
    if (!ok) {
      toast.error("عملیات انتشار ناموفق بود");
      return;
    }
    toast.success(publishing ? "منتشر شد" : "انتشار لغو شد");
    setDestructiveKind(null);
    await refreshArticle(article.articleId);
  }, [article, refreshArticle]);

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
            <Link href="/admin/content" className="text-sm text-[#2563EB] underline">
              بازگشت به فهرست مقالات
            </Link>
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
            </div>
          </div>

          <div className="flex flex-wrap items-center gap-2">
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
              <>
                <button type="button" className="rounded-xl border px-4 py-2 text-sm" onClick={handleCancel}>
                  انصراف
                </button>
                <button
                  type="button"
                  className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-semibold text-white"
                  disabled={saving}
                  data-testid="content-article-save"
                  onClick={() => void save()}
                >
                  ذخیره
                </button>
              </>
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

      <div className="mb-4 flex flex-wrap gap-2 border-b pb-2">
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
          </button>
        ))}
      </div>

      <section className="rounded-2xl border bg-surface-elevated p-4">
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
              <span className="mb-1 block text-muted">زبان</span>
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
                    form.markDirty();
                  }}
                >
                  {languageOptions.map((option) => (
                    <option key={option.code} value={option.code}>
                      {languageOptionLabel(option)}
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
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={draftIsFeatured}
                disabled={form.mode === "view"}
                onChange={(e) => {
                  setDraftIsFeatured(e.target.checked);
                  form.markDirty();
                }}
              />
              <span>ویژه در ریل خانه</span>
            </label>
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
              <span className="mb-1 block text-sm text-muted">بدنه</span>
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
                <ContentArticleRichTextEditor
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
                />
              )}
            </div>
          </div>
        ) : null}

        {tab === "categories" ? (
          <div className="space-y-3">
            <label className="block text-sm">
              <span className="mb-1 block text-muted">دسته (همان زبان مقاله)</span>
              <select
                className="w-full rounded-xl border px-3 py-2"
                value={draftCategoryId}
                disabled={form.mode === "view"}
                data-testid="content-article-category-select"
                onChange={(e) => {
                  const selected = categoryOptions.find((row) => row.id === e.target.value);
                  setDraftCategoryId(e.target.value);
                  setDraftCategory(selected?.name ?? "");
                  form.markDirty();
                }}
              >
                <option value="">— بدون دسته —</option>
                {categoryOptions.map((row) => (
                  <option key={row.id} value={row.id}>
                    {row.name}
                  </option>
                ))}
              </select>
            </label>
          </div>
        ) : null}

        {tab === "author" ? (
          <div className="space-y-3">
            {form.mode === "view" ? (
              <p className="text-sm" data-testid="content-article-author-readonly">
                <span className="mb-1 block text-muted">نویسنده</span>
                {selectedAuthorLabel}
              </p>
            ) : (
              <>
                <label className="block text-sm">
                  <span className="mb-1 block text-muted">جستجوی نویسنده</span>
                  <input
                    className="w-full rounded-xl border px-3 py-2"
                    value={authorFilter}
                    placeholder="نام نویسنده…"
                    data-testid="content-article-author-filter"
                    onChange={(e) => setAuthorFilter(e.target.value)}
                  />
                </label>
                <label className="block text-sm">
                  <span className="mb-1 block text-muted">نویسندهٔ فعال</span>
                  <select
                    className="w-full rounded-xl border px-3 py-2"
                    value={draftAuthorId}
                    data-testid="content-article-author-select"
                    onChange={(e) => {
                      setDraftAuthorId(e.target.value);
                      form.markDirty();
                    }}
                  >
                    <option value="">— انتخاب نویسنده —</option>
                    {filteredAuthors.map((row) => (
                      <option key={row.authorId} value={row.authorId}>
                        {row.displayName}
                      </option>
                    ))}
                  </select>
                </label>
              </>
            )}
            <p className="text-sm text-muted">نام نمایشی از پروفایل نویسنده هنگام ذخیره همگام می‌شود.</p>
          </div>
        ) : null}

        {tab === "media" && articleId ? (
          <ContentArticleMediaPanel
            articleId={articleId}
            editable={form.mode !== "view"}
            onWorkspaceChange={(workspace) => {
              setMediaWorkspace(workspace);
              setUseFeaturedForSeo(!workspace.seoImageMediaAssetId);
            }}
          />
        ) : null}

        {tab === "seo" ? (
          <div className="space-y-4">
            <div className="rounded-xl border p-3" data-testid="content-article-seo-image-panel">
              <h3 className="mb-2 text-sm font-semibold">تصویر اشتراک‌گذاری و شبکه‌های اجتماعی</h3>
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
              <span className="mb-1 block text-muted">عنوان SEO</span>
              <input
                className="w-full rounded-xl border px-3 py-2"
                value={draftSeoTitle}
                disabled={form.mode === "view"}
                onChange={(e) => {
                  setDraftSeoTitle(e.target.value);
                  form.markDirty();
                }}
              />
            </label>
            <label className="block text-sm">
              <span className="mb-1 block text-muted">توضیح SEO</span>
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
            </label>
            <label className="block text-sm">
              <span className="mb-1 block text-muted">برچسب‌ها (با ویرگول)</span>
              <input
                className="w-full rounded-xl border px-3 py-2"
                value={draftTags}
                disabled={form.mode === "view"}
                onChange={(e) => {
                  setDraftTags(e.target.value);
                  form.markDirty();
                }}
              />
            </label>
          </div>
        ) : null}

        {tab === "publication" ? (
          <div className="space-y-4">
            <p className="text-sm">
              وضعیت: <strong>{statusLabel(article.status)}</strong>
            </p>
            <label className="block text-sm">
              <span className="mb-1 block text-muted">زمان انتشار (برنامه‌ریزی)</span>
              <input
                type="datetime-local"
                className="w-full rounded-xl border px-3 py-2"
                dir="ltr"
                value={draftPublishDate}
                disabled={form.mode === "view"}
                onChange={(e) => {
                  setDraftPublishDate(e.target.value);
                  form.markDirty();
                }}
              />
            </label>
            <p className="text-sm text-muted">
              نمایش: {formatArticleDate(article.publishDate, article.locale)}
            </p>
            <button
              type="button"
              className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
              disabled={saving || form.mode === "view" || archived}
              data-testid="content-article-publish-toggle"
              onClick={() => setDestructiveKind(isPublished(article.status) ? "unpublish" : "publish")}
            >
              {isPublished(article.status) ? "لغو انتشار" : "انتشار"}
            </button>
          </div>
        ) : null}

        {tab === "history" ? (
          <div className="grid gap-3 sm:grid-cols-2">
            <div className="rounded-xl border p-3 text-sm">
              <div className="text-muted">ایجاد</div>
              <div>{formatArticleDate(article.createdAt, article.locale)}</div>
            </div>
            <div className="rounded-xl border p-3 text-sm">
              <div className="text-muted">آخرین به‌روزرسانی</div>
              <div>{formatArticleDate(article.updatedAt, article.locale)}</div>
            </div>
            <div className="rounded-xl border p-3 text-sm">
              <div className="text-muted">زمان انتشار</div>
              <div>{formatArticleDate(article.publishDate, article.locale)}</div>
            </div>
            <div className="rounded-xl border p-3 text-sm">
              <div className="text-muted">تقویم Admin</div>
              <div>{formatJalaliDate(article.updatedAt, "fa")}</div>
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
        onClose={() => {
          if (!saving) setDestructiveKind(null);
        }}
        onConfirm={() => {
          if (destructiveKind === "delete") return handleDelete();
          if (destructiveKind === "archive") return handleArchive();
          if (destructiveKind === "publish" || destructiveKind === "unpublish") return togglePublish();
        }}
      />
      <MediaLibraryDialog
        open={inlineImageOpen}
        selectionMode="single"
        onClose={() => {
          setInlineImageOpen(false);
          resolveDamPick(null);
        }}
        onConfirm={(assets) => {
          setInlineImageOpen(false);
          const picked = assets[0];
          resolveDamPick(
            picked
              ? {
                  mediaAssetId: picked.mediaAssetId,
                  alt: picked.originalFileName,
                }
              : null,
          );
        }}
      />
      <MediaLibraryDialog
        open={seoImageOpen}
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
