"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import {
  CheckCircle,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  Clock,
  Filter,
  Package,
  Search,
  Star,
  X,
} from "lucide-react";
import {
  loadSellerReviews,
  readSellerPartyId,
  type SellerReviewRow,
  type SellerReviewsPage,
} from "./seller-api";

const ITEMS_PER_PAGE = 4;

function toPersianDigits(value: number | string): string {
  return String(value).replace(/\d/g, (d) => "۰۱۲۳۴۵۶۷۸۹"[Number(d)]);
}

function formatReviewDate(iso: string): string {
  if (!iso) return "—";
  try {
    return new Intl.DateTimeFormat("fa-IR", { dateStyle: "short" }).format(new Date(iso));
  } catch {
    return iso;
  }
}

function statusLabelOf(row: SellerReviewRow): string {
  if (row.statusLabel) return row.statusLabel;
  switch (row.status) {
    case "Published":
      return "تایید شده";
    case "Pending":
      return "در انتظار";
    case "Rejected":
      return "رد شده";
    default:
      return row.status || "—";
  }
}

type StatusFilter = "all" | "تایید شده" | "در انتظار" | "رد شده";

function filterToApiStatus(filter: StatusFilter): string | undefined {
  switch (filter) {
    case "تایید شده":
      return "Published";
    case "در انتظار":
      return "Pending";
    case "رد شده":
      return "Rejected";
    default:
      return undefined;
  }
}

/**
 * فهرست زندهٔ نظرات فروشنده — پورت بصری Shopeiva reviewsList با دادهٔ Host.
 * بدون تایید/رد/حذف فروشنده و بدون شمارش جعلی؛ پاسخ فروشنده پشتیبانی نمی‌شود.
 */
