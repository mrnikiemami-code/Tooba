"use client";

import { useCallback, useMemo, useState, type ReactNode } from "react";
import { ClipboardCheck } from "lucide-react";
import {
  AppDataGrid,
  ErrorState,
  adminGridQueryAdapter,
  faWorkspaceMessages,
  useLegacyAdminGridDirectProps,
} from "../../design-system";
import { AppGridRowActionsCell, type AppGridRowAction } from "../../design-system/app-data-grid/app-grid-row-actions";
import type { GridColumnDef, GridServerQuery } from "../../design-system/data-grid";
import { toPersianDigits } from "../../lib/persian-digits";
import { formatQuantityDisplay } from "../../lib/quantity-display";
import {
  RETURN_WORK_STRIPS,
  formatRefundLifecycleStatus,
  formatReturnDate,
  formatReturnStatus,
  formatReturnWorkCategory,
  queryAdminReturnsWorkQueue,
  readReturnWorkSelectionFromUrl,
  refundLifecycleBadgeClass,
  returnStatusBadgeClass,
  returnWorkBadgeClass,
  writeReturnWorkSelectionToUrl,
  type AdminReturnWorkCounts,
  type ReturnWorkQueueRow,
  type ReturnWorkQueueSelection,
} from "../returns/return-api";
import { mapAdminErrorMessage } from "./admin-error-map";
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

/**
 * TB-275: the ONE row action. The queue is a work list, not an action surface: every decision, receipt, retry and
 * physical settlement happens inside the reviewed case workspace, so a kebab menu can never fire an intent the
 * operator did not deliberately review.
 */
const reviewAction: AppGridRowAction<ReturnWorkQueueRow>[] = [
  {
    id: "review-return",
    label: "بررسی مرجوعی",
    icon: ClipboardCheck,
    href: (row) => `/admin/returns/${row.returnRequestId}`,
    testId: (row) => `admin-return-review-${row.returnRequestId}`,
  },
];

/** One requested line group: quantity + its OWN unit, never summed across incompatible units. */
function lineSummary(row: ReturnWorkQueueRow): { label: string; meta: string } {
  if (row.lines.length === 0) {
    return {
      label: row.productLabel || "—",
      meta: `${formatQuantityDisplay(row.quantityRequested)} ${row.unitLabel}`.trim(),
    };
  }
  if (row.lines.length === 1) {
    const line = row.lines[0]!;
    return {
      label: line.productLabel || row.productLabel || "—",
      meta: `${formatQuantityDisplay(line.quantity)} ${line.unitLabel}`.trim(),
    };
  }
  return {
    label: `${toPersianDigits(row.lines.length)} قلم کالا`,
    meta: row.lines
      .map((line) => `${formatQuantityDisplay(line.quantity)} ${line.unitLabel}`)
      .join(" · "),
  };
}

/**
 * TB-275: the authoritative Returns work queue — one server-classified work stage per case, counted BEFORE paging, and
 * a single reviewed entry into the case workspace.
 */
export function AdminReturnsWorkQueueScreen() {
  const savedViewStore = useMemo(() => createHostSavedViewStore(ADMIN_RETURN_GRID_VIEW_KEY), []);
  // The strip lives in the URL as ONE canonical token, so opening a case and coming back restores the same selection.
  const [selection, setSelection] = useState<ReturnWorkQueueSelection>(() =>
    typeof window === "undefined" ? "open" : readReturnWorkSelectionFromUrl(window.location.search),
  );
  const [counts, setCounts] = useState<AdminReturnWorkCounts>();
  const [denied, setDenied] = useState(false);
  const [gridError, setGridError] = useState<string>();
  const [reloadToken, setReloadToken] = useState(0);
  const refresh = useCallback(() => setReloadToken((value) => value + 1), []);

  const select = useCallback((next: ReturnWorkQueueSelection) => {
    if (typeof window !== "undefined") {
      const search = writeReturnWorkSelectionToUrl(window.location.search, next);
      window.history.replaceState(null, "", `${window.location.pathname}${search}`);
    }
    setSelection(next);
  }, []);

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
      id: "productLabel",
      header: "کالا",
      accessor: (row) => lineSummary(row).label,
      cell: (row) => {
        const summary = lineSummary(row);
        return (
          <div className="app-grid-cell-content min-w-0">
            <span className="block truncate font-semibold" title={summary.label}>{summary.label}</span>
            <span className="block truncate text-xs text-muted" dir="rtl">{summary.meta}</span>
          </div>
        );
      },
      width: 200,
      minWidth: 150,
      filterKind: "text",
      sortable: true,
    },
    {
      id: "workCategory",
      header: "مرحلهٔ کار",
      accessor: (row) => row.workCategory,
      cell: (row) => (
        <div className="app-grid-cell-content min-w-0">
          <span className={returnWorkBadgeClass(row.workCategory)}>
            {formatReturnWorkCategory(row.workCategory)}
          </span>
          {row.workBlockerCode ? (
            <span className="mt-1 block truncate text-xs text-amber-700" title={mapAdminErrorMessage(row.workBlockerCode, "fa")}>
              {mapAdminErrorMessage(row.workBlockerCode, "fa")}
            </span>
          ) : null}
        </div>
      ),
      width: 180,
      minWidth: 140,
      filterKind: "status",
      enumOptions: RETURN_WORK_STRIPS.filter((strip) => strip.id !== "open" && strip.id !== "history").map((strip) => ({
        value: strip.id,
        label: strip.labelFa,
      })),
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
          <AppGridRowActionsCell row={row} actions={reviewAction} compact />
        </span>
      ),
      width: 150,
      minWidth: 130,
      exportable: false,
      sortable: false,
    },
  ], []);

  const queryFn = useCallback(
    async (query: GridServerQuery) => {
      const result = await queryAdminReturnsWorkQueue(query, selection);
      if (result.meta) setCounts(result.meta);
      return result;
    },
    [selection],
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
          <div className="flex flex-wrap items-center justify-between gap-2">
            <span className="text-sm text-muted">دادهٔ زندهٔ فروشگاه — مرحلهٔ کار پیش از صفحه‌بندی شمرده می‌شود</span>
            <span className="text-xs text-muted" data-testid="admin-returns-active-count">
              {counts ? `${toPersianDigits(counts.allOpen)} پروندهٔ باز` : "—"}
            </span>
          </div>
          <div
            className="flex flex-wrap gap-2"
            data-testid="admin-returns-work-strips"
            role="tablist"
            aria-label="مرحلهٔ کار مرجوعی"
          >
            {RETURN_WORK_STRIPS.map((strip) => {
              const active = selection === strip.id;
              const count = counts ? counts[strip.countKey] : undefined;
              return (
                <button
                  key={strip.id}
                  type="button"
                  role="tab"
                  aria-selected={active}
                  data-testid={`admin-returns-queue-filter-${strip.id}`}
                  className={
                    active
                      ? "inline-flex min-h-11 items-center gap-2 rounded-full bg-primary px-4 text-sm font-bold text-primary-foreground"
                      : "inline-flex min-h-11 items-center gap-2 rounded-full border border-border bg-surface px-4 text-sm font-semibold text-muted hover:bg-secondary"
                  }
                  onClick={() => {
                    if (selection === strip.id) return;
                    select(strip.id);
                    refresh();
                  }}
                >
                  <span>{strip.labelFa}</span>
                  <span
                    className={
                      active
                        ? "rounded-full bg-primary-foreground/20 px-2 text-xs"
                        : "rounded-full bg-secondary px-2 text-xs"
                    }
                  >
                    {toPersianDigits(count ?? 0)}
                  </span>
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
