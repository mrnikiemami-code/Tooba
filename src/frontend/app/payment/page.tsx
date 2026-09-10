import type { Metadata } from "next";
import { Suspense } from "react";
import { StorefrontPaymentHandoff } from "./storefront-payment-handoff.tsx";
import { StorefrontShell } from "../storefront/storefront-shell.tsx";
import { loadStorefrontHome } from "../storefront/storefront-api.ts";

export const metadata: Metadata = {
  title: "پرداخت | توبا",
  robots: { index: false, follow: false },
};

/**
 * مسیر پرداخت — قالب handoff تا T003. روش پرداخت در این Task پیاده نمی‌شود.
 */
export default async function PaymentPage() {
  const home = await loadStorefrontHome();
  return (
    <StorefrontShell categories={home?.categories ?? []} searchCatalog={home?.featuredProducts ?? []}>
      <Suspense fallback={<div className="py-16 text-center text-sm text-gray-500">در حال آماده‌سازی پرداخت…</div>}>
        <StorefrontPaymentHandoff />
      </Suspense>
    </StorefrontShell>
  );
}
