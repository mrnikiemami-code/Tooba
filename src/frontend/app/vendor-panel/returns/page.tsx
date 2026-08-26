"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { DataGrid, ErrorState, faWorkspaceMessages } from "../../../design-system";
import { executeGridQuery } from "../../../design-system/data-grid/query-engine";
import type { GridColumnDef, GridServerQuery } from "../../../design-system/data-grid";
import {
  formatReturnDate,
  formatReturnStatus,
  loadSellerReturns,
  returnStatusBadgeClass,
  type ReturnListRow,
} from "../../returns/return-api";
import { readSellerPartyId, type HostReadSource } from "../seller-api";

const columns: GridColumnDef<ReturnListRow>[] = [
  {
    id: "returnRequestId",
    header: "شناسه",
    accessor: (row) => row.returnRequestId,
    cell: (row) => (
      <Link className="font-semibold hover:underline" href={`/vendor-panel/returns/${row.returnRequestId}`}>
        {row.returnRequestId.slice(0, 8)}
      </Link>
    ),
    width: 120,
    minWidth: 96,
    maxWidth: 160,
    sticky: "start",
    filterKind: "text",
    sortable: true,
  },
  {
    id: "sellerOrderId",
    header: "سفارش",
    accessor: (row) => row.sellerOrderId,
    cell: (row) => <span className="tabular-nums text-sm">{row.sellerOrderId.slice(0, 8)}</span>,
    width: 120,
    minWidth: 96,
    maxWidth: 160,
    filterKind: "text",
  },
  {
    id: "itemCount",
    header: "اقلام",
    accessor: (row) => row.itemCount,
    cell: (row) => <span className="tabular-nums">{row.itemCount.toLocaleString("fa-IR")}</span>,
    width: 80,
    minWidth: 64,
    maxWidth: 96,
    sortable: true,
  },
  {
    id: "refundAmount",
    header: "بازپرداخت",
    accessor: (row) => row.refundAmount,
    cell: (row) => <span className="tabular-nums">{row.refundAmount.toLocaleString("fa-IR")}</span>,
    width: 120,
    minWidth: 96,
    maxWidth: 150,
    sortable: true,
  },
  {
    id: "status",
    header: "وضعیت",
    accessor: (row) => row.status,
    cell: (row) => <span className={returnStatusBadgeClass(row.status)}>{formatReturnStatus(row.status)}</span>,
    width: 140,
    minWidth: 120,
    maxWidth: 180,
    filterKind: "status",
  },
  {
    id: "createdAt",
    header: "تاریخ",
    accessor: (row) => row.createdAt,
    cell: (row) => <span className="text-xs text-gray-500">{formatReturnDate(row.createdAt)}</span>,
    width: 140,
    minWidth: 110,
    maxWidth: 180,
    sortable: true,
  },
  {
    id: "open",
    header: "عملیات",
    accessor: (row) => row.returnRequestId,
    cell: (row) => (
      <Link
        className="inline-flex min-h-9 items-center rounded-ds bg-primary px-3 text-sm text-primary-foreground"
        href={`/vendor-panel/returns/${row.returnRequestId}`}
      >
        جزئیات
      </Link>
    ),
    width: 96,
    minWidth: 88,
    maxWidth: 120,
    sortable: false,
  },
];

/** فهرست مرجوعی فروشنده. */
export default function SellerReturnsPage() {
  const [source, setSource] = useState<HostReadSource | "loading">("loading");
  const [rows, setRows] = useState<ReturnListRow[]>([]);
  const [message, setMessage] = useState<string | undefined>(undefined);
  const [denied, setDenied] = useState(false);

  function refresh() {
    const sellerPartyId = readSellerPartyId(window.location.search);
    if (!sellerPartyId) {
      setSource("error");
      setMessage("seller.identity.missing");
      return;
    }
    void loadSellerReturns(sellerPartyId).then((result) => {
      setSource(result.source);
      setRows(result.rows);
      setMessage(result.message);
      setDenied(Boolean(result.denied));
    });
  }

  useEffect(refresh, []);

  const queryAdapter = useMemo(
    () => async (query: GridServerQuery) => executeGridQuery(rows, columns, query),
    [rows],
  );

  if (denied) {
    return (
      <main data-testid="seller-auth-denied">
        <ErrorState
          title="دسترسی مجاز نیست"
          detail="این Actor مجوز مشاهدهٔ مرجوعی این فروشنده را ندارد."
          onRetry={refresh}
          retryLabel={faWorkspaceMessages.retry}
        />
      </main>
    );
  }

  return (
    <main>
      <div className="mb-5">
        <p className="text-sm text-muted">خانه / مرجوعی</p>
        <h1 className="mt-1 text-2xl font-semibold tracking-tight">مرجوعی و بازپرداخت</h1>
        <p className="mt-1 text-base text-muted">بررسی درخواست‌های مرجوعی مشتری</p>
      </div>
      <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
        <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border px-4 py-3 md:px-5">
          <p className="text-sm text-muted">
            {source === "host" ? "دادهٔ زندهٔ Host" : source === "loading" ? "در حال بارگذاری" : "اتصال Host برقرار نیست"}
          </p>
          <span className="rounded-full bg-secondary px-3 py-1 text-xs tabular-nums">
            {rows.length.toLocaleString("fa-IR")} درخواست
          </span>
        </div>
        <div className="p-2 md:p-4">
          {source === "error" ? (
            <ErrorState title="Host در دسترس نیست" detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
          ) : (
            <div className="overflow-x-auto">
              <DataGrid columns={columns} queryAdapter={queryAdapter} />
            </div>
          )}
        </div>
      </section>
    </main>
  );
}
