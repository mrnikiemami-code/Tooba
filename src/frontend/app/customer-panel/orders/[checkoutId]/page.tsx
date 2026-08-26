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
} from "../../customer-api";
import { loadCustomerFulfillments, type FulfillmentSnapshot } from "../../../fulfillment/fulfillment-api";
import { FulfillmentShippingInfoBlock } from "../../../fulfillment/fulfillment-ui";
import { ReturnFormModal } from "../../../returns/return-ui";

/**
 * جزئیات سفارش مشتری با خطوط هر فروشنده و snapshot ارسال واقعی.
 */
export default function CustomerOrderDetail() {
  const params = useParams<{ checkoutId: string }>();
  const [page, setPage] = useState<CustomerOrderDetailPage | null | undefined>(undefined);
  const [fulfillments, setFulfillments] = useState<FulfillmentSnapshot[] | null | undefined>(undefined);
  const [returnModal, setReturnModal] = useState<{ sellerOrderId: string; items: FulfillmentSnapshot["items"] } | null>(null);

  useEffect(() => {
    void loadCustomerOrderDetail(params.checkoutId).then(setPage);
    void loadCustomerFulfillments(params.checkoutId).then(setFulfillments);
  }, [params.checkoutId]);

  const fulfillmentBySeller = useMemo(() => {
    const map = new Map<string, FulfillmentSnapshot>();
    for (const snapshot of fulfillments ?? []) {
      map.set(snapshot.sellerOrderId, snapshot);
    }
    return map;
  }, [fulfillments]);

  if (page === undefined) {
    return <div className="bg-white rounded-2xl border p-8 text-center text-gray-500">در حال دریافت جزئیات سفارش...</div>;
  }
  if (!page) {
    return (
      <div className="bg-white rounded-2xl border p-8 text-center">
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

      <section className="bg-white rounded-2xl border border-gray-100 p-4 md:p-6 shadow-sm">
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
            <span className="bg-blue-50 text-[#2563EB] rounded-xl px-4 py-2 text-sm font-bold">
              سفارش: {formatCustomerOrderStatus(page.status)}
            </span>
          </div>
        </div>

        <div className="grid grid-cols-2 md:grid-cols-4 gap-3 py-5">
          <Money label="جمع کالاها" amount={page.subtotal} currency={page.currency} />
          <Money label="تخفیف" amount={page.discountAmount} currency={page.currency} />
          <Money label="مالیات" amount={page.taxAmount} currency={page.currency} />
          <Money label="مبلغ پرداختی" amount={page.payableAmount} currency={page.currency} strong />
        </div>

        <div className="space-y-4">
          {page.sellerOrders.map((seller) => (
            <article key={seller.sellerOrderId} className="rounded-2xl border border-gray-100 overflow-hidden">
              <div className="bg-gray-50 px-4 py-3 flex flex-wrap items-center gap-3">
                <Store className="w-5 h-5 text-[#2563EB]" />
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
                      <Package className="w-6 h-6 text-[#2563EB]" />
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
                <div className="px-4 pb-4 space-y-3">
                  <FulfillmentShippingInfoBlock snapshot={fulfillmentBySeller.get(seller.sellerOrderId)!} />
                  {fulfillmentBySeller.get(seller.sellerOrderId)!.status === "Delivered" ? (
                    <button
                      type="button"
                      onClick={() => setReturnModal({
                        sellerOrderId: seller.sellerOrderId,
                        items: fulfillmentBySeller.get(seller.sellerOrderId)!.items,
                      })}
                      className="inline-flex items-center gap-2 rounded-xl border border-[#2563EB] px-4 py-2 text-sm font-bold text-[#2563EB] hover:bg-blue-50 transition-colors"
                    >
                      <RotateCcw className="w-4 h-4" />
                      درخواست مرجوعی
                    </button>
                  ) : null}
                </div>
              ) : fulfillments !== undefined && fulfillments !== null && fulfillments.length === 0 ? (
                <p className="px-4 pb-4 text-xs text-gray-400">وضعیت ارسال این فروشنده هنوز ثبت نشده است.</p>
              ) : null}
            </article>
          ))}
        </div>
      </section>

      <section className="bg-white rounded-2xl border border-gray-100 p-4 md:p-6 shadow-sm grid md:grid-cols-2 gap-5">
        <div className="flex gap-3">
          <MapPin className="w-5 h-5 text-[#2563EB] shrink-0" />
          <div>
            <h2 className="font-black text-sm">نشانی تحویل</h2>
            <p className="text-sm text-gray-600 mt-2 leading-7">
              {page.recipientName}، {page.provinceName}، {page.cityName}، {page.postalAddress}
            </p>
            <p className="text-xs text-gray-400">{page.contactMobile} · کد پستی {page.postalCode}</p>
          </div>
        </div>
        <div className="flex gap-3">
          <Truck className="w-5 h-5 text-[#2563EB] shrink-0" />
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
      {returnModal ? (
        <ReturnFormModal
          open
          sellerOrderId={returnModal.sellerOrderId}
          fulfillmentItems={returnModal.items}
          onClose={() => setReturnModal(null)}
        />
      ) : null}
    </div>
  );
}

function Money({ label, amount, currency, strong = false }: { label: string; amount: number; currency: string; strong?: boolean }) {
  return (
    <div className={`rounded-xl p-3 ${strong ? "bg-blue-50" : "bg-gray-50"}`}>
      <p className="text-xs text-gray-500">{label}</p>
      <p className={`text-sm mt-2 ${strong ? "font-black text-[#2563EB]" : "font-bold"}`}>
        {formatCustomerMoney(amount, currency)}
      </p>
    </div>
  );
}
