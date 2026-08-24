"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useState, type ReactNode } from "react";
import {
  BarChart3,
  FileText,
  LayoutDashboard,
  Menu,
  Package,
  Settings,
  ShoppingBag,
  Store,
  Tag,
  Users,
  Warehouse,
  X,
} from "lucide-react";
import { useTheme } from "../../design-system";

const nav = [
  { href: "#dashboard", label: "داشبورد", enabled: false, icon: LayoutDashboard },
  { href: "/admin/products", label: "محصولات", enabled: true, icon: Package },
  { href: "#orders", label: "سفارش‌ها", enabled: false, icon: ShoppingBag },
  { href: "#customers", label: "مشتریان", enabled: false, icon: Users },
  { href: "#sellers", label: "فروشندگان", enabled: false, icon: Store },
  { href: "#inventory", label: "موجودی", enabled: false, icon: Warehouse },
  { href: "#promotions", label: "پروموشن", enabled: false, icon: Tag },
  { href: "#content", label: "محتوا", enabled: false, icon: FileText },
  { href: "#analytics", label: "تحلیل", enabled: false, icon: BarChart3 },
  { href: "#settings", label: "تنظیمات", enabled: false, icon: Settings },
];

/**
 * پوستهٔ عملیاتی Admin. ماژول‌های آینده فقط مکان‌نما هستند و route جدید نمی‌سازند.
 */
export function AdminShell({ children }: { children: ReactNode }) {
  const path = usePathname();
  const [menuOpen, setMenuOpen] = useState(false);

  function NavItems({ onNavigate }: { onNavigate?: () => void }) {
    return (
      <nav className="flex flex-1 flex-col gap-1 p-3 text-base" aria-label="Admin">
        {nav.map((item) => {
          const active = item.enabled && path.startsWith(item.href);
          const Icon = item.icon;
          const body = (
            <>
              <Icon className="size-4 shrink-0 opacity-80" aria-hidden />
              {item.label}
            </>
          );
          if (!item.enabled) {
            return (
              <span key={item.label} className="flex items-center gap-3 rounded-ds px-3 py-2.5 text-sm text-muted/70">
                {body}
              </span>
            );
          }
          return (
            <Link
              key={item.href}
              href={item.href}
              onClick={onNavigate}
              className={
                active
                  ? "flex items-center gap-3 rounded-ds bg-primary px-3 py-2.5 text-sm font-medium text-primary-foreground"
                  : "flex items-center gap-3 rounded-ds px-3 py-2.5 text-sm text-foreground hover:bg-secondary"
              }
            >
              {body}
            </Link>
          );
        })}
      </nav>
    );
  }

  return (
    <div className="flex min-h-screen bg-background">
      <aside className="sticky top-0 hidden h-screen w-72 shrink-0 flex-col border-e border-border bg-surface-elevated shadow-sm md:flex">
        <div className="border-b border-border px-5 py-5">
          <p className="text-sm font-semibold tracking-wide text-primary">TOOBA</p>
          <p className="mt-1 text-xl font-semibold">عملیات فروش</p>
          <p className="mt-2 text-sm text-muted">کاتالوگ چندفروشنده‌ای</p>
        </div>
        <NavItems />
      </aside>
      {menuOpen ? (
        <div className="fixed inset-0 z-40 md:hidden">
          <button type="button" className="absolute inset-0 bg-foreground/40" aria-label="بستن منو" onClick={() => setMenuOpen(false)} />
          <aside className="relative z-50 flex h-full w-72 flex-col bg-surface-elevated shadow-lg">
            <div className="flex items-center justify-between border-b border-border px-4 py-4">
              <p className="text-lg font-semibold">منو</p>
              <button type="button" className="inline-flex min-h-11 min-w-11 items-center justify-center rounded-ds bg-secondary" aria-label="بستن" onClick={() => setMenuOpen(false)}>
                <X className="size-5" />
              </button>
            </div>
            <NavItems onNavigate={() => setMenuOpen(false)} />
          </aside>
        </div>
      ) : null}
      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex min-h-16 items-center justify-between gap-3 border-b border-border bg-surface px-4 md:px-8">
          <div className="flex min-w-0 items-center gap-3">
            <button
              type="button"
              className="inline-flex min-h-11 min-w-11 items-center justify-center rounded-ds border border-border md:hidden"
              aria-label="باز کردن منو"
              onClick={() => setMenuOpen(true)}
            >
              <Menu className="size-5" />
            </button>
            <div className="min-w-0">
              <p className="truncate text-base font-medium">کاتالوگ محصول</p>
              <p className="text-sm text-muted">فروشگاه آلفا</p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <AppearanceControls />
            <span className="hidden rounded-ds bg-secondary px-3 py-1.5 text-sm text-secondary-foreground sm:inline">اپراتور کاتالوگ</span>
          </div>
        </header>
        <div className="min-w-0 flex-1">{children}</div>
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
