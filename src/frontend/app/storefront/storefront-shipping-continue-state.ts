/**
 * TB-240-R7 — یک مرجع واحد برای «آیا ادامهٔ مسیر ارسال روشن است؟» و «اگر نه، دلیل کوتاه مالک آن چیست؟».
 *
 * چرا این ماژول وجود دارد: پیش از این صفحه فقط `isValid` را می‌ساخت و دکمهٔ غیرفعال هیچ دلیلی
 * نشان نمی‌داد؛ کاربر یک دکمهٔ خاکستری بی‌توضیح می‌دید. این تابع همان تصمیم را می‌گیرد و **همان**
 * تصمیم را به یک دلیل کوتاه و قابل‌فهم تبدیل می‌کند، بدون ساختن اعتبارسنج دوم:
 * - فیلدهای آدرس با همان قواعد قبلی (نام/موبایل/استان/شهر/نشانی/کد پستی).
 * - مسیر capture‌شده با `capturedSelectionBlocker` موجود (هیچ اعتبارسنج موازی).
 * - مسیر قدیمی با همان booleanهای قبلی صفحه، دست‌نخورده.
 *
 * هیچ تاریخ/ساعت/مبلغی اینجا ساخته نمی‌شود؛ فقط وضعیت و دلیل گزارش می‌شود.
 */
import type { StorefrontCapturedPreview } from "./storefront-shipping-api.ts";
import type { StorefrontCapturedGroupChoice } from "./storefront-shipping-group-cards.tsx";
import { capturedSelectionBlocker } from "./storefront-shipping-captured-selection.ts";

export type ShippingContinueBlockerCode =
  | "recipient"
  | "contact_mobile"
  | "province"
  | "city"
  | "postal_address"
  | "postal_code"
  | "preview_pending"
  | "no_preview"
  | "preview_incomplete"
  | "group_unavailable"
  | "group_no_dates"
  | "group_date"
  | "method"
  | "schedule";

export interface ShippingContinueBlocker {
  code: ShippingContinueBlockerCode;
  /** فقط برای دلیل‌های گروهی: کلید پایدار گروه سرور (برچسب در صفحه ترجمه می‌شود). */
  groupKey?: string;
}

export interface ShippingContinueState {
  ready: boolean;
  blocker: ShippingContinueBlocker | null;
}

/** کامل‌بودن فیلدهای آدرس؛ مقدار شخصی هیچ‌جا ذخیره/گزارش نمی‌شود، فقط boolean. */
export interface ShippingAddressCompleteness {
  recipient: boolean;
  contactMobile: boolean;
  province: boolean;
  city: boolean;
  postalAddress: boolean;
  postalCode: boolean;
}

export interface ShippingContinueInput {
  address: ShippingAddressCompleteness;
  captured: {
    active: boolean;
    /** پاسخ همین هویت (مقصد/کوپن/نسخهٔ سبد) تأیید شده است. */
    confirmed: boolean;
    preview: StorefrontCapturedPreview | null;
    choices: Readonly<Record<string, StorefrontCapturedGroupChoice>>;
  };
  legacy: {
    /** آیا مسیر قدیمی برای این سبد اصلاً روش نمایش‌دادنی دارد. */
    available: boolean;
    methodReady: boolean;
    scheduleReady: boolean;
  };
}

const ADDRESS_ORDER: readonly (keyof ShippingAddressCompleteness)[] = [
  "recipient",
  "contactMobile",
  "province",
  "city",
  "postalAddress",
  "postalCode",
];

const ADDRESS_CODES: Readonly<Record<keyof ShippingAddressCompleteness, ShippingContinueBlockerCode>> = {
  recipient: "recipient",
  contactMobile: "contact_mobile",
  province: "province",
  city: "city",
  postalAddress: "postal_address",
  postalCode: "postal_code",
};

function firstAddressBlocker(address: ShippingAddressCompleteness): ShippingContinueBlocker | null {
  for (const field of ADDRESS_ORDER) {
    if (!address[field]) return { code: ADDRESS_CODES[field] };
  }
  return null;
}

function hasAnyDate(methods: StorefrontCapturedPreview["groups"][number]["methods"]): boolean {
  return methods.some((method) => method.availableDates.length > 0);
}

function capturedGroupBlocker(
  preview: StorefrontCapturedPreview | null,
  choices: Readonly<Record<string, StorefrontCapturedGroupChoice>>,
): ShippingContinueBlocker | null {
  if (!preview || preview.state !== "ok" || !preview.enabled || preview.groups.length === 0) {
    return { code: "no_preview" };
  }
  for (const group of preview.groups) {
    if (group.methods.length === 0) return { code: "group_unavailable", groupKey: group.groupKey };
    const choice = choices[group.groupKey];
    const method = choice
      ? group.methods.find(
          (entry) =>
            entry.serviceCode === choice.serviceCode
            && (entry.optionCode ?? "") === (choice.optionCode ?? ""),
        ) ?? null
      : null;
    const dateOk = Boolean(method && choice?.date)
      && Boolean(method?.availableDates.some((slot) => slot.value === choice?.date));
    if (!dateOk) {
      return {
        code: hasAnyDate(group.methods) ? "group_date" : "group_no_dates",
        groupKey: group.groupKey,
      };
    }
  }
  // بازتاب‌های تأیید/مبلغ کالا با همان اعتبارسنج موجود بررسی می‌شوند (نه یک کپی دوم).
  if (capturedSelectionBlocker(preview, choices) !== null) {
    const firstGroup = preview.groups[0];
    return { code: "preview_incomplete", groupKey: firstGroup?.groupKey };
  }
  return null;
}

