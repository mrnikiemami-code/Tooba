"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Plus, Trash2 } from "lucide-react";
import {
  AppDataGrid,
  adminGridQueryAdapter,
  createClientGridQueryAdapter,
  formatJalaliDate,
  useLegacyAdminGridDirectProps,
} from "../../../design-system";
import type { GridBulkAction, GridColumnDef, GridServerQuery } from "../../../design-system/data-grid";
import { AppGridRowActionsCell, type AppGridRowAction } from "../../../design-system/app-data-grid/app-grid-row-actions";
import { DEFAULT_APP_GRID_CAPABILITIES } from "../../../design-system/app-data-grid/app-grid-capabilities";
import { createHostSavedViewStore } from "../saved-view-store";
import {
  listAdminBrandOptions,
  queryAdminProductGrid,
  type AdminProductListRow,
} from "../host-client";
import { loadCategoryTree } from "../catalog-category-api";
import {
  queryAdminContentArticlesGrid,
  type AdminContentArticle,
} from "../../content/content-api.ts";
import { storefrontMediaUrl } from "../../storefront/storefront-api";
import { formatAdminStatus } from "../admin-api";
import type { ResourceFamily } from "../../../lib/storefront-composition/source-capability.ts";

type ResourceSelectorProps = {
  family: Exclude<ResourceFamily, "none">;
  selectedIds: string[];
  onChange: (ids: string[], labels?: Record<string, string>) => void;
  multiSelect?: boolean;
  maxCount?: number;
  open?: boolean;
  onClose?: () => void;
  title?: string;
};

type BrandRow = { id: string; name: string; status: string };
type CategoryRow = { id: string; name: string; status: string };
type LabeledRow = { id: string; label: string };

const ORDERS_LIKE_CAPABILITIES = {
  ...DEFAULT_APP_GRID_CAPABILITIES,
  csvExport: false,
  excelExport: false,
};

const SINGLE_SELECT_CAPABILITIES = {
  ...ORDERS_LIKE_CAPABILITIES,
  rowSelection: false,
};

function gridCapabilities(multiSelect: boolean) {
  return multiSelect ? ORDERS_LIKE_CAPABILITIES : SINGLE_SELECT_CAPABILITIES;
}

function buildSelectionRowActions<T extends { id: string }>(
  tab: "all" | "selected",
  selectedSet: Set<string>,
  getLabel: (row: T) => string,
  onToggle: (id: string, label: string) => void,
): AppGridRowAction<T>[] {
  if (tab === "selected") {
    return [
      {
        id: "remove",
        label: "حذف",
        icon: Trash2,
        variant: "destructive",
        onClick: (row) => onToggle(row.id, getLabel(row)),
      },
    ];
  }
  return [
    {
      id: "add",
      label: "افزودن",
      icon: Plus,
      onClick: (row) => onToggle(row.id, getLabel(row)),
      visible: (row) => !selectedSet.has(row.id),
    },
    {
      id: "remove",
      label: "حذف از انتخاب",
      icon: Trash2,
      variant: "destructive",
      onClick: (row) => onToggle(row.id, getLabel(row)),
      visible: (row) => selectedSet.has(row.id),
    },
  ];
}

/**
 * Shared Resource Selector — Orders-grid standard (AppDataGrid canonical profile).
 * Selection persists across pages/filters via external selected map + selected-items tab.
 */
