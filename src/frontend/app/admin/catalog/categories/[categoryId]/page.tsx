"use client";

import { Suspense } from "react";
import { CategoryAdminScreen } from "../../../category-admin-screen.tsx";

/** workspace دسته‌بندی انتخاب‌شده (تب عمومی پیش‌فرض). */
export default function AdminCatalogCategoryDetailPage() {
  return (
    <Suspense fallback={<p className="p-6 text-sm text-gray-500">در حال بارگذاری…</p>}>
      <div className="p-4 lg:p-6">
        <CategoryAdminScreen />
      </div>
    </Suspense>
  );
}
