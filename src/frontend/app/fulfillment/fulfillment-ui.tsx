"use client";

import { MapPin, Package, Phone, Truck } from "lucide-react";
import type { FulfillmentShipment, FulfillmentSnapshot } from "./fulfillment-api.ts";
import {
  formatFulfillmentDate,
  formatFulfillmentStatus,
  formatShipmentStatus,
  fulfillmentStatusBadgeClass,
} from "./fulfillment-api.ts";

/** badge وضعیت با الگوی Shopeiva orderDetail (rounded-full + icon density). */
export function ShipmentStatusBadge({ status }: { status: string }) {
  return (
    <span className={`inline-flex items-center gap-1 rounded-full px-3 py-1 text-[10px] font-medium ${fulfillmentStatusBadgeClass(status)}`}>
      {formatShipmentStatus(status)}
    </span>
  );
}

/**
 * بلوک «اطلاعات ارسال» مطابق orderDetailModal.jsx Shopeiva
 * (bg-gray-50 rounded-xl p-4 space-y-2 text-sm + MapPin/Phone/Package rows).
 */
export function FulfillmentShippingInfoBlock({ snapshot }: { snapshot: FulfillmentSnapshot }) {
  const primaryTracking = snapshot.shipments.find((shipment) => shipment.trackingReference)?.trackingReference ?? null;

  return (
    <div className="mt-4 border-t border-gray-100 pt-4">
      <h4 className="text-sm font-bold text-gray-900 mb-3 flex items-center gap-2">
        <Truck className="w-4 h-4 text-[#2563EB]" />
        اطلاعات ارسال
      </h4>
      <div className="bg-gray-50 rounded-xl p-4 space-y-2 text-sm">
        <div className="flex items-start gap-3 text-gray-600">
          <MapPin className="w-4 h-4 text-[#2563EB] mt-0.5 shrink-0" />
          <span>
            {snapshot.provinceName}، {snapshot.cityName}، {snapshot.postalAddress}
          </span>
        </div>
        <div className="flex items-center gap-3 text-gray-600">
          <Phone className="w-4 h-4 text-[#2563EB] shrink-0" />
          <span dir="ltr">{snapshot.contactMobile || "—"}</span>
        </div>
        {primaryTracking ? (
          <div className="flex items-center gap-3 text-gray-600">
            <Package className="w-4 h-4 text-[#2563EB] shrink-0" />
            <span>
              کد پیگیری: <span className="font-mono font-bold">{primaryTracking}</span>
            </span>
          </div>
        ) : null}
        <div className="flex flex-wrap items-center gap-2 pt-1">
          <span className={fulfillmentStatusBadgeClass(snapshot.status)}>{formatFulfillmentStatus(snapshot.status)}</span>
          <span className="text-xs text-gray-500">{snapshot.shippingMethodLabel || "—"}</span>
        </div>
      </div>
      <FulfillmentShipmentList shipments={snapshot.shipments} className="mt-3" />
    </div>
  );
}

/** فهرست محموله‌ها — hover transition مطابق ردیف محصول Shopeiva. */
export function FulfillmentShipmentList({
  shipments,
  className = "",
}: {
  shipments: FulfillmentShipment[];
  className?: string;
}) {
  if (shipments.length === 0) {
    return <p className={`text-sm text-gray-500 ${className}`}>هنوز محموله‌ای ثبت نشده است.</p>;
  }

  return (
    <ul className={`space-y-2 ${className}`}>
      {shipments.map((shipment) => (
        <li
          key={shipment.shipmentId}
          className="rounded-xl border border-gray-100 bg-gray-50 p-3 transition-colors hover:bg-gray-100"
        >
          <div className="flex flex-wrap items-center gap-2">
            <Truck className="w-4 h-4 text-[#2563EB]" />
            <strong className="text-sm text-gray-900">{shipment.carrierDisplayName || "حامل"}</strong>
            <ShipmentStatusBadge status={shipment.status} />
          </div>
          <dl className="mt-3 grid gap-2 text-xs text-gray-600 sm:grid-cols-2">
            <div>
              <dt className="text-gray-400">کد رهگیری</dt>
              <dd className="mt-1 font-mono font-bold" dir="ltr">{shipment.trackingReference || "ثبت نشده"}</dd>
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

/** @deprecated Use FulfillmentShippingInfoBlock inside seller-order article. */
export function FulfillmentSummaryCard({ snapshot }: { snapshot: FulfillmentSnapshot }) {
  return <FulfillmentShippingInfoBlock snapshot={snapshot} />;
}
