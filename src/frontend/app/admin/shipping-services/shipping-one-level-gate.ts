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

import type { OneLevelMethodRow } from "./shipping-one-level-tabs-model.ts";

/** نام دقیق بخش تنظیمات سمت سرور که مرجع دروازه است. */
export const ONE_LEVEL_CATALOG_SERVER_SECTION = "Tooba:ShippingOneLevelCatalog";

/** توضیح صریح بایند غایب؛ در نتیجهٔ تسک و شواهد نقل می‌شود. */
export const ONE_LEVEL_CATALOG_MISSING_BINDING =
  "هیچ endpoint فقط-خواندنی ادمین وضعیت ShippingOneLevelCatalogPolicy را به فرانت نمی‌دهد؛ " +
  "پس درز فرانت پیش‌فرض-خاموش می‌ماند و UI جدید mount نمی‌شود.";

export type OneLevelCatalogGate =
  | { state: "disabled"; reason: "absent" | "explicitly-off" | "untrusted-source" }
  | { state: "enabled"; source: "server-flag" };

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
