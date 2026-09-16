"use client";

import { useSearchParams } from "next/navigation";
import { Suspense } from "react";
import { parseFashionPreviewSource } from "../../../../lib/storefront-composition/fashion-demo-preview.ts";
import { FashionTemplatePreviewView } from "../fashion-template-preview-view.tsx";

function FashionTemplateFullPageBody() {
  const params = useSearchParams();
  const source = parseFashionPreviewSource(params.get("source"));
  const locale = params.get("locale")?.trim() || "fa-IR";
  return (
    <FashionTemplatePreviewView
      context={{
        templateKey: "fashion",
        sourceMode: source,
        locale,
        deviceMode: "fullPage",
      }}
      fullPage
    />
  );
}

/**
 * Full-page Fashion template preview — same canonical context as iframe.
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
