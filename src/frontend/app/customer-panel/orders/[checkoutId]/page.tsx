"use client";

import Link from "next/link";
import { ChevronRight, MapPin, Package, RotateCcw, Store, Truck } from "lucide-react";
import { useParams } from "next/navigation";
import { useEffect, useMemo, useState } from "react";
import {
  type CustomerOrderDetailPage,
  customerStatusClasses,
  formatCustomerMoney,
  formatCustomerOrderStatus,
  loadCustomerOrderDetail,
  retryCustomerUnpaidOrder,
} from "../../customer-api";
import { loadCustomerFulfillments, type FulfillmentSnapshot } from "../../../fulfillment/fulfillment-api";
import { FulfillmentShippingInfoBlock } from "../../../fulfillment/fulfillment-ui";
import { loadCustomerReturnEligibility, type CustomerReturnEligibilitySnapshot } from "../../../returns/return-api";
import { hasUnresolvedCustomerReturnSubmission } from "../../../returns/return-submission";
import { ReturnRequestModal, type ReturnRequestSeller } from "../../../returns/return-request-modal";
import { loadStorefrontSession } from "../../../storefront/storefront-identity-api";

/**
 * جزئیات سفارش مشتری با خطوط هر فروشنده و snapshot ارسال واقعی.
 *
 * TB-278: a return request is offered per seller order from the OWNING Returns eligibility read — delivered lines that
 * are still inside their own return window and still have a lawful remainder — never from the aggregate fulfillment
 * status or the shipped quantity. One deliberate act may select lines from SEVERAL sellers.
 */
