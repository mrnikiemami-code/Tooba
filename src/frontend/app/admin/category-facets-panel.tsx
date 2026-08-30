"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { toast } from "react-toastify";
import { buildCategoryPath, type AppCategoryTreeNode } from "../../design-system";
import {
  loadEffectiveCategorySchema,
  valueKindLabel,
  type EffectiveSchemaEntry,
} from "./catalog-attribute-api.ts";
import { humanizeAttributeCode } from "./category-attributes-panel.tsx";
import { mapAdminErrorMessage } from "./admin-error-map.ts";
import { resolveAdminChromeLocale } from "./admin-chrome-messages.ts";
import {
  displayTypeLabel,
  FACET_DISPLAY_TYPES,
  isSearchableDisplayType,
  loadEffectiveCategoryFacets,
  partitionEffectiveFacets,
  removeCategoryFacetOverride,
  reorderCategoryFacets,
  suggestFacetDisplayType,
  upsertCategoryFacet,
  type CatalogFacetDisplayType,
  type EffectiveCategoryFacet,
  type UpsertCategoryFacetInput,
} from "./catalog-facet-api.ts";

function badgeClass(tone: "blue" | "slate" | "amber" | "emerald"): string {
  switch (tone) {
    case "blue":
      return "rounded-full bg-blue-50 px-2 py-0.5 text-[11px] font-medium text-blue-800";
    case "amber":
      return "rounded-full bg-amber-50 px-2 py-0.5 text-[11px] font-medium text-amber-900";
    case "emerald":
      return "rounded-full bg-emerald-50 px-2 py-0.5 text-[11px] font-medium text-emerald-900";
    default:
      return "rounded-full bg-slate-100 px-2 py-0.5 text-[11px] font-medium text-slate-700";
  }
}

function FacetBadges({ row, locale }: { row: EffectiveCategoryFacet; locale: "fa" | "en" }) {
  return (
    <div className="flex flex-wrap gap-1">
      <span className={badgeClass("blue")} data-testid="facet-badge-type">
        {displayTypeLabel(row.displayType, locale)}
      </span>
      {!row.isVisible ? (
        <span className={badgeClass("slate")} data-testid="facet-badge-hidden">
          مخفی
        </span>
      ) : (
        <span className={badgeClass("emerald")} data-testid="facet-badge-visible">
          فعال
        </span>
      )}
      {row.isSearchable ? (
        <span className={badgeClass("amber")} data-testid="facet-badge-searchable">
          قابل جستجو
        </span>
      ) : null}
      {row.isInherited ? (
        <span className={badgeClass("slate")} data-testid="facet-badge-inherited">
          ارث‌برده‌شده
        </span>
      ) : null}
    </div>
  );
}

function defaultFacetInput(row?: EffectiveCategoryFacet, valueKind?: EffectiveSchemaEntry["valueKind"]): UpsertCategoryFacetInput {
  const kind = valueKind ?? row?.valueKind ?? "Enumeration";
  const displayType = row?.displayType ?? suggestFacetDisplayType(kind);
  return {
    displayType,
    sortOrder: row?.sortOrder ?? 0,
    isVisible: row?.isVisible ?? true,
    isSearchable: row?.isSearchable ?? isSearchableDisplayType(displayType),
    isCollapsedByDefault: row?.isCollapsedByDefault ?? false,
    showCounts: row?.showCounts ?? true,
  };
}

