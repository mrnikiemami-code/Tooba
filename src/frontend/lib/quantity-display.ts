/**
 * نمایش مقدار کالا بدون صفرهای ذخیره‌سازی؛ parseInt استفاده نمی‌شود.
 */
export function formatQuantityDisplay(value: number, decimalPlaces = 6): string {
  if (!Number.isFinite(value)) {
    return "";
  }
  const places = Math.min(6, Math.max(0, decimalPlaces));
  const raw = value.toFixed(places);
  return raw.replace(/\.?0+$/, "");
}

const PERSIAN_DIGITS = ["۰", "۱", "۲", "۳", "۴", "۵", "۶", "۷", "۸", "۹"] as const;
const PERSIAN_DECIMAL_SEPARATOR = "٫";

/**
 * نمایش مقدار برای اپراتور فارسی‌زبان: ارقام فارسی و جداکنندهٔ اعشار فارسی («۲٫۵»).
 *
 * فقط لایهٔ نمایش است؛ مقدار داده و مقدار ورودی‌ها دست‌نخورده می‌ماند، چون `parseQuantityInput`
 * ارقام لاتین را می‌خواند.
 */
export function formatQuantityDisplayFa(value: number, decimalPlaces = 6): string {
  const latin = formatQuantityDisplay(value, decimalPlaces);
  if (!latin) {
    return latin;
  }
  return latin
    .replace(/\./g, PERSIAN_DECIMAL_SEPARATOR)
    .replace(/\d/g, (digit) => PERSIAN_DIGITS[Number(digit)] ?? digit);
}

/**
 * ورودی مقدار اعشاری را بدون parseInt می‌خواند.
 */
export function parseQuantityInput(raw: string): number | null {
  const text = raw.trim().replace(",", ".");
  if (!text || !/^\d+(\.\d+)?$/.test(text)) {
    return null;
  }
  const value = Number(text);
  return Number.isFinite(value) && value > 0 ? value : null;
}
