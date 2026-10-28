/**
 * TB-237 — قصد ثبت انتخاب capture‌شده، ساخته‌شده **فقط** از پاسخ موفق سرور.
 *
 * قواعد این ماژول عمداً سخت‌گیرانه و خالص‌اند:
 * - هیچ روش/تاریخ/مبلغ/مالکی حدس زده نمی‌شود؛ همه از همان `capturedPreview` موفق می‌آیند.
 * - مبلغ کالای هر گروه همان متن دقیق سرور است (بدون `Number`/شناور/گردکردن/`|| 0`).
 * - بازتاب snapshot/مبدأ/نسخه/ناحیه/ردیف برای هر روش انتخاب‌شده الزامی است؛ نبود هر کدام یعنی
 *   قصد ناقص و هیچ درخواستی ساخته نمی‌شود.
 * - روش با کلید متعارف سرویس/گزینهٔ **همان گروه** پیدا می‌شود، نه با برچسب ترجمه یا تطبیق فازی.
 */
import type {
  StorefrontCapturedGroupSelectionInput,
  StorefrontCapturedPreview,
  StorefrontCapturedPreviewGroup,
  StorefrontCapturedPreviewMethod,
} from "./storefront-shipping-api.ts";
import type { StorefrontCapturedGroupChoice } from "./storefront-shipping-group-cards.tsx";

/** متن اعشاری متعارف و غیرمنفی؛ همان شکلی که سرور تولید می‌کند (`"0"` صریح قانونی است). */
const EXACT_DECIMAL = /^\d+(\.\d+)?$/;

function methodOf(
  group: StorefrontCapturedPreviewGroup,
  choice: StorefrontCapturedGroupChoice,
): StorefrontCapturedPreviewMethod | null {
  return group.methods.find(
    (method) =>
      method.serviceCode === choice.serviceCode
      && (method.optionCode ?? "") === (choice.optionCode ?? ""),
  ) ?? null;
}

/**
 * آیا هر گروه قابل‌ارسال یک روش انتخاب‌شده با تاریخِ **مجاز همان روش** و مبلغ کالای دقیق دارد.
 * گروهی که هیچ روشی ندارد (با دلیل صریح) در این شمارش گروهِ قابل انتخاب نیست.
 */
export function capturedSelectionComplete(
  preview: StorefrontCapturedPreview | null,
  choices: Readonly<Record<string, StorefrontCapturedGroupChoice>>,
): boolean {
  if (!preview || preview.state !== "ok" || !preview.enabled) return false;
  const selectable = preview.groups.filter((group) => group.methods.length > 0);
  if (selectable.length === 0) return false;
  return selectable.every((group) => {
    const choice = choices[group.groupKey];
    if (!choice || !choice.date) return false;
    const method = methodOf(group, choice);
    if (!method) return false;
    if (!method.availableDates.some((slot) => slot.value === choice.date)) return false;
    return typeof group.finalGoodsAmountText === "string" && EXACT_DECIMAL.test(group.finalGoodsAmountText);
  });
}

/**
 * ساخت قصد گروه‌ها برای PUT انتخاب. `null` یعنی قصد ناقص/نامعتبر است و **نباید** ارسال شود؛ هیچ
 * fallback به shorthand قدیمی یا مبلغ صفر وجود ندارد.
 */
export function buildCapturedSelectionIntent(
  preview: StorefrontCapturedPreview | null,
  choices: Readonly<Record<string, StorefrontCapturedGroupChoice>>,
): StorefrontCapturedGroupSelectionInput[] | null {
  if (!preview || preview.state !== "ok" || !preview.enabled) return null;
  if (!capturedSelectionComplete(preview, choices)) return null;

  const intent: StorefrontCapturedGroupSelectionInput[] = [];
  for (const group of preview.groups) {
    if (group.methods.length === 0) continue;
    const choice = choices[group.groupKey];
    const method = methodOf(group, choice);
    const goods = group.finalGoodsAmountText;
    if (!method || !choice.date || !goods || !EXACT_DECIMAL.test(goods)) return null;
    const slot = method.availableDates.find((entry) => entry.value === choice.date);
    if (!slot) return null;
    // بازه از همان تاریخ مجاز همان روش می‌آید؛ نبودِ بازه یعنی «بدون بازه»، هرگز حدس.
    const window = slot.intervalText ?? "";

    intent.push({
      groupKey: group.groupKey,
      shippingMethodCode: method.serviceCode,
      serviceCode: method.serviceCode,
      optionCode: method.optionCode,
      cartLineIds: [...group.cartLineIds],
      selectedDeliveryDate: slot.value,
      selectedDeliveryTimeWindow: window,
      // متن دقیق سرور؛ بدون تبدیل عددی و بدون `|| 0`.
      expectedGoodsAmount: goods,
      confirmationSnapshotId: method.confirmationSnapshotId,
      confirmationSourceRevision: method.confirmationSourceRevision,
      confirmationSelectionVersion: method.confirmationSelectionVersion,
      confirmationAreaGroupId: method.confirmationAreaGroupId,
      confirmationRateRowId: method.confirmationRateRowId,
    });
  }

  return intent.length > 0 ? intent : null;
}

/**
 * هر تغییر context (مقصد/کوپن/نسخهٔ سبد/مبدأ پاسخ) تأییدهای قبلی را باطل می‌کند: کلید context از
 * همان مقادیر ساخته می‌شود و هر تغییر، انتخاب‌های گروهی را پاک می‌کند تا مشتری دوباره تأیید کند.
 */
export function capturedSelectionContextKey(input: {
  destinationKey: string;
  couponCode: string | null;
  cartVersion: number;
  snapshotId: string | null;
  selectionVersion: string | null;
}): string {
  return [
    input.destinationKey,
    input.couponCode?.trim() ?? "",
    String(input.cartVersion),
    input.snapshotId ?? "",
    input.selectionVersion ?? "",
  ].join("\u001f");
}
