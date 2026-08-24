"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import type { ReactNode } from "react";
import { useTheme } from "../../design-system";

const nav = [
  { href: "/admin/products", label: "محصولات", enabled: true },
  { href: "#orders", label: "سفارش‌ها", enabled: false },
  { href: "#customers", label: "مشتریان", enabled: false },
  { href: "#sellers", label: "فروشندگان", enabled: false },
  { href: "#inventory", label: "موجودی", enabled: false },
  { href: "#promotions", label: "پروموشن", enabled: false },
  { href: "#content", label: "محتوا", enabled: false },
  { href: "#analytics", label: "تحلیل", enabled: false },
  { href: "#settings", label: "تنظیمات", enabled: false },
];

/**
 * پوستهٔ عملیاتی Admin. ماژول‌های آینده فقط مکان‌نما هستند و route جدید نمی‌سازند.
 */
export function AdminShell({ children }: { children: ReactNode }) {
  const path = usePathname();
  return (
    <div className="flex min-h-screen bg-background">
      <aside className="sticky top-0 hidden h-screen w-72 shrink-0 flex-col border-e border-border bg-surface-elevated shadow-sm md:flex">
        <div className="border-b border-border px-5 py-5">
          <p className="text-sm font-semibold tracking-wide text-primary">TOOBA</p>
          <p className="mt-1 text-xl font-semibold">عملیات فروش</p>
          <p className="mt-2 text-sm text-muted">کاتالوگ چندفروشنده‌ای</p>
        </div>
        <nav className="flex flex-1 flex-col gap-1 p-3 text-base" aria-label="Admin">
          {nav.map((item) => {
            const active = item.enabled && path.startsWith(item.href);
            if (!item.enabled) {
              return (
                <span key={item.label} className="rounded-ds px-3 py-2 text-sm text-muted/70">
                  {item.label}
                </span>
              );
            }
            return (
              <Link
                key={item.href}
                href={item.href}
                className={
                  active
                    ? "rounded-ds bg-primary px-3 py-2 text-sm font-medium text-primary-foreground"
                    : "rounded-ds px-3 py-2 text-sm text-foreground hover:bg-secondary"
                }
              >
                {item.label}
              </Link>
            );
          })}
        </nav>
      </aside>
      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex min-h-16 items-center justify-between gap-3 border-b border-border bg-surface px-5 md:px-8">
          <div>
            <p className="text-base font-medium">کاتالوگ · Workspace محصول</p>
            <p className="text-sm text-muted">فروشگاه store-alpha</p>
          </div>
          <div className="flex items-center gap-2">
            <AppearanceControls />
            <span className="rounded-ds bg-secondary px-3 py-1.5 text-sm text-secondary-foreground">اپراتور کاتالوگ</span>
          </div>
        </header>
        <div className="flex-1">{children}</div>
      </div>
    </div>
  );
}

/**
 * کنترل ظاهر عملیاتی Admin. جهت و طرح رنگ را بدون نوار اشکال‌زدایی روی پوسته عوض می‌کند.
 */
function AppearanceControls() {
  const { theme, setColorScheme, setDirection } = useTheme();
  return (
    <div className="flex items-center gap-1" role="group" aria-label="ظاهر">
      <button
        type="button"
        className="min-h-9 rounded-ds border border-border px-2 text-xs"
        onClick={() => setDirection(theme.direction === "rtl" ? "ltr" : "rtl")}
      >
        {theme.direction === "rtl" ? "LTR" : "RTL"}
      </button>
      <button
        type="button"
        className="min-h-9 rounded-ds border border-border px-2 text-xs"
        onClick={() => setColorScheme(theme.colorScheme === "dark" ? "light" : "dark")}
      >
        {theme.colorScheme === "dark" ? "روشن" : "تیره"}
      </button>
    </div>
  );
}
