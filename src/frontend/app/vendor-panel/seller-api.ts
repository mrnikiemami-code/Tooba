/**
 * کلاینت Host برای پنل فروشنده.
 * Actor و SellerPartyId جدا هستند؛ مجوز فقط در Host/SpiceDB حل می‌شود.
 */

export type HostReadSource = "host" | "error";

export const SELLER_PARTY_HEADER = "X-Tooba-Seller-Party-Id";
export const DEV_ACTOR_HEADER = "X-Tooba-Dev-Actor-User-Id";
export const SELLER_PARTY_STORAGE_KEY = "tooba.sellerPartyId";
export const ACTOR_STORAGE_KEY = "tooba.sellerActorUserId";

/** فروشندهٔ پیش‌فرض seed برای ورود اولیه قبل از انتخاب کاربر. */
export const DEFAULT_SELLER_PARTY_ID = "01a030d1-40cb-7000-8abe-6d31739956c5";

export interface SellerDevContext {
  actorUserId: string;
  actorLabel: string;
  sellerPartyId: string;
  sellerLabel: string;
}

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

export interface SellerReviewRow {
  id: string;
  reviewId: string;
  productTitle: string;
  authorDisplayName: string;
  rating: number;
  title: string | null;
  body: string;
  statusLabel: string;
  status: string;
  verifiedPurchase: boolean;
  createdAt: string;
}

