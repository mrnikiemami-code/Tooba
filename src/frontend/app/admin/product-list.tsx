"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { Edit2, Eye, Trash2 } from "lucide-react";
import { useCallback, useMemo, useRef, useState } from "react";
import type { ColDef, ICellRendererParams } from "ag-grid-community";
import { AppDataGrid, ErrorState, faWorkspaceMessages, formatJalaliDate } from "../../design-system";
import {
  ADMIN_PRODUCT_EXTERNAL_FILTER_FIELDS,
  applyProductGridFilterHeader,
} from "./product-grid-filter-matrix";
import type { AppGridFilterColumnDef } from "../../design-system/app-data-grid/filter-column-def";
import { pinnedGridEdge } from "../../design-system/app-data-grid/grid-direction";
import { useOverflowTooltip } from "../../design-system/app-data-grid/use-overflow-tooltip";
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

function ProductActionsCell({
  row,
  onLifecycle,
}: {
  row: AdminProductListRow;
  onLifecycle: (productId: string, action: "publish" | "unpublish" | "archive" | "delete") => Promise<void>;
}) {
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<string | undefined>();

  async function onDelete() {
    if (!window.confirm(`حذف «${row.title}»؟ در صورت وجود ارجاع، محصول بایگانی می‌شود.`)) {
      return;
    }
    setBusy(true);
    setMessage(undefined);
    try {
      await onLifecycle(row.id, "delete");
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "خطا");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="app-grid-cell-content">
      <div className="relative flex items-center justify-center gap-2 px-1">
        <Link
          href={`/admin/products/${row.id}?scope=view`}
          className="inline-flex size-10 shrink-0 items-center justify-center rounded-full border border-border bg-surface text-muted transition-colors hover:bg-secondary hover:text-foreground"
          aria-label="مشاهده"
          title="مشاهده"
          data-testid={`admin-product-view-${row.id}`}
        >
          <Eye className="size-4" aria-hidden />
        </Link>
        <Link
          href={`/admin/products/${row.id}`}
          className="inline-flex size-10 shrink-0 items-center justify-center rounded-full border border-border bg-surface text-muted transition-colors hover:bg-secondary hover:text-foreground"
          aria-label="ویرایش"
          title="ویرایش"
          data-testid={`admin-product-edit-${row.id}`}
        >
          <Edit2 className="size-4" aria-hidden />
        </Link>
        <button
          type="button"
          disabled={busy}
          onClick={() => void onDelete()}
          className="inline-flex size-10 shrink-0 items-center justify-center rounded-full border border-danger/30 bg-surface text-danger transition-colors hover:bg-danger/10 disabled:opacity-50"
          aria-label="حذف"
          title="حذف"
          data-testid={`admin-product-delete-${row.id}`}
        >
          <Trash2 className="size-4" aria-hidden />
        </button>
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
    <div className="app-grid-cell-content app-grid-cell-media">
      <div className="flex w-full items-center justify-end gap-2">
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
  const rootRef = useRef<HTMLDivElement>(null);
  const row = params.data;
  const title = row?.title ?? "";
  const code = row ? productCode(row.id) : "";
  useOverflowTooltip(params, row ? `${title}\n${code}` : "", rootRef);
  if (!row) return null;
  return (
    <div ref={rootRef} className="app-grid-cell-content">
      <Link className="block min-w-0 text-right hover:underline" href={`/admin/products/${row.id}`}>
      <span data-overflow-measure className="block truncate text-sm font-semibold leading-snug">{row.title}</span>
      <span data-overflow-measure className="mt-0.5 block truncate text-xs text-muted" dir="ltr">
        {code}
      </span>
    </Link>
    </div>
  );
}

function StatusCell(params: ICellRendererParams<AdminProductListRow>) {
  const rootRef = useRef<HTMLSpanElement>(null);
  const label = formatAdminStatus(String(params.value ?? ""));
  useOverflowTooltip(params, label, rootRef);
  return (
    <div className="app-grid-cell-content">
      <span ref={rootRef} data-overflow-measure className={productStatusClass(String(params.value ?? ""))}>
        {label}
      </span>
    </div>
  );
}

function StockCell(params: ICellRendererParams<AdminProductListRow>) {
  const rootRef = useRef<HTMLSpanElement>(null);
  const label = Number(params.value ?? 0).toLocaleString("fa-IR");
  useOverflowTooltip(params, label, rootRef);
  return (
    <div className="app-grid-cell-content">
      <span ref={rootRef} data-overflow-measure className={stockClass(Number(params.value ?? 0))}>
        {label}
      </span>
    </div>
  );
}

const PRODUCT_STATUS_FILTER_OPTIONS = [
  { value: "Published", label: "منتشر شده" },
  { value: "Draft", label: "پیش‌نویس" },
  { value: "Archived", label: "بایگانی" },
] as const;

function buildColumnDefs(
  onLifecycle: (productId: string, action: "publish" | "unpublish" | "archive" | "delete") => Promise<void>,
): ColDef<AdminProductListRow>[] {
  const actionsPin = pinnedGridEdge("rtl");

  return [
    applyProductGridFilterHeader({
      colId: "media",
      headerName: "رسانه",
      width: 88,
      minWidth: 80,
      sortable: false,
      cellRenderer: MediaCell,
    }),
    applyProductGridFilterHeader({
      field: "title",
      headerName: "محصول",
      minWidth: 220,
      flex: 1.4,
      cellRenderer: ProductCell,
    }),
    applyProductGridFilterHeader({
      field: "status",
      headerName: "وضعیت",
      width: 120,
      valueFormatter: (p) => formatAdminStatus(String(p.value ?? "")),
      cellRenderer: StatusCell,
    }),
    applyProductGridFilterHeader({ field: "categorySummary", headerName: "دسته", width: 130 }),
    applyProductGridFilterHeader({ field: "offerAmountRange", headerName: "قیمت (تومان)", width: 150 }),
    applyProductGridFilterHeader({
      field: "sellableUnits",
      headerName: "موجودی",
      width: 100,
      cellRenderer: StockCell,
    }),
    applyProductGridFilterHeader({
      field: "updatedAt",
      headerName: "به‌روزرسانی",
      width: 120,
      valueFormatter: (p) => formatJalaliDate(String(p.value ?? ""), "fa"),
    }),
    applyProductGridFilterHeader({ field: "variantCount", headerName: "گونه", width: 90, hide: true }),
    applyProductGridFilterHeader({ field: "offerCount", headerName: "پیشنهاد", width: 100, hide: true }),
    applyProductGridFilterHeader({ field: "locationCount", headerName: "محل", width: 90, hide: true }),
    applyProductGridFilterHeader({
      colId: "actions",
      headerName: "عملیات",
      width: 188,
      minWidth: 176,
      maxWidth: 240,
      sortable: false,
      lockVisible: true,
      lockPinned: true,
      lockPosition: actionsPin,
      pinned: actionsPin,
      cellClass: "app-grid-cell-align-center",
      cellRenderer: (params: ICellRendererParams<AdminProductListRow>) =>
        params.data ? <ProductActionsCell row={params.data} onLifecycle={onLifecycle} /> : null,
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
  { id: "variantCount", header: "گونه", filterKind: "number" },
  { id: "offerCount", header: "پیشنهاد", filterKind: "number" },
  { id: "categorySummary", header: "دسته", filterKind: "text" },
  { id: "offerAmountRange", header: "قیمت (تومان)", filterKind: "number" },
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
          externalFilterFields={ADMIN_PRODUCT_EXTERNAL_FILTER_FIELDS}
          statusFilterOptions={[...PRODUCT_STATUS_FILTER_OPTIONS]}
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
