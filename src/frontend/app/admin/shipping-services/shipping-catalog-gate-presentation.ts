/**
 * نمایش واحدِ شکستِ خواندنِ کاتالوگ یک‌سطحی روش‌های ارسال.
 *
 * چرا این لایه جدا است: پیش‌تر وضعیت دروازه و خطای گذرا در یک رشته خلاصه می‌شد و UI با
 * مقایسهٔ رشته‌ای تصمیم می‌گرفت؛ نتیجه این بود که «دروازهٔ خاموش در این محیط» هم به‌شکل
 * «بارگذاری ناموفق + تلاش دوباره» دیده می‌شد، در حالی که تلاش دوباره هرگز نمی‌تواند آن را
 * برطرف کند. این ماژول ناب و مستقل از React است تا:
 *
 *   - کد شکست یک قرارداد بسته و قابل‌گسترش باشد (افزودن کد تازه = یک ورودی تازه در نقشه)؛
 *   - «تلاش دوباره» فقط برای شکست‌های گذرا پیشنهاد شود؛
 *   - هیچ متن فارسی در چند نقطه تکرار یا با برچسب دکمه اشتباه گرفته نشود.
 */

/** کدهای پایدار شکست خواندن کاتالوگ؛ همان مقادیری که لایهٔ API برمی‌گرداند. */
export type ShippingCatalogFailureCode =
  | "admin.authorization.denied"
  | "shipping_catalog.gate_disabled"
  | "shipping_catalog.not_ready"
  | "shipping_catalog.invalid_response"
  | "shipping_catalog.load_failed"
  | "host-unreachable";

/** نمای آمادهٔ نمایش یک شکست؛ `retryable` تنها مجوز نمایش دکمهٔ تلاش دوباره است. */
export interface ShippingCatalogFailureView {
  readonly title: string;
  readonly detail: string;
  readonly retryable: boolean;
}

const VIEWS: Record<ShippingCatalogFailureCode, ShippingCatalogFailureView> = {
  "admin.authorization.denied": {
    title: "دسترسی ندارید",
    detail:
      "هویت مدیر تأیید نشد؛ فهرست روش‌های ارسال نمایش داده نمی‌شود. اگر با نقش مدیر وارد شده‌اید، دوباره تلاش کنید.",
    retryable: true,
  },
  "shipping_catalog.gate_disabled": {
    title: "کاتالوگ روش‌های ارسال در این محیط فعال نیست",
    detail:
      "این قابلیت با پرچم پیکربندی سرور (Tooba:ShippingOneLevelCatalog:Enabled) روشن می‌شود. تا فعال‌شدن آن در این محیط، فهرست روش‌های ارسال نمایش داده نمی‌شود و تلاش دوباره نتیجه‌ای ندارد.",
    retryable: false,
  },
  "shipping_catalog.not_ready": {
    title: "کاتالوگ روش‌های ارسال هنوز آماده نیست",
    detail: "مهاجرت کاتالوگ یک‌سطحی در این محیط کامل نشده است؛ پس از تکمیل مهاجرت دوباره تلاش کنید.",
    retryable: true,
  },
  "shipping_catalog.invalid_response": {
    title: "پاسخ سرور برای کاتالوگ روش‌های ارسال معتبر نبود",
    detail: "پاسخ خوانده‌شده با قرارداد کاتالوگ یک‌سطحی سازگار نیست؛ دوباره تلاش کنید و در صورت تکرار آن را گزارش کنید.",
    retryable: true,
  },
  "shipping_catalog.load_failed": {
    title: "بارگذاری روش‌های ارسال ناموفق بود",
    detail: "سرور فهرست روش‌های ارسال را برنگرداند؛ دوباره تلاش کنید.",
    retryable: true,
  },
  "host-unreachable": {
    title: "ارتباط با سرور برقرار نشد",
    detail: "اتصال به سرویس‌دهنده برقرار نشد؛ وضعیت شبکه یا اجرای سرور را بررسی و دوباره تلاش کنید.",
    retryable: true,
  },
};

/**
 * کد شکست را به نمای نمایشی تبدیل می‌کند. کد ناشناخته/تهی به شکست عمومیِ خواندن نگاشت می‌شود؛
 * هرگز متن دکمه به‌جای توضیح نمایش داده نمی‌شود.
 */
export function presentShippingCatalogFailure(
  code: string | null | undefined,
): ShippingCatalogFailureView {
  if (code && Object.prototype.hasOwnProperty.call(VIEWS, code)) {
    return VIEWS[code as ShippingCatalogFailureCode];
  }
  return VIEWS["shipping_catalog.load_failed"];
}
