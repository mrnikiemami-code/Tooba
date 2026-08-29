import type {
  ProductVariantAxisEditorField,
  ProductVariantCombinationPreview,
  ProductVariantListItem,
  ProductVariantSelectedAxisInput,
} from "./catalog-attribute-api.ts";

export type VariantAxisDraft = Record<string, string[]>;

export type VariantRowDraft = {
  status: string;
  catalogCodeSeam: string;
  isDefault: boolean;
};

/** پیش‌نویس محورها از حالت سرور. */
export function axisDraftFromState(axes: ProductVariantAxisEditorField[]): VariantAxisDraft {
  const next: VariantAxisDraft = {};
  for (const axis of axes) {
    next[axis.definitionId] = [...axis.selectedOptionIds];
  }
  return next;
}

/** پیش‌نویس ردیف‌های تنوع از فهرست سرور. */
export function rowDraftFromVariants(variants: ProductVariantListItem[]): Record<string, VariantRowDraft> {
  const next: Record<string, VariantRowDraft> = {};
  for (const variant of variants) {
    next[variant.variantId] = {
      status: variant.status,
      catalogCodeSeam: variant.catalogCodeSeam ?? "",
      isDefault: variant.isDefault,
    };
  }
  return next;
}

/** آیا انتخاب محورها نسبت به سرور تغییر کرده؟ */
export function isAxisDraftDirty(
  axes: ProductVariantAxisEditorField[],
  draft: VariantAxisDraft,
): boolean {
  for (const axis of axes) {
    const current = [...(draft[axis.definitionId] ?? [])].sort();
    const baseline = [...axis.selectedOptionIds].sort();
    if (current.length !== baseline.length || current.some((id, i) => id !== baseline[i])) {
      return true;
    }
  }
  return false;
}

/** آیا پچ ردیف‌ها نسبت به سرور تغییر کرده؟ */
export function isRowDraftDirty(
  variants: ProductVariantListItem[],
  draft: Record<string, VariantRowDraft>,
): boolean {
  for (const variant of variants) {
    const row = draft[variant.variantId];
    if (!row) continue;
    if (row.status !== variant.status) return true;
    if ((row.catalogCodeSeam || "") !== (variant.catalogCodeSeam ?? "")) return true;
    if (row.isDefault !== variant.isDefault) return true;
  }
  return false;
}

/** payload انتخاب محورها برای preview/apply. */
export function selectedAxesFromDraft(
  axes: ProductVariantAxisEditorField[],
  draft: VariantAxisDraft,
): ProductVariantSelectedAxisInput[] {
  return axes
    .map((axis) => ({
      definitionId: axis.definitionId,
      optionIds: [...(draft[axis.definitionId] ?? [])],
    }))
    .filter((axis) => axis.optionIds.length > 0);
}

/** برچسب خوانای ترکیب بدون شناسه خام. */
export function formatCombinationLabel(labels: { definitionName: string; valueLabel: string }[]): string {
  if (labels.length === 0) return "—";
  return labels.map((x) => x.valueLabel).join(" / ");
}

/** برچسب فارسی وضعیت. */
export function formatVariantStatus(status: string): string {
  switch (status) {
    case "Published":
      return "فعال";
    case "Archived":
      return "بایگانی‌شده";
    case "Draft":
    default:
      return "غیرفعال";
  }
}

/** برچسب فارسی عمل پیش‌نمایش. */
export function formatPreviewAction(action: ProductVariantCombinationPreview["action"]): string {
  switch (action) {
    case "New":
      return "جدید";
    case "Deactivate":
      return "غیرفعال می‌شود";
    default:
      return "بدون تغییر";
  }
}

/** تعداد ترکیب تقریبی از انتخاب فعلی (برای هشدار UI). */
export function estimateCombinationCount(draft: VariantAxisDraft): number {
  const counts = Object.values(draft)
    .map((ids) => ids.length)
    .filter((n) => n > 0);
  if (counts.length === 0) return 0;
  return counts.reduce((acc, n) => acc * n, 1);
}
