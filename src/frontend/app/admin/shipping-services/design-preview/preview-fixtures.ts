/**
 * TB-SHIPPING-ADMIN-UI-PREVIEW-186 — دادهٔ نمونهٔ صریح پیش‌نمایش.
 *
 * همهٔ ردیف‌ها SAMPLE هستند: نام روش، نوع، مناطق، اعداد و مبالغ هیچ ارزش تجاری، seed یا
 * تصمیم قیمت‌گذاری نیستند. این فایل هیچ شبکه/ذخیره‌سازی/Prisma/EF ندارد و فقط برای بازبینی
 * ظاهر و تعامل در حافظه خوانده می‌شود.
 */

import {
  PREVIEW_WEEKDAY_IDS,
  type PreviewDayHours,
  type PreviewMethod,
  type PreviewSchedule,
  type PreviewWeekdayId,
} from "./preview-model.ts";

type SampleWindow = { fromTime: string; toTime: string };

/**
 * نمونهٔ صریح برنامهٔ هفتگی: هر روز مستقل و بازهٔ هر روز فقط همان روز است.
 * روز تعطیل ساعت پیش‌فرض نمونه را نگه می‌دارد تا فعال‌کردن دوباره‌اش خالی نباشد.
 */
function sampleSchedule(
  open: Partial<Record<PreviewWeekdayId, SampleWindow>>,
  cutoffHours: number,
): PreviewSchedule {
  const hours = {} as Record<PreviewWeekdayId, PreviewDayHours>;
  for (const day of PREVIEW_WEEKDAY_IDS) {
    const window = open[day];
    hours[day] = {
      working: window !== undefined,
      fromTime: window?.fromTime ?? "09:00",
      toTime: window?.toTime ?? "18:00",
    };
  }
  return { hours, cutoffHours };
}

/** نمونهٔ پایه: شنبه تا چهارشنبه ۰۹:۰۰–۱۸:۰۰، پنجشنبه و جمعه تعطیل. */
const SCHEDULE_STANDARD = sampleSchedule(
  {
    sat: { fromTime: "09:00", toTime: "18:00" },
    sun: { fromTime: "09:00", toTime: "18:00" },
    mon: { fromTime: "09:00", toTime: "18:00" },
    tue: { fromTime: "09:00", toTime: "18:00" },
    wed: { fromTime: "09:00", toTime: "18:00" },
  },
  4,
);

/**
 * نمونهٔ درخواست مالک: شنبه ۰۹:۰۰–۱۳:۰۰، یکشنبه ۱۵:۰۰–۱۸:۰۰، جمعه تعطیل.
 * تفاوت ساعت روزها صرفاً نمونهٔ نمایشی است، نه ساعات واقعی کسب‌وکار.
 */
const SCHEDULE_MIXED_DAYS = sampleSchedule(
  {
    sat: { fromTime: "09:00", toTime: "13:00" },
    sun: { fromTime: "15:00", toTime: "18:00" },
    mon: { fromTime: "09:00", toTime: "18:00" },
    tue: { fromTime: "10:00", toTime: "14:00" },
    wed: { fromTime: "09:00", toTime: "12:30" },
    thu: { fromTime: "16:00", toTime: "19:00" },
  },
  4,
);

/** نمونهٔ گسترده: شنبه تا پنجشنبه ۰۸:۰۰–۲۰:۰۰، جمعه تعطیل. */
const SCHEDULE_EXTENDED = sampleSchedule(
  {
    sat: { fromTime: "08:00", toTime: "20:00" },
    sun: { fromTime: "08:00", toTime: "20:00" },
    mon: { fromTime: "08:00", toTime: "20:00" },
    tue: { fromTime: "08:00", toTime: "20:00" },
    wed: { fromTime: "08:00", toTime: "20:00" },
    thu: { fromTime: "08:00", toTime: "20:00" },
  },
  6,
);

