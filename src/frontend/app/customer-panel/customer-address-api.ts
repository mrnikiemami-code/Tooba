import { customerAuthHeaders } from "./customer-api.ts";

/**
 * نشانی خصوصی مشتری. مالکیت فقط با هویت نشست/Actor سمت Host تعیین می‌شود.
 */
export interface CustomerAddress {
  addressId: string;
  recipientName: string;
  contactMobile: string;
  country: string;
  provinceName: string;
  cityName: string;
  postalCode: string;
  postalAddress: string;
  buildingUnit: string | null;
  label: string | null;
  isDefault: boolean;
  createdAt: string | null;
  updatedAt: string | null;
}

/**
 * بدنهٔ ایجاد/ویرایش دفترچه. فیلدهای اختیاری خالی به Host فرستاده نمی‌شوند.
 */
export interface CustomerAddressWriteInput {
  recipientName: string;
  contactMobile: string;
  country: string;
  provinceName: string;
  cityName: string;
  postalCode: string;
  postalAddress: string;
  buildingUnit?: string;
  label?: string;
  isDefault?: boolean;
}

/** خطای قابل تشخیص دفترچهٔ آدرس، از جمله ۴۰۱ بدون نشست معتبر. */
export class CustomerAddressApiError extends Error {
  readonly status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

function recordOf(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : null;
}

function prop(item: Record<string, unknown>, camel: string, pascal: string): unknown {
  return item[camel] ?? item[pascal];
}

function text(value: unknown, fallback = ""): string {
  return value == null ? fallback : String(value);
}

function nullableText(value: unknown): string | null {
  return value == null || String(value).length === 0 ? null : String(value);
}

function flag(value: unknown): boolean {
  return value === true;
}

/** یک نشانی Host را از camelCase یا PascalCase نگاشت می‌کند. */
export function mapCustomerAddress(value: unknown): CustomerAddress | null {
  const item = recordOf(value);
  if (!item) return null;
  const addressId = text(prop(item, "addressId", "AddressId"));
  if (!addressId) return null;
  return {
    addressId,
    recipientName: text(prop(item, "recipientName", "RecipientName")),
    contactMobile: text(prop(item, "contactMobile", "ContactMobile")),
    country: text(prop(item, "country", "Country")),
    provinceName: text(prop(item, "provinceName", "ProvinceName")),
    cityName: text(prop(item, "cityName", "CityName")),
    postalCode: text(prop(item, "postalCode", "PostalCode")),
    postalAddress: text(prop(item, "postalAddress", "PostalAddress")),
    buildingUnit: nullableText(prop(item, "buildingUnit", "BuildingUnit")),
    label: nullableText(prop(item, "label", "Label")),
    isDefault: flag(prop(item, "isDefault", "IsDefault")),
    createdAt: nullableText(prop(item, "createdAt", "CreatedAt")),
    updatedAt: nullableText(prop(item, "updatedAt", "UpdatedAt")),
  };
}

/**
 * فهرست نشانی‌ها را از آرایه یا پوشش items/addresses می‌خواند.
 * ردیف بدون AddressId حذف می‌شود و شمارش ساختگی اضافه نمی‌گردد.
 */
export function mapCustomerAddressList(value: unknown): CustomerAddress[] {
  if (Array.isArray(value)) {
    return value.map(mapCustomerAddress).filter((row): row is CustomerAddress => row !== null);
  }
  const wrapped = recordOf(value);
  if (!wrapped) return [];
  const raw = prop(wrapped, "items", "Items") ?? prop(wrapped, "addresses", "Addresses");
  return Array.isArray(raw)
    ? raw.map(mapCustomerAddress).filter((row): row is CustomerAddress => row !== null)
    : [];
}

/**
 * بدنهٔ نوشتن دفترچه را بدون فیلد اختیاری خالی می‌سازد.
 * کشور/شهر/نشانی اجباری‌اند؛ واحد و برچسب فقط با مقدار واقعی می‌روند.
 */
export function toCustomerAddressWritePayload(input: CustomerAddressWriteInput): Record<string, unknown> {
  const payload: Record<string, unknown> = {
    recipientName: input.recipientName.trim(),
    contactMobile: input.contactMobile.trim(),
    country: input.country.trim(),
    provinceName: input.provinceName.trim(),
    cityName: input.cityName.trim(),
    postalCode: input.postalCode.trim(),
    postalAddress: input.postalAddress.trim(),
  };
  const unit = input.buildingUnit?.trim();
  const label = input.label?.trim();
  if (unit) payload.buildingUnit = unit;
  if (label) payload.label = label;
  payload.isDefault = input.isDefault === true;
  return payload;
}

/** متن حالت خالی را فقط برای مجموعهٔ واقعاً تهی برمی‌گرداند. */
export function addressBookEmptyMessage(count: number): string | null {
  return count === 0 ? "هیچ آدرسی یافت نشد" : null;
}

/** پیام امن فارسی برای شکست خواندن/نوشتن دفترچه. */
export function addressBookErrorMessage(error: unknown): string {
  if (error instanceof CustomerAddressApiError) {
    if (error.status === 401 || error.status === 403) {
      return "برای مدیریت آدرس‌ها باید وارد حساب شوید.";
    }
    return error.message;
  }
  return "ارتباط با دفترچهٔ آدرس برقرار نشد.";
}

/**
 * اگر فهرست نشانی برای تسویه قابل نمایش است true است.
 * مهمان یا پاسخ ۴۰۱/۴۰۳ فهرست ذخیره‌شده را پنهان می‌کند.
 */
export function canShowCheckoutSavedAddresses(status: number): boolean {
  return status !== 401 && status !== 403 && status !== 0;
}

async function parse(response: Response): Promise<unknown> {
  return response.json().catch(() => null);
}

async function request(path: string, init?: RequestInit): Promise<{ status: number; ok: boolean; payload: unknown }> {
  const json = init?.body != null;
  try {
    const response = await fetch(path, {
      ...init,
      cache: "no-store",
      headers: { ...customerAuthHeaders(json), ...(init?.headers ?? {}) },
    });
    return { status: response.status, ok: response.ok, payload: await parse(response) };
  } catch {
    throw new CustomerAddressApiError(0, "ارتباط با دفترچهٔ آدرس برقرار نشد.");
  }
}

function ensureOk(response: { status: number; ok: boolean; payload: unknown }, fallback: string): unknown {
  if (response.status === 401 || response.status === 403) {
    throw new CustomerAddressApiError(response.status, "برای مدیریت آدرس‌ها باید وارد حساب شوید.");
  }
  if (!response.ok) {
    const item = recordOf(response.payload);
    const detail = item ? text(prop(item, "detail", "Detail")) : "";
    throw new CustomerAddressApiError(response.status, detail || fallback);
  }
  return response.payload;
}

/** فهرست نشانی‌های متعلق به هویت جاری را از Host می‌خواند. */
export async function listCustomerAddresses(): Promise<CustomerAddress[]> {
  const response = await request("/v1/customer/addresses");
  const payload = ensureOk(response, "دریافت آدرس‌ها ممکن نشد.");
  return mapCustomerAddressList(payload);
}

/**
 * فهرست را برای تسویه می‌خواند. وضعیت بدون هویت معتبر null است تا مسیر مهمان حفظ شود.
 */
export async function listCheckoutSavedAddresses(): Promise<{ status: number; addresses: CustomerAddress[] | null }> {
  const response = await request("/v1/customer/addresses");
  if (!canShowCheckoutSavedAddresses(response.status) || !response.ok) {
    return { status: response.status, addresses: null };
  }
  return { status: response.status, addresses: mapCustomerAddressList(response.payload) };
}

/** نشانی متعلق به هویت جاری را ایجاد می‌کند. */
export async function createCustomerAddress(input: CustomerAddressWriteInput): Promise<CustomerAddress> {
  const response = await request("/v1/customer/addresses", {
    method: "POST",
    body: JSON.stringify(toCustomerAddressWritePayload(input)),
  });
  const mapped = mapCustomerAddress(ensureOk(response, "ثبت آدرس انجام نشد."));
  if (!mapped) throw new CustomerAddressApiError(response.status, "پاسخ ثبت آدرس نامعتبر است.");
  return mapped;
}

/** نشانی متعلق به هویت جاری را به‌روز می‌کند. */
export async function updateCustomerAddress(addressId: string, input: CustomerAddressWriteInput): Promise<CustomerAddress> {
  const response = await request(`/v1/customer/addresses/${encodeURIComponent(addressId)}`, {
    method: "PUT",
    body: JSON.stringify(toCustomerAddressWritePayload(input)),
  });
  const mapped = mapCustomerAddress(ensureOk(response, "ویرایش آدرس انجام نشد."));
  if (!mapped) throw new CustomerAddressApiError(response.status, "پاسخ ویرایش آدرس نامعتبر است.");
  return mapped;
}

/** نشانی متعلق به هویت جاری را حذف می‌کند. جایگزینی پیش‌فرض در UI ساخته نمی‌شود. */
export async function deleteCustomerAddress(addressId: string): Promise<void> {
  const response = await request(`/v1/customer/addresses/${encodeURIComponent(addressId)}`, { method: "DELETE" });
  ensureOk(response, "حذف آدرس انجام نشد.");
}

/** نشانی را به‌عنوان پیش‌فرض اتمیک روی Host علامت می‌زند. */
export async function setDefaultCustomerAddress(addressId: string): Promise<void> {
  const response = await request(`/v1/customer/addresses/${encodeURIComponent(addressId)}/default`, { method: "POST" });
  ensureOk(response, "تنظیم آدرس پیش‌فرض انجام نشد.");
}

/** فیلدهای ارسال تسویه را از نشانی ذخیره‌شده پر می‌کند؛ ژئوکد ساخته نمی‌شود. */
export function shippingFromCustomerAddress(address: CustomerAddress): {
  recipientName: string;
  contactMobile: string;
  provinceName: string;
  cityName: string;
  postalAddress: string;
  postalCode: string;
} {
  return {
    recipientName: address.recipientName,
    contactMobile: address.contactMobile,
    provinceName: address.provinceName,
    cityName: address.cityName,
    postalAddress: address.postalAddress,
    postalCode: address.postalCode,
  };
}
