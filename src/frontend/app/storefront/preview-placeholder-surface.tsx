"use client";

import {
  PREVIEW_PLACEHOLDER_MARK,
  previewPlaceholderCopy,
  type PreviewPlaceholderKind,
} from "../../lib/storefront-composition/template-preview-context.ts";

/** Preview-only geometry filler — never persisted, never on published storefront. */
export function PreviewPlaceholderSurface({
  kind,
  locale = "fa",
  className = "",
  slots = 1,
  aspectClass = "aspect-[21/9]",
}: {
  kind: PreviewPlaceholderKind;
  locale?: string;
  className?: string;
  slots?: number;
  aspectClass?: string;
}) {
  const copy = previewPlaceholderCopy(kind, locale);
  const count = Math.max(1, Math.min(8, slots));
  return (
    <div
      className={`w-full px-2 sm:px-4 py-6 ${className}`}
      data-testid={`preview-placeholder-${kind}`}
      data-preview-placeholder={PREVIEW_PLACEHOLDER_MARK}
      data-preview-placeholder-kind={kind}
    >
      <div className={`grid gap-4 ${count > 1 ? "grid-cols-1 sm:grid-cols-2" : "grid-cols-1"}`}>
        {Array.from({ length: count }, (_, index) => (
          <div
            key={`${kind}-${index}`}
            className={`flex flex-col items-center justify-center gap-2 rounded-3xl border-2 border-dashed border-slate-300 bg-slate-50 text-slate-600 ${aspectClass}`}
            data-preview-placeholder-slot={String(index)}
          >
            <p className="text-sm font-black">{copy.title}</p>
            <p className="max-w-sm px-4 text-center text-xs font-medium leading-relaxed">{copy.body}</p>
          </div>
        ))}
      </div>
    </div>
  );
}
