"use client";

import { useEffect, useMemo, useState } from "react";
import { Button } from "../../../design-system/primitives/core";
import { Dialog } from "../../../design-system/primitives/overlays";
import { formatJalaliDate } from "../../../design-system/app-data-grid/jalali";

/**
 * TB-254/TB-254-R1 — the ONE purposeful Marketplace operational-group workflow surface.
 *
 * The normal order page only displays parcels and their permitted next action. Creating the
 * constituent parcels + the common package — and explicitly reusing an existing parcel — happens
 * here, inside a clearly titled dialog, so a normal Store/Seller card never shows an unexplained
 * checkbox or a quantity field.
 *
 * Ownership (FRONTEND-ARCHITECTURE-STANDARD): this module is the feature-owned use-case surface for
 * the order operational group workflow. It deliberately declares its own narrow input contract
 * instead of importing an app/page client: the page passes the facts it already holds and the
 * display formatters it already uses. That keeps the feature independent of `app/admin/*` internals
 * and free of a duplicated status/money dictionary.
 *
 * TB-253-R4/R5/R6 behaviour is unchanged: the reviewed envelope (members + exact quantities +
 * explicitly chosen reuse parcels) is what the caller prebinds to one occurrence. While an attempt
 * is unresolved the dialog is read-only and can only replay that recorded envelope; closing the
 * dialog never cancels the server operation and never erases the occurrence.
 */

/** Display facts of the operational group this workflow belongs to. */
export type OperationalGroupWorkflowGroup = {
  operationalGroupKey: string;
  methodLabel: string;
  methodCode: string;
  deliveryDate: string | null;
  deliveryTimeWindow: string | null;
  combinedFeeTotal: number;
  currency: string;
};

export type OperationalGroupDialogLine = {
  orderLineId: string;
  title: string;
  /** Packed quantity still available for a new parcel in this member group. */
  remaining: number;
};

/** Existing parcel of a member group that MAY be reused in the common package. */
export type OperationalGroupDialogParcel = {
  shipmentId: string;
  itemCount: number;
  status: string;
  trackingReference?: string | null;
};

export type OperationalGroupDialogRow = {
  memberShippingGroupId: string;
  sellerDisplayName: string;
  lines: OperationalGroupDialogLine[];
  parcels: OperationalGroupDialogParcel[];
};

export type OperationalGroupDialogRequest = {
  selections: Array<{ orderLineId: string; quantity: number }>;
  reuseShipmentIds: string[];
};

/**
 * The recorded, unresolved envelope. Structural subset of the page-owned attempt record: the dialog
 * reads the recorded request only and never re-derives it from the current rows.
 */
export type RetainedOperationalGroupEnvelope = {
  occurrenceId: string;
  selections: ReadonlyArray<{ orderLineId: string; quantity: number }>;
  reuseShipmentIds: ReadonlyArray<string>;
};

/** Display formatters owned by the page boundary (no duplicated status/money dictionary here). */
export type OperationalGroupWorkflowFormatters = {
  formatMoney: (amount: number, currency: string) => string;
  formatStatus: (status: string) => string;
};

type Props = OperationalGroupWorkflowFormatters & {
  open: boolean;
  group: OperationalGroupWorkflowGroup;
  rows: OperationalGroupDialogRow[];
  pending: boolean;
  errorFa: string | null;
  /**
   * The recorded envelope of an unresolved operation. When present the dialog is READ-ONLY and its
   * only action replays exactly that request (no new occurrence, no edited quantities).
   */
  retainedAttempt: RetainedOperationalGroupEnvelope | null;
  /**
   * TB-254-R1: titles of EVERY line of the group (including lines whose packed quantity is already
   * consumed). A retained summary must stay readable after partial server progress, so recorded
   * selections are never hidden behind a zero-remaining row.
   */
  lineTitles?: Record<string, string>;
  onClose: () => void;
  onSubmit: (request: OperationalGroupDialogRequest) => void;
};

function clampQuantity(value: number, max: number): number {
  if (!Number.isFinite(value) || value <= 0) return 0;
  return Math.min(value, max);
}

function quantityText(value: number): string {
  return value.toLocaleString("fa-IR", { maximumFractionDigits: 3 });
}

function jalaliDay(isoDate: string | null | undefined): string | null {
  const raw = (isoDate ?? "").trim();
  if (!raw) return null;
  return formatJalaliDate(raw.includes("T") ? raw : `${raw}T12:00:00`, "fa");
}

function parcelLabel(parcel: OperationalGroupDialogParcel, index: number): string {
  const count = parcel.itemCount > 0 ? `${parcel.itemCount.toLocaleString("fa-IR")} قلم` : "بدون قلم";
  return `مرسولهٔ ${(index + 1).toLocaleString("fa-IR")} · ${count}`;
}

