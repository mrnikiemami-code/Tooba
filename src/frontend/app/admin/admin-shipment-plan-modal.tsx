"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Button } from "../../design-system/primitives/core";
import { Dialog } from "../../design-system/primitives/overlays";
import { mapAdminErrorMessage } from "./admin-error-map";
import {
  executeAdminOrderOperation,
  loadShipmentDispatchPlanPreview,
  type AdminShipmentDispatchPlanOption,
  type AdminShipmentDispatchPlanPreview,
  type AdminOrderOperationRequest,
} from "./admin-order-operations";
import {
  clearShipmentPlanAttempt,
  markShipmentPlanAttemptFailed,
  mintShipmentPlanOccurrence,
  readShipmentPlanAttempt,
  writeShipmentPlanAttempt,
} from "./admin-shipment-plan-attempt";
import {
  operationalDayLabel,
  operationalInstantLabel,
  operationalTimeWindowLabel,
  operationalTimeZoneLabel,
} from "../../lib/operational-format";

type Props = {
  open: boolean;
  onClose: () => void;
  checkoutId: string;
  shipmentId: string;
  /** Human parcel number inside its group, for the title only. */
  parcelOrdinal?: number;
  /** Viewer/owner scope of the retained review envelope. */
  viewerScopeKey?: string | null;
  onCompleted?: () => void;
  /** Test seams — default to the real client functions. */
  previewLoader?: typeof loadShipmentDispatchPlanPreview;
  operationExecutor?: typeof executeAdminOrderOperation;
};

function slotKey(option: AdminShipmentDispatchPlanOption): string {
  return `${option.receivingDate}|${option.receivingOpensAt}|${option.receivingClosesAt}`;
}

function attemptKindLabel(kind: string | null | undefined): string {
  return kind === "replacement" ? "جایگزینی" : "ارسال مجدد";
}

/**
 * TB-270-R2 — «زمان‌بندی ارسال» یک مرسولهٔ ارسال مجدد/جایگزین موجود.
 *
 * The physical parcel already exists (Fulfillment made it); this review only makes it dispatch-authorized.
 * Real captured inputs → real feasible customer slots → one deliberate selection → one immutable Order plan.
 * No date is invented: a blocker is shown as the blocker, and an expired selection is refused by the server
 * rather than clamped to another slot.
 */
