"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useState, type ReactNode } from "react";
import { LayoutDashboard, Menu, Package, ShoppingBag, X } from "lucide-react";
import { DEFAULT_SELLER_PARTY_ID, readSellerPartyId, writeSellerPartyId } from "./seller-api";

const DEMO_SELLERS = [
  { id: "01a030d1-40cb-7000-8abe-6d31739956c5", label: "فروشگاه آرمان" },
  { id: "01a030d1-40db-7000-b90c-a0705133f0eb", label: "دیجی‌استایل نمونه" },
];

const nav = [
  { href: "/vendor-panel", label: "داشبورد", icon: LayoutDashboard, exact: true },
  { href: "/vendor-panel/products", label: "محصولات", icon: Package, exact: false },
  { href: "/vendor-panel/orders", label: "سفارش‌ها", icon: ShoppingBag, exact: false },
];

/**
 * پوستهٔ Vendor Panel به سبک Shopeiva با ناوبری موبایل و انتخاب فروشندهٔ demo.
 */
export function VendorShell({ children }: { children: ReactNode }) {
  const path = usePathname();
  const [menuOpen, setMenuOpen] = useState(false);
  const [sellerPartyId, setSellerPartyId] = useState<string>(DEFAULT_SELLER_PARTY_ID);

  useEffect(() => {
    const existing = readSellerPartyId(window.location.search) ?? DEFAULT_SELLER_PARTY_ID;
    writeSellerPartyId(existing);
    setSellerPartyId(existing);
  }, []);

  function onSellerChange(next: string) {
    if (!next || next === sellerPartyId) {
      return;
    }
    writeSellerPartyId(next);
    setSellerPartyId(next);
    window.location.assign(window.location.pathname + window.location.search);
  }

  function NavItems({ onNavigate }: { onNavigate?: () => void }) {
    return (
      <nav className="flex flex-1 flex-col gap-1 p-3 text-base" aria-label="Seller">
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
                  ? "flex items-center gap-3 rounded-ds bg-primary px-3 py-2.5 text-sm font-medium text-primary-foreground"
                  : "flex items-center gap-3 rounded-ds px-3 py-2.5 text-sm text-foreground hover:bg-secondary"
              }
            >
              <Icon className="size-4 shrink-0 opacity-80" aria-hidden />
              {item.label}
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
          <p className="mt-1 text-xl font-semibold">پنل فروشنده</p>
          <p className="mt-2 text-sm text-muted">Vendor Panel</p>
        </div>
        <NavItems />
      </aside>
      {menuOpen ? (
        <div className="fixed inset-0 z-40 md:hidden">
          <button type="button" className="absolute inset-0 bg-foreground/40" aria-label="بستن منو" onClick={() => setMenuOpen(false)} />
          <aside className="relative z-50 flex h-full w-72 flex-col bg-surface-elevated shadow-lg">
            <div className="flex items-center justify-between border-b border-border px-4 py-4">
              <p className="text-lg font-semibold">منو</p>
              <button
                type="button"
                className="inline-flex min-h-11 min-w-11 items-center justify-center rounded-ds bg-secondary"
                aria-label="بستن"
                onClick={() => setMenuOpen(false)}
              >
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
              <p className="truncate text-base font-medium">عملیات فروشنده</p>
              <p className="text-sm text-muted">دادهٔ زندهٔ Tooba</p>
            </div>
          </div>
          <label className="flex min-w-0 flex-col text-xs text-muted">
            فروشنده
            <select
              className="mt-1 min-h-11 max-w-[14rem] rounded-ds border border-border bg-surface px-3 text-sm text-foreground"
              value={sellerPartyId}
              onChange={(event) => onSellerChange(event.target.value)}
              data-testid="seller-party-select"
            >
              {DEMO_SELLERS.map((seller) => (
                <option key={seller.id} value={seller.id}>
                  {seller.label}
                </option>
              ))}
            </select>
          </label>
        </header>
        <div className="min-w-0 flex-1">{children}</div>
      </div>
    </div>
  );
}
