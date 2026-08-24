"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { ErrorState, faWorkspaceMessages } from "../../../../design-system";
import {
  formatMoney,
  formatPaymentState,
  formatUnits,
  loadSellerOrderDetail,
  readSellerPartyId,
  type HostReadSource,
  type SellerOrderDetail,
} from "../../seller-api";

/**
 * جزئیات سفارش فروشنده بدون نشت خطوط فروشندهٔ دیگر.
 */
export default function VendorOrderDetailPage() {
  const params = useParams<{ sellerOrderId: string }>();
  const sellerOrderId = params.sellerOrderId;
  const [source, setSource] = useState<HostReadSource | "loading">("loading");
  const [detail, setDetail] = useState<SellerOrderDetail | null>(null);
  const [message, setMessage] = useState<string | undefined>(undefined);
  const [denied, setDenied] = useState(false);

  function refresh() {
    const sellerPartyId = readSellerPartyId(window.location.search);
    if (!sellerPartyId) {
      setSource("error");
      setMessage("seller.identity.missing");
      return;
    }
    void loadSellerOrderDetail(sellerPartyId, sellerOrderId).then((result) => {
      setSource(result.source);
      setDetail(result.detail);
      setMessage(result.message);
      setDenied(Boolean(result.denied));
    });
  }

  useEffect(() => {
    const sellerPartyId = readSellerPartyId(window.location.search);
    if (!sellerPartyId) {
      setSource("error");
      setMessage("seller.identity.missing");
      return;
    }
    void loadSellerOrderDetail(sellerPartyId, sellerOrderId).then((result) => {
      setSource(result.source);
      setDetail(result.detail);
      setMessage(result.message);
      setDenied(Boolean(result.denied));
    });
  }, [sellerOrderId]);

  if (denied) {
    return (
      <main data-testid="seller-auth-denied">
        <ErrorState
          title="دسترسی مجاز نیست"
          detail="این سفارش متعلق به فروشندهٔ دیگری است یا پیدا نشد."
          onRetry={refresh}
          retryLabel={faWorkspaceMessages.retry}
        />
        <Link className="mt-4 inline-flex text-primary underline" href="/vendor-panel/orders">
          بازگشت به فهرست
        </Link>
      </main>
    );
  }

  return (
    <main>
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
        <div>
          <p className="text-sm text-muted">خانه / سفارش‌ها / جزئیات</p>
          <h1 className="mt-1 text-2xl font-semibold tracking-tight">جزئیات سفارش</h1>
          <p className="mt-1 text-base text-muted">{detail?.orderNumber ?? "…"}</p>
        </div>
        <Link className="text-sm text-primary underline-offset-4 hover:underline" href="/vendor-panel/orders">
          بازگشت
        </Link>
      </div>
      {source === "error" ? (
        <ErrorState title="Host در دسترس نیست" detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
      ) : detail ? (
        <div className="grid max-w-4xl gap-6">
          <section className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm">
            <dl className="grid gap-3 text-sm sm:grid-cols-3">
              <div>
                <dt className="text-muted">وضعیت</dt>
                <dd className="mt-1 font-medium">{formatPaymentState(detail.status)}</dd>
              </div>
              <div>
                <dt className="text-muted">پرداخت</dt>
                <dd className="mt-1 font-medium">{formatPaymentState(detail.paymentState)}</dd>
              </div>
              <div>
                <dt className="text-muted">تاریخ</dt>
                <dd className="mt-1 tabular-nums font-medium">{detail.submittedAt.slice(0, 10)}</dd>
              </div>
              <div>
                <dt className="text-muted">گیرنده</dt>
                <dd className="mt-1 font-medium">{detail.recipientName}</dd>
              </div>
              <div>
                <dt className="text-muted">موبایل</dt>
                <dd className="mt-1 font-medium" dir="ltr">
                  {detail.contactMobile || "—"}
                </dd>
              </div>
              <div>
                <dt className="text-muted">ارسال</dt>
                <dd className="mt-1 font-medium">{detail.shippingMethodLabel || "—"}</dd>
              </div>
            </dl>
            <p className="mt-4 text-sm text-muted">
              {detail.provinceName} {detail.cityName} — {detail.postalAddress} {detail.postalCode}
            </p>
          </section>
          <section className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm">
            <h2 className="text-base font-semibold">خطوط این فروشنده</h2>
            <ul className="mt-4 divide-y divide-border">
              {detail.lines.map((line) => (
                <li key={`${line.offerId}-${line.title}`} className="flex flex-wrap items-center justify-between gap-3 py-3 text-sm">
                  <div className="min-w-0">
                    <p className="font-medium">{line.title}</p>
                    <p className="text-muted">
                      {formatUnits(line.quantity)} × {formatMoney(line.unitAmount, line.currency)}
                    </p>
                  </div>
                  <p className="tabular-nums font-semibold">{formatMoney(line.linePayable, line.currency)}</p>
                </li>
              ))}
            </ul>
            <dl className="mt-4 grid gap-2 border-t border-border pt-4 text-sm sm:grid-cols-2">
              <div className="flex justify-between gap-3">
                <dt className="text-muted">جمع</dt>
                <dd className="tabular-nums">{formatMoney(detail.subtotal, detail.currency)}</dd>
              </div>
              <div className="flex justify-between gap-3">
                <dt className="text-muted">مالیات</dt>
                <dd className="tabular-nums">{formatMoney(detail.taxAmount, detail.currency)}</dd>
              </div>
              <div className="flex justify-between gap-3">
                <dt className="text-muted">تخفیف</dt>
                <dd className="tabular-nums">{formatMoney(detail.discountAmount, detail.currency)}</dd>
              </div>
              <div className="flex justify-between gap-3 font-semibold">
                <dt>قابل‌پرداخت</dt>
                <dd className="tabular-nums">{formatMoney(detail.payableAmount, detail.currency)}</dd>
              </div>
            </dl>
          </section>
        </div>
      ) : (
        <p className="text-muted">در حال بارگذاری…</p>
      )}
    </main>
  );
}
