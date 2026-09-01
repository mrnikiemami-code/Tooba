"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import {
  ArrowRight,
  ClipboardList,
  CreditCard,
  Package,
  Printer,
  ShoppingBag,
  Store,
  Wallet,
} from "lucide-react";
import {
  AppDataGrid,
  ErrorState,
  createClientGridQueryAdapter,
  faWorkspaceMessages,
  useLegacyAdminGridDirectProps,
} from "../../design-system";
import type { GridColumnDef, GridServerQuery } from "../../design-system/data-grid";
import {
  formatAdminDate,
  formatAdminMoney,
  formatAdminMoneyOptional,
  formatAdminPaymentProvider,
  formatAdminPaymentReference,
  formatAdminStatus,
  loadAdminOrderDetail,
  type AdminFinancialEvent,
  type AdminFinancialSummary,
  type AdminOrderDetail,
  type AdminResult,
  type AdminSellerFinancial,
} from "./admin-api";

function Denied({ retry }: { retry: () => void }) {
  return (
    <ErrorState
      title="دسترسی مجاز نیست"
      detail="سامانه هویت فعلی را مدیر تشخیص نداد."
      onRetry={retry}
      retryLabel={faWorkspaceMessages.retry}
    />
  );
}

function SummaryCard({
  label,
  value,
  icon,
  tone,
  badge,
}: {
  label: string;
  value: string;
  icon: React.ReactNode;
  tone: string;
  badge?: { text: string; className: string };
}) {
  return (
    <div className="rounded-2xl border border-gray-200 bg-white p-4 shadow-sm">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="text-sm text-gray-500">{label}</p>
          <p className="mt-2 text-xl font-black tabular-nums">{value}</p>
          {badge ? (
            <span className={`mt-2 inline-flex rounded-full px-2.5 py-1 text-xs font-bold ${badge.className}`}>
              {badge.text}
            </span>
          ) : null}
        </div>
        <span className={`inline-flex size-11 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br ${tone} text-white`}>
          {icon}
        </span>
      </div>
    </div>
  );
}

function settlementBadge(status: string): { text: string; className: string } {
  switch (status) {
    case "Settled":
      return { text: "تسویه‌شده", className: "bg-emerald-50 text-emerald-700" };
    case "WaitingForSettlement":
      return { text: "در انتظار تسویه", className: "bg-blue-50 text-blue-700" };
    default:
      return { text: "تسویه‌نشده", className: "bg-amber-50 text-amber-700" };
  }
}

function eventTypeLabel(type: string): string {
  switch (type) {
    case "CustomerReceipt":
      return "دریافت مشتری";
    case "SellerSettlement":
      return "تسویه فروشنده";
    case "SettlementAdjustment":
      return "تعدیل تسویه";
    case "WalletDeposit":
      return "واریز کیف پول";
    default:
      return type || "رویداد مالی";
  }
}

function eventTypeClass(type: string): string {
  switch (type) {
    case "CustomerReceipt":
      return "bg-teal-50 text-teal-700";
    case "SellerSettlement":
      return "bg-emerald-50 text-emerald-700";
    case "WalletDeposit":
      return "bg-blue-50 text-blue-700";
    default:
      return "bg-gray-100 text-gray-700";
  }
}

function paymentBadge(state: string): { text: string; className: string } {
  if (state === "Paid" || state === "Succeeded") {
    return { text: formatAdminStatus(state), className: "bg-emerald-50 text-emerald-700" };
  }
  if (state === "PendingPayment" || state === "Pending") {
    return { text: formatAdminStatus(state), className: "bg-blue-50 text-blue-700" };
  }
  return { text: formatAdminStatus(state), className: "bg-gray-100 text-gray-700" };
}

