/**
 * TB-SHIPPING-FULL-OPERATIONS-074 — «شرایط مرسوله» form model for the per-service rates & coverage
 * dialog. This is the presentation/validation layer for the *already canonical* 073 tier contract
 * (`condition: { weightGrams: { min, max }, itemCount: { min, max } }`); it never redefines the
 * pricing semantics and never invents a second rule engine.
 *
 * Two decisions worth stating explicitly:
 *
 * 1. **Grams on the wire, kilograms for humans.** The API carries integer grams so a boundary is
 *    exact. A human thinks in kilograms, so the weight inputs are kilograms and are converted with
 *    string/integer arithmetic only — never through floating point — so `1.999` becomes exactly
 *    `1999` grams and cannot drift.
 * 2. **Half-open, always.** Every interval is `[min, max)`: «از شامل، تا غیرشامل». An empty upper
 *    bound means "no upper limit", never zero.
 */

/** Wire shape of one axis: `min` inclusive, `max` exclusive (`null` = open upper bound). */
export type ShippingTierAxisWrite = {
  min: number;
  max: number | null;
};

/**
 * Wire shape of a rate condition, matching the backend `ShippingRateConditionWrite` exactly.
 * Both axes are optional; `null` on the whole condition means an unconditioned (base) rule.
 */
export type ShippingRateConditionWrite = {
  weightGrams?: ShippingTierAxisWrite | null;
  itemCount?: ShippingTierAxisWrite | null;
};

/** One editable axis of the condition form. Weight is entered in kilograms, count in items. */
export type ShippingConditionAxisForm = {
  enabled: boolean;
  min: string;
  max: string;
};

export type ShippingRateConditionForm = {
  weight: ShippingConditionAxisForm;
  count: ShippingConditionAxisForm;
};

/** Backend ceilings, mirrored so a malformed bound is refused before any API call. */
export const MAX_WEIGHT_GRAMS = 1_000_000_000;
export const MAX_ITEM_COUNT = 1_000_000;

/** An untouched form: neither axis enabled, i.e. an unconditioned rule. */
export const EMPTY_CONDITION_FORM: ShippingRateConditionForm = {
  weight: { enabled: false, min: "", max: "" },
  count: { enabled: false, min: "", max: "" },
};

/** Stable backend error codes this section maps to adjacent Persian guidance. */
export const TIER_ERROR_CODES = {
  tierOverlap: "shipping.rate_policy.tier_overlap",
  scopeDuplicate: "shipping.rate_policy.scope_duplicate",
  tierRangeInvalid: "shipping.validation.tier_range_invalid",
  tierMinNegative: "shipping.validation.tier_min_negative",
  tierOutOfRange: "shipping.validation.tier_out_of_range",
} as const;

/**
 * Exact kilogram → gram conversion with no floating point.
 *
 * Only a plain decimal with at most three fractional digits is accepted, because grams are the
 * smallest unit the API expresses. Anything finer (for example `1.0005`) is refused instead of
 * being silently rounded, which is what keeps `1999` and `2000` from ever collapsing together.
 */
export function parseKilogramsToGrams(raw: string): number | null {
  const trimmed = raw.trim();
  if (trimmed === "") return null;
  const match = /^(\d+)(?:\.(\d{1,3}))?$/.exec(trimmed);
  if (!match) return null;
  const whole = Number(match[1]);
  if (!Number.isSafeInteger(whole)) return null;
  const fraction = (match[2] ?? "").padEnd(3, "0");
  const grams = whole * 1000 + Number(fraction);
  return Number.isSafeInteger(grams) ? grams : null;
}

/** Exact gram → kilogram text, with trailing zeros trimmed (`1999` → `1.999`, `2000` → `2`). */
export function formatGramsAsKilograms(grams: number): string {
  if (!Number.isFinite(grams)) return "";
  const whole = Math.trunc(grams / 1000);
  const fraction = Math.abs(grams % 1000);
  if (fraction === 0) return String(whole);
  return `${whole}.${String(fraction).padStart(3, "0").replace(/0+$/, "")}`;
}

/** Non-negative integer item count, entered directly in items. */
export function parseItemCount(raw: string): number | null {
  const trimmed = raw.trim();
  if (!/^\d+$/.test(trimmed)) return null;
  const parsed = Number(trimmed);
  return Number.isSafeInteger(parsed) ? parsed : null;
}

/**
 * Reads the persisted condition of an existing rate into the form, losslessly.
 *
 * A stored `null` (or a condition with no axis) produces the untouched form, so saving an existing
 * unconditioned rate can never silently turn it into a restricted tier.
 */
