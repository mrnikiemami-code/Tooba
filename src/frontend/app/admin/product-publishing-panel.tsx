"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import {
  getAdminProductPublishReadiness,
  mutateAdminProductLifecycle,
} from "./host-client.ts";
import {
  buildPublishChecklist,
  formatProductLifecycleLabelFa,
  type ProductPublishReadiness,
} from "./product-publishing-panel-model.ts";

export type ProductPublishingPanelMode = "view" | "edit";

/**
 * تب انتشار Workspace — چک‌لیست آمادگی تجمیعی + چرخهٔ عمر Draft/Published/Archived.
 */
export function ProductPublishingPanel({
  productId,
  status,
  statusUpdatedAt,
  canPublish,
  mode,
  purchasableHint,
  onStatusChanged,
  onNavigateTab,
}: {
  productId: string;
  status: string;
  statusUpdatedAt?: string | null;
  canPublish: boolean;
  mode: ProductPublishingPanelMode;
  purchasableHint: boolean;
  onStatusChanged: (viewHint?: { status: string }) => void;
  onNavigateTab: (tabId: string) => void;
}) {
  const [readiness, setReadiness] = useState<ProductPublishReadiness | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const actionable = canPublish && mode === "edit";
  const checklist = useMemo(() => buildPublishChecklist(readiness), [readiness]);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    const result = await getAdminProductPublishReadiness(productId);
    setLoading(false);
    if (!result.ok) {
      setError(result.message);
      setReadiness(null);
      return;
    }
    setReadiness(result.readiness);
  }, [productId]);

  useEffect(() => {
    void reload();
  }, [reload]);

  async function runAction(action: "publish" | "unpublish" | "archive" | "restore" | "delete") {
    if (!actionable) return;
    const confirms: Record<string, string> = {
      publish: "محصول منتشر شود؟",
      unpublish: "انتشار لغو و محصول به پیش‌نویس برگردد؟",
      archive: "محصول بایگانی شود؟ داده‌ها حفظ می‌مانند و حذف سخت انجام نمی‌شود.",
      restore: "محصول از بایگانی به پیش‌نویس بازگردد؟",
      delete: "بایگانی / حذف امن انجام شود؟ در صورت ارجاع، فقط آرشیو می‌شود.",
    };
    if (!window.confirm(confirms[action] ?? "ادامه؟")) return;

    setBusy(true);
    setError(null);
    const result = await mutateAdminProductLifecycle(productId, action);
    setBusy(false);
    if (!result.ok) {
      setError(result.message);
      return;
    }
    if (action === "delete") {
      onStatusChanged({ status: "__deleted__" });
      return;
    }
    if (result.view) {
      onStatusChanged({ status: result.view.status });
    } else {
      onStatusChanged();
    }
    await reload();
  }

  if (loading) {
    return <p className="text-sm text-muted">در حال بارگذاری آمادگی انتشار…</p>;
  }

  return (
    <div className="space-y-4" data-testid="admin-product-publishing-panel">
      {error ? (
        <p className="rounded-ds border border-danger/40 bg-danger/10 px-3 py-2 text-sm text-danger" role="alert">
          {error}
        </p>
      ) : null}

      <div className="grid gap-4 md:grid-cols-2">
        <section aria-labelledby="publish-lifecycle-heading">
          <h3 id="publish-lifecycle-heading" className="mb-2 font-semibold">
            وضعیت انتشار
          </h3>
          <p className="text-lg font-semibold" data-testid="publish-lifecycle-label">
            {formatProductLifecycleLabelFa(status)}
          </p>
          {statusUpdatedAt ? (
            <p className="mt-1 text-sm text-muted">آخرین به‌روزرسانی: {statusUpdatedAt}</p>
          ) : null}
          <p className="mt-2 text-sm text-muted">
            انتشار هویت Catalog است و با قابل‌خرید بودن Offer یکی نیست.
          </p>
        </section>

        <section aria-labelledby="publish-readiness-heading">
          <h3 id="publish-readiness-heading" className="mb-2 font-semibold">
            انتشار محصول
          </h3>
          <ul className="space-y-2" data-testid="publish-readiness-checklist">
            {checklist.map((item) => (
              <li key={item.code} className="flex flex-wrap items-center gap-2 text-sm">
                <span aria-hidden="true">{item.ready ? "✅" : "❌"}</span>
                <span className={item.ready ? "text-foreground" : "text-danger"}>{item.label}</span>
                {!item.ready ? (
                  <button
                    type="button"
                    className="min-h-11 rounded-ds border border-border px-3 text-xs hover:bg-secondary"
                    onClick={() => onNavigateTab(item.workspaceTab)}
                  >
                    تکمیل در تب مربوطه
                  </button>
                ) : null}
              </li>
            ))}
          </ul>
          <p className="mt-3 text-sm font-medium" data-testid="publish-readiness-summary">
            {readiness?.messageFa ?? "—"}
          </p>
        </section>
      </div>

      <section className="rounded-ds border border-border p-4" data-testid="product-commercial-readonly">
        <p className="font-semibold">خلاصه تجاری (فقط‌خواندنی)</p>
        <p className="mt-1 text-sm text-muted">
          قیمت و موجودی متعلق به Offer هستند و در آمادگی انتشار Product دخیل نیستند.
        </p>
        <p className="mt-2 text-sm">
          وضعیت فروشگاهی: {purchasableHint ? "آمادهٔ فروش (Offer)" : "غیرقابل‌خرید تا تکمیل Offer"}
        </p>
      </section>

      {actionable ? (
        <section className="rounded-ds border border-border p-4" aria-labelledby="publish-actions-heading">
          <h3 id="publish-actions-heading" className="font-semibold">
            عملیات چرخهٔ عمر
          </h3>
          <div className="mt-4 flex flex-wrap gap-2">
            <button
              type="button"
              disabled={busy || status === "Published" || readiness?.isReady === false}
              className="min-h-11 rounded-ds bg-emerald-600 px-4 text-sm text-white hover:bg-emerald-700 disabled:opacity-50"
              onClick={() => void runAction("publish")}
              data-testid="publish-action-publish"
            >
              انتشار
            </button>
            <button
              type="button"
              disabled={busy || status !== "Published"}
              className="min-h-11 rounded-ds border border-border px-4 text-sm hover:bg-secondary disabled:opacity-50"
              onClick={() => void runAction("unpublish")}
              data-testid="publish-action-unpublish"
            >
              لغو انتشار
            </button>
            <button
              type="button"
              disabled={busy || status === "Archived"}
              className="min-h-11 rounded-ds border border-border px-4 text-sm hover:bg-secondary disabled:opacity-50"
              onClick={() => void runAction("archive")}
              data-testid="publish-action-archive"
            >
              بایگانی
            </button>
            <button
              type="button"
              disabled={busy || status !== "Archived"}
              className="min-h-11 rounded-ds border border-border px-4 text-sm hover:bg-secondary disabled:opacity-50"
              onClick={() => void runAction("restore")}
              data-testid="publish-action-restore"
            >
              بازگردانی به پیش‌نویس
            </button>
            <button
              type="button"
              disabled={busy}
              className="min-h-11 rounded-ds border border-danger/40 px-4 text-sm text-danger hover:bg-danger/10 disabled:opacity-50"
              onClick={() => void runAction("delete")}
              data-testid="publish-action-delete"
            >
              بایگانی / حذف امن
            </button>
          </div>
          {status !== "Published" && readiness && !readiness.isReady ? (
            <p className="mt-3 text-sm text-muted">دکمهٔ انتشار تا تکمیل آمادگی غیرفعال است؛ Host همچنان مرجع نهایی است.</p>
          ) : null}
        </section>
      ) : (
        <p className="text-sm text-muted" data-testid="publish-view-only-note">
          حالت مشاهده: وضعیت و چک‌لیست قابل‌دیدن است؛ عملیات انتشار مجاز نیست.
        </p>
      )}
    </div>
  );
}
