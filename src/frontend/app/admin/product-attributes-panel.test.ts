import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import {
  draftToValueInput,
  isAttributeDraftDirty,
  validateAttributeDrafts,
} from "./product-attributes-panel-model.ts";
import type { ProductAttributeEditorField } from "./catalog-attribute-api.ts";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)));

function field(
  partial: Partial<ProductAttributeEditorField> &
    Pick<ProductAttributeEditorField, "definitionId" | "code" | "localizedName" | "valueKind">,
): ProductAttributeEditorField {
  return {
    unit: null,
    isRequired: false,
    isVariantAxis: false,
    isFilterable: false,
    isComparable: false,
    isMultivalue: false,
    displayOrder: 0,
    options: [],
    currentCanonicalValue: null,
    currentEnumOptionId: null,
    displayValue: null,
    isMissingRequired: false,
    ...partial,
  };
}

test("validateAttributeDrafts blocks missing required text/enum", () => {
  const fields = [
    field({
      definitionId: "d1",
      code: "note",
      localizedName: "یادداشت",
      valueKind: "Text",
      isRequired: true,
    }),
    field({
      definitionId: "d2",
      code: "material",
      localizedName: "جنس",
      valueKind: "Enumeration",
      isRequired: true,
      options: [{ optionId: "o1", localizedLabel: "آلومینیوم", isActive: true }],
    }),
  ];
  const errors = validateAttributeDrafts(fields, {
    d1: { rawValue: "", enumOptionId: "", multiOptionIds: [], clear: false },
    d2: { rawValue: "", enumOptionId: "", multiOptionIds: [], clear: false },
  });
  assert.ok(errors.d1);
  assert.ok(errors.d2);
  assert.match(errors.d1!, /الزامی/);
});

test("draftToValueInput builds enum and clear payloads without raw IDs in UI contract", () => {
  const enumField = field({
    definitionId: "d2",
    code: "material",
    localizedName: "جنس",
    valueKind: "Enumeration",
    currentCanonicalValue: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
    currentEnumOptionId: "11111111-1111-7111-8111-111111111111",
    options: [
      {
        optionId: "11111111-1111-7111-8111-111111111111",
        localizedLabel: "آلومینیوم",
        isActive: true,
      },
    ],
  });
  const setInput = draftToValueInput(enumField, {
    rawValue: "",
    enumOptionId: "11111111-1111-7111-8111-111111111111",
    multiOptionIds: [],
    clear: false,
  });
  assert.equal(setInput?.enumOptionId, "11111111-1111-7111-8111-111111111111");
  assert.equal(setInput?.clear, false);

  const clearInput = draftToValueInput(enumField, {
    rawValue: "",
    enumOptionId: "",
    multiOptionIds: [],
    clear: true,
  });
  assert.equal(clearInput?.clear, true);

  const axis = field({
    definitionId: "d3",
    code: "color",
    localizedName: "رنگ",
    valueKind: "Enumeration",
    isVariantAxis: true,
  });
  assert.equal(
    draftToValueInput(axis, {
      rawValue: "",
      enumOptionId: "x",
      multiOptionIds: [],
      clear: false,
    }),
    null,
  );
});

test("isAttributeDraftDirty detects enum and number changes", () => {
  const numberField = field({
    definitionId: "d1",
    code: "screen",
    localizedName: "صفحه",
    valueKind: "Number",
    currentCanonicalValue: "6.1",
  });
  assert.equal(
    isAttributeDraftDirty(numberField, {
      rawValue: "6.1",
      enumOptionId: "",
      multiOptionIds: [],
      clear: false,
    }),
    false,
  );
  assert.equal(
    isAttributeDraftDirty(numberField, {
      rawValue: "6.5",
      enumOptionId: "",
      multiOptionIds: [],
      clear: false,
    }),
    true,
  );
});

test("panel source has VIEW/EDIT typed controls and no raw enumOptionId inputs", () => {
  const src = fs.readFileSync(path.join(root, "product-attributes-panel.tsx"), "utf8");
  assert.match(src, /mode:\s*ProductAttributesPanelMode/);
  assert.match(src, /product-attributes-save/);
  assert.match(src, /محور تنوع/);
  assert.match(src, /بله/);
  assert.match(src, /خیر/);
  assert.equal(src.includes("شناسه گزینه"), false);
  assert.equal(src.includes("rawValue (اختیاری)"), false);
  assert.match(src, /localizedLabel/);
});
