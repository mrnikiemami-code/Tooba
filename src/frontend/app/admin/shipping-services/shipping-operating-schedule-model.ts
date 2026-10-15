/**
 * TB-SHIPPING-FULL-OPERATIONS-072 — pure model for the weekly operating-schedule editor.
 *
 * No React and no network here, so weekday identity, 24-hour time parsing, local validation, the
 * `HH:mm:ss` wire shape and the Persian wording stay testable without a browser.
 *
 * Carrier operating windows are NOT customer delivery promises: this editor only describes when a
 * carrier can pick up / dispatch a shipment. Nothing here creates availability or a promise.
 */
import type {
  ShippingOperatingScheduleAdminItem,
  ShippingOperatingScheduleWriteRequest,
  ShippingOperatingWindowAdminItem,
  ShippingOperatingWindowWrite,
} from "./shipping-operating-schedule-api.ts";

/** پشتیبانی‌شده: فقط همان دامنهٔ نیم‌باز `[start,end)` که بک‌اند ۰۷۱ اعتبارسنجی می‌کند. */
export const MAX_WINDOWS_PER_SCHEDULE = 64;

/** بیشترین ظرفیت قابل تنظیم برای یک پنجره؛ همان سقف دامنهٔ Shipping. */
export const MAX_WINDOW_CAPACITY = 1_000_000;

/**
 * منطقهٔ زمانی پیش‌فرض صریح. عمداً از مرورگر حدس زده نمی‌شود: یک «13:00» بی‌منطقه بی‌معنا است و
 * حدس خاموش می‌تواند بازهٔ عملیاتی را در سکوت جابجا کند.
 */
export const DEFAULT_TIME_ZONE_ID = "Asia/Tehran";

/** منطقهٔ زمانی‌هایی که این ویرایشگر به‌عنوان گزینهٔ صریح پیشنهاد می‌کند (ورودی آزاد هم پذیرفته است). */
export const COMMON_TIME_ZONE_IDS: readonly string[] = [
  "Asia/Tehran",
  "Asia/Dubai",
  "Europe/Istanbul",
  "UTC",
];

/** یک روز هفتهٔ ISO با نام فارسی؛ ۱ دوشنبه تا ۷ یکشنبه. */
export type ShippingWeekday = 1 | 2 | 3 | 4 | 5 | 6 | 7;

type WeekdayDescriptor = { weekday: ShippingWeekday; labelFa: string };

/** ترتیب نمایش از شنبه شروع می‌شود (هفتهٔ کاری ایران) اما هویت ISO تغییر نمی‌کند. */
export const SHIPPING_WEEKDAYS: readonly WeekdayDescriptor[] = [
  { weekday: 6, labelFa: "شنبه" },
  { weekday: 7, labelFa: "یکشنبه" },
  { weekday: 1, labelFa: "دوشنبه" },
  { weekday: 2, labelFa: "سه‌شنبه" },
  { weekday: 3, labelFa: "چهارشنبه" },
  { weekday: 4, labelFa: "پنجشنبه" },
  { weekday: 5, labelFa: "جمعه" },
];

/** برچسب فارسی یک روز هفته؛ روز ناشناخته با خود عدد نمایش داده می‌شود تا هویت پنهان نماند. */
export function weekdayLabelFa(weekday: number): string {
  return SHIPPING_WEEKDAYS.find((row) => row.weekday === weekday)?.labelFa ?? `روز ${weekday}`;
}

/** آیا این عدد یک روز هفتهٔ ISO ۱..۷ است. */
export function isIsoWeekday(value: number): value is ShippingWeekday {
  return Number.isInteger(value) && value >= 1 && value <= 7;
}

/* ---------------------------------------------------------------------------------------------
 * Time fields: unambiguous 24-hour local HH:mm.
 * ------------------------------------------------------------------------------------------- */

const TIME_24H = /^([01]\d|2[0-3]):([0-5]\d)$/;

/** دقیقه‌های یک زمان محلی `HH:mm`؛ ورودی نامعتبر `null` می‌دهد (هیچ نرمال‌سازی خاموشی رخ نمی‌دهد). */
export function parseLocalTime(value: string | null | undefined): number | null {
  const trimmed = (value ?? "").trim();
  const match = TIME_24H.exec(trimmed);
  if (!match) return null;
  return Number(match[1]) * 60 + Number(match[2]);
}

