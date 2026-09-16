"use client";

import { useCallback, useEffect, useState, type FormEvent, type MouseEvent } from "react";
import {
  FASHION_DEMO_ORIGIN,
  FASHION_STORE_ORIGIN,
  fashionPreviewErrorMessage,
  loadFashionStorePreview,
  loadFashionTemplatePreview,
  type FashionPreviewSource,
} from "../../../lib/storefront-composition/fashion-demo-preview.ts";
import { useLocale } from "../../../lib/i18n/locale-context.tsx";
import type {
  LandingRenderContext,
  StorefrontLandingPage,
} from "../../storefront/storefront-landing-api.ts";
import { StorefrontLandingSections } from "../../storefront/storefront-landing-sections.tsx";
import { StorefrontShell } from "../../storefront/storefront-shell.tsx";

type Props = {
  source: FashionPreviewSource;
  /** Full standalone page (not Admin iframe). */
  fullPage?: boolean;
};

/**
 * Fashion template preview — same composition for sample and store; only data source changes.
 */
export function FashionTemplatePreviewView({ source, fullPage = false }: Props) {
  const locale = useLocale();
  const [context, setContext] = useState<LandingRenderContext | null>(null);
  const [page, setPage] = useState<StorefrontLandingPage | null>(null);
  const [origin, setOrigin] = useState(source === "store" ? FASHION_STORE_ORIGIN : FASHION_DEMO_ORIGIN);
  const [error, setError] = useState<string | null>(null);
  const [purityOk, setPurityOk] = useState(source === "sample");

  useEffect(() => {
    let cancelled = false;
    setError(null);
    setContext(null);
    setPage(null);

    const load = source === "store" ? loadFashionStorePreview(locale) : loadFashionTemplatePreview();
    load
      .then((result) => {
        if (cancelled) return;
        setOrigin(result.origin);
        setContext(result.context);
        setPage(result.page);
        setPurityOk(result.kind === "sample" ? result.purity.isPure : false);
      })
      .catch((err: unknown) => {
        if (cancelled) return;
        const raw = err instanceof Error ? err.message : "preview.load.failed";
        setError(fashionPreviewErrorMessage(raw, locale));
      });

    return () => {
      cancelled = true;
    };
  }, [source, locale]);

  const blockPreviewSideEffects = useCallback((event: MouseEvent) => {
    if (fullPage) return;
    const target = event.target as HTMLElement | null;
    const anchor = target?.closest?.("a");
    if (anchor) {
      event.preventDefault();
      event.stopPropagation();
    }
  }, [fullPage]);

  const blockSubmit = useCallback((event: FormEvent) => {
    if (fullPage) return;
    event.preventDefault();
    event.stopPropagation();
  }, [fullPage]);

  if (error) {
    return (
      <div data-testid="fashion-template-preview-error" className="p-6 text-sm text-red-700" lang={locale}>
        {error}
      </div>
    );
  }

  if (!context || !page) {
    return (
      <div data-testid="fashion-template-preview-loading" className="p-6 text-sm text-slate-600" lang={locale}>
        {source === "sample"
          ? locale === "en"
            ? "Loading Template Catalog…"
            : "در حال بارگذاری Template Catalog…"
          : locale === "en"
            ? "Loading store data into the Fashion template…"
            : "در حال بارگذاری داده فروشگاه روی قالب پوشاک…"}
      </div>
    );
  }

  const banner =
    source === "sample"
      ? locale === "en"
        ? "Fashion template preview — persisted Template Catalog · no operational store mix"
        : "پیش‌نمایش قالب پوشاک — Template Catalog پایدار · بدون مخلوط فروشگاه عملیاتی"
      : locale === "en"
        ? "Same Fashion template — data from operational store Catalog"
        : "همان قالب پوشاک — داده از Catalog عملیاتی فروشگاه";

  return (
    <div
      data-testid="fashion-template-preview"
      data-demo-origin={origin}
      data-preview-source={source}
      data-preview-safe={fullPage ? undefined : "1"}
      data-preview-full={fullPage ? "1" : undefined}
      data-template-purity={source === "sample" ? (purityOk ? "pure" : "mixed") : "store"}
      data-template-key="fashion"
      lang={locale}
      onClickCapture={blockPreviewSideEffects}
      onSubmitCapture={blockSubmit}
    >
      <div
        className={`border-b px-4 py-2 text-center text-xs font-bold ${
          source === "sample" ? "border-amber-200 bg-amber-50 text-amber-900" : "border-sky-200 bg-sky-50 text-sky-900"
        }`}
      >
        {banner}
      </div>
      <StorefrontShell categories={context.categories} fullBleed={fullPage}>
        <StorefrontLandingSections page={page} context={context} preview={!fullPage} />
      </StorefrontShell>
    </div>
  );
}
