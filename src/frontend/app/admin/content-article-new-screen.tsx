"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { toast } from "react-toastify";
import { prepareAdminDevActor } from "./admin-api.ts";
import { fetchActiveContentAuthors } from "./content-author-api.ts";
import { createAdminArticle } from "../content/content-api.ts";

const LANGUAGE_OPTIONS = [
  { code: "fa-IR", label: "فارسی", title: "پیش‌نویس جدید", excerpt: "چکیدهٔ مقاله" },
  { code: "en-US", label: "English", title: "New draft article", excerpt: "Article excerpt" },
] as const;

/** ایجاد زبان‌محور مقاله — انتخاب زبان سپس ورود به workspace. */
export function ContentArticleNewScreen() {
  const router = useRouter();
  const [locale, setLocale] = useState<(typeof LANGUAGE_OPTIONS)[number]["code"]>("fa-IR");
  const [busy, setBusy] = useState(false);
  const [authorsReady, setAuthorsReady] = useState(false);
  const [defaultAuthorId, setDefaultAuthorId] = useState<string | null>(null);

  useEffect(() => {
    void prepareAdminDevActor().then(() =>
      fetchActiveContentAuthors().then((result) => {
        if (result.state === "ok" && result.data?.length) {
          setDefaultAuthorId(result.data[0]!.authorId);
        }
        setAuthorsReady(true);
      }),
    );
  }, []);

  const startDraft = useCallback(async () => {
    if (!defaultAuthorId) {
      toast.error("حداقل یک نویسندهٔ فعال لازم است.");
      return;
    }
    const option = LANGUAGE_OPTIONS.find((row) => row.code === locale) ?? LANGUAGE_OPTIONS[0];
    const slug = `draft-${Date.now().toString(36)}`;
    setBusy(true);
    const result = await createAdminArticle({
      slug,
      title: option.title,
      excerpt: option.excerpt,
      body: "",
      authorDisplayName: "",
      authorId: defaultAuthorId,
      locale: option.code,
    });
    setBusy(false);
    if (!result.ok || !result.article) {
      toast.error(result.message ?? "ایجاد پیش‌نویس ناموفق بود");
      return;
    }
    router.push(`/admin/content/articles/${result.article.articleId}`);
  }, [defaultAuthorId, locale, router]);

  return (
    <main className="mx-auto w-full max-w-lg p-4" data-testid="content-article-new">
      <Link href="/admin/content" className="text-sm text-[#2563EB] underline">
        بازگشت به فهرست مقالات
      </Link>
      <h1 className="mt-4 text-xl font-bold">مقالهٔ جدید</h1>
      <p className="mt-1 text-sm text-muted">ابتدا زبان محتوا را انتخاب کنید؛ هر مقاله یک موجودیت مستقل در یک زبان است.</p>

      <div className="mt-6 space-y-3">
        {LANGUAGE_OPTIONS.map((option) => (
          <label
            key={option.code}
            className={`flex cursor-pointer items-center gap-3 rounded-xl border p-4 ${locale === option.code ? "border-[#2563EB] bg-blue-50" : "border-border"}`}
          >
            <input
              type="radio"
              name="article-locale"
              value={option.code}
              checked={locale === option.code}
              onChange={() => setLocale(option.code)}
            />
            <span className="font-medium">{option.label}</span>
            <span className="text-xs text-muted" dir="ltr">
              {option.code}
            </span>
          </label>
        ))}
      </div>

      {!authorsReady ? (
        <p className="mt-4 text-sm text-muted">در حال بارگذاری نویسندگان…</p>
      ) : !defaultAuthorId ? (
        <p className="mt-4 text-sm text-danger">
          نویسندهٔ فعالی یافت نشد. ابتدا از{" "}
          <Link href="/admin/content/authors" className="underline">
            نویسندگان
          </Link>{" "}
          یک نویسنده بسازید.
        </p>
      ) : (
        <button
          type="button"
          className="mt-6 inline-flex min-h-11 items-center rounded-xl bg-[#2563EB] px-5 text-sm font-semibold text-white disabled:opacity-50"
          disabled={busy}
          data-testid="content-article-new-start"
          onClick={() => void startDraft()}
        >
          شروع ویرایش
        </button>
      )}
    </main>
  );
}
