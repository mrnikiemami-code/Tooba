/**
 * TB-SHIPPING-INDEPENDENT-GROUP-FRONTEND-150 — زمان‌بندی مستقل هر گروه ارسال.
 *
 * قاعدهٔ قطعی (wireframe تأییدشدهٔ TB-145):
 * - برای هر «گروه ارسال» (فروشندهٔ واقعی سبد × روش canonical) یک افق تاریخ/بازهٔ **مستقل** وجود دارد.
 * - افق هیچ گروهی با گروه دیگر **اشتراک/تلاقی** نمی‌گیرد و هیچ «دیرترین حداقل» سراسری اعمال نمی‌شود.
 * - همهٔ تاریخ/بازه‌ها فقط از ردیف authoritative سرور برای همان گروه می‌آید
 *   (`shippingGroupHorizons` در پاسخ `POST /v1/storefront/shipping/projection`). در کلاینت هیچ تاریخ،
 *   بازه، ظرفیت یا وعدهٔ جدیدی ساخته نمی‌شود.
 *
 * این ماژول هیچ `fetch` و هیچ React ندارد؛ فقط منطق خالص تا مستقل و قابل‌سنجش بماند.
 */
import type {
  StorefrontDeliveryDateOption,
  StorefrontDeliveryTimeOption,
  StorefrontShippingGroupHorizonView,
  StorefrontShippingProjection,
} from "./storefront-shipping-api.ts";

/** انتخاب زمان‌بندی یک گروه؛ کلید نگاشت، کلید پایدار همان گروه است. */
export interface StorefrontShippingGroupSelection {
  deliveryDate: string;
  deliveryTime: string;
}

/** افق مؤثر یک گروه؛ یا از ردیف سرور آمده (`resolved`) یا هنوز در دسترس نیست. */
export interface StorefrontShippingGroupSchedule {
  groupKey: string;
  shippingGroupId: string | null;
  sellerPartyId: string;
  methodCode: string;
  minimumDeliveryDate: string | null;
  dates: StorefrontDeliveryDateOption[];
  timeWindows: StorefrontDeliveryTimeOption[];
  leadDays: number;
  sellerPreparationDays: number;
  /** افق از پاسخ سرور برای همین گروه آمده است. */
  resolved: boolean;
  /** انتخاب ذخیره‌شدهٔ سرور برای همین گروه (بازیابی رفرش/برگشت). */
  persistedDate: string | null;
  persistedTime: string | null;
}

/** شناسهٔ نرمال فروشنده برای تطبیق قطعی ردیف سرور با گروه کلاینت (بدون حدس). */
export function normalizeSellerId(value: string): string {
  return (value ?? "").trim().replace(/-/g, "").toLowerCase();
}

/**
 * تطبیق ردیف سرور با گروه کلاینت: فروشندهٔ نرمال‌شده + کد روش (بدون حساسیت به بزرگی/کوچکی).
 * شناسهٔ گروه سرور هیچ‌گاه توسط کلاینت ساخته نمی‌شود؛ فقط برای تطبیق همین دو محور مصرف می‌شود.
 */
export function findGroupHorizon(
  horizons: readonly StorefrontShippingGroupHorizonView[],
  group: { sellerPartyId: string; methodCode: string },
): StorefrontShippingGroupHorizonView | null {
  const seller = normalizeSellerId(group.sellerPartyId);
  const code = (group.methodCode ?? "").trim().toLowerCase();
  if (!seller || !code) return null;
  return (
    horizons.find(
      (row) => normalizeSellerId(row.sellerPartyId) === seller && row.methodCode.trim().toLowerCase() === code,
    ) ?? null
  );
}

/** افق گروه‌های «انتخاب‌نشده» را خالی/non-resolved نگه می‌دارد (بدون تاریخ جعلی). */
export function emptyGroupSchedule(group: {
  key: string;
  sellerPartyId: string;
  methodCode: string;
}): StorefrontShippingGroupSchedule {
  return {
    groupKey: group.key,
    shippingGroupId: null,
    sellerPartyId: group.sellerPartyId,
    methodCode: group.methodCode,
    minimumDeliveryDate: null,
    dates: [],
    timeWindows: [],
    leadDays: 0,
    sellerPreparationDays: 0,
    resolved: false,
    persistedDate: null,
    persistedTime: null,
  };
}

/**
 * افق مستقل هر گروه را از پاسخ سرور می‌سازد. نگاشت با کلید پایدار گروه انجام می‌شود؛ گروهی که
 * ردیف سرور ندارد افق `resolved: false` می‌گیرد (نه یک تاریخ حدسی) و ادامهٔ سفارش را ناممکن می‌کند.
 */
