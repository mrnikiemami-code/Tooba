"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import {
  CheckCircle,
  FileText,
  Gift,
  Plus,
  TrendingDown,
  TrendingUp,
  Wallet,
} from "lucide-react";
import { toast } from "react-toastify";
import {
  adjustAdminWallet,
  createWalletIdempotencyKey,
  formatGiftCardStatus,
  formatLedgerDate,
  formatLedgerEntryLabel,
  formatWalletMoney,
  isCreditDirection,
  issueAdminGiftCard,
  loadAdminGiftCard,
  loadAdminGiftCards,
  loadAdminWallet,
  loadAdminWalletLedger,
  loadCustomerLedger,
  loadCustomerWallet,
  loadWalletDemoPreview,
  redeemCustomerGiftCard,
  revokeAdminGiftCard,
  toPersianDigits,
  type GiftCardDetail,
  type GiftCardIssueResult,
  type GiftCardSummary,
  type WalletLedgerEntry,
  type WalletLedgerPage,
  type WalletSummary,
} from "./wallet-api.ts";

const ACCENT = "#E53935";
const ACCENT_DARK = "#c62828";

function redeemErrorMessage(code: string): string {
  if (code.includes("expired") || code.includes("Expired")) return "کارت هدیه منقضی شده است";
  if (code.includes("revoked") || code.includes("Revoked") || code.includes("disabled")) {
    return "کارت هدیه باطل شده است";
  }
  if (code.includes("invalid") || code.includes("notfound") || code.includes("NotFound")) {
    return "کد کارت هدیه نامعتبر است";
  }
  if (code.includes("currency")) return "ارز کارت با کیف پول هم‌خوان نیست";
  if (code === "host-unreachable") return "ارتباط با سرور برقرار نشد";
  return "بازخرید کارت هدیه ناموفق بود";
}

/** فرم بازخرید — هندسهٔ Shopeiva userGiftCards/giftCardRedeem. */
export function GiftCardRedeemForm({
  onSuccess,
}: {
  onSuccess?: (message: string) => void;
}) {
  const [code, setCode] = useState("");
  const [busy, setBusy] = useState(false);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function onRedeem() {
    if (code.trim().length < 4) {
      setError("کد نامعتبر");
      toast.error("کد نامعتبر");
      return;
    }
    setBusy(true);
    setError(null);
    setFeedback(null);
    const result = await redeemCustomerGiftCard(code);
    setBusy(false);
    if (!result.ok) {
      const msg = redeemErrorMessage(result.errorCode);
      setError(msg);
      toast.error(msg);
      return;
    }
    const msg = result.result.idempotentReplay
      ? "این کد قبلاً ثبت شده بود (بدون اعتبار مضاعف)"
      : `کارت هدیه با موفقیت به کیف پول اضافه شد (+${formatWalletMoney(result.result.amount)} تومان)`;
    setFeedback(msg);
    toast.success(msg);
    setCode("");
    onSuccess?.(msg);
  }

  return (
    <div
      className="bg-white rounded-2xl border border-gray-200 p-4"
      data-testid="wallet-gift-redeem"
    >
      <p className="text-xs font-bold text-gray-900 mb-3 flex items-center gap-1">
        <Plus className="w-3.5 h-3.5 text-emerald-500" /> اضافه کردن کارت هدیه
      </p>
      <div className="flex gap-2">
        <input
          type="text"
          value={code}
          onChange={(e) => setCode(e.target.value)}
          placeholder="کد کارت هدیه را وارد کنید"
          disabled={busy}
          className="flex-1 px-3 py-2.5 bg-gray-50 rounded-xl text-xs border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#E53935] text-gray-900 placeholder-gray-400"
          data-testid="wallet-gift-redeem-input"
        />
        <button
          type="button"
          onClick={() => void onRedeem()}
          disabled={busy}
          className="px-4 py-2.5 bg-emerald-500 text-white rounded-xl text-xs font-bold hover:bg-emerald-600 transition-all shadow-lg shadow-emerald-500/30 disabled:opacity-60"
          data-testid="wallet-gift-redeem-submit"
        >
          {busy ? "…" : "ثبت"}
        </button>
      </div>
      {error ? (
        <p className="mt-2 text-[11px] text-red-600" data-testid="wallet-gift-redeem-error">
          {error}
        </p>
      ) : null}
      {feedback ? (
        <p className="mt-2 text-[11px] text-emerald-600" data-testid="wallet-gift-redeem-success">
          {feedback}
        </p>
      ) : null}
    </div>
  );
}

