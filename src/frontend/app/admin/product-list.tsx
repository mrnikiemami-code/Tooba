"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useMemo, useState } from "react";
import { DataGrid, ErrorState, faWorkspaceMessages } from "../../design-system";
import { executeGridQuery } from "../../design-system/data-grid/query-engine";
import type { GridColumnDef, GridServerQuery } from "../../design-system/data-grid";
import {
  createAdminProduct,
  loadAdminProductList,
  type AdminProductListRow,
  type HostReadSource,
} from "./host-client";

const columns: GridColumnDef<AdminProductListRow>[] = [
  {
    id: "title",
    header: "محصول",
    accessor: (row) => row.title,
    cell: (row) => (
      <Link className="flex min-w-0 items-center gap-3 hover:underline" href={`/admin/products/${row.id}`}>
        <span className="flex size-10 shrink-0 items-center justify-center rounded-ds bg-secondary text-xs text-muted">تصویر</span>
        <span className="min-w-0">
          <span className="block truncate font-semibold">{row.title}</span>
          <span className="block truncate text-sm text-muted">{row.categorySummary}</span>
        </span>
      </Link>
    ),
    width: 240,
    minWidth: 180,
    maxWidth: 320,
    sticky: "start",
    filterKind: "text",
    sortable: true,
  },
  {
    id: "status",
    header: "انتشار",
    accessor: (row) => row.status,
    cell: (row) => (
      <span className="rounded-ds bg-success/15 px-2 py-1 text-sm text-success">{row.status === "Published" ? "منتشرشده" : row.status}</span>
    ),
    width: 110,
    minWidth: 96,
    maxWidth: 140,
    filterKind: "status",
    enumOptions: [
      { value: "Published", label: "منتشرشده" },
      { value: "Draft", label: "پیش‌نویس" },
    ],
  },
  {
    id: "variantCount",
    header: "گونه",
    accessor: (row) => row.variantCount,
    width: 72,
    minWidth: 64,
    maxWidth: 100,
    filterKind: "number",
    sortable: true,
  },
  {
    id: "offerCount",
    header: "پیشنهاد",
    accessor: (row) => row.offerCount,
    width: 80,
    minWidth: 64,
    maxWidth: 110,
    filterKind: "number",
    sortable: true,
  },
  {
    id: "categorySummary",
    header: "دسته",
    accessor: (row) => row.categorySummary,
    width: 130,
    minWidth: 100,
    maxWidth: 180,
    filterKind: "text",
    sortable: true,
  },
  {
    id: "offerAmountRange",
    header: "قیمت",
    accessor: (row) => row.offerAmountRange,
    width: 150,
    minWidth: 120,
    maxWidth: 200,
    sortable: true,
  },
  {
    id: "sellableUnits",
    header: "موجود",
    accessor: (row) => row.sellableUnits,
    width: 88,
    minWidth: 72,
    maxWidth: 120,
    align: "end",
    filterKind: "number",
    sortable: true,
  },
  {
    id: "locationCount",
    header: "محل",
    accessor: (row) => row.locationCount,
    width: 72,
    minWidth: 64,
    maxWidth: 100,
    filterKind: "number",
    sortable: true,
    defaultVisible: false,
  },
  {
    id: "updatedAt",
    header: "به‌روزرسانی",
    accessor: (row) => row.updatedAt,
    cell: (row) => <span className="text-sm tabular-nums">{row.updatedAt ? row.updatedAt.slice(0, 10) : "—"}</span>,
    width: 108,
    minWidth: 96,
    maxWidth: 140,
    sortable: true,
  },
  {
    id: "open",
    header: "عملیات",
    accessor: (row) => row.id,
    cell: (row) => (
      <Link className="text-primary underline-offset-4 hover:underline" href={`/admin/products/${row.id}`}>
        باز کردن
      </Link>
    ),
    width: 88,
    minWidth: 80,
    maxWidth: 120,
    sortable: false,
  },
];

/**
 * فهرست Admin با DataGrid پذیرفته‌شده. داده از Host خوانده می‌شود؛ در قطع ارتباط fixture با بنر صریح است.
 */
export function ProductListScreen() {
  const router = useRouter();
  const [source, setSource] = useState<HostReadSource | "loading">("loading");
  const [rows, setRows] = useState<AdminProductListRow[]>([]);
  const [message, setMessage] = useState<string | undefined>(undefined);
  const [denied, setDenied] = useState(false);
  const [creating, setCreating] = useState(false);
  const [createOpen, setCreateOpen] = useState(false);
  const [createTitle, setCreateTitle] = useState("");
  const [createSlug, setCreateSlug] = useState("");
  const [createError, setCreateError] = useState<string | undefined>(undefined);

  function refresh() {
    void loadAdminProductList().then((result) => {
      setSource(result.source);
      setRows(result.rows);
      setMessage(result.message);
      setDenied(Boolean(result.denied));
    });
  }

  useEffect(() => {
    refresh();
  }, []);

  const queryAdapter = useMemo(
    () => async (query: GridServerQuery) => executeGridQuery(rows, columns, query),
    [rows],
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
        <ErrorState title="دسترسی مجاز نیست" detail="Host هویت فعلی را مدیر تشخیص نداد." onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
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
        <button
          type="button"
          onClick={() => setCreateOpen((open) => !open)}
          className="min-h-11 rounded-ds bg-primary px-4 text-base font-medium text-primary-foreground"
          data-testid="admin-create-product"
        >
          محصول جدید
        </button>
      </div>
      {createOpen ? (
        <section className="mb-5 max-w-xl rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm">
          <h2 className="text-base font-semibold">ایجاد محصول Catalog</h2>
          <p className="mt-1 text-sm text-muted">عنوان + slug + گونهٔ پیش‌فرض؛ قیمت و موجودی روی Product نیست</p>
          <div className="mt-4 grid gap-3">
            <label className="flex flex-col gap-1 text-sm">
              عنوان
              <input
                className="min-h-11 rounded-ds border border-border bg-surface px-3"
                value={createTitle}
                onChange={(event) => setCreateTitle(event.target.value)}
              />
            </label>
            <label className="flex flex-col gap-1 text-sm">
              slug (اختیاری)
              <input
                className="min-h-11 rounded-ds border border-border bg-surface px-3"
                value={createSlug}
                onChange={(event) => setCreateSlug(event.target.value)}
                dir="ltr"
              />
            </label>
          </div>
          {createError ? <p className="mt-3 text-sm text-danger">{createError}</p> : null}
          <button
            type="button"
            disabled={creating}
            onClick={() => void onCreate()}
            className="mt-4 inline-flex min-h-11 items-center rounded-ds bg-primary px-5 text-sm font-medium text-primary-foreground disabled:opacity-50"
          >
            {creating ? "در حال ایجاد…" : "ایجاد و انتشار"}
          </button>
        </section>
      ) : null}
      <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
        <div className="flex items-center justify-between gap-3 border-b border-border px-4 py-3 md:px-5">
          <p className="text-sm text-muted" data-testid="list-source">
            {source === "host" ? "دادهٔ زندهٔ Host" : source === "loading" ? "در حال بارگذاری فهرست" : "اتصال فروشگاه برقرار نیست"}
          </p>
          <span className="rounded-full bg-secondary px-3 py-1 text-xs">{rows.length.toLocaleString("fa-IR")} محصول</span>
        </div>
        <div className="p-2 md:p-4">
          {source === "error" ? (
            <ErrorState title="Host در دسترس نیست" detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
          ) : (
            <DataGrid columns={columns} queryAdapter={queryAdapter} />
          )}
        </div>
      </section>
    </main>
  );
}
