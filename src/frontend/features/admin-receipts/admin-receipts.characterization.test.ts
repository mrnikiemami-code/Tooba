import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { mapAdminReceipt, queryAdminReceiptsGrid } from "./api/receipts-api.ts";

const feRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const featureRoot = path.join(feRoot, "features/admin-receipts");

test("admin-receipts public boundary exports screen and API", () => {
  const index = fs.readFileSync(path.join(featureRoot, "index.ts"), "utf8");
  assert.match(index, /AdminReceiptsScreen/);
  assert.match(index, /mapAdminReceipt/);
  assert.match(index, /queryAdminReceiptsGrid/);
});

test("receipts route stays thin composition through public boundary", () => {
  const page = fs.readFileSync(path.join(feRoot, "app/admin/receipts/page.tsx"), "utf8");
  assert.match(page, /features\/admin-receipts/);
  assert.doesNotMatch(page, /admin-screens/);
  assert.ok(!page.includes("use client"));
});

test("receipts API maps Host payload with reservation projection", () => {
  const api = fs.readFileSync(path.join(featureRoot, "api/receipts-api.ts"), "utf8");
  assert.match(api, /lib\/admin\/admin-result/);
  assert.match(api, /\/v1\/admin\/payments\/query/);
  assert.doesNotMatch(api, /from ["'].*admin-api/);
  const receipt = mapAdminReceipt({
    PaymentId: "p1",
    CheckoutId: "c1",
    ReservationLabel: "پایان‌یافته #1",
    ReservationState: "expired",
    ReservationRetryPossible: true,
    ReservationNeedsReacquire: true,
  });
  assert.equal(receipt?.reservationLabel, "پایان‌یافته #1");
  assert.equal(receipt?.reservationRetryPossible, true);
  assert.equal(typeof queryAdminReceiptsGrid, "function");
});

test("receipts screen keeps grid markers and deep-link actions", () => {
  const screen = fs.readFileSync(path.join(featureRoot, "components/receipts-screen.tsx"), "utf8");
  assert.match(screen, /testId=["']admin-receipts["']/);
  assert.match(screen, /queryAdminReceiptsGrid/);
  assert.match(screen, /ADMIN_RECEIPT_GRID_VIEW_KEY/);
  assert.match(screen, /رزرو موجودی/);
  assert.match(screen, /["']use client["']/);
});

test("admin-api no longer owns receipts list capability exports", () => {
  const api = fs.readFileSync(path.join(feRoot, "app/admin/admin-api.ts"), "utf8");
  assert.doesNotMatch(api, /export interface AdminReceiptRow/);
  assert.doesNotMatch(api, /export function mapAdminReceipt/);
  assert.doesNotMatch(api, /export function queryAdminReceiptsGrid/);
});
