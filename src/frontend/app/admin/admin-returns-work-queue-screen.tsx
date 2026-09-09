"use client";

import { useCallback, useMemo, useState, type ReactNode } from "react";
import { Eye } from "lucide-react";
import {
  AppDataGrid,
  ErrorState,
  adminGridQueryAdapter,
  faWorkspaceMessages,
  useLegacyAdminGridDirectProps,
} from "../../design-system";
import { AppGridRowActionsCell, type AppGridRowAction } from "../../design-system/app-data-grid/app-grid-row-actions";
import type { GridColumnDef, GridServerQuery } from "../../design-system/data-grid";
import { formatQuantityDisplay } from "../../lib/quantity-display";
import {
  RETURN_QUEUE_FILTERS,
  formatRefundLifecycleStatus,
  formatReturnDate,
  formatReturnStatus,
  queryAdminReturnsGrid,
  refundLifecycleBadgeClass,
  returnStatusBadgeClass,
  type ReturnQueueFilter,
  type ReturnWorkQueueRow,
} from "../returns/return-api";
import { mapAdminErrorMessage } from "./admin-error-map";
import { AdminOrderOperationsMenu } from "./admin-order-operations-menu";
import { ADMIN_RETURN_GRID_VIEW_KEY, createHostSavedViewStore } from "./saved-view-store";

function PageHeading({ title, description }: { title: string; description: string }) {
  return (
    <div className="mb-5">
      <p className="text-sm text-muted">خانه / {title}</p>
      <h1 className="mt-1 text-2xl font-semibold tracking-tight">{title}</h1>
      <p className="mt-1 text-base text-muted">{description}</p>
    </div>
  );
}

function truncatedCell(content: ReactNode, title?: string) {
  return (
    <div className="app-grid-cell-content min-w-0">
      <span className="block truncate" title={title}>{content}</span>
    </div>
  );
}

const returnStatusEnumOptions = [
  { value: "Requested", label: formatReturnStatus("Requested") },
  { value: "Approved", label: formatReturnStatus("Approved") },
  { value: "Rejected", label: formatReturnStatus("Rejected") },
  { value: "Completed", label: formatReturnStatus("Completed") },
  { value: "Cancelled", label: formatReturnStatus("Cancelled") },
];

const refundStatusEnumOptions = [
  { value: "none", label: formatRefundLifecycleStatus("none") },
  { value: "pending", label: formatRefundLifecycleStatus("pending") },
  { value: "failed", label: formatRefundLifecycleStatus("failed") },
  { value: "completed", label: formatRefundLifecycleStatus("completed") },
];

const viewActions: AppGridRowAction<ReturnWorkQueueRow>[] = [
  {
    id: "view-order",
    label: "مشاهده سفارش",
    icon: Eye,
    href: (row) => `/admin/orders/${row.checkoutId}`,
    testId: (row) => `admin-return-view-order-${row.returnRequestId}`,
  },
  {
    id: "view-return",
    label: "جزئیات مرجوعی",
    icon: Eye,
    href: (row) => `/admin/returns/${row.returnRequestId}`,
    testId: (row) => `admin-return-view-detail-${row.returnRequestId}`,
  },
];

function formatQueueQuantity(row: ReturnWorkQueueRow): string {
  return `${formatQuantityDisplay(row.quantityRequested)} ${row.unitLabel}`.trim();
}

/**
 * صف کار عملیاتی مرجوعی‌ها و بازگشت وجه — AppDataGrid، بدون GUID، lifecycle جدا.
 */