export function AdminResourceSelector({
  family,
  selectedIds,
  onChange,
  multiSelect = true,
  maxCount = 24,
  open = true,
  onClose,
  title,
}: ResourceSelectorProps) {
  const [tab, setTab] = useState<"all" | "selected">("all");
  const [labels, setLabels] = useState<Record<string, string>>({});
  const [reloadToken, setReloadToken] = useState(0);
  const selectedSet = useMemo(() => new Set(selectedIds), [selectedIds]);

  const mergeLabels = useCallback((rows: Array<{ id: string; label: string }>) => {
    setLabels((current) => {
      const next = { ...current };
      for (const row of rows) next[row.id] = row.label;
      return next;
    });
  }, []);

  const toggleId = useCallback(
    (id: string, label: string) => {
      if (selectedSet.has(id)) {
        onChange(selectedIds.filter((item) => item !== id), labels);
        return;
      }
      if (!multiSelect) {
        onChange([id], { ...labels, [id]: label });
        return;
      }
      if (selectedIds.length >= maxCount) return;
      onChange([...selectedIds, id], { ...labels, [id]: label });
    },
    [labels, maxCount, multiSelect, onChange, selectedIds, selectedSet],
  );

  const addMany = useCallback(
    (items: LabeledRow[]) => {
      const nextLabels = { ...labels };
      for (const item of items) nextLabels[item.id] = item.label;
      if (!multiSelect) {
        const first = items[0];
        if (!first) return;
        setLabels(nextLabels);
        onChange([first.id], nextLabels);
        return;
      }
      const nextIds = [...selectedIds];
      for (const item of items) {
        if (nextIds.includes(item.id)) continue;
        if (nextIds.length >= maxCount) break;
        nextIds.push(item.id);
      }
      setLabels(nextLabels);
      onChange(nextIds, nextLabels);
    },
    [labels, maxCount, multiSelect, onChange, selectedIds],
  );

  const removeMany = useCallback(
    (ids: string[]) => {
      const drop = new Set(ids);
      onChange(selectedIds.filter((id) => !drop.has(id)), labels);
    },
    [labels, onChange, selectedIds],
  );

  const clearAll = useCallback(() => {
    onChange([]);
  }, [onChange]);

  const titleFa =
    title
    ?? (family === "products"
      ? "انتخاب کالا"
      : family === "articles"
        ? "انتخاب مطلب"
        : family === "brands"
          ? "انتخاب برند"
          : "انتخاب دسته");

  if (!open) return null;

  return (
    <div
      className="fixed inset-0 z-[120] flex items-end justify-center bg-black/40 p-3 sm:items-center"
      role="dialog"
      aria-modal="true"
      data-testid="admin-resource-selector"
      data-resource-family={family}
      data-grid-profile="orders-canonical"
      data-selector-tab={tab}
      data-multi-select={multiSelect ? "true" : "false"}
    >
      <div className="flex max-h-[92vh] w-full max-w-6xl flex-col overflow-hidden rounded-2xl bg-white shadow-xl">
        <div className="border-b border-border px-5 py-4">
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <h3 className="text-base font-black text-slate-900">{titleFa}</h3>
              <p className="mt-1 text-sm text-muted">
                {multiSelect
                  ? "فیلتر ستون، فیلتر پیشرفته، صفحه‌بندی و انتخاب چندصفحه‌ای — استاندارد فهرست سفارش‌ها"
                  : "فقط یک مورد قابل انتخاب است. با انتخاب مورد جدید، انتخاب قبلی جایگزین می‌شود."}
              </p>
            </div>
            <button type="button" className="rounded-xl border px-3 py-2 text-sm" onClick={onClose} data-testid="resource-selector-close">
              بستن
            </button>
          </div>
          <div className="mt-3 flex flex-wrap items-center gap-2" role="tablist">
            <button
              type="button"
              role="tab"
              aria-selected={tab === "all"}
              className={tab === "all" ? "rounded-full bg-[#2563EB] px-3 py-1.5 text-xs font-semibold text-white" : "rounded-full border px-3 py-1.5 text-xs"}
              onClick={() => { setTab("all"); setReloadToken((n) => n + 1); }}
              data-testid="resource-selector-tab-all"
            >
              همه
            </button>
            <button
              type="button"
              role="tab"
              aria-selected={tab === "selected"}
              className={tab === "selected" ? "rounded-full bg-[#2563EB] px-3 py-1.5 text-xs font-semibold text-white" : "rounded-full border px-3 py-1.5 text-xs"}
              onClick={() => { setTab("selected"); setReloadToken((n) => n + 1); }}
              data-testid="resource-selector-tab-selected"
            >
              انتخاب‌شده‌ها ({selectedIds.length.toLocaleString("fa-IR")})
            </button>
            {tab === "selected" ? (
              <button
                type="button"
                className="ms-auto rounded-xl border border-red-300 px-3 py-1.5 text-xs font-bold text-red-700 disabled:opacity-40"
                disabled={selectedIds.length === 0}
                onClick={clearAll}
                data-testid="resource-selector-clear-all"
              >
                حذف همه
              </button>
            ) : (
              <span className="ms-auto" />
            )}
            <button
              type="button"
              className="rounded-xl bg-[#2563EB] px-3 py-1.5 text-xs font-bold text-white"
              onClick={onClose}
              data-testid="resource-selector-confirm"
            >
              تأیید انتخاب
            </button>
          </div>
        </div>
        <div className="min-h-0 flex-1 overflow-auto p-3" data-testid="resource-selector-grid">
          {family === "products" ? (
            <ProductResourceGrid
              tab={tab}
              selectedIds={selectedIds}
              selectedSet={selectedSet}
              reloadToken={reloadToken}
              multiSelect={multiSelect}
              onToggle={toggleId}
              onAddMany={addMany}
              onRemoveMany={removeMany}
              onLabels={mergeLabels}
            />
          ) : null}
          {family === "articles" ? (
            <ArticleResourceGrid
              tab={tab}
              selectedIds={selectedIds}
              selectedSet={selectedSet}
              reloadToken={reloadToken}
              multiSelect={multiSelect}
              onToggle={toggleId}
              onAddMany={addMany}
              onRemoveMany={removeMany}
              onLabels={mergeLabels}
            />
          ) : null}
          {family === "brands" ? (
            <BrandResourceGrid
              tab={tab}
              selectedSet={selectedSet}
              reloadToken={reloadToken}
              multiSelect={multiSelect}
              onToggle={toggleId}
              onAddMany={addMany}
              onRemoveMany={removeMany}
              onLabels={mergeLabels}
            />
          ) : null}
          {family === "categories" ? (
            <CategoryResourceGrid
              tab={tab}
              selectedSet={selectedSet}
              reloadToken={reloadToken}
              multiSelect={multiSelect}
              onToggle={toggleId}
              onAddMany={addMany}
              onRemoveMany={removeMany}
              onLabels={mergeLabels}
            />
          ) : null}
        </div>
        {selectedIds.length > 0 ? (
          <div className="border-t border-border px-5 py-3" data-testid="resource-selector-selected-summary">
            <p className="mb-2 text-xs font-bold text-muted">انتخاب‌شده‌ها</p>
            <div className="flex flex-wrap gap-2">
              {selectedIds.map((id) => (
                <span key={id} className="inline-flex items-center gap-2 rounded-full bg-slate-100 px-3 py-1 text-xs">
                  {labels[id] ?? "مورد انتخاب‌شده"}
                  <button type="button" onClick={() => toggleId(id, labels[id] ?? id)} aria-label="حذف">×</button>
                </span>
              ))}
            </div>
          </div>
        ) : null}
      </div>
    </div>
  );
}