export function VendorReviewsUi({ sellerPartyId }: { sellerPartyId: string }) {
  const [searchTerm, setSearchTerm] = useState("");
  const [filter, setFilter] = useState<StatusFilter>("all");
  const [isFilterOpen, setIsFilterOpen] = useState(false);
  const [currentPage, setCurrentPage] = useState(0);
  const [loading, setLoading] = useState(true);
  const [denied, setDenied] = useState(false);
  const [message, setMessage] = useState<string>();
  const [page, setPage] = useState<SellerReviewsPage | null>(null);

  const refresh = useCallback(async () => {
    setLoading(true);
    const result = await loadSellerReviews(sellerPartyId, {
      status: filterToApiStatus(filter),
      page: 1,
      pageSize: 100,
    });
    setDenied(Boolean(result.denied));
    setMessage(result.message);
    setPage(result.page);
    setLoading(false);
  }, [sellerPartyId, filter]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  useEffect(() => {
    setCurrentPage(0);
  }, [searchTerm, filter]);

  const searchResults = useMemo(() => {
    const rows = page?.rows ?? [];
    const needle = searchTerm.trim().toLowerCase();
    if (!needle) return rows;
    return rows.filter((row) => {
      const haystack = [row.authorDisplayName, row.productTitle, row.body, row.title ?? "", statusLabelOf(row)]
        .join(" ")
        .toLowerCase();
      return haystack.includes(needle);
    });
  }, [page, searchTerm]);

  const pageCount = Math.max(1, Math.ceil(searchResults.length / ITEMS_PER_PAGE));
  const safePage = Math.min(currentPage, pageCount - 1);
  const offset = safePage * ITEMS_PER_PAGE;
  const currentItems = searchResults.slice(offset, offset + ITEMS_PER_PAGE);

  const stats = {
    total: (page?.publishedCount ?? 0) + (page?.pendingCount ?? 0) + (page?.rejectedCount ?? 0),
    approved: page?.publishedCount ?? 0,
    pending: page?.pendingCount ?? 0,
    rejected: page?.rejectedCount ?? 0,
  };

  const clearSearch = () => {
    setSearchTerm("");
    setCurrentPage(0);
  };

  const renderStars = (rating: number) => (
    <div className="flex items-center gap-0.5">
      {[1, 2, 3, 4, 5].map((i) => (
        <Star
          key={i}
          className={`h-3.5 w-3.5 ${i <= rating ? "fill-amber-400 text-amber-400" : "text-gray-300"}`}
        />
      ))}
    </div>
  );

  if (denied) {
    return (
      <div className="rounded-2xl border border-red-200 bg-white p-8 text-center dark:border-red-900 dark:bg-[#111]">
        <p className="text-sm text-red-600">دسترسی به نظرات این فروشنده مجاز نیست.</p>
        <button type="button" onClick={() => void refresh()} className="mt-3 text-xs font-medium text-[#E53935] hover:underline">
          تلاش دوباره
        </button>
      </div>
    );
  }

  return (
    <div className="space-y-4" data-testid="vendor-reviews">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-2">
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-[#E53935]/10">
            <Star className="h-5 w-5 text-[#E53935]" />
          </div>
          <div>
            <h2 className="text-lg font-bold text-gray-900 dark:text-white">مدیریت نظرات</h2>
            <p className="text-xs text-gray-500 dark:text-gray-400">
              {toPersianDigits(stats.total)} نظر · {toPersianDigits(stats.approved)} تایید شده ·{" "}
              {toPersianDigits(stats.pending)} در انتظار
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2 rounded-full border border-emerald-200 bg-emerald-50 px-3 py-1.5 dark:border-emerald-800 dark:bg-emerald-900/20">
          <CheckCircle className="h-4 w-4 text-emerald-500" />
          <span className="text-xs font-bold text-emerald-600 dark:text-emerald-400">
            {toPersianDigits(stats.approved)} تایید شده
          </span>
        </div>
      </div>

      <div className="grid grid-cols-3 gap-2">
        <div className="rounded-xl border border-gray-200 bg-white p-3 text-center dark:border-gray-800 dark:bg-[#111]">
          <p className="text-lg font-black text-emerald-500">{toPersianDigits(stats.approved)}</p>
          <p className="text-[10px] text-gray-500 dark:text-gray-400">تایید شده</p>
        </div>
        <div className="rounded-xl border border-gray-200 bg-white p-3 text-center dark:border-gray-800 dark:bg-[#111]">
          <p className="text-lg font-black text-amber-500">{toPersianDigits(stats.pending)}</p>
          <p className="text-[10px] text-gray-500 dark:text-gray-400">در انتظار</p>
        </div>
        <div className="rounded-xl border border-gray-200 bg-white p-3 text-center dark:border-gray-800 dark:bg-[#111]">
          <p className="text-lg font-black text-red-500">{toPersianDigits(stats.rejected)}</p>
          <p className="text-[10px] text-gray-500 dark:text-gray-400">رد شده</p>
        </div>
      </div>

      <div className="flex flex-col gap-3 sm:flex-row">
        <div className="relative flex-1">
          <Search className="absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-gray-400" />
          <input
            type="text"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            placeholder="جستجو در نظرات (مشتری، محصول، متن نظر...)"
            className="w-full rounded-xl border border-gray-200 bg-white px-4 py-2.5 pr-10 text-sm text-gray-900 focus:outline-none focus:ring-2 focus:ring-[#E53935] dark:border-gray-700 dark:bg-[#111] dark:text-white"
          />
          {searchTerm ? (
            <button
              type="button"
              onClick={clearSearch}
              className="absolute left-3 top-1/2 -translate-y-1/2 rounded-full p-1 transition-colors hover:bg-gray-200 dark:hover:bg-gray-700"
            >
              <X className="h-4 w-4 text-gray-400" />
            </button>
          ) : null}
        </div>
        <div className="relative">
          <button
            type="button"
            onClick={() => setIsFilterOpen(!isFilterOpen)}
            className={`flex items-center gap-2 rounded-xl px-4 py-2.5 text-sm font-medium transition-all ${
              filter !== "all"
                ? "bg-[#E53935] text-white shadow-lg shadow-[#E53935]/30"
                : "border border-gray-200 bg-white text-gray-700 hover:border-[#E53935]/50 dark:border-gray-700 dark:bg-[#111] dark:text-gray-300"
            }`}
          >
            <Filter className="h-4 w-4" />
            {filter === "all" ? "همه" : filter}
            <ChevronDown className={`h-4 w-4 transition-transform ${isFilterOpen ? "rotate-180" : ""}`} />
          </button>
          {isFilterOpen ? (
            <div className="absolute left-0 top-full z-10 mt-1 min-w-[140px] rounded-xl border border-gray-200 bg-white shadow-lg dark:border-gray-700 dark:bg-[#111]">
              {(["all", "تایید شده", "در انتظار", "رد شده"] as const).map((status) => (
                <button
                  key={status}
                  type="button"
                  onClick={() => {
                    setFilter(status);
                    setIsFilterOpen(false);
                  }}
                  className={`block w-full px-4 py-2 text-right text-sm transition-colors hover:bg-gray-100 dark:hover:bg-gray-800 ${
                    filter === status ? "font-bold text-[#E53935]" : "text-gray-700 dark:text-gray-300"
                  }`}
                >
                  {status === "all" ? "همه" : status}
                  {status !== "all" ? (
                    <span className="mr-1 text-[10px] text-gray-400">
                      (
                      {toPersianDigits(
                        status === "تایید شده" ? stats.approved : status === "در انتظار" ? stats.pending : stats.rejected,
                      )}
                      )
                    </span>
                  ) : null}
                </button>
              ))}
            </div>
          ) : null}
        </div>
      </div>

      {searchTerm ? (
        <div className="text-xs text-gray-500 dark:text-gray-400">
          {toPersianDigits(searchResults.length)} نتیجه برای &quot;{searchTerm}&quot;
        </div>
      ) : null}

      {loading ? (
        <div className="rounded-2xl border border-gray-200 bg-white p-8 text-center text-sm text-gray-500 dark:border-gray-800 dark:bg-[#111]">
          در حال بارگذاری نظرات…
        </div>
      ) : message && !page ? (
        <div className="rounded-2xl border border-red-200 bg-white p-8 text-center dark:border-red-900 dark:bg-[#111]">
          <p className="text-sm text-red-600">خواندن نظرات ممکن نشد.</p>
          <button type="button" onClick={() => void refresh()} className="mt-2 text-xs text-[#E53935] hover:underline">
            تلاش دوباره
          </button>
        </div>
      ) : (
        <div className="space-y-3">
          {currentItems.length === 0 ? (
            <div className="rounded-2xl border border-gray-200 bg-white p-8 text-center dark:border-gray-800 dark:bg-[#111]">
              <div className="mx-auto mb-3 flex h-16 w-16 items-center justify-center rounded-full bg-gray-100 dark:bg-gray-800">
                <Star className="h-8 w-8 text-gray-300 dark:text-gray-600" />
              </div>
              <p className="text-sm text-gray-500 dark:text-gray-400">هیچ نظری یافت نشد</p>
              {searchTerm ? (
                <button type="button" onClick={clearSearch} className="mt-2 text-xs text-[#E53935] hover:underline">
                  پاک کردن جستجو
                </button>
              ) : null}
            </div>
          ) : (
            currentItems.map((review) => {
              const label = statusLabelOf(review);
              const statusColors: Record<string, string> = {
                "تایید شده":
                  "bg-emerald-100 dark:bg-emerald-900/20 text-emerald-600 dark:text-emerald-400 border-emerald-200 dark:border-emerald-800",
                "در انتظار":
                  "bg-amber-100 dark:bg-amber-900/20 text-amber-600 dark:text-amber-400 border-amber-200 dark:border-amber-800",
                "رد شده":
                  "bg-red-100 dark:bg-red-900/20 text-red-600 dark:text-red-400 border-red-200 dark:border-red-800",
              };
              const borderClass =
                label === "تایید شده"
                  ? "border-emerald-200 dark:border-emerald-800"
                  : label === "در انتظار"
                    ? "border-amber-200 dark:border-amber-800"
                    : "border-red-200 dark:border-red-800";
              return (
                <div
                  key={review.reviewId}
                  className={`rounded-2xl border-2 bg-white p-4 transition-all duration-300 hover:shadow-lg dark:bg-[#111] ${borderClass}`}
                >
                  <div className="flex items-start gap-4">
                    <div className="flex h-16 w-16 flex-shrink-0 items-center justify-center overflow-hidden rounded-lg border border-gray-200 bg-gray-100 dark:border-gray-700 dark:bg-gray-800">
                      <Package className="h-7 w-7 text-gray-400" aria-hidden />
                    </div>
                    <div className="min-w-0 flex-1">
                      <div className="flex flex-wrap items-center justify-between gap-2">
                        <div className="flex items-center gap-2">
                          <p className="font-bold text-gray-900 dark:text-white">{review.authorDisplayName}</p>
                          <span
                            className={`rounded-full border px-2 py-0.5 text-[10px] font-medium ${statusColors[label] ?? "border-gray-200 text-gray-500"}`}
                          >
                            {label}
                          </span>
                          {review.verifiedPurchase ? (
                            <span className="rounded-full border border-emerald-200 bg-emerald-50 px-2 py-0.5 text-[10px] text-emerald-600 dark:border-emerald-800 dark:bg-emerald-900/20 dark:text-emerald-400">
                              خرید تأییدشده
                            </span>
                          ) : null}
                        </div>
                        <div className="flex items-center gap-2 text-xs text-gray-500 dark:text-gray-400">
                          <Clock className="h-3.5 w-3.5" />
                          {formatReviewDate(review.createdAt)}
                        </div>
                      </div>
                      <div className="mt-1 flex items-center gap-2 text-xs text-gray-500 dark:text-gray-400">
                        <Package className="h-3.5 w-3.5" />
                        <span>{review.productTitle}</span>
                      </div>
                      <div className="mt-1 flex items-center gap-3">
                        {renderStars(review.rating)}
                        <span className="text-xs font-bold text-gray-700 dark:text-gray-300">
                          {toPersianDigits(review.rating)}.۰
                        </span>
                      </div>
                      {review.title ? (
                        <p className="mt-2 text-sm font-medium text-gray-800 dark:text-gray-200">{review.title}</p>
                      ) : null}
                      <p className="mt-2 text-sm leading-relaxed text-gray-600 dark:text-gray-400">{review.body}</p>
                    </div>
                  </div>
                </div>
              );
            })
          )}
        </div>
      )}

      {!loading && searchResults.length > ITEMS_PER_PAGE ? (
        <div className="flex flex-col flex-wrap items-center justify-between gap-3">
          <div className="flex items-center gap-1.5">
            <button
              type="button"
              disabled={safePage <= 0}
              onClick={() => setCurrentPage((p) => Math.max(0, p - 1))}
              className="flex h-8 w-8 items-center justify-center rounded-lg text-gray-400 transition-colors hover:bg-gray-100 hover:text-gray-700 disabled:pointer-events-none disabled:opacity-50 dark:hover:bg-gray-800 dark:hover:text-gray-300"
            >
              <ChevronRight className="h-4 w-4" />
            </button>
            {Array.from({ length: pageCount }, (_, i) => (
              <button
                key={i}
                type="button"
                onClick={() => setCurrentPage(i)}
                className={`flex h-8 w-8 items-center justify-center rounded-lg text-sm font-medium transition-colors ${
                  i === safePage
                    ? "bg-[#E53935] text-white shadow-lg shadow-[#E53935]/30"
                    : "text-gray-700 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-800"
                }`}
              >
                {toPersianDigits(i + 1)}
              </button>
            ))}
            <button
              type="button"
              disabled={safePage >= pageCount - 1}
              onClick={() => setCurrentPage((p) => Math.min(pageCount - 1, p + 1))}
              className="flex h-8 w-8 items-center justify-center rounded-lg text-gray-400 transition-colors hover:bg-gray-100 hover:text-gray-700 disabled:pointer-events-none disabled:opacity-50 dark:hover:bg-gray-800 dark:hover:text-gray-300"
            >
              <ChevronLeft className="h-4 w-4" />
            </button>
          </div>
          <div className="text-xs text-gray-500 dark:text-gray-400">
            نمایش {toPersianDigits(offset + 1)} تا{" "}
            {toPersianDigits(Math.min(offset + ITEMS_PER_PAGE, searchResults.length))} از{" "}
            {toPersianDigits(searchResults.length)} نتیجه
          </div>
        </div>
      ) : null}

      {page && !page.sellerResponseSupported ? (
        <p className="text-[11px] text-gray-400">پاسخ فروشنده به نظر در این نسخه پشتیبانی نمی‌شود.</p>
      ) : null}
    </div>
  );
}

/** صفحهٔ نظرات فروشنده با seller party از storage/query. */
export function VendorReviewsPageClient() {
  const [sellerPartyId, setSellerPartyId] = useState("");
  useEffect(() => {
    setSellerPartyId(readSellerPartyId(window.location.search) ?? "");
  }, []);
  if (!sellerPartyId) return <p className="text-muted">فروشنده انتخاب نشده است.</p>;
  return <VendorReviewsUi sellerPartyId={sellerPartyId} />;
}
