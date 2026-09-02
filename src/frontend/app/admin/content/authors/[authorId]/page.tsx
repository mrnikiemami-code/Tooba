"use client";

import { Suspense } from "react";
import { ContentAuthorAdminScreen } from "../../../content-author-admin-screen.tsx";

export default function AdminContentAuthorDetailPage() {
  return (
    <Suspense fallback={<p className="p-6 text-sm text-gray-500">در حال بارگذاری…</p>}>
      <div className="p-4 lg:p-6">
        <ContentAuthorAdminScreen />
      </div>
    </Suspense>
  );
}
