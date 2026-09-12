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
  formatJalaliDateTime,
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
import { AdminOrderItemsShippingPanel } from "./admin-order-items-shipping-panel";
import { AdminOrderOperationsMenu } from "./admin-order-operations-menu";
import { loadAdminOrderOperations } from "./admin-order-operations";
import { paymentStatusBadge, resolveOrderStatusCard, resolvePaymentStatusCard } from "./admin-order-status-cards";
import {
  addAdminOrderNote,
  adminOrderInvoiceUrl,
  adminOrderReceiptUrl,
  deleteAdminOrderNote,
  loadAdminOrderNotes,
  loadAdminOrderOperationalHistory,
  openAdminOrderHtmlDocument,
  type AdminOperationalHistoryEntry,
  type AdminOrderNote,
} from "./admin-order-completeness";
import { mapAdminErrorMessage } from "./admin-error-map";

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
      return "دریافت از مشتری";
    case "CustomerRefund":
      return "بازگشت وجه به مشتری";
    case "SellerPayout":
    case "SellerSettlement":
      return "واریز سهم فروشنده";
    case "SellerRefundAdjustment":
    case "SettlementAdjustment":
      return "کسر از حساب فروشنده بابت بازگشت وجه";
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
    case "CustomerRefund":
      return "bg-rose-50 text-rose-700";
    case "SellerPayout":
    case "SellerSettlement":
      return "bg-emerald-50 text-emerald-700";
    case "SellerRefundAdjustment":
    case "SettlementAdjustment":
      return "bg-amber-50 text-amber-800";
    case "WalletDeposit":
      return "bg-blue-50 text-blue-700";
    default:
      return "bg-gray-100 text-gray-700";
  }
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
    width: 168,
    minWidth: 140,
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
      const badge = paymentStatusBadge(row.status);
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

function OperationalHistoryTimeline({
  entries,
  empty,
}: {
  entries: AdminOperationalHistoryEntry[];
  empty: string;
}) {
  const [filter, setFilter] = useState<"all" | "order" | "shipment" | "return" | "payment">("all");
  const filtered = useMemo(() => {
    if (filter === "all") return entries;
    return entries.filter((entry) => historyFilterBucket(entry.kind) === filter);
  }, [entries, filter]);

  if (entries.length === 0) {
    return (
      <p className="text-sm text-gray-500" data-testid="admin-order-history-empty">
        {empty}
      </p>
    );
  }

  const filters: { id: typeof filter; label: string }[] = [
    { id: "all", label: "همه" },
    { id: "order", label: "سفارش" },
    { id: "shipment", label: "ارسال" },
    { id: "return", label: "مرجوعی" },
    { id: "payment", label: "پرداخت" },
  ];

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap gap-1.5" data-testid="admin-order-history-filters">
        {filters.map((item) => (
          <button
            key={item.id}
            type="button"
            onClick={() => setFilter(item.id)}
            className={`rounded-lg px-2.5 py-1 text-[11px] font-bold ${
              filter === item.id
                ? "bg-slate-800 text-white"
                : "border border-gray-200 bg-white text-gray-600 hover:bg-gray-50"
            }`}
            data-testid={`admin-order-history-filter-${item.id}`}
          >
            {item.label}
          </button>
        ))}
      </div>
      {filtered.length === 0 ? (
        <p className="text-sm text-gray-500">رویدادی در این محدوده نیست.</p>
      ) : (
        <ol className="space-y-3" data-testid="admin-order-operational-history">
          {filtered.map((entry, index) => (
            <li
              key={`${entry.kind}-${entry.occurredAt}-${index}`}
              className="rounded-xl border border-gray-100 p-3 text-sm"
              data-testid={`admin-order-history-${entry.kind}`}
            >
              <div className="flex flex-wrap items-center justify-between gap-2">
                <span className="font-semibold text-gray-900">{entry.labelFa}</span>
                <span className="text-xs text-gray-500" dir="ltr">
                  {formatJalaliDateTime(entry.occurredAt, "fa")}
                </span>
              </div>
              {entry.summaryFa ? <p className="mt-1 text-gray-600">{entry.summaryFa}</p> : null}
              <p className="mt-2 text-xs text-gray-500">{entry.actorDisplayFa}</p>
            </li>
          ))}
        </ol>
      )}
    </div>
  );
}

