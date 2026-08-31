"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import { CheckCircle, ChevronDown, ChevronUp, Eye, EyeOff, LayoutTemplate, Package, ShoppingBag, Star, Store, Users } from "lucide-react";
import { ErrorState, faWorkspaceMessages, LegacyAppDataGrid } from "../../design-system";
import type { GridColumnDef, SavedViewStore } from "../../design-system/data-grid";
import {
  formatAdminDate,
  formatAdminMoney,
  formatAdminStatus,
  loadAdminCustomers,
  loadAdminDashboard,
  loadAdminOrderDetail,
  loadAdminOrders,
  loadAdminSellers,
  loadAdminReviews,
  moderateAdminReview,
  loadAdminPromotions,
  deactivateAdminPromotion,
  type AdminCustomerRow,
  type AdminDashboard,
  type AdminLoadState,
  type AdminOrderDetail,
  type AdminOrderRow,
  type AdminResult,
  type AdminSellerRow,
  type AdminReviewRow,
  type AdminPromotionRow,
} from "./admin-api";
import { ADMIN_ORDER_GRID_VIEW_KEY, createHostSavedViewStore, ADMIN_FULFILLMENT_GRID_VIEW_KEY, ADMIN_RETURN_GRID_VIEW_KEY, ADMIN_SELLER_GRID_VIEW_KEY, ADMIN_CUSTOMER_GRID_VIEW_KEY, ADMIN_SETTLEMENT_GRID_VIEW_KEY, ADMIN_REVIEW_GRID_VIEW_KEY, ADMIN_PROMOTION_GRID_VIEW_KEY, ADMIN_PAYOUT_GRID_VIEW_KEY, ADMIN_CONTENT_GRID_VIEW_KEY } from "./saved-view-store";
import {
  formatFulfillmentStatus,
  fulfillmentStatusBadgeClass,
  loadAdminFulfillmentDetail,
  loadAdminFulfillments,
  type FulfillmentListRow,
  type FulfillmentSnapshot,
} from "../fulfillment/fulfillment-api";
import { FulfillmentShipmentList } from "../fulfillment/fulfillment-ui";
import {
  adminRetryReturnRefund,
  formatReturnDate,
  formatReturnStatus,
  loadAdminReturnDetail,
  loadAdminReturns,
  returnStatusBadgeClass,
  type ReturnListRow,
  type ReturnSnapshot,
} from "../returns/return-api";
import { ReturnDetailCard } from "../returns/return-ui";
import {
  formatPayoutStatus,
  formatSettlementMoney,
  loadAdminPayoutQueue,
  loadAdminSettlementBalances,
  payoutStatusClass,
  processAdminPayout,
  type PayoutRequestRow,
  type SettlementBalance,
} from "../settlement/settlement-api";
import {
  createAdminArticle,
  formatContentDate,
  loadAdminContentArticles,
  publishAdminArticle,
  unpublishAdminArticle,
  type AdminContentArticle,
} from "../content/content-api";
import {
  ADMIN_STORY_CAPABILITIES,
  StoryManagementScreen,
} from "../stories/management";
import {
  loadAdminHomeComposition,
  reorderAdminHomeSections,
  restoreDefaultAdminHomeComposition,
  SECTION_TYPE_LABELS,
  updateAdminHomeSection,
  type AdminHomeCompositionSectionItem,
} from "../composition/composition-api";

function Denied({ retry }: { retry: () => void }) {
  return (
    <div data-testid="admin-auth-denied">
      <ErrorState
        title="دسترسی مجاز نیست"
        detail="سامانه هویت فعلی را مدیر تشخیص نداد. تغییر مسیر یا هدر مرورگر مجوز ایجاد نمی‌کند."
        onRetry={retry}
        retryLabel={faWorkspaceMessages.retry}
      />
    </div>
  );
}

function PageHeading({ title, description }: { title: string; description: string }) {
  return (
    <div className="mb-5">
      <p className="text-sm text-muted">خانه / {title}</p>
      <h1 className="mt-1 text-2xl font-semibold tracking-tight">{title}</h1>
      <p className="mt-1 text-base text-muted">{description}</p>
    </div>
  );
}

