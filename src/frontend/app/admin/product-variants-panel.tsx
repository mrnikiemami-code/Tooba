"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import {
  applyProductVariantMatrix,
  getProductVariantEditorState,
  previewProductVariantCombinations,
  type ProductVariantCombinationPreview,
  type ProductVariantEditorState,
  type ProductVariantPreviewResult,
} from "./catalog-attribute-api.ts";
import {
  axisDraftFromState,
  estimateCombinationCount,
  formatCombinationLabel,
  formatPreviewAction,
  formatVariantStatus,
  isAxisDraftDirty,
  isRowDraftDirty,
  rowDraftFromVariants,
  selectedAxesFromDraft,
  type VariantAxisDraft,
  type VariantRowDraft,
} from "./product-variants-panel-model.ts";

export type ProductVariantsPanelMode = "view" | "edit";

function badgeClass(tone: "amber" | "violet" | "slate" | "emerald" | "rose"): string {
  switch (tone) {
    case "amber":
      return "rounded-full bg-amber-50 px-2 py-0.5 text-[11px] font-medium text-amber-900";
    case "violet":
      return "rounded-full bg-violet-50 px-2 py-0.5 text-[11px] font-medium text-violet-900";
    case "emerald":
      return "rounded-full bg-emerald-50 px-2 py-0.5 text-[11px] font-medium text-emerald-900";
    case "rose":
      return "rounded-full bg-rose-50 px-2 py-0.5 text-[11px] font-medium text-rose-800";
    default:
      return "rounded-full bg-slate-100 px-2 py-0.5 text-[11px] font-medium text-slate-700";
  }
}

/**
 * پنل ماتریس تنوع‌های محصول Workspace — بدون قیمت/موجودی و بدون AgGrid.
 */
