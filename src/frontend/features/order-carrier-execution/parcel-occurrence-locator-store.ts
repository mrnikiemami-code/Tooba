/**
 * Client-side parcel occurrence locator/hint scoped to checkout + shipping group.
 * Never authorization — server still validates owner/checkout/group binding.
 */

export type ParcelOccurrenceLocatorHint = {
  occurrenceId: string;
  uncertain: boolean;
};

function storageKey(checkoutId: string, shippingGroupId: string): string {
  return `tb.carrier.parcel-occurrence.v1:${checkoutId}:${shippingGroupId}`;
}

function canUseSessionStorage(): boolean {
  try {
    return typeof sessionStorage !== "undefined" && typeof sessionStorage.getItem === "function";
  } catch {
    return false;
  }
}

export function readParcelOccurrenceLocator(
  checkoutId: string,
  shippingGroupId: string,
): ParcelOccurrenceLocatorHint | null {
  if (!checkoutId || !shippingGroupId || !canUseSessionStorage()) return null;
  try {
    const raw = sessionStorage.getItem(storageKey(checkoutId, shippingGroupId));
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
  checkoutId: string,
  shippingGroupId: string,
  hint: ParcelOccurrenceLocatorHint,
): void {
  if (!checkoutId || !shippingGroupId || !canUseSessionStorage()) return;
  const occurrenceId = hint.occurrenceId.trim();
  if (!occurrenceId) return;
  try {
    sessionStorage.setItem(
      storageKey(checkoutId, shippingGroupId),
      JSON.stringify({ occurrenceId, uncertain: hint.uncertain === true }),
    );
  } catch {
    /* quota / private mode — in-memory state remains authoritative for this mount */
  }
}

export function clearParcelOccurrenceLocator(checkoutId: string, shippingGroupId: string): void {
  if (!checkoutId || !shippingGroupId || !canUseSessionStorage()) return;
  try {
    sessionStorage.removeItem(storageKey(checkoutId, shippingGroupId));
  } catch {
    /* ignore */
  }
}
