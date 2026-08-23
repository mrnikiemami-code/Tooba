"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { DataGrid } from "../../design-system";
import { executeGridQuery } from "../../design-system/data-grid/query-engine";
import type { GridColumnDef, GridServerQuery } from "../../design-system/data-grid";
import { loadAdminProductList, type AdminProductListRow, type HostReadSource } from "./host-client";

const columns: GridColumnDef<AdminProductListRow>[] = [
  {
    id: "title",
    header: "عنوان",
    accessor: (row) => row.title,
    width: 240,
    minWidth: 160,
    maxWidth: 480,
    filterKind: "text",
    sortable: true,
  },
  {
    id: "status",
    header: "وضعیت Catalog",
    accessor: (row) => row.status,
    width: 160,
    minWidth: 120,
    maxWidth: 240,
    filterKind: "status",
    enumOptions: [
      { value: "Published", label: "Published" },
      { value: "Draft", label: "Draft" },
    ],
  },
  {
    id: "variantCount",
    header: "Variants",
    accessor: (row) => row.variantCount,
    width: 120,
    minWidth: 80,
    maxWidth: 160,
    filterKind: "number",
    sortable: true,
  },
  {
    id: "offerCount",
    header: "Offers",
    accessor: (row) => row.offerCount,
    width: 120,
    minWidth: 80,
    maxWidth: 160,
    filterKind: "number",
    sortable: true,
  },
];

/**
 * فهرست Admin با DataGrid پذیرفته‌شده. داده از Host خوانده می‌شود؛ در قطع ارتباط fixture با بنر صریح است.
 */
export function ProductListScreen() {
  const [source, setSource] = useState<HostReadSource>("fixture");
  const [rows, setRows] = useState<AdminProductListRow[]>([]);

  useEffect(() => {
    void loadAdminProductList().then((result) => {
      setSource(result.source);
      setRows(result.rows);
    });
  }, []);

  const queryAdapter = useMemo(
    () => async (query: GridServerQuery) => executeGridQuery(rows, columns, query),
    [rows],
  );

  return (
    <main className="p-4">
      <h1 className="mb-3 text-xl font-semibold">فهرست محصول Admin</h1>
      <p className="mb-2 text-sm text-muted">ورود به Workspace ترکیبی؛ CRUD ماژولی نیست. Price/Stock روی Product نیستند.</p>
      <p className="mb-4 text-sm" data-testid="list-source">
        منبع داده: {source === "host" ? "Host ترکیب‌شده" : "fixture قرارداد (Host در دسترس نبود یا فهرست خالی بود)"}
      </p>
      <DataGrid columns={columns} queryAdapter={queryAdapter} />
      <ul className="mt-4 space-y-2">
        {rows.map((row) => (
          <li key={row.id}>
            <Link className="underline" href={`/admin/products/${row.id}`}>
              گشودن Workspace: {row.title}
            </Link>
          </li>
        ))}
      </ul>
    </main>
  );
}
