"use client";

import { useEffect, useId, useRef, useState, type ReactNode } from "react";
import { Portal } from "../primitives/Portal";

/** پنل شناور app-owned — خارج از AG Grid popup/menu؛ anchor به دکمهٔ header. */
export function ColumnFilterPopover({
  open,
  onClose,
  anchorRef,
  title,
  children,
  width = 400,
  testId = "column-filter-popover",
}: {
  open: boolean;
  onClose: () => void;
  anchorRef: React.RefObject<HTMLElement | null>;
  title: string;
  children: ReactNode;
  width?: number;
  testId?: string;
}) {
  const panelRef = useRef<HTMLDivElement>(null);
  const titleId = useId();
  const [style, setStyle] = useState<{ top: number; left: number; width: number }>({ top: 0, left: 0, width });

  useEffect(() => {
    if (!open) return;
    const anchor = anchorRef.current;
    if (!anchor) return;

    function reposition() {
      const rect = anchor!.getBoundingClientRect();
      const panelWidth = Math.min(width, window.innerWidth - 16);
      let left = rect.left;
      const maxLeft = window.innerWidth - panelWidth - 8;
      if (left > maxLeft) left = Math.max(8, maxLeft);
      const top = rect.bottom + 8;
      const fitsBelow = top + 320 < window.innerHeight;
      setStyle({
        top: fitsBelow ? top : Math.max(8, rect.top - 320),
        left,
        width: panelWidth,
      });
    }

    reposition();
    const onKey = (event: KeyboardEvent) => {
      if (event.key === "Escape") onClose();
    };
    const onDoc = (event: MouseEvent) => {
      const target = event.target as Node;
      if (panelRef.current?.contains(target)) return;
      if (anchorRef.current?.contains(target)) return;
      if ((event.target as HTMLElement | null)?.closest?.("[data-jalali-picker-panel]")) return;
      onClose();
    };
    window.addEventListener("resize", reposition);
    window.addEventListener("scroll", reposition, true);
    document.addEventListener("keydown", onKey);
    document.addEventListener("mousedown", onDoc);
    return () => {
      window.removeEventListener("resize", reposition);
      window.removeEventListener("scroll", reposition, true);
      document.removeEventListener("keydown", onKey);
      document.removeEventListener("mousedown", onDoc);
    };
  }, [open, onClose, anchorRef, width]);

  useEffect(() => {
    if (!open) return;
    const frame = window.requestAnimationFrame(() => {
      panelRef.current?.querySelector<HTMLElement>("input,select,button")?.focus();
    });
    return () => window.cancelAnimationFrame(frame);
  }, [open]);

  if (!open) return null;

  const isMobile = typeof window !== "undefined" && window.innerWidth < 768;

  if (isMobile) {
    return (
      <Portal>
        <div className="fixed inset-0 z-[var(--z-popover)] flex flex-col justify-end bg-foreground/35" data-testid={testId}>
          <button type="button" className="absolute inset-0" aria-label="بستن" onClick={onClose} />
          <div
            ref={panelRef}
            role="dialog"
            aria-labelledby={titleId}
            className="relative max-h-[85vh] overflow-y-auto rounded-t-2xl border border-border bg-surface-elevated p-4 shadow-ds"
            data-app-filter-panel
          >
            <div className="mb-3 flex items-center justify-between gap-2">
              <h3 id={titleId} className="text-base font-semibold">
                {title}
              </h3>
              <button type="button" className="rounded-ds px-2 py-1 text-lg text-muted hover:bg-secondary" onClick={onClose} aria-label="بستن">
                ×
              </button>
            </div>
            {children}
          </div>
        </div>
      </Portal>
    );
  }

  return (
    <Portal>
      <div
        ref={panelRef}
        role="dialog"
        aria-labelledby={titleId}
        className="fixed z-[var(--z-popover)] rounded-2xl border border-border bg-surface-elevated p-4 shadow-ds"
        style={{ top: style.top, left: style.left, width: style.width, minWidth: 320 }}
        data-testid={testId}
        data-app-filter-panel
      >
        <div className="mb-3 flex items-center justify-between gap-2">
          <h3 id={titleId} className="text-base font-semibold">
            {title}
          </h3>
          <button type="button" className="rounded-ds px-2 py-1 text-lg text-muted hover:bg-secondary" onClick={onClose} aria-label="بستن">
            ×
          </button>
        </div>
        {children}
      </div>
    </Portal>
  );
}
