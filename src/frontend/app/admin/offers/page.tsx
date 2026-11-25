"use client";

import Link from "next/link";
import { useMemo } from "react";
import { Eye, Plus } from "lucide-react";
import { AppDataGrid, useLegacyAdminGridDirectProps } from "../../../design-system";
import { AppGridRowActionsCell, type AppGridRowAction } from "../../../design-system/app-data-grid/app-grid-row-actions";
import type { GridColumnDef } from "../../../design-system/data-grid";
import { ADMIN_OFFER_GRID_VIEW_KEY, createHostSavedViewStore } from "../saved-view-store";
import { queryAdminOffersGrid, type AdminOfferRow } from "../admin-offer-api";
import { useLocale } from "../../../lib/i18n/locale-context";
import {
  channelLabel,
  createOfferFormatters,
  offerListMessagesFor,
  offerLocaleFromAppLocale,
  offerStatusLabel,
  type OfferListMessages,
} from "../../../lib/offer-workspace/i18n";

type AdminOfferGridRow = AdminOfferRow & { id: string };

function statusClass(status: string): string {
  if (status === "Active") return "bg-success/15 text-success";
  if (status === "Suspended") return "bg-warning/15 text-warning";
  return "bg-secondary text-secondary-foreground";
}

function buildColumns(t: OfferListMessages, format: ReturnType<typeof createOfferFormatters>): GridColumnDef<AdminOfferGridRow>[] {
  const actions: AppGridRowAction<AdminOfferGridRow>[] = [
    {
      id: "edit",
      label: t.view,
      icon: Eye,
      href: (row) => `/admin/offers/${row.offerId}`,
      testId: (row) => `admin-offer-edit-${row.offerId}`,
    },
  ];
  return [
    {
      id: "productTitle",
      header: t.product,
      accessor: (row) => row.productTitle,
      cell: (row) => {
        const variantMeta = row.variantLabel
          ? `${row.brandName ? `${row.brandName} · ` : ""}${row.variantLabel}`
          : row.brandName;
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
              <span className="block truncate text-xs text-muted md:hidden" title={row.sellerDisplayName ?? ""}>{row.sellerDisplayName || "—"}</span>
              <span className="hidden truncate text-xs text-muted md:block" title={variantMeta ?? ""}>{variantMeta || "—"}</span>
            </span>
          </div>
        );
      },
      width: 190,
      minWidth: 170,
      maxWidth: 300,
      sortable: false,
    },
    {
      id: "seller",
      header: t.seller,
      accessor: (row) => row.sellerDisplayName ?? "",
      cell: (row) => <span className="block truncate text-sm" title={row.sellerDisplayName ?? ""}>{row.sellerDisplayName || "—"}</span>,
      width: 150,
      minWidth: 110,
      maxWidth: 200,
      sortable: false,
    },
    {
      id: "sellerSku",
      header: t.sellerSku,
      accessor: (row) => row.sellerSku ?? "",
      cell: (row) => <span dir="ltr" className="block truncate text-sm" title={row.sellerSku || undefined}>{row.sellerSku || "—"}</span>,
      width: 130,
      minWidth: 100,
      maxWidth: 180,
      sortable: true,
    },
    {
      id: "channel",
      header: t.channel,
      accessor: (row) => row.channel,
      cell: (row) => <span className="text-sm">{channelLabel(t, row.channel)}</span>,
      width: 96,
      minWidth: 82,
      maxWidth: 130,
      sortable: false,
    },
    {
      id: "amount",
      header: t.price,
      accessor: (row) => row.amount ?? 0,
      cell: (row) => <span className="tabular-nums text-sm">{format.money(row.amount, row.currency)}</span>,
      width: 150,
      minWidth: 120,
      maxWidth: 200,
      sortable: false,
    },
    {
      id: "availableUnits",
      header: t.available,
      accessor: (row) => row.availableUnits,
      cell: (row) => <span className="tabular-nums text-sm">{row.availableUnits <= 0 ? t.unavailable : format.integer(row.availableUnits)}</span>,
      width: 110,
      minWidth: 96,
      maxWidth: 140,
      align: "end",
      sortable: false,
    },
    {
      id: "status",
      header: t.status,
      accessor: (row) => row.status,
      cell: (row) => <span className={`inline-flex rounded-full px-2.5 py-1 text-xs font-medium ${statusClass(row.status)}`}>{offerStatusLabel(t, row.status)}</span>,
      width: 110,
      minWidth: 96,
      maxWidth: 140,
      filterKind: "status",
      enumOptions: [
        { value: "Active", label: t.statusActive },
        { value: "Suspended", label: t.statusSuspended },
        { value: "Draft", label: t.statusDraft },
        { value: "Archived", label: t.statusArchived },
      ],
      sortable: true,
    },
    {
      id: "lastUpdatedAt",
      header: t.lastUpdated,
      accessor: (row) => row.lastUpdatedAt ?? "",
      cell: (row) => <span className="whitespace-nowrap tabular-nums text-sm">{format.date(row.lastUpdatedAt)}</span>,
      width: 120,
      minWidth: 105,
      maxWidth: 160,
      sortable: true,
    },
    {
      id: "actions",
      header: t.actions,
      accessor: () => "",
      cell: (row) => <AppGridRowActionsCell row={row} actions={actions} compact />,
      width: 80,
      minWidth: 72,
      maxWidth: 100,
      exportable: false,
      sortable: false,
    },
  ];
}

export default function AdminOffersPage() {
  const appLocale = useLocale();
  const offerLocale = offerLocaleFromAppLocale(appLocale);
  const format = useMemo(() => createOfferFormatters(offerLocale), [offerLocale]);
  const t = useMemo(() => offerListMessagesFor(offerLocale.language), [offerLocale.language]);
  const columns = useMemo(() => buildColumns(t, format), [t, format]);
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
    locale: offerLocale.language === "en" ? "en" : "fa",
  });

  return (
    <main className="space-y-4" data-testid="admin-offers">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <p className="text-sm text-muted">{t.breadcrumb}</p>
          <h1 className="mt-1 text-2xl font-semibold tracking-tight">{t.title}</h1>
          <p className="mt-1 text-base text-muted">{t.subtitle}</p>
        </div>
        <Link
          className="inline-flex min-h-11 items-center gap-1 rounded-xl bg-primary px-4 text-sm font-bold text-primary-foreground shadow-sm"
          href="/admin/offers/new"
          data-testid="admin-offer-create-link"
        >
          <Plus className="h-4 w-4" aria-hidden />
          {t.create}
        </Link>
      </div>
      <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
        <div className="p-2 md:p-4">
          <AppDataGrid<AdminOfferGridRow> {...gridProps} rowHeight={64} rowCountNoun={{ fa: t.offersNoun, en: t.offersNoun }} />
        </div>
      </section>
    </main>
  );
}
