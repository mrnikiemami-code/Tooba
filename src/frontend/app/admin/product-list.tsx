"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { DataGrid, ErrorState, faWorkspaceMessages } from "../../design-system";
import { executeGridQuery } from "../../design-system/data-grid/query-engine";
import type { GridColumnDef, GridServerQuery } from "../../design-system/data-grid";
import { loadAdminProductList, type AdminProductListRow, type HostReadSource } from "./host-client";

const columns: GridColumnDef<AdminProductListRow>[] = [
  {
    id: "title",
    header: "محصول",
    accessor: (row) => row.title,
    cell: (row) => (
      <Link className="font-medium text-foreground hover:underline" href={`/admin/products/${row.id}`}>
        {row.title}
      </Link>
    ),
    width: 360,
    minWidth: 220,
    maxWidth: 560,
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
    width: 140,
    minWidth: 120,
    maxWidth: 200,
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
    width: 100,
    minWidth: 80,
    maxWidth: 140,
    filterKind: "number",
    sortable: true,
  },
  {
    id: "offerCount",
    header: "پیشنهاد فروشنده",
    accessor: (row) => row.offerCount,
    width: 140,
    minWidth: 100,
    maxWidth: 180,
    filterKind: "number",
    sortable: true,
  },
  {
    id: "categorySummary",
    header: "دسته",
    accessor: (row) => row.categorySummary,
    width: 180,
    minWidth: 120,
    maxWidth: 280,
    filterKind: "text",
    sortable: true,
  },
  {
    id: "offerAmountRange",
    header: "بازهٔ مبلغ پیشنهاد",
    accessor: (row) => row.offerAmountRange,
    width: 200,
    minWidth: 140,
    maxWidth: 280,
    sortable: true,
  },
  {
    id: "sellableUnits",
    header: "قابل‌فروش",
    accessor: (row) => row.sellableUnits,
    width: 120,
    minWidth: 90,
    maxWidth: 160,
    filterKind: "number",
    sortable: true,
  },
  {
    id: "locationCount",
    header: "محل",
    accessor: (row) => row.locationCount,
    width: 90,
    minWidth: 70,
    maxWidth: 120,
    filterKind: "number",
    sortable: true,
  },
  {
    id: "updatedAt",
    header: "به‌روزرسانی",
    accessor: (row) => row.updatedAt,
    cell: (row) => <span className="text-sm tabular-nums">{row.updatedAt ? row.updatedAt.slice(0, 10) : "—"}</span>,
    width: 130,
    minWidth: 110,
    maxWidth: 180,
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
    width: 110,
    minWidth: 90,
    maxWidth: 140,
    sortable: false,
  },
];

/**
 * فهرست Admin با DataGrid پذیرفته‌شده. داده از Host خوانده می‌شود؛ در قطع ارتباط fixture با بنر صریح است.
 */
export function ProductListScreen() {
  const [source, setSource] = useState<HostReadSource | "loading">("loading");
  const [rows, setRows] = useState<AdminProductListRow[]>([]);
  const [message, setMessage] = useState<string | undefined>(undefined);

  function refresh() {
    void loadAdminProductList().then((result) => {
      setSource(result.source);
      setRows(result.rows);
      setMessage(result.message);
    });
  }

  useEffect(() => {
    refresh();
  }, []);

  const queryAdapter = useMemo(
    () => async (query: GridServerQuery) => executeGridQuery(rows, columns, query),
    [rows],
  );

  return (
    <main className="w-full p-6 md:p-8">
      <div className="mb-5 flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-[length:var(--type-title)] font-semibold tracking-tight">محصولات</h1>
          <p className="mt-1 text-[length:var(--type-body)] text-muted">قیمت و موجودی روی هویت Product نیستند؛ ورود به Workspace ترکیبی است.</p>
        </div>
        <button type="button" disabled className="min-h-11 rounded-ds bg-primary px-4 text-base font-medium text-primary-foreground opacity-50">
          محصول جدید
        </button>
      </div>
      <p className="mb-4 text-base text-muted" data-testid="list-source">
        {source === "host" ? "فهرست عملیاتی فروشگاه" : source === "loading" ? "در حال بارگذاری فهرست" : "اتصال فروشگاه برقرار نیست"}
      </p>
      {source === "error" ? (
        <ErrorState title="Host در دسترس نیست" detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
      ) : (
        <DataGrid columns={columns} queryAdapter={queryAdapter} />
      )}
    </main>
  );
}
