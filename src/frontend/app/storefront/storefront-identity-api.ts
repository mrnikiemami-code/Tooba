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

export function notifyAuthChanged(): void {
  if (typeof window === "undefined") {
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
  const response = await fetch("/api/auth/me", { credentials: "include", cache: "no-store" });
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
    return anonymousSession(fallback);
  }
  return {
    authenticated: true,
    userId: payload.userId,
    displayName: payload.displayName ?? null,
    firstName: payload.firstName ?? null,
    lastName: payload.lastName ?? null,
    mobile: payload.mobile ?? null,
    label: storefrontAccountLabel(payload, fallback),
  };
}

export async function requiresCheckoutLogin(): Promise<boolean> {
  const [policy, session] = await Promise.all([loadCheckoutIdentityPolicy(), loadStorefrontSession()]);
  return policy.checkoutAuthenticationRequired && !session.authenticated;
}
