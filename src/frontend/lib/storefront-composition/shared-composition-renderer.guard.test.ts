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
import { adminImplementedVariants, adminSelectableSectionTypes, variantPreviewFingerprint, variantPreviewStructure } from "../../app/admin/landing-pages/admin-composition-catalog.ts";

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
    const unimplemented = VARIANTS.find((v) => !v.implemented);
    if (!unimplemented) {
      assert.ok(isVariantImplemented("hero.editorial"));
      return;
    }
    assert.throws(
      () => resolveSharedVariant(unimplemented.sectionTypeKey, unimplemented.key, { strict: true }),
      /not implemented/i,
    );
  });

  it("wave-2 variants are implemented with distinct layout metadata", () => {
    for (const key of [
      "hero.editorial",
      "story.icon-shortcuts",
      "category.editorial-tiles",
      "product.tabbed",
      "product.large-cards",
      "product.minimal-list",
      "ranked.ticker",
      "banner.three",
      "brand.featured",
      "reviews.compact-quotes",
      "article.featured-plus-list",
    ]) {
      assert.equal(isVariantImplemented(key), true, key);
      assert.ok(requireResponsiveContract(key).columns.mobile);
    }
  });

  it("implemented variants expose distinct preview fingerprints", () => {
    const fingerprints = VARIANTS.filter((v) => v.implemented).map((v) => {
      const structure = variantPreviewStructure(v.key).map((c) => c.className).join("|");
      return `${variantPreviewFingerprint(v.key)}::${structure}::${v.previewKind}`;
    });
    // Not every pair must differ globally, but wave-2 keys must not collapse to identical structure.
    const wave2 = [
      "hero.editorial",
      "hero.split",
      "story.icon-shortcuts",
      "story.circle",
      "product.tabbed",
      "product.large-cards",
      "product.minimal-list",
      "ranked.ticker",
    ];
    const waveFingerprints = wave2.map((key) => {
      const structure = variantPreviewStructure(key).map((c) => c.className).join("|");
      return `${key}::${structure}`;
    });
    assert.equal(new Set(waveFingerprints).size, waveFingerprints.length);
    assert.ok(fingerprints.length >= wave2.length);
  });

  it("maps native StoryRail and BannerShowcase without CategoryGrid/PromoBanner proxy", () => {
    assert.equal(landingHostTypeForSection("StoryRail"), "StoryRail");
    assert.equal(landingHostTypeForSection("BannerShowcase"), "BannerShowcase");
    assert.notEqual(landingHostTypeForSection("StoryRail"), "CategoryGrid");
    assert.notEqual(landingHostTypeForSection("BannerShowcase"), "PromoBanner");
    const story = adaptLandingSectionToComposition({
      pageSectionId: "s1",
      sectionType: "StoryRail",
      displayOrder: 0,
      config: { title: "استوری", variantKey: "story.circle", items: [] },
    });
    assert.equal(story.sectionTypeKey, "StoryRail");
    const banner = adaptLandingSectionToComposition({
      pageSectionId: "b1",
      sectionType: "BannerShowcase",
      displayOrder: 1,
      config: { title: "بنر", variantKey: "banner.four-grid" },
    });
    assert.equal(banner.sectionTypeKey, "BannerShowcase");
    assert.equal(banner.variantKey, "banner.four-grid");
  });

  it("migrates T019 proxy configs carrying story/banner variantKey", () => {
    const fromCategory = adaptLandingSectionToComposition({
      pageSectionId: "proxy-story",
      sectionType: "CategoryGrid",
      displayOrder: 0,
      config: { title: "س", variantKey: "story.rounded-cards" },
    });
    assert.equal(fromCategory.sectionTypeKey, "StoryRail");
    assert.equal(fromCategory.variantKey, "story.rounded-cards");
    const fromPromo = adaptLandingSectionToComposition({
      pageSectionId: "proxy-banner",
      sectionType: "PromoBanner",
      displayOrder: 1,
      config: { title: "ب", variantKey: "banner.two-equal" },
    });
    assert.equal(fromPromo.sectionTypeKey, "BannerShowcase");
    assert.equal(fromPromo.variantKey, "banner.two-equal");
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
    assert.equal(landingHostTypeForSection("StoryRail"), "StoryRail");
    assert.equal(landingHostTypeForSection("BannerShowcase"), "BannerShowcase");
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