export function conditionFormFromPolicy(
  condition: ShippingRateConditionWrite | null | undefined,
): ShippingRateConditionForm {
  const weight = condition?.weightGrams ?? null;
  const count = condition?.itemCount ?? null;
  return {
    weight: {
      enabled: weight != null,
      min: weight ? formatGramsAsKilograms(weight.min) : "",
      max: weight && weight.max != null ? formatGramsAsKilograms(weight.max) : "",
    },
    count: {
      enabled: count != null,
      min: count ? String(count.min) : "",
      max: count && count.max != null ? String(count.max) : "",
    },
  };
}

/**
 * Converts the form to the exact wire shape: `null` for an unconditioned rule, otherwise only the
 * axes that are actually enabled. Call only after {@link validateConditionForm} passed.
 */
export function conditionFormToWrite(form: ShippingRateConditionForm): ShippingRateConditionWrite | null {
  const weight = axisToWrite(form.weight, parseKilogramsToGrams);
  const count = axisToWrite(form.count, parseItemCount);
  if (!weight && !count) return null;
  return { weightGrams: weight, itemCount: count };
}

function axisToWrite(
  axis: ShippingConditionAxisForm,
  parse: (raw: string) => number | null,
): ShippingTierAxisWrite | null {
  if (!axis.enabled) return null;
  const min = parse(axis.min);
  if (min == null) return null;
  const max = axis.max.trim() === "" ? null : parse(axis.max);
  return { min, max };
}

/**
 * Validates the condition form. Weight messages quote kilograms *and* the exact gram value so a
 * user never has to guess how their input was interpreted.
 */
export function validateConditionForm(form: ShippingRateConditionForm): string[] {
  const issues: string[] = [];
  validateAxis(
    form.weight,
    parseKilogramsToGrams,
    MAX_WEIGHT_GRAMS,
    {
      labelFa: "وزن",
      unitFa: "کیلوگرم",
      minMissingFa: "کران پایین وزن را وارد کنید (کیلوگرم).",
      minInvalidFa: "کران پایین وزن باید عددی نامنفی با حداکثر سه رقم اعشار باشد (مثال: 1.999).",
      maxInvalidFa: "کران بالای وزن باید عددی نامنفی با حداکثر سه رقم اعشار باشد یا خالی بماند.",
      minOutOfRangeFa: "کران پایین وزن از سقف مجاز (۱۰۰۰ تن) بیشتر است.",
      maxOutOfRangeFa: "کران بالای وزن از سقف مجاز (۱۰۰۰ تن) بیشتر است.",
    },
    issues,
  );
  validateAxis(
    form.count,
    parseItemCount,
    MAX_ITEM_COUNT,
    {
      labelFa: "تعداد اقلام",
      unitFa: "قلم",
      minMissingFa: "کران پایین تعداد اقلام را وارد کنید.",
      minInvalidFa: "کران پایین تعداد اقلام باید عددی صحیح و نامنفی باشد.",
      maxInvalidFa: "کران بالای تعداد اقلام باید عددی صحیح و نامنفی باشد یا خالی بماند.",
      minOutOfRangeFa: "کران پایین تعداد اقلام از سقف مجاز (۱٬۰۰۰٬۰۰۰) بیشتر است.",
      maxOutOfRangeFa: "کران بالای تعداد اقلام از سقف مجاز (۱٬۰۰۰٬۰۰۰) بیشتر است.",
    },
    issues,
  );
  return issues;
}

type AxisMessages = {
  labelFa: string;
  unitFa: string;
  minMissingFa: string;
  minInvalidFa: string;
  maxInvalidFa: string;
  minOutOfRangeFa: string;
  maxOutOfRangeFa: string;
};

function validateAxis(
  axis: ShippingConditionAxisForm,
  parse: (raw: string) => number | null,
  ceiling: number,
  messages: AxisMessages,
  issues: string[],
): void {
  if (!axis.enabled) return;
  const min = parse(axis.min);
  if (min == null) {
    issues.push(axis.min.trim() === "" ? messages.minMissingFa : messages.minInvalidFa);
  } else if (min < 0) {
    issues.push(messages.minInvalidFa);
  } else if (min > ceiling) {
    issues.push(messages.minOutOfRangeFa);
  }

  const maxText = axis.max.trim();
  if (maxText !== "") {
    const max = parse(maxText);
    if (max == null) {
      issues.push(messages.maxInvalidFa);
    } else if (max > ceiling) {
      issues.push(messages.maxOutOfRangeFa);
    } else if (min != null && max <= min) {
      // Half-open: a zero-width or reversed range can never match a parcel, so it is refused here
      // with the same meaning the backend reports as `tier_range_invalid`.
      issues.push(
        `کران بالای ${messages.labelFa} باید بزرگتر از کران پایین باشد؛ بازهٔ نیمباز [از شامل، تا غیرشامل) با کرانهای مساوی یا معکوس بیمعناست.`,
      );
    }
  }
}

