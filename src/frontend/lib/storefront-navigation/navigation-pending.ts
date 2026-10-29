/**
 * Pure navigation-pending rules for storefront primary navigations.
 * Keeps click acknowledgment separate from router/RSC timing.
 */

export type NavigationPendingSnapshot = {
  readonly href: string | null;
  readonly startedAtMs: number | null;
};

export const NAVIGATION_PENDING_IDLE: NavigationPendingSnapshot = {
  href: null,
  startedAtMs: null,
};

/** Soft failure unlock after a no-op / stuck soft navigation. */
export const NAVIGATION_PENDING_TIMEOUT_MS = 5_000;

export type BeginNavigationResult =
  | { readonly kind: "native" }
  | { readonly kind: "started"; readonly pending: NavigationPendingSnapshot }
  | { readonly kind: "duplicate" }
  | { readonly kind: "same-location" };

/** True when the click should keep native browser semantics (new tab, download, etc.). */
export function isModifiedNavigationEvent(event: {
  readonly button?: number;
  readonly metaKey?: boolean;
  readonly ctrlKey?: boolean;
  readonly shiftKey?: boolean;
  readonly altKey?: boolean;
  readonly defaultPrevented?: boolean;
}): boolean {
  if (event.defaultPrevented) return true;
  if (typeof event.button === "number" && event.button !== 0) return true;
  return Boolean(event.metaKey || event.ctrlKey || event.shiftKey || event.altKey);
}

/** Absolute or path href normalized for same-document comparison. */
export function normalizeNavigationHref(href: string, origin = ""): string {
  const trimmed = href.trim();
  if (!trimmed || trimmed.startsWith("#")) return trimmed;
  try {
    const base = origin || "http://local.invalid";
    const url = new URL(trimmed, base);
    return `${url.pathname}${url.search}${url.hash}`;
  } catch {
    return trimmed.startsWith("/") ? trimmed : `/${trimmed}`;
  }
}

export function shouldIgnoreDuplicatePendingClick(
  pending: NavigationPendingSnapshot,
  nextHref: string,
  nowMs: number,
  origin = "",
): boolean {
  if (!pending.href || pending.startedAtMs == null) return false;
  if (normalizeNavigationHref(pending.href, origin) !== normalizeNavigationHref(nextHref, origin)) {
    return false;
  }
  return nowMs - pending.startedAtMs < NAVIGATION_PENDING_TIMEOUT_MS;
}

export function beginNavigationPending(
  href: string,
  nowMs: number,
): NavigationPendingSnapshot {
  return { href, startedAtMs: nowMs };
}

export function clearNavigationPending(): NavigationPendingSnapshot {
  return NAVIGATION_PENDING_IDLE;
}

/**
 * Decide how a primary click should interact with pending state.
 * Only `duplicate` means the caller should preventDefault.
 */
export function resolveBeginNavigation(input: {
  readonly href: string;
  readonly currentHref: string;
  readonly pending: NavigationPendingSnapshot;
  readonly nowMs: number;
  readonly origin?: string;
  readonly event?: Parameters<typeof isModifiedNavigationEvent>[0];
}): BeginNavigationResult {
  const origin = input.origin ?? "";
  if (input.event && isModifiedNavigationEvent(input.event)) {
    return { kind: "native" };
  }
  const target = normalizeNavigationHref(input.href, origin);
  const current = normalizeNavigationHref(input.currentHref, origin);
  if (target === current) {
    return { kind: "same-location" };
  }
  if (shouldIgnoreDuplicatePendingClick(input.pending, input.href, input.nowMs, origin)) {
    return { kind: "duplicate" };
  }
  return { kind: "started", pending: beginNavigationPending(input.href, input.nowMs) };
}

/** Clear when the visible location already matches the pending target. */
export function reconcileNavigationPending(
  pending: NavigationPendingSnapshot,
  currentHref: string,
  origin = "",
): NavigationPendingSnapshot {
  if (!pending.href) return pending;
  const pendingPath = normalizeNavigationHref(pending.href, origin);
  const currentPath = normalizeNavigationHref(currentHref, origin);
  if (pendingPath === currentPath) {
    return NAVIGATION_PENDING_IDLE;
  }
  return pending;
}

export function navigationPendingTimedOut(
  pending: NavigationPendingSnapshot,
  nowMs: number,
  timeoutMs = NAVIGATION_PENDING_TIMEOUT_MS,
): boolean {
  if (!pending.href || pending.startedAtMs == null) return false;
  return nowMs - pending.startedAtMs >= timeoutMs;
}

export function navigationPendingLiveMessage(
  locale: "fa" | "en",
  phase: "pending" | "retry",
): string {
  if (locale === "en") {
    return phase === "retry"
      ? "Navigation did not finish. Try again."
      : "Navigating…";
  }
  return phase === "retry"
    ? "انتقال کامل نشد. دوباره تلاش کنید."
    : "در حال رفتن به صفحه…";
}
