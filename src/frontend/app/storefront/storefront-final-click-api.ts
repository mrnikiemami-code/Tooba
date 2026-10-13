import {
  cartHeaders,
  clearCartSession,
  COMMITTED_PROOF_STORE_KEY,
  readCartSession,
  StorefrontCartApiError,
  writeCommittedCheckoutProof,
  writePaymentResultProof,
} from "./storefront-cart-api.ts";
import { customerAuthHeaders } from "../customer-panel/customer-api.ts";
import {
  mapStorefrontCheckout,
  readStoredCouponCode,
  toCustomerCheckoutMessage,
  type StorefrontCheckoutPage,
} from "./storefront-checkout-api.ts";
import {
  mapStorefrontPaymentInitiation,
  toCustomerPaymentMessage,
  type StorefrontPaymentInitiation,
  type StorefrontPaymentMethodId,
} from "./storefront-payment-api.ts";
import { type Locale } from "../../lib/i18n/locale.ts";

/**
 * «کلیک نهایی پرداخت» با آدرس سبد (Order 028) — مسیر واحد پول‌محور بدون commit زودهنگام ارسال.
 *
 * قرارداد مبلغ/موجودی/سیاست مهلت در بک‌اند است: این ماژول هیچ مبلغ، شناسهٔ سفارش، شناسهٔ کاربر یا
 * مهلتی نمی‌سازد و در درخواست نمی‌فرستد. شناسهٔ سبد فقط از مسیر، نسخهٔ مورد انتظار سبد از پیش‌نویس
 * خوانده‌شده، و گواه مهمان از هدر/کوکی متعارف می‌آید.
 */
const FINAL_CLICK_IDEMPOTENCY_KEY = "tooba.storefront.finalClickIdempotency";

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : null;
}

function readProp(record: Record<string, unknown>, ...names: string[]): unknown {
  for (const name of names) {
    if (Object.prototype.hasOwnProperty.call(record, name) && record[name] !== undefined) {
      return record[name];
    }
  }
  return undefined;
}

function asString(value: unknown, fallback = ""): string {
  return value == null ? fallback : String(value);
}

/**
 * پیش‌نویس خواندنی پرداخت برای سبد Active.
 *
 * مسیر: GET /v1/storefront/cart/{cartId}/payment-draft (Order 011). پاسخ همان قرارداد نقل‌قول متعارف
 * با checkoutId=null است؛ بنابراین هنوز سفارشی ساخته نشده و نه رزروی می‌شود و نه تبدیلی. مبلغ و روش ارسال
 * نمایش‌داده‌شده همیشه از همین پاسخ خام می‌آید (هیچ نقل‌قول جعلی ساخته نمی‌شود).
 */
export async function loadCartPaymentDraft(
  cartId: string,
  couponCode?: string | null,
): Promise<StorefrontCheckoutPage> {
  const query = new URLSearchParams();
  const code = couponCode?.trim() || readStoredCouponCode();
  if (code) query.set("couponCode", code);
  const suffix = query.size > 0 ? `?${query.toString()}` : "";
  const response = await fetch(`/v1/storefront/cart/${encodeURIComponent(cartId)}/payment-draft${suffix}`, {
    method: "GET",
    cache: "no-store",
    headers: {
      ...cartHeaders(),
      ...customerAuthHeaders(),
    },
  });
  const payload: unknown = await response.json().catch(() => null);
  if (!response.ok) {
    const record = asRecord(payload);
    throw new StorefrontCartApiError(
      response.status,
      record ? asString(readProp(record, "errorCode", "ErrorCode")) || null : null,
      record ? asString(readProp(record, "detail", "Detail")) || null : null,
      { checkoutId: readProblemCheckoutId(record) },
    );
  }
  const page = mapStorefrontCheckout(payload);
  if (!page) {
    throw new StorefrontCartApiError(500, "checkout.invalid", "پاسخ پیش‌نویس پرداخت نامعتبر است.");
  }
  return page;
}

/** شناسهٔ سفارشِ نگه‌داشته‌شده در ProblemDetails برای بازیابی همان سفارش پس از شکست پس از commit. */
function readProblemCheckoutId(record: Record<string, unknown> | null): string | null {
  if (!record) return null;
  const raw = readProp(record, "checkoutId", "CheckoutId");
  return raw == null ? null : asString(raw) || null;
}

/**
 * کلید idempotency پایدار برای «همان عملیات»: تا زمانی که سبد یا روش پرداخت عوض نشود، هر رندر/تلاش
 * دوبارهٔ React همان کلید را می‌فرستد و کلیک دوم همان سفارش/پرداخت را replay می‌کند. تغییر روش پرداخت
 * (یا سبد) یک قصد/تلاش متمایز است و کلید خودش را دارد.
 */