/**
 * وضعیت ادامهٔ مسیر + دلیل مالک. `ready === true` تنها وقتی است که همان تصمیم قبلی صفحه برقرار
 * باشد؛ هیچ شل‌شدنی برای «روشن‌کردن دکمه» اضافه نشده است.
 */
export function resolveShippingContinueState(input: ShippingContinueInput): ShippingContinueState {
  const addressBlocker = firstAddressBlocker(input.address);
  if (addressBlocker) return { ready: false, blocker: addressBlocker };

  if (input.captured.active) {
    if (!input.captured.confirmed) return { ready: false, blocker: { code: "preview_pending" } };
    const groupBlocker = capturedGroupBlocker(input.captured.preview, input.captured.choices);
    if (groupBlocker) return { ready: false, blocker: groupBlocker };
    return { ready: true, blocker: null };
  }

  if (!input.legacy.available) return { ready: false, blocker: { code: "no_preview" } };
  if (!input.legacy.methodReady) return { ready: false, blocker: { code: "method" } };
  if (!input.legacy.scheduleReady) return { ready: false, blocker: { code: "schedule" } };
  return { ready: true, blocker: null };
}

/** متن کوتاه و کاربری دلیل؛ نام گروه فقط برچسب فروشندهٔ همان گروه است. */
export function describeShippingContinueBlocker(
  blocker: ShippingContinueBlocker | null,
  options: { locale: string; groupLabel?: string | null },
): string | null {
  if (!blocker) return null;
  const fa = options.locale !== "en";
  const group = (options.groupLabel ?? "").trim();
  const named = group.length > 0 ? `«${group}»` : (fa ? "این گروه" : "this group");
  switch (blocker.code) {
    case "recipient":
      return fa ? "نام و نام خانوادگی گیرنده را کامل کنید." : "Complete the recipient first and last name.";
    case "contact_mobile":
      return fa ? "شمارهٔ تماس گیرنده را وارد کنید." : "Enter the recipient contact mobile.";
    case "province":
      return fa ? "استان را انتخاب کنید تا هزینه و زمان ارسال محاسبه شود." : "Choose the province.";
    case "city":
      return fa ? "شهر را انتخاب کنید تا هزینه و زمان ارسال محاسبه شود." : "Choose the city.";
    case "postal_address":
      return fa ? "نشانی پستی را کامل کنید." : "Complete the postal address.";
    case "postal_code":
      return fa ? "کد پستی را وارد کنید." : "Enter the postal code.";
    case "preview_pending":
      return fa
        ? "پاسخ هزینه و زمان ارسال برای این مقصد هنوز تأیید نشده است؛ چند لحظه صبر کنید و دوباره تلاش کنید."
        : "The shipping price and time for this destination is not confirmed yet; retry in a moment.";
    case "no_preview":
      return fa
        ? "پیش‌نمایش ارسال برای این سبد در دسترس نیست؛ دلیل آن بالای همین صفحه آمده است."
        : "Shipping preview is unavailable for this cart; the reason is shown above.";
    case "preview_incomplete":
      return fa
        ? "پاسخ ارسال یک گروه ناقص است؛ صفحه را دوباره بارگذاری کنید."
        : "The shipping response for a group is incomplete; reload the page.";
    case "group_unavailable":
      return fa
        ? `برای ${named} روش ارسال قابل‌انتخاب نیست؛ دلیل آن روی کارت همین گروه نوشته شده است.`
        : `No selectable shipping method exists for ${named}; its card shows the reason.`;
    case "group_no_dates":
      return fa
        ? `برای ${named} تاریخ تحویل مجاز اعلام نشده است؛ دلیل آن روی کارت همین گروه نوشته شده است.`
        : `No lawful delivery date was published for ${named}; its card shows the reason.`;
    case "group_date":
      return fa
        ? `برای ${named} یکی از تاریخ‌های تحویل را انتخاب کنید.`
        : `Choose one delivery date for ${named}.`;
    case "method":
      return fa ? "روش ارسال هر گروه را انتخاب کنید." : "Choose a shipping method for each group.";
    case "schedule":
      return fa ? "تاریخ و بازهٔ ساعت تحویل را انتخاب کنید." : "Choose the delivery date and time window.";
    default:
      return null;
  }
}
