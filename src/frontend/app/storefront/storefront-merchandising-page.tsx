import { StorefrontShell } from "./storefront-shell.tsx";
import { loadStorefrontHome, loadStorefrontMerchandising } from "./storefront-api.ts";
import { StorefrontMerchandisingGrid } from "./storefront-merchandising.tsx";

/** مسیرهای Shopeiva merchandising را به پاسخ زندهٔ Host وصل می‌کند و unsupported را بدون ادعای ساختگی نشان می‌دهد. */
export async function StorefrontMerchandisingRoute({ kind }: { kind: string }) {
  const [home, page] = await Promise.all([loadStorefrontHome(), loadStorefrontMerchandising(kind)]);
  const categories = home?.categories ?? [];
  if (!page) {
    return <StorefrontShell categories={categories}><div className="sf-error">این مسیر اکنون در دسترس نیست.</div></StorefrontShell>;
  }
  if (!page.supported) {
    return (
      <StorefrontShell categories={categories}>
        <div className="py-8">
          <div className="rounded-2xl border bg-white p-10 text-center">
            <h1 className="text-2xl font-black mb-3">{page.title}</h1>
            <p className="text-gray-500">{page.unavailableReason}</p>
            <p className="text-xs text-gray-400 mt-4">تا زمان وجود سیگنال معتبر، رتبه یا کالای ساختگی نمایش داده نمی‌شود.</p>
          </div>
        </div>
      </StorefrontShell>
    );
  }
  const description = kind === "new-products"
    ? "جدیدترین کالاها بر پایهٔ زمان ایجاد واقعی در Catalog"
    : "فقط کالاهایی که Promotion معتبر backend روی آن‌ها اعمال شده است";
  return <StorefrontShell categories={categories}><StorefrontMerchandisingGrid title={page.title} description={description} products={page.products} /></StorefrontShell>;
}
