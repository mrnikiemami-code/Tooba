"use client";

import { LocalizedLink as Link } from "../../lib/i18n/LocalizedLink.tsx";
import { useEffect, useState } from "react";
import { ArrowRight, BookOpen, Calendar, ChevronLeft, Flame, User } from "lucide-react";
import {
  contentCoverUrl,
  formatArticleDate,
  loadPublishedArticles,
  type ContentArticleCard,
} from "../content/content-api";
import { useLocale } from "../../lib/i18n/locale-context.tsx";
import { localeToContentApi } from "../../lib/i18n/routing.ts";
import { storefrontMediaUrl } from "../storefront/storefront-api";
import { blogsAuthorPath, blogsCategoryPath, blogsCopy } from "./blogs-copy.ts";

const ACCENT = "#2563EB";
const PAGE_SIZE = 12;

function coverSrc(article: ContentArticleCard): string {
  return article.coverMediaAssetId
    ? storefrontMediaUrl(article.coverMediaAssetId) || contentCoverUrl(article.coverMediaAssetId)
    : contentCoverUrl(null);
}

function TaxonomyPostCard({
  post,
  contentLocale,
  copy,
}: {
  post: ContentArticleCard;
  contentLocale: string;
  copy: ReturnType<typeof blogsCopy>;
}) {
  return (
    <article className="group relative bg-white rounded-2xl overflow-hidden border border-gray-200 shadow-sm hover:shadow-2xl hover:-translate-y-2 transition-all duration-400 h-full flex flex-col">
      <div className="relative overflow-hidden aspect-[16/10] bg-gray-100">
        <Link href={`/blogs/${post.slug}`} className="absolute inset-0 z-0" aria-label={post.title}>
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img
            src={coverSrc(post)}
            alt=""
            className="absolute inset-0 w-full h-full object-cover transition-transform duration-700 group-hover:scale-110"
          />
        </Link>
        {post.isFeatured ? (
          <span className="absolute top-2 right-2 z-10 px-1.5 py-0.5 text-[9px] font-bold bg-gradient-to-r from-amber-500 to-orange-500 text-white rounded-lg flex items-center gap-1">
            <Flame className="w-2.5 h-2.5" /> {copy.featured}
          </span>
        ) : null}
      </div>
      <div className="p-3 flex-1 flex flex-col">
        <div className="flex items-center gap-1.5 mb-1.5 text-[9px] text-gray-500">
          <User className="w-2.5 h-2.5" />
          {post.authorSlug ? (
            <Link href={blogsAuthorPath(post.authorSlug)} className="line-clamp-1 hover:text-[#2563EB]">
              {post.authorDisplayName}
            </Link>
          ) : (
            <span className="line-clamp-1">{post.authorDisplayName}</span>
          )}
          <span>•</span>
          <Calendar className="w-2.5 h-2.5" />
          <span>{formatArticleDate(post.publishDate, contentLocale)}</span>
        </div>
        {post.category ? (
          post.categorySlug ? (
            <Link
              href={blogsCategoryPath(post.categorySlug)}
              className="mb-1 self-start text-[9px] font-medium"
              style={{ color: ACCENT }}
            >
              {post.category}
            </Link>
          ) : (
            <span className="mb-1 self-start text-[9px] font-medium" style={{ color: ACCENT }}>
              {post.category}
            </span>
          )
        ) : null}
        <Link href={`/blogs/${post.slug}`} className="block">
          <h3 className="text-sm font-bold text-gray-900 line-clamp-2 group-hover:text-[#2563EB] transition-colors">
            {post.title}
          </h3>
          <p className="mt-1 text-xs text-gray-500 line-clamp-2 flex-1">{post.excerpt}</p>
          <span className="mt-3 inline-flex items-center gap-1 text-xs font-bold" style={{ color: ACCENT }}>
            {copy.readMore} <ChevronLeft className="w-3.5 h-3.5" />
          </span>
        </Link>
      </div>
    </article>
  );
}

export type BlogsTaxonomyKind = "category" | "author";

/** فهرست مقالات فیلترشده بر اساس دسته یا نویسنده. */
export function BlogsTaxonomyListingClient({
  kind,
  slug,
  heading,
  description,
}: {
  kind: BlogsTaxonomyKind;
  slug: string;
  heading: string;
  description?: string | null;
}) {
  const locale = useLocale();
  const copy = blogsCopy(locale);
  const contentLocale = localeToContentApi(locale);
  const [page, setPage] = useState(1);
  const [items, setItems] = useState<ContentArticleCard[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setLoading(true);
    const categorySlug = kind === "category" ? slug : undefined;
    const authorSlug = kind === "author" ? slug : undefined;
    void loadPublishedArticles(page, PAGE_SIZE, undefined, contentLocale, categorySlug, authorSlug).then((result) => {
      setItems(result.items);
      setTotal(result.totalCount);
      setLoading(false);
    });
  }, [page, kind, slug, contentLocale]);

  const label = kind === "category" ? copy.categoryHeading : copy.authorHeading;

  return (
    <main className="mx-auto max-w-6xl space-y-6 px-3 py-6 md:px-4" data-testid={`blogs-${kind}-listing`}>
      <Link href="/blogs" className="inline-flex items-center gap-1 text-sm text-[#2563EB]">
        <ArrowRight className="size-4" /> {copy.backToMagazine}
      </Link>
      <div className="space-y-2">
        <div className="flex items-center gap-2">
          <BookOpen className="size-5" style={{ color: ACCENT }} />
          <p className="text-xs font-bold uppercase tracking-wide text-gray-500">{label}</p>
        </div>
        <h1 className="text-xl font-black md:text-2xl">{heading}</h1>
        {description ? <p className="max-w-2xl text-sm text-gray-600 leading-7">{description}</p> : null}
      </div>
      {loading ? <p className="text-sm text-gray-500">{copy.loading}</p> : null}
      {!loading && items.length === 0 ? (
        <div className="rounded-2xl border border-dashed border-gray-300 bg-white p-10 text-center text-sm text-gray-500">
          {copy.articlesEmpty}
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {items.map((post) => (
            <TaxonomyPostCard key={post.articleId} post={post} contentLocale={contentLocale} copy={copy} />
          ))}
        </div>
      )}
      {total > PAGE_SIZE ? (
        <div className="flex justify-center gap-2">
          <button
            type="button"
            disabled={page <= 1}
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            className="rounded-xl border px-4 py-2 text-sm disabled:opacity-40"
          >
            {copy.prev}
          </button>
          <span className="rounded-xl bg-gray-50 px-4 py-2 text-sm">{page.toLocaleString(contentLocale)}</span>
          <button
            type="button"
            disabled={page * PAGE_SIZE >= total}
            onClick={() => setPage((p) => p + 1)}
            className="rounded-xl border px-4 py-2 text-sm disabled:opacity-40"
          >
            {copy.next}
          </button>
        </div>
      ) : null}
    </main>
  );
}
