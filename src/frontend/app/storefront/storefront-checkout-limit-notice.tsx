"use client";

import { LocalizedLink as Link } from "../../lib/i18n/LocalizedLink.tsx";

export function StorefrontCheckoutLimitNotice({
  errorCode,
  message,
}: {
  errorCode?: string | null;
  message: string;
}) {
  const openUnpaid = errorCode === "checkout.open_unpaid_limit_reached";
  const churn = errorCode === "checkout.reservation_commit_limit_reached";
  return (
    <div className="space-y-2" data-testid="checkout-limit-notice" role="alert">
      <p className="text-sm text-red-600">{message}</p>
      {openUnpaid ? (
        <div className="flex flex-wrap gap-3 text-sm">
          <a href="#pending-payments" className="font-bold text-primary" data-testid="checkout-limit-pending-link">
            مشاهده سفارش‌های در انتظار پرداخت
          </a>
          <Link href="/customer-panel/orders" className="font-bold text-primary" data-testid="checkout-limit-orders-link">
            سفارش‌های من
          </Link>
        </div>
      ) : null}
      {churn ? (
        <p className="text-xs text-gray-500">موجودی رزرو نشده است. سبد خرید شما باقی مانده است.</p>
      ) : null}
    </div>
  );
}
