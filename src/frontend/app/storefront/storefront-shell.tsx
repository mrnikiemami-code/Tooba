import "./storefront.css";
import type { ReactNode } from "react";
import type { StorefrontCategoryItem, StorefrontProductCard } from "./storefront-model.ts";
import { StorefrontShopeivaFooter } from "./storefront-footer.tsx";
import { StorefrontShopeivaHeader } from "./storefront-header.tsx";

/**
 * پوستهٔ فروشگاه با هدر/مگامنو/فوتر Shopeiva و عرض محتوای قالب.
 */
export function StorefrontShell({
  categories,
  children,
}: {
  categories: StorefrontCategoryItem[];
  children: ReactNode;
  searchCatalog?: StorefrontProductCard[];
}) {
  return (
    <div className="min-h-screen bg-[#f3f5f8] text-gray-900 flex flex-col">
      <StorefrontShopeivaHeader categories={categories} />
      <main className="flex-1 w-full">
        <div className="max-w-[1800px] mx-auto px-4 sm:px-6">{children}</div>
      </main>
      <StorefrontShopeivaFooter categories={categories} />
    </div>
  );
}
