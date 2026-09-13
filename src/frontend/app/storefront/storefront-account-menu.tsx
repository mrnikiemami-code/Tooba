"use client";

import { useEffect, useRef, useState } from "react";
import { ChevronDown, LogOut, Package, User } from "lucide-react";
import { LocalizedLink as Link } from "../../lib/i18n/LocalizedLink.tsx";
import { useLocale } from "../../lib/i18n/locale-context.tsx";
import { bffFetchHeaders, ensureCsrfCookie } from "../../lib/auth/browser-session.ts";
import { clearCartSession, notifyCartChanged, resetStorefrontMergeTransition } from "./storefront-cart-api.ts";
import { AUTH_CHANGED_EVENT, loadStorefrontSession, markStorefrontSessionAnonymous, notifyAuthChanged } from "./storefront-identity-api.ts";

/**
 * منوی حساب Shopeiva: ورود وقتی ناشناس؛ پروفایل / سفارش‌ها / خروج وقتی وارد شده.
 */
export function StorefrontAccountMenu({ compact = false }: { compact?: boolean }) {
  const locale = useLocale();
  const [authenticated, setAuthenticated] = useState(false);
  const [accountLabel, setAccountLabel] = useState("");
  const [open, setOpen] = useState(false);
  const root = useRef<HTMLDivElement | null>(null);
  const copy = locale === "en"
    ? { login: "Sign in", account: "Account", orders: "My orders", panel: "Customer panel", logout: "Sign out" }
    : { login: "ورود", account: "حساب کاربری", orders: "سفارش‌های من", panel: "پنل مشتری", logout: "خروج از حساب کاربری" };

  useEffect(() => {
    const refresh = () => {
      void loadStorefrontSession(copy.account).then((session) => {
        setAuthenticated(session.authenticated);
        setAccountLabel(session.label);
      });
    };
    refresh();
    window.addEventListener(AUTH_CHANGED_EVENT, refresh);
    return () => window.removeEventListener(AUTH_CHANGED_EVENT, refresh);
  }, [copy.account]);

  useEffect(() => {
    if (!open) {
      return;
    }
    const onDoc = (event: MouseEvent) => {
      if (root.current && !root.current.contains(event.target as Node)) {
        setOpen(false);
      }
    };
    document.addEventListener("mousedown", onDoc);
    return () => document.removeEventListener("mousedown", onDoc);
  }, [open]);

  async function logout() {
    await ensureCsrfCookie();
    await fetch("/api/auth/logout", {
      method: "POST",
      credentials: "include",
      cache: "no-store",
      headers: bffFetchHeaders(true),
    });
    markStorefrontSessionAnonymous(copy.account);
    resetStorefrontMergeTransition();
    clearCartSession();
    notifyCartChanged();
    notifyAuthChanged();
    setAuthenticated(false);
    setOpen(false);
  }

  if (!authenticated) {
    return (
      <Link
        href="/login"
        className={compact
          ? "flex items-center gap-3 p-3 rounded-xl hover:bg-gray-100 text-gray-700"
          : "hidden sm:flex items-center gap-1 px-3 py-2 rounded-xl text-sm text-gray-600 hover:bg-gray-50"}
        data-testid="header-login-link"
      >
        <User className="w-4 h-4" />
        {copy.login}
      </Link>
    );
  }

  return (
    <div className={`relative ${compact ? "w-full" : ""}`} ref={root} data-testid="header-account-menu">
      <button
        type="button"
        className={compact
          ? "flex items-center justify-between w-full p-3 rounded-xl hover:bg-gray-100 text-gray-800"
          : "flex items-center gap-1.5 px-2.5 py-1.5 rounded-xl hover:bg-gray-50 border border-gray-200"}
        aria-expanded={open}
        data-testid="header-account-button"
        onClick={() => setOpen((value) => !value)}
      >
        <span className="flex items-center gap-1.5">
          <span className="w-7 h-7 bg-gradient-to-br from-[#2563EB] to-[#1d4ed8] rounded-full flex items-center justify-center">
            <User className="w-3.5 h-3.5 text-white" />
          </span>
          <span className={compact ? "text-sm font-medium" : "hidden md:block text-sm font-medium text-gray-700"} data-testid="header-account-label">
            {accountLabel || copy.account}
          </span>
        </span>
        <ChevronDown className="w-3 h-3 text-gray-400" />
      </button>
      {open ? (
        <div
          className={compact
            ? "mt-1 rounded-2xl border border-gray-100 bg-white py-1"
            : "absolute left-0 mt-2 w-48 bg-white rounded-2xl shadow-2xl border border-gray-200 py-2 z-50"}
          data-testid="header-account-dropdown"
        >
          <Link href="/customer-panel" className="flex items-center gap-2 px-4 py-2.5 text-sm text-gray-700 hover:bg-gray-50" data-testid="header-account-panel" onClick={() => setOpen(false)}>
            <User className="w-4 h-4 text-gray-400" />
            {copy.panel}
          </Link>
          <Link href="/customer-panel/orders" className="flex items-center gap-2 px-4 py-2.5 text-sm text-gray-700 hover:bg-gray-50" data-testid="header-account-orders" onClick={() => setOpen(false)}>
            <Package className="w-4 h-4 text-gray-400" />
            {copy.orders}
          </Link>
          <hr className="my-1 border-gray-100" />
          <button type="button" className="flex items-center gap-2 w-full text-right px-4 py-2.5 text-sm text-red-500 hover:bg-red-50" data-testid="header-account-logout" onClick={() => void logout()}>
            <LogOut className="w-4 h-4" />
            {copy.logout}
          </button>
        </div>
      ) : null}
    </div>
  );
}
