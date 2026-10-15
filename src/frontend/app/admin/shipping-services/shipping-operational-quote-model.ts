/**
 * TB-SHIPPING-FULL-OPERATIONS-078 — pure presentation/validation model for the diagnostic
 * «بررسی هزینه و شرایط ارسال» inspector. No React and no network here, so unit conversion,
 * timezone resolution, money wording and the Persian refusal messages stay testable without a
 * server.
 *
 * Three boundaries are stated explicitly and never blurred:
 *
 * 1. **The quote is a diagnostic, not price authority.** Nothing in this module computes a price:
 *    it only formats what the backend returned. Today's Checkout still runs under the legacy quote
 *    authority.
 * 2. **A nullable amount is not zero.** `null` means «اعلام نشده» and is rendered as such; `0` is a
 *    real quoted amount and stays `0`.
 * 3. **A carrier window is not a customer delivery promise.** Capacity here is configuration only:
 *    «فقط تنظیم شده؛ رزرو/اجرای ظرفیت وجود ندارد».
 */
import {
  formatGramsAsKilograms,
  parseKilogramsToGrams,
  parseItemCount,
  MAX_WEIGHT_GRAMS,
  MAX_ITEM_COUNT,
} from "./shipping-rate-tier-model.ts";
import { DEFAULT_TIME_ZONE_ID } from "./shipping-operating-schedule-model.ts";
import type {
  ShippingConditionMatch,
  ShippingOperationalQuoteResult,
  ShippingScheduleAvailability,
  ShippingScheduleEvaluation,
  ShippingFreightPayerMode,
  ShippingOperationalQuoteOutcome,
} from "./shipping-operational-quote-api.ts";

/** ارز پیش‌فرض همان ارز بازار؛ روی پاسخ با ارز برگشتی اعتبارسنجی می‌شود. */
export const DEFAULT_QUOTE_CURRENCY = "IRR";

/** سقف جمع سبد برای جلوگیری از ورودی نامعقول پیش از API. */
export const MAX_SUBTOTAL_AMOUNT = 1_000_000_000_000;

/** ISO 4217: کد ارز دقیقاً سه حرف لاتین است. */
const CURRENCY_PATTERN = /^[A-Za-z]{3}$/;

/** ورودی لحظهٔ محلی به شکل `YYYY-MM-DDTHH:mm` — همان چیزی که `<input type="datetime-local">` می‌دهد. */
const LOCAL_DATE_TIME_PATTERN = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})$/;

export type ShippingOperationalQuoteFormValues = {
  /** کد ارز؛ پیش‌فرض IRR و پیش از فراخوانی اعتبارسنجی می‌شود. */
  currencyCode: string;
  /** جمع سبد بدون هزینهٔ ارسال، به IRR. */
  cartSubtotal: string;
  /** وزن اندازه‌گیری‌شدهٔ مرسوله به کیلوگرم؛ خالی یعنی «اعلام‌نشده». */
  weightKilograms: string;
  /** تعداد اقلام فیزیکی مرسوله (نه خطوط سبد)؛ خالی یعنی «اعلام‌نشده». */
  itemCount: string;
  /** گزینهٔ روش ارسال؛ تهی یعنی فقط در سطح خدمت. */
  optionCode: string | null;
  /** لحظهٔ محلی تحویل به حامل به شکل `YYYY-MM-DDTHH:mm`؛ خالی یعنی ارزیابی زمانی انجام نشود. */
  pickupLocal: string;
  /** منطقهٔ زمانی IANA که ورودی محلی در آن تفسیر می‌شود؛ هرگز از مرورگر حدس زده نمی‌شود. */
  timeZoneId: string;
};

export function emptyQuoteForm(): ShippingOperationalQuoteFormValues {
  return {
    currencyCode: DEFAULT_QUOTE_CURRENCY,
    cartSubtotal: "",
    weightKilograms: "",
    itemCount: "",
    optionCode: null,
    pickupLocal: "",
    timeZoneId: DEFAULT_TIME_ZONE_ID,
  };
}