function FacetConfigEditor({
  draft,
  valueKind,
  onChange,
  locale = "fa",
}: {
  draft: UpsertCategoryFacetInput;
  valueKind: EffectiveSchemaEntry["valueKind"];
  onChange: (next: UpsertCategoryFacetInput) => void;
  locale?: "fa" | "en";
}) {
  const allowedTypes = FACET_DISPLAY_TYPES.filter((type) => {
    if (valueKind === "Boolean") return type === "BooleanToggle";
    if (valueKind === "Number") return type === "Range";
    if (valueKind === "Text") return type === "SearchableSelect" || type === "CheckboxList";
    if (valueKind === "Enumeration") return type === "CheckboxList" || type === "SearchableSelect";
    return false;
  });

  return (
    <div className="space-y-3 rounded-xl border border-gray-100 bg-slate-50 p-3 text-sm">
      <label className="block">
        <span className="mb-1 block text-xs text-slate-600">
          {locale === "en" ? "Display type" : "نوع نمایش"}
        </span>
        <select
          className="w-full rounded-lg border border-gray-200 px-3 py-2"
          value={draft.displayType}
          onChange={(e) => {
            const displayType = e.target.value as CatalogFacetDisplayType;
            onChange({
              ...draft,
              displayType,
              isSearchable: isSearchableDisplayType(displayType) ? draft.isSearchable : false,
            });
          }}
          data-testid="facet-display-type"
        >
          {allowedTypes.map((type) => (
            <option key={type} value={type}>
              {displayTypeLabel(type, locale)}
            </option>
          ))}
        </select>
      </label>
      <label className="flex items-center gap-2">
        <input
          type="checkbox"
          checked={draft.isVisible}
          onChange={(e) => onChange({ ...draft, isVisible: e.target.checked })}
          data-testid="facet-visible"
        />
        {locale === "en" ? "Show on product-list page" : "نمایش در صفحه محصولات"}
      </label>
      {isSearchableDisplayType(draft.displayType) ? (
        <label className="flex items-center gap-2">
          <input
            type="checkbox"
            checked={draft.isSearchable}
            onChange={(e) => onChange({ ...draft, isSearchable: e.target.checked })}
            data-testid="facet-searchable"
          />
          {locale === "en" ? "Searchable" : "قابل جستجو"}
        </label>
      ) : null}
      <label className="flex items-center gap-2">
        <input
          type="checkbox"
          checked={draft.isCollapsedByDefault}
          onChange={(e) => onChange({ ...draft, isCollapsedByDefault: e.target.checked })}
          data-testid="facet-collapsed"
        />
        {locale === "en" ? "Collapsed by default" : "پیش‌فرض بسته"}
      </label>
      <label className="flex items-center gap-2">
        <input
          type="checkbox"
          checked={draft.showCounts}
          onChange={(e) => onChange({ ...draft, showCounts: e.target.checked })}
          data-testid="facet-show-counts"
        />
        {locale === "en" ? "Show counts" : "نمایش تعداد"}
      </label>
    </div>
  );
}

/**
 * تب فیلترهای PLP — پیکربندی نمایش فیلتر برای ویژگی‌های قابل فیلتر.
 */
