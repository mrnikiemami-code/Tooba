import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import {
  axisDraftFromState,
  estimateCombinationCount,
  formatCombinationLabel,
  isAxisDraftDirty,
  selectedAxesFromDraft,
} from "./product-variants-panel-model.ts";
import type { ProductVariantAxisEditorField } from "./catalog-attribute-api.ts";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)));

function axis(
  partial: Partial<ProductVariantAxisEditorField> &
    Pick<ProductVariantAxisEditorField, "definitionId" | "code" | "localizedName">,
): ProductVariantAxisEditorField {
  return {
    valueKind: "Enumeration",
    options: [
      { optionId: "o1", localizedLabel: "مشکی", code: "black", isActive: true },
      { optionId: "o2", localizedLabel: "سفید", code: "white", isActive: true },
    ],
    selectedOptionIds: [],
    ...partial,
  };
}

test("axis draft dirty detection and selectedAxes payload", () => {
  const axes = [
    axis({ definitionId: "color", code: "color", localizedName: "رنگ", selectedOptionIds: ["o1"] }),
    axis({
      definitionId: "storage",
      code: "storage",
      localizedName: "حافظه",
      options: [
        { optionId: "s1", localizedLabel: "128GB", code: "128", isActive: true },
        { optionId: "s2", localizedLabel: "256GB", code: "256", isActive: true },
      ],
      selectedOptionIds: ["s1"],
    }),
  ];
  const draft = axisDraftFromState(axes);
  assert.equal(isAxisDraftDirty(axes, draft), false);
  draft.color = ["o1", "o2"];
  assert.equal(isAxisDraftDirty(axes, draft), true);
  const selected = selectedAxesFromDraft(axes, draft);
  assert.equal(selected.length, 2);
  assert.deepEqual(selected[0]?.optionIds.sort(), ["o1", "o2"]);
});

test("combination estimate and readable labels", () => {
  assert.equal(estimateCombinationCount({ a: ["1", "2"], b: ["x", "y", "z"] }), 6);
  assert.equal(
    formatCombinationLabel([
      { definitionName: "رنگ", valueLabel: "مشکی" },
      { definitionName: "حافظه", valueLabel: "128GB" },
    ]),
    "مشکی / 128GB",
  );
});

test("panel source has VIEW/EDIT, preview, impact, no price/stock/AgGrid/raw IDs in labels", () => {
  const src = fs.readFileSync(path.join(root, "product-variants-panel.tsx"), "utf8");
  assert.match(src, /ProductVariantsPanelMode/);
  assert.match(src, /پیش‌نمایش ترکیب‌ها/);
  assert.match(src, /تنوع پیش‌فرض/);
  assert.match(src, /ذخیره تنوع‌ها/);
  assert.match(src, /انصراف/);
  assert.match(src, /برای این دسته‌بندی ویژگی تنوع تعریف نشده است/);
  assert.match(src, /بدون قیمت یا موجودی/);
  assert.doesNotMatch(src, /AgGridReact/);
  assert.doesNotMatch(src, /\bPrice\b|\bStock\b/);
  assert.match(src, /بدون قیمت یا موجودی/);
  assert.match(src, /formatCombinationLabel/);
});

test("workspace wires ProductVariantsPanel into تنوع‌ها tab", () => {
  const screen = fs.readFileSync(path.join(root, "product-workspace-screen.tsx"), "utf8");
  assert.match(screen, /ProductVariantsPanel/);
  assert.match(screen, /label:\s*"تنوع‌ها"/);
  assert.doesNotMatch(screen, /شناسه گزینه/);
});
