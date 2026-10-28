/**
 * TB-237-R1 — قصد ثبت انتخاب capture‌شده، ساخته‌شده **فقط** از پاسخ موفق سرور.
 *
 * قواعد این ماژول عمداً سخت‌گیرانه و خالص‌اند:
 * - هیچ روش/تاریخ/مبلغ/مالکی حدس زده نمی‌شود؛ همه از همان `capturedPreview` موفق می‌آیند.
 * - مبلغ کالای هر گروه همان متن دقیق سرور است (بدون `Number`/شناور/گردکردن/`|| 0`).
 * - کلیدهای ارسالی نام‌های **نوشتاری** سرور هستند (`expected*`)؛ `confirmation*` فقط نام خواندن است.
 * - **همهٔ** گروه‌های سرور لازم‌اند: گروه بدون روش (غیرقابل‌ارسال) حذف نمی‌شود، بلکه کل قصد را
 *   مسدود می‌کند تا هیچ سبد ناقصی به‌صورت «ارسال جزئی» ثبت نشود.
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

function nonBlank(value: string | null | undefined): value is string {
  return typeof value === "string" && value.trim().length > 0;
}

/**
 * دلیل مسدودبودن ادامهٔ مسیر (یا `null` وقتی همه‌چیز کامل است).
 *
 * `unavailable` یعنی دست‌کم یک گروه سرور هیچ روش قابل‌انتخابی ندارد؛ چنین گروهی **باید** روی صفحه
 * بماند و با دلیل خودش ادامه را ببندد، نه اینکه از قصد حذف شود.
 */
export function capturedSelectionBlocker(
  preview: StorefrontCapturedPreview | null,
  choices: Readonly<Record<string, StorefrontCapturedGroupChoice>>,
): "no_preview" | "unavailable_group" | "incomplete_group" | "slot_required" | null {
  if (!preview || preview.state !== "ok" || !preview.enabled) return "no_preview";
  if (preview.groups.length === 0) return "no_preview";
  for (const group of preview.groups) {
    if (group.methods.length === 0) return "unavailable_group";
    const choice = choices[group.groupKey];
    if (!choice || !choice.date) return "incomplete_group";
    const method = methodOf(group, choice);
    if (!method) return "incomplete_group";
    const date = method.availableDates.find((slot) => slot.value === choice.date);
    if (!date) return "incomplete_group";
    // TB-241: روزی که چند نوبت دارد تا انتخاب صریح یک نوبت از همان پاسخ کامل نیست؛ روزی با یک نوبت
    // همان یک نوبت را دارد و انتخاب تاریخ کافی است.
    if ((date.slots ?? []).length > 1) {
      const chosen = (date.slots ?? []).some(
        (slot) => slot.startTicks === choice.slotStartTicks && slot.endTicks === choice.slotEndTicks,
      );
      if (!chosen) return "slot_required";
    }
    // هر پنج بازتاب و متن دقیق مبلغ کالا باید همان چیزی باشد که در همین پاسخ دیدیم.
    if (
      !nonBlank(method.confirmationSnapshotId)
      || !nonBlank(method.confirmationSourceRevision)
      || !nonBlank(method.confirmationSelectionVersion)
      || !nonBlank(method.confirmationAreaGroupId)
      || !nonBlank(method.confirmationRateRowId)
    ) {
      return "no_preview";
    }
    if (!nonBlank(group.finalGoodsAmountText) || !EXACT_DECIMAL.test(group.finalGoodsAmountText)) {
      return "no_preview";
    }
  }
  return null;
}

/**
 * آیا **همهٔ** گروه‌های سرور انتخاب معتبر دارند. گروه غیرقابل‌ارسال این را `false` می‌کند (نه اینکه
 * نادیده گرفته شود).
 */
export function capturedSelectionComplete(
  preview: StorefrontCapturedPreview | null,
  choices: Readonly<Record<string, StorefrontCapturedGroupChoice>>,
): boolean {
  return capturedSelectionBlocker(preview, choices) === null;
}

/**
 * TB-237-R1 — نسخهٔ ریشهٔ CAS برای PUT انتخاب، فقط از مقدارِ اثبات‌شدهٔ سرور.
 *
 * چرا این تابع لازم است: نمای قدیمی `draft` می‌تواند هنگام ورود دوباره به صفحه **غایب** باشد
 * (مسیر بازیابی، «انتخاب دیگر معتبر نیست») در حالی که ریشهٔ capture‌شده با نسخهٔ بزرگ‌تر از صفر
 * همچنان ذخیره است. در آن حالت فرستادن «0» یک conflict واقعی می‌سازد
 * (`storefront.captured_selection.stale`). نسخهٔ داخل `capturedPreview.selectionRevision` هم از
 * همان ریشهٔ ذخیره‌شده و توسط سرور ساخته می‌شود؛ «0» تنها یعنی ریشه اثباتاً غایب است.
 *
 * `null` یعنی هیچ نسخهٔ قابل‌استنادی نرسیده و اینجا باید محلی رد شود — صفر حدس زده نمی‌شود.
 */
