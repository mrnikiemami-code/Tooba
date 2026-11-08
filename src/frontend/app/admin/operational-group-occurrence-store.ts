/**
 * TB-253-R3: client-side prebound occurrence for the ONE Marketplace operational group workflow.
 *
 * The occurrence is minted BEFORE the first mutation and retained while the same reviewed request
 * is retried, so a lost response / remount replays the recorded parcels instead of allocating a new
 * set. Only an explicit successful completion clears it, so the next deliberate run is a NEW
 * occurrence with its own distinct parcels.
 *
 * This is never authorization: the server validates group ownership, quantities and the occurrence
 * key itself.
 */

function storageKey(checkoutId: string, operationalGroupKey: string): string {
  return `tb.operational-group-occurrence.v1:${checkoutId}:${operationalGroupKey}`;
}

function canUseSessionStorage(): boolean {
  try {
    return typeof sessionStorage !== "undefined" && typeof sessionStorage.getItem === "function";
  } catch {
    return false;
  }
}

function newOccurrenceId(): string {
  const cryptoApi = globalThis.crypto;
  if (cryptoApi && typeof cryptoApi.randomUUID === "function") {
    return cryptoApi.randomUUID();
  }
  // Deterministic-format fallback for runtimes without crypto.randomUUID.
  return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, (token) => {
    const random = Math.floor(Math.random() * 16);
    const value = token === "x" ? random : (random & 0x3) | 0x8;
    return value.toString(16);
  });
}

export function mintOperationalGroupOccurrence(): string {
  return newOccurrenceId();
}

export function readOperationalGroupOccurrence(
  checkoutId: string,
  operationalGroupKey: string,
): string | null {
  if (!checkoutId || !operationalGroupKey || !canUseSessionStorage()) return null;
  try {
    const raw = sessionStorage.getItem(storageKey(checkoutId, operationalGroupKey));
    const value = typeof raw === "string" ? raw.trim() : "";
    return value.length > 0 ? value : null;
  } catch {
    return null;
  }
}

export function writeOperationalGroupOccurrence(
  checkoutId: string,
  operationalGroupKey: string,
  occurrenceId: string,
): void {
  if (!checkoutId || !operationalGroupKey || !occurrenceId || !canUseSessionStorage()) return;
  try {
    sessionStorage.setItem(storageKey(checkoutId, operationalGroupKey), occurrenceId.trim());
  } catch {
    /* quota / private mode — the in-memory ref remains authoritative for this mount */
  }
}

export function clearOperationalGroupOccurrence(
  checkoutId: string,
  operationalGroupKey: string,
): void {
  if (!checkoutId || !operationalGroupKey || !canUseSessionStorage()) return;
  try {
    sessionStorage.removeItem(storageKey(checkoutId, operationalGroupKey));
  } catch {
    /* ignore */
  }
}
