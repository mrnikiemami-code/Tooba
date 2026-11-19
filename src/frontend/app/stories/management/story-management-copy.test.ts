import assert from "node:assert/strict";
import test from "node:test";
import {
  enStoryCopy,
  faStoryCopy,
  resolveStoryUiLocale,
  storyCopy,
} from "./story-management-copy.ts";

test("storyCopy switches catalogs by locale without scattering keys", () => {
  assert.equal(storyCopy("fa").mainStorefront, faStoryCopy.mainStorefront);
  assert.equal(storyCopy("en").mainStorefront, enStoryCopy.mainStorefront);
  assert.notEqual(faStoryCopy.cancel, enStoryCopy.cancel);
  assert.equal(Object.keys(faStoryCopy).sort().join(","), Object.keys(enStoryCopy).sort().join(","));
});

test("resolveStoryUiLocale reads route segment", () => {
  assert.equal(resolveStoryUiLocale("/en/admin/stories"), "en");
  assert.equal(resolveStoryUiLocale("/fa/vendor-panel/stories"), "fa");
  assert.equal(resolveStoryUiLocale("/admin/stories"), "fa");
});
