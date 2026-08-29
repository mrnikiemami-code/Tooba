"use client";

import Link from "next/link";
import { usePathname, useSearchParams } from "next/navigation";
import { useEffect, useMemo, useState, type ReactNode } from "react";
import {
  ChevronLeft,
  Gift,
  LayoutDashboard,
  LayoutTemplate,
  LogOut,
  Menu,
  Package,
  Plus,
  Settings,
  Shield,
  ShoppingBag,
  Sparkles,
  Star,
  Store,
  RotateCcw,
  Ticket,
  Truck,
  Users,
  Wallet,
  WalletCards,
  FileText,
  Tag,
  Tags,
  FolderTree,
  X,
} from "lucide-react";
import { prepareAdminDevActor } from "./admin-api";
import {
  capabilityPermissionIds,
  createAdminAccessApi,
  hasViewCapability,
} from "../access-control/access-control-api";
import {
  adminNavLabels,
  resolveAdminChromeLocale,
  type AdminNavLabels,
} from "./admin-chrome-messages";
import { isActiveAdminNavItem } from "./admin-nav-active";

type NavItemDef = {
  id: string;
  labelKey: keyof AdminNavLabels;
  href: string;
  icon: typeof LayoutDashboard;
  live: boolean;
  exact?: boolean;
  viewPermission?: string;
};

type NavGroupDef = {
  id: string;
  labelKey: keyof AdminNavLabels;
  items: NavItemDef[];
};

/** ناوبری عملیاتی Admin؛ برچسب‌ها از admin-chrome-messages می‌آیند نه انگلیسی خام. */
const navGroupDefs: NavGroupDef[] = [
  {
    id: "ops",
    labelKey: "groupOps",
    items: [
      { id: "dashboard", labelKey: "dashboard", href: "/admin", icon: LayoutDashboard, live: true, exact: true, viewPermission: "admin.dashboard.view" },
      { id: "orders", labelKey: "orders", href: "/admin/orders", icon: ShoppingBag, live: true, viewPermission: "order.view" },
      { id: "fulfillments", labelKey: "fulfillments", href: "/admin/fulfillments", icon: Truck, live: true, viewPermission: "fulfillment.view" },
      { id: "returns", labelKey: "returns", href: "/admin/returns", icon: RotateCcw, live: true, viewPermission: "return.view" },
      { id: "settlement", labelKey: "settlement", href: "/admin/settlement", icon: Wallet, live: true, viewPermission: "settlement.view" },
      { id: "payouts", labelKey: "payouts", href: "/admin/payouts", icon: Wallet, live: true, viewPermission: "settlement.view" },
      { id: "content", labelKey: "content", href: "/admin/content", icon: FileText, live: true, viewPermission: "content.view" },
      { id: "stories", labelKey: "stories", href: "/admin/stories", icon: Sparkles, live: true, viewPermission: "story.view" },
      { id: "page-composition", labelKey: "pageComposition", href: "/admin/page-composition", icon: LayoutTemplate, live: true, viewPermission: "pagecomposition.view" },
    ],
  },
  {
    id: "catalog-categories",
    labelKey: "groupCatalogCategories",
    items: [
      { id: "catalog-categories", labelKey: "catalogCategories", href: "/admin/catalog/categories", icon: FolderTree, live: true, viewPermission: "product.view" },
      { id: "catalog-attributes", labelKey: "catalogAttributes", href: "/admin/catalog/attributes", icon: Tags, live: true, viewPermission: "catalog.attribute.view" },
    ],
  },
  {
    id: "products",
    labelKey: "groupProducts",
    items: [
      { id: "products", labelKey: "productList", href: "/admin/products", icon: Package, live: true, viewPermission: "product.view" },
      { id: "product-create", labelKey: "productCreate", href: "/admin/products?create=1", icon: Plus, live: true, viewPermission: "product.view" },
    ],
  },
  {
    id: "market",
    labelKey: "groupMarket",
    items: [
      { id: "sellers", labelKey: "sellers", href: "/admin/sellers", icon: Store, live: true, viewPermission: "seller.view" },
      { id: "customers", labelKey: "customers", href: "/admin/customers", icon: Users, live: true },
    ],
  },
  {
    id: "moderation",
    labelKey: "groupModeration",
    items: [
      { id: "reviews", labelKey: "reviews", href: "/admin/reviews", icon: Star, live: true, viewPermission: "review.view" },
      { id: "tickets", labelKey: "tickets", href: "/admin/tickets", icon: Ticket, live: true, viewPermission: "support.view" },
      { id: "gift-cards", labelKey: "giftCards", href: "/admin/gift-cards", icon: Gift, live: true, viewPermission: "giftcard.view" },
      { id: "wallets", labelKey: "wallets", href: "/admin/wallets", icon: WalletCards, live: true, viewPermission: "wallet.view" },
      { id: "promotions", labelKey: "promotions", href: "/admin/promotions", icon: Tag, live: true, viewPermission: "promotion.view" },
    ],
  },
  {
    id: "system",
    labelKey: "groupSystem",
    items: [
      { id: "settings", labelKey: "settings", href: "/admin/settings", icon: Settings, live: true },
      { id: "access-control", labelKey: "accessControl", href: "/admin/access-control", icon: Shield, live: true, viewPermission: "accesscontrol.view" },
    ],
  },
];

