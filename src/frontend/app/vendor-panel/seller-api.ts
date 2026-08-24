/**
 * کلاینت Host برای پنل فروشنده. هویت Seller فقط از هدر می‌رود؛ فیلتر UI مرجع نیست.
 */

export type HostReadSource = "host" | "error";

export const SELLER_PARTY_HEADER = "X-Tooba-Seller-Party-Id";
export const SELLER_PARTY_STORAGE_KEY = "tooba.sellerPartyId";
/** فروشندهٔ پیش‌فرض seed برای ورود اولیه قبل از انتخاب کاربر. */
export const DEFAULT_SELLER_PARTY_ID = "01a030d1-40cb-7000-8abe-6d31739956c5";

export interface SellerDashboardSummary {
  sellerPartyId: string;
  sellerDisplayName: string;
  activeOffers: number;
  openOrders: number;
  paidOrders: number;
}

export interface SellerOfferListRow {
  id: string;
  offerId: string;
  catalogVariantId: string;
  productId: string | null;
  productTitle: string;
  sellerSku: string | null;
  status: string;
  amount: number | null;
  currency: string;
  availableUnits: number;
  lastUpdatedAt: string | null;
}

export interface SellerOfferDetail {
  offerId: string;
  sellerPartyId: string;
  sellerDisplayName: string;
  catalogVariantId: string;
  productId: string | null;
  productTitle: string;
  brandName: string | null;
  sellerSku: string | null;
  status: string;
  channel: string;
  amount: number | null;
  currency: string;
  onHand: number;
  reserved: number;
  availableUnits: number;
  catalogReadOnly: boolean;
}

export interface SellerOrderListRow {
  id: string;
  sellerOrderId: string;
  orderNumber: string;
  submittedAt: string;
  recipientName: string;
  lineCount: number;
  payableAmount: number;
  currency: string;
  paymentState: string;
  status: string;
}

export interface SellerOrderLineRow {
  offerId: string;
  title: string;
  quantity: number;
  unitAmount: number;
  linePayable: number;
  currency: string;
}

export interface SellerOrderDetail {
  sellerOrderId: string;
  orderNumber: string;
  sellerPartyId: string;
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
  lines: SellerOrderLineRow[];
}

function readProp(record: Record<string, unknown>, camel: string, pascal: string): unknown {
  return record[camel] ?? record[pascal];
}

function asString(value: unknown, fallback = ""): string {
  if (value == null) {
    return fallback;
  }
  return String(value);
}

function asNumber(value: unknown, fallback = 0): number {
  if (typeof value === "number" && Number.isFinite(value)) {
    return value;
  }
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
}

function asBoolean(value: unknown, fallback = false): boolean {
  return typeof value === "boolean" ? value : fallback;
}

function asNullableString(value: unknown): string | null {
  return value == null ? null : asString(value);
}

function asNullableNumber(value: unknown): number | null {
  if (value == null) {
    return null;
  }
  const n = asNumber(value, Number.NaN);
  return Number.isFinite(n) ? n : null;
}

/**
 * شناسهٔ Party فروشنده را از storage یا query می‌خواند؛ در نبود، seed پیش‌فرض.
 */
export function readSellerPartyId(search?: string): string | null {
  if (typeof window !== "undefined") {
    const fromStorage = window.localStorage.getItem(SELLER_PARTY_STORAGE_KEY);
    if (fromStorage && fromStorage.length > 0) {
      return fromStorage;
    }
  }
  if (search) {
    const params = new URLSearchParams(search.startsWith("?") ? search.slice(1) : search);
    const fromQuery = params.get("sellerPartyId") ?? params.get("seller");
    if (fromQuery && fromQuery.length > 0) {
      return fromQuery;
    }
  }
  return DEFAULT_SELLER_PARTY_ID;
}

/**
 * شناسهٔ فروشندهٔ فعال را برای درخواست‌های بعدی ذخیره می‌کند.
 */
export function writeSellerPartyId(sellerPartyId: string): void {
  if (typeof window !== "undefined") {
    window.localStorage.setItem(SELLER_PARTY_STORAGE_KEY, sellerPartyId);
  }
}

function sellerHeaders(sellerPartyId: string, extra?: Record<string, string>): Record<string, string> {
  return {
    Accept: "application/json",
    [SELLER_PARTY_HEADER]: sellerPartyId,
    ...(extra ?? {}),
  };
}

