"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import { CheckCircle, ChevronDown, ChevronUp, Eye, EyeOff, LayoutTemplate, Package, ShoppingBag, Star, Store, Users } from "lucide-react";
import { ErrorState, faWorkspaceMessages, AppDataGrid, adminGridQueryAdapter, createClientGridQueryAdapter, useLegacyAdminGridDirectProps } from "../../design-system";
import { AppGridRowActionsCell, type AppGridRowAction } from "../../design-system/app-data-grid/app-grid-row-actions";
import type { GridColumnDef, GridServerQuery, SavedViewStore } from "../../design-system/data-grid";
import type { AdminGridQueryResult } from "../../design-system/app-data-grid/admin-grid-query-client.ts";
import {
  formatAdminDate,
  formatAdminMoney,
  formatAdminStatus,
  formatOrderSellerLabel,
  loadAdminDashboard,
  moderateAdminReview,
  loadAdminPromotions,
  deactivateAdminPromotion,
  queryAdminOrdersGrid,
  queryAdminSellersGrid,
  queryAdminCustomersGrid,
  queryAdminReceiptsGrid,
  queryAdminReviewsGrid,
  type AdminCustomerRow,
  type AdminDashboard,
  type AdminLoadState,
  type AdminOrderRow,
  type AdminResult,
  type AdminSellerRow,
  type AdminReviewRow,
  type AdminPromotionRow,
  type AdminReceiptRow,
} from "./admin-api";
export { AdminOrderDetailScreen } from "./admin-order-detail-screen";
export { AdminContentScreen } from "./content-list";
import {
  ADMIN_ORDER_GRID_VIEW_KEY, createHostSavedViewStore, ADMIN_FULFILLMENT_GRID_VIEW_KEY, ADMIN_RETURN_GRID_VIEW_KEY, ADMIN_SELLER_GRID_VIEW_KEY, ADMIN_CUSTOMER_GRID_VIEW_KEY, ADMIN_SETTLEMENT_GRID_VIEW_KEY, ADMIN_REVIEW_GRID_VIEW_KEY, ADMIN_PROMOTION_GRID_VIEW_KEY, ADMIN_PAYOUT_GRID_VIEW_KEY, ADMIN_RECEIPT_GRID_VIEW_KEY,
} from "./saved-view-store";
import {
  formatFulfillmentStatus,
  fulfillmentStatusBadgeClass,
  loadAdminFulfillmentDetail,
  queryAdminFulfillmentsGrid,
  type FulfillmentListRow,
  type FulfillmentSnapshot,
} from "../fulfillment/fulfillment-api";
import { FulfillmentShipmentList } from "../fulfillment/fulfillment-ui";
import {
  adminRetryReturnRefund,
  formatReturnDate,
  formatReturnStatus,
  loadAdminReturnDetail,
  queryAdminReturnsGrid,
  returnStatusBadgeClass,
  type ReturnListRow,
  type ReturnSnapshot,
} from "../returns/return-api";
import { ReturnDetailCard } from "../returns/return-ui";
import {
  formatPayoutStatus,
  formatSettlementMoney,
  loadAdminSettlementBalances,
  queryAdminPayoutGrid,
  payoutStatusClass,
  processAdminPayout,
  type PayoutRequestRow,
  type SettlementBalance,
} from "../settlement/settlement-api";
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

