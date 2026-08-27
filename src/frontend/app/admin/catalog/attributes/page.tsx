"use client";

import { Suspense } from "react";
import { AttributeDefinitionsScreen } from "../../catalog-attribute-ui.tsx";

/** صفحهٔ تعاریف ویژگی Catalog. */
export default function AdminCatalogAttributesPage() {
  return (
    <Suspense fallback={<p className="p-6 text-sm text-gray-500">در حال بارگذاری…</p>}>
      <div className="p-4 lg:p-6">
        <AttributeDefinitionsScreen />
      </div>
    </Suspense>
  );
}
