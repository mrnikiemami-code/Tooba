"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { mapAdminErrorMessage } from "./admin-error-map";
import { updateAdminProductCore } from "./host-client";
import { useProductWorkspaceDirtyRegistration } from "./product-workspace-dirty-context";
import type { ProductTranslationView, ProductWorkspaceView } from "./workspace-model";

const LOCALES = ["fa-IR", "en"] as const;

const LOCALE_DISPLAY: Record<string, string> = {
  "fa-IR": "فارسی",
  en: "English",
  "en-US": "English",
};

export type TranslationReadiness = "complete" | "partial" | "missing";

type TranslationDraft = {
  name: string;
  shortDescription: string;
  description: string;
  seoTitle: string;
  seoDescription: string;
};

function resolveTranslation(
  view: ProductWorkspaceView,
  locale: string,
): ProductTranslationView | undefined {
  const list = view.translations ?? [];
  return (
    list.find((t) => t.locale === locale) ??
    list.find((t) => t.locale.startsWith(locale.split("-")[0] ?? locale))
  );
}

function draftFromLocale(view: ProductWorkspaceView, locale: string): TranslationDraft {
  const existing = resolveTranslation(view, locale);
  if (locale === "fa-IR") {
    return {
      name: existing?.name || view.title || "",
      shortDescription: existing?.shortDescription || view.shortDescription || "",
      description: existing?.description || "",
      seoTitle: existing?.seoTitle || view.seo.seoTitleSeam || "",
      seoDescription: existing?.seoDescription || "",
    };
  }
  return {
    name: existing?.name || "",
    shortDescription: existing?.shortDescription || "",
    description: existing?.description || "",
    seoTitle: existing?.seoTitle || "",
    seoDescription: existing?.seoDescription || "",
  };
}

export function translationReadiness(draft: TranslationDraft): TranslationReadiness {
  const name = draft.name.trim();
  const short = draft.shortDescription.trim();
  const full = draft.description.trim();
  if (!name && !short && !full) return "missing";
  if (name && short && full) return "complete";
  return "partial";
}

function readinessLabel(state: TranslationReadiness): string {
  if (state === "complete") return "کامل";
  if (state === "partial") return "ناقص";
  return "ایجاد نشده";
}

function readinessClass(state: TranslationReadiness): string {
  if (state === "complete") return "rounded-full bg-emerald-50 px-2 py-0.5 text-[11px] font-medium text-emerald-800";
  if (state === "partial") return "rounded-full bg-amber-50 px-2 py-0.5 text-[11px] font-medium text-amber-900";
  return "rounded-full bg-secondary px-2 py-0.5 text-[11px] font-medium text-muted";
}

/**
 * تب ترجمه‌ها — ویرایش واقعی محتوای محلی با ایزولهٔ locale و بدون بازنویسی slug سراسری برای non-fa.
 */
