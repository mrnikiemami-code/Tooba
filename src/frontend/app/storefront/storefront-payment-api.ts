import { cartHeaders, readCartSession, StorefrontCartApiError, toCustomerCartMessage } from "./storefront-cart-api.ts";

const PAYMENT_IDEMPOTENCY_KEY = "tooba.storefront.paymentIdempotency";

/**
 * نتیجهٔ شروع پرداخت. مبلغ از Host است.
 */
export interface StorefrontPaymentInitiation {
  paymentId: string;
  attemptId: string;
  checkoutId: string;
  status: string;
  providerCode: string;
  providerRequestReference: string;
  redirectUrl: string;
  amount: number;
  currency: string;
}

/**
 * تصویر پرداخت برای صفحهٔ نتیجه. موفقیت را UI جعل نمی‌کند.
 */
export interface StorefrontPaymentPage {
  paymentId: string;
  checkoutId: string;
  amount: number;
  currency: string;
  status: string;
  providerCode: string;
}

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

async function parseJson(response: Response): Promise<unknown> {
  const text = await response.text();
  if (!text) {
    return null;
  }
  try {
    return JSON.parse(text) as unknown;
  } catch {
    return text;
  }
}

function throwIfFailed(response: Response, payload: unknown, fallbackCode: string): void {
  if (response.ok) {
    return;
  }
  const record = asRecord(payload);
  const code = asString(readProp(record ?? {}, "errorCode", "ErrorCode"), fallbackCode);
  const detail = asString(readProp(record ?? {}, "detail", "Detail"));
  throw new StorefrontCartApiError(response.status, code, detail);
}

/**
 * JSON شروع پرداخت Host را نگاشت می‌کند.
 */
export function mapStorefrontPaymentInitiation(payload: unknown): StorefrontPaymentInitiation | null {
  const item = asRecord(payload);
  if (!item) {
    return null;
  }
  const paymentId = asString(readProp(item, "paymentId", "PaymentId"));
  const redirectUrl = asString(readProp(item, "redirectUrl", "RedirectUrl"));
  if (!paymentId || !redirectUrl) {
    return null;
  }
  return {
    paymentId,
    attemptId: asString(readProp(item, "attemptId", "AttemptId")),
    checkoutId: asString(readProp(item, "checkoutId", "CheckoutId")),
    status: asString(readProp(item, "status", "Status")),
    providerCode: asString(readProp(item, "providerCode", "ProviderCode")),
    providerRequestReference: asString(readProp(item, "providerRequestReference", "ProviderRequestReference")),
    redirectUrl,
    amount: asNumber(readProp(item, "amount", "Amount")),
    currency: asString(readProp(item, "currency", "Currency"), "IRR"),
  };
}

/**
 * JSON تصویر پرداخت Host را نگاشت می‌کند.
 */
export function mapStorefrontPayment(payload: unknown): StorefrontPaymentPage | null {
  const item = asRecord(payload);
  if (!item) {
    return null;
  }
  const paymentId = asString(readProp(item, "paymentId", "PaymentId"));
  if (!paymentId) {
    return null;
  }
  return {
    paymentId,
    checkoutId: asString(readProp(item, "checkoutId", "CheckoutId")),
    amount: asNumber(readProp(item, "amount", "Amount")),
    currency: asString(readProp(item, "currency", "Currency"), "IRR"),
    status: asString(readProp(item, "status", "Status")),
    providerCode: asString(readProp(item, "providerCode", "ProviderCode")),
  };
}

function paymentIdempotencyKey(checkoutId: string): string {
  const scoped = `${PAYMENT_IDEMPOTENCY_KEY}.${checkoutId}`;
  const existing = window.sessionStorage.getItem(scoped);
  if (existing) {
    return existing;
  }
  const created = crypto.randomUUID();
  window.sessionStorage.setItem(scoped, created);
  return created;
}

/**
 * پیام مشتری برای خطای پرداخت؛ کد فنی را نشان نمی‌دهد.
 */
