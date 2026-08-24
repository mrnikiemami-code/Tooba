import type { Metadata } from "next";
import { StorefrontPaymentResult } from "./storefront-payment-result.tsx";
import { StorefrontShell } from "../../storefront/storefront-shell.tsx";
import { loadStorefrontHome } from "../../storefront/storefront-api.ts";

export const metadata: Metadata = {
  title: "نتیجه پرداخت | توبا",
  robots: { index: false, follow: false },
};

/**
 * نتیجهٔ پرداخت از تصویر Host. ایندکس نمی‌شود و Paid جعلی نیست.
 */
export default async function PaymentResultPage() {
  const home = await loadStorefrontHome();
  return (
    <StorefrontShell categories={home?.categories ?? []} searchCatalog={home?.featuredProducts ?? []}>
      <StorefrontPaymentResult />
    </StorefrontShell>
  );
}
