import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { mapAdminDashboard, loadAdminDashboard } from "./api/dashboard-api.ts";

const feRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const featureRoot = path.join(feRoot, "features/admin-dashboard");

test("admin-dashboard public boundary exports screen and API", () => {
  const index = fs.readFileSync(path.join(featureRoot, "index.ts"), "utf8");
  assert.match(index, /AdminDashboardScreen/);
  assert.match(index, /loadAdminDashboard/);
  assert.match(index, /mapAdminDashboard/);
});

test("dashboard route stays thin composition through public boundary", () => {
  const page = fs.readFileSync(path.join(feRoot, "app/admin/page.tsx"), "utf8");
  assert.match(page, /features\/admin-dashboard/);
  assert.doesNotMatch(page, /admin-screens/);
  assert.ok(!page.includes("use client"));
});

test("dashboard API maps Host metrics without fake revenue fields", () => {
  const api = fs.readFileSync(path.join(featureRoot, "api/dashboard-api.ts"), "utf8");
  assert.match(api, /lib\/admin\/admin-result/);
  assert.match(api, /\/v1\/admin\/dashboard/);
  assert.doesNotMatch(api, /from ["'].*admin-api/);
  const dashboard = mapAdminDashboard({
    ActiveProducts: 2,
    ActiveOffers: 3,
    OpenOrders: 4,
    PaidOrders: 1,
    PendingOrders: 3,
    SellersCount: 5,
    CustomersCount: 6,
  });
  assert.equal(dashboard?.activeOffers, 3);
  assert.equal(dashboard?.customersCount, 6);
});

test("dashboard screen keeps live metric markers", () => {
  const screen = fs.readFileSync(path.join(featureRoot, "components/dashboard-screen.tsx"), "utf8");
  assert.match(screen, /data-testid="admin-dashboard"/);
  assert.match(screen, /loadAdminDashboard/);
  assert.match(screen, /["']use client["']/);
  assert.match(screen, /نمایش داده نمی‌شود/);
  assert.doesNotMatch(screen, /label=["']درآمد/);
  assert.doesNotMatch(screen, /Summary label=["']درآمد/);
});
test("admin-api no longer owns dashboard capability exports", () => {
  const api = fs.readFileSync(path.join(feRoot, "app/admin/admin-api.ts"), "utf8");
  assert.doesNotMatch(api, /export interface AdminDashboard/);
  assert.doesNotMatch(api, /export function loadAdminDashboard/);
  assert.doesNotMatch(api, /export function mapAdminDashboard/);
});

test("dashboard load exposes server denied", async () => {
  const originalFetch = globalThis.fetch;
  globalThis.fetch = (async () => new Response(null, { status: 403 })) as typeof fetch;
  try {
    assert.equal((await loadAdminDashboard()).state, "denied");
  } finally {
    globalThis.fetch = originalFetch;
  }
});
