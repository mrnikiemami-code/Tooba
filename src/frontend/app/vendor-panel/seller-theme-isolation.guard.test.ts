import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const vendorLayout = fs.readFileSync(path.join(root, "app/vendor-panel/layout.tsx"), "utf8");
const globals = fs.readFileSync(path.join(root, "app/globals.css"), "utf8");
const layout = fs.readFileSync(path.join(root, "app/layout.tsx"), "utf8");

test("seller-theme-isolation: seller panel uses fixed panel tokens", () => {
  assert.match(vendorLayout, /data-panel-theme="seller"/);
  assert.match(globals, /\[data-panel-theme="seller"\]/);
  assert.match(globals, /LOCK-SF-261|Storefront Appearance must never recolor/);
});

test("seller-theme-isolation: root does not paint storefront style onto panels", () => {
  assert.doesNotMatch(layout, /style=\{storefrontAppearanceStyle/);
  assert.match(layout, /StorefrontAppearanceProvider/);
});
