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
