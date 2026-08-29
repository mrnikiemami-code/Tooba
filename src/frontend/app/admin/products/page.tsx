import { Suspense } from "react";
import { ProductListScreen } from "../product-list";

/**
 * نقطهٔ ورود فهرست Admin. Workspace سفارش/فروشنده اینجا ساخته نمی‌شود.
 */
export default function AdminProductsPage() {
  return (
    <Suspense fallback={<p className="p-6 text-sm text-muted">در حال بارگذاری فهرست محصولات…</p>}>
      <ProductListScreen />
    </Suspense>
  );
}
