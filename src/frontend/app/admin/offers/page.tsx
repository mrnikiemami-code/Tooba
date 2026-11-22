"use client";

import Link from "next/link";
import { useMemo } from "react";
import { DataGrid } from "../../../design-system";
import type { GridColumnDef } from "../../../design-system/data-grid";
import { queryAdminOffersGrid, type AdminOfferRow } from "../admin-offer-api";

const columns: GridColumnDef<AdminOfferRow>[] = [
  {
    id: "productTitle",
    header: "محصول",
    accessor: (row) => row.productTitle,
    cell: (row) => <Link className="font-semibold hover:underline" href={`/admin/offers/${row.offerId}`}>{row.productTitle}</Link>,
    width: 240,
    minWidth: 180,
    maxWidth: 360,
    filterKind: "text",
    sortable: true,
    sticky: "start",
  },
  { id: "sellerSku", header: "SKU فروشنده", accessor: (row) => row.sellerSku ?? "", cell: (row) => <span dir="ltr">{row.sellerSku ?? "—"}</span>, width: 140, minWidth: 100, maxWidth: 180, filterKind: "text", sortable: true },
  { id: "status", header: "وضعیت", accessor: (row) => row.status, cell: (row) => <span>{row.status === "Draft" ? "پیش‌نویس" : row.status}</span>, width: 100, minWidth: 90, maxWidth: 130 },
  { id: "amount", header: "قیمت", accessor: (row) => row.amount ?? 0, cell: (row) => <span className="tabular-nums">{row.amount == null ? "—" : row.amount.toLocaleString("fa-IR")}</span>, width: 140, minWidth: 110, maxWidth: 180, filterKind: "number", sortable: true },
  { id: "availableUnits", header: "موجودی", accessor: (row) => row.availableUnits, cell: (row) => <span className="tabular-nums">{row.availableUnits.toLocaleString("fa-IR")}</span>, width: 100, minWidth: 90, maxWidth: 130, filterKind: "number", sortable: true },
  { id: "lastUpdatedAt", header: "به‌روزرسانی", accessor: (row) => row.lastUpdatedAt ?? "", cell: (row) => <span className="tabular-nums text-sm">{row.lastUpdatedAt?.slice(0, 10) ?? "—"}</span>, width: 120, minWidth: 100, maxWidth: 150, sortable: true },
];

export default function AdminOffersPage() {
  const adapter = useMemo(
    () => async (query: Parameters<typeof queryAdminOffersGrid>[0]) => {
      const result = await queryAdminOffersGrid(query);
      return result.page;
    },
    [],
  );
  return (
    <main className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-bold text-gray-900">پیشنهادهای فروشندگان</h1>
          <p className="text-sm text-gray-500">ایجاد و مدیریت پیش‌نویس آفر برای فروشندهٔ انتخاب‌شده</p>
        </div>
        <Link className="min-h-11 rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white" href="/admin/offers/new" data-testid="admin-offer-create-link">
          ایجاد پیش‌نویس
        </Link>
      </div>
      <section className="rounded-2xl border border-gray-200 bg-white p-3 shadow-sm">
        <DataGrid columns={columns} queryAdapter={adapter} />
      </section>
    </main>
  );
}
