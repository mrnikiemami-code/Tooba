import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { mapAdminReviews, loadAdminReviews, moderateAdminReview } from "./api/reviews-api.ts";

const feRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const featureRoot = path.join(feRoot, "features/admin-reviews");

test("admin-reviews public boundary exports screen and API", () => {
  const index = fs.readFileSync(path.join(featureRoot, "index.ts"), "utf8");
  assert.match(index, /AdminReviewsScreen/);
  assert.match(index, /loadAdminReviews/);
  assert.match(index, /moderateAdminReview/);
  assert.match(index, /queryAdminReviewsGrid/);
});

test("reviews route stays thin composition through public boundary", () => {
  const page = fs.readFileSync(path.join(feRoot, "app/admin/reviews/page.tsx"), "utf8");
  assert.match(page, /features\/admin-reviews/);
  assert.doesNotMatch(page, /admin-screens/);
  assert.ok(!page.includes("use client"));
});

test("reviews API maps Host payload without internal actor data", () => {
  const api = fs.readFileSync(path.join(featureRoot, "api/reviews-api.ts"), "utf8");
  assert.match(api, /lib\/admin\/admin-result/);
  assert.match(api, /\/v1\/admin\/reviews/);
  assert.doesNotMatch(api, /from ["'].*admin-api/);
  const page = mapAdminReviews({
    Reviews: [{
      ReviewId: "r1",
      AuthorDisplayName: "سارا",
      ProductTitle: "پیراهن",
      Rating: 4,
      Body: "نظر واقعی مشتری",
      VerifiedPurchase: true,
      Status: "Pending",
      CreatedAt: "2026-08-25T00:00:00Z",
      ActorUserId: "private",
    }],
    Page: 1,
    PageSize: 20,
    TotalCount: 1,
  });
  assert.equal(page?.rows[0]?.reviewerDisplayName, "سارا");
  assert.equal(page?.rows[0]?.verifiedPurchase, true);
  assert.equal("ActorUserId" in (page?.rows[0] ?? {}), false);
});

test("reviews screen keeps moderate actions and grid markers", () => {
  const screen = fs.readFileSync(path.join(featureRoot, "components/reviews-screen.tsx"), "utf8");
  assert.match(screen, /testId=["']admin-reviews["']/);
  assert.match(screen, /queryAdminReviewsGrid/);
  assert.match(screen, /moderateAdminReview/);
  assert.match(screen, /["']use client["']/);
});

test("admin-api no longer owns reviews capability exports", () => {
  const api = fs.readFileSync(path.join(feRoot, "app/admin/admin-api.ts"), "utf8");
  assert.doesNotMatch(api, /AdminReviewRow/);
  assert.doesNotMatch(api, /loadAdminReviews/);
  assert.doesNotMatch(api, /moderateAdminReview/);
  assert.doesNotMatch(api, /queryAdminReviewsGrid/);
  assert.doesNotMatch(api, /mapAdminReviews/);
});

test("review list and moderation expose server denied", async () => {
  const originalFetch = globalThis.fetch;
  globalThis.fetch = (async () => new Response(null, { status: 403 })) as typeof fetch;
  try {
    assert.equal((await loadAdminReviews()).state, "denied");
    assert.equal((await moderateAdminReview("r1", "publish")).state, "denied");
  } finally {
    globalThis.fetch = originalFetch;
  }
});
