import { formatQuantityDisplay } from "../../lib/quantity-display";

export const PHYSICAL_SPEC_VALIDATION_FA =
  "وزن و ابعاد فیزیکی در صورت ورود باید عدد مثبت باشند (گرم / سانتی‌متر).";

export type PhysicalSpecificationDraft = {
  weightGrams: string;
  lengthCentimeters: string;
  widthCentimeters: string;
  heightCentimeters: string;
};

export type PhysicalSpecificationValues = {
  weightGrams: number | null;
  lengthCentimeters: number | null;
  widthCentimeters: number | null;
  heightCentimeters: number | null;
};

export function formatOptionalDecimal(value: number | null | undefined): string {
  if (value == null) return "";
  return formatQuantityDisplay(value, 6);
}

export function parseOptionalPositiveDecimal(
  text: string,
): { ok: true; value: number | null } | { ok: false } {
  const trimmed = text.trim();
  if (trimmed.length === 0) return { ok: true, value: null };
  const normalized = trimmed.replace(",", ".");
  const num = Number(normalized);
  if (!Number.isFinite(num) || num <= 0) return { ok: false };
  return { ok: true, value: num };
}

/** Typed weight writes require whole grams (never silently round/truncate). */
export function parseOptionalWholeGrams(
  text: string,
): { ok: true; value: number | null } | { ok: false } {
  const parsed = parseOptionalPositiveDecimal(text);
  if (!parsed.ok) return parsed;
  if (parsed.value == null) return parsed;
  if (!Number.isInteger(parsed.value)) return { ok: false };
  return parsed;
}

export function physicalDraftFromValues(values: PhysicalSpecificationValues): PhysicalSpecificationDraft {
  return {
    weightGrams: formatOptionalDecimal(values.weightGrams),
    lengthCentimeters: formatOptionalDecimal(values.lengthCentimeters),
    widthCentimeters: formatOptionalDecimal(values.widthCentimeters),
    heightCentimeters: formatOptionalDecimal(values.heightCentimeters),
  };
}

export function parsePhysicalSpecificationDraft(
  draft: PhysicalSpecificationDraft,
): { ok: true; values: PhysicalSpecificationValues } | { ok: false } {
  const weightParsed = parseOptionalWholeGrams(draft.weightGrams);
  const lengthParsed = parseOptionalPositiveDecimal(draft.lengthCentimeters);
  const widthParsed = parseOptionalPositiveDecimal(draft.widthCentimeters);
  const heightParsed = parseOptionalPositiveDecimal(draft.heightCentimeters);
  if (!weightParsed.ok || !lengthParsed.ok || !widthParsed.ok || !heightParsed.ok) {
    return { ok: false };
  }
  return {
    ok: true,
    values: {
      weightGrams: weightParsed.value,
      lengthCentimeters: lengthParsed.value,
      widthCentimeters: widthParsed.value,
      heightCentimeters: heightParsed.value,
    },
  };
}
