"use client";

import { useEffect, useState } from "react";
import { CheckCircle, Edit3, Star, X } from "lucide-react";
import {
  loadStorefrontReviews,
  submitStorefrontReview,
  type StorefrontReviewApiError,
} from "./storefront-api.ts";
import type { StorefrontProductDetailPage, StorefrontReviewsPage } from "./storefront-model.ts";

function Stars({ rating, size = "size-4" }: { rating: number; size?: string }) {
  return <span className="inline-flex gap-0.5" aria-label={`${rating} از ۵`}>
    {[1, 2, 3, 4, 5].map((value) => <Star key={value} className={`${size} ${value <= Math.round(rating) ? "fill-amber-400 text-amber-400" : "text-gray-300"}`} />)}
  </span>;
}

/** بخش Shopeiva نظرهای PDP را به دادهٔ عمومی Host و فرمان احراز‌شدهٔ مشتری متصل می‌کند. */
export function StorefrontPdpReviews({ detail }: { detail: StorefrontProductDetailPage }) {
  const [page, setPage] = useState<StorefrontReviewsPage | null>(null);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [rating, setRating] = useState(0);
  const [title, setTitle] = useState("");
  const [body, setBody] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    setLoading(true);
    void loadStorefrontReviews(detail.slug).then(setPage).finally(() => setLoading(false));
  }, [detail.slug]);

  const count = page?.reviewCount ?? detail.reviewCount;
  const average = page?.averageRating ?? detail.averageRating;
  return <div className="space-y-5" data-testid="pdp-reviews">
    {loading ? <p className="text-gray-500">در حال دریافت نظرها…</p> : null}
    {!loading && count > 0 && average !== null ? (
      <div className="rounded-xl border border-gray-200 bg-gray-50 p-4">
        <div className="flex flex-col gap-6 md:flex-row">
          <div className="flex-1 text-center md:text-right">
            <div className="text-4xl font-black text-gray-900">{average.toLocaleString("fa-IR", { maximumFractionDigits: 1 })}</div>
            <Stars rating={average} size="size-5" />
            <p className="mt-1 text-sm text-gray-500">{count.toLocaleString("fa-IR")} نظر</p>
          </div>
          <div className="flex-1 space-y-1.5">
            {(page?.ratingDistribution ?? []).map((row) => {
              const percentage = count > 0 ? Math.round(row.count / count * 100) : 0;
              return <div key={row.rating} className="flex items-center gap-2">
                <span className="w-14 text-xs text-gray-600">{row.rating.toLocaleString("fa-IR")} ستاره</span>
                <div className="h-2 flex-1 overflow-hidden rounded-full bg-gray-200"><div className="h-full rounded-full bg-amber-400" style={{ width: `${percentage}%` }} /></div>
                <span className="w-10 text-xs text-gray-500">{percentage.toLocaleString("fa-IR")}٪</span>
              </div>;
            })}
          </div>
        </div>
      </div>
    ) : null}
    {!showForm ? <button type="button" onClick={() => { setShowForm(true); setMessage(null); }} className="inline-flex items-center gap-2 rounded-xl bg-[#2563EB] px-5 py-2.5 text-sm font-bold text-white">
      <Edit3 className="size-4" /> نوشتن نظر
    </button> : (
      <form className="space-y-4 rounded-xl border border-gray-200 bg-white p-4" onSubmit={(event) => {
        event.preventDefault();
        if (rating < 1 || rating > 5) return setMessage("لطفاً امتیاز ۱ تا ۵ را انتخاب کنید.");
        if (body.trim().length < 10) return setMessage("متن نظر باید حداقل ۱۰ کاراکتر باشد.");
        setSubmitting(true); setMessage(null);
        void submitStorefrontReview({ productId: detail.productId, rating, ...(title.trim() ? { title: title.trim() } : {}), body: body.trim() })
          .then(() => { setMessage("نظر شما ثبت شد و پس از بررسی نمایش داده می‌شود."); setRating(0); setTitle(""); setBody(""); })
          .catch((error: StorefrontReviewApiError) => setMessage(error.message))
          .finally(() => setSubmitting(false));
      }}>
        <div className="flex items-center justify-between"><h3 className="font-bold">نوشتن نظر</h3><button type="button" aria-label="بستن فرم" onClick={() => setShowForm(false)}><X className="size-4 text-gray-500" /></button></div>
        <div><span className="mb-1 block text-sm font-medium">امتیاز شما</span><div className="flex gap-1">{[1, 2, 3, 4, 5].map((value) => <button type="button" key={value} onClick={() => setRating(value)} aria-label={`${value} ستاره`}><Star className={`size-7 ${value <= rating ? "fill-amber-400 text-amber-400" : "text-gray-300"}`} /></button>)}</div></div>
        <label className="block text-sm font-medium">عنوان (اختیاری)<input value={title} onChange={(event) => setTitle(event.target.value)} className="mt-1 w-full rounded-xl bg-gray-100 px-4 py-2 outline-none focus:ring-2 focus:ring-[#2563EB]" /></label>
        <label className="block text-sm font-medium">متن نظر<textarea value={body} onChange={(event) => setBody(event.target.value)} rows={4} minLength={10} required className="mt-1 w-full resize-none rounded-xl bg-gray-100 px-4 py-2 outline-none focus:ring-2 focus:ring-[#2563EB]" /></label>
        <button disabled={submitting} className="rounded-xl bg-[#2563EB] px-5 py-2.5 text-sm font-bold text-white disabled:opacity-60">{submitting ? "در حال ارسال…" : "ارسال نظر"}</button>
      </form>
    )}
    {message ? <p role="status" className="text-sm text-gray-600">{message}</p> : null}
    {!loading && count === 0 ? <div className="rounded-xl border border-gray-200 bg-gray-50 p-7 text-center text-gray-500">هنوز نظری برای این کالا منتشر نشده است.</div> : null}
    <div className="space-y-3">
      {(page?.reviews ?? []).map((review) => <article key={review.publicId} className="rounded-xl border border-gray-200 bg-gray-50 p-4">
        <div className="flex flex-wrap items-start justify-between gap-2">
          <div><div className="flex items-center gap-2"><strong>{review.authorDisplayName}</strong>{review.verifiedPurchase ? <span className="inline-flex items-center gap-1 text-[10px] text-emerald-600"><CheckCircle className="size-3" /> خرید تأییدشده</span> : null}</div><Stars rating={review.rating} /></div>
          <time className="text-xs text-gray-500">{review.createdAt ? new Intl.DateTimeFormat("fa-IR").format(new Date(review.createdAt)) : ""}</time>
        </div>
        {review.title ? <h4 className="mt-3 font-bold">{review.title}</h4> : null}
        <p className="mt-2 leading-7 text-gray-700">{review.body}</p>
      </article>)}
    </div>
  </div>;
}
