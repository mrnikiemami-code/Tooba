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
    <div className="flex min-h-[104px] flex-col rounded-xl border border-gray-200 bg-white p-3 shadow-sm">
      <div className="flex items-start justify-between gap-2">
        <div className="min-w-0 flex-1">
          <p className="text-xs font-medium text-gray-500">{label}</p>
          <div className="mt-1">
            {badge ? (
              <span className={`inline-flex rounded-full px-2.5 py-1 text-sm font-bold ${badge.className}`}>
                {badge.text}
              </span>
            ) : (
              <p className="text-lg font-black leading-tight tabular-nums text-gray-900">{value}</p>
            )}
          </div>
        </div>
        <span className={`inline-flex size-9 shrink-0 items-center justify-center rounded-lg bg-gradient-to-br ${tone} text-white shadow-sm`}>
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
    width: 120,
    minWidth: 100,
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
    width: 128,
    minWidth: 108,
  },
  {
    id: "amount",
    header: "مبلغ",
    accessor: (row) => row.amount,
    cell: (row) => formatAdminMoney(row.amount, row.currency),
    width: 128,
    minWidth: 100,
    sortable: true,
  },
  {
    id: "party",
    header: "طرف",
    accessor: (row) => row.partyDisplayName,
    width: 140,
    minWidth: 110,
  },
  {
    id: "reference",
    header: "مرجع",
    accessor: (row) => row.reference,
    cell: (row) => <span dir="ltr" className="font-mono text-[11px] text-gray-500">{row.reference}</span>,
    width: 120,
    minWidth: 96,
  },
  {
    id: "method",
    header: "روش پرداخت",
    accessor: (row) => row.paymentMethod,
    width: 112,
    minWidth: 92,
  },
  {
    id: "status",
    header: "وضعیت",
    accessor: (row) => row.status,
    cell: (row) => {
      const badge = paymentBadge(row.status);
      return <span className={`rounded-full px-2 py-0.5 text-xs font-bold ${badge.className}`}>{badge.text}</span>;
    },
    width: 100,
    minWidth: 84,
  },
  {
    id: "description",
    header: "توضیح",
    accessor: (row) => row.description,
    width: 180,
    minWidth: 140,
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
    <div className="overflow-x-auto rounded-xl border border-gray-200 bg-white">
      <table className="min-w-full text-sm">
        <thead className="border-b border-gray-200 bg-gray-50/90 text-xs text-gray-600">
          <tr>
            <th className="px-3 py-2.5 text-right font-bold">فروشنده</th>
            <th className="px-3 py-2.5 text-right font-bold">قلم</th>
            <th className="px-3 py-2.5 text-right font-bold">مبلغ ناخالص</th>
            <th className="px-3 py-2.5 text-right font-bold">کارمزد</th>
            <th className="px-3 py-2.5 text-right font-bold">قابل پرداخت</th>
            <th className="px-3 py-2.5 text-right font-bold">وضعیت تسویه</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-gray-100">
          {rows.map((row) => {
            const badge = settlementBadge(row.settlementStatus);
            return (
              <tr key={row.sellerOrderId} className="hover:bg-gray-50/60">
                <td className="px-3 py-2.5 font-semibold text-gray-900">{row.sellerDisplayName}</td>
                <td className="px-3 py-2.5 tabular-nums text-gray-700">{row.lineCount.toLocaleString("fa-IR")}</td>
                <td className="px-3 py-2.5 tabular-nums font-medium">{formatAdminMoney(row.grossAmount, row.currency)}</td>
                <td className="px-3 py-2.5 tabular-nums text-gray-600">
                  {formatAdminMoneyOptional(
                    row.commissionAmount,
                    row.currency,
                    row.settlementStatus !== "Settled" && row.commissionAmount === 0,
                  )}
                </td>
                <td className="px-3 py-2.5 tabular-nums font-semibold">{formatAdminMoney(row.payableAmount, row.currency)}</td>
                <td className="px-3 py-2.5">
                  <span className={`rounded-full px-2 py-0.5 text-[11px] font-bold ${badge.className}`}>{badge.text}</span>
                </td>
              </tr>
            );
          })}
        </tbody>
        <tfoot className="border-t border-gray-200 bg-slate-50 text-sm font-bold">
          <tr>
            <td className="px-3 py-2.5">جمع</td>
            <td className="px-3 py-2.5 tabular-nums">{totals.lines.toLocaleString("fa-IR")}</td>
            <td className="px-3 py-2.5 tabular-nums">{formatAdminMoney(totals.gross, currency)}</td>
            <td className="px-3 py-2.5 tabular-nums">{formatAdminMoney(totals.commission, currency)}</td>
            <td className="px-3 py-2.5 tabular-nums text-blue-800">{formatAdminMoney(totals.payable, currency)}</td>
            <td className="px-3 py-2.5">—</td>
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
    <div className="grid gap-3 lg:grid-cols-2">
      <section className="rounded-xl border border-gray-200 bg-white p-4">
        <h3 className="text-sm font-black text-gray-900">خلاصه مالی کل</h3>
        <dl className="mt-3 divide-y divide-gray-100 text-sm">
          <div className="flex justify-between gap-3 py-2"><dt className="text-gray-500">جمع سهم فروشندگان</dt><dd className="font-bold tabular-nums">{formatAdminMoney(summary.totalSellerShare, summary.currency)}</dd></div>
          <div className="flex justify-between gap-3 py-2"><dt className="text-gray-500">جمع کارمزد</dt><dd className="font-bold tabular-nums">{formatAdminMoney(summary.totalCommission, summary.currency)}</dd></div>
          <div className="flex justify-between gap-3 py-2"><dt className="text-gray-500">سود ناخالص سفارش</dt><dd className="font-bold tabular-nums">{formatAdminMoney(summary.grossOrderProfit, summary.currency)}</dd></div>
          <div className="flex justify-between gap-3 rounded-lg bg-blue-50 px-3 py-2"><dt className="font-bold text-blue-800">قابل پرداخت به فروشندگان</dt><dd className="font-black tabular-nums text-blue-800">{formatAdminMoney(summary.payableToSellers, summary.currency)}</dd></div>
        </dl>
      </section>
      <section className="rounded-xl border border-gray-200 bg-white p-4">
        <h3 className="text-sm font-black text-gray-900">رسید مشتری</h3>
        <dl className="mt-3 divide-y divide-gray-100 text-sm">
          <div className="flex justify-between gap-3 py-2"><dt className="text-gray-500">مبلغ ناخالص سفارش</dt><dd className="font-bold tabular-nums">{formatAdminMoney(summary.customerGrossAmount, summary.currency)}</dd></div>
          <div className="flex justify-between gap-3 py-2"><dt className="text-gray-500">هزینه ارسال</dt><dd className="font-bold tabular-nums">{formatAdminMoney(summary.shippingCost, summary.currency)}</dd></div>
          <div className="flex justify-between gap-3 py-2"><dt className="text-gray-500">تخفیف مشتری</dt><dd className="font-bold tabular-nums">{formatAdminMoney(summary.customerDiscounts, summary.currency)}</dd></div>
          <div className="flex justify-between gap-3 rounded-lg bg-emerald-50 px-3 py-2"><dt className="font-bold text-emerald-800">جمع دریافت از مشتری</dt><dd className="font-black tabular-nums text-emerald-800">{formatAdminMoney(summary.totalReceivedFromCustomer, summary.currency)}</dd></div>
        </dl>
      </section>
    </div>
  );
}

