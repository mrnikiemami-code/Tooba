export type CheckoutIdentityPolicy = "AuthenticatedOnly" | "GuestAllowed";

export interface CheckoutIdentityPolicyView {
  policy: CheckoutIdentityPolicy;
  cartAnonymousAllowed: boolean;
  checkoutAuthenticationRequired: boolean;
}

export async function loadCheckoutIdentityPolicy(): Promise<CheckoutIdentityPolicyView> {
  const response = await fetch("/v1/storefront/checkout-identity-policy", { cache: "no-store" });
  const payload = await response.json().catch(() => null) as Partial<CheckoutIdentityPolicyView> | null;
  const policy = payload?.policy === "GuestAllowed" ? "GuestAllowed" : "AuthenticatedOnly";
  return {
    policy,
    cartAnonymousAllowed: payload?.cartAnonymousAllowed !== false,
    checkoutAuthenticationRequired: policy === "AuthenticatedOnly",
  };
}

export const AUTH_CHANGED_EVENT = "tooba-auth-changed";

export type StorefrontSession = {
  authenticated: boolean;
  userId: string | null;
  displayName: string | null;
  firstName: string | null;
  lastName: string | null;
  mobile: string | null;
  label: string;
};

type SessionCache = {
  session: StorefrontSession | null;
  inFlight: Promise<StorefrontSession> | null;
  knownAnonymous: boolean;
};

const sessionCache: SessionCache = {
  session: null,
  inFlight: null,
  knownAnonymous: false,
};

function isBrowser(): boolean {
  return typeof window !== "undefined";
}

function applyFallback(session: StorefrontSession, fallback: string): StorefrontSession {
  if (!session.authenticated) {
    return { ...session, label: fallback };
  }
  return {
    ...session,
    label: storefrontAccountLabel(session, fallback),
  };
}

function fetchStorefrontSession(fallback: string): Promise<StorefrontSession> {
  return fetch("/api/auth/me", { credentials: "include", cache: "no-store" }).then(async (response) => {
    if (response.status === 401) {
      const anonymous = anonymousSession(fallback);
      if (isBrowser()) {
        sessionCache.session = anonymous;
        sessionCache.knownAnonymous = true;
      }
      return anonymous;
    }
    if (!response.ok) {
      return anonymousSession(fallback);
    }
    const payload = await response.json().catch(() => null) as {
      userId?: string;
      displayName?: string | null;
      firstName?: string | null;
      lastName?: string | null;
      mobile?: string | null;
    } | null;
    if (!payload?.userId) {
      const anonymous = anonymousSession(fallback);
      if (isBrowser()) {
        sessionCache.session = anonymous;
        sessionCache.knownAnonymous = true;
      }
      return anonymous;
    }
    const session: StorefrontSession = {
      authenticated: true,
      userId: payload.userId,
      displayName: payload.displayName ?? null,
      firstName: payload.firstName ?? null,
      lastName: payload.lastName ?? null,
      mobile: payload.mobile ?? null,
      label: storefrontAccountLabel(payload, fallback),
    };
    if (isBrowser()) {
      sessionCache.session = session;
      sessionCache.knownAnonymous = false;
    }
    return session;
  });
}

/**
 * Clears the browser session cache so the next load resolves /api/auth/me once.
 */
export function invalidateStorefrontSession(): void {
  sessionCache.session = null;
  sessionCache.inFlight = null;
  sessionCache.knownAnonymous = false;
}

/**
 * After canonical logout, anonymous state is known. Do not probe /api/auth/me again.
 */
export function markStorefrontSessionAnonymous(fallback = "حساب کاربری"): void {
  sessionCache.session = anonymousSession(fallback);
  sessionCache.inFlight = null;
  sessionCache.knownAnonymous = true;
}

export function resetStorefrontSessionCacheForTests(): void {
  invalidateStorefrontSession();
}

export function notifyAuthChanged(): void {
  if (!isBrowser()) {
    return;
  }
  window.dispatchEvent(new Event(AUTH_CHANGED_EVENT));
}

export function storefrontAccountLabel(
  input: { displayName?: string | null; firstName?: string | null; lastName?: string | null; mobile?: string | null },
  fallback: string,
): string {
  const display = (input.displayName ?? "").trim();
  if (display) {
    return display;
  }
  const first = (input.firstName ?? "").trim();
  const last = (input.lastName ?? "").trim();
  if (first && last) {
    return `${first} ${last}`;
  }
  const mobile = (input.mobile ?? "").trim();
  if (mobile) {
    return mobile;
  }
  return fallback;
}

const anonymousSession = (fallback: string): StorefrontSession => ({
  authenticated: false,
  userId: null,
  displayName: null,
  firstName: null,
  lastName: null,
  mobile: null,
  label: fallback,
});

export async function loadStorefrontSession(fallback = "حساب کاربری"): Promise<StorefrontSession> {
  if (!isBrowser()) {
    return fetchStorefrontSession(fallback);
  }
  if (sessionCache.session) {
    return applyFallback(sessionCache.session, fallback);
  }
  if (sessionCache.knownAnonymous) {
    return anonymousSession(fallback);
  }
  if (sessionCache.inFlight) {
    return sessionCache.inFlight.then((session) => applyFallback(session, fallback));
  }
  sessionCache.inFlight = fetchStorefrontSession(fallback).finally(() => {
    sessionCache.inFlight = null;
  });
  return sessionCache.inFlight.then((session) => applyFallback(session, fallback));
}

export async function requiresCheckoutLogin(): Promise<boolean> {
  const [policy, session] = await Promise.all([loadCheckoutIdentityPolicy(), loadStorefrontSession()]);
  return policy.checkoutAuthenticationRequired && !session.authenticated;
}
