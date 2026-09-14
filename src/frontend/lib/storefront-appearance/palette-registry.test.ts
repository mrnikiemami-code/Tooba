import assert from "node:assert/strict";
import test from "node:test";
import {
  DEFAULT_PALETTE_KEY,
  DEFAULT_PRIMARY_HEX,
  DEFAULT_PRIMARY_STRONG_HEX,
  appearanceCssVars,
  hexToRgbTriple,
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

test("unknown PaletteKey falls back to tooba-blue", () => {
  assert.equal(resolvePaletteKey("unknown-preset"), DEFAULT_PALETTE_KEY);
  assert.equal(resolvePaletteKey(null), DEFAULT_PALETTE_KEY);
  assert.deepEqual(resolveBrandTokens("nope"), resolveBrandTokens(DEFAULT_PALETTE_KEY));
});

test("appearance CSS vars set brand tokens only", () => {
  const vars = appearanceCssVars(resolveBrandTokens("tooba-blue"));
  assert.equal(vars["--color-primary"], "37 99 235");
  assert.equal(vars["--color-primary-strong"], "29 78 216");
  assert.equal(Object.hasOwn(vars, "--color-danger"), false);
  assert.equal(Object.hasOwn(vars, "--color-success"), false);
  assert.equal(Object.hasOwn(vars, "--color-warning"), false);
});
