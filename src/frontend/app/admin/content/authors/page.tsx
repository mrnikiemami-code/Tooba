"use client";

import { Suspense } from "react";
import { AdminContentAuthorsScreen } from "../../content-author-list.tsx";

export default function AdminContentAuthorsPage() {
  return (
    <Suspense fallback={<p className="p-6 text-sm text-gray-500">در حال بارگذاری…</p>}>
      <div className="p-4 lg:p-6">
        <AdminContentAuthorsScreen />
      </div>
    </Suspense>
  );
}