export interface SellerReviewsPage {
  rows: SellerReviewRow[];
  page: number;
  pageSize: number;
  totalCount: number;
  publishedCount: number;
  pendingCount: number;
  rejectedCount: number;
  sellerResponseSupported: boolean;
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
 * برچسب فارسی وضعیت Offer برای اپراتور.
 */
export function formatOfferStatus(status: string): string {
  switch (status) {
    case "Active":
      return "فعال";
    case "Suspended":
      return "معلق";
    case "Draft":
      return "پیش‌نویس";
    case "Archived":
      return "بایگانی";
    default:
      return status;
  }
}

/**
 * برچسب فارسی وضعیت پرداخت/سفارش.
 */
export function formatPaymentState(state: string): string {
  switch (state) {
    case "Paid":
      return "پرداخت‌شده";
    case "PendingPayment":
    case "Submitted":
    case "ReservationRequested":
      return "در انتظار پرداخت";
    case "Cancelled":
      return "لغو شده";
    case "Failed":
      return "ناموفق";
    default:
      return state;
  }
}

/**
 * مبلغ ریالی با ارقام فارسی.
 */
export function formatMoney(amount: number | null | undefined, currency = "IRR"): string {
  if (amount == null || !Number.isFinite(amount)) {
    return "—";
  }
  const digits = amount.toLocaleString("fa-IR");
  return currency === "IRR" ? `${digits} ریال` : `${digits} ${currency}`;
}

/**
 * موجودی با واحد فارسی.
 */
export function formatUnits(units: number): string {
  return `${units.toLocaleString("fa-IR")} عدد`;
}

/**
 * شناسهٔ Party فروشنده را از storage یا query می‌خواند.
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
 * شناسهٔ Actor را از storage می‌خواند.
 */
export function readActorUserId(): string | null {
  if (typeof window === "undefined") {
    return null;
  }
  return window.localStorage.getItem(ACTOR_STORAGE_KEY);
}

/**
 * شناسهٔ فروشندهٔ فعال را ذخیره می‌کند.
 */
export function writeSellerPartyId(sellerPartyId: string): void {
  if (typeof window !== "undefined") {
    window.localStorage.setItem(SELLER_PARTY_STORAGE_KEY, sellerPartyId);
  }
}

/**
 * شناسهٔ Actor فعال را ذخیره می‌کند؛ با SellerPartyId یکی نیست.
 */
export function writeActorUserId(actorUserId: string): void {
  if (typeof window !== "undefined") {
    window.localStorage.setItem(ACTOR_STORAGE_KEY, actorUserId);
  }
}

function sellerHeaders(sellerPartyId: string, actorUserId: string | null, extra?: Record<string, string>): Record<string, string> {
  const headers: Record<string, string> = {
    Accept: "application/json",
    [SELLER_PARTY_HEADER]: sellerPartyId,
    ...(extra ?? {}),
  };
  if (actorUserId) {
    headers[DEV_ACTOR_HEADER] = actorUserId;
  }
  return headers;
}

function isDeniedStatus(status: number): boolean {
  return status === 401 || status === 403;
}

/**
 * جفت‌های demo Actor↔Seller را از Host می‌خواند.
 */
export async function loadSellerDevContexts(): Promise<SellerDevContext[]> {
  try {
    const response = await fetch("/v1/seller/dev-contexts", { headers: { Accept: "application/json" } });
    if (!response.ok) {
      return [];
    }
    const payload = (await response.json()) as { actors?: unknown };
    const items = Array.isArray(payload.actors) ? payload.actors : [];
    return items
      .filter((item): item is Record<string, unknown> => !!item && typeof item === "object")
      .map((item) => ({
        actorUserId: asString(readProp(item, "actorUserId", "ActorUserId")),
        actorLabel: asString(readProp(item, "actorLabel", "ActorLabel")),
        sellerPartyId: asString(readProp(item, "sellerPartyId", "SellerPartyId")),
        sellerLabel: asString(readProp(item, "sellerLabel", "SellerLabel")),
      }))
      .filter((row) => row.actorUserId.length > 0 && row.sellerPartyId.length > 0);
  } catch {
    return [];
  }
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

function currentActor(): string | null {
  return readActorUserId();
}

/**
 * داشبورد فروشنده را از Host می‌خواند.
 */
export async function loadSellerDashboard(
  sellerPartyId: string,
): Promise<{ source: HostReadSource; summary: SellerDashboardSummary | null; message?: string; denied?: boolean }> {
  try {
    const response = await fetch("/v1/seller/dashboard", {
      headers: sellerHeaders(sellerPartyId, currentActor()),
    });
    if (isDeniedStatus(response.status)) {
      return { source: "error", summary: null, message: "seller.authorization.denied", denied: true };
    }
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
): Promise<{ source: HostReadSource; rows: SellerOfferListRow[]; message?: string; denied?: boolean }> {
  try {
    const response = await fetch("/v1/seller/offers", {
      headers: sellerHeaders(sellerPartyId, currentActor()),
    });
    if (isDeniedStatus(response.status)) {
      return { source: "error", rows: [], message: "seller.authorization.denied", denied: true };
    }
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
    const response = await fetch(`/v1/seller/offers/${offerId}`, {
      headers: sellerHeaders(sellerPartyId, currentActor()),
    });
    if (isDeniedStatus(response.status) || response.status === 404) {
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
): Promise<{ ok: true; detail: SellerOfferDetail } | { ok: false; errorCode: string; denied?: boolean }> {
  try {
    const response = await fetch(`/v1/seller/offers/${offerId}`, {
      method: "PATCH",
      headers: sellerHeaders(sellerPartyId, currentActor(), { "Content-Type": "application/json" }),
      body: JSON.stringify({ sellerSku: patch.sellerSku, status: patch.status }),
    });
    if (isDeniedStatus(response.status)) {
      return { ok: false, errorCode: "seller.authorization.denied", denied: true };
    }
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
): Promise<{ source: HostReadSource; rows: SellerOrderListRow[]; message?: string; denied?: boolean }> {
  try {
    const response = await fetch("/v1/seller/orders", {
      headers: sellerHeaders(sellerPartyId, currentActor()),
    });
    if (isDeniedStatus(response.status)) {
      return { source: "error", rows: [], message: "seller.authorization.denied", denied: true };
    }
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
    const response = await fetch(`/v1/seller/orders/${sellerOrderId}`, {
      headers: sellerHeaders(sellerPartyId, currentActor()),
    });
    if (isDeniedStatus(response.status) || response.status === 404) {
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

/**
 * نگاشت پاسخ فهرست نظرات فروشنده از Host.
 */
export function mapSellerReviewsPage(payload: unknown): SellerReviewsPage | null {
  if (!payload || typeof payload !== "object") {
    return null;
  }
  const root = payload as Record<string, unknown>;
  const raw = readProp(root, "reviews", "Reviews");
  if (!Array.isArray(raw)) {
    return null;
  }
  const rows = raw
    .filter((item): item is Record<string, unknown> => !!item && typeof item === "object")
    .map((item) => {
      const reviewId = asString(readProp(item, "reviewId", "ReviewId"));
      return {
        id: reviewId,
        reviewId,
        productTitle: asString(readProp(item, "productTitle", "ProductTitle"), "محصول"),
        authorDisplayName: asString(readProp(item, "authorDisplayName", "AuthorDisplayName"), "مشتری"),
        rating: asNumber(readProp(item, "rating", "Rating")),
        title: asNullableString(readProp(item, "title", "Title")),
        body: asString(readProp(item, "body", "Body")),
        statusLabel: asString(readProp(item, "statusLabel", "StatusLabel"), asString(readProp(item, "status", "Status"))),
        status: asString(readProp(item, "status", "Status")),
        verifiedPurchase: asBoolean(readProp(item, "verifiedPurchase", "VerifiedPurchase")),
        createdAt: asString(readProp(item, "createdAt", "CreatedAt")),
      };
    })
    .filter((row) => row.reviewId.length > 0);
  return {
    rows,
    page: Math.max(1, asNumber(readProp(root, "page", "Page"), 1)),
    pageSize: Math.max(1, asNumber(readProp(root, "pageSize", "PageSize"), 20)),
    totalCount: Math.max(0, asNumber(readProp(root, "totalCount", "TotalCount"))),
    publishedCount: Math.max(0, asNumber(readProp(root, "publishedCount", "PublishedCount"))),
    pendingCount: Math.max(0, asNumber(readProp(root, "pendingCount", "PendingCount"))),
    rejectedCount: Math.max(0, asNumber(readProp(root, "rejectedCount", "RejectedCount"))),
    sellerResponseSupported: asBoolean(readProp(root, "sellerResponseSupported", "SellerResponseSupported")),
  };
}

/**
 * فهرست نظرات محصولات متعلق به فروشنده را از Host می‌خواند.
 */
export async function loadSellerReviews(
  sellerPartyId: string,
  options?: { status?: string; page?: number; pageSize?: number },
): Promise<{ source: HostReadSource; page: SellerReviewsPage | null; message?: string; denied?: boolean }> {
  try {
    const params = new URLSearchParams();
    if (options?.status && options.status !== "all") {
      params.set("status", options.status);
    }
    params.set("page", String(options?.page ?? 1));
    params.set("pageSize", String(options?.pageSize ?? 50));
    const response = await fetch(`/v1/seller/reviews?${params.toString()}`, {
      headers: sellerHeaders(sellerPartyId, currentActor()),
    });
    if (isDeniedStatus(response.status)) {
      return { source: "error", page: null, message: "seller.authorization.denied", denied: true };
    }
    if (!response.ok) {
      return { source: "error", page: null, message: "seller-reviews-http-" + String(response.status) };
    }
    const page = mapSellerReviewsPage(await readJson(response));
    return page
      ? { source: "host", page }
      : { source: "error", page: null, message: "seller-reviews-invalid" };
  } catch {
    return { source: "error", page: null, message: "host-unreachable" };
  }
}

/** مرجع پروموشن فروشنده از Host. */
export interface SellerPromotionRow {
  promotionId: string;
  name: string;
  status: string;
  priority: number;
  effectiveFrom: string;
  effectiveTo: string | null;
  discountKind: string;
  percentageRate: number;
  fixedAmount: number;
  fixedAmountCurrency: string | null;
  couponCode: string | null;
  sellerPartyId: string | null;
  minimumSubtotal: number | null;
}

export interface UpsertSellerPromotionInput {
  name: string;
  couponCode: string;
  discountKind: "PercentageOff" | "FixedAmountOff";
  discountValue: number;
  effectiveFrom?: string | null;
  effectiveTo?: string | null;
  currency?: string | null;
  minimumSubtotal?: number | null;
}

export function mapSellerPromotion(payload: unknown): SellerPromotionRow | null {
  if (!payload || typeof payload !== "object") {
    return null;
  }
  const item = payload as Record<string, unknown>;
  const promotionId = asString(readProp(item, "promotionId", "PromotionId"));
  if (!promotionId) {
    return null;
  }
  return {
    promotionId,
    name: asString(readProp(item, "name", "Name")),
    status: normalizePromotionStatus(readProp(item, "status", "Status")),
    priority: asNumber(readProp(item, "priority", "Priority")),
    effectiveFrom: asString(readProp(item, "effectiveFrom", "EffectiveFrom")),
    effectiveTo: asNullableString(readProp(item, "effectiveTo", "EffectiveTo")),
    discountKind: normalizeDiscountKind(readProp(item, "discountKind", "DiscountKind")),
    percentageRate: asNumber(readProp(item, "percentageRate", "PercentageRate")),
    fixedAmount: asNumber(readProp(item, "fixedAmount", "FixedAmount")),
    fixedAmountCurrency: asNullableString(readProp(item, "fixedAmountCurrency", "FixedAmountCurrency")),
    couponCode: asNullableString(readProp(item, "couponCode", "CouponCode")),
    sellerPartyId: asNullableString(readProp(item, "sellerPartyId", "SellerPartyId")),
    minimumSubtotal: (() => {
      const raw = readProp(item, "minimumSubtotal", "MinimumSubtotal");
      return raw == null ? null : asNumber(raw);
    })(),
  };
}

function normalizePromotionStatus(value: unknown): string {
  if (value === 0 || value === "0") return "Draft";
  if (value === 1 || value === "1") return "Active";
  if (value === 2 || value === "2") return "Expired";
  return asString(value, "Draft");
}

function normalizeDiscountKind(value: unknown): string {
  if (value === 0 || value === "0") return "PercentageOff";
  if (value === 1 || value === "1") return "FixedAmountOff";
  return asString(value, "PercentageOff");
}

export function mapSellerPromotionList(payload: unknown): SellerPromotionRow[] {
  if (!Array.isArray(payload)) {
    return [];
  }
  return payload.map(mapSellerPromotion).filter((row): row is SellerPromotionRow => row != null);
}

/** فهرست پروموشن‌های فروشنده. */
export async function loadSellerPromotions(
  sellerPartyId: string,
): Promise<{ source: HostReadSource; rows: SellerPromotionRow[]; message?: string; denied?: boolean }> {
  try {
    const response = await fetch("/v1/seller/promotions", {
      headers: sellerHeaders(sellerPartyId, currentActor()),
    });
    if (isDeniedStatus(response.status)) {
      return { source: "error", rows: [], message: "seller.authorization.denied", denied: true };
    }
    if (!response.ok) {
      return { source: "error", rows: [], message: "seller-promotions-http-" + String(response.status) };
    }
    return { source: "host", rows: mapSellerPromotionList(await readJson(response)) };
  } catch {
    return { source: "error", rows: [], message: "host-unreachable" };
  }
}

/** جزئیات یک پروموشن فروشنده. */
export async function loadSellerPromotion(
  sellerPartyId: string,
  promotionId: string,
): Promise<{ source: HostReadSource; detail: SellerPromotionRow | null; message?: string; denied?: boolean }> {
  try {
    const response = await fetch(`/v1/seller/promotions/${encodeURIComponent(promotionId)}`, {
      headers: sellerHeaders(sellerPartyId, currentActor()),
    });
    if (isDeniedStatus(response.status) || response.status === 404) {
      return { source: "error", detail: null, message: "seller.promotion.missing", denied: true };
    }
    if (!response.ok) {
      return { source: "error", detail: null, message: "seller-promotion-http-" + String(response.status) };
    }
    const detail = mapSellerPromotion(await readJson(response));
    return detail
      ? { source: "host", detail }
      : { source: "error", detail: null, message: "seller-promotion-invalid" };
  } catch {
    return { source: "error", detail: null, message: "host-unreachable" };
  }
}

/** ایجاد پروموشن پیش‌نویس فروشنده. */
export async function createSellerPromotion(
  sellerPartyId: string,
  input: UpsertSellerPromotionInput,
): Promise<{ ok: true; detail: SellerPromotionRow } | { ok: false; errorCode: string; denied?: boolean }> {
  try {
    const response = await fetch("/v1/seller/promotions", {
      method: "POST",
      headers: sellerHeaders(sellerPartyId, currentActor(), { "Content-Type": "application/json" }),
      body: JSON.stringify(input),
    });
    if (isDeniedStatus(response.status)) {
      return { ok: false, errorCode: "seller.authorization.denied", denied: true };
    }
    if (!response.ok) {
      const body = (await response.json().catch(() => null)) as { errorCode?: string } | null;
      return { ok: false, errorCode: body?.errorCode ?? "seller.promotion.create-failed" };
    }
    const detail = mapSellerPromotion(await readJson(response));
    return detail ? { ok: true, detail } : { ok: false, errorCode: "seller.promotion.create-failed" };
  } catch {
    return { ok: false, errorCode: "host-unreachable" };
  }
}

/** به‌روزرسانی پیش‌نویس/منقضی. */
export async function updateSellerPromotion(
  sellerPartyId: string,
  promotionId: string,
  input: UpsertSellerPromotionInput,
): Promise<{ ok: true; detail: SellerPromotionRow } | { ok: false; errorCode: string; denied?: boolean }> {
  try {
    const response = await fetch(`/v1/seller/promotions/${encodeURIComponent(promotionId)}`, {
      method: "PUT",
      headers: sellerHeaders(sellerPartyId, currentActor(), { "Content-Type": "application/json" }),
      body: JSON.stringify(input),
    });
    if (isDeniedStatus(response.status)) {
      return { ok: false, errorCode: "seller.authorization.denied", denied: true };
    }
    if (!response.ok) {
      const body = (await response.json().catch(() => null)) as { errorCode?: string } | null;
      return { ok: false, errorCode: body?.errorCode ?? "seller.promotion.update-failed" };
    }
    const detail = mapSellerPromotion(await readJson(response));
    return detail ? { ok: true, detail } : { ok: false, errorCode: "seller.promotion.update-failed" };
  } catch {
    return { ok: false, errorCode: "host-unreachable" };
  }
}

/** فعال‌سازی پروموشن فروشنده. */
export async function activateSellerPromotion(
  sellerPartyId: string,
  promotionId: string,
): Promise<{ ok: true; detail: SellerPromotionRow | null } | { ok: false; errorCode: string; denied?: boolean }> {
  return mutateSellerPromotionAction(sellerPartyId, promotionId, "activate");
}

/** غیرفعال‌سازی پروموشن فروشنده. */
export async function deactivateSellerPromotion(
  sellerPartyId: string,
  promotionId: string,
): Promise<{ ok: true; detail: SellerPromotionRow | null } | { ok: false; errorCode: string; denied?: boolean }> {
  return mutateSellerPromotionAction(sellerPartyId, promotionId, "deactivate");
}

async function mutateSellerPromotionAction(
  sellerPartyId: string,
  promotionId: string,
  action: "activate" | "deactivate",
): Promise<{ ok: true; detail: SellerPromotionRow | null } | { ok: false; errorCode: string; denied?: boolean }> {
  try {
    const response = await fetch(`/v1/seller/promotions/${encodeURIComponent(promotionId)}/${action}`, {
      method: "POST",
      headers: sellerHeaders(sellerPartyId, currentActor()),
    });
    if (isDeniedStatus(response.status)) {
      return { ok: false, errorCode: "seller.authorization.denied", denied: true };
    }
    if (!response.ok) {
      const body = (await response.json().catch(() => null)) as { errorCode?: string } | null;
      return { ok: false, errorCode: body?.errorCode ?? `seller.promotion.${action}-failed` };
    }
    return { ok: true, detail: mapSellerPromotion(await readJson(response)) };
  } catch {
    return { ok: false, errorCode: "host-unreachable" };
  }
}
