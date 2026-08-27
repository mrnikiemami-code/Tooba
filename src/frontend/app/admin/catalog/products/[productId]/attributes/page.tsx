"use client";

import { use } from "react";
import { ProductAttributesPanel } from "../../../../catalog-attribute-ui.tsx";

/** صفحهٔ ویژگی‌های محصول Admin. */
export default function AdminProductAttributesPage({
  params,
}: {
  params: Promise<{ productId: string }>;
}) {
  const { productId } = use(params);
  return (
    <div className="p-4 lg:p-6">
      <div className="mb-4">
        <h1 className="text-2xl font-semibold text-gray-900">ویژگی‌های محصول</h1>
        <p className="mt-1 text-sm text-gray-500" dir="ltr">
          {productId}
        </p>
      </div>
      <div className="rounded-2xl border border-gray-200 bg-white p-5 shadow-sm">
        <ProductAttributesPanel productId={productId} />
      </div>
    </div>
  );
}
