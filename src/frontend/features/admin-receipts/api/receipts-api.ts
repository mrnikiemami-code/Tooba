/**
 * Admin client for customer payment receipts list.
 */
import type { GridServerQuery } from "../../../design-system/data-grid/types.ts";
import { postAdminGridQuery, type AdminGridQueryResult } from "../../../design-system/app-data-grid/admin-grid-query-client.ts";
import {
  ADMIN_ACTOR_STORAGE_KEY,
  ADMIN_DEV_ACTOR_HEADER,
  DEFAULT_ADMIN_ACTOR_ID,
} from "../../../lib/admin/admin-result.ts";
import {
  emptyReservationSummary,
  type AdminReservationCycleSummary,
} from "../../../app/admin/admin-reservation-cycle.ts";

function actorId(): string {
  if (typeof window === "undefined") return DEFAULT_ADMIN_ACTOR_ID;
  return window.localStorage.getItem(ADMIN_ACTOR_STORAGE_KEY) ?? DEFAULT_ADMIN_ACTOR_ID;
}

function adminHeaders(extra?: Record<string, string>): Record<string, string> {
  return { Accept: "application/json", [ADMIN_DEV_ACTOR_HEADER]: actorId(), ...(extra ?? {}) };
}

function record(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : null;
}

function prop(item: Record<string, unknown>, camel: string, pascal: string): unknown {
  return item[camel] ?? item[pascal];
}

function text(value: unknown, fallback = ""): string {
  return value == null ? fallback : String(value);
}

function number(value: unknown): number {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
}

function mapReservationSummaryFields(item: Record<string, unknown>): AdminReservationCycleSummary {
  const fallback = emptyReservationSummary();
  const cycleRaw = prop(item, "reservationCycleNumber", "ReservationCycleNumber");
  const cycleNumber = cycleRaw === null || cycleRaw === undefined || cycleRaw === ""
    ? null
    : number(cycleRaw);
  return {
    reservationLabel: text(prop(item, "reservationLabel", "ReservationLabel"), fallback.reservationLabel),
    reservationLabelEn: text(prop(item, "reservationLabelEn", "ReservationLabelEn"), fallback.reservationLabelEn),
    reservationState: text(prop(item, "reservationState", "ReservationState"), fallback.reservationState),
    reservationCycleNumber: cycleNumber === 0 && (cycleRaw === null || cycleRaw === undefined) ? null : cycleNumber || null,
    reservationRetryPossible: Boolean(prop(item, "reservationRetryPossible", "ReservationRetryPossible")),
    reservationNeedsReacquire: Boolean(prop(item, "reservationNeedsReacquire", "ReservationNeedsReacquire")),
    reservationRetryLimitReached: Boolean(prop(item, "reservationRetryLimitReached", "ReservationRetryLimitReached")),
  };
}

/** ردیف دریافت/پرداخت مشتری برای Admin. */
export interface AdminReceiptRow {
  id: string;
  paymentId: string;
  checkoutId: string;
  orderReference: string;
  customerDisplayName: string;
  amount: number;
  currency: string;
  status: string;
  providerCode: string;
  createdAt: string;
  completedAt: string | null;
  supplyStatus: string;
  reservationLabel: string;
  reservationLabelEn: string;
  reservationState: string;
  reservationCycleNumber: number | null;
  reservationRetryPossible: boolean;
  reservationNeedsReacquire: boolean;
  reservationRetryLimitReached: boolean;
}

/** نگاشت ردیف دریافت از Host. */
export function mapAdminReceipt(value: unknown): AdminReceiptRow | null {
  const item = record(value);
  if (!item) return null;
  const paymentId = text(prop(item, "paymentId", "PaymentId"));
  const checkoutId = text(prop(item, "checkoutId", "CheckoutId"));
  if (!paymentId || !checkoutId) return null;
  return {
    id: paymentId,
    paymentId,
    checkoutId,
    orderReference: text(prop(item, "orderReference", "OrderReference"), checkoutId.slice(0, 12)),
    customerDisplayName: text(prop(item, "customerDisplayName", "CustomerDisplayName"), "مشتری"),
    amount: number(prop(item, "amount", "Amount")),
    currency: text(prop(item, "currency", "Currency"), "IRR"),
    status: text(prop(item, "status", "Status")),
    providerCode: text(prop(item, "providerCode", "ProviderCode")),
    createdAt: text(prop(item, "createdAt", "CreatedAt")),
    completedAt: text(prop(item, "completedAt", "CompletedAt")) || null,
    supplyStatus: text(prop(item, "supplyStatus", "SupplyStatus"), "NotApplicable"),
    ...mapReservationSummaryFields(item),
  };
}

/** Server GridQuery — دریافت‌های Admin. */
export function queryAdminReceiptsGrid(query: GridServerQuery): Promise<AdminGridQueryResult<AdminReceiptRow>> {
  return postAdminGridQuery("/v1/admin/payments/query", query, adminHeaders(), (item) => mapAdminReceipt(item));
}