export function resolveCapturedSelectionRevision(input: {
  draftRevision?: string | null;
  previewRevision?: string | null;
}): string | null {
  const digits = (value: string | null | undefined): string | null =>
    typeof value === "string" && /^\d+$/.test(value) ? value : null;
  return digits(input.draftRevision) ?? digits(input.previewRevision);
}

/**
 * ساخت قصد گروه‌ها برای PUT انتخاب. `null` یعنی قصد ناقص/نامعتبر است و **نباید** ارسال شود؛ هیچ
 * fallback به shorthand قدیمی یا مبلغ صفر وجود ندارد.
 */
export function buildCapturedSelectionIntent(
  preview: StorefrontCapturedPreview | null,
  choices: Readonly<Record<string, StorefrontCapturedGroupChoice>>,
): StorefrontCapturedGroupSelectionInput[] | null {
  if (capturedSelectionBlocker(preview, choices) !== null || !preview) return null;

  const intent: StorefrontCapturedGroupSelectionInput[] = [];
  for (const group of preview.groups) {
    const choice = choices[group.groupKey];
    const method = methodOf(group, choice);
    const goods = group.finalGoodsAmountText;
    if (!method || !choice.date || !goods || !EXACT_DECIMAL.test(goods)) return null;
    const slot = method.availableDates.find((entry) => entry.value === choice.date);
    if (!slot) return null;
    // TB-241: تنها نوبتِ همان تاریخ از همان پاسخ. روز چندنوبتی بدون انتخاب صریح قصد معتبر ندارد.
    const chosenSlot = (slot.slots ?? []).find(
      (entry) => entry.startTicks === choice.slotStartTicks && entry.endTicks === choice.slotEndTicks,
    ) ?? null;
    if ((slot.slots ?? []).length > 1 && !chosenSlot) return null;

    intent.push({
      groupKey: group.groupKey,
      serviceCode: method.serviceCode,
      optionCode: method.optionCode,
      selectedDeliveryDate: slot.value,
      // متن دقیق سرور، فقط برای مقایسه؛ نه قیمت و نه مقدار پیش‌فرض.
      expectedGoodsAmount: goods,
      expectedSnapshotId: method.confirmationSnapshotId as string,
      expectedSourceRevision: method.confirmationSourceRevision as string,
      expectedSelectionVersion: method.confirmationSelectionVersion as string,
      expectedAreaGroupId: method.confirmationAreaGroupId as string,
      expectedRateRowId: method.confirmationRateRowId as string,
      selectedSlotStartTicks: chosenSlot?.startTicks ?? null,
      selectedSlotEndTicks: chosenSlot?.endTicks ?? null,
    });
  }

  return intent.length > 0 ? intent : null;
}

/**
 * هر تغییر context (مقصد/کوپن/نسخهٔ سبد/هویت کامل پاسخ) تأییدهای قبلی را باطل می‌کند.
 *
 * کلید فقط از هویت **تأییدشدهٔ** سرور ساخته می‌شود: برای هر گروه، کلید گروه + همهٔ بازتاب‌های
 * روش‌هایش + مبلغ کالای دقیق. بنابراین تغییر قیمت/تقویم/ناحیه/ردیف/مبدأ پاسخ، حتی با snapshot
 * ظاهراً یکسان، انتخاب قدیمی را نگه نمی‌دارد.
 */
export function capturedSelectionContextKey(input: {
  destinationKey: string;
  couponCode: string | null;
  cartVersion: number;
  preview: StorefrontCapturedPreview | null;
}): string {
  const preview = input.preview;
  const identity = preview && preview.state === "ok"
    ? preview.groups
        .map((group) => [
          group.groupKey,
          group.finalGoodsAmountText ?? "",
          ...group.methods.map((method) => [
            method.serviceCode,
            method.optionCode ?? "",
            method.confirmationSnapshotId ?? "",
            method.confirmationSourceRevision ?? "",
            method.confirmationSelectionVersion ?? "",
            method.confirmationAreaGroupId ?? "",
            method.confirmationRateRowId ?? "",
            method.availableDates.map((slot) => slot.value).join(","),
            method.availableDates
              .map((slot) => (slot.slots ?? []).map((entry) => `${entry.startTicks}-${entry.endTicks}`).join(","))
              .join(","),
          ].join("|")),
        ].join("~"))
        .join("^")
    : "";
  return [
    input.destinationKey,
    input.couponCode?.trim() ?? "",
    String(input.cartVersion),
    identity,
  ].join("\u001f");
}