function ServerGridPage<T extends { id: string }>({
  title,
  description,
  gridId,
  columns,
  queryFn,
  savedViewStore: savedViewStoreInput,
  testId,
}: {
  title: string;
  description: string;
  gridId: string;
  columns: GridColumnDef<T>[];
  queryFn: (query: GridServerQuery) => Promise<AdminGridQueryResult<T>>;
  savedViewStore?: SavedViewStore;
  testId?: string;
}) {
  const savedViewStore = useMemo(
    () => savedViewStoreInput ?? createHostSavedViewStore(gridId),
    [gridId, savedViewStoreInput],
  );
  const [denied, setDenied] = useState(false);
  const [gridError, setGridError] = useState<string>();
  const [reloadToken, setReloadToken] = useState(0);
  const refresh = () => setReloadToken((value) => value + 1);
  const queryAdapter = useCallback(
    async (query: GridServerQuery) => {
      void reloadToken;
      return adminGridQueryAdapter(queryFn, () => setDenied(true), (message) => setGridError(message))(query);
    },
    [queryFn, reloadToken],
  );
  const gridProps = useLegacyAdminGridDirectProps({ gridId, columns, queryAdapter, savedViewStore });
  if (denied) return <Denied retry={refresh} />;
  return (
    <main data-testid={testId}>
      <PageHeading title={title} description={description} />
      <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
        <div className="flex items-center justify-between gap-3 border-b border-border px-4 py-3 md:px-5">
          <span className="text-sm text-muted">دادهٔ زندهٔ فروشگاه — server GridQuery</span>
        </div>
        <div className="p-2 md:p-4">
          {gridError ? (
            <ErrorState title="فروشگاه در دسترس نیست" detail={gridError} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
          ) : (
            <AppDataGrid<T> {...gridProps} />
          )}
        </div>
      </section>
    </main>
  );
}

