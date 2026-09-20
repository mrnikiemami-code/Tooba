import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { mapAdminSellers, loadAdminSellers } from "./api/sellers-api.ts";

const feRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const featureRoot = path.join(feRoot, "features/admin-sellers");

test("admin-sellers public boundary exports screen and API", () => {
  const index = fs.readFileSync(path.join(featureRoot, "index.ts"), "utf8");
  assert.match(index, /AdminSellersScreen/);
  assert.match(index, /loadAdminSellers/);
  assert.match(index, /queryAdminSellersGrid/);
  assert.match(index, /mapAdminSellers/);
});

test("sellers route stays thin composition through public boundary", () => {
  const page = fs.readFileSync(path.join(feRoot, "app/admin/sellers/page.tsx"), "utf8");
  assert.match(page, /features\/admin-sellers/);
  assert.doesNotMatch(page, /admin-screens/);
  assert.ok(!page.includes("use client"));
});

test("sellers API maps Host payload without CRM invention", () => {
  const api = fs.readFileSync(path.join(featureRoot, "api/sellers-api.ts"), "utf8");
  assert.match(api, /lib\/admin\/admin-result/);
  assert.match(api, /\/v1\/admin\/sellers/);
  assert.doesNotMatch(api, /from ["'].*admin-api/);
  const rows = mapAdminSellers([{ SellerPartyId: "s1", SellerDisplayName: "فروشگاه آرمان", ActiveOffers: 7 }]);
  assert.equal(rows[0]?.activeOfferCount, 7);
  assert.equal(rows[0]?.displayName, "فروشگاه آرمان");
});

test("sellers screen keeps grid markers and query wiring", () => {
  const screen = fs.readFileSync(path.join(featureRoot, "components/sellers-screen.tsx"), "utf8");
  assert.match(screen, /testId=["']admin-sellers["']/);
  assert.match(screen, /queryAdminSellersGrid/);
  assert.match(screen, /["']use client["']/);
});

test("admin-api no longer owns sellers list capability exports", () => {
  const api = fs.readFileSync(path.join(feRoot, "app/admin/admin-api.ts"), "utf8");
  assert.doesNotMatch(api, /export interface AdminSellerRow/);
  assert.doesNotMatch(api, /export function loadAdminSellers/);
  assert.doesNotMatch(api, /export function mapAdminSellers/);
  assert.doesNotMatch(api, /export function queryAdminSellersGrid/);
});

test("sellers list exposes server denied", async () => {
  const originalFetch = globalThis.fetch;
  globalThis.fetch = (async () => new Response(null, { status: 403 })) as typeof fetch;
  try {
    assert.equal((await loadAdminSellers()).state, "denied");
  } finally {
    globalThis.fetch = originalFetch;
  }
});
