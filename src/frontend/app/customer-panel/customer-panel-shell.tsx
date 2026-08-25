"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import type { ReactNode } from "react";
import {
  Bell,
  ChevronDown,
  CircleUserRound,
  CreditCard,
  Heart,
  Home,
  LogOut,
  MapPin,
  Menu,
  Moon,
  Package,
  Search,
  Settings,
  Ticket,
  UserRound,
  WalletCards,
} from "lucide-react";

const links = [
  { href: "/customer-panel", label: "پیشخوان", icon: Home },
  { href: "/customer-panel/orders", label: "سفارش‌ها", icon: Package },
  { href: "/customer-panel/wishlist", label: "علاقه‌مندی‌ها", icon: Heart },
  { href: "/customer-panel/wallet", label: "کیف پول", icon: WalletCards },
  { href: "/customer-panel/gift-cards", label: "کارت هدیه", icon: CreditCard },
  { href: "/customer-panel/addresses", label: "آدرس‌ها", icon: MapPin },
  { href: "/customer-panel/notifications", label: "اعلان‌ها", icon: Bell },
  { href: "/customer-panel/profile", label: "پروفایل", icon: UserRound },
] as const;

/**
 * ساختار قفل‌شدهٔ پنل مشتری Shopeiva: نوار تبلیغ، هدر، نوار حساب، سایدبار و محتوای کارت‌محور.
 */
export function CustomerPanelShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  return (
    <div className="min-h-screen bg-[#f7f8fa] text-gray-900" dir="rtl">
      <div className="h-11 bg-[#d6aa72] text-white flex items-center justify-center px-4 text-sm font-bold">
        محصولات گرم و محبوب در جشنوارهٔ حس خوب خرید
      </div>
      <header className="bg-white border-b border-gray-200">
        <div className="max-w-[1440px] mx-auto h-20 px-4 sm:px-8 flex items-center gap-4">
          <Link href="/" className="shrink-0" aria-label="فروشگاه توبا">
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img src="/images/logos/logo.svg" alt="توبا" className="h-10 w-auto" />
          </Link>
          <div className="hidden md:flex flex-1 max-w-md relative me-8">
            <Search className="absolute right-4 top-3 w-4 h-4 text-gray-400" />
            <input
              readOnly
              aria-label="جستجو در پنل"
              placeholder="جستجو..."
              className="w-full h-10 rounded-xl bg-gray-50 border border-gray-100 pr-11 pl-4 text-sm"
            />
          </div>
          <div className="me-auto flex items-center gap-1.5">
            <Link href="/customer-panel/notifications" className="p-2.5 rounded-xl hover:bg-gray-50" aria-label="اعلان‌ها">
              <Bell className="w-5 h-5" />
            </Link>
            <button type="button" className="p-2.5 rounded-xl hover:bg-gray-50" aria-label="حالت نمایش">
              <Moon className="w-5 h-5" />
            </button>
            <Link href="/customer-panel/orders" className="p-2.5 rounded-xl hover:bg-gray-50 relative" aria-label="سفارش‌ها">
              <Package className="w-5 h-5" />
            </Link>
            <Link
              href="/customer-panel/profile"
              className="flex items-center gap-2 border border-gray-200 rounded-xl px-3 py-2 text-sm"
            >
              <CircleUserRound className="w-5 h-5 text-[#2563EB]" />
              <span className="hidden sm:inline">حساب مشتری</span>
              <ChevronDown className="w-4 h-4 text-gray-400" />
            </Link>
          </div>
        </div>
      </header>

      <div className="bg-white border-b border-gray-200">
        <div className="max-w-[1440px] mx-auto h-16 px-4 sm:px-8 flex items-center gap-3">
          <button type="button" className="lg:hidden p-2 rounded-lg" aria-label="منوی پنل">
            <Menu className="w-5 h-5" />
          </button>
          <div className="w-9 h-9 rounded-xl bg-[#2563EB] text-white flex items-center justify-center font-black">م</div>
          <span className="font-bold text-sm">مشتری توبا</span>
          <Link href="/" className="me-auto flex items-center gap-2 text-sm text-gray-600">
            <LogOut className="w-4 h-4 text-[#2563EB]" />
            بازگشت به فروشگاه
          </Link>
        </div>
      </div>

      <div className="max-w-[1440px] mx-auto px-4 sm:px-8 py-5 lg:py-7 flex gap-6">
        <aside className="hidden lg:block w-60 shrink-0">
          <nav className="bg-white rounded-2xl border border-gray-100 p-3 shadow-sm sticky top-5" aria-label="منوی مشتری">
            {links.map((item) => {
              const Icon = item.icon;
              const active = item.href === "/customer-panel" ? pathname === item.href : pathname.startsWith(item.href);
              return (
                <Link
                  key={item.href}
                  href={item.href}
                  className={`flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm mb-1 ${
                    active ? "bg-blue-50 text-[#2563EB] font-bold" : "text-gray-600 hover:bg-gray-50"
                  }`}
                >
                  <Icon className="w-4.5 h-4.5" />
                  {item.label}
                </Link>
              );
            })}
            <div className="border-t border-gray-100 mt-2 pt-2">
              <span className="flex items-center gap-3 px-3 py-2 text-sm text-gray-400">
                <Ticket className="w-4 h-4" />
                تیکت‌ها
              </span>
              <span className="flex items-center gap-3 px-3 py-2 text-sm text-gray-400">
                <Settings className="w-4 h-4" />
                تنظیمات
              </span>
            </div>
          </nav>
        </aside>
        <main className="min-w-0 flex-1">{children}</main>
      </div>
    </div>
  );
}
