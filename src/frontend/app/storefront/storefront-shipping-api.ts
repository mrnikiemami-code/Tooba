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
  /** (TB-235) شناسهٔ واقعی خطوط سفارش گروه پایدارشده؛ هرگز جای `cartLineIds` نمی‌نشیند. */
  orderLineIds?: string[] | null;
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
  /**
   * TB-237: نسخهٔ واقعی ریشهٔ انتخاب برای CAS بعدی، به‌صورت متن دقیق (بدون `Number`). `null` یعنی
   * هیچ انتخاب capture‌شده‌ای ذخیره نشده است.
   */
  selectionRevision: string | null;
}

/**
 * TB-237: یک گروه capture‌شده برای ثبت. کلید/بازتاب‌ها فقط «مکان‌یابی» همان چیزی است که مشتری دید؛
 * هیچ مالک/مبلغ/صلاحیت/تخفیفی از کلاینت پذیرفته نمی‌شود. `expectedGoodsAmount` برای هر گروه الزامی است
 * (متن اعشاری متعارف، `"0"` صریح قانونی) و سرور آن را با مبلغ authoritative خودش مقایسه می‌کند.
 */
/**
 * TB-237-R1: نام‌های **نوشتاری** همان DTO سرور (`StorefrontCapturedGroupSelection`).
 *
 * نام‌های `Confirmation*` فقط در پاسخ **خواندن** وجود دارند؛ فرستادن آن‌ها به PUT یعنی پنج بازتاب
 * `expected*` خالی می‌رسد و نویسنده با `intent_invalid` رد می‌کند. اینجا فقط کلیدهای bind‌شده هست.
 */
