"use client";

import { Package, Truck } from "lucide-react";
import type { FulfillmentShipment, FulfillmentSnapshot } from "./fulfillment-api.ts";
import {
  formatFulfillmentDate,
  formatFulfillmentStatus,
  formatShipmentStatus,
  fulfillmentStatusBadgeClass,
} from "./fulfillment-api.ts";

/** badge وضعیت محموله با کلاس‌های موجود. */
export function ShipmentStatusBadge({ status }: { status: string }) {
  return (
    <span className={`inline-flex rounded-xl px-3 py-1 text-xs font-bold ${fulfillmentStatusBadgeClass(status).replace("inline-flex ", "")}`}>
      {formatShipmentStatus(status)}
    </span>
  );
}

/** کارت خلاصه fulfillment برای Customer/Seller/Admin. */
export function FulfillmentSummaryCard({ snapshot }: { snapshot: FulfillmentSnapshot }) {
  return (
    <article className="rounded-2xl border border-gray-100 overflow-hidden">
      <div className="bg-gray-50 px-4 py-3 flex flex-wrap items-center gap-3">
        <Package className="w-5 h-5 text-[#2563EB]" />
        <strong className="text-sm">ارسال فروشنده</strong>
        <span className="text-xs text-gray-400">{snapshot.sellerOrderId.slice(0, 8)}</span>
        <span className={fulfillmentStatusBadgeClass(snapshot.status)}>
          {formatFulfillmentStatus(snapshot.status)}
        </span>
        <span className="me-auto text-xs text-gray-500">{snapshot.shippingMethodLabel || "—"}</span>
      </div>
      <div className="p-4 space-y-4">
        <p className="text-sm text-gray-600 leading-7">
          {snapshot.recipientName} · {snapshot.provinceName}، {snapshot.cityName} · {snapshot.postalAddress}
        </p>
        <FulfillmentShipmentList shipments={snapshot.shipments} />
      </div>
    </article>
  );
}

/** فهرست محموله‌ها با tracking و زمان‌بندی. */
export function FulfillmentShipmentList({ shipments }: { shipments: FulfillmentShipment[] }) {
  if (shipments.length === 0) {
    return <p className="text-sm text-gray-500">هنوز محموله‌ای ثبت نشده است.</p>;
  }

  return (
    <ul className="space-y-3">
      {shipments.map((shipment) => (
        <li key={shipment.shipmentId} className="rounded-xl border border-gray-100 p-3">
          <div className="flex flex-wrap items-center gap-2">
            <Truck className="w-4 h-4 text-[#2563EB]" />
            <strong className="text-sm">{shipment.carrierDisplayName || "حامل"}</strong>
            <ShipmentStatusBadge status={shipment.status} />
          </div>
          <dl className="mt-3 grid gap-2 text-xs text-gray-600 sm:grid-cols-2">
            <div>
              <dt className="text-gray-400">کد رهگیری</dt>
              <dd className="mt-1 font-medium" dir="ltr">{shipment.trackingReference || "ثبت نشده"}</dd>
            </div>
            <div>
              <dt className="text-gray-400">ارسال</dt>
              <dd className="mt-1 font-medium">{formatFulfillmentDate(shipment.dispatchedAt)}</dd>
            </div>
            <div>
              <dt className="text-gray-400">تحویل</dt>
              <dd className="mt-1 font-medium">{formatFulfillmentDate(shipment.deliveredAt)}</dd>
            </div>
            <div>
              <dt className="text-gray-400">اقلام</dt>
              <dd className="mt-1 font-medium">{shipment.items.length.toLocaleString("fa-IR")} خط</dd>
            </div>
          </dl>
        </li>
      ))}
    </ul>
  );
}
