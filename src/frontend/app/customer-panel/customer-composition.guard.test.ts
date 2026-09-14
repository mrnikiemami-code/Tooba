import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const dash = fs.readFileSync(path.join(root, "app/customer-panel/page.tsx"), "utf8");
const shell = fs.readFileSync(path.join(root, "app/customer-panel/customer-panel-shell.tsx"), "utf8");
const orders = fs.readFileSync(path.join(root, "app/customer-panel/orders/page.tsx"), "utf8");

test("customer panel composition uses section context and inherit wrappers", () => {
  assert.match(shell, /data-storefront-surface-role="page"/);
  assert.match(shell, /data-testid="customer-panel-main"[^>]*data-storefront-surface-role="section"/);
  assert.match(dash, /data-storefront-surface-role="inherit"/);
  assert.match(dash, /data-storefront-surface-role="elevated"/);
  assert.doesNotMatch(dash, /to-white/);
  assert.doesNotMatch(dash, /lg:col-span-2 bg-surface rounded-2xl/);
  assert.match(orders, /data-storefront-surface-role="inherit"/);
  assert.doesNotMatch(dash, /loadStorefrontAppearance/);
});
