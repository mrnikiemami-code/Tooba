import assert from "node:assert/strict";
import test from "node:test";
import {
  THEME_BOOTSTRAP_SCRIPT,
  isKnownThemeMode,
  resolveEffectiveColorScheme,
  resolveThemeMode,
} from "./theme-mode.ts";

test("legacy Light and Dark map to LightOnly and DarkOnly", () => {
  assert.equal(resolveThemeMode("Light"), "LightOnly");
  assert.equal(resolveThemeMode("Dark"), "DarkOnly");
  assert.equal(resolveThemeMode("unknown"), "LightOnly");
  assert.equal(isKnownThemeMode("UserChoice"), true);
  assert.equal(isKnownThemeMode("Sepia"), false);
});

test("effective scheme follows each ThemeMode", () => {
  assert.equal(resolveEffectiveColorScheme("LightOnly", "dark", true), "light");
  assert.equal(resolveEffectiveColorScheme("DarkOnly", "light", false), "dark");
  assert.equal(resolveEffectiveColorScheme("System", null, true), "dark");
  assert.equal(resolveEffectiveColorScheme("System", "light", false), "light");
  assert.equal(resolveEffectiveColorScheme("UserChoice", "dark", false), "dark");
  assert.equal(resolveEffectiveColorScheme("UserChoice", null, true), "light");
});

test("bootstrap script is static and not loaded from a database payload", () => {
  assert.match(THEME_BOOTSTRAP_SCRIPT, /prefers-color-scheme/);
  assert.match(THEME_BOOTSTRAP_SCRIPT, /tooba.storefront.color-scheme/);
  assert.doesNotMatch(THEME_BOOTSTRAP_SCRIPT, /fetch\(|\/v1\/storefront\/appearance/);
});