function truncated(value: string) {
  return (
    <span className="block truncate" title={value}>{value || "—"}</span>
  );
}

/** Exclude already-selected ids from the All-tab server query (SQL NOT IN). */
function withExcludedIds(
  query: GridServerQuery,
  field: "productId" | "articleId",
  excludeIds: readonly string[],
): GridServerQuery {
  if (excludeIds.length === 0) return query;
  return {
    ...query,
    filters: {
      ...query.filters,
      [field]: { kind: "enum", operator: "notIn", values: [...excludeIds] },
    },
  };
}

/** In-memory All/Selected split with O(1) membership via Set. */
function partitionBySelection<T extends { id: string }>(
  rows: readonly T[],
  tab: "all" | "selected",
  selectedSet: Set<string>,
): T[] {
  if (tab === "selected") {
    if (selectedSet.size === 0) return [];
    return rows.filter((row) => selectedSet.has(row.id));
  }
  if (selectedSet.size === 0) return rows as T[];
  return rows.filter((row) => !selectedSet.has(row.id));
}

function selectionBulkActions<T extends { id: string }>(
  tab: "all" | "selected",
  getLabel: (row: T) => string,
  onAddMany: (items: LabeledRow[]) => void,
  onRemoveMany: (ids: string[]) => void,
): GridBulkAction<T>[] {
  if (tab === "selected") {
    return [
      {
        id: "remove-checked",
        label: "حذف انتخاب‌شده‌ها",
        requiresConfirmation: false,
        isAvailable: (rows) => rows.length > 0,
        execute: async (rows) => {
          onRemoveMany(rows.map((row) => row.id));
          return { ok: true, message: "" };
        },
      },
    ];
  }
  return [
    {
      id: "add-checked",
      label: "افزودن به انتخاب‌شده‌ها",
      requiresConfirmation: false,
      isAvailable: (rows) => rows.length > 0,
      execute: async (rows) => {
        onAddMany(rows.map((row) => ({ id: row.id, label: getLabel(row) })));
        return { ok: true, message: "" };
      },
    },
  ];
}

