"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useState, type ReactNode } from "react";
import {
  ChevronLeft,
  LayoutDashboard,
  LogOut,
  Menu,
  Package,
  Settings,
  Shield,
  ShoppingBag,
  Star,
  Store,
  RotateCcw,
  Truck,
  Users,
  X,
} from "lucide-react";
import { prepareAdminDevActor } from "./admin-api";

type NavItem = {
  id: string;
  label: string;
  href: string;
  icon: typeof LayoutDashboard;
  live: boolean;
  exact?: boolean;
};

type NavGroup = {
  id: string;
  label: string;
  items: NavItem[];
};

/** ناوبری عملیاتی Admin؛ گروه‌بندی workflow نه کپی Seller. */
const navGroups: NavGroup[] = [
  {
    id: "ops",
    label: "عملیات",
    items: [
      { id: "dashboard", label: "داشبورد", href: "/admin", icon: LayoutDashboard, live: true, exact: true },
      { id: "products", label: "کاتالوگ / محصولات", href: "/admin/products", icon: Package, live: true },
      { id: "orders", label: "سفارش‌ها و پرداخت", href: "/admin/orders", icon: ShoppingBag, live: true },
      { id: "fulfillments", label: "ارسال / fulfillment", href: "/admin/fulfillments", icon: Truck, live: true },
      { id: "returns", label: "مرجوعی / refund", href: "/admin/returns", icon: RotateCcw, live: true },
    ],
  },
  {
    id: "market",
    label: "بازار",
    items: [
      { id: "sellers", label: "فروشندگان", href: "/admin/sellers", icon: Store, live: true },
      { id: "customers", label: "مشتریان", href: "/admin/customers", icon: Users, live: true },
    ],
  },
  {
    id: "moderation",
    label: "نظارت",
    items: [{ id: "reviews", label: "نظرات", href: "/admin/reviews", icon: Star, live: true }],
  },
  {
    id: "system",
    label: "سامانه",
    items: [{ id: "settings", label: "تنظیمات", href: "/admin/settings", icon: Settings, live: false }],
  },
];

function isActivePath(pathname: string, item: NavItem): boolean {
  if (item.exact || item.href === "/admin") {
    return pathname === item.href;
  }
  return pathname === item.href || pathname.startsWith(`${item.href}/`);
}

function crumbFor(pathname: string): string {
  for (const group of navGroups) {
    for (const item of group.items) {
      if (isActivePath(pathname, item)) {
        return item.label;
      }
    }
  }
  return "عملیات";
}

/**
 * پوستهٔ Admin حرفه‌ای با زبان بصری Shopeiva Vendor/Account
 * (header چسبان + sidebar + drawer) و هویت عملیاتی جدا از Seller Panel.
 * accent Tooba آبی است.
 */
