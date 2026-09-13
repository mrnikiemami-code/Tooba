import { customerAuthHeaders } from "../customer-panel/customer-api.ts";
import {
  listCommittedCheckoutProofs,
  StorefrontCartApiError,
} from "./storefront-cart-api.ts";

export type StorefrontPendingPaymentLine = {
  title: string;
  quantity: number;
  mediaAssetId: string | null;
};

export type StorefrontPendingPaymentItem = {
  checkoutId: string;
  cartId: string;
  orderReference: string;
  payableAmount: number;
  currency: string;
  items: StorefrontPendingPaymentLine[];
  paymentPresentation: string;
  canInitiatePayment: boolean;
  canRetryPayment: boolean;
  isManualAwaitingReview: boolean;
  primaryAction: "pay" | "retryAfterExpiry" | "none";
  reservationPresentation: "held" | "ended" | "none";
  cycleNumber: number | null;
  secondsRemaining: number;
  serverTime: string;
  holdEndsAt: string | null;
  maxCycles: number;
  retryCountRemaining: number;
  supplyStatus: string | null;
  paymentId: string | null;
  hasReachedRetryLimit: boolean;
};

export type StorefrontPendingPaymentPage = {
  serverTime: string;
  items: StorefrontPendingPaymentItem[];
};

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : null;
}

function readProp(record: Record<string, unknown>, camel: string, pascal: string): unknown {
  return record[camel] ?? record[pascal];
}

function asString(value: unknown, fallback = ""): string {
  return value == null ? fallback : String(value);
}

function asNumber(value: unknown, fallback = 0): number {
  if (typeof value === "number" && Number.isFinite(value)) {
    return value;
  }
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
}

function asBool(value: unknown): boolean {
  return value === true;
}

export function remainingSecondsFromServer(
  holdEndsAt: string | null | undefined,
  serverTime: string,
  clientReceivedAtMs: number,
  nowMs = Date.now(),
): number {
  if (!holdEndsAt) {
    return 0;
  }
  const end = Date.parse(holdEndsAt);
  const server = Date.parse(serverTime);
  if (!Number.isFinite(end) || !Number.isFinite(server)) {
    return 0;
  }
  const elapsed = Math.max(0, nowMs - clientReceivedAtMs);
  return Math.max(0, Math.floor((end - (server + elapsed)) / 1000));
}

