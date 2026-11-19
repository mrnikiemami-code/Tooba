import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)));
const modal = fs.readFileSync(path.join(root, "story-modal.tsx"), "utf8");
const home = fs.readFileSync(path.join(root, "home-stories.tsx"), "utf8");

test("public story modal removes fake social persistence controls", () => {
  assert.doesNotMatch(modal, /\bHeart\b/);
  assert.doesNotMatch(modal, /\bMessageCircle\b/);
  assert.doesNotMatch(modal, /\bBookmark\b/);
  assert.doesNotMatch(modal, /toggleLike|handleCommentSubmit|isLiked|isSaved/);
  assert.match(modal, /story-shop-link|story-shop-name/);
  assert.match(modal, /timeupdate/);
  assert.match(modal, /ended/);
  assert.doesNotMatch(modal, /\sloop\s/);
});

test("home stories open eligible share target from story query", () => {
  assert.match(home, /\.get\("story"\)/);
  assert.match(home, /shopDisplayName/);
});
