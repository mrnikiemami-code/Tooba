"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useMemo, useState } from "react";
import type { ColDef, ICellRendererParams } from "ag-grid-community";
import { AppDataGrid, ErrorState, faWorkspaceMessages, formatJalaliDate } from "../../design-system";
import { COLUMN_FILTER_APPLY_PARAMS } from "../../design-system/app-data-grid/filter-commit";
import type { AppGridFilterColumnDef } from "../../design-system/app-data-grid/filter-column-def";
import type { GridServerQuery } from "../../design-system/data-grid";
import { formatAdminStatus } from "./admin-api";
import {
  createAdminProduct,
  mutateAdminProductLifecycle,
  queryAdminProductGrid,
  type AdminProductListRow,
} from "./host-client";
import { ADMIN_PRODUCT_GRID_VIEW_KEY, createHostSavedViewStore } from "./saved-view-store";
import { storefrontMediaUrl } from "../storefront/storefront-api";

function productCode(id: string): string {
  const compact = id.replace(/-/g, "").slice(0, 7).toUpperCase();
  return `PRD-${compact}`;
}

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

function ProductActionMenu({
  row,
  onLifecycle,
}: {
  row: AdminProductListRow;
  onLifecycle: (productId: string, action: "publish" | "unpublish" | "archive" | "delete") => Promise<void>;
}) {
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<string | undefined>();

  async function run(action: "publish" | "unpublish" | "archive" | "delete") {
    setBusy(true);
    setMessage(undefined);
    try {
      await onLifecycle(row.id, action);
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "خطا");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="app-grid-cell-content">
      <div className="relative flex justify-center">
      <details className="group">
        <summary
          className="flex size-9 cursor-pointer list-none items-center justify-center rounded-full border border-border bg-surface text-lg marker:content-none hover:bg-secondary [&::-webkit-details-marker]:hidden"
          aria-label="عملیات"
        >
          ⋮
        </summary>
        <div className="absolute end-0 z-[var(--z-popover)] mt-1 min-w-[10rem] rounded-ds border border-border bg-surface-elevated p-1 shadow-md">
          <Link className="block rounded-ds px-3 py-2 text-sm hover:bg-secondary" href={`/admin/products/${row.id}`}>
            مشاهده
          </Link>
          <button type="button" disabled={busy || row.status === "Published"} className="block w-full rounded-ds px-3 py-2 text-start text-sm hover:bg-secondary disabled:opacity-50" onClick={() => void run("publish")}>
            انتشار
          </button>
          <button type="button" disabled={busy || row.status !== "Published"} className="block w-full rounded-ds px-3 py-2 text-start text-sm hover:bg-secondary disabled:opacity-50" onClick={() => void run("unpublish")}>
            لغو انتشار
          </button>
          <button type="button" disabled={busy || row.status === "Archived"} className="block w-full rounded-ds px-3 py-2 text-start text-sm hover:bg-secondary disabled:opacity-50" onClick={() => void run("archive")}>
            بایگانی
          </button>
          <button type="button" disabled={busy} className="block w-full rounded-ds px-3 py-2 text-start text-sm text-danger hover:bg-secondary disabled:opacity-50" onClick={() => void run("delete")}>
            حذف امن
          </button>
        </div>
      </details>
      {message ? <p className="absolute top-full mt-1 max-w-[10rem] text-xs text-danger">{message}</p> : null}
    </div>
    </div>
  );
}

function MediaCell(params: ICellRendererParams<AdminProductListRow>) {
  const row = params.data;
  if (!row) return null;
  const thumb = row.primaryMediaAssetId ? storefrontMediaUrl(row.primaryMediaAssetId) : null;
  return (
    <div className="app-grid-cell-content">
      <div className="flex items-center gap-2">
      {thumb ? (
        <img src={thumb} alt="" className="size-11 shrink-0 rounded-ds border border-border object-cover bg-secondary" />
      ) : (
        <span className="flex size-11 shrink-0 items-center justify-center rounded-ds bg-secondary text-[10px] text-muted">بدون تصویر</span>
      )}
      </div>
    </div>
  );
}

