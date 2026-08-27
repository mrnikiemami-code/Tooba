"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState, type ReactNode } from "react";
import {
  Bell,
  ChevronLeft,
  Gift,
  Heart,
  LayoutDashboard,
  LogOut,
  MapPin,
  Menu,
  Package,
  Settings,
  Ticket,
  User,
  Wallet,
  X,
} from "lucide-react";

type NavItem = {
  id: string;
  label: string;
  href: string;
  icon: typeof LayoutDashboard;
  live: boolean;
};

/** ترتیب قفل‌شدهٔ ناوبری Shopeiva؛ مسیرهای بدون backend به‌صورت صادقانه غیرفعال می‌مانند. */
const menuItems: NavItem[] = [
  { id: "dashboard", label: "داشبورد", icon: LayoutDashboard, href: "/customer-panel", live: true },
  { id: "orders", label: "سفارشات", icon: Package, href: "/customer-panel/orders", live: true },
  { id: "wishlist", label: "علاقه‌مندی‌ها", icon: Heart, href: "/customer-panel/wishlist", live: true },
  { id: "wallet", label: "کیف پول", icon: Wallet, href: "/customer-panel/wallet", live: false },
  { id: "tickets", label: "تیکت‌ها", icon: Ticket, href: "/customer-panel/tickets", live: false },
  { id: "gift-cards", label: "کارت‌های هدیه", icon: Gift, href: "/customer-panel/gift-cards", live: false },
  { id: "addresses", label: "آدرس‌ها", icon: MapPin, href: "/customer-panel/addresses", live: true },
  { id: "notifications", label: "اطلاعیه‌ها", icon: Bell, href: "/customer-panel/notifications", live: false },
  { id: "profile", label: "پروفایل", icon: User, href: "/customer-panel/profile", live: true },
  { id: "settings", label: "تنظیمات", icon: Settings, href: "/customer-panel/settings", live: true },
];

function isActivePath(pathname: string, href: string): boolean {
  if (href === "/customer-panel") {
    return pathname === href;
  }
  return pathname === href || pathname.startsWith(`${href}/`);
}

/**
 * پوستهٔ پنل مشتری مطابق layout واقعی Shopeiva:
 * هدر چسبان، سایدبار تمام‌ارتفاع، drawer موبایل، وضعیت انتخاب با ChevronLeft.
 * رنگ برند Tooba آبی است (MINOR TECHNICAL DEVIATION نسبت به #E53935).
 */
