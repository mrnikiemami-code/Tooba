"use client";

import { useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import { Eye, PackageCheck } from "lucide-react";
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
  queryAdminFulfillmentsWorkQueue,
  type FulfillmentListRow,
  type FulfillmentQueueFilter,
} from "../fulfillment/fulfillment-api";
import {
  FULFILLMENT_DISPATCH_FILTERS,
  FULFILLMENT_EMPTY_QUEUE_COUNTS,
  formatCustomerReceiving,
  formatDispatchBucketLabel,
  formatHandoffWindowLabel,
  formatPlannedHandoff,
  formatPreparationDeadlines,
  formatPreparationState,
  normalizeDispatchToken,
  type FulfillmentQueueCounts,
} from "../fulfillment/fulfillment-work-queue-dispatch";
import { mapAdminErrorMessage } from "./admin-error-map";
import { AdminOrderOperationsMenu } from "./admin-order-operations-menu";
import { ADMIN_FULFILLMENT_GRID_VIEW_KEY, createHostSavedViewStore } from "./saved-view-store";

function PageHeading({ title, description }: { title: string; description?: string }) {
  return (
    <div className="mb-5">
      <p className="text-sm text-muted">خانه / {title}</p>
      <h1 className="mt-1 text-2xl font-semibold tracking-tight">{title}</h1>
      {description ? <p className="mt-1 text-base text-muted">{description}</p> : null}
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

/**
 * TB-260-R1: the row's scope, in operator words. A seller order can now appear several times (one row per
 * physical work unit), so the row must say WHAT it owns — an unallocated group, one parcel or one package.
 */
const WORK_UNIT_KIND_LABELS: Record<string, string> = {
  group: "گروه ارسال",
  parcel: "مرسوله",
  package: "بستهٔ تجمیعی",
  fulfillment: "سابقهٔ ارسال",
};

const WORK_UNIT_STATE_LABELS: Record<string, string> = {
  Created: "در انتظار ارسال به حامل",
  Dispatched: "ارسال‌شده به حامل",
  InTransit: "در مسیر تحویل",
  Delivered: "تحویل‌شده",
  Failed: "ناموفق",
  Cancelled: "لغو‌شده",
  NonDelivered: "تحویل ناموفق",
  Returning: "در حال برگشت به مبدأ",
  Returned: "دریافت‌شده در مبدأ",
};

/** The physical state of THIS work unit (never the whole seller order's aggregate status). */
function workUnitStateLabel(row: FulfillmentListRow): string {
  const state = row.workUnitState || "";
  if (state && WORK_UNIT_STATE_LABELS[state]) return WORK_UNIT_STATE_LABELS[state];
  if (row.workUnitKind === "group") {
    return row.workUnitState === "Ungrouped" ? "بدون گروه پذیرش" : "آمادهٔ اقدام عملیاتی";
  }
  return state || formatFulfillmentStatus(row.status);
}

/** What this row owns, in operator words — the row is ONE physical unit, not a seller order. */
function workUnitKindLabel(row: FulfillmentListRow): string {
  const kind = WORK_UNIT_KIND_LABELS[row.workUnitKind] ?? row.workUnitKind;
  if (row.targetPackageNumber) return `${kind} ${row.targetPackageNumber}`;
  if (row.workUnitKind === "group" && !row.targetShippingGroupId) return `${kind} (بدون گروه پذیرش)`;
  return kind;
}

const viewActions: AppGridRowAction<FulfillmentListRow>[] = [
  {
    id: "open-operations",
    label: "عملیات ارسال",
    icon: PackageCheck,
    href: (row) => {
      const query = new URLSearchParams({ fulfillmentId: row.fulfillmentId, from: "queue" });
      // TB-260-R1: the locator is the EXACT physical target of this row — a group, a parcel or a package.
      // Sending only the seller's earliest group is how the wrong work used to be opened.
      if (row.targetShippingGroupId) query.set("shippingGroupId", row.targetShippingGroupId);
      if (row.targetShipmentId) query.set("shipmentId", row.targetShipmentId);
      if (row.targetPackageId) query.set("packageId", row.targetPackageId);
      // Carry the active quick filter so «بازگشت به صف کار» returns to the same queue view.
      if (typeof window !== "undefined") {
        const active = new URLSearchParams(window.location.search).get("filter");
        if (active) query.set("filter", active);
      }
      return `/admin/orders/${row.checkoutId}/operations?${query.toString()}`;
    },
    testId: (row) => `admin-fulfillment-open-operations-${row.id}`,
  },
  {
    id: "view-order",
    label: "مشاهده سفارش",
    icon: Eye,
    href: (row) => `/admin/orders/${row.checkoutId}`,
    testId: (row) => `admin-fulfillment-view-order-${row.id}`,
  },
];

function dispatchCountFor(filter: FulfillmentQueueFilter, counts: FulfillmentQueueCounts): number {
  switch (filter) {
    case "dispatch_preparation_due":
      return counts.preparationDue;
    case "dispatch_overdue":
      return counts.overdue;
    case "dispatch_today":
      return counts.today;
    case "dispatch_due":
      return counts.due;
    case "dispatch_upcoming":
      return counts.upcoming;
    case "dispatch_unspecified":
      return counts.unspecified;
    case "in_transit":
      return counts.inTransit;
    default:
      return 0;
  }
}

/** بُعد زمانی سرور را با رنگ و متن کوتاه نشان می‌دهد؛ مقدار ناشناس نیز صادقانه برچسب می‌خورد. */
function dispatchBucketBadge(row: FulfillmentListRow) {
  const bucket = normalizeDispatchToken(row.dispatchBucket);
  const windowState = normalizeDispatchToken(row.handoffWindowState);
  const tone =
    bucket === "overdue"
      ? "bg-rose-50 text-rose-700"
      : bucket === "today"
        ? "bg-amber-50 text-amber-800"
        : bucket === "upcoming"
          ? "bg-sky-50 text-sky-800"
          : bucket === "in_transit"
            ? "bg-indigo-50 text-indigo-800"
            : bucket === "unspecified"
              ? "bg-slate-100 text-slate-700"
              : "bg-slate-50 text-slate-600";
  const windowHint = row.pendingDispatch && windowState !== "none" && windowState !== ""
    ? ` · ${formatHandoffWindowLabel(windowState)}`
    : "";
  // TB-264: preparation urgency is server derived; it is surfaced beside the bucket so a row whose
  // preparation should already have started can never look comfortably «آینده».
  const preparationToken = normalizeDispatchToken(row.preparationState);
  const preparationHint = preparationToken === "overdue" || preparationToken === "handoff_missed"
    ? ` · ${formatPreparationState(row.preparationState)}`
    : "";
  return (
    <span className={`inline-flex flex-wrap items-center gap-1 rounded-full px-2 py-0.5 text-[11px] font-bold ${tone}`}>
      {formatDispatchBucketLabel(row.dispatchBucket)}
      {windowHint ? <span className="font-medium opacity-80">{windowHint}</span> : null}
      {preparationHint ? <span className="font-medium opacity-80">{preparationHint}</span> : null}
    </span>
  );
}

/**
 * صف کار عملیاتی ارسال و تحویل — AppDataGrid + فیلتر سریع سروری + kebab + bulk سازگار.
 * Orders UI را دست نمی‌زند؛ فقط همین صفحه.
 */
export function AdminFulfillmentWorkQueueScreen() {
  const savedViewStore = useMemo(() => createHostSavedViewStore(ADMIN_FULFILLMENT_GRID_VIEW_KEY), []);
  const [queueFilter, setQueueFilter] = useState<FulfillmentQueueFilter>("all");
  const [counts, setCounts] = useState<FulfillmentQueueCounts>(FULFILLMENT_EMPTY_QUEUE_COUNTS);
  const [denied, setDenied] = useState(false);
  const [gridError, setGridError] = useState<string>();
  const [reloadToken, setReloadToken] = useState(0);
  const refresh = useCallback(() => setReloadToken((value) => value + 1), []);

  // TB-260: returning from the operational view restores the exact quick filter the operator left.
  useEffect(() => {
    const raw = new URLSearchParams(window.location.search).get("filter");
    if (!raw) return;
    const known = [...FULFILLMENT_QUEUE_FILTERS, ...FULFILLMENT_DISPATCH_FILTERS].some((tab) => tab.id === raw);
    if (known) setQueueFilter(raw as FulfillmentQueueFilter);
  }, []);

  const selectFilter = useCallback((id: FulfillmentQueueFilter) => {
    setQueueFilter(id);
    const url = new URL(window.location.href);
    if (id === "all") url.searchParams.delete("filter");
    else url.searchParams.set("filter", id);
    window.history.replaceState({}, "", url.toString());
  }, []);

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
      cell: (row) => {
        // TB-260-R1: the badge describes THIS physical unit. A seller order can now have several rows, so a
        // package/parcel row must never reprint the seller-wide status (a Created parcel under a Dispatched
        // fulfillment would otherwise look dispatched, and «موعد ارسال» would contradict the badge).
        const history = row.workUnitKind === "fulfillment";
        return (
          <span className="flex flex-col gap-0.5" data-testid={`admin-fulfillment-work-unit-${row.id}`}>
            <span className={fulfillmentStatusBadgeClass(history ? row.status : row.workUnitState || row.status)}>
              {history ? formatFulfillmentStatus(row.status) : workUnitStateLabel(row)}
            </span>
            {!history && row.workUnitKind ? (
              <span className="text-[11px] text-muted" data-testid={`admin-fulfillment-work-scope-${row.id}`}>
                {workUnitKindLabel(row)}
              </span>
            ) : null}
          </span>
        );
      },
      width: 150,
      minWidth: 120,
      filterKind: "status",
      enumOptions: fulfillmentStatusEnumOptions,
      sortable: true,
    },
    {
      id: "dispatchBucket",
      header: "بُعد زمانی ارسال",
      accessor: (row) => normalizeDispatchToken(row.dispatchBucket),
      cell: (row) => dispatchBucketBadge(row),
      width: 190,
      minWidth: 150,
      // Timing is filtered by the server-authoritative quick tabs (full-set counts), so the column is
      // sortable but deliberately not a second, silently-ignored filter surface.
      sortable: true,
    },
    {
      id: "dispatchDeadline",
      header: "موعد ارسال",
      accessor: (row) => row.plannedHandoffDate ?? "",
      cell: (row) => (
        <span className="block break-words text-xs font-semibold" data-testid={`admin-fulfillment-plan-${row.id}`}>
          {formatPlannedHandoff(row)}
        </span>
      ),
      width: 210,
      minWidth: 160,
      sortable: true,
    },
    {
      id: "operationalDeadlines",
      header: "مهلت‌های عملیاتی",
      accessor: (row) => row.preparationStartUtc ?? "",
      cell: (row) => (
        <span className="block break-words text-[11px] text-muted" data-testid={`admin-fulfillment-deadlines-${row.id}`}>
          {formatPreparationDeadlines(row)}
        </span>
      ),
      width: 240,
      minWidth: 170,
      sortable: false,
    },
    {
      id: "customerReceiving",
      header: "تحویل مشتری",
      accessor: (row) => row.customerReceivingDate ?? "",
      cell: (row) => (
        <span className="block break-words text-xs" data-testid={`admin-fulfillment-receiving-${row.id}`}>
          {formatCustomerReceiving(row)}
        </span>
      ),
      width: 180,
      minWidth: 140,
      sortable: false,
    },
    {
      id: "nextStep",
      header: "اقدام بعدی",
      accessor: (row) => row.nextStepLabelFa,
      cell: (row) => (
        <span className="block break-words text-xs" data-testid={`admin-fulfillment-next-step-${row.id}`}>
          {row.nextStepLabelFa ? (
            <span className="font-semibold">{row.nextStepLabelFa}</span>
          ) : (
            <span className="text-muted">—</span>
          )}
          {row.nextStepBlockerFa ? (
            <span className="mt-0.5 block text-[11px] text-amber-800">{row.nextStepBlockerFa}</span>
          ) : null}
        </span>
      ),
      width: 200,
      minWidth: 150,
      sortable: false,
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
        <span className="inline-flex items-center gap-1" data-testid={`admin-fulfillment-ops-${row.id}`}>
          <AppGridRowActionsCell row={row} actions={viewActions} compact />
          {/*
            TB-260-R1: the per-fulfillment action menu is a fulfilment-level command surface. A row that owns only
            ONE group / parcel / package must not offer it, otherwise a scoped row could execute a sibling's
            operation (or the wrong parcel's dispatch). Such a row's single action owner is «عملیات ارسال», which
            opens the exact target. History rows (kind = fulfillment) keep the menu when the server allows actions.
          */}
          {row.workUnitKind === "fulfillment" && row.availableActionCodes.length > 0 ? (
            <AdminOrderOperationsMenu
              checkoutId={row.checkoutId}
              label="عملیات"
              compact
              iconOnly
              scope="fulfillment-queue"
              fulfillmentId={row.fulfillmentId}
              onCompleted={refresh}
              testId={`admin-fulfillment-kebab-${row.id}`}
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
      const response = await queryAdminFulfillmentsWorkQueue({ ...query, filters });
      // Counts come from the server for the WHOLE filtered set; a page-local tally would be a lie.
      if (response.meta) setCounts(response.meta);
      return response;
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
      <PageHeading title="ارسال و تحویل" />
      <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
        <div className="flex flex-col gap-3 border-b border-border px-4 py-3 md:px-5">
          <div className="flex flex-wrap gap-2" data-testid="admin-fulfillment-queue-dispatch-filters" role="tablist" aria-label="بُعد زمانی ارسال">
            {FULFILLMENT_DISPATCH_FILTERS.map((tab) => {
              const active = queueFilter === tab.id;
              const badge = dispatchCountFor(tab.id, counts);
              return (
                <button
                  key={tab.id}
                  type="button"
                  role="tab"
                  aria-selected={active}
                  data-testid={`admin-fulfillment-queue-filter-${tab.id}`}
                  data-count={badge}
                  className={
                    active
                      ? "inline-flex min-h-11 items-center gap-1.5 rounded-full bg-primary px-3 text-xs font-bold text-primary-foreground"
                      : "inline-flex min-h-11 items-center gap-1.5 rounded-full border border-border bg-surface px-3 text-xs font-semibold text-muted hover:bg-secondary"
                  }
                  onClick={() => {
                    selectFilter(tab.id);
                    refresh();
                  }}
                >
                  <span>{tab.labelFa}</span>
                  <span
                    className={
                      active
                        ? "rounded-full bg-primary-foreground/20 px-1.5 py-0.5 text-[11px] tabular-nums"
                        : "rounded-full bg-secondary px-1.5 py-0.5 text-[11px] tabular-nums text-foreground"
                    }
                    data-testid={`admin-fulfillment-queue-count-${tab.id}`}
                  >
                    {badge.toLocaleString("fa-IR")}
                  </span>
                </button>
              );
            })}
          </div>
          <div className="flex flex-wrap gap-2" data-testid="admin-fulfillment-queue-filters" role="tablist" aria-label="وضعیت عملیاتی">
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
                      ? "inline-flex min-h-11 items-center rounded-full bg-primary px-3 text-xs font-bold text-primary-foreground"
                      : "inline-flex min-h-11 items-center rounded-full border border-border bg-surface px-3 text-xs font-semibold text-muted hover:bg-secondary"
                  }
                  onClick={() => {
                    selectFilter(tab.id);
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
