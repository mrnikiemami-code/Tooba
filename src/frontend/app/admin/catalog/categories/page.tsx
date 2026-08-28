"use client";

import { Suspense } from "react";
import { CategoryAdminScreen } from "../../category-admin-screen.tsx";

/** فهرست / درخت دسته‌بندی‌های Catalog. */
export default function AdminCatalogCategoriesPage() {
  return (
    <Suspense fallback={<p className="p-6 text-sm text-gray-500">در حال بارگذاری…</p>}>
      <div className="p-4 lg:p-6">
        <CategoryAdminScreen />
      </div>
    </Suspense>
  );
}
