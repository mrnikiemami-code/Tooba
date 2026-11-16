"use client";

import type { ReactNode } from "react";
import { Eye, PackageCheck } from "lucide-react";
import { AppGridRowActionsCell, type AppGridRowAction } from "../../design-system/app-data-grid/app-grid-row-actions";
import type { GridColumnDef } from "../../design-system/data-grid";
import {
  formatFulfillmentDate,
  formatFulfillmentQueueQuantity,
  formatFulfillmentShipmentSummary,
  formatFulfillmentStatus,
  fulfillmentStatusBadgeClass,
  type FulfillmentListRow,
} from "../fulfillment/fulfillment-api";
import {
  formatCustomerReceiving,
  formatDispatchBucketLabel,
  formatHandoffWindowLabel,
  formatJobStageLabel,
  formatJobTime,
  formatPlannedHandoff,
  formatPreparationDeadlines,
  formatPreparationState,
  formatWorkUnitOrdinal,
  normalizeDispatchToken,
} from "../fulfillment/fulfillment-work-queue-dispatch";
import { AdminOrderOperationsMenu } from "./admin-order-operations-menu";

/**
 * TB-271 — the queue's column contract, kept beside the screen but out of it.
 *
 * Every cell renders a SERVER-owned fact: the job stage, the meaning of the relevant instant, the next verb and its
 * blocker. Nothing here derives a deadline, a stage or an authorization. `data-testid` names are part of the mounted
 * contract (the row's stage, job time, scope, next step and locate action).
 */

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
  // TB-271: a parcel of a group that already owns several parcels is named by its HUMAN position («مرسولهٔ ۲ از ۳»),
  // so two parcels of one order never look like the same ambiguous row.
  const ordinal = formatWorkUnitOrdinal(row);
  if (ordinal) return ordinal;
  const kind = WORK_UNIT_KIND_LABELS[row.workUnitKind] ?? row.workUnitKind;
  if (row.targetPackageNumber) return `${kind} ${row.targetPackageNumber}`;
  // TB-266: the state badge beside this label already says «بدون گروه پذیرش», so the scope repeats only the
  // unit kind instead of stacking the same words twice inside a single non-wrapping line.
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
      // TB-271: carry the ACTIVE stage/time selection so «بازگشت به صف کار» returns to the same queue view —
      // there is no legacy `filter` token any more, so an old one can never leak back into the URL.
      if (typeof window !== "undefined") {
        const current = new URLSearchParams(window.location.search);
        const stage = current.get("stage");
        const scope = current.get("scope");
        if (stage) query.set("stage", stage);
        if (scope) query.set("scope", scope);
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

/**
 * TB-271: the ONE job stage of this physical row, as the operator reads it. It is a server-provided label over
 * existing facts — the row's capabilities are untouched, and a blocked row keeps its honest stage with the reason
 * available in the same cell's tooltip.
 */
function jobStageBadge(row: FulfillmentListRow) {
  const stage = (row.jobStage ?? "").trim().toLowerCase();
  const tone =
    stage === "preparation"
      ? "bg-amber-50 text-amber-800"
      : stage === "ready_to_send"
        ? "bg-sky-50 text-sky-800"
        : stage === "handoff"
          ? "bg-violet-50 text-violet-800"
          : stage === "delivery"
            ? "bg-indigo-50 text-indigo-800"
            : stage === "returns"
              ? "bg-rose-50 text-rose-700"
              : "bg-slate-100 text-slate-700";
  const blocker = (row.nextStepBlockerFa ?? "").trim();
  return (
    <span
      className={`inline-flex max-w-full min-w-0 items-center overflow-hidden whitespace-nowrap rounded-full px-2 py-0.5 text-[11px] font-bold ${tone}`}
      title={blocker ? `${formatJobStageLabel(row)} — ${blocker}` : formatJobStageLabel(row)}
      data-testid={`admin-fulfillment-row-stage-${row.id}`}
      data-stage={stage}
    >
      <span className="truncate">{formatJobStageLabel(row)}</span>
    </span>
  );
}

/** TB-271: the relevant operational time of THIS row's current job, with its exact meaning. */
function jobTimeBadge(row: FulfillmentListRow) {
  const state = (row.jobTimeState ?? "").trim().toLowerCase();
  const tone =
    state === "past_due"
      ? "text-rose-700"
      : state === "due_today"
        ? "text-amber-800"
        : state === "undated"
          ? "text-muted"
          : "text-foreground";
  return (
    <span
      className={`block truncate text-xs font-semibold ${tone}`}
      title={formatJobTime(row)}
      data-testid={`admin-fulfillment-job-time-${row.id}`}
      data-job-time-state={state}
    >
      {formatJobTime(row)}
    </span>
  );
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
    // TB-266: the bucket pill stays on ONE line — wrapping its hints inside a fixed row height was what pushed the
    // pill's text out of the cell. The full sentence is kept as a tooltip.
    <span
      className={`inline-flex max-w-full min-w-0 items-center gap-1 overflow-hidden whitespace-nowrap rounded-full px-2 py-0.5 text-[11px] font-bold ${tone}`}
      title={`${formatDispatchBucketLabel(row.dispatchBucket)}${windowHint}${preparationHint}`}
    >
      <span className="shrink-0">{formatDispatchBucketLabel(row.dispatchBucket)}</span>
      {windowHint ? <span className="truncate font-medium opacity-80">{windowHint}</span> : null}
      {preparationHint ? <span className="shrink-0 font-medium opacity-80">{preparationHint}</span> : null}
    </span>
  );
}

