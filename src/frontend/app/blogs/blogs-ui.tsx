"use client";

import { LocalizedLink as Link } from "../../lib/i18n/LocalizedLink.tsx";
import { useEffect, useMemo, useState } from "react";
import { BookOpen, Calendar, ChevronLeft, Flame, Image as ImageIcon, User } from "lucide-react";
import { Swiper, SwiperSlide } from "swiper/react";
import { Autoplay, Pagination, Navigation } from "swiper/modules";
import "swiper/css";
import "swiper/css/pagination";
import "swiper/css/navigation";
import {
  contentCoverUrl,
  formatArticleDate,
  loadPublishedArticles,
  type ContentArticleCard,
} from "../content/content-api";
import { useLocale } from "../../lib/i18n/locale-context.tsx";
import { localeToContentApi } from "../../lib/i18n/routing.ts";
import { storefrontMediaUrl } from "../storefront/storefront-api";

const ACCENT = "#2563EB";

function coverSrc(article: ContentArticleCard): string {
  return article.coverMediaAssetId
    ? storefrontMediaUrl(article.coverMediaAssetId) || contentCoverUrl(article.coverMediaAssetId)
    : contentCoverUrl(null);
}

function BlogSlider({ posts }: { posts: ContentArticleCard[] }) {
  const sliderPosts = posts.slice(0, 5);
  if (sliderPosts.length === 0) return null;
  return (
    <div className="relative w-full overflow-hidden rounded-2xl shadow-xl group">
      <Swiper
        modules={[Autoplay, Pagination, Navigation]}
        autoplay={{ delay: 5000, disableOnInteraction: false }}
        pagination={{ clickable: true, dynamicBullets: true }}
        navigation
        loop={sliderPosts.length > 1}
        className="w-full h-[200px] md:h-[280px] lg:h-[320px]"
      >
        {sliderPosts.map((post, index) => (
          <SwiperSlide key={post.articleId}>
            <Link href={`/blogs/${post.slug}`} className="block w-full h-full relative group">
              <div className="absolute inset-0 bg-gradient-to-t from-black/70 via-black/30 to-transparent z-10" />
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={coverSrc(post)} alt="" className="absolute inset-0 w-full h-full object-cover duration-700" />
              <div className="absolute bottom-0 left-0 right-0 z-20 p-4 md:p-6 text-white">
                <div className="flex items-center gap-2 mb-1 flex-wrap">
                  <span className="text-[8px] font-bold bg-blue-500 px-2 py-0.5 rounded-full flex items-center gap-1">
                    <ImageIcon className="w-2.5 h-2.5" /> تصویر
                  </span>
                  {post.category ? (
                    <span className="text-[8px] font-bold bg-amber-500 px-2 py-0.5 rounded-full">{post.category}</span>
                  ) : null}
                  {index === 0 && post.isFeatured ? (
                    <span className="text-[8px] font-bold bg-[#2563EB] px-2 py-0.5 rounded-full animate-pulse flex items-center gap-1">
                      <Flame className="w-3 h-3" /> ویژه
                    </span>
                  ) : null}
                </div>
                <h2 className="text-base md:text-xl lg:text-2xl font-black line-clamp-2">{post.title}</h2>
                <p className="mt-1 text-xs md:text-sm text-white/80 line-clamp-2">{post.excerpt}</p>
              </div>
            </Link>
          </SwiperSlide>
        ))}
      </Swiper>
    </div>
  );
}