function faNumber(value: number): string {
  return value.toLocaleString("fa-IR");
}

/**
 * Human description of one axis: «از شامل، تا غیرشامل», with kilograms for weight and an explicit
 * gram value so the boundary is unambiguous.
 */
export function describeAxisFa(
  axis: ShippingTierAxisWrite,
  kind: "weight" | "count",
): string {
  if (kind === "weight") {
    const from = `${faNumber(axis.min)} گرم (${formatGramsAsKilograms(axis.min)} کیلوگرم) شامل`;
    if (axis.max == null) return `وزن از ${from} — بدون کران بالا`;
    return `وزن از ${from}، تا ${faNumber(axis.max)} گرم (${formatGramsAsKilograms(axis.max)} کیلوگرم) غیرشامل`;
  }
  const from = `${faNumber(axis.min)} قلم شامل`;
  if (axis.max == null) return `تعداد اقلام از ${from} — بدون کران بالا`;
  return `تعداد اقلام از ${from}، تا ${faNumber(axis.max)} قلم غیرشامل`;
}

/** Explicit text for the unconditioned case; never rendered as a zero interval. */
export const UNCONDITIONED_FA = "بدون شرط اندازه — برای همهٔ مرسولهها";

/**
 * Full Persian description of a persisted condition, shown in every rate row. Both axes present
 * means an intersection, and the text says so instead of implying either axis alone.
 */
export function describeConditionFa(condition: ShippingRateConditionWrite | null | undefined): string {
  const weight = condition?.weightGrams ?? null;
  const count = condition?.itemCount ?? null;
  if (!weight && !count) return UNCONDITIONED_FA;
  const parts: string[] = [];
  if (weight) parts.push(describeAxisFa(weight, "weight"));
  if (count) parts.push(describeAxisFa(count, "count"));
  return parts.length > 1 ? `${parts.join(" و ")} — هر دو شرط باید همزمان برقرار باشند` : parts[0];
}

/** Compact one-line form for dense rows and validation summaries. */
export function conditionSummaryFa(condition: ShippingRateConditionWrite | null | undefined): string {
  const weight = condition?.weightGrams ?? null;
  const count = condition?.itemCount ?? null;
  if (!weight && !count) return UNCONDITIONED_FA;
  const parts: string[] = [];
  if (weight) {
    parts.push(
      weight.max == null
        ? `وزن ≥ ${formatGramsAsKilograms(weight.min)}kg`
        : `وزن [${formatGramsAsKilograms(weight.min)}، ${formatGramsAsKilograms(weight.max)})kg`,
    );
  }
  if (count) {
    parts.push(count.max == null ? `اقلام ≥ ${count.min}` : `اقلام [${count.min}، ${count.max})`);
  }
  return parts.join(" ∩ ");
}

/** Does this condition restrict pricing at all? */
export function isConditioned(condition: ShippingRateConditionWrite | null | undefined): boolean {
  return Boolean(condition?.weightGrams || condition?.itemCount);
}

/**
 * Adjacent Persian guidance for the stable tier error codes, so a refusal is actionable instead of
 * a bare code. Anything unknown returns `null` and the caller keeps its own fallback text.
 */
export function tierErrorFa(code: string | undefined | null): string | null {
  if (!code) return null;
  if (code === TIER_ERROR_CODES.tierOverlap) {
    return "این بازهٔ اندازه با یک پلهٔ فعال دیگر در همان خدمت/گزینه/دامنه همپوشانی دارد؛ بازهٔ نیمباز را طوری تغییر دهید که با پلههای موجود تلاقی نکند.";
  }
  if (code === TIER_ERROR_CODES.scopeDuplicate) {
    return "برای همین خدمت/گزینه/دامنه با همین بازهٔ اندازه از قبل نرخی ثبت شده است.";
  }
  if (code === TIER_ERROR_CODES.tierRangeInvalid) {
    return "بازهٔ اندازه نامعتبر است: کران بالای نیمباز باید بزرگتر از کران پایین باشد.";
  }
  if (code === TIER_ERROR_CODES.tierMinNegative) {
    return "کران پایین اندازه نمیتواند منفی باشد.";
  }
  if (code === TIER_ERROR_CODES.tierOutOfRange) {
    return "کران اندازه از سقف مجاز بیشتر است.";
  }
  return null;
}