export function ProductTranslationsPanel({
  view,
  canEdit,
  mode,
  viewScope,
  onSaved,
}: {
  view: ProductWorkspaceView;
  canEdit: boolean;
  mode: "view" | "edit";
  viewScope?: boolean;
  onSaved: (next: ProductWorkspaceView) => void;
}) {
  const [locale, setLocale] = useState<string>("fa-IR");
  const [draft, setDraft] = useState<TranslationDraft>(() => draftFromLocale(view, "fa-IR"));
  const [baseline, setBaseline] = useState<TranslationDraft>(() => draftFromLocale(view, "fa-IR"));
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const editable = canEdit && mode === "edit";
  const dirty = useMemo(
    () => JSON.stringify(draft) !== JSON.stringify(baseline),
    [draft, baseline],
  );

  const discard = useCallback(() => {
    const next = draftFromLocale(view, locale);
    setDraft(next);
    setBaseline(next);
    setError(null);
  }, [view, locale]);

  useProductWorkspaceDirtyRegistration("translations", dirty && editable, discard);

  useEffect(() => {
    const next = draftFromLocale(view, locale);
    setDraft(next);
    setBaseline(next);
    setError(null);
  }, [view, locale]);

  const rows = LOCALES.map((loc) => {
    const state = translationReadiness(draftFromLocale(view, loc));
    return { locale: loc, state };
  });

  async function onSave() {
    if (!editable || busy) return;
    if (!draft.name.trim()) {
      setError("نام محصول برای این زبان لازم است");
      return;
    }
    setBusy(true);
    setError(null);
    const result = await updateAdminProductCore(
      view.productId,
      {
        locale,
        title: draft.name.trim(),
        // slug سراسری است؛ برای non-fa مقدار فعلی حفظ می‌شود (Host هم محافظت می‌کند).
        slug: locale === "fa-IR" ? view.slug ?? view.seo.slugSeam ?? null : view.slug ?? view.seo.slugSeam ?? null,
        shortDescription: draft.shortDescription.trim() || null,
        description: draft.description.trim() || null,
        seoTitle: draft.seoTitle.trim() || null,
        seoDescription: draft.seoDescription.trim() || null,
        expectedUpdatedAt: view.catalogUpdatedAt,
      },
      viewScope,
    );
    setBusy(false);
    if (!result.ok) {
      setError(mapAdminErrorMessage(result.errorCode));
      return;
    }
    onSaved(result.view);
    const synced = draftFromLocale(result.view, locale);
    setDraft(synced);
    setBaseline(synced);
  }

  const dir = locale.startsWith("fa") ? "rtl" : "ltr";

  return (
    <div className="space-y-4" data-testid="product-translations-panel">
      <div className="rounded-ds border border-border bg-surface p-4">
        <p className="font-semibold">ترجمه‌ها</p>
        <p className="mt-1 text-sm text-muted">
          محتوای محلی نام، خلاصه و توضیح را برای هر زبان ویرایش کنید. نشانی صفحه (slug) سراسری است و از تب عمومی مدیریت می‌شود.
        </p>
        <div className="mt-4 flex flex-wrap gap-2" data-testid="product-locale-switcher">
          {rows.map(({ locale: loc, state }) => {
            const active = locale === loc;
            return (
              <button
                key={loc}
                type="button"
                className={
                  active
                    ? "inline-flex min-h-10 items-center gap-2 rounded-ds bg-primary px-3 text-sm text-primary-foreground"
                    : "inline-flex min-h-10 items-center gap-2 rounded-ds border border-border px-3 text-sm hover:bg-secondary"
                }
                data-testid={`translation-locale-${loc}`}
                onClick={() => {
                  if (dirty && editable) {
                    if (!window.confirm("تغییرات ذخیره‌نشده این زبان از بین می‌رود. ادامه؟")) return;
                  }
                  setLocale(loc);
                }}
              >
                {LOCALE_DISPLAY[loc] ?? loc}
                <span className={readinessClass(state)}>{readinessLabel(state)}</span>
              </button>
            );
          })}
        </div>
      </div>

      <div className="rounded-ds border border-border bg-surface p-4" data-testid="product-translation-editor" dir={dir}>
        <div className="flex flex-wrap items-center justify-between gap-2">
          <p className="text-sm font-medium text-muted">{LOCALE_DISPLAY[locale] ?? locale}</p>
          <span className={readinessClass(translationReadiness(draft))}>
            {readinessLabel(translationReadiness(draft))}
          </span>
        </div>

        {error ? (
          <p className="mt-3 rounded-ds border border-danger/30 bg-danger/5 px-3 py-2 text-sm text-danger" role="alert">
            {error}
          </p>
        ) : null}

        {!editable ? (
          <div className="mt-3 grid gap-3 sm:grid-cols-2">
            <ReadCard label="نام" value={draft.name || "—"} />
            <ReadCard
              label="نامک سراسری"
              value={view.slug ?? view.seo.slugSeam ?? "—"}
              ltr
              hint="slug برای همهٔ زبان‌ها یکسان است"
            />
            <ReadCard label="خلاصه کوتاه" value={draft.shortDescription || "—"} />
            <ReadCard label="توضیح کامل" value={draft.description || "—"} />
            <ReadCard label="عنوان SEO" value={draft.seoTitle || "—"} />
            <ReadCard label="توضیح SEO" value={draft.seoDescription || "—"} />
          </div>
        ) : (
          <div className="mt-3 grid gap-4">
            <label className="block text-sm font-medium">
              نام محصول
              <input
                className="mt-2 min-h-11 w-full rounded-ds border border-border bg-surface px-3 text-base"
                value={draft.name}
                data-testid="translation-edit-name"
                onChange={(event) => setDraft({ ...draft, name: event.target.value })}
              />
            </label>
            <label className="block text-sm font-medium">
              خلاصه کوتاه
              <textarea
                className="mt-2 min-h-20 w-full rounded-ds border border-border bg-surface px-3 py-2 text-base"
                value={draft.shortDescription}
                data-testid="translation-edit-short"
                onChange={(event) => setDraft({ ...draft, shortDescription: event.target.value })}
              />
            </label>
            <label className="block text-sm font-medium">
              توضیح کامل
              <textarea
                className="mt-2 min-h-36 w-full rounded-ds border border-border bg-surface px-3 py-2 text-base"
                value={draft.description}
                data-testid="translation-edit-description"
                onChange={(event) => setDraft({ ...draft, description: event.target.value })}
              />
            </label>
            <div className="grid gap-4 sm:grid-cols-2">
              <label className="block text-sm font-medium">
                عنوان SEO
                <input
                  className="mt-2 min-h-11 w-full rounded-ds border border-border bg-surface px-3 text-base"
                  value={draft.seoTitle}
                  data-testid="translation-edit-seo-title"
                  onChange={(event) => setDraft({ ...draft, seoTitle: event.target.value })}
                />
              </label>
              <label className="block text-sm font-medium">
                توضیح SEO
                <textarea
                  className="mt-2 min-h-20 w-full rounded-ds border border-border bg-surface px-3 py-2 text-base"
                  value={draft.seoDescription}
                  data-testid="translation-edit-seo-description"
                  onChange={(event) => setDraft({ ...draft, seoDescription: event.target.value })}
                />
              </label>
            </div>
            <p className="text-xs text-muted" dir="rtl">
              نامک سراسری: <span dir="ltr">{view.slug ?? view.seo.slugSeam ?? "—"}</span> — از تب عمومی ویرایش می‌شود.
            </p>
            <div className="flex flex-wrap gap-2">
              <button
                type="button"
                disabled={busy || !dirty}
                className="min-h-11 rounded-ds bg-primary px-4 text-sm font-medium text-primary-foreground disabled:opacity-50"
                data-testid="translation-save"
                onClick={() => void onSave()}
              >
                ذخیره ترجمه
              </button>
              <button
                type="button"
                disabled={busy || !dirty}
                className="min-h-11 rounded-ds border border-border px-4 text-sm hover:bg-secondary disabled:opacity-50"
                data-testid="translation-discard"
                onClick={discard}
              >
                انصراف از تغییرات
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

function ReadCard({
  label,
  value,
  ltr,
  hint,
}: {
  label: string;
  value: string;
  ltr?: boolean;
  hint?: string;
}) {
  return (
    <div className="rounded-ds border border-border bg-surface p-3">
      <p className="text-sm text-muted">{label}</p>
      <p className="mt-1 whitespace-pre-wrap text-base font-semibold" dir={ltr ? "ltr" : undefined}>
        {value}
      </p>
      {hint ? <p className="mt-1 text-xs text-muted">{hint}</p> : null}
    </div>
  );
}