/** زمان محلی را به شکل wire بک‌اند (`HH:mm:ss`) می‌برد؛ ورودی نامعتبر `null` می‌دهد. */
export function toWireTime(value: string | null | undefined): string | null {
  const minutes = parseLocalTime(value);
  if (minutes == null) return null;
  const hours = Math.floor(minutes / 60);
  const rest = minutes % 60;
  return `${String(hours).padStart(2, "0")}:${String(rest).padStart(2, "0")}:00`;
}

/**
 * زمان wire (`HH:mm:ss` یا `HH:mm`) را به شکل قابل ویرایش `HH:mm` برمی‌گرداند.
 *
 * مقدار نامعتبر به `""` نگاشت می‌شود تا فیلد خالی بماند؛ ساختن یک «00:00» جعلی از یک مقدار
 * ناخوانا، کاربر را به ذخیرهٔ بازه‌ای وادار می‌کرد که هرگز انتخاب نکرده است.
 */
export function toEditableTime(value: string | null | undefined): string {
  const raw = (value ?? "").trim();
  const match = /^(\d{1,2}):(\d{2})(?::\d{2})?$/.exec(raw);
  if (!match) return "";
  const hours = Number(match[1]);
  const minutes = Number(match[2]);
  if (!Number.isInteger(hours) || hours > 23 || minutes > 59) return "";
  return `${String(hours).padStart(2, "0")}:${String(minutes).padStart(2, "0")}`;
}

/* ---------------------------------------------------------------------------------------------
 * Editor form shape.
 * ------------------------------------------------------------------------------------------- */

export type ShippingWindowFormRow = {
  /** کلید پایدار React؛ فقط هویت ردیف در UI، هرگز روی سیم نمی‌رود. */
  key: string;
  weekday: ShippingWeekday;
  startTime: string;
  endTime: string;
  /** ظرفیت اختیاری به‌صورت رشته تا ورودی انسانی دست‌نخورده بماند؛ خالی یعنی تعیین‌نشده. */
  capacity: string;
};

export type ShippingScheduleFormIssue = {
  /** کلید پایدار ایراد (نه متن) تا تست و UI روی همان هویت توافق کنند. */
  code: string;
  messageFa: string;
  /** کلید ردیف پنجره‌ای که ایراد به آن مربوط است؛ برای ایرادهای سطح‌فرم تهی است. */
  rowKey?: string;
};

export const SHIPPING_SCHEDULE_ISSUE_CODES = {
  windowsRequired: "schedule_windows_required",
  windowsTooMany: "schedule_windows_too_many",
  weekdayInvalid: "schedule_weekday_invalid",
  timeInvalid: "schedule_time_invalid",
  rangeInvalid: "schedule_window_range_invalid",
  overlap: "schedule_window_overlap",
  capacityInvalid: "schedule_window_capacity_invalid",
  timeZoneRequired: "schedule_timezone_required",
  optionUnknown: "service_option_unknown",
} as const;

let rowSequence = 0;

/** یک کلید ردیف تازه و یکتا؛ فقط برای هویت ردیف در UI. */
export function newWindowRowKey(): string {
  rowSequence += 1;
  return `w${rowSequence}`;
}

/** یک ردیف پنجرهٔ خالی روی روز داده‌شده. هیچ زمان جعلی انتخاب نمی‌شود. */
export function emptyWindowRow(weekday: ShippingWeekday = 7): ShippingWindowFormRow {
  return { key: newWindowRowKey(), weekday, startTime: "", endTime: "", capacity: "" };
}

/** یک ردیف پنجره از یک پنجرهٔ ذخیره‌شده. */
export function windowRowFromStored(window: ShippingOperatingWindowAdminItem): ShippingWindowFormRow {
  return {
    key: newWindowRowKey(),
    weekday: isIsoWeekday(window.weekday) ? window.weekday : 1,
    startTime: toEditableTime(window.startTime),
    endTime: toEditableTime(window.endTime),
    capacity: window.capacity == null ? "" : String(window.capacity),
  };
}

/* ---------------------------------------------------------------------------------------------
 * Local validation — mirrors the 071 backend contract so a bad shape never reaches the API.
 * ------------------------------------------------------------------------------------------- */

function parseCapacity(raw: string): { value: number | null; invalid: boolean } {
  const trimmed = raw.trim();
  if (trimmed === "") return { value: null, invalid: false };
  if (!/^\d+$/.test(trimmed)) return { value: null, invalid: true };
  const parsed = Number(trimmed);
  if (!Number.isSafeInteger(parsed) || parsed <= 0 || parsed > MAX_WINDOW_CAPACITY) {
    return { value: null, invalid: true };
  }
  return { value: parsed, invalid: false };
}

