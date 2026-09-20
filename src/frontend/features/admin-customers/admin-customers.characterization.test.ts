import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { mapAdminCustomers, loadAdminCustomers } from "./api/customers-api.ts";

const feRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const featureRoot = path.join(feRoot, "features/admin-customers");

test("admin-customers public boundary exports screen and API", () => {
  const index = fs.readFileSync(path.join(featureRoot, "index.ts"), "utf8");
  assert.match(index, /AdminCustomersScreen/);
  assert.match(index, /loadAdminCustomers/);
  assert.match(index, /queryAdminCustomersGrid/);
  assert.match(index, /mapAdminCustomers/);
});

test("customers route stays thin composition through public boundary", () => {
  const page = fs.readFileSync(path.join(feRoot, "app/admin/customers/page.tsx"), "utf8");
  assert.match(page, /features\/admin-customers/);
  assert.doesNotMatch(page, /admin-screens/);
  assert.ok(!page.includes("use client"));
});

test("customers API maps Host payload without CRM invention", () => {
  const api = fs.readFileSync(path.join(featureRoot, "api/customers-api.ts"), "utf8");
  assert.match(api, /lib\/admin\/admin-result/);
  assert.match(api, /\/v1\/admin\/customers/);
  assert.doesNotMatch(api, /from ["'].*admin-api/);
  const rows = mapAdminCustomers([{ ActorUserId: "u1", DisplayName: "مینا", OrderCount: 4, LastOrderAt: "2026-08-25T00:00:00Z" }]);
  assert.equal(rows[0]?.orderCount, 4);
  assert.equal(rows[0]?.displayName, "مینا");
});

test("customers screen keeps grid markers and query wiring", () => {
  const screen = fs.readFileSync(path.join(featureRoot, "components/customers-screen.tsx"), "utf8");
  assert.match(screen, /testId=["']admin-customers["']/);
  assert.match(screen, /queryAdminCustomersGrid/);
  assert.match(screen, /["']use client["']/);
});

test("admin-api no longer owns customers list capability exports", () => {
  const api = fs.readFileSync(path.join(feRoot, "app/admin/admin-api.ts"), "utf8");
  assert.doesNotMatch(api, /export interface AdminCustomerRow/);
  assert.doesNotMatch(api, /export function loadAdminCustomers/);
  assert.doesNotMatch(api, /export function mapAdminCustomers/);
  assert.doesNotMatch(api, /export function queryAdminCustomersGrid/);
});

test("customers list exposes server denied", async () => {
  const originalFetch = globalThis.fetch;
  globalThis.fetch = (async () => new Response(null, { status: 403 })) as typeof fetch;
  try {
    assert.equal((await loadAdminCustomers()).state, "denied");
  } finally {
    globalThis.fetch = originalFetch;
  }
});