/**
 * مبلغ نامنفی با حداکثر دو رقم اعشار (هم‌شکل ستون `numeric(18,2)` بک‌اند).
 *
 * ورودی دقیق‌تر از دو رقم رد می‌شود و گرد نمی‌شود؛ گرد کردن خاموش یعنی مبلغی که کاربر تایپ
 * نکرده به ارزیابی تحمیل شود.
 */
export function parseMoneyAmount(raw: string): number | null {
  const trimmed = raw.trim();
  if (trimmed === "") return null;
  const match = /^(\d+)(?:\.(\d{1,2}))?$/.exec(trimmed);
  if (!match) return null;
  const whole = Number(match[1]);
  if (!Number.isSafeInteger(whole)) return null;
  const fraction = (match[2] ?? "").padEnd(2, "0");
  const cents = whole * 100 + Number(fraction);
  if (!Number.isSafeInteger(cents)) return null;
  return cents / 100;
}

/** ارز را به شکل canonical (`IRR`) نرمال می‌کند. */
export function normalizeCurrencyCode(raw: string): string {
  return raw.trim().toUpperCase();
}

/** وزن اختیاری: خالی یعنی «اعلام‌نشده» و هر مقدار دیگری باید دقیقاً به گرم تبدیل شود. */
export function parseOptionalWeightGrams(raw: string): { value: number | null; invalid: boolean } {
  if (raw.trim() === "") return { value: null, invalid: false };
  const grams = parseKilogramsToGrams(raw);
  return grams == null ? { value: null, invalid: true } : { value: grams, invalid: false };
}

/** تعداد اقلام اختیاری: خالی یعنی «اعلام‌نشده»؛ صفر یک مرسولهٔ خالی است و رد می‌شود. */
export function parseOptionalItemCount(raw: string): { value: number | null; invalid: boolean } {
  if (raw.trim() === "") return { value: null, invalid: false };
  const count = parseItemCount(raw);
  if (count == null || count < 1 || count > MAX_ITEM_COUNT) return { value: null, invalid: true };
  return { value: count, invalid: false };
}

/* ---------------------------------------------------------------------------------------------
 * Explicit timezone resolution: local wall time + IANA zone → UTC instant, without guessing.
 * ------------------------------------------------------------------------------------------- */

export type LocalDateTimeParts = {
  year: number;
  month: number;
  day: number;
  hour: number;
  minute: number;
};

/** تجزیهٔ ورودی محلی `YYYY-MM-DDTHH:mm`؛ شکل نامعتبر یا تاریخ تقویمی ناموجود `null` می‌دهد. */
export function parseLocalDateTime(raw: string): LocalDateTimeParts | null {
  const match = LOCAL_DATE_TIME_PATTERN.exec(raw.trim());
  if (!match) return null;
  const parts: LocalDateTimeParts = {
    year: Number(match[1]),
    month: Number(match[2]),
    day: Number(match[3]),
    hour: Number(match[4]),
    minute: Number(match[5]),
  };
  if (parts.month < 1 || parts.month > 12 || parts.day < 1 || parts.day > 31) return null;
  if (parts.hour > 23 || parts.minute > 59) return null;
  // Reject a calendar date that does not exist (for example 2026-02-30) instead of rolling it over.
  const probe = new Date(Date.UTC(parts.year, parts.month - 1, parts.day));
  if (probe.getUTCMonth() !== parts.month - 1 || probe.getUTCDate() !== parts.day) return null;
  return parts;
}

/** منطقهٔ زمانی IANA معتبر است؟ یک رشتهٔ ناشناخته نباید بی‌صدا پذیرفته شود. */
export function isKnownTimeZone(timeZoneId: string): boolean {
  const id = timeZoneId.trim();
  if (!id) return false;
  try {
    new Intl.DateTimeFormat("en-US", { timeZone: id });
    return true;
  } catch {
    return false;
  }
}

