import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
  assertVariantCompatible,
  implementedVariantsForSection,
  isVariantImplemented,
  landingHostTypeForSection,
  VARIANTS,
} from "./registry.ts";
import { adaptLandingSectionToComposition } from "./landing-adapter.ts";
import { resolveSharedVariant } from "./resolve-variant.ts";
import { requireResponsiveContract } from "./responsive-contracts.ts";
import { normalizeControlledSettings, BASE_SECTION_SETTINGS } from "./settings.ts";
import { adminImplementedVariants, adminSelectableSectionTypes } from "../../app/admin/landing-pages/admin-composition-catalog.ts";

describe("shared composition renderer", () => {
  it("resolves registered SectionType+Variant", () => {
    const v = resolveSharedVariant("HeroCarousel", "hero.full-width");
    assert.equal(v.key, "hero.full-width");
    assert.equal(v.implemented, true);
    assert.equal(assertVariantCompatible("ProductShowcase", "product.card-carousel").sectionTypeKey, "ProductShowcase");
  });

  it("rejects invalid Variant for SectionType", () => {
    assert.throws(() => resolveSharedVariant("HeroCarousel", "product.card-carousel"), /compatible|not compatible/i);
    assert.throws(() => assertVariantCompatible("BannerShowcase", "hero.full-width"), /compatible/i);
  });

  it("rejects unimplemented variants when strict", () => {
    assert.throws(() => resolveSharedVariant("HeroCarousel", "hero.split", { strict: true }), /not implemented/i);
  });

  it("legacy Landing adapter is deterministic", () => {
    const a = adaptLandingSectionToComposition({
      pageSectionId: "a",
      sectionType: "Hero",
      displayOrder: 0,
      config: { title: "ت" },
    });
    const b = adaptLandingSectionToComposition({
      pageSectionId: "a",
      sectionType: "Hero",
      displayOrder: 0,
      config: { title: "ت" },
    });
    assert.equal(a.variantKey, "hero.contained");
    assert.equal(a.sectionTypeKey, b.sectionTypeKey);
    assert.equal(a.variantKey, b.variantKey);
    const withVariant = adaptLandingSectionToComposition({
      pageSectionId: "b",
      sectionType: "PromoBanner",
      displayOrder: 1,
      config: { title: "ب", variantKey: "banner.two-equal" },
    });
    assert.equal(withVariant.sectionTypeKey, "BannerShowcase");
    assert.equal(withVariant.variantKey, "banner.two-equal");
  });

  it("only implemented variants appear in admin selectable helper", () => {
    const sections = adminSelectableSectionTypes();
    assert.ok(sections.length >= 9);
    for (const s of sections) {
      const impl = adminImplementedVariants(s.sectionTypeKey);
      assert.ok(impl.length >= 1);
      for (const v of impl) {
        assert.equal(isVariantImplemented(v.variantKey), true);
      }
      const all = implementedVariantsForSection(s.sectionTypeKey);
      assert.equal(impl.length, all.length);
    }
    assert.ok(!VARIANTS.filter((v) => !v.implemented).some((v) =>
      adminImplementedVariants(v.sectionTypeKey).some((a) => a.variantKey === v.key),
    ));
    assert.equal(landingHostTypeForSection("HeroCarousel"), "Hero");
  });

  it("responsive contract present for every implemented variant", () => {
    for (const v of VARIANTS.filter((x) => x.implemented)) {
      const contract = requireResponsiveContract(v.responsiveContractKey);
      assert.equal(contract.variantKey, v.key);
      assert.ok(contract.columns.desktop);
      assert.ok(contract.columns.tablet);
      assert.ok(contract.columns.mobile);
    }
  });

  it("unsupported setting rejected", () => {
    assert.throws(
      () => normalizeControlledSettings(BASE_SECTION_SETTINGS, { title: "x", css: "color:red" }),
      /Forbidden/,
    );
    assert.throws(
      () => normalizeControlledSettings(BASE_SECTION_SETTINGS, { mysteryLayout: "x" } as never),
      /arbitrary|Unknown/,
    );
  });
});
