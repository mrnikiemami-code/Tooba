/**
 * TB-245-R6 — advisory بازاعتبارسنجی projection جدا از error عملیاتی شبکه/ذخیره.
 *
 * فقط از هویت پاسخ جاری + انتخاب‌های reconcile‌شده مشتق می‌شود؛ با کامل‌شدن انتخاب
 * همهٔ گروه‌ها خاموش می‌شود بدون HTTP دوباره. انتخاب محلی error عملیاتی را پاک نمی‌کند.
 */
import type { StorefrontCapturedPreview } from "./storefront-shipping-api.ts";
import type { StorefrontCapturedGroupChoice } from "./storefront-shipping-group-cards.tsx";
import { capturedSelectionComplete } from "./storefront-shipping-captured-selection.ts";
import { resolveShippingContinueState } from "./storefront-shipping-continue-state.ts";

export type CapturedRevalidationAdvisory = {
  readonly kind: "selection_stale" | "group_reselect";
  readonly text: string;
  readonly groupKey: string | null;
  readonly sellerPartyId: string | null;
};

function nonBlank(value: string | null | undefined): value is string {
  return typeof value === "string" && value.trim().length > 0;
}

function groupLabel(
  groupKey: string | null,
  sellerPartyId: string | null,
  sellerLabelByPartyId: Readonly<Record<string, string>>,
  locale: string,
): string {
  if (sellerPartyId && nonBlank(sellerLabelByPartyId[sellerPartyId])) {
    return sellerLabelByPartyId[sellerPartyId]!;
  }
  if (groupKey && nonBlank(sellerLabelByPartyId[groupKey])) {
    return sellerLabelByPartyId[groupKey]!;
  }
  return locale === "en" ? "this group" : "این گروه";
}

/**
 * @param projectionMessage — فقط پیام همین پاسخ projection جاری (نه state کهنهٔ error).
 * @param confirmed — هویت مقصد/کوپن تأییدشده؛ در pending/failed هیچ advisory نمایش داده نمی‌شود.
 */
export function resolveCapturedRevalidationAdvisory(input: {
  confirmed: boolean;
  preview: StorefrontCapturedPreview | null;
  choices: Readonly<Record<string, StorefrontCapturedGroupChoice>>;
  projectionMessage: string | null;
  sellerLabelByPartyId: Readonly<Record<string, string>>;
  locale: string;
}): CapturedRevalidationAdvisory | null {
  if (!input.confirmed) return null;
  const message = nonBlank(input.projectionMessage) ? input.projectionMessage.trim() : null;
  if (!message) return null;

  if (capturedSelectionComplete(input.preview, input.choices)) {
    return null;
  }

  const continueState = resolveShippingContinueState({
    address: {
      recipient: true,
      contactMobile: true,
      province: true,
      city: true,
      postalAddress: true,
      postalCode: true,
    },
    captured: {
      active: true,
      confirmed: true,
      preview: input.preview,
      choices: input.choices,
    },
    legacy: { available: false, methodReady: false, scheduleReady: false },
  });

  const groupKey = continueState.blocker?.groupKey ?? null;
  const group = groupKey
    ? input.preview?.groups.find((entry) => entry.groupKey === groupKey) ?? null
    : null;
  const sellerPartyId = group?.sellerPartyId ?? null;
  const named = groupLabel(groupKey, sellerPartyId, input.sellerLabelByPartyId, input.locale);

  if (groupKey) {
    const text =
      input.locale === "en"
        ? `Reselect delivery for «${named}». ${message}`
        : `برای «${named}» دوباره روش/زمان ارسال را انتخاب کنید. ${message}`;
    return {
      kind: "group_reselect",
      text,
      groupKey,
      sellerPartyId,
    };
  }

  return {
    kind: "selection_stale",
    text: message,
    groupKey: null,
    sellerPartyId: null,
  };
}

/** برچسب نمایشی ردیف هزینه: فروشنده — روش؛ بدون GUID خام. */
export function formatCapturedFeeRowLabel(input: {
  methodLabel: string;
  sellerPartyId: string | null;
  sellerLabelByPartyId: Readonly<Record<string, string>>;
  locale: string;
}): string {
  const method = nonBlank(input.methodLabel) ? input.methodLabel.trim() : (input.locale === "en" ? "Shipping" : "ارسال");
  const seller = input.sellerPartyId && nonBlank(input.sellerLabelByPartyId[input.sellerPartyId])
    ? input.sellerLabelByPartyId[input.sellerPartyId]!.trim()
    : (input.locale === "en" ? "This group" : "این گروه");
  return `${seller} — ${method}`;
}
