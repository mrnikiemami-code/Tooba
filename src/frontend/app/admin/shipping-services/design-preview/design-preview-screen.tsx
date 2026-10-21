"use client";

/**
 * TB-SHIPPING-ADMIN-UI-PREVIEW-186 — صفحهٔ کامل پیش‌نمایش طراحی «سرویس ارسال».
 *
 * مرز پیش‌نمایش (Hard boundary):
 * - تنها منبع داده، fixture درون‌حافظه است؛ هیچ fetch/axios/API client، هیچ POST/PUT/PATCH/DELETE،
 *   هیچ localStorage/sessionStorage/cookie و هیچ تغییر schema/migration/config در این مسیر نیست.
 * - state فقط draft محلی است؛ «اعمال در پیش‌نمایش» صریحاً می‌گوید چیزی ذخیره نشده است.
 * - رفتار live صفحهٔ `/admin/shipping-services` دست‌نخورده و مستقل از این مسیر می‌ماند.
 * - زبان رابط (RTL/LTR) و زبان محتوا مستقل‌اند؛ ارز از بافت نمونهٔ فروشگاه می‌آید.
 */

import { useCallback, useMemo, useState } from "react";
import { ArrowRight } from "lucide-react";
import {
  PREVIEW_CONTENT_LANGUAGE_IDS,
  clonePreviewMethod,
  previewDirection,
  previewNewMethod,
  previewNextId,
  previewTextFor,
  type PreviewLocale,
  type PreviewMethod,
  type PreviewOwnerKind,
} from "./preview-model.ts";
import { PREVIEW_METHOD_FIXTURES } from "./preview-fixtures.ts";
import { previewMessages } from "./preview-i18n.ts";
import { PreviewMethodList } from "./preview-method-list.tsx";
import { PreviewGeneralTab } from "./preview-general-tab.tsx";
import { PreviewRegionsTab } from "./preview-regions-tab.tsx";

type PreviewTabId = "general" | "regions";
type PreviewMode = "view" | "edit" | "create";

type EditorState = {
  methodId: string;
  mode: PreviewMode;
};

const BASELINE: ReadonlyMap<string, PreviewMethod> = new Map(
  PREVIEW_METHOD_FIXTURES.map((method) => [method.id, clonePreviewMethod(method)]),
);

function sameMethod(a: PreviewMethod | undefined, b: PreviewMethod | undefined): boolean {
  if (!a || !b) return a === b;
  return JSON.stringify(a) === JSON.stringify(b);
}

