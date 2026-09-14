import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { CUSTOMER_PANEL_NAV_HREFS, STOREFRONT_ROUTE_INVENTORY } from "./storefront-route-inventory.ts";
import { PUBLIC_STOREFRONT_PREFIXES } from "../i18n/routing.ts";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");

function inventoryCoversPrefix(prefix: string): boolean {
  return STOREFRONT_ROUTE_INVENTORY.some((row) => {
    if (row.pattern === prefix) return true;
    if (row.pattern.startsWith(`${prefix}/`)) return true;
    const staticBase = row.pattern.replace(/\/\[[^\]]+\]/g, "");
    return staticBase === prefix || (staticBase.length > 0 && prefix.startsWith(`${staticBase}/`));
  });
}

test("route inventory covers public storefront prefixes and customer nav", () => {
  for (const prefix of PUBLIC_STOREFRONT_PREFIXES) {
    assert.ok(inventoryCoversPrefix(prefix), `missing inventory coverage for ${prefix}`);
  }
  for (const href of CUSTOMER_PANEL_NAV_HREFS) {
    assert.ok(STOREFRONT_ROUTE_INVENTORY.some((row) => row.pattern === href), `missing customer nav ${href}`);
  }
  assert.ok(STOREFRONT_ROUTE_INVENTORY.some((row) => row.id === "home"));
  assert.ok(STOREFRONT_ROUTE_INVENTORY.some((row) => row.id === "pdp"));
  assert.ok(STOREFRONT_ROUTE_INVENTORY.some((row) => row.id === "login"));
  assert.ok(STOREFRONT_ROUTE_INVENTORY.some((row) => row.id === "blogs"));
  assert.equal(STOREFRONT_ROUTE_INVENTORY.every((row) => row.themeCoverage.length > 0), true);
});

test("customer panel live nav matches inventory and shell", () => {
  const shell = fs.readFileSync(path.join(root, "app/customer-panel/customer-panel-shell.tsx"), "utf8");
  for (const href of CUSTOMER_PANEL_NAV_HREFS) {
    assert.ok(shell.includes(`href: "${href}"`), `shell missing ${href}`);
  }
});

test("inventory crawler source exists as a reusable script", () => {
  assert.ok(fs.existsSync(path.join(root, "scripts/storefront-theme-coverage.mjs")));
});
