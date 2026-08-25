/**
 * کلاینت پنل مشتری. هویت تولید از Bearer نشست موجود می‌آید و هدر Actor فقط seam توسعه است.
 */

export const CUSTOMER_SESSION_STORAGE_KEY = "tooba.customerSessionId";
export const CUSTOMER_DEV_ACTOR_STORAGE_KEY = "tooba.customerActorUserId";
export const CUSTOMER_DEV_ACTOR_HEADER = "X-Tooba-Dev-Actor-User-Id";
export const DEFAULT_CUSTOMER_DEV_ACTOR_ID = "aaaaaaaa-aaaa-4aaa-8aaa-000000000009";

export interface CustomerOrderListItem {
  checkoutId: string;
  reference: string;
  submittedAt: string;
  sellerCount: number;
  itemCount: number;
  payableAmount: number;
  currency: string;
  paymentState: string;
  status: string;
}

export interface CustomerDashboardPage {
  actorUserId: string;
  displayName: string;
  totalOrders: number;
  pendingOrders: number;
  paidOrders: number;
  wishlistAvailable: boolean;
  wishlistCount: number;
  addressBookAvailable: boolean;
  addressBookCount: number;
  recentOrders: CustomerOrderListItem[];
}

export interface CustomerProfilePage {
  actorUserId: string;
  displayName: string;
  contactMobile: string | null;
  lastShippingAddress: string | null;
  editable: boolean;
}

export interface CustomerOrderLine {
  offerId: string;
  title: string;
  sellerDisplayName: string;
  quantity: number;
  unitAmount: number;
  linePayable: number;
  currency: string;
}

export interface CustomerSellerOrder {
  sellerOrderId: string;
  orderNumber: string;
  sellerPartyId: string;
  sellerDisplayName: string;
  status: string;
  paymentState: string;
  payableAmount: number;
  currency: string;
  lines: CustomerOrderLine[];
}

