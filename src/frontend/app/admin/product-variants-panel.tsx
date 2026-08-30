"use client";

/**
 * پنل تنوع‌های محصول — جریان ۴مرحله‌ای انسان‌خوان بدون اصطلاحات فنی داخلی.
 */

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import {
  applyProductVariantMatrix,
  getProductVariantEditorState,
  previewProductVariantCombinations,
  type ProductVariantCombinationPreview,
  type ProductVariantEditorState,
  type ProductVariantPreviewResult,
} from "./catalog-attribute-api.ts";
import { mapAdminErrorMessage } from "./admin-error-map";
import { resolveAdminChromeLocale } from "./admin-chrome-messages";
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
import { useProductWorkspaceDirtyRegistration } from "./product-workspace-dirty-context";
import { toast } from "react-toastify";

export type ProductVariantsPanelMode = "view" | "edit";

type BuilderStep = 1 | 2 | 3 | 4;

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

function toUiError(raw: string | null | undefined, locale: "fa" | "en"): string {
  return mapAdminErrorMessage(raw, locale);
}

/**
 * پنل ماتریس تنوع‌های محصول Workspace — بدون قیمت/موجودی و بدون AgGrid.
 */
export function ProductVariantsPanel({
  productId,
  categoryId,
  canEdit,
  mode,
}: {
  productId: string;
  categoryId?: string | null;
  canEdit: boolean;
  mode: ProductVariantsPanelMode;
}) {
  const locale = resolveAdminChromeLocale();
  const [state, setState] = useState<ProductVariantEditorState | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [axisDraft, setAxisDraft] = useState<VariantAxisDraft>({});
  const [enabledAxisIds, setEnabledAxisIds] = useState<string[]>([]);
  const [rowDraft, setRowDraft] = useState<Record<string, VariantRowDraft>>({});
  const [preview, setPreview] = useState<ProductVariantPreviewResult | null>(null);
  const [dirty, setDirty] = useState(false);
  const [step, setStep] = useState<BuilderStep>(1);

  const editable = canEdit && mode === "edit";

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    const result = await getProductVariantEditorState(productId, "fa-IR");
    setLoading(false);
    if (result.state !== "ok" || !result.data) {
      setError(toUiError(result.message, locale));
      setState(null);
      return;
    }
    setState(result.data);
    const draft = axisDraftFromState(result.data.axes);
    setAxisDraft(draft);
    setEnabledAxisIds(
      result.data.axes
        .filter((axis) => (draft[axis.definitionId] ?? []).length > 0)
        .map((axis) => axis.definitionId),
    );
    setRowDraft(rowDraftFromVariants(result.data.variants));
    setPreview(null);
    setDirty(false);
    setStep(1);
  }, [locale, productId]);

  useEffect(() => {
    void reload();
  }, [reload]);

  const axes = useMemo(() => state?.axes ?? [], [state?.axes]);
  const variants = useMemo(() => state?.variants ?? [], [state?.variants]);
  const estimate = estimateCombinationCount(
    Object.fromEntries(enabledAxisIds.map((id) => [id, axisDraft[id] ?? []])),
  );
  const maxCombinations = state?.maxCombinations ?? 200;
  const axisDirty = isAxisDraftDirty(axes, axisDraft);
  const rowDirty = isRowDraftDirty(variants, rowDraft);
  const showDirty = dirty || axisDirty || rowDirty;

  const discardDrafts = useCallback(() => {
    if (!state) return;
    const draft = axisDraftFromState(state.axes);
    setAxisDraft(draft);
    setEnabledAxisIds(
      state.axes
        .filter((axis) => (draft[axis.definitionId] ?? []).length > 0)
        .map((axis) => axis.definitionId),
    );
    setRowDraft(rowDraftFromVariants(state.variants));
    setPreview(null);
    setDirty(false);
    setError(null);
    setStep(1);
  }, [state]);

  useProductWorkspaceDirtyRegistration("variants", showDirty && editable, discardDrafts);

  function toggleAxisEnabled(definitionId: string) {
    setEnabledAxisIds((prev) => {
      if (prev.includes(definitionId)) {
        setAxisDraft((draft) => ({ ...draft, [definitionId]: [] }));
        return prev.filter((id) => id !== definitionId);
      }
      return [...prev, definitionId];
    });
    setDirty(true);
    setPreview(null);
  }

  function toggleOption(definitionId: string, optionId: string) {
    setAxisDraft((prev) => {
      const current = new Set(prev[definitionId] ?? []);
      if (current.has(optionId)) current.delete(optionId);
      else current.add(optionId);
      return { ...prev, [definitionId]: [...current] };
    });
    setEnabledAxisIds((prev) => (prev.includes(definitionId) ? prev : [...prev, definitionId]));
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
    discardDrafts();
  }

  function activeSelectedAxes() {
    const filteredDraft: VariantAxisDraft = {};
    for (const id of enabledAxisIds) {
      filteredDraft[id] = axisDraft[id] ?? [];
    }
    return selectedAxesFromDraft(
      axes.filter((axis) => enabledAxisIds.includes(axis.definitionId)),
      filteredDraft,
    );
  }

  async function onPreview() {
    if (!editable) return;
    setBusy(true);
    setError(null);
    const result = await previewProductVariantCombinations(productId, activeSelectedAxes(), "fa-IR");
    setBusy(false);
    if (result.state !== "ok" || !result.data) {
      setError(toUiError(result.message, locale));
      return;
    }
    setPreview(result.data);
    setStep(3);
  }

  async function onSave() {
    if (!editable || !state || busy) return;
    if (estimate > maxCombinations) {
      setError(
        locale === "en"
          ? `Too many combinations (cap ${maxCombinations}). Select fewer values.`
          : `تعداد تنوع‌ها از سقف ${maxCombinations.toLocaleString("fa-IR")} بیشتر است. مقادیر کمتری انتخاب کنید.`,
      );
      setStep(2);
      return;
    }
    setBusy(true);
    setError(null);
    const selectedAxes = activeSelectedAxes();
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
      setError(toUiError(result.message, locale));
      return;
    }
    await reload();
    setStep(4);
    toast.success(locale === "en" ? "Variants updated." : "تنوع‌ها به‌روزرسانی شدند.");
  }

  if (loading) {
    return <p className="text-sm text-muted">در حال بارگذاری تنوع‌ها…</p>;
  }

  if (!state) {
    return <p className="text-sm text-rose-700">{error ?? "تنوع‌ها در دسترس نیست."}</p>;
  }

  if (axes.length === 0) {
    return (
      <div data-testid="product-variants-empty-axes" className="space-y-3">
        <p className="font-semibold">تنوع‌ها</p>
        <p className="text-sm text-muted">
          {locale === "en"
            ? "No variant-enabled attributes are defined for this primary category yet."
            : "برای این دسته هنوز ویژگی قابل استفاده برای تنوع تعریف نشده است."}
        </p>
        <p className="text-sm text-muted">
          {locale === "en"
            ? "To create variants, first enable attributes such as color or size as «Variant» on the category."
            : "برای ساخت تنوع، ابتدا در دسته‌بندی ویژگی‌هایی مثل رنگ یا سایز را به‌عنوان «تنوع» فعال کنید."}
        </p>
        {categoryId && canEdit ? (
          <Link
            href={`/admin/catalog/categories/${categoryId}?tab=attributes`}
            className="inline-flex min-h-11 items-center rounded-ds border border-border px-4 text-sm hover:bg-secondary"
            data-testid="product-variants-goto-category-attributes"
          >
            {locale === "en" ? "Open category attributes" : "رفتن به ویژگی‌های دسته‌بندی"}
          </Link>
        ) : null}
      </div>
    );
  }

  const introFa =
    "اگر مشتری باید بین چند حالت از یک محصول انتخاب کند، مثل رنگ یا سایز، از تنوع‌ها استفاده کنید.";
  const introEn =
    "If shoppers must choose between options of the same product—like color or size—use Variants.";

  const enabledAxes = axes.filter((axis) => enabledAxisIds.includes(axis.definitionId));
  const hasDefault = Object.values(rowDraft).some((row) => row.isDefault) || variants.some((v) => v.isDefault);

  return (
    <div className="space-y-4" data-testid="product-variants-panel" data-mode={mode}>
      {error ? (
        <p className="rounded-ds border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-800" role="alert">
          {error}
        </p>
      ) : null}

      <div className="rounded-ds border border-border bg-slate-50/80 p-3" data-testid="product-variants-intro">
        <p className="text-sm text-slate-800">{locale === "en" ? introEn : introFa}</p>
        <div className="mt-3 rounded-ds border border-dashed border-slate-200 bg-white p-3 text-sm text-slate-700">
          <p>رنگ: مشکی، سفید</p>
          <p>سایز: M، L</p>
          <p className="mt-1 font-medium">نتیجه: مشکی / M ، مشکی / L ، سفید / M ، سفید / L</p>
        </div>
      </div>

      <div className="rounded-ds border border-border p-3">
        <p className="text-sm font-medium">آمادگی تنوع‌ها</p>
        <p className="mt-1 text-sm text-muted">
          {state.readiness.isValid ? (
            <span className={badgeClass("emerald")}>{locale === "en" ? "Ready" : "آماده"}</span>
          ) : (
            <span className={badgeClass("amber")}>{locale === "en" ? "Needs review" : "نیاز به بررسی"}</span>
          )}
          {state.readiness.noDefaultVariant || !hasDefault ? (
            <span className="ms-2 text-amber-800">
              {locale === "en" ? "Default variant required" : "نیاز به تعیین تنوع پیش‌فرض"}
            </span>
          ) : null}
        </p>
        {state.categoryPath ? (
          <p className="mt-1 text-xs text-muted">
            {locale === "en" ? "From primary category" : "از دسته اصلی محصول"}: {state.categoryPath}
          </p>
        ) : null}
      </div>

      {editable ? (
        <div className="space-y-4" data-testid="product-variants-builder">
          <nav className="flex flex-wrap gap-2" aria-label={locale === "en" ? "Variant steps" : "مراحل تنوع"}>
            {([1, 2, 3, 4] as BuilderStep[]).map((n) => (
              <button
                key={n}
                type="button"
                className={
                  step === n
                    ? "rounded-full bg-[#2563EB] px-3 py-1.5 text-xs font-semibold text-white"
                    : "rounded-full border border-gray-200 px-3 py-1.5 text-xs font-medium text-slate-700"
                }
                onClick={() => setStep(n)}
                data-testid={`product-variants-step-${n}`}
              >
                {n === 1
                  ? locale === "en"
                    ? "1. Variant attributes"
                    : "۱. ویژگی تنوع"
                  : n === 2
                    ? locale === "en"
                      ? "2. Values"
                      : "۲. مقادیر"
                    : n === 3
                      ? locale === "en"
                        ? "3. Preview"
                        : "۳. پیش‌نمایش"
                      : locale === "en"
                        ? "4. Generate"
                        : "۴. ساخت"}
              </button>
            ))}
          </nav>

          {step === 1 ? (
            <section className="space-y-3" aria-label={locale === "en" ? "Select variant attributes" : "انتخاب ویژگی‌های تنوع"}>
              <div>
                <p className="font-semibold">
                  {locale === "en"
                    ? "Which attributes does this product vary on?"
                    : "محصول در چه ویژگی‌هایی تنوع دارد؟"}
                </p>
                <p className="mt-1 text-sm text-muted">
                  {locale === "en"
                    ? "Only variant-enabled attributes from the primary category are listed."
                    : "فقط ویژگی‌های قابل استفاده برای تنوع از دسته اصلی نمایش داده می‌شوند."}
                </p>
              </div>
              <div className="flex flex-wrap gap-2">
                {axes.map((axis) => {
                  const on = enabledAxisIds.includes(axis.definitionId);
                  const unsupported = axis.valueKind !== "Enumeration";
                  return (
                    <button
                      key={axis.definitionId}
                      type="button"
                      disabled={unsupported}
                      aria-pressed={on}
                      className={`min-h-11 rounded-xl border px-4 text-sm ${
                        unsupported
                          ? "cursor-not-allowed border-rose-200 bg-rose-50 text-rose-800"
                          : on
                            ? "border-[#2563EB] bg-blue-50 font-semibold text-blue-900"
                            : "border-gray-200 bg-white text-slate-800"
                      }`}
                      onClick={() => toggleAxisEnabled(axis.definitionId)}
                      data-testid={`product-variants-attr-${axis.code}`}
                    >
                      {axis.localizedName}
                      {unsupported
                        ? locale === "en"
                          ? " (not supported for variants)"
                          : " (برای تنوع پشتیبانی نمی‌شود)"
                        : null}
                    </button>
                  );
                })}
              </div>
              <button
                type="button"
                className="min-h-11 rounded-ds bg-primary px-4 text-sm font-medium text-primary-foreground disabled:opacity-50"
                disabled={enabledAxisIds.length === 0}
                onClick={() => setStep(2)}
              >
                {locale === "en" ? "Next: values" : "ادامه: مقادیر قابل انتخاب"}
              </button>
            </section>
          ) : null}

          {step === 2 ? (
            <section className="space-y-3" aria-label={locale === "en" ? "Selectable values" : "مقادیر قابل انتخاب"}>
              <div>
                <p className="font-semibold">
                  {locale === "en" ? "Choose selectable values" : "مقادیر قابل انتخاب را مشخص کنید"}
                </p>
              </div>
              {enabledAxes.length === 0 ? (
                <p className="text-sm text-amber-800">
                  {locale === "en" ? "Select at least one variant attribute in step 1." : "ابتدا در مرحله ۱ حداقل یک ویژگی تنوع انتخاب کنید."}
                </p>
              ) : (
                <ul className="space-y-3">
                  {enabledAxes.map((axis) => (
                    <li key={axis.definitionId} className="rounded-ds border border-border p-3">
                      <p className="text-sm font-medium">{axis.localizedName}</p>
                      {axis.valueKind !== "Enumeration" ? (
                        <p className="mt-2 text-sm text-rose-700">
                          {locale === "en"
                            ? "This attribute type is not supported for variants."
                            : "این نوع ویژگی برای تنوع پشتیبانی نمی‌شود."}
                        </p>
                      ) : (
                        <div className="mt-2 flex flex-wrap gap-2" role="group" aria-label={axis.localizedName}>
                          {axis.options
                            .filter((o) => o.isActive)
                            .map((option) => {
                              const selected = (axisDraft[axis.definitionId] ?? []).includes(option.optionId);
                              return (
                                <label
                                  key={option.optionId}
                                  className={`inline-flex min-h-10 cursor-pointer items-center gap-2 rounded-ds border px-3 text-sm focus-within:ring-2 focus-within:ring-primary/40 ${
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
              )}
              <div className="flex flex-wrap items-center gap-2">
                <button
                  type="button"
                  disabled={busy || estimate === 0 || estimate > maxCombinations}
                  className="min-h-11 rounded-ds border border-border px-4 text-sm hover:bg-secondary disabled:opacity-50"
                  onClick={() => void onPreview()}
                  data-testid="product-variants-preview"
                >
                  {locale === "en" ? "Preview variants" : "پیش‌نمایش تنوع‌ها"}
                </button>
                <span className="text-sm text-muted">
                  {locale === "en"
                    ? `About ${estimate} variants · cap ${maxCombinations}`
                    : `حدود ${estimate.toLocaleString("fa-IR")} تنوع · سقف ${maxCombinations.toLocaleString("fa-IR")}`}
                </span>
              </div>
              {estimate > maxCombinations ? (
                <p className="text-sm text-amber-800" role="status" data-testid="product-variants-cap-warning">
                  {locale === "en"
                    ? `Too many combinations. Cap is ${maxCombinations}. Choose fewer values.`
                    : `تعداد تنوع‌ها زیاد است. سقف مجاز ${maxCombinations.toLocaleString("fa-IR")} است. مقادیر کمتری انتخاب کنید.`}
                </p>
              ) : null}
            </section>
          ) : null}

          {step === 3 ? (
            <section className="space-y-3">
              {preview ? (
                <PreviewBlock preview={preview} locale={locale} />
              ) : (
                <p className="text-sm text-muted">
                  {locale === "en" ? "Run preview from step 2 first." : "ابتدا از مرحله ۲ پیش‌نمایش بگیرید."}
                </p>
              )}
              <button
                type="button"
                className="min-h-11 rounded-ds bg-primary px-4 text-sm font-medium text-primary-foreground disabled:opacity-50"
                disabled={!preview || preview.capped || estimate > maxCombinations}
                onClick={() => setStep(4)}
              >
                {locale === "en" ? "Next: generate / update" : "ادامه: ساخت / به‌روزرسانی"}
              </button>
            </section>
          ) : null}

          {step === 4 ? (
            <section className="space-y-3" aria-label={locale === "en" ? "Generate variants" : "ساخت تنوع‌ها"}>
              {preview ? (
                <div className="rounded-ds border border-border p-3 text-sm" data-testid="product-variants-impact">
                  <p className="font-medium">{locale === "en" ? "Impact before save" : "اثر قبل از ذخیره"}</p>
                  <ul className="mt-2 list-disc ps-5 text-muted">
                    <li>
                      {locale === "en"
                        ? `${preview.newCount} new variants will be created`
                        : `${preview.newCount.toLocaleString("fa-IR")} تنوع جدید ساخته می‌شود`}
                    </li>
                    <li>
                      {locale === "en"
                        ? `${preview.unchangedCount} variants stay unchanged`
                        : `${preview.unchangedCount.toLocaleString("fa-IR")} تنوع بدون تغییر می‌ماند`}
                    </li>
                    <li>
                      {locale === "en"
                        ? `${preview.deactivateCount} older variants will be archived`
                        : `${preview.deactivateCount.toLocaleString("fa-IR")} تنوع قدیمی بایگانی می‌شود`}
                    </li>
                  </ul>
                </div>
              ) : null}
              <p className="text-sm text-muted">
                {locale === "en"
                  ? "Default variant is shown when no other selection is made."
                  : "این تنوع در صورت نبود انتخاب دیگر، به‌عنوان حالت پیش‌فرض محصول نمایش داده می‌شود."}
              </p>
              <div className="flex flex-wrap gap-2">
                <button
                  type="button"
                  disabled={busy || (!showDirty && !preview)}
                  className="min-h-11 rounded-ds bg-primary px-4 text-sm font-medium text-primary-foreground disabled:opacity-50"
                  onClick={() => void onSave()}
                  data-testid="product-variants-generate"
                >
                  {variants.length === 0
                    ? locale === "en"
                      ? "Generate variants"
                      : "ساخت تنوع‌ها"
                    : locale === "en"
                      ? "Update variants"
                      : "به‌روزرسانی تنوع‌ها"}
                </button>
                <button
                  type="button"
                  disabled={busy || !showDirty}
                  className="min-h-11 rounded-ds border border-border px-4 text-sm hover:bg-secondary disabled:opacity-50"
                  onClick={onCancel}
                >
                  {locale === "en" ? "Cancel" : "انصراف"}
                </button>
                {showDirty ? (
                  <span className="self-center text-sm text-amber-800">
                    {locale === "en" ? "Unsaved changes" : "تغییرات ذخیره‌نشده"}
                  </span>
                ) : null}
              </div>
            </section>
          ) : null}
        </div>
      ) : null}

      <section className="space-y-3" aria-label={locale === "en" ? "Current variants" : "فهرست تنوع‌ها"}>
        <div>
          <p className="font-semibold">{locale === "en" ? "Current variants" : "تنوع‌های فعلی"}</p>
          <p className="mt-1 text-sm text-muted">
            {locale === "en"
              ? "No price or stock on variants · seller offers are separate"
              : "بدون قیمت یا موجودی روی تنوع · پیشنهاد فروشنده جدا است"}
          </p>
        </div>
        {variants.length === 0 ? (
          <p className="text-sm text-muted">{locale === "en" ? "No variants yet." : "هنوز تنوعی ثبت نشده است."}</p>
        ) : (
          <ul className="grid gap-3 sm:grid-cols-2">
            {variants.map((variant) => {
              const row = rowDraft[variant.variantId];
              return (
                <li key={variant.variantId} className="rounded-ds border border-border p-3">
                  <div className="flex flex-wrap items-start justify-between gap-2">
                    <p className="font-medium">{formatCombinationLabel(variant.axisLabels)}</p>
                    {row?.isDefault || variant.isDefault ? (
                      <span className={badgeClass("emerald")}>
                        {locale === "en" ? "Default variant" : "تنوع پیش‌فرض"}
                      </span>
                    ) : null}
                  </div>
                  <p className="mt-1 text-sm text-muted" dir="ltr">
                    {row?.catalogCodeSeam || variant.catalogCodeSeam || (locale === "en" ? "No catalog code" : "بدون کد کاتالوگ")}
                  </p>
                  <div className="mt-2 flex flex-wrap items-center gap-2">
                    <span className={badgeClass("slate")}>
                      {formatVariantStatus(row?.status ?? variant.status)}
                    </span>
                    {variant.offerCount != null ? (
                      <span className="text-sm text-muted">
                        {variant.offerCount.toLocaleString(locale === "en" ? "en-US" : "fa-IR")}{" "}
                        {locale === "en" ? "offers" : "پیشنهاد"}
                      </span>
                    ) : null}
                  </div>
                  {editable ? (
                    <div className="mt-3 space-y-2">
                      <label className="block text-sm">
                        {locale === "en" ? "Status" : "وضعیت"}
                        <select
                          className="mt-1 min-h-10 w-full rounded-ds border border-border bg-surface px-2"
                          value={row?.status ?? variant.status}
                          onChange={(event) => updateRow(variant.variantId, { status: event.target.value })}
                          aria-label={`${locale === "en" ? "Status" : "وضعیت"} ${formatCombinationLabel(variant.axisLabels)}`}
                        >
                          <option value="Draft">{locale === "en" ? "Inactive" : "غیرفعال"}</option>
                          <option value="Published">{locale === "en" ? "Active" : "فعال"}</option>
                          <option value="Archived">{locale === "en" ? "Archived" : "بایگانی‌شده"}</option>
                        </select>
                      </label>
                      <label className="block text-sm">
                        {locale === "en" ? "Catalog code (optional)" : "کد کاتالوگ (اختیاری)"}
                        <input
                          className="mt-1 min-h-10 w-full rounded-ds border border-border bg-surface px-3"
                          dir="ltr"
                          value={row?.catalogCodeSeam ?? ""}
                          onChange={(event) =>
                            updateRow(variant.variantId, { catalogCodeSeam: event.target.value })
                          }
                          aria-label={`${locale === "en" ? "Catalog code" : "کد کاتالوگ"} ${formatCombinationLabel(variant.axisLabels)}`}
                        />
                      </label>
                      <button
                        type="button"
                        className="min-h-10 rounded-ds border border-border px-3 text-sm hover:bg-secondary"
                        onClick={() => setDefaultVariant(variant.variantId)}
                        aria-label={
                          locale === "en"
                            ? `Set ${formatCombinationLabel(variant.axisLabels)} as default variant`
                            : `تنظیم ${formatCombinationLabel(variant.axisLabels)} به‌عنوان تنوع پیش‌فرض`
                        }
                      >
                        {locale === "en" ? "Set as default variant" : "تنظیم به‌عنوان تنوع پیش‌فرض"}
                      </button>
                    </div>
                  ) : null}
                </li>
              );
            })}
          </ul>
        )}
      </section>
    </div>
  );
}

function PreviewBlock({
  preview,
  locale,
}: {
  preview: ProductVariantPreviewResult;
  locale: "fa" | "en";
}) {
  return (
    <section className="space-y-3 rounded-ds border border-border p-3" aria-label={locale === "en" ? "Preview variants" : "پیش‌نمایش تنوع‌ها"} data-testid="product-variants-preview-block">
      <div>
        <p className="font-semibold">{locale === "en" ? "Preview variants" : "پیش‌نمایش تنوع‌ها"}</p>
        <p className="mt-1 text-sm text-muted">
          {locale === "en"
            ? `${preview.totalDesired} variants will be built`
            : `${preview.totalDesired.toLocaleString("fa-IR")} تنوع ساخته خواهد شد`}
        </p>
        {preview.messageFa ? <p className="mt-1 text-sm text-muted">{preview.messageFa}</p> : null}
      </div>
      {preview.capped || preview.warningFa ? (
        <p className="text-sm text-amber-800" role="status">
          {preview.warningFa
            ?? (locale === "en"
              ? "Combination count exceeds the safe cap."
              : "تعداد تنوع‌ها از سقف امن بیشتر است.")}
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
              <p className="mt-1 text-xs text-muted">
                {locale === "en"
                  ? "Linked to seller offers — will not be hard-deleted"
                  : "دارای پیشنهاد فروشنده — حذف سخت نمی‌شود"}
              </p>
            ) : null}
          </li>
        ))}
      </ul>
    </section>
  );
}