export function AdminShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    void prepareAdminDevActor().finally(() => setReady(true));
  }, []);

  useEffect(() => {
    document.body.style.overflow = mobileOpen ? "hidden" : "";
    return () => {
      document.body.style.overflow = "";
    };
  }, [mobileOpen]);

  if (!ready) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-gray-50 text-gray-500" data-testid="admin-shell-loading">
        در حال آماده‌سازی پنل مدیریت…
      </div>
    );
  }

  const crumb = crumbFor(pathname);

  return (
    <div className="min-h-screen bg-gray-50 flex flex-col overflow-x-hidden" dir="rtl" data-testid="admin-panel-shell">
      <header className="sticky top-0 z-40 bg-white border-b border-gray-200 h-[65px] flex items-center" data-testid="admin-panel-header">
        <div className="flex items-center justify-between w-full px-4 lg:px-6 gap-3">
          <div className="flex items-center gap-3 min-w-0">
            <button
              type="button"
              onClick={() => setSidebarOpen((open) => !open)}
              className="hidden lg:flex p-2 rounded-lg hover:bg-gray-100"
              aria-label="جمع‌کردن منو"
            >
              <Menu className="w-5 h-5 text-gray-700" />
            </button>
            <button
              type="button"
              onClick={() => setMobileOpen(true)}
              className="lg:hidden p-2 rounded-lg hover:bg-gray-100"
              aria-label="منوی موبایل"
              data-testid="admin-panel-mobile-menu"
            >
              <Menu className="w-5 h-5 text-gray-700" />
            </button>
            <div className="flex items-center gap-2 min-w-0">
              <div className="w-8 h-8 rounded-xl bg-[#2563EB] flex items-center justify-center shadow-sm shrink-0">
                <Shield className="w-4 h-4 text-white" />
              </div>
              <div className="min-w-0">
                <p className="font-bold text-gray-900 text-sm truncate">مرکز عملیات توبا</p>
                <p className="text-[10px] text-gray-500 hidden sm:block">پنل مدیریت · {crumb}</p>
              </div>
            </div>
          </div>
          <div className="flex items-center gap-2 shrink-0">
            <span className="hidden sm:inline-flex items-center rounded-full bg-[#2563EB]/10 text-[#2563EB] px-3 py-1 text-[11px] font-bold">
              دسترسی مدیر
            </span>
            <Link href="/" className="p-2 rounded-lg hover:bg-red-50 text-red-500" aria-label="خروج به فروشگاه" title="خروج">
              <LogOut className="w-5 h-5" />
            </Link>
          </div>
        </div>
      </header>

      <div className="flex flex-1 relative">
        <aside
          className={`hidden lg:block bg-white border-l border-gray-200 shrink-0 transition-all duration-300 sticky top-[65px] h-[calc(100vh-65px)] overflow-y-auto ${
            sidebarOpen ? "w-64" : "w-0 opacity-0"
          }`}
          data-testid="admin-panel-sidebar"
        >
          <div className="p-4 space-y-5 min-w-[250px]">
            {navGroups.map((group) => (
              <div key={group.id}>
                <p className="px-3 mb-1 text-[10px] font-bold tracking-wide text-gray-400 uppercase">{group.label}</p>
                <nav className="space-y-1" aria-label={group.label}>
                  {group.items.map((item) => (
                    <NavLink key={item.id} item={item} pathname={pathname} />
                  ))}
                </nav>
              </div>
            ))}
          </div>
        </aside>

        <main className="flex-1 min-w-0 w-full p-4 md:p-6 lg:p-8" data-testid="admin-panel-main">
          {children}
        </main>
      </div>

      {mobileOpen ? (
        <div className="lg:hidden fixed inset-0 z-50" role="dialog" aria-modal="true" data-testid="admin-panel-drawer">
          <div className="absolute inset-0 bg-black/60 backdrop-blur-sm" onClick={() => setMobileOpen(false)} />
          <aside className="absolute right-0 top-0 h-full w-[280px] bg-white shadow-2xl flex flex-col">
            <div className="flex items-center justify-between p-4 border-b border-gray-200">
              <div className="flex items-center gap-2">
                <div className="w-8 h-8 rounded-xl bg-[#2563EB] flex items-center justify-center">
                  <Shield className="w-4 h-4 text-white" />
                </div>
                <span className="font-bold text-gray-900">مدیریت توبا</span>
              </div>
              <button type="button" onClick={() => setMobileOpen(false)} className="p-2 rounded-lg hover:bg-gray-100" aria-label="بستن">
                <X className="w-5 h-5" />
              </button>
            </div>
            <div className="flex-1 overflow-y-auto p-4 space-y-5">
              {navGroups.map((group) => (
                <div key={group.id}>
                  <p className="px-3 mb-1 text-[10px] font-bold text-gray-400">{group.label}</p>
                  <nav className="space-y-1">
                    {group.items.map((item) => (
                      <NavLink key={item.id} item={item} pathname={pathname} onNavigate={() => setMobileOpen(false)} dense />
                    ))}
                  </nav>
                </div>
              ))}
            </div>
            <div className="p-4 border-t border-gray-200 bg-gray-50">
              <Link
                href="/"
                className="flex items-center justify-center gap-2 px-4 py-3 w-full rounded-xl text-sm font-medium text-red-500 bg-red-50"
              >
                <LogOut className="w-5 h-5" />
                بازگشت به فروشگاه
              </Link>
            </div>
          </aside>
        </div>
      ) : null}
    </div>
  );
}

function NavLink({
  item,
  pathname,
  onNavigate,
  dense,
}: {
  item: NavItem;
  pathname: string;
  onNavigate?: () => void;
  dense?: boolean;
}) {
  const active = isActivePath(pathname, item);
  return (
    <Link
      href={item.href}
      onClick={onNavigate}
      className={`flex items-center gap-3 rounded-xl text-sm font-medium transition-all ${
        dense ? "px-4 py-3" : "px-3 py-2.5"
      } ${active ? "bg-[#2563EB] text-white shadow-md shadow-[#2563EB]/20" : "text-gray-700 hover:bg-gray-100"}`}
      data-testid={`admin-nav-${item.id}`}
      data-live={item.live ? "true" : "false"}
    >
      <item.icon className="w-5 h-5 shrink-0" />
      <span className="flex-1 truncate">{item.label}</span>
      {!item.live ? <span className={`text-[10px] ${active ? "text-white/80" : "text-gray-400"}`}>به‌زودی</span> : null}
      {active ? <ChevronLeft className="w-4 h-4 shrink-0" /> : null}
    </Link>
  );
}
