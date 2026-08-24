import type { Metadata } from "next";
import { StorefrontShopeivaCart } from "../storefront/storefront-cart.tsx";
import { StorefrontShell } from "../storefront/storefront-shell.tsx";
import { loadStorefrontHome } from "../storefront/storefront-api.ts";

export const metadata: Metadata = {
  title: "سبد خرید | توبا",
  robots: { index: false, follow: false },
};

/**
 * صفحهٔ سبد زنده. ایندکس‌پذیر نیست و Checkout را جعل نمی‌کند.
 */
export default async function CartPage() {
  const home = await loadStorefrontHome();
  return (
    <StorefrontShell categories={home?.categories ?? []} searchCatalog={home?.featuredProducts ?? []}>
      <StorefrontShopeivaCart />
    </StorefrontShell>
  );
}
