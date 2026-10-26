"use client";

/**
 * Thin app-side wrapper around the feature's primary method list.
 *
 * The only reason this file exists is dependency direction: the saved-view adapter lives in the app layer
 * (`app/admin/saved-view-store`), so the app injects it here instead of the feature importing the app.
 *
 * TB-223: the same wrapper serves BOTH named owner surfaces. The surface is a UI boundary that selects the
 * page title and the grid namespace; the server still resolves which owner the request is authorized for.
 */
import { useMemo } from "react";
import {
  ShippingAuthoringMethodListScreen,
  type AuthoringOwnerSurface,
} from "../../../features/admin-shipping-authoring";
import { createHostSavedViewStore } from "../saved-view-store";

/** Saved-view key of this screen (same page identity the previous flat list used). */
export const SHIPPING_SERVICES_GRID_VIEW_KEY = "admin-shipping-services";
/** Saved-view key of the marketplace surface: a separate namespace, never the store's views. */
export const SHIPPING_SERVICES_MARKETPLACE_GRID_VIEW_KEY = "admin-shipping-services-marketplace";

export function ShippingServicesAuthoringListClient({
  locale,
  surface = "store",
}: {
  locale: "fa" | "en";
  surface?: AuthoringOwnerSurface;
}) {
  const savedViewStore = useMemo(
    () =>
      createHostSavedViewStore(
        surface === "marketplace" ? SHIPPING_SERVICES_MARKETPLACE_GRID_VIEW_KEY : SHIPPING_SERVICES_GRID_VIEW_KEY,
      ),
    [surface],
  );
  return <ShippingAuthoringMethodListScreen locale={locale} savedViewStore={savedViewStore} surface={surface} />;
}
