"use client";

import { useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import {
  CheckCircle,
  Clock,
  FileText,
  Minus,
  TrendingDown,
  TrendingUp,
  Wallet,
  X,
  XCircle,
} from "lucide-react";
import {
  formatEntryType,
  formatPayoutStatus,
  formatSettlementMoney,
  loadSellerPayoutRequests,
  loadSellerSettlementBalance,
  loadSellerSettlementEntries,
  requestSellerPayout,
  type PayoutRequestRow,
  type SettlementBalance,
  type SettlementEntryRow,
} from "../settlement/settlement-api";
import { readSellerPartyId } from "./seller-api";

const ACCENT = "#2563EB";
const ACCENT_DARK = "#1D4ED8";

function toPersianDigits(value: number | string): string {
  return String(value).replace(/\d/g, (d) => "۰۱۲۳۴۵۶۷۸۹"[Number(d)]);
}

function formatPostedDate(iso: string): string {
  if (!iso) return "—";
  try {
    return new Intl.DateTimeFormat("fa-IR", { dateStyle: "short", timeStyle: "short" }).format(new Date(iso));
  } catch {
    return iso;
  }
}

function isCreditEntry(entry: SettlementEntryRow): boolean {
  return entry.entryType === "Credit" || entry.entryType === "0" || entry.netAmount >= 0;
}

/**
 * کیف پول فروشنده — پورت ساختار Shopeiva VendorWallet با دادهٔ زندهٔ تسویه marketplace.
 * شارژ/کارت بانکی حذف شده؛ فقط مانده، accrual و درخواست payout.
 */
export function VendorWalletUi({ sellerPartyId }: { sellerPartyId: string }) {
  const [balance, setBalance] = useState<SettlementBalance | null>(null);
  const [entries, setEntries] = useState<SettlementEntryRow[]>([]);
  const [payouts, setPayouts] = useState<PayoutRequestRow[]>([]);
  const [message, setMessage] = useState<string>();
  const [loading, setLoading] = useState(true);
  const [showWithdraw, setShowWithdraw] = useState(false);
  const [amount, setAmount] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [actionMessage, setActionMessage] = useState<string>();

  const refresh = useCallback(async () => {
    setLoading(true);
    const [balanceResult, entryRows, payoutRows] = await Promise.all([
      loadSellerSettlementBalance(sellerPartyId),
      loadSellerSettlementEntries(sellerPartyId),
      loadSellerPayoutRequests(sellerPartyId),
    ]);
    setBalance(balanceResult.balance);
    setMessage(balanceResult.message);
    setEntries(entryRows);
    setPayouts(payoutRows);
    setLoading(false);
  }, [sellerPartyId]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const totalCredits = balance?.postedCredits ?? entries.filter(isCreditEntry).reduce((s, e) => s + e.netAmount, 0);
  const totalDebits = balance?.postedDebits ?? entries.filter((e) => !isCreditEntry(e)).reduce((s, e) => s + Math.abs(e.netAmount), 0);
  const available = balance?.availableBalance ?? 0;
  const transactionCount = entries.length + payouts.length;

  const timeline = useMemo(() => {
    const entryItems = entries.map((entry) => ({
      id: entry.entryId,
      kind: "entry" as const,
      isCredit: isCreditEntry(entry),
      label: formatEntryType(entry.entryType),
      date: entry.postedAt,
      amount: entry.netAmount,
      status: "posted",
      currency: entry.currency,
    }));
    const payoutItems = payouts.map((payout) => ({
      id: payout.payoutRequestId,
      kind: "payout" as const,
      isCredit: false,
      label: "درخواست برداشت",
      date: payout.createdAt,
      amount: payout.amount,
      status: payout.status,
      currency: payout.currency,
    }));
    return [...entryItems, ...payoutItems].sort((a, b) => b.date.localeCompare(a.date));
  }, [entries, payouts]);

  const handleWithdraw = async () => {
    const parsed = Number(amount.replace(/\D/g, ""));
    if (!parsed || parsed < 10000) {
      setActionMessage("حداقل مبلغ برداشت ۱۰٬۰۰۰ ریال است.");
      return;
    }
    if (parsed > available) {
      setActionMessage("موجودی قابل برداشت کافی نیست.");
      return;
    }
    setSubmitting(true);
    setActionMessage(undefined);
    const result = await requestSellerPayout(sellerPartyId, parsed, `wallet-${Date.now()}`);
    setSubmitting(false);
    if (!result.ok) {
      setActionMessage(result.message ?? "درخواست برداشت ناموفق بود.");
      return;
    }
    setShowWithdraw(false);
    setAmount("");
    setActionMessage("درخواست برداشت ثبت شد.");
    await refresh();
  };

  if (loading) {
    return <p className="text-muted">در حال بارگذاری کیف پول…</p>;
  }

  if (!balance && message === "settlement.account.missing") {
    return (
      <div className="rounded-2xl border border-dashed border-gray-300 bg-white p-8 text-center">
        <Wallet className="mx-auto size-10 text-gray-400" />
        <h2 className="mt-4 text-lg font-bold">حساب تسویه هنوز ایجاد نشده</h2>
        <p className="mt-2 text-sm text-gray-500">
          پس از اولین پرداخت موفق سفارش marketplace، accrual تسویه به‌صورت خودکار ثبت می‌شود.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-6" data-testid="vendor-wallet">
      <div
        className="relative overflow-hidden rounded-2xl p-6 md:p-8"
        style={{ background: `linear-gradient(to bottom right, ${ACCENT}, ${ACCENT_DARK})` }}
      >
        <div className="absolute inset-0 opacity-10">
          <div className="absolute top-0 right-0 h-64 w-64 -translate-y-1/2 translate-x-1/3 rounded-full bg-white blur-3xl" />
          <div className="absolute bottom-0 left-0 h-48 w-48 translate-y-1/2 -translate-x-1/3 rounded-full bg-white blur-3xl" />
        </div>
        <div className="relative z-10">
          <div className="mb-4 flex items-center gap-2 text-white/80">
            <Wallet className="size-5" />
            <span className="text-sm font-medium">موجودی قابل برداشت فروشنده</span>
          </div>
          <p className="text-3xl font-black text-white md:text-4xl">
            {toPersianDigits(new Intl.NumberFormat("fa-IR").format(available))}
            <span className="mr-1 text-lg font-medium text-white/70">ریال</span>
          </p>
          <div className="mt-6 flex gap-3">
            <button
              type="button"
              onClick={() => setShowWithdraw(true)}
              disabled={available <= 0}
              className="flex items-center gap-2 rounded-xl bg-white/20 px-5 py-2.5 text-sm font-bold text-white backdrop-blur transition-colors hover:bg-white/30 disabled:cursor-not-allowed disabled:opacity-50"
            >
              <Minus className="size-4" />
              درخواست برداشت
            </button>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-3 gap-3">
        <Stat icon={<TrendingUp className="size-4 text-emerald-500" />} label="کل واریز" value={totalCredits} tone="text-emerald-500" />
        <Stat icon={<TrendingDown className="size-4 text-red-500" />} label="کل برداشت" value={totalDebits} tone="text-red-500" />
        <Stat icon={<FileText className="size-4" style={{ color: ACCENT }} />} label="تعداد تراکنش" value={transactionCount} tone="" accent />
      </div>

      {actionMessage ? <p className="rounded-xl bg-blue-50 px-4 py-2 text-sm text-blue-800">{actionMessage}</p> : null}

      <div className="overflow-hidden rounded-2xl border border-gray-200 bg-white dark:border-gray-800 dark:bg-[#111]">
        <div className="border-b border-gray-200 bg-gradient-to-r from-[#2563EB]/5 to-transparent p-4 dark:border-gray-800">
          <h3 className="flex items-center gap-2 font-bold text-gray-900 dark:text-white">
            <FileText className="size-5 text-[#2563EB]" />
            تاریخچه تسویه و برداشت
          </h3>
        </div>
        <div className="divide-y divide-gray-100 dark:divide-gray-800">
          {timeline.length === 0 ? (
            <p className="p-6 text-center text-sm text-gray-500">هنوز تراکنشی ثبت نشده است.</p>
          ) : (
            timeline.map((item) => {
              const statusLabel = item.kind === "payout" ? formatPayoutStatus(item.status) : "ثبت‌شده";
              const StatusIcon = item.status === "Succeeded" || item.status === "posted" ? CheckCircle : item.status === "Failed" ? XCircle : Clock;
              const statusColor =
                item.status === "Succeeded" || item.status === "posted"
                  ? "text-emerald-500"
                  : item.status === "Failed"
                    ? "text-red-500"
                    : "text-amber-500";
              return (
                <div key={item.id} className="p-3 transition-colors hover:bg-gray-50 dark:hover:bg-gray-900/50 md:p-4">
                  <div className="flex items-center justify-between gap-2">
                    <div className="flex min-w-0 items-center gap-2 md:gap-3">
                      <div className={`flex size-8 shrink-0 items-center justify-center rounded-xl md:size-10 ${item.isCredit ? "bg-emerald-500/10" : "bg-red-500/10"}`}>
                        {item.isCredit ? <TrendingUp className="size-4 text-emerald-500 md:size-5" /> : <TrendingDown className="size-4 text-red-500 md:size-5" />}
                      </div>
                      <div className="min-w-0">
                        <p className="truncate text-xs font-medium text-gray-900 dark:text-white md:text-sm">{item.label}</p>
                        <div className="mt-0.5 flex items-center gap-1 md:gap-2">
                          <span className="text-[10px] text-gray-500 md:text-xs">{formatPostedDate(item.date)}</span>
                          <span className={`flex items-center gap-0.5 text-[9px] font-medium md:text-[10px] ${statusColor}`}>
                            <StatusIcon className="size-2.5 md:size-3" />
                            {statusLabel}
                          </span>
                        </div>
                      </div>
                    </div>
                    <span className={`shrink-0 text-[11px] font-bold md:text-sm ${item.isCredit ? "text-emerald-500" : "text-red-500"}`}>
                      {item.isCredit ? "+" : "-"}
                      {toPersianDigits(new Intl.NumberFormat("fa-IR").format(item.amount))} ریال
                    </span>
                  </div>
                </div>
              );
            })
          )}
        </div>
      </div>

      {showWithdraw ? (
        <div className="fixed inset-0 z-[9999] flex items-center justify-center bg-black/60 p-4 backdrop-blur-sm">
          <div className="w-full max-w-md rounded-2xl border border-gray-200 bg-white p-6 shadow-xl dark:border-gray-800 dark:bg-[#111]" onClick={(e) => e.stopPropagation()}>
            <div className="mb-4 flex items-center justify-between">
              <div className="flex items-center gap-2">
                <div className="flex size-10 items-center justify-center rounded-xl bg-red-500/10">
                  <Minus className="size-5 text-red-500" />
                </div>
                <h3 className="text-lg font-bold text-gray-900 dark:text-white">درخواست برداشت</h3>
              </div>
              <button type="button" onClick={() => setShowWithdraw(false)} className="rounded-lg p-2 hover:bg-gray-100 dark:hover:bg-gray-800">
                <X className="size-5 text-gray-500" />
              </button>
            </div>
            <div className="space-y-4">
              <div className="rounded-xl border border-gray-200 bg-gray-50 p-3 dark:border-gray-700 dark:bg-gray-900/50">
                <p className="text-xs text-gray-500">موجودی قابل برداشت</p>
                <p className="text-lg font-bold" style={{ color: ACCENT }}>
                  {formatSettlementMoney(available)}
                </p>
              </div>
              <div>
                <label className="text-sm font-medium text-gray-700 dark:text-gray-300">مبلغ (ریال)</label>
                <input
                  type="text"
                  value={amount}
                  onChange={(e) => setAmount(e.target.value.replace(/\D/g, ""))}
                  placeholder="مبلغ را وارد کنید"
                  className="mt-1 w-full rounded-xl border border-gray-200 bg-gray-50 px-4 py-2.5 text-sm text-gray-900 focus:outline-none focus:ring-2 focus:ring-[#2563EB] dark:border-gray-700 dark:bg-gray-800 dark:text-white"
                />
              </div>
              <button
                type="button"
                disabled={submitting}
                onClick={() => void handleWithdraw()}
                className="w-full rounded-xl py-3 text-sm font-bold text-white shadow-lg transition-colors hover:opacity-90 disabled:opacity-60"
                style={{ backgroundColor: ACCENT, boxShadow: "0 10px 25px rgba(37,99,235,0.3)" }}
              >
                {submitting ? "در حال ثبت…" : "ثبت درخواست برداشت"}
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}

function Stat({
  icon,
  label,
  value,
  tone,
  accent,
}: {
  icon: ReactNode;
  label: string;
  value: number;
  tone: string;
  accent?: boolean;
}) {
  return (
    <div className="rounded-2xl border border-gray-200 bg-white p-4 text-center transition-all hover:shadow-lg dark:border-gray-800 dark:bg-[#111]">
      <div className={`mx-auto mb-1 flex size-8 items-center justify-center rounded-full ${accent ? "bg-[#2563EB]/10" : tone.replace("text-", "bg-") + "/10"}`}>{icon}</div>
      <p className="whitespace-nowrap text-xs text-gray-500">{label}</p>
      <p className={`truncate text-base font-black md:text-lg ${accent ? "text-[#2563EB]" : tone}`}>
        {toPersianDigits(new Intl.NumberFormat("fa-IR").format(value))}
      </p>
    </div>
  );
}

/** صفحهٔ کیف پول با seller party از query/localStorage. */
export function VendorWalletPageClient() {
  const [sellerPartyId, setSellerPartyId] = useState("");
  useEffect(() => {
    setSellerPartyId(readSellerPartyId(window.location.search) ?? "");
  }, []);
  if (!sellerPartyId) return <p className="text-muted">فروشنده انتخاب نشده است.</p>;
  return <VendorWalletUi sellerPartyId={sellerPartyId} />;
}
