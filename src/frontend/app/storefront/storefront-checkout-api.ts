import { cartHeaders, readCartSession, StorefrontCartApiError, toCustomerCartMessage } from "./storefront-cart-api.ts";
import { customerAuthHeaders } from "../customer-panel/customer-api.ts";

const IDEMPOTENCY_KEY = "tooba.storefront.checkoutIdempotency";
const COUPON_KEY = "tooba.storefront.couponCode";

export function readStoredCouponCode(): string | null {
  if (typeof window === "undefined") {
    return null;
  }
  return window.sessionStorage.getItem(COUPON_KEY);
}

export function writeStoredCouponCode(code: string | null): void {
  if (typeof window === "undefined") {
    return;
  }
  if (!code || !code.trim()) {
    window.sessionStorage.removeItem(COUPON_KEY);
    return;
  }
  window.sessionStorage.setItem(COUPON_KEY, code.trim().toUpperCase());
}

export interface StorefrontCheckoutShipping {
  recipientName: string;
  contactMobile: string;
  provinceName: string;
  cityName: string;
  postalAddress: string;
  postalCode: string;
}

/**
 * بدنهٔ ارسال تسویه را می‌سازد. شناسهٔ نشانی ذخیره‌شده فقط وقتی انتخاب شده باشد می‌آید.
 * مسیر مهمان همان فیلدهای خطی است و savedAddressId ندارد.
 */
export function toCheckoutShippingBody(
  shipping: StorefrontCheckoutShipping,
  savedAddressId?: string | null,
): StorefrontCheckoutShipping & { savedAddressId?: string } {
  if (!savedAddressId) return { ...shipping };
  return { ...shipping, savedAddressId };
}

export interface StorefrontCheckoutLine {
  offerId: string;
  sellerPartyId: string;
  title: string;
  sellerDisplayName: string;
  quantity: number;
  lineExclusiveOfTax: number;
  discountAmount: number;
  taxAmount: number;
  linePayable: number;
  currency: string;
}

export interface StorefrontSellerOrder {
  sellerOrderId: string;
  orderNumber: string;
  sellerPartyId: string;
  sellerDisplayName: string;
  status: string;
  subtotalExclusiveOfTax: number;
  taxAmount: number;
  discountAmount: number;
  payableAmount: number;
  currency: string;
  lines: StorefrontCheckoutLine[];
}

export interface StorefrontCheckoutPage {
  checkoutId: string | null;
  cartId: string;
  cartVersion: number;
  market: string;
  currency: string;
  channel: string;
  paymentState: string;
  shippingMethodCode: string;
  shippingMethodLabel: string;
  recipientName: string;
  contactMobile: string;
  provinceName: string;
  cityName: string;
  postalAddress: string;
  postalCode: string;
  subtotalExclusiveOfTax: number;
  discountAmount: number;
  taxAmount: number;
  shippingAmount: number;
  payableAmount: number;
  sellerOrders: StorefrontSellerOrder[];
}

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

function asNumber(value: unknown, fallback = 0): number {
  if (typeof value === "number" && Number.isFinite(value)) {
    return value;
  }
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
}

function mapLine(row: unknown): StorefrontCheckoutLine {
  const line = asRecord(row) ?? {};
  return {
    offerId: asString(readProp(line, "offerId", "OfferId")),
    sellerPartyId: asString(readProp(line, "sellerPartyId", "SellerPartyId")),
    title: asString(readProp(line, "title", "Title"), "کالا"),
    sellerDisplayName: asString(readProp(line, "sellerDisplayName", "SellerDisplayName"), "فروشنده"),
    quantity: asNumber(readProp(line, "quantity", "Quantity"), 1),
    lineExclusiveOfTax: asNumber(readProp(line, "lineExclusiveOfTax", "LineExclusiveOfTax")),
    discountAmount: asNumber(readProp(line, "discountAmount", "DiscountAmount")),
    taxAmount: asNumber(readProp(line, "taxAmount", "TaxAmount")),
    linePayable: asNumber(readProp(line, "linePayable", "LinePayable")),
    currency: asString(readProp(line, "currency", "Currency"), "IRR"),
  };
}

