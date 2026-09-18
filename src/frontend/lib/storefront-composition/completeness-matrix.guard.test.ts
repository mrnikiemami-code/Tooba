import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { describe, it } from "node:test";
import { fileURLToPath } from "node:url";
import { SECTION_TYPES, VARIANTS } from "./registry.ts";
import { RESPONSIVE_CONTRACTS, requireResponsiveContract } from "./responsive-contracts.ts";
import { canonicalizeVariantKey } from "./resolve-variant.ts";

const here = dirname(fileURLToPath(import.meta.url));
const rendererSource = readFileSync(join(here, "shared-composition-renderer.tsx"), "utf8");
const catalogSource = readFileSync(join(here, "../../app/admin/landing-pages/admin-composition-catalog.ts"), "utf8");

/** Documented shared renderer paths (canonical key → notes). Alias keys must canonicalize here. */
const DOCUMENTED_SHARED_PATHS: Record<string, string> = {
  "banner.mosaic-2x2": "alias → banner.four-grid (CompositionBannerGrid four-grid)",
};

describe("TB-P10-T022 selectable-variant completeness matrix", () => {
  it("every implemented variant has preview metadata + responsive contract + renderer/shared path", () => {
    const implemented = VARIANTS.filter((v) => v.implemented);
    assert.ok(implemented.length >= 40, `expected substantial implemented catalog, got ${implemented.length}`);

    for (const v of implemented) {
      assert.ok(v.previewKind, `missing previewKind for ${v.key}`);
      assert.ok(typeof v.nameFa === "string" && v.nameFa.trim().length > 0, `missing nameFa for ${v.key}`);
      assert.ok(typeof v.descriptionFa === "string" && v.descriptionFa.trim().length > 0, `missing descriptionFa for ${v.key}`);

      const contract = requireResponsiveContract(v.responsiveContractKey);
      assert.equal(contract.variantKey, v.key);
      assert.ok(RESPONSIVE_CONTRACTS[v.key], `missing RESPONSIVE_CONTRACTS entry for ${v.key}`);
      assert.ok(contract.columns.mobile && contract.height.mobile && contract.itemVisible.mobile, `incomplete mobile contract for ${v.key}`);

      const inRenderer =
        rendererSource.includes(`"${v.key}"`)
        || rendererSource.includes(`case "${v.key}"`);
      const shared = DOCUMENTED_SHARED_PATHS[v.key];
      assert.ok(
        inRenderer || shared,
        `implemented variant ${v.key} missing renderer switch case and documented shared path`,
      );
    }
  });

  it("hides accidental alias banner.mosaic-2x2 from selectable catalog", () => {
    const alias = VARIANTS.find((v) => v.key === "banner.mosaic-2x2");
    assert.ok(alias);
    assert.equal(alias!.implemented, false);
    assert.equal(canonicalizeVariantKey("banner.mosaic-2x2"), "banner.four-grid");
    assert.equal(canonicalizeVariantKey("banner.four-grid"), "banner.four-grid");
  });

  it("admin catalog surfaces only implemented variants (no orphan selectable)", () => {
    assert.match(catalogSource, /implementedVariantsForSection/);
    for (const section of SECTION_TYPES) {
      const selectable = VARIANTS.filter((v) => v.sectionTypeKey === section.key && v.implemented);
      assert.ok(selectable.length >= 1, `section ${section.key} has no selectable variant`);
      assert.ok(
        selectable.some((v) => v.key === section.defaultVariantKey),
        `default variant ${section.defaultVariantKey} must be implemented for ${section.key}`,
      );
    }
  });

  it("does not expose technical composition identifiers in variant FA labels", () => {
    const exactProductShowcaseNames = new Set(["سانی", "مانی", "سینمایی", "سینمایی پلاس", "کاشف"]);
    for (const v of VARIANTS.filter((x) => x.implemented)) {
      assert.doesNotMatch(v.nameFa, /SectionType|VariantKey|ResponsiveContract|breakpoint|JSON|CSS/i);
      assert.doesNotMatch(v.descriptionFa, /SectionType|VariantKey|ResponsiveContract|breakpoint/i);
      assert.doesNotMatch(v.nameFa, /\bHTML\b/);
      assert.doesNotMatch(v.descriptionFa, /\bHTML\b/);
      if (!exactProductShowcaseNames.has(v.nameFa)) {
        assert.match(v.nameFa, /طرح/, `human design name missing طرح for ${v.key}`);
      }
    }
  });
});
