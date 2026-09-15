import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { describe, it } from "node:test";
import { fileURLToPath } from "node:url";
import {
  assertRegistryIntegrity,
  SECTION_TYPES,
  VARIANTS,
  getVariant,
  variantsForSection,
  INDUSTRY_TEMPLATE_SEEDS,
  landingHostTypeForSection,
  isVariantImplemented,
} from "./registry.ts";
import { RESPONSIVE_CONTRACTS, requireResponsiveContract } from "./responsive-contracts.ts";
import { normalizeControlledSettings, BASE_SECTION_SETTINGS, assertNoForbiddenSettings } from "./settings.ts";
import { adaptLandingSectionToComposition, assertLandingTypesCovered, LANDING_SECTION_TYPE_MAP, migrateProxySectionType } from "./landing-adapter.ts";
import { LANDING_SECTION_TYPES } from "../../app/admin/landing-pages/landing-section-catalog.ts";
import { SURFACE_ROLES } from "./types.ts";
import { FORBIDDEN_SETTING_KEYS } from "./types.ts";
import { assertIndustryTemplatesValid } from "./industry-templates.ts";

const rendererSource = readFileSync(join(dirname(fileURLToPath(import.meta.url)), "shared-composition-renderer.tsx"), "utf8");

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

  it("adapts existing Landing section types including native StoryRail/BannerShowcase", () => {
    assertLandingTypesCovered(LANDING_SECTION_TYPES);
    assert.equal(Object.keys(LANDING_SECTION_TYPE_MAP).length, LANDING_SECTION_TYPES.length);
    assert.equal(landingHostTypeForSection("StoryRail"), "StoryRail");
    assert.equal(landingHostTypeForSection("BannerShowcase"), "BannerShowcase");
    assert.notEqual(landingHostTypeForSection("StoryRail"), "CategoryGrid");
    assert.notEqual(landingHostTypeForSection("BannerShowcase"), "PromoBanner");
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
    const story = adaptLandingSectionToComposition({
      pageSectionId: "ps-3",
      sectionType: "StoryRail",
      displayOrder: 2,
      config: { title: "استوری", variantKey: "story.circle" },
    });
    assert.equal(story.sectionTypeKey, "StoryRail");
    const migrated = migrateProxySectionType("CategoryGrid", "story.rounded-cards");
    assert.equal(migrated?.sectionTypeKey, "StoryRail");
  });

  it("every implemented variant has renderer case presence and contract", () => {
    for (const v of VARIANTS.filter((x) => x.implemented)) {
      assert.ok(
        rendererSource.includes(`"${v.key}"`) || rendererSource.includes(`case "${v.key}"`),
        `missing renderer case for ${v.key}`,
      );
      assert.ok(RESPONSIVE_CONTRACTS[v.key]);
    }
  });

  it("templates use only implemented truthful variants and stay distinct", () => {
    assertIndustryTemplatesValid();
    const firstHashes = INDUSTRY_TEMPLATE_SEEDS.map((t) =>
      `${t.sectionPresetList[0]?.sectionTypeKey}:${t.sectionPresetList.map((p) => p.variantKey).join(",")}`,
    );
    assert.equal(new Set(firstHashes).size, INDUSTRY_TEMPLATE_SEEDS.length);
    for (const t of INDUSTRY_TEMPLATE_SEEDS) {
      for (const p of t.sectionPresetList) {
        assert.equal(isVariantImplemented(p.variantKey), true);
        assert.doesNotMatch(p.dataSourceIntent, /BestSelling|Featured|MostViewed|Discounted|HotTrending/);
      }
    }
  });

  it("implemented variants do not share identical responsive classification", () => {
    const implemented = VARIANTS.filter((v) => v.implemented);
    const signatures = implemented.map((v) => {
      const c = RESPONSIVE_CONTRACTS[v.key]!;
      return `${v.sectionTypeKey}|${c.columns.desktop}|${c.columns.mobile}|${c.height.desktop}|${c.itemVisible.desktop}|${v.previewKind}`;
    });
    // Allow some overlap across section types, but require more uniqueness than a single alias bucket.
    assert.ok(new Set(signatures).size >= Math.floor(implemented.length * 0.55));
  });

  it("does not expand user color model beyond four global roles", () => {
    assert.equal(SURFACE_ROLES.length, 4);
    assert.deepEqual([...SURFACE_ROLES], ["page", "section", "alternate", "accent"]);
  });
});