export function resolveGroupSchedules(input: {
  isGroupedCart: boolean;
  projection: StorefrontShippingProjection | null;
  groups: readonly { key: string; sellerPartyId: string; methodCode: string }[];
}): Record<string, StorefrontShippingGroupSchedule> {
  const resolved: Record<string, StorefrontShippingGroupSchedule> = {};
  if (!input.isGroupedCart) return resolved;

  const horizons = input.projection?.shippingGroupHorizons ?? [];
  for (const group of input.groups) {
    const row = findGroupHorizon(horizons, group);
    if (!row) {
      resolved[group.key] = emptyGroupSchedule(group);
      continue;
    }

    resolved[group.key] = {
      groupKey: row.groupKey,
      shippingGroupId: row.shippingGroupId,
      sellerPartyId: row.sellerPartyId,
      methodCode: row.methodCode,
      minimumDeliveryDate: row.minimumDeliveryDate || null,
      dates: row.eligibleDates,
      timeWindows: row.eligibleTimeWindows,
      leadDays: row.leadDays,
      sellerPreparationDays: row.sellerPreparationDays,
      resolved: row.eligibleDates.length > 0 && row.eligibleTimeWindows.length > 0,
      persistedDate: row.selectedDeliveryDate,
      persistedTime: row.selectedDeliveryTimeWindow,
    };
  }

  return resolved;
}

/** تاریخ انتخاب‌شدهٔ یک گروه را فقط اگر در فهرست مجاز همان گروه باشد نگه می‌دارد. */
export function pickGroupDeliveryDate(current: string, schedule: StorefrontShippingGroupSchedule | null): string {
  if (!schedule?.resolved) return current;
  if (current && schedule.dates.some((date) => date.value === current)) return current;
  // نوبت نامعتبر فقط برای همین گروه به نزدیک‌ترین نوبت مجاز **همان گروه** می‌رسد (تاریخ سرور).
  return schedule.dates[0]?.value ?? "";
}

/** بازهٔ انتخاب‌شدهٔ یک گروه را فقط اگر برای همان گروه مجاز باشد نگه می‌دارد. */
export function pickGroupDeliveryWindow(current: string, schedule: StorefrontShippingGroupSchedule | null): string {
  if (!schedule?.resolved) return current;
  return current && schedule.timeWindows.some((window) => window.value === current) ? current : "";
}

/** آیا زمان‌بندی مستقل همین گروه کامل و مجاز است؟ (تاریخ و بازه هر دو از سرور همان گروه) */
export function isGroupScheduleComplete(
  schedule: StorefrontShippingGroupSchedule | null,
  deliveryDate: string,
  deliveryTime: string,
): boolean {
  return Boolean(
    schedule?.resolved
      && deliveryDate
      && deliveryTime
      && schedule.dates.some((date) => date.value === deliveryDate)
      && schedule.timeWindows.some((window) => window.value === deliveryTime),
  );
}

/**
 * انتخاب هر گروه را برای «مجموعهٔ گروه‌های فعال» تازه‌سازی می‌کند:
 * - گروه تازه‌ظهور: انتخاب ذخیره‌شدهٔ سرور (اگر معتبر باشد) وگرنه نزدیک‌ترین تاریخ مجاز همان گروه.
 * - گروه ناپدیدشده: حذف می‌شود (بدون انتقال انتخاب به گروه دیگر).
 * - انتخاب نامعتبرِ همان گروه: فقط همان گروه پاک می‌شود؛ گروه‌های دیگر دست‌نخورده می‌مانند.
 */
export function mergeGroupSelections(input: {
  previous: Readonly<Record<string, StorefrontShippingGroupSelection>>;
  schedules: Readonly<Record<string, StorefrontShippingGroupSchedule>>;
}): Record<string, StorefrontShippingGroupSelection> {
  const next: Record<string, StorefrontShippingGroupSelection> = {};
  for (const [key, schedule] of Object.entries(input.schedules)) {
    const current = input.previous[key];
    if (!schedule.resolved) {
      // افق همان گروه هنوز از سرور نیامده: انتخاب قبلی حفظ می‌شود تا گلوگاه مصنوعی ساخته نشود.
      next[key] = current ?? { deliveryDate: "", deliveryTime: "" };
      continue;
    }

    const seeded = current?.deliveryDate
      ? current.deliveryDate
      : (schedule.persistedDate ?? schedule.dates[0]?.value ?? "");
    const seededTime = current?.deliveryTime ? current.deliveryTime : (schedule.persistedTime ?? "");
    next[key] = {
      deliveryDate: pickGroupDeliveryDate(seeded, schedule),
      deliveryTime: pickGroupDeliveryWindow(seededTime, schedule),
    };
  }

  return next;
}

/** تنظیم انتخاب یک گروه؛ هیچ کلید دیگری لمس نمی‌شود. */
export function setGroupSelection(
  state: Readonly<Record<string, StorefrontShippingGroupSelection>>,
  groupKey: string,
  patch: Partial<StorefrontShippingGroupSelection>,
): Record<string, StorefrontShippingGroupSelection> {
  const current = state[groupKey] ?? { deliveryDate: "", deliveryTime: "" };
  return { ...state, [groupKey]: { ...current, ...patch } };
}

/** آیا همهٔ گروه‌های فعال زمان‌بندی کامل و مجاز خودشان را دارند؟ (شرط فعال‌شدن ادامه) */
export function allGroupSchedulesComplete(input: {
  schedules: Readonly<Record<string, StorefrontShippingGroupSchedule>>;
  selections: Readonly<Record<string, StorefrontShippingGroupSelection>>;
}): boolean {
  const keys = Object.keys(input.schedules);
  if (keys.length === 0) return false;
  return keys.every((key) => {
    const selection = input.selections[key];
    return isGroupScheduleComplete(
      input.schedules[key],
      selection?.deliveryDate ?? "",
      selection?.deliveryTime ?? "",
    );
  });
}
