/**
 * مدل ناب شل پنج‌زبانهٔ سرویس ارسال — بدون وابستگی به React.
 *
 * TB-SHIPPING-ONE-LEVEL-TABS-084
 *
 * این مدل فقط «چیدمان و انتخاب» را می‌داند: برچسب‌های ثابت زبانه‌ها، پارس/ساخت امن
 * وضعیت URL، تشخیص وضعیت دادهٔ کاتالوگ یک‌سطحی و هویت پایدار روش‌ها. هیچ دادهٔ
 * ساختگی، هیچ قیمت و هیچ وضعیت فعال/غیرفعالی در اینجا ساخته نمی‌شود؛ فعال‌بودن
 * فقط از پاسخ خوانندهٔ فقط-خواندنی می‌آید.
 */

/** شناسهٔ پایدار زبانه‌ها. برچسب‌های فارسی طبق درخواست معماری قفل‌شده‌اند. */
export const ONE_LEVEL_TAB_IDS = [
  "services",
  "rates-coverage",
  "packaging-fare",
  "schedule-capacity",
  "cost-review",
] as const;

export type OneLevelTabId = (typeof ONE_LEVEL_TAB_IDS)[number];

/** برچسب‌های دقیق پنج زبانه. */
export const ONE_LEVEL_TAB_LABELS: Record<OneLevelTabId, string> = {
  services: "سرویس‌ها",
  "rates-coverage": "تعرفه و پوشش",
  "packaging-fare": "بسته‌بندی و کرایه",
  "schedule-capacity": "برنامه کاری و ظرفیت",
  "cost-review": "بررسی هزینه",
};

/** زبانهٔ پیش‌فرض وقتی هیچ انتخاب معتبری در URL نیست. */
export const DEFAULT_ONE_LEVEL_TAB: OneLevelTabId = "services";

/**
 * زبانه‌هایی که هنوز «مقصد شل» هستند و توانمندی واقعی‌شان در تسک دیگری سیم‌کشی
 * می‌شود. این فهرست عمداً صریح است تا هیچ زبانه‌ای بی‌سروصدا ادعای آماده‌بودن نکند.
 *
 * TB-SHIPPING-ONE-LEVEL-TABS-089: همهٔ پنج زبانه سیم‌کشی شده‌اند (سرویس‌ها، تعرفه و
 * پوشش، بسته‌بندی و کرایه، برنامه کاری و ظرفیت، بررسی هزینه) و هیچ مقصد شل باقی نمانده
 * است؛ نگاشت خالی یعنی هیچ زبانه‌ای متن سیم‌کشی‌نشده نشان نمی‌دهد.
 */
export const ONE_LEVEL_TAB_WIRING_TASK: Record<OneLevelTabId, string | null> = {
  services: null,
  "rates-coverage": null,
  "packaging-fare": null,
  "schedule-capacity": null,
  "cost-review": null,
};

/** حداکثر طول کد روش طبق قرارداد 081/083. */
export const SHIPPING_METHOD_MAX_CODE_LENGTH = 32;

export function isOneLevelTabId(value: unknown): value is OneLevelTabId {
  return typeof value === "string" && (ONE_LEVEL_TAB_IDS as readonly string[]).includes(value);
}

/**
 * نرمال‌سازی کد روش برای مقایسه و URL: trim، حروف کوچک و حذف نویسه‌های خطرناک.
 * فقط حروف کوچک لاتین، رقم، `_`، `-` و حداکثر یک `:` مجاز است؛ بقیه حذف می‌شوند.
 * نتیجه هرگز از ۳۲ نویسه بلندتر نمی‌شود تا با عرض ستون `checkouts.shipping_method_code`
 * (varchar(32)) ناسازگار نشود.
 */
export function sanitizeMethodCode(value: unknown): string {
  if (typeof value !== "string") return "";
  const lowered = value.trim().toLowerCase();
  const kept: string[] = [];
  let colonUsed = false;
  for (const char of lowered) {
    if ((char >= "a" && char <= "z") || (char >= "0" && char <= "9") || char === "_" || char === "-") {
      kept.push(char);
      continue;
    }
    if (char === ":" && !colonUsed) {
      colonUsed = true;
      kept.push(char);
    }
  }
  return kept.join("").slice(0, SHIPPING_METHOD_MAX_CODE_LENGTH);
}

/** نرمال‌سازی شناسهٔ زبانه: فقط UUID canonical پذیرفته می‌شود، وگرنه خالی. */
export function sanitizeLanguageId(value: unknown): string {
  if (typeof value !== "string") return "";
  const trimmed = value.trim().toLowerCase();
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/.test(trimmed) ? trimmed : "";
}

/** انتخاب مؤثر از URL: زبانهٔ نامعتبر ⇒ پیش‌فرض، روش/زبان نامعتبر ⇒ خالی. */
export interface OneLevelSelection {
  tab: OneLevelTabId;
  methodCode: string;
  languageId: string;
}

export function parseOneLevelSelection(search: string | null | undefined): OneLevelSelection {
  const params = new URLSearchParams(search ?? "");
  const tabParam = params.get("tab");
  return {
    tab: isOneLevelTabId(tabParam) ? tabParam : DEFAULT_ONE_LEVEL_TAB,
    methodCode: sanitizeMethodCode(params.get("method")),
    languageId: sanitizeLanguageId(params.get("lang")),
  };
}

/**
 * ساخت query string فقط با مقادیر امن و فقط وقتی با پیش‌فرض تفاوت دارند؛
 * بنابراین `?tab=services` و پارامترهای نامعتبر در URL باقی نمی‌مانند.
 */
