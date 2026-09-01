/**
 * کلاینت تسویه marketplace — دادهٔ واقعی Host؛ بدون موجودی/پرداخت ساختگی.
 */

import { ADMIN_DEV_ACTOR_HEADER, type AdminResult } from "../admin/admin-api.ts";
import type { GridServerQuery } from "../../design-system/data-grid/types.ts";
import { postAdminGridQuery, type AdminGridQueryResult } from "../../design-system/app-data-grid/admin-grid-query-client.ts";
import {
  DEV_ACTOR_HEADER,
  SELLER_PARTY_HEADER,
  readActorUserId,
  readSellerPartyId,
} from "../vendor-panel/seller-api.ts";

export interface SettlementBalance {
  settlementAccountId: string;
  sellerPartyId: string;
  sellerDisplayName: string;
  currency: string;
  postedCredits: number;
  postedDebits: number;
  reservedPayouts: number;
  availableBalance: number;
}

export interface SettlementEntryRow {
  entryId: string;
  entryType: string;
  grossAmount: number;
  commissionAmount: number;
  netAmount: number;
  currency: string;
  sourceType: string;
  sellerOrderId: string | null;
  postedAt: string;
}

export interface SettlementStatementRow {
  statementId: string;
  status: string;
  periodStart: string;
  periodEnd: string;
  openingBalance: number;
  closingBalance: number;
  currency: string;
}

export interface PayoutRequestRow {
  payoutRequestId: string;
  sellerPartyId: string;
  sellerDisplayName: string;
  amount: number;
  currency: string;
  status: string;
  idempotencyKey: string;
  createdAt: string;
  updatedAt: string;
}

