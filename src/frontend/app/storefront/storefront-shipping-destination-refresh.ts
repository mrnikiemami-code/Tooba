import type { GeographyDestinationIdentifiers } from "./storefront-shipping-destination-model.ts";

/**
 * TB-230 — هماهنگ‌کنندهٔ واقعی تازه‌سازی مقصد در پیش‌نمایش capture‌شده.
 *
 * مسئلهٔ اندازه‌گیری‌شده: در مسیر capture، انتخاب‌گر روش قدیمی پنهان است و تنها آن مسیر مقصد را
 * می‌فرستاد؛ پس پر کردن مقصد هرگز پیش‌نمایش را تازه نمی‌کرد و کارت‌ها برای همیشه
 * «destination_required» می‌ماندند.
 *
 * TB-230-R1: تنها یک «بولین در حال بارگذاری» برای تصمیم‌گیری کافی نیست. یک پیش‌نمایش موفق **مالِ**
 * همان هویت مقصدی است که برایش گرفته شده؛ اگر مقصد فعلی متفاوت باشد یا تازه‌سازی‌اش شکست بخورد،
 * پول/تاریخ قبلی هرگز به‌عنوان پاسخ مقصد فعلی برنمی‌گردد. بنابراین سه هویت جدا نگه داشته می‌شود:
 * - `confirmedKey`: کلید مقصدی که پاسخ موفقِ **فعلی** واقعاً برای آن است.
 * - `requestedKey`: کلیدی که آخرین درخواست برایش فرستاده شده (فقط شروع، نه موفقیت).
 * - `failedKey`: کلیدی که آخرین تلاش برایش شکست خورده و فقط با Retry صریح تکرار می‌شود.
 *
 * قواعد این ماژول (فقط همان چیزی که صفحه اجرا می‌کند، بدون fetch/DOM):
 * - هویت پایدار رشته‌ای مقصد ملاک است، نه ارجاع شیء؛ رندر نامرتبط هیچ درخواستی نمی‌سازد.
 * - پاک شدن والد هویت پایین‌تر را پاک می‌کند: کلید ناقص هم درخواست معتبر است و پاسخ صادقانه است.
 * - نسل درخواست، پاسخ‌های قدیمی/بی‌ترتیب را دور می‌ریزد؛ گذارهای این فایل هم همین را تضمین می‌کنند.
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

/** وضعیت صریح تازه‌سازی مقصد: موفق/در انتظار/شکست‌خورده — نه یک بولین بارگذاری. */
export interface DestinationRefreshSnapshot {
  /** هویت مقصدی که پیش‌نمایش فعلی واقعاً برای آن است. */
  readonly confirmedKey: string;
  /** هویتی که برایش درخواست فرستاده شده و هنوز پاسخش نرسیده. */
  readonly requestedKey: string;
  /** هویتی که آخرین تلاشش شکست خورده و منتظر Retry صریح کاربر است. */
  readonly failedKey: string;
  readonly inFlight: boolean;
}

export function createDestinationRefreshSnapshot(): DestinationRefreshSnapshot {
  return { confirmedKey: "", requestedKey: "", failedKey: "", inFlight: false };
}

/** شروع درخواست برای یک هویت: فقط «درخواست‌شده» را جلو می‌برد، هرگز «تأییدشده» را. */
export function beginDestinationRefresh(
  snapshot: DestinationRefreshSnapshot,
  key: string,
): DestinationRefreshSnapshot {
  return {
    confirmedKey: snapshot.confirmedKey,
    requestedKey: key,
    // همان کلید دوباره تلاش می‌شود؟ قفل شکست فقط برای همین تلاش برداشته می‌شود.
    failedKey: snapshot.failedKey === key ? "" : snapshot.failedKey,
    inFlight: true,
  };
}

/** پاسخ موفق: هویت تأییدشده فقط با همین کلید جلو می‌رود و قفل شکست همان کلید پاک می‌شود. */
export function completeDestinationRefresh(
  snapshot: DestinationRefreshSnapshot,
  key: string,
): DestinationRefreshSnapshot {
  if (snapshot.requestedKey !== key) return snapshot;
  return {
    confirmedKey: key,
    requestedKey: "",
    failedKey: snapshot.failedKey === key ? "" : snapshot.failedKey,
    inFlight: false,
  };
}

