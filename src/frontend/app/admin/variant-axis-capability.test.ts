import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

import {
  VARIANT_AXIS_CAPABILITY_HELPER,
  VARIANT_AXIS_CAPABILITY_LABEL,
  VARIANT_AXIS_DISABLED_BY_CAPABILITY,
  VARIANT_AXIS_DISABLED_BY_KIND,
  valueKindBlocksVariantAxis,
} from "./variant-axis-messages.ts";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)));
const panel = fs.readFileSync(path.join(root, "category-attributes-panel.tsx"), "utf8");
const ui = fs.readFileSync(path.join(root, "catalog-attribute-ui.tsx"), "utf8");
const api = fs.readFileSync(path.join(root, "catalog-attribute-api.ts"), "utf8");

test("valueKindBlocksVariantAxis matches TB-P07-T039 rules", () => {
  assert.equal(valueKindBlocksVariantAxis("Boolean"), true);
  assert.equal(valueKindBlocksVariantAxis("Text"), true);
  assert.equal(valueKindBlocksVariantAxis("Instant"), true);
  assert.equal(valueKindBlocksVariantAxis("Enumeration"), false);
  assert.equal(valueKindBlocksVariantAxis("Number"), false);
});

test("distinct disabled reason copy exists for kind vs capability", () => {
  assert.notEqual(
    VARIANT_AXIS_DISABLED_BY_KIND.fa.title,
    VARIANT_AXIS_DISABLED_BY_CAPABILITY.fa.title,
  );
  assert.match(VARIANT_AXIS_DISABLED_BY_KIND.fa.detail, /بله\/خیر/);
  assert.match(VARIANT_AXIS_DISABLED_BY_CAPABILITY.fa.detail, /تعریف اصلی/);
});

test("category attributes panel uses distinct variant disabled reasons", () => {
  assert.match(panel, /VARIANT_AXIS_DISABLED_BY_KIND/);
  assert.match(panel, /VARIANT_AXIS_DISABLED_BY_CAPABILITY/);
  assert.match(panel, /attr-variant-disabled-reason/);
  assert.match(panel, /valueKindBlocksVariantAxis/);
});

test("attribute definitions screen exposes editable variant capability", () => {
  assert.match(ui, /VARIANT_AXIS_CAPABILITY_LABEL/);
  assert.match(ui, /attr-def-variant-capability/);
  assert.match(ui, /previewVariantAxisCapabilityDisable/);
  assert.match(ui, /setVariantAxisCapability/);
  assert.match(VARIANT_AXIS_CAPABILITY_LABEL.fa, /قابل استفاده برای تنوع/);
  assert.match(VARIANT_AXIS_CAPABILITY_HELPER.fa, /هیچ دسته یا محصولی خودکار تغییر نمی‌کند/);
});

test("catalog attribute api wires capability preview and set endpoints", () => {
  assert.match(api, /variant-axis-capability\/disable-preview/);
  assert.match(api, /variant-axis-capability/);
  assert.match(api, /previewVariantAxisCapabilityDisable/);
  assert.match(api, /setVariantAxisCapability/);
});