/** قابلیت‌های عمداً از nav حذف‌شده — deep-link فقط. */
export const ADMIN_DEFERRED_NAV_HREFS = ["/admin/catalog/category-schema"] as const;

/** فهرست hrefهای زنده Admin برای بستهٔ اسکرین‌شات. */
export function listLiveAdminNavHrefs(): string[] {
  return navGroupDefs.flatMap((g) => g.items.filter((i) => i.live).map((i) => i.href));
}

export { isActiveAdminNavItem } from "./admin-nav-active";

function isActivePath(pathname: string, search: string, item: NavItemDef, siblings: NavItemDef[]): boolean {
  return isActiveAdminNavItem(pathname, search, item, siblings);
}

function crumbFor(pathname: string, search: string, labels: AdminNavLabels): string {
  for (const group of navGroupDefs) {
    for (const item of group.items) {
      if (isActivePath(pathname, search, item, group.items)) {
        return labels[item.labelKey];
      }
    }
  }
  return labels.operations;
}

function itemAllowed(item: NavItemDef, caps: Set<string> | null): boolean {
  if (!item.live) return false;
  if (!item.viewPermission) return true;
  if (caps === null) return true;
  return hasViewCapability(caps, item.viewPermission);
}

/**
 * پوستهٔ Admin حرفه‌ای با زبان بصری Shopeiva Vendor/Account
 * (header چسبان + sidebar + drawer) و هویت عملیاتی جدا از Seller Panel.
 */
