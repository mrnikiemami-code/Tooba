"use client";

import Link from "next/link";
import { useRef } from "react";
import type { ICellRendererParams } from "ag-grid-community";
import { useOverflowTooltip } from "./use-overflow-tooltip";

type AppGridTruncatedCellProps = {
  params: ICellRendererParams;
  text: string;
  className?: string;
};

/** متن truncate با tooltip هنگام overflow — reusable برای ستون‌های متنی. */
export function AppGridTruncatedCell({ params, text, className = "" }: AppGridTruncatedCellProps) {
  const rootRef = useRef<HTMLSpanElement>(null);
  useOverflowTooltip(params, text, rootRef);
  return (
    <div className="app-grid-cell-content">
      <span ref={rootRef} data-overflow-measure className={`block min-w-0 truncate ${className}`}>
        {text}
      </span>
    </div>
  );
}

type AppGridLinkSubtitleCellProps = {
  params: ICellRendererParams;
  href: string;
  title: string;
  subtitle: string;
  subtitleDir?: "ltr" | "rtl";
};

/** سلول لینک + زیرعنوان — الگوی canonical برای ستون‌های entity. */
export function AppGridLinkSubtitleCell({
  params,
  href,
  title,
  subtitle,
  subtitleDir = "ltr",
}: AppGridLinkSubtitleCellProps) {
  const rootRef = useRef<HTMLDivElement>(null);
  useOverflowTooltip(params, `${title}\n${subtitle}`, rootRef);
  return (
    <div ref={rootRef} className="app-grid-cell-content">
      <Link className="block min-w-0 text-right hover:underline" href={href}>
        <span data-overflow-measure className="block truncate text-sm font-semibold leading-snug">
          {title}
        </span>
        <span data-overflow-measure className="mt-0.5 block truncate text-xs text-muted" dir={subtitleDir}>
          {subtitle}
        </span>
      </Link>
    </div>
  );
}

type AppGridMediaCellProps = {
  imageUrl: string | null;
  fallbackLabel?: string;
  imageClassName?: string;
};

/** سلول رسانه — تصویر یا placeholder؛ تراز RTL در theme.css. */
export function AppGridMediaCell({
  imageUrl,
  fallbackLabel = "بدون تصویر",
  imageClassName = "size-11 shrink-0 rounded-ds border border-border object-cover bg-secondary",
}: AppGridMediaCellProps) {
  return (
    <div className="app-grid-cell-content app-grid-cell-media">
      <div className="flex w-full items-center justify-end gap-2">
        {imageUrl ? (
          <img src={imageUrl} alt="" className={imageClassName} />
        ) : (
          <span className="flex size-11 shrink-0 items-center justify-center rounded-ds bg-secondary text-[10px] text-muted">
            {fallbackLabel}
          </span>
        )}
      </div>
    </div>
  );
}

type AppGridBadgeCellProps = {
  params: ICellRendererParams;
  label: string;
  className: string;
};

/** badge/status pill با tooltip overflow. */
export function AppGridBadgeCell({ params, label, className }: AppGridBadgeCellProps) {
  const rootRef = useRef<HTMLSpanElement>(null);
  useOverflowTooltip(params, label, rootRef);
  return (
    <div className="app-grid-cell-content">
      <span ref={rootRef} data-overflow-measure className={className}>
        {label}
      </span>
    </div>
  );
}
