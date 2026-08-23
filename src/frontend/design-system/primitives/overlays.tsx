"use client";

import { useEffect, useId, useRef, useState, type ReactNode } from "react";
import { cn } from "../cn";
import { Button } from "./core";

/**
 * راهنمای کوتاه روی فوکوس/هاور. برای محتوای پیچیده از Popover استفاده شود.
 */
export function Tooltip({ label, children }: { label: string; children: ReactNode }) {
  return (
    <span className="relative inline-flex group">
      {children}
      <span role="tooltip" className="pointer-events-none absolute start-1/2 z-[var(--z-overlay)] mt-2 hidden -translate-x-1/2 whitespace-nowrap rounded-ds bg-foreground px-2 py-1 text-xs text-background group-hover:block group-focus-within:block">
        {label}
      </span>
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
}: {
  title: string;
  open: boolean;
  onClose: () => void;
  children: ReactNode;
}) {
  const ref = useRef<HTMLDialogElement>(null);
  useEffect(() => {
    const node = ref.current;
    if (!node) return;
    if (open && !node.open) node.showModal();
    if (!open && node.open) node.close();
  }, [open]);
  return (
    <dialog
      ref={ref}
      onClose={onClose}
      className="z-[var(--z-modal)] w-[min(32rem,calc(100%-2rem))] rounded-ds border border-border bg-surface p-4 shadow-ds"
    >
      <h2 className="mb-3 text-lg font-semibold">{title}</h2>
      {children}
      <div className="mt-4 flex justify-end">
        <Button type="button" tone="secondary" onClick={onClose}>
          بستن
        </Button>
      </div>
    </dialog>
  );
}

/**
 * کشو با قرارگیری منطقی: در RTL از inline-start (راست) باز می‌شود نه left ثابت.
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
  useEffect(() => {
    if (!open) return;
    const onKey = (event: KeyboardEvent) => {
      if (event.key === "Escape") onClose();
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [open, onClose]);
  if (!open) return null;
  return (
    <div className="fixed inset-0 z-[var(--z-modal)]">
      <button type="button" aria-label="بستن پوشش" className="absolute inset-0 bg-foreground/30" onClick={onClose} />
      <aside className="absolute top-0 start-0 h-full w-[min(24rem,100%)] overflow-auto bg-surface p-4 shadow-ds">
        <div className="mb-3 flex items-center justify-between">
          <h2 className="text-lg font-semibold">{title}</h2>
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
