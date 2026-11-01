/**
 * TB-245-R5 — خلاصهٔ مبلغ مسیر capture‌شده فقط از پاسخ تأییدشدهٔ سرور + انتخاب روش.
 *
 * جمع مبالغ با جمع صحیح‌مقیاس (بدون Number/parseFloat). carrier collect هرگز به قابل‌پرداخت اضافه نمی‌شود.
 */
import type {
  StorefrontCapturedPreview,
  StorefrontCapturedPreviewGroup,
  StorefrontCapturedPreviewMethod,
} from "./storefront-shipping-api.ts";
import type { StorefrontCapturedGroupChoice } from "./storefront-shipping-group-cards.tsx";

const EXACT_DECIMAL = /^\d+(\.\d+)?$/;

export type CapturedSummaryFeeRow = {
  groupKey: string;
  methodLabel: string;
  shippingAmountText: string;
  carrierCollectAmountText: string | null;
  currency: string;
};

export type CapturedShippingSummary =
  | { readonly kind: "hidden" }
  | { readonly kind: "empty" }
  | {
      readonly kind: "partial";
      readonly rows: readonly CapturedSummaryFeeRow[];
      readonly currency: string;
      readonly noteCode: "need_all_methods";
    }
  | {
      readonly kind: "ready";
      readonly rows: readonly CapturedSummaryFeeRow[];
      readonly currency: string;
      readonly goodsTotalText: string;
      readonly shippingTotalText: string;
      readonly payableTotalText: string;
      readonly carrierCollectTotalText: string | null;
      readonly shippingIsZero: boolean;
    }
  | { readonly kind: "unavailable" };

type Scaled = { readonly units: bigint; readonly scale: number };

function parseExactDecimal(text: string): Scaled | null {
  if (!EXACT_DECIMAL.test(text)) return null;
  const dot = text.indexOf(".");
  if (dot < 0) return { units: BigInt(text), scale: 0 };
  const whole = text.slice(0, dot);
  const frac = text.slice(dot + 1);
  if (whole.length === 0 || frac.length === 0) return null;
  return { units: BigInt(whole + frac), scale: frac.length };
}

function scaleUp(value: Scaled, targetScale: number): bigint {
  const delta = targetScale - value.scale;
  return value.units * BigInt(10) ** BigInt(delta);
}

/** جمع دقیق دو متن اعشاری غیرمنفی؛ هر ورودی نامعتبر ⇒ null. */
export function addExactDecimalTexts(left: string, right: string): string | null {
  const a = parseExactDecimal(left);
  const b = parseExactDecimal(right);
  if (!a || !b) return null;
  const scale = Math.max(a.scale, b.scale);
  const sum = scaleUp(a, scale) + scaleUp(b, scale);
  return formatScaled(sum, scale);
}

function formatScaled(units: bigint, scale: number): string {
  if (scale === 0) return units.toString();
  const zero = BigInt(0);
  const negative = units < zero;
  const abs = negative ? -units : units;
  const raw = abs.toString().padStart(scale + 1, "0");
  const whole = raw.slice(0, -scale);
  const frac = raw.slice(-scale);
  return `${negative ? "-" : ""}${whole}.${frac}`;
}

function sumExactDecimalTexts(values: readonly string[]): string | null {
  if (values.length === 0) return null;
  let acc = values[0]!;
  for (let i = 1; i < values.length; i += 1) {
    const next = addExactDecimalTexts(acc, values[i]!);
    if (next === null) return null;
    acc = next;
  }
  return acc;
}

function isExactZero(text: string): boolean {
  const parsed = parseExactDecimal(text);
  return parsed !== null && parsed.units === BigInt(0);
}

function methodOf(
  group: StorefrontCapturedPreviewGroup,
  choice: StorefrontCapturedGroupChoice,
): StorefrontCapturedPreviewMethod | null {
  return (
    group.methods.find(
      (method) =>
        method.serviceCode === choice.serviceCode
        && (method.optionCode ?? "") === (choice.optionCode ?? ""),
    ) ?? null
  );
}