/** آیا دو ردیف روی یک روز هفته هم‌پوشانی دارند؛ بازهٔ نیم‌باز یعنی دو ردیف چسبیده هم‌پوشانی ندارند. */
function rowsOverlap(left: ShippingWindowFormRow, right: ShippingWindowFormRow): boolean {
  if (left.weekday !== right.weekday) return false;
  const leftStart = parseLocalTime(left.startTime);
  const leftEnd = parseLocalTime(left.endTime);
  const rightStart = parseLocalTime(right.startTime);
  const rightEnd = parseLocalTime(right.endTime);
  if (leftStart == null || leftEnd == null || rightStart == null || rightEnd == null) return false;
  return leftStart < rightEnd && rightStart < leftEnd;
}

/**
 * اعتبارسنجی کامل فرم پیش از هر فراخوانی API.
 *
 * ترتیب ایرادها پایدار است (سطح‌فرم، سپس ردیف‌ها به ترتیب). همهٔ کدها با کدهای ثابت بک‌اند
 * هم‌معنا هستند تا کاربر یک پیام برای یک قانون ببیند.
 */
export function validateShippingScheduleForm(input: {
  timeZoneId: string;
  windows: readonly ShippingWindowFormRow[];
}): ShippingScheduleFormIssue[] {
  const issues: ShippingScheduleFormIssue[] = [];

  if (input.timeZoneId.trim().length === 0) {
    issues.push({
      code: SHIPPING_SCHEDULE_ISSUE_CODES.timeZoneRequired,
      messageFa: "منطقهٔ زمانی الزامی است؛ بدون آن «13:00» قابل تفسیر نیست.",
    });
  }

  if (input.windows.length === 0) {
    issues.push({
      code: SHIPPING_SCHEDULE_ISSUE_CODES.windowsRequired,
      messageFa: "حداقل یک بازهٔ زمانی لازم است؛ برنامهٔ بدون بازه یعنی «هیچ ساعت صریحی ثبت نشده».",
    });
  }

  if (input.windows.length > MAX_WINDOWS_PER_SCHEDULE) {
    issues.push({
      code: SHIPPING_SCHEDULE_ISSUE_CODES.windowsTooMany,
      messageFa: `تعداد بازه‌ها نمی‌تواند از ${MAX_WINDOWS_PER_SCHEDULE.toLocaleString("fa-IR")} بیشتر باشد.`,
    });
  }

  for (const row of input.windows) {
    if (!isIsoWeekday(row.weekday)) {
      issues.push({
        code: SHIPPING_SCHEDULE_ISSUE_CODES.weekdayInvalid,
        messageFa: "روز هفته باید بین دوشنبه (۱) و یکشنبه (۷) باشد.",
        rowKey: row.key,
      });
    }

    const start = parseLocalTime(row.startTime);
    const end = parseLocalTime(row.endTime);
    if (start == null) {
      issues.push({
        code: SHIPPING_SCHEDULE_ISSUE_CODES.timeInvalid,
        messageFa: "ساعت شروع باید به شکل ۲۴ساعته HH:mm باشد (مثلاً 13:00).",
        rowKey: row.key,
      });
    }
    if (end == null) {
      issues.push({
        code: SHIPPING_SCHEDULE_ISSUE_CODES.timeInvalid,
        messageFa: "ساعت پایان باید به شکل ۲۴ساعته HH:mm باشد (مثلاً 14:00).",
        rowKey: row.key,
      });
    }
    // end > start is the backend invariant: reversed, zero-length and overnight windows all fail here
    // with the same stable meaning, so a window that crosses midnight is refused instead of silently
    // reinterpreted.
    if (start != null && end != null && end <= start) {
      issues.push({
        code: SHIPPING_SCHEDULE_ISSUE_CODES.rangeInvalid,
        messageFa: "پایان بازه باید بعد از شروع آن باشد؛ بازهٔ معکوس، صفر یا عبورکننده از نیمه‌شب پشتیبانی نمی‌شود.",
        rowKey: row.key,
      });
    }

    const capacity = parseCapacity(row.capacity);
    if (capacity.invalid) {
      issues.push({
        code: SHIPPING_SCHEDULE_ISSUE_CODES.capacityInvalid,
        messageFa: "ظرفیت باید عددی مثبت و کمتر از یک میلیون باشد یا خالی بماند.",
        rowKey: row.key,
      });
    }
  }

  for (let i = 0; i < input.windows.length; i += 1) {
    for (let j = i + 1; j < input.windows.length; j += 1) {
      if (rowsOverlap(input.windows[i]!, input.windows[j]!)) {
        issues.push({
          code: SHIPPING_SCHEDULE_ISSUE_CODES.overlap,
          messageFa: `دو بازه در روز «${weekdayLabelFa(input.windows[i]!.weekday)}» هم‌پوشانی دارند؛ بازه‌های چسبیده (مثلاً 13:00–14:00 و 14:00–15:00) مجازند.`,
          rowKey: input.windows[j]!.key,
        });
      }
    }
  }

  return issues;
}

