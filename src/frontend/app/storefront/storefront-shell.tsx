import "./storefront.css";
import type { ReactNode } from "react";
import type { StorefrontCategoryItem, StorefrontProductCard } from "./storefront-model.ts";
import { StorefrontShopeivaFooter } from "./storefront-footer.tsx";
import { StorefrontShopeivaHeader } from "./storefront-header.tsx";
import { StorefrontThemedCanvas } from "./storefront-themed-canvas.tsx";

/**
 * پوستهٔ فروشگاه با هدر/مگامنو/فوتر Shopeiva و عرض محتوای قالب.
 */
export function StorefrontShell({
  categories,
  children,
  fullBleed = false,
}: {
  categories: StorefrontCategoryItem[];
  children: ReactNode;
  searchCatalog?: StorefrontProductCard[];
  fullBleed?: boolean;
}) {
  return (
    <StorefrontThemedCanvas className="min-h-screen bg-page text-gray-900 flex flex-col">
      <StorefrontShopeivaHeader categories={categories} />
      <main className="flex-1 w-full" data-storefront-shell-main="true">
        <div className={fullBleed ? "w-full" : "max-w-[1800px] mx-auto px-4 sm:px-6"}>{children}</div>
      </main>
      <StorefrontShopeivaFooter categories={categories} />
    </StorefrontThemedCanvas>
  );
}
