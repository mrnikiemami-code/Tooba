/**
 * کلاینت خواندنی Admin؛ هویت توسعه فقط به Host ارسال می‌شود و هیچ مجوزی در مرورگر صادر نمی‌شود.
 */
import type { GridServerQuery } from "../../design-system/data-grid/types.ts";
import { postAdminGridQuery, type AdminGridQueryResult } from "../../design-system/app-data-grid/admin-grid-query-client.ts";
import {
  emptyReservationSummary,
  type AdminReservationCycleAudit,
  type AdminReservationCycleEventView,
  type AdminReservationCycleHistoryRow,
  type AdminReservationCycleSummary,
  type AdminReservationShortageLine,
} from "./admin-reservation-cycle.ts";
export {
  ADMIN_DEV_ACTOR_HEADER,
  ADMIN_ACTOR_STORAGE_KEY,
  DEFAULT_ADMIN_ACTOR_ID,
  type AdminLoadState,
  type AdminResult,
} from "../../lib/admin/admin-result.ts";

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
  sellerDisplayNames: string;
  lineCount: number;
  paymentState: string;
  status: string;
  payableAmount: number;
  currency: string;
  createdAt: string;
  supplyStatus: string;
  reservationLabel: string;
  reservationLabelEn: string;
  reservationState: string;
  reservationCycleNumber: number | null;
  reservationRetryPossible: boolean;
  reservationNeedsReacquire: boolean;
  reservationRetryLimitReached: boolean;
}

export interface AdminOrderLine {
  id: string;
  orderLineId: string | null;
  title: string;
  sellerDisplayName: string;
  quantity: number;
  unitAmount: number;
  linePayable: number;
  currency: string;
  quantityShipped: number | null;
  quantityPacked: number | null;
  quantityProcessing: number | null;
  quantityAllocated: number | null;
  imageUrl: string | null;
  operationalStatus: string | null;
  isReturnable: boolean | null;
  returnWindowDays: number | null;
  returnPolicyLabel: string | null;
  returnDeadlineDisplay: string | null;
  returnRemainingDisplay: string | null;
  returnStatusCode: string | null;
}

export interface AdminShipmentLine {
  orderLineId: string;
  quantity: number;
}

export interface AdminShipment {
  shipmentId: string;
  status: string;
  carrierDisplayName: string;
  trackingReference: string | null;
  itemCount: number;
  lines: AdminShipmentLine[];
  shippingMethodCode?: string | null;
  shippingMethodLabel?: string | null;
  activePackageId?: string | null;
  activePackageNumber?: string | null;
  packageLockedReasonFa?: string | null;
  canAddToConsolidatedPackage?: boolean;
}

export interface AdminConsolidatedPackageMember {
  shipmentId: string;
  sellerPartyId: string;
  fulfillmentId: string;
  joinedAt: string;
  releasedAt: string | null;
}

export interface AdminConsolidatedPackage {
  consolidatedPackageId: string;
  packageNumber: string;
  status: string;
  shippingMethodCode: string;
  shippingMethodLabel: string;
  trackingReference: string | null;
  note: string | null;
  sellerCount: number;
  memberShipmentCount: number;
  createdAt: string;
  dispatchedAt: string | null;
  deliveredAt: string | null;
  cancelledAt: string | null;
  members: AdminConsolidatedPackageMember[];
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
  fulfillmentId: string | null;
  fulfillmentStatus: string | null;
  shipments: AdminShipment[];
}

export interface AdminSellerFinancial {
  sellerOrderId: string;
  sellerPartyId: string;
  sellerDisplayName: string;
  lineCount: number;
  grossAmount: number;
  commissionAmount: number;
  payableAmount: number;
  currency: string;
  settlementStatus: string;
}

export interface AdminFinancialEvent {
  occurredAt: string;
  eventType: string;
  amount: number;
  currency: string;
  partyDisplayName: string;
  reference: string;
  paymentMethod: string;
  status: string;
  description: string;
}

export interface AdminFinancialSummary {
  totalSellerShare: number;
  totalCommission: number;
  grossOrderProfit: number;
  payableToSellers: number;
  customerGrossAmount: number;
  shippingCost: number;
  customerDiscounts: number;
  totalReceivedFromCustomer: number;
  currency: string;
}