export interface CustomerOrderDetailPage {
  checkoutId: string;
  reference: string;
  submittedAt: string;
  status: string;
  paymentState: string;
  subtotal: number;
  taxAmount: number;
  discountAmount: number;
  payableAmount: number;
  currency: string;
  recipientName: string;
  contactMobile: string;
  provinceName: string;
  cityName: string;
  postalAddress: string;
  postalCode: string;
  shippingMethodLabel: string;
  sellerOrders: CustomerSellerOrder[];
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

function number(value: unknown): number {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
}

function nullableText(value: unknown): string | null {
  return value == null || String(value).length === 0 ? null : String(value);
}

/** مبلغ snapshot سفارش را با رقم فارسی نمایش می‌دهد. */
export function formatCustomerMoney(amount: number, currency = "IRR"): string {
  const digits = new Intl.NumberFormat("fa-IR").format(amount);
  return currency === "IRR" ? `${digits} ریال` : `${digits} ${currency}`;
}

/** وضعیت Order/Payment را بدون واژهٔ داخلی به فارسی تبدیل می‌کند. */
export function formatCustomerOrderStatus(status: string): string {
  switch (status) {
    case "Paid":
      return "پرداخت‌شده";
    case "PendingPayment":
    case "Submitted":
      return "در انتظار پرداخت";
    case "Failed":
      return "پرداخت ناموفق";
    case "ReservationRequested":
      return "در انتظار بررسی";
    case "Cancelled":
      return "لغو شده";
    case "Mixed":
      return "در حال پردازش";
    default:
      return status || "نامشخص";
  }
}

/** رنگ وضعیت backend را بدون استنتاج وضعیت تجاری در UI برمی‌گرداند. */
export function customerStatusClasses(status: string): string {
  switch (status) {
    case "Paid":
      return "bg-emerald-50 text-emerald-700";
    case "Failed":
    case "Cancelled":
      return "bg-red-50 text-red-700";
    default:
      return "bg-amber-50 text-amber-700";
  }
}

/** ردیف فهرست سفارش را از JSON Host نگاشت می‌کند. */
export function mapCustomerOrder(value: unknown): CustomerOrderListItem | null {
  const item = recordOf(value);
  if (!item) return null;
  const checkoutId = text(prop(item, "checkoutId", "CheckoutId"));
  if (!checkoutId) return null;
  return {
    checkoutId,
    reference: text(prop(item, "reference", "Reference"), checkoutId),
    submittedAt: text(prop(item, "submittedAt", "SubmittedAt")),
    sellerCount: number(prop(item, "sellerCount", "SellerCount")),
    itemCount: number(prop(item, "itemCount", "ItemCount")),
    payableAmount: number(prop(item, "payableAmount", "PayableAmount")),
    currency: text(prop(item, "currency", "Currency"), "IRR"),
    paymentState: text(prop(item, "paymentState", "PaymentState")),
    status: text(prop(item, "status", "Status")),
  };
}

/** داشبورد زنده را نگاشت می‌کند و شمارندهٔ ساختگی نمی‌سازد. */
export function mapCustomerDashboard(value: unknown): CustomerDashboardPage | null {
  const item = recordOf(value);
  if (!item) return null;
  const actorUserId = text(prop(item, "actorUserId", "ActorUserId"));
  if (!actorUserId) return null;
  const recentRaw = prop(item, "recentOrders", "RecentOrders");
  return {
    actorUserId,
    displayName: text(prop(item, "displayName", "DisplayName"), "مشتری توبا"),
    totalOrders: number(prop(item, "totalOrders", "TotalOrders")),
    pendingOrders: number(prop(item, "pendingOrders", "PendingOrders")),
    paidOrders: number(prop(item, "paidOrders", "PaidOrders")),
    wishlistAvailable: prop(item, "wishlistAvailable", "WishlistAvailable") === true,
    wishlistCount: number(prop(item, "wishlistCount", "WishlistCount")),
    addressBookAvailable: prop(item, "addressBookAvailable", "AddressBookAvailable") === true,
    addressBookCount: number(prop(item, "addressBookCount", "AddressBookCount")),
    recentOrders: Array.isArray(recentRaw)
      ? recentRaw.map(mapCustomerOrder).filter((row): row is CustomerOrderListItem => row !== null)
      : [],
  };
}

/** پروفایل خواندنی مشتری را از Host نگاشت می‌کند. */
export function mapCustomerProfile(value: unknown): CustomerProfilePage | null {
  const item = recordOf(value);
  if (!item) return null;
  const actorUserId = text(prop(item, "actorUserId", "ActorUserId"));
  if (!actorUserId) return null;
  return {
    actorUserId,
    displayName: text(prop(item, "displayName", "DisplayName"), "مشتری توبا"),
    contactMobile: nullableText(prop(item, "contactMobile", "ContactMobile")),
    lastShippingAddress: nullableText(prop(item, "lastShippingAddress", "LastShippingAddress")),
    editable: prop(item, "editable", "Editable") === true,
  };
}

/** جزئیات checkout مشتری را با خطوط seller-scoped نگاشت می‌کند. */
export function mapCustomerOrderDetail(value: unknown): CustomerOrderDetailPage | null {
  const item = recordOf(value);
  if (!item) return null;
  const checkoutId = text(prop(item, "checkoutId", "CheckoutId"));
  if (!checkoutId) return null;
  const sellersRaw = prop(item, "sellerOrders", "SellerOrders");
  const sellerOrders = Array.isArray(sellersRaw)
    ? sellersRaw.flatMap((sellerValue): CustomerSellerOrder[] => {
        const seller = recordOf(sellerValue);
        if (!seller) return [];
        const linesRaw = prop(seller, "lines", "Lines");
        const lines = Array.isArray(linesRaw)
          ? linesRaw.flatMap((lineValue): CustomerOrderLine[] => {
              const line = recordOf(lineValue);
              if (!line) return [];
              return [{
                offerId: text(prop(line, "offerId", "OfferId")),
                title: text(prop(line, "title", "Title"), "کالای سفارش"),
                sellerDisplayName: text(prop(line, "sellerDisplayName", "SellerDisplayName"), "فروشنده"),
                quantity: number(prop(line, "quantity", "Quantity")),
                unitAmount: number(prop(line, "unitAmount", "UnitAmount")),
                linePayable: number(prop(line, "linePayable", "LinePayable")),
                currency: text(prop(line, "currency", "Currency"), "IRR"),
              }];
            })
          : [];
        return [{
          sellerOrderId: text(prop(seller, "sellerOrderId", "SellerOrderId")),
          orderNumber: text(prop(seller, "orderNumber", "OrderNumber")),
          sellerPartyId: text(prop(seller, "sellerPartyId", "SellerPartyId")),
          sellerDisplayName: text(prop(seller, "sellerDisplayName", "SellerDisplayName"), "فروشنده"),
          status: text(prop(seller, "status", "Status")),
          paymentState: text(prop(seller, "paymentState", "PaymentState")),
          payableAmount: number(prop(seller, "payableAmount", "PayableAmount")),
          currency: text(prop(seller, "currency", "Currency"), "IRR"),
          lines,
        }];
      })
    : [];
  return {
    checkoutId,
    reference: text(prop(item, "reference", "Reference"), checkoutId),
    submittedAt: text(prop(item, "submittedAt", "SubmittedAt")),
    status: text(prop(item, "status", "Status")),
    paymentState: text(prop(item, "paymentState", "PaymentState")),
    subtotal: number(prop(item, "subtotal", "Subtotal")),
    taxAmount: number(prop(item, "taxAmount", "TaxAmount")),
    discountAmount: number(prop(item, "discountAmount", "DiscountAmount")),
    payableAmount: number(prop(item, "payableAmount", "PayableAmount")),
    currency: text(prop(item, "currency", "Currency"), "IRR"),
    recipientName: text(prop(item, "recipientName", "RecipientName")),
    contactMobile: text(prop(item, "contactMobile", "ContactMobile")),
    provinceName: text(prop(item, "provinceName", "ProvinceName")),
    cityName: text(prop(item, "cityName", "CityName")),
    postalAddress: text(prop(item, "postalAddress", "PostalAddress")),
    postalCode: text(prop(item, "postalCode", "PostalCode")),
    shippingMethodLabel: text(prop(item, "shippingMethodLabel", "ShippingMethodLabel")),
    sellerOrders,
  };
}

/**
 * هدر هویت مشتری را برای Host می‌سازد.
 * مالکیت از Bearer نشست یا هدر توسعهٔ Actor می‌آید؛ شناسهٔ کاربر در بدنه یا query مرجع نیست.
 */
export function customerAuthHeaders(json = false): Record<string, string> {
  const result: Record<string, string> = { Accept: "application/json" };
  if (json) result["Content-Type"] = "application/json";
  if (typeof window === "undefined") return result;
  const session = window.localStorage.getItem(CUSTOMER_SESSION_STORAGE_KEY);
  if (session) {
    result.Authorization = `Bearer ${session}`;
    return result;
  }
  result[CUSTOMER_DEV_ACTOR_HEADER] =
    window.localStorage.getItem(CUSTOMER_DEV_ACTOR_STORAGE_KEY) ?? DEFAULT_CUSTOMER_DEV_ACTOR_ID;
  return result;
}

function headers(): Record<string, string> {
  return customerAuthHeaders();
}

async function read(path: string): Promise<{ ok: boolean; status: number; payload: unknown }> {
  try {
    const response = await fetch(path, { headers: headers() });
    return { ok: response.ok, status: response.status, payload: await response.json().catch(() => null) };
  } catch {
    return { ok: false, status: 0, payload: null };
  }
}

/** داشبورد مشتری را از Host می‌خواند. */
export async function loadCustomerDashboard(): Promise<CustomerDashboardPage | null> {
  const response = await read("/v1/customer/dashboard");
  return response.ok ? mapCustomerDashboard(response.payload) : null;
}

/** پروفایل مشتری را از Host می‌خواند. */
export async function loadCustomerProfile(): Promise<CustomerProfilePage | null> {
  const response = await read("/v1/customer/profile");
  return response.ok ? mapCustomerProfile(response.payload) : null;
}

/** سفارش‌های مشتری را از Host می‌خواند. */
export async function loadCustomerOrders(): Promise<CustomerOrderListItem[] | null> {
  const response = await read("/v1/customer/orders");
  if (!response.ok || !Array.isArray(response.payload)) return null;
  return response.payload.map(mapCustomerOrder).filter((row): row is CustomerOrderListItem => row !== null);
}

/** جزئیات سفارش متعلق به مشتری نشست را می‌خواند. */
export async function loadCustomerOrderDetail(checkoutId: string): Promise<CustomerOrderDetailPage | null> {
  const response = await read(`/v1/customer/orders/${encodeURIComponent(checkoutId)}`);
  return response.ok ? mapCustomerOrderDetail(response.payload) : null;
}