export interface StorefrontCapturedGroupSelectionInput {
  groupKey: string;
  serviceCode: string;
  optionCode: string | null;
  selectedDeliveryDate: string;
  expectedSnapshotId: string;
  expectedSourceRevision: string;
  expectedSelectionVersion: string;
  expectedAreaGroupId: string;
  expectedRateRowId: string;
  expectedGoodsAmount: string;
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

/**
 * TB-165: روشی که سرور آن را به‌دلیل نبود نرخ‌دهی معتبرِ مالکِ روش از فهرست واجد شرایط خارج کرده
 * است. این ردیف **تنها توضیح** است: نه مبلغ دارد، نه قابل انتخاب است و نه جایگزین `methods`
 * می‌شود. هویت آن در سطح «روش» است (نرخ‌دهی به فروشندهٔ سبد وابسته نیست)، بنابراین به فروشندهٔ
 * همسایه نسبت داده نمی‌شود.
 */
export interface StorefrontShippingUnavailableMethod {
  methodCode: string;
  reasonCode: string;
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
  /**
   * TB-165: توضیح روش‌های ردشده توسط نرخ‌دهی مالک روش. پاسخ قدیمی/گیت خاموش این فیلد را ندارد
   * (`null`)؛ در آن حالت دقیقاً رفتار قبلی حفظ می‌شود.
   */
  unavailableMethods: StorefrontShippingUnavailableMethod[] | null;
  /**
   * TB-229: پیش‌نمایش فقط‌خواندنی گروه‌های capture‌شده. `null` یعنی کلید سرور خاموش است و رفتار دقیقاً
   * قبلی است؛ این بخش هیچ روش/قیمتی به `methods` تزریق نمی‌کند و قابل انتخاب/ثبت نیست.
   */
  capturedPreview: StorefrontCapturedPreview | null;
}

/** TB-229-R1: یک تاریخ قابل ارائهٔ capture‌شده با برچسب محلی و بازهٔ همان روز. */
export interface StorefrontCapturedDateSlot {
  value: string;
  label: string;
  subLabel: string;
  intervalText: string | null;
  isEarliest: boolean;
}

/**
 * TB-229-R1: یک روش پیشنهادی از بازبینی capture‌شدهٔ همان گروه.
 *
 * مبالغ **متن اعشاری دقیق** همان چیزی هستند که سرور فرستاده است؛ هیچ تبدیل `Number`/شناور، گردکردن
 * یا تبدیل ارز روی آن‌ها انجام نمی‌شود. `null` یعنی «نامشخص»، هرگز صفر.
 */
export interface StorefrontCapturedPreviewMethod {
  methodLabel: string;
  serviceCode: string;
  optionCode: string | null;
  freightAmountText: string | null;
  packagingAmountText: string | null;
  payableAtCheckoutAmountText: string | null;
  carrierCollectAmountText: string | null;
  currency: string | null;
  earliestEligibleDate: string | null;
  availableDates: StorefrontCapturedDateSlot[];
  unavailableReason: string | null;
  /**
   * TB-237: هویت تأیید همین روش از همان پاسخ موفق. همه **متن دقیق** سرور هستند و هرگز از `Number`
   * عبور نمی‌کنند؛ فقط بازتاب «همان چیزی که دیدم» برای ثبت است، نه مبنای قیمت/مالکیت.
   */
  confirmationSnapshotId: string | null;
  confirmationSourceRevision: string | null;
  confirmationSelectionVersion: string | null;
  confirmationAreaGroupId: string | null;
  confirmationRateRowId: string | null;
}

/** TB-229-R1: یک گروه ارسال capture‌شده با روش‌ها و تقویم خودش. */
export interface StorefrontCapturedPreviewGroup {
  groupKey: string;
  typeLabel: string | null;
  sellerPartyId: string;
  cartLineIds: string[];
  finalGoodsAmountText: string | null;
  methods: StorefrontCapturedPreviewMethod[];
  unavailableReason: string | null;
}

/**
 * TB-229-R1: پاسخ خواندنی.
 *
 * `null` در سطح projection یعنی کلید سرور خاموش/غایب است و رفتار دقیقاً قبلی است. `state === "untrusted"`
 * یعنی پاسخ **حاضر ولی نامعتبر** است: در این حالت هیچ گروهی نمایش داده نمی‌شود و مسیر قدیمی هم جای آن
 * نمی‌نشیند؛ فقط یک پیام صادقانهٔ «در دسترس نیست» می‌آید.
 */
export interface StorefrontCapturedPreview {
  state: "ok" | "untrusted";
  enabled: boolean;
  currency: string | null;
  groups: StorefrontCapturedPreviewGroup[];
  unavailableReason: string | null;
  /**
   * TB-237: نسخهٔ انتخاب ریشه به‌صورت متن دقیق برای CAS بعدی؛ `null` یعنی هیچ انتخاب ثبت‌شده‌ای نیست
   * (نخستین ذخیره «0» می‌فرستد). هرگز با `Number` خوانده نمی‌شود.
   */
  selectionRevision: string | null;
  /**
   * TB-237-R1: توانایی واقعی همین سرور برای نوشتن انتخاب/ثبت نهایی. فقط برای نمایش صادقانهٔ وضعیت
   * استفاده می‌شود؛ هیچ تصمیم مجوز/اعتبار در مرورگر از آن ساخته نمی‌شود.
   */
  selectionEnabled: boolean;
  checkoutEnabled: boolean;
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
  /**
   * TB-237: انتخاب capture‌شدهٔ گروه‌ها؛ همیشه همراه `expectedSelectionRevision` و کوپنِ همان context.
   * هرگز با `shippingGroups` قدیمی مخلوط نمی‌شود.
   */
  capturedGroups?: StorefrontCapturedGroupSelectionInput[] | null;
  /** نسخهٔ انتخاب ریشه به‌صورت متن دقیق؛ «0» یعنی نخستین ذخیره (نبودِ انتخاب قبلی). */
  expectedSelectionRevision?: string | null;
  /** TB-237: قصد کوپنِ همین درخواست؛ تهی/فاصله یعنی بدون کوپن. */
  couponCode?: string | null;
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

/** TB-229: مبلغ nullable؛ نبود مقدار «صفر» نیست. (رمزگشای legacy؛ مسیر capture‌شده متن دقیق دارد.) */
function asNullableNumber(value: unknown): number | null {
  if (value == null || value === "") return null;
  if (typeof value === "number") return Number.isFinite(value) ? value : null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
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
    orderLineIds: asStringArray(prop(item, "orderLineIds", "OrderLineIds")),
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
    /**
     * TB-237: نسخهٔ انتخابِ **ذخیره‌شده** به‌صورت متن دقیق؛ همان مقداری که CAS بعدی باید بفرستد.
     * نبود یعنی صفر/هیچ انتخاب capture‌شده‌ای ثبت نشده است.
     */
    selectionRevision: asExactTokenOrNull(prop(item, "selectionRevision", "SelectionRevision")),
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
  const unavailableRaw = prop(item, "unavailableMethods", "UnavailableMethods");
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
    unavailableMethods: Array.isArray(unavailableRaw)
      ? unavailableRaw
          .map((row) => {
            const r = asRecord(row) ?? {};
            const methodCode = asString(prop(r, "methodCode", "MethodCode"));
            if (!methodCode) return null;
            return {
              methodCode,
              reasonCode: asString(prop(r, "reasonCode", "ReasonCode")),
            };
          })
          .filter((m): m is StorefrontShippingUnavailableMethod => m !== null)
      : null,
    capturedPreview: mapCapturedPreview(prop(item, "capturedPreview", "CapturedPreview")),
  };
}

/**
 * TB-229-R1: پیش‌نمایش خواندنی گروه‌های capture‌شده — رمزگشای **سخت‌گیر**.
 *
 * قواعد:
 * - غایب/`null` ⇒ `null` (کلید خاموش؛ رفتار قبلی دست‌نخورده).
 * - حاضر ولی نامعتبر ⇒ `{ state: "untrusted" }`؛ هیچ گروهی ساخته نمی‌شود، هیچ ردیفی بی‌صدا حذف
 *   نمی‌شود و مسیر قدیمی هم جای آن نمی‌نشیند.
 * - مبالغ فقط **متن اعشاری دقیق** پذیرفته می‌شوند؛ هیچ `Number`/`parseFloat`/گردکردن روی پول نیست و
 *   مقدار نامعتبر هرگز به ۰ یا مبلغ دیگر تبدیل نمی‌شود.
 */
function mapCapturedPreview(value: unknown): StorefrontCapturedPreview | null {
  if (value === undefined || value === null) return null;
  const raw = asRecord(value);
  if (!raw) return untrustedPreview();

  const enabled = prop(raw, "enabled", "Enabled");
  if (enabled === false) {
    return {
      state: "ok",
      enabled: false,
      currency: null,
      groups: [],
      unavailableReason: null,
      selectionRevision: null,
      selectionEnabled: false,
      checkoutEnabled: false,
    };
  }
  if (enabled !== true) return untrustedPreview();

  const currency = asNonEmptyString(prop(raw, "currency", "Currency"));
  if (!currency) return untrustedPreview();

  const groupsRaw = prop(raw, "groups", "Groups");
  if (!Array.isArray(groupsRaw)) return untrustedPreview();

  const groups: StorefrontCapturedPreviewGroup[] = [];
  const seenGroupKeys = new Set<string>();
  for (const row of groupsRaw) {
    const g = asRecord(row);
    if (!g) return untrustedPreview();
    const groupKey = asNonEmptyString(prop(g, "groupKey", "GroupKey"));
    if (!groupKey || seenGroupKeys.has(groupKey)) return untrustedPreview();
    seenGroupKeys.add(groupKey);

    const sellerPartyId = asNonEmptyString(prop(g, "sellerPartyId", "SellerPartyId"));
    if (!sellerPartyId) return untrustedPreview();

    const rawLineIds = prop(g, "cartLineIds", "CartLineIds");
    if (!Array.isArray(rawLineIds) || rawLineIds.length === 0) return untrustedPreview();
    const cartLineIds: string[] = [];
    for (const lineId of rawLineIds) {
      const text = asNonEmptyString(lineId);
      if (!text || cartLineIds.includes(text)) return untrustedPreview();
      cartLineIds.push(text);
    }

    const goodsText = decodeExactAmount(prop(g, "finalGoodsAmountAfterDiscountAndTax", "FinalGoodsAmountAfterDiscountAndTax"));
    if (goodsText === INVALID) return untrustedPreview();

    const methodsRaw = prop(g, "methods", "Methods");
    if (!Array.isArray(methodsRaw)) return untrustedPreview();
    const methods: StorefrontCapturedPreviewMethod[] = [];
    const seenMethods = new Set<string>();
    for (const m of methodsRaw) {
      const method = asRecord(m);
      if (!method) return untrustedPreview();
      const serviceCode = asNonEmptyString(prop(method, "serviceCode", "ServiceCode"));
      if (!serviceCode) return untrustedPreview();
      const optionCode = asNonEmptyString(prop(method, "optionCode", "OptionCode"));
      const methodKey = `${serviceCode}\u001f${optionCode ?? ""}`.toLowerCase();
      if (seenMethods.has(methodKey)) return untrustedPreview();
      seenMethods.add(methodKey);

      const methodLabel = asNonEmptyString(prop(method, "methodLabel", "MethodLabel"));
      if (!methodLabel) return untrustedPreview();

      const methodCurrency = asNonEmptyString(prop(method, "currency", "Currency"));
      if (!methodCurrency || methodCurrency !== currency) return untrustedPreview();

      const freight = decodeExactAmount(prop(method, "freightAmount", "FreightAmount"));
      const packaging = decodeExactAmount(prop(method, "packagingAmount", "PackagingAmount"));
      const payable = decodeExactAmount(prop(method, "payableAtCheckoutAmount", "PayableAtCheckoutAmount"));
      const carrier = decodeExactAmount(prop(method, "carrierCollectAmount", "CarrierCollectAmount"));
      if (freight === INVALID || packaging === INVALID || payable === INVALID || carrier === INVALID) {
        return untrustedPreview();
      }

      const datesRaw = prop(method, "availableDates", "AvailableDates");
      if (!Array.isArray(datesRaw)) return untrustedPreview();
      const availableDates: StorefrontCapturedDateSlot[] = [];
      for (const d of datesRaw) {
        const slot = asRecord(d);
        if (!slot) return untrustedPreview();
        const date = asNonEmptyString(prop(slot, "date", "Date"));
        if (!date || !ISO_DATE.test(date) || availableDates.some((existing) => existing.value === date)) {
          return untrustedPreview();
        }
        availableDates.push({
          value: date,
          label: asString(prop(slot, "label", "Label")),
          subLabel: asString(prop(slot, "subLabel", "SubLabel")),
          intervalText: asNonEmptyString(prop(slot, "intervalText", "IntervalText")),
          isEarliest: prop(slot, "isEarliest", "IsEarliest") === true,
        });
      }

      methods.push({
        methodLabel,
        serviceCode,
        optionCode,
        freightAmountText: freight,
        packagingAmountText: packaging,
        payableAtCheckoutAmountText: payable,
        carrierCollectAmountText: carrier,
        currency: methodCurrency,
        earliestEligibleDate: asIsoDateOrNull(prop(method, "earliestEligibleDate", "EarliestEligibleDate")),
        availableDates,
        unavailableReason: asNonEmptyString(prop(method, "unavailableReason", "UnavailableReason")),
        confirmationSnapshotId: asExactTokenOrNull(prop(method, "confirmationSnapshotId", "ConfirmationSnapshotId")),
        confirmationSourceRevision: asExactTokenOrNull(prop(method, "confirmationSourceRevision", "ConfirmationSourceRevision")),
        confirmationSelectionVersion: asExactTokenOrNull(prop(method, "confirmationSelectionVersion", "ConfirmationSelectionVersion")),
        confirmationAreaGroupId: asExactTokenOrNull(prop(method, "confirmationAreaGroupId", "ConfirmationAreaGroupId")),
        confirmationRateRowId: asExactTokenOrNull(prop(method, "confirmationRateRowId", "ConfirmationRateRowId")),
      });
    }

    groups.push({
      groupKey,
      typeLabel: asNonEmptyString(prop(g, "typeLabel", "TypeLabel")),
      sellerPartyId,
      cartLineIds,
      finalGoodsAmountText: goodsText,
      methods,
      unavailableReason: asNonEmptyString(prop(g, "unavailableReason", "UnavailableReason")),
    });
  }

  return {
    state: "ok",
    enabled: true,
    currency,
    groups,
    unavailableReason: asNonEmptyString(prop(raw, "noticeReason", "NoticeReason")),
    selectionRevision: asExactTokenOrNull(prop(raw, "selectionRevision", "SelectionRevision")),
    selectionEnabled: prop(raw, "selectionEnabled", "SelectionEnabled") === true,
    checkoutEnabled: prop(raw, "checkoutEnabled", "CheckoutEnabled") === true,
  };
}

/** نشانگر «مقدار حاضر ولی نامعتبر»؛ با `null` (نامشخص) اشتباه نمی‌شود. */
const INVALID = Symbol("invalid-amount");
const ISO_DATE = /^\d{4}-\d{2}-\d{2}$/;
const EXACT_DECIMAL = /^\d+(\.\d+)?$/;

/**
 * متن اعشاری دقیق. `null`/`""` یعنی نامشخص (قانونی)؛ هر شکل دیگری که اعشار دقیق غیرمنفی نباشد
 * نامعتبر است و هرگز به عدد/صفر تبدیل نمی‌شود.
 */
function decodeExactAmount(value: unknown): string | null | typeof INVALID {
  if (value === undefined || value === null) return null;
  // فقط متن دقیق پذیرفته می‌شود؛ عدد JSON (که ممکن است قبلاً گِرد شده باشد) یا شکل ناقص نامعتبر است.
  if (typeof value !== "string") return INVALID;
  return EXACT_DECIMAL.test(value) ? value : INVALID;
}

function asNonEmptyString(value: unknown): string | null {
  if (value === undefined || value === null) return null;
  const text = String(value).trim();
  return text.length > 0 ? text : null;
}

/**
 * TB-237: توکن دقیق سرور (نسخه/شناسه/بازتاب) فقط وقتی **متن** باشد پذیرفته می‌شود. عدد JSON ممکن
 * است قبلاً گِرد شده باشد و `String()` یک `[object Object]` می‌سازد؛ هر دو نامعتبرند و `null`
 * («نبود») می‌دهند. هیچ‌گاه صفر/رشتهٔ ساختگی جای مقدار غایب نمی‌نشیند.
 */
function asExactTokenOrNull(value: unknown): string | null {
  if (typeof value !== "string") return null;
  const text = value.trim();
  return text.length > 0 ? text : null;
}

function asIsoDateOrNull(value: unknown): string | null {
  const text = asNonEmptyString(value);
  return text && ISO_DATE.test(text) ? text : null;
}

function untrustedPreview(): StorefrontCapturedPreview {
  return {
    state: "untrusted",
    enabled: true,
    currency: null,
    groups: [],
    unavailableReason: "storefront.captured_preview.untrusted",
    selectionRevision: null,
    selectionEnabled: false,
    checkoutEnabled: false,
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
      // TB-237: کوپن در هیچ خطی از سبد پذیرفته نشد — امتناع صریح، نه «پذیرفته با تخفیف صفر».
      case "checkout.coupon.not_applicable":
        return fa
          ? "کد تخفیف برای اقلام این سبد قابل اعمال نیست. کد را بررسی کنید یا بدون آن ادامه دهید."
          : "This coupon code cannot be applied to the items in this cart. Check the code or continue without it.";
      case "storefront.captured_selection.coupon_unavailable":
        return fa
          ? "ثبت انتخاب ارسال همراه با کد تخفیف در این نسخه پشتیبانی نمی‌شود."
          : "Saving a shipping selection together with a coupon is not supported in this version.";
      case "storefront.captured_selection.conflict":
        return fa
          ? "شرایط ارسال یا مبلغ کالا تغییر کرده است. لطفاً انتخاب‌های خود را دوباره تأیید کنید."
          : "Shipping conditions or goods amount changed. Please confirm your choices again.";
      case "storefront.captured_selection.date_unavailable":
        return fa ? "تاریخ انتخاب‌شده برای این روش مجاز نیست." : "The selected date is not available for this method.";
      case "storefront.captured_selection.destination_unverified":
        return fa ? "نشانی انتخاب‌شده قابل تأیید نیست؛ مقصد را دوباره انتخاب کنید." : "The destination could not be verified. Choose it again.";
      case "storefront.captured_selection.intent_invalid":
        return fa ? "انتخاب ارسال ناقص است؛ هر گروه را دوباره تأیید کنید." : "The shipping selection is incomplete. Confirm every group again.";
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
  /** TB-237: همان قصد کوپنِ همین context؛ فاصله/تهی یعنی «بدون کوپن» (نه مقدار ذخیره‌شده). */
  couponCode?: string | null;
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
      couponCode: input.couponCode?.trim() ? input.couponCode.trim() : null,
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
      // TB-237: انتخاب capture‌شدهٔ گروه‌ها فقط با بازتاب دقیق همان پاسخ موفق؛ هرگز مخلوط با
      // `shippingGroups` قدیمی، و همیشه با نسخهٔ انتخاب و کوپنِ همان context.
      // TB-237-R1: بدنهٔ گروه capture‌شده فقط همان کلیدهای bind‌شدهٔ DTO سرور را دارد؛ هیچ نام
      // خواندنی (confirmation*) و هیچ shorthand قدیمی (shippingMethodCode/cartLineIds/window) نه.
      capturedGroups: selection.capturedGroups && selection.capturedGroups.length > 0
        ? selection.capturedGroups.map((group) => ({
            groupKey: group.groupKey,
            serviceCode: group.serviceCode,
            optionCode: group.optionCode,
            selectedDeliveryDate: group.selectedDeliveryDate,
            expectedSnapshotId: group.expectedSnapshotId,
            expectedSourceRevision: group.expectedSourceRevision,
            expectedSelectionVersion: group.expectedSelectionVersion,
            expectedAreaGroupId: group.expectedAreaGroupId,
            expectedRateRowId: group.expectedRateRowId,
            expectedGoodsAmount: group.expectedGoodsAmount,
          }))
        : null,
      expectedSelectionRevision: selection.expectedSelectionRevision ?? null,
      couponCode: selection.couponCode?.trim() ? selection.couponCode.trim() : null,
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
