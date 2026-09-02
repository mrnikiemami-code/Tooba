import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { join } from "node:path";
import test from "node:test";

const api = readFileSync(join(import.meta.dirname, "../content/content-api.ts"), "utf8");
const screen = readFileSync(join(import.meta.dirname, "content-article-admin-screen.tsx"), "utf8");
const list = readFileSync(join(import.meta.dirname, "content-list.tsx"), "utf8");
const newScreen = readFileSync(join(import.meta.dirname, "content-article-new-screen.tsx"), "utf8");
const dialog = readFileSync(join(import.meta.dirname, "content-article-destructive-dialog.tsx"), "utf8");

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

test("article action flows use canonical Dialog with zero window.confirm", () => {
  assert.doesNotMatch(screen, /window\.confirm/);
  assert.doesNotMatch(list, /window\.confirm/);
  assert.doesNotMatch(dialog, /window\.confirm/);
  assert.doesNotMatch(list, /confirm:\s*\(row\)/);
  assert.match(screen, /ContentArticleDestructiveDialog/);
  assert.match(list, /ContentArticleDestructiveDialog/);
  assert.match(dialog, /from "\.\.\/\.\.\/design-system"/);
  assert.match(dialog, /حذف مقاله/);
  assert.match(dialog, /بایگانی مقاله/);
  assert.match(dialog, /انتشار مقاله/);
  assert.match(dialog, /لغو انتشار مقاله/);
  assert.match(dialog, /Publish article/);
  assert.match(dialog, /Unpublish article/);
  assert.match(dialog, /Delete article/);
  assert.match(dialog, /Archive article/);
  assert.match(dialog, /"publish"/);
  assert.match(dialog, /"unpublish"/);
  assert.match(list, /onRequestAction\("publish"/);
  assert.match(list, /onRequestAction\("unpublish"/);
  assert.match(screen, /setDestructiveKind\(isPublished\(article\.status\) \? "unpublish" : "publish"\)/);
});
