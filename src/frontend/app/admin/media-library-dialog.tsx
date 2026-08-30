"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import {
  mediaPreviewUrl,
  mediaUploadItemMessage,
  queryAdminMediaLibrary,
  uploadAdminMediaFiles,
  type MediaAssetDto,
} from "./media-api.ts";
import { mapAdminErrorMessage } from "./admin-error-map.ts";

export type MediaLibrarySelectionMode = "single" | "multi";

export interface MediaLibraryDialogProps {
  open: boolean;
  title?: string;
  selectionMode: MediaLibrarySelectionMode;
  /** دارایی‌هایی که از قبل به موجودیت وصل‌اند (برای غیرفعال‌کردن دوباره انتخاب در محصول). */
  alreadyAssignedIds?: ReadonlySet<string> | string[];
  onClose: () => void;
  onConfirm: (assets: MediaAssetDto[]) => void | Promise<void>;
}

const PAGE_SIZE = 24;

/**
 * دیالوگ کتابخانهٔ رسانه — تب کتابخانه / آپلود، جستجو، صفحه‌بندی، انتخاب تکی یا چندتایی.
 * شناسهٔ خام Guid در UX عادی نشان داده نمی‌شود.
 */
export function MediaLibraryDialog({
  open,
  title = "کتابخانه رسانه",
  selectionMode,
  alreadyAssignedIds,
  onClose,
  onConfirm,
}: MediaLibraryDialogProps) {
  const assigned = useMemo(() => {
    if (!alreadyAssignedIds) return new Set<string>();
    return alreadyAssignedIds instanceof Set
      ? alreadyAssignedIds
      : new Set(alreadyAssignedIds);
  }, [alreadyAssignedIds]);

  const [tab, setTab] = useState<"library" | "upload">("library");
  const [search, setSearch] = useState("");
  const [searchApplied, setSearchApplied] = useState("");
  const [page, setPage] = useState(1);
  const [items, setItems] = useState<MediaAssetDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selected, setSelected] = useState<Map<string, MediaAssetDto>>(new Map());
  const [busy, setBusy] = useState(false);
  const [uploadNotes, setUploadNotes] = useState<string[]>([]);

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    const result = await queryAdminMediaLibrary({
      search: searchApplied || null,
      page,
      pageSize: PAGE_SIZE,
    });
    setLoading(false);
    if (result.state !== "ok" || !result.data) {
      setItems([]);
      setTotalCount(0);
      setError(mapAdminErrorMessage(result.message ?? "host-unreachable", "fa"));
      return;
    }
    setItems(result.data.items);
    setTotalCount(result.data.totalCount);
  }, [page, searchApplied]);

  useEffect(() => {
    if (!open) return;
    setTab("library");
    setSearch("");
    setSearchApplied("");
    setPage(1);
    setSelected(new Map());
    setUploadNotes([]);
    setError(null);
  }, [open]);

  useEffect(() => {
    if (!open) return;
    void reload();
  }, [open, reload]);

  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape" && !busy) onClose();
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [open, busy, onClose]);

  if (!open) return null;

  function toggleAsset(asset: MediaAssetDto) {
    if (assigned.has(asset.mediaAssetId)) return;
    setSelected((prev) => {
      const next = new Map(prev);
      if (selectionMode === "single") {
        next.clear();
        next.set(asset.mediaAssetId, asset);
        return next;
      }
      if (next.has(asset.mediaAssetId)) next.delete(asset.mediaAssetId);
      else next.set(asset.mediaAssetId, asset);
      return next;
    });
  }

  async function handleUpload(fileList: FileList | null) {
    if (!fileList?.length || busy) return;
    const files = Array.from(fileList);
    setBusy(true);
    setError(null);
    setUploadNotes([]);
    const result = await uploadAdminMediaFiles(files);
    setBusy(false);
    if (result.state !== "ok" || !result.data) {
      setError(mapAdminErrorMessage(result.message ?? "media.upload.failed", "fa"));
      return;
    }
    const notes: string[] = [];
    const uploaded: MediaAssetDto[] = [];
    for (const item of result.data.items) {
      if (item.ok) {
        uploaded.push(item.asset);
        notes.push(`«${item.asset.originalFileName}» با موفقیت بارگذاری شد.`);
      } else {
        notes.push(`«${item.fileName || "فایل"}»: ${mediaUploadItemMessage(item)}`);
      }
    }
    setUploadNotes(notes);
    if (uploaded.length) {
      setSelected((prev) => {
        const next = selectionMode === "single" ? new Map<string, MediaAssetDto>() : new Map(prev);
        for (const asset of uploaded) {
          if (selectionMode === "single") {
            next.clear();
            next.set(asset.mediaAssetId, asset);
          } else {
            next.set(asset.mediaAssetId, asset);
          }
        }
        return next;
      });
      setTab("library");
      setPage(1);
      setSearchApplied("");
      void reload();
    }
  }

  async function handleConfirm() {
    const assets = Array.from(selected.values());
    if (!assets.length || busy) return;
    setBusy(true);
    setError(null);
    try {
      await onConfirm(assets);
    } catch {
      setError(mapAdminErrorMessage("media.upload.failed", "fa"));
      setBusy(false);
      return;
    }
    setBusy(false);
  }

  const selectedCount = selected.size;

  return (
    <div
      className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-3 sm:items-center"
      role="dialog"
      aria-modal="true"
      aria-labelledby="media-library-title"
      data-testid="admin-media-library-dialog"
      onMouseDown={(e) => {
        if (e.target === e.currentTarget && !busy) onClose();
      }}
    >
      <div className="flex max-h-[92vh] w-full max-w-4xl flex-col overflow-hidden rounded-2xl bg-white shadow-xl">
        <div className="flex items-start justify-between gap-3 border-b border-gray-100 px-5 py-4">
          <div>
            <h3 id="media-library-title" className="text-base font-semibold text-slate-900">
              {title}
            </h3>
            <p className="mt-1 text-xs text-slate-500">
              از کتابخانه انتخاب کنید یا فایل تصویر بارگذاری کنید. حذف از موجودیت، فایل را از کتابخانه پاک نمی‌کند.
            </p>
          </div>
          <button
            type="button"
            className="min-h-10 rounded-xl px-3 text-sm text-slate-600 hover:bg-slate-50"
            onClick={onClose}
            disabled={busy}
            aria-label="بستن کتابخانه رسانه"
            data-testid="admin-media-library-close"
          >
            بستن
          </button>
        </div>

        <div className="flex gap-1 border-b border-gray-100 px-4 pt-2" role="tablist">
          <button
            type="button"
            role="tab"
            aria-selected={tab === "library"}
            className={
              tab === "library"
                ? "min-h-10 rounded-t-xl bg-slate-100 px-4 text-sm font-semibold text-slate-900"
                : "min-h-10 rounded-t-xl px-4 text-sm font-medium text-slate-600 hover:bg-slate-50"
            }
            onClick={() => setTab("library")}
            data-testid="admin-media-tab-library"
          >
            کتابخانه
          </button>
          <button
            type="button"
            role="tab"
            aria-selected={tab === "upload"}
            className={
              tab === "upload"
                ? "min-h-10 rounded-t-xl bg-slate-100 px-4 text-sm font-semibold text-slate-900"
                : "min-h-10 rounded-t-xl px-4 text-sm font-medium text-slate-600 hover:bg-slate-50"
            }
            onClick={() => setTab("upload")}
            data-testid="admin-media-tab-upload"
          >
            آپلود فایل
          </button>
        </div>

        <div className="flex min-h-0 flex-1 flex-col overflow-hidden px-5 py-4">
          {error ? (
            <p className="mb-3 rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700" role="alert">
              {error}
            </p>
          ) : null}

          {tab === "upload" ? (
            <div className="space-y-3" data-testid="admin-media-upload-panel">
              <label className="flex min-h-40 cursor-pointer flex-col items-center justify-center gap-2 rounded-2xl border border-dashed border-gray-300 bg-slate-50 px-4 text-center hover:border-blue-400 hover:bg-blue-50/40">
                <span className="text-sm font-medium text-slate-800">انتخاب فایل تصویر</span>
                <span className="text-xs text-slate-500">JPEG، PNG، WebP یا GIF — حداکثر حدود ۵ مگابایت برای هر فایل</span>
                <input
                  type="file"
                  accept="image/jpeg,image/png,image/webp,image/gif,.jpg,.jpeg,.png,.webp,.gif"
                  multiple
                  className="sr-only"
                  disabled={busy}
                  data-testid="admin-media-upload-input"
                  onChange={(e) => {
                    void handleUpload(e.target.files);
                    e.target.value = "";
                  }}
                />
              </label>
              {uploadNotes.length ? (
                <ul className="space-y-1 text-sm text-slate-700" data-testid="admin-media-upload-notes">
                  {uploadNotes.map((note, i) => (
                    <li key={`${i}-${note}`}>{note}</li>
                  ))}
                </ul>
              ) : null}
            </div>
          ) : (
            <div className="flex min-h-0 flex-1 flex-col gap-3" data-testid="admin-media-library-panel">
              <form
                className="flex flex-wrap gap-2"
                onSubmit={(e) => {
                  e.preventDefault();
                  setPage(1);
                  setSearchApplied(search.trim());
                }}
              >
                <input
                  className="min-h-11 min-w-[12rem] flex-1 rounded-xl border border-gray-200 px-3 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="جستجو بر اساس نام فایل…"
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  data-testid="admin-media-search"
                  aria-label="جستجوی کتابخانه رسانه"
                />
                <button
                  type="submit"
                  className="min-h-11 rounded-xl border border-gray-200 bg-white px-4 text-sm font-medium hover:bg-slate-50"
                  data-testid="admin-media-search-submit"
                >
                  جستجو
                </button>
              </form>

              {loading ? (
                <p className="py-8 text-center text-sm text-slate-500">در حال بارگذاری کتابخانه…</p>
              ) : items.length === 0 ? (
                <div
                  className="flex min-h-40 flex-col items-center justify-center gap-2 rounded-2xl border border-dashed border-gray-200 bg-slate-50 text-sm text-slate-600"
                  data-testid="admin-media-library-empty"
                >
                  <p>رسانه‌ای در این صفحه یافت نشد.</p>
                  <button
                    type="button"
                    className="text-blue-600 underline"
                    onClick={() => setTab("upload")}
                  >
                    آپلود فایل جدید
                  </button>
                </div>
              ) : (
                <ul
                  className="grid grid-cols-2 gap-3 overflow-y-auto sm:grid-cols-3 md:grid-cols-4"
                  data-testid="admin-media-library-grid"
                >
                  {items.map((asset) => {
                    const isAssigned = assigned.has(asset.mediaAssetId);
                    const isSelected = selected.has(asset.mediaAssetId);
                    const preview = mediaPreviewUrl(asset.mediaAssetId);
                    return (
                      <li key={asset.mediaAssetId}>
                        <button
                          type="button"
                          disabled={isAssigned || busy}
                          aria-pressed={isSelected}
                          title={
                            isAssigned
                              ? "قبلاً به این مورد وصل شده"
                              : asset.originalFileName
                          }
                          className={
                            isSelected
                              ? "w-full rounded-xl border-2 border-blue-600 bg-blue-50/40 p-1.5 text-start disabled:opacity-50"
                              : "w-full rounded-xl border border-gray-200 bg-white p-1.5 text-start hover:border-blue-300 disabled:opacity-50"
                          }
                          onClick={() => toggleAsset(asset)}
                          data-testid={`admin-media-asset-${asset.mediaAssetId}`}
                        >
                          <div className="aspect-square overflow-hidden rounded-lg bg-slate-100">
                            {/* eslint-disable-next-line @next/next/no-img-element */}
                            <img
                              src={preview ?? undefined}
                              alt={asset.originalFileName}
                              className="h-full w-full object-contain p-2"
                            />
                          </div>
                          <p className="mt-1 truncate text-xs font-medium text-slate-800">
                            {asset.originalFileName || "بدون نام"}
                          </p>
                          <p className="truncate text-[11px] text-slate-500">
                            {isAssigned
                              ? "متصل"
                              : `${Math.max(1, Math.round(asset.byteSize / 1024))} کیلوبایت`}
                          </p>
                        </button>
                      </li>
                    );
                  })}
                </ul>
              )}

              <div className="flex flex-wrap items-center justify-between gap-2 text-xs text-slate-600">
                <span data-testid="admin-media-page-meta">
                  صفحه {page} از {totalPages} — {totalCount} مورد
                </span>
                <div className="flex gap-2">
                  <button
                    type="button"
                    disabled={busy || page <= 1}
                    className="min-h-9 rounded-lg border border-gray-200 px-3 disabled:opacity-40"
                    onClick={() => setPage((p) => Math.max(1, p - 1))}
                    data-testid="admin-media-page-prev"
                  >
                    قبلی
                  </button>
                  <button
                    type="button"
                    disabled={busy || page >= totalPages}
                    className="min-h-9 rounded-lg border border-gray-200 px-3 disabled:opacity-40"
                    onClick={() => setPage((p) => p + 1)}
                    data-testid="admin-media-page-next"
                  >
                    بعدی
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>

        <div className="flex flex-wrap items-center justify-between gap-2 border-t border-gray-100 px-5 py-4">
          <span className="text-sm text-slate-600" data-testid="admin-media-selection-count">
            {selectionMode === "single"
              ? selectedCount
                ? "۱ مورد انتخاب شده"
                : "هنوز موردی انتخاب نشده"
              : `${selectedCount} مورد انتخاب شده`}
          </span>
          <div className="flex flex-wrap gap-2">
            <button
              type="button"
              className="min-h-11 rounded-xl border border-gray-200 bg-white px-4 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-50"
              onClick={onClose}
              disabled={busy}
            >
              انصراف
            </button>
            <button
              type="button"
              className="min-h-11 rounded-xl bg-[#2563EB] px-4 text-sm font-semibold text-white hover:brightness-95 disabled:opacity-50"
              disabled={busy || selectedCount === 0}
              onClick={() => void handleConfirm()}
              data-testid="admin-media-confirm"
            >
              {busy ? "در حال اعمال…" : "تأیید انتخاب"}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
