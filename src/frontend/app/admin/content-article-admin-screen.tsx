"use client";

import Link from "next/link";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { toast } from "react-toastify";
import { formatJalaliDate, useAdminFormMode } from "../../design-system";
import { prepareAdminDevActor } from "./admin-api.ts";
import { fetchActiveContentAuthors, type ContentAuthorPickerItem } from "./content-author-api.ts";
import { fetchContentCategoryTree, type ContentCategoryTreeNodeDto } from "./content-category-api.ts";
import { MediaLibraryDialog } from "./media-library-dialog.tsx";
import { mediaPreviewUrl } from "./media-api.ts";
import { ContentArticleDestructiveDialog, type ArticleDestructiveKind } from "./content-article-destructive-dialog.tsx";
import { ProductRichTextEditor } from "./product-rich-text-editor.tsx";
import { ContentArticleMediaPanel } from "./content-article-media-panel.tsx";
import {
  assignArticleSeoImage,
  fetchArticleMediaWorkspace,
  type ArticleMediaWorkspaceDto,
} from "./content-article-media-api.ts";
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

const LANGUAGE_OPTIONS = [
  { code: "fa-IR", label: "فارسی" },
  { code: "en-US", label: "English" },
] as const;

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
  const [inlineImageOpen, setInlineImageOpen] = useState(false);
  const [seoImageOpen, setSeoImageOpen] = useState(false);
  const [mediaWorkspace, setMediaWorkspace] = useState<ArticleMediaWorkspaceDto | null>(null);
  const [useFeaturedForSeo, setUseFeaturedForSeo] = useState(true);
  const [destructiveKind, setDestructiveKind] = useState<ArticleDestructiveKind | null>(null);

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

  const form = useAdminFormMode({ canView: true, canEdit: true });
  const editorDir = articleEditorDirection(draftLocale);
  const localeLocked = article ? isArticleLocaleLocked(article) : false;
  const archived = article ? isArticleArchived(article.status) : false;

  useEffect(() => {
    if (searchParams.get("mode") === "edit" && !archived) {
      form.onEdit();
    }
  }, [searchParams, archived, form]);

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

  const refreshArticle = useCallback(async (id: string) => {
    const result = await loadAdminArticle(id);
    if (result.state !== "ok" || !result.data) {
      setArticle(null);
      return;
    }
    applyArticle(result.data);
  }, [applyArticle]);

  useEffect(() => {
    if (!articleId) {
      setArticle(null);
      setLoading(false);
      return;
    }
    setLoading(true);
    void prepareAdminDevActor()
      .then(() => Promise.all([refreshArticle(articleId), fetchActiveContentAuthors()]))
      .then(([, authors]) => {
        if (authors.state === "ok" && authors.data) setAuthorOptions(authors.data);
      })
      .finally(() => setLoading(false));
  }, [articleId, refreshArticle]);

  useEffect(() => {
    if (!draftLocale) return;
    void fetchContentCategoryTree(draftLocale).then((result) => {
      if (result.state === "ok" && result.data) setCategoryOptions(result.data);
    });
  }, [draftLocale]);

  useEffect(() => {
    if (!articleId) return;
    void fetchArticleMediaWorkspace(articleId).then((result) => {
      if (result.state === "ok" && result.data) {
        setMediaWorkspace(result.data);
        setUseFeaturedForSeo(!result.data.seoImageMediaAssetId);
      }
    });
  }, [articleId, tab]);

  const savePayload = useMemo(
    () => ({
      title: draftTitle,
      excerpt: draftExcerpt,
      body: sanitizeArticleRichHtml(draftBody),
      authorId: draftAuthorId || null,
      categoryId: draftCategoryId || null,
      category: draftCategory || null,
      coverMediaAssetId: mediaWorkspace?.featuredMediaAssetId ?? article?.coverMediaAssetId ?? null,
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
    setSaving(true);
    const result = await updateAdminArticle(article.articleId, savePayload);
    setSaving(false);
    if (!result.ok || !result.article) {
      toast.error(result.message ?? "ذخیره ناموفق بود");
      return;
    }
    toast.success("ذخیره شد");
    form.onSaved();
    applyArticle(result.article);
  }, [applyArticle, article, form, savePayload]);

  const togglePublish = useCallback(async () => {
    if (!article) return;
    setSaving(true);
    const ok = isPublished(article.status)
      ? await unpublishAdminArticle(article.articleId)
      : await publishAdminArticle(article.articleId);
    setSaving(false);
    if (!ok) {
      toast.error("عملیات انتشار ناموفق بود");
      return;
    }
    toast.success(isPublished(article.status) ? "انتشار لغو شد" : "منتشر شد");
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
      toast.error(result.message ?? "حذف ناموفق بود");
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
      toast.error(result.message ?? "بایگانی ناموفق بود");
      return;
    }
    toast.success("مقاله بایگانی شد");
    setDestructiveKind(null);
    await refreshArticle(article.articleId);
    form.resetToView();
  }, [article, form, refreshArticle]);

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
        <p className="text-muted">در حال بارگذاری workspace…</p>
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

  return (
    <main className="w-full p-4" data-testid="content-article-admin">
      <div className="mb-4 flex flex-wrap items-start justify-between gap-3">
        <div>
          <Link href="/admin/content" className="text-sm text-[#2563EB] underline">
            بازگشت به فهرست مقالات
          </Link>
          <h1 className="mt-2 text-xl font-bold">{article.title || "بدون عنوان"}</h1>
          <p className="text-sm text-muted">
            {formatArticleLocaleLabel(article.locale)} ·{" "}
            <span dir="ltr" className="font-mono text-xs">
              {article.slug}
            </span>
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          {form.mode === "view" ? (
            !archived ? (
              <button type="button" className="rounded-xl border px-4 py-2 text-sm" onClick={() => form.onEdit()}>
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
        </div>
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
              <input className="w-full rounded-xl border bg-slate-50 px-3 py-2 font-mono text-sm" dir="ltr" value={article.slug} readOnly />
            </label>
            <label className="block text-sm">
              <span className="mb-1 block text-muted">زبان</span>
              <select
                className="w-full rounded-xl border px-3 py-2"
                value={draftLocale}
                disabled={form.mode === "view" || localeLocked}
                onChange={(e) => setDraftLocale(e.target.value)}
              >
                {LANGUAGE_OPTIONS.map((option) => (
                  <option key={option.code} value={option.code}>
                    {option.label}
                  </option>
                ))}
              </select>
              {localeLocked ? (
                <p className="mt-1 text-xs text-muted">پس از انتساب نویسنده/دسته یا انتشار، تغییر زبان مجاز نیست.</p>
              ) : null}
            </label>
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={draftIsFeatured}
                disabled={form.mode === "view"}
                onChange={(e) => setDraftIsFeatured(e.target.checked)}
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
                onChange={(e) => setDraftExcerpt(e.target.value)}
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
                <ProductRichTextEditor
                  value={draftBody}
                  onChange={(value) => {
                    setDraftBody(value);
                    form.markDirty();
                  }}
                  disabled={false}
                  dir={editorDir}
                  sanitizeHtml={sanitizeArticleRichHtml}
                  placeholder={editorDir === "rtl" ? "متن مقاله را بنویسید…" : "Write article body…"}
                  testId="content-article-rich-editor"
                  onPickDamImage={() =>
                    new Promise((resolve) => {
                      setInlineImageOpen(true);
                      (window as unknown as { __articleDamPickResolve?: typeof resolve }).__articleDamPickResolve = resolve;
                    })
                  }
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
                onChange={(e) => {
                  const selected = categoryOptions.find((row) => row.id === e.target.value);
                  setDraftCategoryId(e.target.value);
                  setDraftCategory(selected?.name ?? "");
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
            <label className="block text-sm">
              <span className="mb-1 block text-muted">نویسندهٔ فعال</span>
              <select
                className="w-full rounded-xl border px-3 py-2"
                value={draftAuthorId}
                disabled={form.mode === "view"}
                onChange={(e) => setDraftAuthorId(e.target.value)}
              >
                <option value="">— انتخاب نویسنده —</option>
                {authorOptions.map((row) => (
                  <option key={row.authorId} value={row.authorId}>
                    {row.displayName}
                  </option>
                ))}
              </select>
            </label>
            <p className="text-sm text-muted">نام نمایشی از پروفایل نویسنده هنگام ذخیره همگام می‌شود.</p>
          </div>
        ) : null}

        {tab === "media" && articleId ? (
          <ContentArticleMediaPanel articleId={articleId} editable={form.mode !== "view"} />
        ) : null}

        {tab === "seo" ? (
          <div className="space-y-4">
            <div className="rounded-xl border p-3">
              <h3 className="mb-2 text-sm font-semibold">تصویر SEO / OpenGraph</h3>
              <label className="mb-3 flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={useFeaturedForSeo}
                  disabled={form.mode === "view"}
                  onChange={(e) => {
                    setUseFeaturedForSeo(e.target.checked);
                    if (e.target.checked) {
                      void assignArticleSeoImage(article!.articleId, null).then((result) => {
                        if (result.state === "ok" && result.data) setMediaWorkspace(result.data);
                      });
                    }
                  }}
                />
                <span>استفاده از تصویر شاخص</span>
              </label>
              {!useFeaturedForSeo ? (
                <div className="space-y-2">
                  {mediaWorkspace?.seoImageMediaAssetId ? (
                    <img src={mediaPreviewUrl(mediaWorkspace.seoImageMediaAssetId) ?? ""} alt="" className="max-h-40 rounded-xl border object-cover" />
                  ) : (
                    <p className="text-sm text-muted">تصویر SEO اختصاصی انتخاب نشده است.</p>
                  )}
                  {form.mode !== "view" ? (
                    <div className="flex gap-2">
                      <button type="button" className="rounded-xl border px-3 py-2 text-sm" onClick={() => setSeoImageOpen(true)}>
                        انتخاب از DAM
                      </button>
                      {mediaWorkspace?.seoImageMediaAssetId ? (
                        <button
                          type="button"
                          className="rounded-xl border px-3 py-2 text-sm"
                          onClick={() => void assignArticleSeoImage(article!.articleId, null).then((result) => {
                            if (result.state === "ok" && result.data) {
                              setMediaWorkspace(result.data);
                              setUseFeaturedForSeo(true);
                            }
                          })}
                        >
                          حذف اختصاص
                        </button>
                      ) : null}
                    </div>
                  ) : null}
                </div>
              ) : (
                <p className="text-sm text-muted">
                  مؤثر: {mediaWorkspace?.effectiveSeoImageMediaAssetId ? "تصویر شاخص/SEO" : "—"}
                </p>
              )}
            </div>
            <label className="block text-sm">
              <span className="mb-1 block text-muted">عنوان SEO</span>
              <input
                className="w-full rounded-xl border px-3 py-2"
                value={draftSeoTitle}
                disabled={form.mode === "view"}
                onChange={(e) => setDraftSeoTitle(e.target.value)}
              />
            </label>
            <label className="block text-sm">
              <span className="mb-1 block text-muted">توضیح SEO</span>
              <textarea
                className="w-full rounded-xl border px-3 py-2"
                rows={3}
                value={draftSeoDescription}
                disabled={form.mode === "view"}
                onChange={(e) => setDraftSeoDescription(e.target.value)}
              />
            </label>
            <label className="block text-sm">
              <span className="mb-1 block text-muted">برچسب‌ها (با ویرگول)</span>
              <input
                className="w-full rounded-xl border px-3 py-2"
                value={draftTags}
                disabled={form.mode === "view"}
                onChange={(e) => setDraftTags(e.target.value)}
              />
            </label>
          </div>
        ) : null}

        {tab === "publication" ? (
          <div className="space-y-4">
            <p className="text-sm">
              وضعیت:{" "}
              <strong>
                {isArticleArchived(article.status)
                  ? "بایگانی"
                  : isPublished(article.status)
                    ? "منتشر"
                    : "پیش‌نویس"}
              </strong>
            </p>
            <label className="block text-sm">
              <span className="mb-1 block text-muted">زمان انتشار (برنامه‌ریزی)</span>
              <input
                type="datetime-local"
                className="w-full rounded-xl border px-3 py-2"
                dir="ltr"
                value={draftPublishDate}
                disabled={form.mode === "view"}
                onChange={(e) => setDraftPublishDate(e.target.value)}
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
              onClick={() => void togglePublish()}
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
        }}
      />
      <MediaLibraryDialog
        open={inlineImageOpen}
        selectionMode="single"
        onClose={() => {
          setInlineImageOpen(false);
          const resolve = (window as unknown as { __articleDamPickResolve?: (value: null) => void }).__articleDamPickResolve;
          resolve?.(null);
          delete (window as unknown as { __articleDamPickResolve?: unknown }).__articleDamPickResolve;
        }}
        onConfirm={(assets) => {
          setInlineImageOpen(false);
          const picked = assets[0];
          const resolve = (window as unknown as {
            __articleDamPickResolve?: (value: { mediaAssetId: string; alt?: string; title?: string } | null) => void;
          }).__articleDamPickResolve;
          resolve?.(
            picked
              ? {
                  mediaAssetId: picked.mediaAssetId,
                  alt: picked.originalFileName,
                }
              : null,
          );
          delete (window as unknown as { __articleDamPickResolve?: unknown }).__articleDamPickResolve;
        }}
      />
      <MediaLibraryDialog
        open={seoImageOpen}
        selectionMode="single"
        onClose={() => setSeoImageOpen(false)}
        onConfirm={(assets) => {
          setSeoImageOpen(false);
          const picked = assets[0];
          if (!picked || !article) return;
          setUseFeaturedForSeo(false);
          void assignArticleSeoImage(article.articleId, picked.mediaAssetId).then((result) => {
            if (result.state === "ok" && result.data) setMediaWorkspace(result.data);
          });
        }}
      />
    </main>
  );
}
