import type { Metadata } from "next";
import { StorefrontShopeivaCart } from "../storefront/storefront-cart.tsx";
import { StorefrontShell } from "../storefront/storefront-shell.tsx";
import { loadStorefrontCategories } from "../storefront/storefront-api.ts";

export const metadata: Metadata = {
  title: "سبد خرید | توبا",
  robots: { index: false, follow: false },
};

/**
 * صفحهٔ سبد زنده. ایندکس‌پذیر نیست و Checkout را جعل نمی‌کند.
 * TB-245-R4: مسیر بحرانی فقط categories سبک؛ پیشنهادها بعد از قابل‌استفاده شدن سبد در کلاینت.
 */
export default async function CartPage() {
  const categories = await loadStorefrontCategories();
  return (
    <StorefrontShell categories={categories ?? []} searchCatalog={[]}>
      <StorefrontShopeivaCart />
    </StorefrontShell>
  );
}
