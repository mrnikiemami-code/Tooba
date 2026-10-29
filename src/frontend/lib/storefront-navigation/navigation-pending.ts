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
  // Same target still in flight — swallow accidental double activation.
  return nowMs - pending.startedAtMs < 15_000;
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

/** Clear when the visible location already matches the pending target (success) or Back cancelled. */
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
