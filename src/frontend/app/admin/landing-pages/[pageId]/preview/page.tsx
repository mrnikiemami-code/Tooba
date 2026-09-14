"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { loadAdminLandingPreview } from "../../admin-landing-pages-api.ts";
import { loadStorefrontHome } from "../../../../storefront/storefront-api.ts";
import {
  loadLandingRenderContext,
  mapStorefrontLandingPage,
  type StorefrontLandingPage,
} from "../../../../storefront/storefront-landing-api.ts";
import { StorefrontLandingSections } from "../../../../storefront/storefront-landing-sections.tsx";
import { StorefrontShell } from "../../../../storefront/storefront-shell.tsx";
import type { LandingRenderContext } from "../../../../storefront/storefront-landing-api.ts";
import type { StorefrontHomePage } from "../../../../storefront/storefront-model.ts";

/** پیش‌نمایش Admin با همان رندرر ویترین؛ ایندکس نمی‌شود. */
export default function AdminLandingPreviewPage() {
  const params = useParams<{ pageId: string }>();
  const [page, setPage] = useState<StorefrontLandingPage | null>(null);
  const [home, setHome] = useState<StorefrontHomePage | null>(null);
  const [context, setContext] = useState<LandingRenderContext | null>(null);
  const [message, setMessage] = useState<string>();

  useEffect(() => {
    const pageId = params.pageId;
    if (!pageId) return;
    void Promise.all([loadAdminLandingPreview(pageId), loadStorefrontHome("fa-IR")]).then(async ([preview, homePage]) => {
      if (!preview.ok) {
        setMessage(preview.message);
        return;
      }
      const mapped = mapStorefrontLandingPage(preview.data);
      if (!mapped) {
        setMessage("پیش‌نمایش آماده نشد.");
        return;
      }
      setPage(mapped);
      setHome(homePage);
      setContext(await loadLandingRenderContext("fa-IR", mapped));
    });
  }, [params.pageId]);

  if (message) {
    return <p className="p-8 text-sm text-red-600">{message}</p>;
  }
  if (!page || !context) {
    return <p className="p-8 text-sm text-muted">در حال آماده‌سازی پیش‌نمایش…</p>;
  }

  return (
    <div data-testid="landing-draft-preview">
      <div className="mb-4 rounded-xl bg-amber-50 px-4 py-3 text-sm font-bold text-amber-800">
        حالت پیش‌نمایش — بازدیدکنندگان عمومی این صفحه را نمی‌بینند.
      </div>
      <StorefrontShell categories={home?.categories ?? []}>
        <StorefrontLandingSections page={page} context={context} preview />
      </StorefrontShell>
    </div>
  );
}
