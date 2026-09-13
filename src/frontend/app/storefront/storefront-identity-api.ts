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

export function notifyAuthChanged(): void {
  if (typeof window === "undefined") {
    return;
  }
  window.dispatchEvent(new Event(AUTH_CHANGED_EVENT));
}

export async function loadStorefrontSession(): Promise<{ authenticated: boolean; userId: string | null }> {
  const response = await fetch("/api/auth/me", { credentials: "include", cache: "no-store" });
  if (!response.ok) {
    return { authenticated: false, userId: null };
  }
  const payload = await response.json().catch(() => null) as { userId?: string } | null;
  return { authenticated: Boolean(payload?.userId), userId: payload?.userId ?? null };
}

export async function requiresCheckoutLogin(): Promise<boolean> {
  const [policy, session] = await Promise.all([loadCheckoutIdentityPolicy(), loadStorefrontSession()]);
  return policy.checkoutAuthenticationRequired && !session.authenticated;
}