export function CustomerPanelShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [mobileOpen, setMobileOpen] = useState(false);

  useEffect(() => {
    document.body.style.overflow = mobileOpen ? "hidden" : "";
    return () => {
      document.body.style.overflow = "";
    };
  }, [mobileOpen]);

  function leavePanel() {
    router.push("/");
  }

  return (
    <div className="min-h-screen bg-gray-50 flex flex-col overflow-x-hidden" dir="rtl" data-testid="customer-panel-shell">
      <header className="sticky top-0 z-40 bg-white border-b border-gray-200 h-[65px] flex items-center" data-testid="customer-panel-header">
        <div className="flex items-center justify-between w-full px-4 lg:px-6">
          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={() => setSidebarOpen((open) => !open)}
              className="hidden lg:flex p-2 rounded-lg hover:bg-gray-100 transition-colors"
              aria-label="جمع‌کردن منو"
            >
              <Menu className="w-5 h-5 text-gray-700" />
            </button>
            <button
              type="button"
              onClick={() => setMobileOpen(true)}
              className="lg:hidden p-2 rounded-lg hover:bg-gray-100 transition-colors"
              aria-label="منوی موبایل"
              data-testid="customer-panel-mobile-menu"
            >
              <Menu className="w-5 h-5 text-gray-700" />
            </button>
            <div className="flex items-center gap-2">
              <div className="w-8 h-8 rounded-xl bg-[#2563EB] flex items-center justify-center shadow-sm">
                <span className="text-white font-bold text-sm">ت</span>
              </div>
              <span className="font-bold text-gray-900 hidden sm:block">پنل کاربری</span>
            </div>
          </div>
          <div className="flex items-center gap-3">
            <Link href="/" className="text-sm text-gray-600 hover:text-[#2563EB] truncate max-w-[120px] sm:max-w-none">
              بازگشت به فروشگاه
            </Link>
            <button
              type="button"
              onClick={leavePanel}
              className="p-2 rounded-lg hover:bg-red-50 text-red-500 transition-colors"
              title="خروج از پنل"
              aria-label="خروج از پنل"
            >
              <LogOut className="w-5 h-5" />
            </button>
          </div>
        </div>
      </header>

      <div className="flex flex-1 relative">
        <aside
          className={`hidden lg:block bg-white border-l border-gray-200 shrink-0 transition-all duration-300 ease-in-out sticky top-[65px] h-[calc(100vh-65px)] overflow-y-auto ${
            sidebarOpen ? "w-64 translate-x-0" : "w-0 -translate-x-full opacity-0"
          }`}
          data-testid="customer-panel-sidebar"
        >
          <nav className="p-4 space-y-1 min-w-[250px]" aria-label="منوی مشتری">
            {menuItems.map((item) => (
              <NavLink key={item.id} item={item} pathname={pathname} />
            ))}
          </nav>
        </aside>

        <main className="flex-1 min-w-0 w-full p-4 md:p-6 lg:p-8" data-testid="customer-panel-main">
          {children}
        </main>
      </div>

      {mobileOpen ? (
        <div className="lg:hidden fixed inset-0 z-50" role="dialog" aria-modal="true" data-testid="customer-panel-drawer">
          <div className="absolute inset-0 bg-black/60 backdrop-blur-sm" onClick={() => setMobileOpen(false)} />
          <aside className="absolute right-0 top-0 h-full w-[280px] bg-white shadow-2xl flex flex-col">
            <div className="flex items-center justify-between p-4 border-b border-gray-200 shrink-0">
              <div className="flex items-center gap-2">
                <div className="w-8 h-8 rounded-xl bg-[#2563EB] flex items-center justify-center">
                  <span className="text-white font-bold text-sm">ت</span>
                </div>
                <span className="text-lg font-bold text-gray-900">پنل کاربری</span>
              </div>
              <button type="button" onClick={() => setMobileOpen(false)} className="p-2 rounded-lg hover:bg-gray-100" aria-label="بستن">
                <X className="w-5 h-5 text-gray-700" />
              </button>
            </div>
            <nav className="flex-1 overflow-y-auto p-4 space-y-1">
              {menuItems.map((item) => (
                <NavLink key={item.id} item={item} pathname={pathname} onNavigate={() => setMobileOpen(false)} dense />
              ))}
            </nav>
            <div className="p-4 border-t border-gray-200 shrink-0 bg-gray-50">
              <button
                type="button"
                onClick={leavePanel}
                className="flex items-center justify-center gap-2 px-4 py-3 w-full rounded-xl text-sm font-medium text-red-500 bg-red-50 hover:bg-red-100"
              >
                <LogOut className="w-5 h-5" />
                خروج از پنل
              </button>
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
  const active = isActivePath(pathname, item.href);
  return (
    <Link
      href={item.href}
      onClick={onNavigate}
      className={`flex items-center gap-3 rounded-xl text-sm font-medium transition-all ${
        dense ? "px-4 py-3" : "px-3 py-2.5"
      } ${
        active
          ? "bg-[#2563EB] text-white shadow-md shadow-[#2563EB]/20"
          : "text-gray-700 hover:bg-gray-100"
      }`}
      data-testid={`customer-nav-${item.id}`}
      data-live={item.live ? "true" : "false"}
    >
      <item.icon className="w-5 h-5 shrink-0" />
      <span className="flex-1 truncate">{item.label}</span>
      {!item.live ? <span className={`text-[10px] ${active ? "text-white/80" : "text-gray-400"}`}>به‌زودی</span> : null}
      {active ? <ChevronLeft className="w-4 h-4 shrink-0" /> : null}
    </Link>
  );
}