export function AdminOperationalGroupWorkflowDialog({
  open,
  group,
  rows,
  pending,
  errorFa,
  retainedAttempt,
  lineTitles,
  formatMoney,
  formatStatus,
  onClose,
  onSubmit,
}: Props) {
  const [qtyByLine, setQtyByLine] = useState<Record<string, number>>({});
  const [reusedParcelIds, setReusedParcelIds] = useState<string[]>([]);

  const reviewSignature = rows
    .map(
      (row) =>
        `${row.memberShippingGroupId}:${row.lines
          .map((line) => `${line.orderLineId}=${line.remaining}`)
          .join(",")}:${row.parcels.map((parcel) => parcel.shipmentId).join(",")}`,
    )
    .join("|");
  const retainedId = retainedAttempt?.occurrenceId ?? null;

  useEffect(() => {
    if (!open) return;
    if (retainedAttempt) {
      // Read-only replay of the recorded envelope: same quantities, same reused parcels.
      const nextQty: Record<string, number> = {};
      for (const selection of retainedAttempt.selections) {
        nextQty[selection.orderLineId] = selection.quantity;
      }
      setQtyByLine(nextQty);
      setReusedParcelIds([...retainedAttempt.reuseShipmentIds]);
      return;
    }
    const nextQty: Record<string, number> = {};
    for (const row of rows) {
      for (const line of row.lines) {
        // A member that already owns a parcel gets no silent duplicate allocation.
        nextQty[line.orderLineId] = row.parcels.length > 0 ? 0 : line.remaining;
      }
    }
    setQtyByLine(nextQty);
    // No arbitrary default sweeping of unseen parcels: reuse is an explicit choice.
    setReusedParcelIds([]);
    // The dependency list is intentionally the server-projection signature + retained occurrence.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, reviewSignature, retainedId, group.operationalGroupKey]);

  const selections = useMemo(() => {
    if (retainedAttempt) {
      // The recorded envelope is the operation. Partial server progress may have consumed every
      // remaining packed quantity, so re-deriving from the CURRENT rows would silently shrink the
      // request; the recorded selections are replayed verbatim.
      return retainedAttempt.selections
        .map((selection) => ({ orderLineId: selection.orderLineId, quantity: selection.quantity }))
        .sort((a, b) => a.orderLineId.localeCompare(b.orderLineId));
    }
    const collected: Array<{ orderLineId: string; quantity: number }> = [];
    for (const row of rows) {
      for (const line of row.lines) {
        const quantity = clampQuantity(qtyByLine[line.orderLineId] ?? 0, line.remaining);
        if (quantity > 0) collected.push({ orderLineId: line.orderLineId, quantity });
      }
    }
    return collected.sort((a, b) => a.orderLineId.localeCompare(b.orderLineId));
  }, [qtyByLine, rows, retainedAttempt]);

  const reuseShipmentIds = useMemo(
    () => [...(retainedAttempt ? retainedAttempt.reuseShipmentIds : reusedParcelIds)]
      .sort((a, b) => a.localeCompare(b)),
    [reusedParcelIds, retainedAttempt],
  );

  const rowLineTitles = useMemo(() => {
    const map = new Map<string, string>();
    for (const row of rows) {
      for (const line of row.lines) map.set(line.orderLineId, line.title);
    }
    return map;
  }, [rows]);

  const titleOf = (orderLineId: string): string =>
    lineTitles?.[orderLineId] ?? rowLineTitles.get(orderLineId) ?? "کالای سفارش";

  const parcelCount = rows.reduce((sum, row) => sum + row.parcels.length, 0);
  const remainingLines = rows.flatMap((row) => row.lines).filter((line) => line.remaining > 0);
  const consolidatingOnly = remainingLines.length === 0 && parcelCount > 0;
  const title = retainedAttempt
    ? "پیگیری عملیات ثبت‌شده"
    : consolidatingOnly
      ? "تجمیع مرسوله‌ها"
      : "آماده‌سازی ارسال گروه";

  const nothingSelected = selections.length === 0 && reuseShipmentIds.length === 0;
  const canSubmit = !pending && !nothingSelected;
  const day = jalaliDay(group.deliveryDate);
  const timeWindow = (group.deliveryTimeWindow ?? "").trim();

  return (
    <Dialog open={open} onClose={onClose} showCloseButton={false} size="lg" title={title}>
      <div
        className="flex max-h-[min(82vh,44rem)] flex-col text-sm"
        data-testid={`admin-order-operational-dialog-${group.operationalGroupKey}`}
      >
        <div className="min-h-0 flex-1 space-y-3 overflow-y-auto pe-1">
          <p className="text-xs leading-6 text-gray-600">
            {retainedAttempt
              ? "این درخواست ثبت شده و نتیجهٔ قطعی آن دریافت نشده است. فقط همین درخواست قابل پیگیری است و درخواست تازه‌ای ساخته نمی‌شود."
              : consolidatingOnly
                ? "مرسوله‌های ساخته‌شدهٔ این گروه را انتخاب کنید تا در یک بستهٔ مشترک ارسال شوند."
                : "کالاهای باقی‌مانده و مرسوله‌های موجود این گروه را بررسی کنید؛ مرسوله‌ها و بستهٔ مشترک یک‌جا ساخته می‌شوند."}
          </p>

          <div
            className="rounded-lg border border-gray-200 bg-gray-50 px-3 py-2 text-xs text-gray-700"
            data-testid={`admin-order-operational-dialog-summary-${group.operationalGroupKey}`}
          >
            <p className="break-words text-sm font-bold text-gray-900">{group.methodLabel || group.methodCode}</p>
            <p className="mt-1 flex flex-wrap items-center gap-x-2 gap-y-1">
              <span>{day ?? "تاریخ تحویل نامشخص"}</span>
              {timeWindow ? (
                <>
                  <span aria-hidden>·</span>
                  <bdi dir="ltr">{timeWindow}</bdi>
                </>
              ) : null}
              <span aria-hidden>·</span>
              <span dir="ltr">{formatMoney(group.combinedFeeTotal, group.currency)}</span>
            </p>
            <p className="mt-1 text-gray-500">
              روش ارسال، تاریخ و ساعت انتخاب مشتری است و تغییر نمی‌کند.
            </p>
          </div>

          {rows.map((row) => {
            const rowParcelIds = new Set(row.parcels.map((parcel) => parcel.shipmentId));
            const rowReuse = reuseShipmentIds.filter((id) => rowParcelIds.has(id));
            return (
              <section
                key={row.memberShippingGroupId}
                className="rounded-lg border border-gray-200 bg-white p-3"
                data-testid={`admin-order-operational-dialog-member-${row.memberShippingGroupId}`}
              >
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <h4 className="min-w-0 break-words text-sm font-bold text-gray-900">{row.sellerDisplayName}</h4>
                  <span className="text-xs text-gray-500">
                    {`${row.lines.length.toLocaleString("fa-IR")} کالا · ${row.parcels.length.toLocaleString("fa-IR")} مرسوله`}
                  </span>
                </div>

                {row.parcels.length > 0 ? (
                  <fieldset className="mt-2">
                    <legend className="mb-1 text-xs font-bold text-gray-700">افزودن به بستهٔ مشترک</legend>
                    <ul className="space-y-1">
                      {row.parcels.map((parcel, index) => {
                        const inputId = `opgroup-reuse-${parcel.shipmentId}`;
                        return (
                          <li key={parcel.shipmentId}>
                            <label
                              htmlFor={inputId}
                              className="flex min-h-11 cursor-pointer items-start gap-2 rounded-md border border-gray-100 px-2 py-2 text-xs text-gray-700 hover:bg-gray-50"
                            >
                              <input
                                id={inputId}
                                type="checkbox"
                                className="mt-0.5 size-4 shrink-0"
                                checked={reusedParcelIds.includes(parcel.shipmentId)}
                                disabled={pending || Boolean(retainedAttempt)}
                                onChange={(event) => {
                                  const checked = event.currentTarget.checked;
                                  setReusedParcelIds((prev) =>
                                    checked
                                      ? prev.includes(parcel.shipmentId) ? prev : [...prev, parcel.shipmentId]
                                      : prev.filter((id) => id !== parcel.shipmentId),
                                  );
                                }}
                                data-testid={`admin-order-operational-reuse-parcel-${parcel.shipmentId}`}
                              />
                              <span className="min-w-0 flex-1 break-words font-semibold leading-6">
                                {parcelLabel(parcel, index)}
                              </span>
                              <span className="shrink-0 rounded-full bg-gray-100 px-2 py-0.5 text-[11px] font-bold text-gray-600">
                                {formatStatus(parcel.status)}
                              </span>
                              {parcel.trackingReference ? (
                                <bdi dir="ltr" className="shrink-0 text-[11px] leading-6 text-gray-500">
                                  {parcel.trackingReference}
                                </bdi>
                              ) : null}
                            </label>
                          </li>
                        );
                      })}
                    </ul>
                    <p className="mt-1 text-[11px] text-gray-500">
                      {rowReuse.length === 0
                        ? "هیچ مرسوله‌ای انتخاب نشده است."
                        : `${rowReuse.length.toLocaleString("fa-IR")} مرسوله برای بستهٔ مشترک انتخاب شد.`}
                    </p>
                  </fieldset>
                ) : null}

                <div className="mt-2">
                  <p className="mb-1 text-xs font-bold text-gray-700">کالاهای قابل ارسال</p>
                  {row.lines.length === 0 ? (
                    <p className="text-xs text-gray-500">کالای بسته‌بندی‌شدهٔ باقی‌مانده‌ای برای این فروشنده نیست.</p>
                  ) : (
                    <ul className="divide-y divide-gray-100 rounded-md border border-gray-100">
                      {row.lines.map((line) => {
                        const max = line.remaining;
                        const value = clampQuantity(qtyByLine[line.orderLineId] ?? 0, max);
                        const locked = pending || Boolean(retainedAttempt) || max <= 0;
                        return (
                          <li
                            key={line.orderLineId}
                            className="grid grid-cols-[minmax(0,1fr)_auto] items-start gap-2 px-2 py-2"
                            data-testid={`admin-order-operational-line-${line.orderLineId}`}
                          >
                            <span className="min-w-0 break-words text-sm leading-6 text-gray-800">{line.title}</span>
                            <label className="inline-flex min-h-11 shrink-0 items-center gap-1 text-xs text-gray-500">
                              <span className="whitespace-nowrap">از {quantityText(max)}</span>
                              <input
                                inputMode="decimal"
                                dir="ltr"
                                min={0}
                                max={max}
                                step="any"
                                value={value}
                                disabled={locked}
                                onChange={(event) => {
                                  const parsed = Number(event.currentTarget.value.replace(",", "."));
                                  setQtyByLine((prev) => ({
                                    ...prev,
                                    [line.orderLineId]: Number.isFinite(parsed) ? clampQuantity(parsed, max) : 0,
                                  }));
                                }}
                                aria-label={`مقدار ${line.title}`}
                                className="min-h-11 w-16 rounded border border-gray-200 px-2 text-center tabular-nums disabled:bg-gray-50 disabled:opacity-70"
                                data-testid={`admin-order-operational-qty-${line.orderLineId}`}
                              />
                            </label>
                          </li>
                        );
                      })}
                    </ul>
                  )}
                </div>
              </section>
            );
          })}

          {retainedAttempt ? (
            <div
              className="rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-900"
              data-testid={`admin-order-operational-dialog-retained-${group.operationalGroupKey}`}
              role="status"
            >
              <p className="font-bold">درخواست ثبت‌شده نگه داشته شده است.</p>
              {retainedAttempt.selections.length > 0 ? (
                <ul className="mt-1 space-y-0.5">
                  {retainedAttempt.selections.map((selection) => (
                    <li
                      key={selection.orderLineId}
                      className="flex items-start justify-between gap-2 leading-6"
                      data-testid={`admin-order-operational-retained-line-${selection.orderLineId}`}
                    >
                      <span className="min-w-0 break-words">{titleOf(selection.orderLineId)}</span>
                      <span className="shrink-0 whitespace-nowrap tabular-nums">
                        {`× ${quantityText(selection.quantity)}`}
                      </span>
                    </li>
                  ))}
                </ul>
              ) : (
                <p className="mt-1 leading-6">بدون کالای جدید</p>
              )}
              {retainedAttempt.reuseShipmentIds.length > 0 ? (
                <p className="mt-1 leading-6">
                  {`${retainedAttempt.reuseShipmentIds.length.toLocaleString("fa-IR")} مرسولهٔ موجود نیز در همین درخواست است.`}
                </p>
              ) : null}
            </div>
          ) : null}

          {errorFa ? (
            <p
              className="rounded-lg border border-rose-200 bg-rose-50 px-3 py-2 text-xs font-semibold text-rose-900"
              role="alert"
              data-testid="admin-order-operational-workflow-error"
            >
              {errorFa}
            </p>
          ) : null}
        </div>

        <div className="mt-3 flex flex-wrap items-center justify-end gap-2 border-t border-gray-100 pt-3">
          <Button
            type="button"
            tone="secondary"
            onClick={onClose}
            disabled={pending}
            data-testid={`admin-order-operational-dialog-close-${group.operationalGroupKey}`}
          >
            بستن
          </Button>
          <Button
            type="button"
            disabled={!canSubmit}
            data-testid={
              retainedAttempt
                ? `admin-order-operational-reconcile-retry-${group.operationalGroupKey}`
                : `admin-order-operational-dialog-submit-${group.operationalGroupKey}`
            }
            onClick={() => onSubmit({ selections, reuseShipmentIds })}
          >
            {pending
              ? "در حال ارسال…"
              : retainedAttempt
                ? "همگام‌سازی مجدد همین عملیات"
                : consolidatingOnly
                  ? "ایجاد بستهٔ مشترک"
                  : "ایجاد مرسوله‌ها و بستهٔ مشترک"}
          </Button>
        </div>
      </div>
    </Dialog>
  );
}
