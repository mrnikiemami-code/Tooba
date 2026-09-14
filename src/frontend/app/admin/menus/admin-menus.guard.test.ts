import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const dir = dirname(fileURLToPath(import.meta.url));
const list = readFileSync(join(dir, "admin-menus-screen.tsx"), "utf8");
const editor = readFileSync(join(dir, "admin-menu-editor.tsx"), "utf8");
const pickers = readFileSync(join(dir, "admin-menu-pickers.tsx"), "utf8");
const dest = readFileSync(join(dir, "admin-menu-destination.ts"), "utf8");
const api = readFileSync(join(dir, "admin-menus-api.ts"), "utf8");

test("menu admin list is human-readable and hides technical ids", () => {
  assert.match(list, /نام منو/);
  assert.match(list, /تعداد آیتم/);
  assert.match(list, /ایجاد منو/);
  assert.doesNotMatch(list, />شناسه منو<|>MenuId</);
  assert.doesNotMatch(list, /localStorage/);
});

test("menu editor is hierarchical with Persian destinations", () => {
  assert.match(editor, /افزودن آیتم اصلی/);
  assert.match(editor, /menu-tree/);
  assert.match(dest, /صفحهٔ فرود/);
  assert.match(pickers, /AdminSearchableCombobox/);
  assert.doesNotMatch(editor, /dangerouslySetInnerHTML|contentEditable/);
});

test("menu API stays on admin menus routes", () => {
  assert.match(api, /\/v1\/admin\/menus/);
  assert.match(api, /\/v1\/admin\/menus\/header/);
});
