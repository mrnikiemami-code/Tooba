"use client";

import { useCallback, useMemo, useState, type ReactNode } from "react";
import { Eye } from "lucide-react";
import { toast } from "react-toastify";
import {
  AppDataGrid,
  ErrorState,
  adminGridQueryAdapter,
  faWorkspaceMessages,
  useLegacyAdminGridDirectProps,
} from "../../design-system";
import { AppGridRowActionsCell, type AppGridRowAction } from "../../design-system/app-data-grid/app-grid-row-actions";
import type { GridBulkAction, GridColumnDef, GridServerQuery } from "../../design-system/data-grid";
import {
  FULFILLMENT_BULK_ACTION_LABELS,
  FULFILLMENT_QUEUE_FILTERS,
  FULFILLMENT_SAFE_BULK_ACTION_CODES,
  areFulfillmentBulkCompatible,
  executeAdminFulfillmentWorkQueueBulk,
  formatFulfillmentDate,
  formatFulfillmentQueueQuantity,
  formatFulfillmentShipmentSummary,
  formatFulfillmentStatus,
  fulfillmentStatusBadgeClass,
  queryAdminFulfillmentsGrid,
  type FulfillmentListRow,
  type FulfillmentQueueFilter,
} from "../fulfillment/fulfillment-api";
import { mapAdminErrorMessage } from "./admin-error-map";
import { AdminOrderOperationsMenu } from "./admin-order-operations-menu";
import { ADMIN_FULFILLMENT_GRID_VIEW_KEY, createHostSavedViewStore } from "./saved-view-store";

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

const fulfillmentStatusEnumOptions = [
  { value: "ReadyToFulfill", label: formatFulfillmentStatus("ReadyToFulfill") },
  { value: "Processing", label: formatFulfillmentStatus("Processing") },
  { value: "Packed", label: formatFulfillmentStatus("Packed") },
  { value: "PartialDispatched", label: formatFulfillmentStatus("PartialDispatched") },
  { value: "Dispatched", label: formatFulfillmentStatus("Dispatched") },
  { value: "InTransit", label: formatFulfillmentStatus("InTransit") },
  { value: "Delivered", label: formatFulfillmentStatus("Delivered") },
  { value: "Failed", label: formatFulfillmentStatus("Failed") },
  { value: "Cancelled", label: formatFulfillmentStatus("Cancelled") },
];

const shippingMethodEnumOptions = [
  { value: "post", label: "پست" },
  { value: "tipax", label: "تیپاکس" },
  { value: "snapp_courier", label: "اسنپ / پیک آنلاین" },
  { value: "store_courier", label: "پیک فروشگاه" },
  { value: "in_person", label: "تحویل حضوری" },
];

const viewActions: AppGridRowAction<FulfillmentListRow>[] = [
  {
    id: "view-order",
    label: "مشاهده سفارش",
    icon: Eye,
    href: (row) => `/admin/orders/${row.checkoutId}`,
    testId: (row) => `admin-fulfillment-view-order-${row.fulfillmentId}`,
  },
];

/**
 * صف کار عملیاتی ارسال و تحویل — AppDataGrid + فیلتر سریع سروری + kebab + bulk سازگار.
 * Orders UI را دست نمی‌زند؛ فقط همین صفحه.
 */
