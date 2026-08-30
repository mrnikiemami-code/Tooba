"use client";

/**
 * تب محصولات Category Workspace — فهرست و اختصاص از همان رابطهٔ canonical Product↔Category.
 * این صفحه فقط عضویت در دستهٔ جاری را مدیریت می‌کند (نه تغییر دسته اصلی).
 */

import { useCallback, useMemo, useState } from "react";
import type { ColDef, ICellRendererParams } from "ag-grid-community";
import { Eye, Trash2 } from "lucide-react";
import { toast } from "react-toastify";
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
import {
  addAdminProductAdditionalCategory,
  assignAdminProductCategory,
  loadProductWorkspace,
  queryAdminProductGrid,
  removeAdminProductAdditionalCategory,
  type AdminProductListRow,
} from "./host-client";
import {
  getCategoryLevel,
  isAssignableProductCategory,
} from "./product-category-level";
import { storefrontMediaUrl } from "../storefront/storefront-api";

export const CATEGORY_PRODUCTS_LEVEL_BLOCKED_MESSAGE_FA =
  "محصول فقط به دسته‌بندی سطح سوم قابل اختصاص است.";

const PRIMARY_MEMBERSHIP_HELPER_FA =
  "این دسته، دسته اصلی محصول است. برای تغییر دسته اصلی، محصول را باز کنید.";

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
  const [assignChecked, setAssignChecked] = useState<Map<string, AdminProductListRow>>(() => new Map());

  const selectionCount = assignChecked.size;

  const bumpMembershipState = useCallback(() => {
    setAssignReloadToken((n) => n + 1);
    setReloadToken((n) => n + 1);
  }, []);

  const clearAssignSelection = useCallback(() => {
    setAssignChecked(new Map());
  }, []);

  const toUiError = useCallback(
    (raw: string | null | undefined) => mapAdminErrorMessage(raw, locale),
    [locale],
  );

  const toggleAssignChecked = useCallback((row: AdminProductListRow, checked: boolean) => {
    setAssignChecked((prev) => {
      const next = new Map(prev);
      if (checked) next.set(row.id, row);
      else next.delete(row.id);
      return next;
    });
  }, []);

  /** عضویت در دستهٔ جاری: بدون primary → set primary؛ با primary دیگر → additional. */
  const assignProductToCategory = useCallback(async (productId: string): Promise<"added" | "already"> => {
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
      return "added";
    }
    if (primary === categoryId) {
      return "already";
    }
    const alreadyAdditional = (ws.view.categoryAssignments ?? []).some(
      (a) => a.categoryId === categoryId,
    );
    if (alreadyAdditional) {
      return "already";
    }
    const result = await addAdminProductAdditionalCategory(productId, {
      categoryId,
      expectedUpdatedAt: ws.view.catalogUpdatedAt,
    });
    if (!result.ok) throw new Error(result.errorCode);
    return "added";
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

  const afterMembershipAdded = useCallback(() => {
    setSelectedCount((n) => n + 1);
    bumpMembershipState();
    toast.success("محصول به دسته اضافه شد.");
  }, [bumpMembershipState]);

  const afterMembershipRemoved = useCallback(() => {
    setSelectedCount((n) => Math.max(0, n - 1));
    bumpMembershipState();
    toast.success("محصول از این دسته حذف شد.");
  }, [bumpMembershipState]);

  const runBulkAddSelected = useCallback(async () => {
    const rows = [...assignChecked.values()];
    if (rows.length === 0) return;
    setAssignBusy(true);
    setAssignError(null);
    let added = 0;
    const failures: string[] = [];
    try {
      for (const row of rows) {
        try {
          const outcome = await assignProductToCategory(row.id);
          if (outcome === "added") added += 1;
        } catch (e) {
          failures.push(`${row.title}: ${toUiError(e instanceof Error ? e.message : null)}`);
        }
      }
      if (added > 0) {
        setSelectedCount((n) => n + added);
        bumpMembershipState();
        toast.success("محصول به دسته اضافه شد.");
      } else {
        bumpMembershipState();
      }
      clearAssignSelection();
      if (failures.length > 0) {
        setAssignError(failures.slice(0, 5).join(" | "));
      }
    } finally {
      setAssignBusy(false);
    }
  }, [
    assignChecked,
    assignProductToCategory,
    bumpMembershipState,
    clearAssignSelection,
    toUiError,
  ]);

  const rowActions = useMemo((): AppGridRowAction<AdminProductListRow>[] => {
    const actions: AppGridRowAction<AdminProductListRow>[] = [
      {
        id: "view",
        label: "باز کردن محصول",
        icon: Eye,
        href: (row) => `/admin/products/${row.id}?scope=view`,
        testId: (row) => `category-product-view-${row.id}`,
      },
    ];
    if (canEdit && assignable) {
      actions.push({
        id: "remove-membership",
        label: "حذف از این دسته",
        icon: Trash2,
        variant: "destructive",
        visible: (row) => row.primaryCategoryId !== categoryId,
        confirm: () => "این محصول از عضویت این دسته حذف شود؟",
        onClick: async (row) => {
          await removeProductFromCategory(row.id);
          afterMembershipRemoved();
        },
        testId: (row) => `category-product-remove-${row.id}`,
      });
    }
    return actions;
  }, [afterMembershipRemoved, assignable, canEdit, categoryId, removeProductFromCategory]);

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
        width: 128,
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
              title={isPrimary ? undefined : "نمایش در دسته‌های دیگر"}
              data-testid={`category-product-role-${row.id}`}
            >
              {isPrimary ? "دسته اصلی" : "نمایش دیگر"}
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
        colId: "pick",
        headerName: "",
        width: 52,
        maxWidth: 56,
        sortable: false,
        filter: false,
        cellRenderer: (params: ICellRendererParams<AdminProductListRow>) => {
          const row = params.data;
          if (!row || !canEdit) return null;
          const checked = assignChecked.has(row.id);
          return (
            <input
              type="checkbox"
              className="h-4 w-4 accent-[#2563EB]"
              checked={checked}
              disabled={assignBusy}
              aria-label={`انتخاب ${row.title}`}
              data-testid={`category-assign-check-${row.id}`}
              onChange={(e) => toggleAssignChecked(row, e.target.checked)}
            />
          );
        },
      },
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
        width: 220,
        cellRenderer: (params: ICellRendererParams<AdminProductListRow>) => {
          const row = params.data;
          if (!row || !canEdit) return null;
          const isAssignedHere = categorySummaryIncludes(row.categorySummary, categoryName);
          const isPrimaryHere = row.primaryCategoryId === categoryId;
          if (isAssignedHere) {
            if (isPrimaryHere) {
              return (
                <div className="flex max-w-[14rem] flex-col gap-1" data-testid={`category-assign-primary-${row.id}`}>
                  <span className="w-fit rounded-full bg-blue-50 px-2 py-0.5 text-[11px] font-medium text-blue-800">
                    دسته اصلی
                  </span>
                  <p className="text-[11px] leading-snug text-slate-600">{PRIMARY_MEMBERSHIP_HELPER_FA}</p>
                  <a
                    href={`/admin/products/${row.id}?scope=view`}
                    className="text-[11px] font-medium text-[#2563EB] hover:underline"
                    data-testid={`category-assign-open-product-${row.id}`}
                  >
                    باز کردن محصول
                  </a>
                </div>
              );
            }
            return (
              <div className="flex flex-wrap items-center gap-1">
                <span
                  className="rounded-full bg-slate-100 px-2 py-0.5 text-[11px] font-medium text-slate-700"
                  title="نمایش در دسته‌های دیگر"
                >
                  نمایش دیگر
                </span>
                <button
                  type="button"
                  className="rounded-lg border border-red-200 px-2 py-1 text-xs text-red-700 disabled:opacity-50"
                  data-testid={`category-assign-remove-${row.id}`}
                  disabled={assignBusy}
                  onClick={() => {
                    void (async () => {
                      setAssignBusy(true);
                      setAssignError(null);
                      try {
                        await removeProductFromCategory(row.id);
                        afterMembershipRemoved();
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
              </div>
            );
          }
          return (
            <button
              type="button"
              className="rounded-lg bg-[#2563EB] px-3 py-1.5 text-xs font-semibold text-white disabled:opacity-50"
              data-testid={`category-assign-add-${row.id}`}
              disabled={assignBusy}
              onClick={() => {
                void (async () => {
                  setAssignBusy(true);
                  setAssignError(null);
                  try {
                    const outcome = await assignProductToCategory(row.id);
                    if (outcome === "added") {
                      afterMembershipAdded();
                    } else {
                      bumpMembershipState();
                    }
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
  }, [
    afterMembershipAdded,
    afterMembershipRemoved,
    assignBusy,
    assignChecked,
    assignProductToCategory,
    bumpMembershipState,
    canEdit,
    categoryId,
    categoryName,
    removeProductFromCategory,
    toUiError,
    toggleAssignChecked,
  ]);

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
              ? "Products have one primary category and may appear in other categories for discovery. The primary category drives attributes and variants; display-in-other-categories does not change product specs."
              : "دسته اصلی مشخصات و تنوع‌های محصول را تعیین می‌کند. نمایش در دسته‌های دیگر فقط باعث می‌شود محصول در آن دسته‌ها هم پیدا شود و مشخصات محصول را تغییر نمی‌دهد."}
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
              clearAssignSelection();
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
                فقط عضویت در این دسته — جستجو و صفحه‌بندی سمت سرور.
              </p>
              <div className="mt-3 flex flex-wrap items-center gap-2" role="tablist">
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
                {canEdit ? (
                  <button
                    type="button"
                    className="ms-auto inline-flex min-h-9 items-center rounded-xl bg-[#2563EB] px-3 text-xs font-semibold text-white disabled:cursor-not-allowed disabled:opacity-50"
                    disabled={selectionCount === 0 || assignBusy}
                    onClick={() => void runBulkAddSelected()}
                    data-testid="category-assign-bulk-add-selected"
                  >
                    {assignBusy
                      ? "در حال افزودن…"
                      : `افزودن موارد انتخاب‌شده (${selectionCount})`}
                  </button>
                ) : null}
              </div>
            </div>
            {assignError ? (
              <p className="px-5 pt-3 text-sm text-red-600" role="alert" data-testid="category-assign-error">
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
            </div>
            <div className="flex justify-end gap-2 border-t border-gray-100 px-5 py-3">
              <button
                type="button"
                className="inline-flex min-h-11 items-center rounded-xl border border-gray-200 px-4 text-sm"
                disabled={assignBusy}
                onClick={() => {
                  setAssignOpen(false);
                  clearAssignSelection();
                }}
                data-testid="category-products-assign-cancel"
              >
                بستن
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}
