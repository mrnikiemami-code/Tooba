import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
  assertRegistryIntegrity,
  SECTION_TYPES,
  VARIANTS,
  getVariant,
  variantsForSection,
} from "./registry.ts";
import { RESPONSIVE_CONTRACTS, requireResponsiveContract } from "./responsive-contracts.ts";
import { normalizeControlledSettings, BASE_SECTION_SETTINGS, assertNoForbiddenSettings } from "./settings.ts";
import { adaptLandingSectionToComposition, assertLandingTypesCovered, LANDING_SECTION_TYPE_MAP } from "./landing-adapter.ts";
import { LANDING_SECTION_TYPES } from "../../app/admin/landing-pages/landing-section-catalog.ts";
import { SURFACE_ROLES } from "./types.ts";
import { FORBIDDEN_SETTING_KEYS } from "./types.ts";

describe("storefront composition registry", () => {
  it("has unique section and variant keys with valid defaults", () => {
    assertRegistryIntegrity();
    assert.equal(new Set(SECTION_TYPES.map((s) => s.key)).size, SECTION_TYPES.length);
    assert.equal(new Set(VARIANTS.map((v) => v.key)).size, VARIANTS.length);
  });

  it("keeps SectionType ↔ Variant compatibility", () => {
    for (const v of VARIANTS) {
      const siblings = variantsForSection(v.sectionTypeKey);
      assert.ok(siblings.some((s) => s.key === v.key));
      assert.equal(getVariant(v.key)?.sectionTypeKey, v.sectionTypeKey);
    }
    for (const s of SECTION_TYPES) {
      assert.ok(variantsForSection(s.key).length >= 1);
    }
  });

  it("requires a responsive contract for every variant", () => {
    for (const v of VARIANTS) {
      const contract = requireResponsiveContract(v.responsiveContractKey);
      assert.equal(contract.variantKey, v.key);
      assert.ok(RESPONSIVE_CONTRACTS[v.key]);
    }
  });

  it("rejects arbitrary and CSS settings", () => {
    assert.throws(() => assertNoForbiddenSettings({ css: "color:red" }), /Forbidden/);
    assert.throws(() => assertNoForbiddenSettings({ className: "foo" }), /Forbidden/);
    assert.throws(() => assertNoForbiddenSettings({ customCss: "x" }), /Forbidden/);
    assert.throws(() => assertNoForbiddenSettings({ breakpoint: "768" }), /Forbidden/);
    assert.throws(
      () => normalizeControlledSettings(BASE_SECTION_SETTINGS, { title: "ok", mystery: 1 }),
      /arbitrary|Unknown/,
    );
    for (const key of FORBIDDEN_SETTING_KEYS) {
      assert.throws(() => normalizeControlledSettings(BASE_SECTION_SETTINGS, { [key]: "x" }), /Forbidden/);
    }
    const ok = normalizeControlledSettings(BASE_SECTION_SETTINGS, { title: "عنوان", itemCount: 6 });
    assert.equal(ok.title, "عنوان");
    assert.equal(ok.itemCount, 6);
  });

  it("adapts existing Landing section types", () => {
    assertLandingTypesCovered(LANDING_SECTION_TYPES);
    assert.equal(Object.keys(LANDING_SECTION_TYPE_MAP).length, LANDING_SECTION_TYPES.length);
    const hero = adaptLandingSectionToComposition({
      pageSectionId: "ps-1",
      sectionType: "Hero",
      displayOrder: 0,
      config: { title: "بنر", subtitle: "توضیح", href: "/products" },
    });
    assert.equal(hero.sectionTypeKey, "HeroCarousel");
    assert.equal(hero.variantKey, "hero.contained");
    assert.equal(hero.settings.title, "بنر");
    const products = adaptLandingSectionToComposition({
      pageSectionId: "ps-2",
      sectionType: "ProductCollection",
      displayOrder: 1,
      config: { title: "کالاها", source: "Newest", take: 8 },
    });
    assert.equal(products.sectionTypeKey, "ProductShowcase");
    assert.equal(products.settings.dataSource, "Newest");
  });

  it("does not expand user color model beyond four global roles", () => {
    assert.equal(SURFACE_ROLES.length, 4);
    assert.deepEqual([...SURFACE_ROLES], ["page", "section", "alternate", "accent"]);
  });
});