export function AdminFulfillmentWorkQueueScreen() {
  const savedViewStore = useMemo(() => createHostSavedViewStore(ADMIN_FULFILLMENT_GRID_VIEW_KEY), []);
  const [queueFilter, setQueueFilter] = useState<FulfillmentQueueFilter>("all");
  const [denied, setDenied] = useState(false);
  const [gridError, setGridError] = useState<string>();
  const [reloadToken, setReloadToken] = useState(0);
  const refresh = useCallback(() => setReloadToken((value) => value + 1), []);

  const columns = useMemo((): GridColumnDef<FulfillmentListRow>[] => [
    {
      id: "orderReference",
      header: "سفارش",
      accessor: (row) => row.orderReference,
      cell: (row) => truncatedCell(row.orderReference || "—", row.orderReference || undefined),
      width: 160,
      minWidth: 120,
      filterKind: "text",
      sortable: true,
    },
    {
      id: "sellerDisplayName",
      header: "فروشنده",
      accessor: (row) => row.sellerDisplayName,
      cell: (row) => truncatedCell(row.sellerDisplayName || "—", row.sellerDisplayName),
      width: 160,
      minWidth: 120,
      filterKind: "text",
      sortable: true,
    },
    {
      id: "primaryShipmentId",
      header: "مرسوله",
      accessor: (row) => formatFulfillmentShipmentSummary(row),
      cell: (row) => truncatedCell(formatFulfillmentShipmentSummary(row)),
      width: 110,
      minWidth: 88,
      sortable: false,
    },
    {
      id: "quantityOrdered",
      header: "اقلام / مقدار",
      accessor: (row) => row.quantityOrdered,
      cell: (row) => truncatedCell(formatFulfillmentQueueQuantity(row)),
      width: 200,
      minWidth: 150,
      sortable: true,
    },
    {
      id: "shippingMethodLabel",
      header: "روش ارسال",
      accessor: (row) => row.shippingMethodLabel || row.shippingMethodCode,
      cell: (row) => truncatedCell(row.shippingMethodLabel || row.shippingMethodCode || "—"),
      width: 150,
      minWidth: 120,
      filterKind: "status",
      enumOptions: shippingMethodEnumOptions,
      sortable: true,
    },
    {
      id: "shippingMethodCode",
      header: "کد روش ارسال",
      accessor: (row) => row.shippingMethodCode,
      width: 120,
      minWidth: 96,
      filterKind: "status",
      enumOptions: shippingMethodEnumOptions,
      defaultVisible: false,
    },
    {
      id: "status",
      header: "وضعیت عملیاتی",
      accessor: (row) => row.status,
      cell: (row) => (
        <span className={fulfillmentStatusBadgeClass(row.status)}>{formatFulfillmentStatus(row.status)}</span>
      ),
      width: 150,
      minWidth: 120,
      filterKind: "status",
      enumOptions: fulfillmentStatusEnumOptions,
      sortable: true,
    },
    {
      id: "trackingSummary",
      header: "کد رهگیری",
      accessor: (row) => row.trackingSummary,
      cell: (row) => (
        <span className="block truncate font-mono text-xs dir-ltr text-left" title={row.trackingSummary || undefined}>
          {row.trackingSummary || "—"}
        </span>
      ),
      width: 150,
      minWidth: 110,
      filterKind: "text",
    },
    {
      id: "recipientName",
      header: "گیرنده",
      accessor: (row) => row.recipientName,
      cell: (row) => truncatedCell(row.recipientName || "بدون نام", row.recipientName),
      width: 140,
      minWidth: 110,
      filterKind: "text",
      sortable: true,
    },
    {
      id: "cityName",
      header: "مقصد / شهر",
      accessor: (row) => row.cityName,
      cell: (row) => truncatedCell(row.cityName || "—", row.cityName),
      width: 120,
      minWidth: 96,
      filterKind: "text",
      sortable: true,
    },
    {
      id: "createdAt",
      header: "زمان ایجاد",
      accessor: (row) => row.createdAt,
      cell: (row) => truncatedCell(formatFulfillmentDate(row.createdAt)),
      width: 140,
      minWidth: 120,
      filterKind: "date",
      sortable: true,
    },
    {
      id: "updatedAt",
      header: "آخرین تغییر",
      accessor: (row) => row.updatedAt,
      cell: (row) => truncatedCell(formatFulfillmentDate(row.updatedAt)),
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
        <span className="inline-flex items-center gap-1" data-testid={`admin-fulfillment-ops-${row.fulfillmentId}`}>
          <AppGridRowActionsCell row={row} actions={viewActions} compact />
          {row.availableActionCodes.length > 0 ? (
            <AdminOrderOperationsMenu
              checkoutId={row.checkoutId}
              label="عملیات"
              compact
              iconOnly
              scope="fulfillment-queue"
              fulfillmentId={row.fulfillmentId}
              onCompleted={refresh}
              testId={`admin-fulfillment-kebab-${row.fulfillmentId}`}
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
      // Map shipping method column filter field to code when enum selected via label column.
      if (filters.shippingMethodLabel && filters.shippingMethodLabel.kind === "status") {
        filters.shippingMethodCode = filters.shippingMethodLabel;
        delete filters.shippingMethodLabel;
      }
      return queryAdminFulfillmentsGrid({ ...query, filters });
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

  const bulkActions = useMemo((): GridBulkAction<FulfillmentListRow>[] => {
    return FULFILLMENT_SAFE_BULK_ACTION_CODES.map((code) => ({
      id: code,
      label: FULFILLMENT_BULK_ACTION_LABELS[code]?.fa ?? code,
      requiresConfirmation: true,
      isAvailable: (rows) => areFulfillmentBulkCompatible(rows, code),
      execute: async (rows) => {
        const result = await executeAdminFulfillmentWorkQueueBulk({
          actionCode: code,
          items: rows.map((row) => ({
            checkoutId: row.checkoutId,
            fulfillmentId: row.fulfillmentId,
            sellerOrderId: row.sellerOrderId,
            shipmentId: row.primaryShipmentId,
          })),
        });
        if (result.state !== "ok" || !result.data) {
          const raw = result.message ?? "fulfillment.work_queue.bulk_failed";
          const fa = /^[a-z0-9._-]+$/i.test(raw) ? mapAdminErrorMessage(raw, "fa") : raw;
          toast.error(fa);
          return { ok: false, message: fa };
        }
        if (result.data.succeeded < result.data.attempted) {
          const msg = `فقط ${result.data.succeeded.toLocaleString("fa-IR")} از ${result.data.attempted.toLocaleString("fa-IR")} مورد انجام شد.`;
          toast.error(msg);
          refresh();
          return { ok: false, message: msg };
        }
        toast.success(`${result.data.succeeded.toLocaleString("fa-IR")} مورد انجام شد.`);
        refresh();
        return { ok: true, message: "ok" };
      },
    }));
  }, [refresh]);

  const gridProps = useLegacyAdminGridDirectProps({
    gridId: ADMIN_FULFILLMENT_GRID_VIEW_KEY,
    columns,
    queryAdapter,
    savedViewStore,
  });

  if (denied) {
    return (
      <main data-testid="admin-fulfillment-work-queue">
        <PageHeading title="ارسال و تحویل" description="صف کار عملیاتی ارسال و تحویل" />
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
    <main data-testid="admin-fulfillment-work-queue" dir="rtl">
      <PageHeading
        title="ارسال و تحویل"
        description="صف کار عملیاتی بین سفارش‌ها — همان backend fulfillment"
      />
      <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
        <div className="flex flex-col gap-3 border-b border-border px-4 py-3 md:px-5">
          <span className="text-sm text-muted">دادهٔ زندهٔ فروشگاه — server GridQuery</span>
          <div className="flex flex-wrap gap-2" data-testid="admin-fulfillment-queue-filters" role="tablist">
            {FULFILLMENT_QUEUE_FILTERS.map((tab) => {
              const active = queueFilter === tab.id;
              return (
                <button
                  key={tab.id}
                  type="button"
                  role="tab"
                  aria-selected={active}
                  data-testid={`admin-fulfillment-queue-filter-${tab.id}`}
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
            <AppDataGrid<FulfillmentListRow> {...gridProps} bulkActions={bulkActions} />
          )}
        </div>
      </section>
    </main>
  );
}