/**
 * Builds the queue's columns. `onRefresh` is used ONLY by the fulfilment-level action menu after a command.
 *
 * Column widths are the operator's own authored preference: the saved view named «اصلی» on
 * `grid.admin.fulfillments` was promoted here into code defaults (asli-grid) and then deleted. Raw fractional
 * ag-grid widths are kept unrounded so the default render stays pixel-identical to that view. Everything else in
 * the view (layout.order, `shippingMethodCode` hidden, sorts = [], pageSize = 10) already matched the code.
 */
export function createFulfillmentWorkQueueColumns(
  onRefresh: () => void,
): GridColumnDef<FulfillmentListRow>[] {
  return [
    {
      id: "orderReference",
      header: "سفارش",
      accessor: (row) => row.orderReference,
      cell: (row) => truncatedCell(row.orderReference || "—", row.orderReference || undefined),
      width: 255.5555419921875,
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
      width: 217.77777099609375,
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
        // TB-266: this cell used to stack the badge over a wrapping scope label inside a fixed row height, which
        // clipped the badge vertically (and at 390px pushed the scope into 3–4 word-wrapped lines). Both items now
        // share ONE non-wrapping line: the badge keeps its full height and the scope truncates with a tooltip.
        const history = row.workUnitKind === "fulfillment";
        const scope = history ? "" : workUnitKindLabel(row);
        return (
          <span
            className="flex w-full min-w-0 items-center gap-1.5 overflow-hidden whitespace-nowrap"
            data-testid={`admin-fulfillment-work-unit-${row.id}`}
          >
            <span
              className={`${fulfillmentStatusBadgeClass(history ? row.status : row.workUnitState || row.status)} shrink-0 whitespace-nowrap`}
            >
              {history ? formatFulfillmentStatus(row.status) : workUnitStateLabel(row)}
            </span>
            {scope ? (
              <span
                className="min-w-0 truncate text-[11px] leading-4 text-muted"
                title={scope}
                data-testid={`admin-fulfillment-work-scope-${row.id}`}
              >
                {scope}
              </span>
            ) : null}
          </span>
        );
      },
      width: 313.3333740234375,
      minWidth: 150,
      filterKind: "status",
      enumOptions: fulfillmentStatusEnumOptions,
      sortable: true,
    },
    {
      id: "jobStage",
      header: "مرحلهٔ کار",
      accessor: (row) => row.jobStage,
      cell: (row) => jobStageBadge(row),
      width: 170,
      minWidth: 140,
      // TB-271: the stage is a server classification of the physical rows; it is displayed, never filtered here.
      sortable: false,
    },
    {
      id: "jobTime",
      header: "زمان کار جاری",
      accessor: (row) => row.jobTimeUtc ?? "",
      cell: (row) => jobTimeBadge(row),
      width: 230,
      minWidth: 170,
      sortable: false,
    },
    {
      id: "dispatchBucket",
      header: "بُعد زمانی ارسال",
      accessor: (row) => normalizeDispatchToken(row.dispatchBucket),
      cell: (row) => dispatchBucketBadge(row),
      width: 233.33331298828125,
      minWidth: 150,
      // Timing is filtered by the server-authoritative stage/time controls (full-set counts), so the column is
      // sortable but deliberately not a second, silently-ignored filter surface.
      sortable: true,
    },
    {
      id: "dispatchDeadline",
      header: "موعد ارسال",
      accessor: (row) => row.plannedHandoffDate ?? "",
      cell: (row) => (
        <span
          className="block truncate text-xs font-semibold"
          title={formatPlannedHandoff(row)}
          data-testid={`admin-fulfillment-plan-${row.id}`}
        >
          {formatPlannedHandoff(row)}
        </span>
      ),
      width: 291.111083984375,
      minWidth: 160,
      sortable: true,
    },
    {
      id: "operationalDeadlines",
      header: "مهلت‌های عملیاتی",
      accessor: (row) => row.preparationStartUtc ?? "",
      cell: (row) => (
        <span
          className="block truncate text-[11px] text-muted"
          title={formatPreparationDeadlines(row)}
          data-testid={`admin-fulfillment-deadlines-${row.id}`}
        >
          {formatPreparationDeadlines(row)}
        </span>
      ),
      width: 587.7778015136719,
      minWidth: 170,
      sortable: false,
    },
    {
      id: "customerReceiving",
      header: "تحویل مشتری",
      accessor: (row) => row.customerReceivingDate ?? "",
      cell: (row) => (
        <span
          className="block truncate text-xs"
          title={formatCustomerReceiving(row)}
          data-testid={`admin-fulfillment-receiving-${row.id}`}
        >
          {formatCustomerReceiving(row)}
        </span>
      ),
      width: 188.888916015625,
      minWidth: 140,
      sortable: false,
    },
    {
      id: "nextStep",
      header: "اقدام بعدی",
      accessor: (row) => row.nextStepLabelFa,
      cell: (row) => (
        // TB-266: label and blocker share ONE non-wrapping line; the blocker keeps its warning tone without adding a
        // second wrapped line inside the fixed row height.
        <span
          className="flex min-w-0 items-center gap-1 overflow-hidden whitespace-nowrap text-xs"
          title={[row.nextStepLabelFa, row.nextStepBlockerFa].filter(Boolean).join(" — ")}
          data-testid={`admin-fulfillment-next-step-${row.id}`}
        >
          {row.nextStepLabelFa ? (
            <span className="shrink-0 font-semibold">{row.nextStepLabelFa}</span>
          ) : (
            <span className="text-muted">—</span>
          )}
          {row.nextStepBlockerFa ? (
            <span className="min-w-0 truncate text-[11px] text-amber-800">{row.nextStepBlockerFa}</span>
          ) : null}
        </span>
      ),
      width: 220,
      minWidth: 160,
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
      width: 268.88885498046875,
      minWidth: 110,
      filterKind: "text",
    },
    {
      id: "recipientName",
      header: "گیرنده",
      accessor: (row) => row.recipientName,
      cell: (row) => truncatedCell(row.recipientName || "بدون نام", row.recipientName),
      width: 234.4444580078125,
      minWidth: 110,
      filterKind: "text",
      sortable: true,
    },
    {
      id: "cityName",
      header: "مقصد / شهر",
      accessor: (row) => row.cityName,
      cell: (row) => truncatedCell(row.cityName || "—", row.cityName),
      width: 170,
      minWidth: 96,
      filterKind: "text",
      sortable: true,
    },
    {
      id: "createdAt",
      header: "زمان ایجاد",
      accessor: (row) => row.createdAt,
      cell: (row) => truncatedCell(formatFulfillmentDate(row.createdAt)),
      width: 172.22222900390625,
      minWidth: 120,
      filterKind: "date",
      sortable: true,
    },
    {
      id: "updatedAt",
      header: "آخرین تغییر",
      accessor: (row) => row.updatedAt,
      cell: (row) => truncatedCell(formatFulfillmentDate(row.updatedAt)),
      width: 163.33331298828125,
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
              onCompleted={onRefresh}
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
  ];
}
