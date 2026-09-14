"use client";

import { useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { usePathname } from "next/navigation";
import { ChevronLeft, LogOut, Package, User, X } from "lucide-react";
import { LocalizedLink as Link } from "../../lib/i18n/LocalizedLink.tsx";
import { useLocale } from "../../lib/i18n/locale-context.tsx";
import { canonicalReturnTo, loginPath } from "../../lib/auth/login-return-to.ts";
import { bffFetchHeaders, ensureCsrfCookie } from "../../lib/auth/browser-session.ts";
import { clearCartSession, loadStorefrontCart, notifyCartChanged, resetStorefrontMergeTransition } from "./storefront-cart-api.ts";
import { AUTH_CHANGED_EVENT, loadStorefrontSession, markStorefrontSessionAnonymous, notifyAuthChanged } from "./storefront-identity-api.ts";

/**
 * آیکن حساب در هدر؛ با هاور همان اطلاعات فعلی (نام، پنل، سفارش‌ها، خروج) باز می‌شود.
 */
export function StorefrontAccountMenu({ compact = false }: { compact?: boolean }) {
  const locale = useLocale();
  const pathname = usePathname();
  const [authenticated, setAuthenticated] = useState(false);
  const [accountLabel, setAccountLabel] = useState("");
  const [open, setOpen] = useState(false);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const root = useRef<HTMLDivElement | null>(null);
  const leaveTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const copy = locale === "en"
    ? {
        login: "Sign in",
        account: "Account",
        orders: "My orders",
        panel: "User panel",
        logout: "Sign out",
        confirmTitle: "Sign out of your account?",
        confirmBody: "After you sign out you will not have access to your current cart. You can sign in again anytime and continue.",
        confirm: "Sign out",
        cancel: "Cancel",
      }
    : {
        login: "ورود",
        account: "حساب کاربری",
        orders: "سفارش‌های من",
        panel: "پنل کاربری",
        logout: "خروج از حساب کاربری",
        confirmTitle: "از حساب کاربری خارج می‌شوید؟",
        confirmBody: "با خروج از حساب کاربری، به سبد خرید فعلی‌تان دسترسی نخواهید داشت. هر وقت بخواهید می‌توانید مجددا وارد شوید و ادامه دهید",
        confirm: "خروج از حساب",
        cancel: "انصراف",
      };
  const signInHref = loginPath(locale, canonicalReturnTo(locale, pathname));

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

  useEffect(() => () => {
    if (leaveTimer.current) {
      clearTimeout(leaveTimer.current);
    }
  }, []);

  function cancelLeave() {
    if (leaveTimer.current) {
      clearTimeout(leaveTimer.current);
      leaveTimer.current = null;
    }
  }

  function openMenu() {
    cancelLeave();
    setOpen(true);
  }

  function scheduleClose() {
    cancelLeave();
    leaveTimer.current = setTimeout(() => setOpen(false), 160);
  }

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
    setConfirmOpen(false);
  }

  async function requestLogout() {
    setOpen(false);
    const cart = await loadStorefrontCart();
    if ((cart?.itemCount ?? 0) > 0) {
      setConfirmOpen(true);
      return;
    }
    await logout();
  }

  const confirmDialog = confirmOpen && typeof document !== "undefined"
    ? createPortal(
      <div className="fixed inset-0 z-[200] flex items-center justify-center p-4" data-testid="logout-confirm-dialog" role="dialog" aria-modal="true" aria-labelledby="logout-confirm-title">
        <button type="button" className="absolute inset-0 bg-black/40" aria-label={copy.cancel} data-testid="logout-confirm-overlay" onClick={() => setConfirmOpen(false)} />
        <div className="relative w-full max-w-lg rounded-2xl bg-white p-5 sm:p-6 shadow-2xl">
          <button type="button" className="absolute top-3 left-3 p-1.5 rounded-lg text-gray-400 hover:bg-gray-100" aria-label={copy.cancel} data-testid="logout-confirm-close" onClick={() => setConfirmOpen(false)}>
            <X className="w-5 h-5" />
          </button>
          <h2 id="logout-confirm-title" className="text-base sm:text-lg font-black text-gray-900 mb-3" data-testid="logout-confirm-title">
            {copy.confirmTitle}
          </h2>
          <p className="text-sm text-gray-500 leading-7 mb-6" data-testid="logout-confirm-body">
            {copy.confirmBody}
          </p>
          <div className="flex items-center justify-end gap-2">
            <button type="button" className="px-5 py-2.5 rounded-xl text-sm font-bold bg-red-500 text-white hover:bg-red-600" data-testid="logout-confirm-submit" onClick={() => void logout()}>
              {copy.confirm}
            </button>
            <button type="button" className="px-5 py-2.5 rounded-xl text-sm font-bold border border-gray-200 text-gray-700 hover:bg-gray-50" data-testid="logout-confirm-cancel" onClick={() => setConfirmOpen(false)}>
              {copy.cancel}
            </button>
          </div>
        </div>
      </div>,
      document.body,
    )
    : null;

  const menuRows = (
    <>
      <Link
        href="/customer-panel"
        className="flex items-center justify-between px-4 py-3 text-sm font-bold text-gray-800 hover:bg-gray-50"
        data-testid="header-account-panel"
        onClick={() => setOpen(false)}
      >
        <span data-testid="header-account-label">{accountLabel || copy.account}</span>
        <ChevronLeft className="w-4 h-4 text-gray-400" />
      </Link>
      <Link
        href="/customer-panel/orders"
        className="flex items-center gap-3 px-4 py-3 text-sm text-gray-700 hover:bg-gray-50"
        data-testid="header-account-orders"
        onClick={() => setOpen(false)}
      >
        <Package className="w-5 h-5 text-gray-400" />
        {copy.orders}
      </Link>
      <Link
        href="/customer-panel"
        className="flex items-center gap-3 px-4 py-3 text-sm text-gray-700 hover:bg-gray-50"
        onClick={() => setOpen(false)}
      >
        <User className="w-5 h-5 text-gray-400" />
        {copy.panel}
      </Link>
      <hr className="my-1 border-gray-100" />
      <button type="button" className="flex items-center gap-3 w-full text-right px-4 py-3 text-sm text-gray-700 hover:bg-gray-50" data-testid="header-account-logout" onClick={() => void requestLogout()}>
        <LogOut className="w-5 h-5 text-gray-400" />
        {copy.logout}
      </button>
    </>
  );

  if (!authenticated) {
    return (
      <Link
        href={signInHref}
        className={compact
          ? "flex items-center gap-3 p-3 rounded-xl hover:bg-gray-100 text-gray-700"
          : "flex items-center gap-1.5 h-10 px-2.5 rounded-xl hover:bg-gray-100 text-sm text-gray-600"}
        data-testid="header-login-link"
        aria-label={copy.login}
        title={copy.login}
      >
        <User className="w-5 h-5" />
        {copy.login}
      </Link>
    );
  }

  if (compact) {
    return (
      <div className="w-full rounded-2xl border border-gray-100 bg-white py-1" ref={root} data-testid="header-account-menu">
        {menuRows}
        {confirmDialog}
      </div>
    );
  }

  return (
    <div
      className="relative"
      ref={root}
      data-testid="header-account-menu"
      onMouseEnter={openMenu}
      onMouseLeave={scheduleClose}
    >
      <button
        type="button"
        className="w-10 h-10 rounded-xl hover:bg-gray-100 flex items-center justify-center text-gray-600"
        aria-expanded={open}
        aria-label={accountLabel || copy.account}
        data-testid="header-account-button"
        onClick={() => setOpen((value) => !value)}
      >
        <User className="w-5 h-5" />
      </button>
      {open ? (
        <div
          className="absolute left-0 mt-1 w-64 bg-white rounded-2xl shadow-2xl border border-gray-200 py-1 z-50 overflow-hidden"
          data-testid="header-account-dropdown"
        >
          {menuRows}
        </div>
      ) : null}
      {confirmDialog}
    </div>
  );
}
