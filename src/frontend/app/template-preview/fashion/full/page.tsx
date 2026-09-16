"use client";

import { useSearchParams } from "next/navigation";
import { Suspense } from "react";
import { parseFashionPreviewSource } from "../../../../lib/storefront-composition/fashion-demo-preview.ts";
import { FashionTemplatePreviewView } from "../fashion-template-preview-view.tsx";

function FashionTemplateFullPageBody() {
  const params = useSearchParams();
  const source = parseFashionPreviewSource(params.get("source"));
  return <FashionTemplatePreviewView source={source} fullPage />;
}

/**
 * Full-page Fashion template preview (same design as iframe, standalone route).
 */
export default function FashionTemplateFullPage() {
  return (
    <Suspense
      fallback={
        <div data-testid="fashion-template-preview-loading" className="p-6 text-sm text-slate-600">
          در حال بارگذاری…
        </div>
      }
    >
      <FashionTemplateFullPageBody />
    </Suspense>
  );
}
