"use client";

/**
 * Evidence-only page for TB-P10-T022-R13-R2 visual capture.
 * Uses production VariantLivePreview → shared-composition-renderer → ProductShowcaseEmblaRail.
 */

import { VariantLivePreview } from "../../../lib/storefront-composition/variant-live-preview.tsx";

const VARIANTS = [
  { key: "product.sunny", name: "سانی", shot: "sunny" },
  { key: "product.money", name: "مانی", shot: "money" },
  { key: "product.cinematic", name: "سینمایی", shot: "cinematic" },
  { key: "product.cinematic-plus", name: "سینمایی پلاس", shot: "cinematic-plus" },
  { key: "product.explorer", name: "کاشف", shot: "explorer" },
] as const;

export default function R13R2ProductShowcaseEvidencePage() {
  return (
    <main className="min-h-screen bg-section-surface p-4 md:p-8" dir="rtl" data-testid="r13r2-product-showcase-evidence">
      <h1 className="mb-6 text-xl font-black text-gray-900">شواهد نمایش کالا — R13-R2</h1>
      <div className="mb-8 grid gap-3 md:grid-cols-2 lg:grid-cols-3" data-testid="r13r2-picker-grid">
        {VARIANTS.map((item) => (
          <div
            key={item.key}
            className="rounded-2xl border border-gray-200 bg-surface p-4"
            data-variant-key={item.key}
            data-variant-name={item.name}
          >
            <div className="mb-2 text-sm font-bold text-gray-900">{item.name}</div>
            <div className="text-xs text-gray-500">{item.key}</div>
          </div>
        ))}
      </div>
      {VARIANTS.map((item) => (
        <section
          key={`preview-${item.key}`}
          className="mb-10 overflow-hidden rounded-2xl border border-gray-200 bg-surface"
          data-evidence-variant={item.shot}
          data-testid={`r13r2-preview-${item.shot}`}
        >
          <div className="border-b border-gray-100 px-4 py-3 text-sm font-bold">{item.name}</div>
          <VariantLivePreview variantKey={item.key} eager size="review" className="border-0 rounded-none" />
        </section>
      ))}
    </main>
  );
}
