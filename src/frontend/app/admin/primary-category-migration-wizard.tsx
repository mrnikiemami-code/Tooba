"use client";

/**
 * ویزارد سه‌مرحله‌ای تغییر دسته اصلی محصول (TB-P07-T036).
 * مرحله ۱: انتخاب L3 — مرحله ۲: پیش‌نمایش بدون mutation — مرحله ۳: تأیید و مهاجرت تراکنشی.
 */

import { useCallback, useMemo, useState } from "react";
import {
  previewProductCategoryChange,
  type CategoryChangeImpactReport,
} from "./catalog-attribute-api";
import { assignAdminProductCategory } from "./host-client";
import { mapAdminErrorMessage } from "./admin-error-map";
import { ProductCategoryPicker } from "./product-category-picker";
import type { ProductWorkspaceView } from "./workspace-model";

type WizardStep = 1 | 2 | 3;

export function PrimaryCategoryMigrationWizard({
  view,
  viewScope = false,
  open,
  onClose,
  onMigrated,
}: {
  view: ProductWorkspaceView;
  viewScope?: boolean;
  open: boolean;
  onClose: () => void;
  onMigrated: (next: ProductWorkspaceView) => void;
}) {
  const currentPath =
    view.categoryPath ||
    view.categoryNames.join(" › ") ||
    (view.primaryCategoryId ? "دستهٔ فعلی" : "بدون دسته اصلی");
  const [step, setStep] = useState<WizardStep>(1);
  const [targetId, setTargetId] = useState<string | null>(null);
  const [targetPathLabel, setTargetPathLabel] = useState<string | null>(null);
  const [preview, setPreview] = useState<CategoryChangeImpactReport | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const reset = useCallback(() => {
    setStep(1);
    setTargetId(null);
    setTargetPathLabel(null);
    setPreview(null);
    setBusy(false);
    setError(null);
  }, []);

  const handleClose = useCallback(() => {
    if (busy) return;
    reset();
    onClose();
  }, [busy, onClose, reset]);

  const sameAsCurrent =
    Boolean(targetId) &&
    Boolean(view.primaryCategoryId) &&
    targetId === view.primaryCategoryId;

  const loadPreview = useCallback(async () => {
    if (!targetId || sameAsCurrent) {
      setError(sameAsCurrent ? "دستهٔ هدف همان دستهٔ فعلی است." : "دستهٔ سطح سوم را انتخاب کنید.");
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const result = await previewProductCategoryChange(view.productId, targetId, "fa-IR");
      if (result.state !== "ok" || !result.data) {
        setError(mapAdminErrorMessage(result.message) || "پیش‌نمایش ناموفق بود.");
        return;
      }
      setPreview(result.data);
      setTargetPathLabel(result.data.targetCategoryPath || targetPathLabel);
      setStep(2);
    } finally {
      setBusy(false);
    }
  }, [sameAsCurrent, targetId, targetPathLabel, view.productId]);

  const runMigration = useCallback(async () => {
    if (!targetId) return;
    setBusy(true);
    setError(null);
    try {
      const result = await assignAdminProductCategory(
        view.productId,
        {
          categoryId: targetId,
          confirmSchemaImpact: Boolean(view.primaryCategoryId),
          expectedUpdatedAt: view.catalogUpdatedAt,
        },
        viewScope,
      );
      if (!result.ok) {
        setError(mapAdminErrorMessage(result.errorCode));
        return;
      }
      onMigrated(result.view);
      reset();
      onClose();
    } finally {
      setBusy(false);
    }
  }, [onClose, onMigrated, reset, targetId, view.catalogUpdatedAt, view.primaryCategoryId, view.productId, viewScope]);

  const stepTitle = useMemo(() => {
    if (step === 1) return "۱ — انتخاب دستهٔ هدف";
    if (step === 2) return "۲ — پیش‌نمایش تأثیر";
    return "۳ — تأیید نهایی";
  }, [step]);

  if (!open) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-3 sm:items-center"
      role="dialog"
      aria-modal="true"
      aria-labelledby="primary-category-migration-title"
      data-testid="primary-category-migration-wizard"
    >
      <div className="flex max-h-[92vh] w-full max-w-2xl flex-col overflow-hidden rounded-2xl bg-white shadow-xl">
        <div className="border-b border-gray-100 px-5 py-4">
          <h3 id="primary-category-migration-title" className="text-base font-semibold text-slate-900">
            تغییر دسته اصلی
          </h3>
          <p className="mt-1 text-sm text-slate-600" data-testid="primary-migration-step-label">
            {stepTitle}
          </p>
        </div>

        <div className="min-h-0 flex-1 space-y-4 overflow-auto px-5 py-4">
          {error ? (
            <p className="text-sm text-red-600" role="alert" data-testid="primary-migration-error">
              {error}
            </p>
          ) : null}

          {step === 1 ? (
            <div className="space-y-4" data-testid="primary-migration-step-1">
              <div className="rounded-xl border border-slate-200 bg-slate-50 px-3 py-2 text-sm">
                <p className="text-xs font-medium text-slate-500">مسیر فعلی</p>
                <p className="mt-0.5 font-medium text-slate-900" data-testid="primary-migration-current-path">
                  {currentPath}
                </p>
              </div>
              <ProductCategoryPicker
                label="دستهٔ هدف (سطح سوم)"
                value={targetId}
                onChange={(next) => {
                  setTargetId(next);
                  setTargetPathLabel(null);
                  setPreview(null);
                }}
                required
                invalidSelectionHint
              />
              <p className="text-xs text-slate-500">
                تغییر دسته اصلی مهاجرت ساختاری است؛ در مرحله بعد تأثیر ویژگی‌ها و تنوع‌ها را می‌بینید.
              </p>
            </div>
          ) : null}

          {step === 2 && preview ? (
            <div className="space-y-3" data-testid="primary-migration-step-2">
              <PathCompare
                current={preview.currentCategoryPath || currentPath}
                target={preview.targetCategoryPath || targetPathLabel || "—"}
              />
              <ImpactSection title="ویژگی‌های قابل حفظ" items={preview.preservedAttributes} empty="موردی نیست" />
              <ImpactSection title="ویژگی‌های جدید / الزامی" items={preview.addedAttributes.length ? preview.addedAttributes : preview.newlyRequiredLabels} empty="موردی نیست" />
              <ImpactSection title="ویژگی‌های خارج‌شونده" items={preview.removedAttributes.length ? preview.removedAttributes : preview.orphanSummaries.map((o) => o.localizedName)} empty="موردی نیست" />
              <div className="rounded-xl border border-slate-200 px-3 py-2 text-sm" data-testid="primary-migration-variant-impact">
                <p className="font-medium text-slate-800">تنوع‌های تحت تأثیر</p>
                <p className="mt-1 text-slate-700">
                  {preview.variantCompatible
                    ? `سازگار — ${preview.preservedVariantCount} تنوع حفظ می‌شود`
                    : preview.variantImpactMessageFa ||
                      `${preview.affectedVariantCount || preview.impactedVariantCount} تنوع نیاز به بازبینی دارد`}
                </p>
              </div>
              <div className="rounded-xl border border-slate-200 px-3 py-2 text-sm" data-testid="primary-migration-membership-impact">
                <p className="font-medium text-slate-800">نمایش در دسته‌های دیگر</p>
                <p className="mt-1 text-slate-700">
                  {preview.additionalMembershipPromoted
                    ? "دستهٔ هدف از «نمایش در این دسته» به دسته اصلی ارتقا می‌یابد."
                    : "ارتقا از عضویت نمایشی لازم نیست."}
                  {preview.otherDisplayMembershipsRemainCount > 0
                    ? ` ${preview.otherDisplayMembershipsRemainCount} عضویت نمایشی دیگر باقی می‌ماند.`
                    : ""}
                </p>
              </div>
              {preview.readinessBlockers.length > 0 ? (
                <ImpactSection
                  title="موانع آمادگی انتشار"
                  items={preview.readinessBlockers}
                  empty=""
                  tone="warning"
                />
              ) : (
                <p className="text-sm text-slate-600">مانع آمادگی جدیدی گزارش نشده است.</p>
              )}
              <pre className="whitespace-pre-wrap rounded-xl bg-slate-50 px-3 py-2 text-xs text-slate-700" data-testid="primary-migration-message-fa">
                {preview.messageFa}
              </pre>
              <p className="text-xs text-amber-800">
                این مرحله تغییری ذخیره نمی‌کند. برای اعمال باید در مرحله بعد تأیید کنید.
              </p>
            </div>
          ) : null}

          {step === 3 && preview ? (
            <div className="space-y-3" data-testid="primary-migration-step-3">
              <PathCompare
                current={preview.currentCategoryPath || currentPath}
                target={preview.targetCategoryPath || targetPathLabel || "—"}
              />
              <p className="text-sm text-slate-800">
                با تأیید، مهاجرت به‌صورت تراکنشی اعمال می‌شود. مقادیر ناسازگار از اسکیمای فعال حذف می‌شوند و در صورت ناسازگاری ساختاری، محصول منتشرشده طبق سیاست آمادگی از انتشار خارج می‌شود.
              </p>
              {preview.readinessBlockers.length > 0 ? (
                <p className="text-sm text-amber-900">
                  پس از مهاجرت، موارد ناقص را در تب‌های ویژگی/تنوع تکمیل کنید.
                </p>
              ) : null}
            </div>
          ) : null}
        </div>

        <div className="flex flex-wrap items-center justify-between gap-2 border-t border-gray-100 px-5 py-3">
          <button
            type="button"
            className="rounded-xl border border-slate-200 px-4 py-2 text-sm font-medium text-slate-700 disabled:opacity-50"
            disabled={busy}
            onClick={handleClose}
            data-testid="primary-migration-cancel"
          >
            انصراف
          </button>
          <div className="flex flex-wrap gap-2">
            {step > 1 ? (
              <button
                type="button"
                className="rounded-xl border border-slate-200 px-4 py-2 text-sm font-medium text-slate-700 disabled:opacity-50"
                disabled={busy}
                onClick={() => setStep((s) => (s === 3 ? 2 : 1))}
                data-testid="primary-migration-back"
              >
                بازگشت
              </button>
            ) : null}
            {step === 1 ? (
              <button
                type="button"
                className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
                disabled={busy || !targetId || sameAsCurrent}
                onClick={() => void loadPreview()}
                data-testid="primary-migration-to-preview"
              >
                {busy ? "در حال پیش‌نمایش…" : "ادامه به پیش‌نمایش"}
              </button>
            ) : null}
            {step === 2 ? (
              <button
                type="button"
                className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
                disabled={busy}
                onClick={() => setStep(3)}
                data-testid="primary-migration-to-confirm"
              >
                ادامه به تأیید
              </button>
            ) : null}
            {step === 3 ? (
              <button
                type="button"
                className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
                disabled={busy}
                onClick={() => void runMigration()}
                data-testid="primary-migration-confirm"
              >
                {busy ? "در حال اعمال…" : "تأیید و اعمال مهاجرت"}
              </button>
            ) : null}
          </div>
        </div>
      </div>
    </div>
  );
}