function zoneFormatter(timeZoneId: string): Intl.DateTimeFormat {
  return new Intl.DateTimeFormat("en-US", {
    timeZone: timeZoneId,
    hourCycle: "h23",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  });
}

/** اجزای دیوارساعتی یک لحظهٔ UTC در منطقهٔ داده‌شده. */
function partsInZone(formatter: Intl.DateTimeFormat, utcMs: number): LocalDateTimeParts & { second: number } {
  const parts = formatter.formatToParts(new Date(utcMs));
  const read = (type: string): number => Number(parts.find((p) => p.type === type)?.value ?? "0");
  return {
    year: read("year"),
    month: read("month"),
    day: read("day"),
    hour: read("hour") % 24,
    minute: read("minute"),
    second: read("second"),
  };
}

/** آفست محلی نسبت به UTC بر حسب میلی‌ثانیه در یک لحظهٔ مشخص. */
function offsetMsAt(formatter: Intl.DateTimeFormat, utcMs: number): number {
  const p = partsInZone(formatter, utcMs);
  const asUtc = Date.UTC(p.year, p.month - 1, p.day, p.hour, p.minute, p.second);
  return asUtc - utcMs;
}

export type TimeZoneResolution =
  | { kind: "empty" }
  | { kind: "invalid"; messageFa: string }
  | { kind: "ambiguous"; messageFa: string }
  | { kind: "nonexistent"; messageFa: string }
  | { kind: "resolved"; utcIso: string; offsetLabel: string };

/** برچسب خوانای آفست (مثل `UTC+03:30`) برای نمایش صریح تفسیر ورودی. */
function offsetLabelFa(offsetMs: number): string {
  const totalMinutes = Math.round(offsetMs / 60_000);
  const sign = totalMinutes < 0 ? "-" : "+";
  const abs = Math.abs(totalMinutes);
  const hours = String(Math.floor(abs / 60)).padStart(2, "0");
  const minutes = String(abs % 60).padStart(2, "0");
  return `UTC${sign}${hours}:${minutes}`;
}

/**
 * لحظهٔ محلی + منطقهٔ زمانی صریح → لحظهٔ UTC.
 *
 * رفتار برای زمان‌های مبهم و ناموجود (تغییر ساعت تابستانی) **صریح و fail-closed** است: هیچ‌کدام
 * بی‌صدا انتخاب نمی‌شوند و هر دو با پیام فارسی روشن رد می‌شوند، چون هر دو انتخاب خاموش می‌تواند
 * بازهٔ حامل را جابجا کند.
 */
export function resolvePickupToUtc(local: string, timeZoneId: string): TimeZoneResolution {
  if (local.trim() === "") return { kind: "empty" };

  const zone = timeZoneId.trim();
  if (!isKnownTimeZone(zone)) {
    return {
      kind: "invalid",
      messageFa: `منطقهٔ زمانی «${zone || "—"}» شناخته‌شده نیست؛ یک شناسهٔ IANA معتبر وارد کنید (مثال: Asia/Tehran).`,
    };
  }

  const parts = parseLocalDateTime(local);
  if (!parts) {
    return { kind: "invalid", messageFa: "لحظهٔ تحویل به حامل باید به شکل ۲۴ساعته YYYY-MM-DDTHH:mm باشد (مثال: 2026-10-18T13:30)." };
  }

  const formatter = zoneFormatter(zone);
  const wallAsUtc = Date.UTC(parts.year, parts.month - 1, parts.day, parts.hour, parts.minute);

  // Probe the zone offset a day before and a day after the requested wall time. A single-iteration
  // guess is not enough: at a DST fall-back the pre-transition offset is the one that resolves, so
  // both sides must be tried for the two-occurrence case to be detected instead of silently picked.
  const offsets = [...new Set([offsetMsAt(formatter, wallAsUtc - 12 * 3_600_000), offsetMsAt(formatter, wallAsUtc + 12 * 3_600_000)])];

  const candidates = offsets.map((offset) => wallAsUtc - offset).filter((candidate) => {
    const back = partsInZone(formatter, candidate);
    return (
      back.year === parts.year &&
      back.month === parts.month &&
      back.day === parts.day &&
      back.hour === parts.hour &&
      back.minute === parts.minute
    );
  });
  const uniqueCandidates = [...new Set(candidates)];

  if (uniqueCandidates.length === 0) {
    return {
      kind: "nonexistent",
      messageFa: `این ساعت محلی در منطقهٔ «${zone}» وجود ندارد (تغییر ساعت تابستانی)؛ یک لحظهٔ دیگر انتخاب کنید.`,
    };
  }
  if (uniqueCandidates.length > 1) {
    return {
      kind: "ambiguous",
      messageFa: `این ساعت محلی در منطقهٔ «${zone}» دو بار رخ می‌دهد (تغییر ساعت تابستانی)؛ یک لحظهٔ بی‌ابهام انتخاب کنید.`,
    };
  }

  const chosen = uniqueCandidates[0]!;
  return {
    kind: "resolved",
    utcIso: new Date(chosen).toISOString(),
    offsetLabel: offsetLabelFa(offsetMsAt(formatter, chosen)),
  };
}

