/**
 * یک منبع واحد برای تبدیل ارقام لاتین به ارقام فارسی در لایهٔ نمایش.
 *
 * فقط نمایش را عوض می‌کند: مقدار داده، اکسسور ستون، جستجو، فیلتر، مرتب‌سازی و خروجی CSV
 * همچنان با ارقام لاتین کار می‌کنند. هر ماژولی که متن فارسیِ اپراتوری می‌سازد باید از همین
 * تابع استفاده کند تا دو نگاشت موازی از ارقام در پروژه نماند.
 */

export const PERSIAN_DIGITS = ["۰", "۱", "۲", "۳", "۴", "۵", "۶", "۷", "۸", "۹"] as const;

/** جداکنندهٔ اعشار فارسی؛ ورودی‌های عددی همچنان `.` لاتین می‌گیرند. */
export const PERSIAN_DECIMAL_SEPARATOR = "٫";

/**
 * ارقام لاتین یک متن یا عدد را فارسی می‌کند. هر کاراکتر دیگر (حروف، خط تیره، ممیز) دست‌نخورده
 * می‌ماند، پس یک کد فنی شکل خودش را از دست نمی‌دهد و فقط ارقامش فارسی می‌شود.
 */
export function toPersianDigits(value: string | number | null | undefined): string {
  if (value === null || value === undefined) {
    return "";
  }
  return String(value).replace(/\d/g, (digit) => PERSIAN_DIGITS[Number(digit)] ?? digit);
}

const PERSIAN_DIGIT_RANGES = [
  { from: 0x06f0, to: 0x06f9 }, // ۰…۹
  { from: 0x0660, to: 0x0669 }, // ٠…٩
] as const;

/**
 * مسیر برگشت: ارقام فارسی (و عربی) به لاتین.
 *
 * وقتی یک کد فنی روی صفحه با ارقام فارسی دیده می‌شود، اپراتور آن را کپی می‌کند و در جستجو/فیلتر
 * می‌چسباند؛ سرور همان کد را با ارقام لاتین ذخیره کرده است، بنابراین مقدارِ ارسالی به سرور باید
 * دوباره لاتین شود. مقدار ذخیره‌شده و قرارداد جستجو تغییر نمی‌کند.
 */
export function fromPersianDigits(value: string): string {
  let result = "";
  for (const char of value) {
    const code = char.codePointAt(0) ?? 0;
    const range = PERSIAN_DIGIT_RANGES.find((item) => code >= item.from && code <= item.to);
    result += range ? String(code - range.from) : char;
  }
  return result;
}
