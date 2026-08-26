"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import { CheckCircle, Package, ShoppingBag, Star, Store, Users } from "lucide-react";
import { DataGrid, ErrorState, faWorkspaceMessages } from "../../design-system";
import { executeGridQuery } from "../../design-system/data-grid/query-engine";
import type { GridColumnDef, GridServerQuery } from "../../design-system/data-grid";
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
  type AdminCustomerRow,
  type AdminDashboard,
  type AdminLoadState,
  type AdminOrderDetail,
  type AdminOrderRow,
  type AdminResult,
  type AdminSellerRow,
  type AdminReviewRow,
} from "./admin-api";
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

function Denied({ retry }: { retry: () => void }) {
  return (
    <div data-testid="admin-auth-denied">
      <ErrorState
        title="دسترسی مجاز نیست"
        detail="Host هویت فعلی را مدیر تشخیص نداد. تغییر مسیر یا هدر مرورگر مجوز ایجاد نمی‌کند."
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
          خلاصهٔ زنده از Host. درآمد، GMV، نرخ تبدیل و نمودار ساختگی نمایش داده نمی‌شود.
        </p>
      </div>
      {result.state === "error" ? (
        <ErrorState title="Host در دسترس نیست" detail={result.message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
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
            <p className="mt-1 text-sm text-gray-500">اعداد فقط از Admin Dashboard API.</p>
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
}: {
  title: string;
  description: string;
  loader: () => Promise<AdminResult<T[]>>;
  columns: GridColumnDef<T>[];
}) {
  const [state, setState] = useState<AdminLoadState | "loading">("loading");
  const [rows, setRows] = useState<T[]>([]);
  const [message, setMessage] = useState<string>();
  const refresh = () => void loader().then((result) => {
    setState(result.state);
    setRows(result.data ?? []);
    setMessage(result.message);
  });
  useEffect(refresh, [loader]);
  const queryAdapter = useMemo(() => async (query: GridServerQuery) => executeGridQuery(rows, columns, query), [rows, columns]);
  if (state === "denied") return <Denied retry={refresh} />;
  return (
    <main>
      <PageHeading title={title} description={description} />
      <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
        <div className="flex items-center justify-between gap-3 border-b border-border px-4 py-3 md:px-5">
          <span className="text-sm text-muted">{state === "ok" ? "دادهٔ زندهٔ Host" : state === "loading" ? "در حال بارگذاری" : "اتصال برقرار نیست"}</span>
          <span className="rounded-full bg-secondary px-3 py-1 text-xs">{rows.length.toLocaleString("fa-IR")} مورد</span>
        </div>
        <div className="p-2 md:p-4">
          {state === "error" ? <ErrorState title="Host در دسترس نیست" detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} /> : <DataGrid columns={columns} queryAdapter={queryAdapter} />}
        </div>
      </section>
    </main>
  );
}

const orderColumns: GridColumnDef<AdminOrderRow>[] = [
  { id: "reference", header: "سفارش", accessor: (row) => row.reference, cell: (row) => <Link className="font-semibold text-primary hover:underline" href={`/admin/orders/${row.checkoutId}`}>{row.reference}</Link>, width: 140, minWidth: 110, maxWidth: 190, sticky: "start", filterKind: "text", sortable: true },
  { id: "customer", header: "مشتری / گیرنده", accessor: (row) => row.customerDisplayName, width: 150, minWidth: 110, maxWidth: 220, filterKind: "text", sortable: true },
  { id: "sellers", header: "فروشنده", accessor: (row) => row.sellerCount, cell: (row) => row.sellerCount.toLocaleString("fa-IR"), width: 85, minWidth: 70, maxWidth: 110, sortable: true },
  { id: "lines", header: "قلم", accessor: (row) => row.lineCount, cell: (row) => row.lineCount.toLocaleString("fa-IR"), width: 75, minWidth: 64, maxWidth: 100, sortable: true },
  { id: "payment", header: "پرداخت", accessor: (row) => row.paymentState, cell: (row) => <Status value={row.paymentState} />, width: 130, minWidth: 105, maxWidth: 170, filterKind: "status" },
  { id: "status", header: "وضعیت", accessor: (row) => row.status, cell: (row) => <Status value={row.status} />, width: 120, minWidth: 100, maxWidth: 160, filterKind: "status" },
  { id: "amount", header: "قابل پرداخت", accessor: (row) => row.payableAmount, cell: (row) => formatAdminMoney(row.payableAmount, row.currency), width: 150, minWidth: 120, maxWidth: 200, sortable: true },
  { id: "created", header: "تاریخ", accessor: (row) => row.createdAt, cell: (row) => formatAdminDate(row.createdAt), width: 110, minWidth: 95, maxWidth: 150, sortable: true },
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
  return <GridPage title="سفارش‌ها" description="پیگیری checkout و سفارش‌های فروشندگان" loader={loadAdminOrders} columns={orderColumns} />;
}

const fulfillmentColumns: GridColumnDef<FulfillmentListRow>[] = [
  {
    id: "fulfillmentId",
    header: "شناسه",
    accessor: (row) => row.fulfillmentId,
    cell: (row) => <Link className="font-semibold text-primary hover:underline" href={`/admin/fulfillments/${row.fulfillmentId}`}>{row.fulfillmentId.slice(0, 8)}</Link>,
    width: 120,
    minWidth: 96,
    maxWidth: 160,
    sticky: "start",
    filterKind: "text",
    sortable: true,
  },
  { id: "checkoutId", header: "Checkout", accessor: (row) => row.checkoutId, cell: (row) => row.checkoutId.slice(0, 8), width: 120, minWidth: 96, maxWidth: 160, filterKind: "text" },
  { id: "recipientName", header: "گیرنده", accessor: (row) => row.recipientName, width: 150, minWidth: 110, maxWidth: 220, filterKind: "text", sortable: true },
  { id: "cityName", header: "شهر", accessor: (row) => row.cityName, width: 110, minWidth: 90, maxWidth: 150, filterKind: "text", sortable: true },
  { id: "shipmentCount", header: "محموله", accessor: (row) => row.shipmentCount, cell: (row) => row.shipmentCount.toLocaleString("fa-IR"), width: 90, minWidth: 72, maxWidth: 110, sortable: true },
  { id: "status", header: "وضعیت", accessor: (row) => row.status, cell: (row) => <span className={fulfillmentStatusBadgeClass(row.status)}>{formatFulfillmentStatus(row.status)}</span>, width: 140, minWidth: 120, maxWidth: 180, filterKind: "status" },
];

/** فهرست زندهٔ fulfillment برای Admin. */
export function AdminFulfillmentsScreen() {
  return <GridPage title="ارسال / fulfillment" description="نظارت عملیاتی بر fulfillment و محموله‌ها" loader={loadAdminFulfillments} columns={fulfillmentColumns} />;
}

/** جزئیات fulfillment برای Admin (read-only). */
export function AdminFulfillmentDetailScreen({ fulfillmentId }: { fulfillmentId: string }) {
  const [result, setResult] = useState<AdminResult<FulfillmentSnapshot>>({ state: "ok", data: null, status: 0 });
  const refresh = () => void loadAdminFulfillmentDetail(fulfillmentId).then(setResult);
  useEffect(refresh, [fulfillmentId]);
  if (result.state === "denied") return <Denied retry={refresh} />;
  const snapshot = result.data;
  return (
    <main>
      <div className="mb-5 flex flex-wrap items-end justify-between gap-3">
        <PageHeading title="جزئیات fulfillment" description={snapshot?.fulfillmentId.slice(0, 8) ?? "در حال بارگذاری"} />
        <Link className="text-sm text-primary hover:underline" href="/admin/fulfillments">بازگشت به فهرست</Link>
      </div>
      {result.state === "error" ? (
        <ErrorState title="fulfillment خوانده نشد" detail={result.message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
      ) : snapshot ? (
        <div className="grid gap-5">
          <section className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm">
            <div className="flex flex-wrap gap-3 items-center">
              <span className={fulfillmentStatusBadgeClass(snapshot.status)}>{formatFulfillmentStatus(snapshot.status)}</span>
              <span className="text-sm text-muted">Checkout: {snapshot.checkoutId.slice(0, 8)}</span>
              <span className="text-sm text-muted">Seller order: {snapshot.sellerOrderId.slice(0, 8)}</span>
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

const returnColumns: GridColumnDef<ReturnListRow>[] = [
  {
    id: "returnRequestId",
    header: "شناسه",
    accessor: (row) => row.returnRequestId,
    cell: (row) => <Link className="font-semibold text-primary hover:underline" href={`/admin/returns/${row.returnRequestId}`}>{row.returnRequestId.slice(0, 8)}</Link>,
    width: 120,
    minWidth: 96,
    maxWidth: 160,
    sticky: "start",
    filterKind: "text",
    sortable: true,
  },
  { id: "sellerOrderId", header: "سفارش", accessor: (row) => row.sellerOrderId, cell: (row) => row.sellerOrderId.slice(0, 8), width: 120, minWidth: 96, maxWidth: 160, filterKind: "text" },
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
  return <GridPage title="مرجوعی / بازپرداخت" description="نظارت بر درخواست‌های مرجوعی و refund" loader={loadAdminReturns} columns={returnColumns} />;
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
        <PageHeading title="جزئیات مرجوعی" description={snapshot?.returnRequestId.slice(0, 8) ?? "در حال بارگذاری"} />
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
  return <GridPage title="فروشندگان" description="فروشندگان و رابطهٔ عملیاتی ثبت‌شده" loader={loadAdminSellers} columns={sellerColumns} />;
}

/** فهرست صادقانهٔ خریداران شناخته‌شده؛ نه CRM. */
export function AdminCustomersScreen() {
  return <GridPage title="مشتریان" description="خریداران شناخته‌شده از سفارش‌های زنده" loader={loadAdminCustomers} columns={customerColumns} />;
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
  const queryAdapter = useMemo(() => async (query: GridServerQuery) => executeGridQuery(rows, columns, query), [rows, columns]);
  if (state === "denied") return <Denied retry={refresh} />;
  return <main data-testid="admin-reviews"><PageHeading title="مدیریت نظرات" description="بررسی نظرهای در انتظار انتشار" />
    <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
      <div className="border-b border-border px-5 py-3 text-sm text-muted">{rows.length.toLocaleString("fa-IR")} نظر در انتظار</div>
      <div className="p-2 md:p-4">{state === "error" ? <ErrorState title="نظرها خوانده نشد" detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} /> : <DataGrid columns={columns} queryAdapter={queryAdapter} />}</div>
    </section>
  </main>;
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
