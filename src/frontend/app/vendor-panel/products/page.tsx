"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Eye, Plus } from "lucide-react";
import { AppDataGrid, ErrorState, faWorkspaceMessages, formatJalaliDate, useLegacyAdminGridDirectProps } from "../../../design-system";
import { AppGridRowActionsCell, type AppGridRowAction } from "../../../design-system/app-data-grid/app-grid-row-actions";
import { SELLER_OFFER_GRID_VIEW_KEY, createHostSavedViewStore } from "../../admin/saved-view-store";
import type { GridColumnDef, GridServerPage, GridServerQuery } from "../../../design-system/data-grid";
import {
  formatOfferStatus,
  loadSellerOffersPage,
  readSellerPartyId,
  type HostReadSource,
  type SellerOfferListRow,
} from "../seller-api";
import { digitsFa, faMoney } from "../../../lib/offer-workspace/model";

type SellerOfferGridRow = SellerOfferListRow & { id: string };

function statusClass(status: string): string {
  if (status === "Active") return "bg-success/15 text-success";
  if (status === "Suspended") return "bg-warning/15 text-warning";
  return "bg-secondary text-secondary-foreground";
}

const rowActions: AppGridRowAction<SellerOfferGridRow>[] = [
  {
    id: "edit",
    label: "مشاهده",
    icon: Eye,
    href: (row) => `/vendor-panel/products/${row.offerId}`,
    testId: (row) => `seller-offer-edit-${row.offerId}`,
  },
];

const columns: GridColumnDef<SellerOfferGridRow>[] = [
  {
    id: "productTitle",
    header: "محصول",
    accessor: (row) => row.productTitle,
    cell: (row) => (
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
          <span className="block truncate text-xs text-muted">{row.brandName || "—"}</span>
        </span>
      </div>
    ),
    width: 140,
    minWidth: 160,
    maxWidth: 300,
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
    sortable: true,
    filterKind: "number",
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
    maxWidth: 120,
    align: "end",
    filterKind: "number",
    sortable: true,
  },
  {
    id: "status",
    header: "وضعیت آفر",
    accessor: (row) => row.status,
    cell: (row) => (
      <span className={`inline-flex rounded-full px-2.5 py-1 text-xs font-medium ${statusClass(row.status)}`}>
        {formatOfferStatus(row.status)}
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

export default function VendorProductsPage() {
  const [source, setSource] = useState<HostReadSource | "loading">("loading");
  const [total, setTotal] = useState(0);
  const [message, setMessage] = useState<string | undefined>(undefined);
  const [denied, setDenied] = useState(false);
  const [sellerPartyId, setSellerPartyId] = useState("");
  const savedViewStore = useMemo(() => createHostSavedViewStore(SELLER_OFFER_GRID_VIEW_KEY), []);
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    setSellerPartyId(readSellerPartyId(window.location.search) ?? "");
  }, []);

  const queryAdapter = useCallback(async (query: GridServerQuery): Promise<GridServerPage<SellerOfferGridRow>> => {
    void reloadKey;
    if (!sellerPartyId) return { rows: [], total: 0 };
    const result = await loadSellerOffersPage(sellerPartyId, query);
    setSource(result.source);
    setTotal(result.page.total);
    setMessage(result.message);
    setDenied(Boolean(result.denied));
    return { ...result.page, rows: result.page.rows.map((row) => ({ ...row, id: row.offerId })) };
  }, [sellerPartyId, reloadKey]);

  const gridProps = useLegacyAdminGridDirectProps<SellerOfferGridRow>({
    gridId: SELLER_OFFER_GRID_VIEW_KEY,
    columns,
    queryAdapter,
    savedViewStore,
    exportFilenameBase: "seller-offers",
  });

  function refresh() {
    setReloadKey((value) => value + 1);
  }

  if (denied) {
    return (
      <main data-testid="seller-auth-denied">
        <ErrorState
          title="دسترسی مجاز نیست"
          detail="این حساب اجازه مشاهده این فروشگاه را ندارد."
          onRetry={refresh}
          retryLabel={faWorkspaceMessages.retry}
        />
      </main>
    );
  }

  return (
    <main className="space-y-4" data-testid="seller-products">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <p className="text-sm text-muted">خانه / پیشنهادهای فروش</p>
          <h1 className="mt-1 text-2xl font-semibold tracking-tight">پیشنهادهای فروش</h1>
          <p className="mt-1 text-base text-muted">
            {total.toLocaleString("fa-IR")} آفر · جست‌وجو و صفحه‌بندی در سرور
          </p>
        </div>
        <Link
          href="/vendor-panel/products/new"
          className="inline-flex min-h-11 items-center gap-1 rounded-xl bg-primary px-4 text-sm font-bold text-primary-foreground shadow-sm"
          data-testid="seller-create-offer"
        >
          <Plus className="h-4 w-4" aria-hidden />
          ایجاد آفر
        </Link>
      </div>

      <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
        <div className="p-2 md:p-4">
          {source === "error" ? (
            <ErrorState title="فروشگاه در دسترس نیست" detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
          ) : (
            <AppDataGrid<SellerOfferGridRow> {...gridProps} rowHeight={64} rowCountNoun={{ fa: "آفر", en: "offers" }} />
          )}
        </div>
      </section>
    </main>
  );
}
