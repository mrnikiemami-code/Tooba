import type { Locale } from "./locale.ts";

/**
 * رشته‌های chrome مشترک fa/en.
 * سبک کلید هم‌راستا با design-system/workspace/messages.ts (retry/empty/error).
 */
export type ChromeMessages = {
  localeFa: string;
  localeEn: string;
  localeSwitcherLabel: string;
  empty: string;
  error: string;
  retry: string;
  notFound: string;
};

export const faChromeMessages: ChromeMessages = {
  localeFa: "فا",
  localeEn: "EN",
  localeSwitcherLabel: "زبان",
  empty: "موردی نیست",
  error: "خطا",
  retry: "تلاش دوباره",
  notFound: "یافت نشد",
};

export const enChromeMessages: ChromeMessages = {
  localeFa: "FA",
  localeEn: "EN",
  localeSwitcherLabel: "Language",
  empty: "Nothing here",
  error: "Error",
  retry: "Retry",
  notFound: "Not found",
};

export function chromeMessagesFor(locale: Locale): ChromeMessages {
  return locale === "en" ? enChromeMessages : faChromeMessages;
}

/** برچسب‌های سطح تحویل ویترین که روی کارت/نوبت لمس‌شده‌اند (TB-243). */
export type ShippingDeliveryMessages = {
  deliveryHourLegend: string;
  deliveryHourAria: string;
  pickOneHour: string;
  deliveryDay: string;
  pickDateFirst: string;
  /** TB-251: روز انتخاب‌شده نوبت قابل‌انتخاب ندارد (تهی/نامعتبر/غایب). */
  slotsUnavailable: string;
  /** TB-251: شکل نوبت‌های همان روز از سرور قابل اعتماد نیست. */
  slotsMalformed: string;
  yourChoice: string;
  hourNotChosen: string;
  nearest: string;
  groupDeliveryTime: string;
  notChosenYet: string;
  pickGroupDate: string;
  freightUnknown: string;
  freightPrefix: string;
  packagingPrefix: string;
  goodsAmountUnknown: string;
  typeUnset: string;
  beforeMinimum: string;
};

export const faShippingDeliveryMessages: ShippingDeliveryMessages = {
  deliveryHourLegend: "ساعت تحویل",
  deliveryHourAria: "ساعت تحویل",
  pickOneHour: "یکی از ساعت‌های بالا را انتخاب کنید.",
  deliveryDay: "روز تحویل",
  pickDateFirst: "ابتدا یکی از روزهای بالا را انتخاب کنید.",
  slotsUnavailable: "برای این روز ساعت تحویل مجاز اعلام نشده است؛ روش یا مقصد این گروه را بررسی کنید.",
  slotsMalformed: "ساعت‌های تحویل این روز قابل تأیید نیست؛ صفحه را تازه کنید یا روش دیگری را انتخاب کنید.",
  yourChoice: "انتخاب شما:",
  hourNotChosen: "هنوز ساعت تحویل انتخاب نشده",
  nearest: "نزدیک‌ترین",
  groupDeliveryTime: "زمان تحویل این گروه:",
  notChosenYet: "هنوز انتخاب نشده",
  pickGroupDate: "برای این گروه یکی از تاریخ‌های هر روش را انتخاب کنید.",
  freightUnknown: "کرایه نامشخص",
  freightPrefix: "کرایه",
  packagingPrefix: "بسته‌بندی",
  goodsAmountUnknown: "مبلغ کالا نامشخص",
  typeUnset: "نوع ارسال تعیین‌نشده",
  beforeMinimum: "قبل از حداقل زمان فروشگاه مجاز نیست",
};

export const enShippingDeliveryMessages: ShippingDeliveryMessages = {
  deliveryHourLegend: "Delivery time",
  deliveryHourAria: "Delivery time",
  pickOneHour: "Choose one of the times above.",
  deliveryDay: "Delivery day",
  pickDateFirst: "Choose one of the days above first.",
  slotsUnavailable: "No lawful delivery times were published for this day; check this group's method or destination.",
  slotsMalformed: "Delivery times for this day could not be verified; refresh or choose another method.",
  yourChoice: "Your choice:",
  hourNotChosen: "Delivery time not selected yet",
  nearest: "Earliest",
  groupDeliveryTime: "Delivery time for this group:",
  notChosenYet: "Not selected yet",
  pickGroupDate: "Choose a delivery date for one of the methods in this group.",
  freightUnknown: "Freight unknown",
  freightPrefix: "Freight",
  packagingPrefix: "Packaging",
  goodsAmountUnknown: "Goods amount unknown",
  typeUnset: "Shipping type unset",
  beforeMinimum: "Not allowed before the store minimum",
};

export function shippingDeliveryMessagesFor(locale: Locale): ShippingDeliveryMessages {
  return locale === "en" ? enShippingDeliveryMessages : faShippingDeliveryMessages;
}
