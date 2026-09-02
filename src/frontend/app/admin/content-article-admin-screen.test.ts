import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { join } from "node:path";
import test from "node:test";

const api = readFileSync(join(import.meta.dirname, "../content/content-api.ts"), "utf8");
const screen = readFileSync(join(import.meta.dirname, "content-article-admin-screen.tsx"), "utf8");
const list = readFileSync(join(import.meta.dirname, "content-list.tsx"), "utf8");
const newScreen = readFileSync(join(import.meta.dirname, "content-article-new-screen.tsx"), "utf8");

test("content article api exposes workspace load/update and locale helpers", () => {
  assert.match(api, /loadAdminArticle/);
  assert.match(api, /updateAdminArticle/);
  assert.match(api, /isArticleLocaleLocked/);
  assert.match(api, /articleEditorDirection/);
  assert.match(api, /\/v1\/admin\/content\/articles\//);
});

test("content article admin workspace uses TipTap, tabs, and DAM", () => {
  assert.match(screen, /ProductRichTextEditor/);
  assert.match(screen, /MediaLibraryDialog/);
  assert.match(screen, /useAdminFormMode/);
  assert.match(screen, /content-article-tab-/);
  assert.match(screen, /دسته‌بندی‌ها/);
  assert.match(screen, /loadAdminArticle/);
});

test("content list links to language-first create and article workspace edit", () => {
  assert.match(list, /\/admin\/content\/articles\/new/);
  assert.match(list, /\/admin\/content\/articles\/\$\{/);
  assert.match(list, /formatArticleLocaleLabel/);
  assert.doesNotMatch(list, /showCreate/);
  assert.match(newScreen, /LANGUAGE_OPTIONS/);
  assert.match(newScreen, /createAdminArticle/);
});
