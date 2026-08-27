/**
 * کلاینت خواندنی Admin؛ هویت توسعه فقط به Host ارسال می‌شود و هیچ مجوزی در مرورگر صادر نمی‌شود.
 */
export const ADMIN_DEV_ACTOR_HEADER = "X-Tooba-Dev-Actor-User-Id";
export const ADMIN_ACTOR_STORAGE_KEY = "tooba.adminActorUserId";
export const DEFAULT_ADMIN_ACTOR_ID = "";

export type AdminLoadState = "ok" | "denied" | "error";

export interface AdminResult<T> {
  state: AdminLoadState;
  data: T | null;
  status: number;
  message?: string;
}

export interface AdminDashboard {
  activeProducts: number;
  activeOffers: number;
  openOrders: number;
  paidOrders: number;
  pendingOrders: number;
  sellersCount: number;
  customersCount: number;
}

export interface AdminOrderRow {
  id: string;
  checkoutId: string;
  reference: string;
  customerDisplayName: string;
  sellerCount: number;
  lineCount: number;
  paymentState: string;
  status: string;
  payableAmount: number;
  currency: string;
  createdAt: string;
}

export interface AdminOrderLine {
  id: string;
  title: string;
  sellerDisplayName: string;
  quantity: number;
  unitAmount: number;
  linePayable: number;
  currency: string;
}

export interface AdminSellerOrder {
  id: string;
  orderNumber: string;
  sellerDisplayName: string;
  status: string;
  paymentState: string;
  payableAmount: number;
  currency: string;
  lines: AdminOrderLine[];
}

export interface AdminOrderDetail {
  checkoutId: string;
  reference: string;
  createdAt: string;
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
  sellerOrders: AdminSellerOrder[];
  payment?: AdminPaymentOps | null;
}

export interface AdminPaymentOps {
  paymentId: string;
  checkoutId: string;
  status: string;
  amount: number;
  currency: string;
  providerCode: string;
  providerRequestReference: string | null;
  providerTransactionReference: string | null;
  createdAt: string;
  updatedAt: string;
  completedAt: string | null;
  lastFailureCode: string | null;
  reconcileEligible: boolean;
}

export interface AdminSellerRow {
  id: string;
  sellerPartyId: string;
  displayName: string;
  status: string;
  relationship: string;
  activeOfferCount: number;
  orderCount: number;
}

export interface AdminCustomerRow {
  id: string;
  actorUserId: string;
  displayName: string;
  contact: string;
  orderCount: number;
  lastActivityAt: string | null;
  status: string;
}

/** ردیف تعدیل نظر Admin؛ شناسه فقط برای فرمان publish/reject استفاده می‌شود. */
export interface AdminReviewRow {
  id: string;
  reviewerDisplayName: string;
  productTitle: string;
  rating: number;
  excerpt: string;
  verifiedPurchase: boolean;
  status: string;
  createdAt: string;
}

/** صفحهٔ نظرهای Admin با شمارش مقتدر Host. */
export interface AdminReviewsPage {
  rows: AdminReviewRow[];
  page: number;
  pageSize: number;
  totalCount: number;
}

