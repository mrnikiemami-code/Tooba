import type {
  ProductAttributeEditorField,
  ProductAttributeValueInput,
} from "./catalog-attribute-api.ts";

export type AttributeDraftValue = {
  rawValue: string;
  enumOptionId: string;
  multiOptionIds: string[];
  clear: boolean;
};

function nGuidToD(part: string): string {
  if (part.length !== 32) return part;
  return `${part.slice(0, 8)}-${part.slice(8, 12)}-${part.slice(12, 16)}-${part.slice(16, 20)}-${part.slice(20)}`;
}

/** پیش‌نویس اولیه از مقدار ذخیره‌شدهٔ فیلد. */
export function draftFromField(field: ProductAttributeEditorField): AttributeDraftValue {
  if (field.isMultivalue && field.valueKind === "Enumeration") {
    const ids = (field.currentCanonicalValue ?? "")
      .split(",")
      .map((x) => x.trim())
      .filter(Boolean)
      .map(nGuidToD);
    return { rawValue: "", enumOptionId: "", multiOptionIds: ids, clear: false };
  }
  return {
    rawValue:
      field.valueKind === "Enumeration"
        ? ""
        : field.valueKind === "Boolean"
          ? field.currentCanonicalValue === "True" || field.currentCanonicalValue === "true"
            ? "true"
            : field.currentCanonicalValue === "False" || field.currentCanonicalValue === "false"
              ? "false"
              : ""
          : (field.currentCanonicalValue ?? ""),
    enumOptionId: field.currentEnumOptionId ?? "",
    multiOptionIds: [],
    clear: false,
  };
}

/** آیا پیش‌نویس با مقدار ذخیره‌شده فرق دارد. */
export function isAttributeDraftDirty(
  field: ProductAttributeEditorField,
  draft: AttributeDraftValue,
): boolean {
  if (field.isVariantAxis) return false;
  if (draft.clear) return Boolean(field.currentCanonicalValue);
  const baseline = draftFromField(field);
  if (field.isMultivalue && field.valueKind === "Enumeration") {
    const a = [...draft.multiOptionIds].map((x) => x.toLowerCase()).sort();
    const b = [...baseline.multiOptionIds].map((x) => x.toLowerCase()).sort();
    return a.join(",") !== b.join(",");
  }
  if (field.valueKind === "Enumeration") {
    return (draft.enumOptionId || "") !== (baseline.enumOptionId || "");
  }
  return (draft.rawValue || "") !== (baseline.rawValue || "");
}

/** ساخت ورودی API از پیش‌نویس یک فیلد. */
export function draftToValueInput(
  field: ProductAttributeEditorField,
  draft: AttributeDraftValue,
): ProductAttributeValueInput | null {
  if (field.isVariantAxis) return null;
  if (draft.clear) {
    return { definitionId: field.definitionId, clear: true };
  }
  if (field.valueKind === "Enumeration" && field.isMultivalue) {
    if (draft.multiOptionIds.length === 0) {
      return field.currentCanonicalValue
        ? { definitionId: field.definitionId, clear: true }
        : null;
    }
    return {
      definitionId: field.definitionId,
      rawValue: draft.multiOptionIds.join(","),
      clear: false,
    };
  }
  if (field.valueKind === "Enumeration") {
    if (!draft.enumOptionId.trim()) {
      return field.currentCanonicalValue
        ? { definitionId: field.definitionId, clear: true }
        : null;
    }
    return {
      definitionId: field.definitionId,
      rawValue: "ignored",
      enumOptionId: draft.enumOptionId.trim(),
      clear: false,
    };
  }
  if (!draft.rawValue.trim()) {
    return field.currentCanonicalValue
      ? { definitionId: field.definitionId, clear: true }
      : null;
  }
  return {
    definitionId: field.definitionId,
    rawValue: draft.rawValue.trim(),
    clear: false,
  };
}

/** اعتبارسنجی پیش از ذخیره — خطاهای فیلد به فارسی. */
export function validateAttributeDrafts(
  fields: ProductAttributeEditorField[],
  drafts: Record<string, AttributeDraftValue>,
): Record<string, string> {
  const errors: Record<string, string> = {};
  for (const field of fields) {
    if (field.isVariantAxis) continue;
    const draft = drafts[field.definitionId] ?? draftFromField(field);
    if (draft.clear) {
      if (field.isRequired) {
        errors[field.definitionId] = "این ویژگی الزامی است و قابل پاک‌سازی نیست";
      }
      continue;
    }
    if (field.valueKind === "Enumeration" && field.isMultivalue) {
      if (field.isRequired && draft.multiOptionIds.length === 0) {
        errors[field.definitionId] = "انتخاب حداقل یک گزینه الزامی است";
      }
      continue;
    }
    if (field.valueKind === "Enumeration") {
      if (field.isRequired && !draft.enumOptionId.trim()) {
        errors[field.definitionId] = "انتخاب گزینه الزامی است";
      }
      continue;
    }
    if (field.isRequired && !draft.rawValue.trim()) {
      errors[field.definitionId] = "پر کردن این فیلد الزامی است";
      continue;
    }
    if (field.valueKind === "Number" && draft.rawValue.trim()) {
      if (!Number.isFinite(Number(draft.rawValue.trim()))) {
        errors[field.definitionId] = "مقدار عددی نامعتبر است";
      }
    }
    if (field.valueKind === "Boolean" && draft.rawValue.trim()) {
      if (draft.rawValue !== "true" && draft.rawValue !== "false") {
        errors[field.definitionId] = "مقدار بله/خیر نامعتبر است";
      }
    }
  }
  return errors;
}

/** تراشه‌های نمایش از DisplayValue. */
export function displayChips(displayValue: string | null | undefined): string[] {
  if (!displayValue?.trim()) return [];
  return displayValue
    .split(/[،,]/)
    .map((x) => x.trim())
    .filter(Boolean);
}