export function toCustomerPaymentMessage(error: unknown): string {
  if (error instanceof StorefrontCartApiError) {
    switch (error.errorCode) {
      case "payment.already-paid":
        return "این سفارش قبلاً پرداخت شده است.";
      case "payment.missing":
        return "پرداخت پیدا نشد.";
      case "payment.guest.invalid":
        return "دسترسی به پرداخت معتبر نیست.";
      default:
        return error.detail && !/Held|GATEWAY_|Verify/i.test(error.detail)
          ? error.detail
          : "امکان شروع پرداخت در حال حاضر وجود ندارد.";
    }
  }
  return toCustomerCartMessage(error);
}

/**
 * پرداخت سفارش PendingPayment را از Host شروع می‌کند. مبلغ در بدنه نیست.
 */
export async function startStorefrontPayment(checkoutId: string): Promise<StorefrontPaymentInitiation> {
  const session = readCartSession();
  if (!session.cartId) {
    throw new StorefrontCartApiError(401, "payment.guest.invalid", "سبد برای شروع پرداخت پیدا نشد.");
  }
  const response = await fetch(`/v1/storefront/checkout/${encodeURIComponent(checkoutId)}/payments`, {
    method: "POST",
    cache: "no-store",
    headers: cartHeaders(),
    body: JSON.stringify({
      cartId: session.cartId,
      idempotencyKey: paymentIdempotencyKey(checkoutId),
    }),
  });
  const payload = await parseJson(response);
  throwIfFailed(response, payload, "payment.rejected");
  const mapped = mapStorefrontPaymentInitiation(payload);
  if (!mapped) {
    throw new StorefrontCartApiError(500, "payment.rejected", "پاسخ شروع پرداخت نامعتبر بود.");
  }
  return mapped;
}

/**
 * تصویر پرداخت را از Host می‌خواند.
 */
export async function loadStorefrontPayment(paymentId: string): Promise<StorefrontPaymentPage> {
  const session = readCartSession();
  if (!session.cartId) {
    throw new StorefrontCartApiError(401, "payment.guest.invalid", "سبد برای مشاهدهٔ پرداخت پیدا نشد.");
  }
  const response = await fetch(
    `/v1/storefront/payments/${encodeURIComponent(paymentId)}?cartId=${encodeURIComponent(session.cartId)}`,
    { cache: "no-store", headers: cartHeaders() },
  );
  const payload = await parseJson(response);
  throwIfFailed(response, payload, "payment.missing");
  const mapped = mapStorefrontPayment(payload);
  if (!mapped) {
    throw new StorefrontCartApiError(500, "payment.missing", "پاسخ پرداخت نامعتبر بود.");
  }
  return mapped;
}

/**
 * تکمیل sandbox/dev. موفقیت را UI اعلام نمی‌کند؛ Host Verify می‌کند.
 */
export async function completeStorefrontSandboxPayment(
  paymentId: string,
  attemptId: string,
  providerRequestReference: string,
  outcome: "success" | "failure",
): Promise<StorefrontPaymentPage> {
  const session = readCartSession();
  if (!session.cartId) {
    throw new StorefrontCartApiError(401, "payment.guest.invalid", "سبد برای تکمیل پرداخت پیدا نشد.");
  }
  const response = await fetch(`/v1/storefront/payments/${encodeURIComponent(paymentId)}/sandbox/complete`, {
    method: "POST",
    cache: "no-store",
    headers: cartHeaders(),
    body: JSON.stringify({
      cartId: session.cartId,
      attemptId,
      providerRequestReference,
      outcome,
    }),
  });
  const payload = await parseJson(response);
  throwIfFailed(response, payload, "payment.rejected");
  const mapped = mapStorefrontPayment(payload);
  if (!mapped) {
    throw new StorefrontCartApiError(500, "payment.rejected", "پاسخ تأیید پرداخت نامعتبر بود.");
  }
  return mapped;
}
