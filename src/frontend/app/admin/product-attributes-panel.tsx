"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import {
  getProductAttributeEditorState,
  setProductAttributes,
  valueKindLabel,
  type ProductAttributeEditorField,
  type ProductAttributeEditorState,
  type ProductAttributeValueInput,
} from "./catalog-attribute-api.ts";
import {
  draftFromField,
  draftToValueInput,
  displayChips,
  isAttributeDraftDirty,
  validateAttributeDrafts,
  type AttributeDraftValue,
} from "./product-attributes-panel-model.ts";

export type ProductAttributesPanelMode = "view" | "edit";

export {
  draftFromField,
  draftToValueInput,
  displayChips,
  isAttributeDraftDirty,
  validateAttributeDrafts,
} from "./product-attributes-panel-model.ts";

function badgeClass(tone: "amber" | "violet" | "slate" | "emerald" | "rose"): string {
  switch (tone) {
    case "amber":
      return "rounded-full bg-warning/15 px-2 py-0.5 text-[11px] font-medium text-warning";
    case "violet":
      return "rounded-full bg-primary/10 px-2 py-0.5 text-[11px] font-medium text-primary";
    case "emerald":
      return "rounded-full bg-success/15 px-2 py-0.5 text-[11px] font-medium text-success";
    case "rose":
      return "rounded-full bg-danger/15 px-2 py-0.5 text-[11px] font-medium text-danger";
    default:
      return "rounded-full bg-secondary px-2 py-0.5 text-[11px] font-medium text-muted";
  }
}

/**
 * پنل ویژگی‌های محصول Workspace — وابسته به schema مؤثر دسته.
 * محورهای تنوع فقط اطلاع‌رسانی؛ ویرایش ماتریس در تب تنوع‌ها.
 */
