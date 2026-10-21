/**
 * TB-SHIPPING-ADMIN-UI-PREVIEW-186 — دادهٔ نمونهٔ صریح پیش‌نمایش.
 *
 * همهٔ ردیف‌ها SAMPLE هستند: نام روش، نوع، مناطق، اعداد و مبالغ هیچ ارزش تجاری، seed یا
 * تصمیم قیمت‌گذاری نیستند. این فایل هیچ شبکه/ذخیره‌سازی/Prisma/EF ندارد و فقط برای بازبینی
 * ظاهر و تعامل در حافظه خوانده می‌شود.
 */

import type { PreviewMethod } from "./preview-model.ts";

const SCHEDULE_STANDARD = {
  weekdays: [0, 1, 2, 3, 4] as const,
  fromTime: "09:00",
  toTime: "18:00",
  cutoffHours: 4,
};

const SCHEDULE_EXTENDED = {
  weekdays: [0, 1, 2, 3, 4, 5] as const,
  fromTime: "08:00",
  toTime: "20:00",
  cutoffHours: 6,
};

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
    schedule: SCHEDULE_STANDARD,
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