/* ---------------------------------------------------------------------------------------------
 * Local validation — mirrors the 076 backend shape so a bad shape never reaches the API.
 * ------------------------------------------------------------------------------------------- */

export type QuoteFormIssue = {
  /** کلید پایدار فیلد (نه متن) تا تست و UI روی همان هویت توافق کنند. */
  field: keyof ShippingOperationalQuoteFormValues;
  messageFa: string;
};

/** اعتبارسنجی فرم پیش از هر فراخوانی API. خروجی خالی یعنی فرم معتبر است. */
export function validateQuoteForm(values: ShippingOperationalQuoteFormValues): QuoteFormIssue[] {
  const issues: QuoteFormIssue[] = [];

  if (!CURRENCY_PATTERN.test(values.currencyCode.trim())) {
    issues.push({ field: "currencyCode", messageFa: "کد ارز باید سه حرف لاتین باشد (مثال: IRR)." });
  }

  const subtotal = parseMoneyAmount(values.cartSubtotal);
  if (subtotal == null) {
    issues.push({
      field: "cartSubtotal",
      messageFa: "جمع سبد باید عددی نامنفی با حداکثر دو رقم اعشار باشد (مثال: 500000 یا 500000.50).",
    });
  } else if (subtotal > MAX_SUBTOTAL_AMOUNT) {
    issues.push({ field: "cartSubtotal", messageFa: "جمع سبد از سقف مجاز این ابزار بیشتر است." });
  }

  const weight = parseOptionalWeightGrams(values.weightKilograms);
  if (weight.invalid) {
    issues.push({
      field: "weightKilograms",
      messageFa: `وزن باید عددی نامنفی با حداکثر سه رقم اعشار و حداکثر ${MAX_WEIGHT_GRAMS.toLocaleString("fa-IR")} گرم باشد (مثال: 1.999).`,
    });
  }

  const count = parseOptionalItemCount(values.itemCount);
  if (count.invalid) {
    issues.push({
      field: "itemCount",
      messageFa: `تعداد اقلام باید عددی صحیح و مثبت و کمتر از ${MAX_ITEM_COUNT.toLocaleString("fa-IR")} باشد یا خالی بماند.`,
    });
  }

  const pickup = resolvePickupToUtc(values.pickupLocal, values.timeZoneId);
  if (pickup.kind === "invalid" || pickup.kind === "ambiguous" || pickup.kind === "nonexistent") {
    issues.push({ field: "pickupLocal", messageFa: pickup.messageFa });
  }

  return issues;
}

