import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const shell = fs.readFileSync(path.join(root, "app/customer-panel/customer-panel-shell.tsx"), "utf8");
const layout = fs.readFileSync(path.join(root, "app/customer-panel/layout.tsx"), "utf8");
const storefrontLayout = fs.readFileSync(path.join(root, "app/layout.tsx"), "utf8");

test("customer panel inherits store appearance through shared shell tokens", () => {
  assert.match(layout, /CustomerPanelShell/);
  assert.match(shell, /data-customer-panel-canvas/);
  assert.match(shell, /data-storefront-surface-role="page"/);
  assert.match(shell, /data-storefront-surface-role="header"/);
  assert.match(shell, /data-storefront-surface-role="section"/);
  assert.match(shell, /bg-page/);
  assert.match(shell, /bg-section-surface/);
  assert.match(shell, /bg-surface/);
  assert.doesNotMatch(shell, /loadStorefrontAppearance/);
  assert.doesNotMatch(shell, /\/v1\/storefront\/appearance/);
  assert.doesNotMatch(shell, /paletteKey/);
  assert.doesNotMatch(shell, /sticky top-0 z-40 bg-white/);
});

test("root layout remains the only storefront appearance projection", () => {
  assert.match(storefrontLayout, /loadStorefrontAppearance/);
  assert.equal((shell.match(/loadStorefrontAppearance/g) ?? []).length, 0);
});