/**
 * بدنهٔ PUT را از فرم معتبر می‌سازد. پس از <see cref="validateShippingScheduleForm"/> صدا زده می‌شود،
 * پس هیچ مقدار نامعتبری بی‌صدا حذف نمی‌شود.
 */
export function buildScheduleWriteRequest(input: {
  serviceCode: string;
  timeZoneId: string;
  optionCode: string | null;
  windows: readonly ShippingWindowFormRow[];
}): ShippingOperatingScheduleWriteRequest {
  const windows: ShippingOperatingWindowWrite[] = input.windows.map((row) => ({
    weekday: row.weekday,
    startTime: toWireTime(row.startTime) ?? "00:00:00",
    endTime: toWireTime(row.endTime) ?? "00:00:00",
    capacity: parseCapacity(row.capacity).value,
  }));

  return {
    serviceCode: input.serviceCode.trim(),
    timeZoneId: input.timeZoneId.trim(),
    windows,
    optionCode: input.optionCode ? input.optionCode : null,
  };
}

/* ---------------------------------------------------------------------------------------------
 * Option identity + display.
 * ------------------------------------------------------------------------------------------- */

/** یک گزینهٔ واقعی همین خدمت از درخت سرویس Fulfillment؛ هیچ گزینه‌ای ساخته نمی‌شود. */
export type ShippingServiceOptionChoice = {
  code: string;
  labelFa: string;
};

/** مقدار نگه‌دارندهٔ برنامهٔ سطح‌خدمت در `<select>`؛ معادل `optionCode = null` روی سیم. */
export const SERVICE_LEVEL_SCHEDULE_VALUE = "";

/** متن صریح معنای برنامهٔ سطح‌خدمت به‌عنوان fallback. */
export const SERVICE_LEVEL_SCHEDULE_FA =
  "برنامهٔ سطح خدمت — وقتی هیچ برنامهٔ اختصاصی برای گزینه‌ای ثبت نشده باشد، این برنامه هویت پیش‌فرض خدمت است.";

/** برچسب نمایش یک برنامه: گزینهٔ شناخته‌شده با نام فارسی، سطح‌خدمت با متن صریح، ناشناخته با خود کد. */
export function scheduleOptionLabelFa(
  optionCode: string | null,
  options: readonly ShippingServiceOptionChoice[],
): string {
  if (!optionCode) return "سطح خدمت (پیش‌فرض خدمت)";
  const found = options.find((option) => option.code === optionCode);
  return found && found.labelFa ? found.labelFa : optionCode;
}

/** کلید پایدار یک برنامه در UI (سطح‌خدمت مقدار تهی می‌گیرد). */
export function scheduleOptionKey(optionCode: string | null): string {
  return optionCode ?? SERVICE_LEVEL_SCHEDULE_VALUE;
}

/**
 * آیا گزینهٔ انتخابی واقعاً متعلق به همین خدمت است؟ گزینهٔ ناشناخته پیش از فراخوانی API رد می‌شود تا
 * `shipping.service_option.unknown` از سمت مرورگر ساخته نشود.
 */
export function validateScheduleOption(
  optionCode: string | null,
  options: readonly ShippingServiceOptionChoice[],
): ShippingScheduleFormIssue[] {
  if (!optionCode) return [];
  if (options.length === 0) {
    return [{
      code: SHIPPING_SCHEDULE_ISSUE_CODES.optionUnknown,
      messageFa: "این خدمت هیچ گزینه‌ای ندارد؛ برنامه باید در سطح خدمت ثبت شود.",
    }];
  }
  if (!options.some((option) => option.code === optionCode)) {
    return [{
      code: SHIPPING_SCHEDULE_ISSUE_CODES.optionUnknown,
      messageFa: "گزینهٔ انتخابی به این خدمت تعلق ندارد (shipping.service_option.unknown).",
    }];
  }
  return [];
}

