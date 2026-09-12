import {
  CUSTOMER_DEV_ACTOR_HEADER,
  DEFAULT_CUSTOMER_DEV_ACTOR_ID,
} from "../customer-panel/customer-api.ts";
import {
  cartHeaders,
  cartHeadersFromAccess,
  persistPaymentResultProofFromAccess,
  readPaymentResultProof,
  resolveCommittedCheckoutAccess,
  resolvePaymentResultAccess,
  StorefrontCartApiError,
  toCustomerCartMessage,
  writePaymentResultProof,
} from "./storefront-cart-api.ts";

const PAYMENT_ACCESS_DENIED =
  "دسترسی به اطلاعات پرداخت این سفارش تأیید نشد. لطفاً از بخش سفارش‌ها دوباره وارد پرداخت شوید.";

const PAYMENT_IDEMPOTENCY_KEY = "tooba.storefront.paymentIdempotency";

/** کد درگاه کیف پول (پرداخت کامل؛ بدون redirect سندباکس). */
export const WALLET_PROVIDER_CODE = "wallet";

/** کد درگاه کارت‌به‌کارت/دستی (تأیید واریز ادمین). */
export const MANUAL_PROVIDER_CODE = "manual";

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
  customerTransferReference?: string | null;
  proofMediaAssetId?: string | null;
  evidenceSubmittedAt?: string | null;
  orderNumber?: string | null;
  manualProofRequirement?: string;
  manualPaymentInstructions?: string;
  canSubmitManualEvidence?: boolean;
  canRetryManual?: boolean;
  canRetryUnpaid?: boolean;
  evidenceHistory?: Array<{
    attemptId: string;
    attemptStatus: string;
    customerTransferReference?: string | null;
    proofMediaAssetId?: string | null;
    evidenceSubmittedAt?: string | null;
    failureCode?: string | null;
  }>;
}

export interface StorefrontSandboxContext {
  paymentId: string;
  checkoutId: string;
  storeName: string;
  orderNumber: string;
  amount: number;
  currency: string;
  providerLabel: string;
  sandbox: boolean;
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
  manualCardToCardEnabled: boolean;
}

export type StorefrontPaymentMethodId = "gateway" | "wallet" | "manual";

