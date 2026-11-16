"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { toast } from "react-toastify";
import {
  AppDataGrid,
  ErrorState,
  adminGridQueryAdapter,
  faWorkspaceMessages,
  useLegacyAdminGridDirectProps,
} from "../../design-system";
import type { GridBulkAction, GridServerQuery } from "../../design-system/data-grid";
import { DEFAULT_GRID_QUERY } from "../../design-system/app-data-grid/grid-query-mapper.ts";
import {
  FULFILLMENT_BULK_ACTION_LABELS,
  FULFILLMENT_SAFE_BULK_ACTION_CODES,
  areFulfillmentBulkCompatible,
  executeAdminFulfillmentWorkQueueBulk,
  queryAdminFulfillmentsWorkQueue,
  type FulfillmentListRow,
} from "../fulfillment/fulfillment-api";
import {
  FULFILLMENT_HISTORY_STAGE_LABEL,
  FULFILLMENT_JOB_STAGES,
  FULFILLMENT_TIME_SCOPES,
  legacyQueueSelection,
  normalizeJobStage,
  normalizeTimeScope,
  stageCount,
  timeCount,
  type FulfillmentJobStage,
  type FulfillmentQueueCounts,
  type FulfillmentTimeScope,
} from "../fulfillment/fulfillment-work-queue-dispatch";
import { createFulfillmentWorkQueueColumns } from "./admin-fulfillment-work-queue-columns";
import { mapAdminErrorMessage } from "./admin-error-map";
import { ADMIN_FULFILLMENT_GRID_VIEW_KEY, createHostSavedViewStore } from "./saved-view-store";
import { fromPersianDigits } from "../../lib/persian-digits.ts";

function PageHeading({ title, description }: { title: string; description?: string }) {
  return (
    <div className="mb-5">
      <p className="text-sm text-muted">خانه / {title}</p>
      <h1 className="mt-1 text-2xl font-semibold tracking-tight">{title}</h1>
      {description ? <p className="mt-1 text-base text-muted">{description}</p> : null}
    </div>
  );
}

/**
 * صف کار عملیاتی ارسال و تحویل — AppDataGrid + یک مرحلهٔ کار + یک بازهٔ زمانی + kebab + bulk سازگار.
 *
 * TB-271: the two overlapping quick-filter rows are gone. ONE job-stage strip (server-classified, one stage per
 * ACTIVE physical row) and ONE independent time scope are the only selection surface; both are read tokens sent to
 * the existing grid query, so the server keeps the single filtering/counting authority. Orders UI را دست نمی‌زند؛
 * فقط همین صفحه.
 */
/**
 * TB-271-R1: this grid's clean entry is the EMPLOYEE job priority (overdue → today → undated → future), owned by
 * the server. The shared grid default (`updatedAt desc`) is a column sort the operator never asked for, so it is
 * not sent here: an empty sort is what lets the documented queue order run. Any explicit operator/saved-view sort
 * still wins, because the grid sends it verbatim.
 */
const QUEUE_DEFAULT_QUERY: GridServerQuery = { ...DEFAULT_GRID_QUERY, sorts: [] };

