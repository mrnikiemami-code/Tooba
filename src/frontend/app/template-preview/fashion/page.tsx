"use client";

import { useCallback, useEffect, useState, type FormEvent, type MouseEvent } from "react";
import {
  FASHION_DEMO_ORIGIN,
  loadFashionTemplatePreview,
} from "../../../lib/storefront-composition/fashion-demo-preview.ts";
import type {
  LandingRenderContext,
  StorefrontLandingPage,
} from "../../storefront/storefront-landing-api.ts";
import { StorefrontLandingSections } from "../../storefront/storefront-landing-sections.tsx";
import { StorefrontShell } from "../../storefront/storefront-shell.tsx";

/**
 * Fashion template sample preview — Template Catalog persisted data only.
 */
export default function FashionTemplatePreviewPage() {
  const [context, setContext] = useState<LandingRenderContext | null>(null);
  const [page, setPage] = useState<StorefrontLandingPage | null>(null);
  const [origin, setOrigin] = useState(FASHION_DEMO_ORIGIN);
  const [error, setError] = useState<string | null>(null);
  const [purityOk, setPurityOk] = useState(false);

  useEffect(() => {
    let cancelled = false;
    loadFashionTemplatePreview()
      .then((result) => {
        if (cancelled) return;
        setContext(result.context);
        setPage(result.page);
        setOrigin(result.origin);
        setPurityOk(result.purity.isPure);
      })
      .catch((err: unknown) => {
        if (cancelled) return;
        setError(err instanceof Error ? err.message : "preview load failed");
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const blockPreviewSideEffects = useCallback((event: MouseEvent) => {
    const target = event.target as HTMLElement | null;
    const anchor = target?.closest?.("a");
    if (anchor) {
      event.preventDefault();
      event.stopPropagation();
    }
  }, []);

  const blockSubmit = useCallback((event: FormEvent) => {
    event.preventDefault();
    event.stopPropagation();
  }, []);

  if (error) {
    return (
      <div data-testid="fashion-template-preview-error" className="p-6 text-sm text-red-700">
        {error}
      </div>
    );
  }

  if (!context || !page) {
    return (
      <div data-testid="fashion-template-preview-loading" className="p-6 text-sm text-slate-600">
        در حال بارگذاری Template Catalog…
      </div>
    );
  }

  return (
    <div
      data-testid="fashion-template-preview"
      data-demo-origin={origin}
      data-preview-safe="1"
      data-template-purity={purityOk ? "pure" : "mixed"}
      onClickCapture={blockPreviewSideEffects}
      onSubmitCapture={blockSubmit}
    >
      <div className="border-b border-amber-200 bg-amber-50 px-4 py-2 text-center text-xs font-bold text-amber-900">
        پیش‌نمایش قالب پوشاک — Template Catalog پایدار · بدون مخلوط فروشگاه عملیاتی
      </div>
      <StorefrontShell categories={context.categories}>
        <StorefrontLandingSections page={page} context={context} preview />
      </StorefrontShell>
    </div>
  );
}
