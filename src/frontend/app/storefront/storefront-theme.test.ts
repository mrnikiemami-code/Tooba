import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const dir = dirname(fileURLToPath(import.meta.url));
const layout = readFileSync(join(dir, "../layout.tsx"), "utf8");
const toggle = readFileSync(join(dir, "storefront-theme-toggle.tsx"), "utf8");
const header = readFileSync(join(dir, "storefront-header.tsx"), "utf8");
const appearance = readFileSync(join(dir, "storefront-appearance-api.ts"), "utf8");
const provider = readFileSync(join(dir, "../../design-system/theme/ThemeProvider.tsx"), "utf8");
const globals = readFileSync(join(dir, "../globals.css"), "utf8");

test("layout applies SSR theme markers and blocking bootstrap", () => {
  assert.match(layout, /data-storefront-theme-mode/);
  assert.match(layout, /data-storefront-color-scheme/);
  assert.match(layout, /THEME_BOOTSTRAP_SCRIPT/);
  assert.match(layout, /className=\{colorScheme === "dark" \? "dark" : undefined\}/);
  assert.match(layout, /bg-background text-foreground/);
});

test("UserChoice toggle is in header and does not write appearance API", () => {
  assert.match(header, /StorefrontThemeToggle/);
  assert.match(toggle, /data-testid="storefront-theme-toggle"/);
  assert.match(toggle, /aria-pressed/);
  assert.doesNotMatch(toggle, /\/v1\/storefront\/appearance|\/v1\/admin\/settings\/appearance/);
  assert.match(toggle, /localStorage.setItem/);
  assert.match(toggle, /tooba-storefront-color-scheme/);
});

test("one ThemeProvider remains and does not force light after SSR", () => {
  assert.match(provider, /readDocumentScheme/);
  assert.doesNotMatch(provider, /root.classList.toggle\("dark"/);
  assert.equal((provider.match(/export function ThemeProvider/g) ?? []).length, 1);
});

test("pages still do not poll appearance and dark remaps stay canonical", () => {
  assert.doesNotMatch(appearance, /setInterval/);
  assert.match(globals, /html\.dark \.bg-white/);
  assert.match(globals, /html\.dark \.bg-white\\\/90/);
  assert.match(globals, /html\.dark \.text-primary/);
  assert.match(globals, /--color-brand-emphasis: var\(--color-primary-on-dark\)/);
  assert.match(globals, /--color-danger: 248 113 113/);
  assert.doesNotMatch(globals, /--color-primary: 96 165 250/);
  assert.doesNotMatch(globals, /wine-burgundy/);
});
