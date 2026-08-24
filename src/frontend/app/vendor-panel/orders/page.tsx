"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { DataGrid, ErrorState, faWorkspaceMessages } from "../../../design-system";
import { executeGridQuery } from "../../../design-system/data-grid/query-engine";
import type { GridColumnDef, GridServerQuery } from "../../../design-system/data-grid";
import {
  loadSellerOrders,
  readSellerPartyId,
  type HostReadSource,
  type SellerOrderListRow,
} from "../seller-api";

const columns: GridColumnDef<SellerOrderListRow>[] = [
  {
    id: "orderNumber",
    header: "شماره",
    accessor: (row) => row.orderNumber,
    cell: (row) => (
      <Link className="font-semibold hover:underline" href={`/vendor-panel/orders/${row.sellerOrderId}`}>
        {row.orderNumber}
      </Link>
    ),
    width: 140,
    minWidth: 110,
    maxWidth: 180,
    sticky: "start",
    filterKind: "text",
    sortable: true,
  },
  {
    id: "submittedAt",
    header: "تاریخ",
    accessor: (row) => row.submittedAt,
    cell: (row) => <span className="tabular-nums text-sm">{row.submittedAt ? row.submittedAt.slice(0, 10) : "—"}</span>,
    width: 110,
    minWidth: 96,
    maxWidth: 140,
    sortable: true,
  },
  {
    id: "recipientName",
    header: "گیرنده",
    accessor: (row) => row.recipientName,
    width: 140,
    minWidth: 110,
    maxWidth: 200,
    filterKind: "text",
    sortable: true,
  },
  {
    id: "lineCount",
    header: "خطوط",
    accessor: (row) => row.lineCount,
    width: 80,
    minWidth: 64,
    maxWidth: 100,
    align: "end",
    filterKind: "number",
    sortable: true,
  },
  {
    id: "payableAmount",
    header: "مبلغ",
    accessor: (row) => row.payableAmount,
    cell: (row) => (
      <span className="tabular-nums">
        {row.payableAmount.toLocaleString("fa-IR")} {row.currency}
      </span>
    ),
    width: 140,
    minWidth: 110,
    maxWidth: 180,
    sortable: true,
    filterKind: "number",
  },
  {
    id: "paymentState",
    header: "پرداخت",
    accessor: (row) => row.paymentState,
    width: 110,
    minWidth: 96,
    maxWidth: 140,
    filterKind: "status",
  },
  {
    id: "status",
    header: "وضعیت",
    accessor: (row) => row.status,
    width: 110,
    minWidth: 96,
    maxWidth: 140,
    filterKind: "status",
  },
  {
    id: "open",
    header: "عملیات",
    accessor: (row) => row.sellerOrderId,
    cell: (row) => (
      <Link className="text-primary underline-offset-4 hover:underline" href={`/vendor-panel/orders/${row.sellerOrderId}`}>
        جزئیات
      </Link>
    ),
    width: 88,
    minWidth: 80,
    maxWidth: 120,
    sortable: false,
  },
];

/**
 * فهرست سفارش‌های فقط همین فروشنده با Data Grid.
 */
export default function VendorOrdersPage() {
  const [source, setSource] = useState<HostReadSource | "loading">("loading");
  const [rows, setRows] = useState<SellerOrderListRow[]>([]);
  const [message, setMessage] = useState<string | undefined>(undefined);

  function refresh() {
    const sellerPartyId = readSellerPartyId(window.location.search);
    if (!sellerPartyId) {
      setSource("error");
      setMessage("seller.identity.missing");
      return;
    }
    void loadSellerOrders(sellerPartyId).then((result) => {
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
      <div className="mb-5">
        <h1 className="text-[length:var(--type-title)] font-semibold tracking-tight">سفارش‌های فروشنده</h1>
        <p className="mt-1 text-[length:var(--type-body)] text-muted">فقط برش سفارش همین فروشنده</p>
      </div>
      <p className="mb-4 text-base text-muted" data-testid="seller-orders-source">
        {source === "host" ? "دادهٔ زندهٔ Host" : source === "loading" ? "در حال بارگذاری" : "اتصال Host برقرار نیست"}
      </p>
      {source === "error" ? (
        <ErrorState title="Host در دسترس نیست" detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
      ) : (
        <DataGrid columns={columns} queryAdapter={queryAdapter} />
      )}
    </main>
  );
}