/** خطای همان درخواست: کلید شکست‌خورده ثبت می‌شود و «تأییدشدهٔ» قبلی دست‌نخورده می‌ماند. */
export function failDestinationRefresh(
  snapshot: DestinationRefreshSnapshot,
  key: string,
): DestinationRefreshSnapshot {
  if (snapshot.requestedKey !== key) return snapshot;
  return {
    confirmedKey: snapshot.confirmedKey,
    requestedKey: "",
    failedKey: key,
    inFlight: false,
  };
}

/** Retry فقط برای همان کلید شکست‌خورده و فقط یک‌بار (کلیک تکراری درخواست دوم نمی‌سازد). */
export function canRetryDestinationRefresh(
  snapshot: DestinationRefreshSnapshot,
  currentKey: string,
): boolean {
  return !snapshot.inFlight && snapshot.failedKey !== "" && snapshot.failedKey === currentKey;
}

export type DestinationPreviewMode = "current" | "pending" | "failed";

/**
 * وضعیت نمایش کارت‌ها نسبت به مقصد فعلی.
 * - `current`: پیش‌نمایش موجود دقیقاً برای همین هویت است.
 * - `pending`: پاسخ تازه در راه است یا هویت عوض شده؛ پول قبلی نباید «جاری» دیده شود.
 * - `failed`: تلاش برای همین هویت شکست خورده؛ فقط Retry صریح.
 */
export function resolveDestinationPreviewMode(
  snapshot: DestinationRefreshSnapshot,
  currentKey: string,
): DestinationPreviewMode {
  if (snapshot.inFlight) return "pending";
  if (snapshot.failedKey !== "" && snapshot.failedKey === currentKey) return "failed";
  return snapshot.confirmedKey === currentKey ? "current" : "pending";
}

export type DestinationRefreshAction = "skip" | "request";

export interface DestinationRefreshPlan {
  action: DestinationRefreshAction;
  /** هویت دقیقی که این درخواست برایش فرستاده می‌شود (کلید پایدار). */
  key: string;
  /** مقصد دقیقی که باید فرستاده شود؛ `null` یعنی پاک‌شدن هویت و ارسال بدون مقصد. */
  destination: GeographyDestinationIdentifiers | null;
  /**
   * ورودی تایپ‌شدهٔ گیرنده/آدرس هرگز با پاسخ مقصد بازنویسی نمی‌شود؛ این تصمیم صریح و آزمون‌پذیر است.
   */
  preserveTypedAddress: boolean;
}

const SKIP: DestinationRefreshPlan = {
  action: "skip",
  key: "",
  destination: null,
  preserveTypedAddress: true,
};

/**
 * آیا این تغییر مقصد باید درخواست تازه بسازد.
 *
 * فقط وقتی پیش‌نمایش capture فعال است اجرا می‌شود؛ حالت خاموش (legacy) هیچ رفتاری عوض نمی‌کند.
 * کلید تأییدشده/درخواست‌شده/شکست‌خورده هیچ درخواستی نمی‌سازد: رندر نامرتبط، درخواست هم‌زمان دوم و
 * «تلاش خودکار» پس از شکست همه ممنوع‌اند (شکست فقط با Retry صریح تکرار می‌شود).
 */
export function planDestinationRefresh(input: {
  capturedActive: boolean;
  snapshot: DestinationRefreshSnapshot;
  destinationKey: string;
  destination: GeographyDestinationIdentifiers | null;
}): DestinationRefreshPlan {
  const { capturedActive, snapshot, destinationKey, destination } = input;
  if (!capturedActive) return SKIP;
  if (snapshot.inFlight) return SKIP;
  // هویتِ تأییدشده همان پاسخ فعلی است؛ هیچ درخواست تازه‌ای لازم نیست.
  if (destinationKey === snapshot.confirmedKey) return SKIP;
  // شکست فقط با Retry صریح تکرار می‌شود؛ رندر/تغییر نامرتبط هرگز تلاش خودکار نمی‌سازد.
  if (snapshot.failedKey !== "" && destinationKey === snapshot.failedKey) return SKIP;
  return {
    action: "request",
    key: destinationKey,
    // هویت پاک‌شده = درخواست بدون مقصد (پاسخ صادقانهٔ سرور، نه قیمت قدیمی).
    destination: destinationKey === "" ? null : destination,
    preserveTypedAddress: true,
  };
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
