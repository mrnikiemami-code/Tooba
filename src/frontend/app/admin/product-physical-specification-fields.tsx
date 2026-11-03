"use client";

import type { PhysicalSpecificationDraft } from "./product-physical-specification";

/**
 * Canonical product physical editor fields (g / cm). Layout and design tokens preserved
 * from the product workspace general tab; dimensions are never summed.
 */
export function ProductPhysicalSpecificationFields({
  draft,
  onChange,
  onDirty,
}: {
  draft: PhysicalSpecificationDraft;
  onChange: (next: PhysicalSpecificationDraft) => void;
  onDirty?: () => void;
}) {
  function patch(partial: Partial<PhysicalSpecificationDraft>) {
    onChange({ ...draft, ...partial });
    onDirty?.();
  }

  return (
    <div
      className="rounded-ds border border-border bg-secondary/20 p-3"
      data-testid="product-edit-physical-specification"
    >
      <p className="text-sm font-medium">مشخصات فیزیکی</p>
      <p className="mt-1 text-xs text-muted">
        خالی = نامشخص. وزن به گرم، ابعاد به سانتی‌متر. ابعاد محصولات با هم جمع نمی‌شوند.
      </p>
      <div className="mt-3 grid gap-3 sm:grid-cols-2">
        <label className="block text-sm font-medium">
          وزن (گرم)
          <input
            className="mt-2 min-h-11 w-full rounded-ds border border-border bg-surface px-3 text-base tabular-nums"
            value={draft.weightGrams}
            inputMode="decimal"
            dir="ltr"
            data-testid="product-edit-weight-grams"
            onChange={(event) => patch({ weightGrams: event.target.value })}
          />
        </label>
        <label className="block text-sm font-medium">
          طول (سانتی‌متر)
          <input
            className="mt-2 min-h-11 w-full rounded-ds border border-border bg-surface px-3 text-base tabular-nums"
            value={draft.lengthCentimeters}
            inputMode="decimal"
            dir="ltr"
            data-testid="product-edit-length-cm"
            onChange={(event) => patch({ lengthCentimeters: event.target.value })}
          />
        </label>
        <label className="block text-sm font-medium">
          عرض (سانتی‌متر)
          <input
            className="mt-2 min-h-11 w-full rounded-ds border border-border bg-surface px-3 text-base tabular-nums"
            value={draft.widthCentimeters}
            inputMode="decimal"
            dir="ltr"
            data-testid="product-edit-width-cm"
            onChange={(event) => patch({ widthCentimeters: event.target.value })}
          />
        </label>
        <label className="block text-sm font-medium">
          ارتفاع (سانتی‌متر)
          <input
            className="mt-2 min-h-11 w-full rounded-ds border border-border bg-surface px-3 text-base tabular-nums"
            value={draft.heightCentimeters}
            inputMode="decimal"
            dir="ltr"
            data-testid="product-edit-height-cm"
            onChange={(event) => patch({ heightCentimeters: event.target.value })}
          />
        </label>
      </div>
    </div>
  );
}
