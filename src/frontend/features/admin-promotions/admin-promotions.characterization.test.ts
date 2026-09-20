import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { mapAdminPromotions } from "./api/promotions-api.ts";

const feRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const featureRoot = path.join(feRoot, "features/admin-promotions");

test("admin-promotions public boundary exports screen and API", () => {
  const index = fs.readFileSync(path.join(featureRoot, "index.ts"), "utf8");
  assert.match(index, /AdminPromotionsScreen/);
  assert.match(index, /loadAdminPromotions/);
  assert.match(index, /deactivateAdminPromotion/);
});

test("promotions route stays thin composition through public boundary", () => {
  const page = fs.readFileSync(path.join(feRoot, "app/admin/promotions/page.tsx"), "utf8");
  assert.match(page, /features\/admin-promotions/);
  assert.doesNotMatch(page, /admin-screens/);
  assert.ok(!page.includes("use client"));
});

test("promotions API maps Host payload and uses shared admin-result", () => {
  const api = fs.readFileSync(path.join(featureRoot, "api/promotions-api.ts"), "utf8");
  assert.match(api, /lib\/admin\/admin-result/);
  assert.match(api, /\/v1\/admin\/promotions/);
  assert.doesNotMatch(api, /from ["'].*admin-api/);
  const rows = mapAdminPromotions([
    {
      promotionId: "p1",
      name: "Test",
      status: 1,
      discountKind: 0,
      percentageRate: 0.1,
      fixedAmount: 0,
    },
  ]);
  assert.ok(rows);
  assert.equal(rows![0].status, "Active");
  assert.equal(rows![0].discountKind, "PercentageOff");
});

test("promotions screen keeps deactivate action and grid markers", () => {
  const screen = fs.readFileSync(path.join(featureRoot, "components/promotions-screen.tsx"), "utf8");
  assert.match(screen, /data-testid="admin-promotions"/);
  assert.match(screen, /loadAdminPromotions/);
  assert.match(screen, /deactivateAdminPromotion/);
  assert.match(screen, /["']use client["']/);
});

test("admin-api no longer owns promotions capability exports", () => {
  const api = fs.readFileSync(path.join(feRoot, "app/admin/admin-api.ts"), "utf8");
  assert.doesNotMatch(api, /AdminPromotionRow/);
  assert.doesNotMatch(api, /loadAdminPromotions/);
  assert.doesNotMatch(api, /deactivateAdminPromotion/);
});
