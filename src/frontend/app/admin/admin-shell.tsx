"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useState, type ReactNode } from "react";
import { Bell, LayoutDashboard, Menu, Package, Search, ShoppingBag, Store, Users, X } from "lucide-react";
import { prepareAdminDevActor } from "./admin-api";

const nav = [
  { href: "/admin", label: "داشبورد", icon: LayoutDashboard, exact: true },
  { href: "/admin/products", label: "محصولات", icon: Package, exact: false },
  { href: "/admin/orders", label: "سفارش‌ها", icon: ShoppingBag, exact: false },
  { href: "/admin/sellers", label: "فروشندگان", icon: Store, exact: false },
  { href: "/admin/customers", label: "مشتریان", icon: Users, exact: false },
];

/**
 * پوستهٔ Admin با ساختار مستقیم پنل Shopeiva Vendor و ناوبری باریک موبایل.
 */
export function AdminShell({ children }: { children: ReactNode }) {
  const path = usePathname();
  const [menuOpen, setMenuOpen] = useState(false);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    void prepareAdminDevActor().finally(() => setReady(true));
  }, []);

  function NavItems({ onNavigate }: { onNavigate?: () => void }) {
    return (
      <nav className="flex flex-wrap items-center gap-1 text-sm" aria-label="Admin">
        {nav.map((item) => {
          const active = item.exact ? path === item.href : path.startsWith(item.href);
          const Icon = item.icon;
          return (
            <Link
              key={item.href}
              href={item.href}
              onClick={onNavigate}
              className={
                active
                  ? "inline-flex min-h-11 items-center gap-2 rounded-full bg-primary px-4 py-2 font-medium text-primary-foreground"
                  : "inline-flex min-h-11 items-center gap-2 rounded-full px-4 py-2 text-foreground hover:bg-secondary"
              }
            >
              <Icon className="size-4 shrink-0" aria-hidden />
              {item.label}
            </Link>
          );
        })}
      </nav>
    );
  }

  const crumb = nav.find((item) => item.exact ? path === item.href : path.startsWith(item.href))?.label ?? "عملیات";

  if (!ready) {
    return <div className="flex min-h-screen items-center justify-center bg-[rgb(248_248_247)] text-muted">در حال آماده‌سازی پنل مدیریت…</div>;
  }

  return (
    <div className="flex min-h-screen flex-col bg-[rgb(248_248_247)] text-foreground">
      <div className="bg-gradient-to-l from-[rgb(180_140_70)] via-[rgb(198_162_92)] to-[rgb(168_128_58)] text-white shadow-sm">
        <div className="mx-auto flex min-h-12 max-w-7xl items-center justify-between gap-3 px-4 py-2 md:px-8">
          <p className="text-sm font-medium tracking-wide md:text-base">توبا · حس خوب مدیریت</p>
          <div className="hidden items-center gap-3 md:flex">
            <span className="inline-flex min-h-9 items-center gap-2 rounded-full bg-white/15 px-3 text-sm">
              <Search className="size-3.5" aria-hidden />
              جستجو
            </span>
            <Bell className="size-4" aria-hidden />
          </div>
        </div>
      </div>
      <div className="border-b border-border bg-surface-elevated shadow-sm">
        <div className="mx-auto flex max-w-7xl flex-col gap-3 px-4 py-3 md:px-8">
          <div className="flex items-center justify-between gap-3">
            <div className="flex min-w-0 items-center gap-3">
              <button type="button" className="inline-flex min-h-11 min-w-11 items-center justify-center rounded-ds border border-border md:hidden" aria-label="باز کردن منو" onClick={() => setMenuOpen(true)}>
                <Menu className="size-5" />
              </button>
              <div>
                <p className="text-base font-semibold">مرکز عملیات توبا</p>
                <p className="text-sm text-muted">مدیریت / {crumb}</p>
              </div>
            </div>
            <span className="rounded-full bg-[rgb(255_247_237)] px-3 py-1.5 text-xs text-[rgb(194_65_12)]">دسترسی مدیر</span>
          </div>
          <div className="hidden md:block"><NavItems /></div>
        </div>
      </div>
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
            <div className="p-3"><NavItems onNavigate={() => setMenuOpen(false)} /></div>
          </aside>
        </div>
      ) : null}
      <div className="mx-auto w-full max-w-7xl min-w-0 flex-1 px-4 py-5 md:px-8 md:py-7">{children}</div>
    </div>
  );
}
