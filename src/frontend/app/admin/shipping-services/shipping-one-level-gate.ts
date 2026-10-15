/**
 * درز خواندن دروازهٔ کاتالوگ یک‌سطحی در فرانت — پیش‌فرض خاموش و fail-closed.
 *
 * TB-SHIPPING-ONE-LEVEL-TABS-084
 *
 * چرا این درز این‌قدر محافظه‌کار است: مرجع حقیقت دروازه، `Tooba:ShippingOneLevelCatalog` در
 * سمت سرور است و در `ShippingOneLevelCatalogPolicy.Evaluate` تصمیم گرفته می‌شود. فرانت این
 * اپ یک Next.js جدا است و به تنظیمات Host دسترسی ندارد؛ پس هیچ راه «قابل‌اعتماد»ی برای
 * خواندن آن وجود ندارد. طبق درخواست معماری، در نبود چنین بایندی:
 *
 *   - درز صریح و پیش‌فرض-خاموش ساخته می‌شود؛
 *   - هیچ‌گاه از وجود جدول/دادهٔ روش، وجود اسکیما، یا پارامتر query روشن نمی‌شود؛
 *   - هیچ سوئیچ query-param‌ای که دروازهٔ خاموش را دور بزند وجود ندارد؛
 *   - تا وقتی بایند سرور وصل نشود، UI جدید mount نمی‌شود و صفحهٔ قدیمی دست‌نخورده کار می‌کند.
 *
 * بایند موردنیاز (خارج از دامنهٔ این تسک و نیازمند مجوز معماری جداگانه):
 * یک endpoint فقط-خواندنی ادمین که خروجی `ShippingOneLevelCatalogPolicy.Evaluate(...)` را
 * برگرداند و در صفحهٔ سرور مصرف شود.
 */

import { sanitizeLanguageId, sanitizeMethodCode, type OneLevelMethodRow } from "./shipping-one-level-tabs-model.ts";

/** نام دقیق بخش تنظیمات سمت سرور که مرجع دروازه است. */
export const ONE_LEVEL_CATALOG_SERVER_SECTION = "Tooba:ShippingOneLevelCatalog";

/** مسیر فقط-خواندنی ادمین که دروازه/کاتالوگ را به صفحهٔ سرور می‌دهد (TB-085). */
export const ONE_LEVEL_CATALOG_ADMIN_READ_PATH = "/v1/admin/shipping-services/shipping-methods/one-level";

/** توضیح صریح بایند غایب؛ در نتیجهٔ تسک و شواهد نقل می‌شود. */
export const ONE_LEVEL_CATALOG_MISSING_BINDING =
  "هیچ endpoint فقط-خواندنی ادمین وضعیت ShippingOneLevelCatalogPolicy را به فرانت نمی‌دهد؛ " +
  "پس درز فرانت پیش‌فرض-خاموش می‌ماند و UI جدید mount نمی‌شود.";

export type OneLevelCatalogGate =
  | { state: "disabled"; reason: "absent" | "explicitly-off" | "untrusted-source" | "catalog-not-ready" }
  | { state: "enabled"; source: "server-flag" };

/** TB-SHIPPING-ONE-LEVEL-TABS-094: زبان فعال مؤثر که سرور برای برچسب‌ها برگردانده است. */
export function resolveOneLevelActiveLanguageId(payload: unknown): string {
  if (!payload || typeof payload !== "object") return "";
  const record = payload as Record<string, unknown>;
  return sanitizeLanguageId(record.languageId ?? record.LanguageId);
}

/**
 * ارزیابی دروازه. تنها ورودی مجاز، پرچم سمت سرور است که فقط یک Server Component می‌تواند
 * تزریق کند. هر منبع دیگری (query string، localStorage، وجود دادهٔ روش) عمداً نادیده گرفته
 * می‌شود؛ در نتیجه هیچ مسیر کلاینتی نمی‌تواند دروازه را روشن کند.
 */
export function resolveOneLevelCatalogGate(input: { serverFlag?: boolean | null } = {}): OneLevelCatalogGate {
  if (input.serverFlag === true) return { state: "enabled", source: "server-flag" };
  if (input.serverFlag === false) return { state: "disabled", reason: "explicitly-off" };
  return { state: "disabled", reason: "absent" };
}

/**
 * آیا دادهٔ روش یک‌سطحی مجاز به نمایش است؟ فقط دروازهٔ روشنِ سمت سرور.
 * این تابع هرگز به query string یا طول لیست نگاه نمی‌کند.
 */
export function canRenderOneLevelMethods(gate: OneLevelCatalogGate): boolean {
  return gate.state === "enabled";
}

/** وضعیت اولیهٔ داده وقتی دروازه روشن است ولی خواندن هنوز آغاز نشده. */
export const ONE_LEVEL_INITIAL_METHODS: readonly OneLevelMethodRow[] = [];

