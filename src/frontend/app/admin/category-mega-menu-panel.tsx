"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { toast } from "react-toastify";
import {
  loadCategoryMegaMenuConfiguration,
  loadMegaMenuPlacementOptions,
  presentationLevelLabel,
  removeCategoryMegaMenuBinding,
  upsertCategoryMegaMenu,
  type CategoryMegaMenuConfiguration,
  type MegaMenuPlacementOption,
  type UpsertCategoryMegaMenuInput,
} from "./catalog-mega-menu-api.ts";

function defaultDraft(config: CategoryMegaMenuConfiguration | null): UpsertCategoryMegaMenuInput {
  return {
    parentMegaMenuItemId: config?.parentMegaMenuItemId ?? null,
    sortOrder: config?.sortOrder ?? 0,
    isVisible: config?.isVisible ?? true,
    isFeatured: config?.isFeatured ?? false,
    imageMediaAssetId: config?.imageMediaAssetId ?? null,
    iconMediaAssetId: config?.iconMediaAssetId ?? null,
    titleOverride: config?.titleOverride ?? null,
    badgeText: config?.badgeText ?? null,
    shortLabel: config?.shortLabel ?? null,
  };
}

/**
 * تب مگامنو — presentation رده در ناوبری ویترین (CategoryId داخلی؛ بدون URL دستی).
 */
