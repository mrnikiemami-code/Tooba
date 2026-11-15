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
  endShipmentPlanReview,
  markShipmentPlanAttemptFailed,
  mintShipmentPlanOccurrence,
  readShipmentPlanAttempt,
  writeShipmentPlanAttempt,
  type ShipmentPlanAttempt,
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

/** TB-270-R3: reads back an exactly retained reviewed body; a malformed record is never guessed at. */
function safeParseBody(raw: string): AdminOrderOperationRequest | null {
  try {
    const parsed = JSON.parse(raw) as Partial<AdminOrderOperationRequest> | null;
    if (!parsed || typeof parsed !== "object") return null;
    if (parsed.code !== "plan_shipment_dispatch") return null;
    if (typeof parsed.shipmentId !== "string" || typeof parsed.idempotencyKey !== "string") return null;
    return {
      code: "plan_shipment_dispatch",
      shipmentId: parsed.shipmentId,
      idempotencyKey: parsed.idempotencyKey,
      dispatchPlanReceivingDate: parsed.dispatchPlanReceivingDate ?? null,
      dispatchPlanReceivingOpensAt: parsed.dispatchPlanReceivingOpensAt ?? null,
      dispatchPlanReceivingClosesAt: parsed.dispatchPlanReceivingClosesAt ?? null,
      dispatchPlanReviewFingerprint: parsed.dispatchPlanReviewFingerprint ?? null,
    };
  } catch {
    return null;
  }
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
  /** TB-270-R3: the retained reviewed envelope (exact body + key) that must be resolved before any new review. */
  const [retained, setRetained] = useState<ShipmentPlanAttempt | null>(null);
  /** TB-270-R3: the exact reviewed body was already sent with an UNKNOWN outcome; only that body may go again. */
  const [frozen, setFrozen] = useState(false);
  /** TB-270-R3: the exact prior outcome, kept when a deterministic refusal ends that review. */
  const [priorOutcomeFa, setPriorOutcomeFa] = useState<string | null>(null);
  const inFlightRef = useRef(false);
  const epochRef = useRef(0);
  const previewLoaderRef = useRef(previewLoader);
  previewLoaderRef.current = previewLoader;
  const operationExecutorRef = useRef(operationExecutor);
  operationExecutorRef.current = operationExecutor;

  const scope = useMemo(
    () => ({ viewerScopeKey: viewerScopeKey ?? null, checkoutId, shipmentId }),
    [viewerScopeKey, checkoutId, shipmentId],
  );

  const load = useCallback(async (epoch: number) => {
    setLoading(true);
    setLoadError(null);
    try {
      const result = await previewLoaderRef.current(checkoutId, shipmentId);
      if (epoch !== epochRef.current) return;
      if (result.state !== "ok" || !result.data) {
        setPreview(null);
        setLoadError(mapAdminErrorMessage(result.message ?? "order.operation.failed", "fa"));
        return;
      }
      setPreview(result.data);
      // TB-270-R3: ONLY a genuinely unknown outcome may re-send a retained body. A record without an uncertain
      // outcome is history, not permission, so the operator deliberately chooses a fresh reviewed slot.
      const rec = readShipmentPlanAttempt(scope);
      const isFrozen = Boolean(rec?.uncertain && rec?.requestBody);
      setRetained(rec);
      setFrozen(isFrozen);
      const retainedKey = isFrozen && rec?.receivingDate
        ? `${rec.receivingDate}|${rec.receivingOpensAt ?? ""}|${rec.receivingClosesAt ?? ""}`
        : null;
      const stillOffered = retainedKey
        && result.data.options.some((option) => slotKey(option) === retainedKey);
      setSelectedKey(stillOffered ? retainedKey : null);
    } catch {
      if (epoch !== epochRef.current) return;
      setPreview(null);
      setLoadError(mapAdminErrorMessage("host-unreachable", "fa"));
    } finally {
      if (epoch === epochRef.current) setLoading(false);
    }
  }, [checkoutId, shipmentId, scope]);

  useEffect(() => {
    if (!open) return;
    // A new mount/parcel is a new epoch: a late answer for the previous one must never write this screen.
    const epoch = epochRef.current + 1;
    epochRef.current = epoch;
    setSendError(null);
    setReconcileHint(false);
    setPriorOutcomeFa(null);
    setPreview(null);
    setSelectedKey(null);
    setRetained(null);
    setFrozen(false);
    // TB-270-R3: the single-flight guard is NEVER released here. Closing/reopening or retargeting this surface while
    // an earlier POST is still in flight must not make a second, different intent possible; the pending flag mirrors
    // that live request instead of pretending the surface is idle.
    setPending(inFlightRef.current);
    void load(epoch);
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
  const canConfirm = !confirmed && !pending && (frozen ? Boolean(retained?.requestBody) : Boolean(selected));

  /**
   * TB-270-R3: one send path. `raw` is an exact retained body (byte-for-byte intent) and is never rebuilt from
   * current UI state; a fresh reviewed body is persisted together with its exact JSON BEFORE the POST.
   */
  async function send(body: AdminOrderOperationRequest, attempt: ShipmentPlanAttempt) {
    if (pending || inFlightRef.current) return;
    inFlightRef.current = true;
    setPending(true);
    setSendError(null);
    setReconcileHint(false);
    const epoch = epochRef.current;

    try {
      const result = await operationExecutorRef.current(checkoutId, body);
      if (epoch !== epochRef.current) return;
      if (result.state === "ok") {
        endShipmentPlanReview(scope);
        setRetained(null);
        setFrozen(false);
        onClose();
        onCompleted?.();
        return;
      }

      const fa = mapAdminErrorMessage(result.message ?? "order.operation.failed", "fa");
      const unknown = result.status <= 0 || result.status >= 500;
      if (unknown) {
        // ONLY a genuine transport loss / 5xx is uncertain: the exact reviewed body stays frozen and is retried as-is.
        const kept = markShipmentPlanAttemptFailed(scope, attempt, fa, true);
        setRetained(kept);
        setFrozen(true);
        setSendError(fa);
        setReconcileHint(true);
        return;
      }

      // A deterministic refusal ENDS this review: the exact prior outcome is preserved for the operator, and the
      // stale body/key are dropped so a different selection can never ride on an already-refused occurrence.
      endShipmentPlanReview(scope);
      setRetained(null);
      setFrozen(false);
      setSelectedKey(null);
      setPriorOutcomeFa(fa);
      setSendError(fa);
      setReconcileHint(false);
    } catch (thrown) {
      if (epoch !== epochRef.current) return;
      const fa = mapAdminErrorMessage(
        thrown instanceof Error && thrown.message ? thrown.message : "order.operation.failed",
        "fa",
      );
      const kept = markShipmentPlanAttemptFailed(scope, attempt, fa, true);
      setRetained(kept);
      setFrozen(true);
      setSendError(fa);
      setReconcileHint(true);
    } finally {
      // TB-270-R3: the guard is released when the REQUEST settles, never when the surface is reused; only the
      // presentation of that outcome is fenced by the epoch.
      inFlightRef.current = false;
      setPending(false);
    }
  }

  async function confirm() {
    if (confirmed || pending) return;

    // 1) An unresolved reviewed body is re-sent exactly; nothing is rebuilt from current state.
    if (frozen && retained?.requestBody) {
      const parsed = safeParseBody(retained.requestBody);
      if (!parsed || parsed.idempotencyKey !== retained.occurrenceKey) {
        setSendError("درخواست بازبینی‌شدهٔ ذخیره‌شده خوانده نشد؛ بازبینی تازه لازم است.");
        return;
      }
      await send(parsed, retained);
      return;
    }

    if (!selected) return;

    // 2) A fresh review: retain the key + the COMPLETE reviewed body before the first POST. A deterministically
    // refused occurrence was already ended, so a fresh review always carries a NEW key instead of reusing one.
    const occurrenceKey = mintShipmentPlanOccurrence();
    const body: AdminOrderOperationRequest = {
      code: "plan_shipment_dispatch",
      shipmentId,
      idempotencyKey: occurrenceKey,
      dispatchPlanReceivingDate: selected.receivingDate,
      dispatchPlanReceivingOpensAt: selected.receivingOpensAt,
      dispatchPlanReceivingClosesAt: selected.receivingClosesAt,
      dispatchPlanReviewFingerprint: preview?.reviewFingerprint ?? null,
    };
    const written = writeShipmentPlanAttempt({
      scope,
      occurrenceKey,
      receivingDate: selected.receivingDate,
      receivingOpensAt: selected.receivingOpensAt,
      receivingClosesAt: selected.receivingClosesAt,
      receivingTimeZone: selected.receivingTimeZone ?? null,
      attempts: (retained?.attempts ?? 0) + 1,
      requestBody: JSON.stringify(body),
      reviewFingerprint: preview?.reviewFingerprint ?? null,
      uncertain: false,
    });
    if (!written?.durable) {
      // No durable envelope means a reload could lose the key and mint a second review; refuse instead.
      setReconcileHint(true);
      setSendError("ذخیرهٔ محلی این مرور ممکن نیست؛ برای جلوگیری از ثبت دوباره، تا تازه‌سازی صفحه اقدام نکنید.");
      return;
    }
    setRetained(written.attempt);
    await send(body, written.attempt);
  }

  /** TB-270-R3: re-reads the owning preview under a new epoch (never mixes a late answer into this screen). */
  function reload(clearPriorOutcome = false) {
    if (clearPriorOutcome) setPriorOutcomeFa(null);
    const epoch = epochRef.current + 1;
    epochRef.current = epoch;
    void load(epoch);
  }

  /** TB-270-R3: ends a deterministically refused review so a deliberate NEW review can start from a fresh preview. */
  function startFreshReview() {
    endShipmentPlanReview(scope);
    setRetained(null);
    setFrozen(false);
    setSelectedKey(null);
    setSendError(null);
    setReconcileHint(false);
    reload();
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
              <Button type="button" tone="secondary" onClick={() => reload()} disabled={loading}>
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
              {frozen ? (
                <p
                  className="rounded-lg border border-sky-200 bg-sky-50 px-3 py-2 text-xs text-sky-900"
                  role="status"
                  data-testid="admin-shipment-plan-frozen"
                >
                  نتیجهٔ تأیید قبلی مشخص نشد. همان درخواست بازبینی‌شده دقیقاً حفظ شده است؛ تا روشن‌شدن نتیجه
                  فقط می‌توانید همان درخواست را دوباره بفرستید. بازهٔ دیگری در این وضعیت ثبت نمی‌شود.
                </p>
              ) : null}
              <p className="text-xs font-bold text-gray-700">تاریخ تحویل مشتری</p>
              <div className="flex flex-wrap gap-1.5" dir="rtl">
                {dates.map((date) => {
                  const active = date === activeDate;
                  return (
                    <button
                      key={date}
                      type="button"
                      onClick={() => {
                        if (frozen) return;
                        setActiveDate(date);
                        setSelectedKey(null);
                      }}
                      disabled={frozen}
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
                      onClick={() => {
                        if (frozen) return;
                        setSelectedKey(key);
                      }}
                      disabled={frozen}
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

          {priorOutcomeFa && !frozen ? (
            <div
              className="space-y-2 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-900"
              role="status"
              data-testid="admin-shipment-plan-prior-outcome"
            >
              <p>{priorOutcomeFa}</p>
              <p>
                آن بازبینی با همین نتیجه بسته شد و کلید/بازهٔ قبلی برای انتخاب دیگری بازاستفاده نمی‌شود. بازبینی تازه
                از داده‌های فعلی گرفته می‌شود.
              </p>
              <Button type="button" tone="secondary" onClick={startFreshReview} disabled={loading}>
                بازبینی تازه
              </Button>
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
              {frozen
                ? "همان درخواست بازبینی‌شده حفظ شده است؛ با ارسال دوبارهٔ همان درخواست، اگر برنامه ثبت شده باشد همان خوانده می‌شود و برنامهٔ دومی ساخته نمی‌شود."
                : "همان مرور قبلی حفظ شده است؛ اگر پاسخ نرسید، دوباره تأیید کنید تا همان برنامه خوانده شود و برنامهٔ دومی ساخته نشود."}
            </p>
          ) : null}
        </div>

        <div className="mt-3 flex items-center justify-end gap-2 border-t border-gray-100 pt-3">
          <Button type="button" tone="secondary" onClick={onClose} disabled={pending}>
            بستن
          </Button>
          {frozen ? (
            <Button
              type="button"
              onClick={() => void confirm()}
              disabled={!canConfirm}
              data-testid="admin-shipment-plan-retry-same"
            >
              {pending ? "در حال ارسال…" : "تلاش دوباره با همین درخواست"}
            </Button>
          ) : (
            <Button
              type="button"
              onClick={() => void confirm()}
              disabled={!canConfirm}
              data-testid="admin-shipment-plan-confirm"
            >
              {pending ? "در حال تأیید…" : "تأیید زمان‌بندی"}
            </Button>
          )}
        </div>
      </div>
    </Dialog>
  );
}
