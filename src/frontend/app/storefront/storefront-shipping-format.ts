import { formatJalaliDate, isoToJalaliDisplay } from "../../design-system/app-data-grid/jalali.ts";
import type { Locale } from "../../lib/i18n/locale.ts";

/** ارقام فارسی برای نمایش مبلغ/تعداد در سطح ارسال. */
export function toPersianDigits(n: string | number): string {
  return String(n).replace(/\d/g, (d) => "۰۱۲۳۴۵۶۷۸۹"[Number(d)]!);
}

/** ارقام محلی: fa → فارسی، en → لاتین. */
export function toLocaleDigits(n: string | number, locale: Locale): string {
  return locale === "fa" ? toPersianDigits(n) : String(n);
}

/**
 * TB-229-R1: نمایش **متن اعشاری دقیق** یک مبلغ capture‌شده.
 *
 * فقط ارقام صحیح را سه‌رقمی گروه می‌کند و بخش اعشاری را عیناً همان‌طور که سرور فرستاده نگه می‌دارد؛
 * هیچ `Number`/`parseFloat`، گردکردن، حذف اعشار یا تبدیل ارز انجام نمی‌شود. `null` یعنی نامشخص.
 *
 * TB-230-R1: ریال ایران ارز صحیح است؛ اگر بخش اعشاری **کاملاً صفر** باشد (120000.00) همان عدد صحیح با
 * گروه‌بندی و ارقام فارسی نمایش داده می‌شود و توکن خام دست‌نخورده می‌ماند. اعشار غیرصفر هرگز
 * گرد/بریده نمی‌شود و عیناً نمایش داده می‌شود؛ ارزهای دیگر متن اعشاری فعلی خود را نگه می‌دارند.
 */
export function formatExactAmountText(
  text: string | null,
  currency: string | null,
  locale: Locale = "fa",
): string | null {
  if (text === null) return null;
  const match = /^(\d+)(\.\d+)?$/.exec(text);
  if (!match) return null;
  const grouped = match[1]!.replace(/\B(?=(\d{3})+(?!\d))/g, ",");
  const fraction = match[2] ?? "";
  // فقط «همه‌ی اعشار صفر» در ریال حذف می‌شود؛ صفر صریح (0.00) هم همین‌طور معتبر و صفر می‌ماند.
  const integralCurrencyWithZeroFraction =
    currency === "IRR" && fraction !== "" && /^\.0+$/.test(fraction);
  const exact = integralCurrencyWithZeroFraction ? grouped : `${grouped}${fraction}`;
  const digits = toLocaleDigits(exact, locale);
  if (!currency) return digits;
  if (currency === "IRR") {
    return locale === "fa" ? `${digits} ریال` : `${digits} IRR`;
  }
  return `${digits} ${currency}`;
}

const JALALI_MONTHS = [
  "فروردین",
  "اردیبهشت",
  "خرداد",
  "تیر",
  "مرداد",
  "شهریور",
  "مهر",
  "آبان",
  "آذر",
  "دی",
  "بهمن",
  "اسفند",
];

const GREGORIAN_MONTHS_EN = [
  "Jan",
  "Feb",
  "Mar",
  "Apr",
  "May",
  "Jun",
  "Jul",
  "Aug",
  "Sep",
  "Oct",
  "Nov",
  "Dec",
];

/** چیپ تاریخ: روز هفته از سرور، روز و ماه مطابق locale پیکربندی‌شده. */
export function deliveryChipParts(
  option: { value: string; label: string; subLabel: string },
  locale: Locale = "fa",
): {
  weekday: string;
  day: string;
  month: string;
} {
  const iso = `${option.value}T12:00:00`;
  if (locale === "en") {
    const d = new Date(iso);
    const weekday =
      option.label && option.label !== option.subLabel
        ? option.label
        : new Intl.DateTimeFormat("en-US", { weekday: "short" }).format(d);
    return {
      weekday,
      day: String(d.getDate()),
      month: GREGORIAN_MONTHS_EN[d.getMonth()] ?? "",
    };
  }

  const jalaliFull = formatJalaliDate(iso, "fa");
  const parts = isoToJalaliDisplay(iso);
  const weekday =
    option.label && option.label !== jalaliFull && option.label !== option.subLabel ? option.label : "روز";
  return {
    weekday,
    day: parts ? toPersianDigits(parts.day) : option.subLabel,
    month: parts ? JALALI_MONTHS[parts.month - 1] ?? "" : "",
  };
}

/** برچسب تاریخ انتخاب‌شده برای خلاصهٔ گروه. */
export function formatShippingChoiceDate(isoDate: string, locale: Locale): string {
  return formatJalaliDate(`${isoDate}T12:00:00`, locale);
}
