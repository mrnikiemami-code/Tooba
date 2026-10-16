/**
 * TB-SHIPPING-INDEPENDENT-GROUP-FRONTEND-150 — افق تحویلِ مسیر تک‌گروهی (سازگاری گذشته).
 *
 * این ماژول فقط مسیر **تک‌گروهی/تک‌خطی** را نگه می‌دارد: افق همان `projection` سرور است (بدون تغییر
 * رفتار قبلی). سبد چندگروهی دیگر افق مشترک/تلاقی ندارد؛ افق هر گروه مستقل در
 * `storefront-shipping-group-schedule.ts` از ردیف authoritative سرور خوانده می‌شود.
 *
 * هیچ تاریخ، بازه یا وعده‌ای در کلاینت ساخته نمی‌شود.
 */
import type {
  StorefrontDeliveryDateOption,
  StorefrontDeliveryTimeOption,
  StorefrontShippingProjection,
} from "./storefront-shipping-api.ts";

export interface StorefrontShippingDateHorizon {
  dates: StorefrontDeliveryDateOption[];
  timeWindows: StorefrontDeliveryTimeOption[];
  minimumDeliveryDate: string | null;
  /** پاسخ سرور دریافت و مشتق شده است. */
  resolved: boolean;
}

export const EMPTY_SHIPPING_DATE_HORIZON: StorefrontShippingDateHorizon = {
  dates: [],
  timeWindows: [],
  minimumDeliveryDate: null,
  resolved: false,
};

/** افق مسیر تک‌گروهی: دقیقاً فیلدهای همان projection (رفتار قبلی، بدون تغییر). */
export function resolveSingleGroupHorizon(
  projection: StorefrontShippingProjection | null,
): StorefrontShippingDateHorizon {
  if (!projection) return EMPTY_SHIPPING_DATE_HORIZON;
  return {
    dates: projection.deliveryDates,
    timeWindows: projection.deliveryTimeWindows,
    minimumDeliveryDate: projection.minimumDeliveryDate,
    resolved: true,
  };
}

/** تاریخ انتخاب‌شده را نگه می‌دارد فقط اگر هنوز در فهرست مجاز سرور باشد؛ وگرنه نزدیک‌ترین مجاز. */
export function pickEligibleDeliveryDate(
  current: string,
  horizon: StorefrontShippingDateHorizon,
): string {
  if (!horizon.resolved) return current;
  if (horizon.dates.length === 0) return "";
  if (current && horizon.dates.some((date) => date.value === current)) return current;
  return horizon.dates[0].value;
}

/** بازهٔ انتخاب‌شده را پاک می‌کند فقط اگر دیگر برای روش فعلی معتبر نباشد. */
export function pickEligibleDeliveryWindow(
  current: string,
  horizon: StorefrontShippingDateHorizon,
): string {
  if (!horizon.resolved) return current;
  return current && horizon.timeWindows.some((window) => window.value === current) ? current : "";
}

/** آیا انتخاب تاریخ/بازه برای ادامه معتبر است (تاریخ باید واقعاً از سرور آمده باشد). */
export function isShippingDateHorizonComplete(
  horizon: StorefrontShippingDateHorizon,
  deliveryDate: string,
  deliveryTime: string,
): boolean {
  return (
    horizon.resolved
    && horizon.dates.some((date) => date.value === deliveryDate)
    && horizon.timeWindows.some((window) => window.value === deliveryTime)
  );
}
