"use client";

import { useEffect, useState } from "react";
import { CheckCircle, MessageCircle, Send, User } from "lucide-react";
import { loadStorefrontQuestions, submitStorefrontQuestion, type StorefrontQaItem } from "./storefront-api.ts";
import type { StorefrontProductDetailPage } from "./storefront-model.ts";

/**
 * تب پرسش‌وپاسخ Shopeiva با دادهٔ Published زنده؛ بدون sampleQA و بدون like جعلی.
 */
export function StorefrontPdpQa({ detail }: { detail: StorefrontProductDetailPage }) {
  const [items, setItems] = useState<StorefrontQaItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [body, setBody] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    setLoading(true);
    void loadStorefrontQuestions(detail.slug)
      .then((page) => {
        setItems(page?.items ?? []);
        setTotalCount(page?.totalCount ?? 0);
      })
      .finally(() => setLoading(false));
  }, [detail.slug]);

  return (
    <div className="space-y-6" data-testid="pdp-qa">
      <div className="flex items-center justify-between gap-3">
        <h3 className="text-lg font-bold text-gray-900">پرسش و پاسخ</h3>
        {!showForm ? (
          <button
            type="button"
            onClick={() => setShowForm(true)}
            className="px-4 py-2 bg-[#2563EB] text-white rounded-xl text-sm font-bold hover:bg-[#1d4ed8] transition-all flex items-center gap-2"
          >
            <MessageCircle className="w-4 h-4" />
            پرسش جدید
          </button>
        ) : null}
      </div>

      {showForm ? (
        <div className="bg-gray-50 rounded-xl p-4 border border-gray-200 space-y-3">
          <label className="text-sm font-medium text-gray-700">سوال شما</label>
          <textarea
            value={body}
            onChange={(event) => setBody(event.target.value)}
            rows={3}
            maxLength={2000}
            placeholder="سوال خود را بنویسید..."
            className="w-full mt-1 px-4 py-2 bg-white rounded-xl text-sm border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#2563EB] resize-none"
          />
          <div className="flex gap-2">
            <button
              type="button"
              disabled={busy || body.trim().length < 5}
              onClick={() => {
                void (async () => {
                  setBusy(true);
                  setMessage(null);
                  try {
                    await submitStorefrontQuestion(detail.productId, body.trim());
                    setBody("");
                    setShowForm(false);
                    setMessage("پرسش شما ثبت شد و پس از بررسی نمایش داده می‌شود.");
                  } catch (cause) {
                    setMessage(cause instanceof Error ? cause.message : "ثبت پرسش انجام نشد.");
                  } finally {
                    setBusy(false);
                  }
                })();
              }}
              className="px-4 py-2 bg-[#2563EB] text-white rounded-xl text-sm font-bold disabled:opacity-60 flex items-center gap-2"
            >
              <Send className="w-4 h-4" /> ارسال پرسش
            </button>
            <button type="button" onClick={() => setShowForm(false)} className="px-4 py-2 rounded-xl border border-gray-200 text-sm">
              انصراف
            </button>
          </div>
        </div>
      ) : null}

      {message ? <p className="text-sm text-emerald-700" role="status">{message}</p> : null}
      {loading ? <p className="text-gray-500">در حال دریافت پرسش‌ها…</p> : null}
      {!loading && items.length === 0 ? (
        <p className="text-sm text-gray-500">هنوز پرسش منتشرشده‌ای برای این کالا وجود ندارد.</p>
      ) : null}
      {!loading && totalCount > 0 ? <p className="text-xs text-gray-400">{totalCount.toLocaleString("fa-IR")} پرسش منتشرشده</p> : null}

      <div className="space-y-4">
        {items.map((item) => (
          <article key={item.questionId} className="rounded-xl border border-gray-200 p-4 space-y-3">
            <div className="flex items-start gap-3">
              <div className="w-9 h-9 rounded-full bg-gray-100 flex items-center justify-center shrink-0">
                <User className="w-4 h-4 text-gray-500" />
              </div>
              <div className="min-w-0">
                <p className="text-sm font-bold text-gray-800">{item.authorDisplayName}</p>
                <p className="text-sm text-gray-700 leading-7 mt-1">{item.body}</p>
              </div>
            </div>
            {item.answerBody ? (
              <div className="mr-12 rounded-xl bg-blue-50/60 border border-blue-100 p-3">
                <p className="text-xs font-bold text-[#2563EB] flex items-center gap-1">
                  <CheckCircle className="w-3.5 h-3.5" />
                  {item.answerAuthorDisplayName ?? "پاسخ فروشگاه"}
                </p>
                <p className="text-sm text-gray-700 leading-7 mt-1">{item.answerBody}</p>
              </div>
            ) : (
              <p className="mr-12 text-xs text-gray-400">در انتظار پاسخ منتشرشده</p>
            )}
          </article>
        ))}
      </div>
    </div>
  );
}
