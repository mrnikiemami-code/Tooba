"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { ShoppingBag } from "lucide-react";
import { DataGrid, ErrorState, faWorkspaceMessages } from "../../../design-system";
import { executeGridQuery } from "../../../design-system/data-grid/query-engine";
import type { GridColumnDef, GridServerQuery } from "../../../design-system/data-grid";
import {
  formatMoney,
  formatPaymentState,
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
    cell: (row) => <span className="tabular-nums">{row.lineCount.toLocaleString("fa-IR")}</span>,
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
    cell: (row) => <span className="tabular-nums">{formatMoney(row.payableAmount, row.currency)}</span>,
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
    cell: (row) => (
      <span className="inline-flex rounded-full bg-secondary px-2.5 py-1 text-xs font-medium">
        {formatPaymentState(row.paymentState)}
      </span>
    ),
    width: 130,
    minWidth: 110,
    maxWidth: 160,
    filterKind: "status",
  },
  {
    id: "status",
    header: "وضعیت",
    accessor: (row) => row.status,
    cell: (row) => (
      <span className="inline-flex rounded-full bg-secondary px-2.5 py-1 text-xs font-medium">
        {formatPaymentState(row.status)}
      </span>
    ),
    width: 130,
    minWidth: 110,
    maxWidth: 160,
    filterKind: "status",
  },
  {
    id: "open",
    header: "عملیات",
    accessor: (row) => row.sellerOrderId,
    cell: (row) => (
      <Link
        className="inline-flex min-h-9 items-center rounded-ds bg-primary px-3 text-sm text-primary-foreground"
        href={`/vendor-panel/orders/${row.sellerOrderId}`}
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

/**
 * فهرست سفارش‌های فقط همین فروشنده با Data Grid داخل پوستهٔ Vendor.
 */
export default function VendorOrdersPage() {
  const [source, setSource] = useState<HostReadSource | "loading">("loading");
  const [rows, setRows] = useState<SellerOrderListRow[]>([]);
  const [message, setMessage] = useState<string | undefined>(undefined);
  const [denied, setDenied] = useState(false);

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

  if (denied) {
    return (
      <main data-testid="seller-auth-denied">
        <ErrorState
          title="دسترسی مجاز نیست"
          detail="این Actor مجوز مشاهدهٔ سفارش‌های این فروشنده را ندارد."
          onRetry={refresh}
          retryLabel={faWorkspaceMessages.retry}
        />
      </main>
    );
  }

  const paidCount = rows.filter((r) => r.paymentState === "Paid").length;
  const pendingCount = rows.filter((r) => r.paymentState !== "Paid" && r.paymentState !== "Cancelled").length;

  return (
    <main className="space-y-4">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div className="flex items-center gap-2">
          <div className="w-10 h-10 rounded-xl bg-[#2563EB]/10 flex items-center justify-center">
            <ShoppingBag className="w-5 h-5 text-[#2563EB]" />
          </div>
          <div>
            <h2 className="text-lg font-bold text-gray-900">مدیریت سفارشات</h2>
            <p className="text-xs text-gray-500">
              {rows.length.toLocaleString("fa-IR")} سفارش · {paidCount.toLocaleString("fa-IR")} پرداخت‌شده ·{" "}
              {pendingCount.toLocaleString("fa-IR")} در انتظار
            </p>
          </div>
        </div>
      </div>

      <section className="bg-white rounded-2xl border border-gray-200 overflow-hidden shadow-sm">
        <div className="flex flex-wrap items-center justify-between gap-3 border-b border-gray-200 px-4 py-3 md:px-5">
          <p className="text-sm text-gray-500" data-testid="seller-orders-source">
            {source === "host" ? "دادهٔ زندهٔ Host" : source === "loading" ? "در حال بارگذاری" : "اتصال Host برقرار نیست"}
          </p>
          <span className="rounded-full bg-gray-100 px-3 py-1 text-xs tabular-nums text-gray-700">
            {rows.length.toLocaleString("fa-IR")} سفارش
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
