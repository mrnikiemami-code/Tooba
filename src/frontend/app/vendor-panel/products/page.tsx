"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { DataGrid, ErrorState, faWorkspaceMessages } from "../../../design-system";
import { executeGridQuery } from "../../../design-system/data-grid/query-engine";
import type { GridColumnDef, GridServerQuery } from "../../../design-system/data-grid";
import {
  formatMoney,
  formatOfferStatus,
  formatUnits,
  loadSellerOffers,
  readSellerPartyId,
  type HostReadSource,
  type SellerOfferListRow,
} from "../seller-api";

const columns: GridColumnDef<SellerOfferListRow>[] = [
  {
    id: "productTitle",
    header: "محصول",
    accessor: (row) => row.productTitle,
    cell: (row) => (
      <Link className="block min-w-0 hover:underline" href={`/vendor-panel/products/${row.offerId}`}>
        <span className="block truncate font-semibold">{row.productTitle}</span>
        <span className="block truncate text-xs text-muted">{row.sellerSku ?? "بدون SKU"}</span>
      </Link>
    ),
    width: 220,
    minWidth: 160,
    maxWidth: 300,
    sticky: "start",
    filterKind: "text",
    sortable: true,
  },
  {
    id: "sellerSku",
    header: "SKU فروشنده",
    accessor: (row) => row.sellerSku ?? "",
    cell: (row) => <span dir="ltr">{row.sellerSku ?? "—"}</span>,
    width: 120,
    minWidth: 96,
    maxWidth: 160,
    filterKind: "text",
    sortable: true,
  },
  {
    id: "status",
    header: "وضعیت Offer",
    accessor: (row) => row.status,
    cell: (row) => (
      <span
        className={
          row.status === "Active"
            ? "inline-flex rounded-full bg-[rgb(220_252_231)] px-2.5 py-1 text-xs font-medium text-[rgb(22_163_74)]"
            : "inline-flex rounded-full bg-secondary px-2.5 py-1 text-xs font-medium"
        }
      >
        {formatOfferStatus(row.status)}
      </span>
    ),
    width: 110,
    minWidth: 96,
    maxWidth: 140,
    filterKind: "status",
    enumOptions: [
      { value: "Active", label: "فعال" },
      { value: "Suspended", label: "معلق" },
      { value: "Draft", label: "پیش‌نویس" },
    ],
  },
  {
    id: "amount",
    header: "قیمت",
    accessor: (row) => row.amount ?? 0,
    cell: (row) => <span className="tabular-nums">{formatMoney(row.amount, row.currency)}</span>,
    width: 140,
    minWidth: 110,
    maxWidth: 180,
    sortable: true,
    filterKind: "number",
  },
  {
    id: "availableUnits",
    header: "موجودی",
    accessor: (row) => row.availableUnits,
    cell: (row) => (
      <span className="tabular-nums">
        {row.availableUnits <= 0 ? "ناموجود" : formatUnits(row.availableUnits)}
      </span>
    ),
    width: 110,
    minWidth: 88,
    maxWidth: 140,
    align: "end",
    filterKind: "number",
    sortable: true,
  },
  {
    id: "lastUpdatedAt",
    header: "به‌روزرسانی",
    accessor: (row) => row.lastUpdatedAt ?? "",
    cell: (row) => (
      <span className="tabular-nums text-sm">
        {row.lastUpdatedAt ? row.lastUpdatedAt.slice(0, 10) : "—"}
      </span>
    ),
    width: 120,
    minWidth: 100,
    maxWidth: 150,
    sortable: true,
  },
  {
    id: "open",
    header: "عملیات",
    accessor: (row) => row.offerId,
    cell: (row) => (
      <Link
        className="inline-flex min-h-9 items-center rounded-ds bg-primary px-3 text-sm text-primary-foreground"
        href={`/vendor-panel/products/${row.offerId}`}
      >
        ویرایش
      </Link>
    ),
    width: 96,
    minWidth: 88,
    maxWidth: 120,
    sortable: false,
  },
];

/**
 * فهرست Offerهای فروشنده با Data Grid داخل زبان بصری Vendor.
 */
export default function VendorProductsPage() {
  const [source, setSource] = useState<HostReadSource | "loading">("loading");
  const [rows, setRows] = useState<SellerOfferListRow[]>([]);
  const [message, setMessage] = useState<string | undefined>(undefined);
  const [denied, setDenied] = useState(false);

  function refresh() {
    const sellerPartyId = readSellerPartyId(window.location.search);
    if (!sellerPartyId) {
      setSource("error");
      setMessage("seller.identity.missing");
      return;
    }
    void loadSellerOffers(sellerPartyId).then((result) => {
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
          detail="این Actor مجوز مشاهدهٔ محصولات این فروشنده را ندارد."
          onRetry={refresh}
          retryLabel={faWorkspaceMessages.retry}
        />
      </main>
    );
  }

  return (
    <main>
      <div className="mb-5 flex flex-wrap items-end justify-between gap-3">
        <div>
          <p className="text-sm text-muted">خانه / محصولات</p>
          <h1 className="mt-1 text-2xl font-semibold tracking-tight">محصولات فروشنده</h1>
          <p className="mt-1 text-base text-muted">فهرست Offer؛ قیمت روی Product نیست</p>
        </div>
      </div>

      <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
        <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border px-4 py-3 md:px-5">
          <p className="text-sm text-muted" data-testid="seller-products-source">
            {source === "host" ? "دادهٔ زندهٔ Host" : source === "loading" ? "در حال بارگذاری" : "اتصال Host برقرار نیست"}
          </p>
          <span className="rounded-full bg-secondary px-3 py-1 text-xs tabular-nums">
            {rows.length.toLocaleString("fa-IR")} ردیف
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