export function mapStorefrontCheckout(payload: unknown): StorefrontCheckoutPage | null {
  const item = asRecord(payload);
  if (!item) {
    return null;
  }
  const cartId = asString(readProp(item, "cartId", "CartId"));
  if (!cartId) {
    return null;
  }
  const checkoutRaw = readProp(item, "checkoutId", "CheckoutId");
  const sellersRaw = readProp(item, "sellerOrders", "SellerOrders", "sellerOrders");
  return {
    checkoutId: checkoutRaw == null || checkoutRaw === "" ? null : asString(checkoutRaw),
    cartId,
    cartVersion: asNumber(readProp(item, "cartVersion", "CartVersion")),
    market: asString(readProp(item, "market", "Market"), "IR"),
    currency: asString(readProp(item, "currency", "Currency"), "IRR"),
    channel: asString(readProp(item, "channel", "Channel")),
    paymentState: asString(readProp(item, "paymentState", "PaymentState"), "PendingPayment"),
    shippingMethodCode: asString(readProp(item, "shippingMethodCode", "ShippingMethodCode")),
    shippingMethodLabel: asString(readProp(item, "shippingMethodLabel", "ShippingMethodLabel")),
    recipientName: asString(readProp(item, "recipientName", "RecipientName")),
    contactMobile: asString(readProp(item, "contactMobile", "ContactMobile", "contactMobile")),
    provinceName: asString(readProp(item, "provinceName", "ProvinceName")),
    cityName: asString(readProp(item, "cityName", "CityName")),
    postalAddress: asString(readProp(item, "postalAddress", "PostalAddress", "postalAddress")),
    postalCode: asString(readProp(item, "postalCode", "PostalCode")),
    subtotalExclusiveOfTax: asNumber(readProp(item, "subtotalExclusiveOfTax", "SubtotalExclusiveOfTax")),
    discountAmount: asNumber(readProp(item, "discountAmount", "DiscountAmount")),
    taxAmount: asNumber(readProp(item, "taxAmount", "TaxAmount")),
    shippingAmount: asNumber(readProp(item, "shippingAmount", "ShippingAmount", "shippingAmount")),
    payableAmount: asNumber(readProp(item, "payableAmount", "PayableAmount", "payableAmount")),
    sellerOrders: Array.isArray(sellersRaw)
      ? sellersRaw.map((row) => {
          const order = asRecord(row) ?? {};
          const linesRaw = readProp(order, "lines", "Lines");
          return {
            sellerOrderId: asString(readProp(order, "sellerOrderId", "SellerOrderId")),
            orderNumber: asString(readProp(order, "orderNumber", "OrderNumber")),
            sellerPartyId: asString(readProp(order, "sellerPartyId", "SellerPartyId")),
            sellerDisplayName: asString(readProp(order, "sellerDisplayName", "SellerDisplayName"), "فروشنده"),
            status: asString(readProp(order, "status", "Status")),
            subtotalExclusiveOfTax: asNumber(readProp(order, "subtotalExclusiveOfTax", "SubtotalExclusiveOfTax")),
            taxAmount: asNumber(readProp(order, "taxAmount", "TaxAmount")),
            discountAmount: asNumber(readProp(order, "discountAmount", "DiscountAmount")),
            payableAmount: asNumber(readProp(order, "payableAmount", "PayableAmount")),
            currency: asString(readProp(order, "currency", "Currency"), "IRR"),
            lines: Array.isArray(linesRaw) ? linesRaw.map(mapLine) : [],
          };
        })
      : [],
  };
}

async function parseCheckout(response: Response): Promise<StorefrontCheckoutPage> {
  const payload: unknown = await response.json().catch(() => null);
  if (!response.ok) {
    const record = asRecord(payload);
    throw new StorefrontCartApiError(
      response.status,
      record ? asString(readProp(record, "errorCode", "ErrorCode")) || null : null,
      record ? asString(readProp(record, "detail", "Detail")) || null : null,
    );
  }
  const page = mapStorefrontCheckout(payload);
  if (!page) {
    throw new StorefrontCartApiError(response.status, "checkout.invalid", "پاسخ تسویه نامعتبر است.");
  }
  return page;
}

export function checkoutIdempotencyKey(): string {
  if (typeof window === "undefined") {
    return crypto.randomUUID();
  }
  const existing = window.sessionStorage.getItem(IDEMPOTENCY_KEY);
  if (existing) {
    return existing;
  }
  const created = crypto.randomUUID();
  window.sessionStorage.setItem(IDEMPOTENCY_KEY, created);
  return created;
}

export function toCustomerCheckoutMessage(error: unknown): string {
  if (error instanceof StorefrontCartApiError) {
    switch (error.errorCode) {
      case "checkout.price.changed":
        return "قیمت یکی از کالاها تغییر کرده؛ لطفاً سفارش را دوباره بررسی کنید.";
      case "checkout.cart.expired":
        return "سبد خرید منقضی شده است.";
      case "checkout.shipping.incomplete":
        return "اطلاعات ارسال کامل نیست.";
      case "checkout.cart.empty":
        return "سبد خرید خالی است.";
      default:
        return error.detail && !/Held|PRICE_CHANGED|TAX_/.test(error.detail)
          ? error.detail
          : "ثبت سفارش انجام نشد. لطفاً دوباره تلاش کنید.";
    }
  }
  return toCustomerCartMessage(error);
}

export async function previewStorefrontCheckout(
  cartId: string,
  couponCode?: string | null,
): Promise<StorefrontCheckoutPage> {
  const code = couponCode?.trim() || readStoredCouponCode();
  const query = new URLSearchParams({ cartId });
  if (code) {
    query.set("couponCode", code);
  }
  const response = await fetch(`/v1/storefront/checkout/preview?${query.toString()}`, {
    method: "POST",
    cache: "no-store",
    headers: cartHeaders(),
  });
  return parseCheckout(response);
}

export async function submitStorefrontCheckout(
  cartId: string,
  expectedCartVersion: number,
  shipping: StorefrontCheckoutShipping,
  savedAddressId?: string | null,
  couponCode?: string | null,
): Promise<StorefrontCheckoutPage> {
  const headers = { ...(cartHeaders(expectedCartVersion) as Record<string, string>) };
  if (savedAddressId) {
    Object.assign(headers, customerAuthHeaders(true));
  }
  const code = couponCode?.trim() || readStoredCouponCode();
  const response = await fetch("/v1/storefront/checkout", {
    method: "POST",
    cache: "no-store",
    headers,
    body: JSON.stringify({
      cartId,
      expectedCartVersion,
      idempotencyKey: checkoutIdempotencyKey(),
      shipping: toCheckoutShippingBody(shipping, savedAddressId),
      couponCode: code || null,
    }),
  });
  return parseCheckout(response);
}

export async function loadStorefrontCheckout(checkoutId: string): Promise<StorefrontCheckoutPage> {
  const session = readCartSession();
  if (!session.cartId) {
    throw new StorefrontCartApiError(401, "checkout.missing", "سبد برای مشاهدهٔ سفارش پیدا نشد.");
  }
  const response = await fetch(
    `/v1/storefront/checkout/${checkoutId}?cartId=${encodeURIComponent(session.cartId)}`,
    { cache: "no-store", headers: cartHeaders() },
  );
  return parseCheckout(response);
}
