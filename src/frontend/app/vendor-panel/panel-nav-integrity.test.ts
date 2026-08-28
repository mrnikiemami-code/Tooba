import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const shellSource = fs.readFileSync(path.join(root, "app/vendor-panel/vendor-shell.tsx"), "utf8");

const LIVE_HREFS = [
  "/vendor-panel",
  "/vendor-panel/products",
  "/vendor-panel/orders",
  "/vendor-panel/notifications",
  "/vendor-panel/stories",
  "/vendor-panel/coupons",
  "/vendor-panel/reviews",
  "/vendor-panel/fulfillments",
  "/vendor-panel/returns",
  "/vendor-panel/tickets",
  "/vendor-panel/analytics",
  "/vendor-panel/wallet",
  "/vendor-panel/access-control",
  "/vendor-panel/settings",
] as const;

const DEFERRED_HREFS = [
  "/vendor-panel/customers",
  "/vendor-panel/gift-cards",
] as const;

function extractMenuItemsBlock(source: string): string {
  const start = source.indexOf("const menuItems");
  const end = source.indexOf("export const VENDOR_DEFERRED_NAV_HREFS");
  assert.ok(start >= 0 && end > start, "menuItems / VENDOR_DEFERRED_NAV_HREFS markers missing");
  return source.slice(start, end);
}

test("vendor shell exports deferred hrefs and filters live-only nav", () => {
  assert.ok(shellSource.includes("export const VENDOR_DEFERRED_NAV_HREFS"), "missing VENDOR_DEFERRED_NAV_HREFS export");
  assert.ok(shellSource.includes("if (!item.live) return false"), "missing live-only gate inside itemAllowed");
  assert.ok(
    shellSource.includes("visibleMenuItems = useMemo(() => menuItems.filter((item) => itemAllowed(item, caps)), [caps])"),
    "missing capability-aware live nav filter",
  );
  assert.ok(shellSource.includes("visibleMenuItems.map"), "nav must render visibleMenuItems");

  for (const href of DEFERRED_HREFS) {
    assert.ok(shellSource.includes(`"${href}"`), `deferred export missing ${href}`);
  }

  const deferredStart = shellSource.indexOf("export const VENDOR_DEFERRED_NAV_HREFS");
  const deferredBlock = shellSource.slice(
    deferredStart,
    shellSource.indexOf("] as const;", deferredStart) + 11,
  );
  assert.equal(deferredBlock.includes("/vendor-panel/tickets"), false, "tickets must not remain deferred");
});

test("vendor shell menuItems keep tickets live behind support.view", () => {
  const menuBlock = extractMenuItemsBlock(shellSource);

  const settingsIdx = menuBlock.indexOf('href: "/vendor-panel/settings"');
  assert.ok(settingsIdx >= 0, "settings href missing from menuItems");
  assert.ok(menuBlock.slice(settingsIdx, settingsIdx + 120).includes("live: true"), "settings must be live: true");

  const ticketsIdx = menuBlock.indexOf('href: "/vendor-panel/tickets"');
  assert.ok(ticketsIdx >= 0, "tickets href missing from live menuItems");
  const ticketsEntry = menuBlock.slice(ticketsIdx, ticketsIdx + 160);
  assert.ok(ticketsEntry.includes("live: true"), "tickets must be live");
  assert.ok(ticketsEntry.includes('viewPermission: "support.view"'), "tickets must project support.view");

  const accessIdx = menuBlock.indexOf('href: "/vendor-panel/access-control"');
  assert.ok(accessIdx >= 0, "access-control href missing from live menuItems");
  assert.ok(accessIdx < settingsIdx, "access-control must appear before settings");

  for (const href of DEFERRED_HREFS) {
    assert.equal(menuBlock.includes(`"${href}"`), false, `${href} must not appear in live menuItems`);
  }

  for (const href of LIVE_HREFS) {
    if (href === "/vendor-panel") {
      assert.ok(menuBlock.includes('href: "/vendor-panel"'), "dashboard href missing");
      continue;
    }
    assert.ok(menuBlock.includes(`"${href}"`), `live href missing from menuItems: ${href}`);
  }
});
