/**
 * Store+locale+page cache tag helpers for public Store Page SSR (LOCK-SF-326).
 * Tags never mix tenants: storeScope is always part of the key.
 */

export const STOREFRONT_APPEARANCE_TAG = "storefront-appearance";

export function storePagesNamespaceTag(storeScope: string): string {
  return `storefront-pages:${storeScope}`;
}

export function storePageTag(storeScope: string, locale: string, pageKey: string): string {
  return `storefront-page:${storeScope}:${locale}:${pageKey}`;
}

export function storeHomeSelectionTag(storeScope: string): string {
  return `storefront-home-selection:${storeScope}`;
}

export function storefrontHomeTag(storeScope: string, locale: string): string {
  return `storefront-home:${storeScope}:${locale}`;
}

/** Public Store Page Data Cache TTL (seconds). Admin preview stays no-store. */
export const STORE_PAGE_REVALIDATE_SECONDS = 60;

export type StorePageRevalidatePayload = {
  storeScope: string;
  locale?: string | null;
  slug?: string | null;
  /** When true, also bust Home selection + `/` composition cache for this store. */
  homeSelection?: boolean;
};

/**
 * Builds the tag set to invalidate after Landing publish/edit or Home activation.
 */
export function storePageRevalidateTags(payload: StorePageRevalidatePayload): string[] {
  const scope = payload.storeScope || "default";
  const tags = new Set<string>([storePagesNamespaceTag(scope), STOREFRONT_APPEARANCE_TAG]);
  if (payload.homeSelection) {
    tags.add(storeHomeSelectionTag(scope));
  }
  if (payload.locale && payload.slug) {
    tags.add(storePageTag(scope, payload.locale, payload.slug));
    tags.add(storePageTag(scope, payload.locale, "home"));
  }
  return [...tags];
}
