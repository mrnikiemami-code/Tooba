import assert from "node:assert/strict";
import test from "node:test";
import { listStorefrontPalettes, resolveTintTokens } from "./palette-registry.ts";
import {
  NEUTRAL_CARD_RGB,
  deriveComponentSurfaces,
  deriveLocalSurfacesFromContext,
  derivedSurfaceCssVars,
  isPureWhiteRgb,
  mixRgb,
  sectionContextCssVars,
} from "./derived-surface.ts";

test("Neutral light keeps accepted white cards and gray media", () => {
  const tint = resolveTintTokens("tooba-blue");
  const surfaces = deriveComponentSurfaces(tint, "Neutral", false);
  assert.equal(surfaces.card, NEUTRAL_CARD_RGB);
  assert.equal(surfaces.elevated, "255 255 255");
  assert.equal(surfaces.input, "255 255 255");
  assert.equal(surfaces.media, "249 250 251");
  assert.ok(isPureWhiteRgb(surfaces.card));
});

test("PaletteTint derived cards are not pure white and stay off primary", () => {
  for (const palette of listStorefrontPalettes()) {
    const tint = resolveTintTokens(palette.key);
    const light = deriveComponentSurfaces(tint, "PaletteTint", false);
    const dark = deriveComponentSurfaces(tint, "PaletteTint", true);
    assert.equal(isPureWhiteRgb(light.card), false, `${palette.key} card`);
    assert.equal(isPureWhiteRgb(light.elevated), false, `${palette.key} elevated`);
    assert.notEqual(light.card, tint.pageBackgroundRgb, `${palette.key} card != page`);
    assert.notEqual(light.card, palette.tokens.primaryRgb);
    assert.notEqual(dark.card, palette.tokens.primaryRgb);
    assert.notEqual(light.card, light.interactive);
    const vars = derivedSurfaceCssVars(tint);
    assert.equal(vars["--color-card-derived"], light.card);
    assert.equal(vars["--color-card-derived-dark"], dark.card);
  }
});

test("mixRgb interpolates channels", () => {
  assert.equal(mixRgb("0 0 0", "100 0 0", 0.5), "50 0 0");
});

test("local surfaces derive from section context and stay off paper-white under PaletteTint", () => {
  for (const palette of listStorefrontPalettes()) {
    const tint = resolveTintTokens(palette.key);
    for (const context of ["section", "alternate", "accent"] as const) {
      const local = deriveLocalSurfacesFromContext(tint, context, "PaletteTint", false);
      const neutral = deriveLocalSurfacesFromContext(tint, context, "Neutral", false);
      assert.equal(neutral.card, NEUTRAL_CARD_RGB);
      assert.equal(isPureWhiteRgb(local.card), false, `${palette.key} ${context} local-card`);
      assert.notEqual(local.card, palette.tokens.primaryRgb);
    }
    const vars = sectionContextCssVars(tint);
    assert.equal(vars["--color-local-card-section"], deriveLocalSurfacesFromContext(tint, "section", "PaletteTint", false).card);
    assert.ok(vars["--color-local-card-alternate-dark"]);
  }
});