/**
 * خلاصهٔ داشبورد فروشنده را از Host می‌خواند.
 */
export function mapSellerDashboard(payload: unknown): SellerDashboardSummary | null {
  if (!payload || typeof payload !== "object") {
    return null;
  }
  const item = payload as Record<string, unknown>;
  const sellerPartyId = asString(readProp(item, "sellerPartyId", "SellerPartyId"));
  if (!sellerPartyId) {
    return null;
  }
  return {
    sellerPartyId,
    sellerDisplayName: asString(readProp(item, "sellerDisplayName", "SellerDisplayName"), "فروشنده"),
    activeOffers: asNumber(readProp(item, "activeOffers", "ActiveOffers")),
    openOrders: asNumber(readProp(item, "openOrders", "OpenOrders")),
    paidOrders: asNumber(readProp(item, "paidOrders", "PaidOrders")),
  };
}

/**
 * ردیف‌های فهرست Offer فروشنده. مبلغ روی Product نیست.
 */
export function mapSellerOfferList(payload: unknown): SellerOfferListRow[] {
  const items = Array.isArray(payload) ? payload : [];
  return items
    .filter((item): item is Record<string, unknown> => !!item && typeof item === "object")
    .map((item) => {
      const offerId = asString(readProp(item, "offerId", "OfferId"));
      return {
        id: offerId,
        offerId,
        catalogVariantId: asString(readProp(item, "catalogVariantId", "CatalogVariantId")),
        productId: asNullableString(readProp(item, "productId", "ProductId")),
        productTitle: asString(readProp(item, "productTitle", "ProductTitle"), "بدون عنوان"),
        sellerSku: asNullableString(readProp(item, "sellerSku", "SellerSku")),
        status: asString(readProp(item, "status", "Status")),
        amount: asNullableNumber(readProp(item, "amount", "Amount")),
        currency: asString(readProp(item, "currency", "Currency"), "IRR"),
        availableUnits: asNumber(readProp(item, "availableUnits", "AvailableUnits")),
        lastUpdatedAt: asNullableString(readProp(item, "lastUpdatedAt", "LastUpdatedAt")),
      };
    })
    .filter((row) => row.offerId.length > 0);
}

/**
 * جزئیات Offer فروشنده با زمینهٔ فقط‌خواندنی Catalog.
 */
export function mapSellerOfferDetail(payload: unknown): SellerOfferDetail | null {
  if (!payload || typeof payload !== "object") {
    return null;
  }
  const item = payload as Record<string, unknown>;
  const offerId = asString(readProp(item, "offerId", "OfferId"));
  if (!offerId) {
    return null;
  }
  return {
    offerId,
    sellerPartyId: asString(readProp(item, "sellerPartyId", "SellerPartyId")),
    sellerDisplayName: asString(readProp(item, "sellerDisplayName", "SellerDisplayName")),
    catalogVariantId: asString(readProp(item, "catalogVariantId", "CatalogVariantId")),
    productId: asNullableString(readProp(item, "productId", "ProductId")),
    productTitle: asString(readProp(item, "productTitle", "ProductTitle"), "بدون عنوان"),
    brandName: asNullableString(readProp(item, "brandName", "BrandName")),
    sellerSku: asNullableString(readProp(item, "sellerSku", "SellerSku")),
    status: asString(readProp(item, "status", "Status")),
    channel: asString(readProp(item, "channel", "Channel")),
    amount: asNullableNumber(readProp(item, "amount", "Amount")),
    currency: asString(readProp(item, "currency", "Currency"), "IRR"),
    onHand: asNumber(readProp(item, "onHand", "OnHand")),
    reserved: asNumber(readProp(item, "reserved", "Reserved")),
    availableUnits: asNumber(readProp(item, "availableUnits", "AvailableUnits")),
    catalogReadOnly: asBoolean(readProp(item, "catalogReadOnly", "CatalogReadOnly"), true),
  };
}

/**
 * فهرست سفارش‌های فقط همین فروشنده.
 */
