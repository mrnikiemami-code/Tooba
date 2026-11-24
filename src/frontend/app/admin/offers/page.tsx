"use client";

import Link from "next/link";
import { useMemo } from "react";
import { AppDataGrid, useLegacyAdminGridDirectProps } from "../../../design-system";
import type { GridColumnDef } from "../../../design-system/data-grid";
import { ADMIN_OFFER_GRID_VIEW_KEY, createHostSavedViewStore } from "../saved-view-store";
import { queryAdminOffersGrid, type AdminOfferRow } from "../admin-offer-api";

const statusFa: Record<string, string> = {
  Draft: "پیش‌نویس",
  Active: "فعال",
  Suspended: "معلق",
  Archived: "بایگانی‌شده",
};

type AdminOfferGridRow = AdminOfferRow & { id: string };

const columns: GridColumnDef<AdminOfferGridRow>[] = [
  {
    id: "productTitle",
    header: "محصول",
    accessor: (row) => row.productTitle,
    cell: (row) => (
      <Link className="font-semibold hover:underline" href={`/admin/offers/${row.offerId}`}>
        {row.productTitle}
      </Link>
    ),
    width: 240,
    minWidth: 180,
    maxWidth: 360,
    filterKind: "text",
    sortable: true,
    sticky: "start",
  },
  { id: "sellerSku", header: "کد فروشنده", accessor: (row) => row.sellerSku ?? "", cell: (row) => <span dir="ltr">{row.sellerSku ?? "—"}</span>, width: 140, minWidth: 100, maxWidth: 180, filterKind: "text", sortable: true },
  {
    id: "status",
    header: "وضعیت",
    accessor: (row) => row.status,
    cell: (row) => (
      <span
        className={
          row.status === "Active"
            ? "inline-flex rounded-full bg-[rgb(228_245_238)] px-2.5 py-1 text-xs font-medium text-[rgb(11_122_83)]"
            : row.status === "Suspended"
              ? "inline-flex rounded-full bg-[rgb(253_241_221)] px-2.5 py-1 text-xs font-medium text-[rgb(154_91_0)]"
              : row.status === "Archived"
                ? "inline-flex rounded-full bg-secondary px-2.5 py-1 text-xs font-medium"
                : "inline-flex rounded-full bg-secondary px-2.5 py-1 text-xs font-medium"
        }
      >
        {statusFa[row.status] ?? row.status}
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
      { value: "Archived", label: "بایگانی‌شده" },
    ],
  },
  { id: "amount", header: "قیمت / ارز", accessor: (row) => row.amount ?? 0, cell: (row) => <span className="tabular-nums">{row.amount == null ? "—" : `${row.amount.toLocaleString("fa-IR")} ${row.currency ?? ""}`.trim()}</span>, width: 150, minWidth: 120, maxWidth: 190, filterKind: "number", sortable: true },
  { id: "availableUnits", header: "موجودی قابل فروش", accessor: (row) => row.availableUnits, cell: (row) => <span className="tabular-nums">{row.availableUnits <= 0 ? "ناموجود" : row.availableUnits.toLocaleString("fa-IR")}</span>, width: 130, minWidth: 100, maxWidth: 160, align: "end", filterKind: "number", sortable: true },
  { id: "lastUpdatedAt", header: "آخرین تغییر", accessor: (row) => row.lastUpdatedAt ?? "", cell: (row) => <span className="tabular-nums text-sm" dir="ltr">{row.lastUpdatedAt?.slice(0, 10) ?? "—"}</span>, width: 120, minWidth: 100, maxWidth: 150, sortable: true },
  {
    id: "actions",
    header: "عملیات",
    accessor: (row) => row.offerId,
    cell: (row) => (
      <Link className="inline-flex min-h-9 items-center rounded-ds bg-primary px-3 text-sm text-primary-foreground" href={`/admin/offers/${row.offerId}`}>
        ویرایش
      </Link>
    ),
    width: 96,
    minWidth: 88,
    maxWidth: 120,
    sortable: false,
  },
];

export default function AdminOffersPage() {
  const adapter = useMemo(
    () => async (query: Parameters<typeof queryAdminOffersGrid>[0]) => {
      const result = await queryAdminOffersGrid(query);
      return { ...result.page, rows: result.page.rows.map((row) => ({ ...row, id: row.offerId })) };
    },
    [],
  );
  const savedViewStore = useMemo(() => createHostSavedViewStore(ADMIN_OFFER_GRID_VIEW_KEY), []);
  const gridProps = useLegacyAdminGridDirectProps<AdminOfferGridRow>({
    gridId: ADMIN_OFFER_GRID_VIEW_KEY,
    columns,
    queryAdapter: adapter,
    savedViewStore,
    exportFilenameBase: "admin-offers",
  });

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
        <AppDataGrid<AdminOfferGridRow> {...gridProps} rowCountNoun={{ fa: "آفر", en: "offers" }} />
      </section>
    </main>
  );
}



