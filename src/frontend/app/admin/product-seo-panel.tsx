"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { slugifyCategoryName } from "./catalog-category-api.ts";
import { getAdminProductSeo, updateAdminProductSeo } from "./host-client.ts";
import {
  draftFromSeoDetail,
  formatSeoReadinessLabel,
  isSeoDraftDirty,
  resolveSeoPreviewTitle,
  SEO_LOCALE_DISPLAY,
  SEO_LOCALES,
  type ProductSeoDetail,
  type ProductSeoDraft,
} from "./product-seo-panel-model.ts";
import { useProductWorkspaceDirtyRegistration } from "./product-workspace-dirty-context";
import { toast } from "react-toastify";

export type ProductSeoPanelMode = "view" | "edit";

/**
 * پنل SEO محصول Workspace — VIEW/EDIT، ایزولهٔ locale، پیش‌نمایش SERP فشرده.
 */
export function ProductSeoPanel({
  productId,
  canEdit,
  mode,
}: {
  productId: string;
  canEdit: boolean;
  mode: ProductSeoPanelMode;
}) {
  const [locale, setLocale] = useState<string>("fa-IR");
  const [detail, setDetail] = useState<ProductSeoDetail | null>(null);
  const [draft, setDraft] = useState<ProductSeoDraft>({
    slug: "",
    seoTitle: "",
    seoDescription: "",
    slugTouched: true,
  });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const editable = canEdit && mode === "edit";
  const dirty = isSeoDraftDirty(detail, draft);

  const cancelEdit = useCallback(() => {
    if (detail) setDraft(draftFromSeoDetail(detail));
    setError(null);
  }, [detail]);

  useProductWorkspaceDirtyRegistration("seo", dirty && editable, cancelEdit);

  const previewTitle = useMemo(() => resolveSeoPreviewTitle(detail, draft), [detail, draft]);
  const previewPath = useMemo(() => {
    const slug = draft.slug.trim() || detail?.slug || "";
    const prefix = locale.startsWith("en") ? "en" : "fa";
    return slug ? `/${prefix}/products/${slug}` : detail?.publicPath || `/${prefix}/products/`;
  }, [detail?.publicPath, detail?.slug, draft.slug, locale]);

  const reload = useCallback(async (nextLocale: string) => {
    setLoading(true);
    setError(null);
    const result = await getAdminProductSeo(productId, nextLocale);
    setLoading(false);
    if (!result.ok) {
      setError(result.message);
      setDetail(null);
      return;
    }
    setDetail(result.detail);
    setDraft(draftFromSeoDetail(result.detail));
  }, [productId]);

  useEffect(() => {
    void reload(locale);
  }, [locale, reload]);

  function onLocaleChange(next: string) {
    if (dirty && editable) {
      const discard = window.confirm("تغییرات ذخیره‌نشده از بین می‌رود. ادامه؟");
      if (!discard) return;
    }
    setLocale(next);
  }

  function onSlugChange(value: string) {
    setDraft((prev) => ({ ...prev, slug: value, slugTouched: true }));
  }

  function onTitleChange(value: string) {
    setDraft((prev) => {
      const next = { ...prev, seoTitle: value };
      if (!prev.slugTouched && value.trim()) {
        next.slug = slugifyCategoryName(value);
      }
      return next;
    });
  }

  async function save() {
    if (!detail || !editable || busy) return;
    setBusy(true);
    setError(null);
    const result = await updateAdminProductSeo(productId, {
      locale,
      slug: draft.slug.trim() || null,
      seoTitle: draft.seoTitle.trim() || null,
      seoDescription: draft.seoDescription.trim() || null,
      expectedUpdatedAt: detail.updatedAt,
    });
    setBusy(false);
    if (!result.ok) {
      setError(result.message);
      return;
    }
    setDetail(result.detail);
    setDraft(draftFromSeoDetail(result.detail));
    toast.success(
      locale.startsWith("en") ? "Product changes saved." : "تغییرات محصول ذخیره شد.",
    );
  }

  if (loading) {
    return <p className="text-sm text-muted">در حال بارگذاری سئو…</p>;
  }

  return (
    <div className="space-y-4" data-testid="product-seo-panel">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap gap-2" data-testid="product-seo-locale-switcher">
          {SEO_LOCALES.map((item) => (
            <button
              key={item}
              type="button"
              className={
                locale === item
                  ? "min-h-10 rounded-ds bg-primary px-3 text-sm text-primary-foreground"
                  : "min-h-10 rounded-ds border border-border px-3 text-sm hover:bg-secondary"
              }
              data-testid={`seo-locale-${item}`}
              onClick={() => onLocaleChange(item)}
            >
              {SEO_LOCALE_DISPLAY[item] ?? item}
            </button>
          ))}
        </div>
        <p
          className={`text-sm font-medium ${detail?.readiness.isReady ? "text-emerald-700" : "text-amber-700"}`}
          data-testid="product-seo-readiness"
        >
          {formatSeoReadinessLabel(detail?.readiness ?? null)}
        </p>
      </div>

      {error ? (
        <p className="rounded-ds border border-danger/40 bg-danger/5 px-3 py-2 text-sm text-danger" data-testid="product-seo-error">
          {error}
        </p>
      ) : null}

      {!editable ? (
        <div className="grid gap-4 lg:grid-cols-2" data-testid="product-seo-view">
          <div className="space-y-3 rounded-ds border border-border bg-surface p-4">
            <FieldView label="آدرس محصول" value={detail?.slug || "—"} ltr />
            <FieldView label="عنوان برای موتورهای جستجو" value={detail?.seoTitle || detail?.titleFallback || "—"} />
            <FieldView label="توضیح نتیجه جستجو" value={detail?.seoDescription || "—"} />
            <FieldView label="مسیر عمومی" value={detail?.publicPath || "—"} ltr />
          </div>
          <SerpPreview title={previewTitle} path={previewPath} description={detail?.seoDescription || ""} />
        </div>
      ) : (
        <div className="grid gap-4 lg:grid-cols-2" data-testid="product-seo-edit">
          <div className="space-y-3 rounded-ds border border-border bg-surface p-4">
            <label className="block space-y-1">
              <span className="text-sm text-muted">آدرس محصول</span>
              <input
                className="min-h-11 w-full rounded-ds border border-border bg-background px-3 text-sm"
                dir="ltr"
                value={draft.slug}
                data-testid="product-seo-slug"
                onChange={(e) => onSlugChange(e.target.value)}
              />
            </label>
            <label className="block space-y-1">
              <span className="text-sm text-muted">عنوان برای موتورهای جستجو</span>
              <input
                className="min-h-11 w-full rounded-ds border border-border bg-background px-3 text-sm"
                value={draft.seoTitle}
                data-testid="product-seo-title"
                onChange={(e) => onTitleChange(e.target.value)}
              />
            </label>
            <label className="block space-y-1">
              <span className="text-sm text-muted">توضیح نتیجه جستجو</span>
              <textarea
                className="min-h-28 w-full rounded-ds border border-border bg-background px-3 py-2 text-sm"
                value={draft.seoDescription}
                data-testid="product-seo-description"
                onChange={(e) => setDraft((prev) => ({ ...prev, seoDescription: e.target.value }))}
              />
            </label>
            <div className="flex flex-wrap gap-2 pt-2">
              <button
                type="button"
                className="min-h-11 rounded-ds bg-primary px-4 text-sm text-primary-foreground disabled:opacity-50"
                disabled={!dirty || busy}
                data-testid="product-seo-save"
                onClick={() => void save()}
              >
                ذخیره
              </button>
              <button
                type="button"
                className="min-h-11 rounded-ds border border-border px-4 text-sm disabled:opacity-50"
                disabled={!dirty || busy}
                data-testid="product-seo-cancel"
                onClick={cancelEdit}
              >
                انصراف
              </button>
            </div>
          </div>
          <SerpPreview title={previewTitle} path={previewPath} description={draft.seoDescription} />
        </div>
      )}
    </div>
  );
}

function FieldView({ label, value, ltr }: { label: string; value: string; ltr?: boolean }) {
  return (
    <div>
      <p className="text-sm text-muted">{label}</p>
      <p className="mt-1 text-base font-medium" dir={ltr ? "ltr" : undefined}>
        {value}
      </p>
    </div>
  );
}

function SerpPreview({ title, path, description }: { title: string; path: string; description: string }) {
  return (
    <div className="rounded-ds border border-border bg-surface p-4" data-testid="product-seo-serp-preview">
      <p className="text-sm text-muted">پیش‌نمایش نتیجه جستجو</p>
      <p className="mt-3 text-lg font-medium text-primary">{title || "—"}</p>
      <p className="mt-1 text-sm text-muted" dir="ltr">
        {path || "—"}
      </p>
      <p className="mt-2 text-sm text-muted line-clamp-3">{description.trim() || "—"}</p>
    </div>
  );
}
