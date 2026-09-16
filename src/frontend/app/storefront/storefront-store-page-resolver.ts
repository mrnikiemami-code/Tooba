import "server-only";

import { cache } from "react";
import { loadStorefrontAppearance } from "./storefront-appearance-api.ts";
import { loadStorefrontHome } from "./storefront-api.ts";
import {
  loadLandingRenderContext,
  loadPublishedLandingPage,
  loadStorefrontHomeSelection,
  type LandingRenderContext,
  type StorefrontHomeSelection,
  type StorefrontLandingPage,
} from "./storefront-landing-api.ts";
import {
  STORE_PAGE_REVALIDATE_SECONDS,
  STOREFRONT_APPEARANCE_TAG,
  storeHomeSelectionTag,
  storePageTag,
  storePagesNamespaceTag,
  storefrontHomeTag,
} from "./storefront-store-page-cache.ts";
import type { StorefrontHomePage } from "./storefront-model.ts";
import { localeToContentApi } from "../../lib/i18n/routing.ts";
import type { Locale } from "../../lib/i18n/locale.ts";

/**
 * Canonical request-scoped Store Page resolver (LOCK-SF-325).
 * React `cache()` dedupes metadata + page within one SSR request.
 * Fetch tags keep Data Cache Store+locale+page scoped (LOCK-SF-326).
 */

export const resolveStoreScope = cache(async (): Promise<string> => {
  const appearance = await loadStorefrontAppearance({
    revalidateSeconds: STORE_PAGE_REVALIDATE_SECONDS,
    tags: [STOREFRONT_APPEARANCE_TAG],
  });
  return appearance.storeScope || "default";
});

export const resolvePublishedStorePage = cache(
  async (slug: string, locale: string): Promise<StorefrontLandingPage | null> => {
    const storeScope = await resolveStoreScope();
    return loadPublishedLandingPage(slug, locale, {
      revalidateSeconds: STORE_PAGE_REVALIDATE_SECONDS,
      tags: [
        storePageTag(storeScope, locale, slug),
        storePagesNamespaceTag(storeScope),
      ],
    });
  },
);

export const resolveStoreHomeSelection = cache(
  async (): Promise<StorefrontHomeSelection | null> => {
    const storeScope = await resolveStoreScope();
    return loadStorefrontHomeSelection({
      revalidateSeconds: STORE_PAGE_REVALIDATE_SECONDS,
      tags: [storeHomeSelectionTag(storeScope), storePagesNamespaceTag(storeScope)],
    });
  },
);

export const resolveStorefrontHomeCached = cache(
  async (contentLocale: string): Promise<StorefrontHomePage | null> => {
    const storeScope = await resolveStoreScope();
    const shortLocale = contentLocale.toLowerCase().startsWith("en") ? "en" : "fa";
    return loadStorefrontHome(contentLocale, {
      revalidateSeconds: STORE_PAGE_REVALIDATE_SECONDS,
      tags: [
        storefrontHomeTag(storeScope, shortLocale),
        storePagesNamespaceTag(storeScope),
      ],
    });
  },
);

export type StoreLandingRouteModel = {
  page: StorefrontLandingPage;
  home: StorefrontHomePage | null;
  context: LandingRenderContext;
};

/**
 * One canonical model for Landing route: page + section context.
 * Metadata should call `resolvePublishedStorePage` so this shares the page fetch.
 * Does NOT call heavy /home — Host embeds section-scoped shell on the page payload.
 */
export const resolveLandingRouteModel = cache(
  async (slug: string, locale: Locale): Promise<StoreLandingRouteModel | null> => {
    const page = await resolvePublishedStorePage(slug, locale);
    if (!page) return null;
    const contentLocale = localeToContentApi(locale);
    const context = await loadLandingRenderContext(contentLocale, page, { home: null });
    return { page, home: null, context };
  },
);

export type StoreHomeRouteModel = {
  selection: StorefrontHomeSelection | null;
  home: StorefrontHomePage | null;
  context: LandingRenderContext | null;
  customHomePage: StorefrontLandingPage | null;
};

export const resolveHomeRouteModel = cache(
  async (locale: Locale): Promise<StoreHomeRouteModel> => {
    const contentLocale = localeToContentApi(locale);
    const selection = await resolveStoreHomeSelection();
    const selected = selection?.selectedPage ?? null;
    if (!selected) {
      // Canonical Shopeiva home still needs /home composition.
      const home = await resolveStorefrontHomeCached(contentLocale);
      return { selection, home, context: null, customHomePage: null };
    }
    const customHomePage = { ...selected, pageType: "Home" as const };
    // Selected Store Page carries embedded shell — skip heavy /home for custom Home.
    const context = await loadLandingRenderContext(contentLocale, customHomePage, { home: null });
    return { selection, home: null, context, customHomePage };
  },
);
