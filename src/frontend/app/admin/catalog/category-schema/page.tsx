"use client";

import { Suspense } from "react";
import { CategorySchemaScreen } from "../../catalog-attribute-ui.tsx";

/** صفحهٔ schema ویژگی رده. */
export default function AdminCategorySchemaPage() {
  return (
    <Suspense fallback={<p className="p-6 text-sm text-gray-500">در حال بارگذاری…</p>}>
      <div className="p-4 lg:p-6">
        <CategorySchemaScreen />
      </div>
    </Suspense>
  );
}
