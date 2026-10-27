import { cartHeaders, cartHeadersFromAccess, persistCommittedCheckoutAndDetachActiveCart, resolveCommittedCheckoutAccess, StorefrontCartApiError, toCustomerCartMessage } from "./storefront-cart-api.ts";
import { customerAuthHeaders } from "../customer-panel/customer-api.ts";
import { type Locale } from "../../lib/i18n/locale.ts";
import { isDisplayableServerDetail } from "../../lib/i18n/displayable-server-detail.ts";

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
  firstName: string;
  lastName: string;
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
  const firstName = shipping.firstName.trim();
  const lastName = shipping.lastName.trim();
  const body = {
    ...shipping,
    firstName,
    lastName,
    recipientName: firstName && lastName ? `${firstName} ${lastName}` : shipping.recipientName.trim(),
  };
  if (!savedAddressId) return body;
  return { ...body, savedAddressId };
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

export interface StorefrontCheckoutShippingGroup {
  shippingGroupId: string;
  sellerPartyId: string;
  methodCode: string;
  serviceCode: string | null;
  optionCode: string | null;
  methodLabel: string;
  shippingAmount: number;
  currency: string;
  minimumDeliveryDate: string;
  selectedDeliveryDate: string | null;
  selectedDeliveryTimeWindow: string | null;
  leadDays: number;
  lineCount: number;
  /** (TB-235) عضویت خط سبد فقط برای گروه‌های پیش‌نویس؛ در گروه پایدارشده `null` است. */
  cartLineIds?: string[] | null;
  /** (TB-235) شناسهٔ واقعی خطوط سفارش گروه پایدارشده؛ هرگز جای `cartLineIds` نمی‌نشیند. */
  orderLineIds?: string[] | null;
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
  firstName: string;
  lastName: string;
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
  /** گروه‌های ارسال سفارش از projection مالک Order؛ خالی برای سفارش تک‌گروهی/قدیمی. */
  shippingGroups: StorefrontCheckoutShippingGroup[];
  canInitiatePayment: boolean;
  /**
   * (TB-235) نسخهٔ واقعی ریشهٔ انتخاب به‌صورت متن دقیق برای CAS بعدی؛ فقط در نمای پیش‌نویس پرداخت یک
   * انتخاب capture‌شده پر می‌شود. `null` یعنی هیچ انتخاب capture‌شده‌ای وجود ندارد.
   */
  selectionRevision?: string | null;
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
  const shippingGroupsRaw = readProp(item, "shippingGroups", "ShippingGroups");
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
    firstName: asString(readProp(item, "firstName", "FirstName")),
    lastName: asString(readProp(item, "lastName", "LastName")),
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
    shippingGroups: Array.isArray(shippingGroupsRaw)
      ? shippingGroupsRaw.map((row) => {
          const group = asRecord(row) ?? {};
          return {
            shippingGroupId: asString(readProp(group, "shippingGroupId", "ShippingGroupId")),
            sellerPartyId: asString(readProp(group, "sellerPartyId", "SellerPartyId")),
            methodCode: asString(readProp(group, "methodCode", "MethodCode")),
            serviceCode: asString(readProp(group, "serviceCode", "ServiceCode")) || null,
            optionCode: asString(readProp(group, "optionCode", "OptionCode")) || null,
            methodLabel: asString(readProp(group, "methodLabel", "MethodLabel")),
            shippingAmount: asNumber(readProp(group, "shippingAmount", "ShippingAmount")),
            currency: asString(readProp(group, "currency", "Currency"), "IRR"),
            minimumDeliveryDate: asString(readProp(group, "minimumDeliveryDate", "MinimumDeliveryDate")),
            selectedDeliveryDate: asString(readProp(group, "selectedDeliveryDate", "SelectedDeliveryDate")) || null,
            selectedDeliveryTimeWindow:
              asString(readProp(group, "selectedDeliveryTimeWindow", "SelectedDeliveryTimeWindow")) || null,
            leadDays: asNumber(readProp(group, "leadDays", "LeadDays")),
            lineCount: asNumber(readProp(group, "lineCount", "LineCount")),
            cartLineIds: readGuidList(readProp(group, "cartLineIds", "CartLineIds")),
            orderLineIds: readGuidList(readProp(group, "orderLineIds", "OrderLineIds")),
          };
        })
      : [],
    canInitiatePayment: readProp(item, "canInitiatePayment", "CanInitiatePayment") === false
      ? false
      : asString(readProp(item, "paymentState", "PaymentState"), "PendingPayment") !== "Paid",
    selectionRevision: asString(readProp(item, "selectionRevision", "SelectionRevision")) || null,
  };
}

/**
 * (TB-235) فهرست شناسهٔ خط <b>اختیاری</b>؛ نبود/ناتهی یعنی «غایب» و هرگز با فهرست خالی یا شناسهٔ
 * گروه دیگر جایگزین نمی‌شود.
 */
