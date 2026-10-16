/**
 * TB-SHIPPING-ONE-LEVEL-TABS-133 — افق تاریخ/بازهٔ تحویل برای سبد چندفروشنده‌ای.
 *
 * این ماژول هیچ `fetch` و هیچ React ندارد؛ فقط منطق خالص است تا بتوان «سرور-محور» بودن انتخاب
 * تاریخ را قطعی سنجید. هیچ نرخ، وعده یا حساب تقویمی در سمت کلاینت انجام نمی‌شود.
 *
 * قواعد مهم:
 * - تاریخ مجاز = **اشتراک** تاریخ‌های مجاز همهٔ روش‌های انتخاب‌شدهٔ متمایز (نه فقط آخرین کلیک).
 * - بازهٔ ساعت مجاز = اشتراک بازه‌های اعلام‌شدهٔ سرور برای همان روش‌ها.
 * - حداقل تاریخ = **دیرترین** حداقلِ اعلام‌شدهٔ سرور (محافظه‌کارانه برای همهٔ گروه‌ها).
 * - اگر هیچ تاریخ مشترکی نماند، فهرست خالی است و ادامهٔ سفارش ممکن نیست (تاریخ جعلی ساخته نمی‌شود).
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
  /** پاسخ سرور برای مجموعهٔ روش‌های فعلی دریافت و مشتق شده است. */
  resolved: boolean;
}

export const EMPTY_SHIPPING_DATE_HORIZON: StorefrontShippingDateHorizon = {
  dates: [],
  timeWindows: [],
  minimumDeliveryDate: null,
  resolved: false,
};

/**
 * افق مجاز مشترک را از پاسخ‌های سرور برای روش‌های **متمایز** می‌سازد.
 * ورودی باید دقیقاً یک projection موفق برای هر روش انتخاب‌شده داشته باشد.
 */
export function deriveGroupDateHorizon(
  projections: readonly StorefrontShippingProjection[],
): StorefrontShippingDateHorizon {
  if (projections.length === 0) return EMPTY_SHIPPING_DATE_HORIZON;

  const minimums = projections
    .map((projection) => projection.minimumDeliveryDate)
    .filter((value): value is string => Boolean(value));
  const minimumDeliveryDate = minimums.length === 0
    ? null
    : minimums.reduce((latest, value) => (value > latest ? value : latest));

  const [first, ...rest] = projections;
  const dateSets = [first, ...rest].map(
    (projection) => new Set(projection.deliveryDates.map((date) => date.value)),
  );
  const dateByValue = new Map(first.deliveryDates.map((date) => [date.value, date]));

  // اشتراک واقعی: تاریخی که حتی یک روش انتخاب‌شده ارائه نکرده باشد قابل استفاده نیست.
  const sharedDates = first.deliveryDates
    .map((date) => date.value)
    .filter((value) => dateSets.every((set) => set.has(value)))
    .filter((value) => (minimumDeliveryDate ? value >= minimumDeliveryDate : true));

  const dates = sharedDates.map((value, index) => {
    const source = dateByValue.get(value)!;
    // برچسب «نزدیک‌ترین» فقط بازنمایی ترتیب همان فهرست سرور است؛ مقداری ساخته نمی‌شود.
    return { ...source, isEarliest: index === 0 };
  });

  const windowSets = [first, ...rest].map(
    (projection) => new Set(projection.deliveryTimeWindows.map((window) => window.value)),
  );
  const windowByValue = new Map(first.deliveryTimeWindows.map((window) => [window.value, window]));
  const timeWindows = first.deliveryTimeWindows
    .map((window) => window.value)
    .filter((value) => windowSets.every((set) => set.has(value)))
    .map((value) => windowByValue.get(value)!);

  return { dates, timeWindows, minimumDeliveryDate, resolved: true };
}

/**
 * افق مؤثر برای رندر: سبد تک‌گروهی دقیقاً مانند گذشته از خود projection می‌آید (بدون تغییر رفتار)،
 * و سبد چندگروهی از افق مشترک مشتق‌شده از سرور.
 */
export function resolveShippingDateHorizon(input: {
  isGroupedCart: boolean;
  projection: StorefrontShippingProjection | null;
  groupHorizon: StorefrontShippingDateHorizon;
}): StorefrontShippingDateHorizon {
  if (input.isGroupedCart) return input.groupHorizon;
  if (!input.projection) return EMPTY_SHIPPING_DATE_HORIZON;
  return {
    dates: input.projection.deliveryDates,
    timeWindows: input.projection.deliveryTimeWindows,
    minimumDeliveryDate: input.projection.minimumDeliveryDate,
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

/** بازهٔ انتخاب‌شده را پاک می‌کند فقط اگر دیگر برای مجموعهٔ روش‌های فعلی معتبر نباشد. */
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
    horizon.resolved &&
    horizon.dates.some((date) => date.value === deliveryDate) &&
    horizon.timeWindows.some((window) => window.value === deliveryTime)
  );
}

/**
 * روش‌های **متمایز** انتخاب‌شده در گروه‌های فروشنده: کلید پایدار واکشی سرور.
 * دو گروه که یک روش را انتخاب کرده‌اند یک درخواست مشترک می‌گیرند و ترتیب گروه‌ها اثری ندارد.
 * روش ناشناخته (خارج از فهرست authoritative فروشگاه) حذف می‌شود.
 */
export function distinctSelectedMethodCodes(
  selections: readonly { methodCode: string }[],
  knownMethodCodes: ReadonlySet<string>,
): string[] {
  return [...new Set(selections.map((selection) => selection.methodCode))]
    .filter((code) => Boolean(code) && knownMethodCodes.has(code))
    .sort();
}

export interface LatestResponseGate {
  /** توکن یکتای درخواست جدید؛ هر درخواست قبلی بی‌اعتبار می‌شود. */
  begin(): number;
  isCurrent(token: number): boolean;
}

/** جلوگیری از race: پاسخ قدیمی هرگز انتخاب جدیدتر را بازنویسی نمی‌کند. */
export function createLatestResponseGate(): LatestResponseGate {
  let current = 0;
  return {
    begin: () => {
      current += 1;
      return current;
    },
    isCurrent: (token) => token === current,
  };
}
