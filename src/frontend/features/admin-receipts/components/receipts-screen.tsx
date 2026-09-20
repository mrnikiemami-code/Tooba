"use client";

import Link from "next/link";
import { useCallback, useMemo, useState } from "react";
import { Eye } from "lucide-react";
import {
  AppDataGrid,
  ErrorState,
  faWorkspaceMessages,
  adminGridQueryAdapter,
  useLegacyAdminGridDirectProps,
} from "../../../design-system";
import { AppGridRowActionsCell, type AppGridRowAction } from "../../../design-system/app-data-grid/app-grid-row-actions";
import type { GridColumnDef, GridServerQuery, SavedViewStore } from "../../../design-system/data-grid";
import type { AdminGridQueryResult } from "../../../design-system/app-data-grid/admin-grid-query-client.ts";
import { formatAdminDate, formatAdminMoney, formatAdminStatus } from "../../../app/admin/admin-api.ts";
import { adminSupplyBadgeClass, formatAdminSupplyStatus, supplyStatusEnumOptions } from "../../../app/admin/admin-order-supply.ts";
import { reservationBadgeClass, reservationStateEnumOptions } from "../../../app/admin/admin-reservation-cycle.ts";
import { createHostSavedViewStore, ADMIN_RECEIPT_GRID_VIEW_KEY } from "../../../app/admin/saved-view-store.ts";
import { queryAdminReceiptsGrid, type AdminReceiptRow } from "../api/receipts-api.ts";

function Denied({ retry }: { retry: () => void }) {
  return (
    <div data-testid="admin-auth-denied">
      <ErrorState
        title="دسترسی مجاز نیست"
        detail="سامانه هویت فعلی را مدیر تشخیص نداد. تغییر مسیر یا هدر مرورگر مجوز ایجاد نمی‌کند."
        onRetry={retry}
        retryLabel={faWorkspaceMessages.retry}
      />
    </div>
  );
}

function PageHeading({ title, description }: { title: string; description: string }) {
  return (
    <div className="mb-5">
      <p className="text-sm text-muted">خانه / {title}</p>
      <h1 className="mt-1 text-2xl font-semibold tracking-tight">{title}</h1>
      <p className="mt-1 text-base text-muted">{description}</p>
    </div>
  );
}

function Status({ value }: { value: string }) {
  return <span className="inline-flex rounded-full bg-secondary px-2.5 py-1 text-xs font-medium">{formatAdminStatus(value)}</span>;
}

function ServerGridPage<T extends { id: string }>({
  title,
  description,
  gridId,
  columns,
  queryFn,
  savedViewStore: savedViewStoreInput,
  testId,
}: {
  title: string;
  description: string;
  gridId: string;
  columns: GridColumnDef<T>[];
  queryFn: (query: GridServerQuery) => Promise<AdminGridQueryResult<T>>;
  savedViewStore?: SavedViewStore;
  testId?: string;
}) {
  const savedViewStore = useMemo(
    () => savedViewStoreInput ?? createHostSavedViewStore(gridId),
    [gridId, savedViewStoreInput],
  );
  const [denied, setDenied] = useState(false);
  const [gridError, setGridError] = useState<string>();
  const [localReloadToken, setLocalReloadToken] = useState(0);
  const refresh = () => setLocalReloadToken((value) => value + 1);
  const queryAdapter = useCallback(
    async (query: GridServerQuery) => {
      void localReloadToken;
      return adminGridQueryAdapter(queryFn, () => setDenied(true), (message) => setGridError(message))(query);
    },
    [queryFn, localReloadToken],
  );
  const gridProps = useLegacyAdminGridDirectProps({ gridId, columns, queryAdapter, savedViewStore });
  if (denied) return <Denied retry={refresh} />;
  return (
    <main data-testid={testId}>
      <PageHeading title={title} description={description} />
      <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
        <div className="flex items-center justify-between gap-3 border-b border-border px-4 py-3 md:px-5">
          <span className="text-sm text-muted">دادهٔ زندهٔ فروشگاه — server GridQuery</span>
        </div>
        <div className="p-2 md:p-4">
          {gridError ? (
            <ErrorState title="فروشگاه در دسترس نیست" detail={gridError} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
          ) : (
            <AppDataGrid<T> {...gridProps} />
          )}
        </div>
      </section>
    </main>
  );
}