function InfoRow({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex items-start justify-between gap-3 border-b border-gray-100 py-2 last:border-b-0">
      <dt className="shrink-0 text-xs text-gray-500">{label}</dt>
      <dd className="min-w-0 text-left text-sm font-semibold text-gray-900">{children}</dd>
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
    <main data-testid="admin-order-detail" className="pb-4">
      <header className="mb-3 flex flex-wrap items-center justify-between gap-3 border-b border-gray-100 pb-3">
        <div className="min-w-0">
          <p className="text-[11px] font-medium text-gray-400">خانه / سفارش‌ها / جزئیات سفارش</p>
          <div className="mt-0.5 flex flex-wrap items-baseline gap-x-3 gap-y-1">
            <h1 className="text-xl font-black tracking-tight text-gray-900">جزئیات سفارش</h1>
            <p className="font-mono text-xs text-gray-500" dir="ltr">{detail?.reference ?? checkoutId}</p>
          </div>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <Link href="/admin/orders" className="inline-flex items-center gap-1.5 rounded-lg border border-gray-200 bg-white px-3 py-1.5 text-xs font-bold text-gray-700 hover:bg-gray-50">
            <ArrowRight className="size-3.5" />
            بازگشت
          </Link>
          <button type="button" className="inline-flex items-center gap-1.5 rounded-lg border border-gray-200 bg-white px-3 py-1.5 text-xs font-bold text-gray-700 hover:bg-gray-50">
            <Printer className="size-3.5" />
            چاپ فاکتور
          </button>
          <button type="button" className="inline-flex items-center gap-1.5 rounded-lg bg-[#2563EB] px-3 py-1.5 text-xs font-bold text-white shadow-sm hover:bg-blue-700">
            ایجاد سند تسویه
          </button>
        </div>
      </header>

      {result.state === "error" ? (
        <ErrorState title="سفارش خوانده نشد" detail={result.message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
      ) : detail ? (
        <div className="space-y-4">
          <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-5">
            <SummaryCard label="تعداد اقلام" value={`${detail.lineCount.toLocaleString("fa-IR")} قلم`} icon={<ShoppingBag className="size-4" />} tone="from-violet-500 to-violet-600" />
            <SummaryCard label="تعداد فروشنده" value={`${detail.sellerCount.toLocaleString("fa-IR")} فروشنده`} icon={<Store className="size-4" />} tone="from-blue-500 to-blue-600" />
            <SummaryCard label="مبلغ کل سفارش" value={formatAdminMoney(detail.payableAmount, detail.currency)} icon={<Wallet className="size-4" />} tone="from-emerald-500 to-emerald-600" />
            <SummaryCard label="وضعیت پرداخت" value={paymentBadgeState.text} icon={<CreditCard className="size-4" />} tone="from-teal-500 to-teal-600" badge={paymentBadgeState} />
            <SummaryCard label="وضعیت سفارش" value={statusBadge.text} icon={<ClipboardList className="size-4" />} tone="from-amber-500 to-amber-600" badge={statusBadge} />
          </div>

          <div className="grid gap-3 lg:grid-cols-2">
            <section className="flex min-h-[220px] flex-col rounded-xl border border-gray-200 bg-white p-4 shadow-sm">
              <h2 className="text-sm font-black text-gray-900">اطلاعات مشتری و ارسال</h2>
              <div className="mt-2 border-t border-gray-100 pt-2">
                <p className="text-sm font-bold text-gray-900">{detail.recipientName || "مشتری توبا"}</p>
                <p className="mt-0.5 text-xs text-gray-500" dir="ltr">{detail.contactMobile || "—"}</p>
                <p className="mt-2 text-sm leading-6 text-gray-700">
                  {detail.provinceName}، {detail.cityName}
                  <br />
                  {detail.postalAddress}
                </p>
                <p className="mt-1 text-xs text-gray-500">
                  کد پستی: {detail.postalCode || "—"} · {detail.shippingMethodLabel || "ارسال"}
                </p>
              </div>
            </section>
            <section className="flex min-h-[220px] flex-col rounded-xl border border-gray-200 bg-white p-4 shadow-sm">
              <h2 className="text-sm font-black text-gray-900">اطلاعات پرداخت</h2>
              {detail.payment ? (
                <dl className="mt-2 flex-1 border-t border-gray-100 pt-1">
                  <InfoRow label="درگاه">{formatAdminPaymentProvider(detail.payment.providerCode)}</InfoRow>
                  <InfoRow label="شناسه تراکنش"><span dir="ltr" className="font-mono text-[11px] font-medium text-gray-600">{formatAdminPaymentReference(detail.payment)}</span></InfoRow>
                  <InfoRow label="وضعیت درگاه"><span className={`rounded-full px-2 py-0.5 text-[11px] font-bold ${paymentBadge(detail.payment.status).className}`}>{formatAdminStatus(detail.payment.status)}</span></InfoRow>
                  <InfoRow label="تاریخ پرداخت">{formatAdminDate(detail.payment.completedAt ?? detail.payment.createdAt)}</InfoRow>
                  <InfoRow label="مبلغ قابل پرداخت">{formatAdminMoney(detail.payment.amount, detail.payment.currency)}</InfoRow>
                </dl>
              ) : (
                <p className="mt-3 text-sm text-gray-500">پرداخت ثبت‌شده‌ای برای این checkout وجود ندارد.</p>
              )}
            </section>
          </div>

          <section className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
            <div className="flex flex-wrap items-center justify-between gap-2 border-b border-gray-200 bg-gray-50/70 px-3 py-2">
              <h2 className="text-sm font-black text-gray-900">بخش مالی سفارش</h2>
              <div className="flex flex-wrap gap-1.5">
                {([
                  ["summary", "خلاصه مالی"],
                  ["sellers", "سهم فروشندگان"],
                  ["payments", "پرداخت‌ها / واریزها"],
                ] as const).map(([id, label]) => (
                  <button
                    key={id}
                    type="button"
                    onClick={() => setTab(id)}
                    className={`rounded-lg px-3 py-1.5 text-xs font-bold transition-colors ${tab === id ? "bg-[#2563EB] text-white shadow-sm" : "bg-white text-gray-700 ring-1 ring-gray-200 hover:bg-gray-50"}`}
                  >
                    {label}
                  </button>
                ))}
              </div>
            </div>

            <div className="space-y-4 p-3 md:p-4">
              {tab === "summary" ? (
                <>
                  <SellerFinancialTable rows={detail.sellerFinancials} currency={detail.currency} />
                  <FinancialSummaryCards summary={detail.financialSummary} />
                </>
              ) : null}

              {tab === "sellers" ? (
                <div className="space-y-3">
                  {detail.sellerOrders.map((order) => (
                    <article key={order.id} className="rounded-lg border border-gray-100 bg-gray-50/40 p-3">
                      <div className="flex flex-wrap items-center justify-between gap-2">
                        <div>
                          <h3 className="text-sm font-black text-gray-900">{order.sellerDisplayName}</h3>
                          <p className="text-xs text-gray-500">{order.orderNumber}</p>
                        </div>
                        <span className="rounded-full bg-white px-2 py-0.5 text-[11px] font-bold ring-1 ring-gray-200">{formatAdminStatus(order.status)}</span>
                      </div>
                      <ul className="mt-2 divide-y divide-gray-200/80 rounded-lg bg-white px-2">
                        {order.lines.map((line) => (
                          <li key={line.id} className="flex flex-wrap items-center justify-between gap-2 py-2 text-sm">
                            <div className="flex min-w-0 items-center gap-2"><Package className="size-3.5 shrink-0 text-gray-400" /><span className="truncate">{line.title}</span></div>
                            <span className="text-xs text-gray-600">{line.quantity.toLocaleString("fa-IR")} × {formatAdminMoney(line.unitAmount, line.currency)}</span>
                            <strong className="tabular-nums">{formatAdminMoney(line.linePayable, line.currency)}</strong>
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
            </div>
          </section>

          <section className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
            <div className="flex flex-wrap items-center justify-between gap-2 border-b border-gray-200 px-3 py-2.5">
              <div>
                <h2 className="text-sm font-black text-gray-900">سابقه پرداخت‌ها / واریزها</h2>
                <p className="text-xs text-gray-500">رویدادهای مالی واقعی checkout</p>
              </div>
            </div>
            <div className="p-2 [&_.ag-root-wrapper]:min-h-[140px]">
              <AppDataGrid {...historyGridProps} />
            </div>
          </section>
        </div>
      ) : (
        <p className="text-sm text-gray-500">در حال بارگذاری…</p>
      )}
    </main>
  );
}