export function ProductAttributesPanel({
  productId,
  categoryId,
  categoryPath,
  canEdit,
  mode,
}: {
  productId: string;
  categoryId?: string | null;
  categoryPath?: string | null;
  canEdit: boolean;
  mode: ProductAttributesPanelMode;
}) {
  const [state, setState] = useState<ProductAttributeEditorState | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [busy, setBusy] = useState(false);
  const [drafts, setDrafts] = useState<Record<string, AttributeDraftValue>>({});
  const [dirty, setDirty] = useState(false);

  const editable = canEdit && mode === "edit";

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    const result = await getProductAttributeEditorState(productId, "fa-IR");
    setLoading(false);
    if (result.state !== "ok" || !result.data) {
      setError(result.message ?? "بارگذاری ویژگی‌ها ناموفق بود");
      setState(null);
      return;
    }
    setState(result.data);
    const next: Record<string, AttributeDraftValue> = {};
    for (const field of result.data.fields) {
      next[field.definitionId] = draftFromField(field);
    }
    setDrafts(next);
    setDirty(false);
    setFieldErrors({});
  }, [productId]);

  useEffect(() => {
    void reload();
  }, [reload]);

  const pathLabel = state?.categoryPath ?? categoryPath ?? null;
  const readiness = state?.readiness;
  const fields = useMemo(() => state?.fields ?? [], [state?.fields]);

  const valueFields = useMemo(() => fields.filter((f) => !f.isVariantAxis), [fields]);
  const axisFields = useMemo(() => fields.filter((f) => f.isVariantAxis), [fields]);

  function updateDraft(definitionId: string, patch: Partial<AttributeDraftValue>) {
    setDrafts((prev) => {
      const base = prev[definitionId] ?? {
        rawValue: "",
        enumOptionId: "",
        multiOptionIds: [],
        clear: false,
      };
      return { ...prev, [definitionId]: { ...base, ...patch, clear: false } };
    });
    setDirty(true);
  }

  function onCancel() {
    if (!state) return;
    if (dirty && !window.confirm("تغییرات ذخیره‌نشده نادیده گرفته شوند؟")) return;
    const next: Record<string, AttributeDraftValue> = {};
    for (const field of state.fields) {
      next[field.definitionId] = draftFromField(field);
    }
    setDrafts(next);
    setDirty(false);
    setFieldErrors({});
    setError(null);
  }

  async function onSave() {
    if (!state) return;
    const errors = validateAttributeDrafts(state.fields, drafts);
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0) {
      setError("لطفاً خطاهای فیلد را برطرف کنید");
      return;
    }
    const values: ProductAttributeValueInput[] = [];
    for (const field of state.fields) {
      if (field.isVariantAxis) continue;
      const draft = drafts[field.definitionId] ?? draftFromField(field);
      if (!isAttributeDraftDirty(field, draft)) continue;
      const input = draftToValueInput(field, draft);
      if (input) values.push(input);
    }
    if (values.length === 0) {
      setDirty(false);
      return;
    }
    setBusy(true);
    setError(null);
    const result = await setProductAttributes(productId, values, "fa-IR");
    setBusy(false);
    if (result.state !== "ok" || !result.data) {
      setError(result.message ?? "ذخیره ویژگی‌ها ناموفق بود");
      return;
    }
    setState(result.data);
    const next: Record<string, AttributeDraftValue> = {};
    for (const field of result.data.fields) {
      next[field.definitionId] = draftFromField(field);
    }
    setDrafts(next);
    setDirty(false);
    setFieldErrors({});
  }

  if (!categoryId && !state?.categoryId) {
    return (
      <div data-testid="product-attributes-category-required" className="text-sm text-muted">
        <p className="font-semibold text-foreground">دسته لازم است</p>
        <p className="mt-2">برای بارگذاری ویژگی‌ها، ابتدا در تب عمومی یک دسته انتخاب و ذخیره کنید.</p>
      </div>
    );
  }

  return (
    <div className="space-y-4" dir="rtl" data-testid="admin-product-attributes" data-mode={mode}>
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 className="text-base font-semibold text-foreground">ویژگی‌های محصول</h2>
          <p className="mt-1 text-xs text-muted" data-testid="product-attributes-category-path">
            {pathLabel ? `دسته: ${pathLabel}` : "دسته انتخاب شده"}
          </p>
        </div>
        {editable ? (
          <div className="flex flex-wrap gap-2">
            <button
              type="button"
              className="rounded-ds border border-border px-3 py-2 text-sm hover:bg-secondary disabled:opacity-50"
              disabled={busy || loading || !dirty}
              onClick={onCancel}
            >
              انصراف
            </button>
            <button
              type="button"
              className="rounded-ds bg-primary px-3 py-2 text-sm text-primary-foreground disabled:opacity-50"
              disabled={busy || loading || !dirty}
              data-testid="product-attributes-save"
              onClick={() => void onSave()}
            >
              ذخیره ویژگی‌ها
            </button>
          </div>
        ) : null}
      </div>

      {readiness ? (
        <div
          className="rounded-ds border border-border bg-secondary/40 px-3 py-2 text-sm"
          data-testid="product-attributes-readiness"
        >
          {readiness.isComplete ? (
            <span className={badgeClass("emerald")}>تکمیل ویژگی‌های الزامی</span>
          ) : (
            <span className={badgeClass("rose")}>
              ناقص — {readiness.missingRequiredCodes.length} ویژگی الزامی مانده
            </span>
          )}
        </div>
      ) : null}

      {error ? <p className="text-sm text-danger">{error}</p> : null}

      {loading ? (
        <p className="text-sm text-muted">در حال بارگذاری…</p>
      ) : (
        <>
          <ul className="space-y-3" data-testid="product-attributes-value-list">
            {valueFields.length === 0 ? (
              <li className="text-sm text-muted" data-testid="product-attributes-empty-schema">
                <p>برای این دسته‌بندی هنوز ویژگی‌ای تعریف نشده است.</p>
                <Link
                  href="/admin/catalog/categories"
                  className="mt-2 inline-block text-sm font-medium text-primary hover:underline"
                  data-testid="product-attributes-manage-category-link"
                >
                  مدیریت ویژگی‌های دسته‌بندی
                </Link>
              </li>
            ) : (
              valueFields.map((field) => (
                <AttributeFieldRow
                  key={field.definitionId}
                  field={field}
                  draft={drafts[field.definitionId] ?? draftFromField(field)}
                  mode={editable ? "edit" : "view"}
                  error={fieldErrors[field.definitionId]}
                  onChange={(patch) => updateDraft(field.definitionId, patch)}
                  onClear={() => {
                    if (field.isRequired) return;
                    setDrafts((prev) => ({
                      ...prev,
                      [field.definitionId]: {
                        rawValue: "",
                        enumOptionId: "",
                        multiOptionIds: [],
                        clear: true,
                      },
                    }));
                    setDirty(true);
                  }}
                />
              ))
            )}
          </ul>

          {axisFields.length > 0 ? (
            <div
              className="rounded-ds border border-dashed border-border p-3"
              data-testid="product-attributes-variant-axes"
            >
              <p className="text-sm font-medium text-foreground">محورهای تنوع</p>
              <p className="mt-1 text-xs text-muted">محور تنوع — در تب تنوع‌ها مدیریت می‌شود.</p>
              <ul className="mt-3 space-y-2">
                {axisFields.map((field) => (
                  <li key={field.definitionId} className="flex flex-wrap items-center gap-2 text-sm">
                    <span className="font-medium">{field.localizedName}</span>
                    <span className={badgeClass("violet")}>محور تنوع</span>
                    <span className="text-muted">{valueKindLabel(field.valueKind)}</span>
                  </li>
                ))}
              </ul>
            </div>
          ) : null}
        </>
      )}
    </div>
  );
}

