"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useMemo, useState } from "react";
import type { ColDef, ICellRendererParams } from "ag-grid-community";
import { AppDataGrid, ErrorState, faWorkspaceMessages, formatJalaliDate } from "../../design-system";
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

function productStatusClass(status: string): string {
  if (status === "Published") return "rounded-ds bg-success/15 px-2 py-1 text-sm text-success";
  if (status === "Archived") return "rounded-ds bg-secondary px-2 py-1 text-sm text-muted";
  return "rounded-ds bg-secondary px-2 py-1 text-sm";
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
    <div className="relative">
      <details className="group">
        <summary className="cursor-pointer list-none rounded-ds border border-border px-2 py-1 text-sm marker:content-none [&::-webkit-details-marker]:hidden">
          عملیات
        </summary>
        <div className="absolute end-0 z-20 mt-1 min-w-[10rem] rounded-ds border border-border bg-surface-elevated p-1 shadow-md">
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
      {message ? <p className="mt-1 max-w-[10rem] text-xs text-danger">{message}</p> : null}
    </div>
  );
}

function TitleCell(params: ICellRendererParams<AdminProductListRow>) {
  const row = params.data;
  if (!row) return null;
  const thumb = row.primaryMediaAssetId ? storefrontMediaUrl(row.primaryMediaAssetId) : null;
  return (
    <Link className="flex min-w-0 items-center gap-3 hover:underline" href={`/admin/products/${row.id}`}>
      {thumb ? (
        <img src={thumb} alt="" className="size-10 shrink-0 rounded-ds border border-border object-cover bg-secondary" />
      ) : (
        <span className="flex size-10 shrink-0 items-center justify-center rounded-ds bg-secondary text-xs text-muted">تصویر</span>
      )}
      <span className="min-w-0">
        <span className="block truncate font-semibold">{row.title}</span>
        <span className="block truncate text-sm text-muted">{row.categorySummary}</span>
      </span>
    </Link>
  );
}

function buildColumnDefs(
  onLifecycle: (productId: string, action: "publish" | "unpublish" | "archive" | "delete") => Promise<void>,
): ColDef<AdminProductListRow>[] {
  return [
    { field: "title", headerName: "محصول", minWidth: 220, cellRenderer: TitleCell, filter: "agTextColumnFilter" },
    {
      field: "status",
      headerName: "انتشار",
      width: 120,
      valueFormatter: (p) => formatAdminStatus(String(p.value ?? "")),
      cellClass: (p) => productStatusClass(String(p.value ?? "")),
      filter: "agSetColumnFilter",
      filterParams: { values: ["Published", "Draft", "Archived"] },
    },
    { field: "variantCount", headerName: "گونه", width: 90, filter: "agNumberColumnFilter" },
    { field: "offerCount", headerName: "پیشنهاد", width: 100, filter: "agNumberColumnFilter" },
    { field: "categorySummary", headerName: "دسته", width: 140, filter: "agTextColumnFilter" },
    { field: "offerAmountRange", headerName: "قیمت", width: 150 },
    { field: "sellableUnits", headerName: "موجود", width: 100, filter: "agNumberColumnFilter" },
    { field: "locationCount", headerName: "محل", width: 90, hide: true, filter: "agNumberColumnFilter" },
    {
      field: "updatedAt",
      headerName: "به‌روزرسانی",
      width: 120,
      valueFormatter: (p) => formatJalaliDate(String(p.value ?? ""), "fa"),
      filter: "agDateColumnFilter",
    },
    {
      colId: "actions",
      headerName: "عملیات",
      width: 110,
      sortable: false,
      filter: false,
      cellRenderer: (params: ICellRendererParams<AdminProductListRow>) =>
        params.data ? <ProductActionMenu row={params.data} onLifecycle={onLifecycle} /> : null,
      pinned: "right",
    },
  ];
}

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
          <p className="mt-1 text-[length:var(--type-body)] text-muted">فهرست عملیاتی کاتالوگ — AG Grid + API server query</p>
        </div>
        <button type="button" onClick={() => setCreateOpen((open) => !open)} className="min-h-11 rounded-ds bg-primary px-4 text-base font-medium text-primary-foreground" data-testid="admin-create-product">
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
      <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated p-2 shadow-sm md:p-4">
        {gridError ? (
          <p className="mb-2 text-sm text-danger" data-testid="list-source">
            اتصال فروشگاه برقرار نیست ({gridError})
          </p>
        ) : (
          <p className="mb-2 text-sm text-muted" data-testid="list-source">
            دادهٔ زندهٔ فروشگاه — server GridQuery
          </p>
        )}
        <AppDataGrid<AdminProductListRow>
          columnDefs={columnDefs}
          queryAdapter={queryAdapter}
          locale="fa"
          direction="rtl"
          savedViewStore={savedViewStore}
          exportFilenameBase="admin-products"
          exportHeaders={["محصول", "انتشار", "گونه", "پیشنهاد", "دسته", "قیمت", "موجود", "به‌روزرسانی"]}
          getExportRow={(row) => [
            row.title,
            formatAdminStatus(row.status),
            String(row.variantCount),
            String(row.offerCount),
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
