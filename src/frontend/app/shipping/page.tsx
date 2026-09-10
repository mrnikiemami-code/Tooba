import type { Metadata } from "next";
import { StorefrontShopeivaShipping } from "../storefront/storefront-shipping.tsx";
import { StorefrontShell } from "../storefront/storefront-shell.tsx";
import { loadStorefrontHome } from "../storefront/storefront-api.ts";

export const metadata: Metadata = {
  title: "اطلاعات ارسال | توبا",
  robots: { index: false, follow: false },
};

/**
 * مرحلهٔ ارسال فروشگاهی روی قرارداد Host. ایندکس‌پذیر نیست.
 */
export default async function ShippingPage() {
  const home = await loadStorefrontHome();
  return (
    <StorefrontShell categories={home?.categories ?? []} searchCatalog={home?.featuredProducts ?? []}>
      <StorefrontShopeivaShipping />
    </StorefrontShell>
  );
}
