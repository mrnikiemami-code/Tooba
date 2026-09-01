"use client";

import Link from "next/link";
import { Archive, Edit2, Eye } from "lucide-react";
import { useCallback, useMemo, useState } from "react";
import type { ColDef, ICellRendererParams } from "ag-grid-community";
import { AppDataGrid, ErrorState, faWorkspaceMessages, formatJalaliDate } from "../../design-system";
import {
  ADMIN_PRODUCT_EXTERNAL_FILTER_FIELDS,
  applyProductGridFilterHeader,
} from "./product-grid-filter-matrix";
import type { AppGridFilterColumnDef } from "../../design-system/app-data-grid/filter-column-def";
import {
  AppGridBadgeCell,
  AppGridLinkSubtitleCell,
  AppGridMediaCell,
  AppGridTruncatedCell,
} from "../../design-system/app-data-grid/app-grid-cells";
import { buildPinnedActionsColumnDef } from "../../design-system/app-data-grid/app-grid-pinned-actions";
import { AppGridRowActionsCell, type AppGridRowAction } from "../../design-system/app-data-grid/app-grid-row-actions";
import type { GridServerQuery } from "../../design-system/data-grid";
import { formatAdminStatus } from "./admin-api";
import {
  mutateAdminProductLifecycle,
  queryAdminProductGrid,
  type AdminProductListRow,
} from "./host-client";
import { ADMIN_PRODUCT_GRID_VIEW_KEY, createHostSavedViewStore } from "./saved-view-store";
import { storefrontMediaUrl } from "../storefront/storefront-api";
import { AdditionalCategoryListCell } from "./additional-category-list-cell";

function productStatusClass(status: string): string {
  if (status === "Published") return "inline-flex rounded-full bg-success/15 px-2.5 py-1 text-xs font-medium text-success";
  if (status === "Archived") return "inline-flex rounded-full bg-secondary px-2.5 py-1 text-xs font-medium text-muted";
  return "inline-flex rounded-full bg-warning/15 px-2.5 py-1 text-xs font-medium text-warning";
}

function stockClass(units: number): string {
  if (units <= 0) return "font-medium text-danger";
  if (units < 10) return "font-medium text-warning";
  return "font-medium text-success";
}

function buildProductRowActions(
  onLifecycle: (productId: string, action: "publish" | "unpublish" | "archive" | "delete") => Promise<void>,
): AppGridRowAction<AdminProductListRow>[] {
  return [
    {
      id: "view",
      label: "مشاهده",
      icon: Eye,
      href: (row) => `/admin/products/${row.id}?scope=view`,
      testId: (row) => `admin-product-view-${row.id}`,
    },
    {
      id: "edit",
      label: "ویرایش",
      icon: Edit2,
      href: (row) => `/admin/products/${row.id}?scope=edit`,
      testId: (row) => `admin-product-edit-${row.id}`,
    },
    {
      id: "delete",
      label: "بایگانی / حذف امن",
      icon: Archive,
      variant: "destructive",
      confirm: (row) => `بایگانی «${row.title}»؟ در صورت نیاز می‌توانید بعداً از بایگانی بازگردانید.`,
      onClick: (row) => onLifecycle(row.id, "archive"),
      testId: (row) => `admin-product-delete-${row.id}`,
    },
  ];
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
  const subtitle = row.primaryCategoryName?.trim() || "";
  return (
    <AppGridLinkSubtitleCell
      params={params}
      href={`/admin/products/${row.id}?scope=view`}
      title={row.title}
      subtitle={subtitle}
    />
  );
}

function PrimaryCategoryCell(params: ICellRendererParams<AdminProductListRow>) {
  const row = params.data;
  if (!row) return null;
  const name = row.primaryCategoryName?.trim();
  if (!name) {
    return <span className="text-muted">—</span>;
  }
  return <AppGridTruncatedCell params={params} text={name} />;
}

function AdditionalCategoriesCell(params: ICellRendererParams<AdminProductListRow>) {
  const row = params.data;
  if (!row) return null;
  return <AdditionalCategoryListCell params={params} names={row.additionalCategoryNames} />;
}

function StatusCell(params: ICellRendererParams<AdminProductListRow>) {
  const label = formatAdminStatus(String(params.value ?? ""));
  return <AppGridBadgeCell params={params} label={label} className={productStatusClass(String(params.value ?? ""))} />;
}

