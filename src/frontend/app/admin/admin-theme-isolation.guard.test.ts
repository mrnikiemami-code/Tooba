import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const layout = fs.readFileSync(path.join(root, "app/layout.tsx"), "utf8");
const adminLayout = fs.readFileSync(path.join(root, "app/admin/layout.tsx"), "utf8");
const globals = fs.readFileSync(path.join(root, "app/globals.css"), "utf8");
const shell = fs.readFileSync(path.join(root, "app/storefront/storefront-shell.tsx"), "utf8");
const canvas = fs.readFileSync(path.join(root, "app/storefront/storefront-themed-canvas.tsx"), "utf8");

test("admin-theme-isolation: storefront appearance style is not applied on root html", () => {
  assert.match(layout, /StorefrontAppearanceProvider/);
  assert.doesNotMatch(layout, /style=\{storefrontAppearanceStyle/);
  assert.match(adminLayout, /data-panel-theme="admin"/);
  assert.match(globals, /\[data-panel-theme="admin"\]/);
  assert.match(globals, /--color-primary: 37 99 235/);
});

test("admin-theme-isolation: storefront tokens apply only inside themed canvas", () => {
  assert.match(shell, /StorefrontThemedCanvas/);
  assert.match(canvas, /data-storefront-canvas/);
  assert.match(canvas, /useStorefrontAppearanceStyle/);
  assert.match(canvas, /data-storefront-theme-scope="storefront"/);
});
