import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
  assertIndustryTemplatesValid,
  buildTemplateSectionPayloads,
  bannerSlotCountForVariant,
  listIndustryTemplates,
} from "./industry-templates.ts";
import { isVariantImplemented, VARIANTS } from "./registry.ts";
import { requireResponsiveContract } from "./responsive-contracts.ts";

describe("industry templates + native contracts", () => {
  it("seeds 10 distinct templates from implemented variants and truthful sources", () => {
    assertIndustryTemplatesValid();
    assert.equal(listIndustryTemplates().length, 10);
  });

  it("builds normal editable draft section payloads", () => {
    const payloads = buildTemplateSectionPayloads("fashion");
    assert.ok(payloads.length >= 5);
    for (const row of payloads) {
      assert.ok(row.hostType);
      assert.equal(typeof row.config.variantKey, "string");
      assert.equal(isVariantImplemented(String(row.config.variantKey)), true);
    }
    const auto = buildTemplateSectionPayloads("auto-parts");
    const fashionSig = payloads.map((p) => `${p.hostType}:${p.config.variantKey}`).join("|");
    const autoSig = auto.map((p) => `${p.hostType}:${p.config.variantKey}`).join("|");
    assert.notEqual(fashionSig, autoSig);
  });

  it("banner slot counts match layout variants", () => {
    assert.equal(bannerSlotCountForVariant("banner.single"), 1);
    assert.equal(bannerSlotCountForVariant("banner.two-equal"), 2);
    assert.equal(bannerSlotCountForVariant("banner.four-grid"), 4);
    assert.equal(bannerSlotCountForVariant("banner.one-large-four-small"), 5);
    assert.equal(bannerSlotCountForVariant("banner.eight-compact"), 8);
  });

  it("every implemented variant has responsive contract", () => {
    for (const v of VARIANTS.filter((x) => x.implemented)) {
      const contract = requireResponsiveContract(v.key);
      assert.ok(contract.columns.mobile);
    }
  });
});
