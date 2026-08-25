import type { Metadata } from "next";
import { StorefrontMerchandisingRoute } from "../storefront/storefront-merchandising-page.tsx";

export const metadata: Metadata = { title: "محصولات جدید | توبا", description: "جدیدترین کالاهای منتشرشدهٔ Catalog", alternates: { canonical: "/new-products" } };

/** مسیر محصولات جدید بر پایهٔ chronology واقعی Catalog. */
export default function NewProductsPage() {
  return <StorefrontMerchandisingRoute kind="new-products" />;
}