function ClientGridPage<T extends { id: string }>({
  title,
  description,
  loader,
  columns,
  gridId,
  savedViewStore: savedViewStoreInput,
  boundedReason,
}: {
  title: string;
  description: string;
  loader: () => Promise<AdminResult<T[]>>;
  columns: GridColumnDef<T>[];
  gridId: string;
  savedViewStore?: SavedViewStore;
  boundedReason?: string;
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
  const queryAdapter = useCallback(
    async (query: GridServerQuery) => createClientGridQueryAdapter(rows, columns)(query),
    [rows, columns],
  );
  const gridProps = useLegacyAdminGridDirectProps({ gridId, columns, queryAdapter, savedViewStore });
  if (state === "denied") return <Denied retry={refresh} />;
  return (
    <main>
      <PageHeading title={title} description={description} />
      <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
        <div className="flex items-center justify-between gap-3 border-b border-border px-4 py-3 md:px-5">
          <span className="text-sm text-muted">
            {boundedReason ?? (state === "ok" ? "فهرست bounded — client GridQuery" : state === "loading" ? "در حال بارگذاری" : "اتصال برقرار نیست")}
          </span>
          <span className="rounded-full bg-secondary px-3 py-1 text-xs">{rows.length.toLocaleString("fa-IR")} مورد</span>
        </div>
        <div className="p-2 md:p-4">
          {state === "error" ? (
            <ErrorState title="فروشگاه در دسترس نیست" detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
          ) : state === "loading" ? (
            <p className="py-8 text-center text-sm text-muted">در حال بارگذاری…</p>
          ) : (
            <AppDataGrid<T> {...gridProps} />
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

const orderRowActions: AppGridRowAction<AdminOrderRow>[] = [
  {
    id: "view",
    label: "مشاهده",
    icon: Eye,
    href: (row) => `/admin/orders/${row.checkoutId}`,
    testId: (row) => `admin-order-view-${row.checkoutId}`,
  },
];

function truncatedCell(content: ReactNode, title?: string) {
  return (
    <div className="app-grid-cell-content min-w-0">
      <span className="block truncate" title={title}>{content}</span>
    </div>
  );
}

const orderColumns: GridColumnDef<AdminOrderRow>[] = [
  {
    id: "reference",
    header: "سفارش",
    accessor: (row) => row.reference,
    cell: (row) => (
      <div className="app-grid-cell-content min-w-0">
        <Link
          className="block truncate font-semibold text-primary hover:underline"
          href={`/admin/orders/${row.checkoutId}`}
          title={row.reference}
        >
          {row.reference}
        </Link>
      </div>
    ),
    width: 220,
    minWidth: 160,
    maxWidth: 280,
    filterKind: "text",
    sortable: true,
  },
  {
    id: "customer",
    header: "مشتری / گیرنده",
    accessor: (row) => row.customerDisplayName,
    cell: (row) => truncatedCell(row.customerDisplayName, row.customerDisplayName),
    width: 150,
    minWidth: 110,
    maxWidth: 220,
    filterKind: "text",
    sortable: true,
  },
  {
    id: "sellers",
    header: "فروشنده",
    accessor: (row) => formatOrderSellerLabel(row),
    cell: (row) => {
      const label = formatOrderSellerLabel(row);
      return truncatedCell(label, label === "—" ? undefined : label);
    },
    width: 210,
    minWidth: 160,
    maxWidth: 300,
    filterKind: "text",
    sortable: true,
  },
  { id: "lines", header: "قلم", accessor: (row) => row.lineCount, cell: (row) => row.lineCount.toLocaleString("fa-IR"), width: 75, minWidth: 64, maxWidth: 100, sortable: true },
  { id: "payment", header: "پرداخت", accessor: (row) => row.paymentState, cell: (row) => <Status value={row.paymentState} />, width: 130, minWidth: 105, maxWidth: 170, filterKind: "status", enumOptions: orderPaymentEnumOptions },
  { id: "status", header: "وضعیت", accessor: (row) => row.status, cell: (row) => <Status value={row.status} />, width: 120, minWidth: 100, maxWidth: 160, filterKind: "status", enumOptions: orderStatusEnumOptions },
  { id: "amount", header: "قابل پرداخت", accessor: (row) => row.payableAmount, cell: (row) => formatAdminMoney(row.payableAmount, row.currency), width: 150, minWidth: 120, maxWidth: 200, sortable: true },
  { id: "created", header: "تاریخ", accessor: (row) => row.createdAt, cell: (row) => formatAdminDate(row.createdAt), width: 110, minWidth: 95, maxWidth: 150, sortable: true },
  {
    id: "actions",
    header: "عملیات",
    accessor: () => "",
    cell: (row) => <AppGridRowActionsCell row={row} actions={orderRowActions} compact />,
    exportable: false,
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
    <ServerGridPage
      title="سفارش‌ها"
      description="پیگیری تسویه و سفارش‌های فروشندگان"
      queryFn={queryAdminOrdersGrid}
      columns={orderColumns}
      gridId={ADMIN_ORDER_GRID_VIEW_KEY}
      savedViewStore={savedViewStore}
      testId="admin-orders"
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
  return (
    <ServerGridPage
      title="ارسال و تحویل"
      description="نظارت عملیاتی بر ارسال و تحویل و محموله‌ها"
      queryFn={queryAdminFulfillmentsGrid}
      columns={fulfillmentColumns}
      gridId={ADMIN_FULFILLMENT_GRID_VIEW_KEY}
    />
  );
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
  return (
    <ServerGridPage
      title="مرجوعی و بازپرداخت"
      description="نظارت بر درخواست‌های مرجوعی و بازپرداخت"
      queryFn={queryAdminReturnsGrid}
      columns={returnColumns}
      gridId={ADMIN_RETURN_GRID_VIEW_KEY}
    />
  );
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
  return (
    <ServerGridPage
      title="فروشندگان"
      description="فروشندگان و رابطهٔ عملیاتی ثبت‌شده"
      queryFn={queryAdminSellersGrid}
      columns={sellerColumns}
      gridId={ADMIN_SELLER_GRID_VIEW_KEY}
    />
  );
}

/** فهرست صادقانهٔ خریداران شناخته‌شده؛ نه CRM. */
export function AdminCustomersScreen() {
  return (
    <ServerGridPage
      title="مشتریان"
      description="خریداران شناخته‌شده از سفارش‌های زنده"
      queryFn={queryAdminCustomersGrid}
      columns={customerColumns}
      gridId={ADMIN_CUSTOMER_GRID_VIEW_KEY}
    />
  );
}

/** حداقل سطح تعدیل نظر با AppDataGrid canonical و server GridQuery. */
export function AdminReviewsScreen() {
  const [reloadToken, setReloadToken] = useState(0);
  const moderate = useCallback(
    (id: string, action: "publish" | "reject") =>
      void moderateAdminReview(id, action).then((result) => {
        if (result.state === "ok") setReloadToken((value) => value + 1);
      }),
    [],
  );
  const columns = useMemo(() => reviewColumns(moderate), [moderate]);
  const queryFn = useCallback(
    (query: GridServerQuery) => {
      void reloadToken;
      return queryAdminReviewsGrid(query);
    },
    [reloadToken],
  );
  return (
    <ServerGridPage
      title="مدیریت نظرات"
      description="بررسی نظرهای در انتظار انتشار"
      queryFn={queryFn}
      columns={columns}
      gridId={ADMIN_REVIEW_GRID_VIEW_KEY}
      testId="admin-reviews"
    />
  );
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

/** نظارت ادمین بر پروموشن/کوپن فروشندگان — bounded client grid. */
export function AdminPromotionsScreen() {
  const [reloadToken, setReloadToken] = useState(0);
  const loader = useCallback(async () => {
    void reloadToken;
    return loadAdminPromotions();
  }, [reloadToken]);
  const deactivate = useCallback(
    (id: string) =>
      void deactivateAdminPromotion(id).then((result) => {
        if (result.state === "ok") setReloadToken((value) => value + 1);
      }),
    [],
  );
  const columns = useMemo(() => promotionColumns(deactivate), [deactivate]);
  return (
    <ClientGridPage
      title="نظارت پروموشن‌ها"
      description="فهرست و غیرفعال‌سازی نظارتی کدهای تخفیف فروشندگان"
      loader={loader}
      columns={columns}
      gridId={ADMIN_PROMOTION_GRID_VIEW_KEY}
      boundedReason="SMALL_BOUNDED_CLIENT_SAFE — bounded by active seller promotions"
    />
  );
}

/** جزئیات checkout — re-export از admin-order-detail-screen. */

const receiptStatusEnumOptions = [
  { value: "Pending", label: formatAdminStatus("Pending") },
  { value: "Succeeded", label: formatAdminStatus("Succeeded") },
  { value: "Failed", label: formatAdminStatus("Failed") },
  { value: "Cancelled", label: formatAdminStatus("Cancelled") },
];

const receiptRowActions: AppGridRowAction<AdminReceiptRow>[] = [
  {
    id: "view",
    label: "مشاهده",
    icon: Eye,
    href: (row) => `/admin/orders/${row.checkoutId}`,
    testId: (row) => `admin-receipt-view-${row.paymentId}`,
  },
];

const receiptColumns: GridColumnDef<AdminReceiptRow>[] = [
  {
    id: "reference",
    header: "سفارش",
    accessor: (row) => row.orderReference,
    cell: (row) => (
      <Link className="font-semibold text-primary hover:underline" href={`/admin/orders/${row.checkoutId}`}>
        {row.orderReference}
      </Link>
    ),
    width: 180,
    minWidth: 140,
    maxWidth: 240,
    sticky: "start",
    filterKind: "text",
    sortable: true,
  },
  {
    id: "customer",
    header: "مشتری",
    accessor: (row) => row.customerDisplayName,
    width: 150,
    minWidth: 110,
    filterKind: "text",
    sortable: true,
  },
  {
    id: "amount",
    header: "مبلغ",
    accessor: (row) => row.amount,
    cell: (row) => formatAdminMoney(row.amount, row.currency),
    width: 140,
    minWidth: 110,
    sortable: true,
  },
  {
    id: "status",
    header: "وضعیت",
    accessor: (row) => row.status,
    cell: (row) => <Status value={row.status} />,
    width: 120,
    minWidth: 100,
    filterKind: "status",
    enumOptions: receiptStatusEnumOptions,
  },
  {
    id: "provider",
    header: "درگاه",
    accessor: (row) => row.providerCode,
    cell: (row) => <span dir="ltr">{row.providerCode || "—"}</span>,
    width: 120,
    minWidth: 90,
    filterKind: "text",
  },
  {
    id: "created",
    header: "تاریخ",
    accessor: (row) => row.createdAt,
    cell: (row) => formatAdminDate(row.createdAt),
    width: 120,
    minWidth: 100,
    sortable: true,
  },
  {
    id: "actions",
    header: "عملیات",
    accessor: () => "",
    cell: (row) => <AppGridRowActionsCell row={row} actions={receiptRowActions} compact />,
    exportable: false,
    sortable: false,
  },
];

/** فهرست دریافت‌های مشتری (پرداخت) برای Admin. */
export function AdminReceiptsScreen() {
  return (
    <ServerGridPage
      title="دریافت‌ها"
      description="پرداخت‌های واقعی مشتریان با enrich سفارش از Host"
      queryFn={queryAdminReceiptsGrid}
      columns={receiptColumns}
      gridId={ADMIN_RECEIPT_GRID_VIEW_KEY}
      testId="admin-receipts"
    />
  );
}

type SettlementBalanceRow = SettlementBalance & { id: string };
type PayoutQueueRow = PayoutRequestRow & { id: string };

const settlementBalanceColumns: GridColumnDef<SettlementBalanceRow>[] = [
  {
    id: "seller",
    header: "فروشنده",
    accessor: (row) => row.sellerDisplayName || row.sellerPartyId,
    cell: (row) => (
      <span className="text-sm">
        <span className="block font-semibold">{row.sellerDisplayName || "فروشنده"}</span>
        <span dir="ltr" className="font-mono text-xs text-muted">{row.sellerPartyId.slice(0, 8)}…</span>
      </span>
    ),
    width: 180,
    minWidth: 140,
    maxWidth: 240,
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
    <ClientGridPage
      title="تسویه فروشندگان"
      description="ماندهٔ ثبت‌شده و قابل برداشت هر فروشنده بازارگاه"
      loader={loadAdminSettlementBalanceRows}
      columns={settlementBalanceColumns}
      gridId={ADMIN_SETTLEMENT_GRID_VIEW_KEY}
      boundedReason="SMALL_BOUNDED_CLIENT_SAFE — bounded by seller-party count"
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
  const [reloadToken, setReloadToken] = useState(0);
  const refresh = useCallback(() => setReloadToken((value) => value + 1), []);
  const columns = useMemo((): GridColumnDef<PayoutQueueRow>[] => [
    {
      id: "seller",
      header: "فروشنده",
      accessor: (row) => row.sellerDisplayName || row.sellerPartyId,
      cell: (row) => (
        <span className="text-sm">
          <span className="block font-semibold">{row.sellerDisplayName || "فروشنده"}</span>
          <span dir="ltr" className="font-mono text-xs text-muted">{row.sellerPartyId.slice(0, 8)}…</span>
        </span>
      ),
      width: 170,
      minWidth: 130,
      maxWidth: 220,
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
  const queryFn = useCallback(
    (query: GridServerQuery) => {
      void reloadToken;
      return queryAdminPayoutGrid(query);
    },
    [reloadToken],
  );
  return (
    <ServerGridPage
      title="صف پرداخت به فروشنده"
      description="درخواست‌های برداشت فروشندگان بازارگاه"
      queryFn={queryFn}
      columns={columns}
      gridId={ADMIN_PAYOUT_GRID_VIEW_KEY}
      testId="admin-payout-queue"
    />
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