export default function CustomerOrderDetail() {
  const params = useParams<{ checkoutId: string }>();
  const [page, setPage] = useState<CustomerOrderDetailPage | null | undefined>(undefined);
  const [fulfillments, setFulfillments] = useState<FulfillmentSnapshot[] | null | undefined>(undefined);
  const [eligibility, setEligibility] = useState<Record<string, CustomerReturnEligibilitySnapshot | null> | null>(null);
  const [returnModalOpen, setReturnModalOpen] = useState(false);
  const [pendingReturn, setPendingReturn] = useState(false);
  /**
   * TB-279: the signed-in customer. The durable return act is scoped to this viewer, so another customer using the same
   * browser can never read, resume or clear this customer's possibly-sent act.
   */
  const [viewerId, setViewerId] = useState<string | null>(null);
  const [retryBusy, setRetryBusy] = useState(false);
  const [retryError, setRetryError] = useState<string | null>(null);

  useEffect(() => {
    void loadCustomerOrderDetail(params.checkoutId).then(setPage);
    void loadCustomerFulfillments(params.checkoutId).then((result) => {
      if (result == null) {
        setFulfillments(null);
        return;
      }
      setFulfillments(result.snapshots);
    });
  }, [params.checkoutId]);

  useEffect(() => {
    if (!page) return;
    let cancelled = false;
    setEligibility(null);
    void Promise.all(
      page.sellerOrders.map(async (seller) => {
        const result = await loadCustomerReturnEligibility(seller.sellerOrderId);
        return [seller.sellerOrderId, result.ok ? result.snapshot : null] as const;
      }),
    ).then((entries) => {
      if (!cancelled) setEligibility(Object.fromEntries(entries));
    });
    return () => {
      cancelled = true;
    };
  }, [page]);

  useEffect(() => {
    let cancelled = false;
    void loadStorefrontSession().then((session) => {
      if (!cancelled) setViewerId(session.authenticated ? session.userId : null);
    });
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    const storage = typeof window === "undefined" ? null : window.sessionStorage;
    setPendingReturn(hasUnresolvedCustomerReturnSubmission(storage, params.checkoutId, viewerId));
  }, [params.checkoutId, returnModalOpen, viewerId]);

  const fulfillmentBySeller = useMemo(() => {
    const map = new Map<string, FulfillmentSnapshot>();
    for (const snapshot of fulfillments ?? []) {
      map.set(snapshot.sellerOrderId, snapshot);
    }
    return map;
  }, [fulfillments]);

  const returnSellers = useMemo<ReturnRequestSeller[]>(() => {
    if (!page || !eligibility) return [];
    const rows: ReturnRequestSeller[] = [];
    for (const seller of page.sellerOrders) {
      const snapshot = eligibility[seller.sellerOrderId];
      if (!snapshot) continue;
      if (!snapshot.lines.some((line) => line.selectable && line.remainingReturnableQuantity > 0)) continue;
      rows.push({
        sellerOrderId: seller.sellerOrderId,
        sellerDisplayName: seller.sellerDisplayName,
        orderNumber: seller.orderNumber,
        eligibility: snapshot,
      });
    }
    return rows;
  }, [page, eligibility]);

  if (page === undefined) {
    return <div className="bg-surface rounded-2xl border p-8 text-center text-gray-500">در حال دریافت جزئیات سفارش...</div>;
  }
  if (!page) {
    return (
      <div className="bg-surface rounded-2xl border p-8 text-center">
        <Package className="w-10 h-10 mx-auto text-gray-300 mb-3" />
        <h1 className="font-black">سفارش پیدا نشد</h1>
        <p className="text-sm text-gray-500 mt-2">این سفارش متعلق به نشست جاری نیست یا دیگر در دسترس نیست.</p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2 text-sm">
        <Link href="/customer-panel/orders" className="text-gray-500 flex items-center gap-1">
          <ChevronRight className="w-4 h-4" />
          سفارش‌ها
        </Link>
        <span className="text-gray-300">/</span>
        <strong className="truncate">{page.reference}</strong>
      </div>

      <section className="bg-surface rounded-2xl border border-gray-100 p-4 md:p-6 shadow-sm" data-storefront-surface-role="elevated">
        <div className="flex flex-wrap items-center justify-between gap-3 pb-5 border-b border-gray-100">
          <div>
            <p className="text-xs text-gray-400">شماره سفارش</p>
            <h1 className="text-lg font-black mt-1">{page.reference}</h1>
            <p className="text-xs text-gray-500 mt-1">{new Date(page.submittedAt).toLocaleString("fa-IR")}</p>
          </div>
          <div className="flex flex-wrap gap-2">
            <span className={`rounded-xl px-4 py-2 text-sm font-bold ${customerStatusClasses(page.paymentState)}`}>
              پرداخت: {formatCustomerOrderStatus(page.paymentState)}
            </span>
            <span className="bg-blue-50 text-primary rounded-xl px-4 py-2 text-sm font-bold">
              سفارش: {formatCustomerOrderStatus(page.status)}
            </span>
          </div>
        </div>
        {page.paymentState === "PaymentExpired" || page.paymentState === "Expired" ? (
          <div className="mt-4 rounded-xl border border-amber-100 bg-amber-50 px-4 py-3" data-testid="customer-unpaid-expired">
            <p className="text-sm font-bold text-amber-800">مهلت پرداخت این سفارش به پایان رسیده است.</p>
            {page.canRetryUnpaid ? (
              <button
                type="button"
                disabled={retryBusy}
                data-testid="customer-unpaid-retry"
                className="mt-3 inline-flex items-center rounded-xl bg-primary px-4 py-2 text-sm font-bold text-white disabled:opacity-60"
                onClick={() => {
                  setRetryBusy(true);
                  setRetryError(null);
                  void retryCustomerUnpaidOrder(page.checkoutId).then((result) => {
                    if (result.ok) {
                      setPage(result.page);
                    } else {
                      setRetryError(result.message);
                    }
                    setRetryBusy(false);
                  });
                }}
              >
                تلاش مجدد پرداخت
              </button>
            ) : null}
            {retryError ? <p className="mt-2 text-xs font-bold text-red-600">{retryError}</p> : null}
          </div>
        ) : null}

        <div className="grid grid-cols-2 md:grid-cols-4 gap-3 py-5">
          <Money label="جمع کالاها" amount={page.subtotal} currency={page.currency} />
          <Money label="تخفیف" amount={page.discountAmount} currency={page.currency} />
          <Money label="مالیات" amount={page.taxAmount} currency={page.currency} />
          <Money label="مبلغ پرداختی" amount={page.payableAmount} currency={page.currency} strong />
        </div>

        {pendingReturn || returnSellers.length > 0 ? (
          <div className="mb-4 rounded-2xl border border-blue-100 bg-blue-50/60 px-4 py-3 flex flex-wrap items-center gap-3">
            <RotateCcw className="w-4 h-4 text-primary shrink-0" />
            <p className="text-xs text-gray-700 flex-1 min-w-[12rem]">
              {pendingReturn
                ? "یک درخواست مرجوعی ناتمام برای این سفارش نگه داشته شده است."
                : "برای اقلام تحویل‌شدهٔ این سفارش می‌توانید درخواست مرجوعی ثبت کنید؛ فقط اقلامی که انتخاب کنید مرجوع می‌شوند."}
            </p>
            <button
              type="button"
              data-testid="open-customer-return-request"
              onClick={() => setReturnModalOpen(true)}
              className="inline-flex items-center gap-2 rounded-xl bg-primary px-4 py-2.5 text-sm font-bold text-white hover:bg-blue-700 transition-colors min-h-11"
            >
              <RotateCcw className="w-4 h-4" />
              {pendingReturn ? "ادامهٔ درخواست مرجوعی" : "درخواست مرجوعی"}
            </button>
          </div>
        ) : null}

        {eligibility === null ? (
          <p className="mb-3 text-xs text-gray-500">در حال بررسی شرایط مرجوعی...</p>
        ) : returnSellers.length === 0 ? (
          <p className="mb-3 text-xs text-gray-500">در حال حاضر قلم قابل مرجوعی برای این سفارش وجود ندارد.</p>
        ) : null}

        <div className="space-y-4">
          {page.sellerOrders.map((seller) => (
            <article key={seller.sellerOrderId} className="rounded-2xl border border-gray-100 overflow-hidden bg-surface" data-storefront-surface-role="card">
              <div className="bg-gray-50 px-4 py-3 flex flex-wrap items-center gap-3">
                <Store className="w-5 h-5 text-primary" />
                <strong className="text-sm">{seller.sellerDisplayName}</strong>
                <span className="text-xs text-gray-400">{seller.orderNumber}</span>
                <span className="me-auto text-xs font-bold">سفارش: {formatCustomerOrderStatus(seller.status)}</span>
                <span className={`rounded-lg px-2.5 py-1 text-xs font-bold ${customerStatusClasses(seller.paymentState)}`}>
                  پرداخت: {formatCustomerOrderStatus(seller.paymentState)}
                </span>
              </div>
              <div className="divide-y divide-gray-100">
                {seller.lines.map((line) => (
                  <div key={`${seller.sellerOrderId}-${line.offerId}`} className="p-4 flex items-center gap-4">
                    <div className="w-14 h-14 bg-blue-50 rounded-xl flex items-center justify-center shrink-0">
                      <Package className="w-6 h-6 text-primary" />
                    </div>
                    <div className="min-w-0 flex-1">
                      <p className="font-bold text-sm truncate">{line.title}</p>
                      <p className="text-xs text-gray-400 mt-1">
                        تعداد {line.quantity.toLocaleString("fa-IR")} · {line.sellerDisplayName}
                      </p>
                    </div>
                    <strong className="text-sm">{formatCustomerMoney(line.linePayable, line.currency)}</strong>
                  </div>
                ))}
              </div>
              {fulfillmentBySeller.get(seller.sellerOrderId) ? (
                <div className="px-4 pb-4">
                  <FulfillmentShippingInfoBlock snapshot={fulfillmentBySeller.get(seller.sellerOrderId)!} />
                </div>
              ) : fulfillments !== undefined && fulfillments !== null && fulfillments.length === 0 ? (
                <p className="px-4 pb-4 text-xs text-gray-400">وضعیت ارسال این فروشنده هنوز ثبت نشده است.</p>
              ) : null}
            </article>
          ))}
        </div>
      </section>

      <section className="bg-surface rounded-2xl border border-gray-100 p-4 md:p-6 shadow-sm grid md:grid-cols-2 gap-5">
        <div className="flex gap-3">
          <MapPin className="w-5 h-5 text-primary shrink-0" />
          <div>
            <h2 className="font-black text-sm">نشانی تحویل</h2>
            <p className="text-sm text-gray-600 mt-2 leading-7">
              {page.recipientName}، {page.provinceName}، {page.cityName}، {page.postalAddress}
            </p>
            <p className="text-xs text-gray-400">{page.contactMobile} · کد پستی {page.postalCode}</p>
          </div>
        </div>
        <div className="flex gap-3">
          <Truck className="w-5 h-5 text-primary shrink-0" />
          <div className="min-w-0 flex-1">
            <h2 className="font-black text-sm">روش ارسال</h2>
            <p className="text-sm text-gray-600 mt-2">{page.shippingMethodLabel || "ثبت نشده"}</p>
            <p className="text-xs text-gray-400 mt-2">اطلاعات فوق snapshot زمان ثبت سفارش است.</p>
            {fulfillments === undefined ? (
              <p className="text-xs text-gray-500 mt-3">در حال دریافت وضعیت محموله‌ها...</p>
            ) : fulfillments === null ? (
              <p className="text-xs text-red-600 mt-3">وضعیت محموله در دسترس نیست.</p>
            ) : null}
          </div>
        </div>
      </section>
      {returnModalOpen ? (
        <ReturnRequestModal
          open
          checkoutId={params.checkoutId}
          orderReference={page.reference}
          viewerId={viewerId}
          sellers={returnSellers}
          onClose={() => setReturnModalOpen(false)}
          onSubmitted={() => {
            const storage = typeof window === "undefined" ? null : window.sessionStorage;
            setPendingReturn(hasUnresolvedCustomerReturnSubmission(storage, params.checkoutId, viewerId));
          }}
        />
      ) : null}
    </div>
  );
}

function Money({ label, amount, currency, strong = false }: { label: string; amount: number; currency: string; strong?: boolean }) {
  return (
    <div className={`rounded-xl p-3 ${strong ? "bg-blue-50" : "bg-gray-50"}`}>
      <p className="text-xs text-gray-500">{label}</p>
      <p className={`text-sm mt-2 ${strong ? "font-black text-primary" : "font-bold"}`}>
        {formatCustomerMoney(amount, currency)}
      </p>
    </div>
  );
}
