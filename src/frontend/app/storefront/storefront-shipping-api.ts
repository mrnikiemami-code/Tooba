import { cartHeaders, persistCommittedCheckoutAndDetachActiveCart, readCartSession, StorefrontCartApiError, toCustomerCartMessage } from "./storefront-cart-api.ts";
import { customerAuthHeaders } from "../customer-panel/customer-api.ts";
import { readStoredCouponCode, toCustomerCheckoutMessage } from "./storefront-checkout-api.ts";
import type { StorefrontCheckoutPage } from "./storefront-checkout-api.ts";
import { type Locale } from "../../lib/i18n/locale.ts";
import { isDisplayableServerDetail } from "../../lib/i18n/displayable-server-detail.ts";

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

export interface StorefrontShippingGroupView {
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
  /** (135) شناسهٔ واقعی خطوط سبد این گروه برای بازیابی قطعی انتخاب «هر خط». */
  cartLineIds: string[] | null;
}

export interface StorefrontShippingDraft {
  cartId: string;
  cartVersion: number;
  firstName: string;
  lastName: string;
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
  /** گروه‌های ذخیره‌شدهٔ پایدار؛ در سبد تک‌گروهی/قدیمی خالی است. */
  shippingGroups: StorefrontShippingGroupView[];
}

export interface StorefrontShippingGroupSelectionInput {
  shippingMethodCode: string;
  cartLineIds: string[];
  serviceCode?: string | null;
  optionCode?: string | null;
  /**
   * TB-150: زمان‌بندی **همین** گروه. سرور آن را در برابر حداقل تاریخ همان گروه اعتبارسنجی می‌کند؛
   * در سبد چندگروهی نبودِ آن fail-closed رد می‌شود (هیچ تاریخی روی همهٔ گروه‌ها کپی نمی‌شود).
   */
  selectedDeliveryDate?: string | null;
  selectedDeliveryTimeWindow?: string | null;
}

/**
 * TB-150: افق زمان‌بندی یک گروه ارسال. تمام مقادیر را سرور محاسبه کرده است (آماده‌سازی فروشندهٔ
 * همان گروه × روزهای حمل همان روش). هیچ تاریخ/پنجره‌ای در کلاینت ساخته یا بین گروه‌ها اشتراک
 * گرفته نمی‌شود. `groupKey` کلید پایدار سرور-مالک مرحلهٔ پیش‌نویس است و `shippingGroupId` پس از
 * نخستین ذخیره ضمیمه می‌شود (شناسهٔ پایدار واقعی گروه).
 */
export interface StorefrontShippingGroupHorizonView {
  groupKey: string;
  shippingGroupId: string | null;
  sellerPartyId: string;
  methodCode: string;
  minimumDeliveryDate: string;
  eligibleDates: StorefrontDeliveryDateOption[];
  eligibleTimeWindows: StorefrontDeliveryTimeOption[];
  selectedDeliveryDate: string | null;
  selectedDeliveryTimeWindow: string | null;
  leadDays: number;
  sellerPreparationDays: number;
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
  /** TB-150: افق مستقل هر جفت (فروشندهٔ سبد × روش واجد شرایط)؛ در سبد تک‌خطی خالی است. */
  shippingGroupHorizons: StorefrontShippingGroupHorizonView[];
}

