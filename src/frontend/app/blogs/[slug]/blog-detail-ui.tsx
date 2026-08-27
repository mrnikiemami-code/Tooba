"use client";

import { LocalizedLink as Link } from "../../../lib/i18n/LocalizedLink.tsx";
import { useEffect, useState } from "react";
import { ArrowRight, BookOpen, Calendar, User } from "lucide-react";
import {
  contentCoverUrl,
  formatContentDate,
  loadPublishedArticleBySlug,
  type ContentArticleCard,
} from "../../content/content-api";
import { storefrontMediaUrl } from "../../storefront/storefront-api";

/** جزئیات مقالهٔ زنده — ساختار سادهٔ Shopeiva detail با داده Host. */
export function BlogDetailClient({ slug }: { slug: string }) {
  const [article, setArticle] = useState<ContentArticleCard | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setLoading(true);
    void loadPublishedArticleBySlug(slug).then((row) => {
      setArticle(row);
      setLoading(false);
    });
  }, [slug]);

  if (loading) return <p className="p-6 text-sm text-gray-500">در حال بارگذاری مقاله…</p>;
  if (!article) {
    return (
      <main className="mx-auto max-w-3xl px-4 py-10 text-center">
        <h1 className="text-xl font-bold">مقاله پیدا نشد</h1>
        <Link href="/blogs" className="mt-4 inline-flex items-center gap-1 text-sm text-[#2563EB]">بازگشت به مجله <ArrowRight className="size-4" /></Link>
      </main>
    );
  }

  const cover = article.coverMediaAssetId
    ? storefrontMediaUrl(article.coverMediaAssetId) || contentCoverUrl(article.coverMediaAssetId)
    : contentCoverUrl(null);

  return (
    <main className="mx-auto max-w-3xl space-y-6 px-4 py-6" data-testid="blog-detail">
      <Link href="/blogs" className="inline-flex items-center gap-1 text-sm text-[#2563EB]">
        <ArrowRight className="size-4" /> مجله توبا
      </Link>
      <article className="overflow-hidden rounded-2xl border border-gray-200 bg-white shadow-sm">
        <div className="relative aspect-[16/9] bg-gray-100">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img src={cover} alt="" className="absolute inset-0 h-full w-full object-cover" />
        </div>
        <div className="space-y-4 p-5 md:p-8">
          <div className="flex flex-wrap items-center gap-3 text-xs text-gray-500">
            <span className="inline-flex items-center gap-1"><User className="size-3.5" />{article.authorDisplayName}</span>
            <span className="inline-flex items-center gap-1"><Calendar className="size-3.5" />{formatContentDate(article.publishDate)}</span>
            {article.category ? <span className="rounded-full bg-blue-50 px-2 py-0.5 font-bold text-[#2563EB]">{article.category}</span> : null}
          </div>
          <h1 className="text-2xl font-black text-gray-900 md:text-3xl">{article.title}</h1>
          <p className="text-base text-gray-600 leading-8">{article.excerpt}</p>
          <div className="prose prose-neutral max-w-none whitespace-pre-wrap text-sm leading-8 text-gray-800 md:text-base">
            {article.body || article.excerpt}
          </div>
          {article.tags.length > 0 ? (
            <div className="flex flex-wrap gap-2 border-t border-gray-100 pt-4">
              {article.tags.map((tag) => (
                <span key={tag} className="rounded-lg bg-gray-50 px-2 py-1 text-[11px] text-gray-600">#{tag}</span>
              ))}
            </div>
          ) : null}
        </div>
      </article>
      <div className="flex items-center gap-2 text-sm text-gray-500">
        <BookOpen className="size-4 text-[#2563EB]" />
        محتوای منتشرشده از ماژول Content
      </div>
    </main>
  );
}