/** داشبورد Admin با تراکم Shopeiva و فقط متریک‌های زندهٔ Host؛ بدون نمودار/درآمد جعلی. */
export function AdminDashboardScreen() {
  const [result, setResult] = useState<AdminResult<AdminDashboard>>({ state: "ok", data: null, status: 0 });
  const refresh = () => void loadAdminDashboard().then(setResult);
  useEffect(refresh, []);
  if (result.state === "denied") return <Denied retry={refresh} />;
  return (
    <main className="space-y-6" data-testid="admin-dashboard">
      <div className="bg-gradient-to-l from-[#2563EB] to-[#3B82F6] rounded-2xl p-5 md:p-6 text-white shadow-lg shadow-[#2563EB]/20">
        <p className="text-white/80 text-sm">خانه / داشبورد</p>
        <h1 className="mt-1 text-2xl md:text-3xl font-black">مرکز عملیات توبا</h1>
        <p className="mt-2 text-sm text-white/90 max-w-2xl leading-7">
          خلاصهٔ زنده از فروشگاه. درآمد، مجموع فروش، نرخ تبدیل و نمودار ساختگی نمایش داده نمی‌شود.
        </p>
      </div>
      {result.state === "error" ? (
        <ErrorState title="فروشگاه در دسترس نیست" detail={result.message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
      ) : (
        <>
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <Summary label="محصول فعال" value={result.data?.activeProducts} icon={<Package className="size-5" />} tone="from-blue-500 to-blue-600" />
            <Summary label="سفارش باز" value={result.data?.openOrders} icon={<ShoppingBag className="size-5" />} tone="from-amber-500 to-amber-600" />
            <Summary label="فروشنده" value={result.data?.sellersCount} icon={<Store className="size-5" />} tone="from-violet-500 to-violet-600" />
            <Summary label="مشتری سفارش‌دهنده" value={result.data?.customersCount} icon={<Users className="size-5" />} tone="from-emerald-500 to-emerald-600" />
          </div>
          <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3">
            {[
              { label: "محصولات", href: "/admin/products", icon: Package },
              { label: "سفارش‌ها", href: "/admin/orders", icon: ShoppingBag },
              { label: "فروشندگان", href: "/admin/sellers", icon: Store },
              { label: "مشتریان", href: "/admin/customers", icon: Users },
              { label: "نظرات", href: "/admin/reviews", icon: Star },
            ].map((action) => (
              <Link
                key={action.href}
                href={action.href}
                className="bg-white rounded-2xl border border-gray-200 p-4 flex flex-col items-center gap-2 hover:shadow-md transition-shadow text-center"
              >
                <span className="w-10 h-10 bg-[#2563EB] text-white rounded-xl flex items-center justify-center">
                  <action.icon className="w-5 h-5" />
                </span>
                <span className="text-xs font-bold text-gray-800">{action.label}</span>
              </Link>
            ))}
          </div>
          <section className="rounded-2xl border border-gray-200 bg-white p-5 shadow-sm">
            <h2 className="text-lg font-black">وضعیت سفارش و عرضه</h2>
            <p className="mt-1 text-sm text-gray-500">اعداد فقط از داشبورد عملیاتی فروشگاه.</p>
            <div className="mt-4 grid gap-3 sm:grid-cols-3">
              <Metric label="پیشنهاد فعال" value={result.data?.activeOffers} />
              <Metric label="پرداخت‌شده" value={result.data?.paidOrders} />
              <Metric label="در انتظار پرداخت" value={result.data?.pendingOrders} />
            </div>
          </section>
        </>
      )}
    </main>
  );
}

function Summary({ label, value, icon, tone }: { label: string; value?: number; icon: ReactNode; tone: string }) {
  return (
    <div className="rounded-2xl border border-gray-200 bg-white p-5 shadow-sm">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-sm text-gray-500">{label}</p>
          <p className="mt-2 text-3xl font-black tabular-nums">{value?.toLocaleString("fa-IR") ?? "…"}</p>
        </div>
        <span className={`inline-flex size-11 items-center justify-center rounded-xl bg-gradient-to-br ${tone} text-white`}>{icon}</span>
      </div>
    </div>
  );
}

function Metric({ label, value }: { label: string; value?: number }) {
  return (
    <div className="flex items-center justify-between rounded-xl bg-gray-50 px-4 py-3">
      <span>{label}</span>
      <strong className="tabular-nums">{value?.toLocaleString("fa-IR") ?? "…"}</strong>
    </div>
  );
}

function GridPage<T extends { id: string }>({
  title,
  description,
  loader,
  columns,
  gridId,
  savedViewStore: savedViewStoreInput,
}: {
  title: string;
  description: string;
  loader: () => Promise<AdminResult<T[]>>;
  columns: GridColumnDef<T>[];
  gridId: string;
  savedViewStore?: SavedViewStore;
}) {
  const savedViewStore = useMemo(
    () => savedViewStoreInput ?? createHostSavedViewStore(gridId),
    [gridId, savedViewStoreInput],
  );
  const [state, setState] = useState<AdminLoadState | "loading">("loading");
  const [rows, setRows] = useState<T[]>([]);
  const [message, setMessage] = useState<string>();
  const refresh = () => void loader().then((result) => {
    setState(result.state);
    setRows(result.data ?? []);
    setMessage(result.message);
  });
  useEffect(refresh, [loader]);
  if (state === "denied") return <Denied retry={refresh} />;
  return (
    <main>
      <PageHeading title={title} description={description} />
      <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
        <div className="flex items-center justify-between gap-3 border-b border-border px-4 py-3 md:px-5">
          <span className="text-sm text-muted">{state === "ok" ? "دادهٔ زندهٔ فروشگاه" : state === "loading" ? "در حال بارگذاری" : "اتصال برقرار نیست"}</span>
          <span className="rounded-full bg-secondary px-3 py-1 text-xs">{rows.length.toLocaleString("fa-IR")} مورد</span>
        </div>
        <div className="p-2 md:p-4">
          {state === "error" ? (
            <ErrorState title="فروشگاه در دسترس نیست" detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
          ) : state === "loading" ? (
            <p className="py-8 text-center text-sm text-muted">در حال بارگذاری…</p>
          ) : (
            <LegacyAppDataGrid gridId={gridId} columns={columns} rows={rows} savedViewStore={savedViewStore} />
          )}
        </div>
      </section>
    </main>
  );
}

const orderPaymentEnumOptions = [
  { value: "Paid", label: formatAdminStatus("Paid") },
  { value: "PendingPayment", label: formatAdminStatus("PendingPayment") },
  { value: "Cancelled", label: formatAdminStatus("Cancelled") },
];

const orderStatusEnumOptions = [
  { value: "Submitted", label: formatAdminStatus("Submitted") },
  { value: "PendingPayment", label: formatAdminStatus("PendingPayment") },
  { value: "ReservationRequested", label: formatAdminStatus("ReservationRequested") },
  { value: "Paid", label: formatAdminStatus("Paid") },
  { value: "Cancelled", label: formatAdminStatus("Cancelled") },
  { value: "Mixed", label: formatAdminStatus("Mixed") },
  { value: "Processing", label: formatAdminStatus("Processing") },
];

const orderColumns: GridColumnDef<AdminOrderRow>[] = [
  { id: "reference", header: "سفارش", accessor: (row) => row.reference, cell: (row) => <Link className="font-semibold text-primary hover:underline" href={`/admin/orders/${row.checkoutId}`}>{row.reference}</Link>, width: 140, minWidth: 110, maxWidth: 190, sticky: "start", filterKind: "text", sortable: true },
  { id: "customer", header: "مشتری / گیرنده", accessor: (row) => row.customerDisplayName, width: 150, minWidth: 110, maxWidth: 220, filterKind: "text", sortable: true },
  { id: "sellers", header: "فروشنده", accessor: (row) => row.sellerCount, cell: (row) => row.sellerCount.toLocaleString("fa-IR"), width: 85, minWidth: 70, maxWidth: 110, sortable: true },
  { id: "lines", header: "قلم", accessor: (row) => row.lineCount, cell: (row) => row.lineCount.toLocaleString("fa-IR"), width: 75, minWidth: 64, maxWidth: 100, sortable: true },
  { id: "payment", header: "پرداخت", accessor: (row) => row.paymentState, cell: (row) => <Status value={row.paymentState} />, width: 130, minWidth: 105, maxWidth: 170, filterKind: "status", enumOptions: orderPaymentEnumOptions },
  { id: "status", header: "وضعیت", accessor: (row) => row.status, cell: (row) => <Status value={row.status} />, width: 120, minWidth: 100, maxWidth: 160, filterKind: "status", enumOptions: orderStatusEnumOptions },
  { id: "amount", header: "قابل پرداخت", accessor: (row) => row.payableAmount, cell: (row) => formatAdminMoney(row.payableAmount, row.currency), width: 150, minWidth: 120, maxWidth: 200, sortable: true },
  { id: "created", header: "تاریخ", accessor: (row) => row.createdAt, cell: (row) => formatAdminDate(row.createdAt), width: 110, minWidth: 95, maxWidth: 150, sortable: true },
  {
    id: "actions",
    header: "عملیات",
    accessor: (row) => row.checkoutId,
    cell: (row) => (
      <Link className="text-primary underline-offset-4 hover:underline" href={`/admin/orders/${row.checkoutId}`}>
        مشاهده
      </Link>
    ),
    width: 88,
    minWidth: 80,
    maxWidth: 120,
    sortable: false,
  },
];

const sellerColumns: GridColumnDef<AdminSellerRow>[] = [
  { id: "name", header: "فروشنده", accessor: (row) => row.displayName, cell: (row) => <strong>{row.displayName}</strong>, width: 220, minWidth: 150, maxWidth: 300, sticky: "start", filterKind: "text", sortable: true },
  { id: "relationship", header: "رابطه", accessor: (row) => row.relationship, width: 150, minWidth: 110, maxWidth: 210, filterKind: "text" },
  { id: "status", header: "وضعیت", accessor: (row) => row.status, cell: (row) => <Status value={row.status} />, width: 120, minWidth: 100, maxWidth: 160, filterKind: "status" },
  { id: "offers", header: "پیشنهاد فعال", accessor: (row) => row.activeOfferCount, cell: (row) => row.activeOfferCount.toLocaleString("fa-IR"), width: 120, minWidth: 95, maxWidth: 150, sortable: true },
  { id: "orders", header: "سفارش", accessor: (row) => row.orderCount, cell: (row) => row.orderCount.toLocaleString("fa-IR"), width: 100, minWidth: 80, maxWidth: 130, sortable: true },
];

const customerColumns: GridColumnDef<AdminCustomerRow>[] = [
  { id: "name", header: "مشتری", accessor: (row) => row.displayName, cell: (row) => <strong>{row.displayName}</strong>, width: 220, minWidth: 150, maxWidth: 300, sticky: "start", filterKind: "text", sortable: true },
  { id: "contact", header: "راه ارتباطی", accessor: (row) => row.contact, width: 160, minWidth: 120, maxWidth: 220, filterKind: "text" },
  { id: "orders", header: "تعداد سفارش", accessor: (row) => row.orderCount, cell: (row) => row.orderCount.toLocaleString("fa-IR"), width: 120, minWidth: 95, maxWidth: 150, sortable: true },
  { id: "activity", header: "آخرین فعالیت", accessor: (row) => row.lastActivityAt ?? "", cell: (row) => formatAdminDate(row.lastActivityAt), width: 130, minWidth: 105, maxWidth: 170, sortable: true },
  { id: "status", header: "وضعیت", accessor: (row) => row.status, cell: (row) => <Status value={row.status} />, width: 110, minWidth: 90, maxWidth: 150, filterKind: "status" },
];

const reviewColumns = (moderate: (id: string, action: "publish" | "reject") => void): GridColumnDef<AdminReviewRow>[] => [
  { id: "reviewer", header: "نویسنده", accessor: (row) => row.reviewerDisplayName, cell: (row) => <strong>{row.reviewerDisplayName}</strong>, width: 150, minWidth: 110, maxWidth: 210, sticky: "start" },
  { id: "product", header: "محصول", accessor: (row) => row.productTitle, width: 180, minWidth: 130, maxWidth: 260 },
  { id: "rating", header: "امتیاز", accessor: (row) => row.rating, cell: (row) => <span className="inline-flex items-center gap-1"><Star className="size-4 fill-amber-400 text-amber-400" />{row.rating.toLocaleString("fa-IR")}</span>, width: 90, minWidth: 75, maxWidth: 110 },
  { id: "excerpt", header: "نظر", accessor: (row) => row.excerpt, width: 260, minWidth: 180, maxWidth: 360 },
  { id: "verified", header: "خرید تأییدشده", accessor: (row) => row.verifiedPurchase ? "بله" : "خیر", cell: (row) => row.verifiedPurchase ? <CheckCircle className="size-4 text-emerald-600" aria-label="بله" /> : "—", width: 120, minWidth: 100, maxWidth: 150 },
  { id: "status", header: "وضعیت", accessor: (row) => row.status, cell: (row) => <Status value={row.status} />, width: 110, minWidth: 90, maxWidth: 150 },
  { id: "created", header: "تاریخ", accessor: (row) => row.createdAt, cell: (row) => formatAdminDate(row.createdAt), width: 110, minWidth: 95, maxWidth: 150 },
  { id: "actions", header: "عملیات", accessor: () => "", cell: (row) => <span className="flex gap-2"><button onClick={() => moderate(row.id, "publish")} className="rounded-lg bg-emerald-600 px-3 py-1.5 text-xs text-white">انتشار</button><button onClick={() => moderate(row.id, "reject")} className="rounded-lg bg-red-600 px-3 py-1.5 text-xs text-white">رد</button></span>, width: 160, minWidth: 145, maxWidth: 190 },
];

function Status({ value }: { value: string }) {
  return <span className="inline-flex rounded-full bg-secondary px-2.5 py-1 text-xs font-medium">{formatAdminStatus(value)}</span>;
}

/** فهرست زندهٔ سفارش‌ها. */
export function AdminOrdersScreen() {
  const savedViewStore = useMemo(() => createHostSavedViewStore(ADMIN_ORDER_GRID_VIEW_KEY), []);
  return (
    <GridPage
      title="سفارش‌ها"
      description="پیگیری تسویه و سفارش‌های فروشندگان"
      loader={loadAdminOrders}
      columns={orderColumns}
      gridId={ADMIN_ORDER_GRID_VIEW_KEY}
      savedViewStore={savedViewStore}
    />
  );
}

const fulfillmentStatusEnumOptions = [
  { value: "ReadyToFulfill", label: formatFulfillmentStatus("ReadyToFulfill") },
  { value: "Processing", label: formatFulfillmentStatus("Processing") },
  { value: "Packed", label: formatFulfillmentStatus("Packed") },
  { value: "Dispatched", label: formatFulfillmentStatus("Dispatched") },
  { value: "InTransit", label: formatFulfillmentStatus("InTransit") },
  { value: "Delivered", label: formatFulfillmentStatus("Delivered") },
  { value: "Failed", label: formatFulfillmentStatus("Failed") },
  { value: "Cancelled", label: formatFulfillmentStatus("Cancelled") },
];

const fulfillmentColumns: GridColumnDef<FulfillmentListRow>[] = [
  {
    id: "recipientName",
    header: "گیرنده",
    accessor: (row) => row.recipientName,
    cell: (row) => (
      <Link className="min-w-0 hover:underline" href={`/admin/fulfillments/${row.fulfillmentId}`}>
        <span className="block truncate font-semibold text-primary">{row.recipientName || "بدون نام"}</span>
        <span className="block truncate text-xs text-muted">شناسه کوتاه: {row.fulfillmentId.slice(0, 8)}</span>
      </Link>
    ),
    width: 180,
    minWidth: 140,
    maxWidth: 260,
    sticky: "start",
    filterKind: "text",
    sortable: true,
  },
  {
    id: "fulfillmentId",
    header: "شناسه کوتاه",
    accessor: (row) => row.fulfillmentId,
    cell: (row) => <span className="font-mono text-xs text-muted">{row.fulfillmentId.slice(0, 8)}</span>,
    width: 110,
    minWidth: 88,
    maxWidth: 140,
    filterKind: "text",
    sortable: true,
    defaultVisible: false,
  },
  { id: "checkoutId", header: "شناسه کوتاه تسویه", accessor: (row) => row.checkoutId, cell: (row) => row.checkoutId.slice(0, 8), width: 120, minWidth: 96, maxWidth: 160, filterKind: "text" },
  { id: "cityName", header: "شهر", accessor: (row) => row.cityName, width: 110, minWidth: 90, maxWidth: 150, filterKind: "text", sortable: true },
  { id: "shipmentCount", header: "محموله", accessor: (row) => row.shipmentCount, cell: (row) => row.shipmentCount.toLocaleString("fa-IR"), width: 90, minWidth: 72, maxWidth: 110, sortable: true },
  { id: "status", header: "وضعیت", accessor: (row) => row.status, cell: (row) => <span className={fulfillmentStatusBadgeClass(row.status)}>{formatFulfillmentStatus(row.status)}</span>, width: 140, minWidth: 120, maxWidth: 180, filterKind: "status", enumOptions: fulfillmentStatusEnumOptions },
];

/** فهرست زندهٔ ارسال و تحویل برای Admin. */
export function AdminFulfillmentsScreen() {
  return <GridPage title="ارسال و تحویل" description="نظارت عملیاتی بر ارسال و تحویل و محموله‌ها" loader={loadAdminFulfillments} columns={fulfillmentColumns} gridId={ADMIN_FULFILLMENT_GRID_VIEW_KEY} />;
}

/** جزئیات ارسال و تحویل برای Admin (فقط‌خواندنی). */
export function AdminFulfillmentDetailScreen({ fulfillmentId }: { fulfillmentId: string }) {
  const [result, setResult] = useState<AdminResult<FulfillmentSnapshot>>({ state: "ok", data: null, status: 0 });
  const refresh = () => void loadAdminFulfillmentDetail(fulfillmentId).then(setResult);
  useEffect(refresh, [fulfillmentId]);
  if (result.state === "denied") return <Denied retry={refresh} />;
  const snapshot = result.data;
  return (
    <main>
      <div className="mb-5 flex flex-wrap items-end justify-between gap-3">
        <PageHeading
          title="جزئیات ارسال و تحویل"
          description={
            snapshot
              ? `${snapshot.recipientName || "بدون نام"} · شناسه کوتاه: ${snapshot.fulfillmentId.slice(0, 8)}`
              : "در حال بارگذاری"
          }
        />
        <Link className="text-sm text-primary hover:underline" href="/admin/fulfillments">بازگشت به فهرست</Link>
      </div>
      {result.state === "error" ? (
        <ErrorState title="ارسال و تحویل خوانده نشد" detail={result.message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
      ) : snapshot ? (
        <div className="grid gap-5">
          <section className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm">
            <div className="flex flex-wrap gap-3 items-center">
              <span className={fulfillmentStatusBadgeClass(snapshot.status)}>{formatFulfillmentStatus(snapshot.status)}</span>
              <span className="text-sm text-muted">تسویه (شناسه کوتاه): {snapshot.checkoutId.slice(0, 8)}</span>
              <span className="text-sm text-muted">سفارش فروشنده (شناسه کوتاه): {snapshot.sellerOrderId.slice(0, 8)}</span>
            </div>
            <p className="mt-4 text-sm">{snapshot.recipientName} · {snapshot.provinceName}، {snapshot.cityName} · {snapshot.postalAddress}</p>
          </section>
          <section className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm">
            <h2 className="font-semibold">محموله‌ها</h2>
            <div className="mt-4"><FulfillmentShipmentList shipments={snapshot.shipments} /></div>
          </section>
        </div>
      ) : <p className="text-muted">در حال بارگذاری…</p>}
    </main>
  );
}

const returnStatusEnumOptions = [
  { value: "Requested", label: formatReturnStatus("Requested") },
  { value: "Approved", label: formatReturnStatus("Approved") },
  { value: "Rejected", label: formatReturnStatus("Rejected") },
  { value: "RefundProcessing", label: formatReturnStatus("RefundProcessing") },
  { value: "Completed", label: formatReturnStatus("Completed") },
  { value: "RefundFailed", label: formatReturnStatus("RefundFailed") },
  { value: "Cancelled", label: formatReturnStatus("Cancelled") },
];

const returnColumns: GridColumnDef<ReturnListRow>[] = [
  {
    id: "returnRequestId",
    header: "شناسه کوتاه",
    accessor: (row) => row.returnRequestId,
    cell: (row) => (
      <Link className="font-semibold text-primary hover:underline" href={`/admin/returns/${row.returnRequestId}`}>
        {row.returnRequestId.slice(0, 8)}
      </Link>
    ),
    width: 120,
    minWidth: 96,
    maxWidth: 160,
    sticky: "start",
    filterKind: "text",
    sortable: true,
  },
  { id: "sellerOrderId", header: "سفارش (شناسه کوتاه)", accessor: (row) => row.sellerOrderId, cell: (row) => row.sellerOrderId.slice(0, 8), width: 120, minWidth: 96, maxWidth: 160, filterKind: "text" },
  { id: "itemCount", header: "اقلام", accessor: (row) => row.itemCount, width: 80, minWidth: 64, maxWidth: 96, sortable: true },
  {
    id: "refundAmount",
    header: "بازپرداخت",
    accessor: (row) => row.refundAmount,
    cell: (row) => row.refundAmount.toLocaleString("fa-IR"),
    width: 120,
    minWidth: 96,
    maxWidth: 150,
    sortable: true,
  },
  {
    id: "status",
    header: "وضعیت",
    accessor: (row) => row.status,
    cell: (row) => <span className={returnStatusBadgeClass(row.status)}>{formatReturnStatus(row.status)}</span>,
    width: 140,
    minWidth: 120,
    maxWidth: 180,
    filterKind: "status",
    enumOptions: returnStatusEnumOptions,
  },
  {
    id: "createdAt",
    header: "تاریخ",
    accessor: (row) => row.createdAt,
    cell: (row) => formatReturnDate(row.createdAt),
    width: 140,
    minWidth: 110,
    maxWidth: 180,
    sortable: true,
  },
];

/** فهرست زندهٔ مرجوعی برای Admin. */
export function AdminReturnsScreen() {
  return <GridPage title="مرجوعی و بازپرداخت" description="نظارت بر درخواست‌های مرجوعی و بازپرداخت" loader={loadAdminReturns} columns={returnColumns} gridId={ADMIN_RETURN_GRID_VIEW_KEY} />;
}

/** جزئیات مرجوعی Admin با retry refund. */
export function AdminReturnDetailScreen({ returnRequestId }: { returnRequestId: string }) {
  const [result, setResult] = useState<AdminResult<ReturnSnapshot>>({ state: "ok", data: null, status: 0 });
  const refresh = () => void loadAdminReturnDetail(returnRequestId).then(setResult);
  useEffect(refresh, [returnRequestId]);
  if (result.state === "denied") return <Denied retry={refresh} />;
  const snapshot = result.data;
  const canRetry = snapshot?.status === "RefundFailed";

  return (
    <main>
      <div className="mb-5 flex flex-wrap items-end justify-between gap-3">
        <PageHeading
          title="جزئیات مرجوعی"
          description={snapshot ? `شناسه کوتاه: ${snapshot.returnRequestId.slice(0, 8)}` : "در حال بارگذاری"}
        />
        <Link className="text-sm text-primary hover:underline" href="/admin/returns">بازگشت به فهرست</Link>
      </div>
      {result.state === "error" ? (
        <ErrorState title="مرجوعی خوانده نشد" detail={result.message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
      ) : snapshot ? (
        <div className="grid gap-5">
          <ReturnDetailCard snapshot={snapshot} />
          {canRetry ? (
            <button
              type="button"
              className="rounded-xl px-4 py-2 text-sm font-bold bg-[#2563EB] text-white hover:bg-blue-700 transition-colors w-fit"
              onClick={() => void adminRetryReturnRefund(returnRequestId).then(refresh)}
            >
              تلاش مجدد بازپرداخت
            </button>
          ) : null}
        </div>
      ) : <p className="text-muted">در حال بارگذاری…</p>}
    </main>
  );
}

/** فهرست زندهٔ فروشندگان. */
export function AdminSellersScreen() {
  return <GridPage title="فروشندگان" description="فروشندگان و رابطهٔ عملیاتی ثبت‌شده" loader={loadAdminSellers} columns={sellerColumns} gridId={ADMIN_SELLER_GRID_VIEW_KEY} />;
}

/** فهرست صادقانهٔ خریداران شناخته‌شده؛ نه CRM. */
export function AdminCustomersScreen() {
  return <GridPage title="مشتریان" description="خریداران شناخته‌شده از سفارش‌های زنده" loader={loadAdminCustomers} columns={customerColumns} gridId={ADMIN_CUSTOMER_GRID_VIEW_KEY} />;
}

/** حداقل سطح تعدیل نظر با DataGrid توبا و فرمان‌های مقتدر Host. */
export function AdminReviewsScreen() {
  const [state, setState] = useState<AdminLoadState | "loading">("loading");
  const [rows, setRows] = useState<AdminReviewRow[]>([]);
  const [message, setMessage] = useState<string>();
  const refresh = useCallback(() => void loadAdminReviews().then((result) => {
    setState(result.state); setRows(result.data?.rows ?? []); setMessage(result.message);
  }), []);
  useEffect(refresh, [refresh]);
  const moderate = useCallback((id: string, action: "publish" | "reject") => void moderateAdminReview(id, action).then((result) => {
    if (result.state === "denied") setState("denied");
    else if (result.state === "error") { setState("error"); setMessage(result.message); }
    else refresh();
  }), [refresh]);
  const columns = useMemo(() => reviewColumns(moderate), [moderate]);
  const savedViewStore = useMemo(() => createHostSavedViewStore(ADMIN_REVIEW_GRID_VIEW_KEY), []);
  if (state === "denied") return <Denied retry={refresh} />;
  return <main data-testid="admin-reviews"><PageHeading title="مدیریت نظرات" description="بررسی نظرهای در انتظار انتشار" />
    <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
      <div className="border-b border-border px-5 py-3 text-sm text-muted">{rows.length.toLocaleString("fa-IR")} نظر در انتظار</div>
      <div className="p-2 md:p-4">{state === "error" ? <ErrorState title="نظرها خوانده نشد" detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} /> : state === "loading" ? <p className="py-8 text-center text-sm text-muted">در حال بارگذاری…</p> : <LegacyAppDataGrid gridId={ADMIN_REVIEW_GRID_VIEW_KEY} columns={columns} rows={rows} savedViewStore={savedViewStore} />}</div>
    </section>
  </main>;
}

const promotionColumns = (deactivate: (id: string) => void): GridColumnDef<AdminPromotionRow>[] => [
  {
    id: "code",
    header: "کد",
    accessor: (row) => row.couponCode ?? "",
    cell: (row) => <strong className="font-mono" dir="ltr">{row.couponCode ?? "—"}</strong>,
    width: 140,
    minWidth: 110,
    maxWidth: 200,
    sticky: "start",
  },
  { id: "name", header: "نام", accessor: (row) => row.name, width: 180, minWidth: 130, maxWidth: 260 },
  {
    id: "discount",
    header: "تخفیف",
    accessor: (row) =>
      row.discountKind === "FixedAmountOff"
        ? row.fixedAmount
        : Math.round(row.percentageRate * 100),
    cell: (row) =>
      row.discountKind === "FixedAmountOff"
        ? formatAdminMoney(row.fixedAmount, "IRR")
        : `${Math.round(row.percentageRate * 100).toLocaleString("fa-IR")}٪`,
    width: 120,
    minWidth: 100,
    maxWidth: 160,
  },
  {
    id: "seller",
    header: "فروشنده",
    accessor: (row) => row.sellerPartyId ?? "",
    cell: (row) => (
      <span className="font-mono text-xs" dir="ltr">
        {row.sellerPartyId ? `${row.sellerPartyId.slice(0, 8)}…` : "—"}
      </span>
    ),
    width: 130,
    minWidth: 110,
    maxWidth: 180,
  },
  {
    id: "status",
    header: "وضعیت",
    accessor: (row) => row.status,
    cell: (row) => <Status value={row.status} />,
    width: 110,
    minWidth: 90,
    maxWidth: 150,
  },
  {
    id: "expires",
    header: "انقضا",
    accessor: (row) => row.effectiveTo ?? "",
    cell: (row) => (row.effectiveTo ? formatAdminDate(row.effectiveTo) : "باز"),
    width: 120,
    minWidth: 100,
    maxWidth: 150,
  },
  {
    id: "actions",
    header: "عملیات",
    accessor: () => "",
    cell: (row) =>
      row.status === "Active" ? (
        <button
          type="button"
          onClick={() => deactivate(row.promotionId)}
          className="rounded-lg bg-red-600 px-3 py-1.5 text-xs text-white"
        >
          غیرفعال
        </button>
      ) : (
        "—"
      ),
    width: 120,
    minWidth: 100,
    maxWidth: 150,
  },
];

/** نظارت ادمین بر پروموشن/کوپن فروشندگان — DataGrid نازک. */
export function AdminPromotionsScreen() {
  const [state, setState] = useState<AdminLoadState | "loading">("loading");
  const [rows, setRows] = useState<AdminPromotionRow[]>([]);
  const [message, setMessage] = useState<string>();
  const refresh = useCallback(
    () =>
      void loadAdminPromotions().then((result) => {
        setState(result.state);
        setRows(result.data ?? []);
        setMessage(result.message);
      }),
    [],
  );
  useEffect(refresh, [refresh]);
  const deactivate = useCallback(
    (id: string) =>
      void deactivateAdminPromotion(id).then((result) => {
        if (result.state === "denied") {
          setState("denied");
        } else if (result.state === "error") {
          setState("error");
          setMessage(result.message);
        } else {
          refresh();
        }
      }),
    [refresh],
  );
  const columns = useMemo(() => promotionColumns(deactivate), [deactivate]);
  const savedViewStore = useMemo(() => createHostSavedViewStore(ADMIN_PROMOTION_GRID_VIEW_KEY), []);
  if (state === "denied") {
    return <Denied retry={refresh} />;
  }
  return (
    <main data-testid="admin-promotions">
      <PageHeading title="نظارت پروموشن‌ها" description="فهرست و غیرفعال‌سازی نظارتی کدهای تخفیف فروشندگان" />
      <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
        <div className="border-b border-border px-5 py-3 text-sm text-muted">
          {rows.length.toLocaleString("fa-IR")} پروموشن
        </div>
        <div className="p-2 md:p-4">
          {state === "error" ? (
            <ErrorState
              title="پروموشن‌ها خوانده نشد"
              detail={message}
              onRetry={refresh}
              retryLabel={faWorkspaceMessages.retry}
            />
          ) : state === "loading" ? (
            <p className="py-8 text-center text-sm text-muted">در حال بارگذاری…</p>
          ) : (
            <LegacyAppDataGrid gridId={ADMIN_PROMOTION_GRID_VIEW_KEY} columns={columns} rows={rows} savedViewStore={savedViewStore} />
          )}
        </div>
      </section>
    </main>
  );
}

/** جزئیات checkout شامل snapshot ارسال و خطوط هر فروشنده. */
export function AdminOrderDetailScreen({ checkoutId }: { checkoutId: string }) {
  const [result, setResult] = useState<AdminResult<AdminOrderDetail>>({ state: "ok", data: null, status: 0 });
  const refresh = () => void loadAdminOrderDetail(checkoutId).then(setResult);
  useEffect(refresh, [checkoutId]);
  if (result.state === "denied") return <Denied retry={refresh} />;
  const detail = result.data;
  return (
    <main>
      <div className="mb-5 flex flex-wrap items-end justify-between gap-3">
        <PageHeading title="جزئیات سفارش" description={detail?.reference ?? "در حال بارگذاری سفارش"} />
        <Link className="text-sm text-primary hover:underline" href="/admin/orders">بازگشت به سفارش‌ها</Link>
      </div>
      {result.state === "error" ? <ErrorState title="سفارش خوانده نشد" detail={result.message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} /> : detail ? (
        <div className="grid gap-5">
          <section className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm">
            <dl className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
              <Info label="وضعیت" value={formatAdminStatus(detail.status)} />
              <Info label="پرداخت" value={formatAdminStatus(detail.paymentState)} />
              <Info label="تاریخ" value={formatAdminDate(detail.createdAt)} />
              <Info label="قابل پرداخت" value={formatAdminMoney(detail.payableAmount, detail.currency)} />
            </dl>
          </section>
          {detail.payment ? (
            <section className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm">
              <h2 className="font-semibold">پرداخت (سامانه)</h2>
              <dl className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
                <Info label="PaymentId" value={detail.payment.paymentId} />
                <Info label="وضعیت درگاه" value={formatAdminStatus(detail.payment.status)} />
                <Info label="Provider" value={detail.payment.providerCode || "—"} />
                <Info label="مرجع درگاه" value={detail.payment.providerRequestReference || "—"} />
                <Info label="تراکنش تأییدشده" value={detail.payment.providerTransactionReference || "—"} />
                <Info label="ایجاد" value={formatAdminDate(detail.payment.createdAt)} />
                <Info label="به‌روزرسانی" value={formatAdminDate(detail.payment.updatedAt)} />
                <Info label="شکست ایمن" value={detail.payment.lastFailureCode || "—"} />
              </dl>
            </section>
          ) : null}
          <section className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm">
            <h2 className="font-semibold">گیرنده و ارسال</h2>
            <p className="mt-3">{detail.recipientName} · <span dir="ltr">{detail.contactMobile || "—"}</span></p>
            <p className="mt-2 text-sm text-muted">{detail.provinceName}، {detail.cityName}، {detail.postalAddress} · {detail.postalCode} · {detail.shippingMethodLabel}</p>
          </section>
          {detail.sellerOrders.map((order) => (
            <section key={order.id} className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm">
              <div className="flex flex-wrap justify-between gap-3"><div><h2 className="font-semibold">{order.sellerDisplayName}</h2><p className="text-sm text-muted">{order.orderNumber}</p></div><Status value={order.status} /></div>
              <ul className="mt-4 divide-y divide-border">
                {order.lines.map((line) => <li key={line.id} className="flex flex-wrap justify-between gap-3 py-3"><div><p className="font-medium">{line.title}</p><p className="text-sm text-muted">{line.quantity.toLocaleString("fa-IR")} عدد × {formatAdminMoney(line.unitAmount, line.currency)}</p></div><strong>{formatAdminMoney(line.linePayable, line.currency)}</strong></li>)}
              </ul>
            </section>
          ))}
          <section className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm">
            <div className="grid gap-3 sm:grid-cols-2"><MetricMoney label="جمع" value={detail.subtotal} currency={detail.currency} /><MetricMoney label="مالیات" value={detail.taxAmount} currency={detail.currency} /><MetricMoney label="تخفیف" value={detail.discountAmount} currency={detail.currency} /><MetricMoney label="قابل پرداخت" value={detail.payableAmount} currency={detail.currency} /></div>
          </section>
        </div>
      ) : <p className="text-muted">در حال بارگذاری…</p>}
    </main>
  );
}

function Info({ label, value }: { label: string; value: string }) {
  return <div><dt className="text-sm text-muted">{label}</dt><dd className="mt-1 font-semibold">{value}</dd></div>;
}

function MetricMoney({ label, value, currency }: { label: string; value: number; currency: string }) {
  return <div className="flex justify-between rounded-ds bg-secondary/60 px-3 py-3"><span>{label}</span><strong>{formatAdminMoney(value, currency)}</strong></div>;
}

type SettlementBalanceRow = SettlementBalance & { id: string };
type PayoutQueueRow = PayoutRequestRow & { id: string };

const settlementBalanceColumns: GridColumnDef<SettlementBalanceRow>[] = [
  {
    id: "seller",
    header: "فروشنده",
    accessor: (row) => row.sellerPartyId,
    cell: (row) => <span dir="ltr" className="font-mono text-xs">{row.sellerPartyId.slice(0, 8)}…</span>,
    width: 140,
    minWidth: 110,
    maxWidth: 180,
    sticky: "start",
    filterKind: "text",
    sortable: true,
  },
  {
    id: "available",
    header: "قابل برداشت",
    accessor: (row) => row.availableBalance,
    cell: (row) => formatSettlementMoney(row.availableBalance, row.currency),
    width: 150,
    minWidth: 120,
    maxWidth: 200,
    sortable: true,
  },
  {
    id: "credits",
    header: "واریز",
    accessor: (row) => row.postedCredits,
    cell: (row) => formatSettlementMoney(row.postedCredits, row.currency),
    width: 130,
    minWidth: 100,
    maxWidth: 170,
    sortable: true,
  },
  {
    id: "debits",
    header: "برداشت/تعدیل",
    accessor: (row) => row.postedDebits,
    cell: (row) => formatSettlementMoney(row.postedDebits, row.currency),
    width: 130,
    minWidth: 100,
    maxWidth: 170,
    sortable: true,
  },
  {
    id: "reserved",
    header: "رزرو پرداخت به فروشنده",
    accessor: (row) => row.reservedPayouts,
    cell: (row) => formatSettlementMoney(row.reservedPayouts, row.currency),
    width: 130,
    minWidth: 100,
    maxWidth: 170,
    sortable: true,
  },
];

async function loadAdminSettlementBalanceRows(): Promise<AdminResult<SettlementBalanceRow[]>> {
  const result = await loadAdminSettlementBalances();
  if (result.state !== "ok" || !result.data) return result as AdminResult<SettlementBalanceRow[]>;
  return {
    ...result,
    data: result.data.map((row) => ({ ...row, id: row.settlementAccountId })),
  };
}

/** فهرست ماندهٔ تسویه فروشندگان. */
export function AdminSettlementScreen() {
  return (
    <GridPage
      title="تسویه فروشندگان"
      description="ماندهٔ ثبت‌شده و قابل برداشت هر فروشنده بازارگاه"
      loader={loadAdminSettlementBalanceRows}
      columns={settlementBalanceColumns}
      gridId={ADMIN_SETTLEMENT_GRID_VIEW_KEY}
    />
  );
}

const payoutStatusEnumOptions = [
  { value: "Requested", label: formatPayoutStatus("Requested") },
  { value: "Processing", label: formatPayoutStatus("Processing") },
  { value: "Succeeded", label: formatPayoutStatus("Succeeded") },
  { value: "Failed", label: formatPayoutStatus("Failed") },
  { value: "Cancelled", label: formatPayoutStatus("Cancelled") },
];

/** صف پرداخت به فروشنده با پردازش admin. */
export function AdminPayoutQueueScreen() {
  const [state, setState] = useState<AdminLoadState | "loading">("loading");
  const [rows, setRows] = useState<PayoutQueueRow[]>([]);
  const [message, setMessage] = useState<string>();
  const refresh = useCallback(() => void loadAdminPayoutQueue().then((result) => {
    setState(result.state);
    setRows((result.data ?? []).map((row) => ({ ...row, id: row.payoutRequestId })));
    setMessage(result.message);
  }), []);
  useEffect(refresh, [refresh]);

  const columns = useMemo((): GridColumnDef<PayoutQueueRow>[] => [
    {
      id: "seller",
      header: "فروشنده",
      accessor: (row) => row.sellerPartyId,
      cell: (row) => (
        <span className="text-sm">
          <span className="block text-muted">شناسه کوتاه</span>
          <span dir="ltr" className="font-mono text-xs">{row.sellerPartyId.slice(0, 8)}</span>
        </span>
      ),
      width: 130,
      minWidth: 100,
      maxWidth: 170,
      sticky: "start",
    },
    {
      id: "amount",
      header: "مبلغ",
      accessor: (row) => row.amount,
      cell: (row) => formatSettlementMoney(row.amount, row.currency),
      width: 140,
      minWidth: 110,
      maxWidth: 180,
      sortable: true,
    },
    {
      id: "status",
      header: "وضعیت",
      accessor: (row) => row.status,
      cell: (row) => <span className={`rounded-full px-2 py-0.5 text-xs font-bold ${payoutStatusClass(row.status)}`}>{formatPayoutStatus(row.status)}</span>,
      width: 120,
      minWidth: 95,
      maxWidth: 150,
      filterKind: "status",
      enumOptions: payoutStatusEnumOptions,
    },
    {
      id: "created",
      header: "تاریخ",
      accessor: (row) => row.createdAt,
      cell: (row) => formatAdminDate(row.createdAt),
      width: 130,
      minWidth: 105,
      maxWidth: 170,
      sortable: true,
    },
    {
      id: "actions",
      header: "عملیات",
      accessor: () => "",
      cell: (row) =>
        row.status === "Requested" || row.status === "0" || row.status === "Failed" || row.status === "3" ? (
          <button
            type="button"
            className="rounded-lg bg-[#2563EB] px-3 py-1.5 text-xs font-bold text-white hover:bg-blue-700"
            onClick={() => void processAdminPayout(row.payoutRequestId).then(refresh)}
          >
            پردازش
          </button>
        ) : (
          "—"
        ),
      width: 110,
      minWidth: 95,
      maxWidth: 130,
    },
  ], [refresh]);

  const savedViewStore = useMemo(() => createHostSavedViewStore(ADMIN_PAYOUT_GRID_VIEW_KEY), []);
  if (state === "denied") return <Denied retry={refresh} />;
  return (
    <main data-testid="admin-payout-queue">
      <PageHeading title="صف پرداخت به فروشنده" description="درخواست‌های برداشت فروشندگان بازارگاه" />
      <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
        <div className="border-b border-border px-5 py-3 text-sm text-muted">{rows.length.toLocaleString("fa-IR")} درخواست</div>
        <div className="p-2 md:p-4">
          {state === "error" ? (
            <ErrorState title="صف پرداخت خوانده نشد" detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
          ) : state === "loading" ? (
            <p className="py-8 text-center text-sm text-muted">در حال بارگذاری…</p>
          ) : (
            <LegacyAppDataGrid gridId={ADMIN_PAYOUT_GRID_VIEW_KEY} columns={columns} rows={rows} savedViewStore={savedViewStore} />
          )}
        </div>
      </section>
    </main>
  );
}

/** مدیریت مقالات Content / بلاگ. */
export function AdminContentScreen() {
  const [state, setState] = useState<AdminLoadState | "loading">("loading");
  const [rows, setRows] = useState<AdminContentArticle[]>([]);
  const [message, setMessage] = useState<string>();
  const [showCreate, setShowCreate] = useState(false);
  const [draft, setDraft] = useState({
    slug: "",
    title: "",
    excerpt: "",
    body: "",
    authorDisplayName: "تحریریه توبا",
    category: "",
    seoTitle: "",
    seoDescription: "",
  });

  const refresh = useCallback(() => void loadAdminContentArticles().then((result) => {
    setState(result.state);
    setRows(result.data ?? []);
    setMessage(result.message);
  }), []);
  useEffect(refresh, [refresh]);

  const columns = useMemo((): GridColumnDef<AdminContentArticle>[] => [
    {
      id: "title",
      header: "عنوان",
      accessor: (row) => row.title,
      cell: (row) => <strong className="line-clamp-2">{row.title}</strong>,
      width: 220,
      minWidth: 160,
      maxWidth: 320,
      sticky: "start",
      filterKind: "text",
      sortable: true,
    },
    {
      id: "slug",
      header: "نشانی صفحه",
      accessor: (row) => row.slug,
      cell: (row) => <span dir="ltr" className="font-mono text-xs">{row.slug}</span>,
      width: 160,
      minWidth: 120,
      maxWidth: 220,
    },
    {
      id: "status",
      header: "وضعیت",
      accessor: (row) => row.status,
      cell: (row) => (
        <span className={`rounded-full px-2 py-0.5 text-xs font-bold ${row.status === "Published" || row.status === "1" ? "bg-emerald-50 text-emerald-700" : "bg-amber-50 text-amber-700"}`}>
          {row.status === "Published" || row.status === "1" ? "منتشر" : "پیش‌نویس"}
        </span>
      ),
      width: 110,
      minWidth: 90,
      maxWidth: 140,
      filterKind: "status",
    },
    {
      id: "category",
      header: "دسته",
      accessor: (row) => row.category ?? "",
      cell: (row) => row.category ?? "—",
      width: 120,
      minWidth: 90,
      maxWidth: 160,
    },
    {
      id: "updated",
      header: "به‌روزرسانی",
      accessor: (row) => row.updatedAt,
      cell: (row) => formatContentDate(row.updatedAt),
      width: 130,
      minWidth: 100,
      maxWidth: 170,
      sortable: true,
    },
    {
      id: "actions",
      header: "عملیات",
      accessor: () => "",
      cell: (row) => (
        <span className="flex gap-2">
          {row.status === "Published" || row.status === "1" ? (
            <button type="button" className="rounded-lg bg-amber-600 px-3 py-1.5 text-xs text-white" onClick={() => void unpublishAdminArticle(row.articleId).then(refresh)}>لغو انتشار</button>
          ) : (
            <button type="button" className="rounded-lg bg-emerald-600 px-3 py-1.5 text-xs text-white" onClick={() => void publishAdminArticle(row.articleId).then(refresh)}>انتشار</button>
          )}
        </span>
      ),
      width: 140,
      minWidth: 120,
      maxWidth: 180,
    },
  ], [refresh]);

  const savedViewStore = useMemo(() => createHostSavedViewStore(ADMIN_CONTENT_GRID_VIEW_KEY), []);
  if (state === "denied") return <Denied retry={refresh} />;

  return (
    <main data-testid="admin-content">
      <div className="mb-5 flex flex-wrap items-end justify-between gap-3">
        <PageHeading title="محتوا / بلاگ" description="ایجاد، انتشار و بهینه‌سازی جستجوی مقالات" />
        <button type="button" className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white" onClick={() => setShowCreate(true)}>مقاله جدید</button>
      </div>
      <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
        <div className="border-b border-border px-5 py-3 text-sm text-muted">{rows.length.toLocaleString("fa-IR")} مقاله</div>
        <div className="p-2 md:p-4">
          {state === "error" ? (
            <ErrorState title="مقالات خوانده نشد" detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
          ) : state === "loading" ? (
            <p className="py-8 text-center text-sm text-muted">در حال بارگذاری…</p>
          ) : (
            <LegacyAppDataGrid gridId={ADMIN_CONTENT_GRID_VIEW_KEY} columns={columns} rows={rows} savedViewStore={savedViewStore} />
          )}
        </div>
      </section>

      {showCreate ? (
        <div className="fixed inset-0 z-[9999] flex items-center justify-center bg-black/50 p-4">
          <div className="max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-2xl bg-white p-5 shadow-xl">
            <h2 className="mb-4 text-lg font-bold">ایجاد پیش‌نویس</h2>
            <div className="space-y-3">
              {([
                ["slug", "نشانی صفحه"],
                ["title", "عنوان"],
                ["excerpt", "چکیده"],
                ["body", "بدنه"],
                ["authorDisplayName", "نویسنده"],
                ["category", "دسته"],
                ["seoTitle", "عنوان جستجو"],
                ["seoDescription", "توضیح جستجو"],
              ] as const).map(([key, label]) => (
                <label key={key} className="block text-sm">
                  <span className="mb-1 block text-gray-600">{label}</span>
                  {key === "body" || key === "excerpt" || key === "seoDescription" ? (
                    <textarea
                      className="w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
                      rows={key === "body" ? 5 : 2}
                      value={draft[key]}
                      onChange={(e) => setDraft((current) => ({ ...current, [key]: e.target.value }))}
                    />
                  ) : (
                    <input
                      className="w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
                      value={draft[key]}
                      onChange={(e) => setDraft((current) => ({ ...current, [key]: e.target.value }))}
                    />
                  )}
                </label>
              ))}
            </div>
            <div className="mt-4 flex justify-end gap-2">
              <button type="button" className="rounded-xl px-4 py-2 text-sm" onClick={() => setShowCreate(false)}>انصراف</button>
              <button
                type="button"
                className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white"
                onClick={() => void createAdminArticle(draft).then((result) => {
                  if (result.ok) { setShowCreate(false); refresh(); }
                  else setMessage(result.message);
                })}
              >
                ذخیره پیش‌نویس
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </main>
  );
}

/** مدیریت ترکیب صفحهٔ خانه — section catalog تأییدشده. */
export function AdminPageCompositionScreen() {
  const [state, setState] = useState<AdminLoadState | "loading">("loading");
  const [rows, setRows] = useState<AdminHomeCompositionSectionItem[]>([]);
  const [message, setMessage] = useState<string>();
  const [busy, setBusy] = useState(false);

  const refresh = useCallback(() => {
    void loadAdminHomeComposition().then((result) => {
      setState(result.state);
      setRows(result.data?.sections ?? []);
      setMessage(result.message);
    });
  }, []);

  useEffect(refresh, [refresh]);

  const move = async (index: number, direction: -1 | 1) => {
    const target = index + direction;
    if (target < 0 || target >= rows.length) return;
    const next = rows.slice();
    const current = next[index]!;
    next[index] = next[target]!;
    next[target] = current;
    setBusy(true);
    const result = await reorderAdminHomeSections(next.map((row) => row.pageSectionId));
    setBusy(false);
    if (result.state === "ok" && result.data) {
      setRows(result.data.sections);
      setMessage(undefined);
    } else {
      setMessage(result.message ?? "مرتب‌سازی ذخیره نشد");
      refresh();
    }
  };

  const toggleVisibility = async (row: AdminHomeCompositionSectionItem) => {
    setBusy(true);
    const result = await updateAdminHomeSection(row.pageSectionId, { isVisible: !row.isVisible });
    setBusy(false);
    if (result.state === "ok" && result.data) {
      setRows(result.data.sections);
    } else {
      setMessage(result.message ?? "به‌روزرسانی visibility انجام نشد");
    }
  };

  const restoreDefault = async () => {
    setBusy(true);
    const result = await restoreDefaultAdminHomeComposition();
    setBusy(false);
    if (result.state === "ok" && result.data) {
      setRows(result.data.sections);
      setMessage(undefined);
    } else {
      setMessage(result.message ?? "بازگردانی پیش‌فرض انجام نشد");
    }
  };

  if (state === "denied") return <Denied retry={refresh} />;

  return (
    <main data-testid="admin-page-composition">
      <div className="mb-5 flex flex-wrap items-end justify-between gap-3">
        <PageHeading
          title="ترکیب صفحهٔ خانه"
          description="ترتیب، نمایش/پنهان و variant sectionهای تأییدشدهٔ Shopeiva"
        />
        <button
          type="button"
          className="rounded-xl border border-border bg-surface-elevated px-4 py-2 text-sm font-bold"
          disabled={busy}
          onClick={() => void restoreDefault()}
        >
          بازگردانی پیش‌فرض
        </button>
      </div>

      <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
        <div className="border-b border-border px-5 py-3 text-sm text-muted">
          {rows.length.toLocaleString("fa-IR")} section — فقط کاتالوگ تأییدشده
        </div>
        <div className="p-4 md:p-5">
          {state === "error" ? (
            <ErrorState title="ترکیب خانه خوانده نشد" detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
          ) : state === "loading" ? (
            <p className="text-sm text-muted">در حال بارگذاری…</p>
          ) : (
            <ol className="space-y-3">
              {rows.map((row, index) => (
                <li
                  key={row.pageSectionId}
                  className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-border bg-white px-4 py-3"
                  data-testid={`admin-composition-row-${row.sectionType}`}
                >
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2">
                      <LayoutTemplate className="h-4 w-4 text-[#2563EB]" />
                      <strong>{SECTION_TYPE_LABELS[row.sectionType] ?? row.sectionType}</strong>
                      <span className="rounded-full bg-slate-100 px-2 py-0.5 text-xs font-mono" dir="ltr">
                        {row.sectionType}
                      </span>
                    </div>
                    <p className="mt-1 text-xs text-muted">
                      ترتیب {index + 1} · variant {row.variant}
                      {!row.isVisible ? " · پنهان" : ""}
                    </p>
                  </div>
                  <div className="flex items-center gap-2">
                    <button
                      type="button"
                      aria-label="بالا"
                      className="rounded-lg border border-border p-2 hover:bg-slate-50 disabled:opacity-40"
                      disabled={busy || index === 0}
                      onClick={() => void move(index, -1)}
                    >
                      <ChevronUp className="h-4 w-4" />
                    </button>
                    <button
                      type="button"
                      aria-label="پایین"
                      className="rounded-lg border border-border p-2 hover:bg-slate-50 disabled:opacity-40"
                      disabled={busy || index === rows.length - 1}
                      onClick={() => void move(index, 1)}
                    >
                      <ChevronDown className="h-4 w-4" />
                    </button>
                    <button
                      type="button"
                      className={`rounded-lg px-3 py-2 text-xs font-bold ${row.isVisible ? "bg-emerald-50 text-emerald-700" : "bg-amber-50 text-amber-700"}`}
                      disabled={busy}
                      onClick={() => void toggleVisibility(row)}
                    >
                      {row.isVisible ? (
                        <span className="inline-flex items-center gap-1"><Eye className="h-3.5 w-3.5" /> نمایان</span>
                      ) : (
                        <span className="inline-flex items-center gap-1"><EyeOff className="h-3.5 w-3.5" /> پنهان</span>
                      )}
                    </button>
                  </div>
                </li>
              ))}
            </ol>
          )}
          {message ? <p className="mt-4 text-sm text-red-600">{message}</p> : null}
        </div>
      </section>
    </main>
  );
}

/** مدیریت استوری‌های ویترین — wrapper نازک روی سیستم مشترک. */
export function AdminStoriesScreen() {
  return <StoryManagementScreen capabilities={ADMIN_STORY_CAPABILITIES} />;
}
