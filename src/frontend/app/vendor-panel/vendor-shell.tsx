"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useState, type ReactNode } from "react";
import {
  BarChart3,
  Bell,
  ChevronLeft,
  Images,
  LayoutDashboard,
  LogOut,
  Menu,
  Package,
  Settings,
  ShoppingBag,
  Star,
  Store,
  RotateCcw,
  Tag,
  Truck,
  Wallet,
  X,
} from "lucide-react";
import {
  DEFAULT_SELLER_PARTY_ID,
  loadSellerDevContexts,
  readActorUserId,
  readSellerPartyId,
  writeActorUserId,
  writeSellerPartyId,
  type SellerDevContext,
} from "./seller-api";

type NavItem = {
  id: string;
  label: string;
  href: string;
  icon: typeof LayoutDashboard;
  live: boolean;
};

/** فقط مسیرهای زندهٔ Host در ناوبری. */
const menuItems: NavItem[] = [
  { id: "dashboard", label: "داشبورد", icon: LayoutDashboard, href: "/vendor-panel", live: true },
  { id: "products", label: "محصولات", icon: Package, href: "/vendor-panel/products", live: true },
  { id: "orders", label: "سفارشات", icon: ShoppingBag, href: "/vendor-panel/orders", live: true },
  { id: "notifications", label: "اطلاعیه‌ها", icon: Bell, href: "/vendor-panel/notifications", live: true },
  { id: "stories", label: "استوری‌ها", icon: Images, href: "/vendor-panel/stories", live: true },
  { id: "coupons", label: "تخفیف‌ها", icon: Tag, href: "/vendor-panel/coupons", live: true },
  { id: "reviews", label: "نظرات", icon: Star, href: "/vendor-panel/reviews", live: true },
  { id: "fulfillments", label: "ارسال", icon: Truck, href: "/vendor-panel/fulfillments", live: true },
  { id: "returns", label: "مرجوعی", icon: RotateCcw, href: "/vendor-panel/returns", live: true },
  { id: "analytics", label: "آمار و نمودار", icon: BarChart3, href: "/vendor-panel/analytics", live: true },
  { id: "wallet", label: "کیف پول", icon: Wallet, href: "/vendor-panel/wallet", live: true },
  { id: "settings", label: "تنظیمات", icon: Settings, href: "/vendor-panel/settings", live: true },
];

/** قابلیت‌های عمداً از nav حذف‌شده — deep-link فقط. */
export const VENDOR_DEFERRED_NAV_HREFS = [
  "/vendor-panel/customers",
  "/vendor-panel/tickets",
  "/vendor-panel/gift-cards",
] as const;

const visibleMenuItems = menuItems.filter((item) => item.live);

function isActivePath(pathname: string, href: string): boolean {
  if (href === "/vendor-panel") {
    return pathname === href;
  }
  return pathname === href || pathname.startsWith(`${href}/`);
}

/**
 * پوستهٔ پنل فروشنده مطابق layout واقعی Shopeiva vendor-panel.
 * زمینهٔ Actor+Seller برای مرز SpiceDB حفظ می‌شود. accent Tooba آبی است.
 */