function StockCell(params: ICellRendererParams<AdminProductListRow>) {
  const label = Number(params.value ?? 0).toLocaleString("fa-IR");
  return <AppGridTruncatedCell params={params} text={label} className={stockClass(Number(params.value ?? 0))} />;
}

const PRODUCT_STATUS_FILTER_OPTIONS = [
  { value: "Published", label: "منتشر شده" },
  { value: "Draft", label: "پیش‌نویس" },
  { value: "Archived", label: "بایگانی" },
] as const;

function buildColumnDefs(
  rowActions: AppGridRowAction<AdminProductListRow>[],
): ColDef<AdminProductListRow>[] {
  return [
    applyProductGridFilterHeader({
      colId: "media",
      headerName: "رسانه",
      width: 68,
      minWidth: 64,
      maxWidth: 76,
      sortable: false,
      cellRenderer: MediaCell,
    }),
    applyProductGridFilterHeader({
      field: "title",
      headerName: "محصول",
      minWidth: 280,
      flex: 2,
      cellRenderer: ProductCell,
    }),
    applyProductGridFilterHeader({
      field: "status",
      headerName: "وضعیت",
      width: 120,
      valueFormatter: (p) => formatAdminStatus(String(p.value ?? "")),
      cellRenderer: StatusCell,
    }),
    applyProductGridFilterHeader({
      field: "primaryCategoryName",
      headerName: "دسته اصلی",
      width: 150,
      cellRenderer: PrimaryCategoryCell,
    }),
    applyProductGridFilterHeader({
      colId: "additionalCategoryNames",
      field: "additionalCategoryNames",
      headerName: "نمایش در دسته‌های دیگر",
      width: 320,
      minWidth: 260,
      maxWidth: 480,
      flex: 1.2,
      sortable: false,
      cellRenderer: AdditionalCategoriesCell,
    }),
    applyProductGridFilterHeader({ field: "brandName", headerName: "برند", width: 140, sortable: false }),
    applyProductGridFilterHeader({ field: "variantCount", headerName: "تنوع", width: 90 }),
    applyProductGridFilterHeader({
      field: "updatedAt",
      headerName: "به‌روزرسانی",
      width: 120,
      valueFormatter: (p) => formatJalaliDate(String(p.value ?? ""), "fa"),
    }),
    // Offer-composed commercial fields stay available as hidden columns — not Product.Price/Stock.
    applyProductGridFilterHeader({ field: "offerAmountRange", headerName: "بازه پیشنهاد", width: 150, hide: true }),
    applyProductGridFilterHeader({
      field: "sellableUnits",
      headerName: "قابل‌فروش ترکیبی",
      width: 120,
      hide: true,
      cellRenderer: StockCell,
    }),
    applyProductGridFilterHeader({ field: "offerCount", headerName: "پیشنهاد", width: 100, hide: true }),
    applyProductGridFilterHeader({ field: "locationCount", headerName: "محل", width: 90, hide: true }),
    buildPinnedActionsColumnDef<AdminProductListRow>({
      direction: "rtl",
      actionSlots: 3,
      width: 168,
      minWidth: 152,
      maxWidth: 220,
      cellRenderer: (params: ICellRendererParams<AdminProductListRow>) =>
        params.data ? <AppGridRowActionsCell row={params.data} actions={rowActions} /> : null,
    }),
  ];
}

const PRODUCT_GRID_ADVANCED_FILTERS: AppGridFilterColumnDef[] = [
  { id: "title", header: "عنوان", filterKind: "text" },
  {
    id: "status",
    header: "وضعیت",
    filterKind: "status",
    enumOptions: [
      { value: "Published", label: "منتشر شده" },
      { value: "Draft", label: "پیش‌نویس" },
      { value: "Archived", label: "بایگانی" },
    ],
  },
  { id: "variantCount", header: "تنوع", filterKind: "number" },
  { id: "offerCount", header: "پیشنهاد", filterKind: "number" },
  { id: "primaryCategoryName", header: "دسته اصلی", filterKind: "text" },
  { id: "offerAmountRange", header: "بازه پیشنهاد", filterKind: "number" },
  { id: "sellableUnits", header: "قابل‌فروش ترکیبی", filterKind: "number" },
  { id: "locationCount", header: "محل", filterKind: "number" },
  { id: "updatedAt", header: "به‌روزرسانی", filterKind: "date" },
];

