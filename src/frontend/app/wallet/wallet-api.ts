/**
 * کلاینت کیف پول و کارت هدیه — Customer BFF و Admin Host.
 * قرارداد: /v1/customer/wallet* و /v1/admin/gift-cards|wallets|wallet/*
 */

import { ADMIN_DEV_ACTOR_HEADER, type AdminResult } from "../admin/admin-api.ts";
import { customerAuthHeaders } from "../customer-panel/customer-api.ts";
import { ensureCsrfCookie } from "../../lib/auth/browser-session.ts";

export interface WalletSummary {
  accountId: string;
  customerActorUserId: string;
  currency: string;
  status: string;
  balance: number;
  totalCredits: number;
  totalDebits: number;
  entryCount: number;
  createdAt: string;
}

export interface WalletLedgerEntry {
  entryId: string;
  accountId: string;
  type: string;
  amount: number;
  currency: string;
  direction: string;
  sourceType: string;
  sourceId: string;
  createdAt: string;
  metadata: string | null;
}

export interface WalletLedgerPage {
  items: WalletLedgerEntry[];
  total: number;
  page: number;
  pageSize: number;
  balance: number;
}

export interface GiftCardRedeemResult {
  redemptionId: string;
  cardId: string;
  accountId: string;
  amount: number;
  walletBalance: number;
  cardStatus: string;
  cardRemainingAmount: number;
  idempotentReplay: boolean;
}

export interface GiftCardSummary {
  cardId: string;
  currency: string;
  initialAmount: number;
  remainingAmount: number;
  status: string;
  issuedAt: string;
  expiresAt: string | null;
  recipientActorUserId: string | null;
  createdByActorUserId: string;
  redemptionCount: number;
}

export interface GiftCardRedemption {
  redemptionId: string;
  cardId: string;
  accountId: string;
  amount: number;
  createdAt: string;
}

export interface GiftCardDetail {
  cardId: string;
  currency: string;
  initialAmount: number;
  remainingAmount: number;
  status: string;
  issuedAt: string;
  expiresAt: string | null;
  recipientActorUserId: string | null;
  createdByActorUserId: string;
  redemptions: GiftCardRedemption[];
}

export interface GiftCardListPage {
  items: GiftCardSummary[];
  total: number;
  page: number;
  pageSize: number;
}

export interface GiftCardIssueResult {
  card: GiftCardSummary;
  displayCode: string;
  idempotentReplay: boolean;
}

export interface WalletDemoPreview {
  customerActorUserId: string;
  accountId: string;
  balance: number;
  unusedGiftCardId: string;
  unusedGiftCardDemoCode: string;
  partiallyRedeemedGiftCardId: string;
  expiredGiftCardId: string;
  revokedGiftCardId: string;
  note: string;
}

export interface IssueGiftCardInput {
  initialAmount: number;
  currency?: string;
  expiresAt?: string | null;
  recipientActorUserId?: string | null;
  idempotencyKey: string;
}

export interface AdminWalletAdjustmentInput {
  amount: number;
  direction: "Credit" | "Debit" | string;
  reason: string;
  idempotencyKey: string;
}

export interface AdminWalletAdjustmentResult {
  entry: WalletLedgerEntry;
  balance: number;
  idempotentReplay: boolean;
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

function nullableText(value: unknown): string | null {
  return value == null || String(value).length === 0 ? null : String(value);
}

function number(value: unknown): number {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
}

function flag(value: unknown): boolean {
  return value === true;
}

function adminActorHeader(json = false): Record<string, string> {
  const actor =
    typeof window !== "undefined" ? (window.localStorage.getItem("tooba.adminActorUserId") ?? "") : "";
  const headers: Record<string, string> = { Accept: "application/json", [ADMIN_DEV_ACTOR_HEADER]: actor };
  if (json) headers["Content-Type"] = "application/json";
  return headers;
}

function buildQuery(params: Record<string, string | number | undefined | null>): string {
  const qs = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value == null || value === "") continue;
    qs.set(key, String(value));
  }
  const raw = qs.toString();
  return raw ? `?${raw}` : "";
}

function errorCodeFrom(payload: unknown, fallback: string): string {
  const item = record(payload);
  return text(prop(item ?? {}, "errorCode", "ErrorCode"), fallback);
}