/**
 * یک برنامه را به ردیف‌های فرم تبدیل می‌کند و ترتیب را از خود سرور حفظ می‌کند (روز، سپس شروع).
 */
export function windowRowsFromSchedule(schedule: ShippingOperatingScheduleAdminItem): ShippingWindowFormRow[] {
  return schedule.windows.map(windowRowFromStored);
}

/** مرتب‌سازی پایدار ردیف‌ها برای نمایش: روز، سپس ساعت شروع؛ ردیف ناقص آخر می‌ماند. */
export function sortWindowRows(rows: readonly ShippingWindowFormRow[]): ShippingWindowFormRow[] {
  return [...rows].sort((left, right) => {
    if (left.weekday !== right.weekday) return left.weekday - right.weekday;
    const leftStart = parseLocalTime(left.startTime);
    const rightStart = parseLocalTime(right.startTime);
    if (leftStart == null && rightStart == null) return 0;
    if (leftStart == null) return 1;
    if (rightStart == null) return -1;
    return leftStart - rightStart;
  });
}

/** خلاصهٔ خوانای یک پنجرهٔ ذخیره‌شده؛ ظرفیت با برچسب «تنظیم‌شده» نمایش داده می‌شود. */
export function formatWindowFa(window: ShippingOperatingWindowAdminItem): string {
  const range = `${toEditableTime(window.startTime) || window.startTime}–${toEditableTime(window.endTime) || window.endTime}`;
  if (window.capacity == null) return range;
  return `${range} · ظرفیت تنظیم‌شده: ${window.capacity.toLocaleString("fa-IR")}`;
}

/* ---------------------------------------------------------------------------------------------
 * Explicit disclosures.
 * ------------------------------------------------------------------------------------------- */

/**
 * برچسب صریح ظرفیت. این متن عمداً نمی‌گوید «ظرفیت باقی‌مانده» یا «رزرو شده»: در ۰۷۱ هیچ شمارش،
 * رزرو یا قفل اتمی وجود ندارد، پس این عدد فقط پیکربندی است.
 */
export const CAPACITY_CONFIG_ONLY_FA = "ظرفیت تنظیم‌شده (هنوز اعمال نمی‌شود)";

/** توضیح صریح معنای نبود برنامه: نه یک بلاک، بلکه «هیچ ساعت صریحی ثبت نشده». */
export const NO_SCHEDULE_DISCLOSURE_FA =
  "نبود یا غیرفعال بودن برنامه، هیچ‌چیز را در Checkout فعلی نمی‌بندد؛ فقط یعنی هیچ ساعت صریحی برای تحویل به حامل ثبت نشده است.";

/** یادآوری مرز مالکیت: برنامهٔ عملیاتی ≠ وعدهٔ تحویل به مشتری. */
export const OPERATING_WINDOW_SCOPE_FA =
  "این بازه‌ها «در دسترس بودن تحویل به حامل» است، نه زمان تحویل وعده‌داده‌شده به مشتری؛ وعدهٔ مشتری دارایی Order است.";

/** خلاصهٔ شمارشی نما برای هدر بخش. */
export type ScheduleSummary = {
  scheduleCount: number;
  activeCount: number;
  windowCount: number;
  configuredCapacityTotal: number | null;
};

/** خلاصهٔ شمارشی یک خدمت. */
export function summarizeSchedules(schedules: readonly ShippingOperatingScheduleAdminItem[]): ScheduleSummary {
  const capacities = schedules
    .map((schedule) => schedule.configuredCapacityTotal)
    .filter((value): value is number => value != null);
  return {
    scheduleCount: schedules.length,
    activeCount: schedules.filter((schedule) => schedule.isActive).length,
    windowCount: schedules.reduce((total, schedule) => total + schedule.windows.length, 0),
    configuredCapacityTotal: capacities.length === 0 ? null : capacities.reduce((a, b) => a + b, 0),
  };
}

/** متن صریح وضعیت برنامه‌های یک خدمت. */
export function scheduleStateFa(schedules: readonly ShippingOperatingScheduleAdminItem[]): string {
  const summary = summarizeSchedules(schedules);
  if (summary.scheduleCount === 0) {
    return "هیچ برنامهٔ عملیاتی ثبت نشده — یعنی هیچ ساعت صریحی برای تحویل به حامل تعیین نشده است.";
  }
  const active = summary.activeCount.toLocaleString("fa-IR");
  const total = summary.scheduleCount.toLocaleString("fa-IR");
  const windows = summary.windowCount.toLocaleString("fa-IR");
  return `برنامه‌های فعال: ${active} از ${total} · مجموع بازه‌ها: ${windows}`;
}

