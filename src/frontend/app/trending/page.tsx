import type { Metadata } from "next";
import { StorefrontMerchandisingRoute } from "../storefront/storefront-merchandising-page.tsx";

export const metadata: Metadata = { title: "محبوب‌های روز | توبا", description: "وضعیت سیگنال روند کالاهای توبا", alternates: { canonical: "/trending" }, robots: { index: false, follow: true } };

/** پوستهٔ صادقانهٔ Shopeiva تا زمان وجود سیگنال trend معتبر. */
export default function TrendingPage() {
  return <StorefrontMerchandisingRoute kind="trending" />;
}