function BalanceHero({
  balance,
  subtitle = "موجودی کیف پول",
}: {
  balance: number;
  subtitle?: string;
}) {
  return (
    <div
      className="relative bg-gradient-to-br from-[#E53935] to-[#c62828] rounded-2xl p-6 md:p-8 overflow-hidden"
      data-testid="wallet-balance-hero"
    >
      <div className="absolute inset-0 opacity-10">
        <div className="absolute top-0 right-0 w-64 h-64 bg-white rounded-full blur-3xl -translate-y-1/2 translate-x-1/3" />
        <div className="absolute bottom-0 left-0 w-48 h-48 bg-white rounded-full blur-3xl translate-y-1/2 -translate-x-1/3" />
      </div>
      <div className="relative z-10">
        <div className="flex items-center gap-2 text-white/80 mb-4">
          <Wallet className="w-5 h-5" />
          <span className="text-sm font-medium">{subtitle}</span>
        </div>
        <p className="text-3xl md:text-4xl font-black text-white">
          {formatWalletMoney(balance)}
          <span className="text-lg font-medium text-white/70 mr-1">تومان</span>
        </p>
      </div>
    </div>
  );
}

function StatsRow({ summary }: { summary: WalletSummary }) {
  return (
    <div className="grid grid-cols-3 gap-3" data-testid="wallet-stats">
      <div className="bg-white rounded-2xl p-4 border border-gray-200 text-center">
        <p className="text-xs text-gray-500 whitespace-nowrap">کل واریز</p>
        <p className="text-base md:text-lg font-black text-emerald-500 truncate">
          {formatWalletMoney(summary.totalCredits)}
        </p>
      </div>
      <div className="bg-white rounded-2xl p-4 border border-gray-200 text-center">
        <p className="text-xs text-gray-500 whitespace-nowrap">کل برداشت</p>
        <p className="text-base md:text-lg font-black text-red-500 truncate">
          {formatWalletMoney(summary.totalDebits)}
        </p>
      </div>
      <div className="bg-white rounded-2xl p-4 border border-gray-200 text-center">
        <p className="text-xs text-gray-500 whitespace-nowrap">تعداد تراکنش</p>
        <p className="text-base md:text-lg font-black text-[#E53935] truncate">
          {toPersianDigits(summary.entryCount)}
        </p>
      </div>
    </div>
  );
}

