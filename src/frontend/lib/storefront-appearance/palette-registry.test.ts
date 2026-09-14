import assert from "node:assert/strict";
import test from "node:test";
import {
  DEFAULT_PALETTE_KEY,
  DEFAULT_PRIMARY_HEX,
  DEFAULT_PRIMARY_STRONG_HEX,
  appearanceCssVars,
  contrastRatio,
  hexToRgbTriple,
  listStorefrontPalettes,
  resolveBrandTokens,
  resolvePaletteKey,
} from "./palette-registry.ts";

test("default palette is visually equivalent to #2563EB", () => {
  assert.equal(DEFAULT_PALETTE_KEY, "tooba-blue");
  assert.equal(DEFAULT_PRIMARY_HEX, "#2563EB");
  assert.equal(hexToRgbTriple(DEFAULT_PRIMARY_HEX), "37 99 235");
  assert.equal(hexToRgbTriple(DEFAULT_PRIMARY_STRONG_HEX), "29 78 216");
  const tokens = resolveBrandTokens(DEFAULT_PALETTE_KEY);
  assert.equal(tokens.primaryRgb, hexToRgbTriple(DEFAULT_PRIMARY_HEX));
  assert.equal(tokens.primaryStrongRgb, hexToRgbTriple(DEFAULT_PRIMARY_STRONG_HEX));
});

test("curated registry has six to eight production palettes including tooba-blue", () => {
  const keys = listStorefrontPalettes().map((item) => item.key);
  assert.ok(keys.length >= 6 && keys.length <= 8);
  assert.ok(keys.includes(DEFAULT_PALETTE_KEY));
  assert.equal(new Set(keys).size, keys.length);
});

test("unknown PaletteKey falls back to tooba-blue", () => {
  assert.equal(resolvePaletteKey("unknown-preset"), DEFAULT_PALETTE_KEY);
  assert.equal(resolvePaletteKey(null), DEFAULT_PALETTE_KEY);
  assert.deepEqual(resolveBrandTokens("nope"), resolveBrandTokens(DEFAULT_PALETTE_KEY));
});

test("every curated palette meets AA contrast for CTA and links", () => {
  const paper = "250 250 249";
  for (const palette of listStorefrontPalettes()) {
    const { primaryRgb, primaryStrongRgb, onPrimaryRgb } = palette.tokens;
    assert.ok(contrastRatio(primaryRgb, onPrimaryRgb) >= 4.5, `${palette.key} CTA`);
    assert.ok(contrastRatio(primaryRgb, paper) >= 4.5, `${palette.key} link`);
    assert.ok(contrastRatio(primaryStrongRgb, onPrimaryRgb) >= 4.5, `${palette.key} strong`);
    assert.equal(onPrimaryRgb, "255 255 255");
  }
});

test("dark surfaces meet AA for body muted danger and brand CTA", () => {
  const darkPaper = "12 12 14";
  const darkInk = "250 250 250";
  const darkMuted = "180 180 188";
  const darkDanger = "248 113 113";
  assert.ok(contrastRatio(darkInk, darkPaper) >= 4.5, "body");
  assert.ok(contrastRatio(darkMuted, darkPaper) >= 4.5, "muted");
  assert.ok(contrastRatio(darkDanger, darkPaper) >= 4.5, "danger");
  for (const palette of listStorefrontPalettes()) {
    assert.ok(contrastRatio(palette.tokens.primaryRgb, palette.tokens.onPrimaryRgb) >= 4.5, `${palette.key} dark CTA`);
    assert.ok(
      contrastRatio(palette.tokens.primaryOnDarkRgb, darkPaper) >= 4.5,
      `${palette.key} brand-emphasis on dark`,
    );
  }
});

test("appearance CSS vars set brand tokens only", () => {
  const vars = appearanceCssVars(resolveBrandTokens("tooba-blue"));
  assert.equal(vars["--color-primary"], "37 99 235");
  assert.equal(vars["--color-primary-strong"], "29 78 216");
  assert.equal(vars["--color-primary-on-dark"], "59 115 237");
  assert.equal(Object.hasOwn(vars, "--color-danger"), false);
  assert.equal(Object.hasOwn(vars, "--color-success"), false);
  assert.equal(Object.hasOwn(vars, "--color-warning"), false);
});

test("wine-burgundy dark emphasis is canonical not a page special case", () => {
  const wine = resolveBrandTokens("wine-burgundy");
  assert.equal(wine.primaryRgb, "159 18 57");
  assert.equal(wine.primaryOnDarkRgb, "189 91 118");
  assert.ok(contrastRatio(wine.primaryRgb, "12 12 14") < 3);
  assert.ok(contrastRatio(wine.primaryOnDarkRgb, "12 12 14") >= 4.5);
});
