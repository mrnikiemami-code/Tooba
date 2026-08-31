"use client";

import type { ICellRendererParams } from "ag-grid-community";
import { AppGridTruncatedCell } from "../../design-system/app-data-grid/app-grid-cells";

/** نام‌های دستهٔ نمایشی — ویرگول‌جداشده با tooltip هنگام overflow. */
export function AdditionalCategoryListCell({
  params,
  names,
}: {
  params: ICellRendererParams;
  names: string[];
}) {
  const clean = names.map((n) => n.trim()).filter((n) => n.length > 0);
  if (clean.length === 0) {
    return (
      <span className="text-muted" data-testid="additional-category-empty">
        —
      </span>
    );
  }
  const text = clean.join("، ");
  return <AppGridTruncatedCell params={params} text={text} />;
}