export function AdminShipmentPlanModal({
  open,
  onClose,
  checkoutId,
  shipmentId,
  parcelOrdinal,
  viewerScopeKey,
  onCompleted,
  previewLoader = loadShipmentDispatchPlanPreview,
  operationExecutor = executeAdminOrderOperation,
}: Props) {
  const [loading, setLoading] = useState(false);
  const [preview, setPreview] = useState<AdminShipmentDispatchPlanPreview | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [selectedKey, setSelectedKey] = useState<string | null>(null);
  const [pending, setPending] = useState(false);
  const [sendError, setSendError] = useState<string | null>(null);
  const [reconcileHint, setReconcileHint] = useState(false);
  const inFlightRef = useRef(false);
  const previewLoaderRef = useRef(previewLoader);
  previewLoaderRef.current = previewLoader;
  const operationExecutorRef = useRef(operationExecutor);
  operationExecutorRef.current = operationExecutor;

  const scope = useMemo(
    () => ({ viewerScopeKey: viewerScopeKey ?? null, checkoutId, shipmentId }),
    [viewerScopeKey, checkoutId, shipmentId],
  );

  const load = useCallback(async () => {
    setLoading(true);
    setLoadError(null);
    try {
      const result = await previewLoaderRef.current(checkoutId, shipmentId);
      if (result.state !== "ok" || !result.data) {
        setPreview(null);
        setLoadError(mapAdminErrorMessage(result.message ?? "order.operation.failed", "fa"));
        return;
      }
      setPreview(result.data);
      // A retained review keeps its reviewed slot selected; otherwise nothing is preselected, because the
      // operator must choose the customer commitment deliberately.
      const retained = readShipmentPlanAttempt(scope);
      const retainedKey = retained?.receivingDate
        ? `${retained.receivingDate}|${retained.receivingOpensAt ?? ""}|${retained.receivingClosesAt ?? ""}`
        : null;
      const stillOffered = retainedKey
        && result.data.options.some((option) => slotKey(option) === retainedKey);
      setSelectedKey(stillOffered ? retainedKey : null);
    } catch {
      setPreview(null);
      setLoadError(mapAdminErrorMessage("host-unreachable", "fa"));
    } finally {
      setLoading(false);
    }
  }, [checkoutId, shipmentId, scope]);

  useEffect(() => {
    if (!open) return;
    setPending(false);
    setSendError(null);
    setReconcileHint(false);
    setPreview(null);
    setSelectedKey(null);
    inFlightRef.current = false;
    void load();
  }, [open, load]);

  const options = preview?.options ?? [];
  const selected = options.find((option) => slotKey(option) === selectedKey) ?? null;

  const dates = useMemo(() => {
    const seen = new Set<string>();
    const ordered: string[] = [];
    for (const option of options) {
      if (seen.has(option.receivingDate)) continue;
      seen.add(option.receivingDate);
      ordered.push(option.receivingDate);
    }
    return ordered;
  }, [options]);

  const [activeDate, setActiveDate] = useState<string | null>(null);

  useEffect(() => {
    if (dates.length === 0) {
      setActiveDate(null);
      return;
    }
    const keep = selected?.receivingDate;
    if (keep && dates.includes(keep)) {
      setActiveDate(keep);
      return;
    }
    setActiveDate((current) => (current && dates.includes(current) ? current : dates[0] ?? null));
  }, [dates, selected?.receivingDate]);

  const slotsForDate = useMemo(
    () => options.filter((option) => option.receivingDate === activeDate),
    [options, activeDate],
  );

  const confirmed = preview?.confirmedPlan ?? null;
  const canConfirm = Boolean(selected) && !pending && !confirmed;

  async function confirm() {
    if (!selected || pending || confirmed) return;
    if (inFlightRef.current) return;
    inFlightRef.current = true;
    setPending(true);
    setSendError(null);
    setReconcileHint(false);

    // Retain the reviewed intent + key BEFORE the first POST: a lost response reconciles the SAME plan.
    const retained = readShipmentPlanAttempt(scope);
    const occurrenceKey = retained?.occurrenceKey ?? mintShipmentPlanOccurrence();
    const written = writeShipmentPlanAttempt({
      scope,
      occurrenceKey,
      receivingDate: selected.receivingDate,
      receivingOpensAt: selected.receivingOpensAt,
      receivingClosesAt: selected.receivingClosesAt,
      attempts: (retained?.attempts ?? 0) + 1,
    });
    if (!written?.durable) {
      // No durable envelope means a reload could lose the key and mint a second review; refuse instead.
      inFlightRef.current = false;
      setPending(false);
      setReconcileHint(true);
      setSendError("ذخیرهٔ محلی این مرور ممکن نیست؛ برای جلوگیری از ثبت دوباره، تا تازه‌سازی صفحه اقدام نکنید.");
      return;
    }

    const body: AdminOrderOperationRequest = {
      code: "plan_shipment_dispatch",
      shipmentId,
      idempotencyKey: occurrenceKey,
      dispatchPlanReceivingDate: selected.receivingDate,
      dispatchPlanReceivingOpensAt: selected.receivingOpensAt,
      dispatchPlanReceivingClosesAt: selected.receivingClosesAt,
    };

    try {
      const result = await operationExecutorRef.current(checkoutId, body);
      if (result.state !== "ok") {
        markShipmentPlanAttemptFailed(
          scope,
          written.attempt,
          mapAdminErrorMessage(result.message ?? "order.operation.failed", "fa"),
        );
        setSendError(mapAdminErrorMessage(result.message ?? "order.operation.failed", "fa"));
        // A refusal is deterministic, but the reviewed intent is kept: the operator may pick a fresh slot
        // and the SAME key then answers the recorded plan instead of creating a second one.
        setReconcileHint(true);
        return;
      }
      clearShipmentPlanAttempt(scope);
      onClose();
      onCompleted?.();
    } catch (thrown) {
      const fa = mapAdminErrorMessage(
        thrown instanceof Error && thrown.message ? thrown.message : "order.operation.failed",
        "fa",
      );
      markShipmentPlanAttemptFailed(scope, written.attempt, fa);
      setSendError(fa);
      setReconcileHint(true);
    } finally {
      inFlightRef.current = false;
      setPending(false);
    }
  }

  const title = parcelOrdinal
    ? `زمان‌بندی ارسال · مرسولهٔ ${parcelOrdinal.toLocaleString("fa-IR")}`
    : "زمان‌بندی ارسال";

  const receivingZoneLabel = operationalTimeZoneLabel(
    selected?.receivingTimeZone ?? confirmed?.receivingTimeZone ?? null,
  );
  const handoffZoneLabel = operationalTimeZoneLabel(
    selected?.handoffTimeZone ?? confirmed?.handoffTimeZone ?? null,
  );

  return (
    <Dialog title={title} open={open} onClose={onClose} showCloseButton={false} size="lg">
      <div className="flex max-h-[min(82vh,40rem)] flex-col text-sm" data-testid="admin-shipment-plan-modal">
        <div className="min-h-0 flex-1 space-y-3 overflow-y-auto pe-1">
          {preview ? (
            <div className="space-y-0.5 text-xs text-gray-700" data-testid="admin-shipment-plan-context">
              <p className="text-sm font-bold text-gray-900">
                {attemptKindLabel(preview.attemptKind)}
                {preview.methodLabel ? (
                  <span className="ms-2 font-normal text-gray-600">· {preview.methodLabel}</span>
                ) : null}
              </p>
              <p>
                {preview.readinessSource === "return_receipt"
                  ? "آماده از رسید دریافت سالم در مبدأ"
                  : preview.readinessSource === "replacement_hold"
                    ? "آماده از رزرو موجودی سالم همین جایگزینی"
                    : "منبع آماده‌بودن ثبت نشده است"}
                {preview.readinessAtUtc ? (
                  <>
                    {" · "}
                    {operationalInstantLabel(preview.readinessAtUtc, selected?.receivingTimeZone ?? null) ?? "—"}
                  </>
                ) : null}
              </p>
            </div>
          ) : null}

          {loading ? (
            <p className="text-xs text-gray-500" role="status">در حال خواندن گزینه‌های زمان‌بندی…</p>
          ) : null}

          {loadError ? (
            <div className="space-y-2 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-xs text-red-700" role="alert">
              <p data-testid="admin-shipment-plan-load-error">{loadError}</p>
              <Button type="button" tone="secondary" onClick={() => void load()} disabled={loading}>
                تلاش دوباره
              </Button>
            </div>
          ) : null}

          {preview?.blockerCode ? (
            <p
              className="rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-900"
              role="status"
              data-testid="admin-shipment-plan-blocker"
            >
              {preview.blockerFa
                ?? mapAdminErrorMessage(preview.blockerCode, "fa")}
            </p>
          ) : null}

          {confirmed ? (
            <div
              className="space-y-1 rounded-lg border border-emerald-200 bg-emerald-50 px-3 py-2 text-xs text-emerald-900"
              data-testid="admin-shipment-plan-confirmed"
            >
              <p className="font-bold">این مرسوله برنامهٔ تأییدشدهٔ خودش را دارد.</p>
              <p>
                تحویل مشتری: {operationalDayLabel(confirmed.receivingDate) ?? "—"}
                {" · "}
                <bdi dir="ltr">{operationalTimeWindowLabel(`${confirmed.receivingOpensAt}-${confirmed.receivingClosesAt}`)}</bdi>
                {operationalTimeZoneLabel(confirmed.receivingTimeZone) ? (
                  <> {" · "} {operationalTimeZoneLabel(confirmed.receivingTimeZone)}</>
                ) : null}
              </p>
              <p>
                تحویل به حامل: {operationalDayLabel(confirmed.handoffDate) ?? "—"}
                {" · "}
                <bdi dir="ltr">{operationalTimeWindowLabel(`${confirmed.handoffOpensAt}-${confirmed.handoffClosesAt}`)}</bdi>
                {operationalTimeZoneLabel(confirmed.handoffTimeZone) ? (
                  <> {" · "} {operationalTimeZoneLabel(confirmed.handoffTimeZone)}</>
                ) : null}
              </p>
              <p>
                آخرین مهلت شروع آماده‌سازی:{" "}
                {operationalInstantLabel(confirmed.preparationStartUtc, confirmed.receivingTimeZone) ?? "—"}
              </p>
              <p>
                آخرین مهلت تحویل به حامل:{" "}
                {operationalInstantLabel(confirmed.handoffDeadlineUtc, confirmed.handoffTimeZone) ?? "—"}
              </p>
              <p className="text-[11px] text-emerald-800">
                برنامهٔ ثبت‌شده تغییر نمی‌کند؛ برای تلاش تازه، مسیر منطقی خودش را دارد.
              </p>
            </div>
          ) : null}

          {!loading && !loadError && !preview?.blockerCode && !confirmed && options.length === 0 ? (
            <p className="text-xs text-gray-500" role="status" data-testid="admin-shipment-plan-empty">
              با ورودی‌های زمان‌بندی همین روش، تاریخ قابل ارائه‌ای پیدا نشد.
            </p>
          ) : null}

          {!loading && !loadError && !preview?.blockerCode && !confirmed && dates.length > 0 ? (
            <div className="space-y-2">
              <p className="text-xs font-bold text-gray-700">تاریخ تحویل مشتری</p>
              <div className="flex flex-wrap gap-1.5" dir="rtl">
                {dates.map((date) => {
                  const active = date === activeDate;
                  return (
                    <button
                      key={date}
                      type="button"
                      onClick={() => {
                        setActiveDate(date);
                        setSelectedKey(null);
                      }}
                      className={
                        active
                          ? "min-h-11 rounded-lg border border-primary bg-primary/5 px-3 text-xs font-bold text-primary"
                          : "min-h-11 rounded-lg border border-gray-200 px-3 text-xs font-bold text-gray-700 hover:bg-gray-50"
                      }
                      aria-pressed={active}
                      data-testid={`admin-shipment-plan-date-${date}`}
                    >
                      {operationalDayLabel(date) ?? date}
                    </button>
                  );
                })}
              </div>

              <p className="text-xs font-bold text-gray-700">بازهٔ تحویل</p>
              <div className="grid gap-1.5 sm:grid-cols-2">
                {slotsForDate.map((option) => {
                  const key = slotKey(option);
                  const active = key === selectedKey;
                  return (
                    <button
                      key={key}
                      type="button"
                      onClick={() => setSelectedKey(key)}
                      className={
                        active
                          ? "min-h-11 rounded-lg border border-primary bg-primary/5 px-3 py-1.5 text-start text-xs text-gray-900"
                          : "min-h-11 rounded-lg border border-gray-200 px-3 py-1.5 text-start text-xs text-gray-700 hover:bg-gray-50"
                      }
                      aria-pressed={active}
                      data-testid={`admin-shipment-plan-slot-${option.receivingDate}-${option.receivingOpensAt}`}
                    >
                      <bdi dir="ltr" className="font-mono font-bold">
                        {operationalTimeWindowLabel(`${option.receivingOpensAt}-${option.receivingClosesAt}`)}
                      </bdi>
                    </button>
                  );
                })}
              </div>

              {selected ? (
                <div
                  className="space-y-0.5 rounded-lg border border-gray-200 bg-gray-50 px-3 py-2 text-xs text-gray-700"
                  data-testid="admin-shipment-plan-deadlines"
                >
                  <p>
                    <span className="font-bold">آخرین مهلت شروع آماده‌سازی:</span>
                    {" "}
                    {operationalInstantLabel(selected.preparationStartUtc, selected.receivingTimeZone) ?? "—"}
                  </p>
                  <p>
                    <span className="font-bold">آخرین مهلت خروج به سمت حامل:</span>
                    {" "}
                    {operationalInstantLabel(selected.transferDepartureUtc, selected.receivingTimeZone) ?? "—"}
                  </p>
                  <p>
                    <span className="font-bold">تحویل به حامل تا:</span>
                    {" "}
                    {operationalInstantLabel(selected.handoffDeadlineUtc, selected.handoffTimeZone) ?? "—"}
                    {handoffZoneLabel ? <> {" · "} {handoffZoneLabel}</> : null}
                  </p>
                  {receivingZoneLabel ? (
                    <p className="text-[11px] text-gray-500">مهلت‌های آماده‌سازی {receivingZoneLabel} است.</p>
                  ) : null}
                </div>
              ) : null}
            </div>
          ) : null}

          {sendError ? (
            <p
              className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-xs text-red-700"
              role="alert"
              data-testid="admin-shipment-plan-error"
            >
              {sendError}
            </p>
          ) : null}

          {reconcileHint ? (
            <p className="text-[11px] text-gray-600" role="status" data-testid="admin-shipment-plan-reconcile">
              همان مرور قبلی حفظ شده است؛ اگر پاسخ نرسید، دوباره تأیید کنید تا همان برنامه خوانده شود و برنامهٔ
              دومی ساخته نشود.
            </p>
          ) : null}
        </div>

        <div className="mt-3 flex items-center justify-end gap-2 border-t border-gray-100 pt-3">
          <Button type="button" tone="secondary" onClick={onClose} disabled={pending}>
            بستن
          </Button>
          <Button
            type="button"
            onClick={() => void confirm()}
            disabled={!canConfirm}
            data-testid="admin-shipment-plan-confirm"
          >
            {pending ? "در حال تأیید…" : "تأیید زمان‌بندی"}
          </Button>
        </div>
      </div>
    </Dialog>
  );
}