function ProductResourceGrid({
  tab,
  selectedIds,
  selectedSet,
  reloadToken,
  multiSelect,
  onToggle,
  onAddMany,
  onRemoveMany,
  onLabels,
}: {
  tab: "all" | "selected";
  selectedIds: string[];
  selectedSet: Set<string>;
  reloadToken: number;
  multiSelect: boolean;
  onToggle: (id: string, label: string) => void;
  onAddMany: (items: LabeledRow[]) => void;
  onRemoveMany: (ids: string[]) => void;
  onLabels: (rows: Array<{ id: string; label: string }>) => void;
}) {
  const savedViewStore = useMemo(() => createHostSavedViewStore("grid.admin.resource.products"), []);
  const actions = useMemo(
    () => buildSelectionRowActions<AdminProductListRow>(tab, selectedSet, (row) => row.title, onToggle),
    [onToggle, selectedSet, tab],
  );
  const bulkActions = useMemo(
    () => (multiSelect ? selectionBulkActions<AdminProductListRow>(tab, (row) => row.title, onAddMany, onRemoveMany) : []),
    [multiSelect, onAddMany, onRemoveMany, tab],
  );

  const columns = useMemo(
    (): GridColumnDef<AdminProductListRow>[] => [
      {
        id: "media",
        header: "تصویر",
        accessor: (row) => row.primaryMediaAssetId ?? "",
        cell: (row) => (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={row.primaryMediaAssetId ? storefrontMediaUrl(row.primaryMediaAssetId) : undefined}
            alt=""
            className="h-10 w-10 rounded-lg object-cover bg-slate-100"
          />
        ),
        width: 72,
        minWidth: 64,
        maxWidth: 80,
        filterable: false,
        sortable: false,
        exportable: false,
      },
      {
        id: "title",
        header: "عنوان کالا",
        accessor: (row) => row.title,
        cell: (row) => truncated(row.title),
        width: 220,
        minWidth: 160,
        filterKind: "text",
        sortable: true,
      },
      {
        id: "primaryCategoryName",
        header: "دسته اصلی",
        accessor: (row) => row.primaryCategoryName ?? "",
        cell: (row) => truncated(row.primaryCategoryName ?? "—"),
        width: 160,
        minWidth: 120,
        filterKind: "text",
        sortable: true,
      },
      {
        id: "additionalCategoryNames",
        header: "نمایش در دسته‌های دیگر",
        accessor: (row) => row.additionalCategoryNames.join("، "),
        cell: (row) => truncated(row.additionalCategoryNames.join("، ") || "—"),
        width: 180,
        minWidth: 140,
        filterKind: "text",
      },
      {
        id: "status",
        header: "وضعیت",
        accessor: (row) => row.status,
        cell: (row) => formatAdminStatus(row.status),
        width: 120,
        minWidth: 100,
        filterKind: "status",
        sortable: true,
      },
      {
        id: "actions",
        header: "عملیات",
        accessor: () => "",
        cell: (row) => <AppGridRowActionsCell row={row} actions={actions} compact />,
        width: 88,
        minWidth: 80,
        exportable: false,
        sortable: false,
      },
    ],
    [actions],
  );

  const queryAdapter = useCallback(
    async (query: GridServerQuery) => {
      void reloadToken;
      if (tab === "selected") {
        if (selectedIds.length === 0) return { rows: [], total: 0 };
        const result = await queryAdminProductGrid({
          ...query,
          page: 1,
          pageSize: Math.max(selectedIds.length, query.pageSize),
          filters: {
            ...query.filters,
            productId: { kind: "enum", operator: "in", values: selectedIds },
          },
        });
        if (result.source === "error" || result.denied) throw new Error(result.message ?? "error");
        const rows = result.page.rows.filter((row) => selectedSet.has(row.id));
        onLabels(rows.map((row) => ({ id: row.id, label: row.title })));
        return { rows, total: rows.length };
      }
      const result = await adminGridQueryAdapter(queryAdminProductGrid)(
        withExcludedIds(query, "productId", selectedIds),
      );
      onLabels(result.rows.map((row) => ({ id: row.id, label: row.title })));
      return result;
    },
    [onLabels, reloadToken, selectedIds, selectedSet, tab],
  );

  const gridProps = useLegacyAdminGridDirectProps({
    gridId: "grid.admin.resource.products",
    columns,
    queryAdapter,
    savedViewStore,
  });

  return (
    <AppDataGrid<AdminProductListRow>
      {...gridProps}
      capabilities={gridCapabilities(multiSelect)}
      bulkActions={bulkActions}
      rowCountNoun={{ fa: "کالا", en: "products" }}
      messageOverrides={{
        advancedFilterTitle: "فیلتر پیشرفته کالاها",
        advancedFilterSubtitle: "جستجوی دقیق مانند فهرست سفارش‌ها",
      }}
    />
  );
}