function record(value: unknown): Record<string, unknown> | null {
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

function array(value: unknown): unknown[] {
  return Array.isArray(value) ? value : [];
}

/** مبلغ snapshot را با رقم فارسی و واحد بازار نمایش می‌دهد. */
export function formatAdminMoney(amount: number, currency = "IRR"): string {
  const digits = new Intl.NumberFormat("fa-IR").format(amount);
  return currency === "IRR" ? `${digits} ریال` : `${digits} ${currency}`;
}

/** وضعیت‌های Host را برای اپراتور فارسی می‌کند. */
export function formatAdminStatus(status: string): string {
  const labels: Record<string, string> = {
    Active: "فعال",
    Published: "منتشرشده",
    Draft: "پیش‌نویس",
    Suspended: "معلق",
    Paid: "پرداخت‌شده",
    Pending: "در انتظار",
    PendingPayment: "در انتظار پرداخت",
    Submitted: "ثبت‌شده",
    ReservationRequested: "در انتظار بررسی",
    Processing: "در حال پردازش",
    Mixed: "در حال پردازش",
    Cancelled: "لغو شده",
    Failed: "ناموفق",
  };
  return labels[status] ?? (status || "نامشخص");
}

/** تاریخ Host را بدون نمایش شناسهٔ فنی به قالب فارسی تبدیل می‌کند. */
export function formatAdminDate(value: string | null | undefined): string {
  if (!value) return "—";
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? value
    : new Intl.DateTimeFormat("fa-IR", { year: "numeric", month: "2-digit", day: "2-digit" }).format(date);
}

/** خلاصهٔ زندهٔ داشبورد را از DTO با casing رایج Host نگاشت می‌کند. */
export function mapAdminDashboard(value: unknown): AdminDashboard | null {
  const item = record(value);
  if (!item) return null;
  return {
    activeProducts: number(prop(item, "activeProducts", "ActiveProducts") ?? prop(item, "publishedProducts", "PublishedProducts")),
    activeOffers: number(prop(item, "activeOffers", "ActiveOffers")),
    openOrders: number(prop(item, "openOrders", "OpenOrders")),
    paidOrders: number(prop(item, "paidOrders", "PaidOrders")),
    pendingOrders: number(prop(item, "pendingOrders", "PendingOrders")),
    sellersCount: number(prop(item, "sellersCount", "SellersCount") ?? prop(item, "sellers", "Sellers")),
    customersCount: number(prop(item, "customersCount", "CustomersCount") ?? prop(item, "customers", "Customers")),
  };
}

/** یک ردیف سفارش Admin را نگاشت می‌کند. */
export function mapAdminOrder(value: unknown): AdminOrderRow | null {
  const item = record(value);
  if (!item) return null;
  const checkoutId = text(prop(item, "checkoutId", "CheckoutId"));
  if (!checkoutId) return null;
  return {
    id: checkoutId,
    checkoutId,
    reference: text(prop(item, "reference", "Reference"), text(prop(item, "orderReference", "OrderReference"), "سفارش")),
    customerDisplayName: text(
      prop(item, "customerDisplayName", "CustomerDisplayName"),
      text(prop(item, "recipientName", "RecipientName"), "مشتری"),
    ),
    sellerCount: number(prop(item, "sellerCount", "SellerCount")),
    lineCount: number(prop(item, "lineCount", "LineCount") ?? prop(item, "itemCount", "ItemCount")),
    paymentState: text(prop(item, "paymentState", "PaymentState")),
    status: text(prop(item, "status", "Status")),
    payableAmount: number(prop(item, "payableAmount", "PayableAmount")),
    currency: text(prop(item, "currency", "Currency"), "IRR"),
    createdAt: text(prop(item, "createdAt", "CreatedAt"), text(prop(item, "submittedAt", "SubmittedAt"))),
  };
}

/** جزئیات checkout و برش‌های فروشندگان را نگاشت می‌کند. */
export function mapAdminOrderDetail(value: unknown): AdminOrderDetail | null {
  const item = record(value);
  if (!item) return null;
  const checkoutId = text(prop(item, "checkoutId", "CheckoutId"));
  if (!checkoutId) return null;
  const sellerOrders = array(prop(item, "sellerOrders", "SellerOrders")).flatMap((sellerValue): AdminSellerOrder[] => {
    const seller = record(sellerValue);
    if (!seller) return [];
    const sellerOrderId = text(prop(seller, "sellerOrderId", "SellerOrderId"));
    const lines = array(prop(seller, "lines", "Lines")).flatMap((lineValue, index): AdminOrderLine[] => {
      const line = record(lineValue);
      if (!line) return [];
      return [{
        id: text(prop(line, "offerId", "OfferId"), `${sellerOrderId}-${index}`),
        title: text(prop(line, "title", "Title"), text(prop(line, "productTitle", "ProductTitle"), "کالای سفارش")),
        sellerDisplayName: text(prop(line, "sellerDisplayName", "SellerDisplayName"), text(prop(seller, "sellerDisplayName", "SellerDisplayName"), "فروشنده")),
        quantity: number(prop(line, "quantity", "Quantity")),
        unitAmount: number(prop(line, "unitAmount", "UnitAmount")),
        linePayable: number(prop(line, "linePayable", "LinePayable")),
        currency: text(prop(line, "currency", "Currency"), "IRR"),
      }];
    });
    return [{
      id: sellerOrderId || text(prop(seller, "orderNumber", "OrderNumber")),
      orderNumber: text(prop(seller, "orderNumber", "OrderNumber"), "سفارش فروشنده"),
      sellerDisplayName: text(prop(seller, "sellerDisplayName", "SellerDisplayName"), "فروشنده"),
      status: text(prop(seller, "status", "Status")),
      paymentState: text(prop(seller, "paymentState", "PaymentState")),
      payableAmount: number(prop(seller, "payableAmount", "PayableAmount")),
      currency: text(prop(seller, "currency", "Currency"), "IRR"),
      lines,
    }];
  });
  return {
    checkoutId,
    reference: text(prop(item, "reference", "Reference"), "سفارش"),
    createdAt: text(prop(item, "createdAt", "CreatedAt"), text(prop(item, "submittedAt", "SubmittedAt"))),
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
    payment: mapAdminPaymentOps(prop(item, "payment", "Payment")),
  };
}

function mapAdminPaymentOps(value: unknown): AdminPaymentOps | null {
  const item = record(value);
  if (!item) return null;
  const paymentId = text(prop(item, "paymentId", "PaymentId"));
  if (!paymentId) return null;
  return {
    paymentId,
    checkoutId: text(prop(item, "checkoutId", "CheckoutId")),
    status: text(prop(item, "status", "Status")),
    amount: number(prop(item, "amount", "Amount")),
    currency: text(prop(item, "currency", "Currency"), "IRR"),
    providerCode: text(prop(item, "providerCode", "ProviderCode")),
    providerRequestReference: text(prop(item, "providerRequestReference", "ProviderRequestReference")) || null,
    providerTransactionReference: text(prop(item, "providerTransactionReference", "ProviderTransactionReference")) || null,
    createdAt: text(prop(item, "createdAt", "CreatedAt")),
    updatedAt: text(prop(item, "updatedAt", "UpdatedAt")),
    completedAt: text(prop(item, "completedAt", "CompletedAt")) || null,
    lastFailureCode: text(prop(item, "lastFailureCode", "LastFailureCode")) || null,
    reconcileEligible: Boolean(prop(item, "reconcileEligible", "ReconcileEligible")),
  };
}

/** فهرست فروشندگان را بدون ایجاد دادهٔ CRM نگاشت می‌کند. */
export function mapAdminSellers(value: unknown): AdminSellerRow[] {
  return array(value).flatMap((raw): AdminSellerRow[] => {
    const item = record(raw);
    if (!item) return [];
    const sellerPartyId = text(prop(item, "sellerPartyId", "SellerPartyId"));
    if (!sellerPartyId) return [];
    return [{
      id: sellerPartyId,
      sellerPartyId,
      displayName: text(prop(item, "displayName", "DisplayName"), text(prop(item, "sellerDisplayName", "SellerDisplayName"), "فروشنده")),
      status: text(prop(item, "status", "Status"), "Active"),
      relationship: text(prop(item, "relationship", "Relationship"), "فروشنده"),
      activeOfferCount: number(prop(item, "activeOfferCount", "ActiveOfferCount") ?? prop(item, "activeOffers", "ActiveOffers")),
      orderCount: number(prop(item, "orderCount", "OrderCount")),
    }];
  });
}

/** فهرست خریداران شناخته‌شده از سفارش‌ها را نگاشت می‌کند؛ این مدل CRM نیست. */
export function mapAdminCustomers(value: unknown): AdminCustomerRow[] {
  return array(value).flatMap((raw): AdminCustomerRow[] => {
    const item = record(raw);
    if (!item) return [];
    const actorUserId = text(prop(item, "actorUserId", "ActorUserId"), text(prop(item, "customerUserId", "CustomerUserId"), text(prop(item, "customerId", "CustomerId"))));
    if (!actorUserId) return [];
    return [{
      id: actorUserId,
      actorUserId,
      displayName: text(prop(item, "displayName", "DisplayName"), text(prop(item, "customerDisplayName", "CustomerDisplayName"), "مشتری")),
      contact: text(prop(item, "contact", "Contact"), text(prop(item, "contactMobile", "ContactMobile"), "—")),
      orderCount: number(prop(item, "orderCount", "OrderCount")),
      lastActivityAt: text(prop(item, "lastActivityAt", "LastActivityAt"), text(prop(item, "lastOrderAt", "LastOrderAt"))) || null,
      status: text(prop(item, "status", "Status"), "Active"),
    }];
  });
}

/** پاسخ صفحه‌بندی‌شدهٔ تعدیل نظر را با casingهای Host نگاشت می‌کند. */
export function mapAdminReviews(value: unknown): AdminReviewsPage | null {
  const root = record(value);
  if (!root) return null;
  const rawRows = prop(root, "reviews", "Reviews") ?? prop(root, "items", "Items");
  if (!Array.isArray(rawRows)) return null;
  const rows = rawRows.flatMap((raw): AdminReviewRow[] => {
    const item = record(raw);
    if (!item) return [];
    const id = text(prop(item, "reviewId", "ReviewId") ?? prop(item, "publicId", "PublicId"));
    if (!id) return [];
    const body = text(prop(item, "body", "Body") ?? prop(item, "excerpt", "Excerpt"));
    return [{
      id,
      reviewerDisplayName: text(prop(item, "authorDisplayName", "AuthorDisplayName") ?? prop(item, "reviewerDisplayName", "ReviewerDisplayName"), "مشتری"),
      productTitle: text(prop(item, "productTitle", "ProductTitle"), "کالا"),
      rating: number(prop(item, "rating", "Rating")),
      excerpt: body.length > 120 ? `${body.slice(0, 120)}…` : body,
      verifiedPurchase: prop(item, "verifiedPurchase", "VerifiedPurchase") === true,
      status: text(prop(item, "status", "Status"), "Pending"),
      createdAt: text(prop(item, "createdAt", "CreatedAt")),
    }];
  });
  return {
    rows,
    page: Math.max(1, number(prop(root, "page", "Page")) || 1),
    pageSize: Math.max(1, number(prop(root, "pageSize", "PageSize")) || 20),
    totalCount: Math.max(0, number(prop(root, "totalCount", "TotalCount"))),
  };
}

function actorId(): string {
  if (typeof window === "undefined") return DEFAULT_ADMIN_ACTOR_ID;
  return window.localStorage.getItem(ADMIN_ACTOR_STORAGE_KEY) ?? DEFAULT_ADMIN_ACTOR_ID;
}

/** Actor نمونهٔ Admin را از seam توسعهٔ Host می‌گیرد و برای درخواست‌های بعدی نگه می‌دارد. */
export async function prepareAdminDevActor(): Promise<boolean> {
  if (typeof window === "undefined") return false;
  if (window.localStorage.getItem(ADMIN_ACTOR_STORAGE_KEY)) return true;
  try {
    const response = await fetch("/v1/admin/dev-context", { headers: { Accept: "application/json" } });
    if (!response.ok) return false;
    const payload = record(await response.json());
    const actor = payload ? text(prop(payload, "actorUserId", "ActorUserId")) : "";
    if (!actor) return false;
    window.localStorage.setItem(ADMIN_ACTOR_STORAGE_KEY, actor);
    return true;
  } catch {
    return false;
  }
}

async function read(path: string): Promise<AdminResult<unknown>> {
  try {
    const response = await fetch(path, {
      headers: { Accept: "application/json", [ADMIN_DEV_ACTOR_HEADER]: actorId() },
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: `admin.http.${response.status}` };
    }
    return { state: "ok", data: payload, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

async function mapped<T>(path: string, mapper: (value: unknown) => T | null): Promise<AdminResult<T>> {
  const response = await read(path);
  if (response.state !== "ok") return { ...response, data: null };
  const data = mapper(response.data);
  return data == null ? { state: "error", data: null, status: response.status, message: "admin.invalid-response" } : { ...response, data };
}

/** داشبورد عملیاتی را از Host می‌خواند. */
export function loadAdminDashboard(): Promise<AdminResult<AdminDashboard>> {
  return mapped("/v1/admin/dashboard", mapAdminDashboard);
}

/** فهرست سفارش‌های همهٔ فروشندگان را از Host می‌خواند. */
export function loadAdminOrders(): Promise<AdminResult<AdminOrderRow[]>> {
  return mapped("/v1/admin/orders", (value) => Array.isArray(value) ? value.map(mapAdminOrder).filter((row): row is AdminOrderRow => row !== null) : null);
}

/** جزئیات checkout را از مرز مجاز Admin می‌خواند. */
export function loadAdminOrderDetail(checkoutId: string): Promise<AdminResult<AdminOrderDetail>> {
  return mapped(`/v1/admin/orders/${encodeURIComponent(checkoutId)}`, mapAdminOrderDetail);
}

/** فروشندگان را از read composition زنده می‌خواند. */
export function loadAdminSellers(): Promise<AdminResult<AdminSellerRow[]>> {
  return mapped("/v1/admin/sellers", (value) => Array.isArray(value) ? mapAdminSellers(value) : null);
}

/** خریداران شناخته‌شده را از read composition زنده می‌خواند. */
export function loadAdminCustomers(): Promise<AdminResult<AdminCustomerRow[]>> {
  return mapped("/v1/admin/customers", (value) => Array.isArray(value) ? mapAdminCustomers(value) : null);
}

/** نظرهای در انتظار را از مرز مقتدر Admin می‌خواند. */
export function loadAdminReviews(page = 1, pageSize = 20): Promise<AdminResult<AdminReviewsPage>> {
  return mapped(`/v1/admin/reviews?status=Pending&page=${page}&pageSize=${pageSize}`, mapAdminReviews);
}

/** فرمان تعدیل را به routeهای متمرکز Host می‌فرستد؛ UI هیچ مجوزی صادر نمی‌کند. */
export async function moderateAdminReview(reviewId: string, action: "publish" | "reject"): Promise<AdminResult<null>> {
  try {
    const response = await fetch(`/v1/admin/reviews/${encodeURIComponent(reviewId)}/${action}`, {
      method: "POST",
      headers: adminHeaders({ "content-type": "application/json" }),
    });
    if (response.status === 401 || response.status === 403) return { state: "denied", data: null, status: response.status };
    if (!response.ok) return { state: "error", data: null, status: response.status, message: `admin.http.${response.status}` };
    return { state: "ok", data: null, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export interface AdminPromotionRow {
  id: string;
  promotionId: string;
  name: string;
  status: string;
  couponCode: string | null;
  discountKind: string;
  percentageRate: number;
  fixedAmount: number;
  sellerPartyId: string | null;
  effectiveTo: string | null;
}

export function mapAdminPromotions(value: unknown): AdminPromotionRow[] | null {
  if (!Array.isArray(value)) {
    return null;
  }
  return value
    .map((row) => {
      if (!row || typeof row !== "object") {
        return null;
      }
      const item = row as Record<string, unknown>;
      const promotionId = String(item.promotionId ?? item.PromotionId ?? "");
      if (!promotionId) {
        return null;
      }
      return {
        id: promotionId,
        promotionId,
        name: String(item.name ?? item.Name ?? ""),
        status: normalizeAdminPromotionStatus(item.status ?? item.Status),
        couponCode: item.couponCode == null && item.CouponCode == null
          ? null
          : String(item.couponCode ?? item.CouponCode),
        discountKind: normalizeAdminDiscountKind(item.discountKind ?? item.DiscountKind),
        percentageRate: Number(item.percentageRate ?? item.PercentageRate ?? 0),
        fixedAmount: Number(item.fixedAmount ?? item.FixedAmount ?? 0),
        sellerPartyId: item.sellerPartyId == null && item.SellerPartyId == null
          ? null
          : String(item.sellerPartyId ?? item.SellerPartyId),
        effectiveTo: item.effectiveTo == null && item.EffectiveTo == null
          ? null
          : String(item.effectiveTo ?? item.EffectiveTo),
      };
    })
    .filter((row): row is AdminPromotionRow => row != null);
}

function normalizeAdminPromotionStatus(value: unknown): string {
  if (value === 0 || value === "0") return "Draft";
  if (value === 1 || value === "1") return "Active";
  if (value === 2 || value === "2") return "Expired";
  return String(value ?? "Draft");
}

function normalizeAdminDiscountKind(value: unknown): string {
  if (value === 0 || value === "0") return "PercentageOff";
  if (value === 1 || value === "1") return "FixedAmountOff";
  return String(value ?? "PercentageOff");
}

/** فهرست نظارتی پروموشن‌ها. */
export function loadAdminPromotions(sellerPartyId?: string): Promise<AdminResult<AdminPromotionRow[]>> {
  const query = sellerPartyId ? `?sellerPartyId=${encodeURIComponent(sellerPartyId)}` : "";
  return mapped(`/v1/admin/promotions${query}`, mapAdminPromotions);
}

/** غیرفعال‌سازی نظارتی پروموشن. */
export async function deactivateAdminPromotion(promotionId: string): Promise<AdminResult<null>> {
  try {
    const response = await fetch(`/v1/admin/promotions/${encodeURIComponent(promotionId)}/deactivate`, {
      method: "POST",
      headers: adminHeaders(),
    });
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: `admin.http.${response.status}` };
    }
    return { state: "ok", data: null, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** هدر Admin را برای کلاینت قدیمی Product Workspace فراهم می‌کند. */
export function adminHeaders(extra?: Record<string, string>): Record<string, string> {
  return { Accept: "application/json", [ADMIN_DEV_ACTOR_HEADER]: actorId(), ...(extra ?? {}) };
}