/** بدنهٔ ارسال به API؛ فقط پس از اعتبارسنجی موفق صدا زده می‌شود. */
export function quoteFormToPayload(
  values: ShippingOperationalQuoteFormValues,
  scope: { countryId: string; administrativeAreaId: string | null; cityId: string | null; serviceAreaId: string | null },
): {
  scope: typeof scope;
  cartSubtotalExclusive: number;
  currencyCode: string;
  optionCode: string | null;
  weightGrams: number | null;
  itemCount: number | null;
  requestedPickupAtUtc: string | null;
} {
  const pickup = resolvePickupToUtc(values.pickupLocal, values.timeZoneId);
  return {
    scope,
    cartSubtotalExclusive: parseMoneyAmount(values.cartSubtotal) ?? 0,
    currencyCode: normalizeCurrencyCode(values.currencyCode),
    optionCode: values.optionCode,
    weightGrams: parseOptionalWeightGrams(values.weightKilograms).value,
    itemCount: parseOptionalItemCount(values.itemCount).value,
    requestedPickupAtUtc: pickup.kind === "resolved" ? pickup.utcIso : null,
  };
}

/* ---------------------------------------------------------------------------------------------
 * Explicit disclosures.
 * ------------------------------------------------------------------------------------------- */

/** مبلغ اعلام‌نشده هرگز صفر نمایش داده نمی‌شود. */
export const NOT_DECLARED_FA = "اعلام نشده";

/** متن اعلام اینکه این ارزیابی یک تشخیص درونی است، نه قیمت مشتری. */
export const QUOTE_IS_DIAGNOSTIC_FA =
  "این ارزیابی یک «تشخیص عملیاتی» درون ادمین است، نه قیمت نهایی مشتری: Checkout امروز همچنان با مرجع قیمت قدیمی کار می‌کند و این عدد روی هیچ سفارشی نوشته نمی‌شود.";

/** متن اعلام مرز مالکیت: بازهٔ حامل ≠ وعدهٔ تحویل به مشتری. */
export const PICKUP_WINDOW_IS_NOT_PROMISE_FA =
  "بازهٔ حامل یعنی «چه زمانی حامل مرسوله را می‌پذیرد»، نه زمان تحویل وعده‌داده‌شده به مشتری؛ وعدهٔ مشتری دارایی Order است.";

/** برچسب صریح ظرفیت: فقط پیکربندی، بدون رزرو/اجرا. */
export const CAPACITY_CONFIG_ONLY_QUOTE_FA =
  "ظرفیت: فقط تنظیم شده؛ رزرو/اجرای ظرفیت وجود ندارد.";

/** متن اعلام اینکه حالت پیش‌کرایه/پس‌کرایه یک واقعیت نرخ است، نه تراکنش پرداخت. */
export const PAYER_SPLIT_IS_QUOTE_FACT_FA =
  "تفکیک «پرداخت هنگام تسویه» و «وصول هنگام تحویل» یک واقعیت نرخ است؛ این ابزار هیچ تراکنش پرداخت یا وصول واقعی نمی‌سازد.";

/** توضیح فارسی «پیش‌کرایه». */
export const PREPAID_EXPLANATION_FA =
  "پیش‌کرایه: کرایهٔ حامل هنگام تسویهٔ سفارش پرداخت می‌شود و در همان ستون «قابل پرداخت هنگام تسویه» دیده می‌شود.";

/** توضیح فارسی «پس‌کرایه» بدون ادعای تسویهٔ واقعی. */
export const COLLECT_ON_DELIVERY_EXPLANATION_FA =
  "پس‌کرایه: کرایهٔ حامل هنگام تحویل وصول می‌شود و در ستون «وصول هنگام تحویل» دیده می‌شود. وصول و تسویهٔ واقعی پس‌کرایه هنوز به Checkout/پرداخت متصل نشده است.";

/** متن اعلام اینکه بسته‌بندی همیشه بخش تسویه است. */
export const PACKAGING_ALWAYS_CHECKOUT_FA =
  "هزینهٔ بسته‌بندی همیشه بخش تسویه است، چون حامل هیچ قراردادی برای وصول آن ندارد.";