export function ProductVariantsPanel({
  productId,
  canEdit,
  mode,
}: {
  productId: string;
  canEdit: boolean;
  mode: ProductVariantsPanelMode;
}) {
  const [state, setState] = useState<ProductVariantEditorState | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [axisDraft, setAxisDraft] = useState<VariantAxisDraft>({});
  const [rowDraft, setRowDraft] = useState<Record<string, VariantRowDraft>>({});
  const [preview, setPreview] = useState<ProductVariantPreviewResult | null>(null);
  const [dirty, setDirty] = useState(false);

  const editable = canEdit && mode === "edit";

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    const result = await getProductVariantEditorState(productId, "fa-IR");
    setLoading(false);
    if (result.state !== "ok" || !result.data) {
      setError(result.message ?? "بارگذاری تنوع‌ها ناموفق بود");
      setState(null);
      return;
    }
    setState(result.data);
    setAxisDraft(axisDraftFromState(result.data.axes));
    setRowDraft(rowDraftFromVariants(result.data.variants));
    setPreview(null);
    setDirty(false);
  }, [productId]);

  useEffect(() => {
    void reload();
  }, [reload]);

  const axes = useMemo(() => state?.axes ?? [], [state?.axes]);
  const variants = useMemo(() => state?.variants ?? [], [state?.variants]);
  const estimate = estimateCombinationCount(axisDraft);
  const maxCombinations = state?.maxCombinations ?? 200;

  function toggleOption(definitionId: string, optionId: string) {
    setAxisDraft((prev) => {
      const current = new Set(prev[definitionId] ?? []);
      if (current.has(optionId)) current.delete(optionId);
      else current.add(optionId);
      return { ...prev, [definitionId]: [...current] };
    });
    setDirty(true);
    setPreview(null);
  }

  function updateRow(variantId: string, patch: Partial<VariantRowDraft>) {
    setRowDraft((prev) => ({
      ...prev,
      [variantId]: {
        status: prev[variantId]?.status ?? "Draft",
        catalogCodeSeam: prev[variantId]?.catalogCodeSeam ?? "",
        isDefault: prev[variantId]?.isDefault ?? false,
        ...patch,
      },
    }));
    setDirty(true);
  }

  function setDefaultVariant(variantId: string) {
    setRowDraft((prev) => {
      const next: Record<string, VariantRowDraft> = {};
      for (const [id, row] of Object.entries(prev)) {
        next[id] = { ...row, isDefault: id === variantId };
      }
      return next;
    });
    setDirty(true);
  }

  function onCancel() {
    if (!state) return;
    if (dirty && !window.confirm("تغییرات ذخیره‌نشده نادیده گرفته شوند؟")) return;
    setAxisDraft(axisDraftFromState(state.axes));
    setRowDraft(rowDraftFromVariants(state.variants));
    setPreview(null);
    setDirty(false);
    setError(null);
  }

  async function onPreview() {
    if (!editable) return;
    setBusy(true);
    setError(null);
    const result = await previewProductVariantCombinations(
      productId,
      selectedAxesFromDraft(axes, axisDraft),
      "fa-IR",
    );
    setBusy(false);
    if (result.state !== "ok" || !result.data) {
      setError(result.message ?? "پیش‌نمایش ترکیب‌ها ناموفق بود");
      return;
    }
    setPreview(result.data);
  }

  async function onSave() {
    if (!editable || !state) return;
    setBusy(true);
    setError(null);
    const selectedAxes = selectedAxesFromDraft(axes, axisDraft);
    const defaultVariantId =
      Object.entries(rowDraft).find(([, row]) => row.isDefault)?.[0] ?? null;
    const variantPatches = variants.map((variant) => {
      const row = rowDraft[variant.variantId];
      return {
        variantId: variant.variantId,
        status: row?.status ?? variant.status,
        catalogCodeSeam: row?.catalogCodeSeam ?? variant.catalogCodeSeam,
        sortOrder: variant.sortOrder,
        isDefault: row?.isDefault ?? variant.isDefault,
      };
    });
    const result = await applyProductVariantMatrix(productId, {
      locale: "fa-IR",
      selectedAxes,
      defaultVariantId,
      variantPatches,
    });
    setBusy(false);
    if (result.state !== "ok") {
      setError(result.message ?? "ذخیره تنوع‌ها ناموفق بود");
      return;
    }
    await reload();
  }

  if (loading) {
    return <p className="text-sm text-muted">در حال بارگذاری تنوع‌ها…</p>;
  }

  if (!state) {
    return <p className="text-sm text-rose-700">{error ?? "تنوع‌ها در دسترس نیست."}</p>;
  }

  if (axes.length === 0) {
    return (
      <div data-testid="product-variants-empty-axes" className="space-y-2">
        <p className="font-semibold">تنوع‌ها</p>
        <p className="text-sm text-muted">
          {state.messageFa ?? "برای این دسته‌بندی ویژگی تنوع تعریف نشده است."}
        </p>
      </div>
    );
  }

  const axisDirty = isAxisDraftDirty(axes, axisDraft);
  const rowDirty = isRowDraftDirty(variants, rowDraft);
  const showDirty = dirty || axisDirty || rowDirty;

  return (
    <div className="space-y-4" data-testid="product-variants-panel" data-mode={mode}>
      {error ? (
        <p className="rounded-ds border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-800" role="alert">
          {error}
        </p>
      ) : null}

      <div className="rounded-ds border border-border p-3">
        <p className="text-sm font-medium">آمادگی تنوع‌ها</p>
        <p className="mt-1 text-sm text-muted">
          {state.readiness.isValid ? "آماده" : "نیاز به بررسی"}
          {state.readiness.noDefaultVariant ? " · تنوع پیش‌فرض مشخص نشده" : null}
        </p>
        {state.categoryPath ? <p className="mt-1 text-xs text-muted">{state.categoryPath}</p> : null}
      </div>

      {editable ? (
        <section className="space-y-3" aria-label="انتخاب محورهای تنوع">
          <div>
            <p className="font-semibold">مقادیر محورها</p>
            <p className="mt-1 text-sm text-muted">مقادیر قابل‌استفاده برای ساخت ترکیب‌های این محصول را انتخاب کنید.</p>
          </div>
          <ul className="space-y-3">
            {axes.map((axis) => (
              <li key={axis.definitionId} className="rounded-ds border border-border p-3">
                <p className="text-sm font-medium">{axis.localizedName}</p>
                {axis.valueKind !== "Enumeration" ? (
                  <p className="mt-2 text-sm text-rose-700">این محور گزینه‌دار نیست و در ماتریس پشتیبانی نمی‌شود.</p>
                ) : (
                  <div className="mt-2 flex flex-wrap gap-2" role="group" aria-label={axis.localizedName}>
                    {axis.options
                      .filter((o) => o.isActive)
                      .map((option) => {
                        const selected = (axisDraft[axis.definitionId] ?? []).includes(option.optionId);
                        return (
                          <label
                            key={option.optionId}
                            className={`inline-flex min-h-10 cursor-pointer items-center gap-2 rounded-ds border px-3 text-sm ${
                              selected ? "border-primary bg-primary/5" : "border-border bg-surface"
                            }`}
                          >
                            <input
                              type="checkbox"
                              className="accent-primary"
                              checked={selected}
                              onChange={() => toggleOption(axis.definitionId, option.optionId)}
                            />
                            <span>{option.localizedLabel}</span>
                          </label>
                        );
                      })}
                  </div>
                )}
              </li>
            ))}
          </ul>

          <div className="flex flex-wrap items-center gap-2">
            <button
              type="button"
              disabled={busy}
              className="min-h-11 rounded-ds border border-border px-4 text-sm hover:bg-secondary disabled:opacity-50"
              onClick={() => void onPreview()}
            >
              پیش‌نمایش ترکیب‌ها
            </button>
            <span className="text-sm text-muted">
              حدود {estimate.toLocaleString("fa-IR")} ترکیب · سقف {maxCombinations.toLocaleString("fa-IR")}
            </span>
          </div>
          {estimate > maxCombinations ? (
            <p className="text-sm text-amber-800" role="status">
              تعداد ترکیب‌ها زیاد است. قبل از ذخیره گزینه‌های کمتری انتخاب کنید.
            </p>
          ) : null}
        </section>
      ) : null}

      {preview ? <PreviewBlock preview={preview} /> : null}

      <section className="space-y-3" aria-label="فهرست تنوع‌ها">
        <div>
          <p className="font-semibold">تنوع‌های فعلی</p>
          <p className="mt-1 text-sm text-muted">بدون قیمت یا موجودی روی تنوع · پیشنهاد فروشنده جدا است</p>
        </div>
        {variants.length === 0 ? (
          <p className="text-sm text-muted">هنوز تنوعی ثبت نشده است.</p>
        ) : (
          <ul className="grid gap-3 sm:grid-cols-2">
            {variants.map((variant) => {
              const row = rowDraft[variant.variantId];
              return (
                <li key={variant.variantId} className="rounded-ds border border-border p-3">
                  <div className="flex flex-wrap items-start justify-between gap-2">
                    <p className="font-medium">{formatCombinationLabel(variant.axisLabels)}</p>
                    {row?.isDefault || variant.isDefault ? (
                      <span className={badgeClass("emerald")}>تنوع پیش‌فرض</span>
                    ) : null}
                  </div>
                  <p className="mt-1 text-sm text-muted" dir="ltr">
                    {row?.catalogCodeSeam || variant.catalogCodeSeam || "بدون کد کاتالوگ"}
                  </p>
                  <div className="mt-2 flex flex-wrap items-center gap-2">
                    <span className={badgeClass("slate")}>
                      {formatVariantStatus(row?.status ?? variant.status)}
                    </span>
                    {variant.offerCount != null ? (
                      <span className="text-sm text-muted">{variant.offerCount.toLocaleString("fa-IR")} پیشنهاد</span>
                    ) : null}
                  </div>
                  {editable ? (
                    <div className="mt-3 space-y-2">
                      <label className="block text-sm">
                        وضعیت
                        <select
                          className="mt-1 min-h-10 w-full rounded-ds border border-border bg-surface px-2"
                          value={row?.status ?? variant.status}
                          onChange={(event) => updateRow(variant.variantId, { status: event.target.value })}
                          aria-label={`وضعیت ${formatCombinationLabel(variant.axisLabels)}`}
                        >
                          <option value="Draft">غیرفعال</option>
                          <option value="Published">فعال</option>
                          <option value="Archived">بایگانی‌شده</option>
                        </select>
                      </label>
                      <label className="block text-sm">
                        کد کاتالوگ (اختیاری)
                        <input
                          className="mt-1 min-h-10 w-full rounded-ds border border-border bg-surface px-3"
                          dir="ltr"
                          value={row?.catalogCodeSeam ?? ""}
                          onChange={(event) =>
                            updateRow(variant.variantId, { catalogCodeSeam: event.target.value })
                          }
                          aria-label={`کد کاتالوگ ${formatCombinationLabel(variant.axisLabels)}`}
                        />
                      </label>
                      <button
                        type="button"
                        className="min-h-10 rounded-ds border border-border px-3 text-sm hover:bg-secondary"
                        onClick={() => setDefaultVariant(variant.variantId)}
                        aria-label={`تنظیم ${formatCombinationLabel(variant.axisLabels)} به‌عنوان تنوع پیش‌فرض`}
                      >
                        تنظیم به‌عنوان تنوع پیش‌فرض
                      </button>
                    </div>
                  ) : null}
                </li>
              );
            })}
          </ul>
        )}
      </section>

      {editable ? (
        <div className="flex flex-wrap gap-2 border-t border-border pt-3">
          <button
            type="button"
            disabled={busy || !showDirty}
            className="min-h-11 rounded-ds bg-primary px-4 text-sm font-medium text-primary-foreground disabled:opacity-50"
            onClick={() => void onSave()}
          >
            ذخیره تنوع‌ها
          </button>
          <button
            type="button"
            disabled={busy || !showDirty}
            className="min-h-11 rounded-ds border border-border px-4 text-sm hover:bg-secondary disabled:opacity-50"
            onClick={onCancel}
          >
            انصراف
          </button>
          {showDirty ? <span className="self-center text-sm text-amber-800">تغییرات ذخیره‌نشده</span> : null}
        </div>
      ) : null}
    </div>
  );
}

