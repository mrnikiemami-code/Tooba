"use client";

import { useSearchParams } from "next/navigation";
import { Suspense } from "react";
import { parseFashionPreviewSource } from "../../../lib/storefront-composition/fashion-demo-preview.ts";
import { FashionTemplatePreviewView } from "./fashion-template-preview-view.tsx";

function FashionTemplatePreviewBody() {
  const params = useSearchParams();
  const source = parseFashionPreviewSource(params.get("source"));
  const locale = params.get("locale")?.trim() || "fa-IR";
  return (
    <FashionTemplatePreviewView
      context={{
        templateKey: "fashion",
        sourceMode: source,
        locale,
        deviceMode: "desktop",
      }}
    />
  );
}

/**
 * Fashion template sample/store preview — iframe target for Admin template workspace.
 */
export default function FashionTemplatePreviewPage() {
  return (
    <Suspense
      fallback={
        <div data-testid="fashion-template-preview-loading" className="p-6 text-sm text-slate-600">
          در حال بارگذاری…
        </div>
      }
    >
      <FashionTemplatePreviewBody />
    </Suspense>
  );
}
