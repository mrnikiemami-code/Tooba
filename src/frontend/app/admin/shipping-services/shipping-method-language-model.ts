/**
 * مدل ناب ویرایش چندزبانهٔ نام یک روش ارسال — بدون وابستگی به React.
 *
 * TB-SHIPPING-ADMIN-MULTILINGUAL-UX-148
 *
 * قواعد سخت:
 *
 * - فهرست زبان‌ها **تنها** از رجیستری authoritative زبان‌های سرور می‌آید
 *   (`GET /v1/admin/languages`). هیچ زبان، شناسه یا نام `fa`/`en`/`ru` در این فایل ساخته نمی‌شود و
 *   هیچ زبان جعلی اضافه نمی‌شود؛ زبان بدون `languageId` واقعی کنار گذاشته می‌شود چون مسیر ذخیرهٔ
 *   نام دقیقاً به همان شناسه نیاز دارد.
 * - نبود ترجمه یک زبان، «خالیِ صادقانه» است و هرگز با نام زبان یا نام روش دیگر پر نمی‌شود.
 * - هویت روش هرگز تغییر نمی‌کند؛ این مدل فقط «نام یک روش در یک زبان» را می‌شناسد.
 * - پیش‌نویس‌های ذخیره‌نشده در یک نگاشت زبان‌محور نگه داشته می‌شوند تا جابه‌جایی زبان پیش از
 *   ذخیره هیچ متنی را از دست ندهد.
 */

import { sanitizeLanguageId } from "./shipping-one-level-tabs-model.ts";

/** شکل محافظه‌کارانهٔ یک زبان از رجیستری سرور؛ فقط فیلدهای مصرفی این مدل. */
export type MethodLanguageSource = {
  languageId?: string;
  code?: string;
  urlPrefix?: string;
  displayName?: string;
  nativeName?: string;
  direction?: string;
  active?: boolean;
  sortOrder?: number;
};

/** زبان قابل ویرایش با برچسب واقعی خودش از رجیستری؛ هیچ نامی اختراع نمی‌شود. */
export type MethodLanguageDefinition = {
  languageId: string;
  label: string;
  code: string;
  direction: "rtl" | "ltr";
  active: boolean;
};

/** وضعیت ترجمهٔ نام یک روش در یک زبان. */
export type MethodLanguageTranslationState = MethodLanguageDefinition & {
  /** نام ثبت‌شدهٔ سرور در آن زبان؛ در نبود ترجمه رشتهٔ خالی است. */
  name: string;
  /** آیا ترجمهٔ واقعی و غیرخالی وجود دارد. */
  translated: boolean;
  /** آیا زبان مؤثرِ برچسب فهرست (زبان اعلام‌شدهٔ سرور) همین زبان است. */
  isListLanguage: boolean;
};

/** خلاصهٔ کامل‌بودن ترجمه‌ها؛ برای نمایش صریح «x از y». */
export type MethodTranslationSummary = {
  translated: number;
  missing: number;
  total: number;
};

function trimmed(value: unknown): string {
  return typeof value === "string" ? value.trim() : "";
}

/**
 * زبان‌های قابل ویرایش از رجیستری سرور.
 *
 * - فقط زبان‌هایی که `languageId` معتبر دارند (چون مسیر ذخیره همان شناسه را می‌خواهد)؛
 * - برچسب فقط از دادهٔ خود زبان: `nativeName` سپس `displayName` سپس `code`؛
 * - ترتیب سرور (`sortOrder` و سپس ترتیب ورودی) حفظ می‌شود؛ هیچ مرتب‌سازی الفبایی محلی‌ای که
 *   ترتیب رجیستری را عوض کند اعمال نمی‌شود.
 */
export function methodLanguageDefinitions(
  languages: readonly MethodLanguageSource[],
): MethodLanguageDefinition[] {
  const rows: (MethodLanguageDefinition & { sortOrder: number; index: number })[] = [];
  const seen = new Set<string>();
  languages.forEach((row, index) => {
    const languageId = sanitizeLanguageId(row?.languageId);
    if (!languageId || seen.has(languageId)) return;
    seen.add(languageId);
    const code = trimmed(row?.code) || trimmed(row?.urlPrefix);
    rows.push({
      languageId,
      label: trimmed(row?.nativeName) || trimmed(row?.displayName) || code || languageId,
      code,
      direction: trimmed(row?.direction).toLowerCase() === "ltr" ? "ltr" : "rtl",
      active: row?.active !== false,
      sortOrder: Number.isFinite(row?.sortOrder) ? Number(row?.sortOrder) : index,
      index,
    });
  });
  // ترتیب سرور با `sortOrder` پایدار می‌شود و برای مقادیر برابر ترتیب ورودی حفظ می‌شود.
  return rows
    .sort((a, b) => (a.sortOrder === b.sortOrder ? a.index - b.index : a.sortOrder - b.sortOrder))
    .map(({ sortOrder: _sortOrder, index: _index, ...row }) => row);
}

