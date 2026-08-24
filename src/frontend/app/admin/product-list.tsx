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
    width: 280,
    minWidth: 180,
    maxWidth: 480,
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
    <main className="mx-auto max-w-6xl p-6">
      <div className="mb-6 flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">محصولات</h1>
          <p className="mt-1 text-base text-muted">قیمت و موجودی روی هویت Product نیستند؛ ورود به Workspace ترکیبی است.</p>
        </div>
        <button type="button" disabled className="min-h-11 rounded-ds bg-primary px-4 text-sm font-medium text-primary-foreground opacity-50">
          محصول جدید
        </button>
      </div>
      <p className="mb-4 text-sm text-muted" data-testid="list-source">
        {source === "host" ? "داده از Host زنده" : source === "loading" ? "در حال خواندن Host" : "Host در دسترس نیست"}
      </p>
      {source === "error" ? (
        <ErrorState title="Host در دسترس نیست" detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
      ) : (
        <DataGrid columns={columns} queryAdapter={queryAdapter} />
      )}
    </main>
  );
}