export interface StorefrontPaymentMethodsPage {
  methods: Array<{ code: string; labelFa: string; descriptionFa: string }>;
  manualCardToCardEnabled: boolean;
  manualProofRequirement?: string;
  manualPaymentInstructions?: string;
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
  const isManual = providerCode.toLowerCase() === MANUAL_PROVIDER_CODE;
  if (!redirectUrl && !isWallet && !isManual) {
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
  const checkoutRaw = readProp(item, "checkoutId", "CheckoutId");
  const checkoutId = checkoutRaw == null || checkoutRaw === "" ? null : asString(checkoutRaw);
  if (!checkoutId) {
    return null;
  }
  const cartFromPayload = asString(readProp(item, "cartId", "CartId"));
  const cartId = cartFromPayload;
  const mixedRaw = readProp(item, "mixedTenderAvailable", "MixedTenderAvailable");
  const balanceRaw = readProp(item, "walletBalance", "WalletBalance");
  const maxUsableRaw = readProp(item, "maxUsable", "MaxUsable");
  return {
    checkoutId,
    cartId,
    currency: asString(readProp(item, "currency", "Currency"), "IRR"),
    balance: asNumber(balanceRaw ?? readProp(item, "balance", "Balance")),
    maxUsableAmount: asNumber(maxUsableRaw ?? readProp(item, "maxUsableAmount", "MaxUsableAmount")),
    selectedWalletAmount: asNumber(readProp(item, "selectedWalletAmount", "SelectedWalletAmount")),
    remainingPayable: asNumber(readProp(item, "remainingPayable", "RemainingPayable")),
    payableAmount: asNumber(readProp(item, "payableAmount", "PayableAmount")),
    canPayFullyWithWallet: Boolean(readProp(item, "canPayFullyWithWallet", "CanPayFullyWithWallet")),
    mixedTenderAvailable: typeof mixedRaw === "boolean" ? mixedRaw : false,
    manualCardToCardEnabled: Boolean(readProp(item, "manualCardToCardEnabled", "ManualCardToCardEnabled")),
  };
}

/**
 * روش‌های پرداخت فعال فروشگاه را از Host می‌خواند.
 */
export async function loadStorefrontPaymentMethods(): Promise<StorefrontPaymentMethodsPage> {
  const response = await fetch("/v1/storefront/payment-methods", { cache: "no-store" });
  const payload = await parseJson(response);
  throwIfFailed(response, payload, "payment.methods.missing");
  const item = asRecord(payload);
  const methodsRaw = readProp(item ?? {}, "methods", "Methods");
  const methods = Array.isArray(methodsRaw)
    ? methodsRaw
        .map((row) => {
          const r = asRecord(row);
          if (!r) return null;
          return {
            code: asString(readProp(r, "code", "Code")),
            labelFa: asString(readProp(r, "labelFa", "LabelFa")),
            descriptionFa: asString(readProp(r, "descriptionFa", "DescriptionFa")),
          };
        })
        .filter((x): x is { code: string; labelFa: string; descriptionFa: string } => !!x?.code)
    : [];
  return {
    methods,
    manualCardToCardEnabled: Boolean(readProp(item ?? {}, "manualCardToCardEnabled", "ManualCardToCardEnabled")),
    manualProofRequirement: asString(readProp(item ?? {}, "manualProofRequirement", "ManualProofRequirement"), "Optional"),
    manualPaymentInstructions: asString(readProp(item ?? {}, "manualPaymentInstructions", "ManualPaymentInstructions")),
  };
}

/**
 * آیا شروع پرداخت نیاز به redirect درگاه/سندباکس دارد؟
 * wallet کامل و وضعیت Succeeded → بدون redirect.
 */
export function requiresProviderRedirect(initiation: StorefrontPaymentInitiation): boolean {
  const provider = initiation.providerCode.trim().toLowerCase();
  if (provider === WALLET_PROVIDER_CODE || provider === MANUAL_PROVIDER_CODE) {
    return false;
  }
  if (initiation.status === "Succeeded") {
    return false;
  }
  return initiation.redirectUrl.trim().length > 0;
}

function storefrontActorHeaders(
  access?: { cartId: string | null; guestSecret: string | null },
  version?: number,
): Record<string, string> {
  const headers: Record<string, string> = {
    ...((access ? cartHeadersFromAccess(access, version) : cartHeaders(version)) as Record<string, string>),
  };
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
    customerTransferReference: asString(readProp(item, "customerTransferReference", "CustomerTransferReference")) || null,
    proofMediaAssetId: asString(readProp(item, "proofMediaAssetId", "ProofMediaAssetId")) || null,
    evidenceSubmittedAt: asString(readProp(item, "evidenceSubmittedAt", "EvidenceSubmittedAt")) || null,
    orderNumber: asString(readProp(item, "orderNumber", "OrderNumber")) || null,
    manualProofRequirement: asString(readProp(item, "manualProofRequirement", "ManualProofRequirement"), "Optional"),
    manualPaymentInstructions: asString(readProp(item, "manualPaymentInstructions", "ManualPaymentInstructions")),
    canSubmitManualEvidence: Boolean(readProp(item, "canSubmitManualEvidence", "CanSubmitManualEvidence")),
    canRetryManual: Boolean(readProp(item, "canRetryManual", "CanRetryManual")),
    canRetryUnpaid: Boolean(readProp(item, "canRetryUnpaid", "CanRetryUnpaid")),
  };
}

export function resetStorefrontPaymentIdempotency(checkoutId: string, providerCode: string): void {
  const provider = providerCode.trim().toLowerCase() || "gateway";
  window.sessionStorage.removeItem(`${PAYMENT_IDEMPOTENCY_KEY}.${checkoutId}.${provider}`);
}

/**
 * کلید idempotency را به‌ازای checkout + روش پرداخت نگه می‌دارد.
 * تعویض gateway ↔ manual ↔ wallet نباید پرداخت قبلی را replay کند.
 */
