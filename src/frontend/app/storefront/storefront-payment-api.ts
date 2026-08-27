import {
  CUSTOMER_DEV_ACTOR_HEADER,
  DEFAULT_CUSTOMER_DEV_ACTOR_ID,
} from "../customer-panel/customer-api.ts";
import { cartHeaders, readCartSession, StorefrontCartApiError, toCustomerCartMessage } from "./storefront-cart-api.ts";

const PAYMENT_IDEMPOTENCY_KEY = "tooba.storefront.paymentIdempotency";

/** کد درگاه کیف پول (پرداخت کامل؛ بدون redirect سندباکس). */
export const WALLET_PROVIDER_CODE = "wallet";

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
  /** برای wallet کامل ممکن است خالی باشد. */
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

/**
 * نقل‌قول کیف‌پول محاسبه‌شده در Host برای تسویه/تأیید سفارش.
 * mixedTenderAvailable فقط وقتی LIVE است true می‌شود؛ در غیر این صورت UI نباید ادعا کند.
 */
export interface StorefrontWalletQuote {
  checkoutId: string | null;
  cartId: string;
  currency: string;
  balance: number;
  maxUsableAmount: number;
  selectedWalletAmount: number;
  remainingPayable: number;
  payableAmount: number;
  canPayFullyWithWallet: boolean;
  mixedTenderAvailable: boolean;
}

export type StorefrontPaymentMethodId = "gateway" | "wallet";

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
 * redirectUrl برای پرداخت کامل wallet می‌تواند خالی باشد.
 */
export function mapStorefrontPaymentInitiation(payload: unknown): StorefrontPaymentInitiation | null {
  const item = asRecord(payload);
  if (!item) {
    return null;
  }
  const paymentId = asString(readProp(item, "paymentId", "PaymentId"));
  if (!paymentId) {
    return null;
  }
  const providerCode = asString(readProp(item, "providerCode", "ProviderCode"));
  const redirectUrl = asString(readProp(item, "redirectUrl", "RedirectUrl"));
  const isWallet = providerCode.toLowerCase() === WALLET_PROVIDER_CODE;
  if (!redirectUrl && !isWallet) {
    return null;
  }
  return {
    paymentId,
    attemptId: asString(readProp(item, "attemptId", "AttemptId")),
    checkoutId: asString(readProp(item, "checkoutId", "CheckoutId")),
    status: asString(readProp(item, "status", "Status")),
    providerCode,
    providerRequestReference: asString(readProp(item, "providerRequestReference", "ProviderRequestReference")),
    redirectUrl,
    amount: asNumber(readProp(item, "amount", "Amount")),
    currency: asString(readProp(item, "currency", "Currency"), "IRR"),
  };
}

/**
 * نقل‌قول کیف‌پول Host را نگاشت می‌کند. موجودی را UI محاسبه نمی‌کند.
 */
export function mapStorefrontWalletQuote(payload: unknown): StorefrontWalletQuote | null {
  const item = asRecord(payload);
  if (!item) {
    return null;
  }
  const cartId = asString(readProp(item, "cartId", "CartId"));
  if (!cartId) {
    return null;
  }
  const checkoutRaw = readProp(item, "checkoutId", "CheckoutId");
  const mixedRaw = readProp(item, "mixedTenderAvailable", "MixedTenderAvailable");
  return {
    checkoutId: checkoutRaw == null || checkoutRaw === "" ? null : asString(checkoutRaw),
    cartId,
    currency: asString(readProp(item, "currency", "Currency"), "IRR"),
    balance: asNumber(readProp(item, "balance", "Balance")),
    maxUsableAmount: asNumber(readProp(item, "maxUsableAmount", "MaxUsableAmount")),
    selectedWalletAmount: asNumber(readProp(item, "selectedWalletAmount", "SelectedWalletAmount")),
    remainingPayable: asNumber(readProp(item, "remainingPayable", "RemainingPayable")),
    payableAmount: asNumber(readProp(item, "payableAmount", "PayableAmount")),
    canPayFullyWithWallet: Boolean(readProp(item, "canPayFullyWithWallet", "CanPayFullyWithWallet")),
    mixedTenderAvailable: typeof mixedRaw === "boolean" ? mixedRaw : false,
  };
}