/**
 * TB-SHIPPING-ONE-LEVEL-TABS-085 — نگاشت ناب پاسخ خواندن سرور به وضعیت دروازه.
 *
 * این تابع عمداً خالص است تا در Server Component و در تست بدون هیچ وابستگی به Next اجرا شود.
 * تنها منبع روشن‌شدن دروازه، پرچم صریح سرور است:
 *
 *   - `gate === "Enabled"` و `ready === true` ⇒ enabled (کاتالوگ واقعاً خوانده شده است)؛
 *   - `gate === "Enabled"` ولی `ready === false` ⇒ disabled با دلیل `catalog-not-ready`؛
 *     «آماده‌نبودن» هرگز به‌عنوان «فعال» یا «خالی» جا زده نمی‌شود؛
 *   - `Disabled`، پاسخ نامعتبر، خطای شبکه/مجوز یا هر چیز دیگر ⇒ disabled (fail-closed).
 */
export function resolveOneLevelCatalogGateFromHost(input: {
  status: number;
  payload: unknown;
}): OneLevelCatalogGate {
  if (input.status !== 200) return { state: "disabled", reason: "untrusted-source" };
  const payload = input.payload;
  if (!payload || typeof payload !== "object") return { state: "disabled", reason: "untrusted-source" };
  const record = payload as Record<string, unknown>;
  const gate = record.gate ?? record.Gate;
  const ready = record.ready ?? record.Ready;
  if (gate !== "Enabled") return { state: "disabled", reason: "explicitly-off" };
  if (ready !== true) return { state: "disabled", reason: "catalog-not-ready" };
  return { state: "enabled", source: "server-flag" };
}

/**
 * TB-SHIPPING-ONE-LEVEL-TABS-094: نگاشت محافظه‌کارانهٔ پاسخ سرور به ردیف‌های قابل‌ویرایش.
 *
 * قواعد سخت:
 * - کد خام باید از قبل canonical و ≤۳۲ نویسه باشد؛ کد غیرcanonical هرگز «نرمال» و به هویت
 *   تازه تبدیل نمی‌شود و ردیف تکراری/نامعتبر دور ریخته می‌شود.
 * - ترجمه‌های واقعی همان روش نگه داشته می‌شوند تا فرم ویرایش مقدار زبان انتخاب‌شده را از دادهٔ
 *   واقعی پیش‌پر کند و هیچ نامی اختراع نشود.
 * - هیچ سطح دومی از پاسخ سرور ساخته نمی‌شود و `providerKind` همیشه خالی می‌ماند.
 */
export function mapHostOneLevelMethods(payload: unknown): readonly OneLevelMethodRow[] {
  if (!payload || typeof payload !== "object") return [];
  const record = payload as Record<string, unknown>;
  const raw = record.methods ?? record.Methods;
  if (!Array.isArray(raw)) return [];
  const rows: OneLevelMethodRow[] = [];
  const seen = new Set<string>();
  for (const item of raw) {
    if (!item || typeof item !== "object") continue;
    const row = item as Record<string, unknown>;
    const declared = typeof (row.methodCode ?? row.MethodCode) === "string"
      ? String(row.methodCode ?? row.MethodCode)
      : "";
    const code = sanitizeMethodCode(declared);
    // کد خام باید از قبل canonical و در محدودهٔ ۳۲ نویسه باشد.
    if (!code || code !== declared || seen.has(code)) continue;
    seen.add(code);
    const name = typeof (row.name ?? row.Name) === "string" ? String(row.name ?? row.Name).trim() : "";
    rows.push({
      methodCode: code,
      providerKind: "",
      isActive: (row.isActive ?? row.IsActive) === true,
      legacyServiceCode: null,
      legacyOptionCode: null,
      translations: mapHostOneLevelTranslations(row, name),
    });
  }
  return rows;
}

/** ترجمه‌های واقعی ردیف؛ فقط زبان معتبر با نام غیرخالی، بدون تکرار زبان و بدون ساختن نام. */
function mapHostOneLevelTranslations(
  row: Record<string, unknown>,
  fallbackName: string,
): { languageId: string; name: string; description: string | null }[] {
  const raw = row.translations ?? row.Translations;
  const translations: { languageId: string; name: string; description: string | null }[] = [];
  const seen = new Set<string>();
  if (Array.isArray(raw)) {
    for (const item of raw) {
      if (!item || typeof item !== "object") continue;
      const entry = item as Record<string, unknown>;
      const languageId = sanitizeLanguageId(entry.languageId ?? entry.LanguageId);
      const value = typeof (entry.name ?? entry.Name) === "string" ? String(entry.name ?? entry.Name).trim() : "";
      if (!languageId || value.length === 0 || seen.has(languageId)) continue;
      seen.add(languageId);
      translations.push({ languageId, name: value, description: null });
    }
  }
  // ردیف‌های قدیمی‌تر فقط نام زبان مؤثر را داشتند؛ همان یک ترجمه نگه داشته می‌شود، نه نامی تازه.
  if (translations.length === 0 && fallbackName.length > 0) {
    translations.push({ languageId: "", name: fallbackName, description: null });
  }
  return translations;
}
