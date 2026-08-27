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
  "/customer-panel/wallet",
  "/customer-panel/gift-cards",
  "/customer-panel/profile",
  "/customer-panel/settings",
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

  const deferredBlock = shellSource.slice(
    shellSource.indexOf("export const CUSTOMER_DEFERRED_NAV_HREFS"),
    shellSource.indexOf("] as const;", shellSource.indexOf("export const CUSTOMER_DEFERRED_NAV_HREFS")) + 11,
  );
  assert.equal(deferredBlock.includes("/customer-panel/wallet"), false, "wallet must not remain deferred");
  assert.equal(deferredBlock.includes("/customer-panel/gift-cards"), false, "gift-cards must not remain deferred");
  assert.equal(deferredBlock.includes("/customer-panel/tickets"), false, "tickets must not remain deferred");
});

test("customer shell menuItems keep wallet/gift-cards live alongside settings", () => {
  const menuBlock = extractMenuItemsBlock(shellSource);

  const settingsIdx = menuBlock.indexOf('href: "/customer-panel/settings"');
  assert.ok(settingsIdx >= 0, "settings href missing from menuItems");
  assert.ok(menuBlock.slice(settingsIdx, settingsIdx + 120).includes("live: true"), "settings must be live: true");

  const ticketsIdx = menuBlock.indexOf('href: "/customer-panel/tickets"');
  assert.ok(ticketsIdx >= 0, "tickets href missing from live menuItems");
  assert.ok(menuBlock.slice(ticketsIdx, ticketsIdx + 120).includes("live: true"), "tickets must be live");

  const walletIdx = menuBlock.indexOf('href: "/customer-panel/wallet"');
  assert.ok(walletIdx >= 0, "wallet href missing from live menuItems");
  assert.ok(menuBlock.slice(walletIdx, walletIdx + 120).includes("live: true"), "wallet must be live");
  assert.ok(menuBlock.includes("Wallet"), "wallet nav must use Wallet icon");

  const giftIdx = menuBlock.indexOf('href: "/customer-panel/gift-cards"');
  assert.ok(giftIdx >= 0, "gift-cards href missing from live menuItems");
  assert.ok(menuBlock.slice(giftIdx, giftIdx + 120).includes("live: true"), "gift-cards must be live");
  assert.ok(menuBlock.includes("CreditCard"), "gift-cards nav must use CreditCard icon");

  for (const href of LIVE_HREFS) {
    if (href === "/customer-panel") {
      assert.ok(menuBlock.includes('href: "/customer-panel"'), "dashboard href missing");
      continue;
    }
    assert.ok(menuBlock.includes(`"${href}"`), `live href missing from menuItems: ${href}`);
  }
});