/** متن اعلام معنای «ارزیابی‌نشده». */
export const NOT_EVALUATED_FA =
  "ارزیابی‌نشده: لحظهٔ تحویل به حامل اعلام نشده است، پس هیچ ادعایی دربارهٔ بازهٔ حامل نمی‌شود و این حالت «قابل رزرو» نیست.";

/* ---------------------------------------------------------------------------------------------
 * Faithful display of the backend answer.
 * ------------------------------------------------------------------------------------------- */

/** مبلغ و ارز به‌شکل خوانا؛ `null` هرگز صفر نمی‌شود. */
export function formatAmountFa(amount: number | null, currencyCode: string | null): string {
  if (amount == null) return NOT_DECLARED_FA;
  const formatted = new Intl.NumberFormat("fa-IR").format(amount);
  const currency = currencyCode ? normalizeCurrencyCode(currencyCode) : "";
  return currency ? `${formatted} ${currency}` : formatted;
}

/** مبلغ اعشاری دقیق برای مقایسهٔ خام؛ بدون گرد کردن و بدون ارز. */
export function rawAmount(value: number | null): string {
  return value == null ? NOT_DECLARED_FA : String(value);
}

/** برچسب فارسی نتیجهٔ قیمت‌گذاری. */
export function outcomeLabelFa(outcome: ShippingOperationalQuoteOutcome): string {
  switch (outcome) {
    case "Quoted":
      return "قیمت اعلام شد";
    case "FreightNotQuoted":
      return "کرایه اعلام نشد";
    case "PackagingAmbiguous":
      return "بسته‌بندی مبهم";
    case "CurrencyMismatch":
      return "ناسازگاری ارز";
    default:
      return "ناشناخته";
  }
}

/** برچسب فارسی در دسترس بودن بازهٔ حامل؛ «ارزیابی‌نشده» هرگز «در دسترس» خوانده نمی‌شود. */
export function availabilityLabelFa(availability: ShippingScheduleAvailability): string {
  switch (availability) {
    case "NotConfigured":
      return "برنامهٔ عملیاتی ثبت نشده (هیچ محدودیتی اعمال نمی‌شود)";
    case "InactiveNotEnforced":
      return "برنامهٔ ثبت‌شده غیرفعال است (به‌عنوان محدودیت اعمال نمی‌شود)";
    case "Available":
      return "داخل بازهٔ فعال حامل";
    case "Unavailable":
      return "خارج از همهٔ بازه‌های فعال حامل";
    case "NotEvaluated":
      return "ارزیابی‌نشده (لحظهٔ تحویل اعلام نشده)";
    case "Ambiguous":
      return "مبهم: بیش از یک برنامهٔ فعال هم‌سطح وجود دارد";
    default:
      return "ناشناخته";
  }
}

/** برچسب فارسی طرف پرداخت کرایه. */
export function payerModeLabelFa(mode: ShippingFreightPayerMode): string {
  return mode === "CollectOnDelivery" ? "پس‌کرایه" : "پیش‌کرایه";
}

/** روز هفتهٔ ISO محلی از همان برچسب فارسی برنامهٔ عملیاتی. */
export function weekdayLabelFa(weekday: number | null): string {
  if (weekday == null) return NOT_DECLARED_FA;
  const labels: Record<number, string> = {
    1: "دوشنبه",
    2: "سه‌شنبه",
    3: "چهارشنبه",
    4: "پنجشنبه",
    5: "جمعه",
    6: "شنبه",
    7: "یکشنبه",
  };
  return labels[weekday] ?? `روز ${weekday}`;
}

/** خلاصهٔ خوانای وضعیت بازهٔ حامل، همراه با زمان محلی و روز هفته اگر برگشته باشد. */
export function scheduleSummaryFa(schedule: ShippingScheduleEvaluation | null): string {
  if (!schedule) return NOT_DECLARED_FA;
  const parts = [availabilityLabelFa(schedule.availability)];
  if (schedule.timeZoneId) parts.push(`منطقه: ${schedule.timeZoneId}`);
  if (schedule.localWeekday != null) parts.push(`روز محلی: ${weekdayLabelFa(schedule.localWeekday)}`);
  if (schedule.localTime) parts.push(`ساعت محلی: ${schedule.localTime}`);
  if (schedule.matchedWindowId) parts.push(`پنجرهٔ منطبق: ${schedule.matchedWindowId}`);
  if (schedule.configuredCapacityTotal != null) {
    parts.push(`مجموع ظرفیت تنظیم‌شده: ${schedule.configuredCapacityTotal.toLocaleString("fa-IR")}`);
  }
  return parts.join(" · ");
}

