"use client";

/**
 * تب محصولات Category Workspace — فهرست و اختصاص از همان رابطهٔ canonical Product↔Category.
 */

import { useCallback, useMemo, useState } from "react";
import type { ColDef, ICellRendererParams } from "ag-grid-community";
import { Edit2, Eye } from "lucide-react";
import { formatJalaliDate, AppDataGrid } from "../../design-system/app-data-grid";
import type { AppCategoryTreeNode } from "../../design-system/app-category-tree";
import {
  AppGridBadgeCell,
  AppGridLinkSubtitleCell,
  AppGridMediaCell,
} from "../../design-system/app-data-grid/app-grid-cells";
import { buildPinnedActionsColumnDef } from "../../design-system/app-data-grid/app-grid-pinned-actions";
import { AppGridRowActionsCell, type AppGridRowAction } from "../../design-system/app-data-grid/app-grid-row-actions";
import type { GridServerQuery } from "../../design-system/data-grid";
import { formatAdminStatus } from "./admin-api";
import { mapAdminErrorMessage } from "./admin-error-map";
import { resolveAdminChromeLocale } from "./admin-chrome-messages";
import { previewProductCategoryChange } from "./catalog-attribute-api";
import {
  assignAdminProductCategory,
  loadProductWorkspace,
  queryAdminProductGrid,
  type AdminProductListRow,
} from "./host-client";
import { ProductCategoryPicker } from "./product-category-picker";
import {
  getCategoryLevel,
  isAssignableProductCategory,
} from "./product-category-level";
import { storefrontMediaUrl } from "../storefront/storefront-api";

export const CATEGORY_PRODUCTS_LEVEL_BLOCKED_MESSAGE_FA =
  "محصول فقط به دسته‌بندی سطح سوم قابل اختصاص است.";

function productStatusClass(status: string): string {
  if (status === "Published") return "inline-flex rounded-full bg-success/15 px-2.5 py-1 text-xs font-medium text-success";
  if (status === "Archived") return "inline-flex rounded-full bg-secondary px-2.5 py-1 text-xs font-medium text-muted";
  return "inline-flex rounded-full bg-warning/15 px-2.5 py-1 text-xs font-medium text-warning";
}

function MediaCell(params: ICellRendererParams<AdminProductListRow>) {
  const row = params.data;
  if (!row) return null;
  const thumb = row.primaryMediaAssetId ? storefrontMediaUrl(row.primaryMediaAssetId) : null;
  return <AppGridMediaCell imageUrl={thumb} />;
}

function ProductCell(params: ICellRendererParams<AdminProductListRow>) {
  const row = params.data;
  if (!row) return null;
  return (
    <AppGridLinkSubtitleCell
      params={params}
      href={`/admin/products/${row.id}?scope=view`}
      title={row.title}
      subtitle=""
    />
  );
}

function StatusCell(params: ICellRendererParams<AdminProductListRow>) {
  const label = formatAdminStatus(String(params.value ?? ""));
  return <AppGridBadgeCell params={params} label={label} className={productStatusClass(String(params.value ?? ""))} />;
}

/**
 * پنل محصولات اختصاص‌یافته به یک دسته — برای سطح ۱/۲ فقط توضیح؛ سطح ۳ فهرست + اختصاص.
 */
