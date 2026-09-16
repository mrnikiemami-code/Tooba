"use client";

import { useCallback, useEffect, useMemo, useState, type FormEvent, type MouseEvent } from "react";
import {
  fashionPreviewErrorMessage,
  INDUSTRY_STORE_ORIGIN,
  industryDemoOrigin,
  isBatchATemplateKey,
  loadIndustryStorePreview,
  loadIndustryTemplatePreview,
  type BatchATemplateKey,
  type IndustryStorePreviewFixture,
} from "../../../lib/storefront-composition/industry-template-preview.ts";
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
  fullPage?: boolean;
  storeFixture?: IndustryStorePreviewFixture;
};

/**
 * Batch A industry template preview — shared Storefront renderer (LOCK-SF-345).
 */
export function IndustryTemplatePreviewView({
  context: previewContext,
  fullPage = false,
  storeFixture = null,
}: Props) {
  const templateKey = previewContext.templateKey;
  if (!isBatchATemplateKey(templateKey)) {
    return (
      <div data-testid="industry-template-preview-error" className="p-6 text-sm text-red-700">
        قالب Batch A نامعتبر است.
      </div>
    );
  }

  return (
    <IndustryTemplatePreviewBody
      templateKey={templateKey}
      previewContext={previewContext}
      fullPage={fullPage}
      storeFixture={storeFixture}
    />
  );
}

function IndustryTemplatePreviewBody({
  templateKey,
  previewContext,
  fullPage,
  storeFixture,
}: {
  templateKey: BatchATemplateKey;
  previewContext: TemplatePreviewContext;
  fullPage: boolean;
  storeFixture: IndustryStorePreviewFixture;
}) {
  const source = previewContext.sourceMode;
  const localeCode = previewContext.locale || "fa-IR";
  const uiLocale = localeCode.toLowerCase().startsWith("en") ? "en" : "fa";
  const [context, setContext] = useState<LandingRenderContext | null>(null);
  const [page, setPage] = useState<StorefrontLandingPage | null>(null);
  const [origin, setOrigin] = useState(
    source === "store" ? INDUSTRY_STORE_ORIGIN : industryDemoOrigin(templateKey),
  );
  const [error, setError] = useState<string | null>(null);
  const [purityOk, setPurityOk] = useState(source === "sample");

  useEffect(() => {
    let cancelled = false;
    setError(null);
    setContext(null);
    setPage(null);

    const load = source === "store"
      ? loadIndustryStorePreview(templateKey, uiLocale, { fixture: storeFixture })
      : loadIndustryTemplatePreview(templateKey);
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
  }, [source, uiLocale, localeCode, storeFixture, templateKey]);

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
      <div data-testid="industry-template-preview-error" className="p-6 text-sm text-red-700" lang={uiLocale}>
        {error}
      </div>
    );
  }

  if (!context || !page) {
    return (
      <div data-testid="industry-template-preview-loading" className="p-6 text-sm text-slate-600" lang={uiLocale}>
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
      data-testid="industry-template-preview"
      data-demo-origin={origin}
      data-preview-source={source}
      data-preview-locale={localeCode}
      data-preview-device={previewContext.deviceMode}
      data-preview-safe={fullPage ? undefined : "1"}
      data-preview-full={fullPage ? "1" : undefined}
      data-template-purity={source === "sample" ? (purityOk ? "pure" : "mixed") : "store"}
      data-template-key={templateKey}
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