const receiptStatusEnumOptions = [
  { value: "Pending", label: formatAdminStatus("Pending") },
  { value: "Succeeded", label: formatAdminStatus("Succeeded") },
  { value: "Failed", label: formatAdminStatus("Failed") },
  { value: "Cancelled", label: formatAdminStatus("Cancelled") },
  { value: "Expired", label: formatAdminStatus("Expired") },
];

const receiptRowActions: AppGridRowAction<AdminReceiptRow>[] = [
  {
    id: "view",
    label: "مشاهده",
    icon: Eye,
    href: (row) => `/admin/orders/${row.checkoutId}`,
    testId: (row) => `admin-receipt-view-${row.paymentId}`,
  },
];

const receiptColumns: GridColumnDef<AdminReceiptRow>[] = [
  {
    id: "reference",
    header: "سفارش",
    accessor: (row) => row.orderReference,
    cell: (row) => (
      <Link className="font-semibold text-primary hover:underline" href={`/admin/orders/${row.checkoutId}`}>
        {row.orderReference}
      </Link>
    ),
    width: 180,
    minWidth: 140,
    maxWidth: 240,
    sticky: "start",
    filterKind: "text",
    sortable: true,
  },
  {
    id: "customer",
    header: "مشتری",
    accessor: (row) => row.customerDisplayName,
    width: 150,
    minWidth: 110,
    filterKind: "text",
    sortable: true,
  },
  {
    id: "amount",
    header: "مبلغ",
    accessor: (row) => row.amount,
    cell: (row) => formatAdminMoney(row.amount, row.currency),
    width: 140,
    minWidth: 110,
    sortable: true,
  },
  {
    id: "status",
    header: "وضعیت",
    accessor: (row) => row.status,
    cell: (row) => <Status value={row.status} />,
    width: 120,
    minWidth: 100,
    filterKind: "status",
    enumOptions: receiptStatusEnumOptions,
  },
  {
    id: "supply",
    header: "وضعیت تأمین",
    accessor: (row) => row.supplyStatus,
    cell: (row) => (
      <span className={`inline-flex rounded-full px-2.5 py-1 text-xs font-medium ${adminSupplyBadgeClass(row.supplyStatus)}`}>
        {formatAdminSupplyStatus(row.supplyStatus)}
      </span>
    ),
    width: 140,
    minWidth: 120,
    filterKind: "status",
    enumOptions: supplyStatusEnumOptions,
    sortable: true,
  },
  {
    id: "reservation",
    header: "رزرو موجودی",
    accessor: (row) => row.reservationState,
    cell: (row) => (
      <span className="inline-flex max-w-full flex-col gap-0.5">
        <span className={`inline-flex rounded-full px-2.5 py-1 text-xs font-medium ${reservationBadgeClass(row.reservationState)}`}>
          {row.reservationLabel || "—"}
        </span>
        {row.reservationRetryLimitReached ? (
          <span className="text-[10px] text-rose-700">سقف رزرو</span>
        ) : row.reservationNeedsReacquire ? (
          <span className="text-[10px] text-amber-800">نیاز به رزرو مجدد</span>
        ) : row.reservationRetryPossible ? (
          <span className="text-[10px] text-gray-500">امکان تلاش مجدد</span>
        ) : null}
      </span>
    ),
    width: 150,
    minWidth: 124,
    filterKind: "status",
    enumOptions: reservationStateEnumOptions,
    sortable: true,
  },
  {
    id: "provider",
    header: "درگاه",
    accessor: (row) => row.providerCode,
    cell: (row) => <span dir="ltr">{row.providerCode || "—"}</span>,
    width: 120,
    minWidth: 90,
    filterKind: "text",
  },
  {
    id: "created",
    header: "تاریخ",
    accessor: (row) => row.createdAt,
    cell: (row) => formatAdminDate(row.createdAt),
    width: 120,
    minWidth: 100,
    sortable: true,
  },
  {
    id: "actions",
    header: "عملیات",
    accessor: () => "",
    cell: (row) => <AppGridRowActionsCell row={row} actions={receiptRowActions} compact />,
    exportable: false,
    sortable: false,
  },
];

/** فهرست دریافت‌های مشتری (پرداخت) برای Admin. */
export function AdminReceiptsScreen() {
  return (
    <ServerGridPage
      title="دریافت‌ها"
      description="پرداخت‌های واقعی مشتریان با enrich سفارش از Host"
      queryFn={queryAdminReceiptsGrid}
      columns={receiptColumns}
      gridId={ADMIN_RECEIPT_GRID_VIEW_KEY}
      testId="admin-receipts"
    />
  );
}