function nonBlank(value: string | null | undefined): value is string {
  return typeof value === "string" && value.trim().length > 0;
}

function resolveSelectedFee(
  group: StorefrontCapturedPreviewGroup,
  choice: StorefrontCapturedGroupChoice | undefined,
  expectedCurrency: string,
): CapturedSummaryFeeRow | "missing" | "invalid" {
  if (!choice) return "missing";
  const method = methodOf(group, choice);
  if (!method) return "invalid";
  const shipping = method.payableAtCheckoutAmountText;
  const currency = method.currency;
  if (!nonBlank(shipping) || !EXACT_DECIMAL.test(shipping)) return "invalid";
  if (!nonBlank(currency) || currency !== expectedCurrency) return "invalid";
  const carrier = method.carrierCollectAmountText;
  if (carrier != null && (!nonBlank(carrier) || !EXACT_DECIMAL.test(carrier))) return "invalid";
  return {
    groupKey: group.groupKey,
    methodLabel: method.methodLabel,
    shippingAmountText: shipping,
    carrierCollectAmountText: carrier,
    currency,
  };
}

/**
 * @param confirmed — فقط وقتی هویت مقصد/کوپن جاری است true؛ در pending/failed خلاصه مخفی می‌ماند.
 */
export function projectCapturedShippingSummary(input: {
  preview: StorefrontCapturedPreview | null;
  choices: Readonly<Record<string, StorefrontCapturedGroupChoice>>;
  confirmed: boolean;
}): CapturedShippingSummary {
  if (!input.confirmed) return { kind: "hidden" };
  const preview = input.preview;
  if (!preview || preview.state !== "ok" || !preview.enabled) return { kind: "unavailable" };
  if (preview.groups.length === 0) return { kind: "empty" };
  const currency = preview.currency;
  if (!nonBlank(currency)) return { kind: "unavailable" };

  const rows: CapturedSummaryFeeRow[] = [];
  let missingMethod = false;
  for (const group of preview.groups) {
    const resolved = resolveSelectedFee(group, input.choices[group.groupKey], currency);
    if (resolved === "missing") {
      missingMethod = true;
      continue;
    }
    if (resolved === "invalid") return { kind: "unavailable" };
    rows.push(resolved);
  }

  if (missingMethod || rows.length < preview.groups.length) {
    return {
      kind: "partial",
      rows,
      currency,
      noteCode: "need_all_methods",
    };
  }

  const goodsParts: string[] = [];
  for (const group of preview.groups) {
    const goods = group.finalGoodsAmountText;
    if (!nonBlank(goods) || !EXACT_DECIMAL.test(goods)) return { kind: "unavailable" };
    goodsParts.push(goods);
  }
  const goodsTotalText = sumExactDecimalTexts(goodsParts);
  const shippingTotalText = sumExactDecimalTexts(rows.map((row) => row.shippingAmountText));
  if (goodsTotalText === null || shippingTotalText === null) return { kind: "unavailable" };
  const payableTotalText = addExactDecimalTexts(goodsTotalText, shippingTotalText);
  if (payableTotalText === null) return { kind: "unavailable" };

  const carrierParts = rows
    .map((row) => row.carrierCollectAmountText)
    .filter((value): value is string => value != null && !isExactZero(value));
  let carrierCollectTotalText: string | null = null;
  if (carrierParts.length > 0) {
    const summed = sumExactDecimalTexts(carrierParts);
    if (summed === null) return { kind: "unavailable" };
    carrierCollectTotalText = isExactZero(summed) ? null : summed;
  }

  return {
    kind: "ready",
    rows,
    currency,
    goodsTotalText,
    shippingTotalText,
    payableTotalText,
    carrierCollectTotalText,
    shippingIsZero: isExactZero(shippingTotalText),
  };
}
