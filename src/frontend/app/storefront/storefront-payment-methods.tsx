"use client";

/**
 * روش پرداخت Shopeiva-locked — هندسه PaymentMethods.jsx بدون کارت جعلی/شارژ.
 * کیف پول فقط وقتی Host canPayFullyWithWallet بدهد؛ mixed tender ادعا نمی‌شود.
 */

import { CheckCircle, CreditCard, ShieldCheck, Wallet } from "lucide-react";
import { formatOfferAmount } from "./storefront-api.ts";
import type { StorefrontPaymentMethodId, StorefrontWalletQuote } from "./storefront-payment-api.ts";

const ACCENT = "#2563EB";

export function StorefrontPaymentMethodPicker({
  selected,
  onChange,
  quote,
  showMixedDeferred = true,
}: {
  selected: StorefrontPaymentMethodId;
  onChange: (id: StorefrontPaymentMethodId) => void;
  quote: StorefrontWalletQuote | null;
  /** اگر mixed LIVE نیست، نشان «به‌زودی» یا پنهان — پیش‌فرض نشان deferred. */
  showMixedDeferred?: boolean;
}) {
  const walletEligible = Boolean(quote?.canPayFullyWithWallet);
  const mixedLive = Boolean(quote?.mixedTenderAvailable);

  return (
    <section
      className="bg-white border border-gray-200 rounded-2xl p-4 md:p-5 shadow-sm space-y-3"
      data-testid="payment-method-picker"
      dir="rtl"
    >
      <div className="flex items-center gap-2.5 mb-1">
        <div
          className="w-9 h-9 rounded-lg flex items-center justify-center"
          style={{ backgroundColor: `${ACCENT}1A` }}
        >
          <CreditCard className="w-4 h-4" style={{ color: ACCENT }} />
        </div>
        <h2 className="text-base md:text-lg font-bold text-gray-900">روش پرداخت</h2>
      </div>

      <MethodRow
        id="gateway"
        selected={selected === "gateway"}
        onSelect={() => onChange("gateway")}
        title="درگاه بانکی"
        subtitle="پرداخت امن پس از ثبت سفارش — بدون کارت جعلی در صفحه"
        icon={ShieldCheck}
        iconClass="text-[#2563EB]"
        iconBg="bg-[#2563EB]/10"
        testId="payment-method-gateway"
      />

      {walletEligible ? (
        <MethodRow
          id="wallet"
          selected={selected === "wallet"}
          onSelect={() => onChange("wallet")}
          title="کیف پول"
          subtitle={
            quote
              ? `موجودی: ${formatOfferAmount(quote.balance, quote.currency)} · قابل استفاده: ${formatOfferAmount(quote.maxUsableAmount, quote.currency)}`
              : "پرداخت کامل از موجودی کیف پول"
          }
          icon={Wallet}
          iconClass="text-violet-500"
          iconBg="bg-violet-50"
          testId="payment-method-wallet"
        />
      ) : null}

      {selected === "wallet" && quote ? (
        <div
          className="mt-1 p-4 rounded-xl bg-violet-50 border border-violet-200 space-y-1.5"
          data-testid="payment-wallet-summary"
        >
          <p className="text-xs font-bold text-violet-700">پرداخت کامل با کیف پول</p>
          <p className="text-[10px] md:text-xs text-gray-600">
            موجودی: <span className="font-bold">{formatOfferAmount(quote.balance, quote.currency)}</span>
          </p>
          <p className="text-[10px] md:text-xs text-gray-600">
            مبلغ قابل استفاده:{" "}
            <span className="font-bold">{formatOfferAmount(quote.maxUsableAmount, quote.currency)}</span>
          </p>
          <p className="text-[10px] md:text-xs text-gray-600">
            باقی‌مانده قابل پرداخت:{" "}
            <span className="font-bold text-violet-700">
              {formatOfferAmount(quote.remainingPayable, quote.currency)}
            </span>
            {quote.remainingPayable === 0 ? " (صفر — بدون درگاه)" : null}
          </p>
        </div>
      ) : null}

      {!mixedLive && showMixedDeferred ? (
        <div
          className="rounded-xl border border-dashed border-gray-200 bg-gray-50 px-3 py-2.5"
          data-testid="payment-mixed-deferred"
        >
          <p className="text-[10px] md:text-xs text-gray-500">
            پرداخت ترکیبی کیف پول + درگاه فعلاً فعال نیست (DEFERRED) و در این صفحه ادعا نمی‌شود.
          </p>
        </div>
      ) : null}

      {!walletEligible && quote && quote.balance > 0 && !quote.canPayFullyWithWallet ? (
        <p className="text-[10px] text-amber-700 bg-amber-50 border border-amber-100 rounded-xl px-3 py-2" data-testid="payment-wallet-insufficient">
          موجودی کیف پول برای پوشش کامل مبلغ کافی نیست. فقط درگاه بانکی فعال است.
        </p>
      ) : null}
    </section>
  );
}

function MethodRow({
  id,
  selected,
  onSelect,
  title,
  subtitle,
  icon: Icon,
  iconClass,
  iconBg,
  testId,
}: {
  id: string;
  selected: boolean;
  onSelect: () => void;
  title: string;
  subtitle: string;
  icon: typeof CreditCard;
  iconClass: string;
  iconBg: string;
  testId: string;
}) {
  return (
    <button
      type="button"
      onClick={onSelect}
      data-testid={testId}
      data-method={id}
      className={`w-full flex items-center gap-3 md:gap-4 p-3 md:p-4 rounded-xl border-2 transition-all text-right ${
        selected
          ? "border-[#2563EB] bg-[#2563EB]/5 shadow-sm shadow-[#2563EB]/10"
          : "border-gray-200 hover:border-gray-300"
      }`}
    >
      <div className={`w-11 h-11 md:w-12 md:h-12 rounded-xl ${iconBg} flex items-center justify-center shrink-0`}>
        <Icon className={`w-5 h-5 md:w-6 md:h-6 ${iconClass}`} />
      </div>
      <div className="flex-1 min-w-0">
        <p className="text-sm md:text-base font-bold text-gray-900">{title}</p>
        <p className="text-[10px] md:text-xs text-gray-500 truncate">{subtitle}</p>
      </div>
      <div
        className={`shrink-0 w-4 h-4 md:w-5 md:h-5 rounded-full border-2 flex items-center justify-center transition-all ${
          selected ? "border-[#2563EB] bg-[#2563EB]" : "border-gray-300"
        }`}
      >
        {selected ? <CheckCircle className="w-3 h-3 md:w-3.5 md:h-3.5 text-white" /> : null}
      </div>
    </button>
  );
}
