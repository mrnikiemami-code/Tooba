import type { Metadata } from "next";
import { StorefrontPaymentSandbox } from "./storefront-payment-sandbox.tsx";
import { StorefrontShell } from "../../storefront/storefront-shell.tsx";
import { loadStorefrontHome } from "../../storefront/storefront-api.ts";

export const metadata: Metadata = {
  title: "درگاه آزمایشی | توبا",
  robots: { index: false, follow: false },
};

/**
 * صفحهٔ تحویل sandbox/dev. بانک واقعی نیست و ایندکس نمی‌شود.
 */
export default async function PaymentSandboxPage() {
  const home = await loadStorefrontHome();
  return (
    <StorefrontShell categories={home?.categories ?? []} searchCatalog={home?.featuredProducts ?? []}>
      <StorefrontPaymentSandbox />
    </StorefrontShell>
  );
}