export function AdminShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const search = searchParams?.toString() ?? "";
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [ready, setReady] = useState(false);
  const [caps, setCaps] = useState<Set<string> | null>(null);
  const [labels, setLabels] = useState<AdminNavLabels>(() => adminNavLabels("fa"));

  useEffect(() => {
    setLabels(adminNavLabels(resolveAdminChromeLocale()));
    void prepareAdminDevActor()
      .then(async () => {
        try {
          const effective = await createAdminAccessApi().getMyCapabilities();
          setCaps(capabilityPermissionIds(effective));
        } catch {
          setCaps(null);
        }
      })
      .finally(() => setReady(true));
  }, []);

  useEffect(() => {
    document.body.style.overflow = mobileOpen ? "hidden" : "";
    return () => {
      document.body.style.overflow = "";
    };
  }, [mobileOpen]);

  const visibleGroups = useMemo(
    () =>
      navGroupDefs
        .map((group) => ({
          ...group,
          label: labels[group.labelKey],
          items: group.items
            .filter((item) => itemAllowed(item, caps))
            .map((item) => ({ ...item, label: labels[item.labelKey] })),
        }))
        .filter((group) => group.items.length > 0),
    [caps, labels],
  );

  if (!ready) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-gray-50 text-gray-500" data-testid="admin-shell-loading">
        در حال آماده‌سازی پنل مدیریت…
      </div>
    );
  }

  const crumb = crumbFor(pathname, search, labels);

  return (
    <div className="min-h-screen bg-gray-50 flex flex-col overflow-x-hidden" dir="rtl" data-testid="admin-panel-shell">
      <header className="sticky top-0 z-40 bg-white border-b border-gray-200 h-[65px] flex items-center transition-shadow duration-200" data-testid="admin-panel-header">
        <div className="flex items-center justify-between w-full px-4 lg:px-6 gap-3">
          <div className="flex items-center gap-3 min-w-0">
            <button
              type="button"
              onClick={() => setSidebarOpen((open) => !open)}
              className="hidden lg:flex p-2 rounded-lg hover:bg-gray-100 transition-colors"
              aria-label={labels.closeMenu}
            >
              <Menu className="w-5 h-5 text-gray-700" />
            </button>
            <button
              type="button"
              onClick={() => setMobileOpen(true)}
              className="lg:hidden p-2 rounded-lg hover:bg-gray-100 transition-colors"
              aria-label={labels.openMenu}
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
            <Link href="/" className="p-2 rounded-lg hover:bg-red-50 text-red-500 transition-colors" aria-label={labels.signOut} title={labels.signOut}>
              <LogOut className="w-5 h-5" />
            </Link>
          </div>
        </div>
      </header>

      <div className="flex flex-1 relative">
        <aside
          className={`hidden lg:block bg-white border-l border-gray-200 shrink-0 transition-all duration-300 ease-out sticky top-[65px] h-[calc(100vh-65px)] overflow-y-auto motion-reduce:transition-none ${
            sidebarOpen ? "w-64" : "w-0 opacity-0"
          }`}
          data-testid="admin-panel-sidebar"
        >
          <div className="p-4 space-y-5 min-w-[250px]">
            {visibleGroups.map((group) => (
              <div key={group.id}>
                <p className="px-3 mb-1 text-[10px] font-bold tracking-wide text-gray-400">{group.label}</p>
                <nav className="space-y-1" aria-label={group.label} data-testid={group.id === "ops" ? "admin-panel-nav-live-only" : undefined}>
                  {group.items.map((item) => (
                    <NavLink key={item.id} item={item} pathname={pathname} search={search} siblings={group.items} />
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
          <div className="absolute inset-0 bg-black/60 backdrop-blur-sm transition-opacity" onClick={() => setMobileOpen(false)} />
          <aside className="absolute right-0 top-0 h-full w-[280px] bg-white shadow-2xl flex flex-col animate-in slide-in-from-right duration-200 motion-reduce:animate-none">
            <div className="flex items-center justify-between p-4 border-b border-gray-200">
              <div className="flex items-center gap-2">
                <div className="w-8 h-8 rounded-xl bg-[#2563EB] flex items-center justify-center">
                  <Shield className="w-4 h-4 text-white" />
                </div>
                <span className="font-bold text-gray-900">مدیریت توبا</span>
              </div>
              <button type="button" onClick={() => setMobileOpen(false)} className="p-2 rounded-lg hover:bg-gray-100" aria-label={labels.closeMenu}>
                <X className="w-5 h-5" />
              </button>
            </div>
            <div className="flex-1 overflow-y-auto p-4 space-y-5">
              {visibleGroups.map((group) => (
                <div key={group.id}>
                  <p className="px-3 mb-1 text-[10px] font-bold text-gray-400">{group.label}</p>
                  <nav className="space-y-1">
                    {group.items.map((item) => (
                      <NavLink
                        key={item.id}
                        item={item}
                        pathname={pathname}
                        search={search}
                        siblings={group.items}
                        onNavigate={() => setMobileOpen(false)}
                        dense
                      />
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
  search,
  siblings,
  onNavigate,
  dense,
}: {
  item: NavItemDef & { label: string };
  pathname: string;
  search: string;
  siblings: Array<NavItemDef & { label: string }>;
  onNavigate?: () => void;
  dense?: boolean;
}) {
  const active = isActivePath(pathname, search, item, siblings);
  return (
    <Link
      href={item.href}
      onClick={onNavigate}
      className={`flex items-center gap-3 rounded-xl text-sm font-medium transition-all duration-150 motion-reduce:transition-none ${
        dense ? "px-4 py-3" : "px-3 py-2.5"
      } ${active ? "bg-[#2563EB] text-white shadow-md shadow-[#2563EB]/20" : "text-gray-700 hover:bg-gray-100"}`}
      data-testid={`admin-nav-${item.id}`}
      data-live={item.live ? "true" : "false"}
      aria-current={active ? "page" : undefined}
    >
      <item.icon className="w-5 h-5 shrink-0" />
      <span className="flex-1 truncate">{item.label}</span>
      {!item.live ? <span className={`text-[10px] ${active ? "text-white/80" : "text-gray-400"}`}>به‌زودی</span> : null}
      {active ? <ChevronLeft className="w-4 h-4 shrink-0" /> : null}
    </Link>
  );
}
