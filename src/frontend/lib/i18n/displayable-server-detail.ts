import { type Locale } from "./locale.ts";

/**
 * واژگان فنی داخلی (رزرو موجودی، نام استثنا، کدهای داخلی) که هرگز نباید به مشتری نمایش داده شود.
 */
const TECHNICAL_DETAIL = /Held|reservation|رزرو|آزادسازی|Exception|Npgsql|Postgres|Microsoft\.|System\.|Tooba\.|\bat\s|PRICE_CHANGED|TAX_/i;

const PERSIAN_SCRIPT = /[\u0600-\u06FF]/;
const LATIN_SCRIPT = /[A-Za-z]/;

/**
 * آیا متن خطای سرور برای نمایش مستقیم به مشتری در locale فعال قابل استفاده است؟
 *
 * سرور زبان عنوان/جزئیات را از هدر Accept-Language می‌گیرد؛ اگر آن هدر نرسد یا کلید
 * ترجمه نداشته باشد، متن انگلیسی برمی‌گردد. UI چندزبانه نباید متن زبان دیگر را نشت دهد،
 * پس متن فقط وقتی پذیرفته می‌شود که به خط locale فعال نوشته شده و واژهٔ فنی داخلی نداشته باشد.
 */
export function isDisplayableServerDetail(detail: string | null | undefined, locale: Locale): boolean {
  const value = detail?.trim();
  if (!value) {
    return false;
  }
  if (TECHNICAL_DETAIL.test(value)) {
    return false;
  }
  return locale === "fa"
    ? PERSIAN_SCRIPT.test(value)
    : LATIN_SCRIPT.test(value) && !PERSIAN_SCRIPT.test(value);
}
