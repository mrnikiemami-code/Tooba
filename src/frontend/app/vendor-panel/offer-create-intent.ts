/** Durable per-intent request id used to avoid duplicate Offer creation. */
export function readOrCreateOfferIntent(scope: string): string {
  const key = `tooba.offerCreateIntent:${scope}`;
  const existing = window.localStorage.getItem(key);
  if (existing && existing.length > 0) return existing;
  const token = typeof crypto !== "undefined" && "randomUUID" in crypto
    ? crypto.randomUUID()
    : `tb285-${Date.now()}-${Math.random().toString(36).slice(2)}`;
  window.localStorage.setItem(key, token);
  if (window.localStorage.getItem(key) !== token) {
    throw new Error("offer.create.intent_not_durable");
  }
  return token;
}

export function clearOfferIntent(scope: string): void {
  window.localStorage.removeItem(`tooba.offerCreateIntent:${scope}`);
}
