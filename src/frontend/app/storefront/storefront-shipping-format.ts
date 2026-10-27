import { formatJalaliDate, isoToJalaliDisplay } from "../../design-system/app-data-grid/jalali.ts";

/** ارقام فارسی برای نمایش مبلغ/تعداد در سطح ارسال. */
export function toPersianDigits(n: string | number): string {
  return String(n).replace(/\d/g, (d) => "۰۱۲۳۴۵۶۷۸۹"[Number(d)]!);
}

/**
 * TB-229-R1: نمایش **متن اعشاری دقیق** یک مبلغ capture‌شده.
 *
 * فقط ارقام صحیح را سه‌رقمی گروه می‌کند و بخش اعشاری را عیناً همان‌طور که سرور فرستاده نگه می‌دارد؛
 * هیچ `Number`/`parseFloat`، گردکردن، حذف اعشار یا تبدیل ارز انجام نمی‌شود. `null` یعنی نامشخص.
 */
export function formatExactAmountText(text: string | null, currency: string | null): string | null {
  if (text === null) return null;
  const match = /^(\d+)(\.\d+)?$/.exec(text);
  if (!match) return null;
  const grouped = match[1]!.replace(/\B(?=(\d{3})+(?!\d))/g, ",");
  const exact = `${grouped}${match[2] ?? ""}`;
  const digits = toPersianDigits(exact);
  if (!currency) return digits;
  return currency === "IRR" ? `${digits} ریال` : `${digits} ${currency}`;
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

/** چیپ تاریخ: روز هفته از سرور، روز و ماه جلالی فقط یک‌بار. */
export function deliveryChipParts(option: { value: string; label: string; subLabel: string }): {
  weekday: string;
  day: string;
  month: string;
} {
  const jalaliFull = formatJalaliDate(`${option.value}T12:00:00`, "fa");
  const parts = isoToJalaliDisplay(`${option.value}T12:00:00`);
  const weekday =
    option.label && option.label !== jalaliFull && option.label !== option.subLabel ? option.label : "روز";
  return {
    weekday,
    day: parts ? toPersianDigits(parts.day) : option.subLabel,
    month: parts ? JALALI_MONTHS[parts.month - 1] ?? "" : "",
  };
}
