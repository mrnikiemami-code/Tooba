"use client";

import Link from "next/link";
import { ChevronLeft, Package, Plus } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import {
  type CustomerOrderListItem,
  formatCustomerMoney,
  formatCustomerOrderStatus,
  loadCustomerOrders,
} from "../customer-api";

const filters = ["همه", "پرداخت‌شده", "در انتظار پرداخت", "لغو شده"] as const;

/**
 * فهرست سفارش Shopeiva با تب‌ها و ردیف‌های جمع‌شوندهٔ متصل به Host.
 */
export default function CustomerOrdersPage() {
  const [rows, setRows] = useState<CustomerOrderListItem[] | null | undefined>(undefined);
  const [filter, setFilter] = useState<(typeof filters)[number]>("همه");

  useEffect(() => {
    void loadCustomerOrders().then(setRows);
  }, []);

  const filtered = useMemo(() => {
    if (!rows || filter === "همه") return rows ?? [];
    return rows.filter((row) => formatCustomerOrderStatus(row.paymentState) === filter);
  }, [filter, rows]);

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-black">سفارش‌های من</h1>
          <p className="text-xs text-gray-500 mt-1">پیگیری سفارش‌های ثبت‌شده در فروشگاه توبا</p>
        </div>
        <Link href="/products" className="inline-flex items-center gap-2 bg-[#2563EB] text-white px-4 py-2.5 rounded-xl text-sm font-bold">
          <Plus className="w-4 h-4" />
          سفارش جدید
        </Link>
      </div>

      <section className="bg-white rounded-2xl border border-gray-100 p-3 md:p-5 shadow-sm">
        <div className="grid grid-cols-3 gap-2 mb-4">
          <StatusSummary label="کل سفارش‌ها" value={rows?.length ?? 0} color="blue" />
          <StatusSummary label="در انتظار پرداخت" value={rows?.filter((x) => x.paymentState !== "Paid").length ?? 0} color="amber" />
          <StatusSummary label="پرداخت‌شده" value={rows?.filter((x) => x.paymentState === "Paid").length ?? 0} color="green" />
        </div>
        <div className="flex gap-2 overflow-x-auto pb-3 border-b border-gray-100">
          {filters.map((item) => (
            <button
              key={item}
              type="button"
              onClick={() => setFilter(item)}
              className={`shrink-0 px-4 py-2 rounded-xl text-xs font-bold ${
                filter === item ? "bg-[#2563EB] text-white" : "border border-gray-200 text-gray-600"
              }`}
            >
              {item}
            </button>
          ))}
        </div>

        {rows === undefined ? (
          <p className="py-12 text-center text-gray-500">در حال دریافت سفارش‌ها...</p>
        ) : rows === null ? (
          <p className="py-12 text-center text-red-600">دریافت سفارش‌ها ممکن نشد.</p>
        ) : filtered.length === 0 ? (
          <div className="py-14 text-center text-gray-500">
            <Package className="w-11 h-11 mx-auto text-gray-300 mb-3" />
            <p className="font-bold">سفارشی در این وضعیت وجود ندارد.</p>
          </div>
        ) : (
          <div className="space-y-3 pt-4">
            {filtered.map((order) => (
              <Link
                key={order.checkoutId}
                href={`/customer-panel/orders/${order.checkoutId}`}
                className="flex flex-wrap md:flex-nowrap items-center gap-3 rounded-xl border border-gray-100 px-4 py-4 hover:border-blue-200"
              >
                <div className="min-w-0 flex-1">
                  <p className="font-bold text-sm truncate">سفارش {order.reference}</p>
                  <p className="text-xs text-gray-400 mt-1">
                    {new Date(order.submittedAt).toLocaleDateString("fa-IR")} · {order.itemCount.toLocaleString("fa-IR")} کالا
                  </p>
                </div>
                <span className="text-xs font-bold text-[#2563EB] bg-blue-50 rounded-lg px-3 py-1.5">
                  {formatCustomerOrderStatus(order.paymentState)}
                </span>
                <strong className="text-sm min-w-36 text-left">{formatCustomerMoney(order.payableAmount, order.currency)}</strong>
                <ChevronLeft className="w-4 h-4 text-gray-400" />
              </Link>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}

function StatusSummary({ label, value, color }: { label: string; value: number; color: "blue" | "amber" | "green" }) {
  const colors = { blue: "text-blue-500", amber: "text-amber-500", green: "text-emerald-500" };
  return (
    <div className="rounded-xl border border-gray-100 px-3 py-4 text-center">
      <strong className={`text-xl ${colors[color]}`}>{value.toLocaleString("fa-IR")}</strong>
      <p className="text-[10px] md:text-xs text-gray-500 mt-1">{label}</p>
    </div>
  );
}