export interface AdminOrderDetail {
  checkoutId: string;
  reference: string;
  createdAt: string;
  status: string;
  paymentState: string;
  lineCount: number;
  sellerCount: number;
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
  consolidatedPackages: AdminConsolidatedPackage[];
  sellerFinancials: AdminSellerFinancial[];
  financialEvents: AdminFinancialEvent[];
  financialSummary: AdminFinancialSummary;
  payment?: AdminPaymentOps | null;
  reservationCycle?: AdminReservationCycleAudit | null;
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
  customerTransferReference?: string | null;
  proofMediaAssetId?: string | null;
  evidenceSubmittedAt?: string | null;
  reservationLabel?: string;
  reservationLabelEn?: string;
  reservationState?: string;
  reservationCycleNumber?: number | null;
  reservationRetryPossible?: boolean;
  reservationNeedsReacquire?: boolean;
  reservationRetryLimitReached?: boolean;
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

/** مبلغ را برای فیلدهای اختیاری/ناموجود «—» برمی‌گرداند. */
export function formatAdminMoneyOptional(
  amount: number | null | undefined,
  currency = "IRR",
  missing = false,
): string {
  if (missing || amount == null || !Number.isFinite(amount)) return "—";
  return formatAdminMoney(amount, currency);
}

/** کد درگاه Host را برای اپراتور فارسی می‌کند. */
export function formatAdminPaymentProvider(providerCode: string | null | undefined): string {
  const code = (providerCode ?? "").trim();
  if (!code) return "—";
  const labels: Record<string, string> = {
    wallet: "کیف پول",
    fake: "درگاه آزمایشی",
    webhook: "درگاه وب‌هوک",
    manual: "کارت به کارت",
    "fail-closed": "درگاه غیرفعال",
  };
  const normalized = code.toLowerCase();
  if (labels[normalized]) return labels[normalized];
  if (/^[0-9a-f-]{32,36}$/i.test(code)) return "کیف پول";
  return code;
}

/** مرجع قابل‌نمایش پرداخت را از snapshot عملیاتی می‌سازد. */
export function formatAdminPaymentReference(payment: AdminPaymentOps): string {
  const tx = payment.providerTransactionReference?.trim();
  if (tx) return tx;
  const req = payment.providerRequestReference?.trim();
  if (!req) return payment.paymentId.slice(0, 12);
  if (req.includes("|")) {
    const parts = req.split("|");
    if (parts[0]?.toLowerCase() === "w" && parts.length > 1) {
      return `wallet:${parts[1].slice(0, 8)}…`;
    }
  }
  return req.length > 24 ? `${req.slice(0, 24)}…` : req;
}

function isSuccessfulPaymentStatus(status: string): boolean {
  return status === "Succeeded" || status === "Captured" || status === "Paid";
}

function countOrderLines(sellerOrders: AdminSellerOrder[]): number {
  return sellerOrders.reduce((sum, order) => sum + order.lines.length, 0);
}

function countDistinctSellers(sellerOrders: AdminSellerOrder[]): number {
  const ids = new Set(sellerOrders.map((order) => order.id || order.orderNumber).filter(Boolean));
  return ids.size;
}

function synthesizeSellerFinancials(sellerOrders: AdminSellerOrder[]): AdminSellerFinancial[] {
  return sellerOrders.flatMap((order) => {
    const sellerOrderId = order.id;
    if (!sellerOrderId) return [];
    const lineCount = order.lines.length;
    const grossFromLines = order.lines.reduce((sum, line) => sum + line.linePayable, 0);
    const grossAmount = grossFromLines > 0 ? grossFromLines : order.payableAmount;
    const settlementStatus = order.paymentState === "Paid"
      ? "WaitingForSettlement"
      : "NotSettled";
    return [{
      sellerOrderId,
      sellerPartyId: "",
      sellerDisplayName: order.sellerDisplayName,
      lineCount,
      grossAmount,
      commissionAmount: 0,
      payableAmount: order.payableAmount,
      currency: order.currency,
      settlementStatus,
    }];
  });
}

function synthesizeFinancialEvents(
  detail: AdminOrderDetail,
): AdminFinancialEvent[] {
  const payment = detail.payment;
  if (!payment || !isSuccessfulPaymentStatus(payment.status)) return [];
  return [{
    occurredAt: payment.completedAt ?? payment.createdAt,
    eventType: "CustomerReceipt",
    amount: payment.amount,
    currency: payment.currency,
    partyDisplayName: detail.recipientName || "مشتری توبا",
    reference: formatAdminPaymentReference(payment),
    paymentMethod: formatAdminPaymentProvider(payment.providerCode),
    status: payment.status,
    description: "دریافت از مشتری",
  }];
}

function isEmptyFinancialSummary(summary: AdminFinancialSummary): boolean {
  return summary.totalSellerShare === 0
    && summary.totalCommission === 0
    && summary.payableToSellers === 0
    && summary.customerGrossAmount === 0
    && summary.totalReceivedFromCustomer === 0;
}

function synthesizeFinancialSummary(detail: AdminOrderDetail): AdminFinancialSummary {
  const sellerFinancials = detail.sellerFinancials;
  const currency = detail.currency || "IRR";
  const totalSellerShare = sellerFinancials.reduce((sum, row) => sum + row.grossAmount, 0);
  const totalCommission = sellerFinancials.reduce((sum, row) => sum + row.commissionAmount, 0);
  const payableToSellers = sellerFinancials.reduce((sum, row) => sum + row.payableAmount, 0);
  const customerGrossAmount = detail.subtotal > 0
    ? detail.subtotal
    : sellerFinancials.reduce((sum, row) => sum + row.grossAmount, 0);
  const totalReceivedFromCustomer = detail.payment?.amount
    ?? (detail.paymentState === "Paid" ? detail.payableAmount : 0);
  return {
    totalSellerShare,
    totalCommission,
    grossOrderProfit: totalCommission,
    payableToSellers,
    customerGrossAmount,
    shippingCost: 0,
    customerDiscounts: detail.discountAmount,
    totalReceivedFromCustomer,
    currency,
  };
}

/** پس از نگاشت خام، شمارنده‌ها و برش مالی را از Order/Payment تکمیل می‌کند. */
export function enrichAdminOrderDetail(detail: AdminOrderDetail): AdminOrderDetail {
  const sellerOrders = detail.sellerOrders;
  let lineCount = detail.lineCount;
  let sellerCount = detail.sellerCount;
  if (lineCount <= 0 && sellerOrders.length > 0) {
    lineCount = countOrderLines(sellerOrders);
  }
  if (sellerCount <= 0 && sellerOrders.length > 0) {
    sellerCount = countDistinctSellers(sellerOrders);
  }

  let sellerFinancials = detail.sellerFinancials;
  if (sellerFinancials.length === 0 && sellerOrders.length > 0) {
    sellerFinancials = synthesizeSellerFinancials(sellerOrders);
  }

  let financialEvents = detail.financialEvents.map((event) => ({
    ...event,
    paymentMethod: formatAdminPaymentProvider(event.paymentMethod),
  }));
  if (financialEvents.length === 0) {
    financialEvents = synthesizeFinancialEvents(detail);
  }

  let financialSummary = detail.financialSummary;
  if (isEmptyFinancialSummary(financialSummary) && (sellerFinancials.length > 0 || detail.payment)) {
    financialSummary = synthesizeFinancialSummary({ ...detail, sellerFinancials });
  }

  return {
    ...detail,
    lineCount,
    sellerCount,
    sellerFinancials,
    financialEvents,
    financialSummary,
    consolidatedPackages: detail.consolidatedPackages ?? [],
  };
}

/** وضعیت‌های Host را برای اپراتور فارسی می‌کند. */
export function formatAdminStatus(status: string): string {
  const labels: Record<string, string> = {
    Active: "فعال",
    Published: "منتشرشده",
    Draft: "پیش‌نویس",
    Archived: "بایگانی",
    Suspended: "معلق",
    Paid: "پرداخت‌شده",
    Unpaid: "پرداخت‌نشده",
    Pending: "در انتظار",
    PendingManualConfirmation: "در انتظار تأیید واریز",
    AwaitingManualDeposit: "در انتظار تأیید واریز",
    PendingPayment: "در انتظار پرداخت",
    Submitted: "ثبت‌شده",
    ReservationRequested: "در انتظار بررسی",
    Processing: "در حال پردازش",
    Mixed: "ترکیبی",
    Cancelled: "لغو شده",
    Canceled: "لغو شده",
    Failed: "ناموفق",
    Succeeded: "موفق",
    Verified: "تأییدشده",
    Refunded: "بازگشت وجه",
    RefundCompleted: "بازگشت وجه انجام شد",
    RefundPending: "بازگشت وجه در انتظار",
    PartiallyRefunded: "بازگشت وجه جزئی",
    Authorized: "مجاز",
    Captured: "دریافت‌شده",
    Expired: "مهلت پرداخت پایان یافته",
    PaymentExpired: "مهلت پرداخت پایان یافته",
    AwaitingPaymentExpired: "مهلت پرداخت پایان یافته",
    ReadyToShip: "آماده ارسال",
    ReadyToFulfill: "آماده پردازش",
    Packed: "بسته‌بندی‌شده",
    PartialDispatched: "ارسال جزئی",
    Dispatched: "ارسال‌شده",
    InTransit: "در مسیر تحویل",
    Created: "ایجادشده",
    Shipped: "ارسال‌شده",
    Delivered: "تحویل‌شده",
    Fulfilled: "تکمیل‌شده",
    InFulfillment: "در حال ارسال",
    AwaitingShipment: "در انتظار ارسال",
    Returned: "مرجوعی",
    ReturnRequested: "مرجوعی در انتظار بررسی",
    ReturnApproved: "مرجوعی تأیید شده",
    Rejected: "ردشده",
    Approved: "تأییدشده",
    Open: "باز",
    Closed: "بسته",
    Completed: "تکمیل‌شده",
    RefundFailed: "شکست بازگشت وجه",
    RefundProcessing: "بازگشت وجه در انتظار",
    Reserved: "تأمین‌شده",
    AvailableForReacquire: "قابل تأمین",
    Unavailable: "غیرقابل تأمین",
    PartiallyUnavailable: "تأمین ناقص",
    NotApplicable: "نامرتبط",
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

/** برچسب ستون فروشنده: یک فروشنده → نام؛ چند فروشنده → «N فروشنده». */
export function formatOrderSellerLabel(row: Pick<AdminOrderRow, "sellerCount" | "sellerDisplayNames">): string {
  if (row.sellerCount <= 0) return "—";
  if (row.sellerCount === 1) {
    const name = row.sellerDisplayNames.trim();
    return name || "—";
  }
  return `${row.sellerCount.toLocaleString("fa-IR")} فروشنده`;
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
    sellerDisplayNames: text(
      prop(item, "sellerDisplayNames", "SellerDisplayNames"),
      text(prop(item, "sellerDisplayName", "SellerDisplayName"), "—"),
    ),
    lineCount: number(prop(item, "lineCount", "LineCount") ?? prop(item, "itemCount", "ItemCount")),
    paymentState: text(prop(item, "paymentState", "PaymentState")),
    status: text(prop(item, "status", "Status")),
    payableAmount: number(prop(item, "payableAmount", "PayableAmount")),
    currency: text(prop(item, "currency", "Currency"), "IRR"),
    createdAt: text(prop(item, "createdAt", "CreatedAt"), text(prop(item, "submittedAt", "SubmittedAt"))),
    supplyStatus: text(prop(item, "supplyStatus", "SupplyStatus"), "NotApplicable"),
    ...mapReservationSummaryFields(item),
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
      const orderLineId = text(prop(line, "orderLineId", "OrderLineId")) || null;
      const offerId = text(prop(line, "offerId", "OfferId"), `${sellerOrderId}-${index}`);
      const shippedRaw = prop(line, "quantityShipped", "QuantityShipped");
      const packedRaw = prop(line, "quantityPacked", "QuantityPacked");
      const processingRaw = prop(line, "quantityProcessing", "QuantityProcessing");
      const allocatedRaw = prop(line, "quantityAllocated", "QuantityAllocated");
      const isReturnableRaw = prop(line, "isReturnable", "IsReturnable");
      const windowRaw = prop(line, "returnWindowDays", "ReturnWindowDays");
      return [{
        id: orderLineId || offerId,
        orderLineId,
        title: text(prop(line, "title", "Title"), text(prop(line, "productTitle", "ProductTitle"), "کالای سفارش")),
        sellerDisplayName: text(prop(line, "sellerDisplayName", "SellerDisplayName"), text(prop(seller, "sellerDisplayName", "SellerDisplayName"), "فروشنده")),
        quantity: number(prop(line, "quantity", "Quantity")),
        unitAmount: number(prop(line, "unitAmount", "UnitAmount")),
        linePayable: number(prop(line, "linePayable", "LinePayable")),
        currency: text(prop(line, "currency", "Currency"), "IRR"),
        quantityShipped: shippedRaw == null || shippedRaw === "" ? null : number(shippedRaw),
        quantityPacked: packedRaw == null || packedRaw === "" ? null : number(packedRaw),
        quantityProcessing: processingRaw == null || processingRaw === "" ? null : number(processingRaw),
        quantityAllocated: allocatedRaw == null || allocatedRaw === "" ? null : number(allocatedRaw),
        imageUrl: text(prop(line, "imageUrl", "ImageUrl")) || null,
        operationalStatus: text(prop(line, "operationalStatus", "OperationalStatus")) || null,
        isReturnable: typeof isReturnableRaw === "boolean" ? isReturnableRaw : isReturnableRaw == null ? null : Boolean(isReturnableRaw),
        returnWindowDays: windowRaw == null || windowRaw === "" ? null : number(windowRaw),
        returnPolicyLabel: text(prop(line, "returnPolicyLabel", "ReturnPolicyLabel")) || null,
        returnDeadlineDisplay: text(prop(line, "returnDeadlineDisplay", "ReturnDeadlineDisplay")) || null,
        returnRemainingDisplay: text(prop(line, "returnRemainingDisplay", "ReturnRemainingDisplay")) || null,
        returnStatusCode: text(prop(line, "returnStatusCode", "ReturnStatusCode")) || null,
      }];
    });
    const shipments = array(prop(seller, "shipments", "Shipments")).flatMap((raw): AdminShipment[] => {
      const row = record(raw);
      if (!row) return [];
      const shipmentId = text(prop(row, "shipmentId", "ShipmentId"));
      if (!shipmentId) return [];
      return [{
        shipmentId,
        status: text(prop(row, "status", "Status")),
        carrierDisplayName: text(prop(row, "carrierDisplayName", "CarrierDisplayName"), "—"),
        trackingReference: text(prop(row, "trackingReference", "TrackingReference")) || null,
        itemCount: number(prop(row, "itemCount", "ItemCount")),
        lines: array(prop(row, "lines", "Lines")).flatMap((lineRaw): AdminShipmentLine[] => {
          const line = record(lineRaw);
          if (!line) return [];
          const orderLineId = text(prop(line, "orderLineId", "OrderLineId"));
          if (!orderLineId) return [];
          return [{ orderLineId, quantity: number(prop(line, "quantity", "Quantity")) }];
        }),
        shippingMethodCode: text(prop(row, "shippingMethodCode", "ShippingMethodCode")) || null,
        shippingMethodLabel: text(prop(row, "shippingMethodLabel", "ShippingMethodLabel")) || null,
        activePackageId: text(prop(row, "activePackageId", "ActivePackageId")) || null,
        activePackageNumber: text(prop(row, "activePackageNumber", "ActivePackageNumber")) || null,
        packageLockedReasonFa: text(prop(row, "packageLockedReasonFa", "PackageLockedReasonFa")) || null,
        canAddToConsolidatedPackage: Boolean(prop(row, "canAddToConsolidatedPackage", "CanAddToConsolidatedPackage")),
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
      fulfillmentId: text(prop(seller, "fulfillmentId", "FulfillmentId")) || null,
      fulfillmentStatus: text(prop(seller, "fulfillmentStatus", "FulfillmentStatus")) || null,
      shipments,
    }];
  });
  const consolidatedPackages = array(prop(item, "consolidatedPackages", "ConsolidatedPackages")).flatMap((raw): AdminConsolidatedPackage[] => {
    const row = record(raw);
    if (!row) return [];
    const consolidatedPackageId = text(prop(row, "consolidatedPackageId", "ConsolidatedPackageId"));
    if (!consolidatedPackageId) return [];
    return [{
      consolidatedPackageId,
      packageNumber: text(prop(row, "packageNumber", "PackageNumber"), "MP"),
      status: text(prop(row, "status", "Status")),
      shippingMethodCode: text(prop(row, "shippingMethodCode", "ShippingMethodCode")),
      shippingMethodLabel: text(prop(row, "shippingMethodLabel", "ShippingMethodLabel"), "—"),
      trackingReference: text(prop(row, "trackingReference", "TrackingReference")) || null,
      note: text(prop(row, "note", "Note")) || null,
      sellerCount: number(prop(row, "sellerCount", "SellerCount")),
      memberShipmentCount: number(prop(row, "memberShipmentCount", "MemberShipmentCount")),
      createdAt: text(prop(row, "createdAt", "CreatedAt")),
      dispatchedAt: text(prop(row, "dispatchedAt", "DispatchedAt")) || null,
      deliveredAt: text(prop(row, "deliveredAt", "DeliveredAt")) || null,
      cancelledAt: text(prop(row, "cancelledAt", "CancelledAt")) || null,
      members: array(prop(row, "members", "Members")).flatMap((memberRaw): AdminConsolidatedPackageMember[] => {
        const member = record(memberRaw);
        if (!member) return [];
        const shipmentId = text(prop(member, "shipmentId", "ShipmentId"));
        if (!shipmentId) return [];
        return [{
          shipmentId,
          sellerPartyId: text(prop(member, "sellerPartyId", "SellerPartyId")),
          fulfillmentId: text(prop(member, "fulfillmentId", "FulfillmentId")),
          joinedAt: text(prop(member, "joinedAt", "JoinedAt")),
          releasedAt: text(prop(member, "releasedAt", "ReleasedAt")) || null,
        }];
      }),
    }];
  });
  const mapped: AdminOrderDetail = {
    checkoutId,
    reference: text(prop(item, "reference", "Reference"), "سفارش"),
    createdAt: text(prop(item, "createdAt", "CreatedAt"), text(prop(item, "submittedAt", "SubmittedAt"))),
    status: text(prop(item, "status", "Status")),
    paymentState: text(prop(item, "paymentState", "PaymentState")),
    lineCount: number(prop(item, "lineCount", "LineCount")),
    sellerCount: number(prop(item, "sellerCount", "SellerCount")),
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
    consolidatedPackages,
    sellerFinancials: array(prop(item, "sellerFinancials", "SellerFinancials")).flatMap((raw): AdminSellerFinancial[] => {
      const row = record(raw);
      if (!row) return [];
      const sellerOrderId = text(prop(row, "sellerOrderId", "SellerOrderId"));
      if (!sellerOrderId) return [];
      return [{
        sellerOrderId,
        sellerPartyId: text(prop(row, "sellerPartyId", "SellerPartyId")),
        sellerDisplayName: text(prop(row, "sellerDisplayName", "SellerDisplayName"), "فروشنده"),
        lineCount: number(prop(row, "lineCount", "LineCount")),
        grossAmount: number(prop(row, "grossAmount", "GrossAmount")),
        commissionAmount: number(prop(row, "commissionAmount", "CommissionAmount")),
        payableAmount: number(prop(row, "payableAmount", "PayableAmount")),
        currency: text(prop(row, "currency", "Currency"), "IRR"),
        settlementStatus: text(prop(row, "settlementStatus", "SettlementStatus"), "NotSettled"),
      }];
    }),
    financialEvents: array(prop(item, "financialEvents", "FinancialEvents")).flatMap((raw): AdminFinancialEvent[] => {
      const row = record(raw);
      if (!row) return [];
      return [{
        occurredAt: text(prop(row, "occurredAt", "OccurredAt")),
        eventType: text(prop(row, "eventType", "EventType")),
        amount: number(prop(row, "amount", "Amount")),
        currency: text(prop(row, "currency", "Currency"), "IRR"),
        partyDisplayName: text(prop(row, "partyDisplayName", "PartyDisplayName"), "—"),
        reference: text(prop(row, "reference", "Reference")),
        paymentMethod: text(prop(row, "paymentMethod", "PaymentMethod")),
        status: text(prop(row, "status", "Status")),
        description: text(prop(row, "description", "Description")),
      }];
    }),
    financialSummary: mapAdminFinancialSummary(prop(item, "financialSummary", "FinancialSummary")),
    payment: mapAdminPaymentOps(prop(item, "payment", "Payment")),
    reservationCycle: mapAdminReservationCycleAudit(prop(item, "reservationCycle", "ReservationCycle")),
  };
  return enrichAdminOrderDetail(mapped);
}

function mapAdminFinancialSummary(value: unknown): AdminFinancialSummary {
  const item = record(value);
  if (!item) {
    return {
      totalSellerShare: 0,
      totalCommission: 0,
      grossOrderProfit: 0,
      payableToSellers: 0,
      customerGrossAmount: 0,
      shippingCost: 0,
      customerDiscounts: 0,
      totalReceivedFromCustomer: 0,
      currency: "IRR",
    };
  }
  return {
    totalSellerShare: number(prop(item, "totalSellerShare", "TotalSellerShare")),
    totalCommission: number(prop(item, "totalCommission", "TotalCommission")),
    grossOrderProfit: number(prop(item, "grossOrderProfit", "GrossOrderProfit")),
    payableToSellers: number(prop(item, "payableToSellers", "PayableToSellers")),
    customerGrossAmount: number(prop(item, "customerGrossAmount", "CustomerGrossAmount")),
    shippingCost: number(prop(item, "shippingCost", "ShippingCost")),
    customerDiscounts: number(prop(item, "customerDiscounts", "CustomerDiscounts")),
    totalReceivedFromCustomer: number(prop(item, "totalReceivedFromCustomer", "TotalReceivedFromCustomer")),
    currency: text(prop(item, "currency", "Currency"), "IRR"),
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
    customerTransferReference: text(prop(item, "customerTransferReference", "CustomerTransferReference")) || null,
    proofMediaAssetId: text(prop(item, "proofMediaAssetId", "ProofMediaAssetId")) || null,
    evidenceSubmittedAt: text(prop(item, "evidenceSubmittedAt", "EvidenceSubmittedAt")) || null,
    ...mapReservationSummaryFields(item),
  };
}

function mapReservationSummaryFields(item: Record<string, unknown>): AdminReservationCycleSummary {
  const fallback = emptyReservationSummary();
  const cycleRaw = prop(item, "reservationCycleNumber", "ReservationCycleNumber");
  const cycleNumber = cycleRaw === null || cycleRaw === undefined || cycleRaw === ""
    ? null
    : number(cycleRaw);
  return {
    reservationLabel: text(prop(item, "reservationLabel", "ReservationLabel"), fallback.reservationLabel),
    reservationLabelEn: text(prop(item, "reservationLabelEn", "ReservationLabelEn"), fallback.reservationLabelEn),
    reservationState: text(prop(item, "reservationState", "ReservationState"), fallback.reservationState),
    reservationCycleNumber: cycleNumber === 0 && (cycleRaw === null || cycleRaw === undefined) ? null : cycleNumber || null,
    reservationRetryPossible: Boolean(prop(item, "reservationRetryPossible", "ReservationRetryPossible")),
    reservationNeedsReacquire: Boolean(prop(item, "reservationNeedsReacquire", "ReservationNeedsReacquire")),
    reservationRetryLimitReached: Boolean(prop(item, "reservationRetryLimitReached", "ReservationRetryLimitReached")),
  };
}

export function mapAdminReservationCycleAudit(value: unknown): AdminReservationCycleAudit | null {
  const item = record(value);
  if (!item) return null;
  const statusFa = text(prop(item, "statusLabelFa", "StatusLabelFa"));
  if (!statusFa) return null;
  return {
    statusLabelFa: statusFa,
    statusLabelEn: text(prop(item, "statusLabelEn", "StatusLabelEn")),
    reasonLabelFa: text(prop(item, "reasonLabelFa", "ReasonLabelFa"), "—"),
    reasonLabelEn: text(prop(item, "reasonLabelEn", "ReasonLabelEn"), "—"),
    currentCycleNumber: optionalNumber(prop(item, "currentCycleNumber", "CurrentCycleNumber")),
    totalCyclesUsed: number(prop(item, "totalCyclesUsed", "TotalCyclesUsed")),
    maxCycles: number(prop(item, "maxCycles", "MaxCycles")),
    retryCountRemaining: number(prop(item, "retryCountRemaining", "RetryCountRemaining")),
    startedAt: text(prop(item, "startedAt", "StartedAt")) || null,
    expiresAt: text(prop(item, "expiresAt", "ExpiresAt")) || null,
    serverTime: text(prop(item, "serverTime", "ServerTime")),
    secondsRemaining: number(prop(item, "secondsRemaining", "SecondsRemaining")),
    supplyStatus: text(prop(item, "supplyStatus", "SupplyStatus"), "NotApplicable"),
    supplyStatusLabelFa: text(prop(item, "supplyStatusLabelFa", "SupplyStatusLabelFa")),
    retryLimitReached: Boolean(prop(item, "retryLimitReached", "RetryLimitReached")),
    canRetryReservation: Boolean(prop(item, "canRetryReservation", "CanRetryReservation")),
    canExtendTimer: Boolean(prop(item, "canExtendTimer", "CanExtendTimer")),
    history: array(prop(item, "history", "History")).flatMap((raw): AdminReservationCycleHistoryRow[] => {
      const row = record(raw);
      if (!row) return [];
      return [{
        cycleNumber: number(prop(row, "cycleNumber", "CycleNumber")),
        statusLabelFa: text(prop(row, "statusLabelFa", "StatusLabelFa")),
        statusLabelEn: text(prop(row, "statusLabelEn", "StatusLabelEn")),
        reasonLabelFa: text(prop(row, "reasonLabelFa", "ReasonLabelFa"), "—"),
        reasonLabelEn: text(prop(row, "reasonLabelEn", "ReasonLabelEn"), "—"),
        startedAt: text(prop(row, "startedAt", "StartedAt")),
        expiresAt: text(prop(row, "expiresAt", "ExpiresAt")),
        endedAt: text(prop(row, "endedAt", "EndedAt")) || null,
        effectiveHoldMinutes: number(prop(row, "effectiveHoldMinutes", "EffectiveHoldMinutes")),
        effectiveMaxCycles: number(prop(row, "effectiveMaxCycles", "EffectiveMaxCycles")),
        policySource: text(prop(row, "policySource", "PolicySource")),
        policySourceLabelFa: text(prop(row, "policySourceLabelFa", "PolicySourceLabelFa")),
        policySourceLabelEn: text(prop(row, "policySourceLabelEn", "PolicySourceLabelEn")),
        paymentAttemptRef: text(prop(row, "paymentAttemptRef", "PaymentAttemptRef")) || null,
      }];
    }),
    events: array(prop(item, "events", "Events")).flatMap((raw): AdminReservationCycleEventView[] => {
      const row = record(raw);
      if (!row) return [];
      return [{
        kindLabelFa: text(prop(row, "kindLabelFa", "KindLabelFa")),
        kindLabelEn: text(prop(row, "kindLabelEn", "KindLabelEn")),
        occurredAt: text(prop(row, "occurredAt", "OccurredAt")),
        cycleNumber: optionalNumber(prop(row, "cycleNumber", "CycleNumber")),
        detailFa: text(prop(row, "detailFa", "DetailFa")),
        detailEn: text(prop(row, "detailEn", "DetailEn")),
      }];
    }),
    shortages: array(prop(item, "shortages", "Shortages")).flatMap((raw): AdminReservationShortageLine[] => {
      const row = record(raw);
      if (!row) return [];
      return [{
        itemTitle: text(prop(row, "itemTitle", "ItemTitle"), "قلم"),
        required: number(prop(row, "required", "Required")),
        available: number(prop(row, "available", "Available")),
        shortage: number(prop(row, "shortage", "Shortage")),
        unitCode: text(prop(row, "unitCode", "UnitCode")) || null,
      }];
    }),
  };
}

function optionalNumber(value: unknown): number | null {
  if (value === null || value === undefined || value === "") return null;
  const n = number(value);
  return Number.isFinite(n) ? n : null;
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





export type AdminOrderSupplyStatus = {
  checkoutId: string;
  status: string;
  lines: Array<{
    itemTitle: string | null;
    unitCode: string | null;
    required: number;
    available: number;
    shortage: number;
    lineStatus: string;
  }>;
};

/** تصویر CheckOnly تأمین سفارش — یک درخواست برای جزئیات. */
export async function loadAdminOrderSupply(checkoutId: string): Promise<AdminResult<AdminOrderSupplyStatus>> {
  try {
    const response = await fetch(`/v1/admin/orders/${encodeURIComponent(checkoutId)}/supply-status`, {
      headers: adminHeaders(),
    });
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
    }
    const item = record(await response.json().catch(() => null));
    const id = text(prop(item ?? {}, "checkoutId", "CheckoutId"));
    if (!item || !id) {
      return { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
    }
    const rawLines = prop(item, "lines", "Lines");
    return {
      state: "ok",
      status: response.status,
      data: {
        checkoutId: id,
        status: text(prop(item, "status", "Status"), "NotApplicable"),
        lines: Array.isArray(rawLines)
          ? rawLines.flatMap((line) => {
              const row = record(line);
              if (!row) return [];
              return [{
                itemTitle: text(prop(row, "itemTitle", "ItemTitle")) || null,
                unitCode: text(prop(row, "unitCode", "UnitCode")) || null,
                required: number(prop(row, "required", "Required")),
                available: number(prop(row, "available", "Available")),
                shortage: number(prop(row, "shortage", "Shortage")),
                lineStatus: text(prop(row, "lineStatus", "LineStatus"), "NotApplicable"),
              }];
            })
          : [],
      },
    };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** Server GridQuery — سفارش‌های Admin. */
export function queryAdminOrdersGrid(query: GridServerQuery): Promise<AdminGridQueryResult<AdminOrderRow>> {
  return postAdminGridQuery("/v1/admin/orders/query", query, adminHeaders(), (item) => mapAdminOrder(item));
}





/** هدر Admin را برای کلاینت قدیمی Product Workspace فراهم می‌کند. */
export function adminHeaders(extra?: Record<string, string>): Record<string, string> {
  return { Accept: "application/json", [ADMIN_DEV_ACTOR_HEADER]: actorId(), ...(extra ?? {}) };
}
