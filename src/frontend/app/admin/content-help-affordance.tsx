"use client";

import Link from "next/link";
import { useEffect, useId, useRef, useState } from "react";
import {
  CONTENT_HELP_PAGE_HREF,
  contentHelpSummary,
  contentHelpTitle,
  getContentHelpTopic,
  type ContentHelpKey,
  type ContentHelpLocale,
} from "./content-help-content.ts";

type ContentHelpAffordanceProps = {
  helpKey: ContentHelpKey;
  locale?: ContentHelpLocale;
  className?: string;
};

/** سبک Help / ? نزدیک فیلدهای گیج‌کننده — popover + لینک به صفحهٔ راهنمای مرکزی. */
export function ContentHelpAffordance({
  helpKey,
  locale = "fa",
  className = "",
}: ContentHelpAffordanceProps) {
  const topic = getContentHelpTopic(helpKey);
  const [open, setOpen] = useState(false);
  const panelId = useId();
  const rootRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const onDoc = (event: MouseEvent) => {
      if (!rootRef.current?.contains(event.target as Node)) setOpen(false);
    };
    const onKey = (event: KeyboardEvent) => {
      if (event.key === "Escape") setOpen(false);
    };
    document.addEventListener("mousedown", onDoc);
    document.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("mousedown", onDoc);
      document.removeEventListener("keydown", onKey);
    };
  }, [open]);

  if (!topic) return null;

  const title = contentHelpTitle(topic, locale);
  const summary = contentHelpSummary(topic, locale);

  return (
    <div ref={rootRef} className={`relative inline-flex ${className}`} data-testid={`content-help-${helpKey}`}>
      <button
        type="button"
        className="inline-flex h-6 w-6 items-center justify-center rounded-full border text-xs font-semibold text-muted hover:bg-slate-50"
        aria-expanded={open}
        aria-controls={panelId}
        aria-label={locale === "en" ? `Help: ${title}` : `راهنما: ${title}`}
        data-testid={`content-help-trigger-${helpKey}`}
        onClick={() => setOpen((v) => !v)}
      >
        ?
      </button>
      {open ? (
        <div
          id={panelId}
          role="dialog"
          className="absolute z-30 mt-8 w-72 rounded-xl border bg-white p-3 text-start shadow-lg ltr:left-0 rtl:right-0"
          data-testid={`content-help-popover-${helpKey}`}
        >
          <p className="text-sm font-semibold">{title}</p>
          <p className="mt-1 text-xs leading-5 text-muted">{summary}</p>
          <Link
            href={`${CONTENT_HELP_PAGE_HREF}#${helpKey}`}
            className="mt-2 inline-block text-xs font-medium text-[#2563EB] underline"
            onClick={() => setOpen(false)}
          >
            {locale === "en" ? "Open Content Help" : "باز کردن راهنمای محتوا"}
          </Link>
        </div>
      ) : null}
    </div>
  );
}