export interface StorefrontShippingSelectionInput {
  firstName: string;
  lastName: string;
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
  /**
   * انتخاب گروهی افزایشی. با ارسال آن، سرور همان گروه‌ها را با CartLineIds واقعی و روش‌های
   * واجد شرایط اعتبارسنجی می‌کند. در سبد تک‌گروهی همان `shippingMethodCode` قدیمی کافی است.
   */
  shippingGroups?: StorefrontShippingGroupSelectionInput[] | null;
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

/** (135) فهرست شناسهٔ خطوط؛ مقدار نامعتبر ⇒ null (سرور قدیمی/پاسخ ناقص) بدون حدس. */
function asStringArray(value: unknown): string[] | null {
  if (!Array.isArray(value)) return null;
  return value.filter((item): item is string => typeof item === "string" && item.length > 0);
}

async function throwShippingError(response: Response): Promise<never> {
  const payload: unknown = await response.json().catch(() => null);
  const body = asRecord(payload);
  throw new StorefrontCartApiError(
    response.status,
    body ? asString(prop(body, "errorCode", "ErrorCode")) || null : null,
    body ? asString(prop(body, "detail", "Detail") ?? prop(body, "title", "Title"), "خطای ارسال") : "خطای ارسال",
    {
      currentCount: asNumber(prop(body ?? {}, "currentCount", "CurrentCount"), Number.NaN),
      maxCount: asNumber(prop(body ?? {}, "maxCount", "MaxCount"), Number.NaN),
      nextAvailableAt: asString(prop(body ?? {}, "nextAvailableAt", "NextAvailableAt")) || null,
    },
  );
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

function mapGroup(value: unknown): StorefrontShippingGroupView | null {
  const item = asRecord(value);
  if (!item) return null;
  const shippingGroupId = asString(prop(item, "shippingGroupId", "ShippingGroupId"));
  const methodCode = asString(prop(item, "methodCode", "MethodCode"));
  if (!shippingGroupId || !methodCode) return null;
  return {
    shippingGroupId,
    sellerPartyId: asString(prop(item, "sellerPartyId", "SellerPartyId")),
    methodCode,
    serviceCode: asString(prop(item, "serviceCode", "ServiceCode")) || null,
    optionCode: asString(prop(item, "optionCode", "OptionCode")) || null,
    methodLabel: asString(prop(item, "methodLabel", "MethodLabel")),
    shippingAmount: asNumber(prop(item, "shippingAmount", "ShippingAmount")),
    currency: asString(prop(item, "currency", "Currency"), "IRR"),
    minimumDeliveryDate: asString(prop(item, "minimumDeliveryDate", "MinimumDeliveryDate")),
    selectedDeliveryDate: asString(prop(item, "selectedDeliveryDate", "SelectedDeliveryDate")) || null,
    selectedDeliveryTimeWindow:
      asString(prop(item, "selectedDeliveryTimeWindow", "SelectedDeliveryTimeWindow")) || null,
    leadDays: asNumber(prop(item, "leadDays", "LeadDays")),
    lineCount: asNumber(prop(item, "lineCount", "LineCount")),
    cartLineIds: asStringArray(prop(item, "cartLineIds", "CartLineIds")),
  };
}

function mapDraft(value: unknown): StorefrontShippingDraft | null {
  const item = asRecord(value);
  if (!item) return null;
  const cartId = asString(prop(item, "cartId", "CartId"));
  if (!cartId) return null;
  const groupsRaw = prop(item, "shippingGroups", "ShippingGroups");
  return {
    cartId,
    cartVersion: asNumber(prop(item, "cartVersion", "CartVersion")),
    firstName: asString(prop(item, "firstName", "FirstName")),
    lastName: asString(prop(item, "lastName", "LastName")),
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
    shippingGroups: Array.isArray(groupsRaw)
      ? groupsRaw.map(mapGroup).filter((g): g is StorefrontShippingGroupView => g !== null)
      : [],
  };
}

function mapDateOption(row: unknown): StorefrontDeliveryDateOption {
  const r = asRecord(row) ?? {};
  return {
    value: asString(prop(r, "value", "Value")),
    label: asString(prop(r, "label", "Label")),
    subLabel: asString(prop(r, "subLabel", "SubLabel")),
    isEarliest: asBool(prop(r, "isEarliest", "IsEarliest")),
  };
}

function mapTimeOption(row: unknown): StorefrontDeliveryTimeOption {
  const r = asRecord(row) ?? {};
  return {
    value: asString(prop(r, "value", "Value")),
    label: asString(prop(r, "label", "Label")),
  };
}

function asArray(value: unknown): unknown[] {
  return Array.isArray(value) ? value : [];
}

/** TB-150: افق مستقل یک گروه؛ ردیف بدون کلید پایدار/روش نادیده گرفته می‌شود (بدون حدس). */
function mapGroupHorizon(value: unknown): StorefrontShippingGroupHorizonView | null {
  const item = asRecord(value);
  if (!item) return null;
  const groupKey = asString(prop(item, "groupKey", "GroupKey"));
  const methodCode = asString(prop(item, "methodCode", "MethodCode"));
  if (!groupKey || !methodCode) return null;
  return {
    groupKey,
    shippingGroupId: asString(prop(item, "shippingGroupId", "ShippingGroupId")) || null,
    sellerPartyId: asString(prop(item, "sellerPartyId", "SellerPartyId")),
    methodCode,
    minimumDeliveryDate: asString(prop(item, "minimumDeliveryDate", "MinimumDeliveryDate")),
    eligibleDates: asArray(prop(item, "eligibleDates", "EligibleDates")).map(mapDateOption),
    eligibleTimeWindows: asArray(prop(item, "eligibleTimeWindows", "EligibleTimeWindows")).map(mapTimeOption),
    selectedDeliveryDate: asString(prop(item, "selectedDeliveryDate", "SelectedDeliveryDate")) || null,
    selectedDeliveryTimeWindow:
      asString(prop(item, "selectedDeliveryTimeWindow", "SelectedDeliveryTimeWindow")) || null,
    leadDays: asNumber(prop(item, "leadDays", "LeadDays")),
    sellerPreparationDays: asNumber(prop(item, "sellerPreparationDays", "SellerPreparationDays")),
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
  const horizonsRaw = prop(item, "shippingGroupHorizons", "ShippingGroupHorizons");
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
    deliveryDates: asArray(datesRaw).map(mapDateOption),
    deliveryTimeWindows: asArray(timesRaw).map(mapTimeOption),
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
    shippingGroupHorizons: asArray(horizonsRaw)
      .map(mapGroupHorizon)
      .filter((h): h is StorefrontShippingGroupHorizonView => h !== null),
  };
}

export function toCustomerShippingMessage(cause: unknown, locale: Locale = "fa"): string {
  const fa = locale === "fa";
  if (cause instanceof StorefrontCartApiError) {
    if (
      cause.errorCode === "checkout.open_unpaid_limit_reached"
      || cause.errorCode === "checkout.reservation_commit_limit_reached"
    ) {
      return toCustomerCheckoutMessage(cause, locale);
    }
    switch (cause.errorCode) {
      case "shipping.firstname.required":
        return fa ? "نام الزامی است." : "First name is required.";
      case "shipping.lastname.required":
        return fa ? "نام خانوادگی الزامی است." : "Last name is required.";
      case "cart.version.conflict":
      case "checkout.version.conflict":
        return fa
          ? "سبد خرید شما هم‌زمان در جای دیگری تغییر کرده است. برای دیدن مبالغ و تعداد به‌روز به سبد برگردید و دوباره تلاش کنید."
          : "Your cart changed elsewhere. Return to the cart to see updated amounts, then try again.";
      case "cart.expired":
      case "checkout.cart.expired":
        return fa ? "سبد خرید منقضی شده است. لطفاً دوباره از سبد شروع کنید." : "The cart has expired. Please restart from the cart.";
      case "checkout.cart.empty":
        return fa ? "سبد خرید خالی است." : "The cart is empty.";
      default:
        return toCustomerCartMessage(cause, locale);
    }
  }
  if (cause instanceof Error && isDisplayableServerDetail(cause.message, locale)) {
    return cause.message;
  }
  return fa ? "امکان ادامهٔ مرحلهٔ ارسال وجود ندارد." : "The shipping step cannot continue.";
}

export async function loadShippingProjection(input: {
  cartId: string;
  provinceName?: string;
  cityName?: string;
  destination?: {
    countryId: string;
    administrativeAreaId?: string | null;
    cityId?: string | null;
  } | null;
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
      cityName: input.cityName || null,
      destination: input.destination
        ? {
            countryId: input.destination.countryId,
            administrativeAreaId: input.destination.administrativeAreaId || null,
            cityId: input.destination.cityId || null,
          }
        : null,
      methodCode: input.methodCode || null,
      language: "fa",
    }),
  });
  if (!response.ok) {
    await throwShippingError(response);
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
      firstName: selection.firstName,
      lastName: selection.lastName,
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
      shippingGroups: selection.shippingGroups && selection.shippingGroups.length > 0
        ? selection.shippingGroups.map((group) => ({
            shippingMethodCode: group.shippingMethodCode,
            cartLineIds: group.cartLineIds,
            serviceCode: group.serviceCode || null,
            optionCode: group.optionCode || null,
            // TB-150: زمان‌بندی مستقل همین گروه؛ سرور آن را در برابر حداقل تاریخ همین گروه
            // اعتبارسنجی می‌کند. نبود آن در سبد چندگروهی fail-closed رد می‌شود.
            selectedDeliveryDate: group.selectedDeliveryDate || null,
            selectedDeliveryTimeWindow: group.selectedDeliveryTimeWindow || null,
          }))
        : null,
    }),
  });
  if (!response.ok) {
    await throwShippingError(response);
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

/**
 * مسیر قدیمی commit زودهنگام ارسال (checkoutId-محور). دیگر در مسیر عادی «ادامه به پرداخت» استفاده
 * نمی‌شود: ناوبری ارسال → پرداخت فقط پیش‌نویس را ذخیره می‌کند و سبد Active را نگه می‌دارد، و ساخت
 * سفارش/رزرو تنها با کلیک نهایی پرداخت (start-payment سبد-محور) انجام می‌شود.
 * فقط برای بازیابی صریح جریان قدیمی نگه داشته شده است — هیچ فراخوان خودکار ندارد.
 */
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
    await throwShippingError(response);
  }
  const page = mapStorefrontCheckout(await response.json());
  if (!page) {
    throw new StorefrontCartApiError(500, "پاسخ ثبت ارسال نامعتبر است.");
  }
  if (page.checkoutId) {
    writeStoredCheckoutId(page.checkoutId);
    persistCommittedCheckoutAndDetachActiveCart(page.checkoutId, page.cartId);
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