export function CategoryMegaMenuPanel({
  categoryId,
  isEdit,
  canEdit,
  busy: externalBusy,
  onEnterEdit,
  onCancelEdit,
}: {
  categoryId: string;
  isEdit: boolean;
  canEdit: boolean;
  busy?: boolean;
  onEnterEdit: () => void;
  onCancelEdit: () => void;
}) {
  const [config, setConfig] = useState<CategoryMegaMenuConfiguration | null>(null);
  const [placements, setPlacements] = useState<MegaMenuPlacementOption[]>([]);
  const [draft, setDraft] = useState<UpsertCategoryMegaMenuInput | null>(null);
  const [showTitleOverride, setShowTitleOverride] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const isBusy = busy || Boolean(externalBusy);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    const [configRes, placementRes] = await Promise.all([
      loadCategoryMegaMenuConfiguration(categoryId),
      loadMegaMenuPlacementOptions(categoryId),
    ]);
    setLoading(false);
    if (configRes.state !== "ok" || !configRes.data) {
      setError(configRes.message ?? "خطا در بارگذاری مگامنو");
      return;
    }
    setConfig(configRes.data);
    setDraft(defaultDraft(configRes.data));
    setShowTitleOverride(Boolean(configRes.data.titleOverride));
    if (placementRes.state === "ok" && placementRes.data) {
      setPlacements(placementRes.data);
    } else {
      setPlacements([]);
    }
  }, [categoryId]);

  useEffect(() => {
    void reload();
  }, [reload]);

  const selectedPlacement = useMemo(
    () => placements.find((p) => p.megaMenuItemId === draft?.parentMegaMenuItemId) ?? null,
    [placements, draft?.parentMegaMenuItemId],
  );

  const handleSave = async () => {
    if (!draft) return;
    setBusy(true);
    const result = await upsertCategoryMegaMenu(categoryId, {
      ...draft,
      titleOverride: showTitleOverride ? draft.titleOverride : null,
    });
    setBusy(false);
    if (result.state !== "ok") {
      toast.error(result.message ?? "ذخیره ناموفق");
      return;
    }
    toast.success("تنظیمات مگامنو ذخیره شد");
    onCancelEdit();
    await reload();
  };

  const handleEnable = async () => {
    if (!draft) return;
    setBusy(true);
    const result = await upsertCategoryMegaMenu(categoryId, { ...draft, isVisible: true });
    setBusy(false);
    if (result.state !== "ok") {
      toast.error(result.message ?? "فعال‌سازی ناموفق");
      return;
    }
    toast.success("دسته در مگامنو فعال شد");
    await reload();
  };

  const handleDisable = async () => {
    if (!window.confirm("این دسته از مگامنو حذف شود؟ (خود دسته‌بندی حذف نمی‌شود)")) return;
    setBusy(true);
    const result = await removeCategoryMegaMenuBinding(categoryId);
    setBusy(false);
    if (result.state !== "ok") {
      toast.error(result.message ?? "غیرفعال‌سازی ناموفق");
      return;
    }
    toast.success("اتصال مگامنو حذف شد");
    onCancelEdit();
    await reload();
  };

  return (
    <div className="space-y-6" data-testid="category-mega-menu-panel">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 className="text-lg font-semibold text-slate-900">نمایش در مگامنو</h2>
          <p className="mt-1 text-sm text-slate-500">
            مشخص می‌کند این دسته در منوی بالای فروشگاه چگونه و کجا دیده شود. مسیر ویترین از خود دسته‌بندی گرفته می‌شود.
          </p>
        </div>
        {canEdit && !isEdit ? (
          <button
            type="button"
            className="inline-flex min-h-10 items-center justify-center rounded-xl bg-slate-900 px-4 text-sm font-medium text-white hover:bg-slate-800"
            onClick={onEnterEdit}
            data-testid="mega-menu-enter-edit"
          >
            ویرایش
          </button>
        ) : null}
        {isEdit && canEdit ? (
          <div className="flex flex-wrap gap-2">
            <button
              type="button"
              className="inline-flex min-h-10 items-center rounded-xl bg-[#2563EB] px-4 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-40"
              disabled={isBusy}
              onClick={() => void handleSave()}
              data-testid="mega-menu-save"
            >
              ذخیره
            </button>
            <button
              type="button"
              className="inline-flex min-h-10 items-center rounded-xl border border-gray-200 px-4 text-sm font-medium text-slate-700 hover:bg-slate-50"
              onClick={onCancelEdit}
              data-testid="mega-menu-cancel-edit"
            >
              انصراف
            </button>
          </div>
        ) : null}
      </div>

      {loading ? <p className="text-sm text-slate-500">در حال بارگذاری…</p> : null}
      {error ? <p className="text-sm text-red-600">{error}</p> : null}

      {!loading && !error && config ? (
        <div className="space-y-4 rounded-2xl border border-gray-100 bg-white p-4 lg:p-5">
          <div className="grid gap-3 sm:grid-cols-2">
            <div>
              <div className="text-xs text-slate-500">وضعیت نمایش در مگامنو</div>
              <div className="mt-0.5 font-medium text-slate-900" data-testid="mega-menu-bound-status">
                {config.isBound && config.isVisible ? "فعال" : config.isBound ? "مخفی" : "غیرفعال"}
              </div>
            </div>
            <div>
              <div className="text-xs text-slate-500">عنوان نمایشی</div>
              <div className="mt-0.5 font-medium text-slate-900">{config.displayTitle}</div>
            </div>
            <div>
              <div className="text-xs text-slate-500">محل/سطح نمایش</div>
              <div className="mt-0.5 font-medium text-slate-900">
                {config.isBound
                  ? config.parentMenuPath
                    ? `${config.parentMenuPath} (${presentationLevelLabel(config.presentationLevel)})`
                    : presentationLevelLabel(config.presentationLevel)
                  : "—"}
              </div>
            </div>
            <div>
              <div className="text-xs text-slate-500">مسیر ویترین</div>
              <div className="mt-0.5 font-medium text-slate-900" dir="ltr" data-testid="mega-menu-destination-preview">
                {config.destinationPreview || "—"}
              </div>
            </div>
          </div>

          {!config.categoryPublished || !config.categoryVisible ? (
            <p className="rounded-xl bg-amber-50 px-3 py-2 text-sm text-amber-900">
              این دسته برای ویترین منتشرشده/قابل‌مشاهده نیست؛ در مگامنو نمایش داده نمی‌شود.
            </p>
          ) : null}

          {isEdit && canEdit && draft ? (
            <div className="space-y-4 border-t border-gray-100 pt-4">
              <div className="flex flex-wrap gap-2">
                {!config.isBound ? (
                  <button
                    type="button"
                    className="inline-flex min-h-10 items-center rounded-xl border border-gray-200 px-4 text-sm font-medium hover:bg-slate-50 disabled:opacity-40"
                    disabled={isBusy}
                    onClick={() => void handleEnable()}
                    data-testid="mega-menu-enable"
                  >
                    فعال‌سازی در مگامنو
                  </button>
                ) : (
                  <button
                    type="button"
                    className="inline-flex min-h-10 items-center rounded-xl border border-red-200 px-4 text-sm font-medium text-red-700 hover:bg-red-50 disabled:opacity-40"
                    disabled={isBusy}
                    onClick={() => void handleDisable()}
                    data-testid="mega-menu-disable"
                  >
                    حذف از مگامنو
                  </button>
                )}
              </div>

              <label className="block text-sm">
                <span className="mb-1 block text-xs text-slate-600">این دسته در کدام بخش مگامنو نمایش داده شود؟</span>
                <select
                  className="w-full rounded-lg border border-gray-200 px-3 py-2"
                  value={draft.parentMegaMenuItemId ?? ""}
                  onChange={(e) =>
                    setDraft({
                      ...draft,
                      parentMegaMenuItemId: e.target.value ? e.target.value : null,
                    })
                  }
                  data-testid="mega-menu-placement"
                >
                  <option value="">ریشهٔ منو (سطح اول)</option>
                  {placements.map((opt) => (
                    <option key={opt.megaMenuItemId} value={opt.megaMenuItemId}>
                      {opt.menuPath}
                    </option>
                  ))}
                </select>
                {selectedPlacement ? (
                  <span className="mt-1 block text-xs text-slate-500">والد انتخاب‌شده: {selectedPlacement.menuPath}</span>
                ) : null}
              </label>

              <div className="grid gap-3 sm:grid-cols-2">
                <label className="block text-sm">
                  <span className="mb-1 block text-xs text-slate-600">ترتیب</span>
                  <input
                    type="number"
                    className="w-full rounded-lg border border-gray-200 px-3 py-2"
                    value={draft.sortOrder}
                    onChange={(e) => setDraft({ ...draft, sortOrder: Number(e.target.value) || 0 })}
                    data-testid="mega-menu-sort-order"
                  />
                </label>
              </div>

              <label className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={draft.isVisible}
                  onChange={(e) => setDraft({ ...draft, isVisible: e.target.checked })}
                  data-testid="mega-menu-visible"
                />
                نمایش در مگامنو
              </label>
              <label className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={draft.isFeatured}
                  onChange={(e) => setDraft({ ...draft, isFeatured: e.target.checked })}
                  data-testid="mega-menu-featured"
                />
                برجسته
              </label>

              {!showTitleOverride ? (
                <button
                  type="button"
                  className="text-sm font-medium text-[#2563EB] hover:underline"
                  onClick={() => setShowTitleOverride(true)}
                  data-testid="mega-menu-title-override-toggle"
                >
                  عنوان متفاوت در مگامنو
                </button>
              ) : (
                <label className="block text-sm">
                  <span className="mb-1 block text-xs text-slate-600">عنوان متفاوت در مگامنو</span>
                  <input
                    className="w-full rounded-lg border border-gray-200 px-3 py-2"
                    value={draft.titleOverride ?? ""}
                    onChange={(e) => setDraft({ ...draft, titleOverride: e.target.value || null })}
                    data-testid="mega-menu-title-override"
                  />
                </label>
              )}
            </div>
          ) : null}
        </div>
      ) : null}
    </div>
  );
}