export function AdminReturnsWorkQueueScreen() {
  const savedViewStore = useMemo(() => createHostSavedViewStore(ADMIN_RETURN_GRID_VIEW_KEY), []);
  const [queueFilter, setQueueFilter] = useState<ReturnQueueFilter>("all");
  const [denied, setDenied] = useState(false);
  const [gridError, setGridError] = useState<string>();
  const [reloadToken, setReloadToken] = useState(0);
  const refresh = useCallback(() => setReloadToken((value) => value + 1), []);

  const columns = useMemo((): GridColumnDef<ReturnWorkQueueRow>[] => [
    {
      id: "returnReference",
      header: "مرجوعی",
      accessor: (row) => row.returnReference,
      cell: (row) => truncatedCell(row.returnReference || "—", row.returnReference || undefined),
      width: 140,
      minWidth: 110,
      filterKind: "text",
      sortable: true,
    },
    {
      id: "orderReference",
      header: "سفارش",
      accessor: (row) => row.orderReference,
      cell: (row) => truncatedCell(row.orderReference || "—", row.orderReference || undefined),
      width: 150,
      minWidth: 120,
      filterKind: "text",
      sortable: true,
    },
    {
      id: "customerDisplayName",
      header: "مشتری",
      accessor: (row) => row.customerDisplayName,
      cell: (row) => truncatedCell(row.customerDisplayName || "—", row.customerDisplayName),
      width: 140,
      minWidth: 110,
      filterKind: "text",
      sortable: true,
    },
    {
      id: "sellerDisplayName",
      header: "فروشنده",
      accessor: (row) => row.sellerDisplayName,
      cell: (row) => truncatedCell(row.sellerDisplayName || "—", row.sellerDisplayName),
      width: 150,
      minWidth: 120,
      filterKind: "text",
      sortable: true,
    },
    {
      id: "productLabel",
      header: "کالا",
      accessor: (row) => row.productLabel,
      cell: (row) => truncatedCell(row.productLabel || "—", row.productLabel),
      width: 180,
      minWidth: 140,
    },
    {
      id: "quantityRequested",
      header: "مقدار",
      accessor: (row) => row.quantityRequested,
      cell: (row) => truncatedCell(formatQueueQuantity(row)),
      width: 120,
      minWidth: 96,
      sortable: true,
    },
    {
      id: "returnStatus",
      header: "وضعیت مرجوعی",
      accessor: (row) => row.returnStatus,
      cell: (row) => (
        <span className={returnStatusBadgeClass(row.returnStatus)}>{formatReturnStatus(row.returnStatus)}</span>
      ),
      width: 150,
      minWidth: 120,
      filterKind: "status",
      enumOptions: returnStatusEnumOptions,
      sortable: true,
    },
    {
      id: "refundStatus",
      header: "وضعیت بازگشت وجه",
      accessor: (row) => row.refundStatus,
      cell: (row) => (
        <span className={refundLifecycleBadgeClass(row.refundStatus)}>
          {formatRefundLifecycleStatus(row.refundStatus)}
        </span>
      ),
      width: 170,
      minWidth: 140,
      filterKind: "status",
      enumOptions: refundStatusEnumOptions,
      sortable: true,
    },
    {
      id: "eligibilitySummary",
      header: "مهلت / شرایط",
      accessor: (row) => row.eligibilitySummary,
      cell: (row) => truncatedCell(row.eligibilitySummary || "—", row.eligibilitySummary),
      width: 180,
      minWidth: 140,
    },
    {
      id: "createdAt",
      header: "زمان درخواست",
      accessor: (row) => row.createdAt,
      cell: (row) => truncatedCell(formatReturnDate(row.createdAt)),
      width: 140,
      minWidth: 120,
      filterKind: "date",
      sortable: true,
    },
    {
      id: "updatedAt",
      header: "آخرین تغییر",
      accessor: (row) => row.updatedAt,
      cell: (row) => truncatedCell(formatReturnDate(row.updatedAt)),
      width: 140,
      minWidth: 120,
      filterKind: "date",
      sortable: true,
    },
    {
      id: "actions",
      header: "عملیات",
      accessor: () => "",
      cell: (row) => (
        <span className="inline-flex items-center gap-1" data-testid={`admin-return-ops-${row.returnRequestId}`}>
          <AppGridRowActionsCell row={row} actions={viewActions} compact />
          {row.availableActionCodes.length > 0 ? (
            <AdminOrderOperationsMenu
              checkoutId={row.checkoutId}
              label="عملیات"
              compact
              iconOnly
              scope="returns-queue"
              returnRequestId={row.returnRequestId}
              onCompleted={refresh}
              testId={`admin-return-kebab-${row.returnRequestId}`}
            />
          ) : null}
        </span>
      ),
      width: 120,
      minWidth: 100,
      exportable: false,
      sortable: false,
    },
  ], [refresh]);

  const queryFn = useCallback(
    async (query: GridServerQuery) => {
      const filters = { ...query.filters };
      if (queueFilter !== "all") {
        filters.queueFilter = { kind: "status", values: [queueFilter] };
      } else {
        delete filters.queueFilter;
      }
      return queryAdminReturnsGrid({ ...query, filters });
    },
    [queueFilter],
  );

  const queryAdapter = useCallback(
    async (query: GridServerQuery) => {
      void reloadToken;
      return adminGridQueryAdapter(queryFn, () => setDenied(true), (message) => setGridError(message))(query);
    },
    [queryFn, reloadToken],
  );

  const gridProps = useLegacyAdminGridDirectProps({
    gridId: ADMIN_RETURN_GRID_VIEW_KEY,
    columns,
    queryAdapter,
    savedViewStore,
  });

  if (denied) {
    return (
      <main data-testid="admin-returns-work-queue">
        <PageHeading title="مرجوعی‌ها و بازگشت وجه" description="صف کار عملیاتی مرجوعی و بازگشت وجه" />
        <ErrorState
          title="دسترسی ندارید"
          detail={mapAdminErrorMessage("admin.authorization.denied", "fa")}
          onRetry={refresh}
          retryLabel={faWorkspaceMessages.retry}
        />
      </main>
    );
  }

  return (
    <main data-testid="admin-returns-work-queue" dir="rtl">
      <PageHeading
        title="مرجوعی‌ها و بازگشت وجه"
        description="صف کار عملیاتی روی همان دامنه مرجوعی و بازگشت وجه"
      />
      <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
        <div className="flex flex-col gap-3 border-b border-border px-4 py-3 md:px-5">
          <span className="text-sm text-muted">دادهٔ زندهٔ فروشگاه — server GridQuery</span>
          <div className="flex flex-wrap gap-2" data-testid="admin-returns-queue-filters" role="tablist">
            {RETURN_QUEUE_FILTERS.map((tab) => {
              const active = queueFilter === tab.id;
              return (
                <button
                  key={tab.id}
                  type="button"
                  role="tab"
                  aria-selected={active}
                  data-testid={`admin-returns-queue-filter-${tab.id}`}
                  className={
                    active
                      ? "rounded-full bg-primary px-3 py-1.5 text-xs font-bold text-primary-foreground"
                      : "rounded-full border border-border bg-surface px-3 py-1.5 text-xs font-semibold text-muted hover:bg-secondary"
                  }
                  onClick={() => {
                    setQueueFilter(tab.id);
                    refresh();
                  }}
                >
                  {tab.labelFa}
                </button>
              );
            })}
          </div>
        </div>
        <div className="overflow-x-auto p-2 md:p-4">
          {gridError ? (
            <ErrorState title="فروشگاه در دسترس نیست" detail={gridError} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
          ) : (
            <AppDataGrid<ReturnWorkQueueRow> {...gridProps} />
          )}
        </div>
      </section>
    </main>
  );
}
