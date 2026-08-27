import assert from "node:assert/strict";
import test from "node:test";
import {
  ADMIN_STORY_CAPABILITIES,
  SELLER_STORY_CAPABILITIES,
  canEditSellerStory,
  canSubmitStory,
} from "./story-capabilities.ts";

test("seller capabilities forbid review and publish actions", () => {
  assert.equal(SELLER_STORY_CAPABILITIES.mode, "seller");
  assert.equal(SELLER_STORY_CAPABILITIES.canReview, false);
  assert.equal(SELLER_STORY_CAPABILITIES.canPublish, false);
  assert.equal(SELLER_STORY_CAPABILITIES.canSchedule, false);
  assert.equal(SELLER_STORY_CAPABILITIES.canDisable, false);
  assert.equal(SELLER_STORY_CAPABILITIES.canSubmit, true);
  assert.equal(SELLER_STORY_CAPABILITIES.canCreate, true);
  assert.equal(SELLER_STORY_CAPABILITIES.canEdit, true);
  assert.equal(SELLER_STORY_CAPABILITIES.showOrigin, false);
  assert.equal(SELLER_STORY_CAPABILITIES.showSellerOwner, false);
});

test("admin capabilities allow review and publish", () => {
  assert.equal(ADMIN_STORY_CAPABILITIES.mode, "admin");
  assert.equal(ADMIN_STORY_CAPABILITIES.canReview, true);
  assert.equal(ADMIN_STORY_CAPABILITIES.canPublish, true);
  assert.equal(ADMIN_STORY_CAPABILITIES.canSchedule, true);
  assert.equal(ADMIN_STORY_CAPABILITIES.canSubmit, false);
  assert.equal(ADMIN_STORY_CAPABILITIES.showOrigin, true);
  assert.equal(ADMIN_STORY_CAPABILITIES.showSellerOwner, true);
});

test("seller submit/edit gates follow review lifecycle", () => {
  assert.equal(canSubmitStory("None"), true);
  assert.equal(canSubmitStory("Rejected"), true);
  assert.equal(canSubmitStory("Submitted"), false);
  assert.equal(canSubmitStory("Approved"), false);
  assert.equal(canEditSellerStory("None"), true);
  assert.equal(canEditSellerStory("Rejected"), true);
  assert.equal(canEditSellerStory("Submitted"), false);
  assert.equal(canEditSellerStory("Approved"), false);
});
