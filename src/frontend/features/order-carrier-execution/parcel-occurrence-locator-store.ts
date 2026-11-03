/**
 * Client-side parcel occurrence locator/hint scoped to owner + checkout + shipping group.
 * Never authorization — server still validates owner/checkout/group binding.
 * Fail closed: without a safe owner context key, do not read or write persisted hints.
 */

export type ParcelOccurrenceLocatorHint = {
  occurrenceId: string;
  uncertain: boolean;
};

function normalizeOwnerContextKey(ownerContextKey: string | null | undefined): string | null {
  if (typeof ownerContextKey !== "string") return null;
  const trimmed = ownerContextKey.trim();
  return trimmed.length > 0 ? trimmed : null;
}

function storageKey(
  ownerContextKey: string,
  checkoutId: string,
  shippingGroupId: string,
): string {
  return `tb.carrier.parcel-occurrence.v1:${ownerContextKey}:${checkoutId}:${shippingGroupId}`;
}

function canUseSessionStorage(): boolean {
  try {
    return typeof sessionStorage !== "undefined" && typeof sessionStorage.getItem === "function";
  } catch {
    return false;
  }
}

export function readParcelOccurrenceLocator(
  ownerContextKey: string | null | undefined,
  checkoutId: string,
  shippingGroupId: string,
): ParcelOccurrenceLocatorHint | null {
  const owner = normalizeOwnerContextKey(ownerContextKey);
  if (!owner || !checkoutId || !shippingGroupId || !canUseSessionStorage()) return null;
  try {
    const raw = sessionStorage.getItem(storageKey(owner, checkoutId, shippingGroupId));
    if (!raw) return null;
    const parsed = JSON.parse(raw) as Partial<ParcelOccurrenceLocatorHint>;
    const occurrenceId = typeof parsed.occurrenceId === "string" ? parsed.occurrenceId.trim() : "";
    if (!occurrenceId) return null;
    return { occurrenceId, uncertain: parsed.uncertain === true };
  } catch {
    return null;
  }
}

export function writeParcelOccurrenceLocator(
  ownerContextKey: string | null | undefined,
  checkoutId: string,
  shippingGroupId: string,
  hint: ParcelOccurrenceLocatorHint,
): void {
  const owner = normalizeOwnerContextKey(ownerContextKey);
  if (!owner || !checkoutId || !shippingGroupId || !canUseSessionStorage()) return;
  const occurrenceId = hint.occurrenceId.trim();
  if (!occurrenceId) return;
  try {
    sessionStorage.setItem(
      storageKey(owner, checkoutId, shippingGroupId),
      JSON.stringify({ occurrenceId, uncertain: hint.uncertain === true }),
    );
  } catch {
    /* quota / private mode — in-memory state remains authoritative for this mount */
  }
}

export function clearParcelOccurrenceLocator(
  ownerContextKey: string | null | undefined,
  checkoutId: string,
  shippingGroupId: string,
): void {
  const owner = normalizeOwnerContextKey(ownerContextKey);
  if (!owner || !checkoutId || !shippingGroupId || !canUseSessionStorage()) return;
  try {
    sessionStorage.removeItem(storageKey(owner, checkoutId, shippingGroupId));
  } catch {
    /* ignore */
  }
}
