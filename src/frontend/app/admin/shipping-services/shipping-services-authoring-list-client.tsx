"use client";

/**
 * Thin app-side wrapper around the feature's primary method list.
 *
 * The only reason this file exists is dependency direction: the saved-view adapter lives in the app layer
 * (`app/admin/saved-view-store`), so the app injects it here instead of the feature importing the app.
 */
import { useMemo } from "react";
import { ShippingAuthoringMethodListScreen } from "../../../features/admin-shipping-authoring";
import { createHostSavedViewStore } from "../saved-view-store";

/** Saved-view key of this screen (same page identity the previous flat list used). */
export const SHIPPING_SERVICES_GRID_VIEW_KEY = "admin-shipping-services";

export function ShippingServicesAuthoringListClient({ locale }: { locale: "fa" | "en" }) {
  const savedViewStore = useMemo(() => createHostSavedViewStore(SHIPPING_SERVICES_GRID_VIEW_KEY), []);
  return <ShippingAuthoringMethodListScreen locale={locale} savedViewStore={savedViewStore} />;
}
