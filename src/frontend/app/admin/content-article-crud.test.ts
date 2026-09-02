import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { join } from "node:path";
import test from "node:test";

const api = readFileSync(join(import.meta.dirname, "../content/content-api.ts"), "utf8");
const screen = readFileSync(join(import.meta.dirname, "content-article-admin-screen.tsx"), "utf8");
const list = readFileSync(join(import.meta.dirname, "content-list.tsx"), "utf8");
const newScreen = readFileSync(join(import.meta.dirname, "content-article-new-screen.tsx"), "utf8");

test("create uses dedicated route not modal", () => {
  assert.match(list, /\/admin\/content\/articles\/new/);
  assert.doesNotMatch(list, /showCreate|CreateModal|modal/i);
  assert.match(newScreen, /content-article-new/);
  assert.match(newScreen, /router\.push/);
});

test("workspace has explicit view and edit modes", () => {
  assert.match(screen, /useAdminFormMode/);
  assert.match(screen, /form\.mode === "view"/);
  assert.match(screen, /content-article-body-view/);
  assert.match(screen, /form\.onEdit/);
  assert.match(screen, /handleCancel/);
  assert.match(screen, /content-article-save/);
});

test("list exposes view edit delete archive actions", () => {
  assert.match(list, /admin-content-view-/);
  assert.match(list, /\?mode=edit/);
  assert.match(list, /admin-content-delete-/);
  assert.match(list, /admin-content-archive-/);
  assert.match(list, /بایگانی/);
});

test("content api exposes lifecycle delete archive helpers", () => {
  assert.match(api, /deleteAdminArticle/);
  assert.match(api, /archiveAdminArticle/);
  assert.match(api, /canHardDeleteArticle/);
  assert.match(api, /canArchiveArticle/);
  assert.match(api, /method: "DELETE"/);
  assert.match(api, /\/archive/);
});

test("destructive actions labeled delete vs archive", () => {
  assert.match(screen, /content-article-delete/);
  assert.match(screen, /content-article-archive/);
  assert.match(screen, /حذف/);
  assert.match(screen, /بایگانی/);
});
