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
  "/customer-panel/notifications",
  "/customer-panel/tickets",
  "/customer-panel/profile",
  "/customer-panel/settings",
] as const;

const DEFERRED_HREFS = [
  "/customer-panel/wallet",
  "/customer-panel/gift-cards",
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
  assert.equal(shellSource.includes('"/customer-panel/tickets"') && shellSource.includes("CUSTOMER_DEFERRED_NAV_HREFS"), true);
  const deferredBlock = shellSource.slice(
    shellSource.indexOf("export const CUSTOMER_DEFERRED_NAV_HREFS"),
    shellSource.indexOf("] as const;", shellSource.indexOf("export const CUSTOMER_DEFERRED_NAV_HREFS")) + 11,
  );
  assert.equal(deferredBlock.includes("/customer-panel/tickets"), false, "tickets must not remain deferred");
});

test("customer shell menuItems keep settings live and omit deferred wallet/gift-cards", () => {
  const menuBlock = extractMenuItemsBlock(shellSource);

  const settingsIdx = menuBlock.indexOf('href: "/customer-panel/settings"');
  assert.ok(settingsIdx >= 0, "settings href missing from menuItems");
  const settingsEntry = menuBlock.slice(settingsIdx, settingsIdx + 120);
  assert.ok(settingsEntry.includes("live: true"), "settings must be live: true");

  const ticketsIdx = menuBlock.indexOf('href: "/customer-panel/tickets"');
  assert.ok(ticketsIdx >= 0, "tickets href missing from live menuItems");
  assert.ok(menuBlock.slice(ticketsIdx, ticketsIdx + 120).includes("live: true"), "tickets must be live");

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
