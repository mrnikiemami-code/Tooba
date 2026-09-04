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

test("content category language tab survives node selection via query and workspace locale", () => {
  assert.match(screen, /useSearchParams/);
  assert.match(screen, /categoriesHref/);
  assert.match(screen, /language=\$\{encodeURIComponent/);
  assert.match(screen, /setLanguageCode\(data\.languageCode\)/);
  assert.match(screen, /router\.push\(categoriesHref\(languageCode, id\)\)/);
  assert.match(screen, /router\.push\(categoriesHref\(opt\.code\)\)/);
  // Must not hard-seed fa-IR as the only initial language (remount would wipe EN tab).
  assert.doesNotMatch(screen, /useState<string>\("fa-IR"\)/);
});