function ProductCell(params: ICellRendererParams<AdminProductListRow>) {
  const row = params.data;
  if (!row) return null;
  return (
    <div className="app-grid-cell-content">
      <Link className="block min-w-0 hover:underline" href={`/admin/products/${row.id}`}>
      <span className="block truncate text-sm font-semibold leading-snug">{row.title}</span>
      <span className="mt-0.5 block truncate text-xs text-muted" dir="ltr">
        {productCode(row.id)}
      </span>
    </Link>
    </div>
  );
}

function buildColumnDefs(
  onLifecycle: (productId: string, action: "publish" | "unpublish" | "archive" | "delete") => Promise<void>,
): ColDef<AdminProductListRow>[] {
  return [
    {
      colId: "actions",
      headerName: "عملیات",
      width: 72,
      minWidth: 72,
      maxWidth: 80,
      sortable: false,
      filter: false,
      pinned: directionPin(),
      cellRenderer: (params: ICellRendererParams<AdminProductListRow>) =>
        params.data ? <ProductActionMenu row={params.data} onLifecycle={onLifecycle} /> : null,
    },
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
      headerName: "محصول",
      minWidth: 220,
      flex: 1.4,
      cellRenderer: ProductCell,
      filter: false,
      headerComponent: "appColumnHeader",
      headerComponentParams: { externalFilter: "text" },
    },
    {
      field: "status",
      headerName: "وضعیت",
      width: 120,
      valueFormatter: (p) => formatAdminStatus(String(p.value ?? "")),
      cellRenderer: (params: ICellRendererParams<AdminProductListRow>) => (
        <span className={productStatusClass(String(params.value ?? ""))}>{formatAdminStatus(String(params.value ?? ""))}</span>
      ),
      filter: false,
    },
    { field: "categorySummary", headerName: "دسته", width: 130, filter: "agTextColumnFilter", filterParams: COLUMN_FILTER_APPLY_PARAMS },
    { field: "offerAmountRange", headerName: "قیمت (تومان)", width: 150, filter: false },
    {
      field: "sellableUnits",
      headerName: "موجودی",
      width: 100,
      filter: "agNumberColumnFilter",
      filterParams: COLUMN_FILTER_APPLY_PARAMS,
      cellRenderer: (params: ICellRendererParams<AdminProductListRow>) => (
        <div className="app-grid-cell-content">
          <span className={stockClass(Number(params.value ?? 0))}>{Number(params.value ?? 0).toLocaleString("fa-IR")}</span>
        </div>
      ),
    },
    {
      field: "updatedAt",
      headerName: "به‌روزرسانی",
      width: 120,
      valueFormatter: (p) => formatJalaliDate(String(p.value ?? ""), "fa"),
      filter: false,
      headerComponent: "appColumnHeader",
      headerComponentParams: { externalFilter: "jalali-date" },
    },
    { field: "variantCount", headerName: "گونه", width: 90, hide: true, filter: "agNumberColumnFilter", filterParams: COLUMN_FILTER_APPLY_PARAMS },
    { field: "offerCount", headerName: "پیشنهاد", width: 100, hide: true, filter: "agNumberColumnFilter", filterParams: COLUMN_FILTER_APPLY_PARAMS },
    { field: "locationCount", headerName: "محل", width: 90, hide: true, filter: "agNumberColumnFilter", filterParams: COLUMN_FILTER_APPLY_PARAMS },
  ];
}

function directionPin(): "left" | "right" {
  return "right";
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
  { id: "variantCount", header: "گونه", filterKind: "number" },
  { id: "offerCount", header: "پیشنهاد", filterKind: "number" },
  { id: "categorySummary", header: "دسته", filterKind: "text" },
  { id: "sellableUnits", header: "موجودی", filterKind: "number" },
  { id: "locationCount", header: "محل", filterKind: "number" },
  { id: "updatedAt", header: "به‌روزرسانی", filterKind: "date" },
];

