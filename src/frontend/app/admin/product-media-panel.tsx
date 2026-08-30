"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import {
  attachAdminProductMedia,
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
import { MediaLibraryDialog } from "./media-library-dialog.tsx";
import { mapAdminErrorMessage } from "./admin-error-map.ts";
import type { MediaAssetDto } from "./media-api.ts";

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
  const [libraryOpen, setLibraryOpen] = useState(false);
  const [selectedId, setSelectedId] = useState<string | null>(null);

  const editable = canEdit && mode === "edit";
  const rows = useMemo(() => sortMediaItems(items), [items]);
  const primary = rows.find((row) => row.primary) ?? rows[0] ?? null;
  const selected =
    rows.find((row) => row.mediaAssetId === selectedId) ?? primary ?? rows[0] ?? null;
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

  useEffect(() => {
    if (!rows.length) {
      setSelectedId(null);
      return;
    }
    if (!selectedId || !rows.some((row) => row.mediaAssetId === selectedId)) {
      setSelectedId((rows.find((row) => row.primary) ?? rows[0]).mediaAssetId);
    }
  }, [rows, selectedId]);

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

  async function onLibraryConfirm(assets: MediaAssetDto[]) {
    if (!editable || busy) return;
    setBusy(true);
    setError(null);
    let lastOk: { ok: true; media: ProductMediaItem[] } | null = null;
    for (const asset of assets) {
      if (items.some((row) => row.mediaAssetId === asset.mediaAssetId)) continue;
      const result = toMutationResult(await attachAdminProductMedia(productId, asset.mediaAssetId));
      if (!result.ok) {
        setBusy(false);
        setError(mapAdminErrorMessage(result.message, "fa"));
        return;
      }
      lastOk = result;
    }
    setBusy(false);
    setLibraryOpen(false);
    if (lastOk) await refreshAfterMutation(lastOk);
    else await reload();
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

  const selectedIndex = selected
    ? rows.findIndex((row) => row.mediaAssetId === selected.mediaAssetId)
    : -1;

  return (
    <div className="space-y-4" data-testid="admin-product-media-panel">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <p className="font-semibold">گالری تصویر</p>
          <p className="mt-1 text-sm text-muted">
            پیش‌نمایش تصویر اصلی، بندانگشتی‌ها، ترتیب، متن جایگزین و تصویر اصلی — بدون نمایش شناسه در مسیر عادی.
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

      {rows.length === 0 ? (
        <div
          className="flex min-h-48 flex-col items-center justify-center gap-2 rounded-ds border border-dashed border-border bg-secondary/30 px-4 py-8 text-center"
          data-testid="admin-product-media-empty"
        >
          <p className="font-medium">هنوز رسانه‌ای به این محصول وصل نشده است</p>
          <p className="max-w-md text-sm text-muted">
            پس از افزودن رسانه از کتابخانه، گالری با تصویر اصلی و بندانگشتی‌ها نمایش داده می‌شود.
          </p>
        </div>
      ) : (
        <div className="grid gap-4 xl:grid-cols-[minmax(0,1.2fr)_minmax(0,1fr)]">
          <div className="space-y-3" data-testid="admin-product-media-primary">
            <div className="relative aspect-square overflow-hidden rounded-ds border border-border bg-secondary">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img
                src={storefrontMediaUrl((selected ?? primary)!.mediaAssetId)}
                alt={(selected ?? primary)!.altText ?? "تصویر محصول"}
                className="h-full w-full object-contain p-6"
              />
              {(selected ?? primary)?.primary ? (
                <span className="absolute start-3 top-3 rounded-ds bg-success/90 px-2 py-0.5 text-xs text-white">
                  تصویر اصلی
                </span>
              ) : null}
            </div>
            <ul className="flex flex-wrap gap-2" data-testid="admin-product-media-thumbs">
              {rows.map((item, index) => {
                const active = item.mediaAssetId === (selected?.mediaAssetId ?? primary?.mediaAssetId);
                return (
                  <li key={item.mediaAssetId}>
                    <button
                      type="button"
                      className={
                        active
                          ? "rounded-ds border-2 border-primary p-0.5"
                          : "rounded-ds border border-border p-0.5 hover:border-primary/50"
                      }
                      aria-label={`انتخاب رسانه ${index + 1}`}
                      aria-pressed={active}
                      onClick={() => setSelectedId(item.mediaAssetId)}
                    >
                      {/* eslint-disable-next-line @next/next/no-img-element */}
                      <img
                        src={storefrontMediaUrl(item.mediaAssetId)}
                        alt={item.altText ?? `رسانه ${index + 1}`}
                        className="size-16 rounded-[calc(var(--radius-ds)-2px)] bg-secondary object-contain p-1 sm:size-20"
                      />
                    </button>
                  </li>
                );
              })}
            </ul>
          </div>

          {selected ? (
            <div className="space-y-3 rounded-ds border border-border bg-surface p-4" data-testid="admin-product-media-detail">
              <p className="text-sm font-medium text-muted">جزئیات رسانه انتخاب‌شده</p>
              {editable ? (
                <div className="flex flex-wrap gap-2">
                  <button
                    type="button"
                    disabled={busy || selected.primary}
                    className="min-h-10 rounded-ds border border-border px-3 text-sm hover:bg-secondary disabled:opacity-50"
                    onClick={() => void onSetPrimary(selected.mediaAssetId)}
                  >
                    تنظیم به‌عنوان تصویر اصلی
                  </button>
                  <button
                    type="button"
                    disabled={busy || selectedIndex <= 0}
                    className="min-h-10 rounded-ds border border-border px-3 text-sm hover:bg-secondary disabled:opacity-50"
                    aria-label="جابه‌جایی به بالا در گالری"
                    onClick={() => void onReorder(selected.mediaAssetId, -1)}
                  >
                    جلوتر
                  </button>
                  <button
                    type="button"
                    disabled={busy || selectedIndex < 0 || selectedIndex >= rows.length - 1}
                    className="min-h-10 rounded-ds border border-border px-3 text-sm hover:bg-secondary disabled:opacity-50"
                    aria-label="جابه‌جایی به پایین در گالری"
                    onClick={() => void onReorder(selected.mediaAssetId, 1)}
                  >
                    عقب‌تر
                  </button>
                  <button
                    type="button"
                    disabled={busy}
                    className="min-h-10 rounded-ds border border-danger/40 px-3 text-sm text-danger hover:bg-danger/10 disabled:opacity-50"
                    onClick={() => void onRemove(selected.mediaAssetId)}
                  >
                    حذف از محصول
                  </button>
                </div>
              ) : (
                <p className="text-sm text-muted">
                  {selected.primary ? "این تصویر به‌عنوان تصویر اصلی علامت خورده است." : "برای ویرایش گالری وارد حالت ویرایش شوید."}
                </p>
              )}
              <label className="block text-sm">
                متن جایگزین
                <input
                  className="mt-1 min-h-10 w-full rounded-ds border border-border bg-surface px-3"
                  value={altDrafts[selected.mediaAssetId] ?? ""}
                  disabled={!editable || busy}
                  onChange={(event) =>
                    setAltDrafts((prev) => ({ ...prev, [selected.mediaAssetId]: event.target.value }))
                  }
                />
              </label>
              {editable ? (
                <button
                  type="button"
                  disabled={busy || (altDrafts[selected.mediaAssetId] ?? "") === (selected.altText ?? "")}
                  className="min-h-10 rounded-ds border border-border px-3 text-sm hover:bg-secondary disabled:opacity-50"
                  onClick={() => void onSaveAlt(selected.mediaAssetId)}
                >
                  ذخیره متن جایگزین
                </button>
              ) : null}
            </div>
          ) : null}
        </div>
      )}

      {editable ? (
        <div className="space-y-3 rounded-ds border border-border bg-surface p-3">
          <button
            type="button"
            disabled={busy}
            className="min-h-11 rounded-ds bg-primary px-4 text-sm font-medium text-primary-foreground disabled:opacity-50"
            onClick={() => setLibraryOpen(true)}
            data-testid="admin-product-media-open-library"
          >
            افزودن رسانه
          </button>
          <p className="text-xs text-muted">
            از کتابخانهٔ Media انتخاب کنید یا فایل واقعی بارگذاری کنید. «حذف از محصول» فقط ارجاع را برمی‌دارد و دارایی را از کتابخانه پاک نمی‌کند.
          </p>
          {altDirty ? (
            <p className="text-xs text-amber-800">متن جایگزین ذخیره‌نشده دارید؛ «ذخیره متن جایگزین» را بزنید.</p>
          ) : null}
        </div>
      ) : null}

      <MediaLibraryDialog
        open={libraryOpen}
        title="افزودن رسانه به محصول"
        selectionMode="multi"
        alreadyAssignedIds={items.map((row) => row.mediaAssetId)}
        onClose={() => {
          if (!busy) setLibraryOpen(false);
        }}
        onConfirm={onLibraryConfirm}
      />
    </div>
  );
}