export function CategoryProductsPanel({
  categoryId,
  categoryName,
  treeNodes,
  canEdit,
}: {
  categoryId: string;
  categoryName: string;
  treeNodes: AppCategoryTreeNode[];
  canEdit: boolean;
}) {
  const locale = resolveAdminChromeLocale();
  const assignable = isAssignableProductCategory(treeNodes, categoryId);
  const level = getCategoryLevel(treeNodes, categoryId);
  const [reloadToken, setReloadToken] = useState(0);
  const [gridError, setGridError] = useState<string | null>(null);
  const [assignOpen, setAssignOpen] = useState(false);
  const [assignSearch, setAssignSearch] = useState("");
  const [assignCandidates, setAssignCandidates] = useState<AdminProductListRow[]>([]);
  const [assignBusy, setAssignBusy] = useState(false);
  const [assignError, setAssignError] = useState<string | null>(null);
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [changeRow, setChangeRow] = useState<AdminProductListRow | null>(null);
  const [changeCategoryId, setChangeCategoryId] = useState<string | null>(null);
  const [changeBusy, setChangeBusy] = useState(false);
  const [changeError, setChangeError] = useState<string | null>(null);

  const toUiError = useCallback(
    (raw: string | null | undefined) => mapAdminErrorMessage(raw, locale),
    [locale],
  );

  const onChangeCategory = useCallback((row: AdminProductListRow) => {
    setChangeRow(row);
    setChangeCategoryId(null);
    setChangeError(null);
  }, []);

  const rowActions = useMemo((): AppGridRowAction<AdminProductListRow>[] => {
    const actions: AppGridRowAction<AdminProductListRow>[] = [
      {
        id: "view",
        label: "مشاهده محصول",
        icon: Eye,
        href: (row) => `/admin/products/${row.id}?scope=view`,
        testId: (row) => `category-product-view-${row.id}`,
      },
    ];
    if (canEdit && assignable) {
      actions.push({
        id: "change-category",
        label: "تغییر دسته‌بندی",
        icon: Edit2,
        onClick: (row) => onChangeCategory(row),
        testId: (row) => `category-product-change-${row.id}`,
      });
    }
    return actions;
  }, [assignable, canEdit, onChangeCategory]);

  const columnDefs = useMemo(
    (): ColDef<AdminProductListRow>[] => [
      {
        colId: "media",
        headerName: "رسانه",
        width: 88,
        minWidth: 80,
        sortable: false,
        filter: false,
        cellRenderer: MediaCell,
      },
      {
        field: "title",
        headerName: "نام محصول",
        minWidth: 200,
        flex: 1.4,
        cellRenderer: ProductCell,
      },
      {
        field: "status",
        headerName: "وضعیت",
        width: 120,
        cellRenderer: StatusCell,
      },
      {
        field: "categorySummary",
        headerName: "مسیر دسته",
        minWidth: 160,
        flex: 1,
      },
      {
        field: "updatedAt",
        headerName: "به‌روزرسانی",
        width: 120,
        valueFormatter: (p) => formatJalaliDate(String(p.value ?? ""), "fa"),
      },
      buildPinnedActionsColumnDef<AdminProductListRow>({
        direction: "rtl",
        cellRenderer: (params: ICellRendererParams<AdminProductListRow>) =>
          params.data ? <AppGridRowActionsCell row={params.data} actions={rowActions} /> : null,
      }),
    ],
    [rowActions],
  );

  const queryAdapter = useCallback(
    async (query: GridServerQuery) => {
      const merged: GridServerQuery = {
        ...query,
        filters: {
          ...query.filters,
          categorySummary: {
            kind: "text",
            operator: "equals",
            query: categoryName.trim(),
          },
        },
      };
      const result = await queryAdminProductGrid(merged);
      void reloadToken;
      if (result.denied) {
        setGridError(toUiError("admin.authorization.denied"));
        throw new Error(result.message);
      }
      if (result.source === "error") {
        setGridError(toUiError(result.message));
        throw new Error(result.message ?? "host-unreachable");
      }
      setGridError(null);
      return result.page;
    },
    [categoryName, reloadToken, toUiError],
  );

  const searchAssignCandidates = async () => {
    setAssignBusy(true);
    setAssignError(null);
    const result = await queryAdminProductGrid({
      page: 1,
      pageSize: 30,
      sorts: [{ columnId: "updatedAt", direction: "desc" }],
      filters: {},
      search: assignSearch.trim() || undefined,
    });
    setAssignBusy(false);
    if (result.source === "error") {
      setAssignError(toUiError(result.message));
      setAssignCandidates([]);
      return;
    }
    const rows = result.page.rows.filter(
      (r) => r.categorySummary !== categoryName && !r.categorySummary.endsWith(` / ${categoryName}`),
    );
    setAssignCandidates(rows);
  };

  const toggleSelected = (id: string) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const assignSelected = async () => {
    if (selectedIds.size === 0) return;
    setAssignBusy(true);
    setAssignError(null);
    try {
      for (const productId of selectedIds) {
        const candidate = assignCandidates.find((r) => r.id === productId);
        const hasOtherCategory =
          Boolean(candidate?.categorySummary)
          && candidate!.categorySummary !== "بدون دسته"
          && candidate!.categorySummary !== categoryName;
        if (hasOtherCategory) {
          const ok = window.confirm(
            `«${candidate!.title}» هم‌اکنون در دسته «${candidate!.categorySummary}» است. به این دسته منتقل شود؟`,
          );
          if (!ok) continue;
        }

        const ws = await loadProductWorkspace(productId, false);
        if (ws.source !== "host" || !ws.view) {
          throw new Error(ws.message ?? "workspace.product.missing");
        }

        const needsConfirm = Boolean(ws.view.primaryCategoryId);
        if (needsConfirm) {
          const preview = await previewProductCategoryChange(productId, categoryId, "fa-IR");
          const message =
            preview.state === "ok" && preview.data?.messageFa
              ? `${preview.data.messageFa}\n\nتغییر دسته را تأیید می‌کنید؟`
              : "تغییر دسته ممکن است ویژگی‌ها و تنوع‌های وابسته به schema را تحت تأثیر قرار دهد. ادامه می‌دهید؟";
          if (!window.confirm(message)) continue;
        }

        const result = await assignAdminProductCategory(productId, {
          categoryId,
          confirmSchemaImpact: needsConfirm,
          expectedUpdatedAt: ws.view.catalogUpdatedAt,
        });
        if (!result.ok) {
          throw new Error(result.errorCode);
        }
      }
      setAssignOpen(false);
      setSelectedIds(new Set());
      setAssignCandidates([]);
      setAssignSearch("");
      setReloadToken((n) => n + 1);
    } catch (e) {
      setAssignError(toUiError(e instanceof Error ? e.message : null));
    } finally {
      setAssignBusy(false);
    }
  };

  const submitChangeCategory = async () => {
    if (!changeRow || !changeCategoryId) return;
    if (!isAssignableProductCategory(treeNodes, changeCategoryId)) {
      setChangeError(CATEGORY_PRODUCTS_LEVEL_BLOCKED_MESSAGE_FA);
      return;
    }
    setChangeBusy(true);
    setChangeError(null);
    try {
      const ws = await loadProductWorkspace(changeRow.id, false);
      if (ws.source !== "host" || !ws.view) {
        throw new Error(ws.message ?? "workspace.product.missing");
      }
      const needsConfirm = Boolean(ws.view.primaryCategoryId)
        && ws.view.primaryCategoryId !== changeCategoryId;
      if (needsConfirm) {
        const preview = await previewProductCategoryChange(changeRow.id, changeCategoryId, "fa-IR");
        const message =
          preview.state === "ok" && preview.data?.messageFa
            ? `${preview.data.messageFa}\n\nتغییر دسته را تأیید می‌کنید؟`
            : "تغییر دسته ممکن است ویژگی‌ها و تنوع‌های وابسته به schema را تحت تأثیر قرار دهد. ادامه می‌دهید؟";
        if (!window.confirm(message)) {
          setChangeBusy(false);
          return;
        }
      }
      const result = await assignAdminProductCategory(changeRow.id, {
        categoryId: changeCategoryId,
        confirmSchemaImpact: needsConfirm,
        expectedUpdatedAt: ws.view.catalogUpdatedAt,
      });
      if (!result.ok) {
        throw new Error(result.errorCode);
      }
      setChangeRow(null);
      setChangeCategoryId(null);
      setReloadToken((n) => n + 1);
    } catch (e) {
      setChangeError(toUiError(e instanceof Error ? e.message : null));
    } finally {
      setChangeBusy(false);
    }
  };

  if (!assignable) {
    return (
      <div
        className="rounded-2xl border border-amber-200 bg-amber-50 p-6 text-sm text-amber-900"
        data-testid="category-products-level-blocked"
        data-category-level={level ?? "unknown"}
      >
        <p className="font-semibold">{CATEGORY_PRODUCTS_LEVEL_BLOCKED_MESSAGE_FA}</p>
        <p className="mt-2 text-amber-800/90">
          این دسته سطح {level ?? "—"} است و فقط برای ناوبری استفاده می‌شود. اختصاص محصول فقط در دسته‌های سطح سوم ممکن است.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-4" data-testid="category-products-panel" data-category-level={level ?? 3}>
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <p className="text-sm text-slate-600">
            محصولات اختصاص‌یافته به «{categoryName}» — همان رابطهٔ canonical که از workspace محصول هم ویرایش می‌شود.
          </p>
        </div>
        {canEdit ? (
          <button
            type="button"
            className="inline-flex min-h-11 items-center rounded-xl bg-[#2563EB] px-4 text-sm font-semibold text-white hover:bg-blue-700"
            onClick={() => {
              setAssignOpen(true);
              setAssignError(null);
              setSelectedIds(new Set());
              setAssignCandidates([]);
              void searchAssignCandidates();
            }}
            data-testid="category-products-assign-open"
          >
            اختصاص محصول موجود
          </button>
        ) : null}
      </div>

      {gridError ? (
        <p className="text-sm text-red-600" role="alert" data-testid="category-products-error">
          {gridError}
        </p>
      ) : null}

      <section className="rounded-2xl border border-gray-200 bg-white p-2 shadow-sm md:p-3">
        <AppDataGrid<AdminProductListRow>
          gridId={`grid.admin.category-products.${categoryId}`}
          columnDefs={columnDefs}
          queryAdapter={queryAdapter}
          locale="fa"
          direction="rtl"
          rowCountNoun={{ fa: "محصول", en: "products" }}
          capabilities={{
            search: true,
            advancedFilter: false,
            savedViews: false,
            columnManager: false,
            csvExport: false,
            excelExport: false,
            rowSelection: false,
          }}
        />
      </section>

      {assignOpen ? (
        <div
          className="fixed inset-0 z-40 flex items-end justify-center bg-black/40 p-4 sm:items-center"
          role="dialog"
          aria-modal="true"
          aria-labelledby="category-products-assign-title"
          data-testid="category-products-assign-dialog"
        >
          <div className="max-h-[90vh] w-full max-w-lg overflow-auto rounded-2xl bg-white p-5 shadow-xl">
            <h3 id="category-products-assign-title" className="text-base font-semibold text-slate-900">
              اختصاص محصول موجود
            </h3>
            <p className="mt-1 text-sm text-slate-600">جستجو بر اساس عنوان — شناسه خام نمایش داده نمی‌شود.</p>
            <div className="mt-4 flex gap-2">
              <input
                type="search"
                value={assignSearch}
                onChange={(e) => setAssignSearch(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === "Enter") void searchAssignCandidates();
                }}
                className="min-h-11 flex-1 rounded-xl border border-gray-200 px-3 text-sm"
                placeholder="جستجوی عنوان محصول"
                data-testid="category-products-assign-search"
              />
              <button
                type="button"
                className="inline-flex min-h-11 items-center rounded-xl border border-gray-200 px-3 text-sm font-medium"
                disabled={assignBusy}
                onClick={() => void searchAssignCandidates()}
                data-testid="category-products-assign-search-btn"
              >
                جستجو
              </button>
            </div>
            {assignError ? (
              <p className="mt-2 text-sm text-red-600" role="alert">{assignError}</p>
            ) : null}
            <ul className="mt-3 max-h-64 space-y-2 overflow-auto" data-testid="category-products-assign-list">
              {assignCandidates.length === 0 && !assignBusy ? (
                <li className="text-sm text-slate-500">محصولی برای نمایش نیست.</li>
              ) : null}
              {assignCandidates.map((row) => {
                const checked = selectedIds.has(row.id);
                const other =
                  row.categorySummary
                  && row.categorySummary !== "بدون دسته"
                  && row.categorySummary !== categoryName;
                return (
                  <li key={row.id}>
                    <label className="flex cursor-pointer items-start gap-3 rounded-xl border border-gray-100 p-3 hover:bg-slate-50">
                      <input
                        type="checkbox"
                        checked={checked}
                        onChange={() => toggleSelected(row.id)}
                        data-testid={`category-products-assign-pick-${row.id}`}
                      />
                      <span className="min-w-0 flex-1">
                        <span className="block text-sm font-medium text-slate-900">{row.title}</span>
                        <span className="mt-0.5 block text-xs text-slate-500">
                          {formatAdminStatus(row.status)}
                          {row.categorySummary ? ` · ${row.categorySummary}` : ""}
                        </span>
                        {other ? (
                          <span className="mt-1 block text-xs text-amber-700">
                            هشدار: هم‌اکنون در دسته دیگری است.
                          </span>
                        ) : null}
                      </span>
                    </label>
                  </li>
                );
              })}
            </ul>
            <div className="mt-4 flex flex-wrap gap-2">
              <button
                type="button"
                className="inline-flex min-h-11 items-center rounded-xl bg-[#2563EB] px-4 text-sm font-semibold text-white disabled:opacity-50"
                disabled={assignBusy || selectedIds.size === 0}
                onClick={() => void assignSelected()}
                data-testid="category-products-assign-confirm"
              >
                {assignBusy ? "در حال اختصاص…" : "اختصاص به این دسته"}
              </button>
              <button
                type="button"
                className="inline-flex min-h-11 items-center rounded-xl border border-gray-200 px-4 text-sm"
                disabled={assignBusy}
                onClick={() => setAssignOpen(false)}
                data-testid="category-products-assign-cancel"
              >
                انصراف
              </button>
            </div>
          </div>
        </div>
      ) : null}

      {changeRow ? (
        <div
          className="fixed inset-0 z-40 flex items-end justify-center bg-black/40 p-4 sm:items-center"
          role="dialog"
          aria-modal="true"
          data-testid="category-products-change-dialog"
        >
          <div className="w-full max-w-lg rounded-2xl bg-white p-5 shadow-xl">
            <h3 className="text-base font-semibold">تغییر دسته‌بندی</h3>
            <p className="mt-1 text-sm text-slate-600">
              محصول «{changeRow.title}» — به جای حذف دسته، دستهٔ سطح سوم دیگری انتخاب کنید.
            </p>
            <div className="mt-4">
              <ProductCategoryPicker
                value={changeCategoryId}
                onChange={setChangeCategoryId}
              />
            </div>
            {changeError ? (
              <p className="mt-2 text-sm text-red-600" role="alert">{changeError}</p>
            ) : null}
            <div className="mt-4 flex flex-wrap gap-2">
              <button
                type="button"
                className="inline-flex min-h-11 items-center rounded-xl bg-[#2563EB] px-4 text-sm font-semibold text-white disabled:opacity-50"
                disabled={changeBusy || !changeCategoryId}
                onClick={() => void submitChangeCategory()}
                data-testid="category-products-change-confirm"
              >
                {changeBusy ? "در حال ذخیره…" : "ذخیره تغییر دسته"}
              </button>
              <button
                type="button"
                className="inline-flex min-h-11 items-center rounded-xl border border-gray-200 px-4 text-sm"
                disabled={changeBusy}
                onClick={() => setChangeRow(null)}
              >
                انصراف
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}