function paymentIdempotencyKey(checkoutId: string, providerCode: string): string {
  const provider = providerCode.trim().toLowerCase() || "gateway";
  const scoped = `${PAYMENT_IDEMPOTENCY_KEY}.${checkoutId}.${provider}`;
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
      case "payment.already_succeeded":
        return "پرداخت این سفارش قبلاً با موفقیت انجام شده است.";
      case "payment.missing":
        return "پرداخت پیدا نشد.";
      case "payment.guest.invalid":
        return "دسترسی به پرداخت معتبر نیست.";
      case "payment.access.denied":
      case "checkout.access.denied":
        return PAYMENT_ACCESS_DENIED;
      case "payment.wallet.insufficient":
        return "موجودی کیف پول برای پرداخت کامل کافی نیست.";
      case "payment.wallet.unavailable":
        return "پرداخت با کیف پول در حال حاضر در دسترس نیست.";
      case "payment.method.unavailable":
        return "این روش پرداخت برای فروشگاه فعال نیست.";
      case "payment.tracking.required":
        return "شماره پیگیری پرداخت الزامی است.";
      case "payment.proof.required":
        return "بارگذاری مدرک پرداخت الزامی است.";
      case "payment.proof.foreign":
        return "مدرک پرداخت معتبر نیست.";
      case "payment.sandbox.unavailable":
        return "درگاه آزمایشی در این محیط در دسترس نیست.";
      case "payment.unpaid.supply_unavailable":
        return "این سفارش در حال حاضر قابل تأمین نیست.";
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
  const access = resolveCommittedCheckoutAccess(checkoutId);
  if (!access.cartId) {
    return null;
  }
  try {
    const response = await fetch(
      `/v1/storefront/checkout/${encodeURIComponent(checkoutId)}/wallet-quote?cartId=${encodeURIComponent(access.cartId)}`,
      { cache: "no-store", headers: storefrontActorHeaders(access) },
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
  const access = resolveCommittedCheckoutAccess(checkoutId);
  if (!access.cartId) {
    throw new StorefrontCartApiError(403, "payment.access.denied", PAYMENT_ACCESS_DENIED);
  }
  const providerCode = options?.providerCode?.trim() || "gateway";
  const body: Record<string, string | boolean> = {
    cartId: access.cartId,
    idempotencyKey: paymentIdempotencyKey(checkoutId, providerCode),
    providerCode,
  };
  if (providerCode.toLowerCase() === WALLET_PROVIDER_CODE) {
    body.useWallet = true;
  }
  const headers =
    providerCode.toLowerCase() === WALLET_PROVIDER_CODE
      ? storefrontActorHeaders(access)
      : (cartHeadersFromAccess(access) as Record<string, string>);
  const response = await fetch(`/v1/storefront/checkout/${encodeURIComponent(checkoutId)}/payments`, {
    method: "POST",
    cache: "no-store",
    headers,
    body: JSON.stringify(body),
  });
  const payload = await parseJson(response);
  throwIfFailed(response, payload, "payment.rejected");
  const mapped = mapStorefrontPaymentInitiation(payload);
  if (!mapped) {
    throw new StorefrontCartApiError(500, "payment.rejected", "پاسخ شروع پرداخت نامعتبر بود.");
  }
  persistPaymentResultProofFromAccess(mapped, access);
  return mapped;
}

/**
 * تصویر پرداخت را از Host می‌خواند.
 * پس از نهایی‌شدن سبد، از اثبات نتیجهٔ پرداخت (cart متعهد + guest secret) استفاده می‌کند نه سبد فعال جدید.
 */
export async function loadStorefrontPayment(paymentId: string, checkoutId?: string | null): Promise<StorefrontPaymentPage> {
  const access = checkoutId
    ? resolveCommittedCheckoutAccess(checkoutId, paymentId)
    : resolvePaymentResultAccess(paymentId);
  if (!access.cartId) {
    throw new StorefrontCartApiError(403, "payment.access.denied", PAYMENT_ACCESS_DENIED);
  }
  const response = await fetch(
    `/v1/storefront/payments/${encodeURIComponent(paymentId)}?cartId=${encodeURIComponent(access.cartId)}`,
    { cache: "no-store", headers: cartHeadersFromAccess(access) },
  );
  const payload = await parseJson(response);
  throwIfFailed(response, payload, "payment.missing");
  const mapped = mapStorefrontPayment(payload);
  if (!mapped) {
    throw new StorefrontCartApiError(500, "payment.missing", "پاسخ پرداخت نامعتبر بود.");
  }
  persistPaymentResultProofFromAccess(mapped, access);
  return mapped;
}

/** آیا وضعیت پرداخت هنوز به‌صورت ناهمگام ممکن است عوض شود و نیاز به poll دارد. */
export function shouldPollStorefrontPayment(payment: StorefrontPaymentPage | null | undefined): boolean {
  if (!payment) {
    return false;
  }
  const status = (payment.status ?? "").toLowerCase();
  if (status === "succeeded" || status === "failed" || status === "cancelled" || status === "expired") {
    return false;
  }
  const manual = (payment.providerCode ?? "").toLowerCase() === "manual";
  if (manual) {
    // AwaitingAdmin یا منتظر فرم مشتری — Admin/کاربر ممکن است ساعت‌ها بعد عمل کند.
    if (payment.evidenceSubmittedAt || payment.canSubmitManualEvidence) {
      return false;
    }
  }
  // Pending آنلاین / در حال Verify
  return status === "pending" || status === "processing" || status === "verifying";
}

export { readPaymentResultProof, writePaymentResultProof, resolvePaymentResultAccess };

/**
 * تکمیل sandbox/dev. موفقیت را UI اعلام نمی‌کند؛ Host Verify می‌کند.
 */
export async function completeStorefrontSandboxPayment(
  paymentId: string,
  attemptId: string,
  providerRequestReference: string,
  outcome: "success" | "failure",
): Promise<StorefrontPaymentPage> {
  const access = resolvePaymentResultAccess(paymentId);
  if (!access.cartId) {
    throw new StorefrontCartApiError(403, "payment.access.denied", PAYMENT_ACCESS_DENIED);
  }
  const response = await fetch(`/v1/storefront/payments/${encodeURIComponent(paymentId)}/sandbox/complete`, {
    method: "POST",
    cache: "no-store",
    headers: cartHeadersFromAccess(access),
    body: JSON.stringify({
      cartId: access.cartId,
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
  persistPaymentResultProofFromAccess(mapped, access);
  return mapped;
}

export async function loadStorefrontSandboxContext(paymentId: string): Promise<StorefrontSandboxContext> {
  const access = resolvePaymentResultAccess(paymentId);
  if (!access.cartId) {
    throw new StorefrontCartApiError(403, "payment.access.denied", PAYMENT_ACCESS_DENIED);
  }
  const headers = cartHeadersFromAccess(access) as Record<string, string>;
  const response = await fetch(
    `/v1/storefront/payments/${encodeURIComponent(paymentId)}/sandbox?cartId=${encodeURIComponent(access.cartId)}`,
    { cache: "no-store", headers },
  );
  const payload = await parseJson(response);
  throwIfFailed(response, payload, "payment.sandbox.unavailable");
  const item = asRecord(payload);
  if (!item) {
    throw new StorefrontCartApiError(500, "payment.missing", "پاسخ درگاه نامعتبر بود.");
  }
  return {
    paymentId: asString(readProp(item, "paymentId", "PaymentId")),
    checkoutId: asString(readProp(item, "checkoutId", "CheckoutId")),
    storeName: asString(readProp(item, "storeName", "StoreName"), "Tooba"),
    orderNumber: asString(readProp(item, "orderNumber", "OrderNumber")),
    amount: asNumber(readProp(item, "amount", "Amount")),
    currency: asString(readProp(item, "currency", "Currency"), "IRR"),
    providerLabel: asString(readProp(item, "providerLabel", "ProviderLabel"), "درگاه بانکی"),
    sandbox: Boolean(readProp(item, "sandbox", "Sandbox")),
  };
}

export async function submitStorefrontManualEvidence(
  paymentId: string,
  transferReference: string,
  proofMediaAssetId?: string | null,
): Promise<StorefrontPaymentPage> {
  const access = resolvePaymentResultAccess(paymentId);
  if (!access.cartId) {
    throw new StorefrontCartApiError(403, "payment.access.denied", PAYMENT_ACCESS_DENIED);
  }
  const response = await fetch(`/v1/storefront/payments/${encodeURIComponent(paymentId)}/manual-evidence`, {
    method: "POST",
    cache: "no-store",
    headers: cartHeadersFromAccess(access),
    body: JSON.stringify({
      cartId: access.cartId,
      transferReference,
      proofMediaAssetId: proofMediaAssetId || null,
    }),
  });
  const payload = await parseJson(response);
  throwIfFailed(response, payload, "payment.rejected");
  const mappedEvidence = mapStorefrontPayment(payload);
  if (!mappedEvidence) {
    throw new StorefrontCartApiError(500, "payment.rejected", "پاسخ ثبت پرداخت نامعتبر بود.");
  }
  persistPaymentResultProofFromAccess(mappedEvidence, access);
  return mappedEvidence;
}

export async function retryStorefrontManualPayment(paymentId: string): Promise<StorefrontPaymentPage> {
  const access = resolvePaymentResultAccess(paymentId);
  if (!access.cartId) {
    throw new StorefrontCartApiError(403, "payment.access.denied", PAYMENT_ACCESS_DENIED);
  }
  const response = await fetch(`/v1/storefront/payments/${encodeURIComponent(paymentId)}/manual-retry`, {
    method: "POST",
    cache: "no-store",
    headers: cartHeadersFromAccess(access),
    body: JSON.stringify({ cartId: access.cartId }),
  });
  const payload = await parseJson(response);
  throwIfFailed(response, payload, "payment.rejected");
  const mappedRetry = mapStorefrontPayment(payload);
  if (!mappedRetry) {
    throw new StorefrontCartApiError(500, "payment.rejected", "پاسخ تلاش مجدد نامعتبر بود.");
  }
  return mappedRetry;
}

export async function retryStorefrontUnpaidPayment(paymentId: string): Promise<StorefrontPaymentPage> {
  const access = resolvePaymentResultAccess(paymentId);
  if (!access.cartId) {
    throw new StorefrontCartApiError(403, "payment.access.denied", PAYMENT_ACCESS_DENIED);
  }
  const response = await fetch(`/v1/storefront/payments/${encodeURIComponent(paymentId)}/unpaid-retry`, {
    method: "POST",
    cache: "no-store",
    headers: cartHeadersFromAccess(access),
    body: JSON.stringify({ cartId: access.cartId }),
  });
  const payload = await parseJson(response);
  throwIfFailed(response, payload, "payment.unpaid.supply_unavailable");
  const mappedRetry = mapStorefrontPayment(payload);
  if (!mappedRetry) {
    throw new StorefrontCartApiError(500, "payment.rejected", "پاسخ تلاش مجدد نامعتبر بود.");
  }
  return mappedRetry;
}

export async function uploadStorefrontManualProof(paymentId: string, file: File): Promise<string> {
  const access = resolvePaymentResultAccess(paymentId);
  if (!access.cartId) {
    throw new StorefrontCartApiError(403, "payment.access.denied", PAYMENT_ACCESS_DENIED);
  }
  const form = new FormData();
  form.append("file", file);
  const headers = cartHeadersFromAccess(access) as Record<string, string>;
  delete headers["content-type"];
  const response = await fetch(
    `/v1/storefront/payments/${encodeURIComponent(paymentId)}/proof?cartId=${encodeURIComponent(access.cartId)}`,
    { method: "POST", cache: "no-store", headers, body: form },
  );
  const payload = await parseJson(response);
  throwIfFailed(response, payload, "payment.proof.required");
  const item = asRecord(payload);
  const id = asString(readProp(item ?? {}, "mediaAssetId", "MediaAssetId"));
  if (!id) {
    throw new StorefrontCartApiError(500, "payment.proof.required", "بارگذاری مدرک ناموفق بود.");
  }
  return id;
}