const historyColumns: GridColumnDef<AdminFinancialEvent & { id: string }>[] = [
  {
    id: "occurredAt",
    header: "تاریخ و زمان",
    accessor: (row) => row.occurredAt,
    cell: (row) => formatAdminDate(row.occurredAt),
    width: 130,
    minWidth: 110,
    sortable: true,
  },
  {
    id: "eventType",
    header: "نوع",
    accessor: (row) => row.eventType,
    cell: (row) => (
      <span className={`rounded-full px-2 py-0.5 text-xs font-bold ${eventTypeClass(row.eventType)}`}>
        {eventTypeLabel(row.eventType)}
      </span>
    ),
    width: 140,
    minWidth: 120,
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
    id: "party",
    header: "طرف",
    accessor: (row) => row.partyDisplayName,
    width: 160,
    minWidth: 120,
  },
  {
    id: "reference",
    header: "مرجع",
    accessor: (row) => row.reference,
    cell: (row) => <span dir="ltr" className="font-mono text-xs">{row.reference}</span>,
    width: 130,
    minWidth: 100,
  },
  {
    id: "method",
    header: "روش پرداخت",
    accessor: (row) => row.paymentMethod,
    width: 130,
    minWidth: 100,
  },
  {
    id: "status",
    header: "وضعیت",
    accessor: (row) => row.status,
    cell: (row) => {
      const badge = paymentBadge(row.status);
      return <span className={`rounded-full px-2 py-0.5 text-xs font-bold ${badge.className}`}>{badge.text}</span>;
    },
    width: 110,
    minWidth: 90,
  },
  {
    id: "description",
    header: "توضیح",
    accessor: (row) => row.description,
    width: 220,
    minWidth: 160,
  },
];

function SellerFinancialTable({
  rows,
  currency,
}: {
  rows: AdminSellerFinancial[];
  currency: string;
}) {
  const totals = useMemo(
    () => ({
      lines: rows.reduce((sum, row) => sum + row.lineCount, 0),
      gross: rows.reduce((sum, row) => sum + row.grossAmount, 0),
      commission: rows.reduce((sum, row) => sum + row.commissionAmount, 0),
      payable: rows.reduce((sum, row) => sum + row.payableAmount, 0),
    }),
    [rows],
  );

  return (
    <div className="overflow-x-auto rounded-2xl border border-gray-200">
      <table className="min-w-full text-sm">
        <thead className="bg-gray-50 text-gray-600">
          <tr>
            <th className="px-4 py-3 text-right font-bold">فروشنده</th>
            <th className="px-4 py-3 text-right font-bold">قلم</th>
            <th className="px-4 py-3 text-right font-bold">مبلغ ناخالص</th>
            <th className="px-4 py-3 text-right font-bold">کارمزد</th>
            <th className="px-4 py-3 text-right font-bold">قابل پرداخت</th>
            <th className="px-4 py-3 text-right font-bold">وضعیت تسویه</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-gray-100 bg-white">
          {rows.map((row) => {
            const badge = settlementBadge(row.settlementStatus);
            return (
              <tr key={row.sellerOrderId}>
                <td className="px-4 py-3 font-semibold">{row.sellerDisplayName}</td>
                <td className="px-4 py-3 tabular-nums">{row.lineCount.toLocaleString("fa-IR")}</td>
                <td className="px-4 py-3 tabular-nums">{formatAdminMoney(row.grossAmount, row.currency)}</td>
                <td className="px-4 py-3 tabular-nums">
                  {formatAdminMoneyOptional(
                    row.commissionAmount,
                    row.currency,
                    row.settlementStatus !== "Settled" && row.commissionAmount === 0,
                  )}
                </td>
                <td className="px-4 py-3 tabular-nums">{formatAdminMoney(row.payableAmount, row.currency)}</td>
                <td className="px-4 py-3">
                  <span className={`rounded-full px-2 py-0.5 text-xs font-bold ${badge.className}`}>{badge.text}</span>
                </td>
              </tr>
            );
          })}
        </tbody>
        <tfoot className="bg-gray-50 font-bold">
          <tr>
            <td className="px-4 py-3">جمع</td>
            <td className="px-4 py-3 tabular-nums">{totals.lines.toLocaleString("fa-IR")}</td>
            <td className="px-4 py-3 tabular-nums">{formatAdminMoney(totals.gross, currency)}</td>
            <td className="px-4 py-3 tabular-nums">{formatAdminMoney(totals.commission, currency)}</td>
            <td className="px-4 py-3 tabular-nums">{formatAdminMoney(totals.payable, currency)}</td>
            <td className="px-4 py-3">—</td>
          </tr>
        </tfoot>
      </table>
    </div>
  );
}