export function AdminFulfillmentWorkQueueScreen() {
  const savedViewStore = useMemo(() => createHostSavedViewStore(ADMIN_FULFILLMENT_GRID_VIEW_KEY), []);
  // TB-271: ONE job-stage selection and ONE independent time scope. `null` counts mean "unknown" and are rendered
  // as «—», never as a successful-looking zero.
  const [jobStage, setJobStage] = useState<FulfillmentJobStage>("all");
  const [timeScope, setTimeScope] = useState<FulfillmentTimeScope>("today");
  const [counts, setCounts] = useState<FulfillmentQueueCounts | null>(null);
  const [denied, setDenied] = useState(false);
  const [gridError, setGridError] = useState<string>();
  const [reloadToken, setReloadToken] = useState(0);
  const refresh = useCallback(() => setReloadToken((value) => value + 1), []);
  // Counters are fenced too: a slow response for an OLD stage/time selection must never publish its badges.
  const requestSeq = useRef(0);

  /**
   * Reads the queue selection from the URL. A legacy `filter` deep link is translated ONCE through the documented
   * mapping and rewritten into `stage`/`scope`, so an old link can never combine with the visible controls.
   */
  const readSelectionFromUrl = useCallback((url: URL): { stage: FulfillmentJobStage; scope: FulfillmentTimeScope } => {
    const legacy = url.searchParams.get("filter");
    if (legacy) {
      const mapped = legacyQueueSelection(legacy);
      url.searchParams.delete("filter");
      url.searchParams.set("stage", mapped.stage);
      url.searchParams.set("scope", mapped.scope);
      window.history.replaceState({}, "", url.toString());
      return mapped;
    }
    return {
      stage: normalizeJobStage(url.searchParams.get("stage")),
      scope: normalizeTimeScope(url.searchParams.get("scope")),
    };
  }, []);

  // Clean entry = the live default work; an explicit valid URL state is restored exactly.
  useEffect(() => {
    const next = readSelectionFromUrl(new URL(window.location.href));
    setJobStage(next.stage);
    setTimeScope(next.scope);
  }, [readSelectionFromUrl]);

  // Browser back/forward restores the same queue state instead of leaving a stale hidden selection behind.
  useEffect(() => {
    const onPopState = () => {
      const next = readSelectionFromUrl(new URL(window.location.href));
      setJobStage(next.stage);
      setTimeScope(next.scope);
    };
    window.addEventListener("popstate", onPopState);
    return () => window.removeEventListener("popstate", onPopState);
  }, [readSelectionFromUrl]);

  const applySelection = useCallback(
    (next: { stage: FulfillmentJobStage; scope: FulfillmentTimeScope }) => {
      setJobStage(next.stage);
      setTimeScope(next.scope);
      const url = new URL(window.location.href);
      url.searchParams.delete("filter");
      if (next.stage === "all") url.searchParams.delete("stage");
      else url.searchParams.set("stage", next.stage);
      if (next.scope === "today") url.searchParams.delete("scope");
      else url.searchParams.set("scope", next.scope);
      window.history.pushState({}, "", url.toString());
    },
    [],
  );

  const columns = useMemo(() => createFulfillmentWorkQueueColumns(refresh), [refresh]);

  const queryFn = useCallback(
    async (query: GridServerQuery) => {
      const sequence = ++requestSeq.current;
      const filters = { ...query.filters };
      // TB-271: the ONE selection authority — the two narrow tokens. A legacy `queueFilter` is never sent, so a
      // hidden second filter can never combine with the visible stage/time controls.
      filters.jobStage = { kind: "status", values: [jobStage] };
      filters.timeScope = { kind: "status", values: [timeScope] };
      delete filters.queueFilter;
      // Map shipping method column filter field to code when enum selected via label column.
      if (filters.shippingMethodLabel && filters.shippingMethodLabel.kind === "status") {
        filters.shippingMethodCode = filters.shippingMethodLabel;
        delete filters.shippingMethodLabel;
      }
      // Codes are DISPLAYED with Persian digits, so an operator copies «TB-۲۰۲۶…» and pastes it into search or a
      // text filter. The server still stores the Latin code, so the outgoing value is folded back to Latin digits;
      // nothing else about the query, the filter or the server contract changes.
      for (const [field, value] of Object.entries(filters)) {
        if (value.kind === "text" && value.query) {
          filters[field] = { ...value, query: fromPersianDigits(value.query) };
        }
      }
      const search = (query.search ?? "").trim();
      const response = await queryAdminFulfillmentsWorkQueue({
        ...query,
        search: search ? fromPersianDigits(search) : undefined,
        filters,
      });
      // Counts come from the server for the WHOLE filtered set; a page-local tally would be a lie. A response for
      // an OLDER stage/time selection never publishes its badges over a newer one.
      if (sequence === requestSeq.current) setCounts(response.meta ?? null);
      return response;
    },
    [jobStage, timeScope],
  );

  const queryAdapter = useCallback(
    async (query: GridServerQuery) => {
      void reloadToken;
      return adminGridQueryAdapter(
        queryFn,
        () => setDenied(true),
        (message) => {
          // A missing/error payload is unknown, not six honest zeros.
          setCounts(null);
          setGridError(message);
        },
      )(query);
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
          {/*
            TB-271: ONE job-stage strip replaces the two overlapping quick-filter rows. Each badge counts the WHOLE
            active set inside the selected time scope (server-owned), and «سابقهٔ ارسال» stays a separate quiet
            archive choice outside the five competing work stages.
          */}
          <div
            className="flex flex-wrap gap-2"
            data-testid="admin-fulfillment-job-stages"
            role="tablist"
            aria-label="مرحلهٔ کار"
          >
            {FULFILLMENT_JOB_STAGES.map((stage) => {
              const active = jobStage === stage.id;
              const badge = counts ? stageCount(counts, stage.id) : null;
              return (
                <button
                  key={stage.id}
                  type="button"
                  role="tab"
                  aria-selected={active}
                  data-testid={`admin-fulfillment-job-stage-${stage.id}`}
                  data-count={badge ?? ""}
                  className={
                    active
                      ? "inline-flex min-h-11 items-center gap-1.5 rounded-full bg-primary px-3 text-xs font-bold text-primary-foreground"
                      : "inline-flex min-h-11 items-center gap-1.5 rounded-full border border-border bg-surface px-3 text-xs font-semibold text-muted hover:bg-secondary"
                  }
                  onClick={() => applySelection({ stage: stage.id, scope: timeScope })}
                >
                  <span>{stage.labelFa}</span>
                  <span
                    className={
                      active
                        ? "rounded-full bg-primary-foreground/20 px-1.5 py-0.5 text-[11px] tabular-nums"
                        : "rounded-full bg-secondary px-1.5 py-0.5 text-[11px] tabular-nums text-foreground"
                    }
                    data-testid={`admin-fulfillment-job-stage-count-${stage.id}`}
                    title={badge === null ? "شمارش در دسترس نیست" : undefined}
                  >
                    {badge === null ? "—" : badge.toLocaleString("fa-IR")}
                  </span>
                </button>
              );
            })}
          </div>
          <div className="flex flex-wrap items-center gap-3">
            <div
              className="flex flex-wrap gap-1.5"
              data-testid="admin-fulfillment-time-scopes"
              role="tablist"
              aria-label="بازهٔ زمانی"
            >
              {FULFILLMENT_TIME_SCOPES.map((scope) => {
                const active = timeScope === scope.id && jobStage !== "history";
                const badge = counts ? timeCount(counts, scope.id) : null;
                return (
                  <button
                    key={scope.id}
                    type="button"
                    role="tab"
                    aria-selected={active}
                    disabled={jobStage === "history"}
                    title={jobStage === "history" ? "سابقهٔ ارسال بازهٔ زمانی ندارد" : scope.hintFa}
                    data-testid={`admin-fulfillment-time-scope-${scope.id}`}
                    data-count={badge ?? ""}
                    className={
                      active
                        ? "inline-flex min-h-11 items-center gap-1.5 rounded-lg bg-secondary px-3 text-xs font-bold text-foreground"
                        : "inline-flex min-h-11 items-center gap-1.5 rounded-lg border border-border bg-surface px-3 text-xs font-semibold text-muted hover:bg-secondary disabled:cursor-not-allowed disabled:opacity-50"
                    }
                    onClick={() => applySelection({ stage: jobStage, scope: scope.id })}
                  >
                    <span>{scope.labelFa}</span>
                    <span
                      className="rounded-full bg-surface px-1.5 py-0.5 text-[11px] tabular-nums text-foreground"
                      data-testid={`admin-fulfillment-time-scope-count-${scope.id}`}
                    >
                      {badge === null ? "—" : badge.toLocaleString("fa-IR")}
                    </span>
                  </button>
                );
              })}
            </div>
            <button
              type="button"
              role="tab"
              aria-selected={jobStage === "history"}
              data-testid="admin-fulfillment-job-stage-history"
              className={
                jobStage === "history"
                  ? "inline-flex min-h-11 items-center gap-1.5 rounded-lg bg-slate-200 px-3 text-xs font-bold text-slate-800"
                  : "inline-flex min-h-11 items-center gap-1.5 rounded-lg px-3 text-xs font-semibold text-muted hover:bg-secondary"
              }
              onClick={() => applySelection({ stage: "history", scope: "all" })}
            >
              <span>{FULFILLMENT_HISTORY_STAGE_LABEL}</span>
              <span className="rounded-full bg-secondary px-1.5 py-0.5 text-[11px] tabular-nums text-foreground">
                {counts === null ? "—" : counts.stageHistory.toLocaleString("fa-IR")}
              </span>
            </button>
            <p
              className="text-xs text-muted"
              data-testid="admin-fulfillment-queue-summary"
              data-selected-total={counts?.selectedTotal ?? ""}
            >
              {counts === null
                ? "شمارش این نمایش در دسترس نیست."
                : counts.undated > 0
                  ? `${counts.selectedTotal.toLocaleString("fa-IR")} ردیف در این نمایش · ${counts.undated.toLocaleString("fa-IR")} ردیف بدون موعد مشخص`
                  : `${counts.selectedTotal.toLocaleString("fa-IR")} ردیف در این نمایش`}
            </p>
          </div>
        </div>
        <div className="overflow-x-auto p-2 md:p-4">
          {gridError ? (
            <ErrorState title="فروشگاه در دسترس نیست" detail={gridError} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
          ) : (
            <AppDataGrid<FulfillmentListRow>
              {...gridProps}
              rowHeight={64}
              bulkActions={bulkActions}
              defaultQuery={QUEUE_DEFAULT_QUERY}
            />
          )}
        </div>
      </section>
    </main>
  );
}