/**
 * آیا شروع پرداخت نیاز به redirect درگاه/سندباکس دارد؟
 * wallet کامل و وضعیت Succeeded → بدون redirect.
 */
export function requiresProviderRedirect(initiation: StorefrontPaymentInitiation): boolean {
  const provider = initiation.providerCode.trim().toLowerCase();
  if (provider === WALLET_PROVIDER_CODE) {
    return false;
  }
  if (initiation.status === "Succeeded") {
    return false;
  }
  return initiation.redirectUrl.trim().length > 0;
}

function storefrontActorHeaders(version?: number): Record<string, string> {
  const headers: Record<string, string> = { ...(cartHeaders(version) as Record<string, string>) };
  if (typeof window !== "undefined") {
    const stored = window.localStorage.getItem("tooba.customerActorUserId");
    headers[CUSTOMER_DEV_ACTOR_HEADER] = stored || DEFAULT_CUSTOMER_DEV_ACTOR_ID;
  }
  return headers;
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
      case "payment.wallet.insufficient":
        return "موجودی کیف پول برای پرداخت کامل کافی نیست.";
      case "payment.wallet.unavailable":
        return "پرداخت با کیف پول در حال حاضر در دسترس نیست.";
      case "wallet.quote.missing":
        return "اطلاعات کیف پول برای این سفارش در دسترس نیست.";
      default:
        return error.detail && !/Held|GATEWAY_|Verify/i.test(error.detail)
          ? error.detail
          : "امکان شروع پرداخت در حال حاضر وجود ندارد.";
    }
  }
  return toCustomerCartMessage(error);
}

/**
 * نقل‌قول کیف‌پول را از Host می‌خواند (endpoint بک‌اند).
 */
export async function loadStorefrontWalletQuote(checkoutId: string): Promise<StorefrontWalletQuote | null> {
  const session = readCartSession();
  if (!session.cartId) {
    return null;
  }
  try {
    const response = await fetch(
      `/v1/storefront/checkout/${encodeURIComponent(checkoutId)}/wallet-quote?cartId=${encodeURIComponent(session.cartId)}`,
      { cache: "no-store", headers: storefrontActorHeaders() },
    );
    if (response.status === 401 || response.status === 404) {
      return null;
    }
    const payload = await parseJson(response);
    if (!response.ok) {
      return null;
    }
    return mapStorefrontWalletQuote(payload);
  } catch {
    return null;
  }
}

/**
 * پرداخت سفارش PendingPayment را از Host شروع می‌کند. مبلغ در بدنه نیست.
 * providerCode=wallet برای پرداخت کامل کیف‌پول؛ در غیر این صورت درگاه پیش‌فرض Host.
 */
export async function startStorefrontPayment(
  checkoutId: string,
  options?: { providerCode?: string },
): Promise<StorefrontPaymentInitiation> {
  const session = readCartSession();
  if (!session.cartId) {
    throw new StorefrontCartApiError(401, "payment.guest.invalid", "سبد برای شروع پرداخت پیدا نشد.");
  }
  const providerCode = options?.providerCode?.trim();
  const body: Record<string, string | boolean> = {
    cartId: session.cartId,
    idempotencyKey: paymentIdempotencyKey(checkoutId),
  };
  if (providerCode) {
    body.providerCode = providerCode;
    if (providerCode.toLowerCase() === WALLET_PROVIDER_CODE) {
      body.useWallet = true;
    }
  }
  const response = await fetch(`/v1/storefront/checkout/${encodeURIComponent(checkoutId)}/payments`, {
    method: "POST",
    cache: "no-store",
    headers: providerCode?.toLowerCase() === WALLET_PROVIDER_CODE ? storefrontActorHeaders() : cartHeaders(),
    body: JSON.stringify(body),
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