function FinancialSummaryCards({
  summary,
}: {
  summary: AdminFinancialSummary;
}) {
  return (
    <div className="grid gap-4 lg:grid-cols-2">
      <section className="rounded-2xl border border-gray-200 bg-white p-5 shadow-sm">
        <h3 className="text-base font-black">خلاصه مالی کل</h3>
        <dl className="mt-4 space-y-3 text-sm">
          <div className="flex justify-between gap-3"><dt className="text-gray-500">جمع سهم فروشندگان</dt><dd className="font-bold">{formatAdminMoney(summary.totalSellerShare, summary.currency)}</dd></div>
          <div className="flex justify-between gap-3"><dt className="text-gray-500">جمع کارمزد</dt><dd className="font-bold">{formatAdminMoney(summary.totalCommission, summary.currency)}</dd></div>
          <div className="flex justify-between gap-3"><dt className="text-gray-500">سود ناخالص سفارش</dt><dd className="font-bold">{formatAdminMoney(summary.grossOrderProfit, summary.currency)}</dd></div>
          <div className="flex justify-between gap-3 rounded-xl bg-blue-50 px-3 py-2"><dt className="font-bold text-blue-800">قابل پرداخت به فروشندگان</dt><dd className="font-black text-blue-800">{formatAdminMoney(summary.payableToSellers, summary.currency)}</dd></div>
        </dl>
      </section>
      <section className="rounded-2xl border border-gray-200 bg-white p-5 shadow-sm">
        <h3 className="text-base font-black">رسید مشتری</h3>
        <dl className="mt-4 space-y-3 text-sm">
          <div className="flex justify-between gap-3"><dt className="text-gray-500">مبلغ ناخالص سفارش</dt><dd className="font-bold">{formatAdminMoney(summary.customerGrossAmount, summary.currency)}</dd></div>
          <div className="flex justify-between gap-3"><dt className="text-gray-500">هزینه ارسال</dt><dd className="font-bold">{formatAdminMoney(summary.shippingCost, summary.currency)}</dd></div>
          <div className="flex justify-between gap-3"><dt className="text-gray-500">تخفیف مشتری</dt><dd className="font-bold">{formatAdminMoney(summary.customerDiscounts, summary.currency)}</dd></div>
          <div className="flex justify-between gap-3 rounded-xl bg-emerald-50 px-3 py-2"><dt className="font-bold text-emerald-800">جمع دریافت از مشتری</dt><dd className="font-black text-emerald-800">{formatAdminMoney(summary.totalReceivedFromCustomer, summary.currency)}</dd></div>
        </dl>
      </section>
    </div>
  );
}