function readGuidList(value: unknown): string[] | null {
  if (!Array.isArray(value)) {
    return null;
  }
  const ids: string[] = [];
  for (const entry of value) {
    const text = typeof entry === "string" ? entry.trim() : "";
    if (!text) {
      return null;
    }
    ids.push(text);
  }
  return ids;
}

async function parseCheckout(response: Response): Promise<StorefrontCheckoutPage> {
  const payload: unknown = await response.json().catch(() => null);
  if (!response.ok) {
    const record = asRecord(payload);
    throw new StorefrontCartApiError(
      response.status,
      record ? asString(readProp(record, "errorCode", "ErrorCode")) || null : null,
      record ? asString(readProp(record, "detail", "Detail")) || null : null,
      {
        currentCount: Number(readProp(record ?? {}, "currentCount", "CurrentCount") ?? Number.NaN),
        maxCount: Number(readProp(record ?? {}, "maxCount", "MaxCount") ?? Number.NaN),
        nextAvailableAt: asString(readProp(record ?? {}, "nextAvailableAt", "NextAvailableAt")) || null,
      },
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

export function humanizeCheckoutRetryAfter(iso: string | null | undefined): string | null {
  if (!iso) {
    return null;
  }
  const at = Date.parse(iso);
  if (!Number.isFinite(at)) {
    return null;
  }
  const minutes = Math.max(1, Math.ceil((at - Date.now()) / 60000));
  return minutes === 1 ? "حدود یک دقیقه دیگر" : `حدود ${minutes} دقیقه دیگر`;
}

export function toCustomerCheckoutMessage(error: unknown, locale: Locale = "fa"): string {
  const fa = locale === "fa";
  if (error instanceof StorefrontCartApiError) {
    switch (error.errorCode) {
      case "checkout.open_unpaid_limit_reached":
        return fa
          ? "شما به حداکثر تعداد سفارش‌های در انتظار پرداخت رسیده‌اید. ابتدا یکی از سفارش‌های قبلی را پرداخت یا لغو کنید."
          : "You have reached the maximum number of open unpaid orders. Pay or cancel a previous order first.";
      case "checkout.reservation_commit_limit_reached": {
        const wait = humanizeCheckoutRetryAfter(error.meta.nextAvailableAt);
        if (!fa) {
          return "The limit for starting a reservation in this period has been reached. Try again later.";
        }
        return wait
          ? `تعداد دفعات مجاز شروع رزرو در بازه زمانی اخیر به پایان رسیده است. ${wait} دوباره تلاش کنید.`
          : "تعداد دفعات مجاز شروع رزرو در بازه زمانی اخیر به پایان رسیده است. کمی بعد دوباره تلاش کنید.";
      }
      case "checkout.price.changed":
        return fa ? "قیمت یکی از کالاها تغییر کرده؛ لطفاً سفارش را دوباره بررسی کنید." : "An item price changed. Please review the order again.";
      case "checkout.cart.expired":
        return fa ? "سبد خرید منقضی شده است." : "The cart has expired.";
      case "checkout.shipping.incomplete":
        return fa ? "اطلاعات ارسال کامل نیست." : "Shipping details are incomplete.";
      case "checkout.cart.empty":
        return fa ? "سبد خرید خالی است." : "The cart is empty.";
      case "checkout.version.conflict":
      case "cart.version.conflict":
        return fa
          ? "سبد خرید شما هم‌زمان در جای دیگری تغییر کرده است. صفحه را تازه کنید و دوباره تلاش کنید."
          : "Your cart changed elsewhere. Refresh the page and try again.";
      case "checkout.access.denied":
      case "payment.access.denied":
        return fa
          ? "دسترسی به اطلاعات پرداخت این سفارش تأیید نشد. لطفاً از بخش سفارش‌ها دوباره وارد پرداخت شوید."
          : "Access to this order payment was not confirmed. Re-enter payment from your orders.";
      default:
        if (!error.detail || !isDisplayableServerDetail(error.detail, locale)) {
          return fa ? "ثبت سفارش انجام نشد. لطفاً دوباره تلاش کنید." : "The order was not placed. Please try again.";
        }
        return error.detail;
    }
  }
  return toCustomerCartMessage(error, locale);
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
  const page = await parseCheckout(response);
  if (page.checkoutId) {
    persistCommittedCheckoutAndDetachActiveCart(page.checkoutId, page.cartId);
  }
  return page;
}

export async function loadStorefrontCheckout(checkoutId: string, paymentId?: string | null): Promise<StorefrontCheckoutPage> {
  const access = resolveCommittedCheckoutAccess(checkoutId, paymentId);
  if (!access.cartId) {
    throw new StorefrontCartApiError(
      403,
      "checkout.access.denied",
      "دسترسی به اطلاعات پرداخت این سفارش تأیید نشد. لطفاً از بخش سفارش‌ها دوباره وارد پرداخت شوید.",
    );
  }
  const response = await fetch(
    `/v1/storefront/checkout/${checkoutId}?cartId=${encodeURIComponent(access.cartId)}`,
    { cache: "no-store", headers: cartHeadersFromAccess(access) },
  );
  return parseCheckout(response);
}
