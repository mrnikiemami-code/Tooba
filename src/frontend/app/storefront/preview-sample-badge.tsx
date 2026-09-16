"use client";

import { previewSampleBadgeLabel } from "../../lib/storefront-composition/preview-fake-locale.ts";

/** Compact preview-only chip; does not change card geometry. */
export function PreviewSampleBadge({
  locale = "fa",
  className = "absolute top-2 left-2",
}: {
  locale?: string;
  className?: string;
}) {
  return (
    <span
      className={`pointer-events-none z-20 rounded bg-black/55 px-1.5 py-0.5 text-[9px] font-bold leading-none text-white ${className}`}
      data-preview-fake-badge="true"
    >
      {previewSampleBadgeLabel(locale)}
    </span>
  );
}
