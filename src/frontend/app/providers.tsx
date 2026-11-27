"use client";

import type { ReactNode } from "react";
import { ToastContainer } from "react-toastify";
import "react-toastify/dist/ReactToastify.css";
import { ThemeProvider } from "../design-system";
import { StorefrontWishlistProvider } from "./storefront/storefront-wishlist-provider";
import { StorefrontNavigationPendingProvider } from "../lib/storefront-navigation/navigation-pending-context.tsx";
import { useRegistryLocale } from "../lib/i18n/registry-locale.ts";

/** Central registry direction also controls shared Toast feedback. */
function RegistryToastContainer() {
  const registry = useRegistryLocale();
  return (
    <ToastContainer
      position="top-right"
      rtl={registry.locale.direction === "rtl"}
      autoClose={3000}
      hideProgressBar={false}
      newestOnTop
      closeOnClick
      pauseOnFocusLoss
      draggable
      pauseOnHover
      theme="colored"
    />
  );
}

/** Client shell; no domain logic. */
export function AppProviders({ children }: { children: ReactNode }) {
  return (
    <ThemeProvider>
         <StorefrontWishlistProvider>
          <StorefrontNavigationPendingProvider>
            <RegistryToastContainer />
            {children}
          </StorefrontNavigationPendingProvider>
        </StorefrontWishlistProvider>
     </ThemeProvider>
  );
}
