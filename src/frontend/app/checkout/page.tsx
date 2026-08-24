import type { Metadata } from "next";
import { StorefrontShopeivaCheckout } from "../storefront/storefront-checkout.tsx";
import { StorefrontShell } from "../storefront/storefront-shell.tsx";
import { loadStorefrontHome } from "../storefront/storefront-api.ts";

export const metadata: Metadata = {
  title: "تسویه | توبا",
  robots: { index: false, follow: false },
};

/**
 * تسویه زنده روی سبد Host. ایندکس‌پذیر نیست.
 */
export default async function CheckoutPage() {
  const home = await loadStorefrontHome();
  return (
    <StorefrontShell categories={home?.categories ?? []} searchCatalog={home?.featuredProducts ?? []}>
      <StorefrontShopeivaCheckout />
    </StorefrontShell>
  );
}
