"use client";

import type { ReactNode } from "react";
import { ToastContainer } from "react-toastify";
import "react-toastify/dist/ReactToastify.css";
import { ThemeProvider } from "../design-system";
import { StorefrontWishlistProvider } from "./storefront/storefront-wishlist-provider";

/**
 * پوشش کلاینت برای تم معنایی. ToastContainer مطابق Shopeiva providers.jsx.
 * منطق دامنه اینجا نیست.
 */
export function AppProviders({ children }: { children: ReactNode }) {
  return (
    <ThemeProvider>
      <StorefrontWishlistProvider>
        <ToastContainer
          position="top-right"
          rtl={true}
          autoClose={3000}
          hideProgressBar={false}
          newestOnTop
          closeOnClick
          pauseOnFocusLoss
          draggable
          pauseOnHover
          theme="colored"
        />
        {children}
      </StorefrontWishlistProvider>
    </ThemeProvider>
  );
}
