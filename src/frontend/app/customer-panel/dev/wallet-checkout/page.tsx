"use client";

import { Suspense } from "react";
import { StorefrontOrderConfirmation } from "../../../order/confirmation/storefront-order-confirmation.tsx";

/**
 * Development User-Preview entry for wallet checkout.
 * Avoids locale-prefix middleware rewrite loops on /fa/order/confirmation
 * while reusing the same confirmation + wallet payment UI.
 */
export default function CustomerWalletCheckoutPreviewPage() {
  return (
    <div className="min-h-screen bg-[#F5F5F5]" data-testid="wallet-checkout-preview">
      <div className="mx-auto max-w-5xl px-4 py-6">
        <p className="mb-4 text-xs text-gray-500">
          پیش‌نمایش توسعهٔ پرداخت با کیف پول — همان UI تأیید سفارش فروشگاه.
        </p>
        <Suspense fallback={<p className="py-16 text-center text-sm text-gray-500">در حال بارگذاری…</p>}>
          <StorefrontOrderConfirmation />
        </Suspense>
      </div>
    </div>
  );
}
