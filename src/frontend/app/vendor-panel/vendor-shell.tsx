"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useMemo, useState, type ReactNode } from "react";
import {
  Award,
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
  Shield,
  Star,
  Store,
  RotateCcw,
  Tag,
  Ticket,
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
import {
  capabilityPermissionIds,
  createSellerAccessApi,
  hasViewCapability,
} from "../access-control/access-control-api";

type NavItem = {
  id: string;
  label: string;
  href: string;
  icon: typeof LayoutDashboard;
  live: boolean;
  /** مجوز *.view لازم برای نمایش؛ بدون آن در صورت live همیشه دیده می‌شود. */
  viewPermission?: string;
};

/**
 * ترتیب نزدیک به Shopeiva vendor nav؛ فقط مسیرهای زندهٔ Host.
 * Access Control بلافاصله قبل از Settings با زبان Vendor.
 */
const menuItems: NavItem[] = [
  { id: "dashboard", label: "داشبورد", icon: LayoutDashboard, href: "/vendor-panel", live: true },
  { id: "products", label: "محصولات", icon: Package, href: "/vendor-panel/products", live: true, viewPermission: "product.view" },
  { id: "orders", label: "سفارشات", icon: ShoppingBag, href: "/vendor-panel/orders", live: true, viewPermission: "order.view" },
  { id: "analytics", label: "آمار و نمودار", icon: BarChart3, href: "/vendor-panel/analytics", live: true },
  { id: "coupons", label: "تخفیف‌ها", icon: Tag, href: "/vendor-panel/coupons", live: true, viewPermission: "promotion.view" },
  { id: "reviews", label: "نظرات", icon: Star, href: "/vendor-panel/reviews", live: true, viewPermission: "review.view" },
  { id: "wallet", label: "کیف پول", icon: Wallet, href: "/vendor-panel/wallet", live: true, viewPermission: "settlement.view" },
  { id: "tickets", label: "تیکت‌ها", icon: Ticket, href: "/vendor-panel/tickets", live: true, viewPermission: "support.view" },
  { id: "notifications", label: "اطلاعیه‌ها", icon: Bell, href: "/vendor-panel/notifications", live: true },
  { id: "stories", label: "استوری‌ها", icon: Images, href: "/vendor-panel/stories", live: true, viewPermission: "story.view" },
  { id: "fulfillments", label: "ارسال", icon: Truck, href: "/vendor-panel/fulfillments", live: true, viewPermission: "fulfillment.view" },
  { id: "returns", label: "مرجوعی", icon: RotateCcw, href: "/vendor-panel/returns", live: true, viewPermission: "return.view" },
  { id: "access-control", label: "کنترل دسترسی", icon: Shield, href: "/vendor-panel/access-control", live: true, viewPermission: "accesscontrol.view" },
  { id: "settings", label: "تنظیمات", icon: Settings, href: "/vendor-panel/settings", live: true },
];

/** قابلیت‌های عمداً از nav حذف‌شده — deep-link فقط. */
export const VENDOR_DEFERRED_NAV_HREFS = [
  "/vendor-panel/customers",
  "/vendor-panel/gift-cards",
] as const;

/** فهرست hrefهای زنده Seller برای بستهٔ اسکرین‌شات. */
export function listLiveVendorNavHrefs(): string[] {
  return menuItems.filter((item) => item.live).map((item) => item.href);
}

function isActivePath(pathname: string, href: string): boolean {
  if (href === "/vendor-panel") {
    return pathname === href;
  }
  return pathname === href || pathname.startsWith(`${href}/`);
}

function itemAllowed(item: NavItem, caps: Set<string> | null): boolean {
  if (!item.live) return false;
  if (!item.viewPermission) return true;
  if (caps === null) return true;
  return hasViewCapability(caps, item.viewPermission);
}

function VendorShellSkeleton() {
  return (
    <div className="min-h-screen bg-gray-50 flex flex-col overflow-x-hidden" data-testid="vendor-shell-loading" dir="rtl">
      <div className="sticky top-0 z-40 h-[65px] border-b border-gray-200 bg-white px-4 lg:px-6 flex items-center gap-3">
        <div className="h-8 w-8 rounded-xl bg-gray-200 animate-pulse" />
        <div className="h-4 w-28 rounded bg-gray-200 animate-pulse" />
        <div className="ms-auto h-4 w-24 rounded bg-gray-100 animate-pulse hidden md:block" />
      </div>
      <div className="flex flex-1">
        <div className="hidden lg:block w-64 border-l border-gray-200 bg-white p-4 space-y-2">
          {Array.from({ length: 8 }).map((_, i) => (
            <div key={i} className="h-10 rounded-xl bg-gray-100 animate-pulse" />
          ))}
        </div>
        <div className="flex-1 p-4 md:p-6 lg:p-8 space-y-4">
          <div className="h-24 rounded-2xl bg-white border border-gray-200 animate-pulse" />
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
            {Array.from({ length: 4 }).map((_, i) => (
              <div key={i} className="h-28 rounded-2xl bg-white border border-gray-200 animate-pulse" />
            ))}
          </div>
          <div className="h-48 rounded-2xl bg-white border border-gray-200 animate-pulse" />
        </div>
      </div>
    </div>
  );
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
  const [caps, setCaps] = useState<Set<string> | null>(null);

  useEffect(() => {
    void loadSellerDevContexts().then(async (rows) => {
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
      try {
        const effective = await createSellerAccessApi().getMyCapabilities();
        setCaps(capabilityPermissionIds(effective));
      } catch {
        setCaps(null);
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

  const visibleMenuItems = useMemo(() => menuItems.filter((item) => itemAllowed(item, caps)), [caps]);

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
    return <VendorShellSkeleton />;
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
              data-testid="vendor-panel-mobile-menu"
            >
              <Menu className="w-5 h-5 text-gray-700" />
            </button>
            <div className="flex items-center gap-2 min-w-0">
              <div className="w-8 h-8 rounded-xl bg-[#2563EB] flex items-center justify-center shadow-sm shrink-0">
                <Store className="w-4 h-4 text-white" />
              </div>
              <span className="font-bold text-gray-900 hidden sm:block">پنل فروشنده</span>
            </div>
          </div>
          <div className="flex items-center gap-3 shrink-0">
            <div className="hidden md:flex items-center gap-2 text-xs text-gray-500">
              <Award className="w-4 h-4 text-amber-500" />
              <span>فروشنده ویژه</span>
            </div>
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
            <span className="text-sm text-gray-600 truncate max-w-[100px] sm:max-w-none">{sellerLabel}</span>
            <Link
              href="/"
              className="p-2 rounded-lg hover:bg-red-50 text-red-500 transition-colors"
              aria-label="خروج به فروشگاه"
              title="خروج"
            >
              <LogOut className="w-5 h-5" />
            </Link>
          </div>
        </div>
      </header>

      <div className="flex flex-1 relative">
        <aside
          className={`hidden lg:block bg-white border-l border-gray-200 shrink-0 transition-all duration-300 ease-in-out sticky top-[65px] h-[calc(100vh-65px)] overflow-y-auto scrollbar-thin ${
            sidebarOpen ? "w-64 translate-x-0" : "w-0 -translate-x-full opacity-0"
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
          <div
            className="absolute inset-0 bg-black/60 backdrop-blur-sm transition-opacity"
            onClick={() => setMobileOpen(false)}
          />
          <aside className="absolute right-0 top-0 h-full w-[280px] bg-white shadow-2xl flex flex-col animate-in slide-in-from-right duration-300">
            <div className="flex items-center justify-between p-4 border-b border-gray-200 shrink-0">
              <div className="flex items-center gap-2">
                <div className="w-8 h-8 rounded-xl bg-[#2563EB] flex items-center justify-center">
                  <Store className="w-4 h-4 text-white" />
                </div>
                <span className="text-lg font-bold text-gray-900">پنل فروشنده</span>
              </div>
              <button type="button" onClick={() => setMobileOpen(false)} className="p-2 rounded-lg hover:bg-gray-100 transition-colors" aria-label="بستن">
                <X className="w-5 h-5 text-gray-700" />
              </button>
            </div>
            <nav className="flex-1 overflow-y-auto p-4 space-y-1">
              {visibleMenuItems.map((item) => (
                <NavLink key={item.id} item={item} pathname={pathname} onNavigate={() => setMobileOpen(false)} dense />
              ))}
            </nav>
            <div className="p-4 border-t border-gray-200 shrink-0 bg-gray-50">
              <Link
                href="/"
                className="flex items-center justify-center gap-2 px-4 py-3 w-full rounded-xl text-sm font-medium text-red-500 bg-red-50 hover:bg-red-100 transition-all"
              >
                <LogOut className="w-5 h-5" />
                <span>خروج از حساب</span>
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
