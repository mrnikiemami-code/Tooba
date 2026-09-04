import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { join } from "node:path";
import test from "node:test";

const api = readFileSync(join(import.meta.dirname, "content-category-api.ts"), "utf8");
const screen = readFileSync(join(import.meta.dirname, "content-category-admin-screen.tsx"), "utf8");

test("content category api targets content-owned endpoints", () => {
  assert.match(api, /\/v1\/admin\/content\/categories\/tree/);
  assert.match(api, /languageCode/);
  assert.doesNotMatch(api, /\/v1\/admin\/catalog\/categories/);
});

test("content category admin uses canonical AppCategoryTree and route selection", () => {
  assert.match(screen, /AppCategoryTree/);
  assert.match(screen, /\/admin\/content\/categories\//);
  assert.match(screen, /loading=\{loading\}/);
  assert.match(screen, /minmax\(320px,48%\)/);
  assert.match(screen, /parentOptions/);
  assert.match(screen, /loadAdminLanguages/);
  assert.doesNotMatch(screen, /LANGUAGE_OPTIONS/);
  assert.doesNotMatch(screen, /content-category-tree-search/);
  assert.match(screen, /در حال بارگذاری…/);
});