function PreviewBlock({ preview }: { preview: ProductVariantPreviewResult }) {
  return (
    <section className="space-y-3 rounded-ds border border-border p-3" aria-label="پیش‌نمایش ترکیب‌ها">
      <div>
        <p className="font-semibold">پیش‌نمایش ترکیب‌ها</p>
        <p className="mt-1 text-sm text-muted">{preview.messageFa}</p>
      </div>
      {preview.capped || preview.warningFa ? (
        <p className="text-sm text-amber-800" role="status">
          {preview.warningFa ?? "تعداد ترکیب‌ها از سقف امن بیشتر است."}
        </p>
      ) : null}
      <ul className="max-h-72 space-y-2 overflow-y-auto">
        {preview.combinations.map((combo: ProductVariantCombinationPreview) => (
          <li key={`${combo.action}-${combo.desiredFingerprint}`} className="rounded-ds border border-border/70 px-3 py-2 text-sm">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <span>{formatCombinationLabel(combo.axisLabels)}</span>
              <span className={badgeClass(combo.action === "New" ? "emerald" : combo.action === "Deactivate" ? "rose" : "slate")}>
                {formatPreviewAction(combo.action)}
              </span>
            </div>
            {combo.referencedByOffers ? (
              <p className="mt-1 text-xs text-muted">دارای پیشنهاد فروشنده — حذف سخت نمی‌شود</p>
            ) : null}
          </li>
        ))}
      </ul>
    </section>
  );
}
