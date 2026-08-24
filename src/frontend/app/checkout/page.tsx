import type { Metadata } from "next";
import Link from "next/link";
import { StorefrontShell } from "../storefront/storefront-shell.tsx";
import { loadStorefrontHome } from "../storefront/storefront-api.ts";

export const metadata: Metadata = {
  title: "تسویه | توبا",
  robots: { index: false, follow: false },
};

/**
 * پوستهٔ تسویهٔ بعدی. پرداخت یا سفارش موفق جعل نمی‌شود.
 */
export default async function CheckoutShellPage() {
  const home = await loadStorefrontHome();
  return (
    <StorefrontShell categories={home?.categories ?? []} searchCatalog={home?.featuredProducts ?? []}>
      <div className="py-16 text-center bg-white rounded-2xl mt-6 border border-gray-100">
        <h1 className="text-xl font-black mb-3">درز تسویه هنوز وصل نیست</h1>
        <p className="text-sm text-gray-500 mb-6 max-w-md mx-auto">
          سبد زنده است. Checkout/Order در این تسک ساخته نمی‌شود و پرداخت موفق نمایش داده نمی‌شود.
        </p>
        <Link href="/cart" className="inline-flex px-5 py-2.5 rounded-xl bg-[#2563EB] text-white text-sm font-bold">
          بازگشت به سبد
        </Link>
      </div>
    </StorefrontShell>
  );
}
