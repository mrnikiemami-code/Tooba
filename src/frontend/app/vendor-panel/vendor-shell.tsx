"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useState, type ReactNode } from "react";
import { Bell, LayoutDashboard, Menu, Package, Search, ShoppingBag, Star, X } from "lucide-react";
import {
  DEFAULT_SELLER_PARTY_ID,
  loadSellerDevContexts,
  readActorUserId,
  readSellerPartyId,
  writeActorUserId,
  writeSellerPartyId,
  type SellerDevContext,
} from "./seller-api";

const nav = [
  { href: "/vendor-panel", label: "داشبورد", icon: LayoutDashboard, exact: true },
  { href: "/vendor-panel/products", label: "محصولات", icon: Package, exact: false },
  { href: "/vendor-panel/orders", label: "سفارش‌ها", icon: ShoppingBag, exact: false },
];

/**
 * پوستهٔ Vendor به زبان Shopeiva: نوار طلایی بالا، ناوبری افقی، کارت‌ها؛ accent اصلی Tooba آبی است.
 */
export function VendorShell({ children }: { children: ReactNode }) {
  const path = usePathname();
  const [menuOpen, setMenuOpen] = useState(false);
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
        // Actor و Seller جدا نگه داشته می‌شوند تا deny سمت Host قابل اثبات باشد.
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

  if (!ready) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-[rgb(248_248_247)] text-muted">
        در حال آماده‌سازی پنل فروشنده…
      </div>
    );
  }

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

  const contextKey = actorUserId && sellerPartyId ? `${actorUserId}|${sellerPartyId}` : "";
  const crumb =
    path.startsWith("/vendor-panel/products")
      ? "محصولات"
      : path.startsWith("/vendor-panel/orders")
        ? "سفارش‌ها"
        : "داشبورد";

  function NavItems({ onNavigate }: { onNavigate?: () => void }) {
    return (
      <nav className="flex flex-wrap items-center gap-1 text-sm" aria-label="Seller">
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

  return (
    <div className="flex min-h-screen flex-col bg-[rgb(248_248_247)] text-foreground">
      <div className="bg-gradient-to-l from-[rgb(180_140_70)] via-[rgb(198_162_92)] to-[rgb(168_128_58)] text-white shadow-sm">
        <div className="mx-auto flex min-h-12 max-w-7xl items-center justify-between gap-3 px-4 py-2 md:px-8">
          <p className="text-sm font-medium tracking-wide md:text-base">توبا · حس خوب فروش</p>
          <div className="hidden items-center gap-3 md:flex">
            <span className="inline-flex min-h-9 items-center gap-2 rounded-full bg-white/15 px-3 text-sm">
              <Search className="size-3.5 opacity-90" aria-hidden />
              جستجو
            </span>
            <Bell className="size-4 opacity-90" aria-hidden />
          </div>
        </div>
      </div>

      <div className="border-b border-border bg-surface-elevated shadow-sm">
        <div className="mx-auto flex max-w-7xl flex-col gap-3 px-4 py-3 md:px-8">
          <div className="flex items-center justify-between gap-3">
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
                <div className="flex flex-wrap items-center gap-2">
                  <p className="truncate text-base font-semibold">{sellerLabel}</p>
                  <span className="inline-flex items-center gap-1 rounded-full bg-[rgb(255_247_237)] px-2 py-0.5 text-xs text-[rgb(194_65_12)]">
                    <Star className="size-3 fill-current" aria-hidden />
                    فروشنده ویژه
                  </span>
                </div>
                <p className="text-sm text-muted">
                  پنل فروشنده · {crumb}
                </p>
              </div>
            </div>
            <label className="flex min-w-0 flex-col text-xs text-muted">
              زمینهٔ مجاز (Actor + فروشنده)
              <select
                className="mt-1 min-h-11 max-w-[18rem] rounded-ds border border-border bg-surface px-3 text-sm text-foreground"
                value={contextKey}
                onChange={(event) => onContextChange(event.target.value)}
                data-testid="seller-context-select"
              >
                {contexts.length === 0 ? (
                  <option value="">در حال آماده‌سازی…</option>
                ) : (
                  contexts.map((row) => (
                    <option key={`${row.actorUserId}|${row.sellerPartyId}`} value={`${row.actorUserId}|${row.sellerPartyId}`}>
                      {row.actorLabel} → {row.sellerLabel}
                    </option>
                  ))
                )}
              </select>
            </label>
          </div>
          <div className="hidden md:block">
            <NavItems />
          </div>
        </div>
      </div>

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
            <div className="p-3">
              <NavItems onNavigate={() => setMenuOpen(false)} />
            </div>
          </aside>
        </div>
      ) : null}

      <div className="mx-auto w-full max-w-7xl flex-1 px-4 py-5 md:px-8 md:py-7">{children}</div>
    </div>
  );
}
