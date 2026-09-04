"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { BookOpen, Calendar, User } from "lucide-react";
import { prepareAdminDevActor } from "../../../../admin-api.ts";
import { mapAdminErrorMessage } from "../../../../admin-error-map.ts";
import type { ArticlePreviewSnapshot } from "../../../../content-article-publication-model.ts";
import { ArticleBodyHtml } from "../../../../../content/article-body-html.tsx";
import {
  contentCoverUrl,
  formatArticleDate,
  loadArticleAdminPreview,
} from "../../../../../content/content-api.ts";
import { storefrontMediaUrl } from "../../../../../storefront/storefront-api.ts";

/**
 * پیش‌نمایش Admin مقاله — permission-aware، noindex، بدون مسیر عمومی قابل حدس.
 * Unsaved behavior: والد قبل از باز کردن این صفحه Save الزامی می‌کند.
 */
export default function ContentArticlePreviewPage() {
  const params = useParams<{ articleId?: string }>();
  const articleId = typeof params.articleId === "string" ? params.articleId : null;
  const [preview, setPreview] = useState<ArticlePreviewSnapshot | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!articleId) {
      setLoading(false);
      setError("content.preview.unavailable");
      return;
    }
    setLoading(true);
    void prepareAdminDevActor()
      .then(() => loadArticleAdminPreview(articleId))
      .then((result) => {
        if (result.state === "ok" && result.data) {
          setPreview(result.data);
          setError(null);
        } else {
          setPreview(null);
          setError(result.message ?? "content.preview.unavailable");
        }
      })
      .finally(() => setLoading(false));
  }, [articleId]);

  useEffect(() => {
    if (typeof document === "undefined") return;
    let robots = document.querySelector('meta[name="robots"]');
    if (!robots) {
      robots = document.createElement("meta");
      robots.setAttribute("name", "robots");
      document.head.appendChild(robots);
    }
    robots.setAttribute("content", "noindex,nofollow,noarchive");
  }, []);

  if (loading) {
    return <main className="p-6 text-sm text-muted">در حال بارگذاری پیش‌نمایش…</main>;
  }
  if (!preview) {
    return (
      <main className="p-6" data-testid="content-article-preview-denied">
        <p>{mapAdminErrorMessage(error, "fa")}</p>
      </main>
    );
  }

  const cover = preview.coverMediaAssetId
    ? storefrontMediaUrl(preview.coverMediaAssetId) || contentCoverUrl(preview.coverMediaAssetId)
    : contentCoverUrl(null);
  const fa = preview.locale.toLowerCase().startsWith("fa");

  return (
    <main
      className="mx-auto max-w-3xl space-y-4 px-3 py-6 md:px-4"
      data-testid="content-article-preview"
      dir={fa ? "rtl" : "ltr"}
    >
      <div
        className="rounded-xl border border-amber-300 bg-amber-50 px-3 py-2 text-sm text-amber-950"
        data-testid="content-article-preview-banner"
      >
        {fa
          ? "پیش‌نمایش Admin — ایندکس نمی‌شود و مسیر عمومی Draft نیست."
          : "Admin preview — not indexed and not a public Draft route."}
      </div>
      <article className="overflow-hidden rounded-2xl border border-gray-200 bg-white shadow-sm">
        <div className="relative aspect-[16/9] bg-gray-100">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img src={cover} alt="" className="absolute inset-0 h-full w-full object-cover" />
        </div>
        <div className="space-y-4 p-5 md:p-8">
          <div className="flex flex-wrap items-center gap-3 text-xs text-gray-500">
            <span className="inline-flex items-center gap-1">
              <User className="size-3.5" />
              {preview.authorDisplayName || "—"}
            </span>
            <span className="inline-flex items-center gap-1">
              <Calendar className="size-3.5" />
              {formatArticleDate(preview.publishDate, preview.locale)}
            </span>
            {preview.category ? (
              <span className="rounded-full bg-blue-50 px-2 py-0.5 font-bold text-[#2563EB]">
                {preview.category}
              </span>
            ) : null}
            <span className="inline-flex items-center gap-1 rounded-full border px-2 py-0.5">
              <BookOpen className="size-3.5" />
              {preview.status}
            </span>
          </div>
          <h1 className="text-2xl font-black text-gray-900 md:text-3xl">{preview.title}</h1>
          <p className="text-base leading-8 text-gray-600">{preview.excerpt}</p>
          <div className="prose prose-neutral max-w-none text-sm leading-8 text-gray-800 md:text-base">
            <ArticleBodyHtml html={preview.body || preview.excerpt} />
          </div>
          {preview.tags.length > 0 ? (
            <div className="flex flex-wrap gap-2 border-t border-gray-100 pt-4">
              {preview.tags.map((tag) => (
                <span key={tag} className="rounded-lg bg-gray-50 px-2 py-1 text-[11px] text-gray-600">
                  #{tag}
                </span>
              ))}
            </div>
          ) : null}
        </div>
      </article>
    </main>
  );
}
