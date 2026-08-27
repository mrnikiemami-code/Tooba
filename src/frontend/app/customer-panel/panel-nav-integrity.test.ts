import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const shellSource = fs.readFileSync(path.join(root, "app/customer-panel/customer-panel-shell.tsx"), "utf8");

const LIVE_HREFS = [
  "/customer-panel",
  "/customer-panel/orders",
  "/customer-panel/wishlist",
  "/customer-panel/addresses",
  "/customer-panel/profile",
  "/customer-panel/settings",
] as const;

const DEFERRED_HREFS = [
  "/customer-panel/wallet",
  "/customer-panel/tickets",
  "/customer-panel/gift-cards",
  "/customer-panel/notifications",
] as const;

function extractMenuItemsBlock(source: string): string {
  const start = source.indexOf("const menuItems");
  const end = source.indexOf("export const CUSTOMER_DEFERRED_NAV_HREFS");
  assert.ok(start >= 0 && end > start, "menuItems / CUSTOMER_DEFERRED_NAV_HREFS markers missing");
  return source.slice(start, end);
}

test("customer shell exports deferred hrefs and filters live-only nav", () => {
  assert.ok(shellSource.includes("export const CUSTOMER_DEFERRED_NAV_HREFS"), "missing CUSTOMER_DEFERRED_NAV_HREFS export");
  assert.ok(shellSource.includes("visibleMenuItems = menuItems.filter((item) => item.live)"), "missing live-only filter");
  assert.ok(shellSource.includes("visibleMenuItems.map"), "nav must render visibleMenuItems");

  for (const href of DEFERRED_HREFS) {
    assert.ok(shellSource.includes(`"${href}"`), `deferred export missing ${href}`);
  }
});

test("customer shell menuItems keep settings live and omit deferred wallet/tickets", () => {
  const menuBlock = extractMenuItemsBlock(shellSource);

  const settingsIdx = menuBlock.indexOf('href: "/customer-panel/settings"');
  assert.ok(settingsIdx >= 0, "settings href missing from menuItems");
  const settingsEntry = menuBlock.slice(settingsIdx, settingsIdx + 120);
  assert.ok(settingsEntry.includes("live: true"), "settings must be live: true");

  for (const href of DEFERRED_HREFS) {
    assert.equal(menuBlock.includes(`"${href}"`), false, `${href} must not appear in live menuItems`);
  }

  for (const href of LIVE_HREFS) {
    if (href === "/customer-panel") {
      assert.ok(menuBlock.includes('href: "/customer-panel"'), "dashboard href missing");
      continue;
    }
    assert.ok(menuBlock.includes(`"${href}"`), `live href missing from menuItems: ${href}`);
  }
});