/** جزئیات سفارش Admin با UX مالی بازارگاه مطابق مرجع T042. */
export function AdminOrderDetailScreen({ checkoutId }: { checkoutId: string }) {
  const [result, setResult] = useState<AdminResult<AdminOrderDetail>>({ state: "ok", data: null, status: 0 });
  const [tab, setTab] = useState<"summary" | "sellers" | "payments">("summary");
  const refresh = () => void loadAdminOrderDetail(checkoutId).then(setResult);
  useEffect(refresh, [checkoutId]);

  const historyRows = useMemo(
    () => (result.data?.financialEvents ?? []).map((row, index) => ({ ...row, id: `${row.reference}-${index}` })),
    [result.data?.financialEvents],
  );
  const historyAdapter = useCallback(
    async (query: GridServerQuery) => createClientGridQueryAdapter(historyRows, historyColumns)(query),
    [historyRows],
  );
  const historyGridProps = useLegacyAdminGridDirectProps({
    gridId: `grid.admin.order-detail.history.${checkoutId}`,
    columns: historyColumns,
    queryAdapter: historyAdapter,
  });

  if (result.state === "denied") return <Denied retry={refresh} />;
  const detail = result.data;
  const paymentBadgeState = paymentBadge(detail?.paymentState ?? "PendingPayment");
  const statusBadge = paymentBadge(detail?.status ?? "Submitted");

  return (
    <main data-testid="admin-order-detail">
      <div className="mb-5 flex flex-wrap items-start justify-between gap-4">
        <div>
          <p className="text-sm text-gray-500">خانه / سفارش‌ها / جزئیات سفارش</p>
          <h1 className="mt-1 text-2xl font-black tracking-tight">جزئیات سفارش</h1>
          <p className="mt-1 font-mono text-sm text-gray-600" dir="ltr">{detail?.reference ?? checkoutId}</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <Link href="/admin/orders" className="inline-flex items-center gap-2 rounded-xl border border-gray-200 bg-white px-4 py-2 text-sm font-bold hover:bg-gray-50">
            <ArrowRight className="size-4" />
            بازگشت
          </Link>
          <button type="button" className="inline-flex items-center gap-2 rounded-xl border border-gray-200 bg-white px-4 py-2 text-sm font-bold hover:bg-gray-50">
            <Printer className="size-4" />
            چاپ فاکتور
          </button>
          <button type="button" className="inline-flex items-center gap-2 rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white hover:bg-blue-700">
            ایجاد سند تسویه
          </button>
        </div>
      </div>

      {result.state === "error" ? (
        <ErrorState title="سفارش خوانده نشد" detail={result.message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
      ) : detail ? (
        <div className="space-y-6">
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
            <SummaryCard label="تعداد اقلام" value={`${detail.lineCount.toLocaleString("fa-IR")} قلم`} icon={<ShoppingBag className="size-5" />} tone="from-violet-500 to-violet-600" />
            <SummaryCard label="تعداد فروشنده" value={`${detail.sellerCount.toLocaleString("fa-IR")} فروشنده`} icon={<Store className="size-5" />} tone="from-blue-500 to-blue-600" />
            <SummaryCard label="مبلغ کل سفارش" value={formatAdminMoney(detail.payableAmount, detail.currency)} icon={<Wallet className="size-5" />} tone="from-emerald-500 to-emerald-600" />
            <SummaryCard label="وضعیت پرداخت" value={paymentBadgeState.text} icon={<CreditCard className="size-5" />} tone="from-teal-500 to-teal-600" badge={paymentBadgeState} />
            <SummaryCard label="وضعیت سفارش" value={statusBadge.text} icon={<ClipboardList className="size-5" />} tone="from-amber-500 to-amber-600" badge={statusBadge} />
          </div>

          <div className="grid gap-4 lg:grid-cols-2">
            <section className="rounded-2xl border border-gray-200 bg-white p-5 shadow-sm">
              <h2 className="font-black">اطلاعات مشتری و ارسال</h2>
              <p className="mt-3 font-semibold">{detail.recipientName || "مشتری توبا"}</p>
              <p className="mt-1 text-sm text-gray-600" dir="ltr">{detail.contactMobile || "—"}</p>
              <p className="mt-3 text-sm leading-7 text-gray-700">
                {detail.provinceName}، {detail.cityName}
                <br />
                {detail.postalAddress}
                <br />
                کد پستی: {detail.postalCode || "—"} · {detail.shippingMethodLabel || "ارسال"}
              </p>
            </section>
            <section className="rounded-2xl border border-gray-200 bg-white p-5 shadow-sm">
              <h2 className="font-black">اطلاعات پرداخت</h2>
              {detail.payment ? (
                <dl className="mt-4 space-y-3 text-sm">
                  <div className="flex justify-between gap-3"><dt className="text-gray-500">درگاه</dt><dd className="font-bold">{formatAdminPaymentProvider(detail.payment.providerCode)}</dd></div>
                  <div className="flex justify-between gap-3"><dt className="text-gray-500">شناسه تراکنش</dt><dd className="font-mono text-xs" dir="ltr">{formatAdminPaymentReference(detail.payment)}</dd></div>
                  <div className="flex justify-between gap-3"><dt className="text-gray-500">وضعیت درگاه</dt><dd><span className={`rounded-full px-2 py-0.5 text-xs font-bold ${paymentBadge(detail.payment.status).className}`}>{formatAdminStatus(detail.payment.status)}</span></dd></div>
                  <div className="flex justify-between gap-3"><dt className="text-gray-500">تاریخ پرداخت</dt><dd className="font-bold">{formatAdminDate(detail.payment.completedAt ?? detail.payment.createdAt)}</dd></div>
                  <div className="flex justify-between gap-3"><dt className="text-gray-500">مبلغ قابل پرداخت</dt><dd className="font-bold">{formatAdminMoney(detail.payment.amount, detail.payment.currency)}</dd></div>
                </dl>
              ) : (
                <p className="mt-4 text-sm text-gray-500">پرداخت ثبت‌شده‌ای برای این checkout وجود ندارد.</p>
              )}
            </section>
          </div>

          <section className="rounded-2xl border border-gray-200 bg-white p-5 shadow-sm">
            <div className="mb-4 flex flex-wrap gap-2">
              {([
                ["summary", "خلاصه مالی"],
                ["sellers", "سهم فروشندگان"],
                ["payments", "پرداخت‌ها / واریزها"],
              ] as const).map(([id, label]) => (
                <button
                  key={id}
                  type="button"
                  onClick={() => setTab(id)}
                  className={`rounded-xl px-4 py-2 text-sm font-bold transition-colors ${tab === id ? "bg-[#2563EB] text-white" : "bg-gray-100 text-gray-700 hover:bg-gray-200"}`}
                >
                  {label}
                </button>
              ))}
            </div>

            {tab === "summary" ? (
              <div className="space-y-5">
                <SellerFinancialTable rows={detail.sellerFinancials} currency={detail.currency} />
                <FinancialSummaryCards summary={detail.financialSummary} />
              </div>
            ) : null}

            {tab === "sellers" ? (
              <div className="space-y-4">
                {detail.sellerOrders.map((order) => (
                  <article key={order.id} className="rounded-xl border border-gray-100 p-4">
                    <div className="flex flex-wrap items-center justify-between gap-3">
                      <div>
                        <h3 className="font-black">{order.sellerDisplayName}</h3>
                        <p className="text-sm text-gray-500">{order.orderNumber}</p>
                      </div>
                      <span className="rounded-full bg-gray-100 px-2.5 py-1 text-xs font-bold">{formatAdminStatus(order.status)}</span>
                    </div>
                    <ul className="mt-3 divide-y divide-gray-100">
                      {order.lines.map((line) => (
                        <li key={line.id} className="flex flex-wrap items-center justify-between gap-3 py-2 text-sm">
                          <div className="flex items-center gap-2"><Package className="size-4 text-gray-400" /><span>{line.title}</span></div>
                          <span>{line.quantity.toLocaleString("fa-IR")} × {formatAdminMoney(line.unitAmount, line.currency)}</span>
                          <strong>{formatAdminMoney(line.linePayable, line.currency)}</strong>
                        </li>
                      ))}
                    </ul>
                  </article>
                ))}
              </div>
            ) : null}

            {tab === "payments" ? (
              <SellerFinancialTable rows={detail.sellerFinancials} currency={detail.currency} />
            ) : null}
          </section>

          <section className="overflow-hidden rounded-2xl border border-gray-200 bg-white shadow-sm">
            <div className="border-b border-gray-200 px-5 py-4">
              <h2 className="font-black">سابقه پرداخت‌ها / واریزها</h2>
              <p className="mt-1 text-sm text-gray-500">رویدادهای مالی واقعی checkout از Payment و Settlement.</p>
            </div>
            <div className="p-2 md:p-4">
              <AppDataGrid {...historyGridProps} />
            </div>
          </section>
        </div>
      ) : (
        <p className="text-gray-500">در حال بارگذاری…</p>
      )}
    </main>
  );
}
