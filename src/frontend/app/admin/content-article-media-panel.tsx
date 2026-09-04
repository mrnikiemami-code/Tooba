"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { toast } from "react-toastify";
import { MediaLibraryDialog } from "./media-library-dialog.tsx";
import { mediaPreviewUrl } from "./media-api.ts";
import { ContentHelpAffordance } from "./content-help-affordance.tsx";
import {
  addArticleGalleryItems,
  assignArticleFeaturedImage,
  fetchArticleMediaWorkspace,
  mapArticleMediaMutationError,
  patchArticleGalleryItem,
  removeArticleGalleryItem,
  reorderArticleGallery,
  type ArticleGalleryItemDto,
  type ArticleMediaWorkspaceDto,
} from "./content-article-media-api.ts";

type PickerTarget = "featured" | "gallery" | null;

function sortGallery(rows: ArticleGalleryItemDto[]): ArticleGalleryItemDto[] {
  return [...rows].sort((a, b) => a.displayOrder - b.displayOrder || a.mediaAssetId.localeCompare(b.mediaAssetId));
}

/** پنل رسانهٔ workspace مقاله — شاخص، گالری، کتابخانهٔ مشترک؛ بدون آپلود موازی. */
export function ContentArticleMediaPanel({
  articleId,
  editable,
  onWorkspaceChange,
}: {
  articleId: string;
  editable: boolean;
  /** همگام‌سازی workspace با والد تا Save عمومی cover را خراب نکند. */
  onWorkspaceChange?: (workspace: ArticleMediaWorkspaceDto) => void;
}) {
  const [workspace, setWorkspace] = useState<ArticleMediaWorkspaceDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [pickerOpen, setPickerOpen] = useState(false);
  const [pickerTarget, setPickerTarget] = useState<PickerTarget>(null);
  const [altDrafts, setAltDrafts] = useState<Record<string, string>>({});
  const [captionDrafts, setCaptionDrafts] = useState<Record<string, string>>({});

  const gallery = useMemo(() => sortGallery(workspace?.gallery ?? []), [workspace?.gallery]);
  const assignedIds = useMemo(() => new Set(gallery.map((row) => row.mediaAssetId)), [gallery]);

  const applyWorkspace = useCallback(
    (data: ArticleMediaWorkspaceDto) => {
      setWorkspace(data);
      const nextAlt: Record<string, string> = {};
      const nextCap: Record<string, string> = {};
      for (const row of data.gallery) {
        nextAlt[row.mediaAssetId] = row.altText ?? "";
        nextCap[row.mediaAssetId] = row.caption ?? "";
      }
      setAltDrafts(nextAlt);
      setCaptionDrafts(nextCap);
      onWorkspaceChange?.(data);
    },
    [onWorkspaceChange],
  );

  const reload = useCallback(async () => {
    setLoading(true);
    const result = await fetchArticleMediaWorkspace(articleId);
    setLoading(false);
    if (result.state !== "ok" || !result.data) {
      setWorkspace(null);
      return;
    }
    applyWorkspace(result.data);
  }, [applyWorkspace, articleId]);

  // Reload only when articleId changes. Depending on `reload` identity causes an
  // infinite load loop when parent passes a fresh onWorkspaceChange each render.
  useEffect(() => {
    void reload();
    // eslint-disable-next-line react-hooks/exhaustive-deps -- intentional articleId-only
  }, [articleId]);

  const openPicker = (target: PickerTarget) => {
    if (!editable) return;
    setPickerTarget(target);
    setPickerOpen(true);
  };

  const moveGallery = async (mediaAssetId: string, direction: -1 | 1) => {
    const index = gallery.findIndex((row) => row.mediaAssetId === mediaAssetId);
    if (index < 0) return;
    const target = index + direction;
    if (target < 0 || target >= gallery.length) return;
    const ordered = gallery.map((row) => row.mediaAssetId);
    const [removed] = ordered.splice(index, 1);
    ordered.splice(target, 0, removed!);
    setBusy(true);
    const result = await reorderArticleGallery(articleId, ordered);
    setBusy(false);
    if (result.state !== "ok" || !result.data) {
      toast.error(mapArticleMediaMutationError(result));
      return;
    }
    applyWorkspace(result.data);
  };

  const saveGalleryMeta = async (mediaAssetId: string) => {
    setBusy(true);
    const result = await patchArticleGalleryItem(articleId, mediaAssetId, {
      altText: altDrafts[mediaAssetId] || null,
      caption: captionDrafts[mediaAssetId] || null,
    });
    setBusy(false);
    if (result.state !== "ok" || !result.data) {
      toast.error(mapArticleMediaMutationError(result));
      return;
    }
    toast.success("متادیتای گالری ذخیره شد");
    applyWorkspace(result.data);
  };

  if (loading) {
    return <p className="text-sm text-muted">در حال بارگذاری رسانه…</p>;
  }

  return (
    <div className="space-y-6" data-testid="content-article-media-panel">
      <p className="text-xs text-muted">
        تصویر شاخص، گالری مقاله، تصویر داخل متن و تصویر اشتراک‌گذاری همگی از همان کتابخانهٔ رسانهٔ توبا می‌آیند.
        حذف از مقاله فایل اصلی را پاک نمی‌کند.
      </p>
      <section className="space-y-3">
        <h3 className="flex items-center gap-2 text-sm font-semibold">
          تصویر شاخص
          <ContentHelpAffordance helpKey="featuredImage" />
        </h3>
        {workspace?.featuredMediaAssetId ? (
          <img
            src={mediaPreviewUrl(workspace.featuredMediaAssetId) ?? ""}
            alt=""
            className="max-h-56 rounded-xl border object-cover"
          />
        ) : (
          <p className="text-sm text-muted">تصویر شاخص اختصاص داده نشده است.</p>
        )}
        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            className="rounded-xl border px-3 py-2 text-sm"
            disabled={!editable || busy}
            data-testid="content-article-featured-pick"
            onClick={() => openPicker("featured")}
          >
            انتخاب از کتابخانه
          </button>
          {workspace?.featuredMediaAssetId ? (
            <button
              type="button"
              className="rounded-xl border px-3 py-2 text-sm"
              disabled={!editable || busy}
              data-testid="content-article-featured-remove"
              onClick={() =>
                void assignArticleFeaturedImage(articleId, null).then((result) => {
                  if (result.state === "ok" && result.data) applyWorkspace(result.data);
                  else toast.error(mapArticleMediaMutationError(result));
                })
              }
            >
              حذف اختصاص
            </button>
          ) : null}
        </div>
        <p className="text-xs text-muted">حذف اختصاص، فایل را از کتابخانه رسانه پاک نمی‌کند.</p>
      </section>

      <section className="space-y-3">
        <div className="flex items-center justify-between gap-2">
          <h3 className="flex items-center gap-2 text-sm font-semibold">
            گالری مقاله
            <ContentHelpAffordance helpKey="galleryMedia" />
          </h3>
          <button
            type="button"
            className="rounded-xl border px-3 py-2 text-sm"
            disabled={!editable || busy}
            data-testid="content-article-gallery-pick"
            onClick={() => openPicker("gallery")}
          >
            کتابخانه رسانه
          </button>
        </div>
        {gallery.length === 0 ? (
          <p className="text-sm text-muted">گالری خالی است.</p>
        ) : (
          <ul className="space-y-3">
            {gallery.map((row, index) => (
              <li key={row.mediaAssetId} className="rounded-xl border p-3">
                <div className="flex flex-wrap items-start gap-3">
                  <img
                    src={mediaPreviewUrl(row.mediaAssetId) ?? ""}
                    alt=""
                    className="h-20 w-20 rounded-lg border object-cover"
                  />
                  <div className="min-w-[12rem] flex-1 space-y-2">
                    <label className="block text-xs">
                      <span className="text-muted">متن جایگزین (سطح مقاله)</span>
                      <input
                        className="mt-1 w-full rounded-lg border px-2 py-1 text-sm"
                        disabled={!editable}
                        value={altDrafts[row.mediaAssetId] ?? ""}
                        onChange={(e) => setAltDrafts((c) => ({ ...c, [row.mediaAssetId]: e.target.value }))}
                      />
                    </label>
                    <label className="block text-xs">
                      <span className="text-muted">توضیح تصویر</span>
                      <input
                        className="mt-1 w-full rounded-lg border px-2 py-1 text-sm"
                        disabled={!editable}
                        value={captionDrafts[row.mediaAssetId] ?? ""}
                        onChange={(e) => setCaptionDrafts((c) => ({ ...c, [row.mediaAssetId]: e.target.value }))}
                      />
                    </label>
                  </div>
                  <div className="flex flex-col gap-1">
                    <button
                      type="button"
                      className="rounded border px-2 py-1 text-xs"
                      disabled={!editable || busy || index === 0}
                      onClick={() => void moveGallery(row.mediaAssetId, -1)}
                    >
                      ↑
                    </button>
                    <button
                      type="button"
                      className="rounded border px-2 py-1 text-xs"
                      disabled={!editable || busy || index === gallery.length - 1}
                      onClick={() => void moveGallery(row.mediaAssetId, 1)}
                    >
                      ↓
                    </button>
                    <button
                      type="button"
                      className="rounded border px-2 py-1 text-xs text-danger"
                      disabled={!editable || busy}
                      onClick={() =>
                        void removeArticleGalleryItem(articleId, row.mediaAssetId).then((result) => {
                          if (result.state === "ok" && result.data) applyWorkspace(result.data);
                          else toast.error(mapArticleMediaMutationError(result));
                        })
                      }
                    >
                      حذف
                    </button>
                    {editable ? (
                      <button
                        type="button"
                        className="rounded border px-2 py-1 text-xs"
                        disabled={busy}
                        onClick={() => void saveGalleryMeta(row.mediaAssetId)}
                      >
                        ذخیره
                      </button>
                    ) : null}
                  </div>
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>

      <MediaLibraryDialog
        open={pickerOpen}
        selectionMode={pickerTarget === "gallery" ? "multi" : "single"}
        alreadyAssignedIds={pickerTarget === "gallery" ? assignedIds : undefined}
        onClose={() => {
          setPickerOpen(false);
          setPickerTarget(null);
        }}
        onConfirm={async (assets) => {
          const target = pickerTarget;
          setPickerOpen(false);
          setPickerTarget(null);
          if (!assets.length || !target) return;
          setBusy(true);
          if (target === "featured") {
            const result = await assignArticleFeaturedImage(articleId, assets[0]!.mediaAssetId);
            setBusy(false);
            if (result.state === "ok" && result.data) applyWorkspace(result.data);
            else toast.error(mapArticleMediaMutationError(result));
            return;
          }
          const result = await addArticleGalleryItems(
            articleId,
            assets.map((a) => a.mediaAssetId),
          );
          setBusy(false);
          if (result.state === "ok" && result.data) applyWorkspace(result.data);
          else toast.error(mapArticleMediaMutationError(result));
        }}
      />
    </div>
  );
}
