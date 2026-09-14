import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const read = (rel: string) => fs.readFileSync(path.join(root, rel), "utf8");

test("major storefront wrappers use semantic surface roles instead of page-level bg-white", () => {
  const shell = read("app/storefront/storefront-shell.tsx");
  const header = read("app/storefront/storefront-header.tsx");
  const footer = read("app/storefront/storefront-footer.tsx");
  const login = read("app/login/storefront-login.tsx");
  const pdp = read("app/storefront/storefront-pdp.tsx");
  const shipping = read("app/storefront/storefront-shipping.tsx");
  const primitive = read("app/storefront/storefront-surface.tsx");
  const home = read("app/storefront/storefront-home.tsx");
  const stories = read("app/storefront/stories/home-stories.tsx");

  assert.match(shell, /data-storefront-surface-role="page"/);
  assert.match(shell, /bg-page/);
  assert.match(shell, /fullBleed/);
  assert.doesNotMatch(shell, /min-h-screen bg-white/);

  assert.match(header, /data-storefront-surface-role="header"/);
  assert.match(header, /bg-surface/);
  assert.doesNotMatch(header, /w-full bg-white border-b/);

  assert.match(footer, /data-storefront-surface-role="footer"/);
  assert.doesNotMatch(footer, /mt-10 bg-white border-t/);

  assert.match(login, /data-storefront-surface-role="section"/);
  assert.match(login, /data-storefront-surface-role="elevated"/);
  assert.doesNotMatch(login, /bg-white rounded-2xl border border-gray-200 shadow-sm p-5/);

  assert.match(pdp, /data-testid="pdp-primary-card"/);
  assert.match(pdp, /data-storefront-surface-role="inherit"/);
  assert.match(pdp, /data-storefront-surface-role="alternate"/);
  assert.doesNotMatch(pdp, /data-testid="pdp-primary-card"[^>]*data-storefront-surface-role="card"/);
  assert.doesNotMatch(pdp, /<div className="bg-white rounded-2xl border border-gray-200 shadow-sm">/);

  assert.doesNotMatch(shipping, /section className="w-full bg-white"/);
  assert.match(shipping, /bg-section-surface/);

  assert.match(primitive, /export function StorefrontPageSurface/);
  assert.match(primitive, /export function StorefrontSectionSurface/);
  assert.match(primitive, /export function StorefrontPanelSurface/);
  assert.doesNotMatch(primitive, /backgroundColor/);
  assert.doesNotMatch(primitive, /bg-\[#/);

  assert.match(home, /data-storefront-surface-role="accent"/);
  assert.match(stories, /data-storefront-surface-role="section"/);
});

test("storefront wrappers do not fetch appearance per page", () => {
  const files = [
    "app/storefront/storefront-shell.tsx",
    "app/storefront/storefront-pdp.tsx",
    "app/login/storefront-login.tsx",
    "app/storefront/storefront-shipping.tsx",
    "app/customer-panel/customer-panel-shell.tsx",
    "app/blogs/layout.tsx",
  ];
  for (const file of files) {
    const source = read(file);
    assert.doesNotMatch(source, /loadStorefrontAppearance/);
    assert.doesNotMatch(source, /\/v1\/storefront\/appearance/);
  }
});
