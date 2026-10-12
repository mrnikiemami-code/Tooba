import { type Locale } from "../../lib/i18n/locale.ts";

/**
 * متن مشتری‌پسند کدهای خطای سبد/تسویه در زبان‌های پشتیبانی‌شده.
 * متن‌ها از کلاینت HTTP جدا نگه داشته می‌شوند تا هر دو زبان صریح و قابل بازبینی باشند
 * و هیچ‌گاه متن خام سرور (که ممکن است به زبان دیگر برگردد) به مشتری نشت نکند.
 */
const CART_ERROR_TEXT: Readonly<Record<string, readonly [fa: string, en: string]>> = {
  "cart.generic": [
    "عملیات سبد انجام نشد. لطفاً دوباره تلاش کنید.",
    "The cart operation failed. Please try again.",
  ],
  "cart.failed": ["عملیات سبد شکست خورد.", "The cart operation failed."],
  "cart.missing": ["سبد خرید پیدا نشد.", "The cart was not found."],
  "cart.expired": ["سبد خرید منقضی شده است.", "The cart has expired."],
  "cart.version.conflict": [
    "سبد هم‌زمان به‌روز شده است. صفحه را تازه کنید.",
    "The cart was updated at the same time. Refresh the page.",
  ],
  "cart.inventory.insufficient": [
    "تعداد انتخاب‌شده بیشتر از موجودی قابل فروش است.",
    "The selected quantity exceeds sellable inventory.",
  ],
  "cart.inventory.stale": [
    "موجودی این کالا تغییر کرده است. لطفاً تعداد را دوباره بررسی کنید.",
    "Inventory changed. Review the quantity and try again.",
  ],
  "cart.quantity.invalid": ["تعداد انتخاب‌شده معتبر نیست.", "The selected quantity is not valid."],
  "cart.offer.unavailable": [
    "این کالا در حال حاضر قابل افزودن به سبد نیست.",
    "This item cannot be added to the cart right now.",
  ],
};

/**
 * متن کد خطای سبد در locale فعال؛ ناشناخته → متن عمومی «cart.generic».
 */
export function cartErrorText(code: string | null | undefined, locale: Locale): string {
  const entry = (code ? CART_ERROR_TEXT[code] : undefined) ?? CART_ERROR_TEXT["cart.generic"]!;
  return locale === "fa" ? entry[0] : entry[1];
}
