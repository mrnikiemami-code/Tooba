/** Durable browser intent: retries of the same owner write reuse its ID and original revision. */
export function ownerWriteIntent(scope: string, revision: string | null | undefined, payload: unknown) {
  if (!revision?.trim()) throw new Error("concurrency.conflict");
  const key = "tooba:offer-owner-write:" + JSON.stringify([scope, payload]);
  const storage = window.sessionStorage;
  const previous = storage.getItem(key);
  if (previous) {
    const saved = JSON.parse(previous) as { revision: string; operationId: string };
    if (saved.revision !== revision || !saved.operationId) throw new Error("concurrency.conflict");
    return { operationId: saved.operationId, clear: () => storage.removeItem(key) };
  }
  const operationId = globalThis.crypto.randomUUID();
  storage.setItem(key, JSON.stringify({ revision, operationId }));
  if (!storage.getItem(key)) throw new Error("offer.create.intent_not_durable");
  return { operationId, clear: () => storage.removeItem(key) };
}

/** Browser identity only scopes recovery state; server still authorizes each request. */
export function offerIntentScope(role: "admin" | "seller", owner: string, variantOrOffer: string, channel: string) {
  const storage = window.localStorage;
  return JSON.stringify([storage.getItem("tooba.tenantId") ?? "", storage.getItem(role === "admin" ? "tooba.adminActorUserId" : "tooba.sellerActorUserId") ?? "", role, owner, variantOrOffer, channel]);
}