/** فهرست Admin — adapter مرجع برای AppDataGrid canonical. */
export function ProductListScreen() {
  const [denied, setDenied] = useState(false);
  const [gridError, setGridError] = useState<string | undefined>();
  const [reloadToken, setReloadToken] = useState(0);
  const savedViewStore = useMemo(() => createHostSavedViewStore(ADMIN_PRODUCT_GRID_VIEW_KEY), []);

  const onLifecycle = useCallback(async (productId: string, action: "publish" | "unpublish" | "archive" | "delete") => {
    const result = await mutateAdminProductLifecycle(productId, action);
    if (!result.ok) throw new Error(result.message);
    setReloadToken((value) => value + 1);
  }, []);

  const rowActions = useMemo(() => buildProductRowActions(onLifecycle), [onLifecycle]);
  const columnDefs = useMemo(() => buildColumnDefs(rowActions), [rowActions]);

  const queryAdapter = useCallback(
    async (query: GridServerQuery) => {
      const result = await queryAdminProductGrid(query);
      if (result.denied) {
        setDenied(true);
        throw new Error(result.message);
      }
      if (result.source === "error") {
        setGridError(result.message);
        throw new Error(result.message ?? "host-unreachable");
      }
      setGridError(undefined);
      void reloadToken;
      return result.page;
    },
    [reloadToken],
  );

  if (denied) {
    return (
      <main data-testid="admin-auth-denied">
        <ErrorState title="دسترسی مجاز نیست" detail="سامانه هویت فعلی را مدیر تشخیص نداد." retryLabel={faWorkspaceMessages.retry} />
      </main>
    );
  }

  return (
    <main className="w-full" data-testid="admin-product-list-screen">
      <div className="mb-5 flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-[length:var(--type-title)] font-semibold tracking-tight">محصولات</h1>
          <p className="mt-1 text-[length:var(--type-body)] text-muted">فهرست عملیاتی کاتالوگ فروشگاه</p>
        </div>
        <Link
          href="/admin/products/new"
          className="inline-flex min-h-11 items-center gap-1 rounded-xl bg-[#2563EB] px-4 text-sm font-semibold text-white hover:brightness-95"
          data-testid="admin-create-product"
        >
          <span aria-hidden>+</span>
          افزودن محصول
        </Link>
      </div>
      <section className="rounded-2xl border border-border bg-surface-elevated p-2 shadow-sm md:p-4">
        {gridError ? (
          <p className="mb-2 text-sm text-danger" data-testid="list-source">
            اتصال فروشگاه برقرار نیست ({gridError})
          </p>
        ) : null}
        <AppDataGrid<AdminProductListRow>
          gridId={ADMIN_PRODUCT_GRID_VIEW_KEY}
          columnDefs={columnDefs}
          queryAdapter={queryAdapter}
          advancedFilterColumns={PRODUCT_GRID_ADVANCED_FILTERS}
          externalFilterFields={ADMIN_PRODUCT_EXTERNAL_FILTER_FIELDS}
          statusFilterOptions={[...PRODUCT_STATUS_FILTER_OPTIONS]}
          locale="fa"
          direction="rtl"
          rowCountNoun={{ fa: "محصول", en: "rows" }}
          messageOverrides={{
            advancedFilterTitle: "فیلتر پیشرفته محصولات",
            advancedFilterSubtitle: "جستجوی دقیق میان محصولات",
          }}
          savedViewStore={savedViewStore}
          exportFilenameBase="admin-products"
          exportHeaders={["محصول", "وضعیت", "دسته اصلی", "نمایش در دسته‌های دیگر", "برند", "تنوع", "به‌روزرسانی"]}
          getExportRow={(row) => [
            row.title,
            formatAdminStatus(row.status),
            row.primaryCategoryName ?? "",
            row.additionalCategoryNames.join("، "),
            row.brandName,
            String(row.variantCount),
            formatJalaliDate(row.updatedAt, "fa"),
          ]}
        />
      </section>
    </main>
  );
}
