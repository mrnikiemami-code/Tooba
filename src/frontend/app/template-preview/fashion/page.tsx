"use client";

import { useCallback, type FormEvent, type MouseEvent } from "react";
import {
  buildFashionDemoContext,
  buildFashionDemoPage,
  FASHION_DEMO_ORIGIN,
} from "../../../lib/storefront-composition/fashion-demo-preview.ts";
import { StorefrontLandingSections } from "../../storefront/storefront-landing-sections.tsx";
import { StorefrontShell } from "../../storefront/storefront-shell.tsx";

/**
 * Fashion template pilot preview — shared composition engine only.
 * Isolated demo context; clicks do not mutate cart/auth.
 */
export default function FashionTemplatePreviewPage() {
  const context = buildFashionDemoContext();
  const page = buildFashionDemoPage(context);

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

  return (
    <div
      data-testid="fashion-template-preview"
      data-demo-origin={FASHION_DEMO_ORIGIN}
      data-preview-safe="1"
      onClickCapture={blockPreviewSideEffects}
      onSubmitCapture={blockSubmit}
    >
      <div className="border-b border-amber-200 bg-amber-50 px-4 py-2 text-center text-xs font-bold text-amber-900">
        پیش‌نمایش آزمایشی قالب پوشاک — داده نمونه ایزوله · بدون ثبت در فروشگاه
      </div>
      <StorefrontShell categories={context.categories}>
        <StorefrontLandingSections page={page} context={context} preview />
      </StorefrontShell>
    </div>
  );
}