function recordOf(value: unknown): Record<string, unknown> | null {
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

function sellerHeaders(sellerPartyId: string, json = false): Record<string, string> {
  const headers: Record<string, string> = {
    Accept: "application/json",
    [SELLER_PARTY_HEADER]: sellerPartyId,
  };
  if (json) headers["Content-Type"] = "application/json";
  const actor = readActorUserId();
  if (actor) headers[DEV_ACTOR_HEADER] = actor;
  return headers;
}

function adminHeaders(json = false): Record<string, string> {
  const headers: Record<string, string> = { Accept: "application/json" };
  if (json) headers["Content-Type"] = "application/json";
  const actor = typeof window !== "undefined" ? localStorage.getItem("tooba.adminActorUserId") : null;
  if (actor) headers[ADMIN_DEV_ACTOR_HEADER] = actor;
  return headers;
}

export function mapSettlementBalance(value: unknown): SettlementBalance | null {
  const item = recordOf(value);
  if (!item) return null;
  const id = text(prop(item, "settlementAccountId", "SettlementAccountId"));
  if (!id) return null;
  return {
    settlementAccountId: id,
    sellerPartyId: text(prop(item, "sellerPartyId", "SellerPartyId")),
    sellerDisplayName: text(prop(item, "sellerDisplayName", "SellerDisplayName"), text(prop(item, "displayName", "DisplayName"), "فروشنده")),
    currency: text(prop(item, "currency", "Currency"), "IRR"),
    postedCredits: number(prop(item, "postedCredits", "PostedCredits")),
    postedDebits: number(prop(item, "postedDebits", "PostedDebits")),
    reservedPayouts: number(prop(item, "reservedPayouts", "ReservedPayouts")),
    availableBalance: number(prop(item, "availableBalance", "AvailableBalance")),
  };
}

export function mapSettlementEntry(value: unknown): SettlementEntryRow | null {
  const item = recordOf(value);
  if (!item) return null;
  const id = text(prop(item, "entryId", "EntryId"));
  if (!id) return null;
  const orderRaw = prop(item, "sellerOrderId", "SellerOrderId");
  return {
    entryId: id,
    entryType: text(prop(item, "entryType", "EntryType")),
    grossAmount: number(prop(item, "grossAmount", "GrossAmount")),
    commissionAmount: number(prop(item, "commissionAmount", "CommissionAmount")),
    netAmount: number(prop(item, "netAmount", "NetAmount")),
    currency: text(prop(item, "currency", "Currency"), "IRR"),
    sourceType: text(prop(item, "sourceType", "SourceType")),
    sellerOrderId: orderRaw == null ? null : text(orderRaw),
    postedAt: text(prop(item, "postedAt", "PostedAt")),
  };
}

export function mapPayoutRequest(value: unknown): PayoutRequestRow | null {
  const item = recordOf(value);
  if (!item) return null;
  const id = text(prop(item, "payoutRequestId", "PayoutRequestId"));
  if (!id) return null;
  return {
    payoutRequestId: id,
    sellerPartyId: text(prop(item, "sellerPartyId", "SellerPartyId")),
    sellerDisplayName: text(prop(item, "sellerDisplayName", "SellerDisplayName"), text(prop(item, "displayName", "DisplayName"), "فروشنده")),
    amount: number(prop(item, "amount", "Amount")),
    currency: text(prop(item, "currency", "Currency"), "IRR"),
    status: text(prop(item, "status", "Status")),
    idempotencyKey: text(prop(item, "idempotencyKey", "IdempotencyKey")),
    createdAt: text(prop(item, "createdAt", "CreatedAt")),
    updatedAt: text(prop(item, "updatedAt", "UpdatedAt")),
  };
}

export function formatSettlementMoney(amount: number, currency = "IRR"): string {
  const digits = new Intl.NumberFormat("fa-IR").format(amount);
  return currency === "IRR" ? `${digits} ریال` : `${digits} ${currency}`;
}

export function formatEntryType(entryType: string): string {
  switch (entryType) {
    case "Credit":
    case "0":
      return "واریز تسویه";
    case "Debit":
    case "1":
      return "برداشت/تعدیل";
    case "Adjustment":
    case "2":
      return "تعدیل";
    default:
      return entryType || "نامشخص";
  }
}

export function formatPayoutStatus(status: string): string {
  switch (status) {
    case "Requested":
    case "0":
      return "در انتظار";
    case "Processing":
    case "1":
      return "در حال پردازش";
    case "Succeeded":
    case "2":
      return "موفق";
    case "Failed":
    case "3":
      return "ناموفق";
    case "Cancelled":
    case "4":
      return "لغو شده";
    default:
      return status || "نامشخص";
  }
}

export function payoutStatusClass(status: string): string {
  if (status === "Succeeded" || status === "2") return "bg-emerald-50 text-emerald-700";
  if (status === "Failed" || status === "3") return "bg-red-50 text-red-700";
  return "bg-amber-50 text-amber-700";
}

export async function loadSellerSettlementBalance(sellerPartyId: string): Promise<{
  balance: SettlementBalance | null;
  message?: string;
}> {
  try {
    const response = await fetch("/v1/seller/settlement/balance", {
      headers: sellerHeaders(sellerPartyId),
    });
    if (response.status === 404) return { balance: null, message: "settlement.account.missing" };
    if (!response.ok) return { balance: null, message: `seller-settlement-http-${response.status}` };
    return { balance: mapSettlementBalance(await response.json()) };
  } catch {
    return { balance: null, message: "host-unreachable" };
  }
}

export async function loadSellerSettlementEntries(sellerPartyId: string): Promise<SettlementEntryRow[]> {
  try {
    const response = await fetch("/v1/seller/settlement/entries", {
      headers: sellerHeaders(sellerPartyId),
    });
    if (!response.ok) return [];
    const payload = await response.json();
    if (!Array.isArray(payload)) return [];
    return payload.map(mapSettlementEntry).filter((row): row is SettlementEntryRow => row !== null);
  } catch {
    return [];
  }
}

export async function loadSellerPayoutRequests(sellerPartyId: string): Promise<PayoutRequestRow[]> {
  try {
    const response = await fetch("/v1/seller/settlement/payout-requests", {
      headers: sellerHeaders(sellerPartyId),
    });
    if (!response.ok) return [];
    const payload = await response.json();
    if (!Array.isArray(payload)) return [];
    return payload.map(mapPayoutRequest).filter((row): row is PayoutRequestRow => row !== null);
  } catch {
    return [];
  }
}

export async function requestSellerPayout(
  sellerPartyId: string,
  amount: number,
  idempotencyKey: string,
): Promise<{ ok: boolean; payout?: PayoutRequestRow; message?: string }> {
  try {
    const response = await fetch("/v1/seller/settlement/payout-requests", {
      method: "POST",
      headers: sellerHeaders(sellerPartyId, true),
      body: JSON.stringify({ amount, idempotencyKey }),
    });
    const payload = await response.json().catch(() => null);
    if (!response.ok) {
      return { ok: false, message: recordOf(payload)?.detail as string ?? `payout-http-${response.status}` };
    }
    const payout = mapPayoutRequest(payload);
    return payout ? { ok: true, payout } : { ok: false, message: "invalid-response" };
  } catch {
    return { ok: false, message: "host-unreachable" };
  }
}

export async function loadAdminSettlementBalances(): Promise<AdminResult<SettlementBalance[]>> {
  try {
    const response = await fetch("/v1/admin/settlement/balances", { headers: adminHeaders() });
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) return { state: "error", data: null, status: response.status, message: `admin.http.${response.status}` };
    const payload = await response.json();
    const rows = Array.isArray(payload)
      ? payload.map(mapSettlementBalance).filter((row): row is SettlementBalance => row !== null)
      : [];
    return { state: "ok", data: rows, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function loadAdminPayoutQueue(): Promise<AdminResult<PayoutRequestRow[]>> {
  try {
    const response = await fetch("/v1/admin/settlement/payout-queue", { headers: adminHeaders() });
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) return { state: "error", data: null, status: response.status, message: `admin.http.${response.status}` };
    const payload = await response.json();
    const rows = Array.isArray(payload)
      ? payload.map(mapPayoutRequest).filter((row): row is PayoutRequestRow => row !== null)
      : [];
    return { state: "ok", data: rows, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export type PayoutQueueRow = PayoutRequestRow & { id: string };

/** Server GridQuery — صف payout Admin. */
export function queryAdminPayoutGrid(query: GridServerQuery): Promise<AdminGridQueryResult<PayoutQueueRow>> {
  return postAdminGridQuery("/v1/admin/settlement/payout-queue/query", query, adminHeaders(), (item) => {
    const row = mapPayoutRequest(item);
    return row ? { ...row, id: row.payoutRequestId } : null;
  });
}

export async function processAdminPayout(payoutRequestId: string): Promise<boolean> {
  try {
    const response = await fetch(`/v1/admin/settlement/payout-requests/${payoutRequestId}/process`, {
      method: "POST",
      headers: adminHeaders(true),
    });
    return response.ok;
  } catch {
    return false;
  }
}

export function resolveSellerPartyFromLocation(search: string): string | null {
  return readSellerPartyId(search);
}
