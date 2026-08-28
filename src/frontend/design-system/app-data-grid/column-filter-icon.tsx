"use client";

import { Filter } from "lucide-react";

/** آیکون فیلتر یکپارچه برای همهٔ ستون‌های فیلترپذیر. */
export function ColumnFilterIcon({ active }: { active: boolean }) {
  return (
    <Filter
      className={`size-4 shrink-0 ${active ? "text-primary-foreground" : "text-muted"}`}
      aria-hidden
      strokeWidth={2.25}
    />
  );
}
