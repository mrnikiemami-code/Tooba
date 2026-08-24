"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { DataGrid, ErrorState, faWorkspaceMessages } from "../../../design-system";
import { executeGridQuery } from "../../../design-system/data-grid/query-engine";
import type { GridColumnDef, GridServerQuery } from "../../../design-system/data-grid";
import {
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
        <span className="block truncate text-sm text-muted" dir="ltr">
          {row.offerId.slice(0, 8)}
        </span>
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
      <span className="rounded-ds bg-secondary px-2 py-1 text-sm">{row.status === "Active" ? "فعال" : row.status}</span>
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
    cell: (row) => (
      <span className="tabular-nums">
        {row.amount == null ? "—" : `${row.amount.toLocaleString("fa-IR")} ${row.currency}`}
      </span>
    ),
    width: 140,
    minWidth: 110,
    maxWidth: 180,
    sortable: true,
    filterKind: "number",
  },
  {
    id: "availableUnits",
    header: "موجود",
    accessor: (row) => row.availableUnits,
    width: 88,
    minWidth: 72,
    maxWidth: 120,
    align: "end",
    filterKind: "number",
    sortable: true,
  },
  {
    id: "open",
    header: "عملیات",
    accessor: (row) => row.offerId,
    cell: (row) => (
      <Link className="text-primary underline-offset-4 hover:underline" href={`/vendor-panel/products/${row.offerId}`}>
        ویرایش
      </Link>
    ),
    width: 88,
    minWidth: 80,
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
        <h1 className="text-[length:var(--type-title)] font-semibold tracking-tight">محصولات فروشنده</h1>
        <p className="mt-1 text-[length:var(--type-body)] text-muted">فهرست Offer؛ قیمت روی Product نیست</p>
      </div>
      <p className="mb-4 text-base text-muted" data-testid="seller-products-source">
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
