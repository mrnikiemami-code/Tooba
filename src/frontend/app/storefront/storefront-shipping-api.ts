import { cartHeaders, readCartSession, StorefrontCartApiError } from "./storefront-cart-api.ts";
import { customerAuthHeaders } from "../customer-panel/customer-api.ts";
import { readStoredCouponCode } from "./storefront-checkout-api.ts";
import type { StorefrontCheckoutPage } from "./storefront-checkout-api.ts";

const SHIPPING_IDEMPOTENCY_KEY = "tooba.storefront.shippingCommitIdempotency";
const CHECKOUT_ID_KEY = "tooba.storefront.checkoutId";

export interface StorefrontShippingMethod {
  methodCode: string;
  label: string;
  serviceCode: string;
  iconKey: string;
  priceAmount: number;
  leadDays: number;
  isFree: boolean;
  estimationLabel: string;
}

export interface StorefrontDeliveryDateOption {
  value: string;
  label: string;
  subLabel: string;
  isEarliest: boolean;
}

export interface StorefrontDeliveryTimeOption {
  value: string;
  label: string;
}

export interface StorefrontProvinceOption {
  code: string;
  label: string;
  cities: string[];
}

export interface StorefrontShippingDraft {
  cartId: string;
  cartVersion: number;
  recipientName: string;
  contactMobile: string;
  provinceName: string;
  cityName: string;
  postalAddress: string;
  postalCode: string;
  savedAddressId: string | null;
  shippingMethodCode: string;
  shippingMethodLabel: string;
  shippingAmount: number;
  minimumDeliveryDate: string;
  selectedDeliveryDate: string | null;
  selectedDeliveryTimeWindow: string | null;
  customerNote: string | null;
}

export interface StorefrontShippingProjection {
  cartId: string;
  cartVersion: number;
  currency: string;
  itemCount: number;
  subtotalExclusiveOfTax: number;
  sellerCount: number;
  maxSellerPreparationDays: number;
  methods: StorefrontShippingMethod[];
  selectedMethodCode: string | null;
  selectedShippingAmount: number;
  minimumDeliveryDate: string | null;
  deliveryDates: StorefrontDeliveryDateOption[];
  deliveryTimeWindows: StorefrontDeliveryTimeOption[];
  draft: StorefrontShippingDraft | null;
  revalidationMessage: string | null;
  provinces: StorefrontProvinceOption[];
}

export interface StorefrontShippingSelectionInput {
  recipientName: string;
  contactMobile: string;
  provinceName: string;
  cityName: string;
  postalAddress: string;
  postalCode: string;
  savedAddressId?: string | null;
  shippingMethodCode: string;
  selectedDeliveryDate: string;
  selectedDeliveryTimeWindow: string;
  customerNote?: string | null;
}

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : null;
}

function prop(record: Record<string, unknown>, ...names: string[]): unknown {
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
  if (typeof value === "number" && Number.isFinite(value)) return value;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
}

function asBool(value: unknown): boolean {
  return value === true;
}

async function readError(response: Response): Promise<string> {
  try {
    const body = asRecord(await response.json());
    if (!body) return "خطای ارسال";
    return asString(prop(body, "detail", "Detail") ?? prop(body, "title", "Title"), "خطای ارسال");
  } catch {
    return "خطای ارسال";
  }
}

function mapMethod(value: unknown): StorefrontShippingMethod | null {
  const item = asRecord(value);
  if (!item) return null;
  const methodCode = asString(prop(item, "methodCode", "MethodCode"));
  if (!methodCode) return null;
  return {
    methodCode,
    label: asString(prop(item, "label", "Label")),
    serviceCode: asString(prop(item, "serviceCode", "ServiceCode")),
    iconKey: asString(prop(item, "iconKey", "IconKey"), "truck"),
    priceAmount: asNumber(prop(item, "priceAmount", "PriceAmount")),
    leadDays: asNumber(prop(item, "leadDays", "LeadDays")),
    isFree: asBool(prop(item, "isFree", "IsFree")),
    estimationLabel: asString(prop(item, "estimationLabel", "EstimationLabel")),
  };
}