/** فهرست Admin با AppDataGrid (AG Grid Community) + API GridQuery/GridPage. */
export function ProductListScreen() {
  const router = useRouter();
  const [denied, setDenied] = useState(false);
  const [gridError, setGridError] = useState<string | undefined>();
  const [creating, setCreating] = useState(false);
  const [createOpen, setCreateOpen] = useState(false);
  const [createTitle, setCreateTitle] = useState("");
  const [createSlug, setCreateSlug] = useState("");
  const [createError, setCreateError] = useState<string | undefined>();
  const [reloadToken, setReloadToken] = useState(0);
  const savedViewStore = useMemo(() => createHostSavedViewStore(ADMIN_PRODUCT_GRID_VIEW_KEY), []);

  const onLifecycle = useCallback(async (productId: string, action: "publish" | "unpublish" | "archive" | "delete") => {
    const result = await mutateAdminProductLifecycle(productId, action);
    if (!result.ok) throw new Error(result.message);
    setReloadToken((value) => value + 1);
  }, []);

  const columnDefs = useMemo(() => buildColumnDefs(onLifecycle), [onLifecycle]);

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

  async function onCreate() {
    if (!createTitle.trim()) {
      setCreateError("عنوان لازم است");
      return;
    }
    setCreating(true);
    setCreateError(undefined);
    const result = await createAdminProduct({
      title: createTitle.trim(),
      slug: createSlug.trim() || null,
      locale: "fa-IR",
    });
    setCreating(false);
    if (!result.ok) {
      setCreateError(result.denied ? "دسترسی مجاز نیست" : result.errorCode);
      return;
    }
    setCreateOpen(false);
    setCreateTitle("");
    setCreateSlug("");
    router.push(`/admin/products/${result.productId}`);
  }

  if (denied) {
    return (
      <main data-testid="admin-auth-denied">
        <ErrorState title="دسترسی مجاز نیست" detail="سامانه هویت فعلی را مدیر تشخیص نداد." retryLabel={faWorkspaceMessages.retry} />
      </main>
    );
  }

  return (
    <main className="w-full">
      <div className="mb-5 flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-[length:var(--type-title)] font-semibold tracking-tight">محصولات</h1>
          <p className="mt-1 text-[length:var(--type-body)] text-muted">فهرست عملیاتی کاتالوگ فروشگاه</p>
        </div>
        <button type="button" onClick={() => setCreateOpen((open) => !open)} className="inline-flex min-h-11 items-center gap-2 rounded-ds bg-primary px-4 text-base font-medium text-primary-foreground shadow-sm" data-testid="admin-create-product">
          <span aria-hidden>+</span>
          محصول جدید
        </button>
      </div>
      {createOpen ? (
        <section className="mb-5 max-w-xl rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm">
          <h2 className="text-base font-semibold">ایجاد محصول کاتالوگ</h2>
          <div className="mt-4 grid gap-3">
            <label className="flex flex-col gap-1 text-sm">
              عنوان
              <input className="min-h-11 rounded-ds border border-border bg-surface px-3" value={createTitle} onChange={(e) => setCreateTitle(e.target.value)} />
            </label>
            <label className="flex flex-col gap-1 text-sm">
              نشانی صفحه (اختیاری)
              <input className="min-h-11 rounded-ds border border-border bg-surface px-3" value={createSlug} onChange={(e) => setCreateSlug(e.target.value)} dir="ltr" />
            </label>
          </div>
          {createError ? <p className="mt-3 text-sm text-danger">{createError}</p> : null}
          <button type="button" disabled={creating} onClick={() => void onCreate()} className="mt-4 inline-flex min-h-11 items-center rounded-ds bg-primary px-5 text-sm font-medium text-primary-foreground disabled:opacity-50">
            {creating ? "در حال ایجاد…" : "ایجاد و انتشار"}
          </button>
        </section>
      ) : null}
      <section className="rounded-2xl border border-border bg-surface-elevated p-2 shadow-sm md:p-4">
        {gridError ? (
          <p className="mb-2 text-sm text-danger" data-testid="list-source">
            اتصال فروشگاه برقرار نیست ({gridError})
          </p>
        ) : null}
        <AppDataGrid<AdminProductListRow>
          columnDefs={columnDefs}
          queryAdapter={queryAdapter}
          advancedFilterColumns={PRODUCT_GRID_ADVANCED_FILTERS}
          externalFilterFields={["title", "updatedAt"]}
          locale="fa"
          direction="rtl"
          savedViewStore={savedViewStore}
          exportFilenameBase="admin-products"
          exportHeaders={["محصول", "کد", "وضعیت", "دسته", "قیمت", "موجودی", "به‌روزرسانی"]}
          getExportRow={(row) => [
            row.title,
            productCode(row.id),
            formatAdminStatus(row.status),
            row.categorySummary,
            row.offerAmountRange,
            String(row.sellableUnits),
            formatJalaliDate(row.updatedAt, "fa"),
          ]}
        />
      </section>
    </main>
  );
}