/** خلاصهٔ خوانای پلهٔ اندازهٔ منطبق؛ فقط توصیفی و بدون محاسبهٔ قیمت. */
export function conditionMatchFa(match: ShippingConditionMatch | null): string {
  if (!match) return "بدون پلهٔ اندازهٔ منطبق (نرخ بی‌شرط یا نرخ اعلام‌نشده)";
  const parts: string[] = [];
  if (match.weight) {
    const from = `${formatGramsAsKilograms(match.weight.min)}kg شامل`;
    parts.push(
      match.weight.max == null
        ? `وزن از ${from} — بدون کران بالا`
        : `وزن [${from}، ${formatGramsAsKilograms(match.weight.max)}kg)`,
    );
  }
  if (match.count) {
    parts.push(match.count.max == null ? `اقلام ≥ ${match.count.min}` : `اقلام [${match.count.min}، ${match.count.max})`);
  }
  return parts.length > 0 ? parts.join(" ∩ ") : "بدون پلهٔ اندازهٔ منطبق";
}

/**
 * آیا پاسخ برگشتی «قابل اعلام» است. صریحاً نبود قیمت را با «مبهم» یکی نمی‌کند و هیچ‌گاه
 * «قابل رزرو» نمی‌گوید.
 */
export function isQuotedResult(result: ShippingOperationalQuoteResult): boolean {
  return result.outcome === "Quoted" && result.isQuoted;
}

/** آیا پاسخ برگشتی هیچ محدودیت بازهٔ حامل را نقض نکرده است. */
export function isOperationallyEligible(result: ShippingOperationalQuoteResult): boolean {
  return isQuotedResult(result) && (result.schedule?.isConstraintSatisfied ?? false);
}

/** آیا ارز درخواست با ارز پاسخ یکسان است؛ ناسازگاری باید دیده شود، نه نرمال شود. */
export function currencyMatchesRequest(
  requestedCurrencyCode: string,
  result: ShippingOperationalQuoteResult,
): boolean {
  const requested = normalizeCurrencyCode(requestedCurrencyCode);
  const returned = result.currencyCode ? normalizeCurrencyCode(result.currencyCode) : "";
  if (!requested || !returned) return true;
  return requested === returned;
}

/** کدهای خطای پایدار Shipping → پیام fa مجاور فیلد. کد ناشناخته `fallback` می‌گیرد. */
const QUOTE_FAILURE_FA: Record<string, string> = {
  "shipping.validation.service_code_required": "کد خدمت الزامی است.",
  "shipping.validation.service_code_too_long": "کد خدمت بیش از حد بلند است.",
  "shipping.validation.scope_required": "دامنهٔ مقصد الزامی است؛ کشور مقصد را از فهرست انتخاب کنید.",
  "shipping.validation.country_required": "کشور مقصد را از فهرست مرجع Geography انتخاب کنید.",
  "shipping.validation.service_area_requires_city": "ناحیهٔ خدمت بدون شهر مالک معتبر نیست.",
  "shipping.validation.option_code_too_long": "کد گزینهٔ خدمت بیش از حد بلند است.",
  "shipping.validation.cart_subtotal_negative": "جمع سبد نمی‌تواند منفی باشد.",
  "shipping.validation.currency_invalid": "کد ارز باید سه حرف لاتین باشد (مثال: IRR).",
  "shipping.validation.operational_weight_negative": "وزن نمی‌تواند منفی باشد.",
  "shipping.validation.operational_weight_out_of_range": "وزن از سقف مجاز بیشتر است.",
  "shipping.validation.operational_item_count_out_of_range": "تعداد اقلام باید عددی صحیح و مثبت باشد.",
  "shipping.validation.body_invalid": "بدنهٔ درخواست خوانا نبود؛ دوباره تلاش کنید.",
  "shipping.service_code.unknown": "این کد خدمت در کاتالوگ روش‌های ارسال وجود ندارد.",
  "shipping.service_option.unknown": "گزینهٔ انتخابی برای این خدمت شناخته‌شده نیست.",
  "shipping.destination_scope.invalid": "دامنهٔ مقصد نامعتبر است؛ کشور/استان/شهر را از فهرست مرجع انتخاب کنید.",
  "admin.authorization.denied": "دسترسی لازم برای مشاهدهٔ ارزیابی عملیاتی را ندارید.",
  "admin.actor.missing": "نشست ادمین شناسایی نشد؛ دوباره وارد شوید.",
  "host-unreachable": "ارتباط با سرور برقرار نشد؛ دوباره تلاش کنید.",
};

