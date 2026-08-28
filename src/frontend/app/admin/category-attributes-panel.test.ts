import assert from "node:assert/strict";
import test from "node:test";
import {
  ATTRIBUTE_FLAG_LABELS,
  attributeCodeFromLabel,
  humanizeAttributeCode,
  isLocalSchemaEntry,
  partitionEffectiveSchema,
} from "./category-attributes-panel.tsx";
import type { EffectiveSchemaEntry } from "./catalog-attribute-api.ts";

function entry(
  partial: Partial<EffectiveSchemaEntry> & Pick<EffectiveSchemaEntry, "definitionId" | "code" | "inheritedFromCategoryId">,
): EffectiveSchemaEntry {
  return {
    valueKind: "Text",
    isVariantAxisAllowed: false,
    isVariantAxis: false,
    unit: null,
    isRequired: false,
    isFilterable: false,
    isComparable: false,
    isMultivalue: false,
    displayOrder: 0,
    definitionIsActive: true,
    ...partial,
  };
}

test("humanizeAttributeCode produces readable labels without GUIDs", () => {
  assert.equal(humanizeAttributeCode("brand-name"), "brand name");
  assert.equal(humanizeAttributeCode("screen_size"), "screen size");
  assert.equal(humanizeAttributeCode(""), "—");
});

test("attributeCodeFromLabel slugifies Persian/ Latin names", () => {
  const code = attributeCodeFromLabel("گوشی موبایل");
  assert.ok(code.length > 0);
  assert.equal(code.includes(" "), false);
});

test("local vs inherited partition uses inheritedFromCategoryId", () => {
  const categoryId = "cat-child";
  const rows = [
    entry({ definitionId: "d1", code: "brand", inheritedFromCategoryId: "cat-root" }),
    entry({ definitionId: "d2", code: "color", inheritedFromCategoryId: categoryId }),
  ];
  assert.equal(isLocalSchemaEntry(rows[1]!, categoryId), true);
  assert.equal(isLocalSchemaEntry(rows[0]!, categoryId), false);
  const { inherited, local } = partitionEffectiveSchema(rows, categoryId);
  assert.equal(inherited.length, 1);
  assert.equal(local.length, 1);
  assert.equal(inherited[0]?.code, "brand");
  assert.equal(local[0]?.code, "color");
});

test("ordinary-user Persian flag labels are defined", () => {
  assert.match(ATTRIBUTE_FLAG_LABELS.required, /الزامی/);
  assert.match(ATTRIBUTE_FLAG_LABELS.filterable, /فیلتر/);
  assert.match(ATTRIBUTE_FLAG_LABELS.variant, /تنوع/);
  assert.match(ATTRIBUTE_FLAG_LABELS.comparable, /مقایسه/);
  assert.equal(ATTRIBUTE_FLAG_LABELS.variant.toLowerCase().includes("variant"), false);
});
