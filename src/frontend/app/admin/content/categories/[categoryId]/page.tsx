"use client";

import { Suspense } from "react";
import { ContentCategoryAdminScreen } from "../../../content-category-admin-screen.tsx";

export default function AdminContentCategoryDetailPage() {
  return (
    <Suspense fallback={<p className="p-6 text-sm text-gray-500">در حال بارگذاری…</p>}>
      <div className="p-4 lg:p-6">
        <ContentCategoryAdminScreen />
      </div>
    </Suspense>
  );
}
