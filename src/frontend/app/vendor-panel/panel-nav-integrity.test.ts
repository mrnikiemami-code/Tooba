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
  "/vendor-panel/analytics",
  "/vendor-panel/wallet",
  "/vendor-panel/settings",
] as const;

const DEFERRED_HREFS = [
  "/vendor-panel/customers",
  "/vendor-panel/tickets",
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
  // live gate + capability projection (TB-P06-T024-R1): itemAllowed requires item.live
  assert.ok(shellSource.includes("if (!item.live) return false"), "missing live-only gate inside itemAllowed");
  assert.ok(
    shellSource.includes("visibleMenuItems = useMemo(() => menuItems.filter((item) => itemAllowed(item, caps)), [caps])"),
    "missing capability-aware live nav filter",
  );
  assert.ok(shellSource.includes("visibleMenuItems.map"), "nav must render visibleMenuItems");

  for (const href of DEFERRED_HREFS) {
    assert.ok(shellSource.includes(`"${href}"`), `deferred export missing ${href}`);
  }
});

test("vendor shell menuItems keep settings live and omit deferred routes", () => {
  const menuBlock = extractMenuItemsBlock(shellSource);

  const settingsIdx = menuBlock.indexOf('href: "/vendor-panel/settings"');
  assert.ok(settingsIdx >= 0, "settings href missing from menuItems");
  const settingsEntry = menuBlock.slice(settingsIdx, settingsIdx + 120);
  assert.ok(settingsEntry.includes("live: true"), "settings must be live: true");

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
