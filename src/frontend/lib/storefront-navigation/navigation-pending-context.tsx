"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { usePathname } from "next/navigation";
import { useLocale } from "../i18n/locale-context.tsx";
import {
  clearNavigationPending,
  NAVIGATION_PENDING_IDLE,
  NAVIGATION_PENDING_TIMEOUT_MS,
  navigationPendingLiveMessage,
  navigationPendingTimedOut,
  reconcileNavigationPending,
  resolveBeginNavigation,
  type BeginNavigationResult,
  type NavigationPendingSnapshot,
  isModifiedNavigationEvent,
} from "./navigation-pending.ts";

type NavigationPendingApi = {
  readonly pending: NavigationPendingSnapshot;
  readonly isPending: boolean;
  readonly retryHint: string | null;
  readonly begin: (
    href: string,
    event?: Parameters<typeof isModifiedNavigationEvent>[0],
  ) => BeginNavigationResult;
  readonly clear: () => void;
};

const NavigationPendingContext = createContext<NavigationPendingApi | null>(null);

function readLocationHref(): string {
  if (typeof window === "undefined") return "";
  return `${window.location.pathname}${window.location.search}${window.location.hash}`;
}

/**
 * Storefront-scoped pending navigation acknowledgment (≤100ms visual/ARIA).
 */
export function StorefrontNavigationPendingProvider({ children }: { children: ReactNode }) {
  const [pending, setPending] = useState<NavigationPendingSnapshot>(NAVIGATION_PENDING_IDLE);
  const [retryHint, setRetryHint] = useState<string | null>(null);
  const pendingRef = useRef(pending);
  pendingRef.current = pending;
  const pathname = usePathname();
  const locale = useLocale();

  const clear = useCallback(() => {
    pendingRef.current = clearNavigationPending();
    setPending(pendingRef.current);
    setRetryHint(null);
  }, []);

  const begin = useCallback(
    (href: string, event?: Parameters<typeof isModifiedNavigationEvent>[0]): BeginNavigationResult => {
      const now = typeof performance !== "undefined" ? performance.now() : Date.now();
      const origin = typeof window !== "undefined" ? window.location.origin : "";
      const result = resolveBeginNavigation({
        href,
        currentHref: readLocationHref() || pathname || "",
        pending: pendingRef.current,
        nowMs: now,
        origin,
        event,
      });
      if (result.kind === "started") {
        pendingRef.current = result.pending;
        setPending(result.pending);
        setRetryHint(null);
      }
      return result;
    },
    [pathname],
  );

  const syncFromLocation = useCallback(() => {
    const origin = typeof window !== "undefined" ? window.location.origin : "";
    const current = readLocationHref() || pathname || "";
    setPending((prev) => {
      const next = reconcileNavigationPending(prev, current, origin);
      pendingRef.current = next;
      return next;
    });
  }, [pathname]);

  useEffect(() => {
    syncFromLocation();
  }, [pathname, syncFromLocation]);

  useEffect(() => {
    const onPop = () => {
      pendingRef.current = clearNavigationPending();
      setPending(pendingRef.current);
      setRetryHint(null);
    };
    const onHash = () => syncFromLocation();
    window.addEventListener("popstate", onPop);
    window.addEventListener("hashchange", onHash);
    return () => {
      window.removeEventListener("popstate", onPop);
      window.removeEventListener("hashchange", onHash);
    };
  }, [syncFromLocation]);

  useEffect(() => {
    if (!pending.href || pending.startedAtMs == null) return;
    const tick = () => {
      syncFromLocation();
      const now = typeof performance !== "undefined" ? performance.now() : Date.now();
      if (navigationPendingTimedOut(pendingRef.current, now)) {
        pendingRef.current = clearNavigationPending();
        setPending(pendingRef.current);
        setRetryHint(navigationPendingLiveMessage(locale, "retry"));
      }
    };
    const interval = window.setInterval(tick, 100);
    const timeout = window.setTimeout(tick, NAVIGATION_PENDING_TIMEOUT_MS);
    return () => {
      window.clearInterval(interval);
      window.clearTimeout(timeout);
    };
  }, [pending.href, pending.startedAtMs, locale, syncFromLocation]);

  const value = useMemo<NavigationPendingApi>(
    () => ({
      pending,
      isPending: pending.href != null,
      retryHint,
      begin,
      clear,
    }),
    [pending, retryHint, begin, clear],
  );

  return (
    <NavigationPendingContext.Provider value={value}>
      {children}
      <StorefrontNavigationPendingIndicator
        pending={pending}
        retryHint={retryHint}
        locale={locale}
      />
    </NavigationPendingContext.Provider>
  );
}

export function useStorefrontNavigationPending(): NavigationPendingApi {
  const ctx = useContext(NavigationPendingContext);
  if (!ctx) {
    return {
      pending: NAVIGATION_PENDING_IDLE,
      isPending: false,
      retryHint: null,
      begin: () => ({ kind: "started", pending: NAVIGATION_PENDING_IDLE }),
      clear: () => undefined,
    };
  }
  return ctx;
}

function StorefrontNavigationPendingIndicator({
  pending,
  retryHint,
  locale,
}: {
  pending: NavigationPendingSnapshot;
  retryHint: string | null;
  locale: "fa" | "en";
}) {
  const active = pending.href != null;
  const live = retryHint ?? (active ? navigationPendingLiveMessage(locale, "pending") : "");
  return (
    <div
      data-testid="storefront-nav-pending"
      data-nav-pending={active ? "true" : "false"}
      data-nav-retry={retryHint ? "true" : "false"}
      aria-busy={active ? true : undefined}
      aria-live="polite"
      aria-atomic="true"
      className={
        active
          ? "pointer-events-none fixed inset-x-0 top-0 z-[200] h-1 overflow-hidden bg-primary/20"
          : "pointer-events-none fixed inset-x-0 top-0 z-[200] h-0 overflow-hidden"
      }
    >
      {active ? <div className="h-full w-1/3 animate-pulse bg-primary" /> : null}
      {live ? <span className="sr-only">{live}</span> : <span className="sr-only" />}
    </div>
  );
}