export function mapQuoteErrorFa(raw: string | null | undefined, fallback: string): string {
  const code = (raw ?? "").trim();
  return QUOTE_FAILURE_FA[code] ?? fallback;
}

/** کدهای دلیل پایدار نتیجه → توضیح فارسی؛ کد ناشناخته خود کد خام را نشان می‌دهد. */
const QUOTE_REASON_FA: Record<string, string> = {
  "shipping.operational_quote.quoted": "کرایه و بسته‌بندی قطعی اعلام شدند.",
  "shipping.operational_quote.freight_not_quoted":
    "کرایه اعلام نشد (پوشش/پلهٔ اندازه/ارز/خدمت)؛ هیچ مبلغی نمایش داده نمی‌شود.",
  "shipping.operational_quote.packaging_ambiguous":
    "سیاست‌های بسته‌بندی روی این هویت/دامنه مبهم‌اند؛ هیچ مبلغی نمایش داده نمی‌شود.",
  "shipping.operational_quote.currency_mismatch":
    "ارز مؤلفهٔ حمل با ارز بسته‌بندی یکسان نیست؛ جمع خودکار انجام نمی‌شود.",
  "shipping.schedule.not_configured": "هیچ برنامهٔ عملیاتی برای این خدمت/گزینه ثبت نشده است.",
  "shipping.schedule.inactive_not_enforced": "برنامهٔ ثبت‌شده غیرفعال است و اعمال نمی‌شود.",
  "shipping.schedule.available": "لحظهٔ درخواستی داخل یک پنجرهٔ فعال است.",
  "shipping.schedule.outside_windows": "لحظهٔ درخواستی خارج از همهٔ پنجره‌های فعال همان روز است.",
  "shipping.schedule.not_evaluated": "لحظهٔ درخواستی اعلام نشده است؛ ارزیابی انجام نشد.",
  "shipping.schedule.ambiguous": "بیش از یک برنامهٔ فعال هم‌سطح وجود دارد؛ هیچ ادعایی نمی‌شود.",
  "shipping.schedule.timezone_unresolvable": "منطقهٔ زمانی ذخیره‌شده روی این سرور قابل حل نیست.",
  "shipping.capacity.not_enforced": "ظرفیت پیکربندی‌شده اجرا نمی‌شود.",
  "shipping.packaging.not_configured": "سیاست بسته‌بندی ثبت نشده است؛ هزینهٔ بسته‌بندی صفر است.",
  "shipping.packaging.charged": "هزینهٔ بسته‌بندی از یک سیاست فعال محاسبه شد.",
  "shipping.quote.quoted": "کرایه از موتور نرخ موجود اعلام شد.",
};

export function quoteReasonFa(code: string | null | undefined): string {
  const key = (code ?? "").trim();
  if (!key) return NOT_DECLARED_FA;
  return QUOTE_REASON_FA[key] ?? key;
}
