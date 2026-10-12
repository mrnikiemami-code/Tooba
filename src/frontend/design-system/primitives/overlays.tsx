"use client";

import { useCallback, useEffect, useId, useLayoutEffect, useRef, useState, type ReactNode } from "react";
import { createPortal } from "react-dom";
import { cn } from "../cn";
import { Button } from "./core";

const TOOLTIP_GAP = 8;
const TOOLTIP_VIEWPORT_MARGIN = 8;

type TooltipAnchor = { top: number; bottom: number; center: number };
type TooltipPlacement = { top: number; left: number };

/**
 * راهنمای کوتاه روی فوکوس/هاور. برای محتوای پیچیده از Popover استفاده شود.
 *
 * حباب با `createPortal` روی `document.body` و با `position: fixed` رندر می‌شود تا
 * `overflow: hidden` نیاکان (مثل سلول‌های `AppDataGrid`) آن را نبرد و بریده نشود.
 * مختصات از `getBoundingClientRect` می‌آید، پس مستقل از جهت RTL/LTR یکسان و دقیق
 * است؛ پیش‌فرض زیر تریگر و اگر جا نشد بالا، و افقی داخل viewport کلمپ می‌شود.
 * مستقیم از createPortal استفاده می‌شود (نه primitive «Portal» که یک تیک دیرتر mount
 * می‌شود) تا حباب در همان commit موجود و قابل اندازه‌گیری در useLayoutEffect باشد.
 */
export function Tooltip({ label, children }: { label: string; children: ReactNode }) {
  const [open, setOpen] = useState(false);
  const [anchor, setAnchor] = useState<TooltipAnchor | null>(null);
  const [placement, setPlacement] = useState<TooltipPlacement | null>(null);
  const triggerRef = useRef<HTMLSpanElement>(null);
  const bubbleRef = useRef<HTMLSpanElement>(null);

  const place = useCallback(() => {
    const rect = triggerRef.current?.getBoundingClientRect();
    if (!rect) return;
    setAnchor({ top: rect.top, bottom: rect.bottom, center: rect.left + rect.width / 2 });
  }, []);

  const show = useCallback(() => {
    place();
    setOpen(true);
  }, [place]);

  const hide = useCallback(() => {
    setOpen(false);
    setPlacement(null);
  }, []);

  // پیش از paint اجرا می‌شود: نه پرش دیده می‌شود و نه فلش.
  useLayoutEffect(() => {
    if (!open || !anchor) return;
    const bubble = bubbleRef.current;
    if (!bubble) return;
    const { offsetWidth: width, offsetHeight: height } = bubble;
    const half = width / 2;
    // پیش‌فرض زیر تریگر (رفتار اصلی)؛ فقط اگر در viewport جا نشد به بالا فلیپ می‌کند.
    const below = anchor.bottom + TOOLTIP_GAP;
    const top =
      below + height <= window.innerHeight - TOOLTIP_VIEWPORT_MARGIN
        ? below
        : Math.max(TOOLTIP_VIEWPORT_MARGIN, anchor.top - TOOLTIP_GAP - height);
    const center = Math.min(
      Math.max(anchor.center, TOOLTIP_VIEWPORT_MARGIN + half),
      window.innerWidth - TOOLTIP_VIEWPORT_MARGIN - half,
    );
    setPlacement({ top, left: center - half });
  }, [open, anchor, label]);

  useEffect(() => {
    if (!open) return;
    window.addEventListener("scroll", place, true);
    window.addEventListener("resize", place);
    return () => {
      window.removeEventListener("scroll", place, true);
      window.removeEventListener("resize", place);
    };
  }, [open, place]);

  return (
    <span
      ref={triggerRef}
      className="relative inline-flex group"
      onMouseEnter={show}
      onMouseLeave={hide}
      onFocus={show}
      onBlur={hide}
    >
      {children}
      {open && anchor && typeof document !== "undefined"
        ? createPortal(
            <span
              ref={bubbleRef}
              role="tooltip"
              className="pointer-events-none fixed z-[var(--z-overlay)] whitespace-nowrap rounded-ds bg-foreground px-2 py-1 text-xs text-background"
              style={placement ?? { top: anchor.bottom + TOOLTIP_GAP, left: anchor.center }}
            >
              {label}
            </span>,
            document.body,
          )
        : null}
    </span>
  );
}

/**
 * پنل سبک بدون تلهٔ فوکوس تمام‌صفحه؛ برای منوهای کوچک.
 */
export function Popover({ trigger, children }: { trigger: ReactNode; children: ReactNode }) {
  const [open, setOpen] = useState(false);
  return (
    <span className="relative inline-flex">
      <span onClick={() => setOpen((value) => !value)}>{trigger}</span>
      {open ? <div className="absolute start-0 z-[var(--z-overlay)] mt-2 min-w-48 rounded-ds border border-border bg-surface-elevated p-3 shadow-ds">{children}</div> : null}
    </span>
  );
}

