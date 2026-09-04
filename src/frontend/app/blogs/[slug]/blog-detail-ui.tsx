"use client";

import { LocalizedLink as Link } from "../../../lib/i18n/LocalizedLink.tsx";
import { useEffect, useState } from "react";
import { ArrowLeft, ArrowRight, BookOpen, Calendar, User } from "lucide-react";
import { ArticleBodyHtml } from "../../content/article-body-html.tsx";
import {
  contentCoverUrl,
  formatArticleDate,
  loadPublishedArticleBySlug,
  type ContentArticleCard,
} from "../../content/content-api";
import { storefrontMediaUrl } from "../../storefront/storefront-api";
import { useLocale } from "../../../lib/i18n/locale-context.tsx";
import { blogsAuthorPath, blogsCategoryPath, blogsCopy } from "../blogs-copy.ts";

/** جزئیات مقالهٔ زنده — locale-aware lookup بدون fallback. */
export function BlogDetailClient({ slug, contentLocale }: { slug: string; contentLocale: string }) {
  const locale = useLocale();
  const copy = blogsCopy(locale);
  const [article, setArticle] = useState<ContentArticleCard | null>(null);
  const [loading, setLoading] = useState(true);
  const BackArrow = locale === "en" ? ArrowLeft : ArrowRight;

  useEffect(() => {
    setLoading(true);
    void loadPublishedArticleBySlug(slug, contentLocale).then((row) => {
      setArticle(row);
      setLoading(false);
    });
  }, [slug, contentLocale]);

  if (loading) return <p className="p-6 text-sm text-gray-500">{copy.loading}</p>;
  if (!article) {
    return (
      <main className="mx-auto max-w-3xl px-3 py-10 text-center md:px-4">
        <h1 className="text-xl font-bold">{copy.notFound}</h1>
        <Link href="/blogs" className="mt-4 inline-flex items-center gap-1 text-sm text-[#2563EB]">
          <BackArrow className="size-4" /> {copy.backToMagazine}
        </Link>
      </main>
    );
  }

  const cover = article.coverMediaAssetId
    ? storefrontMediaUrl(article.coverMediaAssetId) || contentCoverUrl(article.coverMediaAssetId)
    : contentCoverUrl(null);

  return (
    <main className="mx-auto max-w-3xl space-y-6 px-3 py-6 md:px-4" data-testid="blog-detail">
      <Link href="/blogs" className="inline-flex items-center gap-1 text-sm text-[#2563EB]">
        <BackArrow className="size-4" /> {copy.backToMagazine}
      </Link>
      <article className="overflow-hidden rounded-2xl border border-gray-200 bg-white shadow-sm">
        <div className="relative aspect-[16/9] bg-gray-100">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img src={cover} alt="" className="absolute inset-0 h-full w-full object-cover" />
        </div>
        <div className="space-y-4 p-5 md:p-8">
          <div className="flex flex-wrap items-center gap-3 text-xs text-gray-500">
            {article.authorSlug ? (
              <Link href={blogsAuthorPath(article.authorSlug)} className="inline-flex items-center gap-1 hover:text-[#2563EB]">
                <User className="size-3.5" />
                {article.authorDisplayName}
              </Link>
            ) : (
              <span className="inline-flex items-center gap-1">
                <User className="size-3.5" />
                {article.authorDisplayName}
              </span>
            )}
            <span className="inline-flex items-center gap-1">
              <Calendar className="size-3.5" />
              {formatArticleDate(article.publishDate, contentLocale)}
            </span>
            {article.category ? (
              article.categorySlug ? (
                <Link
                  href={blogsCategoryPath(article.categorySlug)}
                  className="rounded-full bg-blue-50 px-2 py-0.5 font-bold text-[#2563EB]"
                >
                  {article.category}
                </Link>
              ) : (
                <span className="rounded-full bg-blue-50 px-2 py-0.5 font-bold text-[#2563EB]">{article.category}</span>
              )
            ) : null}
          </div>
          <h1 className="text-2xl font-black text-gray-900 md:text-3xl">{article.title}</h1>
          <p className="text-base text-gray-600 leading-8">{article.excerpt}</p>
          <div className="prose prose-neutral max-w-none text-sm leading-8 text-gray-800 md:text-base">
            <ArticleBodyHtml html={article.body || article.excerpt || ""} />
          </div>
          {article.tags.length > 0 ? (
            <div className="flex flex-wrap gap-2 border-t border-gray-100 pt-4">
              {article.tags.map((tag) => (
                <span key={tag} className="rounded-lg bg-gray-50 px-2 py-1 text-[11px] text-gray-600">
                  #{tag}
                </span>
              ))}
            </div>
          ) : null}
        </div>
      </article>
      <div className="flex items-center gap-2 text-sm text-gray-500">
        <BookOpen className="size-4 text-[#2563EB]" />
        {copy.magazineFooter}
      </div>
    </main>
  );
}
