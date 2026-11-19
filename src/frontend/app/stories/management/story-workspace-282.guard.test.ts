import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)));
const screen = fs.readFileSync(path.join(root, "StoryManagementScreen.tsx"), "utf8");
const vendorStories = fs.readFileSync(
  path.resolve(root, "../../vendor-panel/stories/page.tsx"),
  "utf8",
);

test("shared story workspace uses merchant labels and owning media uploads", () => {
  assert.match(screen, /استوری جدید/);
  assert.match(screen, /ذخیره پیش‌نویس/);
  assert.match(screen, /ارسال برای تأیید/);
  assert.match(screen, /عکس یا ویدیو/);
  assert.match(screen, /uploadSellerStoryMedia/);
  assert.match(screen, /uploadAdminStoryMediaForShop/);
  assert.match(screen, /uploadAdminStoryMedia/);
  assert.match(screen, /getAdminStory/);
  assert.match(screen, /getSellerStory/);
  assert.match(screen, /querySellerStories/);
  assert.match(screen, /AppGridRowActionsCell/);
  assert.match(screen, /react-toastify/);
  assert.doesNotMatch(screen, /window\.prompt|window\.alert|window\.confirm/);
  assert.doesNotMatch(screen, /createClientGridQueryAdapter/);
});

test("vendor panel stories page keeps shared StoryManagementScreen", () => {
  assert.match(vendorStories, /StoryManagementScreen/);
  assert.match(vendorStories, /SELLER_STORY_CAPABILITIES/);
});
