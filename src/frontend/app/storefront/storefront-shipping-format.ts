import { formatJalaliDate, isoToJalaliDisplay } from "../../design-system/app-data-grid/jalali.ts";
import type { StorefrontDeliveryDateOption } from "./storefront-shipping-api.ts";

/** ارقام فارسی برای نمایش مبلغ/تعداد در سطح ارسال. */
export function toPersianDigits(n: string | number): string {
  return String(n).replace(/\d/g, (d) => "۰۱۲۳۴۵۶۷۸۹"[Number(d)]!);
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
export function deliveryChipParts(option: StorefrontDeliveryDateOption): {
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
