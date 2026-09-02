"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { toast } from "react-toastify";
import { formatJalaliDate, useAdminFormMode } from "../../design-system";
import { prepareAdminDevActor } from "./admin-api.ts";
import { fetchActiveContentAuthors, type ContentAuthorPickerItem } from "./content-author-api.ts";
import { fetchContentCategoryTree, type ContentCategoryTreeNodeDto } from "./content-category-api.ts";
import { MediaLibraryDialog } from "./media-library-dialog.tsx";
import { mediaPreviewUrl, type MediaAssetDto } from "./media-api.ts";
import { ProductRichTextEditor } from "./product-rich-text-editor.tsx";
import {
  articleEditorDirection,
  formatArticleDate,
  formatArticleLocaleLabel,
  isArticleLocaleLocked,
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
  return status === "Published" || status === "1";
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
  const articleId = typeof params.articleId === "string" ? params.articleId : null;
  const [article, setArticle] = useState<AdminContentArticle | null>(null);
  const [tab, setTab] = useState<TabId>("general");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [categoryOptions, setCategoryOptions] = useState<ContentCategoryTreeNodeDto[]>([]);
  const [authorOptions, setAuthorOptions] = useState<ContentAuthorPickerItem[]>([]);
  const [coverAsset, setCoverAsset] = useState<MediaAssetDto | null>(null);
  const [mediaOpen, setMediaOpen] = useState(false);

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
    setCoverAsset(
      data.coverMediaAssetId ? ({ mediaAssetId: data.coverMediaAssetId } as MediaAssetDto) : null,
    );
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

  const savePayload = useMemo(
    () => ({
      title: draftTitle,
      excerpt: draftExcerpt,
      body: draftBody,
      authorId: draftAuthorId || null,
      categoryId: draftCategoryId || null,
      category: draftCategory || null,
      coverMediaAssetId: coverAsset?.mediaAssetId ?? null,
      seoTitle: draftSeoTitle || null,
      seoDescription: draftSeoDescription || null,
      tags: tagsFromString(draftTags),
      isFeatured: draftIsFeatured,
      locale: draftLocale,
      publishDate: draftPublishDate ? new Date(draftPublishDate).toISOString() : null,
    }),
    [
      coverAsset?.mediaAssetId,
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
            <button type="button" className="rounded-xl border px-4 py-2 text-sm" onClick={() => form.onEdit()}>
              ویرایش
            </button>
          ) : (
            <>
              <button type="button" className="rounded-xl border px-4 py-2 text-sm" onClick={() => form.onCancel()}>
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
                onChange={(e) => setDraftTitle(e.target.value)}
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
              <ProductRichTextEditor
                value={draftBody}
                onChange={setDraftBody}
                disabled={form.mode === "view"}
                dir={editorDir}
                placeholder={editorDir === "rtl" ? "متن مقاله را بنویسید…" : "Write article body…"}
                testId="content-article-rich-editor"
              />
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

        {tab === "media" ? (
          <div className="space-y-3">
            <h3 className="text-sm font-semibold">تصویر شاخص (DAM)</h3>
            {coverAsset?.mediaAssetId ? (
              <img
                src={mediaPreviewUrl(coverAsset.mediaAssetId) ?? ""}
                alt=""
                className="max-h-56 rounded-xl border object-cover"
              />
            ) : (
              <p className="text-sm text-muted">تصویری اختصاص داده نشده است.</p>
            )}
            <div className="flex gap-2">
              <button
                type="button"
                className="rounded-xl border px-3 py-2 text-sm"
                disabled={form.mode === "view"}
                onClick={() => setMediaOpen(true)}
              >
                انتخاب از کتابخانه
              </button>
              {coverAsset ? (
                <button
                  type="button"
                  className="rounded-xl border px-3 py-2 text-sm"
                  disabled={form.mode === "view"}
                  onClick={() => setCoverAsset(null)}
                >
                  حذف اختصاص
                </button>
              ) : null}
            </div>
          </div>
        ) : null}

        {tab === "seo" ? (
          <div className="space-y-3">
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
              <strong>{isPublished(article.status) ? "منتشر" : "پیش‌نویس"}</strong>
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
              disabled={saving || form.mode === "view"}
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

      <MediaLibraryDialog
        open={mediaOpen}
        selectionMode="single"
        onClose={() => setMediaOpen(false)}
        onConfirm={(assets) => {
          setCoverAsset(assets[0] ?? null);
          setMediaOpen(false);
        }}
      />
    </main>
  );
}