function finalClickIdempotencyKey(cartId: string, providerCode: string): string {
  const provider = providerCode.trim().toLowerCase() || "gateway";
  const scoped = `${FINAL_CLICK_IDEMPOTENCY_KEY}.${cartId}.${provider}`;
  if (typeof window === "undefined") return crypto.randomUUID();
  const existing = window.sessionStorage.getItem(scoped);
  if (existing) return existing;
  const created = crypto.randomUUID();
  window.sessionStorage.setItem(scoped, created);
  return created;
}

function providerCodeFor(method: StorefrontPaymentMethodId, explicit?: string | null): string {
  const trimmed = explicit?.trim();
  if (trimmed) return trimmed;
  return method === "wallet" ? "wallet" : method === "manual" ? "manual" : "gateway";
}

/**
 * پاسخ 028 وضعیت را در paymentStatus می‌دهد؛ قرارداد متعارف شروع پرداخت آن را status می‌نامد.
 * نرمال‌سازی فقط نام را یکسان می‌کند و هیچ مقداری را جعل/تخمین نمی‌زند.
 */
function normalizeInitiationPayload(payload: unknown): unknown {
  const record = asRecord(payload);
  if (!record) return payload;
  return {
    ...record,
    status: asString(readProp(record, "status", "Status")) || asString(readProp(record, "paymentStatus", "PaymentStatus")),
  };
}

export interface StorefrontFinalClickInput {
  cartId: string;
  expectedCartVersion: number;
  method: StorefrontPaymentMethodId;
  providerCode?: string | null;
  couponCode?: string | null;
}

/**
 * کلیک نهایی: تنها نقطهٔ شروع پرداخت روی سبد Active.
 *
 * مسیر: POST /v1/storefront/cart/{cartId}/start-payment (Order 028). بدنه فقط نسخهٔ سبد + کلید
 * idempotency + انتخاب روش + کد تخفیف ذخیره‌شده است؛ مبلغ/شناسهٔ سفارش/شناسهٔ کاربر/مهلت از مرورگر نمی‌آید.
 */
export async function startFinalClickPayment(
  input: StorefrontFinalClickInput,
): Promise<StorefrontPaymentInitiation> {
  const providerCode = providerCodeFor(input.method, input.providerCode);
  const coupon = input.couponCode?.trim() || readStoredCouponCode();
  const response = await fetch(`/v1/storefront/cart/${encodeURIComponent(input.cartId)}/start-payment`, {
    method: "POST",
    cache: "no-store",
    headers: {
      "content-type": "application/json",
      ...cartHeaders(),
      ...customerAuthHeaders(),
    },
    body: JSON.stringify({
      expectedCartVersion: input.expectedCartVersion,
      idempotencyKey: finalClickIdempotencyKey(input.cartId, providerCode),
      useWallet: input.method === "wallet",
      providerCode,
      couponCode: coupon || null,
    }),
  });
  const payload: unknown = await response.json().catch(() => null);
  if (!response.ok) {
    const problemCheckoutId = readProblemCheckoutId(asRecord(payload));
    if (problemCheckoutId) {
      // شکست پس از commit: سفارش پایدار است. اثبات همان سفارش نگه داشته می‌شود تا UI بتواند روی همان
      // سفارش بازیابی کند؛ اشاره‌گر سبد دست‌نخورده می‌ماند تا مسیر «بازگشت به سبد» هم ممکن بماند.
      persistCommittedProof(problemCheckoutId, input.cartId);
    }
    const record = asRecord(payload);
    throw new StorefrontCartApiError(
      response.status,
      record ? asString(readProp(record, "errorCode", "ErrorCode")) || null : null,
      record ? asString(readProp(record, "detail", "Detail")) || null : null,
      { checkoutId: problemCheckoutId },
    );
  }
  const initiation = mapStorefrontPaymentInitiation(normalizeInitiationPayload(payload));
  if (!initiation) {
    throw new StorefrontCartApiError(500, "payment.rejected", "پاسخ شروع پرداخت نامعتبر بود.");
  }
  if (initiation.checkoutId) {
    persistFinalClickSuccess(initiation, input.cartId);
  }
  return initiation;
}

/** اثبات سفارش متعهد را برای بازیابی همان سفارش می‌نویسد (بدون دست‌زدن به اشاره‌گر سبد). */
function persistCommittedProof(checkoutId: string, sourceCartId: string): void {
  writeCommittedCheckoutProof({
    checkoutId,
    cartId: sourceCartId,
    guestSecret: readCartSession().guestSecret ?? "",
    storeKey: COMMITTED_PROOF_STORE_KEY,
  });
}

