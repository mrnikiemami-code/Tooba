import { LocalizedLink as Link } from "../../lib/i18n/LocalizedLink.tsx";
import { StorefrontProductCardView } from "./storefront-product-card.tsx";
import type { StorefrontProductCard } from "./storefront-model.ts";

/** پوستهٔ شبکهٔ merchandising قالب Shopeiva را برای کارت‌های زنده و حالت خالی صادقانه حفظ می‌کند. */
export function StorefrontMerchandisingGrid({
  title,
  description,
  products,
}: {
  title: string;
  description: string;
  products: StorefrontProductCard[];
}) {
  return (
    <div className="py-5 md:py-8">
      <div className="rounded-2xl bg-gradient-to-l from-[#2563EB] to-slate-900 text-white p-6 md:p-10 mb-5">
        <p className="text-xs opacity-80 mb-2">خانه / {title}</p>
        <h1 className="text-2xl md:text-3xl font-black mb-2">{title}</h1>
        <p className="text-sm opacity-90">{description}</p>
      </div>
      {products.length ? (
        <div className="grid grid-cols-2 sm:grid-cols-2 lg:grid-cols-4 gap-3 md:gap-4" data-testid="brand-product-grid">
          {products.map((product) => <StorefrontProductCardView key={product.productId} card={product} />)}
        </div>
      ) : (
        <div className="rounded-2xl border bg-white p-10 text-center text-gray-500">
          در حال حاضر کالای منطبق و قابل نمایش وجود ندارد.
        </div>
      )}
    </div>
  );
}

/** کارت دایرکتوری عمومی برند/فروشنده را با الگوی سادهٔ Shopeiva و پیوند landing نمایش می‌دهد. */
export function StorefrontDirectoryCard({
  href,
  title,
  meta,
}: {
  href: string;
  title: string;
  meta: string;
}) {
  return (
    <Link href={href} className="rounded-2xl border bg-white p-5 hover:shadow-lg transition min-w-0">
      <div className="w-12 h-12 rounded-full bg-blue-50 text-[#2563EB] grid place-items-center font-black mb-4">
        {title.slice(0, 1)}
      </div>
      <h2 className="font-black truncate">{title}</h2>
      <p className="text-xs text-gray-500 mt-2">{meta}</p>
    </Link>
  );
}