/* ---------------------------------------------------------------------------------------------
 * Stable API failure codes → Persian, field-adjacent messages.
 * ------------------------------------------------------------------------------------------- */

const SCHEDULE_FAILURE_FA: Record<string, string> = {
  "shipping.schedule.timezone.required": "منطقهٔ زمانی الزامی است؛ بدون آن بازه‌ها قابل تفسیر نیستند.",
  "shipping.schedule.timezone.invalid": "این منطقهٔ زمانی روی سرور شناخته نمی‌شود (مثلاً Asia/Tehran).",
  "shipping.schedule.weekday.invalid": "روز هفته نامعتبر است؛ روزها باید بین دوشنبه (۱) و یکشنبه (۷) باشند.",
  "shipping.schedule.window.range_invalid": "بازهٔ نامعتبر: پایان باید بعد از شروع باشد و عبور از نیمه‌شب پشتیبانی نمی‌شود.",
  "shipping.schedule.window.overlap": "دو بازه در یک روز هم‌پوشانی دارند؛ بازه‌های چسبیده مجازند.",
  "shipping.schedule.window.capacity.invalid": "ظرفیت باید عددی مثبت و در محدودهٔ مجاز باشد.",
  "shipping.schedule.windows.too_many": "تعداد بازه‌ها از سقف مجاز بیشتر است.",
  "shipping.schedule.not_found": "این برنامه پیدا نشد؛ فهرست را دوباره بارگذاری کنید.",
  "shipping.schedule.duplicate": "برای این خدمت و گزینه از قبل برنامه‌ای وجود دارد؛ همان را ویرایش کنید.",
  "shipping.schedule.option_mismatch": "این برنامه به هویت دیگری تعلق دارد و از این مسیر قابل تغییر نیست.",
  "shipping.service_scope.mismatch": "این برنامه به خدمت دیگری تعلق دارد؛ تغییر میان‌خدمتی مجاز نیست.",
  "shipping.service_option.unknown": "گزینهٔ انتخابی به این خدمت تعلق ندارد.",
  "shipping.validation.schedule_timezone_required": "منطقهٔ زمانی الزامی است.",
  "shipping.validation.schedule_timezone_too_long": "شناسهٔ منطقهٔ زمانی بیش از حد بلند است.",
  "shipping.validation.schedule_weekday_out_of_range": "روز هفته باید بین ۱ (دوشنبه) و ۷ (یکشنبه) باشد.",
  "shipping.validation.schedule_window_range_invalid": "بازهٔ نامعتبر: پایان باید بعد از شروع باشد.",
  "shipping.validation.schedule_window_overlap": "دو بازه در یک روز هم‌پوشانی دارند.",
  "shipping.validation.schedule_window_capacity_invalid": "ظرفیت باید عددی مثبت باشد.",
  "shipping.validation.schedule_windows_required": "حداقل یک بازه لازم است.",
  "shipping.validation.schedule_windows_too_many": "تعداد بازه‌ها از سقف مجاز بیشتر است.",
  "shipping.validation.body_service_code_mismatch": "کد خدمت بدنه با کد خدمت مسیر یکسان نیست.",
  "shipping.validation.service_code_required": "کد خدمت الزامی است.",
  "shipping.validation.body_invalid": "بدنهٔ درخواست خوانا نبود؛ دوباره تلاش کنید.",
  "shipping.service_code.required": "کد خدمت الزامی است.",
  "admin.authorization.denied": "دسترسی لازم برای تغییر برنامهٔ ارسال را ندارید.",
  "admin.actor.missing": "نشست ادمین شناسایی نشد؛ دوباره وارد شوید.",
  "host-unreachable": "ارتباط با سرور برقرار نشد؛ دوباره تلاش کنید.",
};

/**
 * کد خطای پایدار را به پیام fa کاربرپسند نگاشت می‌کند. برای کد ناشناخته `fallback` برگردانده می‌شود
 * تا کد خام هرگز در UI دیده نشود.
 */
export function mapScheduleErrorFa(raw: string | null | undefined, fallback: string): string {
  const code = (raw ?? "").trim();
  return SCHEDULE_FAILURE_FA[code] ?? fallback;
}
