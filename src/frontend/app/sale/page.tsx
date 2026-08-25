import type { Metadata } from "next";
import { StorefrontMerchandisingRoute } from "../storefront/storefront-merchandising-page.tsx";

export const metadata: Metadata = { title: "فروش ویژه | توبا", description: "کالاهای دارای Promotion معتبر توبا", alternates: { canonical: "/sale" } };

/** مسیر فروش ویژهٔ زندهٔ Shopeiva. */
export default function SalePage() {
  return <StorefrontMerchandisingRoute kind="sale" />;
}
