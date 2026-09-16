"use client";

import { useSearchParams } from "next/navigation";
import { Suspense } from "react";
import {
  parseIndustryPreviewSource,
  parseIndustryStorePreviewFixture,
} from "../../../../lib/storefront-composition/industry-template-preview.ts";
import { IndustryTemplatePreviewView } from "../../_shared/industry-template-preview-view.tsx";

function BuildingMaterialsFullPreviewBody() {
  const params = useSearchParams();
  const source = parseIndustryPreviewSource(params.get("source"));
  const locale = params.get("locale")?.trim() || "fa-IR";
  const storeFixture = parseIndustryStorePreviewFixture(params.get("fixture"));
  return (
    <IndustryTemplatePreviewView
      context={{
        templateKey: "building-materials",
        sourceMode: source,
        locale,
        deviceMode: "fullPage",
      }}
      fullPage
      storeFixture={storeFixture}
    />
  );
}

export default function BuildingMaterialsTemplateFullPreviewPage() {
  return (
    <Suspense
      fallback={
        <div data-testid="industry-template-preview-loading" className="p-6 text-sm text-slate-600">
          در حال بارگذاری…
        </div>
      }
    >
      <BuildingMaterialsFullPreviewBody />
    </Suspense>
  );
}