/** نگاشت زبان‌ها به وضعیت ترجمهٔ همان روش؛ زبان بدون ترجمه «بدون ترجمه» می‌ماند. */
export function methodTranslationStates(
  translations: readonly { languageId: string; name: string }[],
  definitions: readonly MethodLanguageDefinition[],
  listLanguageId: string,
): MethodLanguageTranslationState[] {
  const list = sanitizeLanguageId(listLanguageId);
  return definitions.map((definition) => {
    const name = trimmed(
      translations.find((row) => sanitizeLanguageId(row.languageId) === definition.languageId)?.name,
    );
    return {
      ...definition,
      name,
      translated: name.length > 0,
      isListLanguage: definition.languageId === list,
    };
  });
}

/** شمارش صریح ترجمه‌های موجود/غایب؛ «خالی» هرگز «کامل» گزارش نمی‌شود. */
export function methodTranslationSummary(
  states: readonly MethodLanguageTranslationState[],
): MethodTranslationSummary {
  const translated = states.filter((row) => row.translated).length;
  return { translated, missing: states.length - translated, total: states.length };
}

/** پیش‌نویس اولیهٔ هر زبان از دادهٔ واقعی سرور؛ زبان بدون ترجمه خالی می‌ماند. */
export function methodNameDrafts(
  translations: readonly { languageId: string; name: string }[],
  definitions: readonly MethodLanguageDefinition[],
): Record<string, string> {
  const drafts: Record<string, string> = {};
  for (const definition of definitions) {
    drafts[definition.languageId] = trimmed(
      translations.find((row) => sanitizeLanguageId(row.languageId) === definition.languageId)?.name,
    );
  }
  return drafts;
}

/** تنظیم پیش‌نویس یک زبان بدون دست‌زدن به پیش‌نویس زبان‌های دیگر. */
export function setMethodNameDraft(
  drafts: Readonly<Record<string, string>>,
  languageId: string,
  value: string,
): Record<string, string> {
  const language = sanitizeLanguageId(languageId);
  if (!language) return { ...drafts };
  return { ...drafts, [language]: typeof value === "string" ? value : "" };
}

/** مقدار پیش‌نویس یک زبان؛ زبان ناشناخته رشتهٔ خالی می‌دهد. */
export function methodNameDraftFor(
  drafts: Readonly<Record<string, string>>,
  languageId: string,
): string {
  const language = sanitizeLanguageId(languageId);
  if (!language) return "";
  const value = drafts[language];
  return typeof value === "string" ? value : "";
}

/** نام تأییدشدهٔ سرور برای یک زبان؛ در نبود ترجمه رشتهٔ خالی. */
export function serverMethodNameFor(
  translations: readonly { languageId: string; name: string }[],
  languageId: string,
): string {
  const language = sanitizeLanguageId(languageId);
  if (!language) return "";
  return trimmed(translations.find((row) => sanitizeLanguageId(row.languageId) === language)?.name);
}

/** آیا پیش‌نویس این زبان با نام ثبت‌شدهٔ سرور تفاوت معناداری دارد. */
export function methodNameDraftIsDirty(
  draft: string,
  serverName: string,
): boolean {
  return (typeof draft === "string" ? draft : "").trim() !== trimmed(serverName);
}

/** زبان‌هایی که پیش‌نویس ذخیره‌نشده دارند؛ برای هشدار صریح پیش از بستن/جابه‌جایی. */
export function dirtyMethodLanguages(
  drafts: Readonly<Record<string, string>>,
  translations: readonly { languageId: string; name: string }[],
  definitions: readonly MethodLanguageDefinition[],
): string[] {
  return definitions
    .map((row) => row.languageId)
    .filter((languageId) => methodNameDraftIsDirty(
      methodNameDraftFor(drafts, languageId),
      serverMethodNameFor(translations, languageId),
    ));
}