export function mapSellerOrderList(payload: unknown): SellerOrderListRow[] {
  const items = Array.isArray(payload) ? payload : [];
  return items
    .filter((item): item is Record<string, unknown> => !!item && typeof item === "object")
    .map((item) => {
      const sellerOrderId = asString(readProp(item, "sellerOrderId", "SellerOrderId"));
      return {
        id: sellerOrderId,
        sellerOrderId,
        orderNumber: asString(readProp(item, "orderNumber", "OrderNumber")),
        submittedAt: asString(readProp(item, "submittedAt", "SubmittedAt")),
        recipientName: asString(readProp(item, "recipientName", "RecipientName")),
        lineCount: asNumber(readProp(item, "lineCount", "LineCount")),
        payableAmount: asNumber(readProp(item, "payableAmount", "PayableAmount")),
        currency: asString(readProp(item, "currency", "Currency"), "IRR"),
        paymentState: asString(readProp(item, "paymentState", "PaymentState")),
        status: asString(readProp(item, "status", "Status")),
      };
    })
    .filter((row) => row.sellerOrderId.length > 0);
}

/**
 * جزئیات سفارش فروشنده بدون خطوط فروشندهٔ دیگر.
 */
export function mapSellerOrderDetail(payload: unknown): SellerOrderDetail | null {
  if (!payload || typeof payload !== "object") {
    return null;
  }
  const item = payload as Record<string, unknown>;
  const sellerOrderId = asString(readProp(item, "sellerOrderId", "SellerOrderId"));
  if (!sellerOrderId) {
    return null;
  }
  const linesRaw = readProp(item, "lines", "Lines");
  const lines = Array.isArray(linesRaw)
    ? linesRaw
        .filter((line): line is Record<string, unknown> => !!line && typeof line === "object")
        .map((line) => ({
          offerId: asString(readProp(line, "offerId", "OfferId")),
          title: asString(readProp(line, "title", "Title"), "کالای سفارش"),
          quantity: asNumber(readProp(line, "quantity", "Quantity")),
          unitAmount: asNumber(readProp(line, "unitAmount", "UnitAmount")),
          linePayable: asNumber(readProp(line, "linePayable", "LinePayable")),
          currency: asString(readProp(line, "currency", "Currency"), "IRR"),
        }))
    : [];
  return {
    sellerOrderId,
    orderNumber: asString(readProp(item, "orderNumber", "OrderNumber")),
    sellerPartyId: asString(readProp(item, "sellerPartyId", "SellerPartyId")),
    submittedAt: asString(readProp(item, "submittedAt", "SubmittedAt")),
    status: asString(readProp(item, "status", "Status")),
    paymentState: asString(readProp(item, "paymentState", "PaymentState")),
    subtotal: asNumber(readProp(item, "subtotal", "Subtotal")),
    taxAmount: asNumber(readProp(item, "taxAmount", "TaxAmount")),
    discountAmount: asNumber(readProp(item, "discountAmount", "DiscountAmount")),
    payableAmount: asNumber(readProp(item, "payableAmount", "PayableAmount")),
    currency: asString(readProp(item, "currency", "Currency"), "IRR"),
    recipientName: asString(readProp(item, "recipientName", "RecipientName")),
    contactMobile: asString(readProp(item, "contactMobile", "ContactMobile")),
    provinceName: asString(readProp(item, "provinceName", "ProvinceName")),
    cityName: asString(readProp(item, "cityName", "CityName")),
    postalAddress: asString(readProp(item, "postalAddress", "PostalAddress")),
    postalCode: asString(readProp(item, "postalCode", "PostalCode")),
    shippingMethodLabel: asString(readProp(item, "shippingMethodLabel", "ShippingMethodLabel")),
    lines,
  };
}

async function readJson(response: Response): Promise<unknown> {
  return response.json();
}

/**
 * داشبورد فروشنده را از Host می‌خواند.
 */
export async function loadSellerDashboard(
  sellerPartyId: string,
): Promise<{ source: HostReadSource; summary: SellerDashboardSummary | null; message?: string }> {
  try {
    const response = await fetch("/v1/seller/dashboard", { headers: sellerHeaders(sellerPartyId) });
    if (!response.ok) {
      return { source: "error", summary: null, message: "seller-dashboard-http-" + String(response.status) };
    }
    const summary = mapSellerDashboard(await readJson(response));
    return summary ? { source: "host", summary } : { source: "error", summary: null, message: "seller-dashboard-invalid" };
  } catch {
    return { source: "error", summary: null, message: "host-unreachable" };
  }
}

