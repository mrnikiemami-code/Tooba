"use client";

import { Suspense } from "react";
import { CatalogUnitsScreen } from "../../catalog-units-screen.tsx";

/** صفحهٔ واحدهای اندازه‌گیری کاتالوگ. */
export default function AdminCatalogUnitsPage() {
  return (
    <Suspense fallback={<p className="p-6 text-sm text-gray-500">در حال بارگذاری…</p>}>
      <div className="p-4 lg:p-6">
        <CatalogUnitsScreen />
      </div>
    </Suspense>
  );
}