function mapDraft(value: unknown): StorefrontShippingDraft | null {
  const item = asRecord(value);
  if (!item) return null;
  const cartId = asString(prop(item, "cartId", "CartId"));
  if (!cartId) return null;
  return {
    cartId,
    cartVersion: asNumber(prop(item, "cartVersion", "CartVersion")),
    recipientName: asString(prop(item, "recipientName", "RecipientName")),
    contactMobile: asString(prop(item, "contactMobile", "ContactMobile")),
    provinceName: asString(prop(item, "provinceName", "ProvinceName")),
    cityName: asString(prop(item, "cityName", "CityName")),
    postalAddress: asString(prop(item, "postalAddress", "PostalAddress")),
    postalCode: asString(prop(item, "postalCode", "PostalCode")),
    savedAddressId: asString(prop(item, "savedAddressId", "SavedAddressId")) || null,
    shippingMethodCode: asString(prop(item, "shippingMethodCode", "ShippingMethodCode")),
    shippingMethodLabel: asString(prop(item, "shippingMethodLabel", "ShippingMethodLabel")),
    shippingAmount: asNumber(prop(item, "shippingAmount", "ShippingAmount")),
    minimumDeliveryDate: asString(prop(item, "minimumDeliveryDate", "MinimumDeliveryDate")),
    selectedDeliveryDate: asString(prop(item, "selectedDeliveryDate", "SelectedDeliveryDate")) || null,
    selectedDeliveryTimeWindow: asString(prop(item, "selectedDeliveryTimeWindow", "SelectedDeliveryTimeWindow")) || null,
    customerNote: asString(prop(item, "customerNote", "CustomerNote")) || null,
  };
}

export function mapShippingProjection(value: unknown): StorefrontShippingProjection {
  const item = asRecord(value);
  if (!item) {
    throw new StorefrontCartApiError(500, "پاسخ ارسال نامعتبر است.");
  }
  const methodsRaw = prop(item, "methods", "Methods");
  const datesRaw = prop(item, "deliveryDates", "DeliveryDates");
  const timesRaw = prop(item, "deliveryTimeWindows", "DeliveryTimeWindows");
  const provincesRaw = prop(item, "provinces", "Provinces");
  return {
    cartId: asString(prop(item, "cartId", "CartId")),
    cartVersion: asNumber(prop(item, "cartVersion", "CartVersion")),
    currency: asString(prop(item, "currency", "Currency"), "IRR"),
    itemCount: asNumber(prop(item, "itemCount", "ItemCount")),
    subtotalExclusiveOfTax: asNumber(prop(item, "subtotalExclusiveOfTax", "SubtotalExclusiveOfTax")),
    sellerCount: asNumber(prop(item, "sellerCount", "SellerCount")),
    maxSellerPreparationDays: asNumber(prop(item, "maxSellerPreparationDays", "MaxSellerPreparationDays")),
    methods: Array.isArray(methodsRaw)
      ? methodsRaw.map(mapMethod).filter((m): m is StorefrontShippingMethod => m !== null)
      : [],
    selectedMethodCode: asString(prop(item, "selectedMethodCode", "SelectedMethodCode")) || null,
    selectedShippingAmount: asNumber(prop(item, "selectedShippingAmount", "SelectedShippingAmount")),
    minimumDeliveryDate: asString(prop(item, "minimumDeliveryDate", "MinimumDeliveryDate")) || null,
    deliveryDates: Array.isArray(datesRaw)
      ? datesRaw.map((row) => {
          const r = asRecord(row) ?? {};
          return {
            value: asString(prop(r, "value", "Value")),
            label: asString(prop(r, "label", "Label")),
            subLabel: asString(prop(r, "subLabel", "SubLabel")),
            isEarliest: asBool(prop(r, "isEarliest", "IsEarliest")),
          };
        })
      : [],
    deliveryTimeWindows: Array.isArray(timesRaw)
      ? timesRaw.map((row) => {
          const r = asRecord(row) ?? {};
          return {
            value: asString(prop(r, "value", "Value")),
            label: asString(prop(r, "label", "Label")),
          };
        })
      : [],
    draft: mapDraft(prop(item, "draft", "Draft")),
    revalidationMessage: asString(prop(item, "revalidationMessage", "RevalidationMessage")) || null,
    provinces: Array.isArray(provincesRaw)
      ? provincesRaw.map((row) => {
          const r = asRecord(row) ?? {};
          const cities = prop(r, "cities", "Cities");
          return {
            code: asString(prop(r, "code", "Code")),
            label: asString(prop(r, "label", "Label")),
            cities: Array.isArray(cities) ? cities.map((c) => String(c)) : [],
          };
        })
      : [],
  };
}

export function toCustomerShippingMessage(cause: unknown): string {
  if (cause instanceof StorefrontCartApiError) return cause.message;
  if (cause instanceof Error && cause.message) return cause.message;
  return "امکان ادامهٔ مرحلهٔ ارسال وجود ندارد.";
}