function LedgerList({ entries, emptyHint }: { entries: WalletLedgerEntry[]; emptyHint: string }) {
  if (entries.length === 0) {
    return (
      <div
        className="bg-white rounded-2xl border border-gray-200 p-8 text-center text-sm text-gray-500"
        data-testid="wallet-ledger-empty"
      >
        {emptyHint}
      </div>
    );
  }

  return (
    <div
      className="bg-white rounded-2xl border border-gray-200 overflow-hidden"
      data-testid="wallet-ledger-list"
    >
      <div className="p-4 border-b border-gray-200">
        <h3 className="font-bold text-gray-900 flex items-center gap-2">
          <FileText className="w-5 h-5 text-[#E53935]" />
          تاریخچه تراکنش‌ها
        </h3>
      </div>
      <div className="divide-y divide-gray-100">
        {entries.map((item) => {
          const credit = isCreditDirection(item.direction);
          return (
            <div
              key={item.entryId}
              className="p-3 md:p-4 hover:bg-gray-50 transition-colors"
              data-testid="wallet-ledger-row"
            >
              <div className="flex items-center justify-between gap-2">
                <div className="flex items-center gap-2 md:gap-3 min-w-0">
                  <div
                    className={`w-8 h-8 md:w-10 md:h-10 rounded-xl ${credit ? "bg-emerald-500/10" : "bg-red-500/10"} flex items-center justify-center shrink-0`}
                  >
                    {credit ? (
                      <TrendingUp className="w-4 h-4 md:w-5 md:h-5 text-emerald-500" />
                    ) : (
                      <TrendingDown className="w-4 h-4 md:w-5 md:h-5 text-red-500" />
                    )}
                  </div>
                  <div className="min-w-0">
                    <p className="text-xs md:text-sm font-medium text-gray-900 truncate">
                      {formatLedgerEntryLabel(item)}
                    </p>
                    <div className="flex items-center gap-1 md:gap-2 mt-0.5">
                      <span className="text-[10px] md:text-xs text-gray-500">
                        {formatLedgerDate(item.createdAt)}
                      </span>
                      <span className="text-[9px] md:text-[10px] font-medium text-emerald-500 flex items-center gap-0.5">
                        <CheckCircle className="w-2.5 h-2.5 md:w-3 md:h-3" />
                        ثبت‌شده
                      </span>
                    </div>
                  </div>
                </div>
                <span
                  className={`text-[11px] md:text-sm font-bold shrink-0 ${credit ? "text-emerald-500" : "text-red-500"}`}
                >
                  {credit ? "+" : "-"}
                  {formatWalletMoney(item.amount)} تومان
                </span>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

/**
 * پنل کیف پول مشتری — Shopeiva wallet بدون شارژ/برداشت/کارت بانکی.
 */
export function CustomerWalletPanel() {
  const [summary, setSummary] = useState<WalletSummary | null>(null);
  const [ledger, setLedger] = useState<WalletLedgerPage | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(() => {
    setLoading(true);
    setError(null);
    void Promise.all([loadCustomerWallet(), loadCustomerLedger({ page: 1, pageSize: 50 })]).then(
      ([wallet, page]) => {
        setLoading(false);
        if (!wallet || !page) {
          setError("خواندن کیف پول از Host ناموفق بود");
          setSummary(null);
          setLedger(null);
          return;
        }
        setSummary(wallet);
        setLedger(page);
      },
    );
  }, []);

  useEffect(refresh, [refresh]);

  if (loading) {
    return (
      <div className="space-y-4 animate-pulse" data-testid="wallet-loading" dir="rtl">
        <div className="h-40 rounded-2xl bg-red-100" />
        <div className="grid grid-cols-3 gap-3">
          <div className="h-20 rounded-2xl bg-gray-100" />
          <div className="h-20 rounded-2xl bg-gray-100" />
          <div className="h-20 rounded-2xl bg-gray-100" />
        </div>
        <div className="h-48 rounded-2xl bg-gray-100" />
      </div>
    );
  }

  if (error || !summary) {
    return (
      <div
        className="rounded-2xl border border-red-200 bg-red-50 p-6 text-sm text-red-700 space-y-3"
        data-testid="wallet-error"
        dir="rtl"
      >
        <p>{error ?? "کیف پول در دسترس نیست"}</p>
        <button
          type="button"
          onClick={refresh}
          className="px-4 py-2 rounded-xl bg-white border border-red-200 text-red-700 text-xs font-bold"
        >
          تلاش مجدد
        </button>
      </div>
    );
  }

  return (
    <div className="space-y-6" dir="rtl" data-testid="customer-wallet-live">
      <BalanceHero balance={summary.balance} />
      <StatsRow summary={summary} />
      <LedgerList
        entries={ledger?.items ?? []}
        emptyHint="هنوز تراکنشی در دفتر کیف پول ثبت نشده است"
      />
      <p className="text-[11px] text-gray-400 text-center">
        شارژ مستقیم و کارت بانکی پشتیبانی نمی‌شود — فقط اعتبار واقعی دفتر (کارت هدیه / تعدیل مجاز).
        {" · "}
        <Link href="/customer-panel/gift-cards" className="text-[#E53935] font-bold hover:underline">
          بازخرید کارت هدیه
        </Link>
      </p>
    </div>
  );
}

/**
 * صفحهٔ کارت هدیه مشتری — موجودی کیف + فرم بازخرید؛ بدون لیست جعلی کارت خریداری‌شده.
 */
export function CustomerGiftCardsPanel() {
  const [summary, setSummary] = useState<WalletSummary | null>(null);
  const [giftCredits, setGiftCredits] = useState<WalletLedgerEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(() => {
    setLoading(true);
    setError(null);
    void Promise.all([loadCustomerWallet(), loadCustomerLedger({ page: 1, pageSize: 50 })]).then(
      ([wallet, page]) => {
        setLoading(false);
        if (!wallet) {
          setError("خواندن کیف پول ناموفق بود");
          setSummary(null);
          setGiftCredits([]);
          return;
        }
        setSummary(wallet);
        setGiftCredits(
          (page?.items ?? []).filter(
            (e) => e.type === "GiftCardCredit" || e.sourceType === "GiftCard",
          ),
        );
      },
    );
  }, []);

  useEffect(refresh, [refresh]);

  if (loading) {
    return (
      <div className="space-y-4 animate-pulse" data-testid="gift-cards-loading" dir="rtl">
        <div className="h-28 rounded-2xl bg-red-100" />
        <div className="h-24 rounded-2xl bg-gray-100" />
      </div>
    );
  }

  if (error || !summary) {
    return (
      <div
        className="rounded-2xl border border-red-200 bg-red-50 p-6 text-sm text-red-700"
        data-testid="gift-cards-error"
        dir="rtl"
      >
        {error ?? "کارت هدیه در دسترس نیست"}
      </div>
    );
  }

  return (
    <div className="space-y-5" dir="rtl" data-testid="customer-gift-cards-live">
      <div className="flex items-center justify-between">
        <h1 className="text-lg md:text-xl font-extrabold text-gray-900 flex items-center gap-2">
          <Gift className="w-5 h-5 text-[#E53935]" /> کارت‌های هدیه
        </h1>
        <Link
          href="/customer-panel/wallet"
          className="text-[10px] text-[#E53935] font-bold hover:underline"
        >
          مشاهده کیف پول
        </Link>
      </div>

      <div className="bg-gradient-to-r from-[#E53935] to-red-600 rounded-2xl p-5 text-white relative overflow-hidden">
        <div className="absolute inset-0 bg-[url('data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNDAiIGhlaWdodD0iNDAiIHZpZXdCb3g9IjAgMCA0MCA0MCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48cGF0aCBkPSJNMjAgMzBhMTAgMTAgMCAxMTAtMjAgMTAgMTAgMCAwMTAgMjB6IiBmaWxsPSIjZmZmIiBmaWxsLW9wYWNpdHk9Ii4wNSIvPjwvc3ZnPg==')]" />
        <div className="relative">
          <div className="flex justify-between items-start">
            <div>
              <p className="text-[10px] opacity-70">موجودی قابل استفاده در کیف پول</p>
              <p className="text-2xl font-black mt-1">{formatWalletMoney(summary.balance)}</p>
              <p className="text-[9px] opacity-60">تومان</p>
            </div>
            <Gift className="w-8 h-8 opacity-80" />
          </div>
        </div>
      </div>

      <GiftCardRedeemForm onSuccess={() => refresh()} />

      <div className="space-y-3" data-testid="gift-card-credit-history">
        <h2 className="text-sm font-bold text-gray-900">اعتبارهای ثبت‌شده از کارت هدیه</h2>
        {giftCredits.length === 0 ? (
          <p className="text-xs text-gray-500 bg-white rounded-2xl border border-gray-200 p-4">
            هنوز کارت هدیه‌ای بازخرید نشده است.
          </p>
        ) : (
          giftCredits.map((entry) => (
            <div
              key={entry.entryId}
              className="bg-white rounded-2xl border border-gray-200 p-4 flex items-center justify-between"
            >
              <div>
                <p className="text-sm font-medium text-gray-900">{formatLedgerEntryLabel(entry)}</p>
                <p className="text-[10px] text-gray-500 mt-0.5">{formatLedgerDate(entry.createdAt)}</p>
              </div>
              <span className="text-sm font-bold text-emerald-500">
                +{formatWalletMoney(entry.amount)}
              </span>
            </div>
          ))
        )}
      </div>
    </div>
  );
}

/** فهرست + صدور Admin. */
export function AdminGiftCardsScreen() {
  const [rows, setRows] = useState<GiftCardSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [denied, setDenied] = useState(false);
  const [status, setStatus] = useState<string>("");
  const [qDraft, setQDraft] = useState("");
  const [q, setQ] = useState("");
  const [amount, setAmount] = useState("500000");
  const [issuing, setIssuing] = useState(false);
  const [issued, setIssued] = useState<GiftCardIssueResult | null>(null);
  const [demoNote, setDemoNote] = useState<string | null>(null);

  const refresh = useCallback(() => {
    setLoading(true);
    setError(null);
    setDenied(false);
    void loadAdminGiftCards({
      status: status || undefined,
      q: q || undefined,
      pageSize: 100,
    }).then((result) => {
      setLoading(false);
      if (result.state === "denied") {
        setDenied(true);
        setRows([]);
        return;
      }
      if (result.state !== "ok" || !result.data) {
        setError(result.message ?? "خطا در خواندن کارت‌ها");
        setRows([]);
        return;
      }
      setRows(result.data.items);
    });
  }, [status, q]);

  useEffect(refresh, [refresh]);

  useEffect(() => {
    void loadWalletDemoPreview().then((result) => {
      if (result.state === "ok" && result.data) {
        setDemoNote(
          `Demo unused: ${result.data.unusedGiftCardDemoCode} · balance ${formatWalletMoney(result.data.balance)}`,
        );
      }
    });
  }, []);

  async function onIssue() {
    const value = Number(amount.replace(/,/g, ""));
    if (!Number.isFinite(value) || value <= 0) {
      toast.error("مبلغ نامعتبر است");
      return;
    }
    setIssuing(true);
    setIssued(null);
    const result = await issueAdminGiftCard({
      initialAmount: value,
      idempotencyKey: createWalletIdempotencyKey(),
    });
    setIssuing(false);
    if (result.state === "denied") {
      toast.error("دسترسی صدور کارت مجاز نیست");
      return;
    }
    if (result.state !== "ok" || !result.data) {
      toast.error(result.message ?? "صدور ناموفق بود");
      return;
    }
    setIssued(result.data);
    toast.success("کارت هدیه صادر شد");
    refresh();
  }

  if (denied) {
    return (
      <main
        data-testid="admin-auth-denied"
        className="rounded-2xl border border-red-200 bg-red-50 p-6 text-sm text-red-700"
        dir="rtl"
      >
        دسترسی به کارت‌های هدیه مجاز نیست.
      </main>
    );
  }

  return (
    <div className="space-y-6" dir="rtl" data-testid="admin-gift-cards">
      <div className="flex items-center justify-between gap-3 flex-wrap">
        <h1 className="text-xl font-black text-gray-900 flex items-center gap-2">
          <Gift className="w-5 h-5" style={{ color: ACCENT }} />
          کارت‌های هدیه
        </h1>
        <Link
          href="/admin/wallets"
          className="text-xs font-bold text-[#2563EB] hover:underline"
        >
          بازرسی کیف پول مشتری
        </Link>
      </div>

      {demoNote ? (
        <p className="text-[11px] text-gray-500 bg-white border border-gray-200 rounded-xl px-3 py-2" data-testid="admin-wallet-demo-note">
          {demoNote}
        </p>
      ) : null}

      <div className="bg-white rounded-2xl border border-gray-200 p-4 space-y-3" data-testid="admin-gift-issue">
        <h2 className="text-sm font-bold text-gray-900">صدور کارت جدید</h2>
        <div className="flex flex-wrap gap-2">
          <input
            value={amount}
            onChange={(e) => setAmount(e.target.value.replace(/[^\d]/g, ""))}
            placeholder="مبلغ (تومان)"
            className="px-3 py-2.5 bg-gray-50 rounded-xl text-sm border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#E53935]"
          />
          <button
            type="button"
            disabled={issuing}
            onClick={() => void onIssue()}
            className="px-4 py-2.5 text-white rounded-xl text-sm font-bold hover:opacity-90 disabled:opacity-60"
            style={{ backgroundColor: ACCENT }}
          >
            {issuing ? "در حال صدور…" : "صدور"}
          </button>
        </div>
        {issued ? (
          <div className="rounded-xl bg-emerald-50 border border-emerald-200 p-3 text-sm" data-testid="admin-gift-issue-code">
            <p className="font-bold text-emerald-800">کد نمایشی (فقط یک‌بار):</p>
            <p className="font-mono text-emerald-900 mt-1 break-all">{issued.displayCode}</p>
            <p className="text-[11px] text-emerald-700 mt-1">
              CardId: {issued.card.cardId}
              {issued.idempotentReplay ? " · replay" : ""}
            </p>
          </div>
        ) : null}
      </div>

      <form
        className="flex gap-2 flex-wrap"
        onSubmit={(e) => {
          e.preventDefault();
          setQ(qDraft.trim());
        }}
      >
        <input
          value={qDraft}
          onChange={(e) => setQDraft(e.target.value)}
          placeholder="جستجو (شناسه)…"
          className="flex-1 min-w-[200px] px-4 py-2.5 bg-white rounded-xl text-sm border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#E53935]"
        />
        <select
          value={status}
          onChange={(e) => setStatus(e.target.value)}
          className="px-3 py-2.5 bg-white rounded-xl text-sm border border-gray-200"
        >
          <option value="">همه وضعیت‌ها</option>
          <option value="Active">Active</option>
          <option value="PartiallyRedeemed">PartiallyRedeemed</option>
          <option value="Redeemed">Redeemed</option>
          <option value="Expired">Expired</option>
          <option value="Revoked">Revoked</option>
        </select>
        <button
          type="submit"
          className="px-4 py-2.5 text-white rounded-xl text-sm font-bold"
          style={{ backgroundColor: ACCENT }}
        >
          فیلتر
        </button>
      </form>

      {loading ? (
        <p className="text-sm text-gray-500">در حال بارگذاری…</p>
      ) : error ? (
        <div className="rounded-2xl border border-red-200 bg-red-50 p-4 text-sm text-red-700 space-y-2">
          <p>{error}</p>
          <button type="button" onClick={refresh} className="text-xs font-bold underline">
            تلاش مجدد
          </button>
        </div>
      ) : rows.length === 0 ? (
        <p className="text-sm text-gray-500 bg-white rounded-2xl border border-gray-200 p-6">
          کارتی یافت نشد.
        </p>
      ) : (
        <div className="bg-white rounded-2xl border border-gray-200 overflow-hidden">
          <table className="w-full text-sm" data-testid="admin-gift-cards-table">
            <thead className="bg-gray-50 text-gray-500 text-xs">
              <tr>
                <th className="text-right p-3 font-bold">شناسه</th>
                <th className="text-right p-3 font-bold">مبلغ اولیه</th>
                <th className="text-right p-3 font-bold">مانده</th>
                <th className="text-right p-3 font-bold">وضعیت</th>
                <th className="text-right p-3 font-bold">صدور</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {rows.map((row) => (
                <tr key={row.cardId} className="hover:bg-gray-50">
                  <td className="p-3">
                    <Link
                      href={`/admin/gift-cards/${row.cardId}`}
                      className="font-mono text-xs text-[#2563EB] hover:underline"
                    >
                      {row.cardId.slice(0, 8)}…
                    </Link>
                  </td>
                  <td className="p-3">{formatWalletMoney(row.initialAmount)}</td>
                  <td className="p-3">{formatWalletMoney(row.remainingAmount)}</td>
                  <td className="p-3">{formatGiftCardStatus(row.status)}</td>
                  <td className="p-3 text-xs text-gray-500">{formatLedgerDate(row.issuedAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

/** جزئیات + ابطال. */
export function AdminGiftCardDetailScreen({ cardId }: { cardId: string }) {
  const [detail, setDetail] = useState<GiftCardDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [denied, setDenied] = useState(false);
  const [revoking, setRevoking] = useState(false);

  const refresh = useCallback(() => {
    setLoading(true);
    setError(null);
    setDenied(false);
    void loadAdminGiftCard(cardId).then((result) => {
      setLoading(false);
      if (result.state === "denied") {
        setDenied(true);
        setDetail(null);
        return;
      }
      if (result.state !== "ok" || !result.data) {
        setError(result.message ?? "کارت یافت نشد");
        setDetail(null);
        return;
      }
      setDetail(result.data);
    });
  }, [cardId]);

  useEffect(refresh, [refresh]);

  async function onRevoke() {
    if (!detail || detail.status === "Revoked") return;
    if (!window.confirm("ابطال کارت هدیه قطعی است. ادامه؟")) return;
    setRevoking(true);
    const result = await revokeAdminGiftCard(cardId);
    setRevoking(false);
    if (result.state === "denied") {
      toast.error("دسترسی ابطال مجاز نیست");
      return;
    }
    if (result.state !== "ok" || !result.data) {
      toast.error(result.message ?? "ابطال ناموفق");
      return;
    }
    setDetail(result.data);
    toast.success("کارت باطل شد");
  }

  if (denied) {
    return (
      <main data-testid="admin-auth-denied" className="rounded-2xl border border-red-200 bg-red-50 p-6 text-sm text-red-700" dir="rtl">
        دسترسی مجاز نیست.
      </main>
    );
  }

  if (loading) {
    return <p className="text-sm text-gray-500" dir="rtl">در حال بارگذاری…</p>;
  }

  if (error || !detail) {
    return (
      <div className="rounded-2xl border border-red-200 bg-red-50 p-6 text-sm text-red-700 space-y-2" dir="rtl">
        <p>{error ?? "کارت یافت نشد"}</p>
        <Link href="/admin/gift-cards" className="text-xs font-bold underline">
          بازگشت به فهرست
        </Link>
      </div>
    );
  }

  const canRevoke = detail.status === "Active" || detail.status === "PartiallyRedeemed";

  return (
    <div className="space-y-6" dir="rtl" data-testid="admin-gift-card-detail">
      <div className="flex items-center justify-between gap-3 flex-wrap">
        <div>
          <Link href="/admin/gift-cards" className="text-xs text-[#2563EB] hover:underline">
            ← فهرست کارت‌ها
          </Link>
          <h1 className="text-xl font-black text-gray-900 mt-1">جزئیات کارت هدیه</h1>
          <p className="font-mono text-xs text-gray-500 mt-1">{detail.cardId}</p>
        </div>
        {canRevoke ? (
          <button
            type="button"
            disabled={revoking}
            onClick={() => void onRevoke()}
            className="px-4 py-2.5 rounded-xl text-sm font-bold text-white disabled:opacity-60"
            style={{ backgroundColor: ACCENT_DARK }}
            data-testid="admin-gift-revoke"
          >
            {revoking ? "…" : "ابطال کارت"}
          </button>
        ) : null}
      </div>

      <div className="grid md:grid-cols-2 gap-4">
        <div className="bg-white rounded-2xl border border-gray-200 p-4 space-y-2 text-sm">
          <p>
            <span className="text-gray-500">وضعیت: </span>
            <strong>{formatGiftCardStatus(detail.status)}</strong>
          </p>
          <p>
            <span className="text-gray-500">مبلغ اولیه: </span>
            {formatWalletMoney(detail.initialAmount)} تومان
          </p>
          <p>
            <span className="text-gray-500">مانده: </span>
            {formatWalletMoney(detail.remainingAmount)} تومان
          </p>
          <p>
            <span className="text-gray-500">صدور: </span>
            {formatLedgerDate(detail.issuedAt)}
          </p>
          <p>
            <span className="text-gray-500">انقضا: </span>
            {detail.expiresAt ? formatLedgerDate(detail.expiresAt) : "—"}
          </p>
        </div>
        <div className="bg-white rounded-2xl border border-gray-200 p-4 space-y-2 text-sm">
          <p className="font-bold text-gray-900 mb-2">تاریخچه بازخرید</p>
          {detail.redemptions.length === 0 ? (
            <p className="text-gray-500 text-xs">بازخریدی ثبت نشده</p>
          ) : (
            <ul className="space-y-2" data-testid="admin-gift-redemptions">
              {detail.redemptions.map((r) => (
                <li key={r.redemptionId} className="flex justify-between gap-2 text-xs border-b border-gray-100 pb-2">
                  <span className="font-mono text-gray-500">{r.redemptionId.slice(0, 8)}…</span>
                  <span className="text-emerald-600 font-bold">+{formatWalletMoney(r.amount)}</span>
                  <span className="text-gray-400">{formatLedgerDate(r.createdAt)}</span>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </div>
  );
}

/** بازرسی کیف پول مشتری برای Admin. */
export function AdminWalletInspectScreen() {
  const [actorDraft, setActorDraft] = useState("");
  const [actorId, setActorId] = useState("");
  const [summary, setSummary] = useState<WalletSummary | null>(null);
  const [ledger, setLedger] = useState<WalletLedgerPage | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [denied, setDenied] = useState(false);
  const [loading, setLoading] = useState(false);
  const [adjAmount, setAdjAmount] = useState("");
  const [adjReason, setAdjReason] = useState("");
  const [adjDirection, setAdjDirection] = useState<"Credit" | "Debit">("Credit");
  const [adjusting, setAdjusting] = useState(false);

  useEffect(() => {
    void loadWalletDemoPreview().then((result) => {
      if (result.state === "ok" && result.data?.customerActorUserId) {
        setActorDraft(result.data.customerActorUserId);
      }
    });
  }, []);

  const load = useCallback((id: string) => {
    if (!id.trim()) return;
    setLoading(true);
    setError(null);
    setDenied(false);
    void Promise.all([
      loadAdminWallet(id.trim()),
      loadAdminWalletLedger(id.trim(), { pageSize: 50 }),
    ]).then(([walletResult, ledgerResult]) => {
      setLoading(false);
      if (walletResult.state === "denied" || ledgerResult.state === "denied") {
        setDenied(true);
        setSummary(null);
        setLedger(null);
        return;
      }
      if (walletResult.state !== "ok" || !walletResult.data) {
        setError(walletResult.message ?? "کیف پول یافت نشد");
        setSummary(null);
        setLedger(null);
        return;
      }
      setSummary(walletResult.data);
      setLedger(ledgerResult.state === "ok" ? ledgerResult.data : null);
    });
  }, []);

  async function onAdjust() {
    if (!actorId || !summary) return;
    const amount = Number(adjAmount);
    if (!Number.isFinite(amount) || amount <= 0 || !adjReason.trim()) {
      toast.error("مبلغ و دلیل الزامی است");
      return;
    }
    setAdjusting(true);
    const result = await adjustAdminWallet(actorId, {
      amount,
      direction: adjDirection,
      reason: adjReason.trim(),
      idempotencyKey: createWalletIdempotencyKey(),
    });
    setAdjusting(false);
    if (result.state === "denied") {
      toast.error("دسترسی تعدیل مجاز نیست");
      return;
    }
    if (result.state !== "ok" || !result.data) {
      toast.error(result.message ?? "تعدیل ناموفق");
      return;
    }
    toast.success(`تعدیل ثبت شد · موجودی ${formatWalletMoney(result.data.balance)}`);
    setAdjAmount("");
    setAdjReason("");
    load(actorId);
  }

  if (denied) {
    return (
      <main data-testid="admin-auth-denied" className="rounded-2xl border border-red-200 bg-red-50 p-6 text-sm text-red-700" dir="rtl">
        دسترسی بازرسی کیف پول مجاز نیست.
      </main>
    );
  }

  return (
    <div className="space-y-6" dir="rtl" data-testid="admin-wallet-inspect">
      <h1 className="text-xl font-black text-gray-900 flex items-center gap-2">
        <Wallet className="w-5 h-5 text-[#2563EB]" />
        بازرسی کیف پول مشتری
      </h1>

      <form
        className="flex flex-wrap gap-2"
        onSubmit={(e) => {
          e.preventDefault();
          setActorId(actorDraft.trim());
          load(actorDraft.trim());
        }}
      >
        <input
          value={actorDraft}
          onChange={(e) => setActorDraft(e.target.value)}
          placeholder="Customer ActorUserId (GUID)"
          className="flex-1 min-w-[260px] px-4 py-2.5 bg-white rounded-xl text-sm border border-gray-200 font-mono focus:outline-none focus:ring-2 focus:ring-[#2563EB]"
          data-testid="admin-wallet-actor-input"
        />
        <button
          type="submit"
          className="px-4 py-2.5 bg-[#2563EB] text-white rounded-xl text-sm font-bold"
        >
          بارگذاری
        </button>
      </form>

      {loading ? <p className="text-sm text-gray-500">در حال بارگذاری…</p> : null}
      {error ? (
        <p className="text-sm text-red-600 bg-red-50 border border-red-200 rounded-xl p-3">{error}</p>
      ) : null}

      {summary ? (
        <>
          <BalanceHero balance={summary.balance} subtitle="موجودی مشتق‌شده از دفتر" />
          <StatsRow summary={summary} />
          <div className="bg-white rounded-2xl border border-gray-200 p-4 space-y-3" data-testid="admin-wallet-adjust">
            <h2 className="text-sm font-bold">تعدیل ممیزی‌شده (دفتر immutable)</h2>
            <div className="flex flex-wrap gap-2">
              <select
                value={adjDirection}
                onChange={(e) => setAdjDirection(e.target.value as "Credit" | "Debit")}
                className="px-3 py-2.5 rounded-xl border border-gray-200 text-sm bg-gray-50"
              >
                <option value="Credit">Credit</option>
                <option value="Debit">Debit</option>
              </select>
              <input
                value={adjAmount}
                onChange={(e) => setAdjAmount(e.target.value.replace(/[^\d.]/g, ""))}
                placeholder="مبلغ"
                className="px-3 py-2.5 rounded-xl border border-gray-200 text-sm bg-gray-50"
              />
              <input
                value={adjReason}
                onChange={(e) => setAdjReason(e.target.value)}
                placeholder="دلیل (الزامی)"
                className="flex-1 min-w-[160px] px-3 py-2.5 rounded-xl border border-gray-200 text-sm bg-gray-50"
              />
              <button
                type="button"
                disabled={adjusting}
                onClick={() => void onAdjust()}
                className="px-4 py-2.5 rounded-xl text-sm font-bold text-white disabled:opacity-60"
                style={{ backgroundColor: ACCENT }}
              >
                ثبت تعدیل
              </button>
            </div>
            <p className="text-[11px] text-gray-400">موجودی مستقیماً set نمی‌شود — فقط سطر دفتر.</p>
          </div>
          <LedgerList
            entries={ledger?.items ?? []}
            emptyHint="دفتری برای این مشتری ثبت نشده"
          />
        </>
      ) : null}
    </div>
  );
}