/**
 * گفتگو با عنصر native dialog، بستن Escape، و بازگردانی فوکوس.
 */
export function Dialog({
  title,
  open,
  onClose,
  children,
  showCloseButton = true,
  size = "md",
}: {
  title: string;
  open: boolean;
  onClose: () => void;
  children: ReactNode;
  /** وقتی دکمه‌های صریح در children هست، دکمهٔ پیش‌فرض «بستن» را پنهان کنید. */
  showCloseButton?: boolean;
  size?: "md" | "lg" | "xl";
}) {
  const ref = useRef<HTMLDialogElement>(null);
  useEffect(() => {
    const node = ref.current;
    if (!node) return;
    if (open && !node.open) node.showModal();
    if (!open && node.open) node.close();
  }, [open]);
  const widthClass =
    size === "xl"
      ? "w-[min(52rem,calc(100%-2rem))]"
      : size === "lg"
        ? "w-[min(42rem,calc(100%-2rem))]"
        : "w-[min(32rem,calc(100%-2rem))]";
  return (
    <dialog
      ref={ref}
      onClose={onClose}
      className={cn(
        "z-[var(--z-modal)] max-h-[90vh] overflow-hidden rounded-ds border border-border bg-surface p-4 shadow-ds",
        widthClass,
      )}
    >
      <h2 className="mb-3 text-lg font-semibold">{title}</h2>
      {children}
      {showCloseButton ? (
        <div className="mt-4 flex justify-end">
          <Button type="button" tone="secondary" onClick={onClose}>
            بستن
          </Button>
        </div>
      ) : null}
    </dialog>
  );
}

/**
 * کشو با قرارگیری منطقی: در RTL از inline-start (راست) باز می‌شود نه left ثابت.
 * باز شدن با انتقال کوتاه و لایهٔ فشرده برای کاهش فضای خالی بزرگ.
 */
export function Drawer({
  title,
  open,
  onClose,
  children,
}: {
  title: string;
  open: boolean;
  onClose: () => void;
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
    <div className={cn("fixed inset-0 z-[var(--z-modal)] transition-opacity duration-150", entered ? "opacity-100" : "opacity-0")}>
      <button type="button" aria-label="بستن پوشش" className="absolute inset-0 bg-foreground/30" onClick={onClose} />
      <aside
        className={cn(
          "absolute top-0 start-0 h-full w-[min(22rem,100%)] overflow-auto bg-surface p-3 shadow-ds transition-transform duration-200 ease-out",
          entered ? "translate-x-0" : "ltr:-translate-x-3 rtl:translate-x-3",
        )}
      >
        <div className="mb-2 flex items-center justify-between gap-2">
          <h2 className="text-base font-semibold">{title}</h2>
          <Button type="button" tone="ghost" onClick={onClose}>
            بستن
          </Button>
        </div>
        {children}
      </aside>
    </div>
  );
}

/**
 * زبانه با دکمه/پنل مرتبط.
 */
export function Tabs({ tabs }: { tabs: Array<{ id: string; label: string; panel: ReactNode }> }) {
  const [active, setActive] = useState(tabs[0]?.id);
  return (
    <div>
      <div role="tablist" className="flex gap-2">
        {tabs.map((tab) => (
          <button
            key={tab.id}
            type="button"
            role="tab"
            aria-selected={tab.id === active}
            className={cn("min-h-11 rounded-ds px-3 text-sm", tab.id === active ? "bg-primary text-primary-foreground" : "bg-secondary")}
            onClick={() => setActive(tab.id)}
          >
            {tab.label}
          </button>
        ))}
      </div>
      {tabs.map((tab) =>
        tab.id === active ? (
          <div key={tab.id} role="tabpanel" className="mt-3">
            {tab.panel}
          </div>
        ) : null,
      )}
    </div>
  );
}

/**
 * آکاردئون تک‌باز با دکمهٔ معنایی.
 */
export function Accordion({ items }: { items: Array<{ title: string; body: ReactNode }> }) {
  const [open, setOpen] = useState(0);
  return (
    <div className="grid gap-2">
      {items.map((item, index) => (
        <div key={item.title} className="rounded-ds border border-border">
          <button type="button" className="flex min-h-11 w-full items-center justify-between px-3 text-start" onClick={() => setOpen(index)}>
            {item.title}
          </button>
          {open === index ? <div className="border-t border-border p-3 text-sm">{item.body}</div> : null}
        </div>
      ))}
    </div>
  );
}

const toastId = () => "toast-region";

/**
 * ناحیهٔ زنده برای پیام‌های غیرمسدودکننده. جایگزین react-toastify قالب نیست.
 */
export function ToastRegion({ message }: { message: string | null }) {
  const id = useId();
  return (
    <div id={toastId() + id} aria-live="polite" aria-atomic="true" className="fixed bottom-4 end-4 z-[var(--z-overlay)]">
      {message ? <div className="rounded-ds bg-foreground px-4 py-2 text-sm text-background">{message}</div> : null}
    </div>
  );
}
