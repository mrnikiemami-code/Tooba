"use client";

/**
 * TB-SHIPPING-ONE-LEVEL-TABS-133 — هماهنگ‌کنندهٔ افق تحویل سبد چندفروشنده‌ای.
 *
 * این hook فقط سیم‌کشی و ترتیب درخواست‌ها را مدیریت می‌کند و هیچ منطق تجاری یا تاریخ‌سازی ندارد:
 * برای هر روش **متمایز** انتخاب‌شده یک‌بار مسیر مجاز موجود `shipping/projection` را صدا می‌زند و
 * افق مشترک را از پاسخ سرور مشتق می‌کند. پاسخ قدیمی هرگز انتخاب جدیدتر را بازنویسی نمی‌کند.
 *
 * این فایل هیچ تغییر بصری ندارد؛ فقط وضعیت/API را به سطح ارسال وصل می‌کند.
 */
import { useEffect, useRef, useState } from "react";

import type { GeographyDestinationIdentifiers } from "./storefront-shipping-destination-model.ts";
import { loadShippingProjection, type StorefrontShippingProjection } from "./storefront-shipping-api.ts";
import {
  createLatestResponseGate,
  deriveGroupDateHorizon,
  EMPTY_SHIPPING_DATE_HORIZON,
  type LatestResponseGate,
  type StorefrontShippingDateHorizon,
} from "./storefront-shipping-group-horizon.ts";

export function useGroupShippingDateHorizon(input: {
  enabled: boolean;
  cartId: string | null;
  methodCodes: readonly string[];
  provinceName: string;
  cityName: string;
  destination: GeographyDestinationIdentifiers | null;
  onError: (cause: unknown) => void;
}): { horizon: StorefrontShippingDateHorizon; pending: boolean } {
  const [horizon, setHorizon] = useState<StorefrontShippingDateHorizon>(EMPTY_SHIPPING_DATE_HORIZON);
  const [pending, setPending] = useState(false);
  const gateRef = useRef<LatestResponseGate>(createLatestResponseGate());
  // آخرین callback خطا بدون وابستگی به هویت آن در هر رندر.
  const onErrorRef = useRef(input.onError);
  onErrorRef.current = input.onError;

  const { enabled, cartId, provinceName, cityName, destination } = input;
  const methodKey = input.methodCodes.join("|");

  useEffect(() => {
    if (!enabled || !cartId || input.methodCodes.length === 0) {
      gateRef.current.begin();
      setPending(false);
      setHorizon(EMPTY_SHIPPING_DATE_HORIZON);
      return;
    }
    const token = gateRef.current.begin();
    let active = true;
    setPending(true);
    void (async () => {
      const settled = await Promise.allSettled(
        input.methodCodes.map((code) =>
          loadShippingProjection({
            cartId,
            provinceName: provinceName || undefined,
            cityName: cityName || undefined,
            destination,
            methodCode: code,
          }),
        ),
      );
      // پاسخ کهنه (روش/مقصد جدیدتر شده) نه state را بازنویسی می‌کند و نه خطا را.
      if (!active || !gateRef.current.isCurrent(token)) return;
      setPending(false);
      const failed = settled.find((result) => result.status === "rejected");
      if (failed?.status === "rejected") {
        setHorizon(EMPTY_SHIPPING_DATE_HORIZON);
        onErrorRef.current(failed.reason);
        return;
      }
      const pages = settled
        .filter((result): result is PromiseFulfilledResult<StorefrontShippingProjection> => result.status === "fulfilled")
        .map((result) => result.value);
      if (pages.length !== input.methodCodes.length) return;
      setHorizon(deriveGroupDateHorizon(pages));
    })();
    return () => {
      active = false;
    };
    // `methodKey` و `input.methodCodes` هر دو لازم‌اند: کلید از واکشی تکراری برای مجموعهٔ یکسان
    // جلوگیری می‌کند و آرایهٔ متمایز/مرتب‌شده هویت پایدار درخواست‌ها را می‌دهد.
  }, [enabled, cartId, methodKey, input.methodCodes, provinceName, cityName, destination]);

  return { horizon, pending };
}
