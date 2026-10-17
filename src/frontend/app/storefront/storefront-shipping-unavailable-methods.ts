/**
 * TB-SHIPPING-STOREFRONT-OWNED-RATE-BROWSER-165 — توضیح قابل‌نمایش روش‌های بدون نرخ معتبر.
 *
 * ورودی این ماژول تنها ردیف‌های `UnavailableMethods` پاسخ سرور است. قواعد سخت:
 * - ردیف unavailable **فقط توضیح** است؛ هرگز روش را قابل انتخاب نمی‌کند و هرگز بر `methods`
 *   واجد شرایط سرور اولویت ندارد (اگر همان روش در `methods` باشد، توضیح حذف می‌شود).
 * - هیچ مبلغ، «رایگان» یا تاریخ برای آن ساخته نمی‌شود.
 * - هویت در سطح «روش» است (نرخ‌دهی مالک روش به فروشندهٔ سبد وابسته نیست)، پس ردیف به فروشندهٔ
 *   دیگر نسبت داده نمی‌شود و تکرار فروشنده‌ها ردیف تکراری نمی‌سازد.
 * - فقط کدهای پایدار شناخته‌شده ترجمه می‌شوند؛ کد ناشناس/خالی متن عمومیِ راستگو می‌گیرد.
 */
import type { StorefrontShippingMethod, StorefrontShippingUnavailableMethod } from "./storefront-shipping-api.ts";

/** متن عمومی وقتی کد دلیل ناشناخته یا خالی است؛ هیچ ادعای فنی جعلی نمی‌کند. */
export const SHIPPING_UNAVAILABLE_FALLBACK_TEXT = "این روش ارسال در حال حاضر قابل انتخاب نیست.";

const REASON_TEXT: Readonly<Record<string, string>> = {
  "shipping.quote.owned_option_not_priced": "نرخ اختصاصی این گزینهٔ خدمت در تنظیمات حمل تعریف نشده است.",
  "shipping.service.not_priced": "برای این روش نرخ فعالی تعریف نشده است.",
  "shipping.coverage.missing": "برای این مقصد پوشش حمل فعالی ثبت نشده است.",
  "shipping.coverage.method_restricted": "این روش برای مقصد انتخابی مجاز نیست.",
  "shipping.quote.measurement_required": "برای قیمت‌گذاری این روش، اندازهٔ بسته لازم است.",
  "shipping.quote.condition_not_matched": "اندازهٔ اعلام‌شدهٔ بسته با شرایط نرخ این روش سازگار نیست.",
  "shipping.quote.condition_ambiguous": "شرایط نرخ این روش برای این مرسوله مبهم است.",
  "shipping.scope.ambiguous": "دامنهٔ نرخ این روش مبهم است.",
  "shipping.currency.mismatch": "واحد ارز نرخ این روش با ارز سبد یکسان نیست.",
  "shipping.owned_rate.currency_mismatch": "واحد ارز نرخ این روش با ارز سبد یکسان نیست.",
  "shipping.owned_rate.owner_unavailable": "سرویس نرخ‌دهی حمل در حال حاضر پاسخ نمی‌دهد.",
  "shipping.owned_rate.destination_unresolved": "مقصد انتخابی برای نرخ‌دهی این روش قابل تشخیص نیست.",
  "shipping.owned_rate.not_owned": "نرخ این روش به نرخ‌دهی داخلی فروشگاه تعلق ندارد.",
  "shipping.owned_rate.method_identity_incomplete": "هویت خدمت/گزینهٔ این روش کامل نیست.",
  "shipping.owned_rate.quote_failed": "نرخ‌دهی این روش انجام نشد.",
};

export interface StorefrontShippingUnavailableNotice {
  methodCode: string;
  /** برچسب فقط وقتی از `methods` واجد شرایط خوانده می‌شود؛ وگرنه خودِ کد روش (بدون حدس). */
  label: string;
  reasonCode: string;
  reasonText: string;
}

/** ترجمهٔ کد دلیل پایدار سرور؛ فقط کدهای شناخته‌شده، وگرنه متن عمومی. */
export function shippingUnavailableReasonText(reasonCode: string | null | undefined): string {
  const code = (reasonCode ?? "").trim();
  return REASON_TEXT[code] ?? SHIPPING_UNAVAILABLE_FALLBACK_TEXT;
}

/**
 * ردیف‌های قابل‌نمایش توضیحی را می‌سازد: بدون تکرار روش، بدون روش واجد شرایط (سرور صاحب
 * حقیقت است) و بدون هیچ مبلغ. `null`/غایب ⇒ فهرست خالی (سازگاری کامل با گیت خاموش).
 */
export function resolveShippingUnavailableNotices(input: {
  unavailableMethods?: readonly StorefrontShippingUnavailableMethod[] | null;
  methods: readonly StorefrontShippingMethod[];
}): StorefrontShippingUnavailableNotice[] {
  const rows = input.unavailableMethods;
  if (!rows || rows.length === 0) return [];
  const eligible = new Set(input.methods.map((method) => method.methodCode));
  const labels = new Map(input.methods.map((method) => [method.methodCode, method.label] as const));
  const seen = new Set<string>();
  const notices: StorefrontShippingUnavailableNotice[] = [];
  for (const row of rows) {
    const methodCode = (row?.methodCode ?? "").trim();
    if (!methodCode) continue;
    // روشی که سرور در فهرست واجد شرایط آورده هرگز با یک ردیف توضیحی غیرفعال نمی‌شود.
    if (eligible.has(methodCode)) continue;
    if (seen.has(methodCode)) continue;
    seen.add(methodCode);
    notices.push({
      methodCode,
      label: (labels.get(methodCode) ?? "").trim() || methodCode,
      reasonCode: (row?.reasonCode ?? "").trim(),
      reasonText: shippingUnavailableReasonText(row?.reasonCode),
    });
  }
  return notices;
}