/**
 * فهرست Offerهای فروشنده را می‌خواند.
 */
export async function loadSellerOffers(
  sellerPartyId: string,
): Promise<{ source: HostReadSource; rows: SellerOfferListRow[]; message?: string }> {
  try {
    const response = await fetch("/v1/seller/offers", { headers: sellerHeaders(sellerPartyId) });
    if (!response.ok) {
      return { source: "error", rows: [], message: "seller-offers-http-" + String(response.status) };
    }
    return { source: "host", rows: mapSellerOfferList(await readJson(response)) };
  } catch {
    return { source: "error", rows: [], message: "host-unreachable" };
  }
}

/**
 * جزئیات Offer را فقط برای همان فروشنده می‌خواند.
 */
export async function loadSellerOfferDetail(
  sellerPartyId: string,
  offerId: string,
): Promise<{ source: HostReadSource; detail: SellerOfferDetail | null; message?: string; denied?: boolean }> {
  try {
    const response = await fetch(`/v1/seller/offers/${offerId}`, { headers: sellerHeaders(sellerPartyId) });
    if (response.status === 404) {
      return { source: "error", detail: null, message: "seller.offer.missing", denied: true };
    }
    if (!response.ok) {
      return { source: "error", detail: null, message: "seller-offer-http-" + String(response.status) };
    }
    const detail = mapSellerOfferDetail(await readJson(response));
    return detail ? { source: "host", detail } : { source: "error", detail: null, message: "seller-offer-invalid" };
  } catch {
    return { source: "error", detail: null, message: "host-unreachable" };
  }
}

/**
 * SKU یا وضعیت Offer را با مرز سرور به‌روز می‌کند.
 */
export async function patchSellerOffer(
  sellerPartyId: string,
  offerId: string,
  patch: { sellerSku?: string | null; status?: string | null },
): Promise<{ ok: true; detail: SellerOfferDetail } | { ok: false; errorCode: string }> {
  try {
    const response = await fetch(`/v1/seller/offers/${offerId}`, {
      method: "PATCH",
      headers: sellerHeaders(sellerPartyId, { "Content-Type": "application/json" }),
      body: JSON.stringify({ sellerSku: patch.sellerSku, status: patch.status }),
    });
    if (!response.ok) {
      const body = (await response.json().catch(() => null)) as { errorCode?: string } | null;
      return { ok: false, errorCode: body?.errorCode ?? "seller.offer.patch-failed" };
    }
    const detail = mapSellerOfferDetail(await readJson(response));
    if (!detail) {
      return { ok: false, errorCode: "seller.offer.patch-failed" };
    }
    return { ok: true, detail };
  } catch {
    return { ok: false, errorCode: "host-unreachable" };
  }
}

/**
 * فهرست سفارش‌های فروشنده را می‌خواند.
 */
export async function loadSellerOrders(
  sellerPartyId: string,
): Promise<{ source: HostReadSource; rows: SellerOrderListRow[]; message?: string }> {
  try {
    const response = await fetch("/v1/seller/orders", { headers: sellerHeaders(sellerPartyId) });
    if (!response.ok) {
      return { source: "error", rows: [], message: "seller-orders-http-" + String(response.status) };
    }
    return { source: "host", rows: mapSellerOrderList(await readJson(response)) };
  } catch {
    return { source: "error", rows: [], message: "host-unreachable" };
  }
}

/**
 * جزئیات سفارش فروشنده؛ برای سفارش فروشندهٔ دیگر 404 است.
 */
export async function loadSellerOrderDetail(
  sellerPartyId: string,
  sellerOrderId: string,
): Promise<{ source: HostReadSource; detail: SellerOrderDetail | null; message?: string; denied?: boolean }> {
  try {
    const response = await fetch(`/v1/seller/orders/${sellerOrderId}`, { headers: sellerHeaders(sellerPartyId) });
    if (response.status === 404) {
      return { source: "error", detail: null, message: "seller.order.missing", denied: true };
    }
    if (!response.ok) {
      return { source: "error", detail: null, message: "seller-order-http-" + String(response.status) };
    }
    const detail = mapSellerOrderDetail(await readJson(response));
    return detail ? { source: "host", detail } : { source: "error", detail: null, message: "seller-order-invalid" };
  } catch {
    return { source: "error", detail: null, message: "host-unreachable" };
  }
}
