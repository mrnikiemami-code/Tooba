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
import {
  beginNavigationPending,
  clearNavigationPending,
  isModifiedNavigationEvent,
  NAVIGATION_PENDING_IDLE,
  reconcileNavigationPending,
  shouldIgnoreDuplicatePendingClick,
  type NavigationPendingSnapshot,
} from "./navigation-pending.ts";

type NavigationPendingApi = {
  readonly pending: NavigationPendingSnapshot;
  readonly isPending: boolean;
  readonly begin: (href: string, event?: Parameters<typeof isModifiedNavigationEvent>[0]) => boolean;
  readonly clear: () => void;
};

const NavigationPendingContext = createContext<NavigationPendingApi | null>(null);

/**
 * Storefront-scoped pending navigation acknowledgment (≤100ms visual/ARIA).
 * Does not replace router semantics; only surfaces that a primary click was accepted.
 */
export function StorefrontNavigationPendingProvider({ children }: { children: ReactNode }) {
  const [pending, setPending] = useState<NavigationPendingSnapshot>(NAVIGATION_PENDING_IDLE);
  const pendingRef = useRef(pending);
  pendingRef.current = pending;
  const pathname = usePathname();

  const clear = useCallback(() => {
    pendingRef.current = clearNavigationPending();
    setPending(pendingRef.current);
  }, []);

  const begin = useCallback(
    (href: string, event?: Parameters<typeof isModifiedNavigationEvent>[0]) => {
      if (event && isModifiedNavigationEvent(event)) {
        return false;
      }
      const now = typeof performance !== "undefined" ? performance.now() : Date.now();
      const origin = typeof window !== "undefined" ? window.location.origin : "";
      if (shouldIgnoreDuplicatePendingClick(pendingRef.current, href, now, origin)) {
        return false;
      }
      const next = beginNavigationPending(href, now);
      pendingRef.current = next;
      setPending(next);
      return true;
    },
    [],
  );

  useEffect(() => {
    const current =
      typeof window !== "undefined"
        ? `${window.location.pathname}${window.location.search}${window.location.hash}`
        : (pathname ?? "");
    const origin = typeof window !== "undefined" ? window.location.origin : "";
    setPending((prev) => reconcileNavigationPending(prev, current, origin));
  }, [pathname]);

  useEffect(() => {
    const onPop = () => {
      setPending(clearNavigationPending());
    };
    window.addEventListener("popstate", onPop);
    return () => window.removeEventListener("popstate", onPop);
  }, []);

  const value = useMemo<NavigationPendingApi>(
    () => ({
      pending,
      isPending: pending.href != null,
      begin,
      clear,
    }),
    [pending, begin, clear],
  );

  return (
    <NavigationPendingContext.Provider value={value}>
      {children}
      <StorefrontNavigationPendingIndicator pending={pending} />
    </NavigationPendingContext.Provider>
  );
}

export function useStorefrontNavigationPending(): NavigationPendingApi {
  const ctx = useContext(NavigationPendingContext);
  if (!ctx) {
    return {
      pending: NAVIGATION_PENDING_IDLE,
      isPending: false,
      // Outside the storefront provider: do not block native Link navigation.
      begin: () => true,
      clear: () => undefined,
    };
  }
  return ctx;
}

function StorefrontNavigationPendingIndicator({
  pending,
}: {
  pending: NavigationPendingSnapshot;
}) {
  const active = pending.href != null;
  return (
    <div
      data-testid="storefront-nav-pending"
      data-nav-pending={active ? "true" : "false"}
      aria-busy={active ? true : undefined}
      aria-live="polite"
      aria-atomic="true"
      className={
        active
          ? "pointer-events-none fixed inset-x-0 top-0 z-[200] h-1 overflow-hidden bg-primary/20"
          : "pointer-events-none fixed inset-x-0 top-0 z-[200] h-0 overflow-hidden"
      }
    >
      {active ? (
        <>
          <div className="h-full w-1/3 animate-pulse bg-primary" />
          <span className="sr-only">در حال رفتن به صفحه…</span>
        </>
      ) : (
        <span className="sr-only" />
      )}
    </div>
  );
}