export async function loadShippingProjection(input: {
  cartId: string;
  provinceName?: string;
  methodCode?: string;
}): Promise<StorefrontShippingProjection> {
  const response = await fetch("/v1/storefront/shipping/projection", {
    method: "POST",
    cache: "no-store",
    headers: {
      "content-type": "application/json",
      ...cartHeaders(),
      ...customerAuthHeaders(),
    },
    body: JSON.stringify({
      cartId: input.cartId,
      provinceName: input.provinceName || null,
      methodCode: input.methodCode || null,
      language: "fa",
    }),
  });
  if (!response.ok) {
    throw new StorefrontCartApiError(response.status, await readError(response));
  }
  return mapShippingProjection(await response.json());
}

export async function saveShippingSelection(
  cartId: string,
  expectedCartVersion: number,
  selection: StorefrontShippingSelectionInput,
): Promise<StorefrontShippingDraft> {
  const response = await fetch("/v1/storefront/shipping/selection", {
    method: "PUT",
    cache: "no-store",
    headers: {
      "content-type": "application/json",
      ...cartHeaders(),
      ...customerAuthHeaders(),
    },
    body: JSON.stringify({
      cartId,
      expectedCartVersion,
      recipientName: selection.recipientName,
      contactMobile: selection.contactMobile,
      provinceName: selection.provinceName,
      cityName: selection.cityName,
      postalAddress: selection.postalAddress,
      postalCode: selection.postalCode,
      savedAddressId: selection.savedAddressId || null,
      shippingMethodCode: selection.shippingMethodCode,
      selectedDeliveryDate: selection.selectedDeliveryDate,
      selectedDeliveryTimeWindow: selection.selectedDeliveryTimeWindow,
      customerNote: selection.customerNote || null,
    }),
  });
  if (!response.ok) {
    throw new StorefrontCartApiError(response.status, await readError(response));
  }
  const draft = mapDraft(await response.json());
  if (!draft) throw new StorefrontCartApiError(500, "پاسخ ذخیرهٔ ارسال نامعتبر است.");
  return draft;
}

function readOrCreateShippingIdempotency(): string {
  if (typeof window === "undefined") return crypto.randomUUID();
  const existing = window.sessionStorage.getItem(SHIPPING_IDEMPOTENCY_KEY);
  if (existing) return existing;
  const created = crypto.randomUUID();
  window.sessionStorage.setItem(SHIPPING_IDEMPOTENCY_KEY, created);
  return created;
}

export function writeStoredCheckoutId(checkoutId: string | null): void {
  if (typeof window === "undefined") return;
  if (!checkoutId) {
    window.sessionStorage.removeItem(CHECKOUT_ID_KEY);
    return;
  }
  window.sessionStorage.setItem(CHECKOUT_ID_KEY, checkoutId);
}

export function readStoredCheckoutId(): string | null {
  if (typeof window === "undefined") return null;
  return window.sessionStorage.getItem(CHECKOUT_ID_KEY);
}

export async function commitShippingToPayment(
  cartId: string,
  expectedCartVersion: number,
): Promise<StorefrontCheckoutPage> {
  const { mapStorefrontCheckout } = await import("./storefront-checkout-api.ts");
  const response = await fetch("/v1/storefront/shipping/commit", {
    method: "POST",
    cache: "no-store",
    headers: {
      "content-type": "application/json",
      ...cartHeaders(),
      ...customerAuthHeaders(),
    },
    body: JSON.stringify({
      cartId,
      expectedCartVersion,
      idempotencyKey: readOrCreateShippingIdempotency(),
      couponCode: readStoredCouponCode(),
    }),
  });
  if (!response.ok) {
    throw new StorefrontCartApiError(response.status, await readError(response));
  }
  const page = mapStorefrontCheckout(await response.json());
  if (!page) {
    throw new StorefrontCartApiError(500, "پاسخ ثبت ارسال نامعتبر است.");
  }
  if (page.checkoutId) {
    writeStoredCheckoutId(page.checkoutId);
    if (typeof window !== "undefined") {
      window.sessionStorage.removeItem(SHIPPING_IDEMPOTENCY_KEY);
    }
  }
  return page;
}

export function readCartIdOrThrow(): string {
  const session = readCartSession();
  if (!session.cartId) {
    throw new StorefrontCartApiError(400, "سبد خرید پیدا نشد.");
  }
  return session.cartId;
}