function historyFilterBucket(kind: string): "order" | "shipment" | "return" | "payment" {
  if (
    kind.startsWith("shipment_")
    || kind.startsWith("fulfillment_")
    || kind === "tracking_assigned"
  ) {
    return "shipment";
  }
  if (kind.startsWith("return_") || kind.startsWith("refund_")) {
    return "return";
  }
  if (
    kind.startsWith("payment_")
    || kind.startsWith("settlement_")
  ) {
    return "payment";
  }
  return "order";
}

/** جزئیات سفارش Admin با UX مالی بازارگاه مطابق مرجع T042. */
export function AdminOrderDetailScreen({ checkoutId }: { checkoutId: string }) {
  const [result, setResult] = useState<AdminResult<AdminOrderDetail>>({ state: "ok", data: null, status: 0 });
  const [tab, setTab] = useState<"summary" | "sellers" | "payments">("summary");
  const [notes, setNotes] = useState<AdminOrderNote[]>([]);
  const [noteBody, setNoteBody] = useState("");
  const [noteBusy, setNoteBusy] = useState(false);
  const [noteError, setNoteError] = useState<string | null>(null);
  const [historyEntries, setHistoryEntries] = useState<AdminOperationalHistoryEntry[]>([]);
  const [historyPage, setHistoryPage] = useState(1);
  const [historyTotal, setHistoryTotal] = useState(0);
  const [historyError, setHistoryError] = useState<string | null>(null);
  const [docError, setDocError] = useState<string | null>(null);
  const historyPageSize = 20;
  const refreshNotes = useCallback(() => {
    void loadAdminOrderNotes(checkoutId).then((res) => {
      if (res.state === "ok" && res.data) setNotes(res.data);
    });
  }, [checkoutId]);

  const refreshHistory = useCallback(
    (page: number, append: boolean) => {
      void loadAdminOrderOperationalHistory(checkoutId, page, historyPageSize).then((res) => {
        if (res.state !== "ok" || !res.data) {
          setHistoryError(res.message || mapAdminErrorMessage("order.history.failed", "fa"));
          return;
        }
        setHistoryError(null);
        setHistoryPage(res.data.page);
        setHistoryTotal(res.data.totalCount);
        setHistoryEntries((prev) => (append ? [...prev, ...res.data!.items] : res.data!.items));
      });
    },
    [checkoutId],
  );

  const refresh = () => {
    void loadAdminOrderDetail(checkoutId).then(setResult);
    refreshNotes();
    refreshHistory(1, false);
  };
  useEffect(refresh, [checkoutId, refreshNotes, refreshHistory]);

  const openInvoice = async () => {
    setDocError(null);
    const res = await openAdminOrderHtmlDocument(adminOrderInvoiceUrl(checkoutId), "order.invoice.unavailable");
    if (res.state !== "ok") setDocError(res.message || mapAdminErrorMessage("order.invoice.unavailable", "fa"));
  };

  const openReceipt = async () => {
    setDocError(null);
    const res = await openAdminOrderHtmlDocument(adminOrderReceiptUrl(checkoutId), "order.receipt.unavailable");
    if (res.state !== "ok") setDocError(res.message || mapAdminErrorMessage("order.receipt.unavailable", "fa"));
  };

  const submitNote = async () => {
    setNoteBusy(true);
    setNoteError(null);
    const res = await addAdminOrderNote(checkoutId, noteBody);
    setNoteBusy(false);
    if (res.state !== "ok") {
      setNoteError(res.message || mapAdminErrorMessage("order.note.invalid", "fa"));
      return;
    }
    setNoteBody("");
    refreshNotes();
    refreshHistory(1, false);
  };

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
  const paymentBadgeState = detail ? resolvePaymentStatusCard(detail) : paymentStatusBadge("PendingPayment");
  const orderBadgeState = detail ? resolveOrderStatusCard(detail) : resolveOrderStatusCard({ status: "Submitted" });

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
          <AdminOrderOperationsMenu
            checkoutId={checkoutId}
            label="عملیات سفارش"
            scope="whole-order"
            onCompleted={refresh}
            testId={`admin-order-detail-ops-${checkoutId}`}
          />
          <button
            type="button"
            data-testid="admin-order-print-invoice"
            onClick={() => void openInvoice()}
            className="inline-flex items-center gap-1.5 rounded-lg border border-gray-200 bg-white px-3 py-1.5 text-xs font-bold text-gray-700 hover:bg-gray-50"
          >
            <Printer className="size-3.5" />
            چاپ فاکتور
          </button>
          {detail?.payment ? (
            <button
              type="button"
              data-testid="admin-order-print-receipt"
              onClick={() => void openReceipt()}
              className="inline-flex items-center gap-1.5 rounded-lg border border-gray-200 bg-white px-3 py-1.5 text-xs font-bold text-gray-700 hover:bg-gray-50"
            >
              <Printer className="size-3.5" />
              چاپ رسید پرداخت
            </button>
          ) : null}
        </div>
      </header>
      {docError ? <p className="mb-3 text-sm text-red-600">{docError}</p> : null}
      <InventoryRecoveryBanner checkoutId={checkoutId} />

      {result.state === "error" ? (
        <ErrorState title="سفارش خوانده نشد" detail={result.message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
      ) : detail ? (
        <div className="space-y-4">
          <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-5">
            <SummaryCard label="تعداد اقلام" value={`${detail.lineCount.toLocaleString("fa-IR")} قلم`} icon={<ShoppingBag className="size-4" />} tone="from-violet-500 to-violet-600" />
            <SummaryCard label="تعداد فروشنده" value={`${detail.sellerCount.toLocaleString("fa-IR")} فروشنده`} icon={<Store className="size-4" />} tone="from-blue-500 to-blue-600" />
            <SummaryCard label="مبلغ کل سفارش" value={formatAdminMoney(detail.payableAmount, detail.currency)} icon={<Wallet className="size-4" />} tone="from-emerald-500 to-emerald-600" />
            <SummaryCard label="وضعیت پرداخت" value="" icon={<CreditCard className="size-4" />} tone="from-teal-500 to-teal-600" badge={paymentBadgeState} />
            <SummaryCard label="وضعیت سفارش" value="" icon={<ClipboardList className="size-4" />} tone="from-amber-500 to-amber-600" badge={orderBadgeState} />
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
                  <InfoRow label="وضعیت درگاه"><span className={`rounded-full px-2 py-0.5 text-[11px] font-bold ${paymentStatusBadge(detail.payment.providerCode?.toLowerCase() === "manual" && detail.payment.status === "Pending" ? "PendingManualConfirmation" : detail.payment.status).className}`}>{formatAdminStatus(detail.payment.providerCode?.toLowerCase() === "manual" && detail.payment.status === "Pending" ? "PendingManualConfirmation" : detail.payment.status)}</span></InfoRow>
                  <InfoRow label="تاریخ پرداخت">{formatAdminDate(detail.payment.completedAt ?? detail.payment.createdAt)}</InfoRow>
                  <InfoRow label="مبلغ قابل پرداخت">{formatAdminMoney(detail.payment.amount, detail.payment.currency)}</InfoRow>
                  {detail.payment.customerTransferReference ? (
                    <InfoRow label="شماره پیگیری پرداخت"><span dir="ltr">{detail.payment.customerTransferReference}</span></InfoRow>
                  ) : null}
                  {detail.payment.proofMediaAssetId ? (
                    <InfoRow label="مدرک پرداخت">
                      <a className="text-[#2563EB] text-xs font-bold" href={`/v1/media/${detail.payment.proofMediaAssetId}`} target="_blank" rel="noreferrer">مشاهده مدرک</a>
                    </InfoRow>
                  ) : null}
                  {detail.payment.evidenceSubmittedAt ? (
                    <InfoRow label="زمان ثبت مشتری">{formatAdminDate(detail.payment.evidenceSubmittedAt)}</InfoRow>
                  ) : null}
                </dl>
              ) : (
                <p className="mt-3 text-sm text-gray-500">پرداخت ثبت‌شده‌ای برای این checkout وجود ندارد.</p>
              )}
            </section>
          </div>

          <AdminOrderItemsShippingPanel detail={detail} checkoutId={checkoutId} onCompleted={refresh} />

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

          <section className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm" data-testid="admin-order-internal-notes">
            <div className="border-b border-gray-200 px-3 py-2.5">
              <h2 className="text-sm font-black text-gray-900">یادداشت داخلی</h2>
              <p className="text-xs text-gray-500">فقط برای اپراتور؛ در ویترین نمایش داده نمی‌شود</p>
            </div>
            <div className="space-y-3 p-3">
              {notes.length === 0 ? (
                <p className="text-sm text-gray-500">هنوز یادداشتی ثبت نشده است.</p>
              ) : (
                <ul className="space-y-2">
                  {notes.map((note) => (
                    <li key={note.noteId} className="rounded-lg border border-gray-100 bg-gray-50/50 p-2.5 text-sm">
                      <p className="whitespace-pre-wrap text-gray-900">{note.body}</p>
                      <div className="mt-1.5 flex flex-wrap items-center gap-3 text-[11px] text-gray-500">
                        <span>{note.actorDisplayFa}</span>
                        <span dir="ltr">{formatJalaliDateTime(note.createdAt, "fa")}</span>
                        {note.canDelete ? (
                          <button
                            type="button"
                            className="text-red-600 hover:underline"
                            data-testid={`admin-order-note-delete-${note.noteId}`}
                            onClick={() => {
                              if (!window.confirm("این یادداشت حذف شود؟")) return;
                              void deleteAdminOrderNote(checkoutId, note.noteId).then((res) => {
                                if (res.state === "ok") refreshNotes();
                                else setNoteError(res.message || "حذف یادداشت ممکن نیست.");
                              });
                            }}
                          >
                            حذف
                          </button>
                        ) : null}
                      </div>
                    </li>
                  ))}
                </ul>
              )}
              <textarea
                value={noteBody}
                onChange={(e) => setNoteBody(e.target.value)}
                maxLength={2000}
                rows={3}
                placeholder="یادداشت عملیاتی…"
                className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm outline-none focus:border-blue-400"
                data-testid="admin-order-note-input"
              />
              {noteError ? <p className="text-sm text-red-600">{noteError}</p> : null}
              <button
                type="button"
                disabled={noteBusy || noteBody.trim().length === 0}
                onClick={() => void submitNote()}
                className="rounded-lg bg-[#2563EB] px-3 py-1.5 text-xs font-bold text-white disabled:opacity-50"
                data-testid="admin-order-note-submit"
              >
                ثبت یادداشت
              </button>
            </div>
          </section>

          <section className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
            <div className="flex flex-wrap items-center justify-between gap-2 border-b border-gray-200 px-3 py-2.5">
              <div>
                <h2 className="text-sm font-black text-gray-900">سابقه پرداخت‌ها / واریزها</h2>
              </div>
            </div>
            <div className="p-2 [&_.ag-root-wrapper]:min-h-[140px]">
              <AppDataGrid {...historyGridProps} />
            </div>
          </section>

          <section className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm" data-testid="admin-order-history-section">
            <div className="border-b border-gray-200 px-3 py-2.5">
              <h2 className="text-sm font-black text-gray-900">تاریخچه عملیات</h2>
              <p className="text-xs text-gray-500">ترکیب رویدادهای موجود سفارش / پرداخت / ارسال / مرجوعی</p>
            </div>
            <div className="space-y-3 p-3">
              {historyError ? <p className="text-sm text-red-600">{historyError}</p> : null}
              <OperationalHistoryTimeline entries={historyEntries} empty="هنوز رویدادی ثبت نشده است." />
              {historyEntries.length < historyTotal ? (
                <button
                  type="button"
                  data-testid="admin-order-history-load-more"
                  onClick={() => refreshHistory(historyPage + 1, true)}
                  className="rounded-lg border border-gray-200 bg-white px-3 py-1.5 text-xs font-bold text-gray-700 hover:bg-gray-50"
                >
                  مشاهده بیشتر
                </button>
              ) : null}
            </div>
          </section>
        </div>
      ) : (
        <p className="text-sm text-gray-500">در حال بارگذاری…</p>
      )}
    </main>
  );
}

function InventoryRecoveryBanner({ checkoutId }: { checkoutId: string }) {
  const [warning, setWarning] = useState<string | null>(null);
  useEffect(() => {
    let cancelled = false;
    void loadAdminOrderOperations(checkoutId).then((result) => {
      if (cancelled) return;
      if (result.state === "ok" && result.data?.inventoryRecoveryWarningFa) {
        setWarning(result.data.inventoryRecoveryWarningFa);
      } else {
        setWarning(null);
      }
    });
    return () => {
      cancelled = true;
    };
  }, [checkoutId]);
  if (!warning) return null;
  return (
    <p
      data-testid="admin-order-inventory-recovery-warning"
      className="mb-3 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-sm font-medium text-amber-900"
    >
      {warning}
    </p>
  );
}
