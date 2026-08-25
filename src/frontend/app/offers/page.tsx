import type { Metadata } from "next";
import { StorefrontMerchandisingRoute } from "../storefront/storefront-merchandising-page.tsx";

export const metadata: Metadata = { title: "پیشنهادها | توبا", description: "پیشنهادهای دارای تخفیف معتبر توبا", alternates: { canonical: "/offers" } };

/** مسیر پیشنهادهای زندهٔ Shopeiva. */
export default function OffersPage() {
  return <StorefrontMerchandisingRoute kind="offers" />;
}
