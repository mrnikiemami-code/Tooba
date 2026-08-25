"use client";

import type { ReactNode } from "react";
import { ThemeProvider } from "../design-system";
import { StorefrontWishlistProvider } from "./storefront/storefront-wishlist-provider";

/**
 * پوشش کلاینت برای تم معنایی. منطق دامنه اینجا نیست.
 */
export function AppProviders({ children }: { children: ReactNode }) {
  return <ThemeProvider><StorefrontWishlistProvider>{children}</StorefrontWishlistProvider></ThemeProvider>;
}