export const PREVIEW_METHOD_FIXTURES: readonly PreviewMethod[] = [
  {
    id: "method-express",
    code: "preview:express",
    ownerKind: "marketplace",
    shippingTypeId: "ordinary",
    names: { fa: "ارسال سریع درون‌شهری", en: "Same-day city delivery" },
    descriptions: {
      fa: "تحویل در همان روز برای سفارش‌های ثبت‌شده تا ساعت ۱۲.",
      en: "Same-day delivery for orders placed before noon.",
    },
    freightPayerMode: "prepaid",
    calculationMode: "manual",
    status: "active",
    carrierId: null,
    schedule: SCHEDULE_MIXED_DAYS,
    regionGroups: [
      {
        id: "group-tehran-qom",
        titles: { fa: "تهران و قم", en: "Tehran and Qom" },
        places: ["place-ir-23", "place-ir-25"],
        rows: [
          {
            id: "row-express-1",
            weightGrams: { from: 1000, to: 2000 },
            itemCount: { from: 1, to: 4 },
            merchandiseAmount: null,
            freight: 800000,
            packaging: 120000,
            delivery: { minDays: 1, maxDays: 1 },
          },
          {
            id: "row-express-2",
            weightGrams: { from: 1000, to: 2000 },
            itemCount: { from: 4, to: 11 },
            merchandiseAmount: null,
            freight: 1200000,
            packaging: 180000,
            delivery: { minDays: 1, maxDays: 1 },
          },
          {
            id: "row-express-3",
            weightGrams: { from: 2000, to: 5000 },
            itemCount: { from: 4, to: 11 },
            merchandiseAmount: { from: 5000000, to: 10000000 },
            freight: 1800000,
            packaging: 240000,
            delivery: { minDays: 1, maxDays: 2 },
          },
        ],
      },
    ],
  },
  {
    id: "method-heavy",
    code: "preview:heavy",
    ownerKind: "marketplace",
    shippingTypeId: "heavy",
    names: { fa: "باربری اقلام سنگین", en: "Heavy goods freight" },
    descriptions: {
      fa: "مناسب اقلام سنگین با هماهنگی تلفنی پیش از ارسال.",
      en: "For heavy items, coordinated by phone before dispatch.",
    },
    freightPayerMode: "collect",
    calculationMode: "manual",
    status: "active",
    carrierId: null,
    schedule: SCHEDULE_STANDARD,
    regionGroups: [
      {
        id: "group-isfahan",
        titles: { fa: "اصفهان و حومه", en: "Isfahan and suburbs" },
        places: ["place-ir-10-glp"],
        rows: [
          {
            id: "row-heavy-1",
            weightGrams: { from: 20000, to: null },
            itemCount: null,
            merchandiseAmount: null,
            freight: 4500000,
            packaging: 600000,
            delivery: { minDays: 2, maxDays: 4 },
          },
        ],
      },
    ],
  },
  {
    id: "method-carrier",
    code: "preview:carrier",
    ownerKind: "marketplace",
    shippingTypeId: "ordinary",
    names: { fa: "ارسال با نرخ حامل", en: "Carrier-rate delivery" },
    descriptions: { fa: "", en: "" },
    freightPayerMode: "prepaid",
    calculationMode: "carrier",
    status: "active",
    carrierId: "carrier-post",
    schedule: SCHEDULE_EXTENDED,
    regionGroups: [
      {
        id: "group-country",
        titles: { fa: "سراسر کشور", en: "Nationwide" },
        places: ["place-ir"],
        rows: [
          {
            id: "row-carrier-1",
            weightGrams: { from: null, to: 30000 },
            itemCount: null,
            merchandiseAmount: null,
            freight: 0,
            packaging: 150000,
            delivery: { minDays: 2, maxDays: 5 },
          },
        ],
      },
    ],
  },
  {
    id: "method-free",
    code: "preview:free",
    ownerKind: "marketplace",
    shippingTypeId: "grocery",
    names: { fa: "ارسال رایگان سوپرمارکتی", en: "Free grocery delivery" },
    descriptions: {
      fa: "سفارش‌های سوپرمارکتی در محدودهٔ نمونه، بدون هزینهٔ ارسال.",
      en: "Sample-area grocery orders with no shipping charge.",
    },
    freightPayerMode: "prepaid",
    calculationMode: "free",
    status: "inactive",
    carrierId: null,
    schedule: SCHEDULE_EXTENDED,
    regionGroups: [],
  },
  {
    id: "method-extra-heavy",
    code: "preview:extra-heavy",
    ownerKind: "seller",
    shippingTypeId: "extra-heavy",
    names: { fa: "حمل فوق‌سنگین (پیش‌نویس)", en: "Extra-heavy haul (draft)" },
    descriptions: { fa: "", en: "" },
    freightPayerMode: "collect",
    calculationMode: "manual",
    status: "inactive",
    carrierId: null,
    schedule: SCHEDULE_STANDARD,
    // مالک مستقل (فروشنده) می‌تواند همان منطقهٔ روش مارکت‌پلیس را داشته باشد؛
    // دامنهٔ منع تعارض فقط داخل همان مالک و همان روش است.
    regionGroups: [
      {
        id: "group-seller-tehran",
        titles: { fa: "تهران — ناحیهٔ فروشنده", en: "Tehran — seller area" },
        places: ["place-ir-23-thr-1"],
        rows: [
          {
            id: "row-extra-heavy-1",
            weightGrams: { from: 50000, to: null },
            itemCount: null,
            merchandiseAmount: null,
            freight: 6500000,
            packaging: 900000,
            delivery: { minDays: 3, maxDays: 7 },
          },
        ],
      },
    ],
  },
];

/** روش نمایشی که در حالت اولیه باز می‌شود. */
export const PREVIEW_OPENING_METHOD_ID = "method-express";