export function ShippingDesignPreviewScreen() {
  const [locale, setLocale] = useState<PreviewLocale>("fa");
  const [contentLanguageId, setContentLanguageId] = useState<string>(PREVIEW_CONTENT_LANGUAGE_IDS[0]);
  const [ownerKind, setOwnerKind] = useState<PreviewOwnerKind>("marketplace");
  const [methods, setMethods] = useState<readonly PreviewMethod[]>(() =>
    PREVIEW_METHOD_FIXTURES.map(clonePreviewMethod),
  );
  const [editor, setEditor] = useState<EditorState | null>(null);
  const [draft, setDraft] = useState<PreviewMethod | null>(null);
  const [tab, setTab] = useState<PreviewTabId>("general");
  const [notice, setNotice] = useState<string | undefined>(undefined);

  const messages = useMemo(() => previewMessages(locale), [locale]);
  const direction = previewDirection(locale);

  const ownerMethods = useMemo(
    () => methods.filter((method) => method.ownerKind === ownerKind),
    [methods, ownerKind],
  );
  const applied = editor ? methods.find((method) => method.id === editor.methodId) : undefined;
  const dirty = editor !== null && draft !== null && !sameMethod(draft, applied);

  const openEditor = useCallback(
    (method: PreviewMethod, mode: PreviewMode) => {
      setNotice(undefined);
      setDraft(clonePreviewMethod(method));
      setEditor({ methodId: method.id, mode });
      setTab("general");
    },
    [],
  );

  const openNew = useCallback(() => {
    const fresh = previewNewMethod(previewNextId("method"), ownerKind);
    setNotice(undefined);
    setDraft(fresh);
    setEditor({ methodId: fresh.id, mode: "create" });
    setTab("general");
  }, [ownerKind]);

  const closeEditor = useCallback(() => {
    if (dirty && typeof window !== "undefined" && !window.confirm(messages.discardAndGoBack)) return;
    setEditor(null);
    setDraft(null);
  }, [dirty, messages.discardAndGoBack]);

  /** فقط draft محلی؛ هیچ درخواستی به سرور نمی‌رود و پیام صریح همان را می‌گوید. */
  const applyDraft = useCallback(() => {
    if (!draft || !editor) return;
    setMethods((current) => {
      const exists = current.some((method) => method.id === draft.id);
      return exists
        ? current.map((method) => (method.id === draft.id ? clonePreviewMethod(draft) : method))
        : [...current, clonePreviewMethod(draft)];
    });
    if (editor.mode === "create") setEditor({ methodId: draft.id, mode: "edit" });
    setNotice(messages.appliedToPreview);
  }, [draft, editor, messages.appliedToPreview]);

  const resetDraft = useCallback(() => {
    if (!draft) return;
    const baseline = BASELINE.get(draft.id);
    if (!baseline) return;
    setDraft(clonePreviewMethod(baseline));
    setMethods((current) => current.map((method) => (method.id === draft.id ? clonePreviewMethod(baseline) : method)));
    setNotice(messages.resetDone);
  }, [draft, messages.resetDone]);

  const canReset = draft !== null && BASELINE.has(draft.id);

  return (
    <div dir={direction} lang={locale} className="space-y-4" data-testid="shipping-design-preview">
      <header className="space-y-2">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex flex-wrap items-center gap-2">
            <h1 className="text-xl font-black text-gray-900">{messages.pageTitle}</h1>
            <span className="rounded-full bg-primary px-2.5 py-0.5 text-[11px] font-bold text-primary-foreground">
              {messages.previewBadge}
            </span>
            <span className="rounded-full bg-secondary px-2.5 py-0.5 text-[11px] font-bold text-gray-700">
              {messages.sampleBadge}
            </span>
          </div>

          {/* زبان رابط مستقل از زبان محتوا */}
          <div className="flex items-center gap-2" data-testid="shipping-preview-interface-language">
            <span className="text-[11px] font-bold text-gray-600">{messages.interfaceLanguage}</span>
            <div role="group" aria-label={messages.interfaceLanguage} className="inline-flex overflow-hidden rounded-lg border border-border">
              {(["fa", "en"] as PreviewLocale[]).map((option) => (
                <button
                  key={option}
                  type="button"
                  aria-pressed={locale === option}
                  data-testid={`shipping-preview-iface-${option}`}
                  className={
                    locale === option
                      ? "min-h-8 bg-primary px-3 text-xs font-bold text-primary-foreground"
                      : "min-h-8 bg-surface px-3 text-xs font-bold text-gray-700"
                  }
                  onClick={() => setLocale(option)}
                >
                  {option === "fa" ? "فارسی" : "EN"}
                </button>
              ))}
            </div>
          </div>
        </div>

        <p
          role="note"
          className="rounded-xl border border-amber-200 bg-amber-50 px-3 py-2 text-xs font-semibold text-amber-800"
          data-testid="shipping-preview-banner"
        >
          {messages.previewBanner}
        </p>
        <p className="text-[11px] text-gray-500" data-testid="shipping-preview-boundary">
          {messages.boundaryNote}
        </p>

        <div className="flex flex-wrap items-end justify-between gap-3 rounded-xl border border-border bg-surface px-3 py-2">
          <div className="flex flex-wrap items-end gap-3">
            <label className="text-xs">
              <span className="block font-bold text-gray-900">{messages.ownerLabel}</span>
              <select
                className="mt-1 min-h-9 rounded-lg border border-border bg-surface px-2 text-xs"
                value={ownerKind}
                data-testid="shipping-preview-owner"
                onChange={(event) => {
                  setOwnerKind(event.target.value as PreviewOwnerKind);
                  setEditor(null);
                  setDraft(null);
                }}
              >
                <option value="marketplace">{messages.ownerMarketplace}</option>
                <option value="seller">{messages.ownerSeller}</option>
              </select>
            </label>
            <p className="max-w-md pb-1 text-[11px] text-gray-500">{messages.ownerNote}</p>
          </div>
          <p className="pb-1 text-[11px] text-gray-500">
            <span className="font-bold text-gray-700">{messages.currencyLabel}: </span>
            {messages.currencyNote}
          </p>
        </div>
      </header>

      {notice ? (
        <p
          role="status"
          className="rounded-xl border border-emerald-200 bg-emerald-50 px-3 py-2 text-xs font-semibold text-emerald-800"
          data-testid="shipping-preview-notice"
        >
          {notice}
        </p>
      ) : null}

      {editor && draft ? (
        <article className="space-y-3" data-testid="shipping-preview-editor">
          <div className="sticky top-0 z-10 flex flex-wrap items-center justify-between gap-2 rounded-xl border border-border bg-surface-elevated px-3 py-2 shadow-sm">
            <div className="flex min-w-0 items-center gap-2">
              <button
                type="button"
                className="inline-flex min-h-9 items-center gap-1 rounded-lg border border-border px-2.5 text-xs font-bold text-gray-700"
                data-testid="shipping-preview-back"
                onClick={closeEditor}
              >
                <ArrowRight className="size-3.5" aria-hidden />
                {messages.backToList}
              </button>
              <div className="min-w-0">
                <h2 className="truncate text-sm font-black text-gray-900">
                  {previewTextFor(draft.names, contentLanguageId).trim() || messages.editorNewTitle}
                </h2>
                <p className="text-[11px] text-gray-500">
                  {editor.mode === "view" ? messages.viewReadOnly : messages.sampleNotice}
                  {dirty ? ` · ${messages.unsavedChanges}` : ""}
                </p>
              </div>
            </div>
            <div className="flex flex-wrap items-center gap-2">
              {canReset && editor.mode !== "view" ? (
                <button
                  type="button"
                  className="inline-flex min-h-9 items-center rounded-lg border border-border px-3 text-xs font-bold text-gray-700"
                  data-testid="shipping-preview-reset"
                  onClick={resetDraft}
                >
                  {messages.resetDraft}
                </button>
              ) : null}
              {editor.mode !== "view" ? (
                <button
                  type="button"
                  className="inline-flex min-h-9 items-center rounded-lg bg-primary px-3 text-xs font-bold text-primary-foreground"
                  data-testid="shipping-preview-apply"
                  onClick={applyDraft}
                >
                  {messages.saveDraft}
                </button>
              ) : null}
            </div>
          </div>

          <div role="tablist" aria-label={messages.pageTitle} className="flex flex-wrap gap-1 border-b border-border">
            {(
              [
                { id: "general" as PreviewTabId, label: messages.tabGeneral },
                { id: "regions" as PreviewTabId, label: messages.tabRegions },
              ]
            ).map((item) => (
              <button
                key={item.id}
                type="button"
                role="tab"
                aria-selected={tab === item.id}
                data-testid={`shipping-preview-tab-${item.id}`}
                className={
                  tab === item.id
                    ? "-mb-px min-h-10 border-b-2 border-primary px-3 text-sm font-black text-gray-900"
                    : "-mb-px min-h-10 border-b-2 border-transparent px-3 text-sm font-bold text-gray-500"
                }
                onClick={() => setTab(item.id)}
              >
                {item.label}
              </button>
            ))}
          </div>

          <div role="tabpanel">
            {tab === "general" ? (
              <PreviewGeneralTab
                draft={draft}
                readOnly={editor.mode === "view"}
                locale={locale}
                messages={messages}
                contentLanguageId={contentLanguageId}
                onContentLanguageChange={setContentLanguageId}
                onChange={setDraft}
              />
            ) : (
              <PreviewRegionsTab
                draft={draft}
                readOnly={editor.mode === "view"}
                locale={locale}
                messages={messages}
                contentLanguageId={contentLanguageId}
                onChange={setDraft}
              />
            )}
          </div>
        </article>
      ) : (
        <PreviewMethodList
          methods={ownerMethods}
          locale={locale}
          contentLanguageId={contentLanguageId}
          messages={messages}
          onView={(method) => openEditor(method, "view")}
          onEdit={(method) => openEditor(method, "edit")}
          onAdd={openNew}
        />
      )}
    </div>
  );
}