/**
 * پس از شروع موفق: اثبات سفارش + اثبات نتیجهٔ پرداخت نوشته می‌شود و اشاره‌گر سبد فعال جدا می‌گردد
 * (سبد مصرف شده است). صفحهٔ نتیجه/بازیابی از همین اثبات تغذیه می‌شود، نه از سبد فعال جدید.
 */
function persistFinalClickSuccess(initiation: StorefrontPaymentInitiation, sourceCartId: string): void {
  const guestSecret = readCartSession().guestSecret ?? "";
  persistCommittedProof(initiation.checkoutId as string, sourceCartId);
  writePaymentResultProof({
    paymentId: initiation.paymentId,
    checkoutId: initiation.checkoutId,
    cartId: sourceCartId,
    guestSecret,
  });
  clearCartSession();
}

/**
 * پیام مشتری برای شکست کلیک نهایی. هیچ کد فنی و هیچ ادعای «تمدید خودکار» ساخته نمی‌شود:
 * برای مهلت رزرو ازدست‌رفته فقط مسیر عملی «بازگشت به سبد» پیشنهاد می‌شود.
 */
export function toCustomerFinalClickMessage(error: unknown, locale: Locale = "fa"): string {
  const fa = locale === "fa";
  if (error instanceof StorefrontCartApiError) {
    switch (error.errorCode) {
      case "checkout.open_unpaid_limit_reached":
      case "checkout.reservation_commit_limit_reached":
      case "checkout.price.changed":
        return toCustomerCheckoutMessage(error, locale);
      case "payment.reservation.not_payable":
        return fa
          ? "مهلت نگه‌داری موجودی این سفارش دیگر قابل شروع نیست. به سبد خرید برگردید، سفارش را بازبینی کنید و دوباره تلاش کنید."
          : "The stock hold for this order can no longer be started. Return to the cart, review it, then try again.";
      case "payment.wallet.mixed_deferred":
        // نقل‌قول کیف‌پول بین پیش‌نمایش و کلیک نهایی تغییر کرده است (سرور همچنان authoritative است و هیچ
        // برداشتی رخ نداده). پیام فقط مسیر عملی «بازگشت به سبد/انتخاب روش دیگر» را پیشنهاد می‌کند.
        return fa
          ? "موجودی کیف پول برای پرداخت کامل این سبد کافی نیست یا تغییر کرده است. به سبد خرید برگردید و روش دیگری انتخاب کنید یا موجودی را بررسی کنید."
          : "Your wallet balance no longer covers this cart in full. Return to the cart and choose another method or review your balance.";
      case "payment.wallet.insufficient":
        return fa
          ? "موجودی کیف پول برای پرداخت کامل این سبد کافی نیست. روش دیگری انتخاب کنید."
          : "Your wallet balance is not enough to pay for this cart in full. Choose another method.";
      case "checkout.inventory.unavailable":
        return fa
          ? "موجودی یکی از کالاها در دسترس نیست. به سبد خرید برگردید و سبد را بازبینی کنید."
          : "One of the items is no longer available. Return to the cart and review it.";
      case "checkout.cart.missing":
      case "shipping.cart.missing":
        return fa
          ? "سبد خرید پیدا نشد. لطفاً دوباره از سبد شروع کنید."
          : "The cart was not found. Please restart from the cart.";
      case "checkout.cart.expired":
      case "cart.expired":
        return fa
          ? "سبد خرید منقضی شده است. لطفاً دوباره از سبد شروع کنید."
          : "The cart has expired. Please restart from the cart.";
      case "shipping.selection.required":
        return fa
          ? "اطلاعات ارسال این سبد ذخیره نشده است. مرحلهٔ ارسال را دوباره تکمیل کنید."
          : "Shipping details for this cart were not saved. Complete the shipping step again.";
      case "checkout.cart.empty":
        return fa ? "سبد خرید خالی است." : "The cart is empty.";
      case "checkout.version.conflict":
      case "cart.version.conflict":
        return fa
          ? "سبد خرید شما هم‌زمان در جای دیگری تغییر کرده است. برای دیدن مبالغ به‌روز به سبد برگردید و دوباره تلاش کنید."
          : "Your cart changed elsewhere. Return to the cart to see updated amounts, then try again.";
      case "checkout.access.denied":
      case "payment.access.denied":
        return fa
          ? "دسترسی به اطلاعات پرداخت این سبد تأیید نشد. لطفاً دوباره از بخش سفارش‌های خود وارد پرداخت شوید."
          : "Access to this cart payment was not confirmed. Re-enter payment from your orders.";
      default:
        return toCustomerPaymentMessage(error);
    }
  }
  return toCustomerPaymentMessage(error);
}