function PostCard({ post, contentLocale }: { post: ContentArticleCard; contentLocale: string }) {
  return (
    <Link href={`/blogs/${post.slug}`} className="group block h-full">
      <article className="bg-white rounded-2xl overflow-hidden border border-gray-200 shadow-sm hover:shadow-2xl hover:-translate-y-2 transition-all duration-400 h-full flex flex-col">
        <div className="relative overflow-hidden aspect-[16/10] bg-gray-100">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img src={coverSrc(post)} alt="" className="absolute inset-0 w-full h-full object-cover transition-transform duration-700 group-hover:scale-110" />
          {post.category ? (
            <span className="absolute top-2 left-2 px-2 py-0.5 text-[9px] font-medium bg-white/95 rounded-lg shadow-sm border" style={{ color: ACCENT }}>
              {post.category}
            </span>
          ) : null}
          {post.isFeatured ? (
            <span className="absolute top-2 right-2 px-1.5 py-0.5 text-[9px] font-bold bg-gradient-to-r from-amber-500 to-orange-500 text-white rounded-lg flex items-center gap-1">
              <Flame className="w-2.5 h-2.5" /> ویژه
            </span>
          ) : null}
        </div>
        <div className="p-3 flex-1 flex flex-col">
          <div className="flex items-center gap-1.5 mb-1.5 text-[9px] text-gray-500">
            <User className="w-2.5 h-2.5" />
            <span className="line-clamp-1">{post.authorDisplayName}</span>
            <span>•</span>
            <Calendar className="w-2.5 h-2.5" />
            <span>{formatArticleDate(post.publishDate, contentLocale)}</span>
          </div>
          <h3 className="text-sm font-bold text-gray-900 line-clamp-2 group-hover:text-[#2563EB] transition-colors">{post.title}</h3>
          <p className="mt-1 text-xs text-gray-500 line-clamp-2 flex-1">{post.excerpt}</p>
          <span className="mt-3 inline-flex items-center gap-1 text-xs font-bold" style={{ color: ACCENT }}>
            مطالعه <ChevronLeft className="w-3.5 h-3.5" />
          </span>
        </div>
      </article>
    </Link>
  );
}

/** فهرست زندهٔ بلاگ — ساختار Shopeiva blogsUi با داده Host. */
export function BlogsListingClient() {
  const locale = useLocale();
  const contentLocale = localeToContentApi(locale);
  const [page, setPage] = useState(1);
  const [items, setItems] = useState<ContentArticleCard[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [category, setCategory] = useState<string>("");

  useEffect(() => {
    setLoading(true);
    void loadPublishedArticles(page, 12, category || undefined, contentLocale).then((result) => {
      setItems(result.items);
      setTotal(result.totalCount);
      setLoading(false);
    });
  }, [page, category, contentLocale]);

  const categories = useMemo(() => {
    const set = new Set<string>();
    for (const item of items) if (item.category) set.add(item.category);
    return [...set];
  }, [items]);

  return (
    <main className="mx-auto max-w-6xl space-y-6 px-3 py-6 md:px-4" data-testid="blogs-listing">
      <div className="flex items-center gap-2">
        <BookOpen className="size-5" style={{ color: ACCENT }} />
        <h1 className="text-xl font-black md:text-2xl">مجله توبا</h1>
      </div>
      <BlogSlider posts={items} />
      {categories.length > 0 ? (
        <div className="flex flex-wrap gap-2">
          <button type="button" onClick={() => { setCategory(""); setPage(1); }} className={`rounded-full px-3 py-1 text-xs font-bold ${category === "" ? "bg-[#2563EB] text-white" : "bg-gray-100 text-gray-700"}`}>همه</button>
          {categories.map((cat) => (
            <button key={cat} type="button" onClick={() => { setCategory(cat); setPage(1); }} className={`rounded-full px-3 py-1 text-xs font-bold ${category === cat ? "bg-[#2563EB] text-white" : "bg-gray-100 text-gray-700"}`}>{cat}</button>
          ))}
        </div>
      ) : null}
      {loading ? <p className="text-sm text-gray-500">در حال بارگذاری مقالات…</p> : null}
      {!loading && items.length === 0 ? (
        <div className="rounded-2xl border border-dashed border-gray-300 bg-white p-10 text-center text-sm text-gray-500">
          هنوز مقالهٔ منتشرشده‌ای وجود ندارد.
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {items.map((post) => <PostCard key={post.articleId} post={post} contentLocale={contentLocale} />)}
        </div>
      )}
      {total > 12 ? (
        <div className="flex justify-center gap-2">
          <button type="button" disabled={page <= 1} onClick={() => setPage((p) => Math.max(1, p - 1))} className="rounded-xl border px-4 py-2 text-sm disabled:opacity-40">قبلی</button>
          <span className="rounded-xl bg-gray-50 px-4 py-2 text-sm">{page.toLocaleString("fa-IR")}</span>
          <button type="button" disabled={page * 12 >= total} onClick={() => setPage((p) => p + 1)} className="rounded-xl border px-4 py-2 text-sm disabled:opacity-40">بعدی</button>
        </div>
      ) : null}
    </main>
  );
}
