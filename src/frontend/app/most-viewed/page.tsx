import type { Metadata } from "next";
import { StorefrontMerchandisingRoute } from "../storefront/storefront-merchandising-page.tsx";

export const metadata: Metadata = { title: "پربازدیدترین‌ها | توبا", description: "وضعیت سیگنال بازدید کالاهای توبا", alternates: { canonical: "/most-viewed" }, robots: { index: false, follow: true } };

/** پوستهٔ صادقانهٔ Shopeiva تا زمان وجود سیگنال بازدید معتبر. */
export default function MostViewedPage() {
  return <StorefrontMerchandisingRoute kind="most-viewed" />;
}