function ArticleResourceGrid({
  tab,
  selectedIds,
  selectedSet,
  reloadToken,
  multiSelect,
  onToggle,
  onAddMany,
  onRemoveMany,
  onLabels,
}: {
  tab: "all" | "selected";
  selectedIds: string[];
  selectedSet: Set<string>;
  reloadToken: number;
  multiSelect: boolean;
  onToggle: (id: string, label: string) => void;
  onAddMany: (items: LabeledRow[]) => void;
  onRemoveMany: (ids: string[]) => void;
  onLabels: (rows: Array<{ id: string; label: string }>) => void;
}) {
  const savedViewStore = useMemo(() => createHostSavedViewStore("grid.admin.resource.articles"), []);
  const actions = useMemo(
    () => buildSelectionRowActions<AdminContentArticle>(tab, selectedSet, (row) => row.title, onToggle),
    [onToggle, selectedSet, tab],
  );
  const bulkActions = useMemo(
    () => (multiSelect ? selectionBulkActions<AdminContentArticle>(tab, (row) => row.title, onAddMany, onRemoveMany) : []),
    [multiSelect, onAddMany, onRemoveMany, tab],
  );

  const columns = useMemo(
    (): GridColumnDef<AdminContentArticle>[] => [
      {
        id: "title",
        header: "عنوان",
        accessor: (row) => row.title,
        cell: (row) => truncated(row.title),
        width: 220,
        minWidth: 160,
        filterKind: "text",
        sortable: true,
      },
      {
        id: "category",
        header: "دسته",
        accessor: (row) => row.category ?? "",
        cell: (row) => truncated(row.category ?? "—"),
        width: 140,
        minWidth: 110,
        filterKind: "text",
        sortable: true,
      },
      {
        id: "authorDisplayName",
        header: "نویسنده",
        accessor: (row) => row.authorDisplayName,
        cell: (row) => truncated(row.authorDisplayName || "—"),
        width: 140,
        minWidth: 110,
        filterKind: "text",
        sortable: true,
      },
      {
        id: "status",
        header: "وضعیت",
        accessor: (row) => row.status,
        cell: (row) => formatAdminStatus(row.status),
        width: 120,
        minWidth: 100,
        filterKind: "status",
        sortable: true,
      },
      {
        id: "publishDate",
        header: "تاریخ انتشار",
        accessor: (row) => row.publishDate,
        cell: (row) => (row.publishDate ? formatJalaliDate(row.publishDate) : "—"),
        width: 140,
        minWidth: 120,
        filterKind: "date",
        sortable: true,
      },
      {
        id: "actions",
        header: "عملیات",
        accessor: () => "",
        cell: (row) => <AppGridRowActionsCell row={row} actions={actions} compact />,
        width: 88,
        minWidth: 80,
        exportable: false,
        sortable: false,
      },
    ],
    [actions],
  );

  const queryAdapter = useCallback(
    async (query: GridServerQuery) => {
      void reloadToken;
      if (tab === "selected") {
        if (selectedIds.length === 0) return { rows: [] as AdminContentArticle[], total: 0 };
        const result = await queryAdminContentArticlesGrid({
          ...query,
          page: 1,
          pageSize: Math.max(selectedIds.length, query.pageSize),
          filters: {
            ...query.filters,
            articleId: { kind: "enum", operator: "in", values: selectedIds },
          },
        });
        if (result.denied || result.source === "error") throw new Error(result.message ?? "error");
        const rows = result.page.rows.filter((row) => selectedSet.has(row.id));
        onLabels(rows.map((row) => ({ id: row.id, label: row.title })));
        return { rows, total: rows.length };
      }
      const result = await adminGridQueryAdapter(queryAdminContentArticlesGrid)(
        withExcludedIds(query, "articleId", selectedIds),
      );
      onLabels(result.rows.map((row) => ({ id: row.id, label: row.title })));
      return result;
    },
    [onLabels, reloadToken, selectedIds, selectedSet, tab],
  );

  const gridProps = useLegacyAdminGridDirectProps({
    gridId: "grid.admin.resource.articles",
    columns,
    queryAdapter,
    savedViewStore,
  });

  return (
    <AppDataGrid<AdminContentArticle>
      {...gridProps}
      capabilities={gridCapabilities(multiSelect)}
      bulkActions={bulkActions}
      rowCountNoun={{ fa: "مطلب", en: "articles" }}
    />
  );
}