export function VendorShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [contexts, setContexts] = useState<SellerDevContext[]>([]);
  const [sellerPartyId, setSellerPartyId] = useState<string>(() =>
    typeof window !== "undefined" ? (readSellerPartyId(window.location.search) ?? DEFAULT_SELLER_PARTY_ID) : DEFAULT_SELLER_PARTY_ID,
  );
  const [actorUserId, setActorUserId] = useState<string>(() =>
    typeof window !== "undefined" ? (readActorUserId() ?? "") : "",
  );
  const [sellerLabel, setSellerLabel] = useState("فروشنده");
  const [ready, setReady] = useState(false);

  useEffect(() => {
    void loadSellerDevContexts().then((rows) => {
      setContexts(rows);
      const existingSeller = readSellerPartyId(window.location.search) ?? DEFAULT_SELLER_PARTY_ID;
      const existingActor = readActorUserId();
      const exact =
        rows.find((row) => row.actorUserId === existingActor && row.sellerPartyId === existingSeller) ?? null;
      if (exact) {
        writeSellerPartyId(exact.sellerPartyId);
        writeActorUserId(exact.actorUserId);
        setSellerPartyId(exact.sellerPartyId);
        setActorUserId(exact.actorUserId);
        setSellerLabel(exact.sellerLabel);
      } else if (existingActor && existingSeller) {
        writeActorUserId(existingActor);
        writeSellerPartyId(existingSeller);
        setActorUserId(existingActor);
        setSellerPartyId(existingSeller);
        setSellerLabel("زمینهٔ غیرمجاز");
      } else if (rows[0]) {
        const fallback = rows[0];
        writeSellerPartyId(fallback.sellerPartyId);
        writeActorUserId(fallback.actorUserId);
        setSellerPartyId(fallback.sellerPartyId);
        setActorUserId(fallback.actorUserId);
        setSellerLabel(fallback.sellerLabel);
      }
      setReady(true);
    });
  }, []);

  useEffect(() => {
    document.body.style.overflow = mobileOpen ? "hidden" : "";
    return () => {
      document.body.style.overflow = "";
    };
  }, [mobileOpen]);

  function onContextChange(nextKey: string) {
    const [nextActor, nextSeller] = nextKey.split("|");
    if (!nextActor || !nextSeller) {
      return;
    }
    if (nextActor === actorUserId && nextSeller === sellerPartyId) {
      return;
    }
    writeActorUserId(nextActor);
    writeSellerPartyId(nextSeller);
    setActorUserId(nextActor);
    setSellerPartyId(nextSeller);
    const match = contexts.find((row) => row.actorUserId === nextActor && row.sellerPartyId === nextSeller);
    setSellerLabel(match?.sellerLabel ?? "فروشنده");
    window.location.assign(window.location.pathname + window.location.search);
  }

  if (!ready) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-gray-50 text-gray-500" data-testid="vendor-shell-loading">
        در حال آماده‌سازی پنل فروشنده…
      </div>
    );
  }

  const contextKey = actorUserId && sellerPartyId ? `${actorUserId}|${sellerPartyId}` : "";

  return (
    <div className="min-h-screen bg-gray-50 flex flex-col overflow-x-hidden" dir="rtl" data-testid="vendor-panel-shell">
      <header className="sticky top-0 z-40 bg-white border-b border-gray-200 h-[65px] flex items-center" data-testid="vendor-panel-header">
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
              data-testid="vendor-panel-mobile-menu"
            >
              <Menu className="w-5 h-5 text-gray-700" />
            </button>
            <div className="flex items-center gap-2 min-w-0">
              <div className="w-8 h-8 rounded-xl bg-[#2563EB] flex items-center justify-center shadow-sm shrink-0">
                <Store className="w-4 h-4 text-white" />
              </div>
              <div className="min-w-0">
                <p className="font-bold text-gray-900 text-sm truncate">{sellerLabel}</p>
                <p className="text-[10px] text-gray-500 hidden sm:block">پنل فروشنده</p>
              </div>
            </div>
          </div>
          <div className="flex items-center gap-2 shrink-0">
            <label className="hidden md:flex flex-col text-[10px] text-gray-500">
              زمینهٔ مجاز
              <select
                className="mt-0.5 min-h-9 max-w-[14rem] rounded-xl border border-gray-200 bg-white px-2 text-xs text-gray-800"
                value={contextKey}
                onChange={(event) => onContextChange(event.target.value)}
                data-testid="seller-context-select"
              >
                {contexts.length === 0 ? (
                  <option value="">…</option>
                ) : (
                  contexts.map((row) => (
                    <option key={`${row.actorUserId}|${row.sellerPartyId}`} value={`${row.actorUserId}|${row.sellerPartyId}`}>
                      {row.actorLabel} → {row.sellerLabel}
                    </option>
                  ))
                )}
              </select>
            </label>
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
          data-testid="vendor-panel-sidebar"
        >
          <nav className="p-4 space-y-1 min-w-[250px]" aria-label="منوی فروشنده" data-testid="vendor-panel-nav-live-only">
            {visibleMenuItems.map((item) => (
              <NavLink key={item.id} item={item} pathname={pathname} />
            ))}
          </nav>
        </aside>

        <main className="flex-1 min-w-0 w-full p-4 md:p-6 lg:p-8" data-testid="vendor-panel-main">
          <div className="md:hidden mb-4">
            <label className="flex flex-col text-[10px] text-gray-500">
              زمینهٔ مجاز (Actor + فروشنده)
              <select
                className="mt-1 min-h-11 w-full rounded-xl border border-gray-200 bg-white px-3 text-sm"
                value={contextKey}
                onChange={(event) => onContextChange(event.target.value)}
              >
                {contexts.map((row) => (
                  <option key={`${row.actorUserId}|${row.sellerPartyId}`} value={`${row.actorUserId}|${row.sellerPartyId}`}>
                    {row.actorLabel} → {row.sellerLabel}
                  </option>
                ))}
              </select>
            </label>
          </div>
          {children}
        </main>
      </div>

      {mobileOpen ? (
        <div className="lg:hidden fixed inset-0 z-50" role="dialog" aria-modal="true" data-testid="vendor-panel-drawer">
          <div className="absolute inset-0 bg-black/60 backdrop-blur-sm" onClick={() => setMobileOpen(false)} />
          <aside className="absolute right-0 top-0 h-full w-[280px] bg-white shadow-2xl flex flex-col">
            <div className="flex items-center justify-between p-4 border-b border-gray-200">
              <div className="flex items-center gap-2">
                <div className="w-8 h-8 rounded-xl bg-[#2563EB] flex items-center justify-center">
                  <Store className="w-4 h-4 text-white" />
                </div>
                <span className="font-bold text-gray-900">پنل فروشنده</span>
              </div>
              <button type="button" onClick={() => setMobileOpen(false)} className="p-2 rounded-lg hover:bg-gray-100" aria-label="بستن">
                <X className="w-5 h-5" />
              </button>
            </div>
            <nav className="flex-1 overflow-y-auto p-4 space-y-1">
              {visibleMenuItems.map((item) => (
                <NavLink key={item.id} item={item} pathname={pathname} onNavigate={() => setMobileOpen(false)} dense />
              ))}
            </nav>
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
  const active = isActivePath(pathname, item.href);
  return (
    <Link
      href={item.href}
      onClick={onNavigate}
      className={`flex items-center gap-3 rounded-xl text-sm font-medium transition-all ${
        dense ? "px-4 py-3" : "px-3 py-2.5"
      } ${active ? "bg-[#2563EB] text-white shadow-md shadow-[#2563EB]/20" : "text-gray-700 hover:bg-gray-100"}`}
      data-testid={`vendor-nav-${item.id}`}
      data-live={item.live ? "true" : "false"}
    >
      <item.icon className="w-5 h-5 shrink-0" />
      <span className="flex-1 truncate">{item.label}</span>
      {!item.live ? <span className={`text-[10px] ${active ? "text-white/80" : "text-gray-400"}`}>به‌زودی</span> : null}
      {active ? <ChevronLeft className="w-4 h-4 shrink-0" /> : null}
    </Link>
  );
}
