/** Durable admin create intent; avoids a second Offer on retry/reload. */
export function adminOfferIntent(scope: string): string {
  const key = `tooba.adminOfferCreateIntent:${scope}`;
  const existing = window.localStorage.getItem(key);
  if (existing) return existing;
  const token = typeof crypto !== "undefined" && "randomUUID" in crypto ? crypto.randomUUID() : `tb285-admin-${Date.now()}-${Math.random().toString(36).slice(2)}`;
  window.localStorage.setItem(key, token);
  if (window.localStorage.getItem(key) !== token) throw new Error("admin.offer.intent_not_durable");
  return token;
}
export function clearAdminOfferIntent(scope: string): void { window.localStorage.removeItem(`tooba.adminOfferCreateIntent:${scope}`); }
