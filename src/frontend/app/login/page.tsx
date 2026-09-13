import type { Metadata } from "next";
import { Suspense } from "react";
import { StorefrontShell } from "../storefront/storefront-shell.tsx";
import { loadStorefrontHome } from "../storefront/storefront-api.ts";
import { StorefrontCustomerLogin } from "./storefront-login.tsx";

export const metadata: Metadata = {
  title: "ورود | توبا",
  robots: { index: false, follow: false },
};

/**
 * نخستین صفحهٔ ورود مشتری فروشگاه — موبایل و OTP.
 */
export default async function LoginPage() {
  const home = await loadStorefrontHome();
  return (
    <StorefrontShell categories={home?.categories ?? []} searchCatalog={home?.featuredProducts ?? []}>
      <Suspense fallback={<div className="py-16 text-center text-sm text-gray-500">…</div>}>
        <StorefrontCustomerLogin />
      </Suspense>
    </StorefrontShell>
  );
}
