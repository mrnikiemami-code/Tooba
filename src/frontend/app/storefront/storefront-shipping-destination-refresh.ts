import type { GeographyDestinationIdentifiers } from "./storefront-shipping-destination-model.ts";

/**
 * TB-230 — هماهنگ‌کنندهٔ واقعی تازه‌سازی مقصد در پیش‌نمایش capture‌شده.
 *
 * مسئلهٔ اندازه‌گیری‌شده: در مسیر capture، انتخاب‌گر روش قدیمی پنهان است و تنها آن مسیر مقصد را
 * می‌فرستاد؛ پس پر کردن مقصد هرگز پیش‌نمایش را تازه نمی‌کرد و کارت‌ها برای همیشه
 * «destination_required» می‌ماندند.
 *
 * قواعد این ماژول (فقط همان چیزی که صفحه اجرا می‌کند، بدون fetch/DOM):
 * - هویت پایدار رشته‌ای مقصد ملاک است، نه ارجاع شیء؛ رندر نامرتبط هیچ درخواستی نمی‌سازد.
 * - پاک شدن والد هویت پایین‌تر را پاک می‌کند: کلید ناقص هم درخواست معتبر است و پاسخ صادقانه است.
 * - نسل درخواست، پاسخ‌های قدیمی/بی‌ترتیب را دور می‌ریزد.
 */

/** کلید پایدار مقصد از شناسه‌های تأییدشدهٔ Geography؛ خالی یعنی هیچ هویتی وجود ندارد. */
export function destinationIdentityKey(destination: GeographyDestinationIdentifiers | null): string {
  if (!destination) return "";
  const country = destination.countryId?.trim() ?? "";
  if (!country) return "";
  const area = destination.administrativeAreaId?.trim() ?? "";
  const city = destination.cityId?.trim() ?? "";
  return `${country}|${area}|${city}`;
}

export type DestinationRefreshAction = "skip" | "request";

export interface DestinationRefreshPlan {
  action: DestinationRefreshAction;
  /** مقصد دقیقی که باید فرستاده شود؛ `null` یعنی پاک‌شدن هویت و ارسال بدون مقصد. */
  destination: GeographyDestinationIdentifiers | null;
}

/**
 * آیا این تغییر مقصد باید درخواست تازه بسازد.
 *
 * فقط وقتی پیش‌نمایش capture فعال است اجرا می‌شود؛ حالت خاموش (legacy) هیچ رفتاری عوض نمی‌کند.
 * کلید تکراری (رندر نامرتبط/همان مقدار) هیچ درخواستی نمی‌سازد.
 */
export function planDestinationRefresh(input: {
  capturedActive: boolean;
  destinationKey: string;
  appliedKey: string;
  destination: GeographyDestinationIdentifiers | null;
}): DestinationRefreshPlan {
  if (!input.capturedActive) return { action: "skip", destination: null };
  if (input.destinationKey === input.appliedKey) return { action: "skip", destination: null };
  if (input.destinationKey === "") {
    // هویت مقصد پاک شد (مثلاً تغییر/پاک شدن والد): قیمت قبلی نباید «جاری» بماند.
    return { action: "request", destination: null };
  }
  return { action: "request", destination: input.destination };
}

/** نسل درخواست: هر درخواست شمارهٔ تازه می‌گیرد و فقط آخرین شماره معتبر است. */
export function createRequestGeneration(): {
  next: () => number;
  isCurrent: (sequence: number) => boolean;
} {
  let current = 0;
  return {
    next: () => {
      current += 1;
      return current;
    },
    isCurrent: (sequence: number) => sequence === current,
  };
}
