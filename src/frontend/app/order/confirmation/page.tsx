import type { Metadata } from "next";
import { StorefrontOrderConfirmation } from "./storefront-order-confirmation.tsx";
import { StorefrontShell } from "../../storefront/storefront-shell.tsx";
import { loadStorefrontHome } from "../../storefront/storefront-api.ts";

export const metadata: Metadata = {
  title: "تأیید سفارش | توبا",
  robots: { index: false, follow: false },
};

/**
 * تأیید سفارش زنده. ایندکس‌پذیر نیست و Paid جعل نمی‌شود.
 */
export default async function OrderConfirmationPage() {
  const home = await loadStorefrontHome();
  return (
    <StorefrontShell categories={home?.categories ?? []} searchCatalog={home?.featuredProducts ?? []}>
      <StorefrontOrderConfirmation />
    </StorefrontShell>
  );
}