export function buildOneLevelSearch(selection: OneLevelSelection): string {
  const params = new URLSearchParams();
  if (selection.tab !== DEFAULT_ONE_LEVEL_TAB) params.set("tab", selection.tab);
  const methodCode = sanitizeMethodCode(selection.methodCode);
  if (methodCode) params.set("method", methodCode);
  const languageId = sanitizeLanguageId(selection.languageId);
  if (languageId) params.set("lang", languageId);
  const text = params.toString();
  return text ? `?${text}` : "";
}

/**
 * چرخهٔ حرکت کلید در tablist افقی (RTL و LTR یکسان در منطق، چون ترتیب DOM ملاک است).
 * ArrowRight/ArrowLeft/Home/End پیمایش می‌کنند؛ کلید ناشناخته ⇒ `null` تا مرورگر
 * رفتار خودش را داشته باشد.
 */
export function nextTabIndex(
  current: number,
  key: string,
  count: number,
  options: { wrap?: boolean } = {},
): number | null {
  if (count <= 0) return null;
  const wrap = options.wrap ?? true;
  const clamped = current < 0 || current >= count ? 0 : current;
  const step = (delta: number): number => {
    const raw = clamped + delta;
    if (wrap) return (raw + count) % count;
    return raw < 0 || raw >= count ? clamped : raw;
  };
  switch (key) {
    case "ArrowRight":
      return step(1);
    case "ArrowLeft":
      return step(-1);
    case "Home":
      return 0;
    case "End":
      return count - 1;
    default:
      return null;
  }
}

/**
 * یک روش یک‌سطحی همان‌طور که از خوانندهٔ فقط-خواندنی می‌آید. `providerKind` فرادادهٔ
 * پنهان است و در UI هرگز به‌عنوان سطح دوم قابل انتخاب نمی‌شود؛ اما برای رد کردن
 * ردیف‌های نامعتبر در همین لایه لازم است.
 */
export interface OneLevelMethodRow {
  methodCode: string;
  providerKind: string;
  isActive: boolean;
  legacyServiceCode: string | null;
  legacyOptionCode: string | null;
  translations: { languageId: string; name: string; description: string | null }[];
}

/**
 * ردیف‌های قابل نمایش. قواعد:
 * - کد روش باید canonical و ≤۳۲ نویسه باشد؛ ردیف نامعتبر دور ریخته می‌شود.
 * - یک روش برای هر ردیف؛ هیچ گروه‌بندی provider→option ساخته نمی‌شود.
 * - روش غیرفعال حذف نمی‌شود؛ فقط `isActive=false` می‌ماند تا هیچ‌وقت بی‌سروصدا
 *   قابل رزرو نشود.
 * - برچسب نمایشی فقط از دادهٔ واقعی ترجمه می‌آید؛ در نبود ترجمهٔ زبان فعال، برچسب
 *   خالی می‌ماند و UI باید «بدون برچسب» را صادقانه نشان دهد، نه یک نام اختراعی.
 */
export function projectMethodRows(
  methods: readonly OneLevelMethodRow[],
  activeLanguageId: string,
): { methodCode: string; providerKind: string; isActive: boolean; label: string; hasLabel: boolean }[] {
  const rows: { methodCode: string; providerKind: string; isActive: boolean; label: string; hasLabel: boolean }[] = [];
  for (const method of methods) {
    const code = sanitizeMethodCode(method.methodCode);
    if (!code || code !== method.methodCode) continue;
    const translation = method.translations.find((t) => t.languageId === activeLanguageId);
    const label = translation?.name?.trim() ?? "";
    rows.push({
      methodCode: code,
      providerKind: method.providerKind,
      isActive: method.isActive === true,
      label,
      hasLabel: label.length > 0,
    });
  }
  return rows;
}

/**
 * وضعیت دادهٔ زبانهٔ «سرویس‌ها». چهار حالت صادقانه:
 * - `unavailable`: دروازهٔ سمت سرور خاموش است ⇒ UI اصلاً mount نمی‌شود.
 * - `loading` / `error`: خواندن کاتالوگ در جریان است یا شکست خورده.
 * - `ready`: دادهٔ واقعی رسیده. لیست خالی هم `ready` است، نه خطا.
 *
 * عمداً هیچ حالت «fallback به درخت دو‌سطحی قدیمی» وجود ندارد: در حالت نبود داده،
 * UI باید نبودن را نشان دهد و نباید ادعا کند کاتالوگ یک‌سطحی مرجع است.
 */
export type OneLevelDataState = "unavailable" | "loading" | "error" | "ready";

export function resolveDataState(input: {
  gateEnabled: boolean;
  loading: boolean;
  error: boolean;
}): OneLevelDataState {
  if (!input.gateEnabled) return "unavailable";
  if (input.error) return "error";
  if (input.loading) return "loading";
  return "ready";
}

/** آیا ویرایش در UI جدید مجاز است؟ هیچ نوشتنی در این تسک مجاز نیست. */
export const ONE_LEVEL_UI_IS_READ_ONLY = true;

/** متن صادقانهٔ زبانهٔ سیم‌کشی‌نشده. */
export function wiringNoticeFor(tab: OneLevelTabId): string | null {
  const task = ONE_LEVEL_TAB_WIRING_TASK[tab];
  if (!task) return null;
  return `این بخش در تسک ${task} سیم‌کشی می‌شود؛ در این نسخه فقط مقصد شل است و هیچ عملیاتی انجام نمی‌دهد.`;
}