function newIdempotencyKey(): string {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }
  return `wallet-${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

/** خلاصهٔ کیف پول مشتری/Admin. */
export function mapWalletSummary(value: unknown): WalletSummary | null {
  const item = record(value);
  if (!item) return null;
  const accountId = text(prop(item, "accountId", "AccountId"));
  if (!accountId) return null;
  return {
    accountId,
    customerActorUserId: text(prop(item, "customerActorUserId", "CustomerActorUserId")),
    currency: text(prop(item, "currency", "Currency"), "IRR"),
    status: text(prop(item, "status", "Status"), "Active"),
    balance: number(prop(item, "balance", "Balance")),
    totalCredits: number(prop(item, "totalCredits", "TotalCredits")),
    totalDebits: number(prop(item, "totalDebits", "TotalDebits")),
    entryCount: number(prop(item, "entryCount", "EntryCount")),
    createdAt: text(prop(item, "createdAt", "CreatedAt")),
  };
}

/** یک سطر دفتر. */
export function mapWalletLedgerEntry(value: unknown): WalletLedgerEntry | null {
  const item = record(value);
  if (!item) return null;
  const entryId = text(prop(item, "entryId", "EntryId"));
  if (!entryId) return null;
  return {
    entryId,
    accountId: text(prop(item, "accountId", "AccountId")),
    type: text(prop(item, "type", "Type")),
    amount: number(prop(item, "amount", "Amount")),
    currency: text(prop(item, "currency", "Currency"), "IRR"),
    direction: text(prop(item, "direction", "Direction")),
    sourceType: text(prop(item, "sourceType", "SourceType")),
    sourceId: text(prop(item, "sourceId", "SourceId")),
    createdAt: text(prop(item, "createdAt", "CreatedAt")),
    metadata: nullableText(prop(item, "metadata", "Metadata")),
  };
}

/** صفحهٔ دفتر. */
export function mapWalletLedgerPage(value: unknown): WalletLedgerPage {
  const item = record(value);
  const rawItems = item ? prop(item, "items", "Items") : value;
  const items = Array.isArray(rawItems)
    ? rawItems.map(mapWalletLedgerEntry).filter((row): row is WalletLedgerEntry => row !== null)
    : [];
  return {
    items,
    total: item ? number(prop(item, "total", "Total")) : items.length,
    page: item ? number(prop(item, "page", "Page")) || 1 : 1,
    pageSize: item ? number(prop(item, "pageSize", "PageSize")) || items.length || 20 : 20,
    balance: item ? number(prop(item, "balance", "Balance")) : 0,
  };
}

/** نتیجهٔ بازخرید. */
export function mapGiftCardRedeemResult(value: unknown): GiftCardRedeemResult | null {
  const item = record(value);
  if (!item) return null;
  const redemptionId = text(prop(item, "redemptionId", "RedemptionId"));
  if (!redemptionId) return null;
  return {
    redemptionId,
    cardId: text(prop(item, "cardId", "CardId")),
    accountId: text(prop(item, "accountId", "AccountId")),
    amount: number(prop(item, "amount", "Amount")),
    walletBalance: number(prop(item, "walletBalance", "WalletBalance")),
    cardStatus: text(prop(item, "cardStatus", "CardStatus")),
    cardRemainingAmount: number(prop(item, "cardRemainingAmount", "CardRemainingAmount")),
    idempotentReplay: flag(prop(item, "idempotentReplay", "IdempotentReplay")),
  };
}

/** خلاصهٔ کارت هدیه Admin. */
export function mapGiftCardSummary(value: unknown): GiftCardSummary | null {
  const item = record(value);
  if (!item) return null;
  const cardId = text(prop(item, "cardId", "CardId"));
  if (!cardId) return null;
  return {
    cardId,
    currency: text(prop(item, "currency", "Currency"), "IRR"),
    initialAmount: number(prop(item, "initialAmount", "InitialAmount")),
    remainingAmount: number(prop(item, "remainingAmount", "RemainingAmount")),
    status: text(prop(item, "status", "Status")),
    issuedAt: text(prop(item, "issuedAt", "IssuedAt")),
    expiresAt: nullableText(prop(item, "expiresAt", "ExpiresAt")),
    recipientActorUserId: nullableText(prop(item, "recipientActorUserId", "RecipientActorUserId")),
    createdByActorUserId: text(prop(item, "createdByActorUserId", "CreatedByActorUserId")),
    redemptionCount: number(prop(item, "redemptionCount", "RedemptionCount")),
  };
}

function mapGiftCardRedemption(value: unknown): GiftCardRedemption | null {
  const item = record(value);
  if (!item) return null;
  const redemptionId = text(prop(item, "redemptionId", "RedemptionId"));
  if (!redemptionId) return null;
  return {
    redemptionId,
    cardId: text(prop(item, "cardId", "CardId")),
    accountId: text(prop(item, "accountId", "AccountId")),
    amount: number(prop(item, "amount", "Amount")),
    createdAt: text(prop(item, "createdAt", "CreatedAt")),
  };
}

/** جزئیات کارت. */
export function mapGiftCardDetail(value: unknown): GiftCardDetail | null {
  const item = record(value);
  if (!item) return null;
  const cardId = text(prop(item, "cardId", "CardId"));
  if (!cardId) return null;
  const raw = prop(item, "redemptions", "Redemptions");
  const redemptions = Array.isArray(raw)
    ? raw.map(mapGiftCardRedemption).filter((row): row is GiftCardRedemption => row !== null)
    : [];
  return {
    cardId,
    currency: text(prop(item, "currency", "Currency"), "IRR"),
    initialAmount: number(prop(item, "initialAmount", "InitialAmount")),
    remainingAmount: number(prop(item, "remainingAmount", "RemainingAmount")),
    status: text(prop(item, "status", "Status")),
    issuedAt: text(prop(item, "issuedAt", "IssuedAt")),
    expiresAt: nullableText(prop(item, "expiresAt", "ExpiresAt")),
    recipientActorUserId: nullableText(prop(item, "recipientActorUserId", "RecipientActorUserId")),
    createdByActorUserId: text(prop(item, "createdByActorUserId", "CreatedByActorUserId")),
    redemptions,
  };
}

/** فهرست صفحه‌بندی‌شدهٔ کارت. */
export function mapGiftCardListPage(value: unknown): GiftCardListPage {
  const item = record(value);
  const rawItems = item ? prop(item, "items", "Items") : value;
  const items = Array.isArray(rawItems)
    ? rawItems.map(mapGiftCardSummary).filter((row): row is GiftCardSummary => row !== null)
    : [];
  return {
    items,
    total: item ? number(prop(item, "total", "Total")) : items.length,
    page: item ? number(prop(item, "page", "Page")) || 1 : 1,
    pageSize: item ? number(prop(item, "pageSize", "PageSize")) || items.length || 20 : 20,
  };
}

/** نتیجهٔ صدور. */
export function mapGiftCardIssueResult(value: unknown): GiftCardIssueResult | null {
  const item = record(value);
  if (!item) return null;
  const card = mapGiftCardSummary(prop(item, "card", "Card"));
  const displayCode = text(prop(item, "displayCode", "DisplayCode"));
  if (!card || !displayCode) return null;
  return {
    card,
    displayCode,
    idempotentReplay: flag(prop(item, "idempotentReplay", "IdempotentReplay")),
  };
}

/** پیش‌نمایش دانهٔ توسعه. */
export function mapWalletDemoPreview(value: unknown): WalletDemoPreview | null {
  const item = record(value);
  if (!item) return null;
  const accountId = text(prop(item, "accountId", "AccountId"));
  if (!accountId) return null;
  return {
    customerActorUserId: text(prop(item, "customerActorUserId", "CustomerActorUserId")),
    accountId,
    balance: number(prop(item, "balance", "Balance")),
    unusedGiftCardId: text(prop(item, "unusedGiftCardId", "UnusedGiftCardId")),
    unusedGiftCardDemoCode: text(prop(item, "unusedGiftCardDemoCode", "UnusedGiftCardDemoCode")),
    partiallyRedeemedGiftCardId: text(prop(item, "partiallyRedeemedGiftCardId", "PartiallyRedeemedGiftCardId")),
    expiredGiftCardId: text(prop(item, "expiredGiftCardId", "ExpiredGiftCardId")),
    revokedGiftCardId: text(prop(item, "revokedGiftCardId", "RevokedGiftCardId")),
    note: text(prop(item, "note", "Note")),
  };
}

export function mapAdminWalletAdjustmentResult(value: unknown): AdminWalletAdjustmentResult | null {
  const item = record(value);
  if (!item) return null;
  const entry = mapWalletLedgerEntry(prop(item, "entry", "Entry"));
  if (!entry) return null;
  return {
    entry,
    balance: number(prop(item, "balance", "Balance")),
    idempotentReplay: flag(prop(item, "idempotentReplay", "IdempotentReplay")),
  };
}

export function toPersianDigits(value: string | number): string {
  return String(value).replace(/\d/g, (d) => "۰۱۲۳۴۵۶۷۸۹"[Number(d)] ?? d);
}

export function formatWalletMoney(amount: number): string {
  return toPersianDigits(Math.round(amount).toLocaleString("en-US"));
}

export function formatLedgerDate(iso: string): string {
  if (!iso) return "—";
  const date = new Date(iso);
  return Number.isNaN(date.getTime())
    ? iso
    : new Intl.DateTimeFormat("fa-IR", { year: "numeric", month: "2-digit", day: "2-digit" }).format(date);
}

export function formatLedgerEntryLabel(entry: WalletLedgerEntry): string {
  switch (entry.type) {
    case "GiftCardCredit":
      return "اعتبار کارت هدیه";
    case "AdminAdjustment":
      return "تعدیل مدیریت";
    case "OrderPaymentDebit":
      return "پرداخت سفارش";
    case "RefundCredit":
      return "اعتبار مرجوعی";
    default:
      return entry.type || entry.sourceType || "تراکنش";
  }
}

export function formatGiftCardStatus(status: string): string {
  switch (status) {
    case "Active":
      return "فعال";
    case "Redeemed":
      return "مصرف‌شده";
    case "PartiallyRedeemed":
      return "بخشی مصرف‌شده";
    case "Expired":
      return "منقضی";
    case "Revoked":
      return "باطل‌شده";
    default:
      return status || "—";
  }
}

export function isCreditDirection(direction: string): boolean {
  return direction === "Credit" || direction === "0";
}

/** خلاصهٔ کیف پول مشتری از BFF. */
export async function loadCustomerWallet(): Promise<WalletSummary | null> {
  try {
    const response = await fetch("/api/customer/wallet", {
      credentials: "include",
      headers: customerAuthHeaders(),
    });
    if (!response.ok) return null;
    return mapWalletSummary(await response.json());
  } catch {
    return null;
  }
}

/** دفتر مشتری. */
export async function loadCustomerLedger(opts?: {
  page?: number;
  pageSize?: number;
}): Promise<WalletLedgerPage | null> {
  try {
    const query = buildQuery({ page: opts?.page ?? 1, pageSize: opts?.pageSize ?? 20 });
    const response = await fetch(`/api/customer/wallet/ledger${query}`, {
      credentials: "include",
      headers: customerAuthHeaders(),
    });
    if (!response.ok) return null;
    return mapWalletLedgerPage(await response.json());
  } catch {
    return null;
  }
}

/** بازخرید کارت هدیه به کیف پول مشتری. */
export async function redeemCustomerGiftCard(
  code: string,
  idempotencyKey?: string,
): Promise<{ ok: true; result: GiftCardRedeemResult } | { ok: false; errorCode: string }> {
  try {
    await ensureCsrfCookie();
    const key = idempotencyKey?.trim() || newIdempotencyKey();
    const response = await fetch("/api/customer/wallet/gift-cards/redeem", {
      method: "POST",
      credentials: "include",
      headers: customerAuthHeaders(true),
      body: JSON.stringify({ code: code.trim(), idempotencyKey: key }),
    });
    const payload = await response.json().catch(() => null);
    if (!response.ok) {
      return { ok: false, errorCode: errorCodeFrom(payload, "wallet.redeem.rejected") };
    }
    const result = mapGiftCardRedeemResult(payload);
    if (!result) return { ok: false, errorCode: "wallet.invalid-response" };
    return { ok: true, result };
  } catch {
    return { ok: false, errorCode: "host-unreachable" };
  }
}

async function adminRead<T>(path: string, mapper: (value: unknown) => T | null): Promise<AdminResult<T>> {
  try {
    const response = await fetch(path, { headers: adminActorHeader() });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: `admin.http.${response.status}` };
    }
    const data = mapper(payload);
    return data == null
      ? { state: "error", data: null, status: response.status, message: "admin.invalid-response" }
      : { state: "ok", data, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** فهرست Admin کارت‌های هدیه. */
export function loadAdminGiftCards(opts?: {
  status?: string;
  q?: string;
  page?: number;
  pageSize?: number;
}): Promise<AdminResult<GiftCardListPage>> {
  const query = buildQuery({
    status: opts?.status,
    q: opts?.q,
    page: opts?.page ?? 1,
    pageSize: opts?.pageSize ?? 50,
  });
  return adminRead(`/v1/admin/gift-cards${query}`, (value) => mapGiftCardListPage(value));
}

/** جزئیات کارت. */
export function loadAdminGiftCard(cardId: string): Promise<AdminResult<GiftCardDetail>> {
  return adminRead(`/v1/admin/gift-cards/${encodeURIComponent(cardId)}`, mapGiftCardDetail);
}

/** صدور کارت. */
export async function issueAdminGiftCard(
  input: IssueGiftCardInput,
): Promise<AdminResult<GiftCardIssueResult>> {
  try {
    const response = await fetch("/v1/admin/gift-cards", {
      method: "POST",
      headers: adminActorHeader(true),
      body: JSON.stringify({
        initialAmount: input.initialAmount,
        currency: input.currency ?? "IRR",
        expiresAt: input.expiresAt ?? null,
        recipientActorUserId: input.recipientActorUserId ?? null,
        idempotencyKey: input.idempotencyKey,
      }),
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return {
        state: "error",
        data: null,
        status: response.status,
        message: errorCodeFrom(payload, `admin.http.${response.status}`),
      };
    }
    const data = mapGiftCardIssueResult(payload);
    return data == null
      ? { state: "error", data: null, status: response.status, message: "admin.invalid-response" }
      : { state: "ok", data, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** ابطال کارت. */
export async function revokeAdminGiftCard(cardId: string): Promise<AdminResult<GiftCardDetail>> {
  try {
    const response = await fetch(`/v1/admin/gift-cards/${encodeURIComponent(cardId)}/revoke`, {
      method: "POST",
      headers: adminActorHeader(true),
      body: "{}",
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return {
        state: "error",
        data: null,
        status: response.status,
        message: errorCodeFrom(payload, `admin.http.${response.status}`),
      };
    }
    const data = mapGiftCardDetail(payload);
    return data == null
      ? { state: "error", data: null, status: response.status, message: "admin.invalid-response" }
      : { state: "ok", data, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** بازرسی کیف پول مشتری. */
export function loadAdminWallet(customerActorUserId: string): Promise<AdminResult<WalletSummary>> {
  return adminRead(
    `/v1/admin/wallets/${encodeURIComponent(customerActorUserId)}`,
    mapWalletSummary,
  );
}

/** دفتر Admin. */
export function loadAdminWalletLedger(
  customerActorUserId: string,
  opts?: { page?: number; pageSize?: number },
): Promise<AdminResult<WalletLedgerPage>> {
  const query = buildQuery({ page: opts?.page ?? 1, pageSize: opts?.pageSize ?? 50 });
  return adminRead(
    `/v1/admin/wallets/${encodeURIComponent(customerActorUserId)}/ledger${query}`,
    (value) => mapWalletLedgerPage(value),
  );
}

/** تعدیل immutable. */
export async function adjustAdminWallet(
  customerActorUserId: string,
  input: AdminWalletAdjustmentInput,
): Promise<AdminResult<AdminWalletAdjustmentResult>> {
  try {
    const response = await fetch(
      `/v1/admin/wallets/${encodeURIComponent(customerActorUserId)}/adjustments`,
      {
        method: "POST",
        headers: adminActorHeader(true),
        body: JSON.stringify({
          amount: input.amount,
          direction: input.direction,
          reason: input.reason,
          idempotencyKey: input.idempotencyKey,
        }),
      },
    );
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return {
        state: "error",
        data: null,
        status: response.status,
        message: errorCodeFrom(payload, `admin.http.${response.status}`),
      };
    }
    const data = mapAdminWalletAdjustmentResult(payload);
    return data == null
      ? { state: "error", data: null, status: response.status, message: "admin.invalid-response" }
      : { state: "ok", data, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** snapshot پیش‌نمایش توسعه. */
export function loadWalletDemoPreview(): Promise<AdminResult<WalletDemoPreview>> {
  return adminRead("/v1/admin/wallet/demo-preview", mapWalletDemoPreview);
}

export function createWalletIdempotencyKey(): string {
  return newIdempotencyKey();
}
