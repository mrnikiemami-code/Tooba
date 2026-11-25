"use client";

import Link from "next/link";
import { useMemo } from "react";
import { Eye, Plus } from "lucide-react";
import { AppDataGrid, formatJalaliDate, useLegacyAdminGridDirectProps } from "../../../design-system";
import { AppGridRowActionsCell, type AppGridRowAction } from "../../../design-system/app-data-grid/app-grid-row-actions";
import type { GridColumnDef } from "../../../design-system/data-grid";
import { ADMIN_OFFER_GRID_VIEW_KEY, createHostSavedViewStore } from "../saved-view-store";
import { queryAdminOffersGrid, type AdminOfferRow } from "../admin-offer-api";
import { digitsFa, faMoney } from "../../../lib/offer-workspace/model";

type AdminOfferGridRow = AdminOfferRow & { id: string };

const statusFa: Record<string, string> = {
  Draft: "پیش‌نویس",
  Active: "فعال",
  Suspended: "معلق",
  Archived: "بایگانی‌شده",
};

function statusClass(status: string): string {
  if (status === "Active") return "bg-success/15 text-success";
  if (status === "Suspended") return "bg-warning/15 text-warning";
  return "bg-secondary text-secondary-foreground";
}

const rowActions: AppGridRowAction<AdminOfferGridRow>[] = [
  {
    id: "edit",
    label: "مشاهده",
    icon: Eye,
    href: (row) => `/admin/offers/${row.offerId}`,
    testId: (row) => `admin-offer-edit-${row.offerId}`,
  },
];

function productCell(row: AdminOfferGridRow) {
  return (
    <div className="app-grid-cell-content flex min-w-0 items-center gap-2">
      <span className="flex h-9 w-9 shrink-0 items-center justify-center overflow-hidden rounded-lg border border-border bg-secondary">
        {row.primaryMediaAssetId ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img src={`/v1/media/${row.primaryMediaAssetId}`} alt="" className="h-full w-full object-cover" />
        ) : (
          <span className="text-[10px] text-muted">—</span>
        )}
      </span>
      <span className="min-w-0">
        <span className="block truncate font-semibold" title={row.productTitle}>{row.productTitle}</span>
        <span className="block truncate text-xs text-muted md:hidden">{row.sellerDisplayName || "—"}</span>
        <span className="hidden truncate text-xs text-muted md:block">{row.brandName || "—"}</span>
      </span>
    </div>
  );
}

const columns: GridColumnDef<AdminOfferGridRow>[] = [
  {
    id: "productTitle",
    header: "محصول",
    accessor: (row) => row.productTitle,
    cell: productCell,
    width: 140,
    minWidth: 160,
    maxWidth: 300,
    filterKind: "text",
    sortable: true,
  },
  {
    id: "seller",
    header: "فروشنده",
    accessor: (row) => row.sellerDisplayName ?? "",
    cell: (row) => (
      <span className="block truncate text-sm" title={row.sellerDisplayName ?? ""}>
        {row.sellerDisplayName || "—"}
      </span>
    ),
    width: 160,
    minWidth: 80,
    maxWidth: 170,
    filterKind: "text",
    sortable: true,
  },
  {
    id: "sellerSku",
    header: "کد کالا",
    accessor: (row) => row.sellerSku ?? "",
    cell: (row) => <span dir="ltr" className="text-sm" title={row.sellerSku || undefined}>{row.sellerSku || "—"}</span>,
    width: 130,
    minWidth: 80,
    maxWidth: 170,
    filterKind: "text",
    sortable: true,
  },
  {
    id: "channel",
    header: "کانال",
    accessor: () => "Marketplace",
    cell: () => <span className="text-sm">بازارگاه</span>,
    width: 80,
    minWidth: 80,
    maxWidth: 120,
    sortable: false,
  },
  {
    id: "amount",
    header: "قیمت",
    accessor: (row) => row.amount ?? 0,
    cell: (row) => (
      <span className="tabular-nums text-sm">
        {row.amount == null ? "—" : faMoney(row.amount, row.currency, "fa")}
      </span>
    ),
    width: 110,
    minWidth: 110,
    maxWidth: 180,
    filterKind: "number",
    sortable: true,
  },
  {
    id: "availableUnits",
    header: "موجودی",
    accessor: (row) => row.availableUnits,
    cell: (row) => (
      <span className="tabular-nums text-sm">{row.availableUnits <= 0 ? "ناموجود" : row.availableUnits.toLocaleString("fa-IR")}</span>
    ),
    width: 130,
    minWidth: 88,
    maxWidth: 130,
    align: "end",
    filterKind: "number",
    sortable: true,
  },
  {
    id: "status",
    header: "وضعیت",
    accessor: (row) => row.status,
    cell: (row) => (
      <span className={`inline-flex rounded-full px-2.5 py-1 text-xs font-medium ${statusClass(row.status)}`}>
        {statusFa[row.status] ?? row.status}
      </span>
    ),
    width: 110,
    minWidth: 80,
    maxWidth: 120,
    filterKind: "status",
    enumOptions: [
      { value: "Active", label: "فعال" },
      { value: "Suspended", label: "معلق" },
      { value: "Draft", label: "پیش‌نویس" },
      { value: "Archived", label: "بایگانی‌شده" },
    ],
  },
  {
    id: "lastUpdatedAt",
    header: "آخرین تغییر",
    accessor: (row) => row.lastUpdatedAt ?? "",
    cell: (row) => <span className="tabular-nums text-sm">{digitsFa(formatJalaliDate(row.lastUpdatedAt, "fa"))}</span>,
    width: 130,
    minWidth: 90,
    maxWidth: 140,
    sortable: true,
  },
  {
    id: "actions",
    header: "عملیات",
    accessor: () => "",
    cell: (row) => <AppGridRowActionsCell row={row} actions={rowActions} compact />,
    width: 76,
    minWidth: 68,
    maxWidth: 96,
    exportable: false,
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
    <main className="space-y-4" data-testid="admin-offers">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <p className="text-sm text-muted">خانه / پیشنهادهای فروشندگان</p>
          <h1 className="mt-1 text-2xl font-semibold tracking-tight">پیشنهادهای فروشندگان</h1>
          <p className="mt-1 text-base text-muted">ایجاد و مدیریت آفر فروشندگان</p>
        </div>
        <Link
          className="inline-flex min-h-11 items-center gap-1 rounded-xl bg-primary px-4 text-sm font-bold text-primary-foreground shadow-sm"
          href="/admin/offers/new"
          data-testid="admin-offer-create-link"
        >
          <Plus className="h-4 w-4" aria-hidden />
          ایجاد آفر
        </Link>
      </div>
      <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
        <div className="p-2 md:p-4">
          <AppDataGrid<AdminOfferGridRow> {...gridProps} rowHeight={64} rowCountNoun={{ fa: "آفر", en: "offers" }} />
        </div>
      </section>
    </main>
  );
}
