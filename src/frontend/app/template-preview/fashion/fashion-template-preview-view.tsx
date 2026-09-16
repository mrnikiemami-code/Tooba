"use client";

import { useCallback, useEffect, useMemo, useState, type FormEvent, type MouseEvent } from "react";
import {
  FASHION_DEMO_ORIGIN,
  FASHION_STORE_ORIGIN,
  fashionPreviewErrorMessage,
  loadFashionStorePreview,
  loadFashionTemplatePreview,
  type FashionStorePreviewFixture,
} from "../../../lib/storefront-composition/fashion-demo-preview.ts";
import {
  previewStatusLabel,
  type TemplatePreviewContext,
} from "../../../lib/storefront-composition/template-preview-context.ts";
import type {
  LandingRenderContext,
  StorefrontLandingPage,
} from "../../storefront/storefront-landing-api.ts";
import { StorefrontLandingSections } from "../../storefront/storefront-landing-sections.tsx";
import { StorefrontShell } from "../../storefront/storefront-shell.tsx";

type Props = {
  context: TemplatePreviewContext;
  /** Full standalone page (not Admin iframe). */
  fullPage?: boolean;
  /** Preview-only visual fixture (R8-R1 evidence). */
  storeFixture?: FashionStorePreviewFixture;
};

/**
 * Fashion template preview — iframe and full-page share this view + loaders (LOCK-SF-300).
 */
export function FashionTemplatePreviewView({
  context: previewContext,
  fullPage = false,
  storeFixture = null,
}: Props) {
  const source = previewContext.sourceMode;
  const localeCode = previewContext.locale || "fa-IR";
  const uiLocale = localeCode.toLowerCase().startsWith("en") ? "en" : "fa";
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

    const load = source === "store"
      ? loadFashionStorePreview(uiLocale, { fixture: storeFixture })
      : loadFashionTemplatePreview();
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
        setError(fashionPreviewErrorMessage(raw, uiLocale));
      });

    return () => {
      cancelled = true;
    };
  }, [source, uiLocale, localeCode, storeFixture]);

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

  const banner = useMemo(() => previewStatusLabel(source, localeCode), [source, localeCode]);

  if (error) {
    return (
      <div data-testid="fashion-template-preview-error" className="p-6 text-sm text-red-700" lang={uiLocale}>
        {error}
      </div>
    );
  }

  if (!context || !page) {
    return (
      <div data-testid="fashion-template-preview-loading" className="p-6 text-sm text-slate-600" lang={uiLocale}>
        {source === "sample"
          ? uiLocale === "en"
            ? "Loading sample data…"
            : "در حال بارگذاری داده نمونه…"
          : uiLocale === "en"
            ? "Loading your store data…"
            : "در حال بارگذاری اطلاعات فروشگاه…"}
      </div>
    );
  }

  return (
    <div
      data-testid="fashion-template-preview"
      data-demo-origin={origin}
      data-preview-source={source}
      data-preview-locale={localeCode}
      data-preview-device={previewContext.deviceMode}
      data-preview-safe={fullPage ? undefined : "1"}
      data-preview-full={fullPage ? "1" : undefined}
      data-template-purity={source === "sample" ? (purityOk ? "pure" : "mixed") : "store"}
      data-template-key={previewContext.templateKey}
      lang={uiLocale}
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
        <StorefrontLandingSections
          page={page}
          context={context}
          preview
          previewSource={source}
          previewLocale={localeCode}
        />
      </StorefrontShell>
    </div>
  );
}
