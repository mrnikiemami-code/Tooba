"use client";

/**
 * TB-SHIPPING-INDEPENDENT-GROUP-FRONTEND-150 — بخش مسیر **تک‌گروهی/تک‌خطی**.
 *
 * کارت «روش ارسال» + کارت «زمان تحویل» فقط برای سبدی رندر می‌شود که یک گروه ارسال دارد (رفتار و
 * ظاهر قبلی، بدون تغییر). در سبد چندگروهی این بخش رندر نمی‌شود؛ هر گروه کارت و انتخاب تاریخ/بازهٔ
 * مستقل خودش را دارد و هیچ انتخاب‌کنندهٔ سراسری وجود ندارد.
 */
import { Clock, Package, Store, Zap } from "lucide-react";

import { formatOfferAmount } from "./storefront-api.ts";
import type { StorefrontShippingMethod } from "./storefront-shipping-api.ts";
import type { StorefrontShippingDateHorizon } from "./storefront-shipping-group-horizon.ts";
import { StorefrontShippingDeliveryCard } from "./storefront-shipping-delivery-card.tsx";
import { StorefrontShippingUnavailableMethods } from "./storefront-shipping-unavailable-methods.tsx";
import type { StorefrontShippingUnavailableNotice } from "./storefront-shipping-unavailable-methods.ts";

function iconFor(method: StorefrontShippingMethod) {
  const key = method.iconKey || method.serviceCode;
  if (key.includes("zap") || key.includes("express") || method.methodCode.includes("express")) return Zap;
  if (key.includes("store") || key.includes("person") || method.serviceCode === "in_person") return Store;
  return Package;
}

export function StorefrontShippingSingleGroupSection({
  methods,
  unavailableNotices = [],
  currency,
  methodCode,
  onSelectMethod,
  horizon,
  deliveryDate,
  deliveryTime,
  onSelectDate,
  onSelectWindow,
}: {
  methods: readonly StorefrontShippingMethod[];
  /** TB-165: فقط توضیح روش‌های بدون نرخ معتبر؛ هرگز گزینهٔ قابل انتخاب نمی‌سازد. */
  unavailableNotices?: readonly StorefrontShippingUnavailableNotice[];
  currency: string;
  methodCode: string | null;
  onSelectMethod: (methodCode: string) => void;
  horizon: StorefrontShippingDateHorizon;
  deliveryDate: string;
  deliveryTime: string;
  onSelectDate: (value: string) => void;
  onSelectWindow: (value: string) => void;
}) {
  return (
    <>
      <div
        className="bg-surface rounded-2xl border border-gray-200 p-4 md:p-5 shadow-sm"
        data-storefront-surface-role="card"
        data-testid="shipping-methods"
      >
        <h3 className="text-sm md:text-base font-black text-gray-900 flex items-center gap-2 mb-3">
          <Package className="w-4 h-4 text-primary" />
          روش ارسال
        </h3>
        {methods.length === 0 ? (
          <p className="text-sm text-gray-500" data-testid="shipping-methods-empty">
            در حال حاضر روش ارسالی برای این مقصد فعال نیست.
          </p>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
            {methods.map((m) => {
              const Icon = iconFor(m);
              const selected = methodCode === m.methodCode;
              return (
                <button
                  key={m.methodCode}
                  type="button"
                  onClick={() => onSelectMethod(m.methodCode)}
                  className={`relative flex items-start gap-3 p-4 rounded-2xl border-2 text-right transition-all ${
                    selected
                      ? "border-primary bg-primary/5 shadow-md shadow-primary/10"
                      : "border-gray-200 hover:border-gray-300 bg-gray-50"
                  }`}
                  data-testid={`shipping-method-${m.methodCode}`}
                >
                  <div
                    className={`w-11 h-11 rounded-xl flex items-center justify-center shrink-0 ${
                      selected ? "bg-primary/10" : "bg-surface"
                    }`}
                  >
                    <Icon className={`w-5 h-5 ${selected ? "text-primary" : "text-gray-400"}`} />
                  </div>
                  <div className="min-w-0 flex-1">
                    <p className={`text-xs md:text-sm font-bold ${selected ? "text-primary" : "text-gray-700"}`}>
                      {m.label}
                    </p>
                    <p className={`text-[11px] md:text-sm font-black mt-0.5 ${selected ? "text-primary" : "text-gray-900"}`}>
                      {m.isFree || m.priceAmount === 0 ? "رایگان" : formatOfferAmount(m.priceAmount, currency)}
                    </p>
                    <p className="text-[9px] text-gray-400 mt-0.5 flex items-center gap-0.5">
                      <Clock className="w-2.5 h-2.5" /> {m.estimationLabel}
                    </p>
                  </div>
                  {selected ? <div className="absolute top-2 left-2 w-3 h-3 rounded-full bg-primary" /> : null}
                </button>
              );
            })}
          </div>
        )}
        <StorefrontShippingUnavailableMethods notices={unavailableNotices} />
      </div>

      <StorefrontShippingDeliveryCard
        horizon={horizon}
        deliveryDate={deliveryDate}
        deliveryTime={deliveryTime}
        onSelectDate={onSelectDate}
        onSelectWindow={onSelectWindow}
      />
    </>
  );
}
