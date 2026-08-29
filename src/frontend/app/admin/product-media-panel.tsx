"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import {
  attachAdminProductMedia,
  attachAdminProductPlaceholderMedia,
  getAdminProductMediaReadiness,
  listAdminProductMedia,
  patchAdminProductMediaAlt,
  removeAdminProductMedia,
  reorderAdminProductMedia,
  setAdminProductMediaPrimary,
} from "./host-client.ts";
import {
  altDraftsFromItems,
  formatMediaCountLabel,
  formatMediaReadinessLabel,
  isAltDraftDirty,
  moveMediaAssetId,
  normalizeMediaItems,
  sortMediaItems,
  type ProductMediaItem,
  type ProductMediaReadiness,
} from "./product-media-panel-model.ts";
import { storefrontMediaUrl } from "../storefront/storefront-api";
import { useProductWorkspaceDirtyRegistration } from "./product-workspace-dirty-context";

export type ProductMediaPanelMode = "view" | "edit";

/**
 * پنل گالری رسانهٔ محصول Workspace — بدون AgGrid و بدون Guid به‌عنوان UX اصلی.
 */
export function ProductMediaPanel({
  productId,
  canEdit,
  mode,
}: {
  productId: string;
  canEdit: boolean;
  mode: ProductMediaPanelMode;
}) {
  const [items, setItems] = useState<ProductMediaItem[]>([]);
  const [readiness, setReadiness] = useState<ProductMediaReadiness | null>(null);
  const [altDrafts, setAltDrafts] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [advancedAssetId, setAdvancedAssetId] = useState("");

  const editable = canEdit && mode === "edit";
  const rows = useMemo(() => sortMediaItems(items), [items]);
  const primary = rows.find((row) => row.primary) ?? rows[0] ?? null;
  const altDirty = isAltDraftDirty(items, altDrafts);

  const discardAltDrafts = useCallback(() => {
    setAltDrafts(altDraftsFromItems(items));
  }, [items]);

  useProductWorkspaceDirtyRegistration("media", altDirty && editable, discardAltDrafts);

  const applyItems = useCallback((next: ProductMediaItem[]) => {
    setItems(next);
    setAltDrafts(altDraftsFromItems(next));
  }, []);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    const [mediaResult, readyResult] = await Promise.all([
      listAdminProductMedia(productId),
      getAdminProductMediaReadiness(productId),
    ]);
    setLoading(false);
    if (!mediaResult.ok) {
      setError(mediaResult.message);
      setItems([]);
      setReadiness(null);
      return;
    }
    applyItems(normalizeMediaItems(mediaResult.media));
    if (readyResult.ok) {
      setReadiness(readyResult.readiness);
    } else {
      setReadiness(null);
    }
  }, [applyItems, productId]);

  useEffect(() => {
    void reload();
  }, [reload]);

  async function refreshAfterMutation(
    result: { ok: true; media: ProductMediaItem[] } | { ok: false; message: string },
  ) {
    if (!result.ok) {
      setError(result.message);
      return;
    }
    applyItems(result.media);
    const readyResult = await getAdminProductMediaReadiness(productId);
    if (readyResult.ok) setReadiness(readyResult.readiness);
    setError(null);
  }

  function toMutationResult(
    result: { ok: true; media: Parameters<typeof normalizeMediaItems>[0] } | { ok: false; message: string },
  ): { ok: true; media: ProductMediaItem[] } | { ok: false; message: string } {
    if (!result.ok) return result;
    return { ok: true, media: normalizeMediaItems(result.media) };
  }

  async function onAddPlaceholder() {
    if (!editable || busy) return;
    setBusy(true);
    setError(null);
    const result = await attachAdminProductPlaceholderMedia(productId);
    setBusy(false);
    await refreshAfterMutation(toMutationResult(result));
  }

  async function onAdvancedAttach() {
    if (!editable || busy) return;
    const assetId = advancedAssetId.trim();
    if (!assetId) {
      setError("شناسهٔ دارایی برای مسیر پیشرفته لازم است");
      return;
    }
    setBusy(true);
    setError(null);
    const result = await attachAdminProductMedia(productId, assetId);
    setBusy(false);
    if (result.ok) {
      setAdvancedAssetId("");
    }
    await refreshAfterMutation(toMutationResult(result));
  }

  async function onReorder(mediaAssetId: string, direction: -1 | 1) {
    if (!editable || busy) return;
    const ordered = sortMediaItems(items).map((row) => row.mediaAssetId);
    const next = moveMediaAssetId(ordered, mediaAssetId, direction);
    if (!next) return;
    setBusy(true);
    setError(null);
    const result = await reorderAdminProductMedia(productId, next);
    setBusy(false);
    await refreshAfterMutation(toMutationResult(result));
  }

  async function onSetPrimary(mediaAssetId: string) {
    if (!editable || busy) return;
    setBusy(true);
    setError(null);
    const result = await setAdminProductMediaPrimary(productId, mediaAssetId);
    setBusy(false);
    await refreshAfterMutation(toMutationResult(result));
  }

  async function onSaveAlt(mediaAssetId: string) {
    if (!editable || busy) return;
    setBusy(true);
    setError(null);
    const result = await patchAdminProductMediaAlt(
      productId,
      mediaAssetId,
      altDrafts[mediaAssetId]?.trim() || null,
    );
    setBusy(false);
    await refreshAfterMutation(toMutationResult(result));
  }

  async function onRemove(mediaAssetId: string) {
    if (!editable || busy) return;
    setBusy(true);
    setError(null);
    const result = await removeAdminProductMedia(productId, mediaAssetId);
    setBusy(false);
    await refreshAfterMutation(toMutationResult(result));
  }

  if (loading) {
    return <p className="text-sm text-muted">در حال بارگذاری رسانه…</p>;
  }

  return (
    <div className="space-y-4" data-testid="admin-product-media-panel">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <p className="font-semibold">گالری تصویر</p>
          <p className="mt-1 text-sm text-muted">
            انتساب به دارایی رسانه؛ باینری در Catalog ذخیره نمی‌شود. کتابخانهٔ Media هنوز فعال نیست.
          </p>
        </div>
        <div className="flex flex-wrap gap-2 text-xs">
          <span className="rounded-full bg-slate-100 px-2 py-0.5 font-medium text-slate-700">
            {formatMediaCountLabel(readiness?.mediaCount ?? items.length)}
          </span>
          <span
            className={
              readiness?.isReady
                ? "rounded-full bg-emerald-50 px-2 py-0.5 font-medium text-emerald-900"
                : "rounded-full bg-amber-50 px-2 py-0.5 font-medium text-amber-900"
            }
          >
            {formatMediaReadinessLabel(readiness)}
          </span>
        </div>
      </div>

      {error ? (
        <p className="rounded-ds border border-danger/30 bg-danger/5 px-3 py-2 text-sm text-danger" role="alert">
          {error}
        </p>
      ) : null}

      {primary ? (
        <div className="rounded-ds border border-border bg-surface p-3" data-testid="admin-product-media-primary">
          <p className="mb-2 text-sm font-medium">تصویر اصلی</p>
          <div className="relative mx-auto aspect-square max-w-xs overflow-hidden rounded-ds bg-secondary sm:mx-0">
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              src={storefrontMediaUrl(primary.mediaAssetId)}
              alt={primary.altText ?? "تصویر اصلی"}
              className="h-full w-full object-contain p-4"
            />
            <span className="absolute start-2 top-2 rounded-ds bg-success/90 px-2 py-0.5 text-xs text-white">
              تصویر اصلی
            </span>
          </div>
        </div>
      ) : (
        <p className="text-sm text-amber-800">تصویر اصلی تعیین نشده</p>
      )}

      {rows.length === 0 ? (
        <p className="text-sm text-muted">هنوز رسانه‌ای به این محصول وصل نشده است.</p>
      ) : (
        <ul className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
          {rows.map((item, index) => (
            <li key={item.mediaAssetId} className="rounded-ds border border-border bg-surface p-3">
              <div className="relative aspect-square overflow-hidden rounded-ds bg-secondary">
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img
                  src={storefrontMediaUrl(item.mediaAssetId)}
                  alt={item.altText ?? `رسانه ${index + 1}`}
                  className="h-full w-full object-contain p-3"
                />
                {item.primary ? (
                  <span className="absolute start-2 top-2 rounded-ds bg-success/90 px-2 py-0.5 text-xs text-white">
                    تصویر اصلی
                  </span>
                ) : null}
              </div>
              {editable ? (
                <div className="mt-3 flex flex-wrap gap-2">
                  <button
                    type="button"
                    disabled={busy || item.primary}
                    className="rounded-ds border border-border px-2 py-1.5 text-xs hover:bg-secondary disabled:opacity-50"
                    onClick={() => void onSetPrimary(item.mediaAssetId)}
                  >
                    تنظیم به‌عنوان تصویر اصلی
                  </button>
                  <button
                    type="button"
                    disabled={busy || index === 0}
                    className="rounded-ds border border-border px-2 py-1.5 text-xs hover:bg-secondary disabled:opacity-50"
                    aria-label="جابه‌جایی به بالا در گالری"
                    onClick={() => void onReorder(item.mediaAssetId, -1)}
                  >
                    بالا
                  </button>
                  <button
                    type="button"
                    disabled={busy || index >= rows.length - 1}
                    className="rounded-ds border border-border px-2 py-1.5 text-xs hover:bg-secondary disabled:opacity-50"
                    aria-label="جابه‌جایی به پایین در گالری"
                    onClick={() => void onReorder(item.mediaAssetId, 1)}
                  >
                    پایین
                  </button>
                  <button
                    type="button"
                    disabled={busy}
                    className="rounded-ds border border-danger/40 px-2 py-1.5 text-xs text-danger hover:bg-danger/10 disabled:opacity-50"
                    onClick={() => void onRemove(item.mediaAssetId)}
                  >
                    حذف از محصول
                  </button>
                </div>
              ) : null}
              <label className="mt-2 block text-sm">
                متن جایگزین
                <input
                  className="mt-1 min-h-10 w-full rounded-ds border border-border bg-surface px-3"
                  value={altDrafts[item.mediaAssetId] ?? ""}
                  disabled={!editable || busy}
                  onChange={(event) =>
                    setAltDrafts((prev) => ({ ...prev, [item.mediaAssetId]: event.target.value }))
                  }
                />
              </label>
              {editable ? (
                <button
                  type="button"
                  disabled={busy || (altDrafts[item.mediaAssetId] ?? "") === (item.altText ?? "")}
                  className="mt-2 rounded-ds border border-border px-2 py-1.5 text-xs hover:bg-secondary disabled:opacity-50"
                  onClick={() => void onSaveAlt(item.mediaAssetId)}
                >
                  ذخیره متن جایگزین
                </button>
              ) : null}
            </li>
          ))}
        </ul>
      )}

      {editable ? (
        <div className="space-y-3 rounded-ds border border-border bg-surface p-3">
          <button
            type="button"
            disabled={busy}
            className="min-h-11 rounded-ds bg-primary px-4 text-sm font-medium text-primary-foreground disabled:opacity-50"
            onClick={() => void onAddPlaceholder()}
          >
            افزودن تصویر نمایشی (موقت)
          </button>
          <p className="text-xs text-muted">
            راه‌حل موقت تا راه‌اندازی کتابخانهٔ Media: یک شناسهٔ مات نمایشی ساخته و به محصول وصل می‌شود. پیش‌نمایش از مسیر امن ویترین است؛ این جایگزین آپلود فایل نیست.
          </p>
          <details className="rounded-ds border border-border bg-secondary/30 p-3">
            <summary className="cursor-pointer text-sm font-medium text-muted">
              اتصال پیشرفته (شناسهٔ دارایی)
            </summary>
            <div className="mt-3 space-y-2">
              <p className="text-xs text-muted">
                فقط برای اتصال دستی شناسهٔ مات موجود. کتابخانهٔ Media و بارگذاری فایل هنوز آماده نیست.
              </p>
              <label className="block text-sm">
                شناسه دارایی رسانه
                <input
                  className="mt-1 min-h-11 w-full rounded-ds border border-border bg-surface px-3"
                  dir="ltr"
                  value={advancedAssetId}
                  onChange={(event) => setAdvancedAssetId(event.target.value)}
                />
              </label>
              <button
                type="button"
                disabled={busy}
                className="min-h-10 rounded-ds border border-border px-3 text-sm hover:bg-secondary disabled:opacity-50"
                onClick={() => void onAdvancedAttach()}
              >
                پیوست شناسه
              </button>
            </div>
          </details>
          {altDirty ? (
            <p className="text-xs text-amber-800">متن جایگزین ذخیره‌نشده دارید؛ برای هر ردیف «ذخیره متن جایگزین» را بزنید.</p>
          ) : null}
        </div>
      ) : null}
    </div>
  );
}