export function formatCountdown(totalSeconds: number): string {
  const safe = Math.max(0, Math.floor(totalSeconds));
  const minutes = Math.floor(safe / 60);
  const seconds = safe % 60;
  return `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;
}

export function shouldRefreshOnceAtZero(previousSeconds: number, nextSeconds: number, alreadyRefreshed: boolean): boolean {
  return !alreadyRefreshed && previousSeconds > 0 && nextSeconds <= 0;
}

export function toCustomerPendingPaymentMessage(error: unknown, locale: "fa" | "en" = "fa"): string {
  const fa = locale === "fa";
  if (error instanceof StorefrontCartApiError) {
    switch (error.errorCode) {
      case "payment.unpaid.supply_unavailable":
        return fa ? "این سفارش در حال حاضر قابل تأمین نیست." : "This order cannot be supplied right now.";
      case "inventory.reservation.retry_limit_reached":
        return fa
          ? "تعداد دفعات مجاز رزرو مجدد موجودی برای این سفارش به پایان رسیده است."
          : "No more inventory reservation retries remain for this order.";
      case "payment.already_succeeded":
      case "payment.already-paid":
        return fa
          ? "پرداخت این سفارش قبلاً با موفقیت انجام شده است."
          : "This order has already been paid.";
      case "payment.access.denied":
      case "checkout.access.denied":
      case "payment.guest.invalid":
        return fa
          ? "دسترسی به اطلاعات پرداخت این سفارش تأیید نشد. لطفاً از بخش سفارش‌ها دوباره وارد پرداخت شوید."
          : "Access to this order payment was not confirmed.";
      default:
        if (error.detail && !/Held|reservation|Active|Expired|retry_limit/i.test(error.detail)) {
          return error.detail;
        }
    }
  }
  return fa ? "امکان ادامه پرداخت این سفارش نیست." : "This order cannot continue to payment.";
}

export function mapStorefrontPendingPayments(payload: unknown): StorefrontPendingPaymentPage | null {
  const root = asRecord(payload);
  if (!root) {
    return null;
  }
  const rawItems = readProp(root, "items", "Items");
  const items = Array.isArray(rawItems) ? rawItems : [];
  return {
    serverTime: asString(readProp(root, "serverTime", "ServerTime")),
    items: items
      .map((row) => mapItem(row))
      .filter((row): row is StorefrontPendingPaymentItem => row !== null),
  };
}

function mapItem(value: unknown): StorefrontPendingPaymentItem | null {
  const item = asRecord(value);
  if (!item) {
    return null;
  }
  const checkoutId = asString(readProp(item, "checkoutId", "CheckoutId"));
  if (!checkoutId) {
    return null;
  }
  const action = asString(readProp(item, "primaryAction", "PrimaryAction"), "none");
  const reservation = asString(readProp(item, "reservationPresentation", "ReservationPresentation"), "none");
  const rawLines = readProp(item, "items", "Items");
  const lines = Array.isArray(rawLines) ? rawLines : [];
  const paymentIdRaw = readProp(item, "paymentId", "PaymentId");
  const cycleRaw = readProp(item, "cycleNumber", "CycleNumber");
  const holdRaw = readProp(item, "holdEndsAt", "HoldEndsAt");
  return {
    checkoutId,
    cartId: asString(readProp(item, "cartId", "CartId")),
    orderReference: asString(readProp(item, "orderReference", "OrderReference")),
    payableAmount: asNumber(readProp(item, "payableAmount", "PayableAmount")),
    currency: asString(readProp(item, "currency", "Currency"), "IRR"),
    items: lines.map((line) => {
      const rec = asRecord(line) ?? {};
      const media = readProp(rec, "mediaAssetId", "MediaAssetId");
      return {
        title: asString(readProp(rec, "title", "Title"), "کالا"),
        quantity: asNumber(readProp(rec, "quantity", "Quantity"), 1),
        mediaAssetId: media == null || media === "" ? null : asString(media),
      };
    }),
    paymentPresentation: asString(readProp(item, "paymentPresentation", "PaymentPresentation")),
    canInitiatePayment: asBool(readProp(item, "canInitiatePayment", "CanInitiatePayment")),
    canRetryPayment: asBool(readProp(item, "canRetryPayment", "CanRetryPayment")),
    isManualAwaitingReview: asBool(readProp(item, "isManualAwaitingReview", "IsManualAwaitingReview")),
    primaryAction: action === "pay" || action === "retryAfterExpiry" ? action : "none",
    reservationPresentation: reservation === "held" || reservation === "ended" ? reservation : "none",
    cycleNumber: cycleRaw == null || cycleRaw === "" ? null : asNumber(cycleRaw),
    secondsRemaining: asNumber(readProp(item, "secondsRemaining", "SecondsRemaining")),
    serverTime: asString(readProp(item, "serverTime", "ServerTime")),
    holdEndsAt: holdRaw == null || holdRaw === "" ? null : asString(holdRaw),
    maxCycles: asNumber(readProp(item, "maxCycles", "MaxCycles"), 3),
    retryCountRemaining: asNumber(readProp(item, "retryCountRemaining", "RetryCountRemaining")),
    supplyStatus: (() => {
      const supply = readProp(item, "supplyStatus", "SupplyStatus");
      return supply == null || supply === "" ? null : asString(supply);
    })(),
    paymentId: paymentIdRaw == null || paymentIdRaw === "" ? null : asString(paymentIdRaw),
    hasReachedRetryLimit: asBool(readProp(item, "hasReachedRetryLimit", "HasReachedRetryLimit")),
  };
}

export async function loadStorefrontPendingPayments(): Promise<StorefrontPendingPaymentPage> {
  const proofs = listCommittedCheckoutProofs().map((proof) => ({
    checkoutId: proof.checkoutId,
    cartId: proof.cartId,
    guestSecret: proof.guestSecret,
  }));
  const headers = {
    ...customerAuthHeaders(true),
  };
  const response = await fetch("/v1/storefront/pending-payments", {
    method: "POST",
    cache: "no-store",
    credentials: "include",
    headers,
    body: JSON.stringify({ proofs }),
  });
  const payload: unknown = await response.json().catch(() => null);
  if (!response.ok) {
    const record = asRecord(payload);
    throw new StorefrontCartApiError(
      response.status,
      record ? asString(readProp(record, "errorCode", "ErrorCode")) || null : null,
      record ? asString(readProp(record, "detail", "Detail")) || null : null,
    );
  }
  const mapped = mapStorefrontPendingPayments(payload);
  if (!mapped) {
    throw new StorefrontCartApiError(500, "pending.invalid", "پاسخ سفارش‌های در انتظار نامعتبر است.");
  }
  return mapped;
}