function AttributeFieldRow({
  field,
  draft,
  mode,
  error,
  onChange,
  onClear,
}: {
  field: ProductAttributeEditorField;
  draft: AttributeDraftValue;
  mode: ProductAttributesPanelMode;
  error?: string;
  onChange: (patch: Partial<AttributeDraftValue>) => void;
  onClear: () => void;
}) {
  const chips = displayChips(field.displayValue);

  if (mode === "view") {
    return (
      <li
        className="rounded-ds border border-border bg-surface p-3"
        data-testid={`product-attr-view-${field.code}`}
      >
        <div className="flex flex-wrap items-center gap-2">
          <p className="font-medium text-foreground">{field.localizedName}</p>
          {field.isRequired ? <span className={badgeClass("amber")}>الزامی</span> : null}
          {field.isMissingRequired ? <span className={badgeClass("rose")}>مقدار ندارد</span> : null}
          {field.unit ? <span className="text-xs text-muted">{field.unit}</span> : null}
        </div>
        <div className="mt-2 flex flex-wrap gap-1">
          {chips.length === 0 ? (
            <span className="text-sm text-muted">—</span>
          ) : (
            chips.map((chip) => (
              <span key={chip} className={badgeClass("slate")}>
                {chip}
              </span>
            ))
          )}
        </div>
      </li>
    );
  }

  const inputClass =
    "mt-1 min-h-10 w-full rounded-ds border border-border bg-surface px-3 text-sm focus:outline-none focus:ring-2 focus:ring-primary/30";

  return (
    <li
      className="rounded-ds border border-border bg-surface p-3"
      data-testid={`product-attr-edit-${field.code}`}
    >
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div className="flex flex-wrap items-center gap-2">
          <p className="font-medium text-foreground">{field.localizedName}</p>
          {field.isRequired ? <span className={badgeClass("amber")}>الزامی</span> : null}
          <span className="text-xs text-muted">{valueKindLabel(field.valueKind)}</span>
        </div>
        {!field.isRequired && (field.currentCanonicalValue || draft.rawValue || draft.enumOptionId) ? (
          <button type="button" className="text-xs text-danger hover:underline" onClick={onClear}>
            پاک کردن
          </button>
        ) : null}
      </div>

      <div className="mt-2">
        {field.valueKind === "Boolean" ? (
          <select
            className={inputClass}
            value={draft.clear ? "" : draft.rawValue}
            onChange={(e) => onChange({ rawValue: e.target.value })}
            aria-label={field.localizedName}
          >
            <option value="">—</option>
            <option value="true">بله</option>
            <option value="false">خیر</option>
          </select>
        ) : field.valueKind === "Enumeration" && field.isMultivalue ? (
          <div className="space-y-1" role="group" aria-label={field.localizedName}>
            {field.options
              .filter((o) => o.isActive)
              .map((opt) => {
                const checked = draft.multiOptionIds.some(
                  (id) => id.toLowerCase() === opt.optionId.toLowerCase(),
                );
                return (
                  <label key={opt.optionId} className="flex items-center gap-2 text-sm">
                    <input
                      type="checkbox"
                      className="size-4 rounded border-border"
                      checked={checked}
                      onChange={() => {
                        const next = checked
                          ? draft.multiOptionIds.filter(
                              (id) => id.toLowerCase() !== opt.optionId.toLowerCase(),
                            )
                          : [...draft.multiOptionIds, opt.optionId];
                        onChange({ multiOptionIds: next });
                      }}
                    />
                    {opt.localizedLabel}
                  </label>
                );
              })}
          </div>
        ) : field.valueKind === "Enumeration" ? (
          <select
            className={inputClass}
            value={draft.clear ? "" : draft.enumOptionId}
            onChange={(e) => onChange({ enumOptionId: e.target.value })}
            aria-label={field.localizedName}
          >
            <option value="">— انتخاب کنید —</option>
            {field.options
              .filter((o) => o.isActive)
              .map((opt) => (
                <option key={opt.optionId} value={opt.optionId}>
                  {opt.localizedLabel}
                </option>
              ))}
          </select>
        ) : field.valueKind === "Instant" ? (
          <input
            type="datetime-local"
            className={inputClass}
            value={toDatetimeLocal(draft.rawValue)}
            onChange={(e) => onChange({ rawValue: fromDatetimeLocal(e.target.value) })}
            aria-label={field.localizedName}
          />
        ) : (
          <input
            className={inputClass}
            type={field.valueKind === "Number" ? "number" : "text"}
            value={draft.clear ? "" : draft.rawValue}
            onChange={(e) => onChange({ rawValue: e.target.value })}
            aria-label={field.localizedName}
            placeholder={field.unit ? `واحد: ${field.unit}` : undefined}
          />
        )}
      </div>
      {error ? <p className="mt-1 text-xs text-danger">{error}</p> : null}
    </li>
  );
}

function toDatetimeLocal(iso: string): string {
  if (!iso) return "";
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return "";
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

function fromDatetimeLocal(local: string): string {
  if (!local) return "";
  const d = new Date(local);
  return Number.isNaN(d.getTime()) ? local : d.toISOString();
}