function BrandResourceGrid({
  tab,
  selectedSet,
  reloadToken,
  multiSelect,
  onToggle,
  onAddMany,
  onRemoveMany,
  onLabels,
}: {
  tab: "all" | "selected";
  selectedSet: Set<string>;
  reloadToken: number;
  multiSelect: boolean;
  onToggle: (id: string, label: string) => void;
  onAddMany: (items: LabeledRow[]) => void;
  onRemoveMany: (ids: string[]) => void;
  onLabels: (rows: Array<{ id: string; label: string }>) => void;
}) {
  const [rows, setRows] = useState<BrandRow[]>([]);
  const savedViewStore = useMemo(() => createHostSavedViewStore("grid.admin.resource.brands"), []);
  useEffect(() => {
    void listAdminBrandOptions().then((result) => {
      if (!result.ok) return;
      const mapped = result.items.map((item) => ({ id: item.brandId, name: item.name, status: "Active" }));
      setRows(mapped);
      onLabels(mapped.map((row) => ({ id: row.id, label: row.name })));
    });
  }, [onLabels, reloadToken]);

  const visible = useMemo(
    () => partitionBySelection(rows, tab, selectedSet),
    [rows, selectedSet, tab],
  );
  const actions = useMemo(
    () => buildSelectionRowActions<BrandRow>(tab, selectedSet, (row) => row.name, onToggle),
    [onToggle, selectedSet, tab],
  );
  const bulkActions = useMemo(
    () => (multiSelect ? selectionBulkActions<BrandRow>(tab, (row) => row.name, onAddMany, onRemoveMany) : []),
    [multiSelect, onAddMany, onRemoveMany, tab],
  );
  const columns = useMemo(
    (): GridColumnDef<BrandRow>[] => [
      {
        id: "name",
        header: "نام برند",
        accessor: (row) => row.name,
        cell: (row) => truncated(row.name),
        width: 260,
        minWidth: 180,
        filterKind: "text",
        sortable: true,
      },
      {
        id: "actions",
        header: "عملیات",
        accessor: () => "",
        cell: (row) => <AppGridRowActionsCell row={row} actions={actions} compact />,
        width: 88,
        minWidth: 80,
        exportable: false,
      },
    ],
    [actions],
  );
  const queryAdapter = useCallback(
    async (query: GridServerQuery) => createClientGridQueryAdapter(visible, columns)(query),
    [columns, visible],
  );
  const gridProps = useLegacyAdminGridDirectProps({
    gridId: "grid.admin.resource.brands",
    columns,
    queryAdapter,
    savedViewStore,
  });
  return (
    <AppDataGrid<BrandRow>
      {...gridProps}
      capabilities={gridCapabilities(multiSelect)}
      bulkActions={bulkActions}
      rowCountNoun={{ fa: "برند", en: "brands" }}
    />
  );
}

