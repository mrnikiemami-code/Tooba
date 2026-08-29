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
import type { GridBulkAction, GridServerQuery } from "../../design-system/data-grid";
import { formatAdminStatus } from "./admin-api";
import { mapAdminErrorMessage } from "./admin-error-map";
import { resolveAdminChromeLocale } from "./admin-chrome-messages";
import { previewProductCategoryChange } from "./catalog-attribute-api";
import {
  addAdminProductAdditionalCategory,
  assignAdminProductCategory,
  loadProductWorkspace,
  queryAdminProductGrid,
  removeAdminProductAdditionalCategory,
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

function categorySummaryIncludes(summary: string, categoryName: string): boolean {
  const needle = categoryName.trim();
  if (!needle) return false;
  return summary
    .split(/[،,]/)
    .map((part) => part.trim())
    .filter(Boolean)
    .some((part) => part === needle || part.endsWith(` / ${needle}`) || part.endsWith(`/${needle}`));
}

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
  const [assignTab, setAssignTab] = useState<"all" | "selected">("all");
  const [assignBusy, setAssignBusy] = useState(false);
  const [assignError, setAssignError] = useState<string | null>(null);
  const [assignReloadToken, setAssignReloadToken] = useState(0);
  const [selectedCount, setSelectedCount] = useState(0);
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
        colId: "assignmentRole",
        headerName: "نقش",
        width: 120,
        sortable: false,
        filter: false,
        cellRenderer: (params: ICellRendererParams<AdminProductListRow>) => {
          const row = params.data;
          if (!row) return null;
          const isPrimary = row.primaryCategoryId === categoryId;
          return (
            <span
              className={
                isPrimary
                  ? "inline-flex rounded-full bg-blue-50 px-2.5 py-1 text-xs font-medium text-blue-800"
                  : "inline-flex rounded-full bg-slate-100 px-2.5 py-1 text-xs font-medium text-slate-700"
              }
              data-testid={`category-product-role-${row.id}`}
            >
              {isPrimary ? "دسته اصلی" : "اضافی"}
            </span>
          );
        },
      },
      {
        field: "status",
        headerName: "وضعیت",
        width: 120,
        cellRenderer: StatusCell,
      },
      {
        field: "categorySummary",
        headerName: "دسته‌ها",
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
    [categoryId, rowActions],
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

  const assignProductToCategory = useCallback(async (productId: string) => {
    const ws = await loadProductWorkspace(productId, false);
    if (ws.source !== "host" || !ws.view) {
      throw new Error(ws.message ?? "workspace.product.missing");
    }
    const primary = ws.view.primaryCategoryId;
    if (!primary) {
      const result = await assignAdminProductCategory(productId, {
        categoryId,
        confirmSchemaImpact: false,
        expectedUpdatedAt: ws.view.catalogUpdatedAt,
      });
      if (!result.ok) throw new Error(result.errorCode);
      return;
    }
    if (primary === categoryId) {
      return;
    }
    const result = await addAdminProductAdditionalCategory(productId, {
      categoryId,
      expectedUpdatedAt: ws.view.catalogUpdatedAt,
    });
    if (!result.ok) throw new Error(result.errorCode);
  }, [categoryId]);

  const removeProductFromCategory = useCallback(async (productId: string) => {
    const ws = await loadProductWorkspace(productId, false);
    if (ws.source !== "host" || !ws.view) {
      throw new Error(ws.message ?? "workspace.product.missing");
    }
    if (ws.view.primaryCategoryId === categoryId) {
      throw new Error("catalog.category.assignment.cannot_remove_primary");
    }
    const result = await removeAdminProductAdditionalCategory(
      productId,
      categoryId,
      ws.view.catalogUpdatedAt,
    );
    if (!result.ok) throw new Error(result.errorCode);
  }, [categoryId]);

  const assignDialogQueryAdapter = useCallback(
    async (query: GridServerQuery) => {
      void assignReloadToken;
      const filters =
        assignTab === "selected"
          ? {
              ...query.filters,
              categorySummary: {
                kind: "text" as const,
                operator: "equals" as const,
                query: categoryName.trim(),
              },
            }
          : query.filters;
      const result = await queryAdminProductGrid({
        ...query,
        filters,
      });
      if (result.source === "error") {
        throw new Error(result.message ?? "host-unreachable");
      }
      if (assignTab === "selected") {
        setSelectedCount(result.page.total);
      }
      return result.page;
    },
    [assignReloadToken, assignTab, categoryName],
  );

  const assignDialogColumns = useMemo((): ColDef<AdminProductListRow>[] => {
    return [
      {
        colId: "media",
        headerName: "",
        width: 72,
        sortable: false,
        filter: false,
        cellRenderer: MediaCell,
      },
      {
        colId: "title",
        field: "title",
        headerName: "محصول",
        flex: 1.4,
        minWidth: 180,
        cellRenderer: ProductCell,
      },
      {
        colId: "categorySummary",
        field: "categorySummary",
        headerName: "دسته‌های فعلی",
        flex: 1.2,
        minWidth: 160,
      },
      {
        colId: "status",
        field: "status",
        headerName: "وضعیت",
        width: 120,
        cellRenderer: StatusCell,
      },
      buildPinnedActionsColumnDef<AdminProductListRow>({
        direction: "rtl",
        cellRenderer: (params: ICellRendererParams<AdminProductListRow>) => {
          const row = params.data;
          if (!row || !canEdit) return null;
          const isAssignedHere = categorySummaryIncludes(row.categorySummary, categoryName);
          const isPrimaryHere = row.primaryCategoryId === categoryId;
          if (isAssignedHere) {
            return (
              <div className="flex flex-wrap gap-1">
                <span
                  className={
                    isPrimaryHere
                      ? "rounded-full bg-blue-50 px-2 py-0.5 text-[11px] font-medium text-blue-800"
                      : "rounded-full bg-slate-100 px-2 py-0.5 text-[11px] font-medium text-slate-700"
                  }
                >
                  {isPrimaryHere ? "دسته اصلی" : "اضافه شده"}
                </span>
                {isPrimaryHere ? (
                  <button
                    type="button"
                    className="rounded-lg border border-gray-200 px-2 py-1 text-xs"
                    onClick={() => onChangeCategory(row)}
                    data-testid={`category-assign-change-primary-${row.id}`}
                  >
                    تغییر دسته اصلی
                  </button>
                ) : (
                  <button
                    type="button"
                    className="rounded-lg border border-red-200 px-2 py-1 text-xs text-red-700"
                    data-testid={`category-assign-remove-${row.id}`}
                    onClick={() => {
                      void (async () => {
                        setAssignBusy(true);
                        setAssignError(null);
                        try {
                          await removeProductFromCategory(row.id);
                          setAssignReloadToken((n) => n + 1);
                          setReloadToken((n) => n + 1);
                        } catch (e) {
                          setAssignError(toUiError(e instanceof Error ? e.message : null));
                        } finally {
                          setAssignBusy(false);
                        }
                      })();
                    }}
                  >
                    حذف از این دسته
                  </button>
                )}
              </div>
            );
          }
          return (
            <button
              type="button"
              className="rounded-lg bg-[#2563EB] px-3 py-1.5 text-xs font-semibold text-white"
              data-testid={`category-assign-add-${row.id}`}
              disabled={assignBusy}
              onClick={() => {
                void (async () => {
                  setAssignBusy(true);
                  setAssignError(null);
                  try {
                    await assignProductToCategory(row.id);
                    setAssignReloadToken((n) => n + 1);
                    setReloadToken((n) => n + 1);
                  } catch (e) {
                    setAssignError(toUiError(e instanceof Error ? e.message : null));
                  } finally {
                    setAssignBusy(false);
                  }
                })();
              }}
            >
              افزودن
            </button>
          );
        },
      }),
    ];
  }, [assignBusy, assignProductToCategory, canEdit, categoryId, categoryName, onChangeCategory, removeProductFromCategory, toUiError]);

  const assignBulkActions = useMemo((): GridBulkAction<AdminProductListRow>[] => {
    if (!canEdit) return [];
    return [
      {
        id: "bulk-add-additional",
        label: "افزودن گروهی",
        requiresConfirmation: true,
        isAvailable: (rows) => rows.length > 0,
        execute: async (rows) => {
          setAssignBusy(true);
          setAssignError(null);
          let ok = 0;
          const failures: string[] = [];
          try {
            for (const row of rows) {
              try {
                if (row.primaryCategoryId === categoryId) {
                  ok += 1;
                  continue;
                }
                await assignProductToCategory(row.id);
                ok += 1;
              } catch (e) {
                failures.push(`${row.title}: ${toUiError(e instanceof Error ? e.message : null)}`);
              }
            }
            setAssignReloadToken((n) => n + 1);
            setReloadToken((n) => n + 1);
            if (failures.length > 0) {
              setAssignError(failures.slice(0, 5).join(" | "));
              return {
                ok: false,
                message: `موفق: ${ok} — ناموفق: ${failures.length}`,
              };
            }
            return { ok: true, message: `${ok} محصول اضافه شد` };
          } finally {
            setAssignBusy(false);
          }
        },
      },
      {
        id: "bulk-remove-additional",
        label: "حذف گروهی از این دسته",
        requiresConfirmation: true,
        isAvailable: (rows) => rows.some((row) => row.primaryCategoryId !== categoryId),
        execute: async (rows) => {
          setAssignBusy(true);
          setAssignError(null);
          let ok = 0;
          const failures: string[] = [];
          try {
            for (const row of rows) {
              if (row.primaryCategoryId === categoryId) {
                failures.push(`${row.title}: دسته اصلی را نمی‌توان حذف کرد`);
                continue;
              }
              try {
                await removeProductFromCategory(row.id);
                ok += 1;
              } catch (e) {
                failures.push(`${row.title}: ${toUiError(e instanceof Error ? e.message : null)}`);
              }
            }
            setAssignReloadToken((n) => n + 1);
            setReloadToken((n) => n + 1);
            if (failures.length > 0) {
              setAssignError(failures.slice(0, 5).join(" | "));
              return {
                ok: false,
                message: `موفق: ${ok} — ناموفق: ${failures.length}`,
              };
            }
            return { ok: true, message: `${ok} انتساب اضافی حذف شد` };
          } finally {
            setAssignBusy(false);
          }
        },
      },
    ];
  }, [assignProductToCategory, canEdit, categoryId, removeProductFromCategory, toUiError]);

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
        <div className="max-w-3xl space-y-1">
          <p className="text-sm text-slate-600" data-testid="category-products-helper">
            {locale === "en"
              ? "Products can have one primary category and several additional categories. The primary category drives attributes and variants; additional categories are only for discovery and listing."
              : "محصولات می‌توانند یک دسته اصلی و چند دسته اضافی داشته باشند. دسته اصلی ساختار ویژگی‌ها و تنوع‌های محصول را تعیین می‌کند؛ دسته‌های اضافی فقط برای نمایش و پیدا شدن محصول استفاده می‌شوند."}
          </p>
        </div>
        {canEdit ? (
          <button
            type="button"
            className="inline-flex min-h-11 items-center rounded-xl bg-[#2563EB] px-4 text-sm font-semibold text-white hover:bg-blue-700"
            onClick={() => {
              setAssignOpen(true);
              setAssignTab("all");
              setAssignError(null);
              setAssignReloadToken((n) => n + 1);
            }}
            data-testid="category-products-assign-open"
          >
            اختصاص محصولات
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
          className="fixed inset-0 z-40 flex items-end justify-center bg-black/40 p-3 sm:items-center"
          role="dialog"
          aria-modal="true"
          aria-labelledby="category-products-assign-title"
          data-testid="category-products-assign-dialog"
        >
          <div className="flex max-h-[92vh] w-full max-w-5xl flex-col overflow-hidden rounded-2xl bg-white shadow-xl">
            <div className="border-b border-gray-100 px-5 py-4">
              <h3 id="category-products-assign-title" className="text-base font-semibold text-slate-900">
                اختصاص محصولات به این دسته
              </h3>
              <p className="mt-1 text-sm text-slate-600">
                جستجو و صفحه‌بندی سمت سرور — مناسب کاتالوگ‌های بزرگ.
              </p>
              <div className="mt-3 flex flex-wrap gap-2" role="tablist">
                <button
                  type="button"
                  role="tab"
                  aria-selected={assignTab === "all"}
                  className={
                    assignTab === "all"
                      ? "rounded-full bg-[#2563EB] px-3 py-1.5 text-xs font-semibold text-white"
                      : "rounded-full border border-gray-200 px-3 py-1.5 text-xs font-medium text-slate-700"
                  }
                  onClick={() => {
                    setAssignTab("all");
                    setAssignReloadToken((n) => n + 1);
                  }}
                  data-testid="category-assign-tab-all"
                >
                  همه محصولات
                </button>
                <button
                  type="button"
                  role="tab"
                  aria-selected={assignTab === "selected"}
                  className={
                    assignTab === "selected"
                      ? "rounded-full bg-[#2563EB] px-3 py-1.5 text-xs font-semibold text-white"
                      : "rounded-full border border-gray-200 px-3 py-1.5 text-xs font-medium text-slate-700"
                  }
                  onClick={() => {
                    setAssignTab("selected");
                    setAssignReloadToken((n) => n + 1);
                  }}
                  data-testid="category-assign-tab-selected"
                >
                  انتخاب‌شده‌ها ({selectedCount})
                </button>
              </div>
            </div>
            {assignError ? (
              <p className="px-5 pt-3 text-sm text-red-600" role="alert">
                {assignError}
              </p>
            ) : null}
            <div className="min-h-0 flex-1 overflow-auto p-3" data-testid="category-products-assign-grid">
              <AppDataGrid<AdminProductListRow>
                gridId={`grid.admin.category-assign.${categoryId}.${assignTab}`}
                columnDefs={assignDialogColumns}
                queryAdapter={assignDialogQueryAdapter}
                locale="fa"
                direction="rtl"
                rowCountNoun={{ fa: "محصول", en: "products" }}
                pageSelectionOnly
                bulkActions={assignBulkActions}
                capabilities={{
                  search: true,
                  advancedFilter: false,
                  savedViews: false,
                  columnManager: false,
                  csvExport: false,
                  excelExport: false,
                  rowSelection: true,
                }}
              />
            </div>
            <div className="flex justify-end gap-2 border-t border-gray-100 px-5 py-3">
              <button
                type="button"
                className="inline-flex min-h-11 items-center rounded-xl border border-gray-200 px-4 text-sm"
                disabled={assignBusy}
                onClick={() => setAssignOpen(false)}
                data-testid="category-products-assign-cancel"
              >
                بستن
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
