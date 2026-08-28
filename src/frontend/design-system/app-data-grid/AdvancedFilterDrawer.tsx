"use client";

import { useEffect, useState, type ReactNode } from "react";
import { cn } from "../cn";
import { Button } from "../primitives/core";

/** کشوی فیلتر پیشرفته — مطابق mockup تأییدشده؛ RTL، footer چسبان، backdrop. */
export function AdvancedFilterDrawer({
  open,
  onClose,
  title,
  subtitle,
  headerActions,
  activeSummary,
  footer,
  children,
}: {
  open: boolean;
  onClose: () => void;
  title: string;
  subtitle?: string;
  headerActions?: ReactNode;
  activeSummary?: ReactNode;
  footer: ReactNode;
  children: ReactNode;
}) {
  const [entered, setEntered] = useState(false);

  useEffect(() => {
    if (!open) {
      setEntered(false);
      return;
    }
    const onKey = (event: KeyboardEvent) => {
      if (event.key === "Escape") onClose();
    };
    window.addEventListener("keydown", onKey);
    const frame = window.requestAnimationFrame(() => setEntered(true));
    return () => {
      window.removeEventListener("keydown", onKey);
      window.cancelAnimationFrame(frame);
    };
  }, [open, onClose]);

  if (!open) return null;

  return (
    <div
      className={cn(
        "fixed inset-0 z-[var(--z-drawer)] transition-opacity duration-150",
        entered ? "opacity-100" : "opacity-0",
      )}
      data-testid="advanced-filter-drawer"
    >
      <button
        type="button"
        aria-label="بستن پوشش"
        className="absolute inset-0 z-[var(--z-drawer-backdrop)] bg-foreground/35"
        onClick={onClose}
      />
      <aside
        className={cn(
          "absolute top-0 start-0 z-[var(--z-drawer)] flex h-full w-full max-w-none flex-col bg-surface shadow-ds transition-transform duration-200 ease-out md:w-[min(480px,calc(100vw-1rem))]",
          entered ? "translate-x-0" : "ltr:-translate-x-4 rtl:translate-x-4",
        )}
        data-advanced-filter-panel
      >
        <header className="shrink-0 border-b border-border bg-surface px-4 pb-3 pt-4 md:px-5">
          <div className="flex items-start justify-between gap-3">
            <div className="min-w-0">
              <h2 className="text-lg font-semibold leading-snug">{title}</h2>
              {subtitle ? <p className="mt-1 text-sm text-muted">{subtitle}</p> : null}
            </div>
            <Button type="button" tone="ghost" aria-label="بستن" onClick={onClose} className="shrink-0">
              ×
            </Button>
          </div>
          {headerActions ? <div className="mt-4 flex flex-wrap gap-2">{headerActions}</div> : null}
        </header>

        <div className="min-h-0 flex-1 overflow-y-auto px-4 py-4 md:px-5">{children}</div>

        {activeSummary ? (
          <div className="shrink-0 border-t border-border bg-surface px-4 py-3 md:px-5">{activeSummary}</div>
        ) : null}

        <footer className="shrink-0 border-t border-border bg-surface px-4 py-4 md:px-5">{footer}</footer>
      </aside>
    </div>
  );
}