function CategoryResourceGrid({
  tab,
  selectedSet,
  reloadToken,
  multiSelect,
  onToggle,
  onAddMany,
  onRemoveMany,
  onLabels,
}: {
  tab: "all" | "selected";
  selectedSet: Set<string>;
  reloadToken: number;
  multiSelect: boolean;
  onToggle: (id: string, label: string) => void;
  onAddMany: (items: LabeledRow[]) => void;
  onRemoveMany: (ids: string[]) => void;
  onLabels: (rows: Array<{ id: string; label: string }>) => void;
}) {
  const [rows, setRows] = useState<CategoryRow[]>([]);
  const savedViewStore = useMemo(() => createHostSavedViewStore("grid.admin.resource.categories"), []);
  useEffect(() => {
    void loadCategoryTree("fa-IR").then((result) => {
      if (result.state !== "ok" || !result.data) return;
      const mapped = result.data
        .filter((node) => node.status !== "Archived")
        .map((node) => ({ id: node.id, name: node.name, status: node.status }));
      setRows(mapped);
      onLabels(mapped.map((row) => ({ id: row.id, label: row.name })));
    });
  }, [onLabels, reloadToken]);

  const visible = useMemo(
    () => partitionBySelection(rows, tab, selectedSet),
    [rows, selectedSet, tab],
  );
  const actions = useMemo(
    () => buildSelectionRowActions<CategoryRow>(tab, selectedSet, (row) => row.name, onToggle),
    [onToggle, selectedSet, tab],
  );
  const bulkActions = useMemo(
    () => (multiSelect ? selectionBulkActions<CategoryRow>(tab, (row) => row.name, onAddMany, onRemoveMany) : []),
    [multiSelect, onAddMany, onRemoveMany, tab],
  );
  const columns = useMemo(
    (): GridColumnDef<CategoryRow>[] => [
      {
        id: "name",
        header: "نام دسته",
        accessor: (row) => row.name,
        cell: (row) => truncated(row.name),
        width: 260,
        minWidth: 180,
        filterKind: "text",
        sortable: true,
      },
      {
        id: "status",
        header: "وضعیت",
        accessor: (row) => row.status,
        width: 120,
        minWidth: 100,
        filterKind: "status",
        sortable: true,
      },
      {
        id: "actions",
        header: "عملیات",
        accessor: () => "",
        cell: (row) => <AppGridRowActionsCell row={row} actions={actions} compact />,
        width: 88,
        minWidth: 80,
        exportable: false,
      },
    ],
    [actions],
  );
  const queryAdapter = useCallback(
    async (query: GridServerQuery) => createClientGridQueryAdapter(visible, columns)(query),
    [columns, visible],
  );
  const gridProps = useLegacyAdminGridDirectProps({
    gridId: "grid.admin.resource.categories",
    columns,
    queryAdapter,
    savedViewStore,
  });
  return (
    <AppDataGrid<CategoryRow>
      {...gridProps}
      capabilities={gridCapabilities(multiSelect)}
      bulkActions={bulkActions}
      rowCountNoun={{ fa: "دسته", en: "categories" }}
    />
  );
}

/** Compact inline trigger showing selected count + open selector. */
export function ResourceSelectorTrigger({
  count,
  onOpen,
  emptyHint,
  emptyTestId,
}: {
  count: number;
  onOpen: () => void;
  emptyHint: string;
  emptyTestId?: string;
}) {
  return (
    <div className="space-y-2" data-testid="resource-selector-trigger">
      {count === 0 ? (
        <p
          className="rounded-xl border border-dashed px-3 py-2 text-xs text-muted"
          data-testid={emptyTestId}
        >
          {emptyHint}
        </p>
      ) : (
        <p className="text-sm font-bold">{count.toLocaleString("fa-IR")} مورد انتخاب شده</p>
      )}
      <button
        type="button"
        className="rounded-xl border border-[#2563EB] px-4 py-2 text-sm font-bold text-[#2563EB]"
        onClick={onOpen}
        data-testid="resource-selector-open"
      >
        {count === 0 ? "باز کردن فهرست انتخاب" : "ویرایش انتخاب"}
      </button>
    </div>
  );
}