function PathCompare({ current, target }: { current: string; target: string }) {
  return (
    <div className="grid gap-2 sm:grid-cols-2" data-testid="primary-migration-path-compare">
      <div className="rounded-xl border border-slate-200 bg-slate-50 px-3 py-2 text-sm">
        <p className="text-xs text-slate-500">فعلی</p>
        <p className="font-medium text-slate-900">{current}</p>
      </div>
      <div className="rounded-xl border border-blue-200 bg-blue-50 px-3 py-2 text-sm">
        <p className="text-xs text-blue-700">هدف</p>
        <p className="font-medium text-blue-950">{target}</p>
      </div>
    </div>
  );
}

function ImpactSection({
  title,
  items,
  empty,
  tone = "neutral",
}: {
  title: string;
  items: string[];
  empty: string;
  tone?: "neutral" | "warning";
}) {
  const box =
    tone === "warning"
      ? "rounded-xl border border-amber-200 bg-amber-50 px-3 py-2 text-sm"
      : "rounded-xl border border-slate-200 px-3 py-2 text-sm";
  return (
    <div className={box}>
      <p className="font-medium text-slate-800">{title}</p>
      {items.length === 0 ? (
        <p className="mt-1 text-slate-500">{empty}</p>
      ) : (
        <ul className="mt-1 list-disc pe-5 text-slate-700">
          {items.map((item) => (
            <li key={item}>{item}</li>
          ))}
        </ul>
      )}
    </div>
  );
}
