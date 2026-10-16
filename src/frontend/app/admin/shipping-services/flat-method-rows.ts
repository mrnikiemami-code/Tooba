/**
 * مدل خالص فهرست تک‌سطحی «سرویس ارسال» — بدون وابستگی به React.
 *
 * TB-SHIPPING-ADMIN-FLAT-SERVICE-FULL-IMPLEMENTATION-146
 *
 * قاعدهٔ هویت: **یک روش قابل‌انتخاب مشتری = یک ردیف مستقل مدیریتی**.
 *
 * منبع ردیف‌ها کاتالوگ canonical یک‌سطحی است
 * (`GET /v1/admin/shipping-services/shipping-methods/one-level`) که برای هر روش یک `methodCode`
 * canonical و ترجمه‌های واقعی خودش را برمی‌گرداند. هیچ ردیفی از درخت دو‌سطحی ارائه‌دهنده→گزینه
 * ساخته نمی‌شود، چون آن درخت فقط زیرمجموعهٔ گزینه‌های قدیمی را می‌شناسد و روش‌های canonical مانند
 * `post:express` یا `snapp_courier` را از دست می‌دهد؛ نتیجه‌اش ادغام سیاست ارائه‌دهنده با روش‌های
 * خواهر است که قرارداد قفل‌شده منع کرده است.
 *
 * هیچ نامی اختراع نمی‌شود: برچسب فقط از ترجمهٔ واقعی همان روش در زبان فعال می‌آید؛ در نبود ترجمه،
 * `hasLabel=false` می‌ماند و UI باید نبود نام را صادقانه نشان دهد.
 * هیچ تقسیم متنیِ `methodCode` در مرورگر انجام نمی‌شود؛ `serviceCode`/`optionCode` تنها از پیوند
 * authoritative سرور (`…/one-level/{methodCode}/binding`) خوانده می‌شوند.
 */
import type { OneLevelMethodRow } from "./shipping-one-level-tabs-model.ts";

/** یک ردیف روش؛ `id` همان `methodCode` canonical است و هویت پایدار گرید را می‌سازد. */
export type FlatMethodRow = {
  id: string;
  methodCode: string;
  label: string;
  hasLabel: boolean;
  isActive: boolean;
};

export const ICON_OPTIONS = [
  { value: "post", label: "پست" },
  { value: "tipax", label: "تیپاکس" },
  { value: "courier", label: "پیک آنلاین" },
  { value: "bike", label: "پیک" },
  { value: "store", label: "حضوری" },
  { value: "truck", label: "کامیون" },
] as const;

export const COLOR_OPTIONS = [
  { value: "blue", label: "آبی" },
  { value: "amber", label: "کهربایی" },
  { value: "emerald", label: "سبز" },
  { value: "violet", label: "بنفش" },
  { value: "rose", label: "صورتی" },
  { value: "cyan", label: "فیروزه‌ای" },
] as const;

export function boolFa(value: boolean): string {
  return value ? "بله" : "خیر";
}

export const NO_LABEL_FA = "بدون نام";

/**
 * ساخت ردیف‌های تخت از کاتالوگ canonical.
 *
 * - ترتیب سرور حفظ می‌شود (سرور همان ترتیب نمایش/مرتب‌سازی را برمی‌گرداند) و هیچ مرتب‌سازی
 *   محلی‌ای که هویت را بازآرایی کند اعمال نمی‌شود.
 * - روش غیرفعال هرگز حذف نمی‌شود؛ فقط `isActive=false` می‌ماند.
 * - ردیف تکراری (همان `methodCode`) دور ریخته می‌شود تا هویت پایدار گرید یکتا بماند.
 */
export function buildFlatMethodRows(
  methods: readonly OneLevelMethodRow[],
  activeLanguageId: string,
): FlatMethodRow[] {
  const rows: FlatMethodRow[] = [];
  const seen = new Set<string>();
  for (const method of methods) {
    const code = method.methodCode;
    if (!code || seen.has(code)) continue;
    seen.add(code);
    const translation = method.translations.find((t) => t.languageId === activeLanguageId);
    const label = translation?.name?.trim() ?? "";
    rows.push({
      id: code,
      methodCode: code,
      label,
      hasLabel: label.length > 0,
      isActive: method.isActive === true,
    });
  }
  return rows;
}
