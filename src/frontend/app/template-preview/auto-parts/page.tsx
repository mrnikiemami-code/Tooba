"use client";

import { useSearchParams } from "next/navigation";
import { Suspense } from "react";
import {
  parseIndustryPreviewSource,
  parseIndustryStorePreviewFixture,
} from "../../../lib/storefront-composition/industry-template-preview.ts";
import { IndustryTemplatePreviewView } from "../_shared/industry-template-preview-view.tsx";

function AutoPartsPreviewBody() {
  const params = useSearchParams();
  const source = parseIndustryPreviewSource(params.get("source"));
  const locale = params.get("locale")?.trim() || "fa-IR";
  const storeFixture = parseIndustryStorePreviewFixture(params.get("fixture"));
  return (
    <IndustryTemplatePreviewView
      context={{
        templateKey: "auto-parts",
        sourceMode: source,
        locale,
        deviceMode: "desktop",
      }}
      storeFixture={storeFixture}
    />
  );
}

export default function AutoPartsTemplatePreviewPage() {
  return (
    <Suspense
      fallback={
        <div data-testid="industry-template-preview-loading" className="p-6 text-sm text-slate-600">
          در حال بارگذاری…
        </div>
      }
    >
      <AutoPartsPreviewBody />
    </Suspense>
  );
}