export function CategoryFacetsPanel({
  categoryId,
  treeNodes,
  isEdit,
  canEdit,
  busy: externalBusy,
}: {
  categoryId: string;
  treeNodes: AppCategoryTreeNode[];
  isEdit: boolean;
  canEdit: boolean;
  busy?: boolean;
}) {
  const [facets, setFacets] = useState<EffectiveCategoryFacet[]>([]);
  const [schema, setSchema] = useState<EffectiveSchemaEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [addOpen, setAddOpen] = useState(false);
  const [addSearch, setAddSearch] = useState("");
  const [editTarget, setEditTarget] = useState<EffectiveCategoryFacet | null>(null);
  const [editDraft, setEditDraft] = useState<UpsertCategoryFacetInput | null>(null);
  const [addDraft, setAddDraft] = useState<UpsertCategoryFacetInput | null>(null);
  const [selectedDefId, setSelectedDefId] = useState<string | null>(null);

  const isBusy = busy || Boolean(externalBusy);
  const locale = resolveAdminChromeLocale();
  const toUiError = useCallback(
    (raw: string | null | undefined) => mapAdminErrorMessage(raw || "host-unreachable", locale),
    [locale],
  );

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    const [facetRes, schemaRes] = await Promise.all([
      loadEffectiveCategoryFacets(categoryId),
      loadEffectiveCategorySchema(categoryId),
    ]);
    setLoading(false);
    if (facetRes.state !== "ok" || !facetRes.data) {
      setError(toUiError(facetRes.message));
      return;
    }
    if (schemaRes.state !== "ok" || !schemaRes.data) {
      setError(toUiError(schemaRes.message));
      return;
    }
    setFacets(facetRes.data);
    setSchema(schemaRes.data);
  }, [categoryId, toUiError]);

  useEffect(() => {
    void reload();
  }, [reload]);

  const { inherited, local } = useMemo(() => partitionEffectiveFacets(facets), [facets]);

  const categoryNameById = useMemo(() => {
    const map = new Map<string, string>();
    for (const node of treeNodes) {
      map.set(node.id, node.name);
    }
    return map;
  }, [treeNodes]);

  const configuredIds = useMemo(() => new Set(facets.map((f) => f.definitionId)), [facets]);

  const eligibleToAdd = useMemo(() => {
    return schema.filter((row) => row.isFilterable && !configuredIds.has(row.definitionId));
  }, [schema, configuredIds]);

  const filteredEligible = useMemo(() => {
    const q = addSearch.trim().toLowerCase();
    if (!q) return eligibleToAdd;
    return eligibleToAdd.filter((row) => humanizeAttributeCode(row.code).toLowerCase().includes(q));
  }, [eligibleToAdd, addSearch]);

  const handleSaveFacet = async (definitionId: string, input: UpsertCategoryFacetInput) => {
    setBusy(true);
    const result = await upsertCategoryFacet(categoryId, definitionId, input);
    setBusy(false);
    if (result.state !== "ok") {
      toast.error(toUiError(result.message));
      return;
    }
    toast.success(locale === "en" ? "Filter settings saved" : "تنظیمات فیلتر ذخیره شد");
    setEditTarget(null);
    setEditDraft(null);
    setAddOpen(false);
    setSelectedDefId(null);
    setAddDraft(null);
    await reload();
  };

  const handleRemoveLocal = async (row: EffectiveCategoryFacet) => {
    if (!window.confirm("تنظیم فیلتر این دسته حذف شود؟")) return;
    setBusy(true);
    const result = await removeCategoryFacetOverride(categoryId, row.definitionId);
    setBusy(false);
    if (result.state !== "ok") {
      toast.error(toUiError(result.message));
      return;
    }
    toast.success(locale === "en" ? "Local filter override removed" : "تنظیم محلی حذف شد");
    await reload();
  };

  const handleOverrideInherited = (row: EffectiveCategoryFacet) => {
    setEditTarget(row);
    setEditDraft(defaultFacetInput(row));
  };

  const handleMoveLocal = async (index: number, direction: -1 | 1) => {
    const nextIndex = index + direction;
    if (nextIndex < 0 || nextIndex >= local.length) return;
    const reordered = [...local];
    const [item] = reordered.splice(index, 1);
    reordered.splice(nextIndex, 0, item);
    setBusy(true);
    const result = await reorderCategoryFacets(
      categoryId,
      reordered.map((row) => row.definitionId),
    );
    setBusy(false);
    if (result.state !== "ok") {
      toast.error(toUiError(result.message));
      return;
    }
    await reload();
  };

  const renderRow = (row: EffectiveCategoryFacet, index: number, editableLocal: boolean) => {
    const sourceLabel = categoryNameById.get(row.sourceCategoryId)
      ?? buildCategoryPath(treeNodes, row.sourceCategoryId).join(" › ");
    return (
      <li
        key={row.definitionId}
        className="flex flex-col gap-2 rounded-2xl border border-gray-100 bg-white px-4 py-3 sm:flex-row sm:items-center sm:justify-between"
        data-testid={`category-facet-row-${row.code}`}
      >
        <div className="min-w-0">
          <div className="font-medium text-slate-900">{row.localizedName || humanizeAttributeCode(row.code)}</div>
          <div className="mt-0.5 text-xs text-slate-500">نوع: {valueKindLabel(row.valueKind)}</div>
          {row.isInherited ? (
            <div className="mt-1 text-xs text-slate-500" data-testid="facet-source-category">
              از «{sourceLabel || "دستهٔ والد"}»
            </div>
          ) : null}
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <FacetBadges row={row} locale={locale} />
          {isEdit && canEdit ? (
            <div className="flex flex-wrap gap-1">
              {row.isInherited ? (
                <button
                  type="button"
                  className="inline-flex min-h-9 items-center rounded-lg border border-gray-200 px-3 text-xs font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-40"
                  disabled={isBusy}
                  onClick={() => handleOverrideInherited(row)}
                  data-testid={`facet-override-${row.code}`}
                >
                  تنظیم برای این دسته
                </button>
              ) : (
                <>
                  <button
                    type="button"
                    className="inline-flex min-h-9 items-center rounded-lg border border-gray-200 px-3 text-xs font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-40"
                    disabled={isBusy}
                    onClick={() => {
                      setEditTarget(row);
                      setEditDraft(defaultFacetInput(row));
                    }}
                    data-testid={`facet-edit-${row.code}`}
                  >
                    تنظیم رفتار
                  </button>
                  {editableLocal ? (
                    <>
                      <button
                        type="button"
                        className="inline-flex min-h-9 min-w-9 items-center justify-center rounded-lg border border-gray-200 text-sm disabled:opacity-40"
                        disabled={isBusy || index === 0}
                        onClick={() => void handleMoveLocal(index, -1)}
                        aria-label="بالا"
                      >
                        ↑
                      </button>
                      <button
                        type="button"
                        className="inline-flex min-h-9 min-w-9 items-center justify-center rounded-lg border border-gray-200 text-sm disabled:opacity-40"
                        disabled={isBusy || index >= local.length - 1}
                        onClick={() => void handleMoveLocal(index, 1)}
                        aria-label="پایین"
                      >
                        ↓
                      </button>
                      <button
                        type="button"
                        className="inline-flex min-h-9 items-center rounded-lg border border-red-200 px-3 text-xs font-medium text-red-700 hover:bg-red-50 disabled:opacity-40"
                        disabled={isBusy}
                        onClick={() => void handleRemoveLocal(row)}
                        data-testid={`facet-remove-${row.code}`}
                      >
                        حذف تنظیم این دسته
                      </button>
                    </>
                  ) : null}
                </>
              )}
            </div>
          ) : null}
        </div>
      </li>
    );
  };

  return (
    <div className="space-y-6" data-testid="category-facets-panel">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 className="text-lg font-semibold text-slate-900">
            {locale === "en" ? "Product list filters" : "فیلترهای صفحه محصولات"}
          </h2>
          <p className="mt-1 text-sm text-slate-500" data-testid="facets-helper-copy">
            {locale === "en"
              ? "Choose which attributes customers can use to filter products on this category page."
              : "مشخص کنید مشتریان در صفحه این دسته بتوانند محصولات را بر اساس کدام ویژگی‌ها فیلتر کنند."}
          </p>
        </div>
      </div>

      {loading ? <p className="text-sm text-slate-500">در حال بارگذاری…</p> : null}
      {error ? <p className="text-sm text-red-600">{error}</p> : null}

      {!loading && !error ? (
        <>
          {isEdit && canEdit ? (
            <div className="flex flex-wrap gap-2">
              <button
                type="button"
                className="inline-flex min-h-10 items-center rounded-xl border border-gray-200 px-4 text-sm font-medium text-slate-800 hover:bg-slate-50 disabled:opacity-40"
                disabled={isBusy || eligibleToAdd.length === 0}
                onClick={() => {
                  setAddOpen(true);
                  setAddSearch("");
                  setSelectedDefId(null);
                  setAddDraft(null);
                }}
                data-testid="facet-add-button"
              >
                افزودن فیلتر
              </button>
            </div>
          ) : null}

          {inherited.length > 0 ? (
            <section>
              <h3 className="mb-2 text-sm font-semibold text-slate-800">فیلترهای ارث‌برده‌شده</h3>
              <ul className="space-y-2">{inherited.map((row, i) => renderRow(row, i, false))}</ul>
            </section>
          ) : null}

          {local.length > 0 ? (
            <section>
              <h3 className="mb-2 text-sm font-semibold text-slate-800">فیلترهای این دسته</h3>
              <ul className="space-y-2">{local.map((row, i) => renderRow(row, i, true))}</ul>
            </section>
          ) : null}

          {facets.length === 0 ? (
            <p className="rounded-xl border border-dashed border-gray-200 bg-slate-50 px-4 py-6 text-center text-sm text-slate-500">
              هنوز فیلتری پیکربندی نشده. ابتدا در تب ویژگی‌ها گزینه «فیلتر» را برای ویژگی موردنظر فعال کنید.
            </p>
          ) : null}
        </>
      ) : null}

      {addOpen && isEdit ? (
        <div className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-4 sm:items-center" role="dialog">
          <div className="max-h-[90vh] w-full max-w-lg overflow-auto rounded-2xl bg-white p-4 shadow-xl">
            <h3 className="text-base font-semibold text-slate-900">افزودن فیلتر</h3>
            <input
              className="mt-3 w-full rounded-lg border border-gray-200 px-3 py-2 text-sm"
              placeholder="جستجو…"
              value={addSearch}
              onChange={(e) => setAddSearch(e.target.value)}
              data-testid="facet-add-search"
            />
            <ul className="mt-3 max-h-48 space-y-1 overflow-auto">
              {filteredEligible.map((row) => (
                <li key={row.definitionId}>
                  <button
                    type="button"
                    className={`w-full rounded-lg px-3 py-2 text-right text-sm hover:bg-slate-50 ${selectedDefId === row.definitionId ? "bg-blue-50" : ""}`}
                    onClick={() => {
                      setSelectedDefId(row.definitionId);
                      setAddDraft(defaultFacetInput(undefined, row.valueKind));
                    }}
                  >
                    {humanizeAttributeCode(row.code)} · {valueKindLabel(row.valueKind)}
                  </button>
                </li>
              ))}
            </ul>
            {selectedDefId && addDraft ? (
              <div className="mt-3">
                <FacetConfigEditor
                  draft={addDraft}
                  valueKind={schema.find((s) => s.definitionId === selectedDefId)?.valueKind ?? "Enumeration"}
                  onChange={setAddDraft}
                  locale={locale}
                />
              </div>
            ) : null}
            <div className="mt-4 flex justify-end gap-2">
              <button type="button" className="rounded-lg border px-4 py-2 text-sm" onClick={() => setAddOpen(false)}>
                انصراف
              </button>
              <button
                type="button"
                className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white disabled:opacity-40"
                disabled={!selectedDefId || !addDraft || isBusy}
                onClick={() => selectedDefId && addDraft && void handleSaveFacet(selectedDefId, addDraft)}
                data-testid="facet-add-save"
              >
                ذخیره
              </button>
            </div>
          </div>
        </div>
      ) : null}

      {editTarget && editDraft ? (
        <div className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-4 sm:items-center" role="dialog">
          <div className="w-full max-w-lg rounded-2xl bg-white p-4 shadow-xl">
            <h3 className="text-base font-semibold text-slate-900">
              تنظیم فیلتر — {editTarget.localizedName || humanizeAttributeCode(editTarget.code)}
            </h3>
            <div className="mt-3">
              <FacetConfigEditor
                draft={editDraft}
                valueKind={editTarget.valueKind}
                onChange={setEditDraft}
                locale={locale}
              />
            </div>
            <div className="mt-4 flex justify-end gap-2">
              <button
                type="button"
                className="rounded-lg border px-4 py-2 text-sm"
                onClick={() => {
                  setEditTarget(null);
                  setEditDraft(null);
                }}
              >
                انصراف
              </button>
              <button
                type="button"
                className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white disabled:opacity-40"
                disabled={isBusy}
                onClick={() => void handleSaveFacet(editTarget.definitionId, editDraft)}
                data-testid="facet-edit-save"
              >
                ذخیره
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}
