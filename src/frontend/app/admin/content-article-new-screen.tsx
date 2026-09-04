"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { toast } from "react-toastify";
import { prepareAdminDevActor } from "./admin-api.ts";
import { mapAdminErrorMessage, normalizeAdminClientError } from "./admin-error-map.ts";
import { loadAdminLanguages } from "./language-api.ts";
import { createAdminArticle } from "../content/content-api.ts";
import type { SupportedLocaleDefinition } from "../../lib/i18n/supported-locales.ts";

function languageLabel(lang: SupportedLocaleDefinition): string {
  return lang.nativeName?.trim() || lang.displayName?.trim() || lang.code;
}

function draftDefaults(code: string): { title: string; excerpt: string } {
  if (code.toLowerCase().startsWith("fa")) {
    return { title: "پیش‌نویس جدید", excerpt: "چکیدهٔ مقاله" };
  }
  return { title: "New draft article", excerpt: "Article excerpt" };
}

/** ایجاد پیش‌نویس بدون نویسنده — زبان از Admin API و ?language=. */
export function ContentArticleNewScreen() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [languages, setLanguages] = useState<SupportedLocaleDefinition[]>([]);
  const [locale, setLocale] = useState<string>("");
  const [busy, setBusy] = useState(false);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    void prepareAdminDevActor().then(() =>
      loadAdminLanguages().then((result) => {
        if (result.state !== "ok" || !result.data?.length) {
          setLanguages([]);
          setReady(true);
          return;
        }
        const active = result.data
          .filter((row) => row.active)
          .slice()
          .sort((a, b) => a.sortOrder - b.sortOrder || a.code.localeCompare(b.code));
        setLanguages(active);
        const param = searchParams.get("language")?.trim() ?? "";
        const defaultLang = active.find((row) => row.default) ?? active[0]!;
        const matched = active.find((row) => row.code === param);
        setLocale(matched?.code ?? defaultLang.code);
        setReady(true);
      }),
    );
  }, [searchParams]);

  const selected = useMemo(
    () => languages.find((row) => row.code === locale) ?? languages[0] ?? null,
    [languages, locale],
  );

  const startDraft = useCallback(async () => {
    if (!selected) {
      toast.error(mapAdminErrorMessage("localization.language.inactive", "fa"));
      return;
    }
    const defaults = draftDefaults(selected.code);
    const slug = `draft-${Date.now().toString(36)}`;
    setBusy(true);
    const result = await createAdminArticle({
      slug,
      title: defaults.title,
      excerpt: defaults.excerpt,
      body: "",
      authorDisplayName: "",
      authorId: null,
      locale: selected.code,
    });
    setBusy(false);
    if (!result.ok || !result.article) {
      toast.error(
        mapAdminErrorMessage(
          result.message ?? "content.article.create_failed",
          "fa",
        ),
      );
      return;
    }
    router.push(`/admin/content/articles/${result.article.articleId}`);
  }, [router, selected]);

  return (
    <main className="mx-auto w-full max-w-lg p-4" data-testid="content-article-new">
      <Link href="/admin/content" className="text-sm text-[#2563EB] underline">
        بازگشت به فهرست مقالات
      </Link>
      <h1 className="mt-4 text-xl font-bold">مقالهٔ جدید</h1>
      <p className="mt-1 text-sm text-muted">
        ابتدا زبان محتوا را انتخاب کنید؛ پیش‌نویس بدون نویسنده ساخته می‌شود و پس از آن وارد workspace می‌شوید.
      </p>

      {!ready ? (
        <p className="mt-4 text-sm text-muted">در حال بارگذاری زبان‌ها…</p>
      ) : languages.length === 0 ? (
        <p className="mt-4 text-sm text-danger">
          {normalizeAdminClientError({ errorCode: "localization.language.inactive" }, 400, "fa")}
        </p>
      ) : (
        <>
          <div className="mt-6 space-y-3">
            {languages.map((option) => (
              <label
                key={option.code}
                className={`flex cursor-pointer items-center gap-3 rounded-xl border p-4 ${
                  locale === option.code ? "border-[#2563EB] bg-blue-50" : "border-border"
                }`}
              >
                <input
                  type="radio"
                  name="article-locale"
                  value={option.code}
                  checked={locale === option.code}
                  onChange={() => setLocale(option.code)}
                />
                <span className="font-medium">{languageLabel(option)}</span>
                <span className="text-xs text-muted" dir="ltr">
                  {option.code}
                </span>
              </label>
            ))}
          </div>

          <button
            type="button"
            className="mt-6 inline-flex min-h-11 items-center rounded-xl bg-[#2563EB] px-5 text-sm font-semibold text-white disabled:opacity-50"
            disabled={busy || !selected}
            data-testid="content-article-new-start"
            onClick={() => void startDraft()}
          >
            شروع ویرایش
          </button>
        </>
      )}
    </main>
  );
}
