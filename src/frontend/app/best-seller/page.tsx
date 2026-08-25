import type { Metadata } from "next";
import { StorefrontMerchandisingRoute } from "../storefront/storefront-merchandising-page.tsx";

export const metadata: Metadata = { title: "پرفروش‌ترین‌ها | توبا", description: "وضعیت سیگنال پرفروش‌ترین کالاهای توبا", alternates: { canonical: "/best-seller" }, robots: { index: false, follow: true } };

/** پوستهٔ صادقانهٔ Shopeiva تا زمان وجود معیار فروش معتبر. */
export default function BestSellerPage() {
  return <StorefrontMerchandisingRoute kind="best-seller" />;
}
